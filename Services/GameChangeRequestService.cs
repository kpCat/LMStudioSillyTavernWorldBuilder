using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameChangeRequestService
{
    private readonly GameDesignInterviewService _designInterviewService = new();
    private readonly GameRandomDirectorService _randomDirectorService = new();
    private readonly GameBalanceSimulatorService _balanceSimulatorService = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public GameChangeRequestImpactReport AnalyzeRequest(GameProjectData project, string userRequest)
    {
        var request = userRequest.Trim();
        var report = new GameChangeRequestImpactReport
        {
            UserRequest = request,
            Intent = DetectIntent(request).ToString(),
            Confidence = string.IsNullOrWhiteSpace(request) ? 0 : 0.45
        };

        if (string.IsNullOrWhiteSpace(request))
        {
            AddRisk(report, "empty_request", "Запрос пустой. Нужна хотя бы короткая формулировка правки.", "error");
            report.Summary = "Запрос на изменение не задан.";
            report.MissingContextQuestions.Add("Какую часть игры нужно изменить?");
            return report;
        }

        foreach (var mapping in SystemMappings)
        {
            if (ContainsAny(request, mapping.Keywords))
            {
                AddSystems(report, mapping.SystemIds, mapping.Reason);
            }
        }

        var matchedEntities = FindEntityMatches(project, request);
        foreach (var match in matchedEntities)
        {
            AddSystems(report, match.SystemIds, "Запрос упоминает существующую сущность: " + match.DisplayName);
            foreach (var systemId in match.SystemIds)
            {
                AddEntityToSystem(report, systemId, match.Id);
            }
        }

        report.AffectedEntityIds = matchedEntities
            .Select(x => x.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AddIntentSpecificSystems(report, request);
        AddRisks(report, request);
        AddRecommendedStages(report);

        if (report.AffectedSystems.Count == 0)
        {
            AddSystems(report, new[] { "meta", "brief", "concept" }, "Запрос не попал в точную карту систем, поэтому v1 ограничит patch общим редакторским контекстом.");
            report.MissingContextQuestions.Add("Какие сущности или механики нужно затронуть в первую очередь?");
        }

        report.Confidence = CalculateConfidence(report);
        report.Summary = BuildSummary(report);
        return report;
    }

    public GameChangeRequestPatchPlan BuildPatchPlan(GameProjectData project, GameChangeRequestImpactReport report)
    {
        var plan = new GameChangeRequestPatchPlan
        {
            Title = "Draft правки: " + Preview(report.UserRequest, 80),
            UserRequest = report.UserRequest,
            Summary = "Малый patch через существующий draft workflow; автоприменение запрещено."
        };

        if (string.IsNullOrWhiteSpace(report.UserRequest))
        {
            AddStep(plan, "clarify_request", "Уточнить запрос", "Сформулировать конкретную правку перед генерацией JSON patch.", "change-request", 10, ["meta"], []);
            plan.ContextNotes.Add("Пустой запрос не должен отправляться на генерацию без уточнения.");
            return plan;
        }

        if (IsBroadRewrite(report.UserRequest))
        {
            AddStep(plan, "scope_change", "Сузить правку", "Сохранить запрос как scoped patch: диагностика плюс 1-2 безопасных изменения, не переписывая всю игру.", "change-request", 10, ["meta", "brief", "concept"], report.AffectedEntityIds);
        }

        var groupedSystems = report.AffectedSystems
            .Select(x => x.SystemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
        var stages = report.RecommendedPatchStages.Count == 0
            ? new List<string> { "change-request" }
            : report.RecommendedPatchStages.Take(5).ToList();

        var priority = plan.Steps.Count == 0 ? 10 : 20;
        foreach (var stage in stages.Take(IsBroadRewrite(report.UserRequest) ? 2 : 5))
        {
            var targets = groupedSystems
                .Where(system => StageMatchesSystem(stage, system))
                .DefaultIfEmpty(stage)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();
            AddStep(
                plan,
                "patch_" + GameProjectManifestService.SafeId(stage, "stage"),
                BuildStageTitle(stage),
                "Сгенерировать малую partial GameProjectData JSON-пачку для связанных систем: " + string.Join(", ", targets),
                stage,
                priority,
                targets,
                report.AffectedEntityIds);
            priority += 10;
        }

        if (plan.Steps.Count == 0)
        {
            AddStep(plan, "patch_change_request", "Сгенерировать draft правки", "Создать минимальный partial GameProjectData JSON patch по запросу пользователя.", "change-request", 10, groupedSystems, report.AffectedEntityIds);
        }

        plan.ContextNotes.Add("Все шаги MustUseDraftWorkflow=true: результат сохраняется как draft и не применяется автоматически.");
        if (project.WorldState.AmbientEvents.Count > 0)
        {
            plan.ContextNotes.Add("В проекте уже есть ambientEvents; новые события должны использовать уникальные ID.");
        }
        return plan;
    }

    public string BuildGenerationUserPrompt(GameProjectData project, GameChangeRequestImpactReport report, GameChangeRequestPatchPlan plan)
    {
        var affectedSystems = report.AffectedSystems
            .Select(x => x.SystemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var randomReport = _randomDirectorService.BuildReport(project);
        var model = new
        {
            Instruction = "Сгенерируй малый partial GameProjectData JSON patch через существующие модели. Не применяй изменения напрямую.",
            UserRequest = report.UserRequest,
            ImpactReport = BuildCompactImpactReport(report),
            PatchPlan = BuildCompactPatchPlan(plan),
            DesignSummary = _designInterviewService.BuildDesignSummary(project),
            RandomDirectorSummary = ShouldIncludeRandomDirector(affectedSystems)
                ? JsonSerializer.Deserialize<object>(_randomDirectorService.BuildCompactRandomDirectorSummary(project, randomReport))
                : null,
            BalanceSummary = ShouldIncludeBalanceContext(report.UserRequest, affectedSystems)
                ? JsonSerializer.Deserialize<object>(_balanceSimulatorService.BuildCompactBalanceSummary(project, _balanceSimulatorService.BuildReport(project, 10)))
                : null,
            Current = new
            {
                Meta = new
                {
                    project.Meta.Id,
                    project.Meta.Title,
                    project.Meta.Genre,
                    project.Meta.Tone,
                    Description = Preview(project.Meta.Description, 500),
                    project.Meta.StartSceneId,
                    project.Meta.Language
                },
                Brief = Preview(project.Brief.Text, 700),
                Concept = Preview(project.Concept.Text, 700),
                Mvp = Preview(project.MvpPlan.Text, 700),
                GenerationPreferences = BuildCompactGenerationPreferences(project.GenerationPreferences)
            },
            Existing = BuildAffectedExistingModel(project, affectedSystems)
        };

        return JsonSerializer.Serialize(model, _jsonOptions);
    }

    public string BuildCompactChangeRequestSummary(GameChangeRequestImpactReport report, GameChangeRequestPatchPlan plan)
    {
        return JsonSerializer.Serialize(new
        {
            report.Summary,
            report.Intent,
            report.Confidence,
            AffectedSystems = report.AffectedSystems.Select(x => new { x.SystemId, x.Severity, EntityIds = x.EntityIds.Take(8) }),
            report.AffectedEntityIds,
            Risks = report.Risks.Select(x => new { x.Code, x.Severity, x.Message, EntityIds = x.EntityIds.Take(8) }),
            PlanSteps = plan.Steps.Select(x => new { x.Id, x.TargetStage, x.Priority, x.TargetSystems, x.EntityIds, x.MustUseDraftWorkflow })
        }, _jsonOptions);
    }

    public string FormatReportForUi(GameChangeRequestImpactReport report, GameChangeRequestPatchPlan? plan = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== Запрос на изменение игры v1 ===");
        builder.AppendLine(report.Summary);
        builder.AppendLine();
        builder.AppendLine($"Intent: {report.Intent}");
        builder.AppendLine($"Confidence: {report.Confidence:0.##}");
        builder.AppendLine();

        builder.AppendLine("Затронутые системы:");
        if (report.AffectedSystems.Count == 0)
        {
            builder.AppendLine("- нет");
        }
        else
        {
            foreach (var system in report.AffectedSystems.OrderBy(x => x.SystemId, StringComparer.OrdinalIgnoreCase))
            {
                var entityText = system.EntityIds.Count == 0 ? "" : " ids=" + string.Join(", ", system.EntityIds.Take(12));
                builder.AppendLine($"- [{system.Severity}] {system.SystemId}: {system.Reason}{entityText}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Затронутые сущности:");
        builder.AppendLine(report.AffectedEntityIds.Count == 0 ? "- нет точных совпадений" : "- " + string.Join(", ", report.AffectedEntityIds));

        builder.AppendLine();
        builder.AppendLine("Риски:");
        if (report.Risks.Count == 0)
        {
            builder.AppendLine("- нет");
        }
        else
        {
            foreach (var risk in report.Risks)
            {
                builder.AppendLine($"- [{risk.Severity}] {risk.Code}: {risk.Message}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Недостающий контекст:");
        builder.AppendLine(report.MissingContextQuestions.Count == 0 ? "- нет" : string.Join(Environment.NewLine, report.MissingContextQuestions.Select(x => "- " + x)));

        if (plan != null)
        {
            builder.AppendLine();
            builder.AppendLine("План draft patch:");
            foreach (var step in plan.Steps.OrderBy(x => x.Priority))
            {
                builder.AppendLine($"- {step.Priority:00} {step.Title} ({step.TargetStage})");
                builder.AppendLine("  " + step.Description);
                builder.AppendLine("  draft workflow: " + (step.MustUseDraftWorkflow ? "да" : "нет"));
            }
        }

        return builder.ToString();
    }

    private static void AddIntentSpecificSystems(GameChangeRequestImpactReport report, string request)
    {
        var intent = ParseIntent(report.Intent);
        if (intent == GameChangeRequestIntent.RemoveOrReduceContent)
        {
            AddSystems(report, new[] { "meta" }, "Remove/reduce запрос требует безопасной scoped-правки без удаления.");
        }
        if (intent == GameChangeRequestIntent.FixIssue && report.AffectedSystems.Count == 0)
        {
            AddSystems(report, new[] { "scenes", "quests", "variables" }, "Fix запрос без точной системы: проверяются самые частые runtime-ссылки.");
        }
        if (ContainsAny(request, "одеж", "брон", "экип", "clothes", "armor"))
        {
            AddSystems(report, new[] { "requirements", "locations", "scenes" }, "Одежда/экипировка часто влияет на условия доступа и социальные проверки.");
        }
    }

    private static void AddRisks(GameChangeRequestImpactReport report, string request)
    {
        if (ParseIntent(report.Intent) == GameChangeRequestIntent.RemoveOrReduceContent)
        {
            AddRisk(report, "destructive_delete_not_supported", "v1 не поддерживает destructive delete patches. Допустимы только безопасная перенастройка, замена или additive corrective patch через текущую схему.", "warning", report.AffectedEntityIds);
        }
        if (ParseIntent(report.Intent) == GameChangeRequestIntent.Rebalance || ContainsAny(request, "баланс", "гринд", "сложн", "цена"))
        {
            AddRisk(report, "balance_simulator_not_run", "Balance Simulator в этой задаче не запускается; draft будет эвристическим.", "info", report.AffectedEntityIds);
        }
        if (report.AffectedEntityIds.Count == 0 && LooksSpecific(request))
        {
            AddRisk(report, "unknown_reference", "Запрос выглядит конкретным, но существующие ID/имена не найдены. LLM получит только связанные системы и может потребоваться ручное уточнение.", "warning");
            report.MissingContextQuestions.Add("Какие ID или названия сущностей нужно изменить?");
        }
        if (IsBroadRewrite(request))
        {
            AddRisk(report, "broad_rewrite_scoped", "v1 создаёт scoped patch, а не переписывает весь проект.", "warning");
        }
    }

    private static void AddRecommendedStages(GameChangeRequestImpactReport report)
    {
        var stages = new List<string>();
        foreach (var system in report.AffectedSystems.Select(x => x.SystemId))
        {
            stages.Add(SystemToStage(system));
        }

        report.RecommendedPatchStages = stages
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
        if (report.RecommendedPatchStages.Count == 0 && !string.IsNullOrWhiteSpace(report.UserRequest))
        {
            report.RecommendedPatchStages.Add("change-request");
        }
    }

    private static GameChangeRequestIntent DetectIntent(string request)
    {
        if (string.IsNullOrWhiteSpace(request)) return GameChangeRequestIntent.Other;
        if (ContainsAny(request, "убери", "удали", "меньше", "сократи", "слишком много", "не нравится", "remove", "delete", "reduce")) return GameChangeRequestIntent.RemoveOrReduceContent;
        if (ContainsAny(request, "сломано", "ошибка", "не работает", "не выпадает", "не открывается", "softlock", "fix", "bug")) return GameChangeRequestIntent.FixIssue;
        if (ContainsAny(request, "рандом", "случайн", "событ", "вариатив", "переигр", "travel event")) return GameChangeRequestIntent.ImproveRandomness;
        if (ContainsAny(request, "бой", "боёв", "враг", "урон", "сложн", "тактик", "encounter")) return GameChangeRequestIntent.ImproveCombat;
        if (ContainsAny(request, "диалог", "персонаж", "npc", "отнош", "роман", "репутац")) return GameChangeRequestIntent.ImproveDialogue;
        if (ContainsAny(request, "инвент", "предмет", "брон", "одеж", "экип", "оруж", "лут")) return GameChangeRequestIntent.ImproveInventory;
        if (ContainsAny(request, "баланс", "гринд", "прокач", "xp", "уров")) return GameChangeRequestIntent.Rebalance;
        if (ContainsAny(request, "эконом", "валют", "цена")) return GameChangeRequestIntent.ImproveEconomy;
        if (ContainsAny(request, "карта", "локац", "путешеств", "маршрут", "переход")) return GameChangeRequestIntent.ImproveMapTravel;
        if (ContainsAny(request, "атмосфер", "лор", "тон", "мрач", "юмор", "стиль")) return GameChangeRequestIntent.RewriteTone;
        if (ContainsAny(request, "добав", "нов", "ещё", "больше", "расшир")) return GameChangeRequestIntent.AddContent;
        return GameChangeRequestIntent.Other;
    }

    private static GameChangeRequestIntent ParseIntent(string value)
    {
        return Enum.TryParse<GameChangeRequestIntent>(value, true, out var intent) ? intent : GameChangeRequestIntent.Other;
    }

    private static double CalculateConfidence(GameChangeRequestImpactReport report)
    {
        var confidence = 0.35;
        if (report.AffectedSystems.Count > 0) confidence += 0.3;
        if (report.AffectedEntityIds.Count > 0) confidence += 0.2;
        if (report.Risks.Any(x => x.Severity == "error")) confidence -= 0.25;
        if (report.Risks.Any(x => x.Code == "unknown_reference")) confidence -= 0.1;
        return Math.Clamp(confidence, 0, 1);
    }

    private static string BuildSummary(GameChangeRequestImpactReport report)
    {
        var systems = report.AffectedSystems.Select(x => x.SystemId).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
        var systemText = systems.Count == 0 ? "системы не определены" : string.Join(", ", systems);
        var entityText = report.AffectedEntityIds.Count == 0 ? "точных сущностей не найдено" : "сущности: " + string.Join(", ", report.AffectedEntityIds.Take(8));
        return $"Запрос классифицирован как {report.Intent}. Затронуты: {systemText}; {entityText}.";
    }

    private static object BuildCompactImpactReport(GameChangeRequestImpactReport report)
    {
        return new
        {
            report.Summary,
            report.Intent,
            report.Confidence,
            AffectedSystems = report.AffectedSystems.Select(x => new { x.SystemId, x.Reason, x.Severity, EntityIds = x.EntityIds.Take(12) }),
            report.AffectedEntityIds,
            Risks = report.Risks.Select(x => new { x.Code, x.Message, x.Severity, EntityIds = x.EntityIds.Take(12) }),
            report.MissingContextQuestions,
            report.RecommendedPatchStages
        };
    }

    private static object BuildCompactPatchPlan(GameChangeRequestPatchPlan plan)
    {
        return new
        {
            plan.Title,
            plan.Summary,
            Steps = plan.Steps.OrderBy(x => x.Priority).Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.TargetStage,
                x.Priority,
                x.TargetSystems,
                x.EntityIds,
                x.MustUseDraftWorkflow
            }),
            plan.ContextNotes
        };
    }

    private static object BuildCompactGenerationPreferences(GameGenerationPreferences preferences)
    {
        return new
        {
            GeneralGameplayText = Preview(preferences.GeneralGameplayText, 700),
            SkillDesignText = Preview(preferences.SkillDesignText, 700),
            ProgressionDesignText = Preview(preferences.ProgressionDesignText, 700),
            CombatDesignText = Preview(preferences.CombatDesignText, 700),
            AtmosphereDesignText = Preview(preferences.AtmosphereDesignText, 700),
            BalanceText = Preview(preferences.BalanceText, 700),
            ForbiddenDesignText = Preview(preferences.ForbiddenDesignText, 700),
            Notes = Preview(preferences.Notes, 700)
        };
    }

    private static object BuildAffectedExistingModel(GameProjectData project, HashSet<string> systems)
    {
        var includeAll = systems.Count == 0;
        return new
        {
            Stats = Include(includeAll, systems, "stats", "progression", "formulas", "requirements")
                ? project.Stats.Select(x => new { x.Id, x.Name, x.Kind, x.IsResource, x.Tags }).Take(40)
                : null,
            Items = Include(includeAll, systems, "items", "equipmentSlots", "requirements", "effects")
                ? project.Items.Select(x => new { x.Id, x.Name, x.Type, x.SlotId, x.Tags, x.IsEquippable, x.Requirements, x.Modifiers }).Take(40)
                : null,
            EquipmentSlots = Include(includeAll, systems, "equipmentSlots", "items")
                ? project.EquipmentSlots.Select(x => new { x.Id, x.Name, x.AllowedItemTags }).Take(30)
                : null,
            Skills = Include(includeAll, systems, "skills", "actions", "combat", "progression")
                ? project.Skills.Select(x => new { x.Id, x.Name, x.Kind, x.Tags }).Take(40)
                : null,
            Actions = Include(includeAll, systems, "actions", "combat")
                ? project.Actions.Select(x => new { x.Id, x.Name, x.Kind, x.Tags, x.AvailableInCombat, x.TargetScope }).Take(40)
                : null,
            Encounters = Include(includeAll, systems, "encounters", "combat")
                ? project.Encounters.Select(x => new { x.Id, x.Name, x.Kind, x.SceneId, x.VictorySceneId, x.DefeatSceneId }).Take(40)
                : null,
            Combat = Include(includeAll, systems, "combat") ? project.Combat : null,
            Formulas = Include(includeAll, systems, "formulas", "combat", "progression")
                ? project.Formulas.Select(x => new { x.Id, x.Name, x.Expression }).Take(40)
                : null,
            StatusEffects = Include(includeAll, systems, "statusEffects", "combat")
                ? project.StatusEffects.Select(x => new { x.Id, x.Name, x.Kind, x.Tags }).Take(40)
                : null,
            Characters = Include(includeAll, systems, "characters", "relationships", "scenes")
                ? project.Characters.Select(x => new { x.Id, x.Name, x.Role, x.LocationId }).Take(40)
                : null,
            Relationships = Include(includeAll, systems, "relationships", "characters")
                ? project.Relationships.Select(x => new { x.CharacterId, x.Name, x.InitialValue }).Take(40)
                : null,
            Locations = Include(includeAll, systems, "locations", "travel", "worldState", "requirements")
                ? project.Locations.Select(x => new { x.Id, x.Name, x.RegionId, x.StatusId, x.Tags, AccessRequirementCount = x.AccessRequirements.Count }).Take(40)
                : null,
            LocationConnections = Include(includeAll, systems, "locationConnections", "travel")
                ? project.LocationConnections.Select(x => new { x.Id, x.FromLocationId, x.ToLocationId, x.IsTwoWay }).Take(40)
                : null,
            Scenes = Include(includeAll, systems, "scenes", "quests", "characters", "requirements")
                ? project.Scenes.Select(x => new { x.Id, x.Title, x.LocationId, ChoiceCount = x.Choices.Count }).Take(40)
                : null,
            Quests = Include(includeAll, systems, "quests", "scenes")
                ? project.Quests.Select(x => new { x.Id, x.Title, x.IsActiveByDefault }).Take(40)
                : null,
            Currencies = Include(includeAll, systems, "currencies", "items", "progression")
                ? project.Currencies.Select(x => new { x.Id, x.Name, x.InitialAmount }).Take(30)
                : null,
            Variables = Include(includeAll, systems, "variables", "quests", "scenes", "progression")
                ? project.Variables.Select(x => new { x.Id, x.Name, x.IsHidden }).Take(40)
                : null,
            ProgressionNodes = Include(includeAll, systems, "progression")
                ? project.ProgressionNodes.Select(x => new { x.Id, x.Name, x.Kind, x.SkillId, x.ParentNodeIds }).Take(40)
                : null,
            WorldState = Include(includeAll, systems, "worldState", "ambientEvents", "worldRules", "travel")
                ? new
                {
                    project.WorldState.Enabled,
                    project.WorldState.GenreProfile,
                    TimeSegments = project.WorldState.Time.Segments.Select(x => new { x.Id, x.Name, x.Order, x.Tags }).Take(30),
                    Aspects = project.WorldState.Aspects.Select(x => new { x.Id, x.Name, x.Kind, x.DefaultStateId }).Take(30),
                    AmbientEvents = project.WorldState.AmbientEvents.Select(x => new { x.Id, x.Name, x.Kind, x.Trigger, x.LocationIds, x.LocationTags, x.Tags }).Take(50),
                    Rules = project.WorldState.Rules.Select(x => new { x.Id, x.Name, x.Trigger, x.Tags }).Take(40)
                }
                : null
        };
    }

    private static bool Include(bool includeAll, HashSet<string> systems, params string[] names)
    {
        return includeAll || names.Any(x => systems.Contains(x));
    }

    private static bool ShouldIncludeRandomDirector(HashSet<string> systems)
    {
        return systems.Count == 0
            || systems.Contains("worldState")
            || systems.Contains("ambientEvents")
            || systems.Contains("worldRules")
            || systems.Contains("locations")
            || systems.Contains("travel");
    }

    private static bool ShouldIncludeBalanceContext(string request, HashSet<string> systems)
    {
        return systems.Contains("combat")
            || systems.Contains("encounters")
            || systems.Contains("progression")
            || systems.Contains("currencies")
            || systems.Contains("items")
            || systems.Contains("stats")
            || ContainsAny(request, "баланс", "сложн", "гринд", "прокач", "эконом", "ресурс", "цена", "цены", "xp", "experience", "progression", "economy", "resource", "too easy", "too hard", "too long", "too short", "difficulty");
    }

    private static void AddSystems(GameChangeRequestImpactReport report, IEnumerable<string> systemIds, string reason)
    {
        foreach (var systemId in systemIds)
        {
            var existing = report.AffectedSystems.FirstOrDefault(x => string.Equals(x.SystemId, systemId, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                report.AffectedSystems.Add(new GameChangeRequestAffectedSystem
                {
                    SystemId = systemId,
                    Reason = reason,
                    Severity = "info"
                });
            }
        }
    }

    private static void AddEntityToSystem(GameChangeRequestImpactReport report, string systemId, string entityId)
    {
        var system = report.AffectedSystems.FirstOrDefault(x => string.Equals(x.SystemId, systemId, StringComparison.OrdinalIgnoreCase));
        if (system != null && !system.EntityIds.Contains(entityId, StringComparer.OrdinalIgnoreCase))
        {
            system.EntityIds.Add(entityId);
            system.Severity = "warning";
        }
    }

    private static void AddRisk(GameChangeRequestImpactReport report, string code, string message, string severity, IEnumerable<string>? entityIds = null)
    {
        report.Risks.Add(new GameChangeRequestRisk
        {
            Code = code,
            Message = message,
            Severity = severity,
            EntityIds = entityIds?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>()
        });
    }

    private static void AddStep(GameChangeRequestPatchPlan plan, string id, string title, string description, string targetStage, int priority, List<string> targetSystems, List<string> entityIds)
    {
        plan.Steps.Add(new GameChangeRequestPlanStep
        {
            Id = id,
            Title = title,
            Description = description,
            TargetStage = targetStage,
            Priority = priority,
            TargetSystems = targetSystems.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            EntityIds = entityIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            MustUseDraftWorkflow = true
        });
    }

    private static List<EntityMatch> FindEntityMatches(GameProjectData project, string request)
    {
        var result = new List<EntityMatch>();
        AddEntityMatches(result, request, project.Locations.Select(x => (x.Id, x.Name)), "locations");
        AddEntityMatches(result, request, project.Scenes.Select(x => (x.Id, x.Title)), "scenes");
        AddEntityMatches(result, request, project.Quests.Select(x => (x.Id, x.Title)), "quests");
        AddEntityMatches(result, request, project.Characters.Select(x => (x.Id, x.Name)), "characters", "relationships");
        AddEntityMatches(result, request, project.Items.Select(x => (x.Id, x.Name)), "items");
        AddEntityMatches(result, request, project.Skills.Select(x => (x.Id, x.Name)), "skills");
        AddEntityMatches(result, request, project.Actions.Select(x => (x.Id, x.Name)), "actions");
        AddEntityMatches(result, request, project.Encounters.Select(x => (x.Id, x.Name)), "encounters");
        AddEntityMatches(result, request, project.Currencies.Select(x => (x.Id, x.Name)), "currencies");
        AddEntityMatches(result, request, project.Variables.Select(x => (x.Id, x.Name)), "variables");
        AddEntityMatches(result, request, project.Stats.Select(x => (x.Id, x.Name)), "stats");
        AddEntityMatches(result, request, project.StatusEffects.Select(x => (x.Id, x.Name)), "statusEffects");
        AddEntityMatches(result, request, project.ProgressionNodes.Select(x => (x.Id, x.Name)), "progression");
        AddEntityMatches(result, request, project.WorldState.AmbientEvents.Select(x => (x.Id, x.Name)), "ambientEvents", "worldState");
        AddEntityMatches(result, request, project.WorldState.Rules.Select(x => (x.Id, x.Name)), "worldRules", "worldState");
        return result
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    private static void AddEntityMatches(List<EntityMatch> result, string request, IEnumerable<(string Id, string Name)> entities, params string[] systems)
    {
        foreach (var entity in entities)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                continue;
            }

            if (ContainsToken(request, entity.Id) || !string.IsNullOrWhiteSpace(entity.Name) && ContainsToken(request, entity.Name))
            {
                result.Add(new EntityMatch(entity.Id, string.IsNullOrWhiteSpace(entity.Name) ? entity.Id : entity.Name, systems));
            }
        }
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsToken(string text, string token)
    {
        return !string.IsNullOrWhiteSpace(token) && text.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksSpecific(string request)
    {
        return request.Contains('"')
            || request.Contains('\'')
            || request.Contains('_')
            || request.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(x => x.Length >= 12);
    }

    private static bool IsBroadRewrite(string request)
    {
        return ContainsAny(request, "переделай всю игру", "перепиши всю игру", "полностью переделай", "rewrite whole", "redo whole");
    }

    private static string SystemToStage(string system)
    {
        return system switch
        {
            "worldState" or "ambientEvents" or "worldRules" or "travel" => "world-state",
            "combat" or "encounters" => "combat",
            "items" => "items",
            "equipmentSlots" => "equipment",
            "skills" => "skills",
            "actions" => "gameplay-actions",
            "formulas" => "formulas",
            "statusEffects" => "status-effects",
            "progression" => "progression",
            "locations" or "locationConnections" => "locations",
            "scenes" or "quests" or "characters" or "relationships" => "scenes",
            "currencies" => "items",
            "stats" or "variables" => "stats-resources",
            "meta" or "brief" or "concept" => "change-request",
            _ => "change-request"
        };
    }

    private static bool StageMatchesSystem(string stage, string system)
    {
        return string.Equals(SystemToStage(system), stage, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stage, "change-request", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildStageTitle(string stage)
    {
        return stage switch
        {
            "world-state" => "Правка состояния мира и событий",
            "combat" => "Правка боёвки",
            "items" => "Правка предметов",
            "equipment" => "Правка экипировки",
            "skills" => "Правка навыков",
            "gameplay-actions" => "Правка действий",
            "formulas" => "Правка формул",
            "status-effects" => "Правка статусов",
            "progression" => "Правка прогрессии",
            "locations" => "Правка локаций и переходов",
            "scenes" => "Правка сцен и квестов",
            _ => "Правка проекта"
        };
    }

    private static string Preview(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "...";
    }

    private sealed record EntityMatch(string Id, string DisplayName, IReadOnlyList<string> SystemIds);

    private sealed record SystemMapping(string[] Keywords, string[] SystemIds, string Reason);

    private static readonly SystemMapping[] SystemMappings =
    {
        new(["рандом", "случайн", "событ", "вариатив", "переигр", "travel event"], ["worldState", "ambientEvents", "worldRules", "locations", "travel"], "Запрос про рандом, события или переигрываемость."),
        new(["бой", "боёв", "враг", "урон", "сложн", "тактик", "encounter"], ["encounters", "combat", "actions", "skills", "statusEffects", "formulas"], "Запрос про бой, врагов, урон или сложность."),
        new(["инвент", "предмет", "брон", "одеж", "экип", "оруж", "лут"], ["items", "equipmentSlots", "stats", "requirements", "effects"], "Запрос про инвентарь, предметы или экипировку."),
        new(["диалог", "персонаж", "npc", "отнош", "роман", "репутац"], ["characters", "scenes", "relationships", "quests"], "Запрос про диалоги, персонажей или отношения."),
        new(["сюжет", "сцен", "квест", "ветк", "финал"], ["scenes", "quests", "flags", "variables"], "Запрос про сюжет, сцены, квесты или ветвления."),
        new(["карта", "локац", "путешеств", "маршрут", "переход"], ["locations", "locationConnections", "travel", "worldState"], "Запрос про карту, локации или перемещение."),
        new(["баланс", "гринд", "прокач", "xp", "уров", "эконом", "валют", "цена"], ["progression", "stats", "currencies", "items", "formulas"], "Запрос про баланс, прогрессию или экономику."),
        new(["атмосфер", "лор", "тон", "мрач", "юмор", "стиль"], ["meta", "brief", "concept", "scenes", "locations"], "Запрос про тон, лор или атмосферу.")
    };
}
