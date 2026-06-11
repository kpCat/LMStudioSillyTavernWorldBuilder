using System.Text;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Runtime;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameBalanceSimulatorService
{
    private const int DefaultRuns = 30;
    private const int MaxRuns = 100;
    private const int HardRoundCap = 300;

    private readonly GameRuntimeEngine _runtimeEngine = new();
    private readonly GameStorageService _storageService = new();
    private readonly GameProjectCloneService _cloneService = new();
    private readonly JsonSerializerOptions _jsonOptions = GenerationJsonOptions.PromptJson;

    public GameBalanceReport BuildReport(GameProjectData project, int simulationRunsPerEncounter)
    {
        var runs = ClampRuns(simulationRunsPerEncounter);
        var projectSnapshot = _cloneService.Clone(project);
        var report = new GameBalanceReport
        {
            RequestedSimulationCount = runs,
            Combat = BuildCombatReport(projectSnapshot, runs),
            Economy = BuildEconomyReport(projectSnapshot),
            Progression = BuildProgressionReport(projectSnapshot),
            Resources = BuildResourceReport(projectSnapshot)
        };

        AddStaticIssues(report);
        AddCombatIssues(report);
        AddRecommendations(report);
        report.OverallSeverity = CalculateOverallSeverity(report.Issues);
        report.Summary = BuildSummary(report);
        return report;
    }

    public string FormatReportForUi(GameBalanceReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== Balance Simulator v1 ===");
        builder.AppendLine(report.Summary);
        builder.AppendLine();
        builder.AppendLine($"Создан: {report.CreatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine($"Симуляций на encounter: {report.RequestedSimulationCount}");
        builder.AppendLine($"Общая серьёзность: {report.OverallSeverity}");

        builder.AppendLine();
        builder.AppendLine("Бои:");
        if (report.Combat.Encounters.Count == 0)
        {
            builder.AppendLine("- Боевые encounter не найдены.");
        }
        else
        {
            foreach (var encounter in report.Combat.Encounters)
            {
                builder.AppendLine($"- {Display(encounter.Name, encounter.EncounterId)}: runs={encounter.Runs}, winRate={encounter.WinRatePercent:0.#}%, win/loss/stall/error={encounter.Wins}/{encounter.Losses}/{encounter.Stalls}/{encounter.Errors}, avgRounds={encounter.AverageRounds:0.#}");
                if (encounter.AveragePlayerHealthEnd.HasValue)
                {
                    builder.AppendLine($"  Среднее здоровье игрока в конце: {encounter.AveragePlayerHealthEnd.Value:0.#}");
                }
                foreach (var warning in encounter.Warnings)
                {
                    builder.AppendLine("  ! " + warning);
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("Экономика:");
        builder.AppendLine($"- валют={report.Economy.CurrencyCount}, предметов с ценой={report.Economy.PricedItemCount}, источников={report.Economy.CurrencySourceCount}, трат={report.Economy.CurrencySinkCount}");
        AppendWarnings(builder, report.Economy.Warnings);

        builder.AppendLine();
        builder.AppendLine("Прогрессия:");
        builder.AppendLine($"- включена={report.Progression.ProgressionEnabled}, узлов={report.Progression.NodeCount}, XP-источников={report.Progression.ExperienceSourceCount}, unlock-источников={report.Progression.UnlockSourceCount}, disconnected={report.Progression.DisconnectedNodeCount}");
        AppendWarnings(builder, report.Progression.Warnings);

        builder.AppendLine();
        builder.AppendLine("Ресурсы:");
        builder.AppendLine($"- ресурсных статов={report.Resources.ResourceStatCount}, затрат={report.Resources.ResourceCostCount}, восстановления={report.Resources.ResourceRecoveryCount}, healthStat={report.Resources.HealthStatId}");
        AppendWarnings(builder, report.Resources.Warnings);

        builder.AppendLine();
        builder.AppendLine("Риски:");
        if (report.Issues.Count == 0)
        {
            builder.AppendLine("- нет явных рисков v1");
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
            builder.AppendLine("- нет");
        }
        else
        {
            foreach (var recommendation in report.Recommendations.OrderBy(x => x.Priority))
            {
                builder.AppendLine($"- {recommendation.Priority:00} {recommendation.Title} ({recommendation.TargetStage})");
                builder.AppendLine("  " + recommendation.Description);
            }
        }

        return builder.ToString();
    }

    public string BuildCompactBalanceSummary(GameProjectData project, GameBalanceReport report)
    {
        var model = new
        {
            report.Summary,
            report.OverallSeverity,
            report.RequestedSimulationCount,
            Counts = new
            {
                CombatEncounters = report.Combat.EncounterCount,
                SimulatedCombatEncounters = report.Combat.SimulatedEncounterCount,
                Issues = report.Issues.Count,
                Recommendations = report.Recommendations.Count,
                Stats = project.Stats.Count,
                Currencies = project.Currencies.Count,
                Items = project.Items.Count,
                ProgressionNodes = project.ProgressionNodes.Count
            },
            Issues = report.Issues.Take(12).Select(x => new { x.Code, x.Severity, x.Message, EntityIds = x.EntityIds.Take(8) }),
            Recommendations = report.Recommendations.OrderBy(x => x.Priority).Take(8).Select(x => new { x.Id, x.Title, x.TargetStage, x.Priority, x.TargetSystems, EntityIds = x.EntityIds.Take(8) }),
            Combat = report.Combat.Encounters.Take(8).Select(x => new
            {
                x.EncounterId,
                x.Name,
                x.Runs,
                x.Wins,
                x.Losses,
                x.Stalls,
                x.Errors,
                x.WinRatePercent,
                x.AverageRounds,
                x.AveragePlayerHealthEnd,
                Warnings = x.Warnings.Take(6)
            }),
            Economy = report.Economy,
            Progression = report.Progression,
            Resources = report.Resources
        };

        return JsonSerializer.Serialize(model, _jsonOptions);
    }

    public string BuildGenerationUserPrompt(GameProjectData project, GameBalanceReport report)
    {
        var model = new
        {
            Instruction = "Сгенерируй маленький partial GameProjectData JSON patch для правки баланса. Не применяй изменения напрямую.",
            BalanceReport = JsonSerializer.Deserialize<object>(BuildCompactBalanceSummary(project, report)),
            Existing = new
            {
                Meta = new { project.Meta.Id, project.Meta.Title, project.Meta.Genre, project.Meta.Tone, project.Meta.Language },
                Combat = project.Combat,
                Stats = project.Stats.Select(x => new { x.Id, x.Name, x.Kind, x.IsResource, x.MinValue, x.MaxValue, x.InitialValue, x.RegenPerTurn }).Take(60),
                Actions = project.Actions.Select(x => new { x.Id, x.Name, x.AvailableInCombat, x.ActorTeam, x.TargetScope, x.CooldownTurns, x.Costs, Effects = x.Effects.Select(e => new { e.Type, e.TargetId, e.Amount, e.FormulaId, e.FormulaExpression }) }).Take(60),
                Encounters = project.Encounters.Select(x => new { x.Id, x.Name, x.Kind, x.SceneId, x.VictorySceneId, x.DefeatSceneId, Combatants = x.Combatants.Select(c => new { c.Id, c.Name, c.Team, c.IsPlayer, c.Stats, c.ActionIds }), OnWinEffects = x.OnWinEffects }).Take(40),
                Items = project.Items.Select(x => new { x.Id, x.Name, x.Type, x.Value, x.CurrencyId, x.UseEffects, x.Modifiers }).Take(60),
                Currencies = project.Currencies.Select(x => new { x.Id, x.Name, x.InitialAmount }).Take(30),
                Skills = project.Skills.Select(x => new { x.Id, x.Name, x.Kind, x.Costs, x.Effects, x.CooldownTurns, x.ExperienceToNextLevel }).Take(50),
                ProgressionNodes = project.ProgressionNodes.Select(x => new { x.Id, x.Name, x.Kind, x.SkillId, x.ParentNodeIds, x.UnlockRequirements, x.UnlockCosts, x.UnlockEffects }).Take(50),
                Formulas = project.Formulas.Select(x => new { x.Id, x.Name, x.Expression, x.MinResult, x.MaxResult }).Take(50)
            },
            Rules = new[]
            {
                "Весь пользовательский игровой текст пиши на русском.",
                "ID пиши snake_case латиницей.",
                "Если правка касается существующего контента, используй существующие ID.",
                "Не переписывай несвязанные системы.",
                "Не выдумывай неподдержанную схему.",
                "Не добавляй SQLite, Dialogue Graph, C#, standalone export, runtime LLM или новый combat engine.",
                "Предпочитай маленький numeric/content retune: формулы, costs, effects, item values, cooldowns, combatant stats, rewards, resources, progression costs.",
                "Не удаляй контент. Допустимы только безопасная перенастройка и additive correction."
            }
        };

        return JsonSerializer.Serialize(model, _jsonOptions);
    }

    private GameCombatBalanceReport BuildCombatReport(GameProjectData project, int requestedRuns)
    {
        var encounters = project.Encounters
            .Where(x => x.Combatants.Count > 0 || string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var report = new GameCombatBalanceReport { EncounterCount = encounters.Count };
        foreach (var encounter in encounters)
        {
            var encounterReport = SimulateEncounter(project, encounter, requestedRuns);
            report.Encounters.Add(encounterReport);
            if (encounterReport.Runs > 0)
            {
                report.SimulatedEncounterCount++;
            }
        }

        return report;
    }

    private GameCombatEncounterSimulationReport SimulateEncounter(GameProjectData project, GameEncounterDefinition encounter, int requestedRuns)
    {
        var healthStatId = GetHealthStatId(project);
        var report = new GameCombatEncounterSimulationReport
        {
            EncounterId = encounter.Id,
            Name = encounter.Name,
            RequestedRuns = requestedRuns
        };

        if (encounter.Combatants.Count == 0)
        {
            report.Warnings.Add("В encounter нет участников боя.");
            return report;
        }
        if (!encounter.Combatants.Any(IsPlayerSide))
        {
            report.Warnings.Add("Нет player/ally combatant.");
        }
        if (!encounter.Combatants.Any(IsEnemySide))
        {
            report.Warnings.Add("Нет enemy combatant.");
        }

        for (var i = 1; i <= requestedRuns; i++)
        {
            report.RunResults.Add(SimulateRun(project, encounter, healthStatId, i));
        }

        report.Runs = report.RunResults.Count;
        report.Wins = report.RunResults.Count(x => x.Outcome == "win");
        report.Losses = report.RunResults.Count(x => x.Outcome == "loss");
        report.Stalls = report.RunResults.Count(x => x.Outcome == "stall");
        report.Errors = report.RunResults.Count(x => x.Outcome == "error");
        report.WinRatePercent = report.Runs == 0 ? 0 : report.Wins * 100.0 / report.Runs;
        report.AverageRounds = report.Runs == 0 ? 0 : report.RunResults.Average(x => x.Rounds);
        report.MinRounds = report.Runs == 0 ? 0 : report.RunResults.Min(x => x.Rounds);
        report.MaxRounds = report.Runs == 0 ? 0 : report.RunResults.Max(x => x.Rounds);
        var healthResults = report.RunResults.Where(x => x.PlayerHealthEnd >= 0).ToList();
        report.AveragePlayerHealthEnd = healthResults.Count == 0 ? null : healthResults.Average(x => x.PlayerHealthEnd);

        if (report.RunResults.Any(x => x.Message.Contains("no_available_player_action", StringComparison.OrdinalIgnoreCase)))
        {
            report.Warnings.Add("У игрока/союзника не найдено доступных combat action с живой enemy-целью.");
        }
        if (report.Errors > 0)
        {
            report.Warnings.Add("Есть runtime errors при симуляции боя.");
        }
        if (report.Stalls > 0)
        {
            report.Warnings.Add("Есть симуляции, остановленные guard-лимитом.");
        }
        if (report.Runs > 0 && report.WinRatePercent < 35)
        {
            report.Warnings.Add("Win rate ниже 35%: бой может быть слишком сложным.");
        }
        if (report.Runs > 0 && report.WinRatePercent > 90)
        {
            report.Warnings.Add("Win rate выше 90%: бой может быть слишком лёгким.");
        }
        if (report.AverageRounds > 20)
        {
            report.Warnings.Add("Средняя длина боя выше 20 раундов: возможно, бой затянут.");
        }
        if (report.Runs > 0 && report.AverageRounds < 1.5 && report.WinRatePercent > 80)
        {
            report.Warnings.Add("Бой заканчивается почти сразу: возможно, он слишком короткий.");
        }

        return report;
    }

    private GameCombatSimulationRunResult SimulateRun(GameProjectData project, GameEncounterDefinition encounter, string healthStatId, int runIndex)
    {
        var save = _storageService.CreateInitialSave(project, "balance-sim");
        var start = _runtimeEngine.StartEncounterCombatWithResult(project, save, encounter.Id);
        if (!start.Success)
        {
            return new GameCombatSimulationRunResult
            {
                RunIndex = runIndex,
                Outcome = "error",
                Rounds = Math.Max(0, save.Combat.RoundNumber),
                PlayerHealthEnd = GetPlayerHealth(save, healthStatId),
                Message = "start_failed: " + start.Message
            };
        }

        var guard = Math.Min(HardRoundCap, Math.Max(1, project.Combat?.MaxRounds ?? 200));
        var noActionSeen = false;
        while (save.Combat.IsActive && save.Combat.RoundNumber <= guard)
        {
            var actor = _runtimeEngine.GetCurrentCombatant(project, save);
            if (actor == null)
            {
                return BuildRunResult(runIndex, "error", save, healthStatId, "current_combatant_missing");
            }

            if (IsEnemySide(actor))
            {
                var enemyAdvance = _runtimeEngine.EndCombatTurnWithResult(project, save);
                if (!enemyAdvance.Success)
                {
                    return BuildRunResult(runIndex, "error", save, healthStatId, enemyAdvance.Message);
                }
                continue;
            }

            var actionAndTarget = FindPlayerActionAndTarget(project, save, actor);
            if (actionAndTarget == null)
            {
                noActionSeen = true;
                var endTurn = _runtimeEngine.EndCombatTurnWithResult(project, save);
                if (!endTurn.Success)
                {
                    return BuildRunResult(runIndex, "error", save, healthStatId, endTurn.Message);
                }
                continue;
            }

            var result = _runtimeEngine.ExecuteCombatActionWithResult(project, save, actionAndTarget.Value.Action.Id, actionAndTarget.Value.Target.RuntimeId);
            if (!result.Success)
            {
                return BuildRunResult(runIndex, "error", save, healthStatId, result.Message);
            }
            if (result.CombatEnded)
            {
                return BuildRunResult(runIndex, result.PlayerWon ? "win" : result.PlayerLost ? "loss" : "ended", save, healthStatId, result.Message);
            }
        }

        if (!save.Combat.IsActive)
        {
            return BuildRunResult(runIndex, "ended", save, healthStatId, "combat ended");
        }

        return BuildRunResult(runIndex, "stall", save, healthStatId, noActionSeen ? "no_available_player_action; guard_exceeded" : "guard_exceeded");
    }

    private GameCombatSimulationRunResult BuildRunResult(int runIndex, string outcome, SaveGame save, string healthStatId, string message)
    {
        return new GameCombatSimulationRunResult
        {
            RunIndex = runIndex,
            Outcome = outcome,
            Rounds = Math.Max(0, save.Combat.RoundNumber),
            PlayerHealthEnd = GetPlayerHealth(save, healthStatId),
            Message = message
        };
    }

    private (GameActionDefinition Action, GameRuntimeCombatant Target)? FindPlayerActionAndTarget(GameProjectData project, SaveGame save, GameRuntimeCombatant actor)
    {
        var livingEnemies = save.Combat.Combatants.Where(x => IsEnemySide(x) && IsLiving(x, GetHealthStatId(project))).ToList();
        if (livingEnemies.Count == 0)
        {
            return null;
        }

        foreach (var action in _runtimeEngine.GetAvailableCombatActions(project, save, actor))
        {
            if (CanTargetEnemy(action))
            {
                return (action, livingEnemies[0]);
            }
        }

        return null;
    }

    private GameEconomyBalanceReport BuildEconomyReport(GameProjectData project)
    {
        var currencyIds = project.Currencies.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var currencyEffects = AllEffects(project).Where(x => IsType(x.Type, "currency")).ToList();
        var currencyCosts = AllCosts(project).Where(x => IsType(x.Type, "currency")).ToList();
        var report = new GameEconomyBalanceReport
        {
            CurrencyCount = project.Currencies.Count,
            PricedItemCount = project.Items.Count(x => x.Value > 0),
            CurrencySourceCount = currencyEffects.Count(x => x.Amount > 0 || !string.Equals(x.Mode, "remove", StringComparison.OrdinalIgnoreCase)),
            CurrencySinkCount = currencyCosts.Count + project.Items.Count(x => x.Value > 0)
        };

        if (project.Currencies.Count > 0 && report.CurrencySourceCount == 0)
        {
            report.Warnings.Add("Есть валюты, но не найдены явные источники currency effects.");
        }
        if (project.Currencies.Count > 0 && report.CurrencySinkCount == 0)
        {
            report.Warnings.Add("Есть валюты, но не найдены траты: costs или item values.");
        }

        foreach (var item in project.Items.Where(x => x.Value > 0 && string.IsNullOrWhiteSpace(x.CurrencyId)))
        {
            report.Warnings.Add("Предмет с ценой без CurrencyId: " + item.Id);
        }
        foreach (var item in project.Items.Where(x => !string.IsNullOrWhiteSpace(x.CurrencyId) && !currencyIds.Contains(x.CurrencyId)))
        {
            report.Warnings.Add($"Предмет '{item.Id}' ссылается на неизвестную валюту '{item.CurrencyId}'.");
        }
        foreach (var item in project.Items.Where(x => x.Value < 0))
        {
            report.Warnings.Add("Предмет с отрицательной ценой: " + item.Id);
        }
        foreach (var item in project.Items.Where(x => x.Value > 1000000))
        {
            report.Warnings.Add("Предмет с экстремально высокой ценой: " + item.Id);
        }

        return report;
    }

    private GameProgressionBalanceReport BuildProgressionReport(GameProjectData project)
    {
        var effects = AllEffects(project).ToList();
        var report = new GameProgressionBalanceReport
        {
            ProgressionEnabled = project.Mechanics.EnableProgression || project.Mechanics.Experience.EnablePlayerExperience || project.Mechanics.Experience.EnableSkillExperience,
            NodeCount = project.ProgressionNodes.Count,
            ExperienceSourceCount = effects.Count(x => IsType(x.Type, "experience") || IsType(x.Type, "playerExperience") || IsType(x.Type, "skillExperience")),
            UnlockSourceCount = effects.Count(x => IsType(x.Type, "progression") || IsType(x.Type, "unlockProgression")),
            DisconnectedNodeCount = project.ProgressionNodes.Count(x => !x.IsUnlockedByDefault && x.ParentNodeIds.Count == 0 && x.UnlockRequirements.Count == 0 && x.UnlockCosts.Count == 0)
        };

        if ((report.ProgressionEnabled || report.NodeCount > 0) && report.ExperienceSourceCount == 0 && report.UnlockSourceCount == 0)
        {
            report.Warnings.Add("Прогрессия включена или есть узлы, но не найдены XP/unlock источники.");
        }
        if (report.NodeCount > 2 && report.DisconnectedNodeCount * 2 >= report.NodeCount)
        {
            report.Warnings.Add("Много progression nodes без родителей, requirements и costs.");
        }
        if (project.Skills.Count > 0 && project.ProgressionNodes.Count > 0 && project.ProgressionNodes.All(x => string.IsNullOrWhiteSpace(x.SkillId)))
        {
            report.Warnings.Add("Skills и progression nodes выглядят несвязанными: узлы не ссылаются на SkillId.");
        }

        return report;
    }

    private GameResourceBalanceReport BuildResourceReport(GameProjectData project)
    {
        var resourceIds = project.Stats
            .Where(x => x.IsResource || string.Equals(x.Kind, "resource", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var resourceCosts = AllCosts(project).Where(x => IsType(x.Type, "resource") || IsType(x.Type, "stat") && resourceIds.Contains(x.TargetId)).ToList();
        var recoveryEffects = AllEffects(project).Where(x => (IsType(x.Type, "resource") || IsType(x.Type, "stat")) && resourceIds.Contains(x.TargetId) && x.Amount > 0).ToList();
        var report = new GameResourceBalanceReport
        {
            ResourceStatCount = resourceIds.Count,
            ResourceCostCount = resourceCosts.Count,
            ResourceRecoveryCount = recoveryEffects.Count + project.Stats.Count(x => resourceIds.Contains(x.Id) && x.RegenPerTurn.GetValueOrDefault() > 0),
            HealthStatId = GetHealthStatId(project)
        };

        if (resourceCosts.Count > 0 && report.ResourceRecoveryCount == 0)
        {
            report.Warnings.Add("Есть затраты ресурсов, но не найдены recovery sources или regenPerTurn.");
        }
        if (!string.IsNullOrWhiteSpace(report.HealthStatId) && project.Stats.All(x => !string.Equals(x.Id, report.HealthStatId, StringComparison.OrdinalIgnoreCase)))
        {
            report.Warnings.Add("Ключевой health stat из Combat отсутствует в Stats: " + report.HealthStatId);
        }
        foreach (var stat in project.Stats.Where(x => resourceIds.Contains(x.Id)))
        {
            if (stat.MaxValue <= stat.MinValue)
            {
                report.Warnings.Add("Ресурс с MaxValue <= MinValue: " + stat.Id);
            }
            if (stat.InitialValue < stat.MinValue || stat.InitialValue > stat.MaxValue)
            {
                report.Warnings.Add("Ресурс с InitialValue вне диапазона Min/Max: " + stat.Id);
            }
        }

        return report;
    }

    private void AddStaticIssues(GameBalanceReport report)
    {
        foreach (var warning in report.Economy.Warnings)
        {
            AddIssue(report, "economy_warning", warning, GameBalanceSeverity.Warning, "economy");
        }
        foreach (var warning in report.Progression.Warnings)
        {
            AddIssue(report, "progression_warning", warning, GameBalanceSeverity.Warning, "progression");
        }
        foreach (var warning in report.Resources.Warnings)
        {
            AddIssue(report, warning.Contains("health stat", StringComparison.OrdinalIgnoreCase) ? "missing_health_stat" : "resource_warning", warning, GameBalanceSeverity.Warning, "resources");
        }
    }

    private void AddCombatIssues(GameBalanceReport report)
    {
        foreach (var encounter in report.Combat.Encounters)
        {
            foreach (var warning in encounter.Warnings)
            {
                var severity = warning.Contains("runtime errors", StringComparison.OrdinalIgnoreCase) || warning.Contains("Нет", StringComparison.OrdinalIgnoreCase)
                    ? GameBalanceSeverity.Error
                    : GameBalanceSeverity.Warning;
                AddIssue(report, "combat_" + GameProjectManifestService.SafeId(warning, "warning"), warning, severity, "combat", encounter.EncounterId);
            }
        }
    }

    private void AddRecommendations(GameBalanceReport report)
    {
        var priority = 10;
        if (report.Combat.Encounters.Any(x => x.WinRatePercent < 35 && x.Runs > 0))
        {
            AddRecommendation(report, "soften_hard_combats", "Смягчить слишком сложные бои", "Проверить health/damage/cooldowns у врагов и награды за победу. Начать с encounter с низким win rate.", "rebalance", priority, ["combat", "encounters", "actions"]);
            priority += 10;
        }
        if (report.Combat.Encounters.Any(x => x.WinRatePercent > 90 && x.Runs > 0))
        {
            AddRecommendation(report, "raise_easy_combats", "Усилить слишком лёгкие бои", "Плавно поднять выживаемость врагов, снизить burst-урон игрока или добавить cooldown/cost.", "rebalance", priority, ["combat", "encounters", "actions"]);
            priority += 10;
        }
        if (report.Economy.Warnings.Count > 0)
        {
            AddRecommendation(report, "repair_economy_loops", "Закрыть петли экономики", "Добавить явные источники/траты валют и исправить item value/currencyId.", "rebalance", priority, ["currencies", "items"]);
            priority += 10;
        }
        if (report.Progression.Warnings.Count > 0)
        {
            AddRecommendation(report, "connect_progression_sources", "Связать прогрессию с источниками", "Добавить XP/unlock эффекты или requirements/costs для progression nodes.", "rebalance", priority, ["progression", "skills", "effects"]);
            priority += 10;
        }
        if (report.Resources.Warnings.Count > 0)
        {
            AddRecommendation(report, "repair_resource_pressure", "Проверить давление ресурсов", "Сопоставить resource costs с recovery effects, regenPerTurn и диапазонами статов.", "rebalance", priority, ["stats", "resources", "actions"]);
        }
    }

    private static void AddIssue(GameBalanceReport report, string code, string message, string severity, string scope, params string[] entityIds)
    {
        report.Issues.Add(new GameBalanceIssue
        {
            Code = code,
            Message = message,
            Severity = severity,
            Scope = scope,
            EntityIds = entityIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        });
    }

    private static void AddRecommendation(GameBalanceReport report, string id, string title, string description, string stage, int priority, List<string> targetSystems)
    {
        report.Recommendations.Add(new GameBalanceRecommendation
        {
            Id = id,
            Title = title,
            Description = description,
            TargetStage = stage,
            Priority = priority,
            TargetSystems = targetSystems
        });
    }

    private static string BuildSummary(GameBalanceReport report)
    {
        var errors = report.Issues.Count(x => string.Equals(x.Severity, GameBalanceSeverity.Error, StringComparison.OrdinalIgnoreCase));
        var warnings = report.Issues.Count(x => string.Equals(x.Severity, GameBalanceSeverity.Warning, StringComparison.OrdinalIgnoreCase));
        return $"Проверено боёв: {report.Combat.SimulatedEncounterCount}/{report.Combat.EncounterCount}. Риски: errors={errors}, warnings={warnings}. Economy warnings={report.Economy.Warnings.Count}, progression warnings={report.Progression.Warnings.Count}, resource warnings={report.Resources.Warnings.Count}.";
    }

    private static string CalculateOverallSeverity(IEnumerable<GameBalanceIssue> issues)
    {
        if (issues.Any(x => string.Equals(x.Severity, GameBalanceSeverity.Error, StringComparison.OrdinalIgnoreCase)))
        {
            return GameBalanceSeverity.Error;
        }
        return issues.Any(x => string.Equals(x.Severity, GameBalanceSeverity.Warning, StringComparison.OrdinalIgnoreCase))
            ? GameBalanceSeverity.Warning
            : GameBalanceSeverity.Info;
    }

    private static int ClampRuns(int value)
    {
        return Math.Clamp(value <= 0 ? DefaultRuns : value, 1, MaxRuns);
    }

    private static bool CanTargetEnemy(GameActionDefinition action)
    {
        return string.IsNullOrWhiteSpace(action.TargetScope)
            || action.TargetScope.Equals("enemy", StringComparison.OrdinalIgnoreCase)
            || action.TargetScope.Equals("anyEnemy", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlayerSide(GameEncounterCombatantDefinition combatant)
    {
        return combatant.IsPlayer
            || combatant.Team.Equals("player", StringComparison.OrdinalIgnoreCase)
            || combatant.Team.Equals("ally", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEnemySide(GameEncounterCombatantDefinition combatant)
    {
        return combatant.Team.Equals("enemy", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEnemySide(GameRuntimeCombatant combatant)
    {
        return combatant.Team.Equals("enemy", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLiving(GameRuntimeCombatant combatant, string healthStatId)
    {
        return string.IsNullOrWhiteSpace(healthStatId) || combatant.Stats.GetValueOrDefault(healthStatId, 1) > 0;
    }

    private static int GetPlayerHealth(SaveGame save, string healthStatId)
    {
        var player = save.Combat.Combatants.FirstOrDefault(x => !IsEnemySide(x));
        return player == null || string.IsNullOrWhiteSpace(healthStatId) ? -1 : player.Stats.GetValueOrDefault(healthStatId, -1);
    }

    private static string GetHealthStatId(GameProjectData project)
    {
        return string.IsNullOrWhiteSpace(project.Combat?.PlayerHealthStatId) ? "health" : project.Combat.PlayerHealthStatId;
    }

    private static bool IsType(string actual, string expected)
    {
        return actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<GameCost> AllCosts(GameProjectData project)
    {
        foreach (var item in project.Skills.SelectMany(x => x.Costs)) yield return item;
        foreach (var item in project.Actions.SelectMany(x => x.Costs)) yield return item;
        foreach (var item in project.ProgressionNodes.SelectMany(x => x.UnlockCosts)) yield return item;
    }

    private static IEnumerable<GameEffect> AllEffects(GameProjectData project)
    {
        foreach (var item in project.Items.SelectMany(x => x.UseEffects.Concat(x.EquipEffects).Concat(x.UnequipEffects))) yield return item;
        foreach (var item in project.Skills.SelectMany(x => x.Effects)) yield return item;
        foreach (var item in project.Locations.SelectMany(x => x.EnterEffects)) yield return item;
        foreach (var item in project.LocationConnections.SelectMany(x => x.TravelEffects)) yield return item;
        foreach (var item in project.Scenes.SelectMany(x => x.Choices.SelectMany(c => c.Effects))) yield return item;
        foreach (var item in project.Encounters.SelectMany(x => x.OnStartEffects.Concat(x.OnWinEffects).Concat(x.OnLoseEffects).Concat(x.Choices.SelectMany(c => c.Effects)))) yield return item;
        foreach (var item in project.Encounters.SelectMany(x => x.Combatants.SelectMany(c => c.OnDefeatEffects))) yield return item;
        foreach (var item in project.Actions.SelectMany(x => x.Effects)) yield return item;
        foreach (var item in project.StatusEffects.SelectMany(x => x.OnApplyEffects.Concat(x.PeriodicEffects).Concat(x.OnExpireEffects))) yield return item;
        foreach (var item in project.ProgressionNodes.SelectMany(x => x.UnlockEffects)) yield return item;
        foreach (var item in project.WorldState.Time.Segments.SelectMany(x => x.OnEnterEffects)) yield return item;
        foreach (var item in project.WorldState.Aspects.SelectMany(x => x.States.SelectMany(s => s.OnEnterEffects))) yield return item;
        foreach (var item in project.WorldState.AmbientEvents.SelectMany(x => x.Effects)) yield return item;
        foreach (var item in project.WorldState.Rules.SelectMany(x => x.Effects)) yield return item;
    }

    private static void AppendWarnings(StringBuilder builder, IReadOnlyList<string> warnings)
    {
        if (warnings.Count == 0)
        {
            builder.AppendLine("- предупреждений нет");
            return;
        }
        foreach (var warning in warnings)
        {
            builder.AppendLine("- " + warning);
        }
    }

    private static string Display(string name, string id)
    {
        return string.IsNullOrWhiteSpace(name) ? id : name + " (" + id + ")";
    }
}
