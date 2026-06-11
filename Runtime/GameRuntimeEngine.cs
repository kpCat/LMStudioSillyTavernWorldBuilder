using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Runtime;

internal sealed class GameRuntimeEngine
{
    private sealed class ResolvedGameCost
    {
        public required GameCost Source { get; init; }
        public int ResolvedAmount { get; init; }
    }

    private sealed class ResolvedGameEffect
    {
        public required GameEffect Source { get; init; }
        public int ResolvedAmount { get; init; }
        public bool ShouldApply { get; init; }
        public bool ChanceRolled { get; init; }
    }

    private sealed class ResolutionResult<T>
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public T Value { get; init; } = default!;
    }

    private sealed class ChoiceTransitionResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string NextSceneId { get; init; } = string.Empty;
        public string EncounterId { get; init; } = string.Empty;
        public bool LegacyNextSceneEncounter { get; init; }

        public static ChoiceTransitionResult Failure(string message)
        {
            return new ChoiceTransitionResult { Success = false, Message = message };
        }
    }

    public GameScene GetCurrentScene(GameProjectData project, SaveGame save)
    {
        var oldSceneId = save.CurrentSceneId;
        var scene = GameSceneSafety.ResolvePlayableStartScene(project, oldSceneId);
        if (scene == null)
        {
            return new GameScene { Id = "missing_scene", Title = "Нет сцен", Text = "В проекте пока нет игровых сцен." };
        }

        if (!string.Equals(oldSceneId, scene.Id, StringComparison.OrdinalIgnoreCase))
        {
            save.CurrentSceneId = scene.Id;
            if (!string.IsNullOrWhiteSpace(oldSceneId))
            {
                save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] Runtime start scene repaired: old='{oldSceneId}', new='{scene.Id}'.");
            }
        }

        return scene;
    }

    public IReadOnlyList<GameChoice> GetAvailableChoices(GameProjectData project, SaveGame save)
    {
        if (save.Combat.IsActive)
        {
            return new List<GameChoice>();
        }

        return GetCurrentScene(project, save).Choices
            .Where(choice => choice.Conditions.All(condition => CheckRequirement(project, save, ToRequirement(condition))))
            .ToList();
    }

    public IReadOnlyList<GameInventoryEntry> GetInventory(GameProjectData project, SaveGame save)
    {
        EnsureInventoryEntries(project, save);
        return save.InventoryEntries.Where(x => x.Quantity > 0).ToList();
    }

    public IReadOnlyList<GameItemDefinition> GetUsableItems(GameProjectData project, SaveGame save)
    {
        EnsureInventoryEntries(project, save);
        var ownedIds = save.InventoryEntries.Where(x => x.Quantity > 0).Select(x => x.ItemId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return project.Items.Where(x => ownedIds.Contains(x.Id) && (x.IsUsable || x.IsConsumable || x.UseEffects.Count > 0)).ToList();
    }

    public IReadOnlyList<GameKnownSkill> GetAvailableSkills(GameProjectData project, SaveGame save)
    {
        EnsureKnownSkills(project, save);
        return save.KnownSkills
            .Where(x => x.IsEnabled && x.CooldownRemaining <= 0)
            .Where(x => project.Skills.Any(skill => string.Equals(skill.Id, x.SkillId, StringComparison.OrdinalIgnoreCase)
                && skill.UseRequirements.All(req => CheckRequirement(project, save, req))))
            .ToList();
    }

    public Dictionary<string, int> GetEffectiveStats(GameProjectData project, SaveGame save)
    {
        EnsureInventoryEntries(project, save);
        EnsureKnownSkills(project, save);
        var result = new Dictionary<string, int>(save.PlayerStats, StringComparer.OrdinalIgnoreCase);
        foreach (var stat in project.Stats)
        {
            result.TryAdd(stat.Id, stat.InitialValue);
        }

        foreach (var entry in save.InventoryEntries.Where(x => x.IsEquipped))
        {
            var item = project.Items.FirstOrDefault(x => string.Equals(x.Id, entry.ItemId, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                ApplyModifiers(result, item.Modifiers);
            }
        }

        foreach (var known in save.KnownSkills.Where(x => x.IsEnabled))
        {
            var skill = project.Skills.FirstOrDefault(x => string.Equals(x.Id, known.SkillId, StringComparison.OrdinalIgnoreCase));
            if (skill != null && string.Equals(skill.Kind, "passive", StringComparison.OrdinalIgnoreCase))
            {
                ApplyModifiers(result, skill.PassiveModifiers);
            }
        }

        foreach (var activeStatus in save.ActiveStatusEffects.ToList())
        {
            var definition = project.StatusEffects.FirstOrDefault(x => string.Equals(x.Id, activeStatus.StatusEffectId, StringComparison.OrdinalIgnoreCase));
            if (definition != null)
            {
                ApplyModifiers(result, ScaleModifiers(definition.Modifiers, Math.Max(1, activeStatus.Stacks)));
            }
        }

        var segment = FindCurrentTimeSegment(project, save);
        if (segment != null)
        {
            ApplyModifiers(result, segment.Modifiers);
        }

        foreach (var aspectState in GetCurrentAspectStates(project, save))
        {
            ApplyModifiers(result, aspectState.State.Modifiers);
        }

        return result;
    }

    public IReadOnlyList<string> GetWorldStateSummary(GameProjectData project, SaveGame save)
    {
        var lines = new List<string>();
        if (!project.WorldState.Enabled)
        {
            return lines;
        }

        if (project.WorldState.Time.Enabled)
        {
            var dayLabel = string.IsNullOrWhiteSpace(project.WorldState.Time.DayLabel) ? "День" : project.WorldState.Time.DayLabel;
            lines.Add(dayLabel + " " + Math.Max(1, save.WorldState.DayNumber));
            var segment = FindCurrentTimeSegment(project, save);
            if (segment != null)
            {
                lines.Add(DisplayName(segment.Name, segment.Id));
            }
        }

        foreach (var aspectState in GetCurrentAspectStates(project, save))
        {
            var aspectName = DisplayName(aspectState.Aspect.Name, aspectState.Aspect.Id);
            lines.Add(aspectName + ": " + DisplayName(aspectState.State.Name, aspectState.State.Id));
        }

        return lines;
    }

    public bool ApplyChoice(GameProjectData project, SaveGame save, string choiceId, out string message)
    {
        var result = ApplyChoiceWithResult(project, save, choiceId);
        message = result.Message;
        return result.Success;
    }

    public GameRuntimeOperationResult ApplyChoiceWithResult(GameProjectData project, SaveGame save, string choiceId)
    {
        if (save.Combat.IsActive)
        {
            return OperationFailure("Сейчас идёт бой. Выберите действие на вкладке 'Бой' или завершите ход.");
        }

        var scene = GetCurrentScene(project, save);
        var choice = scene.Choices.FirstOrDefault(x => string.Equals(x.Id, choiceId, StringComparison.OrdinalIgnoreCase));
        if (choice == null)
        {
            return OperationFailure("Выбор не найден.");
        }

        if (!choice.Conditions.All(condition => CheckRequirement(project, save, ToRequirement(condition))))
        {
            return OperationFailure("Условия выбора не выполнены.");
        }

        var transition = ResolveChoiceTransition(project, scene, choice);
        if (!transition.Success)
        {
            return OperationFailure(transition.Message);
        }

        var effects = ResolveEffectsForExecution(project, save, choice.Effects);
        if (!effects.Success)
        {
            return OperationFailure(effects.Message);
        }
        var effectValidation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!effectValidation.Success)
        {
            return effectValidation;
        }

        var beforeLogCount = save.EventLog.Count;
        ApplyResolvedEffects(project, save, effects.Value);

        if (!string.IsNullOrWhiteSpace(transition.EncounterId))
        {
            var encounterResult = StartEncounterFromChoice(project, save, transition.EncounterId, choice.Id, transition.LegacyNextSceneEncounter);
            if (!encounterResult.Success)
            {
                return encounterResult;
            }

            var encounterMessage = "Encounter started: " + transition.EncounterId;
            AddRuntimeLog(save, encounterMessage);
            return OperationSuccess(encounterMessage, save.EventLog.Skip(beforeLogCount).ToList(), effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList());
        }

        if (!string.IsNullOrWhiteSpace(transition.NextSceneId))
        {
            save.CurrentSceneId = transition.NextSceneId;
            var nextScene = project.Scenes.FirstOrDefault(x => string.Equals(x.Id, transition.NextSceneId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(nextScene?.LocationId))
            {
                save.CurrentLocationId = nextScene.LocationId;
                DiscoverLocation(save, nextScene.LocationId);
            }
        }

        RunWorldTriggersIntoLog(project, save, "sceneChoice");
        var message = choice.Text;
        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        return OperationSuccess(message, save.EventLog.Skip(beforeLogCount).ToList(), effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList());
    }

    private static ChoiceTransitionResult ResolveChoiceTransition(GameProjectData project, GameScene scene, GameChoice choice)
    {
        var encounterId = choice.EncounterId?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            return project.Encounters.Any(x => string.Equals(x.Id, encounterId, StringComparison.OrdinalIgnoreCase))
                ? new ChoiceTransitionResult { Success = true, EncounterId = encounterId }
                : ChoiceTransitionResult.Failure($"Transition failed: choice='{choice.Id}', encounterId='{encounterId}', reason='encounter not found'.");
        }

        var nextSceneId = choice.NextSceneId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nextSceneId))
        {
            return new ChoiceTransitionResult { Success = true };
        }

        if (project.Scenes.Any(x => string.Equals(x.Id, nextSceneId, StringComparison.OrdinalIgnoreCase)))
        {
            return new ChoiceTransitionResult { Success = true, NextSceneId = nextSceneId };
        }

        if (project.Encounters.Any(x => string.Equals(x.Id, nextSceneId, StringComparison.OrdinalIgnoreCase)))
        {
            return new ChoiceTransitionResult { Success = true, EncounterId = nextSceneId, LegacyNextSceneEncounter = true };
        }

        return ChoiceTransitionResult.Failure($"Transition failed: choice='{choice.Id}', nextSceneId='{nextSceneId}', reason='scene not found'.");
    }

    private GameRuntimeOperationResult StartEncounterFromChoice(GameProjectData project, SaveGame save, string encounterId, string choiceId, bool legacyNextSceneEncounter)
    {
        var encounter = project.Encounters.FirstOrDefault(x => string.Equals(x.Id, encounterId, StringComparison.OrdinalIgnoreCase));
        if (encounter == null)
        {
            return OperationFailure($"Transition failed: choice='{choiceId}', encounterId='{encounterId}', reason='encounter not found'.");
        }

        if (legacyNextSceneEncounter)
        {
            AddRuntimeLog(save, $"Migration warning: choice '{choiceId}' used nextSceneId as encounter id '{encounterId}'.");
        }

        if (string.Equals(encounter.Kind, "combat", StringComparison.OrdinalIgnoreCase) || encounter.Combatants.Count > 0)
        {
            return StartEncounterCombatWithResult(project, save, encounter.Id);
        }

        var startEffects = ResolveEffectsForExecution(project, save, encounter.OnStartEffects);
        if (!startEffects.Success)
        {
            return OperationFailure("Ошибка старта encounter: " + startEffects.Message);
        }

        var validation = ValidateResolvedEffectsBeforeMutation(project, save, startEffects.Value);
        if (!validation.Success)
        {
            return OperationFailure("Ошибка старта encounter: " + validation.Message);
        }

        ApplyResolvedEffects(project, save, startEffects.Value);
        if (!string.IsNullOrWhiteSpace(encounter.SceneId) && project.Scenes.Any(x => string.Equals(x.Id, encounter.SceneId, StringComparison.OrdinalIgnoreCase)))
        {
            save.CurrentSceneId = encounter.SceneId;
        }

        return OperationSuccess("Encounter started: " + encounter.Id);
    }

    public bool AddItem(GameProjectData project, SaveGame save, string itemId, int quantity)
    {
        if (quantity <= 0 || project.Items.All(x => !string.Equals(x.Id, itemId, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        EnsureInventoryEntries(project, save);
        var item = project.Items.First(x => string.Equals(x.Id, itemId, StringComparison.OrdinalIgnoreCase));
        if (item.IsStackable || item.MaxStack > 1)
        {
            var entry = save.InventoryEntries.FirstOrDefault(x => string.Equals(x.ItemId, itemId, StringComparison.OrdinalIgnoreCase) && !x.IsEquipped);
            if (entry == null)
            {
                entry = CreateInventoryEntry(item, 0);
                save.InventoryEntries.Add(entry);
            }

            entry.Quantity += quantity;
        }
        else
        {
            for (var i = 0; i < quantity; i++)
            {
                save.InventoryEntries.Add(CreateInventoryEntry(item, 1));
            }
        }

        SyncLegacyInventory(save);
        return true;
    }

    public bool RemoveItem(GameProjectData project, SaveGame save, string itemIdOrInstanceId, int quantity)
    {
        if (quantity <= 0)
        {
            return false;
        }

        EnsureInventoryEntries(project, save);
        var instance = save.InventoryEntries.FirstOrDefault(x => string.Equals(x.InstanceId, itemIdOrInstanceId, StringComparison.OrdinalIgnoreCase));
        if (instance != null)
        {
            if (instance.Quantity < quantity || instance.IsEquipped)
            {
                return false;
            }

            instance.Quantity -= quantity;
            save.InventoryEntries.RemoveAll(x => x.Quantity <= 0);
            SyncLegacyInventory(save);
            return true;
        }

        var matchingEntries = save.InventoryEntries
            .Where(x => string.Equals(x.ItemId, itemIdOrInstanceId, StringComparison.OrdinalIgnoreCase) && !x.IsEquipped)
            .ToList();
        if (matchingEntries.Sum(x => x.Quantity) < quantity)
        {
            return false;
        }

        var remaining = quantity;
        foreach (var entry in matchingEntries)
        {
            var take = Math.Min(entry.Quantity, remaining);
            entry.Quantity -= take;
            remaining -= take;
            if (remaining == 0)
            {
                break;
            }
        }

        save.InventoryEntries.RemoveAll(x => x.Quantity <= 0);
        SyncLegacyInventory(save);
        return true;
    }

    public bool EquipItem(GameProjectData project, SaveGame save, string instanceId)
    {
        return EquipItemWithResult(project, save, instanceId).Success;
    }

    public GameRuntimeOperationResult EquipItemWithResult(GameProjectData project, SaveGame save, string instanceId)
    {
        EnsureInventoryEntries(project, save);
        var entry = save.InventoryEntries.FirstOrDefault(x => string.Equals(x.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            return OperationFailure("Предмет не найден.");
        }

        var item = project.Items.FirstOrDefault(x => string.Equals(x.Id, entry.ItemId, StringComparison.OrdinalIgnoreCase));
        if (item == null || !item.IsEquippable || string.IsNullOrWhiteSpace(item.SlotId))
        {
            return OperationFailure("Предмет нельзя надеть.");
        }

        var failedRequirement = item.Requirements.Select(req => CheckRequirementDetailed(project, save, req)).FirstOrDefault(x => !x.Success);
        if (failedRequirement != null)
        {
            return OperationFailure("Требования предмета не выполнены. " + failedRequirement.Message);
        }

        var slot = project.EquipmentSlots.FirstOrDefault(x => string.Equals(x.Id, item.SlotId, StringComparison.OrdinalIgnoreCase));
        if (slot != null && slot.AllowedItemTags.Count > 0 && !item.Tags.Any(tag => slot.AllowedItemTags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
        {
            return OperationFailure("Предмет нельзя надеть в этот слот.");
        }

        var effects = ResolveEffectsForExecution(project, save, item.EquipEffects);
        if (!effects.Success)
        {
            return OperationFailure(effects.Message);
        }
        var effectValidation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!effectValidation.Success)
        {
            return effectValidation;
        }

        var beforeLogCount = save.EventLog.Count;
        if (save.EquippedItems.ContainsKey(item.SlotId))
        {
            var unequip = UnequipItemWithResult(project, save, item.SlotId);
            if (!unequip.Success)
            {
                return unequip;
            }
        }

        entry.IsEquipped = true;
        entry.SlotId = item.SlotId;
        save.EquippedItems[item.SlotId] = entry.InstanceId;
        ApplyResolvedEffects(project, save, effects.Value);
        var message = "Надет предмет: " + DisplayName(item.Name, item.Id);
        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        return OperationSuccess(message, save.EventLog.Skip(beforeLogCount).ToList(), effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList());
    }

    public bool UnequipItem(GameProjectData project, SaveGame save, string slotId)
    {
        return UnequipItemWithResult(project, save, slotId).Success;
    }

    public GameRuntimeOperationResult UnequipItemWithResult(GameProjectData project, SaveGame save, string slotId)
    {
        EnsureInventoryEntries(project, save);
        if (!save.EquippedItems.TryGetValue(slotId, out var instanceId))
        {
            return OperationFailure("В слоте нет надетого предмета.");
        }

        var entry = save.InventoryEntries.FirstOrDefault(x => string.Equals(x.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            save.EquippedItems.Remove(slotId);
            return OperationFailure("Надетый предмет не найден.");
        }

        var item = project.Items.FirstOrDefault(x => string.Equals(x.Id, entry.ItemId, StringComparison.OrdinalIgnoreCase));
        var effects = item == null
            ? new ResolutionResult<List<ResolvedGameEffect>> { Success = true, Value = new List<ResolvedGameEffect>() }
            : ResolveEffectsForExecution(project, save, item.UnequipEffects);
        if (!effects.Success)
        {
            return OperationFailure(effects.Message);
        }
        var effectValidation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!effectValidation.Success)
        {
            return effectValidation;
        }

        var beforeLogCount = save.EventLog.Count;
        entry.IsEquipped = false;
        entry.SlotId = string.Empty;
        save.EquippedItems.Remove(slotId);
        ApplyResolvedEffects(project, save, effects.Value);

        var message = "Снят предмет: " + DisplayName(item?.Name ?? string.Empty, entry.ItemId);
        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        return OperationSuccess(message, save.EventLog.Skip(beforeLogCount).ToList(), effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList());
    }

    public bool UseItem(GameProjectData project, SaveGame save, string instanceId)
    {
        return UseItemWithResult(project, save, instanceId).Success;
    }

    public GameRuntimeOperationResult UseItemWithResult(GameProjectData project, SaveGame save, string instanceId)
    {
        EnsureInventoryEntries(project, save);
        var entry = save.InventoryEntries.FirstOrDefault(x => string.Equals(x.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            return OperationFailure("Предмет не найден.");
        }

        var item = project.Items.FirstOrDefault(x => string.Equals(x.Id, entry.ItemId, StringComparison.OrdinalIgnoreCase));
        if (item == null || !(item.IsUsable || item.IsConsumable || item.UseEffects.Count > 0))
        {
            return OperationFailure("Предмет нельзя использовать.");
        }

        var failedRequirement = item.Requirements.Select(req => CheckRequirementDetailed(project, save, req)).FirstOrDefault(x => !x.Success);
        if (failedRequirement != null)
        {
            return OperationFailure("Требования предмета не выполнены. " + failedRequirement.Message);
        }

        var effects = ResolveEffectsForExecution(project, save, item.UseEffects);
        if (!effects.Success)
        {
            return OperationFailure(effects.Message);
        }
        var effectValidation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!effectValidation.Success)
        {
            return effectValidation;
        }

        var beforeLogCount = save.EventLog.Count;
        ApplyResolvedEffects(project, save, effects.Value);
        if (item.IsConsumable)
        {
            RemoveItem(project, save, entry.InstanceId, 1);
        }

        var message = "Использован предмет: " + DisplayName(item.Name, item.Id);
        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        return OperationSuccess(message, save.EventLog.Skip(beforeLogCount).ToList(), effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList());
    }

    public bool LearnSkill(GameProjectData project, SaveGame save, string skillId)
    {
        EnsureKnownSkills(project, save);
        var skill = project.Skills.FirstOrDefault(x => string.Equals(x.Id, skillId, StringComparison.OrdinalIgnoreCase));
        if (skill == null || save.KnownSkills.Any(x => string.Equals(x.SkillId, skillId, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (skill.LearnRequirements.Any(req => !CheckRequirement(project, save, req)))
        {
            return false;
        }

        save.KnownSkills.Add(new GameKnownSkill { SkillId = skill.Id, Level = Math.Max(1, skill.InitialLevel), IsEnabled = true });
        return true;
    }

    public bool UseSkill(GameProjectData project, SaveGame save, string skillId, string? targetId = null)
    {
        return UseSkillWithResult(project, save, skillId, targetId).Success;
    }

    public GameRuntimeOperationResult UseSkillWithResult(GameProjectData project, SaveGame save, string skillId, string? targetId = null)
    {
        EnsureKnownSkills(project, save);
        var skill = project.Skills.FirstOrDefault(x => string.Equals(x.Id, skillId, StringComparison.OrdinalIgnoreCase));
        var known = save.KnownSkills.FirstOrDefault(x => string.Equals(x.SkillId, skillId, StringComparison.OrdinalIgnoreCase));
        if (skill == null || known == null)
        {
            return OperationFailure("Навык не изучен.");
        }
        if (!known.IsEnabled)
        {
            return OperationFailure("Навык отключён.");
        }
        if (known.CooldownRemaining > 0)
        {
            return OperationFailure("Навык на перезарядке: ещё " + known.CooldownRemaining + " ход(ов).");
        }

        var failedRequirement = skill.UseRequirements.Select(req => CheckRequirementDetailed(project, save, req)).FirstOrDefault(x => !x.Success);
        if (failedRequirement != null)
        {
            return OperationFailure("Требования навыка не выполнены. " + failedRequirement.Message);
        }

        var costs = ResolveCostsForExecution(project, save, skill.Costs);
        if (!costs.Success)
        {
            return OperationFailure(costs.Message);
        }
        var costCheck = CanPayResolvedCosts(project, save, costs.Value);
        if (!costCheck.Success)
        {
            return OperationFailure(costCheck.Message);
        }

        var effects = ResolveEffectsForExecution(project, save, skill.Effects);
        if (!effects.Success)
        {
            return OperationFailure(effects.Message);
        }
        var effectValidation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!effectValidation.Success)
        {
            return effectValidation;
        }

        var beforeLogCount = save.EventLog.Count;
        PayResolvedCosts(project, save, costs.Value);
        ApplyResolvedEffects(project, save, effects.Value);
        known.CooldownRemaining = Math.Max(known.CooldownRemaining, skill.CooldownTurns);
        var message = $"Использован навык: {DisplayName(skill.Name, skill.Id)}";
        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        if (!string.IsNullOrWhiteSpace(targetId))
        {
            save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] Цель навыка: {targetId}");
        }

        return OperationSuccess(message, save.EventLog.Skip(beforeLogCount).ToList(), effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList());
    }

    public int EvaluateFormula(GameProjectData project, SaveGame save, string formulaIdOrExpression)
    {
        var result = TryEvaluateFormula(project, save, formulaIdOrExpression);
        return result.Success ? result.Value : 0;
    }

    public GameRuntimeOperationResult AddPlayerExperienceWithResult(GameProjectData project, SaveGame save, int amount, string sourceId = "")
    {
        if (amount < 0)
        {
            return OperationFailure("Нельзя добавить отрицательный опыт игрока обычным эффектом.");
        }

        var experience = project.Mechanics.Experience;
        var maxLevel = Math.Max(1, experience.MaxPlayerLevel);
        var nextLevel = save.PlayerLevel <= 0 ? Math.Max(1, experience.InitialPlayerLevel) : save.PlayerLevel;
        var nextExperience = save.PlayerExperience;
        var levelUps = 0;
        if (amount == 0)
        {
            return OperationSuccess("Опыт игрока не изменился: +0 XP.");
        }

        nextExperience += amount;
        while (nextLevel < maxLevel)
        {
            var threshold = EvaluatePlayerExperienceThreshold(project, save, nextLevel);
            if (!threshold.Success)
            {
                return OperationFailure("Ошибка формулы порога уровня игрока: " + threshold.Message);
            }
            if (nextExperience < threshold.Value)
            {
                break;
            }

            nextExperience -= threshold.Value;
            nextLevel++;
            levelUps++;
        }
        var levelUpEffects = ResolveLevelUpEffectsBeforeExperienceMutation(project, save, experience.PlayerLevelUpEffects, levelUps, probe =>
        {
            probe.PlayerLevel = nextLevel;
            probe.PlayerExperience = nextExperience;
        }, "Ошибка эффектов повышения уровня игрока: ");
        if (!levelUpEffects.Success)
        {
            return OperationFailure(levelUpEffects.Message);
        }

        var beforeLogCount = save.EventLog.Count;
        save.PlayerLevel = nextLevel;
        save.PlayerExperience = nextExperience;
        var message = levelUps > 0
            ? $"Получено {amount} XP. Уровень игрока повышен до {save.PlayerLevel}."
            : $"Получено {amount} XP.";
        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        foreach (var resolvedEffects in levelUpEffects.Value)
        {
            ApplyResolvedEffects(project, save, resolvedEffects);
        }

        return OperationSuccess(message, save.EventLog.Skip(beforeLogCount).ToList(), new List<string> { "playerExperience:" + amount });
    }

    public GameRuntimeOperationResult AddSkillExperienceWithResult(GameProjectData project, SaveGame save, string skillId, int amount, string sourceId = "")
    {
        if (amount < 0)
        {
            return OperationFailure("Нельзя добавить отрицательный опыт навыка обычным эффектом.");
        }

        EnsureKnownSkills(project, save);
        var definition = project.Skills.FirstOrDefault(x => string.Equals(x.Id, skillId, StringComparison.OrdinalIgnoreCase));
        var known = save.KnownSkills.FirstOrDefault(x => string.Equals(x.SkillId, skillId, StringComparison.OrdinalIgnoreCase));
        if (definition == null || known == null)
        {
            return OperationFailure("Навык не найден или не изучен: " + skillId);
        }
        if (amount == 0)
        {
            return OperationSuccess("Опыт навыка не изменился: +0 XP.");
        }

        var nextLevel = Math.Max(1, known.Level);
        var nextExperience = known.Experience + amount;
        var maxLevel = Math.Max(1, definition.MaxLevel);
        var levelUps = 0;
        while (nextLevel < maxLevel)
        {
            var threshold = EvaluateSkillExperienceThreshold(project, save, definition, nextLevel);
            if (!threshold.Success)
            {
                return OperationFailure("Ошибка формулы порога уровня навыка: " + threshold.Message);
            }
            if (nextExperience < threshold.Value)
            {
                break;
            }

            nextExperience -= threshold.Value;
            nextLevel++;
            levelUps++;
        }
        var levelUpEffects = ResolveLevelUpEffectsBeforeExperienceMutation(project, save, project.Mechanics.Experience.SkillLevelUpEffects, levelUps, probe =>
        {
            var probeKnown = probe.KnownSkills.FirstOrDefault(x => string.Equals(x.SkillId, skillId, StringComparison.OrdinalIgnoreCase));
            if (probeKnown != null)
            {
                probeKnown.Level = nextLevel;
                probeKnown.Experience = nextExperience;
            }
        }, "Ошибка эффектов повышения уровня навыка: ");
        if (!levelUpEffects.Success)
        {
            return OperationFailure(levelUpEffects.Message);
        }

        var beforeLogCount = save.EventLog.Count;
        known.Level = nextLevel;
        known.Experience = nextExperience;
        var message = levelUps > 0
            ? $"Навык {DisplayName(definition.Name, definition.Id)} получил {amount} XP и повышен до уровня {known.Level}."
            : $"Навык {DisplayName(definition.Name, definition.Id)} получил {amount} XP.";
        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        foreach (var resolvedEffects in levelUpEffects.Value)
        {
            ApplyResolvedEffects(project, save, resolvedEffects);
        }

        return OperationSuccess(message, save.EventLog.Skip(beforeLogCount).ToList(), new List<string> { "skillExperience:" + skillId + " " + amount });
    }

    public GameFormulaEvaluationResult TryEvaluateFormula(GameProjectData project, SaveGame save, string formulaIdOrExpression)
    {
        if (string.IsNullOrWhiteSpace(formulaIdOrExpression))
        {
            return new GameFormulaEvaluationResult { Success = false, Message = "Формула пустая." };
        }

        var formula = project.Formulas.FirstOrDefault(x => string.Equals(x.Id, formulaIdOrExpression, StringComparison.OrdinalIgnoreCase));
        return formula == null
            ? TryEvaluateExpression(project, save, formulaIdOrExpression)
            : TryEvaluateFormula(project, save, formula);
    }

    public int EvaluateFormula(GameProjectData project, SaveGame save, GameFormulaDefinition formula)
    {
        var result = TryEvaluateFormula(project, save, formula);
        return result.Success ? result.Value : 0;
    }

    public GameFormulaEvaluationResult TryEvaluateFormula(GameProjectData project, SaveGame save, GameFormulaDefinition formula)
    {
        var result = TryEvaluateExpression(project, save, formula.Expression);
        if (!result.Success)
        {
            return result;
        }

        var value = result.Value;
        if (formula.MinResult.HasValue)
        {
            value = Math.Max(formula.MinResult.Value, value);
        }
        if (formula.MaxResult.HasValue)
        {
            value = Math.Min(formula.MaxResult.Value, value);
        }

        return new GameFormulaEvaluationResult { Success = true, Value = value, Message = "OK" };
    }

    public void TickCooldowns(GameProjectData project, SaveGame save)
    {
        TickCooldowns(project, save, null);
    }

    private void TickCooldowns(GameProjectData project, SaveGame save, GameTurnResult? result)
    {
        foreach (var stat in project.Stats.Where(x => x.RegenPerTurn.HasValue))
        {
            var current = save.PlayerStats.GetValueOrDefault(stat.Id, stat.InitialValue);
            var next = Math.Min(stat.MaxValue, current + stat.RegenPerTurn.GetValueOrDefault());
            save.PlayerStats[stat.Id] = next;
            if (next != current)
            {
                result?.LogLines.Add($"{DisplayName(stat.Name, stat.Id)}: {current} -> {next}.");
            }
        }

        foreach (var skill in save.KnownSkills)
        {
            var before = skill.CooldownRemaining;
            skill.CooldownRemaining = Math.Max(0, skill.CooldownRemaining - 1);
            if (before != skill.CooldownRemaining)
            {
                result?.CooldownChanges.Add($"Навык {skill.SkillId}: {before} -> {skill.CooldownRemaining}");
            }
        }
    }

    public void EndTurn(GameProjectData project, SaveGame save)
    {
        EndTurnWithResult(project, save);
    }

    public GameTurnResult EndTurnWithResult(GameProjectData project, SaveGame save)
    {
        save.TurnNumber++;
        var result = new GameTurnResult { NewTurnNumber = save.TurnNumber };
        result.LogLines.Add("Ход " + save.TurnNumber + " начался.");
        TickCooldowns(project, save, result);
        TickActionCooldowns(save, result);
        TickWorldStateCooldowns(save);
        TickStatusEffects(project, save, result);
        AppendLogLines(save, result.LogLines);
        var timeResult = AdvanceTimeWithResult(project, save, project.WorldState.Time.AdvanceSegmentsOnEndTurn, "endTurn");
        AddOperationLogs(result.LogLines, timeResult);
        var rulesResult = RunWorldRules(project, save, "turnEnd");
        AddOperationLogs(result.LogLines, rulesResult);
        var ambientResult = TryRollAmbientEvent(project, save, "turnEnd");
        AddOperationLogs(result.LogLines, ambientResult);

        return result;
    }

    public GameRuntimeOperationResult AdvanceTimeWithResult(GameProjectData project, SaveGame save, int segments, string sourceId = "")
    {
        if (!project.WorldState.Enabled || !project.WorldState.Time.Enabled)
        {
            return OperationSuccess("World time is disabled.");
        }

        var orderedSegments = project.WorldState.Time.Segments
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .OrderBy(x => x.Order)
            .ToList();
        if (orderedSegments.Count == 0)
        {
            return OperationSuccess("No time segments configured.");
        }

        if (segments <= 0)
        {
            return OperationSuccess("Время не изменилось.");
        }

        var steps = segments;
        var logLines = new List<string>();
        for (var i = 0; i < steps; i++)
        {
            var current = orderedSegments.FirstOrDefault(x => string.Equals(x.Id, save.WorldState.TimeSegmentId, StringComparison.OrdinalIgnoreCase))
                ?? orderedSegments.First();
            var next = GetNextTimeSegment(orderedSegments, current);
            var effects = ResolveEffectsForExecution(project, save, next.OnEnterEffects);
            if (!effects.Success)
            {
                var error = "Ошибка смены сегмента времени: " + effects.Message;
                AddRuntimeLog(save, error);
                logLines.Add(error);
                return OperationFailure(error);
            }

            var validation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
            if (!validation.Success)
            {
                var error = "Ошибка смены сегмента времени: " + validation.Message;
                AddRuntimeLog(save, error);
                logLines.Add(error);
                return OperationFailure(error);
            }

            var wrapped = IsCycleWrap(orderedSegments, current, next);
            save.WorldState.TimeSegmentId = next.Id;
            if (wrapped)
            {
                save.WorldState.DayNumber = Math.Max(1, save.WorldState.DayNumber + 1);
            }

            ApplyResolvedEffects(project, save, effects.Value);
            var line = BuildTimeLogLine(project, save, next);
            logLines.Add(line);
        }

        AppendLogLines(save, logLines);
        return OperationSuccess(logLines.LastOrDefault() ?? "OK", logLines, new List<string>());
    }

    public GameRuntimeOperationResult RunWorldRules(GameProjectData project, SaveGame save, string trigger)
    {
        if (!project.WorldState.Enabled)
        {
            return OperationSuccess("World state is disabled.");
        }

        var logLines = new List<string>();
        foreach (var rule in project.WorldState.Rules
            .Where(x => TriggerEquals(x.Trigger, trigger))
            .OrderBy(x => x.Priority))
        {
            if (save.WorldState.RuleCooldowns.GetValueOrDefault(rule.Id) > 0
                || !RollChance(rule.ChancePercent)
                || rule.Requirements.Any(req => !CheckRequirement(project, save, req)))
            {
                continue;
            }

            var resolved = ResolveEffectsForExecution(project, save, rule.Effects);
            if (!resolved.Success)
            {
                var error = "Ошибка правила мира " + rule.Id + ": " + resolved.Message;
                AddRuntimeLog(save, error);
                logLines.Add(error);
                continue;
            }

            var validation = ValidateResolvedEffectsBeforeMutation(project, save, resolved.Value);
            if (!validation.Success)
            {
                var error = "Ошибка правила мира " + rule.Id + ": " + validation.Message;
                AddRuntimeLog(save, error);
                logLines.Add(error);
                continue;
            }

            var beforeLogCount = save.EventLog.Count;
            ApplyResolvedEffects(project, save, resolved.Value);
            if (rule.CooldownTurns > 0)
            {
                save.WorldState.RuleCooldowns[rule.Id] = rule.CooldownTurns;
            }

            var line = "Правило мира: " + DisplayName(rule.Name, rule.Id);
            AddRuntimeLog(save, line);
            logLines.Add(line);
            logLines.AddRange(save.EventLog.Skip(beforeLogCount));
        }

        return OperationSuccess("World rules processed.", logLines, new List<string>());
    }

    public GameRuntimeOperationResult TryRollAmbientEvent(GameProjectData project, SaveGame save, string trigger)
    {
        if (!project.WorldState.Enabled)
        {
            return OperationSuccess("World state is disabled.");
        }

        var candidates = project.WorldState.AmbientEvents
            .Where(x => TriggerEquals(x.Trigger, trigger))
            .Where(x => save.WorldState.AmbientEventCooldowns.GetValueOrDefault(x.Id) <= 0)
            .Where(x => AmbientEventMatchesLocation(project, save, x))
            .Where(x => AmbientEventMatchesTime(save, x))
            .Where(x => x.Requirements.All(req => CheckRequirement(project, save, req)))
            .Where(x => RollChance(x.ChancePercent))
            .Where(x => x.Weight > 0)
            .ToList();
        if (candidates.Count == 0)
        {
            return OperationSuccess("No ambient event.");
        }

        var selected = PickWeighted(candidates);
        if (selected == null)
        {
            return OperationSuccess("No ambient event.");
        }

        var text = !string.IsNullOrWhiteSpace(selected.Text) ? selected.Text : DisplayName(selected.Name, selected.Id);
        var resolved = ResolveEffectsForExecution(project, save, selected.Effects);
        if (!resolved.Success)
        {
            var error = "Ошибка фонового события " + selected.Id + ": " + resolved.Message;
            AddRuntimeLog(save, error);
            return OperationFailure(error);
        }

        var validation = ValidateResolvedEffectsBeforeMutation(project, save, resolved.Value);
        if (!validation.Success)
        {
            var error = "Ошибка фонового события " + selected.Id + ": " + validation.Message;
            AddRuntimeLog(save, error);
            return OperationFailure(error);
        }

        var logLines = new List<string>();
        var beforeLogCount = save.EventLog.Count;
        AddRuntimeLog(save, text);
        logLines.Add(text);
        ApplyResolvedEffects(project, save, resolved.Value);

        if (selected.CooldownTurns > 0)
        {
            save.WorldState.AmbientEventCooldowns[selected.Id] = selected.CooldownTurns;
        }

        save.WorldState.RecentAmbientEventIds.Add(selected.Id);
        if (save.WorldState.RecentAmbientEventIds.Count > 20)
        {
            save.WorldState.RecentAmbientEventIds.RemoveRange(0, save.WorldState.RecentAmbientEventIds.Count - 20);
        }

        logLines.AddRange(save.EventLog.Skip(beforeLogCount));
        return OperationSuccess(text, logLines, new List<string>());
    }

    public IReadOnlyList<GameLocation> GetAvailableLocations(GameProjectData project, SaveGame save)
    {
        if (string.IsNullOrWhiteSpace(save.CurrentLocationId))
        {
            return project.Locations.Where(x => x.AccessRequirements.All(req => CheckRequirement(project, save, req))).ToList();
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var connection in project.LocationConnections)
        {
            if (string.Equals(connection.FromLocationId, save.CurrentLocationId, StringComparison.OrdinalIgnoreCase)
                && connection.Requirements.All(req => CheckRequirement(project, save, req)))
            {
                ids.Add(connection.ToLocationId);
            }
            if (connection.IsTwoWay
                && string.Equals(connection.ToLocationId, save.CurrentLocationId, StringComparison.OrdinalIgnoreCase)
                && connection.Requirements.All(req => CheckRequirement(project, save, req)))
            {
                ids.Add(connection.FromLocationId);
            }
        }

        return project.Locations
            .Where(x => ids.Contains(x.Id) && x.AccessRequirements.All(req => CheckRequirement(project, save, req)))
            .ToList();
    }

    public bool TravelToLocation(GameProjectData project, SaveGame save, string locationId)
    {
        return TravelToLocationWithResult(project, save, locationId).Success;
    }

    public GameRuntimeOperationResult TravelToLocationWithResult(GameProjectData project, SaveGame save, string locationId)
    {
        var location = project.Locations.FirstOrDefault(x => string.Equals(x.Id, locationId, StringComparison.OrdinalIgnoreCase));
        if (location == null)
        {
            return OperationFailure("Локация недоступна.");
        }

        var failedLocationRequirement = location.AccessRequirements.Select(req => CheckRequirementDetailed(project, save, req)).FirstOrDefault(x => !x.Success);
        if (failedLocationRequirement != null)
        {
            return OperationFailure("Требования перехода не выполнены. " + failedLocationRequirement.Message);
        }

        GameLocationConnection? connection = null;
        if (!string.IsNullOrWhiteSpace(save.CurrentLocationId) && project.LocationConnections.Count > 0)
        {
            connection = project.LocationConnections.FirstOrDefault(x =>
                (string.Equals(x.FromLocationId, save.CurrentLocationId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.ToLocationId, locationId, StringComparison.OrdinalIgnoreCase))
                || (x.IsTwoWay && string.Equals(x.ToLocationId, save.CurrentLocationId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.FromLocationId, locationId, StringComparison.OrdinalIgnoreCase)));
            if (connection == null)
            {
                return OperationFailure("Нет связи с локацией.");
            }

            var failedConnectionRequirement = connection.Requirements.Select(req => CheckRequirementDetailed(project, save, req)).FirstOrDefault(x => !x.Success);
            if (failedConnectionRequirement != null)
            {
                return OperationFailure("Требования перехода не выполнены. " + failedConnectionRequirement.Message);
            }
        }

        var effectsToApply = connection == null
            ? location.EnterEffects
            : connection.TravelEffects.Concat(location.EnterEffects).ToList();
        var effects = ResolveEffectsForExecution(project, save, effectsToApply);
        if (!effects.Success)
        {
            return OperationFailure(effects.Message);
        }
        var effectValidation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!effectValidation.Success)
        {
            return effectValidation;
        }

        var beforeLogCount = save.EventLog.Count;
        ApplyResolvedEffects(project, save, effects.Value);
        save.CurrentLocationId = location.Id;
        DiscoverLocation(save, location.Id);
        AdvanceTimeWithResult(project, save, project.WorldState.Time.AdvanceSegmentsOnTravel, "travel");
        RunWorldTriggersIntoLog(project, save, "travel");
        var message = "Переход в локацию: " + DisplayName(location.Name, location.Id);
        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        return OperationSuccess(message, save.EventLog.Skip(beforeLogCount).ToList(), effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList());
    }

    public bool ApplyAction(GameProjectData project, SaveGame save, string actionId)
    {
        return ExecuteAction(project, save, actionId).Success;
    }

    public GameActionExecutionResult ExecuteAction(GameProjectData project, SaveGame save, string actionId, string? targetId = null)
    {
        var action = project.Actions.FirstOrDefault(x => string.Equals(x.Id, actionId, StringComparison.OrdinalIgnoreCase));
        if (action == null)
        {
            return new GameActionExecutionResult { Success = false, Message = "Действие не найдено." };
        }

        var cooldown = save.ActionCooldowns.GetValueOrDefault(action.Id);
        if (cooldown > 0)
        {
            return new GameActionExecutionResult { Success = false, Message = "Перезарядка: ещё " + cooldown + " ход(ов)." };
        }

        var failedRequirement = action.Requirements.Select(req => CheckRequirementDetailed(project, save, req)).FirstOrDefault(x => !x.Success);
        if (failedRequirement != null)
        {
            return new GameActionExecutionResult { Success = false, Message = "Не выполнено требование: " + failedRequirement.Message };
        }

        var costs = ResolveCostsForExecution(project, save, action.Costs);
        if (!costs.Success)
        {
            return new GameActionExecutionResult { Success = false, Message = costs.Message };
        }
        var costCheck = CanPayResolvedCosts(project, save, costs.Value);
        if (!costCheck.Success)
        {
            return new GameActionExecutionResult { Success = false, Message = costCheck.Message };
        }

        var effects = ResolveEffectsForExecution(project, save, action.Effects);
        if (!effects.Success)
        {
            return new GameActionExecutionResult { Success = false, Message = effects.Message };
        }
        var effectValidation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!effectValidation.Success)
        {
            return new GameActionExecutionResult { Success = false, Message = effectValidation.Message };
        }

        var beforeLogCount = save.EventLog.Count;
        PayResolvedCosts(project, save, costs.Value);
        ApplyResolvedEffects(project, save, effects.Value);
        if (action.CooldownTurns > 0)
        {
            save.ActionCooldowns[action.Id] = action.CooldownTurns;
        }

        AdvanceTimeWithResult(project, save, project.WorldState.Time.AdvanceSegmentsOnAction, "action");
        RunWorldTriggersIntoLog(project, save, "action");

        var message = "Выполнено действие: " + DisplayName(action.Name, action.Id);
        if (!string.IsNullOrWhiteSpace(targetId))
        {
            message += " -> " + targetId;
        }

        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        return new GameActionExecutionResult
        {
            Success = true,
            Message = message,
            LogLines = save.EventLog.Skip(beforeLogCount).ToList(),
            AppliedEffectSummaries = effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList()
        };
    }

    public IReadOnlyList<GameActionDefinition> GetAvailableActions(GameProjectData project, SaveGame save)
    {
        return project.Actions
            .Where(action => CheckActionAvailability(project, save, action.Id).IsAvailable)
            .ToList();
    }

    public GameActionAvailabilityResult CheckActionAvailability(GameProjectData project, SaveGame save, string actionId)
    {
        var action = project.Actions.FirstOrDefault(x => string.Equals(x.Id, actionId, StringComparison.OrdinalIgnoreCase));
        if (action == null)
        {
            return new GameActionAvailabilityResult { IsAvailable = false, Reason = "Действие не найдено." };
        }

        var cooldown = save.ActionCooldowns.GetValueOrDefault(action.Id);
        if (cooldown > 0)
        {
            return new GameActionAvailabilityResult
            {
                IsAvailable = false,
                Reason = "Перезарядка: ещё " + cooldown + " ход(ов).",
                CostSummary = DescribeCosts(action.Costs),
                RequirementSummary = DescribeRequirements(action.Requirements)
            };
        }

        foreach (var requirement in action.Requirements)
        {
            var check = CheckRequirementDetailed(project, save, requirement);
            if (!check.Success)
            {
                return new GameActionAvailabilityResult
                {
                    IsAvailable = false,
                    Reason = "Не выполнено требование: " + check.Message,
                    CostSummary = DescribeCosts(action.Costs),
                    RequirementSummary = DescribeRequirements(action.Requirements)
                };
            }
        }

        var costCheck = CanPayCostsDetailed(project, save, action.Costs);
        if (!costCheck.Success)
        {
            return new GameActionAvailabilityResult
            {
                IsAvailable = false,
                Reason = costCheck.Message,
                CostSummary = DescribeCosts(action.Costs),
                RequirementSummary = DescribeRequirements(action.Requirements)
            };
        }

        return new GameActionAvailabilityResult
        {
            IsAvailable = true,
            Reason = "Доступно.",
            CostSummary = DescribeCosts(action.Costs),
            RequirementSummary = DescribeRequirements(action.Requirements)
        };
    }

    public GameRuntimeOperationResult StartCurrentSceneCombatWithResult(GameProjectData project, SaveGame save)
    {
        if (save.Combat.IsActive)
        {
            return OperationFailure("Бой уже активен.");
        }

        var scene = GetCurrentScene(project, save);
        if (!scene.StartsCombat)
        {
            return OperationFailure("Текущая сцена не запускает бой.");
        }

        var encounter = project.Encounters.FirstOrDefault(x => string.Equals(x.SceneId, scene.Id, StringComparison.OrdinalIgnoreCase) && x.Combatants.Count > 0)
            ?? project.Encounters.FirstOrDefault(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) && x.Combatants.Count > 0);
        if (encounter == null)
        {
            return OperationFailure("Боевой encounter для текущей сцены не найден.");
        }

        return StartEncounterCombatWithResult(project, save, encounter.Id);
    }

    public GameRuntimeOperationResult StartEncounterCombatWithResult(GameProjectData project, SaveGame save, string encounterId)
    {
        if (save.Combat.IsActive)
        {
            return OperationFailure("Бой уже активен.");
        }

        var encounter = project.Encounters.FirstOrDefault(x => string.Equals(x.Id, encounterId, StringComparison.OrdinalIgnoreCase));
        if (encounter == null)
        {
            return OperationFailure("Encounter не найден: " + encounterId);
        }
        if (encounter.Combatants.Count == 0)
        {
            return OperationFailure("В encounter нет участников боя.");
        }

        var startEffects = ResolveEffectsForExecution(project, save, encounter.OnStartEffects);
        if (!startEffects.Success)
        {
            return OperationFailure("Ошибка старта боя: " + startEffects.Message);
        }

        var validation = ValidateResolvedEffectsBeforeMutation(project, save, startEffects.Value);
        if (!validation.Success)
        {
            return OperationFailure("Ошибка старта боя: " + validation.Message);
        }

        var beforeLogCount = save.EventLog.Count;
        ApplyResolvedEffects(project, save, startEffects.Value);
        var combatants = encounter.Combatants
            .Select(x => CreateRuntimeCombatant(project, save, x))
            .OrderByDescending(x => x.Initiative)
            .ThenBy(x => TeamSort(x.Team))
            .ThenBy(x => x.Name)
            .ToList();
        save.Combat = new GameRuntimeCombatState
        {
            IsActive = true,
            EncounterId = encounter.Id,
            RoundNumber = 1,
            CurrentTurnIndex = 0,
            Combatants = combatants,
            VictorySceneId = encounter.VictorySceneId,
            DefeatSceneId = encounter.DefeatSceneId
        };

        var message = string.IsNullOrWhiteSpace(encounter.CombatStartText)
            ? "Бой начался: " + DisplayName(encounter.Name, encounter.Id)
            : encounter.CombatStartText;
        AddRuntimeLog(save, message);
        var enemyResult = RunEnemyAutoTurns(project, save);
        var logLines = save.EventLog.Skip(beforeLogCount).ToList();
        if (!enemyResult.Success)
        {
            return new GameRuntimeOperationResult { Success = false, Message = enemyResult.Message, LogLines = logLines };
        }

        return OperationSuccess(message, logLines, startEffects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList());
    }

    public IReadOnlyList<GameRuntimeCombatant> GetCombatants(GameProjectData project, SaveGame save)
    {
        return save.Combat.IsActive ? save.Combat.Combatants.ToList() : new List<GameRuntimeCombatant>();
    }

    public IReadOnlyList<GameActionDefinition> GetAvailableCombatActions(GameProjectData project, SaveGame save, GameRuntimeCombatant? actor = null)
    {
        if (!save.Combat.IsActive)
        {
            return new List<GameActionDefinition>();
        }

        actor ??= GetCurrentCombatant(project, save);
        if (actor == null || !IsLiving(project, actor))
        {
            return new List<GameActionDefinition>();
        }

        return project.Actions
            .Where(x => x.AvailableInCombat)
            .Where(x => ActorTeamMatches(x, actor))
            .Where(x => actor.ActionIds.Count == 0 || actor.ActionIds.Contains(x.Id, StringComparer.OrdinalIgnoreCase))
            .Where(x => actor.ActionCooldowns.GetValueOrDefault(x.Id) <= 0)
            .ToList();
    }

    public GameRuntimeCombatant? GetCurrentCombatant(GameProjectData project, SaveGame save)
    {
        if (!save.Combat.IsActive || save.Combat.Combatants.Count == 0)
        {
            return null;
        }

        save.Combat.CurrentTurnIndex = Math.Clamp(save.Combat.CurrentTurnIndex, 0, save.Combat.Combatants.Count - 1);
        var current = save.Combat.Combatants[save.Combat.CurrentTurnIndex];
        if (IsLiving(project, current))
        {
            return current;
        }

        AdvanceCombatTurn(project, save);
        return save.Combat.IsActive && save.Combat.Combatants.Count > 0
            ? save.Combat.Combatants[Math.Clamp(save.Combat.CurrentTurnIndex, 0, save.Combat.Combatants.Count - 1)]
            : null;
    }

    public GameCombatActionResult ExecuteCombatActionWithResult(GameProjectData project, SaveGame save, string actionId, string targetRuntimeId)
    {
        var result = ExecuteCombatActionCore(project, save, actionId, targetRuntimeId, false, true);
        if (result.Success && !result.CombatEnded)
        {
            var enemyResult = RunEnemyAutoTurns(project, save);
            if (!enemyResult.Success)
            {
                result.Success = false;
                result.Message = enemyResult.Message;
                result.LogLines.AddRange(enemyResult.LogLines);
            }
            else
            {
                result.LogLines.AddRange(enemyResult.LogLines);
                result.CombatEnded = enemyResult.CombatEnded;
                result.PlayerWon = enemyResult.PlayerWon;
                result.PlayerLost = enemyResult.PlayerLost;
            }
        }

        return result;
    }

    public GameCombatActionResult EndCombatTurnWithResult(GameProjectData project, SaveGame save)
    {
        if (!save.Combat.IsActive)
        {
            return CombatFailure("Бой не активен.");
        }

        var beforeLogCount = save.EventLog.Count;
        AdvanceCombatTurn(project, save);
        var result = RunEnemyAutoTurns(project, save);
        result.LogLines.InsertRange(0, save.EventLog.Skip(beforeLogCount).Except(result.LogLines).ToList());
        if (result.Success && string.IsNullOrWhiteSpace(result.Message))
        {
            result.Message = "Ход завершён.";
        }

        return result;
    }

    private GameCombatActionResult ExecuteCombatActionCore(GameProjectData project, SaveGame save, string actionId, string targetRuntimeId, bool allowEnemyActor, bool advanceAfterAction)
    {
        if (!save.Combat.IsActive)
        {
            return CombatFailure("Бой не активен.");
        }

        var actor = GetCurrentCombatant(project, save);
        if (actor == null || !IsLiving(project, actor))
        {
            return CombatFailure("Текущий участник боя не найден.");
        }
        if (IsEnemy(actor) && !allowEnemyActor)
        {
            return CombatFailure("Сейчас ход противника.");
        }

        var action = project.Actions.FirstOrDefault(x => string.Equals(x.Id, actionId, StringComparison.OrdinalIgnoreCase));
        if (action == null || !action.AvailableInCombat)
        {
            return CombatFailure("Боевой action не найден.");
        }
        if (!ActorTeamMatches(action, actor))
        {
            return CombatFailure("Action недоступен для команды участника.");
        }
        if (actor.ActionIds.Count > 0 && !actor.ActionIds.Contains(action.Id, StringComparer.OrdinalIgnoreCase))
        {
            return CombatFailure("Action недоступен этому участнику.");
        }
        if (actor.ActionCooldowns.GetValueOrDefault(action.Id) > 0)
        {
            return CombatFailure("Перезарядка: ещё " + actor.ActionCooldowns.GetValueOrDefault(action.Id) + " ход(ов).");
        }

        var target = ResolveCombatTarget(project, save, actor, action, targetRuntimeId);
        if (target == null)
        {
            return CombatFailure("Цель недоступна.");
        }

        if (!IsEnemy(actor))
        {
            var failedRequirement = action.Requirements.Select(req => CheckRequirementDetailed(project, save, req)).FirstOrDefault(x => !x.Success);
            if (failedRequirement != null)
            {
                return CombatFailure("Не выполнено требование: " + failedRequirement.Message);
            }
        }

        var costs = ResolveCostsForExecution(project, save, action.Costs);
        if (!costs.Success)
        {
            return CombatFailure(costs.Message);
        }

        if (IsEnemy(actor))
        {
            var actorCostCheck = CanPayCombatantCosts(actor, costs.Value);
            if (!actorCostCheck.Success)
            {
                return CombatFailure(actorCostCheck.Message);
            }
        }
        else
        {
            var costCheck = CanPayResolvedCosts(project, save, costs.Value);
            if (!costCheck.Success)
            {
                return CombatFailure(costCheck.Message);
            }
        }

        var effects = ResolveEffectsForExecution(project, save, action.Effects, actor, target);
        if (!effects.Success)
        {
            return CombatFailure(effects.Message);
        }

        var saveEffects = effects.Value.Where(x => ShouldApplySaveEffectInCombat(action, x.Source)).ToList();
        var saveValidation = ValidateResolvedEffectsBeforeMutation(project, save, saveEffects);
        if (!saveValidation.Success)
        {
            return CombatFailure(saveValidation.Message);
        }

        var hitChance = ResolveCombatChance(project, save, actor, target, action.HitChanceFormulaId, action.HitChanceFormulaExpression, project.Combat?.DefaultHitChanceFormulaId, project.Combat?.DefaultHitChanceFormulaExpression, 85);
        var dodgeChance = ResolveCombatChance(project, save, actor, target, action.DodgeChanceFormulaId, action.DodgeChanceFormulaExpression, project.Combat?.DefaultDodgeChanceFormulaId, project.Combat?.DefaultDodgeChanceFormulaExpression, 0);
        var blockChance = ResolveCombatChance(project, save, actor, target, action.BlockChanceFormulaId, action.BlockChanceFormulaExpression, project.Combat?.DefaultBlockChanceFormulaId, project.Combat?.DefaultBlockChanceFormulaExpression, 0);
        var critChance = ResolveCombatChance(project, save, actor, target, action.CritChanceFormulaId, action.CritChanceFormulaExpression, project.Combat?.DefaultCritChanceFormulaId, project.Combat?.DefaultCritChanceFormulaExpression, 5);
        if (!hitChance.Success) return CombatFailure(hitChance.Message);
        if (!dodgeChance.Success) return CombatFailure(dodgeChance.Message);
        if (!blockChance.Success) return CombatFailure(blockChance.Message);
        if (!critChance.Success) return CombatFailure(critChance.Message);

        var beforeLogCount = save.EventLog.Count;
        if (IsEnemy(actor))
        {
            PayCombatantCosts(actor, costs.Value);
        }
        else
        {
            PayResolvedCosts(project, save, costs.Value);
        }

        var message = DisplayName(actor.Name, actor.RuntimeId) + " применяет " + DisplayName(action.Name, action.Id);
        if (!RollChance(hitChance.Value))
        {
            AddRuntimeLog(save, message + ": промах.");
        }
        else if (RollChance(dodgeChance.Value))
        {
            AddRuntimeLog(save, DisplayName(target.Name, target.RuntimeId) + " уклоняется.");
        }
        else
        {
            IReadOnlyList<ResolvedGameEffect> appliedEffects = effects.Value;
            var blocked = RollChance(blockChance.Value);
            var crit = RollChance(critChance.Value);
            if (blocked)
            {
                appliedEffects = ScaleCombatEffects(appliedEffects, Math.Clamp(action.BlockDamagePercent > 0 ? action.BlockDamagePercent : project.Combat?.DefaultBlockDamagePercent ?? 50, 0, 100), true);
                AddRuntimeLog(save, DisplayName(target.Name, target.RuntimeId) + " блокирует удар.");
            }
            if (crit)
            {
                appliedEffects = ScaleCombatEffects(appliedEffects, Math.Max(0, action.CritMultiplierPercent > 0 ? action.CritMultiplierPercent : project.Combat?.DefaultCritMultiplierPercent ?? 150), false);
                AddRuntimeLog(save, "Критический эффект.");
            }

            ApplyCombatResolvedEffects(project, save, actor, target, action, appliedEffects);
            AddRuntimeLog(save, message + ".");
        }

        if (action.CooldownTurns > 0)
        {
            actor.ActionCooldowns[action.Id] = action.CooldownTurns;
        }

        var end = CheckCombatEndAndApplyRewards(project, save, actor.RuntimeId, target.RuntimeId);
        if (!end.Success)
        {
            return new GameCombatActionResult
            {
                Success = false,
                Message = end.Message,
                ActorId = actor.RuntimeId,
                TargetId = target.RuntimeId,
                LogLines = save.EventLog.Skip(beforeLogCount).ToList()
            };
        }
        if (end.CombatEnded)
        {
            end.ActorId = actor.RuntimeId;
            end.TargetId = target.RuntimeId;
            end.LogLines = save.EventLog.Skip(beforeLogCount).ToList();
            return end;
        }

        if (advanceAfterAction)
        {
            AdvanceCombatTurn(project, save);
        }

        return new GameCombatActionResult
        {
            Success = true,
            Message = message,
            ActorId = actor.RuntimeId,
            TargetId = target.RuntimeId,
            LogLines = save.EventLog.Skip(beforeLogCount).ToList(),
            AppliedEffectSummaries = effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList()
        };
    }

    private GameCombatActionResult RunEnemyAutoTurns(GameProjectData project, SaveGame save)
    {
        var result = new GameCombatActionResult { Success = true, Message = "OK" };
        if (!save.Combat.IsActive)
        {
            return result;
        }

        var guard = Math.Max(1, save.Combat.Combatants.Count * 3);
        var beforeLogCount = save.EventLog.Count;
        while (save.Combat.IsActive && guard-- > 0)
        {
            var actor = GetCurrentCombatant(project, save);
            if (actor == null || !IsEnemy(actor))
            {
                break;
            }

            var action = GetAvailableCombatActions(project, save, actor).FirstOrDefault();
            var target = save.Combat.Combatants.FirstOrDefault(x => !IsEnemy(x) && IsLiving(project, x));
            if (action == null || target == null)
            {
                AddRuntimeLog(save, DisplayName(actor.Name, actor.RuntimeId) + " пропускает ход.");
                AdvanceCombatTurn(project, save);
                continue;
            }

            var actionResult = ExecuteCombatActionCore(project, save, action.Id, target.RuntimeId, true, true);
            if (!actionResult.Success || actionResult.CombatEnded)
            {
                actionResult.LogLines = save.EventLog.Skip(beforeLogCount).ToList();
                return actionResult;
            }
        }

        result.LogLines = save.EventLog.Skip(beforeLogCount).ToList();
        return result;
    }

    private GameCombatActionResult CheckCombatEndAndApplyRewards(GameProjectData project, SaveGame save, string actorId, string targetId)
    {
        if (!save.Combat.IsActive)
        {
            return new GameCombatActionResult { Success = true, ActorId = actorId, TargetId = targetId };
        }

        var enemiesDead = save.Combat.Combatants.Where(IsEnemy).All(x => !IsLiving(project, x));
        var playerSideDead = save.Combat.Combatants.Where(x => !IsEnemy(x)).All(x => !IsLiving(project, x));
        if (!enemiesDead && !playerSideDead)
        {
            return new GameCombatActionResult { Success = true, ActorId = actorId, TargetId = targetId };
        }

        var encounter = project.Encounters.FirstOrDefault(x => string.Equals(x.Id, save.Combat.EncounterId, StringComparison.OrdinalIgnoreCase));
        var effects = enemiesDead ? encounter?.OnWinEffects ?? new List<GameEffect>() : encounter?.OnLoseEffects ?? new List<GameEffect>();
        var resolved = ResolveEffectsForExecution(project, save, effects);
        if (!resolved.Success)
        {
            return CombatFailure("Ошибка завершения боя: " + resolved.Message);
        }

        var validation = ValidateResolvedEffectsBeforeMutation(project, save, resolved.Value);
        if (!validation.Success)
        {
            return CombatFailure("Ошибка завершения боя: " + validation.Message);
        }

        ApplyResolvedEffects(project, save, resolved.Value);
        if (enemiesDead)
        {
            var sceneId = !string.IsNullOrWhiteSpace(save.Combat.VictorySceneId) ? save.Combat.VictorySceneId : encounter?.VictorySceneId;
            if (!string.IsNullOrWhiteSpace(sceneId))
            {
                save.CurrentSceneId = sceneId;
            }
            save.Combat = new GameRuntimeCombatState();
            AddRuntimeLog(save, "Победа.");
            return new GameCombatActionResult { Success = true, Message = "Победа.", CombatEnded = true, PlayerWon = true, ActorId = actorId, TargetId = targetId };
        }

        var defeatSceneId = !string.IsNullOrWhiteSpace(save.Combat.DefeatSceneId) ? save.Combat.DefeatSceneId : encounter?.DefeatSceneId;
        if (!string.IsNullOrWhiteSpace(defeatSceneId))
        {
            save.CurrentSceneId = defeatSceneId;
        }
        save.Combat = new GameRuntimeCombatState();
        AddRuntimeLog(save, "Поражение.");
        return new GameCombatActionResult { Success = true, Message = "Поражение.", CombatEnded = true, PlayerLost = true, ActorId = actorId, TargetId = targetId };
    }

    private GameRuntimeCombatant CreateRuntimeCombatant(GameProjectData project, SaveGame save, GameEncounterCombatantDefinition definition)
    {
        var stats = new Dictionary<string, int>(definition.Stats, StringComparer.OrdinalIgnoreCase);
        if (definition.IsPlayer || string.Equals(definition.Team, "player", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var stat in GetEffectiveStats(project, save))
            {
                stats[stat.Key] = stat.Value;
            }
        }
        foreach (var stat in project.Stats)
        {
            stats.TryAdd(stat.Id, stat.InitialValue);
        }

        var combatant = new GameRuntimeCombatant
        {
            RuntimeId = Ids.New("combatant"),
            DefinitionId = definition.Id,
            Name = DisplayName(definition.Name, definition.Id),
            Team = string.IsNullOrWhiteSpace(definition.Team) ? "enemy" : definition.Team,
            IsPlayer = definition.IsPlayer,
            Stats = stats,
            ActionIds = definition.ActionIds.ToList()
        };
        combatant.Initiative = ResolveInitiative(project, save, definition, combatant);
        return combatant;
    }

    private int ResolveInitiative(GameProjectData project, SaveGame save, GameEncounterCombatantDefinition definition, GameRuntimeCombatant combatant)
    {
        var formula = !string.IsNullOrWhiteSpace(definition.InitiativeFormulaId)
            ? definition.InitiativeFormulaId
            : !string.IsNullOrWhiteSpace(definition.InitiativeFormulaExpression)
                ? definition.InitiativeFormulaExpression
                : !string.IsNullOrWhiteSpace(project.Combat?.DefaultInitiativeFormulaId)
                    ? project.Combat.DefaultInitiativeFormulaId
                    : project.Combat?.DefaultInitiativeFormulaExpression ?? string.Empty;
        if (string.IsNullOrWhiteSpace(formula))
        {
            return combatant.Stats.GetValueOrDefault("agility");
        }

        var result = TryEvaluateFormula(project, save, formula, combatant, combatant);
        return result.Success ? result.Value : combatant.Stats.GetValueOrDefault("agility");
    }

    private static int TeamSort(string team)
    {
        return team.ToLowerInvariant() switch
        {
            "player" => 0,
            "ally" => 1,
            _ => 2
        };
    }

    private static bool IsEnemy(GameRuntimeCombatant combatant)
    {
        return string.Equals(combatant.Team, "enemy", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsLiving(GameProjectData project, GameRuntimeCombatant combatant)
    {
        var healthStat = string.IsNullOrWhiteSpace(project.Combat?.PlayerHealthStatId) ? "health" : project.Combat.PlayerHealthStatId;
        return combatant.Stats.GetValueOrDefault(healthStat) > 0;
    }

    private static bool ActorTeamMatches(GameActionDefinition action, GameRuntimeCombatant actor)
    {
        return string.IsNullOrWhiteSpace(action.ActorTeam)
            || string.Equals(action.ActorTeam, actor.Team, StringComparison.OrdinalIgnoreCase)
            || action.ActorTeam.Equals("player", StringComparison.OrdinalIgnoreCase) && actor.IsPlayer;
    }

    private GameRuntimeCombatant? ResolveCombatTarget(GameProjectData project, SaveGame save, GameRuntimeCombatant actor, GameActionDefinition action, string targetRuntimeId)
    {
        var scope = string.IsNullOrWhiteSpace(action.TargetScope) ? "enemy" : action.TargetScope;
        var target = scope.Equals("self", StringComparison.OrdinalIgnoreCase)
            ? actor
            : save.Combat.Combatants.FirstOrDefault(x => string.Equals(x.RuntimeId, targetRuntimeId, StringComparison.OrdinalIgnoreCase));
        if (target == null || !IsLiving(project, target))
        {
            return null;
        }

        if (scope.Equals("self", StringComparison.OrdinalIgnoreCase))
        {
            return target.RuntimeId == actor.RuntimeId ? target : null;
        }
        if (scope.Equals("enemy", StringComparison.OrdinalIgnoreCase) || scope.Equals("anyEnemy", StringComparison.OrdinalIgnoreCase))
        {
            return IsEnemy(actor) != IsEnemy(target) ? target : null;
        }
        if (scope.Equals("ally", StringComparison.OrdinalIgnoreCase) || scope.Equals("anyAlly", StringComparison.OrdinalIgnoreCase))
        {
            return IsEnemy(actor) == IsEnemy(target) ? target : null;
        }

        return target;
    }

    private GameFormulaEvaluationResult ResolveCombatChance(GameProjectData project, SaveGame save, GameRuntimeCombatant actor, GameRuntimeCombatant target, string actionFormulaId, string actionFormulaExpression, string? defaultFormulaId, string? defaultFormulaExpression, int fallback)
    {
        var formula = !string.IsNullOrWhiteSpace(actionFormulaId)
            ? actionFormulaId
            : !string.IsNullOrWhiteSpace(actionFormulaExpression)
                ? actionFormulaExpression
                : !string.IsNullOrWhiteSpace(defaultFormulaId)
                    ? defaultFormulaId
                    : defaultFormulaExpression ?? string.Empty;
        if (string.IsNullOrWhiteSpace(formula))
        {
            return new GameFormulaEvaluationResult { Success = true, Value = Math.Clamp(fallback, 0, 100), Message = "OK" };
        }

        var result = TryEvaluateFormula(project, save, formula, actor, target);
        if (!result.Success)
        {
            return result;
        }

        result.Value = Math.Clamp(result.Value, 0, 100);
        return result;
    }

    private static IReadOnlyList<ResolvedGameEffect> ScaleCombatEffects(IEnumerable<ResolvedGameEffect> effects, int percent, bool damageOnly)
    {
        return effects.Select(x =>
        {
            var type = x.Source.Type.ToLowerInvariant();
            var shouldScale = type == "combatdamage" || (!damageOnly && type == "combatheal");
            return shouldScale
                ? new ResolvedGameEffect { Source = x.Source, ResolvedAmount = x.ResolvedAmount * percent / 100, ShouldApply = x.ShouldApply, ChanceRolled = x.ChanceRolled }
                : x;
        }).ToList();
    }

    private void ApplyCombatResolvedEffects(GameProjectData project, SaveGame save, GameRuntimeCombatant actor, GameRuntimeCombatant target, GameActionDefinition action, IEnumerable<ResolvedGameEffect> effects)
    {
        foreach (var effect in effects.Where(x => x.ShouldApply))
        {
            var type = effect.Source.Type.ToLowerInvariant();
            if (type is "combatdamage" or "combatheal" or "combatstat" or "combatstatus")
            {
                ApplyCombatEffect(project, actor, target, effect.Source, effect.ResolvedAmount);
                continue;
            }

            if (ShouldApplySaveEffectInCombat(action, effect.Source))
            {
                ApplyResolvedEffects(project, save, new[] { effect });
            }
        }
    }

    private void ApplyCombatEffect(GameProjectData project, GameRuntimeCombatant actor, GameRuntimeCombatant target, GameEffect effect, int amount)
    {
        var recipient = effect.TargetScope.Equals("self", StringComparison.OrdinalIgnoreCase)
            || effect.TargetScope.Equals("actor", StringComparison.OrdinalIgnoreCase)
            ? actor
            : target;
        var healthStat = string.IsNullOrWhiteSpace(project.Combat?.PlayerHealthStatId) ? "health" : project.Combat.PlayerHealthStatId;
        var statId = string.IsNullOrWhiteSpace(effect.TargetId) ? healthStat : effect.TargetId;
        var mode = string.IsNullOrWhiteSpace(effect.Mode) ? "add" : effect.Mode.ToLowerInvariant();
        switch (effect.Type.ToLowerInvariant())
        {
            case "combatdamage":
                recipient.Stats[statId] = Math.Max(0, recipient.Stats.GetValueOrDefault(statId) - Math.Max(0, amount));
                break;
            case "combatheal":
                recipient.Stats[statId] = recipient.Stats.GetValueOrDefault(statId) + Math.Max(0, amount);
                break;
            case "combatstat":
                recipient.Stats[statId] = ApplyNumeric(recipient.Stats.GetValueOrDefault(statId), amount, mode);
                break;
            case "combatstatus":
                var statusId = !string.IsNullOrWhiteSpace(effect.StatusEffectId) ? effect.StatusEffectId : effect.TargetId;
                if (!string.IsNullOrWhiteSpace(statusId))
                {
                    recipient.ActiveStatusEffects.Add(new GameActiveStatusEffect
                    {
                        InstanceId = Ids.New("combat_status"),
                        StatusEffectId = statusId,
                        SourceId = effect.SourceId,
                        RemainingTurns = effect.DurationTurns,
                        Stacks = 1
                    });
                }
                break;
        }
    }

    private static bool ShouldApplySaveEffectInCombat(GameActionDefinition action, GameEffect effect)
    {
        var type = effect.Type.ToLowerInvariant();
        if (type is "combatdamage" or "combatheal" or "combatstat" or "combatstatus")
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(action.TargetScope)
            || action.TargetScope.Equals("self", StringComparison.OrdinalIgnoreCase)
            || action.TargetScope.Equals("player", StringComparison.OrdinalIgnoreCase);
    }

    private static GameFormulaEvaluationResult CanPayCombatantCosts(GameRuntimeCombatant actor, IEnumerable<ResolvedGameCost> costs)
    {
        foreach (var resolved in costs)
        {
            var cost = resolved.Source;
            var type = cost.Type.ToLowerInvariant();
            if (type is not ("stat" or "resource"))
            {
                continue;
            }

            var current = actor.Stats.GetValueOrDefault(cost.TargetId);
            if (current < resolved.ResolvedAmount)
            {
                return new GameFormulaEvaluationResult { Success = false, Message = "Недостаточно: " + DescribeCost(cost, resolved.ResolvedAmount) + $" (есть {current})." };
            }
        }

        return new GameFormulaEvaluationResult { Success = true, Message = "OK" };
    }

    private static void PayCombatantCosts(GameRuntimeCombatant actor, IEnumerable<ResolvedGameCost> costs)
    {
        foreach (var resolved in costs)
        {
            var cost = resolved.Source;
            var type = cost.Type.ToLowerInvariant();
            if (type is "stat" or "resource")
            {
                actor.Stats[cost.TargetId] = actor.Stats.GetValueOrDefault(cost.TargetId) - resolved.ResolvedAmount;
            }
        }
    }

    private void AdvanceCombatTurn(GameProjectData project, SaveGame save)
    {
        if (!save.Combat.IsActive || save.Combat.Combatants.Count == 0)
        {
            return;
        }

        var current = save.Combat.Combatants[Math.Clamp(save.Combat.CurrentTurnIndex, 0, save.Combat.Combatants.Count - 1)];
        current.HasActedThisRound = true;
        var count = save.Combat.Combatants.Count;
        for (var i = 1; i <= count; i++)
        {
            var nextIndex = (save.Combat.CurrentTurnIndex + i) % count;
            if (nextIndex <= save.Combat.CurrentTurnIndex)
            {
                StartNewCombatRound(save);
            }
            if (IsLiving(project, save.Combat.Combatants[nextIndex]))
            {
                save.Combat.CurrentTurnIndex = nextIndex;
                return;
            }
        }
    }

    private static void StartNewCombatRound(SaveGame save)
    {
        save.Combat.RoundNumber++;
        foreach (var combatant in save.Combat.Combatants)
        {
            combatant.HasActedThisRound = false;
            foreach (var key in combatant.ActionCooldowns.Keys.ToList())
            {
                combatant.ActionCooldowns[key] = Math.Max(0, combatant.ActionCooldowns[key] - 1);
            }
        }
    }

    private static GameCombatActionResult CombatFailure(string message)
    {
        return new GameCombatActionResult { Success = false, Message = message };
    }
    public bool CheckRequirement(GameProjectData project, SaveGame save, GameRequirement requirement)
    {
        return CheckRequirementDetailed(project, save, requirement).Success;
    }

    private GameFormulaEvaluationResult CheckRequirementDetailed(GameProjectData project, SaveGame save, GameRequirement requirement)
    {
        var type = requirement.Type.ToLowerInvariant();
        if (type == "formula" || !string.IsNullOrWhiteSpace(requirement.FormulaId) || !string.IsNullOrWhiteSpace(requirement.FormulaExpression))
        {
            var formula = !string.IsNullOrWhiteSpace(requirement.FormulaId) ? requirement.FormulaId : requirement.FormulaExpression;
            var result = TryEvaluateFormula(project, save, formula);
            if (!result.Success)
            {
                return new GameFormulaEvaluationResult { Success = false, Message = "ошибка формулы: " + result.Message };
            }

            return new GameFormulaEvaluationResult
            {
                Success = Compare(result.Value, requirement.Operator, requirement.Value),
                Value = result.Value,
                Message = DescribeRequirement(requirement, result.Value)
            };
        }

        if (type == "flag")
        {
            var exists = save.Flags.Contains(requirement.TargetId, StringComparer.OrdinalIgnoreCase);
            var ok = requirement.Operator == "!=" ? !exists : exists;
            return new GameFormulaEvaluationResult { Success = ok, Value = exists ? 1 : 0, Message = DescribeRequirement(requirement, exists ? 1 : 0) };
        }

        if (type == "timesegment")
        {
            var currentSegmentId = save.WorldState.TimeSegmentId;
            var equals = string.Equals(currentSegmentId, requirement.TargetId, StringComparison.OrdinalIgnoreCase);
            var ok = requirement.Operator == "!=" ? !equals : equals;
            return new GameFormulaEvaluationResult
            {
                Success = ok,
                Value = equals ? 1 : 0,
                Message = $"Требуется время {requirement.TargetId}, сейчас {currentSegmentId}."
            };
        }

        if (type is "worldstate" or "worldaspect")
        {
            var expected = GetStringValue(requirement);
            save.WorldState.AspectStates.TryGetValue(requirement.TargetId, out var currentStateId);
            var equals = string.Equals(currentStateId ?? string.Empty, expected, StringComparison.OrdinalIgnoreCase);
            var ok = requirement.Operator == "!=" ? !equals : equals;
            return new GameFormulaEvaluationResult
            {
                Success = ok,
                Value = equals ? 1 : 0,
                Message = $"Требуется состояние {requirement.TargetId}:{expected}, сейчас {currentStateId ?? "<none>"}."
            };
        }

        var effectiveStats = type is "stat" or "effectivestat" or "resource"
            ? GetEffectiveStats(project, save)
            : null;
        var current = type switch
        {
            "stat" or "effectivestat" or "resource" => effectiveStats!.TryGetValue(requirement.TargetId, out var value) ? value : 0,
            "item" => GetItemQuantity(project, save, requirement.TargetId),
            "skill" => save.KnownSkills.FirstOrDefault(x => string.Equals(x.SkillId, requirement.TargetId, StringComparison.OrdinalIgnoreCase))?.Level ?? 0,
            "relationship" => save.Relationships.TryGetValue(requirement.TargetId, out var value) ? value : 0,
            "quest" => save.CompletedQuestIds.Contains(requirement.TargetId, StringComparer.OrdinalIgnoreCase) ? 2 : save.ActiveQuestIds.Contains(requirement.TargetId, StringComparer.OrdinalIgnoreCase) ? 1 : 0,
            "currency" => save.Currencies.TryGetValue(requirement.TargetId, out var value) ? value : 0,
            "locationstate" => save.LocationStates.TryGetValue(requirement.TargetId, out var state) && string.Equals(state, GetStringValue(requirement), StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            "location" => string.Equals(save.CurrentLocationId, requirement.TargetId, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            "variable" => save.Variables.TryGetValue(requirement.TargetId, out var value) ? value : 0,
            "daynumber" => Math.Max(1, save.WorldState.DayNumber),
            "status" or "statuseffect" => save.ActiveStatusEffects.Where(x => string.Equals(x.StatusEffectId, requirement.TargetId, StringComparison.OrdinalIgnoreCase)).Sum(x => Math.Max(1, x.Stacks)),
            "progression" or "unlockprogression" => save.UnlockedProgressionNodeIds.Contains(requirement.TargetId, StringComparer.OrdinalIgnoreCase) ? 1 : 0,
            _ => int.MinValue
        };

        if (current == int.MinValue)
        {
            return new GameFormulaEvaluationResult { Success = false, Message = "неизвестный тип требования: " + requirement.Type };
        }

        return new GameFormulaEvaluationResult
        {
            Success = Compare(current, requirement.Operator, requirement.Value),
            Value = current,
            Message = DescribeRequirement(requirement, current)
        };
    }

    public void ApplyEffects(GameProjectData project, SaveGame save, IEnumerable<GameEffect> effects)
    {
        foreach (var effect in effects)
        {
            var effectValidation = ValidateEffectFormulas(project, save, new[] { effect });
            if (!effectValidation.Success)
            {
                save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {effectValidation.Message}");
                continue;
            }

            ApplyEffect(project, save, effect);
        }
    }

    private void ApplyEffect(GameProjectData project, SaveGame save, GameEffect effect)
    {
        if (!RollChance(effect.ChancePercent))
        {
            return;
        }

        var type = effect.Type.ToLowerInvariant();
        var mode = string.IsNullOrWhiteSpace(effect.Mode) ? "add" : effect.Mode.ToLowerInvariant();
        var amountResult = GetEffectAmountDetailed(project, save, effect);
        if (!amountResult.Success)
        {
            save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {amountResult.Message}");
            return;
        }

        var amount = amountResult.Value;
        ApplyEffect(project, save, effect, amount);
    }

    private void ApplyEffect(GameProjectData project, SaveGame save, GameEffect effect, int amount)
    {
        var type = effect.Type.ToLowerInvariant();
        var mode = string.IsNullOrWhiteSpace(effect.Mode) ? "add" : effect.Mode.ToLowerInvariant();
        switch (type)
        {
            case "stat":
                save.PlayerStats[effect.TargetId] = ApplyNumeric(save.PlayerStats.GetValueOrDefault(effect.TargetId), amount, mode);
                break;
            case "item":
                if (mode == "remove" || amount < 0)
                {
                    RemoveItem(project, save, effect.TargetId, Math.Abs(amount));
                }
                else
                {
                    AddItem(project, save, effect.TargetId, Math.Max(1, amount));
                }
                break;
            case "currency":
                save.Currencies[effect.TargetId] = Math.Max(0, ApplyNumeric(save.Currencies.GetValueOrDefault(effect.TargetId), amount, mode));
                break;
            case "experience":
            case "playerexperience":
                var playerExperience = AddPlayerExperienceWithResult(project, save, amount, effect.SourceId);
                if (!playerExperience.Success)
                {
                    save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] Ошибка применения опыта игрока: {playerExperience.Message}");
                }
                break;
            case "skillexperience":
                var skillExperience = AddSkillExperienceWithResult(project, save, effect.TargetId, amount, effect.SourceId);
                if (!skillExperience.Success)
                {
                    save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] Ошибка применения опыта навыка {effect.TargetId}: {skillExperience.Message}");
                }
                break;
            case "playerlevel":
                save.PlayerLevel = Math.Clamp(ApplyNumeric(save.PlayerLevel <= 0 ? 1 : save.PlayerLevel, amount, mode), 1, Math.Max(1, project.Mechanics.Experience.MaxPlayerLevel));
                break;
            case "relationship":
                save.Relationships[effect.TargetId] = ApplyNumeric(save.Relationships.GetValueOrDefault(effect.TargetId), amount, mode);
                break;
            case "quest":
                ApplyQuestEffect(save, effect, mode);
                break;
            case "variable":
                save.Variables[effect.TargetId] = ApplyNumeric(save.Variables.GetValueOrDefault(effect.TargetId), amount, mode);
                break;
            case "flag":
                if (mode == "remove")
                {
                    save.Flags.RemoveAll(x => string.Equals(x, effect.TargetId, StringComparison.OrdinalIgnoreCase));
                }
                else if (!save.Flags.Contains(effect.TargetId, StringComparer.OrdinalIgnoreCase))
                {
                    save.Flags.Add(effect.TargetId);
                }
                break;
            case "learnskill":
            case "skill":
                LearnSkill(project, save, effect.TargetId);
                break;
            case "status":
            case "statuseffect":
                ApplyStatusEffect(project, save, effect, mode);
                break;
            case "progression":
            case "unlockprogression":
                UnlockProgressionByEffect(project, save, effect.TargetId);
                break;
            case "locationstate":
                var stateId = GetEffectStringValue(effect);
                if (!string.IsNullOrWhiteSpace(stateId))
                {
                    save.LocationStates[effect.TargetId] = stateId;
                }
                break;
            case "advancetime":
                AdvanceTimeWithResult(project, save, amount, effect.SourceId);
                break;
            case "timesegment":
                var segmentId = !string.IsNullOrWhiteSpace(effect.TargetId) ? effect.TargetId : effect.StringValue;
                if (!string.IsNullOrWhiteSpace(segmentId))
                {
                    SetTimeSegment(project, save, segmentId);
                }
                break;
            case "worldstate":
            case "worldaspect":
                var worldStateId = GetEffectStringValue(effect);
                if (!string.IsNullOrWhiteSpace(effect.TargetId) && !string.IsNullOrWhiteSpace(worldStateId))
                {
                    SetWorldAspectState(project, save, effect.TargetId, worldStateId);
                }
                break;
            case "log":
                if (!string.IsNullOrWhiteSpace(effect.Text))
                {
                    save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {effect.Text}");
                }
                break;
        }
    }

    private static void ApplyQuestEffect(SaveGame save, GameEffect effect, string mode)
    {
        if (mode == "complete" || effect.Amount < 0)
        {
            save.ActiveQuestIds.Remove(effect.TargetId);
            if (!save.CompletedQuestIds.Contains(effect.TargetId))
            {
                save.CompletedQuestIds.Add(effect.TargetId);
            }
        }
        else if (!save.ActiveQuestIds.Contains(effect.TargetId))
        {
            save.ActiveQuestIds.Add(effect.TargetId);
        }
    }

    private GameFormulaEvaluationResult EvaluatePlayerExperienceThreshold(GameProjectData project, SaveGame save, int currentLevel)
    {
        var experience = project.Mechanics.Experience;
        var formula = !string.IsNullOrWhiteSpace(experience.PlayerExperienceToNextLevelFormulaId)
            ? experience.PlayerExperienceToNextLevelFormulaId
            : experience.PlayerExperienceToNextLevelFormulaExpression;
        if (string.IsNullOrWhiteSpace(formula))
        {
            return new GameFormulaEvaluationResult { Success = true, Value = Math.Max(1, 100 * Math.Max(1, currentLevel)), Message = "OK" };
        }

        var probe = CloneSaveForFormula(save);
        probe.PlayerLevel = Math.Max(1, currentLevel);
        return TryEvaluateFormula(project, probe, formula);
    }

    private GameFormulaEvaluationResult EvaluateSkillExperienceThreshold(GameProjectData project, SaveGame save, GameSkillDefinition skill, int currentLevel)
    {
        var experience = project.Mechanics.Experience;
        var formula = !string.IsNullOrWhiteSpace(experience.SkillExperienceToNextLevelFormulaId)
            ? experience.SkillExperienceToNextLevelFormulaId
            : experience.SkillExperienceToNextLevelFormulaExpression;
        if (string.IsNullOrWhiteSpace(formula))
        {
            var fallback = skill.ExperienceToNextLevel > 0 ? skill.ExperienceToNextLevel : 50 * Math.Max(1, currentLevel);
            return new GameFormulaEvaluationResult { Success = true, Value = Math.Max(1, fallback), Message = "OK" };
        }

        var probe = CloneSaveForFormula(save);
        var known = probe.KnownSkills.FirstOrDefault(x => string.Equals(x.SkillId, skill.Id, StringComparison.OrdinalIgnoreCase));
        if (known != null)
        {
            known.Level = Math.Max(1, currentLevel);
        }

        return TryEvaluateFormula(project, probe, formula);
    }

    private static SaveGame CloneSaveForFormula(SaveGame save)
    {
        return new SaveGame
        {
            Id = save.Id,
            ProjectId = save.ProjectId,
            Name = save.Name,
            CurrentSceneId = save.CurrentSceneId,
            PlayerStats = new Dictionary<string, int>(save.PlayerStats, StringComparer.OrdinalIgnoreCase),
            Inventory = new Dictionary<string, int>(save.Inventory, StringComparer.OrdinalIgnoreCase),
            InventoryEntries = save.InventoryEntries.Select(x => new GameInventoryEntry { InstanceId = x.InstanceId, ItemId = x.ItemId, Quantity = x.Quantity, Durability = x.Durability, IsEquipped = x.IsEquipped, SlotId = x.SlotId, Metadata = new Dictionary<string, string>(x.Metadata, StringComparer.OrdinalIgnoreCase) }).ToList(),
            EquippedItems = new Dictionary<string, string>(save.EquippedItems, StringComparer.OrdinalIgnoreCase),
            Currencies = new Dictionary<string, int>(save.Currencies, StringComparer.OrdinalIgnoreCase),
            Relationships = new Dictionary<string, int>(save.Relationships, StringComparer.OrdinalIgnoreCase),
            ActiveQuestIds = save.ActiveQuestIds.ToList(),
            CompletedQuestIds = save.CompletedQuestIds.ToList(),
            KnownSkills = save.KnownSkills.Select(x => new GameKnownSkill { SkillId = x.SkillId, Level = x.Level, Experience = x.Experience, CooldownRemaining = x.CooldownRemaining, IsEnabled = x.IsEnabled }).ToList(),
            CurrentLocationId = save.CurrentLocationId,
            LocationStates = new Dictionary<string, string>(save.LocationStates, StringComparer.OrdinalIgnoreCase),
            DiscoveredLocationIds = save.DiscoveredLocationIds.ToList(),
            Variables = new Dictionary<string, int>(save.Variables, StringComparer.OrdinalIgnoreCase),
            Flags = save.Flags.ToList(),
            EventLog = save.EventLog.ToList(),
            WorldState = new GameRuntimeWorldState
            {
                DayNumber = save.WorldState.DayNumber,
                TimeSegmentId = save.WorldState.TimeSegmentId,
                AspectStates = new Dictionary<string, string>(save.WorldState.AspectStates, StringComparer.OrdinalIgnoreCase),
                RuleCooldowns = new Dictionary<string, int>(save.WorldState.RuleCooldowns, StringComparer.OrdinalIgnoreCase),
                AmbientEventCooldowns = new Dictionary<string, int>(save.WorldState.AmbientEventCooldowns, StringComparer.OrdinalIgnoreCase),
                RecentAmbientEventIds = save.WorldState.RecentAmbientEventIds.ToList()
            },
            Combat = new GameRuntimeCombatState
            {
                IsActive = save.Combat.IsActive,
                EncounterId = save.Combat.EncounterId,
                RoundNumber = save.Combat.RoundNumber,
                CurrentTurnIndex = save.Combat.CurrentTurnIndex,
                VictorySceneId = save.Combat.VictorySceneId,
                DefeatSceneId = save.Combat.DefeatSceneId,
                VictoryHandled = save.Combat.VictoryHandled,
                DefeatHandled = save.Combat.DefeatHandled,
                Combatants = save.Combat.Combatants.Select(CloneCombatant).ToList()
            },
            PlayerLevel = save.PlayerLevel,
            PlayerExperience = save.PlayerExperience,
            TurnNumber = save.TurnNumber,
            ActiveStatusEffects = save.ActiveStatusEffects.Select(x => new GameActiveStatusEffect { InstanceId = x.InstanceId, StatusEffectId = x.StatusEffectId, SourceId = x.SourceId, RemainingTurns = x.RemainingTurns, Stacks = x.Stacks }).ToList(),
            UnlockedProgressionNodeIds = save.UnlockedProgressionNodeIds.ToList(),
            ActionCooldowns = new Dictionary<string, int>(save.ActionCooldowns, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static void ApplyModifiers(Dictionary<string, int> stats, IEnumerable<GameModifier> modifiers)
    {
        foreach (var modifier in modifiers.Where(x => string.Equals(x.Type, "stat", StringComparison.OrdinalIgnoreCase)))
        {
            stats[modifier.TargetId] = ApplyNumeric(stats.GetValueOrDefault(modifier.TargetId), modifier.Amount, modifier.Mode);
        }
    }

    private static IEnumerable<GameModifier> ScaleModifiers(IEnumerable<GameModifier> modifiers, int stacks)
    {
        foreach (var modifier in modifiers)
        {
            yield return new GameModifier
            {
                Type = modifier.Type,
                TargetId = modifier.TargetId,
                Amount = modifier.Amount * stacks,
                Mode = modifier.Mode,
                SourceId = modifier.SourceId,
                Description = modifier.Description
            };
        }
    }

    private static GameRuntimeCombatant CloneCombatant(GameRuntimeCombatant combatant)
    {
        return new GameRuntimeCombatant
        {
            RuntimeId = combatant.RuntimeId,
            DefinitionId = combatant.DefinitionId,
            Name = combatant.Name,
            Team = combatant.Team,
            IsPlayer = combatant.IsPlayer,
            Stats = new Dictionary<string, int>(combatant.Stats, StringComparer.OrdinalIgnoreCase),
            ActionIds = combatant.ActionIds.ToList(),
            ActiveStatusEffects = combatant.ActiveStatusEffects.Select(x => new GameActiveStatusEffect { InstanceId = x.InstanceId, StatusEffectId = x.StatusEffectId, SourceId = x.SourceId, RemainingTurns = x.RemainingTurns, Stacks = x.Stacks }).ToList(),
            ActionCooldowns = new Dictionary<string, int>(combatant.ActionCooldowns, StringComparer.OrdinalIgnoreCase),
            Initiative = combatant.Initiative,
            HasActedThisRound = combatant.HasActedThisRound
        };
    }

    public IReadOnlyList<GameProgressionNodeDefinition> GetAvailableProgressionNodes(GameProjectData project, SaveGame save)
    {
        EnsureKnownSkills(project, save);
        return project.ProgressionNodes
            .Where(node => !save.UnlockedProgressionNodeIds.Contains(node.Id, StringComparer.OrdinalIgnoreCase))
            .Where(node => node.ParentNodeIds.All(parentId => save.UnlockedProgressionNodeIds.Contains(parentId, StringComparer.OrdinalIgnoreCase)))
            .Where(node => node.UnlockRequirements.All(req => CheckRequirement(project, save, req)))
            .Where(node => CanPayCosts(project, save, node.UnlockCosts))
            .Where(node => CanUnlockNodeSkill(project, save, node))
            .ToList();
    }

    public GameRuntimeOperationResult UnlockProgressionNodeWithResult(GameProjectData project, SaveGame save, string nodeId)
    {
        EnsureKnownSkills(project, save);
        var node = project.ProgressionNodes.FirstOrDefault(x => string.Equals(x.Id, nodeId, StringComparison.OrdinalIgnoreCase));
        if (node == null)
        {
            return OperationFailure("Узел прокачки не найден.");
        }
        if (save.UnlockedProgressionNodeIds.Contains(node.Id, StringComparer.OrdinalIgnoreCase))
        {
            return OperationFailure("Узел уже открыт.");
        }
        if (node.ParentNodeIds.Any(parentId => !save.UnlockedProgressionNodeIds.Contains(parentId, StringComparer.OrdinalIgnoreCase)))
        {
            return OperationFailure("Сначала откройте предыдущие узлы.");
        }
        if (node.UnlockRequirements.Any(req => !CheckRequirement(project, save, req)))
        {
            var failed = node.UnlockRequirements.Select(req => CheckRequirementDetailed(project, save, req)).FirstOrDefault(x => !x.Success);
            return OperationFailure("Требования для открытия узла не выполнены." + (failed == null ? string.Empty : " " + failed.Message));
        }
        var costs = ResolveCostsForExecution(project, save, node.UnlockCosts);
        if (!costs.Success)
        {
            return OperationFailure(costs.Message);
        }
        var costCheck = CanPayResolvedCosts(project, save, costs.Value);
        if (!costCheck.Success)
        {
            return OperationFailure(costCheck.Message);
        }

        GameSkillDefinition? skillToLearn = null;
        if (!string.IsNullOrWhiteSpace(node.SkillId))
        {
            skillToLearn = project.Skills.FirstOrDefault(x => string.Equals(x.Id, node.SkillId, StringComparison.OrdinalIgnoreCase));
            if (skillToLearn == null)
            {
                return OperationFailure("Навык узла не найден: " + node.SkillId);
            }
            if (save.KnownSkills.Any(x => string.Equals(x.SkillId, node.SkillId, StringComparison.OrdinalIgnoreCase)))
            {
                return OperationFailure("Навык узла уже изучен: " + node.SkillId);
            }
            var failedLearnRequirement = skillToLearn.LearnRequirements.Select(req => CheckRequirementDetailed(project, save, req)).FirstOrDefault(x => !x.Success);
            if (failedLearnRequirement != null)
            {
                return OperationFailure("Навык узла нельзя изучить: " + failedLearnRequirement.Message);
            }
        }

        var effects = ResolveEffectsForExecution(project, save, node.UnlockEffects);
        if (!effects.Success)
        {
            return OperationFailure(effects.Message);
        }
        var effectValidation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!effectValidation.Success)
        {
            return effectValidation;
        }

        var beforeLogCount = save.EventLog.Count;
        PayResolvedCosts(project, save, costs.Value);
        save.UnlockedProgressionNodeIds.Add(node.Id);
        if (skillToLearn != null)
        {
            save.KnownSkills.Add(new GameKnownSkill { SkillId = skillToLearn.Id, Level = Math.Max(1, skillToLearn.InitialLevel), IsEnabled = true });
        }

        ApplyResolvedEffects(project, save, effects.Value);
        var message = "Открыт узел прокачки: " + DisplayName(node.Name, node.Id);
        save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        return OperationSuccess(message, save.EventLog.Skip(beforeLogCount).ToList(), effects.Value.Where(x => x.ShouldApply).Select(x => DescribeEffect(x.Source, x.ResolvedAmount)).ToList());
    }

    public bool UnlockProgressionNode(GameProjectData project, SaveGame save, string nodeId, out string message)
    {
        var result = UnlockProgressionNodeWithResult(project, save, nodeId);
        message = result.Message;
        return result.Success;
    }

    private bool CanPayCosts(GameProjectData project, SaveGame save, IEnumerable<GameCost> costs)
    {
        return CanPayCostsDetailed(project, save, costs).Success;
    }

    private GameFormulaEvaluationResult CanPayCostsDetailed(GameProjectData project, SaveGame save, IEnumerable<GameCost> costs)
    {
        foreach (var cost in costs)
        {
            var amountResult = GetCostAmountDetailed(project, save, cost);
            if (!amountResult.Success)
            {
                return amountResult;
            }

            var amount = amountResult.Value;
            if (amount < 0)
            {
                return new GameFormulaEvaluationResult { Success = false, Message = "Некорректная отрицательная стоимость: " + DescribeCost(cost, amount) };
            }

            var type = cost.Type.ToLowerInvariant();
            var current = type switch
            {
                "stat" or "resource" => save.PlayerStats.GetValueOrDefault(cost.TargetId),
                "currency" => save.Currencies.GetValueOrDefault(cost.TargetId),
                "item" => GetItemQuantity(project, save, cost.TargetId),
                "variable" => save.Variables.GetValueOrDefault(cost.TargetId),
                "cooldown" => amount,
                _ => int.MinValue
            };

            if (current == int.MinValue)
            {
                return new GameFormulaEvaluationResult { Success = false, Message = "Неизвестный тип стоимости: " + cost.Type };
            }
            if (current < amount)
            {
                return new GameFormulaEvaluationResult { Success = false, Message = "Недостаточно: " + DescribeCost(cost, amount) + $" (есть {current})." };
            }
        }

        return new GameFormulaEvaluationResult { Success = true, Message = "OK" };
    }

    private static void AddOperationLogs(List<string> target, GameRuntimeOperationResult result)
    {
        foreach (var line in result.LogLines)
        {
            if (!target.Contains(line))
            {
                target.Add(line);
            }
        }
    }

    private static void AppendLogLines(SaveGame save, IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            AddRuntimeLog(save, line);
        }
    }

    private static void AddRuntimeLog(SaveGame save, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var entry = $"[{DateTime.Now:HH:mm:ss}] {line}";
        if (save.EventLog.Count == 0 || !string.Equals(save.EventLog[^1], entry, StringComparison.Ordinal))
        {
            save.EventLog.Add(entry);
        }
    }

    private GameRuntimeOperationResult ValidateAdvanceTimeEffect(GameProjectData project, SaveGame probeSave, ResolvedGameEffect effect)
    {
        if (effect.ResolvedAmount <= 0)
        {
            return OperationSuccess("OK");
        }

        var result = AdvanceTimeWithResult(project, probeSave, effect.ResolvedAmount, effect.Source.SourceId);
        return result.Success
            ? OperationSuccess("OK")
            : OperationFailure("Ошибка изменения времени: " + result.Message);
    }

    private GameRuntimeOperationResult ValidateTimeSegmentEffect(GameProjectData project, SaveGame probeSave, GameEffect effect)
    {
        var segmentId = !string.IsNullOrWhiteSpace(effect.TargetId) ? effect.TargetId : effect.StringValue;
        var segment = project.WorldState.Time.Segments.FirstOrDefault(x => string.Equals(x.Id, segmentId, StringComparison.OrdinalIgnoreCase));
        if (segment == null)
        {
            return OperationFailure("Ошибка смены сегмента времени: неизвестный сегмент времени: " + segmentId);
        }

        var effects = ResolveEffectsForExecution(project, probeSave, segment.OnEnterEffects);
        if (!effects.Success)
        {
            return OperationFailure("Ошибка смены сегмента времени: " + effects.Message);
        }

        var validation = ValidateResolvedEffectsBeforeMutation(project, probeSave, effects.Value);
        if (!validation.Success)
        {
            return OperationFailure("Ошибка смены сегмента времени: " + validation.Message);
        }

        return OperationSuccess("OK");
    }

    private GameRuntimeOperationResult ValidateWorldAspectEffect(GameProjectData project, SaveGame probeSave, GameEffect effect)
    {
        var aspect = project.WorldState.Aspects.FirstOrDefault(x => string.Equals(x.Id, effect.TargetId, StringComparison.OrdinalIgnoreCase));
        var stateId = GetEffectStringValue(effect);
        if (aspect == null)
        {
            return OperationFailure("Ошибка изменения состояния мира: неизвестный аспект мира: " + effect.TargetId);
        }
        var state = aspect.States.FirstOrDefault(x => string.Equals(x.Id, stateId, StringComparison.OrdinalIgnoreCase));
        if (state == null)
        {
            return OperationFailure("Ошибка изменения состояния мира: неизвестное состояние мира: " + effect.TargetId + "/" + stateId);
        }

        var effects = ResolveEffectsForExecution(project, probeSave, state.OnEnterEffects);
        if (!effects.Success)
        {
            return OperationFailure("Ошибка изменения состояния мира: " + effects.Message);
        }

        var validation = ValidateResolvedEffectsBeforeMutation(project, probeSave, effects.Value);
        if (!validation.Success)
        {
            return OperationFailure("Ошибка изменения состояния мира: " + validation.Message);
        }

        return OperationSuccess("OK");
    }

    private static void TickWorldStateCooldowns(SaveGame save)
    {
        TickCooldownDictionary(save.WorldState.RuleCooldowns);
        TickCooldownDictionary(save.WorldState.AmbientEventCooldowns);
    }

    private static void TickCooldownDictionary(Dictionary<string, int> cooldowns)
    {
        foreach (var key in cooldowns.Keys.ToList())
        {
            cooldowns[key] = Math.Max(0, cooldowns[key] - 1);
        }
    }

    private static string GetStringValue(GameRequirement requirement)
    {
        return !string.IsNullOrWhiteSpace(requirement.StringValue) ? requirement.StringValue : requirement.Text;
    }

    private static string GetEffectStringValue(GameEffect effect)
    {
        if (!string.IsNullOrWhiteSpace(effect.StringValue))
        {
            return effect.StringValue;
        }
        if (effect.Parameters.TryGetValue("stateId", out var stateId) && !string.IsNullOrWhiteSpace(stateId))
        {
            return stateId;
        }

        return effect.Text ?? string.Empty;
    }

    private static GameTimeSegmentDefinition? FindCurrentTimeSegment(GameProjectData project, SaveGame save)
    {
        return project.WorldState.Time.Segments.FirstOrDefault(x => string.Equals(x.Id, save.WorldState.TimeSegmentId, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<(GameWorldAspectDefinition Aspect, GameWorldAspectStateDefinition State)> GetCurrentAspectStates(GameProjectData project, SaveGame save)
    {
        foreach (var aspect in project.WorldState.Aspects)
        {
            if (!save.WorldState.AspectStates.TryGetValue(aspect.Id, out var stateId))
            {
                stateId = aspect.DefaultStateId;
            }

            var state = aspect.States.FirstOrDefault(x => string.Equals(x.Id, stateId, StringComparison.OrdinalIgnoreCase));
            if (state != null)
            {
                yield return (aspect, state);
            }
        }
    }

    private static GameTimeSegmentDefinition GetNextTimeSegment(IReadOnlyList<GameTimeSegmentDefinition> orderedSegments, GameTimeSegmentDefinition current)
    {
        if (!string.IsNullOrWhiteSpace(current.NextSegmentId))
        {
            var explicitNext = orderedSegments.FirstOrDefault(x => string.Equals(x.Id, current.NextSegmentId, StringComparison.OrdinalIgnoreCase));
            if (explicitNext != null)
            {
                return explicitNext;
            }
        }

        var index = orderedSegments.ToList().FindIndex(x => string.Equals(x.Id, current.Id, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < orderedSegments.Count ? orderedSegments[index + 1] : orderedSegments[0];
    }

    private static bool IsCycleWrap(IReadOnlyList<GameTimeSegmentDefinition> orderedSegments, GameTimeSegmentDefinition current, GameTimeSegmentDefinition next)
    {
        return string.Equals(next.Id, orderedSegments[0].Id, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(current.Id, orderedSegments[0].Id, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildTimeLogLine(GameProjectData project, SaveGame save, GameTimeSegmentDefinition segment)
    {
        var dayLabel = string.IsNullOrWhiteSpace(project.WorldState.Time.DayLabel) ? "День" : project.WorldState.Time.DayLabel;
        return dayLabel + " " + Math.Max(1, save.WorldState.DayNumber) + ", " + DisplayName(segment.Name, segment.Id);
    }

    private void SetTimeSegment(GameProjectData project, SaveGame save, string segmentId)
    {
        ChangeTimeSegmentWithResult(project, save, segmentId);
    }

    private GameRuntimeOperationResult ChangeTimeSegmentWithResult(GameProjectData project, SaveGame save, string segmentId, string sourceId = "")
    {
        var segment = project.WorldState.Time.Segments.FirstOrDefault(x => string.Equals(x.Id, segmentId, StringComparison.OrdinalIgnoreCase));
        if (segment == null)
        {
            return OperationFailure("Сегмент времени не найден: " + segmentId);
        }

        var effects = ResolveEffectsForExecution(project, save, segment.OnEnterEffects);
        if (!effects.Success)
        {
            var error = "Ошибка смены сегмента времени: " + effects.Message;
            AddRuntimeLog(save, error);
            return OperationFailure(error);
        }

        var validation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!validation.Success)
        {
            var error = "Ошибка смены сегмента времени: " + validation.Message;
            AddRuntimeLog(save, error);
            return OperationFailure(error);
        }

        save.WorldState.TimeSegmentId = segment.Id;
        ApplyResolvedEffects(project, save, effects.Value);
        var line = "Наступает: " + DisplayName(segment.Name, segment.Id);
        AddRuntimeLog(save, line);
        return OperationSuccess(line);
    }

    private void SetWorldAspectState(GameProjectData project, SaveGame save, string aspectId, string stateId)
    {
        ChangeWorldAspectStateWithResult(project, save, aspectId, stateId);
    }

    private GameRuntimeOperationResult ChangeWorldAspectStateWithResult(GameProjectData project, SaveGame save, string aspectId, string stateId, string sourceId = "")
    {
        var aspect = project.WorldState.Aspects.FirstOrDefault(x => string.Equals(x.Id, aspectId, StringComparison.OrdinalIgnoreCase));
        var state = aspect?.States.FirstOrDefault(x => string.Equals(x.Id, stateId, StringComparison.OrdinalIgnoreCase));
        if (aspect == null || state == null)
        {
            return OperationFailure("Состояние мира не найдено: " + aspectId + "/" + stateId);
        }

        var effects = ResolveEffectsForExecution(project, save, state.OnEnterEffects);
        if (!effects.Success)
        {
            var error = "Ошибка изменения состояния мира: " + effects.Message;
            AddRuntimeLog(save, error);
            return OperationFailure(error);
        }

        var validation = ValidateResolvedEffectsBeforeMutation(project, save, effects.Value);
        if (!validation.Success)
        {
            var error = "Ошибка изменения состояния мира: " + validation.Message;
            AddRuntimeLog(save, error);
            return OperationFailure(error);
        }

        save.WorldState.AspectStates[aspect.Id] = state.Id;
        ApplyResolvedEffects(project, save, effects.Value);
        var line = DisplayName(aspect.Name, aspect.Id) + ": " + DisplayName(state.Name, state.Id);
        AddRuntimeLog(save, line);
        return OperationSuccess(line);
    }
    private void RunWorldTriggersIntoLog(GameProjectData project, SaveGame save, string trigger)
    {
        RunWorldRules(project, save, trigger);
        TryRollAmbientEvent(project, save, trigger);
    }

    private static bool TriggerEquals(string actual, string expected)
    {
        return string.Equals(NormalizeTrigger(actual), NormalizeTrigger(expected), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTrigger(string trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger))
        {
            return "turnEnd";
        }

        return trigger.Equals("actionEnd", StringComparison.OrdinalIgnoreCase) ? "action" : trigger;
    }

    private static bool AmbientEventMatchesLocation(GameProjectData project, SaveGame save, GameAmbientEventDefinition ambientEvent)
    {
        if (ambientEvent.LocationIds.Count > 0 && !ambientEvent.LocationIds.Contains(save.CurrentLocationId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }
        if (ambientEvent.LocationTags.Count == 0)
        {
            return true;
        }

        var location = project.Locations.FirstOrDefault(x => string.Equals(x.Id, save.CurrentLocationId, StringComparison.OrdinalIgnoreCase));
        return location != null && location.Tags.Any(tag => ambientEvent.LocationTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }

    private static bool AmbientEventMatchesTime(SaveGame save, GameAmbientEventDefinition ambientEvent)
    {
        return ambientEvent.TimeSegmentIds.Count == 0
            || ambientEvent.TimeSegmentIds.Contains(save.WorldState.TimeSegmentId, StringComparer.OrdinalIgnoreCase);
    }

    private static GameAmbientEventDefinition? PickWeighted(IReadOnlyList<GameAmbientEventDefinition> candidates)
    {
        var total = candidates.Sum(x => Math.Max(0, x.Weight));
        if (total <= 0)
        {
            return null;
        }

        var roll = Random.Shared.Next(1, total + 1);
        var cumulative = 0;
        foreach (var candidate in candidates)
        {
            cumulative += Math.Max(0, candidate.Weight);
            if (roll <= cumulative)
            {
                return candidate;
            }
        }

        return candidates.LastOrDefault();
    }

    private static GameRuntimeOperationResult OperationFailure(string message)
    {
        return new GameRuntimeOperationResult { Success = false, Message = message };
    }

    private static GameRuntimeOperationResult OperationSuccess(string message)
    {
        return OperationSuccess(message, new List<string>(), new List<string>());
    }

    private static GameRuntimeOperationResult OperationSuccess(string message, List<string> logLines, List<string> appliedEffectSummaries)
    {
        return new GameRuntimeOperationResult
        {
            Success = true,
            Message = message,
            LogLines = logLines,
            AppliedEffectSummaries = appliedEffectSummaries
        };
    }

    private ResolutionResult<List<ResolvedGameCost>> ResolveCostsForExecution(GameProjectData project, SaveGame save, IEnumerable<GameCost> costs)
    {
        var resolved = new List<ResolvedGameCost>();
        foreach (var cost in costs)
        {
            var amountResult = GetCostAmountDetailed(project, save, cost);
            if (!amountResult.Success)
            {
                return new ResolutionResult<List<ResolvedGameCost>> { Success = false, Message = amountResult.Message, Value = resolved };
            }
            if (amountResult.Value < 0)
            {
                return new ResolutionResult<List<ResolvedGameCost>> { Success = false, Message = "Некорректная отрицательная стоимость: " + DescribeCost(cost, amountResult.Value), Value = resolved };
            }

            resolved.Add(new ResolvedGameCost { Source = cost, ResolvedAmount = amountResult.Value });
        }

        return new ResolutionResult<List<ResolvedGameCost>> { Success = true, Message = "OK", Value = resolved };
    }

    private GameFormulaEvaluationResult CanPayResolvedCosts(GameProjectData project, SaveGame save, IEnumerable<ResolvedGameCost> costs)
    {
        foreach (var resolved in costs)
        {
            var cost = resolved.Source;
            var amount = resolved.ResolvedAmount;
            var type = cost.Type.ToLowerInvariant();
            var current = type switch
            {
                "stat" or "resource" => save.PlayerStats.GetValueOrDefault(cost.TargetId),
                "currency" => save.Currencies.GetValueOrDefault(cost.TargetId),
                "item" => GetItemQuantity(project, save, cost.TargetId),
                "variable" => save.Variables.GetValueOrDefault(cost.TargetId),
                "cooldown" => amount,
                _ => int.MinValue
            };

            if (current == int.MinValue)
            {
                return new GameFormulaEvaluationResult { Success = false, Message = "Неизвестный тип стоимости: " + cost.Type };
            }
            if (current < amount)
            {
                return new GameFormulaEvaluationResult { Success = false, Message = "Недостаточно: " + DescribeCost(cost, amount) + $" (есть {current})." };
            }
        }

        return new GameFormulaEvaluationResult { Success = true, Message = "OK" };
    }

    private ResolutionResult<List<ResolvedGameEffect>> ResolveEffectsForExecution(GameProjectData project, SaveGame save, IEnumerable<GameEffect> effects)
    {
        return ResolveEffectsForExecution(project, save, effects, null, null);
    }

    private ResolutionResult<List<ResolvedGameEffect>> ResolveEffectsForExecution(GameProjectData project, SaveGame save, IEnumerable<GameEffect> effects, GameRuntimeCombatant? actor, GameRuntimeCombatant? target)
    {
        var resolved = new List<ResolvedGameEffect>();
        foreach (var effect in effects)
        {
            var amountResult = GetEffectAmountDetailed(project, save, effect, actor, target);
            if (!amountResult.Success)
            {
                return new ResolutionResult<List<ResolvedGameEffect>> { Success = false, Message = amountResult.Message, Value = resolved };
            }

            resolved.Add(new ResolvedGameEffect
            {
                Source = effect,
                ResolvedAmount = amountResult.Value,
                ShouldApply = RollChance(effect.ChancePercent),
                ChanceRolled = effect.ChancePercent is > 0 and < 100
            });
        }

        return new ResolutionResult<List<ResolvedGameEffect>> { Success = true, Message = "OK", Value = resolved };
    }

    private GameRuntimeOperationResult ValidateResolvedEffectsBeforeMutation(GameProjectData project, SaveGame save, IReadOnlyList<ResolvedGameEffect> effects)
    {
        var appliedEffects = effects.Where(x => x.ShouldApply).ToList();
        if (appliedEffects.Count == 0)
        {
            return OperationSuccess("OK");
        }

        var formulaValidation = ValidateEffectFormulas(project, save, appliedEffects.Select(x => x.Source));
        if (!formulaValidation.Success)
        {
            return OperationFailure(formulaValidation.Message);
        }

        var probeSave = CloneSaveForFormula(save);
        foreach (var effect in appliedEffects)
        {
            var type = effect.Source.Type.ToLowerInvariant();
            GameRuntimeOperationResult? result = type switch
            {
                "experience" or "playerexperience" => AddPlayerExperienceWithResult(project, probeSave, effect.ResolvedAmount, effect.Source.SourceId),
                "skillexperience" => AddSkillExperienceWithResult(project, probeSave, effect.Source.TargetId, effect.ResolvedAmount, effect.Source.SourceId),
                "advancetime" => ValidateAdvanceTimeEffect(project, probeSave, effect),
                "timesegment" => ValidateTimeSegmentEffect(project, probeSave, effect.Source),
                "worldstate" or "worldaspect" => ValidateWorldAspectEffect(project, probeSave, effect.Source),
                _ => null
            };

            if (result == null)
            {
                ApplyEffect(project, probeSave, effect.Source, effect.ResolvedAmount);
                continue;
            }

            if (!result.Success)
            {
                if (type == "skillexperience")
                {
                    return OperationFailure($"Ошибка применения опыта навыка {effect.Source.TargetId}: {result.Message}");
                }
                if (type is "experience" or "playerexperience")
                {
                    return OperationFailure("Ошибка применения опыта игрока: " + result.Message);
                }

                return result;
            }
        }

        return OperationSuccess("OK");
    }

    private ResolutionResult<List<List<ResolvedGameEffect>>> ResolveLevelUpEffectsBeforeExperienceMutation(GameProjectData project, SaveGame save, IReadOnlyList<GameEffect> effects, int levelUps, Action<SaveGame> prepareProbe, string messagePrefix)
    {
        var result = new List<List<ResolvedGameEffect>>();
        if (levelUps <= 0 || effects.Count == 0)
        {
            return new ResolutionResult<List<List<ResolvedGameEffect>>>
            {
                Success = true,
                Message = "OK",
                Value = result
            };
        }

        var probeSave = CloneSaveForFormula(save);
        prepareProbe(probeSave);
        for (var i = 0; i < levelUps; i++)
        {
            var resolved = ResolveEffectsForExecution(project, probeSave, effects);
            if (!resolved.Success)
            {
                return new ResolutionResult<List<List<ResolvedGameEffect>>>
                {
                    Success = false,
                    Message = messagePrefix + resolved.Message,
                    Value = result
                };
            }

            var validation = ValidateResolvedEffectsBeforeMutation(project, probeSave, resolved.Value);
            if (!validation.Success)
            {
                return new ResolutionResult<List<List<ResolvedGameEffect>>>
                {
                    Success = false,
                    Message = messagePrefix + validation.Message,
                    Value = result
                };
            }

            result.Add(resolved.Value);
            ApplyResolvedEffects(project, probeSave, resolved.Value);
        }

        return new ResolutionResult<List<List<ResolvedGameEffect>>>
        {
            Success = true,
            Message = "OK",
            Value = result
        };
    }

    private void PayResolvedCosts(GameProjectData project, SaveGame save, IEnumerable<ResolvedGameCost> costs)
    {
        foreach (var resolved in costs)
        {
            var cost = resolved.Source;
            var amount = resolved.ResolvedAmount;
            switch (cost.Type.ToLowerInvariant())
            {
                case "stat":
                case "resource":
                    save.PlayerStats[cost.TargetId] = save.PlayerStats.GetValueOrDefault(cost.TargetId) - amount;
                    break;
                case "currency":
                    save.Currencies[cost.TargetId] = save.Currencies.GetValueOrDefault(cost.TargetId) - amount;
                    break;
                case "item":
                    RemoveItem(project, save, cost.TargetId, amount);
                    break;
                case "variable":
                    save.Variables[cost.TargetId] = save.Variables.GetValueOrDefault(cost.TargetId) - amount;
                    break;
            }
        }
    }

    private void ApplyResolvedEffects(GameProjectData project, SaveGame save, IEnumerable<ResolvedGameEffect> effects)
    {
        foreach (var effect in effects.Where(x => x.ShouldApply))
        {
            ApplyEffect(project, save, effect.Source, effect.ResolvedAmount);
        }
    }

    private bool CanUnlockNodeSkill(GameProjectData project, SaveGame save, GameProgressionNodeDefinition node)
    {
        if (string.IsNullOrWhiteSpace(node.SkillId))
        {
            return true;
        }

        var skill = project.Skills.FirstOrDefault(x => string.Equals(x.Id, node.SkillId, StringComparison.OrdinalIgnoreCase));
        return skill != null
            && save.KnownSkills.All(x => !string.Equals(x.SkillId, node.SkillId, StringComparison.OrdinalIgnoreCase))
            && skill.LearnRequirements.All(req => CheckRequirement(project, save, req));
    }

    private GameFormulaEvaluationResult GetCostAmountDetailed(GameProjectData project, SaveGame save, GameCost cost)
    {
        var formula = !string.IsNullOrWhiteSpace(cost.FormulaId) ? cost.FormulaId : cost.FormulaExpression;
        if (string.IsNullOrWhiteSpace(formula))
        {
            return new GameFormulaEvaluationResult { Success = true, Value = cost.Amount, Message = "OK" };
        }

        var result = TryEvaluateFormula(project, save, formula);
        if (!result.Success)
        {
            result.Message = "Ошибка формулы стоимости '" + cost.TargetId + "': " + result.Message;
        }

        return result;
    }

    private GameFormulaEvaluationResult ValidateEffectFormulas(GameProjectData project, SaveGame save, IEnumerable<GameEffect> effects, HashSet<string>? validatingStatusEffects = null)
    {
        foreach (var effect in effects)
        {
            var amountResult = GetEffectAmountDetailed(project, save, effect);
            if (!amountResult.Success)
            {
                return amountResult;
            }

            var type = effect.Type.ToLowerInvariant();
            if (type is not ("status" or "statuseffect"))
            {
                continue;
            }

            var statusId = !string.IsNullOrWhiteSpace(effect.StatusEffectId) ? effect.StatusEffectId : effect.TargetId;
            if (string.IsNullOrWhiteSpace(statusId))
            {
                continue;
            }

            validatingStatusEffects ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!validatingStatusEffects.Add(statusId))
            {
                continue;
            }

            var definition = project.StatusEffects.FirstOrDefault(x => string.Equals(x.Id, statusId, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                continue;
            }

            var mode = string.IsNullOrWhiteSpace(effect.Mode) ? "add" : effect.Mode.ToLowerInvariant();
            var nestedEffects = mode is "remove" or "removestatus" or "statusremove"
                ? definition.OnExpireEffects
                : definition.OnApplyEffects;
            var nestedValidation = ValidateEffectFormulas(project, save, nestedEffects, validatingStatusEffects);
            if (!nestedValidation.Success)
            {
                return nestedValidation;
            }
        }

        return new GameFormulaEvaluationResult { Success = true, Message = "OK" };
    }

    private GameFormulaEvaluationResult GetEffectAmountDetailed(GameProjectData project, SaveGame save, GameEffect effect)
    {
        return GetEffectAmountDetailed(project, save, effect, null, null);
    }

    private GameFormulaEvaluationResult GetEffectAmountDetailed(GameProjectData project, SaveGame save, GameEffect effect, GameRuntimeCombatant? actor, GameRuntimeCombatant? target)
    {
        var formula = !string.IsNullOrWhiteSpace(effect.FormulaId) ? effect.FormulaId : effect.FormulaExpression;
        if (string.IsNullOrWhiteSpace(formula))
        {
            return new GameFormulaEvaluationResult { Success = true, Value = effect.Amount, Message = "OK" };
        }

        var result = TryEvaluateFormula(project, save, formula, actor, target);
        if (!result.Success)
        {
            var targetLabel = !string.IsNullOrWhiteSpace(effect.StatusEffectId) ? effect.StatusEffectId : effect.TargetId;
            if (string.IsNullOrWhiteSpace(targetLabel))
            {
                targetLabel = effect.Type;
            }

            result.Message = "Ошибка формулы эффекта '" + targetLabel + "/" + effect.Type + "': " + result.Message;
        }

        return result;
    }

    private static string DescribeRequirement(GameRequirement requirement, int current)
    {
        var text = string.IsNullOrWhiteSpace(requirement.Text)
            ? $"{requirement.Type}:{requirement.TargetId} {requirement.Operator} {requirement.Value}"
            : requirement.Text;
        return $"{text} (сейчас {current}).";
    }

    private static string DescribeRequirements(IEnumerable<GameRequirement> requirements)
    {
        var list = requirements.Select(x => string.IsNullOrWhiteSpace(x.Text) ? $"{x.Type}:{x.TargetId} {x.Operator} {x.Value}" : x.Text).ToList();
        return list.Count == 0 ? "-" : string.Join("; ", list);
    }

    private static string DescribeCosts(IEnumerable<GameCost> costs)
    {
        var list = costs.Select(DescribeCostPreview).ToList();
        return list.Count == 0 ? "-" : string.Join("; ", list);
    }

    private static string DescribeCostPreview(GameCost cost)
    {
        if (!string.IsNullOrWhiteSpace(cost.FormulaId))
        {
            return $"{cost.Type}:{cost.TargetId} formula:{cost.FormulaId}";
        }
        if (!string.IsNullOrWhiteSpace(cost.FormulaExpression))
        {
            return $"{cost.Type}:{cost.TargetId} formula:{cost.FormulaExpression}";
        }

        return $"{cost.Type}:{cost.TargetId} x{cost.Amount}";
    }

    private static string DescribeCost(GameCost cost, int amount)
    {
        return $"{cost.Type}:{cost.TargetId} x{amount}";
    }

    private static string DescribeEffect(GameEffect effect)
    {
        return DescribeEffect(effect, effect.Amount);
    }

    private static string DescribeEffect(GameEffect effect, int amount)
    {
        var target = !string.IsNullOrWhiteSpace(effect.StatusEffectId) ? effect.StatusEffectId : effect.TargetId;
        return $"{effect.Type}:{target} {amount}";
    }

    private int EvaluateExpression(GameProjectData project, SaveGame save, string expression)
    {
        var evaluator = new GameFormulaEvaluator(project, save, expression, () => GetEffectiveStats(project, save));
        return evaluator.Evaluate();
    }

    private GameFormulaEvaluationResult TryEvaluateFormula(GameProjectData project, SaveGame save, string formulaIdOrExpression, GameRuntimeCombatant? actor, GameRuntimeCombatant? target)
    {
        if (string.IsNullOrWhiteSpace(formulaIdOrExpression))
        {
            return new GameFormulaEvaluationResult { Success = false, Message = "Формула пустая." };
        }

        var formula = project.Formulas.FirstOrDefault(x => string.Equals(x.Id, formulaIdOrExpression, StringComparison.OrdinalIgnoreCase));
        return formula == null
            ? TryEvaluateExpression(project, save, formulaIdOrExpression, actor, target)
            : TryEvaluateExpression(project, save, formula.Expression, actor, target);
    }

    private GameFormulaEvaluationResult TryEvaluateExpression(GameProjectData project, SaveGame save, string expression)
    {
        return TryEvaluateExpression(project, save, expression, null, null);
    }

    private GameFormulaEvaluationResult TryEvaluateExpression(GameProjectData project, SaveGame save, string expression, GameRuntimeCombatant? actor, GameRuntimeCombatant? target)
    {
        var evaluator = new GameFormulaEvaluator(project, save, expression, () => GetEffectiveStats(project, save), actor?.Stats, target?.Stats);
        return evaluator.TryEvaluate();
    }

    private void ApplyStatusEffect(GameProjectData project, SaveGame save, GameEffect effect, string mode)
    {
        var statusId = !string.IsNullOrWhiteSpace(effect.StatusEffectId) ? effect.StatusEffectId : effect.TargetId;
        if (string.IsNullOrWhiteSpace(statusId))
        {
            return;
        }

        if (mode is "remove" or "removestatus" or "statusremove")
        {
            RemoveStatusEffect(project, save, statusId);
            return;
        }

        var definition = project.StatusEffects.FirstOrDefault(x => string.Equals(x.Id, statusId, StringComparison.OrdinalIgnoreCase));
        if (definition == null)
        {
            return;
        }

        var duration = effect.DurationTurns > 0 ? effect.DurationTurns : definition.DefaultDurationTurns;
        var existing = save.ActiveStatusEffects.FirstOrDefault(x => string.Equals(x.StatusEffectId, statusId, StringComparison.OrdinalIgnoreCase));
        var stackMode = mode is "refresh" or "replace"
            ? mode
            : string.IsNullOrWhiteSpace(definition.StackMode) ? "refresh" : definition.StackMode.ToLowerInvariant();
        var applyOnApplyEffects = false;

        if (existing == null)
        {
            existing = new GameActiveStatusEffect
            {
                InstanceId = Ids.New("status"),
                StatusEffectId = statusId,
                SourceId = effect.SourceId,
                RemainingTurns = duration,
                Stacks = 1
            };
            save.ActiveStatusEffects.Add(existing);
            applyOnApplyEffects = true;
        }
        else if (stackMode == "ignore")
        {
            return;
        }
        else if (stackMode == "stack")
        {
            existing.Stacks = Math.Min(Math.Max(1, definition.MaxStacks), existing.Stacks + 1);
            existing.RemainingTurns = duration;
        }
        else if (stackMode == "replace")
        {
            existing.SourceId = effect.SourceId;
            existing.RemainingTurns = duration;
            existing.Stacks = 1;
            applyOnApplyEffects = true;
        }
        else
        {
            existing.RemainingTurns = duration;
        }

        if (applyOnApplyEffects)
        {
            ApplyEffects(project, save, definition.OnApplyEffects);
        }
    }

    private void RemoveStatusEffect(GameProjectData project, SaveGame save, string statusId)
    {
        var active = save.ActiveStatusEffects
            .Where(x => string.Equals(x.StatusEffectId, statusId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var status in active)
        {
            var definition = project.StatusEffects.FirstOrDefault(x => string.Equals(x.Id, status.StatusEffectId, StringComparison.OrdinalIgnoreCase));
            if (definition != null)
            {
                ApplyEffects(project, save, definition.OnExpireEffects);
            }
            save.ActiveStatusEffects.Remove(status);
        }
    }

    private void TickStatusEffects(GameProjectData project, SaveGame save)
    {
        TickStatusEffects(project, save, null);
    }

    private void TickStatusEffects(GameProjectData project, SaveGame save, GameTurnResult? result)
    {
        var snapshot = save.ActiveStatusEffects.ToList();
        foreach (var status in snapshot)
        {
            var current = save.ActiveStatusEffects.FirstOrDefault(x => string.Equals(x.InstanceId, status.InstanceId, StringComparison.OrdinalIgnoreCase));
            if (current == null)
            {
                continue;
            }

            var definition = project.StatusEffects.FirstOrDefault(x => string.Equals(x.Id, current.StatusEffectId, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                continue;
            }

            var beforeLogCount = save.EventLog.Count;
            ApplyEffects(project, save, definition.PeriodicEffects);
            if (definition.PeriodicEffects.Count > 0)
            {
                var message = "Периодический эффект статуса: " + DisplayName(definition.Name, definition.Id);
                result?.PeriodicEffectMessages.Add(message);
                result?.LogLines.Add(message);
                foreach (var line in save.EventLog.Skip(beforeLogCount))
                {
                    result?.PeriodicEffectMessages.Add(line);
                }
            }
            current = save.ActiveStatusEffects.FirstOrDefault(x => string.Equals(x.InstanceId, status.InstanceId, StringComparison.OrdinalIgnoreCase));
            if (current == null)
            {
                continue;
            }

            var beforeRemainingTurns = current.RemainingTurns;
            if (current.RemainingTurns > 0)
            {
                current.RemainingTurns--;
            }

            if (beforeRemainingTurns > 0 && current.RemainingTurns == 0)
            {
                ApplyEffects(project, save, definition.OnExpireEffects);
                save.ActiveStatusEffects.Remove(current);
                var message = "Истёк статус: " + DisplayName(definition.Name, definition.Id);
                result?.ExpiredStatusEffects.Add(definition.Id);
                result?.LogLines.Add(message);
            }
        }
    }

    private static void TickActionCooldowns(SaveGame save)
    {
        TickActionCooldowns(save, null);
    }

    private static void TickActionCooldowns(SaveGame save, GameTurnResult? result)
    {
        foreach (var actionId in save.ActionCooldowns.Keys.ToList())
        {
            var before = save.ActionCooldowns[actionId];
            save.ActionCooldowns[actionId] = Math.Max(0, save.ActionCooldowns[actionId] - 1);
            if (before != save.ActionCooldowns[actionId])
            {
                result?.CooldownChanges.Add($"Действие {actionId}: {before} -> {save.ActionCooldowns[actionId]}");
            }
        }
    }

    private static bool RollChance(int chancePercent)
    {
        if (chancePercent <= 0)
        {
            return false;
        }
        if (chancePercent >= 100)
        {
            return true;
        }

        return Random.Shared.Next(1, 101) <= chancePercent;
    }

    private static void UnlockProgressionByEffect(GameProjectData project, SaveGame save, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId)
            || save.UnlockedProgressionNodeIds.Contains(nodeId, StringComparer.OrdinalIgnoreCase)
            || project.ProgressionNodes.All(x => !string.Equals(x.Id, nodeId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        save.UnlockedProgressionNodeIds.Add(nodeId);
    }

    private static int ApplyNumeric(int current, int amount, string mode)
    {
        return mode.ToLowerInvariant() switch
        {
            "set" => amount,
            "remove" => Math.Max(0, current - Math.Abs(amount)),
            "multiplypercent" => current + current * amount / 100,
            _ => current + amount
        };
    }

    private static bool Compare(int current, string op, int expected)
    {
        return op switch
        {
            ">" => current > expected,
            ">=" => current >= expected,
            "<" => current < expected,
            "<=" => current <= expected,
            "==" => current == expected,
            "!=" => current != expected,
            _ => current >= expected
        };
    }

    private static GameRequirement ToRequirement(GameCondition condition)
    {
        return new GameRequirement
        {
            Type = condition.Type,
            TargetId = condition.TargetId,
            Operator = condition.Operator,
            Value = condition.Value,
            Text = condition.Text
        };
    }

    private static GameInventoryEntry CreateInventoryEntry(GameItemDefinition item, int quantity)
    {
        return new GameInventoryEntry
        {
            InstanceId = Ids.New("item"),
            ItemId = item.Id,
            Quantity = quantity,
            Durability = item.DurabilityMax
        };
    }

    private static void DiscoverLocation(SaveGame save, string locationId)
    {
        if (!save.DiscoveredLocationIds.Contains(locationId, StringComparer.OrdinalIgnoreCase))
        {
            save.DiscoveredLocationIds.Add(locationId);
        }
    }

    private static int GetItemQuantity(GameProjectData project, SaveGame save, string itemId)
    {
        EnsureInventoryEntries(project, save);
        return save.InventoryEntries.Where(x => string.Equals(x.ItemId, itemId, StringComparison.OrdinalIgnoreCase)).Sum(x => x.Quantity);
    }

    private static void EnsureInventoryEntries(GameProjectData project, SaveGame save)
    {
        if (save.InventoryEntries.Count > 0)
        {
            SyncLegacyInventory(save);
            return;
        }

        foreach (var pair in save.Inventory.Where(x => x.Value > 0))
        {
            var item = project.Items.FirstOrDefault(x => string.Equals(x.Id, pair.Key, StringComparison.OrdinalIgnoreCase))
                ?? new GameItemDefinition { Id = pair.Key, IsStackable = true };
            save.InventoryEntries.Add(CreateInventoryEntry(item, pair.Value));
        }

        SyncLegacyInventory(save);
    }

    private static void SyncLegacyInventory(SaveGame save)
    {
        save.Inventory = save.InventoryEntries
            .Where(x => x.Quantity > 0)
            .GroupBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Sum(e => e.Quantity), StringComparer.OrdinalIgnoreCase);
    }

    private static void EnsureKnownSkills(GameProjectData project, SaveGame save)
    {
        foreach (var node in project.ProgressionNodes.Where(x => x.IsUnlockedByDefault))
        {
            if (!save.UnlockedProgressionNodeIds.Contains(node.Id, StringComparer.OrdinalIgnoreCase))
            {
                save.UnlockedProgressionNodeIds.Add(node.Id);
            }
        }

        foreach (var skill in project.Skills.Where(x => x.IsKnownByDefault || x.InitialLevel > 0))
        {
            if (!save.KnownSkills.Any(x => string.Equals(x.SkillId, skill.Id, StringComparison.OrdinalIgnoreCase)))
            {
                save.KnownSkills.Add(new GameKnownSkill { SkillId = skill.Id, Level = Math.Max(1, skill.InitialLevel), IsEnabled = true });
            }
        }

        foreach (var node in project.ProgressionNodes.Where(x => x.IsUnlockedByDefault && !string.IsNullOrWhiteSpace(x.SkillId)))
        {
            if (!save.KnownSkills.Any(x => string.Equals(x.SkillId, node.SkillId, StringComparison.OrdinalIgnoreCase)))
            {
                var skill = project.Skills.FirstOrDefault(x => string.Equals(x.Id, node.SkillId, StringComparison.OrdinalIgnoreCase));
                if (skill != null)
                {
                    save.KnownSkills.Add(new GameKnownSkill { SkillId = skill.Id, Level = Math.Max(1, skill.InitialLevel), IsEnabled = true });
                }
            }
        }
    }

    private static string DisplayName(string name, string id)
    {
        return string.IsNullOrWhiteSpace(name) ? id : name;
    }
}
