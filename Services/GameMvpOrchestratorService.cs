using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameMvpOrchestratorService
{
    private const int TargetStatsResources = 4;
    private const int TargetFormulas = 2;
    private const int TargetActions = 3;
    private const int TargetWorldState = 1;
    private const int TargetLocations = 3;
    private const int TargetScenes = 6;
    private const int TargetItems = 5;
    private const int TargetEquipment = 1;
    private const int TargetSkills = 3;
    private const int TargetSpells = 2;
    private const int TargetEncounters = 1;
    private const int TargetCombat = 1;
    private const int TargetRandomEvents = 4;
    private const int TargetProgression = 3;
    private const int TargetBalance = 1;

    private readonly GameRandomDirectorService _randomDirectorService = new();
    private readonly GameBalanceSimulatorService _balanceSimulatorService = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public GameMvpReadinessReport BuildReadinessReport(GameProjectData project)
    {
        var report = new GameMvpReadinessReport();
        var signals = BuildSignals(project);

        AddStage(report, "design_profile", "Дизайн-досье и MVP-план", signals.HasDesignBasis ? 1 : 0, 1, 10, "Есть brief/concept/MVP/design slot или описание проекта.");
        AddStage(report, "stats_resources", "Характеристики и ресурсы", CountStatsResources(project), TargetStatsResources, 20, "Минимальный MVP должен иметь несколько параметров, ресурсов, валют или переменных.");
        AddStage(report, "formulas", "Формулы", project.Formulas.Count, TargetFormulas, 30, "Формулы нужны для проверок, урона, восстановления или прогрессии.");
        AddStage(report, "actions", "Игровые действия", project.Actions.Count, TargetActions, 40, "Действия дают игроку повторяемый интерактивный цикл.");
        AddStage(report, "world_state", "Состояние мира", CountWorldStateFoundation(project), TargetWorldState, 45, "WorldState нужен для времени, атмосферы, правил или событий.");
        AddStage(report, "locations", "Локации", project.Locations.Count, TargetLocations, 50, "Нужна стартовая карта из нескольких мест.");
        AddStage(report, "scenes", "Сцены", project.Scenes.Count, TargetScenes, 60, "Playable MVP требует достаточного числа сцен и развилок.");
        AddStage(report, "items", "Предметы", project.Items.Count, TargetItems, 70, "Предметы и награды поддерживают исследование, проверки и ресурсы.");

        if (signals.InventoryRelevant)
        {
            AddStage(report, "equipment", "Экипировка", CountEquipment(project), TargetEquipment, 75, "Инвентарь или экипировка отмечены как релевантные.");
        }
        if (signals.SkillsRelevant)
        {
            AddStage(report, "skills", "Навыки", project.Skills.Count(x => !IsSpell(x)), TargetSkills, 80, "Дизайн или механики подразумевают навыки.");
        }
        if (signals.MagicRelevant)
        {
            AddStage(report, "spells", "Заклинания", project.Skills.Count(IsSpell), TargetSpells, 82, "Магия или заклинания отмечены как релевантные.");
        }
        if (signals.CombatRelevant)
        {
            AddStage(report, "encounters", "Столкновения", project.Encounters.Count, TargetEncounters, 90, "Боевой или encounter-контур должен иметь хотя бы одно столкновение.");
            AddStage(report, "combat", "Боевой контур", CountCombatFoundation(project), TargetCombat, 92, "Для боя нужны combat-настройки или combat actions/encounters.");
        }
        if (signals.RandomnessRelevant)
        {
            AddStage(report, "random_events", "Случайные события", project.WorldState.AmbientEvents.Count, TargetRandomEvents, 100, "Рандом или путешествия требуют небольшой controlled-randomness основы.");
        }
        if (signals.ProgressionRelevant)
        {
            AddStage(report, "progression", "Прогрессия", project.ProgressionNodes.Count, TargetProgression, 110, "Прогрессия включена или запрошена дизайном.");
        }
        if (ShouldRecommendBalance(project, signals))
        {
            AddStage(report, "balance", "Проверка баланса", CountBalanceReadiness(project), TargetBalance, 120, "Контента достаточно, следующий безопасный шаг - draft правки баланса.");
        }

        AddIssues(report, project, signals);
        AddRecommendations(report);
        ApplyNextStage(report);
        ApplySummary(report, project);
        return report;
    }

    public string FormatReportForUi(GameMvpReadinessReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== MVP Orchestrator v1 ===");
        builder.AppendLine(report.Summary);
        builder.AppendLine($"Статус: {report.OverallStatus}; готовность: {report.CompletionPercent}%");
        builder.AppendLine($"Блокирующие проблемы: {(report.HasBlockingProblems ? "да" : "нет")}");
        builder.AppendLine();

        builder.AppendLine("Стадии:");
        foreach (var stage in report.Stages.OrderBy(x => x.Priority))
        {
            builder.AppendLine($"- {(stage.IsSatisfied ? "[ok]" : "[нужно]")} {stage.Title} ({stage.Stage}): {stage.ExistingCount}/{stage.TargetMinimum}");
            builder.AppendLine("  " + stage.Reason);
        }

        builder.AppendLine();
        builder.AppendLine("Проблемы:");
        if (report.Issues.Count == 0)
        {
            builder.AppendLine("- явных проблем нет");
        }
        else
        {
            foreach (var issue in report.Issues)
            {
                builder.AppendLine($"- [{issue.Severity}] {issue.Code}: {issue.Message}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Рекомендации:");
        if (report.Recommendations.Count == 0)
        {
            builder.AppendLine("- MVP выглядит играбельным; дальше можно проверять баланс и вручную ревьюить draft.");
        }
        else
        {
            foreach (var recommendation in report.Recommendations.OrderBy(x => x.Priority))
            {
                builder.AppendLine($"- {recommendation.Priority:000} {recommendation.Title} ({recommendation.Stage}, count={recommendation.SuggestedCount}, category={recommendation.SuggestedCategory})");
                builder.AppendLine("  " + recommendation.Description);
            }
        }

        builder.AppendLine();
        builder.AppendLine("Следующий шаг:");
        builder.AppendLine(string.IsNullOrWhiteSpace(report.NextRecommendedStage)
            ? "- Генерация не требуется: проверьте playable flow вручную или переходите к ревью."
            : $"- {report.NextRecommendedStage} / {report.NextRecommendedCategory}, count={report.NextRecommendedCount}");

        return builder.ToString();
    }

    public string BuildCompactMvpSummary(GameProjectData project, GameMvpReadinessReport report)
    {
        var model = new
        {
            report.Summary,
            report.OverallStatus,
            report.CompletionPercent,
            report.HasBlockingProblems,
            Next = new
            {
                report.NextRecommendedStage,
                report.NextRecommendedCategory,
                report.NextRecommendedCount
            },
            Design = new
            {
                InitialIdea = Preview(project.DesignProfile.InitialIdea, 350),
                FilledSlots = project.DesignProfile.Slots
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .OrderBy(x => x.Priority)
                    .Take(12)
                    .Select(x => new { x.Id, Value = Preview(x.Value, 180), Source = x.Source.ToString() }),
                Brief = Preview(project.Brief.Text, 350),
                Concept = Preview(project.Concept.Text, 350),
                Mvp = Preview(project.MvpPlan.Text, 350),
                Meta = new
                {
                    project.Meta.Title,
                    project.Meta.Genre,
                    project.Meta.Tone,
                    Description = Preview(project.Meta.Description, 350),
                    project.Meta.StartSceneId,
                    project.Meta.Language
                }
            },
            Counts = BuildCounts(project),
            MissingStages = report.Stages
                .Where(x => !x.IsSatisfied)
                .OrderBy(x => x.Priority)
                .Take(12)
                .Select(x => new { x.Stage, x.Title, x.ExistingCount, x.TargetMinimum, x.Priority }),
            Recommendations = report.Recommendations
                .OrderBy(x => x.Priority)
                .Take(8)
                .Select(x => new { x.Id, x.Stage, x.Priority, x.SuggestedCount, x.SuggestedCategory, x.TargetSystems }),
            ExistingIds = new
            {
                Stats = project.Stats.Take(20).Select(x => x.Id),
                Currencies = project.Currencies.Take(12).Select(x => x.Id),
                Variables = project.Variables.Take(20).Select(x => x.Id),
                Formulas = project.Formulas.Take(20).Select(x => x.Id),
                Actions = project.Actions.Take(20).Select(x => x.Id),
                Locations = project.Locations.Take(20).Select(x => x.Id),
                Scenes = project.Scenes.Take(20).Select(x => x.Id),
                Items = project.Items.Take(20).Select(x => x.Id),
                EquipmentSlots = project.EquipmentSlots.Take(12).Select(x => x.Id),
                Skills = project.Skills.Take(20).Select(x => x.Id),
                Encounters = project.Encounters.Take(20).Select(x => x.Id),
                AmbientEvents = project.WorldState.AmbientEvents.Take(20).Select(x => x.Id),
                ProgressionNodes = project.ProgressionNodes.Take(20).Select(x => x.Id)
            }
        };

        return JsonSerializer.Serialize(model, _jsonOptions);
    }

    public GameMvpRecommendation? DetermineNextStage(GameProjectData project, GameMvpReadinessReport report)
    {
        return report.Recommendations
            .OrderBy(x => x.Priority)
            .FirstOrDefault(x => IsAutomatedStage(x.Stage) || x.Stage == "design_profile");
    }

    public string BuildNextStageUserRules(GameProjectData project, GameMvpReadinessReport report, GameMvpRecommendation recommendation)
    {
        var summary = BuildCompactMvpSummary(project, report);
        var builder = new StringBuilder();
        builder.AppendLine("MVP readiness summary:");
        builder.AppendLine(summary);
        builder.AppendLine();
        builder.AppendLine("Generate only the chosen MVP stage: " + recommendation.Stage + ".");
        builder.AppendLine("Reason: " + recommendation.Description);
        builder.AppendLine("Use Russian player-facing text.");
        builder.AppendLine("Use English snake_case IDs.");
        builder.AppendLine("Return only a small partial GameProjectData JSON batch for this stage.");
        builder.AppendLine("Draft-only: do not ask to apply JSON and do not rewrite unrelated systems.");
        builder.AppendLine("Do not generate C# code, SQLite, Dialogue Graph, standalone export, visual map editor, or runtime LLM hooks.");
        return builder.ToString().Trim();
    }

    private static void AddStage(GameMvpReadinessReport report, string stage, string title, int existingCount, int targetMinimum, int priority, string reason)
    {
        report.Stages.Add(new GameMvpStageStatus
        {
            Stage = stage,
            Title = title,
            ExistingCount = existingCount,
            TargetMinimum = targetMinimum,
            IsSatisfied = existingCount >= targetMinimum,
            Priority = priority,
            Reason = reason
        });
    }

    private static void AddIssues(GameMvpReadinessReport report, GameProjectData project, MvpSignals signals)
    {
        if (string.IsNullOrWhiteSpace(project.Meta.Id))
        {
            AddIssue(report, "meta_id_empty", "Meta.Id пустой: проект сложно безопасно сохранять и мержить.", GameMvpReadinessSeverity.Error, "meta");
        }
        if (string.IsNullOrWhiteSpace(project.Meta.Title))
        {
            AddIssue(report, "meta_title_empty", "Meta.Title пустой.", GameMvpReadinessSeverity.Warning, "meta");
        }
        if (project.Scenes.Count > 0 && string.IsNullOrWhiteSpace(project.Meta.StartSceneId))
        {
            AddIssue(report, "start_scene_empty", "Есть сцены, но Meta.StartSceneId пустой.", GameMvpReadinessSeverity.Error, "scenes");
        }
        if (!string.IsNullOrWhiteSpace(project.Meta.StartSceneId)
            && project.Scenes.Count > 0
            && project.Scenes.All(x => !string.Equals(x.Id, project.Meta.StartSceneId, StringComparison.OrdinalIgnoreCase)))
        {
            AddIssue(report, "start_scene_missing", "Meta.StartSceneId не найден среди сцен.", GameMvpReadinessSeverity.Error, "scenes", project.Meta.StartSceneId);
        }
        if (project.Scenes.Count > 0 && project.Scenes.All(x => x.Choices.Count == 0))
        {
            AddIssue(report, "scenes_without_choices", "Сцены есть, но развилки не найдены.", GameMvpReadinessSeverity.Warning, "scenes");
        }
        if (signals.CombatRelevant && project.Actions.Count(x => x.AvailableInCombat) == 0)
        {
            AddIssue(report, "combat_without_actions", "Боёвка релевантна, но combat actions отсутствуют.", GameMvpReadinessSeverity.Warning, "combat");
        }
        if (signals.RandomnessRelevant)
        {
            var randomReport = new GameRandomDirectorService().BuildReport(project);
            foreach (var warning in randomReport.Warnings.Where(x => x.Severity == GameMvpReadinessSeverity.Error).Take(3))
            {
                AddIssue(report, "random_director_" + warning.Code, warning.Message, GameMvpReadinessSeverity.Warning, "random_events", warning.EntityIds.ToArray());
            }
        }
    }

    private static void AddRecommendations(GameMvpReadinessReport report)
    {
        foreach (var stage in report.Stages.Where(x => !x.IsSatisfied).OrderBy(x => x.Priority))
        {
            report.Recommendations.Add(new GameMvpRecommendation
            {
                Id = "generate_" + stage.Stage,
                Title = "Сгенерировать недостающий MVP слой: " + stage.Title,
                Description = stage.Reason,
                Stage = stage.Stage,
                Priority = stage.Priority,
                SuggestedCount = SuggestedCount(stage),
                SuggestedCategory = SuggestedCategory(stage.Stage),
                TargetSystems = TargetSystems(stage.Stage)
            });
        }
    }

    private static void ApplyNextStage(GameMvpReadinessReport report)
    {
        var next = report.Recommendations.OrderBy(x => x.Priority).FirstOrDefault();
        report.NextRecommendedStage = next?.Stage;
        report.NextRecommendedCategory = next?.SuggestedCategory;
        report.NextRecommendedCount = next?.SuggestedCount ?? 0;
        report.HasBlockingProblems = report.Issues.Any(x => string.Equals(x.Severity, GameMvpReadinessSeverity.Error, StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplySummary(GameMvpReadinessReport report, GameProjectData project)
    {
        var requiredStages = report.Stages.Where(x => x.Stage != "balance").ToList();
        var satisfied = requiredStages.Count(x => x.IsSatisfied);
        report.CompletionPercent = requiredStages.Count == 0 ? 0 : Math.Clamp((int)Math.Round(satisfied * 100.0 / requiredStages.Count), 0, 100);

        report.OverallStatus = report.CompletionPercent switch
        {
            0 => GameMvpReadinessStatus.Empty,
            < 35 => GameMvpReadinessStatus.Skeleton,
            < 85 => GameMvpReadinessStatus.Draftable,
            _ => report.HasBlockingProblems ? GameMvpReadinessStatus.NeedsReview : GameMvpReadinessStatus.Playable
        };

        if (project.Scenes.Count > 0 && report.HasBlockingProblems)
        {
            report.OverallStatus = GameMvpReadinessStatus.NeedsReview;
        }

        var missing = report.Stages.Count(x => !x.IsSatisfied);
        report.Summary = $"MVP готовность: {report.CompletionPercent}%. Закрыто стадий: {satisfied}/{requiredStages.Count}. Недостающих стадий: {missing}. Следующий draft: {(report.NextRecommendedStage ?? "не требуется")}.";
    }

    private static int CountStatsResources(GameProjectData project)
    {
        return project.Stats.Count + project.Currencies.Count + project.Variables.Count;
    }

    private static int CountWorldStateFoundation(GameProjectData project)
    {
        return project.WorldState.Enabled
            || project.WorldState.Time.Enabled
            || project.WorldState.Time.Segments.Count > 0
            || project.WorldState.Aspects.Count > 0
            || project.WorldState.Rules.Count > 0
            || project.WorldState.AmbientEvents.Count > 0
            ? 1
            : 0;
    }

    private static int CountEquipment(GameProjectData project)
    {
        return project.EquipmentSlots.Count + project.Items.Count(x => x.IsEquippable);
    }

    private static int CountCombatFoundation(GameProjectData project)
    {
        return project.Combat?.Enabled == true
            || project.Actions.Any(x => x.AvailableInCombat)
            || project.Encounters.Any(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) || x.Combatants.Count > 0)
            ? 1
            : 0;
    }

    private static int CountBalanceReadiness(GameProjectData project)
    {
        return project.Encounters.Any(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) || x.Combatants.Count > 0)
            || project.ProgressionNodes.Count >= TargetProgression
            || project.Currencies.Count > 0 && project.Items.Count >= TargetItems
            ? 1
            : 0;
    }

    private static bool ShouldRecommendBalance(GameProjectData project, MvpSignals signals)
    {
        var contentRich = project.Stats.Count >= TargetStatsResources
            && project.Actions.Count >= TargetActions
            && project.Locations.Count >= TargetLocations
            && project.Scenes.Count >= TargetScenes
            && project.Items.Count >= TargetItems;

        return contentRich && (signals.CombatRelevant || signals.ProgressionRelevant || project.Currencies.Count > 0);
    }

    private static MvpSignals BuildSignals(GameProjectData project)
    {
        var text = BuildSignalText(project);
        var explicitNoCombat = ContainsAny(text, "нет бо", "без бо", "no combat", "non-combat", "without combat");
        var combatRelevant = project.Combat?.Enabled == true
            || project.Encounters.Any(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) || x.Combatants.Count > 0)
            || project.Actions.Any(x => x.AvailableInCombat)
            || !explicitNoCombat && ContainsAny(text, "бой", "боёв", "боев", "сраж", "combat", "battle", "encounter", "враг", "урон");
        var randomnessRelevant = project.Mechanics.EnableDiceRandomness
            || project.WorldState.AmbientEvents.Count > 0
            || ContainsAny(text, "рандом", "случайн", "случайные события", "random", "procedural", "travel event", "путешеств");
        var inventoryRelevant = project.EquipmentSlots.Count > 0
            || project.Items.Any(x => x.IsEquippable)
            || ContainsAny(text, "инвент", "экип", "одеж", "брон", "оруж", "лут", "inventory", "equipment", "item");
        var skillsRelevant = project.Skills.Count > 0
            || ContainsAny(text, "навык", "skill", "умени", "ability");
        var magicRelevant = project.Skills.Any(IsSpell)
            || project.Elements.Count > 0
            || ContainsAny(text, "маг", "заклин", "spell", "magic", "wizard");
        var progressionRelevant = project.Mechanics.EnableProgression
            || project.Mechanics.Experience.EnablePlayerExperience
            || project.Mechanics.Experience.EnableSkillExperience
            || project.ProgressionNodes.Count > 0
            || ContainsAny(text, "прогресс", "прокач", "опыт", "уров", "xp", "experience", "progression", "level");
        var hasDesignBasis = !string.IsNullOrWhiteSpace(project.DesignProfile.InitialIdea)
            || project.DesignProfile.Slots.Any(x => !string.IsNullOrWhiteSpace(x.Value))
            || !string.IsNullOrWhiteSpace(project.Brief.Text)
            || !string.IsNullOrWhiteSpace(project.Concept.Text)
            || !string.IsNullOrWhiteSpace(project.MvpPlan.Text)
            || !string.IsNullOrWhiteSpace(project.Meta.Description)
            || !string.IsNullOrWhiteSpace(project.Meta.Genre)
            || !string.IsNullOrWhiteSpace(project.Meta.Tone);

        return new MvpSignals(combatRelevant, randomnessRelevant, inventoryRelevant, skillsRelevant, magicRelevant, progressionRelevant, hasDesignBasis);
    }

    private static string BuildSignalText(GameProjectData project)
    {
        var builder = new StringBuilder();
        builder.Append(' ').Append(project.Meta.Genre);
        builder.Append(' ').Append(project.Meta.Tone);
        builder.Append(' ').Append(project.Meta.Description);
        builder.Append(' ').Append(project.Brief.Text);
        builder.Append(' ').Append(project.Concept.Text);
        builder.Append(' ').Append(project.MvpPlan.Text);
        builder.Append(' ').Append(project.ArchitecturePlan.Text);
        builder.Append(' ').Append(project.DesignProfile.InitialIdea);
        foreach (var slot in project.DesignProfile.Slots)
        {
            builder.Append(' ').Append(slot.Id).Append(' ').Append(slot.Value);
        }
        foreach (var step in project.CreationPlan.Steps)
        {
            builder.Append(' ').Append(step.Stage).Append(' ').Append(step.Title).Append(' ').Append(step.Description);
        }
        builder.Append(' ').Append(project.Mechanics.Notes);
        builder.Append(' ').Append(project.GenerationPreferences.GeneralGameplayText);
        builder.Append(' ').Append(project.GenerationPreferences.SkillDesignText);
        builder.Append(' ').Append(project.GenerationPreferences.ProgressionDesignText);
        builder.Append(' ').Append(project.GenerationPreferences.CombatDesignText);
        builder.Append(' ').Append(project.GenerationPreferences.AtmosphereDesignText);
        return builder.ToString();
    }

    private static bool IsSpell(GameSkillDefinition skill)
    {
        return string.Equals(skill.Kind, "spell", StringComparison.OrdinalIgnoreCase)
            || skill.Tags.Contains("spell", StringComparer.OrdinalIgnoreCase)
            || skill.Tags.Contains("magic", StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddIssue(GameMvpReadinessReport report, string code, string message, string severity, string scope, params string[] entityIds)
    {
        report.Issues.Add(new GameMvpReadinessIssue
        {
            Code = code,
            Message = message,
            Severity = severity,
            Scope = scope,
            EntityIds = entityIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        });
    }

    private static int SuggestedCount(GameMvpStageStatus stage)
    {
        var missing = Math.Max(1, stage.TargetMinimum - stage.ExistingCount);
        return stage.Stage switch
        {
            "stats_resources" => Math.Clamp(missing + 2, 4, 8),
            "formulas" => Math.Clamp(missing + 1, 2, 4),
            "actions" => Math.Clamp(missing + 1, 3, 6),
            "locations" => Math.Clamp(missing + 1, 3, 5),
            "scenes" => Math.Clamp(missing + 2, 6, 10),
            "items" => Math.Clamp(missing + 2, 5, 8),
            "random_events" => Math.Clamp(missing + 2, 4, 8),
            "progression" => Math.Clamp(missing + 1, 3, 6),
            "balance" => 1,
            _ => Math.Clamp(missing, 1, 6)
        };
    }

    private static string SuggestedCategory(string stage)
    {
        return stage switch
        {
            "stats_resources" => "missing_mvp_stats",
            "formulas" => "missing_mvp_formulas",
            "actions" => "missing_mvp_actions",
            "world_state" => "missing_mvp_world_state",
            "locations" => "missing_mvp_locations",
            "scenes" => "missing_mvp_scenes",
            "items" => "missing_mvp_items",
            "equipment" => "missing_mvp_equipment",
            "skills" => "missing_mvp_skills",
            "spells" => "missing_mvp_spells",
            "encounters" => "missing_mvp_encounters",
            "combat" => "missing_mvp_combat",
            "progression" => "missing_mvp_progression",
            "random_events" => "controlled_random_events",
            "balance" => "core_mvp_balance",
            _ => "core_mvp"
        };
    }

    private static List<string> TargetSystems(string stage)
    {
        return stage switch
        {
            "stats_resources" => ["stats", "currencies", "variables"],
            "formulas" => ["formulas"],
            "actions" => ["actions"],
            "world_state" => ["world-state", "time", "world-rules"],
            "locations" => ["locations", "location-connections"],
            "scenes" => ["scenes", "quests", "characters"],
            "items" => ["items", "currencies"],
            "equipment" => ["equipmentSlots", "items"],
            "skills" => ["skills", "actions"],
            "spells" => ["skills", "elements", "actions"],
            "encounters" => ["encounters", "scenes"],
            "combat" => ["combat", "actions", "formulas"],
            "progression" => ["progression", "skills", "experience"],
            "random_events" => ["worldState", "ambientEvents", "worldRules"],
            "balance" => ["combat", "progression", "items", "stats"],
            _ => [stage]
        };
    }

    private static bool IsAutomatedStage(string stage)
    {
        return stage is "stats_resources" or "formulas" or "actions" or "world_state" or "locations" or "scenes"
            or "items" or "equipment" or "skills" or "spells" or "encounters" or "combat" or "progression"
            or "random_events" or "balance";
    }

    private static object BuildCounts(GameProjectData project)
    {
        return new
        {
            Stats = project.Stats.Count,
            Currencies = project.Currencies.Count,
            Variables = project.Variables.Count,
            Formulas = project.Formulas.Count,
            Actions = project.Actions.Count,
            CombatActions = project.Actions.Count(x => x.AvailableInCombat),
            WorldStateEnabled = project.WorldState.Enabled,
            TimeSegments = project.WorldState.Time.Segments.Count,
            WorldAspects = project.WorldState.Aspects.Count,
            AmbientEvents = project.WorldState.AmbientEvents.Count,
            WorldRules = project.WorldState.Rules.Count,
            Locations = project.Locations.Count,
            LocationConnections = project.LocationConnections.Count,
            Scenes = project.Scenes.Count,
            Items = project.Items.Count,
            EquipmentSlots = project.EquipmentSlots.Count,
            EquippableItems = project.Items.Count(x => x.IsEquippable),
            Skills = project.Skills.Count,
            Spells = project.Skills.Count(IsSpell),
            Encounters = project.Encounters.Count,
            CombatEncounters = project.Encounters.Count(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) || x.Combatants.Count > 0),
            ProgressionNodes = project.ProgressionNodes.Count,
            ImagePrompts = project.ImagePrompts.Count
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

    private sealed record MvpSignals(
        bool CombatRelevant,
        bool RandomnessRelevant,
        bool InventoryRelevant,
        bool SkillsRelevant,
        bool MagicRelevant,
        bool ProgressionRelevant,
        bool HasDesignBasis);
}
