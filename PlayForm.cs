using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Runtime;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder;

internal partial class PlayForm : Form
{
    private readonly GameProjectData _project;
    private readonly SaveGame _save;
    private readonly GameRuntimeEngine _runtimeEngine;
    private readonly GameStorageService _storageService;

    public PlayForm(GameProjectData project, SaveGame save, GameRuntimeEngine runtimeEngine, GameStorageService storageService)
    {
        _project = project;
        _save = save;
        _runtimeEngine = runtimeEngine;
        _storageService = storageService;
        InitializeComponent();
        RefreshPlayView();
    }

    private async void btnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            await _storageService.SaveProgressAsync(_project, _save, "autosave.json");
            AddLog("Прогресс сохранён.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnLoad_Click(object? sender, EventArgs e)
    {
        AddLog("Для загрузки другого сохранения используйте вкладку сохранений в главном окне.");
    }

    private void btnInventory_Click(object? sender, EventArgs e)
    {
        SelectTab(tabInventory);
    }

    private void btnCharacter_Click(object? sender, EventArgs e)
    {
        SelectTab(tabStats);
    }

    private void btnMap_Click(object? sender, EventArgs e)
    {
        SelectTab(tabMap);
    }

    private async void btnEndTurn_Click(object? sender, EventArgs e)
    {
        var result = _runtimeEngine.EndTurnWithResult(_project, _save);
        AddLog("Ход завершён. Новый ход: " + result.NewTurnNumber);
        foreach (var line in result.CooldownChanges)
        {
            AddLog(line);
        }
        await SaveAutosaveProgressAsync();
        RefreshPlayView();
    }
    private void btnClosePlay_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void RefreshPlayView()
    {
        var scene = _runtimeEngine.GetCurrentScene(_project, _save);
        lblTitle.Text = _project.Meta.Title;
        lblStatus.Text = BuildStatusText(scene);
        txtWorldStateSummary.Text = string.Join(" | ", _runtimeEngine.GetWorldStateSummary(_project, _save));
        txtWorldStateSummary.Visible = !string.IsNullOrWhiteSpace(txtWorldStateSummary.Text);
        sceneLayout.RowStyles[1].Height = txtWorldStateSummary.Visible ? 32F : 0F;
        lblSceneTitle.Text = scene.Title;
        txtSceneText.Text = scene.Text;
        LoadSceneImage(scene);
        RefreshChoices();
        RefreshInfoTabs();
    }

    private void RefreshChoices()
    {
        pnlChoices.Controls.Clear();
        if (_save.Combat.IsActive)
        {
            var button = new Button
            {
                Text = "Идёт бой. Выберите действие и цель на вкладке 'Бой' или нажмите 'Конец хода'.",
                Width = 720,
                Height = 40,
                Enabled = false
            };
            pnlChoices.Controls.Add(button);
            SelectTab(tabCombat);
            return;
        }

        foreach (var choice in _runtimeEngine.GetAvailableChoices(_project, _save))
        {
            var button = new Button
            {
                Text = choice.Text,
                Width = 560,
                Height = 40,
                Tag = choice.Id
            };
            button.Click += Choice_Click;
            pnlChoices.Controls.Add(button);
        }
    }

    private void RefreshInfoTabs()
    {
        FillList(lvStats, BuildCharacterRows());
        FillList(lvCurrencies, _save.Currencies.Select(x => (x.Key, FindCurrencyName(x.Key), x.Value.ToString())));
        FillList(lvInventory, _runtimeEngine.GetInventory(_project, _save).Select(x => (x.InstanceId, FindItemName(x.ItemId), x.IsEquipped ? "надето" : x.Quantity.ToString())), ("empty", "Инвентарь пуст", "Нет предметов"));
        FillList(lvEquipment, _save.EquippedItems.Select(x => (x.Key, FindSlotName(x.Key), x.Value)));
        FillList(lvSkills, _save.KnownSkills.Select(x => (x.SkillId, FindSkillName(x.SkillId), BuildSkillStateText(x))));
        FillList(lvRelationships, _save.Relationships.Select(x => (x.Key, FindRelationshipName(x.Key), x.Value.ToString())), ("empty", "Нет отношений", "Нет данных"));
        FillList(lvQuests, _save.ActiveQuestIds.Select(x => (x, FindQuestName(x), "активен")), ("empty", "Активных заданий нет", "Нет данных"));
        FillList(lvMap, _runtimeEngine.GetAvailableLocations(_project, _save).Select(x => (x.Id, x.Name, x.Description)));
        FillActions();
        RefreshCombatTab();
        FillEffects();
        FillProgression();
        RefreshInventoryActionButton();
        RefreshSkillActionButton();
        btnTravelToLocation.Enabled = lvMap.SelectedItems.Count > 0;
        txtLog.Text = string.Join(Environment.NewLine, _save.EventLog);

        var showTurns = _project.Mechanics.EnableTurns || _save.ActiveStatusEffects.Count > 0 || _save.ActionCooldowns.Any(x => x.Value > 0) || _save.KnownSkills.Any(x => x.CooldownRemaining > 0) || _project.Stats.Any(x => x.RegenPerTurn.HasValue);
        btnEndTurn.Visible = showTurns;
        btnEndTurn.Enabled = showTurns;
        SetTabVisible(tabCurrencies, _project.Currencies.Count > 0 || _save.Currencies.Count > 0);
        SetTabVisible(tabInventory, _project.Items.Count > 0 || _save.Inventory.Count > 0 || _save.InventoryEntries.Count > 0);
        SetTabVisible(tabEquipment, _project.EquipmentSlots.Count > 0);
        SetTabVisible(tabSkills, _project.Skills.Count > 0 || _save.KnownSkills.Count > 0);
        SetTabVisible(tabRelationships, _project.Relationships.Count > 0 || _save.Relationships.Count > 0);
        SetTabVisible(tabQuests, _project.Quests.Count > 0 || _save.ActiveQuestIds.Count > 0);
        SetTabVisible(tabMap, _project.LocationConnections.Count > 0 || _project.Locations.Count > 0);
        SetTabVisible(tabActions, _project.Actions.Count > 0 || _project.Mechanics.EnableActionPanel);
        SetTabVisible(tabCombat, _save.Combat.IsActive || _project.Encounters.Any(x => x.Combatants.Count > 0) || _project.Actions.Any(x => x.AvailableInCombat));
        SetTabVisible(tabEffects, _project.StatusEffects.Count > 0 || _save.ActiveStatusEffects.Count > 0);
        SetTabVisible(tabProgression, _project.ProgressionNodes.Count > 0 || _project.Mechanics.EnableProgression);
    }

    private async void Choice_Click(object? sender, EventArgs e)
    {
        if (sender is Button { Tag: string choiceId })
        {
            if (_save.Combat.IsActive)
            {
                AddLog("Сейчас идёт бой. Используйте вкладку 'Бой'.");
                RefreshPlayView();
                return;
            }

            var result = _runtimeEngine.ApplyChoiceWithResult(_project, _save, choiceId);
            AddOperationLog(result);
            if (result.Success)
            {
                await SaveAutosaveProgressAsync();
            }
            RefreshPlayView();
        }
    }

    private void lvInventory_DoubleClick(object? sender, EventArgs e)
    {
        UseSelectedInventoryItem();
    }

    private void btnUseInventoryItem_Click(object? sender, EventArgs e)
    {
        UseSelectedInventoryItem();
    }

    private void lvInventory_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshInventoryActionButton();
    }

    private void RefreshInventoryActionButton()
    {
        if (lvInventory.SelectedItems.Count == 0)
        {
            btnUseInventoryItem.Text = "Выберите предмет";
            btnUseInventoryItem.Enabled = false;
            return;
        }

        var instanceId = lvInventory.SelectedItems[0].Text;
        var entry = _save.InventoryEntries.FirstOrDefault(x => string.Equals(x.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        var item = entry == null ? null : _project.Items.FirstOrDefault(x => string.Equals(x.Id, entry.ItemId, StringComparison.OrdinalIgnoreCase));
        if (entry == null || item == null)
        {
            btnUseInventoryItem.Text = "Предмет не найден";
            btnUseInventoryItem.Enabled = false;
            return;
        }

        if (item.IsEquippable && !entry.IsEquipped)
        {
            btnUseInventoryItem.Text = "Надеть";
            btnUseInventoryItem.Enabled = true;
            return;
        }
        if (entry.IsEquipped)
        {
            btnUseInventoryItem.Text = "Снять";
            btnUseInventoryItem.Enabled = true;
            return;
        }
        if (item.IsUsable || item.IsConsumable || item.UseEffects.Count > 0)
        {
            btnUseInventoryItem.Text = "Использовать";
            btnUseInventoryItem.Enabled = true;
            return;
        }

        btnUseInventoryItem.Text = "Нет действия";
        btnUseInventoryItem.Enabled = false;
    }

    private void UseSelectedInventoryItem()
    {
        if (lvInventory.SelectedItems.Count == 0)
        {
            return;
        }

        var instanceId = lvInventory.SelectedItems[0].Text;
        var entry = _save.InventoryEntries.FirstOrDefault(x => x.InstanceId == instanceId);
        var item = entry == null ? null : _project.Items.FirstOrDefault(x => x.Id == entry.ItemId);
        if (entry == null || item == null)
        {
            AddLog("Предмет не найден.");
            return;
        }

        GameRuntimeOperationResult result;
        if (item.IsEquippable && !entry.IsEquipped)
        {
            result = _runtimeEngine.EquipItemWithResult(_project, _save, instanceId);
        }
        else if (entry.IsEquipped)
        {
            result = _runtimeEngine.UnequipItemWithResult(_project, _save, entry.SlotId);
        }
        else
        {
            result = _runtimeEngine.UseItemWithResult(_project, _save, instanceId);
        }

        if (!result.Success)
        {
            AddLog(result.Message);
        }

        RefreshPlayView();
    }

    private void lvSkills_DoubleClick(object? sender, EventArgs e)
    {
        UseSelectedSkill();
    }

    private void btnUseSkill_Click(object? sender, EventArgs e)
    {
        UseSelectedSkill();
    }

    private void lvSkills_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshSkillActionButton();
    }

    private void RefreshSkillActionButton()
    {
        if (lvSkills.SelectedItems.Count == 0)
        {
            btnUseSkill.Text = "Выберите навык";
            btnUseSkill.Enabled = false;
            return;
        }

        var skillId = lvSkills.SelectedItems[0].Text;
        var known = _save.KnownSkills.FirstOrDefault(x => string.Equals(x.SkillId, skillId, StringComparison.OrdinalIgnoreCase));
        var skill = _project.Skills.FirstOrDefault(x => string.Equals(x.Id, skillId, StringComparison.OrdinalIgnoreCase));
        if (known == null || skill == null)
        {
            btnUseSkill.Text = "Навык недоступен";
            btnUseSkill.Enabled = false;
            return;
        }

        var isAvailable = known.IsEnabled
            && known.CooldownRemaining <= 0
            && _runtimeEngine.GetAvailableSkills(_project, _save).Any(x => string.Equals(x.SkillId, skillId, StringComparison.OrdinalIgnoreCase));
        btnUseSkill.Text = isAvailable ? "Использовать навык" : "Навык недоступен";
        btnUseSkill.Enabled = true;
    }

    private void UseSelectedSkill()
    {
        if (lvSkills.SelectedItems.Count == 0)
        {
            return;
        }

        var result = _runtimeEngine.UseSkillWithResult(_project, _save, lvSkills.SelectedItems[0].Text);
        if (!result.Success)
        {
            AddLog(result.Message);
        }
        RefreshPlayView();
    }

    private void lvMap_DoubleClick(object? sender, EventArgs e)
    {
        TravelToSelectedLocation();
    }

    private void btnTravelToLocation_Click(object? sender, EventArgs e)
    {
        TravelToSelectedLocation();
    }

    private void lvMap_SelectedIndexChanged(object? sender, EventArgs e)
    {
        btnTravelToLocation.Enabled = lvMap.SelectedItems.Count > 0;
    }

    private void TravelToSelectedLocation()
    {
        if (lvMap.SelectedItems.Count == 0)
        {
            return;
        }

        var result = _runtimeEngine.TravelToLocationWithResult(_project, _save, lvMap.SelectedItems[0].Text);
        if (!result.Success)
        {
            AddLog(result.Message);
        }
        RefreshPlayView();
    }

    private void lvActions_DoubleClick(object? sender, EventArgs e)
    {
        ExecuteSelectedAction();
    }

    private void btnExecuteAction_Click(object? sender, EventArgs e)
    {
        ExecuteSelectedAction();
    }

    private void btnRefreshActions_Click(object? sender, EventArgs e)
    {
        RefreshPlayView();
    }

    private void lvActions_SelectedIndexChanged(object? sender, EventArgs e)
    {
        btnExecuteAction.Enabled = IsSelectedActionAvailable();
    }

    private async void ExecuteSelectedAction()
    {
        if (lvActions.SelectedItems.Count == 0)
        {
            return;
        }

        var result = _runtimeEngine.ExecuteAction(_project, _save, lvActions.SelectedItems[0].Text);
        if (!result.Success)
        {
            AddLog(result.Message);
        }
        else
        {
            await SaveAutosaveProgressAsync();
        }
        RefreshPlayView();
    }

    private async void btnStartCombat_Click(object? sender, EventArgs e)
    {
        var result = _runtimeEngine.StartCurrentSceneCombatWithResult(_project, _save);
        AddOperationLog(result);
        if (result.Success)
        {
            await SaveAutosaveProgressAsync();
        }
        RefreshPlayView();
    }

    private async void btnExecuteCombatAction_Click(object? sender, EventArgs e)
    {
        if (lvCombatActions.SelectedItems.Count == 0 || lvCombatants.SelectedItems.Count == 0)
        {
            return;
        }

        var actionId = lvCombatActions.SelectedItems[0].Tag as string ?? lvCombatActions.SelectedItems[0].Text;
        var targetRuntimeId = lvCombatants.SelectedItems[0].Tag as string ?? lvCombatants.SelectedItems[0].Text;
        var result = _runtimeEngine.ExecuteCombatActionWithResult(_project, _save, actionId, targetRuntimeId);
        AddOperationLog(result);
        if (result.Success)
        {
            await SaveAutosaveProgressAsync();
        }
        RefreshPlayView();
    }

    private async void btnEndCombatTurn_Click(object? sender, EventArgs e)
    {
        var result = _runtimeEngine.EndCombatTurnWithResult(_project, _save);
        AddOperationLog(result);
        if (result.Success)
        {
            await SaveAutosaveProgressAsync();
        }
        RefreshPlayView();
    }

    private void lvCombatants_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshCombatButtons();
    }

    private void lvCombatActions_SelectedIndexChanged(object? sender, EventArgs e)
    {
        RefreshCombatButtons();
    }

    private void lvProgression_DoubleClick(object? sender, EventArgs e)
    {
        UnlockSelectedProgressionNode();
    }

    private void btnUnlockProgression_Click(object? sender, EventArgs e)
    {
        UnlockSelectedProgressionNode();
    }

    private void btnRefreshProgression_Click(object? sender, EventArgs e)
    {
        RefreshPlayView();
    }

    private void lvProgression_SelectedIndexChanged(object? sender, EventArgs e)
    {
        btnUnlockProgression.Enabled = IsSelectedProgressionAvailable();
    }

    private void UnlockSelectedProgressionNode()
    {
        if (lvProgression.SelectedItems.Count == 0)
        {
            return;
        }

        var item = lvProgression.SelectedItems[0];
        if (!string.Equals(item.SubItems[3].Text, "доступно", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(item.SubItems[3].Text, "открыто", StringComparison.OrdinalIgnoreCase))
            {
                AddLog("Узел прокачки уже открыт.");
            }
            else
            {
                AddLog("Узел прокачки пока недоступен. Проверьте требования/стоимость в таблице.");
            }
            return;
        }

        var result = _runtimeEngine.UnlockProgressionNodeWithResult(_project, _save, item.Text);
        if (!result.Success)
        {
            AddLog(result.Message);
        }
        RefreshPlayView();
    }
    private void FillActions()
    {
        lvActions.Items.Clear();
        foreach (var action in _project.Actions)
        {
            var availability = _runtimeEngine.CheckActionAvailability(_project, _save, action.Id);
            var item = new ListViewItem(action.Id);
            item.SubItems.Add(string.IsNullOrWhiteSpace(action.Name) ? action.Id : action.Name);
            item.SubItems.Add(action.Kind);
            item.SubItems.Add(availability.IsAvailable ? "да" : "нет");
            item.SubItems.Add(availability.Reason);
            item.SubItems.Add(_save.ActionCooldowns.GetValueOrDefault(action.Id).ToString());
            item.SubItems.Add(availability.CostSummary);
            item.SubItems.Add(action.Description);
            item.Tag = availability;
            lvActions.Items.Add(item);
        }

        btnExecuteAction.Enabled = IsSelectedActionAvailable();
    }

    private void RefreshCombatTab()
    {
        lvCombatants.Items.Clear();
        lvCombatActions.Items.Clear();
        var healthStat = string.IsNullOrWhiteSpace(_project.Combat?.PlayerHealthStatId) ? "health" : _project.Combat.PlayerHealthStatId;
        foreach (var combatant in _runtimeEngine.GetCombatants(_project, _save))
        {
            var item = new ListViewItem(string.IsNullOrWhiteSpace(combatant.Name) ? combatant.RuntimeId : combatant.Name);
            item.SubItems.Add(combatant.Team);
            item.SubItems.Add(combatant.Stats.GetValueOrDefault(healthStat).ToString());
            item.SubItems.Add(combatant.Initiative.ToString());
            item.SubItems.Add(string.Join(", ", combatant.ActiveStatusEffects.Select(x => x.StatusEffectId)));
            item.Tag = combatant.RuntimeId;
            lvCombatants.Items.Add(item);
        }

        var actor = _runtimeEngine.GetCurrentCombatant(_project, _save);
        foreach (var action in _runtimeEngine.GetAvailableCombatActions(_project, _save, actor))
        {
            var item = new ListViewItem(string.IsNullOrWhiteSpace(action.Name) ? action.Id : action.Name);
            item.SubItems.Add(action.TargetScope);
            item.SubItems.Add(action.Description);
            item.Tag = action.Id;
            lvCombatActions.Items.Add(item);
        }

        SelectDefaultCombatItems();
        RefreshCombatButtons();
    }

    private void SelectDefaultCombatItems()
    {
        if (!_save.Combat.IsActive)
        {
            return;
        }

        if (lvCombatActions.SelectedItems.Count == 0 && lvCombatActions.Items.Count > 0)
        {
            lvCombatActions.Items[0].Selected = true;
        }

        var actor = _runtimeEngine.GetCurrentCombatant(_project, _save);
        var selectedActionId = lvCombatActions.SelectedItems.Count > 0
            ? lvCombatActions.SelectedItems[0].Tag as string ?? lvCombatActions.SelectedItems[0].Text
            : string.Empty;
        var action = _project.Actions.FirstOrDefault(x => string.Equals(x.Id, selectedActionId, StringComparison.OrdinalIgnoreCase));
        var targetRuntimeId = ResolvePreferredCombatTargetRuntimeId(_save, actor, action);
        if (string.IsNullOrWhiteSpace(targetRuntimeId) && lvCombatants.Items.Count > 0)
        {
            targetRuntimeId = lvCombatants.Items[0].Tag as string ?? lvCombatants.Items[0].Text;
        }

        if (!string.IsNullOrWhiteSpace(targetRuntimeId))
        {
            foreach (ListViewItem item in lvCombatants.Items)
            {
                var runtimeId = item.Tag as string ?? item.Text;
                item.Selected = string.Equals(runtimeId, targetRuntimeId, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static string ResolvePreferredCombatTargetRuntimeId(SaveGame save, GameRuntimeCombatant? actor, GameActionDefinition? action)
    {
        if (actor == null)
        {
            return string.Empty;
        }

        var scope = action?.TargetScope ?? string.Empty;
        if (scope.Contains("self", StringComparison.OrdinalIgnoreCase) || scope.Contains("actor", StringComparison.OrdinalIgnoreCase))
        {
            return actor.RuntimeId;
        }

        var target = save.Combat.Combatants.FirstOrDefault(x =>
            x.RuntimeId != actor.RuntimeId
            && !string.Equals(x.Team, actor.Team, StringComparison.OrdinalIgnoreCase)
            && x.Stats.Values.Any(value => value > 0));
        if (target != null)
        {
            return target.RuntimeId;
        }

        target = string.Equals(actor.Team, "enemy", StringComparison.OrdinalIgnoreCase)
            ? save.Combat.Combatants.FirstOrDefault(x => !string.Equals(x.Team, "enemy", StringComparison.OrdinalIgnoreCase))
            : save.Combat.Combatants.FirstOrDefault(x => string.Equals(x.Team, "enemy", StringComparison.OrdinalIgnoreCase));

        return target?.RuntimeId ?? actor.RuntimeId;
    }

    private void RefreshCombatButtons()
    {
        var scene = _runtimeEngine.GetCurrentScene(_project, _save);
        var actor = _runtimeEngine.GetCurrentCombatant(_project, _save);
        var isPlayerTurn = actor != null && !string.Equals(actor.Team, "enemy", StringComparison.OrdinalIgnoreCase);
        btnStartCombat.Enabled = !_save.Combat.IsActive && scene.StartsCombat;
        btnExecuteCombatAction.Enabled = _save.Combat.IsActive && isPlayerTurn && lvCombatActions.SelectedItems.Count > 0 && lvCombatants.SelectedItems.Count > 0;
        btnEndCombatTurn.Enabled = _save.Combat.IsActive;
        lblCombatHint.Text = actor == null
            ? "Бой не активен."
            : "Ход: " + (string.IsNullOrWhiteSpace(actor.Name) ? actor.RuntimeId : actor.Name);
    }

    private void AddOperationLog(GameRuntimeOperationResult result)
    {
        if (result.LogLines.Count > 0)
        {
            foreach (var line in result.LogLines)
            {
                AddLog(line);
            }
        }
        else if (!string.IsNullOrWhiteSpace(result.Message))
        {
            AddLog(result.Message);
        }
        if (!result.Success && !string.IsNullOrWhiteSpace(result.Message) && !result.LogLines.Contains(result.Message))
        {
            AddLog(result.Message);
        }
    }

    private async Task SaveAutosaveProgressAsync()
    {
        try
        {
            await _storageService.SaveProgressAsync(_project, _save, "autosave.json");
        }
        catch (Exception ex)
        {
            AddLog("Autosave failed: " + ex.Message);
        }
    }

    private void FillEffects()
    {
        lvEffects.Items.Clear();
        foreach (var active in _save.ActiveStatusEffects)
        {
            var definition = _project.StatusEffects.FirstOrDefault(x => string.Equals(x.Id, active.StatusEffectId, StringComparison.OrdinalIgnoreCase));
            var item = new ListViewItem(active.StatusEffectId);
            item.SubItems.Add(definition?.Name ?? active.StatusEffectId);
            item.SubItems.Add(definition?.Kind ?? string.Empty);
            item.SubItems.Add(active.Stacks.ToString());
            item.SubItems.Add(active.RemainingTurns > 0 ? active.RemainingTurns.ToString() : "бессрочно");
            item.SubItems.Add(active.SourceId);
            item.SubItems.Add(definition?.Description ?? string.Empty);
            lvEffects.Items.Add(item);
        }
    }

    private void FillProgression()
    {
        lvProgression.Items.Clear();
        var availableIds = _runtimeEngine.GetAvailableProgressionNodes(_project, _save)
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var node in _project.ProgressionNodes)
        {
            var state = _save.UnlockedProgressionNodeIds.Contains(node.Id, StringComparer.OrdinalIgnoreCase)
                ? "открыто"
                : availableIds.Contains(node.Id) ? "доступно" : "недоступно";
            var item = new ListViewItem(node.Id);
            item.SubItems.Add(string.IsNullOrWhiteSpace(node.Name) ? node.Id : node.Name);
            item.SubItems.Add(node.Kind);
            item.SubItems.Add(state);
            item.SubItems.Add(DescribeRequirements(node.UnlockRequirements));
            item.SubItems.Add(DescribeCosts(node.UnlockCosts));
            item.SubItems.Add(node.Description);
            lvProgression.Items.Add(item);
        }

        btnUnlockProgression.Enabled = IsSelectedProgressionAvailable();
    }

    private bool IsSelectedActionAvailable()
    {
        return lvActions.SelectedItems.Count > 0
            && lvActions.SelectedItems[0].Tag is GameActionAvailabilityResult { IsAvailable: true };
    }

    private bool IsSelectedProgressionAvailable()
    {
        return lvProgression.SelectedItems.Count > 0
            && string.Equals(lvProgression.SelectedItems[0].SubItems[3].Text, "доступно", StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeRequirements(IEnumerable<GameRequirement> requirements)
    {
        var list = requirements.Select(x => string.IsNullOrWhiteSpace(x.Text) ? $"{x.Type}:{x.TargetId} {x.Operator} {x.Value}" : x.Text).ToList();
        return list.Count == 0 ? "-" : string.Join("; ", list);
    }

    private string DescribeCosts(IEnumerable<GameCost> costs)
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

    private void LoadSceneImage(GameScene scene)
    {
        var oldImage = picScene.Image;
        picScene.Image = null;
        oldImage?.Dispose();

        var imagePath = _project.AssetLinks.FirstOrDefault(x => x.AssetId == scene.ImageAssetId)?.ImagePath
            ?? _project.ImagePrompts.FirstOrDefault(x => x.AssetId == scene.ImageAssetId)?.SelectedImagePath;
        imagePath = ImageAssetService.ResolveProjectPath(_project, imagePath ?? "");
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(imagePath);
            using var sourceImage = Image.FromStream(stream);
            picScene.Image = new Bitmap(sourceImage);
        }
        catch (Exception ex)
        {
            AddLog("Не удалось загрузить изображение сцены: " + ex.Message);
        }
    }

    private string BuildStatusText(GameScene scene)
    {
        var locationId = string.IsNullOrWhiteSpace(_save.CurrentLocationId) ? scene.LocationId : _save.CurrentLocationId;
        var location = _project.Locations.FirstOrDefault(x => x.Id == locationId);
        var prefix = _save.TurnNumber > 0 ? $"Ход: {_save.TurnNumber}. " : string.Empty;
        return prefix + (location == null ? "Локация: -" : "Локация: " + location.Name);
    }

    private void AddLog(string text)
    {
        _save.EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {text}");
        txtLog.Text = string.Join(Environment.NewLine, _save.EventLog);
    }

    private IEnumerable<(string Id, string Name, string Description)> BuildCharacterRows()
    {
        yield return ("player_level", "Уровень", Math.Max(1, _save.PlayerLevel).ToString());
        yield return ("player_xp", "Опыт", BuildPlayerExperienceText());
        foreach (var stat in _runtimeEngine.GetEffectiveStats(_project, _save))
        {
            yield return (stat.Key, FindStatName(stat.Key), stat.Value.ToString());
        }
    }

    private string BuildPlayerExperienceText()
    {
        if (!_project.Mechanics.Experience.EnablePlayerExperience)
        {
            return _save.PlayerExperience.ToString();
        }

        var formula = !string.IsNullOrWhiteSpace(_project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaId)
            ? _project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaId
            : _project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression;
        var threshold = string.IsNullOrWhiteSpace(formula)
            ? new GameFormulaEvaluationResult { Success = true, Value = 100 * Math.Max(1, _save.PlayerLevel) }
            : _runtimeEngine.TryEvaluateFormula(_project, _save, formula);
        return threshold.Success ? $"{_save.PlayerExperience} / {threshold.Value}" : _save.PlayerExperience.ToString();
    }

    private string BuildSkillStateText(GameKnownSkill known)
    {
        var text = $"Ур. {known.Level}";
        if (_project.Mechanics.Experience.EnableSkillExperience)
        {
            text += $", опыт {known.Experience}";
        }
        if (known.CooldownRemaining > 0)
        {
            text += $", перезарядка {known.CooldownRemaining}";
        }

        return text;
    }

    private void SelectTab(TabPage tabPage)
    {
        if (tabInfo.TabPages.Contains(tabPage))
        {
            tabInfo.SelectedTab = tabPage;
        }
    }

    private void SetTabVisible(TabPage tabPage, bool visible)
    {
        if (visible && !tabInfo.TabPages.Contains(tabPage))
        {
            tabInfo.TabPages.Add(tabPage);
        }
        else if (!visible && tabInfo.TabPages.Contains(tabPage))
        {
            tabInfo.TabPages.Remove(tabPage);
        }
    }

    private static void FillList(ListView listView, IEnumerable<(string Id, string Name, string Description)> rows, (string Id, string Name, string Description)? emptyRow = null)
    {
        PlayListViewHelper.FillList(listView, rows, emptyRow);
    }

    private string FindStatName(string id) => _project.Stats.FirstOrDefault(x => x.Id == id)?.Name ?? id;
    private string FindCurrencyName(string id) => _project.Currencies.FirstOrDefault(x => x.Id == id)?.Name ?? id;
    private string FindItemName(string id) => _project.Items.FirstOrDefault(x => x.Id == id)?.Name ?? id;
    private string FindSlotName(string id) => _project.EquipmentSlots.FirstOrDefault(x => x.Id == id)?.Name ?? id;
    private string FindSkillName(string id) => _project.Skills.FirstOrDefault(x => x.Id == id)?.Name ?? id;
    private string FindQuestName(string id) => _project.Quests.FirstOrDefault(x => x.Id == id)?.Title ?? id;
    private string FindRelationshipName(string id) => _project.Relationships.FirstOrDefault(x => x.CharacterId == id)?.Name ?? id;
}
