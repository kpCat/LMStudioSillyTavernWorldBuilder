using System.Text;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Runtime;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameMechanicsReportService
{
    private readonly GameProjectValidator _validator = new();
    private readonly GameRuntimeEngine _runtimeEngine = new();

    public string BuildReport(GameProjectData project)
    {
        var validation = _validator.Validate(project);
        var save = CreateProbeSave(project);
        var builder = new StringBuilder();
        builder.AppendLine("Проверка механик");
        builder.AppendLine("================");
        builder.AppendLine("formulas: " + project.Formulas.Count);
        builder.AppendLine("statusEffects: " + project.StatusEffects.Count);
        builder.AppendLine("progressionNodes: " + project.ProgressionNodes.Count);
        builder.AppendLine("actions: " + project.Actions.Count);
        builder.AppendLine();
        AppendList(builder, "Ошибки GameProjectValidator", validation.Errors);
        AppendList(builder, "Warnings GameProjectValidator", validation.Warnings);
        AppendFormulas(builder, project, save);
        AppendRandomDiceUsage(builder, project);
        AppendActions(builder, project, save);
        AppendCombat(builder, project);
        AppendExperience(builder, project);
        AppendWorldState(builder, project);
        AppendProgressionSources(builder, project);
        AppendStatuses(builder, project);
        AppendProgression(builder, project);
        AppendPlayability(builder, project, save);
        return builder.ToString();
    }

    private void AppendFormulas(StringBuilder builder, GameProjectData project, SaveGame save)
    {
        builder.AppendLine("Проверка формул");
        foreach (var formula in project.Formulas)
        {
            var result = _runtimeEngine.TryEvaluateFormula(project, save, formula);
            if (!result.Success)
            {
                builder.AppendLine("- " + formula.Id + ": " + result.Message);
            }
        }
        builder.AppendLine();
    }

    private void AppendActions(StringBuilder builder, GameProjectData project, SaveGame save)
    {
        builder.AppendLine("Проверка действий");
        var unavailableActions = new List<string>();
        var availableActions = 0;
        foreach (var action in project.Actions)
        {
            if (string.IsNullOrWhiteSpace(action.Name)) builder.AppendLine("- action без названия: " + action.Id);
            if (action.Effects.Count == 0) builder.AppendLine("- action без effects: " + action.Id);
            if (action.CooldownTurns < 0) builder.AppendLine("- cooldown < 0: " + action.Id);
            var availability = _runtimeEngine.CheckActionAvailability(project, save, action.Id);
            if (availability.IsAvailable)
            {
                availableActions++;
            }
            else
            {
                unavailableActions.Add(action.Id + ": " + availability.Reason);
            }
        }
        if (project.Actions.Count > 0 && availableActions == 0)
        {
            builder.AppendLine("- Нет доступных действий на стартовом save");
        }
        builder.AppendLine();
        AppendList(builder, "Недоступные действия на стартовом save", unavailableActions);
    }

    private static void AppendRandomDiceUsage(StringBuilder builder, GameProjectData project)
    {
        var requirementAndCostLines = new List<string>();
        foreach (var requirement in EnumerateRequirements(project))
        {
            if (GameProjectValidator.UsesRandomOrDice(project, requirement.FormulaId, requirement.FormulaExpression))
            {
                requirementAndCostLines.Add("requirement: " + DescribeFormulaReference(requirement.FormulaId, requirement.FormulaExpression));
            }
        }
        foreach (var cost in EnumerateCosts(project))
        {
            if (GameProjectValidator.UsesRandomOrDice(project, cost.FormulaId, cost.FormulaExpression))
            {
                requirementAndCostLines.Add("cost: " + DescribeFormulaReference(cost.FormulaId, cost.FormulaExpression));
            }
        }

        AppendList(builder, "Формулы с random/dice в требованиях/стоимостях", requirementAndCostLines);

        var effectLines = new List<string>();
        foreach (var effect in EnumerateEffects(project))
        {
            if (GameProjectValidator.UsesRandomOrDice(project, effect.FormulaId, effect.FormulaExpression))
            {
                effectLines.Add("effect: " + DescribeFormulaReference(effect.FormulaId, effect.FormulaExpression));
            }
        }

        AppendList(builder, "Формулы с random/dice в эффектах", effectLines);
    }

    private static void AppendCombat(StringBuilder builder, GameProjectData project)
    {
        builder.AppendLine("Боёвка");
        builder.AppendLine("- enabled: " + (project.Combat?.Enabled == true));
        var combatEncounters = project.Encounters.Where(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) || x.Combatants.Count > 0).ToList();
        var combatActions = project.Actions.Where(x => x.AvailableInCombat).ToList();
        builder.AppendLine("- combat encounters: " + combatEncounters.Count);
        builder.AppendLine("- combat actions: " + combatActions.Count);
        var noEnemyOrPlayer = combatEncounters
            .Where(x => !x.Combatants.Any(c => string.Equals(c.Team, "enemy", StringComparison.OrdinalIgnoreCase))
                || !x.Combatants.Any(c => c.IsPlayer || string.Equals(c.Team, "player", StringComparison.OrdinalIgnoreCase) || string.Equals(c.Team, "ally", StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.Id)
            .ToList();
        AppendList(builder, "encounters без enemy/player", noEnemyOrPlayer);
        var actionsWithoutEffects = combatActions.Where(x => x.Effects.Count == 0).Select(x => x.Id).ToList();
        AppendList(builder, "combat actions без effects", actionsWithoutEffects);
        builder.AppendLine();
    }
    private static void AppendStatuses(StringBuilder builder, GameProjectData project)
    {
        builder.AppendLine("Проверка статусов");
        foreach (var status in project.StatusEffects)
        {
            if (string.IsNullOrWhiteSpace(status.Name)) builder.AppendLine("- status без названия: " + status.Id);
            if (status.MaxStacks <= 0) builder.AppendLine("- MaxStacks <= 0: " + status.Id);
            if (status.DefaultDurationTurns < 0) builder.AppendLine("- duration < 0: " + status.Id);
        }
        builder.AppendLine();
    }

    private static void AppendProgression(StringBuilder builder, GameProjectData project)
    {
        builder.AppendLine("Проверка прокачки");
        var ids = project.ProgressionNodes.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var skillIds = project.Skills.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var node in project.ProgressionNodes)
        {
            if (string.IsNullOrWhiteSpace(node.Name)) builder.AppendLine("- node без названия: " + node.Id);
            foreach (var parentId in node.ParentNodeIds.Where(parentId => !ids.Contains(parentId)))
            {
                builder.AppendLine("- несуществующий parent: " + node.Id + " -> " + parentId);
            }
            if (!string.IsNullOrWhiteSpace(node.SkillId) && !skillIds.Contains(node.SkillId))
            {
                builder.AppendLine("- node.SkillId на несуществующий skill: " + node.Id + " -> " + node.SkillId);
            }
        }
        builder.AppendLine();
    }

    private static void AppendExperience(StringBuilder builder, GameProjectData project)
    {
        builder.AppendLine("Опыт и уровни");
        var exp = project.Mechanics.Experience;
        builder.AppendLine("- опыт игрока: " + (exp.EnablePlayerExperience ? "включён" : "выключен"));
        builder.AppendLine("- опыт навыков: " + (exp.EnableSkillExperience ? "включён" : "выключен"));
        builder.AppendLine("- стартовый уровень: " + exp.InitialPlayerLevel + ", стартовый XP: " + exp.InitialPlayerExperience + ", максимум уровня: " + exp.MaxPlayerLevel);
        builder.AppendLine("- формула уровня игрока: " + DescribeFormulaReference(exp.PlayerExperienceToNextLevelFormulaId, exp.PlayerExperienceToNextLevelFormulaExpression));
        builder.AppendLine("- формула уровня навыка: " + DescribeFormulaReference(exp.SkillExperienceToNextLevelFormulaId, exp.SkillExperienceToNextLevelFormulaExpression));
        builder.AppendLine();
    }

    private static void AppendWorldState(StringBuilder builder, GameProjectData project)
    {
        builder.AppendLine("## World State / Atmosphere");
        var worldState = project.WorldState;
        builder.AppendLine("- enabled: " + worldState.Enabled);
        builder.AppendLine("- genre profile: " + worldState.GenreProfile);
        builder.AppendLine("- time enabled: " + worldState.Time.Enabled + ", segments: " + worldState.Time.Segments.Count + ", start: " + worldState.Time.StartSegmentId);
        builder.AppendLine("- aspects: " + worldState.Aspects.Count);
        foreach (var aspect in worldState.Aspects.Take(12))
        {
            var defaultState = aspect.States.FirstOrDefault(x => string.Equals(x.Id, aspect.DefaultStateId, StringComparison.OrdinalIgnoreCase))
                ?? aspect.States.FirstOrDefault();
            builder.AppendLine("  - " + aspect.Id + " -> " + (defaultState?.Id ?? "<none>"));
        }
        builder.AppendLine("- ambient events: " + worldState.AmbientEvents.Count);
        builder.AppendLine("- world rules: " + worldState.Rules.Count);
        var dslEffects = EnumerateEffects(project).Where(x => IsWorldStateDslType(x.Type)).Select(x => x.Type + ":" + x.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var dslRequirements = EnumerateRequirements(project).Where(x => IsWorldStateDslType(x.Type)).Select(x => x.Type + ":" + x.TargetId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        AppendList(builder, "World-state requirements", dslRequirements);
        AppendList(builder, "World-state effects", dslEffects);
    }

    private static bool IsWorldStateDslType(string type)
    {
        return type.Equals("timeSegment", StringComparison.OrdinalIgnoreCase)
            || type.Equals("dayNumber", StringComparison.OrdinalIgnoreCase)
            || type.Equals("worldState", StringComparison.OrdinalIgnoreCase)
            || type.Equals("worldAspect", StringComparison.OrdinalIgnoreCase)
            || type.Equals("advanceTime", StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendProgressionSources(StringBuilder builder, GameProjectData project)
    {
        var lines = new List<string>();
        foreach (var action in project.Actions)
        {
            AddInterestingEffects(lines, "action " + action.Id, action.Effects);
        }
        foreach (var item in project.Items)
        {
            AddInterestingEffects(lines, "item " + item.Id, item.UseEffects);
        }
        foreach (var scene in project.Scenes)
        {
            foreach (var choice in scene.Choices)
            {
                AddInterestingEffects(lines, "choice " + scene.Id + "/" + choice.Id, choice.Effects);
            }
        }
        foreach (var encounter in project.Encounters)
        {
            AddInterestingEffects(lines, "encounter " + encounter.Id + "/win", encounter.OnWinEffects);
            AddInterestingEffects(lines, "encounter " + encounter.Id + "/choice", encounter.Choices.SelectMany(x => x.Effects));
        }

        AppendList(builder, "Прокачка через действия/предметы/квесты", lines);
    }

    private static void AddInterestingEffects(List<string> lines, string owner, IEnumerable<GameEffect> effects)
    {
        foreach (var effect in effects)
        {
            var type = effect.Type.ToLowerInvariant();
            if (type is "experience" or "playerexperience" or "skillexperience" or "unlockprogression" or "progression" or "learnskill" or "skill")
            {
                lines.Add(owner + " -> " + effect.Type + ":" + effect.TargetId + " " + DescribeFormulaReference(effect.FormulaId, effect.FormulaExpression) + " amount=" + effect.Amount);
            }
        }
    }

    private void AppendPlayability(StringBuilder builder, GameProjectData project, SaveGame save)
    {
        builder.AppendLine("Минимальная играбельность");
        var hasStartId = !string.IsNullOrWhiteSpace(project.Meta.StartSceneId);
        var startScene = project.Scenes.FirstOrDefault(x => string.Equals(x.Id, project.Meta.StartSceneId, StringComparison.OrdinalIgnoreCase));
        builder.AppendLine("- есть стартовая сцена: " + (project.Scenes.Count > 0 ? "да" : "нет"));
        builder.AppendLine("- стартовая сцена найдена по Meta.StartSceneId: " + (hasStartId && startScene != null ? "да" : "нет"));
        builder.AppendLine("- есть доступный выбор или доступное действие: " + (_runtimeEngine.GetAvailableChoices(project, save).Count > 0 || _runtimeEngine.GetAvailableActions(project, save).Count > 0 ? "да" : "нет"));
        if (project.Locations.Count > 0)
        {
            builder.AppendLine("- есть стартовая локация: " + (!string.IsNullOrWhiteSpace(save.CurrentLocationId) ? "да" : "нет"));
        }
        builder.AppendLine("- все actions/progression недоступны: " + (project.Actions.Count > 0 && _runtimeEngine.GetAvailableActions(project, save).Count == 0 && project.ProgressionNodes.Count > 0 && _runtimeEngine.GetAvailableProgressionNodes(project, save).Count == 0 ? "да" : "нет"));
        builder.AppendLine();
    }

    private static void AppendList(StringBuilder builder, string title, IReadOnlyCollection<string> lines)
    {
        builder.AppendLine(title);
        if (lines.Count == 0)
        {
            builder.AppendLine("- нет");
        }
        else
        {
            foreach (var line in lines)
            {
                builder.AppendLine("- " + line);
            }
        }
        builder.AppendLine();
    }

    private static string DescribeFormulaReference(string formulaId, string formulaExpression)
    {
        return !string.IsNullOrWhiteSpace(formulaId)
            ? formulaId
            : string.IsNullOrWhiteSpace(formulaExpression) ? "<empty>" : formulaExpression;
    }

    private static IEnumerable<GameRequirement> EnumerateRequirements(GameProjectData project)
    {
        foreach (var item in project.Items.SelectMany(x => x.Requirements)) yield return item;
        foreach (var item in project.Skills.SelectMany(x => x.LearnRequirements.Concat(x.UseRequirements))) yield return item;
        foreach (var item in project.Locations.SelectMany(x => x.AccessRequirements)) yield return item;
        foreach (var item in project.LocationConnections.SelectMany(x => x.Requirements)) yield return item;
        foreach (var item in project.Encounters.SelectMany(x => x.Requirements)) yield return item;
        foreach (var item in project.Actions.SelectMany(x => x.Requirements)) yield return item;
        foreach (var item in project.StatusEffects.SelectMany(x => x.RemoveRequirements)) yield return item;
        foreach (var item in project.ProgressionNodes.SelectMany(x => x.UnlockRequirements)) yield return item;
        foreach (var item in project.WorldState.AmbientEvents.SelectMany(x => x.Requirements)) yield return item;
        foreach (var item in project.WorldState.Rules.SelectMany(x => x.Requirements)) yield return item;
    }

    private static IEnumerable<GameCost> EnumerateCosts(GameProjectData project)
    {
        foreach (var item in project.Skills.SelectMany(x => x.Costs)) yield return item;
        foreach (var item in project.Actions.SelectMany(x => x.Costs)) yield return item;
        foreach (var item in project.ProgressionNodes.SelectMany(x => x.UnlockCosts)) yield return item;
    }

    private static IEnumerable<GameEffect> EnumerateEffects(GameProjectData project)
    {
        foreach (var item in project.Items.SelectMany(x => x.UseEffects.Concat(x.EquipEffects).Concat(x.UnequipEffects))) yield return item;
        foreach (var item in project.Skills.SelectMany(x => x.Effects)) yield return item;
        foreach (var item in project.Locations.SelectMany(x => x.EnterEffects)) yield return item;
        foreach (var item in project.LocationConnections.SelectMany(x => x.TravelEffects)) yield return item;
        foreach (var item in project.Scenes.SelectMany(x => x.Choices.SelectMany(c => c.Effects))) yield return item;
        foreach (var item in project.Encounters.SelectMany(x => x.OnStartEffects.Concat(x.OnWinEffects).Concat(x.OnLoseEffects).Concat(x.Choices.SelectMany(c => c.Effects)))) yield return item;
        foreach (var item in project.Actions.SelectMany(x => x.Effects)) yield return item;
        foreach (var item in project.StatusEffects.SelectMany(x => x.OnApplyEffects.Concat(x.PeriodicEffects).Concat(x.OnExpireEffects))) yield return item;
        foreach (var item in project.ProgressionNodes.SelectMany(x => x.UnlockEffects)) yield return item;
        foreach (var item in project.WorldState.Time.Segments.SelectMany(x => x.OnEnterEffects)) yield return item;
        foreach (var item in project.WorldState.Aspects.SelectMany(x => x.States.SelectMany(s => s.OnEnterEffects))) yield return item;
        foreach (var item in project.WorldState.AmbientEvents.SelectMany(x => x.Effects)) yield return item;
        foreach (var item in project.WorldState.Rules.SelectMany(x => x.Effects)) yield return item;
    }

    private static SaveGame CreateProbeSave(GameProjectData project)
    {
        var save = new SaveGame
        {
            ProjectId = project.Meta.Id,
            CurrentSceneId = project.Meta.StartSceneId,
            CurrentLocationId = project.Scenes.FirstOrDefault(x => x.Id == project.Meta.StartSceneId)?.LocationId ?? string.Empty,
            PlayerStats = project.Stats.ToDictionary(x => x.Id, x => x.InitialValue),
            Currencies = project.Currencies.ToDictionary(x => x.Id, x => x.InitialAmount),
            Variables = project.Variables.ToDictionary(x => x.Id, x => x.InitialValue),
            Relationships = project.Relationships.ToDictionary(x => x.CharacterId, x => x.InitialValue)
        };
        foreach (var item in project.Items)
        {
            save.Inventory.TryAdd(item.Id, 0);
        }
        return save;
    }
}
