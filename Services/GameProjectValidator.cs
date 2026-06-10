using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Runtime;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameProjectValidator
{
    public GameProjectValidationResult Validate(GameProjectData project)
    {
        var result = new GameProjectValidationResult();
        if (string.IsNullOrWhiteSpace(project.Meta.Id))
        {
            result.Errors.Add("Meta.Id is empty.");
        }
        if (string.IsNullOrWhiteSpace(project.Meta.Title))
        {
            result.Errors.Add("Meta.Title is empty.");
        }
        if (project.Scenes.Count == 0)
        {
            result.Errors.Add("Project has no scenes.");
        }

        var sceneIds = AddDuplicateErrors(project.Scenes.Select(x => x.Id), "Scene", result);
        if (!string.IsNullOrWhiteSpace(project.Meta.StartSceneId) && !sceneIds.Contains(project.Meta.StartSceneId))
        {
            result.Errors.Add("Meta.StartSceneId does not exist in Scenes: " + project.Meta.StartSceneId);
        }
        if (string.IsNullOrWhiteSpace(project.Meta.StartSceneId))
        {
            result.Errors.Add("Meta.StartSceneId is empty.");
        }

        var statIds = AddDuplicateErrors(project.Stats.Select(x => x.Id), "Stat", result);
        var skillIds = AddDuplicateErrors(project.Skills.Select(x => x.Id), "Skill", result);
        var itemIds = AddDuplicateErrors(project.Items.Select(x => x.Id), "Item", result);
        var slotIds = AddDuplicateErrors(project.EquipmentSlots.Select(x => x.Id), "Equipment slot", result);
        var elementIds = AddDuplicateErrors(project.Elements.Select(x => x.Id), "Element", result);
        var currencyIds = AddDuplicateErrors(project.Currencies.Select(x => x.Id), "Currency", result);
        var variableIds = AddDuplicateErrors(project.Variables.Select(x => x.Id), "Variable", result);
        var characterIds = AddDuplicateErrors(project.Characters.Select(x => x.Id), "Character", result);
        var locationIds = AddDuplicateErrors(project.Locations.Select(x => x.Id), "Location", result);
        AddDuplicateErrors(project.LocationConnections.Select(x => x.Id), "Location connection", result);
        var locationStateIds = AddDuplicateErrors(project.LocationStates.Select(x => x.Id), "Location state", result);
        var questIds = AddDuplicateErrors(project.Quests.Select(x => x.Id), "Quest", result);
        AddDuplicateErrors(project.Encounters.Select(x => x.Id), "Encounter", result);
        var actionIds = AddDuplicateErrors(project.Actions.Select(x => x.Id), "Action", result);
        var formulaIds = AddDuplicateErrors(project.Formulas.Select(x => x.Id), "Formula", result);
        var statusEffectIds = AddDuplicateErrors(project.StatusEffects.Select(x => x.Id), "Status effect", result);
        var progressionNodeIds = AddDuplicateErrors(project.ProgressionNodes.Select(x => x.Id), "Progression node", result);

        foreach (var scene in project.Scenes)
        {
            if (!string.IsNullOrWhiteSpace(scene.LocationId) && !locationIds.Contains(scene.LocationId))
            {
                result.Warnings.Add($"Scene '{scene.Id}' points to missing location '{scene.LocationId}'.");
            }

            ValidateChoices(scene.Choices, scene.Id, sceneIds, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, formulaIds, statusEffectIds, progressionNodeIds, result);
        }

        foreach (var slot in project.EquipmentSlots.GroupBy(x => x.Order).Where(x => x.Count() > 1))
        {
            result.Warnings.Add("Equipment slot order is duplicated: " + slot.Key);
        }

        foreach (var item in project.Items)
        {
            if (item.IsEquippable && !string.IsNullOrWhiteSpace(item.SlotId) && !slotIds.Contains(item.SlotId))
            {
                result.Errors.Add($"Item '{item.Id}' uses missing equipment slot '{item.SlotId}'.");
            }
            if (item.IsEquippable && string.IsNullOrWhiteSpace(item.SlotId))
            {
                result.Warnings.Add($"Item '{item.Id}' is equippable but has no SlotId.");
            }
            if ((item.IsConsumable || item.IsUsable) && item.UseEffects.Count == 0)
            {
                result.Warnings.Add($"Item '{item.Id}' is consumable/usable but has no UseEffects.");
            }
            if (item.Value > 0 && string.IsNullOrWhiteSpace(item.CurrencyId))
            {
                result.Warnings.Add($"Item '{item.Id}' has Value > 0 but no CurrencyId.");
            }
            if (item.Value > 0 && !string.IsNullOrWhiteSpace(item.CurrencyId) && !currencyIds.Contains(item.CurrencyId))
            {
                result.Warnings.Add($"Item '{item.Id}' has Value > 0 but CurrencyId is unknown: {item.CurrencyId}.");
            }
            ValidateRequirements(item.Requirements, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, formulaIds, statusEffectIds, progressionNodeIds, result, "item " + item.Id);
            ValidateModifiers(item.Modifiers, statIds, skillIds, result, "item " + item.Id);
            ValidateEffects(item.UseEffects.Concat(item.EquipEffects).Concat(item.UnequipEffects), statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, statusEffectIds, progressionNodeIds, result, "item " + item.Id);
        }

        foreach (var skill in project.Skills)
        {
            if (string.Equals(skill.Kind, "spell", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(skill.ElementId))
            {
                result.Warnings.Add($"Spell skill '{skill.Id}' has no ElementId.");
            }
            if (!string.IsNullOrWhiteSpace(skill.ElementId) && !elementIds.Contains(skill.ElementId))
            {
                result.Errors.Add($"Skill '{skill.Id}' uses missing element '{skill.ElementId}'.");
            }
            ValidateRequirements(skill.LearnRequirements.Concat(skill.UseRequirements), statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, formulaIds, statusEffectIds, progressionNodeIds, result, "skill " + skill.Id);
            ValidateCosts(skill.Costs, statIds, itemIds, currencyIds, variableIds, result, "skill " + skill.Id);
            ValidateEffects(skill.Effects, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, statusEffectIds, progressionNodeIds, result, "skill " + skill.Id);
            ValidateModifiers(skill.PassiveModifiers, statIds, skillIds, result, "skill " + skill.Id);
        }

        foreach (var connection in project.LocationConnections)
        {
            if (!locationIds.Contains(connection.FromLocationId))
            {
                result.Errors.Add($"Location connection '{connection.Id}' has missing FromLocationId '{connection.FromLocationId}'.");
            }
            if (!locationIds.Contains(connection.ToLocationId))
            {
                result.Errors.Add($"Location connection '{connection.Id}' has missing ToLocationId '{connection.ToLocationId}'.");
            }
            ValidateRequirements(connection.Requirements, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, formulaIds, statusEffectIds, progressionNodeIds, result, "location connection " + connection.Id);
            ValidateEffects(connection.TravelEffects, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, statusEffectIds, progressionNodeIds, result, "location connection " + connection.Id);
        }

        foreach (var state in project.LocationStates)
        {
            if (!locationIds.Contains(state.LocationId))
            {
                result.Errors.Add($"Location state '{state.Id}' points to missing location '{state.LocationId}'.");
            }
        }

        foreach (var location in project.Locations)
        {
            ValidateRequirements(location.AccessRequirements, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, formulaIds, statusEffectIds, progressionNodeIds, result, "location " + location.Id);
            ValidateEffects(location.EnterEffects, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, statusEffectIds, progressionNodeIds, result, "location " + location.Id);
        }

        foreach (var encounter in project.Encounters)
        {
            if (!string.IsNullOrWhiteSpace(encounter.SceneId) && !sceneIds.Contains(encounter.SceneId))
            {
                result.Errors.Add($"Encounter '{encounter.Id}' points to missing scene '{encounter.SceneId}'.");
            }

            ValidateRequirements(encounter.Requirements, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, formulaIds, statusEffectIds, progressionNodeIds, result, "encounter " + encounter.Id);
            ValidateEffects(encounter.OnStartEffects.Concat(encounter.OnWinEffects).Concat(encounter.OnLoseEffects), statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, statusEffectIds, progressionNodeIds, result, "encounter " + encounter.Id);
            ValidateChoices(encounter.Choices, "encounter " + encounter.Id, sceneIds, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, formulaIds, statusEffectIds, progressionNodeIds, result);
        }

        foreach (var action in project.Actions)
        {
            if (string.IsNullOrWhiteSpace(action.Name))
            {
                result.Warnings.Add("Action has no Name: " + action.Id);
            }
            if (action.Effects.Count == 0)
            {
                result.Warnings.Add("Action has no Effects: " + action.Id);
            }
            if (action.CooldownTurns < 0)
            {
                result.Warnings.Add("Action has negative CooldownTurns: " + action.Id);
            }
            ValidateRequirements(action.Requirements, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, formulaIds, statusEffectIds, progressionNodeIds, result, "action " + action.Id);
            ValidateCosts(action.Costs, statIds, itemIds, currencyIds, variableIds, result, "action " + action.Id);
            ValidateEffects(action.Effects, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, statusEffectIds, progressionNodeIds, result, "action " + action.Id);
        }

        foreach (var status in project.StatusEffects)
        {
            ValidateRequirements(status.RemoveRequirements, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, formulaIds, statusEffectIds, progressionNodeIds, result, "status effect " + status.Id);
            ValidateEffects(status.OnApplyEffects.Concat(status.PeriodicEffects).Concat(status.OnExpireEffects), statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, statusEffectIds, progressionNodeIds, result, "status effect " + status.Id);
        }

        foreach (var node in project.ProgressionNodes)
        {
            ValidateRequirements(node.UnlockRequirements, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, formulaIds, statusEffectIds, progressionNodeIds, result, "progression node " + node.Id);
            ValidateCosts(node.UnlockCosts, statIds, itemIds, currencyIds, variableIds, result, "progression node " + node.Id);
            ValidateEffects(node.UnlockEffects, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, statusEffectIds, progressionNodeIds, result, "progression node " + node.Id);
        }

        ValidateMechanics(project, formulaIds, statusEffectIds, progressionNodeIds, skillIds, result);
        ValidateFormulaReferences(project, formulaIds, statusEffectIds, progressionNodeIds, result);
        ValidateExperience(project, result);
        ValidateWorldState(project, result, locationIds);
        ValidateCombat(project, statIds, actionIds, result);

        foreach (var relationship in project.Relationships)
        {
            if (!string.IsNullOrWhiteSpace(relationship.CharacterId) && !characterIds.Contains(relationship.CharacterId))
            {
                result.Warnings.Add("Relationship points to missing character: " + relationship.CharacterId);
            }
        }

        foreach (var prompt in project.ImagePrompts)
        {
            if (!TargetExists(project, prompt.TargetType, prompt.TargetEntityId))
            {
                result.Warnings.Add($"Image prompt '{prompt.AssetId}' points to missing {prompt.TargetType}: {prompt.TargetEntityId}");
            }
            if (!string.IsNullOrWhiteSpace(prompt.SelectedImagePath))
            {
                var selectedPath = ImageAssetService.ResolveProjectPath(project, prompt.SelectedImagePath);
                if (!File.Exists(selectedPath))
                {
                    result.Warnings.Add($"Image prompt '{prompt.AssetId}' selected image does not exist: {prompt.SelectedImagePath}");
                }
            }
        }

        foreach (var link in project.AssetLinks)
        {
            if (!TargetExists(project, link.TargetType, link.TargetEntityId))
            {
                result.Warnings.Add($"Asset link '{link.AssetId}' points to missing {link.TargetType}: {link.TargetEntityId}");
            }
            if (!string.IsNullOrWhiteSpace(link.ImagePath))
            {
                var imagePath = ImageAssetService.ResolveProjectPath(project, link.ImagePath);
                if (!File.Exists(imagePath))
                {
                    result.Warnings.Add($"Asset link '{link.AssetId}' image does not exist: {link.ImagePath}");
                }
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private static void ValidateCombat(GameProjectData project, HashSet<string> statIds, HashSet<string> actionIds, GameProjectValidationResult result)
    {
        var combat = project.Combat;
        var healthStatId = string.IsNullOrWhiteSpace(combat?.PlayerHealthStatId) ? "health" : combat.PlayerHealthStatId;
        if (combat?.Enabled == true && !statIds.Contains(healthStatId))
        {
            result.Warnings.Add("Combat PlayerHealthStatId points to missing stat: " + healthStatId);
        }

        var targetScopes = new HashSet<string>(new[] { "self", "player", "enemy", "ally", "anyEnemy", "anyAlly" }, StringComparer.OrdinalIgnoreCase);
        var actorTeams = new HashSet<string>(new[] { "", "player", "ally", "enemy" }, StringComparer.OrdinalIgnoreCase);
        foreach (var encounter in project.Encounters.Where(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) || x.Combatants.Count > 0))
        {
            if (encounter.Combatants.Count == 0)
            {
                result.Errors.Add($"Combat encounter '{encounter.Id}' has no combatants.");
                continue;
            }

            if (!encounter.Combatants.Any(x => x.IsPlayer || string.Equals(x.Team, "player", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Team, "ally", StringComparison.OrdinalIgnoreCase)))
            {
                result.Warnings.Add($"Combat encounter '{encounter.Id}' has no player/ally combatant.");
            }
            if (!encounter.Combatants.Any(x => string.Equals(x.Team, "enemy", StringComparison.OrdinalIgnoreCase)))
            {
                result.Warnings.Add($"Combat encounter '{encounter.Id}' has no enemy combatant.");
            }

            foreach (var combatant in encounter.Combatants)
            {
                if (string.IsNullOrWhiteSpace(combatant.Id))
                {
                    result.Errors.Add($"Combat encounter '{encounter.Id}' has combatant without Id.");
                }
                if (string.IsNullOrWhiteSpace(combatant.Name))
                {
                    result.Warnings.Add($"Combatant '{combatant.Id}' has no Name.");
                }
                if (string.IsNullOrWhiteSpace(combatant.Team))
                {
                    result.Errors.Add($"Combatant '{combatant.Id}' has no Team.");
                }
                else if (!actorTeams.Contains(combatant.Team))
                {
                    result.Errors.Add($"Combatant '{combatant.Id}' has unsupported Team '{combatant.Team}'.");
                }
                if (!combatant.Stats.ContainsKey(healthStatId))
                {
                    result.Warnings.Add($"Combatant '{combatant.Id}' has no health stat '{healthStatId}'.");
                }
                foreach (var actionId in combatant.ActionIds.Where(x => !actionIds.Contains(x)))
                {
                    result.Errors.Add($"Combatant '{combatant.Id}' points to missing action '{actionId}'.");
                }
            }
        }

        foreach (var action in project.Actions.Where(x => x.AvailableInCombat))
        {
            if (!targetScopes.Contains(action.TargetScope))
            {
                result.Errors.Add($"Combat action '{action.Id}' has unsupported TargetScope '{action.TargetScope}'.");
            }
            if (!actorTeams.Contains(action.ActorTeam))
            {
                result.Errors.Add($"Combat action '{action.Id}' has unsupported ActorTeam '{action.ActorTeam}'.");
            }
            if (!action.Effects.Any(IsKnownCombatOrSaveEffect))
            {
                result.Warnings.Add($"Combat action '{action.Id}' has no supported effects.");
            }
            if (action.Effects.Count == 0 && action.Costs.Count == 0 && string.IsNullOrWhiteSpace(action.Description))
            {
                result.Warnings.Add($"Combat action '{action.Id}' has no effects, costs, or description.");
            }
        }
    }

    private static bool IsKnownCombatOrSaveEffect(GameEffect effect)
    {
        return effect.Type.ToLowerInvariant() is "combatdamage" or "combatheal" or "combatstat" or "combatstatus"
            or "stat" or "resource" or "item" or "currency" or "experience" or "playerexperience" or "skillexperience"
            or "relationship" or "quest" or "variable" or "flag" or "learnskill" or "skill" or "status" or "statuseffect"
            or "progression" or "unlockprogression" or "advancetime" or "timesegment" or "worldstate" or "worldaspect" or "log";
    }

    private static void ValidateMechanics(GameProjectData project, HashSet<string> formulaIds, HashSet<string> statusEffectIds, HashSet<string> progressionNodeIds, HashSet<string> skillIds, GameProjectValidationResult result)
    {
        var engine = new GameRuntimeEngine();
        var emptySave = new SaveGame();
        var combatFormulaIds = project.Actions.Where(x => x.AvailableInCombat)
            .SelectMany(x => new[] { x.HitChanceFormulaId, x.DodgeChanceFormulaId, x.BlockChanceFormulaId, x.CritChanceFormulaId })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var formula in project.Formulas)
        {
            if (string.IsNullOrWhiteSpace(formula.Expression))
            {
                result.Errors.Add("Formula expression is empty: " + formula.Id);
            }

            var formulaResult = engine.TryEvaluateFormula(project, emptySave, formula);
            if (!formulaResult.Success)
            {
                if (combatFormulaIds.Contains(formula.Id) && formula.Expression.Contains("actor.", StringComparison.OrdinalIgnoreCase)
                    || combatFormulaIds.Contains(formula.Id) && formula.Expression.Contains("target.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Warnings.Add($"Formula '{formula.Id}' could not be evaluated: {formulaResult.Message}");
            }
        }

        var allowedKinds = new HashSet<string>(new[] { "positive", "negative", "neutral", "custom" }, StringComparer.OrdinalIgnoreCase);
        var allowedStackModes = new HashSet<string>(new[] { "refresh", "stack", "ignore", "replace" }, StringComparer.OrdinalIgnoreCase);
        foreach (var status in project.StatusEffects)
        {
            if (string.IsNullOrWhiteSpace(status.Name))
            {
                result.Warnings.Add("Status effect has no Name: " + status.Id);
            }
            if (!allowedKinds.Contains(status.Kind))
            {
                result.Errors.Add($"Status effect '{status.Id}' has unsupported Kind '{status.Kind}'.");
            }
            if (!allowedStackModes.Contains(status.StackMode))
            {
                result.Errors.Add($"Status effect '{status.Id}' has unsupported StackMode '{status.StackMode}'.");
            }
            if (status.DefaultDurationTurns < 0)
            {
                result.Errors.Add($"Status effect '{status.Id}' has negative DefaultDurationTurns.");
            }
            if (status.MaxStacks < 1)
            {
                result.Errors.Add($"Status effect '{status.Id}' has MaxStacks lower than 1.");
            }
        }

        foreach (var node in project.ProgressionNodes)
        {
            if (string.IsNullOrWhiteSpace(node.Name))
            {
                result.Warnings.Add("Progression node has no Name: " + node.Id);
            }
            foreach (var parentId in node.ParentNodeIds)
            {
                if (!progressionNodeIds.Contains(parentId))
                {
                    result.Errors.Add($"Progression node '{node.Id}' points to missing parent '{parentId}'.");
                }
            }
            if (!string.IsNullOrWhiteSpace(node.SkillId) && !skillIds.Contains(node.SkillId))
            {
                result.Errors.Add($"Progression node '{node.Id}' points to missing skill '{node.SkillId}'.");
            }
            if (HasProgressionCycle(project, node.Id, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
            {
                result.Warnings.Add("Progression parent cycle is possible around node: " + node.Id);
            }
        }

        if (!string.IsNullOrWhiteSpace(project.Mechanics.InitiativeFormulaId) && !formulaIds.Contains(project.Mechanics.InitiativeFormulaId))
        {
            result.Warnings.Add("Mechanics InitiativeFormulaId points to missing formula: " + project.Mechanics.InitiativeFormulaId);
        }
    }

    private static void ValidateFormulaReferences(GameProjectData project, HashSet<string> formulaIds, HashSet<string> statusEffectIds, HashSet<string> progressionNodeIds, GameProjectValidationResult result)
    {
        foreach (var requirement in AllRequirements(project))
        {
            if (!string.IsNullOrWhiteSpace(requirement.FormulaId) && !formulaIds.Contains(requirement.FormulaId))
            {
                result.Warnings.Add("Requirement FormulaId points to missing formula: " + requirement.FormulaId);
            }
            if (UsesRandomOrDice(project, requirement.FormulaId, requirement.FormulaExpression))
            {
                result.Warnings.Add("Requirement uses random()/dice(); availability can be unstable. Prefer deterministic requirements: " + DescribeFormulaReference(requirement.FormulaId, requirement.FormulaExpression));
            }
        }

        foreach (var cost in AllCosts(project))
        {
            if (!string.IsNullOrWhiteSpace(cost.FormulaId) && !formulaIds.Contains(cost.FormulaId))
            {
                result.Warnings.Add("Cost FormulaId points to missing formula: " + cost.FormulaId);
            }
            if (UsesRandomOrDice(project, cost.FormulaId, cost.FormulaExpression))
            {
                result.Warnings.Add("Cost uses random()/dice(); cost is rolled at execution time and is not recommended for basic UI checks: " + DescribeFormulaReference(cost.FormulaId, cost.FormulaExpression));
            }
        }

        foreach (var effect in AllEffects(project))
        {
            if (!string.IsNullOrWhiteSpace(effect.FormulaId) && !formulaIds.Contains(effect.FormulaId))
            {
                result.Warnings.Add("Effect FormulaId points to missing formula: " + effect.FormulaId);
            }
            if (effect.ChancePercent is < 0 or > 100)
            {
                result.Warnings.Add("Effect ChancePercent is outside 0..100 for target: " + effect.TargetId);
            }

            var type = effect.Type.ToLowerInvariant();
            if (type is "status" or "statuseffect")
            {
                var statusId = !string.IsNullOrWhiteSpace(effect.StatusEffectId) ? effect.StatusEffectId : effect.TargetId;
                if (string.IsNullOrWhiteSpace(statusId) || !statusEffectIds.Contains(statusId))
                {
                    result.Warnings.Add("Status effect points to missing StatusEffects entry: " + statusId);
                }
            }
            if (type is "progression" or "unlockprogression" && !progressionNodeIds.Contains(effect.TargetId))
            {
                result.Warnings.Add("Progression effect points to missing ProgressionNodes entry: " + effect.TargetId);
            }
        }
    }

    private static void ValidateExperience(GameProjectData project, GameProjectValidationResult result)
    {
        var engine = new GameRuntimeEngine();
        var save = new SaveGame
        {
            PlayerLevel = Math.Max(1, project.Mechanics.Experience.InitialPlayerLevel),
            PlayerExperience = Math.Max(0, project.Mechanics.Experience.InitialPlayerExperience),
            PlayerStats = project.Stats.ToDictionary(x => x.Id, x => x.InitialValue),
            Currencies = project.Currencies.ToDictionary(x => x.Id, x => x.InitialAmount),
            Variables = project.Variables.ToDictionary(x => x.Id, x => x.InitialValue),
            KnownSkills = project.Skills.Where(x => x.IsKnownByDefault || x.InitialLevel > 0)
                .Select(x => new GameKnownSkill { SkillId = x.Id, Level = Math.Max(1, x.InitialLevel), Experience = 0, IsEnabled = true })
                .ToList()
        };

        var playerFormula = !string.IsNullOrWhiteSpace(project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaId)
            ? project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaId
            : project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression;
        if (!string.IsNullOrWhiteSpace(playerFormula) && !engine.TryEvaluateFormula(project, save, playerFormula).Success)
        {
            result.Warnings.Add("PlayerExperienceToNextLevelFormula contains an error: " + playerFormula);
        }

        if (project.Mechanics.Experience.EnableSkillExperience
            && project.Skills.Count > 0
            && project.Skills.Count(x => x.ExperienceToNextLevel <= 0) > project.Skills.Count / 2
            && string.IsNullOrWhiteSpace(project.Mechanics.Experience.SkillExperienceToNextLevelFormulaId)
            && string.IsNullOrWhiteSpace(project.Mechanics.Experience.SkillExperienceToNextLevelFormulaExpression))
        {
            result.Warnings.Add("Skill experience is enabled, but most skills have ExperienceToNextLevel <= 0 and no shared skill XP threshold formula.");
        }

        if (HasAnyPreference(project.GenerationPreferences) == false && project.Scenes.Count + project.Items.Count + project.Skills.Count + project.ProgressionNodes.Count > 20)
        {
            result.Warnings.Add("Generation preferences are empty. For a complex game it is useful to describe skill/progression/combat/balance expectations.");
        }

        WarnIfProgressionCostsHaveNoSources(project, result);
    }

    private static bool HasAnyPreference(GameGenerationPreferences preferences)
    {
        return !string.IsNullOrWhiteSpace(preferences.GeneralGameplayText)
            || !string.IsNullOrWhiteSpace(preferences.SkillDesignText)
            || !string.IsNullOrWhiteSpace(preferences.ProgressionDesignText)
            || !string.IsNullOrWhiteSpace(preferences.CombatDesignText)
            || !string.IsNullOrWhiteSpace(preferences.AtmosphereDesignText)
            || !string.IsNullOrWhiteSpace(preferences.BalanceText)
            || !string.IsNullOrWhiteSpace(preferences.ForbiddenDesignText)
            || !string.IsNullOrWhiteSpace(preferences.Notes);
    }

    private static void ValidateWorldState(GameProjectData project, GameProjectValidationResult result, HashSet<string> locationIds)
    {
        var worldState = project.WorldState;
        var segmentIds = AddDuplicateErrors(worldState.Time.Segments.Select(x => x.Id), "Time segment", result);
        var aspectIds = AddDuplicateErrors(worldState.Aspects.Select(x => x.Id), "World aspect", result);
        var validTriggers = new HashSet<string>(new[] { "turnEnd", "travel", "action", "actionEnd", "sceneChoice" }, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(worldState.Time.StartSegmentId) && !segmentIds.Contains(worldState.Time.StartSegmentId))
        {
            result.Errors.Add("WorldState.Time.StartSegmentId does not exist: " + worldState.Time.StartSegmentId);
        }

        foreach (var segment in worldState.Time.Segments)
        {
            if (!string.IsNullOrWhiteSpace(segment.NextSegmentId) && !segmentIds.Contains(segment.NextSegmentId))
            {
                result.Errors.Add($"Time segment '{segment.Id}' has missing NextSegmentId '{segment.NextSegmentId}'.");
            }
            ValidateWorldStateEffects(segment.OnEnterEffects, segmentIds, worldState, result, "time segment " + segment.Id);
        }

        foreach (var aspect in worldState.Aspects)
        {
            var stateIds = AddDuplicateErrors(aspect.States.Select(x => x.Id), "World aspect state " + aspect.Id, result);
            if (!string.IsNullOrWhiteSpace(aspect.DefaultStateId) && !stateIds.Contains(aspect.DefaultStateId))
            {
                result.Errors.Add($"World aspect '{aspect.Id}' has missing DefaultStateId '{aspect.DefaultStateId}'.");
            }
            foreach (var state in aspect.States)
            {
                ValidateWorldStateEffects(state.OnEnterEffects, segmentIds, worldState, result, "world aspect state " + aspect.Id + "/" + state.Id);
            }
        }

        foreach (var ambientEvent in worldState.AmbientEvents)
        {
            if (string.IsNullOrWhiteSpace(ambientEvent.Id))
            {
                result.Errors.Add("Ambient event has empty Id.");
            }
            if (string.IsNullOrWhiteSpace(ambientEvent.Name))
            {
                result.Warnings.Add("Ambient event has no Name: " + ambientEvent.Id);
            }
            if (string.IsNullOrWhiteSpace(ambientEvent.Text))
            {
                result.Warnings.Add("Ambient event has no Text: " + ambientEvent.Id);
            }
            ValidateTrigger(ambientEvent.Trigger, validTriggers, result, "ambient event " + ambientEvent.Id);
            ValidateChanceAndWeight(ambientEvent.ChancePercent, ambientEvent.Weight, result, "ambient event " + ambientEvent.Id);
            foreach (var locationId in ambientEvent.LocationIds.Where(x => !locationIds.Contains(x)))
            {
                result.Errors.Add($"Ambient event '{ambientEvent.Id}' points to missing location '{locationId}'.");
            }
            foreach (var segmentId in ambientEvent.TimeSegmentIds.Where(x => !segmentIds.Contains(x)))
            {
                result.Errors.Add($"Ambient event '{ambientEvent.Id}' points to missing time segment '{segmentId}'.");
            }
            ValidateWorldStateRequirements(ambientEvent.Requirements, segmentIds, worldState, result, "ambient event " + ambientEvent.Id);
            ValidateWorldStateEffects(ambientEvent.Effects, segmentIds, worldState, result, "ambient event " + ambientEvent.Id);
        }

        foreach (var rule in worldState.Rules)
        {
            ValidateTrigger(rule.Trigger, validTriggers, result, "world rule " + rule.Id);
            ValidateChanceAndWeight(rule.ChancePercent, 1, result, "world rule " + rule.Id);
            ValidateWorldStateRequirements(rule.Requirements, segmentIds, worldState, result, "world rule " + rule.Id);
            ValidateWorldStateEffects(rule.Effects, segmentIds, worldState, result, "world rule " + rule.Id);
            if (rule.Effects.Count == 0)
            {
                result.Warnings.Add("World rule has no effects: " + rule.Id);
            }
        }

        ValidateWorldStateRequirements(AllRequirements(project), segmentIds, worldState, result, "project");
        ValidateWorldStateEffects(AllEffects(project), segmentIds, worldState, result, "project");

        if (worldState.Enabled && worldState.Time.Segments.Count == 0 && worldState.Aspects.Count == 0)
        {
            result.Warnings.Add("WorldState.Enabled=true, but no time segments or aspects are configured.");
        }

        AddGenreWarnings(worldState, result);
    }

    private static void ValidateTrigger(string trigger, HashSet<string> validTriggers, GameProjectValidationResult result, string owner)
    {
        if (!validTriggers.Contains(string.IsNullOrWhiteSpace(trigger) ? "turnEnd" : trigger))
        {
            result.Errors.Add(owner + " has unknown trigger: " + trigger);
        }
    }

    private static void ValidateChanceAndWeight(int chancePercent, int weight, GameProjectValidationResult result, string owner)
    {
        if (chancePercent is < 0 or > 100)
        {
            result.Errors.Add(owner + " has ChancePercent outside 0..100.");
        }
        if (weight <= 0)
        {
            result.Errors.Add(owner + " has Weight <= 0.");
        }
    }

    private static void ValidateWorldStateRequirements(IEnumerable<GameRequirement> requirements, HashSet<string> segmentIds, GameWorldStateDefinition worldState, GameProjectValidationResult result, string owner)
    {
        foreach (var requirement in requirements)
        {
            var type = requirement.Type.ToLowerInvariant();
            if (type == "timesegment" && !segmentIds.Contains(requirement.TargetId))
            {
                result.Errors.Add($"Requirement timeSegment points to missing segment '{requirement.TargetId}' in {owner}.");
            }
            if (type is "worldstate" or "worldaspect")
            {
                ValidateWorldAspectReference(worldState, requirement.TargetId, !string.IsNullOrWhiteSpace(requirement.StringValue) ? requirement.StringValue : requirement.Text, result, "Requirement", owner);
            }
        }
    }

    private static void ValidateWorldStateEffects(IEnumerable<GameEffect> effects, HashSet<string> segmentIds, GameWorldStateDefinition worldState, GameProjectValidationResult result, string owner)
    {
        foreach (var effect in effects)
        {
            var type = effect.Type.ToLowerInvariant();
            if (type == "timesegment")
            {
                var segmentId = !string.IsNullOrWhiteSpace(effect.TargetId) ? effect.TargetId : effect.StringValue;
                if (!segmentIds.Contains(segmentId))
                {
                    result.Errors.Add($"Effect timeSegment points to missing segment '{segmentId}' in {owner}.");
                }
            }
            if (type is "worldstate" or "worldaspect")
            {
                var stateId = !string.IsNullOrWhiteSpace(effect.StringValue)
                    ? effect.StringValue
                    : effect.Parameters.GetValueOrDefault("stateId") ?? effect.Text ?? string.Empty;
                ValidateWorldAspectReference(worldState, effect.TargetId, stateId, result, "Effect", owner);
            }
        }
    }

    private static void ValidateWorldAspectReference(GameWorldStateDefinition worldState, string aspectId, string stateId, GameProjectValidationResult result, string prefix, string owner)
    {
        var aspect = worldState.Aspects.FirstOrDefault(x => string.Equals(x.Id, aspectId, StringComparison.OrdinalIgnoreCase));
        if (aspect == null)
        {
            result.Errors.Add($"{prefix} worldState/worldAspect points to missing aspect '{aspectId}' in {owner}.");
            return;
        }
        if (!string.IsNullOrWhiteSpace(stateId) && aspect.States.All(x => !string.Equals(x.Id, stateId, StringComparison.OrdinalIgnoreCase)))
        {
            result.Errors.Add($"{prefix} worldState/worldAspect points to missing state '{aspectId}/{stateId}' in {owner}.");
        }
    }

    private static void AddGenreWarnings(GameWorldStateDefinition worldState, GameProjectValidationResult result)
    {
        var allWords = string.Join(" ", worldState.Aspects.SelectMany(x => new[] { x.Id, x.Name, x.Kind }.Concat(x.Tags))).ToLowerInvariant();
        if (worldState.GenreProfile.Equals("fantasy", StringComparison.OrdinalIgnoreCase) && !ContainsAny(allWords, "weather", "time", "moon", "magic", "погода", "луна", "маг"))
        {
            result.Warnings.Add("Fantasy WorldState has no weather/time/moon/magic-like aspect.");
        }
        if (worldState.GenreProfile.Equals("space", StringComparison.OrdinalIgnoreCase) && !ContainsAny(allWords, "ship", "oxygen", "energy", "radiation", "alarm", "communication", "кораб", "кислород", "энерг", "радиа", "тревог", "связ"))
        {
            result.Warnings.Add("Space WorldState has no ship/oxygen/energy/radiation/alarm/communication-like aspect.");
        }
        if (worldState.GenreProfile.Equals("social", StringComparison.OrdinalIgnoreCase) && !ContainsAny(allWords, "time", "social", "mood", "schedule", "day", "настро", "распис", "социал"))
        {
            result.Warnings.Add("Social WorldState has no time/social mood/schedule-like aspect.");
        }
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(text.Contains);
    }

    private static void WarnIfProgressionCostsHaveNoSources(GameProjectData project, GameProjectValidationResult result)
    {
        var costTargets = project.ProgressionNodes.SelectMany(x => x.UnlockCosts)
            .Where(x => x.Type.Equals("currency", StringComparison.OrdinalIgnoreCase) || x.Type.Equals("variable", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Type.ToLowerInvariant() + ":" + x.TargetId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (costTargets.Count == 0)
        {
            return;
        }

        var effectTargets = AllEffects(project)
            .Where(x => x.Type.Equals("currency", StringComparison.OrdinalIgnoreCase) || x.Type.Equals("variable", StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Amount > 0 || !string.IsNullOrWhiteSpace(x.FormulaId) || !string.IsNullOrWhiteSpace(x.FormulaExpression))
            .Select(x => x.Type.ToLowerInvariant() + ":" + x.TargetId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var target in costTargets.Where(x => !effectTargets.Contains(x)))
        {
            result.Warnings.Add("Progression cost has no visible positive action/item/choice source: " + target);
        }
    }

    internal static bool UsesRandomOrDice(GameProjectData project, string formulaId, string formulaExpression)
    {
        if (ContainsRandomOrDice(formulaExpression))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(formulaId))
        {
            return false;
        }

        var formula = project.Formulas.FirstOrDefault(x => string.Equals(x.Id, formulaId, StringComparison.OrdinalIgnoreCase));
        return formula != null && ContainsRandomOrDice(formula.Expression);
    }

    private static bool ContainsRandomOrDice(string expression)
    {
        return expression.Contains("random(", StringComparison.OrdinalIgnoreCase)
            || expression.Contains("dice(", StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeFormulaReference(string formulaId, string formulaExpression)
    {
        if (!string.IsNullOrWhiteSpace(formulaId))
        {
            return formulaId;
        }

        return string.IsNullOrWhiteSpace(formulaExpression) ? "<empty>" : formulaExpression;
    }

    private static bool HasProgressionCycle(GameProjectData project, string nodeId, HashSet<string> seen)
    {
        if (!seen.Add(nodeId))
        {
            return true;
        }

        var node = project.ProgressionNodes.FirstOrDefault(x => string.Equals(x.Id, nodeId, StringComparison.OrdinalIgnoreCase));
        if (node == null)
        {
            return false;
        }

        foreach (var parentId in node.ParentNodeIds)
        {
            if (HasProgressionCycle(project, parentId, new HashSet<string>(seen, StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    public GameProjectValidationResult ValidateStorage(string projectFolder, Models.GameProjectManifest manifest)
    {
        var result = new GameProjectValidationResult();
        var manifestFiles = GetManifestFiles(manifest).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var relativePath in manifestFiles)
        {
            if (!File.Exists(Path.Combine(projectFolder, relativePath)))
            {
                result.Warnings.Add("Manifest points to missing file: " + relativePath);
            }
        }

        var storageFolders = new[]
        {
            Path.Combine(projectFolder, "data"),
            Path.Combine(projectFolder, "prompts", "image-prompts"),
            Path.Combine(projectFolder, "prompts", "generated-candidates"),
            Path.Combine(projectFolder, "prompts", "asset-links")
        };

        foreach (var folder in storageFolders.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(projectFolder, file).Replace('\\', '/');
                if (!manifestFiles.Contains(relativePath)
                    && !relativePath.Equals("data/world.json", StringComparison.OrdinalIgnoreCase)
                    && !relativePath.Equals("data/game-meta.json", StringComparison.OrdinalIgnoreCase))
                {
                    result.Warnings.Add("Storage contains orphan file not listed in manifest: " + relativePath);
                }
            }
        }

        result.IsValid = true;
        return result;
    }

    public GameProjectValidationResult ValidateSave(GameProjectData project, SaveGame save)
    {
        var result = new GameProjectValidationResult();
        var sceneIds = project.Scenes.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var itemIds = project.Items.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var skillIds = project.Skills.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var locationIds = project.Locations.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(save.CurrentSceneId) && !sceneIds.Contains(save.CurrentSceneId))
        {
            result.Errors.Add("SaveGame current scene does not exist: " + save.CurrentSceneId);
        }
        if (!string.IsNullOrWhiteSpace(save.CurrentLocationId) && !locationIds.Contains(save.CurrentLocationId))
        {
            result.Errors.Add("SaveGame current location does not exist: " + save.CurrentLocationId);
        }
        foreach (var entry in save.InventoryEntries)
        {
            if (!itemIds.Contains(entry.ItemId))
            {
                result.Errors.Add("SaveGame inventory entry references missing item: " + entry.ItemId);
            }
        }
        var instanceIds = save.InventoryEntries.Select(x => x.InstanceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var equipped in save.EquippedItems)
        {
            if (!instanceIds.Contains(equipped.Value))
            {
                result.Errors.Add("SaveGame equipped item references missing inventory instance: " + equipped.Value);
            }
        }
        foreach (var skill in save.KnownSkills)
        {
            if (!skillIds.Contains(skill.SkillId))
            {
                result.Errors.Add("SaveGame known skill references missing skill: " + skill.SkillId);
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private static HashSet<string> AddDuplicateErrors(IEnumerable<string> ids, string label, GameProjectValidationResult result)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                result.Errors.Add(label + " id is empty.");
                continue;
            }
            if (!set.Add(id))
            {
                result.Errors.Add(label + " id is duplicated: " + id);
            }
        }

        return set;
    }

    private static bool TargetExists(GameProjectData project, ImageTargetType targetType, string targetEntityId)
    {
        return targetType switch
        {
            ImageTargetType.Scene => project.Scenes.Any(x => x.Id == targetEntityId),
            ImageTargetType.Character => project.Characters.Any(x => x.Id == targetEntityId),
            ImageTargetType.Item => project.Items.Any(x => x.Id == targetEntityId),
            ImageTargetType.Location => project.Locations.Any(x => x.Id == targetEntityId),
            ImageTargetType.Skill => project.Skills.Any(x => x.Id == targetEntityId),
            ImageTargetType.Spell => project.Skills.Any(x => x.Id == targetEntityId),
            ImageTargetType.Equipment => project.Items.Any(x => x.Id == targetEntityId),
            ImageTargetType.Encounter => project.Encounters.Any(x => x.Id == targetEntityId),
            ImageTargetType.Cover => true,
            ImageTargetType.Ui => true,
            _ => false
        };
    }

    private static void ValidateChoices(IEnumerable<GameChoice> choices, string owner, HashSet<string> sceneIds, HashSet<string> statIds, HashSet<string> itemIds, HashSet<string> skillIds, HashSet<string> currencyIds, HashSet<string> locationIds, HashSet<string> locationStateIds, HashSet<string> variableIds, HashSet<string> questIds, HashSet<string> formulaIds, HashSet<string> statusEffectIds, HashSet<string> progressionNodeIds, GameProjectValidationResult result)
    {
        foreach (var choice in choices)
        {
            if (!string.IsNullOrWhiteSpace(choice.NextSceneId) && !sceneIds.Contains(choice.NextSceneId))
            {
                result.Errors.Add($"Choice '{choice.Id}' in {owner} points to missing scene '{choice.NextSceneId}'.");
            }

            ValidateConditions(choice.Conditions, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, statusEffectIds, progressionNodeIds, result, "choice " + choice.Id + " in " + owner);
            ValidateEffects(choice.Effects, statIds, itemIds, skillIds, currencyIds, locationIds, locationStateIds, variableIds, questIds, statusEffectIds, progressionNodeIds, result, "choice " + choice.Id + " in " + owner);
        }
    }

    private static void ValidateConditions(IEnumerable<GameCondition> conditions, HashSet<string> statIds, HashSet<string> itemIds, HashSet<string> skillIds, HashSet<string> currencyIds, HashSet<string> locationIds, HashSet<string> locationStateIds, HashSet<string> variableIds, HashSet<string> statusEffectIds, HashSet<string> progressionNodeIds, GameProjectValidationResult result, string owner)
    {
        foreach (var condition in conditions)
        {
            var known = condition.Type.ToLowerInvariant() switch
            {
                "stat" or "resource" or "effectivestat" => statIds.Contains(condition.TargetId),
                "item" => itemIds.Contains(condition.TargetId),
                "skill" => skillIds.Contains(condition.TargetId),
                "currency" => currencyIds.Contains(condition.TargetId),
                "locationstate" => locationStateIds.Contains(condition.TargetId),
                "variable" => variableIds.Contains(condition.TargetId),
                "location" => locationIds.Contains(condition.TargetId),
                "status" or "statuseffect" => statusEffectIds.Contains(condition.TargetId),
                "progression" or "unlockprogression" => progressionNodeIds.Contains(condition.TargetId),
                "relationship" or "quest" or "flag" => true,
                _ => false
            };
            if (!known)
            {
                result.Warnings.Add($"Unknown or unresolved condition '{condition.Type}:{condition.TargetId}' in {owner}.");
            }
        }
    }

    private static void ValidateRequirements(IEnumerable<GameRequirement> requirements, HashSet<string> statIds, HashSet<string> itemIds, HashSet<string> skillIds, HashSet<string> currencyIds, HashSet<string> locationIds, HashSet<string> locationStateIds, HashSet<string> variableIds, HashSet<string> formulaIds, HashSet<string> statusEffectIds, HashSet<string> progressionNodeIds, GameProjectValidationResult result, string owner)
    {
        foreach (var requirement in requirements)
        {
            var known = requirement.Type.ToLowerInvariant() switch
            {
                "stat" or "resource" or "effectivestat" => statIds.Contains(requirement.TargetId),
                "item" => itemIds.Contains(requirement.TargetId),
                "skill" => skillIds.Contains(requirement.TargetId),
                "currency" => currencyIds.Contains(requirement.TargetId),
                "locationstate" => locationStateIds.Contains(requirement.TargetId),
                "location" => locationIds.Contains(requirement.TargetId),
                "variable" => variableIds.Contains(requirement.TargetId),
                "status" or "statuseffect" => statusEffectIds.Contains(requirement.TargetId),
                "progression" or "unlockprogression" => progressionNodeIds.Contains(requirement.TargetId),
                "formula" => !string.IsNullOrWhiteSpace(requirement.FormulaExpression) || (!string.IsNullOrWhiteSpace(requirement.FormulaId) && formulaIds.Contains(requirement.FormulaId)),
                "timesegment" or "daynumber" or "worldstate" or "worldaspect" => true,
                "relationship" or "quest" or "flag" => true,
                _ => false
            };
            if (!known)
            {
                result.Warnings.Add($"Unknown or unresolved requirement '{requirement.Type}:{requirement.TargetId}' in {owner}.");
            }
        }
    }

    private static void ValidateModifiers(IEnumerable<GameModifier> modifiers, HashSet<string> statIds, HashSet<string> skillIds, GameProjectValidationResult result, string owner)
    {
        foreach (var modifier in modifiers)
        {
            var known = modifier.Type.ToLowerInvariant() switch
            {
                "stat" => statIds.Contains(modifier.TargetId),
                "skillpower" => skillIds.Contains(modifier.TargetId),
                "damage" or "defense" or "social" or "custom" => true,
                _ => false
            };
            if (!known)
            {
                result.Warnings.Add($"Unknown or unresolved modifier '{modifier.Type}:{modifier.TargetId}' in {owner}.");
            }
        }
    }

    private static void ValidateCosts(IEnumerable<GameCost> costs, HashSet<string> statIds, HashSet<string> itemIds, HashSet<string> currencyIds, HashSet<string> variableIds, GameProjectValidationResult result, string owner)
    {
        foreach (var cost in costs)
        {
            if (cost.Amount < 0)
            {
                result.Warnings.Add($"Cost has negative Amount '{cost.Type}:{cost.TargetId}' in {owner}.");
            }
            var known = cost.Type.ToLowerInvariant() switch
            {
                "stat" => statIds.Contains(cost.TargetId),
                "resource" => statIds.Contains(cost.TargetId),
                "item" => itemIds.Contains(cost.TargetId),
                "currency" => currencyIds.Contains(cost.TargetId),
                "variable" => variableIds.Contains(cost.TargetId),
                "cooldown" => true,
                _ => false
            };
            if (!known)
            {
                result.Warnings.Add($"Unknown or unresolved cost '{cost.Type}:{cost.TargetId}' in {owner}.");
            }
        }
    }

    private static void ValidateEffects(IEnumerable<GameEffect> effects, HashSet<string> statIds, HashSet<string> itemIds, HashSet<string> skillIds, HashSet<string> currencyIds, HashSet<string> locationIds, HashSet<string> locationStateIds, HashSet<string> variableIds, HashSet<string> questIds, HashSet<string> statusEffectIds, HashSet<string> progressionNodeIds, GameProjectValidationResult result, string owner)
    {
        foreach (var effect in effects)
        {
            var known = effect.Type.ToLowerInvariant() switch
            {
                "stat" or "resource" => statIds.Contains(effect.TargetId),
                "item" => itemIds.Contains(effect.TargetId),
                "currency" => currencyIds.Contains(effect.TargetId),
                "experience" or "playerexperience" => true,
                "skillexperience" => skillIds.Contains(effect.TargetId),
                "playerlevel" => true,
                "relationship" => true,
                "quest" => questIds.Contains(effect.TargetId),
                "variable" => variableIds.Contains(effect.TargetId),
                "flag" => true,
                "learnskill" or "skill" => skillIds.Contains(effect.TargetId),
                "locationstate" => locationStateIds.Contains(effect.TargetId),
                "location" => locationIds.Contains(effect.TargetId),
                "status" or "statuseffect" => statusEffectIds.Contains(!string.IsNullOrWhiteSpace(effect.StatusEffectId) ? effect.StatusEffectId : effect.TargetId),
                "combatdamage" or "combatheal" or "combatstat" or "combatstatus" => true,
                "progression" or "unlockprogression" => progressionNodeIds.Contains(effect.TargetId),
                "advancetime" or "timesegment" or "worldstate" or "worldaspect" => true,
                "log" => true,
                _ => false
            };
            if (!known)
            {
                result.Warnings.Add($"Unknown or unresolved effect '{effect.Type}:{effect.TargetId}' in {owner}.");
            }
        }
    }

    private static IEnumerable<GameRequirement> AllRequirements(GameProjectData project)
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

    private static IEnumerable<string> GetManifestFiles(Models.GameProjectManifest manifest)
    {
        foreach (var path in manifest.Stats) yield return path;
        foreach (var path in manifest.Skills) yield return path;
        foreach (var path in manifest.Items) yield return path;
        foreach (var path in manifest.EquipmentSlots) yield return path;
        foreach (var path in manifest.Elements) yield return path;
        foreach (var path in manifest.Currencies) yield return path;
        foreach (var path in manifest.Variables) yield return path;
        foreach (var path in manifest.Characters) yield return path;
        foreach (var path in manifest.Relationships) yield return path;
        foreach (var path in manifest.Locations) yield return path;
        foreach (var path in manifest.LocationConnections) yield return path;
        foreach (var path in manifest.LocationStates) yield return path;
        foreach (var path in manifest.Scenes) yield return path;
        foreach (var path in manifest.Quests) yield return path;
        foreach (var path in manifest.Encounters) yield return path;
        foreach (var path in manifest.Actions) yield return path;
        foreach (var path in manifest.Formulas) yield return path;
        foreach (var path in manifest.StatusEffects) yield return path;
        foreach (var path in manifest.ProgressionNodes) yield return path;
        if (!string.IsNullOrWhiteSpace(manifest.WorldState)) yield return manifest.WorldState;
        if (!string.IsNullOrWhiteSpace(manifest.Mechanics)) yield return manifest.Mechanics;
        if (!string.IsNullOrWhiteSpace(manifest.Combat)) yield return manifest.Combat;
        if (!string.IsNullOrWhiteSpace(manifest.GenerationPreferences)) yield return manifest.GenerationPreferences;
        foreach (var path in manifest.ImagePrompts) yield return path;
        foreach (var path in manifest.GeneratedImageCandidates) yield return path;
        foreach (var path in manifest.AssetLinks) yield return path;
    }
}

public sealed class GameProjectValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
