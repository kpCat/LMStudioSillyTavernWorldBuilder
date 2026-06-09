using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Runtime;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameRuntimeEngineTests
{
    [Fact]
    public void CurrentScene_FallsBackToFirstScene()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreatePlayableProject();
        var save = TestProjects.CreateSave(project);
        save.CurrentSceneId = "missing";

        var scene = engine.GetCurrentScene(project, save);

        Assert.Equal("scene_start", scene.Id);
    }

    [Fact]
    public void ChoiceTransition_AppliesAllSupportedEffects()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreatePlayableProject();
        var save = TestProjects.CreateSave(project);

        var ok = engine.ApplyChoice(project, save, "choice_go", out _);

        Assert.True(ok);
        Assert.Equal("scene_next", save.CurrentSceneId);
        Assert.Equal(11, save.PlayerStats["will"]);
        Assert.Equal(1, save.Inventory["key"]);
        Assert.Equal(5, save.Relationships["npc"]);
        Assert.Contains("quest_main", save.ActiveQuestIds);
    }

    [Fact]
    public void ConditionCheck_HidesUnavailableChoice()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreatePlayableProject();
        var save = TestProjects.CreateSave(project);
        save.PlayerStats["will"] = 1;

        var choices = engine.GetAvailableChoices(project, save);

        Assert.DoesNotContain(choices, x => x.Id == "choice_go");
    }

    [Fact]
    public void UseConsumableItem_AppliesEffectsAndReducesQuantity()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);
        engine.AddItem(project, save, "potion", 2);
        var entry = engine.GetInventory(project, save).First(x => x.ItemId == "potion");

        var ok = engine.UseItem(project, save, entry.InstanceId);

        Assert.True(ok);
        Assert.Equal(13, save.PlayerStats["will"]);
        Assert.Equal(1, engine.GetInventory(project, save).First(x => x.ItemId == "potion").Quantity);
    }

    [Fact]
    public void EquipItem_AppliesModifiersInEffectiveStats()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);
        engine.AddItem(project, save, "sword", 1);
        var entry = engine.GetInventory(project, save).First(x => x.ItemId == "sword");

        var ok = engine.EquipItem(project, save, entry.InstanceId);

        Assert.True(ok);
        Assert.Equal(16, engine.GetEffectiveStats(project, save)["will"]);
    }

    [Fact]
    public void PassiveSkill_AppliesModifier()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);

        var stats = engine.GetEffectiveStats(project, save);

        Assert.Equal(14, stats["will"]);
    }

    [Fact]
    public void ActiveSkill_ConsumesResourceAndAppliesEffect()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);
        engine.LearnSkill(project, save, "firebolt");

        var ok = engine.UseSkill(project, save, "firebolt");

        Assert.True(ok);
        Assert.Equal(7, save.PlayerStats["mana"]);
        Assert.Equal(1, save.Variables["alarm"]);
    }

    [Fact]
    public void LearnSkill_AddsKnownSkill()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);

        var ok = engine.LearnSkill(project, save, "firebolt");

        Assert.True(ok);
        Assert.Contains(save.KnownSkills, x => x.SkillId == "firebolt");
    }

    [Fact]
    public void CurrencyEffect_ChangesCurrency()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "currency", TargetId = "gold", Amount = 3 } });

        Assert.Equal(8, save.Currencies["gold"]);
    }

    [Fact]
    public void LocationRequirement_BlocksTravel()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);
        save.CurrentLocationId = "location_start";

        var ok = engine.TravelToLocation(project, save, "locked_room");

        Assert.False(ok);
    }

    [Fact]
    public void LocationStateEffect_ChangesLocationState()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "locationState", TargetId = "location_start", Text = "burning" } });

        Assert.Equal("burning", save.LocationStates["location_start"]);
    }

    [Fact]
    public void ChoiceCanDependOnFlagOrVariable()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Scenes[0].Choices.Add(new GameChoice
        {
            Id = "choice_secret",
            Text = "Secret",
            Conditions =
            {
                new GameCondition { Type = "flag", TargetId = "door_open", Operator = "==" },
                new GameCondition { Type = "variable", TargetId = "alarm", Operator = ">=", Value = 1 }
            }
        });
        var save = TestProjects.CreateSave(project);

        Assert.DoesNotContain(engine.GetAvailableChoices(project, save), x => x.Id == "choice_secret");
        save.Flags.Add("door_open");
        save.Variables["alarm"] = 1;
        Assert.Contains(engine.GetAvailableChoices(project, save), x => x.Id == "choice_secret");
    }

    [Fact]
    public void FormulaEvaluator_EvaluatesStatsAndArithmetic()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Formulas.Add(new GameFormulaDefinition { Id = "will_check", Expression = "stat.will + effectiveStat.will * 2" });
        var save = TestProjects.CreateSave(project);

        var value = engine.EvaluateFormula(project, save, "will_check");

        Assert.Equal(38, value);
        Assert.Equal(12, engine.EvaluateFormula(project, save, "10 + 4 / 2"));
    }

    [Fact]
    public void StatusEffect_ModifiesEffectiveStatsAndExpiresAfterTurn()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreatePlayableProject();
        project.StatusEffects.Add(new GameStatusEffectDefinition
        {
            Id = "focused",
            Name = "Focused",
            DefaultDurationTurns = 1,
            Modifiers = { new GameModifier { Type = "stat", TargetId = "will", Amount = 5 } }
        });
        var save = TestProjects.CreateSave(project);

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "status", StatusEffectId = "focused" } });

        Assert.Equal(15, engine.GetEffectiveStats(project, save)["will"]);
        engine.EndTurn(project, save);
        Assert.Empty(save.ActiveStatusEffects);
        Assert.Equal(10, engine.GetEffectiveStats(project, save)["will"]);
    }

    [Fact]
    public void ProgressionNode_UnlocksSkillAndPaysCost()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.ProgressionNodes.Add(new GameProgressionNodeDefinition
        {
            Id = "learn_firebolt",
            Name = "Learn Firebolt",
            SkillId = "firebolt",
            UnlockCosts =
            {
                new GameCost { Type = "currency", TargetId = "gold", Amount = 3 },
                new GameCost { Type = "item", TargetId = "key", Amount = 1 }
            }
        });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;
        engine.AddItem(project, save, "key", 1);

        var ok = engine.UnlockProgressionNode(project, save, "learn_firebolt", out _);

        Assert.True(ok);
        Assert.Contains("learn_firebolt", save.UnlockedProgressionNodeIds);
        Assert.Contains(save.KnownSkills, x => x.SkillId == "firebolt");
        Assert.Equal(2, save.Currencies["gold"]);
        Assert.Equal(0, save.Inventory.GetValueOrDefault("key"));
    }

    [Fact]
    public void Action_AppliesEffectsAndCooldown()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Actions.Add(new GameActionDefinition
        {
            Id = "raise_alarm",
            Name = "Raise Alarm",
            Kind = "social",
            CooldownTurns = 2,
            Effects = { new GameEffect { Type = "variable", TargetId = "alarm", Amount = 1 } }
        });
        var save = TestProjects.CreateSave(project);
        save.Variables["alarm"] = 0;

        Assert.Contains(engine.GetAvailableActions(project, save), x => x.Id == "raise_alarm");
        var ok = engine.ApplyAction(project, save, "raise_alarm");

        Assert.True(ok);
        Assert.Equal(1, save.Variables["alarm"]);
        Assert.Equal(2, save.ActionCooldowns["raise_alarm"]);
        Assert.DoesNotContain(engine.GetAvailableActions(project, save), x => x.Id == "raise_alarm");
        engine.EndTurn(project, save);
        Assert.Equal(1, save.ActionCooldowns["raise_alarm"]);
    }

    [Fact]
    public void ExecuteAction_FailureDoesNotSpendCosts()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Actions.Add(new GameActionDefinition
        {
            Id = "locked_action",
            Name = "Locked",
            Requirements = { new GameRequirement { Type = "flag", TargetId = "missing_flag" } },
            Costs = { new GameCost { Type = "currency", TargetId = "gold", Amount = 3 } },
            Effects = { new GameEffect { Type = "variable", TargetId = "alarm", Amount = 1 } }
        });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;

        var result = engine.ExecuteAction(project, save, "locked_action");

        Assert.False(result.Success);
        Assert.Equal(5, save.Currencies["gold"]);
        Assert.Equal(0, save.Variables.GetValueOrDefault("alarm"));
    }

    [Fact]
    public void ExecuteAction_InvalidEffectFormulaDoesNotSpendCosts()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Actions.Add(new GameActionDefinition
        {
            Id = "bad_formula_action",
            Name = "Bad Formula",
            CooldownTurns = 2,
            Costs = { new GameCost { Type = "currency", TargetId = "gold", Amount = 3 } },
            Effects = { new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "stat.unknown + 1" } }
        });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;
        save.Variables["alarm"] = 0;

        var result = engine.ExecuteAction(project, save, "bad_formula_action");

        Assert.False(result.Success);
        Assert.Equal(5, save.Currencies["gold"]);
        Assert.Equal(0, save.Variables["alarm"]);
        Assert.False(save.ActionCooldowns.ContainsKey("bad_formula_action"));
    }

    [Fact]
    public void ExecuteAction_WithResultInvalidEffectFormulaDoesNotMutate()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Actions.Add(new GameActionDefinition
        {
            Id = "bad_formula_action_result",
            Name = "Bad Formula Result",
            CooldownTurns = 2,
            Costs = { new GameCost { Type = "currency", TargetId = "gold", Amount = 3 } },
            Effects = { new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "stat.unknown + 1" } }
        });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;
        save.Variables["alarm"] = 0;

        var result = engine.ExecuteAction(project, save, "bad_formula_action_result");

        Assert.False(result.Success);
        Assert.Equal(5, save.Currencies["gold"]);
        Assert.Equal(0, save.Variables["alarm"]);
        Assert.False(save.ActionCooldowns.ContainsKey("bad_formula_action_result"));
    }

    [Fact]
    public void UseSkillWithResult_NotEnoughCostDoesNotSpendOrCooldown()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);
        engine.LearnSkill(project, save, "firebolt");
        save.PlayerStats["mana"] = 1;

        var result = engine.UseSkillWithResult(project, save, "firebolt");

        Assert.False(result.Success);
        Assert.Contains("Недостаточно", result.Message);
        Assert.Equal(1, save.PlayerStats["mana"]);
        Assert.Equal(0, save.KnownSkills.Single(x => x.SkillId == "firebolt").CooldownRemaining);
    }

    [Fact]
    public void UseItem_InvalidEffectFormulaDoesNotConsumeItem()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Items.Add(new GameItemDefinition
        {
            Id = "bad_potion",
            Name = "Bad Potion",
            IsStackable = true,
            IsUsable = true,
            IsConsumable = true,
            UseEffects = { new GameEffect { Type = "stat", TargetId = "will", FormulaExpression = "stat.unknown + 1" } }
        });
        var save = TestProjects.CreateSave(project);
        engine.AddItem(project, save, "bad_potion", 1);
        var entry = engine.GetInventory(project, save).First(x => x.ItemId == "bad_potion");

        var ok = engine.UseItem(project, save, entry.InstanceId);

        Assert.False(ok);
        Assert.Equal(1, engine.GetInventory(project, save).First(x => x.ItemId == "bad_potion").Quantity);
        Assert.Equal(10, save.PlayerStats["will"]);
    }

    [Fact]
    public void UseItemWithResult_ConsumableInvalidEffectFormulaDoesNotConsumeItem()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Items.Add(new GameItemDefinition
        {
            Id = "bad_elixir",
            Name = "Bad Elixir",
            IsStackable = true,
            IsUsable = true,
            IsConsumable = true,
            UseEffects = { new GameEffect { Type = "stat", TargetId = "will", FormulaExpression = "stat.unknown + 1" } }
        });
        var save = TestProjects.CreateSave(project);
        engine.AddItem(project, save, "bad_elixir", 1);
        var entry = engine.GetInventory(project, save).First(x => x.ItemId == "bad_elixir");

        var result = engine.UseItemWithResult(project, save, entry.InstanceId);

        Assert.False(result.Success);
        Assert.Equal(1, engine.GetInventory(project, save).First(x => x.ItemId == "bad_elixir").Quantity);
        Assert.Equal(10, save.PlayerStats["will"]);
    }

    [Fact]
    public void ApplyChoice_InvalidEffectFormulaDoesNotMoveScene()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreatePlayableProject();
        project.Scenes[0].Choices.Add(new GameChoice
        {
            Id = "choice_bad_formula",
            Text = "Bad formula",
            NextSceneId = "scene_next",
            Effects = { new GameEffect { Type = "stat", TargetId = "will", FormulaExpression = "stat.unknown + 1" } }
        });
        var save = TestProjects.CreateSave(project);

        var ok = engine.ApplyChoice(project, save, "choice_bad_formula", out var message);

        Assert.False(ok);
        Assert.Contains("Ошибка формулы эффекта", message);
        Assert.Equal("scene_start", save.CurrentSceneId);
        Assert.Equal(10, save.PlayerStats["will"]);
    }

    [Fact]
    public void ActionRequirement_UsesEffectiveStat()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Actions.Add(new GameActionDefinition
        {
            Id = "focus_action",
            Name = "Focus Action",
            Requirements = { new GameRequirement { Type = "stat", TargetId = "will", Operator = ">=", Value = 14 } },
            Effects = { new GameEffect { Type = "variable", TargetId = "alarm", Amount = 1 } }
        });
        var save = TestProjects.CreateSave(project);

        var result = engine.ExecuteAction(project, save, "focus_action");

        Assert.True(result.Success);
        Assert.Equal(1, save.Variables["alarm"]);
    }

    [Fact]
    public void FormulaEvaluator_ReturnsErrorsForBadFormulas()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        var save = TestProjects.CreateSave(project);

        Assert.True(engine.TryEvaluateFormula(project, save, "(10 + 2) * 3").Success);
        Assert.False(engine.TryEvaluateFormula(project, save, "10 / 0").Success);
        Assert.False(engine.TryEvaluateFormula(project, save, "stat.unknown + 1").Success);
    }

    [Fact]
    public void FormulaEvaluator_DiceRandomAndClampFormulaIdWork()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Formulas.Add(new GameFormulaDefinition { Id = "bounded", Expression = "stat.will + 100", MinResult = 0, MaxResult = 25 });
        var save = TestProjects.CreateSave(project);

        var random = engine.TryEvaluateFormula(project, save, "random(1, 3)");
        var dice = engine.TryEvaluateFormula(project, save, "dice(2, 6)");
        var bounded = engine.TryEvaluateFormula(project, save, "bounded");

        Assert.True(random.Success);
        Assert.InRange(random.Value, 1, 3);
        Assert.True(dice.Success);
        Assert.InRange(dice.Value, 2, 12);
        Assert.True(bounded.Success);
        Assert.Equal(25, bounded.Value);
    }

    [Fact]
    public void StatusEffect_StacksAndExpires()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreatePlayableProject();
        project.StatusEffects.Add(new GameStatusEffectDefinition
        {
            Id = "focused",
            Name = "Focused",
            DefaultDurationTurns = 1,
            MaxStacks = 2,
            StackMode = "stack",
            Modifiers = { new GameModifier { Type = "stat", TargetId = "will", Amount = 2 } }
        });
        var save = TestProjects.CreateSave(project);

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "status", StatusEffectId = "focused" } });
        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "status", StatusEffectId = "focused" } });

        Assert.Equal(2, save.ActiveStatusEffects.Single().Stacks);
        Assert.Equal(14, engine.GetEffectiveStats(project, save)["will"]);
        var turn = engine.EndTurnWithResult(project, save);
        Assert.Empty(save.ActiveStatusEffects);
        Assert.NotEmpty(turn.LogLines);
    }

    [Fact]
    public void ProgressionUnlock_DoesNotDoubleSpend()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.ProgressionNodes.Add(new GameProgressionNodeDefinition
        {
            Id = "node_one",
            Name = "Node One",
            UnlockCosts = { new GameCost { Type = "currency", TargetId = "gold", Amount = 2 } }
        });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;

        Assert.True(engine.UnlockProgressionNode(project, save, "node_one", out _));
        Assert.False(engine.UnlockProgressionNode(project, save, "node_one", out _));
        Assert.Equal(3, save.Currencies["gold"]);
    }

    [Fact]
    public void UnlockProgressionNodeWithResult_InvalidEffectFormulaDoesNotSpendOrUnlock()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.ProgressionNodes.Add(new GameProgressionNodeDefinition
        {
            Id = "bad_node",
            Name = "Bad Node",
            UnlockCosts = { new GameCost { Type = "currency", TargetId = "gold", Amount = 2 } },
            UnlockEffects = { new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "stat.unknown + 1" } }
        });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;
        save.Variables["alarm"] = 0;

        var result = engine.UnlockProgressionNodeWithResult(project, save, "bad_node");

        Assert.False(result.Success);
        Assert.Equal(5, save.Currencies["gold"]);
        Assert.Equal(0, save.Variables["alarm"]);
        Assert.DoesNotContain("bad_node", save.UnlockedProgressionNodeIds);
    }

    [Fact]
    public void EndTurn_ReturnsUsefulLogLines()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Stats.First(x => x.Id == "mana").RegenPerTurn = 1;
        var save = TestProjects.CreateSave(project);
        save.PlayerStats["mana"] = 5;
        save.ActionCooldowns["test"] = 2;

        var result = engine.EndTurnWithResult(project, save);

        Assert.Equal(1, result.NewTurnNumber);
        Assert.NotEmpty(result.LogLines);
        Assert.Contains(result.CooldownChanges, x => x.Contains("test", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(6, save.PlayerStats["mana"]);
    }

    [Fact]
    public void PlayerExperienceEffect_AddsExperienceAndRaisesLevel()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnablePlayerExperience = true;
        project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression = "10";
        var save = TestProjects.CreateSave(project);

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "playerExperience", Amount = 12 } });

        Assert.Equal(2, save.PlayerLevel);
        Assert.Equal(2, save.PlayerExperience);
        Assert.Contains(save.EventLog, x => x.Contains("уров", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SkillExperienceEffect_AddsExperienceAndRaisesSkillLevel()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnableSkillExperience = true;
        project.Skills.First(x => x.Id == "focus").ExperienceToNextLevel = 10;
        project.Skills.First(x => x.Id == "focus").MaxLevel = 5;
        var save = TestProjects.CreateSave(project);
        save.KnownSkills.Add(new GameKnownSkill { SkillId = "focus", Level = 1 });

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "skillExperience", TargetId = "focus", Amount = 12 } });

        var known = save.KnownSkills.Single(x => x.SkillId == "focus");
        Assert.Equal(2, known.Level);
        Assert.Equal(2, known.Experience);
        Assert.Contains(save.EventLog, x => x.Contains("focus", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BrokenPlayerExperienceThreshold_DoesNotMutateSave()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnablePlayerExperience = true;
        project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression = "stat.unknown +";
        var save = TestProjects.CreateSave(project);
        save.PlayerLevel = 1;
        save.PlayerExperience = 5;

        var result = engine.AddPlayerExperienceWithResult(project, save, 10);

        Assert.False(result.Success);
        Assert.Equal(1, save.PlayerLevel);
        Assert.Equal(5, save.PlayerExperience);
    }

    [Fact]
    public void ActionPlayerExperienceEffect_BrokenThresholdDoesNotSpendCostOrCooldown()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnablePlayerExperience = true;
        project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression = "stat.unknown +";
        project.Actions.Add(new GameActionDefinition
        {
            Id = "bad_xp_action",
            Name = "Bad XP Action",
            CooldownTurns = 2,
            Costs = { new GameCost { Type = "currency", TargetId = "gold", Amount = 3 } },
            Effects = { new GameEffect { Type = "playerExperience", Amount = 10 } }
        });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;
        save.PlayerLevel = 1;
        save.PlayerExperience = 5;

        var result = engine.ExecuteAction(project, save, "bad_xp_action");

        Assert.False(result.Success);
        Assert.Equal(5, save.Currencies["gold"]);
        Assert.Equal(1, save.PlayerLevel);
        Assert.Equal(5, save.PlayerExperience);
        Assert.False(save.ActionCooldowns.ContainsKey("bad_xp_action"));
    }

    [Fact]
    public void SkillPlayerExperienceEffect_BrokenThresholdDoesNotSpendCostOrCooldown()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnablePlayerExperience = true;
        project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression = "stat.unknown +";
        var skill = project.Skills.First(x => x.Id == "firebolt");
        skill.Effects.Clear();
        skill.Effects.Add(new GameEffect { Type = "playerExperience", Amount = 10 });
        var save = TestProjects.CreateSave(project);
        save.PlayerStats["mana"] = 10;
        save.PlayerLevel = 1;
        save.PlayerExperience = 5;
        engine.LearnSkill(project, save, "firebolt");

        var result = engine.UseSkillWithResult(project, save, "firebolt");

        Assert.False(result.Success);
        Assert.Equal(10, save.PlayerStats["mana"]);
        Assert.Equal(1, save.PlayerLevel);
        Assert.Equal(5, save.PlayerExperience);
        Assert.Equal(0, save.KnownSkills.Single(x => x.SkillId == "firebolt").CooldownRemaining);
    }

    [Fact]
    public void ItemPlayerExperienceEffect_BrokenThresholdDoesNotConsumeItem()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnablePlayerExperience = true;
        project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression = "stat.unknown +";
        project.Items.Add(new GameItemDefinition
        {
            Id = "bad_xp_potion",
            Name = "Bad XP Potion",
            IsStackable = true,
            IsConsumable = true,
            IsUsable = true,
            UseEffects = { new GameEffect { Type = "playerExperience", Amount = 10 } }
        });
        var save = TestProjects.CreateSave(project);
        save.PlayerLevel = 1;
        save.PlayerExperience = 5;
        engine.AddItem(project, save, "bad_xp_potion", 1);
        var entry = engine.GetInventory(project, save).First(x => x.ItemId == "bad_xp_potion");

        var result = engine.UseItemWithResult(project, save, entry.InstanceId);

        Assert.False(result.Success);
        Assert.Equal(1, save.PlayerLevel);
        Assert.Equal(5, save.PlayerExperience);
        Assert.Equal(1, engine.GetInventory(project, save).First(x => x.ItemId == "bad_xp_potion").Quantity);
    }

    [Fact]
    public void ChoicePlayerExperienceEffect_BrokenThresholdDoesNotMoveScene()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnablePlayerExperience = true;
        project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression = "stat.unknown +";
        project.Scenes[0].Choices.Add(new GameChoice
        {
            Id = "bad_xp_choice",
            Text = "Bad XP Choice",
            NextSceneId = "scene_next",
            Effects = { new GameEffect { Type = "playerExperience", Amount = 10 } }
        });
        var save = TestProjects.CreateSave(project);
        save.PlayerLevel = 1;
        save.PlayerExperience = 5;

        var result = engine.ApplyChoiceWithResult(project, save, "bad_xp_choice");

        Assert.False(result.Success);
        Assert.Equal("scene_start", save.CurrentSceneId);
        Assert.Equal(1, save.PlayerLevel);
        Assert.Equal(5, save.PlayerExperience);
    }

    [Fact]
    public void SkillExperienceEffect_BrokenThresholdDoesNotMutateSave()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnableSkillExperience = true;
        project.Mechanics.Experience.SkillExperienceToNextLevelFormulaExpression = "stat.unknown +";
        project.Skills.First(x => x.Id == "focus").MaxLevel = 5;
        project.Actions.Add(new GameActionDefinition
        {
            Id = "bad_skill_xp_action",
            Name = "Bad Skill XP Action",
            Costs = { new GameCost { Type = "currency", TargetId = "gold", Amount = 3 } },
            Effects = { new GameEffect { Type = "skillExperience", TargetId = "focus", Amount = 10 } }
        });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;
        save.KnownSkills.Add(new GameKnownSkill { SkillId = "focus", Level = 1, Experience = 5 });

        var result = engine.ExecuteAction(project, save, "bad_skill_xp_action");

        Assert.False(result.Success);
        Assert.Equal(5, save.Currencies["gold"]);
        var known = save.KnownSkills.Single(x => x.SkillId == "focus");
        Assert.Equal(1, known.Level);
        Assert.Equal(5, known.Experience);
    }

    [Fact]
    public void LevelUpEffect_BrokenFormulaDoesNotPartiallyMutateOperation()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnablePlayerExperience = true;
        project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression = "10";
        project.Mechanics.Experience.PlayerLevelUpEffects.Add(new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "stat.unknown +" });
        project.Actions.Add(new GameActionDefinition
        {
            Id = "bad_levelup_action",
            Name = "Bad LevelUp Action",
            Costs = { new GameCost { Type = "currency", TargetId = "gold", Amount = 3 } },
            Effects = { new GameEffect { Type = "playerExperience", Amount = 10 } }
        });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 5;
        save.PlayerLevel = 1;
        save.PlayerExperience = 0;
        save.Variables["alarm"] = 0;

        var result = engine.ExecuteAction(project, save, "bad_levelup_action");

        Assert.False(result.Success);
        Assert.Equal(5, save.Currencies["gold"]);
        Assert.Equal(1, save.PlayerLevel);
        Assert.Equal(0, save.PlayerExperience);
        Assert.Equal(0, save.Variables["alarm"]);
    }

    [Fact]
    public void ExperienceFormulaWithDice_IsResolvedForSingleOperation()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.Experience.EnablePlayerExperience = true;
        project.Mechanics.Experience.PlayerExperienceToNextLevelFormulaExpression = "100";
        var save = TestProjects.CreateSave(project);

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "experience", FormulaExpression = "dice(1, 1)" } });

        Assert.Equal(1, save.PlayerExperience);
        Assert.Equal(1, save.PlayerLevel);
    }

    [Fact]
    public void CreateInitialSave_InitializesWorldState()
    {
        var project = TestProjects.CreateWorldStateProject();
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        Assert.Equal(2, save.WorldState.DayNumber);
        Assert.Equal("morning", save.WorldState.TimeSegmentId);
        Assert.Equal("clear", save.WorldState.AspectStates["weather"]);
    }

    [Fact]
    public void WorldStateRequirements_CheckTimeAndAspect()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        Assert.True(engine.CheckRequirement(project, save, new GameRequirement { Type = "timeSegment", TargetId = "morning", Operator = "==" }));
        Assert.True(engine.CheckRequirement(project, save, new GameRequirement { Type = "worldAspect", TargetId = "weather", StringValue = "clear" }));
    }

    [Fact]
    public void AdvanceTimeEffect_MovesSegmentAndWrapsDay()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "advanceTime", Amount = 2 } });

        Assert.Equal("morning", save.WorldState.TimeSegmentId);
        Assert.Equal(3, save.WorldState.DayNumber);
    }

    [Fact]
    public void WorldStateEffect_ChangesAspectState()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "worldState", TargetId = "weather", StringValue = "rain" } });

        Assert.Equal("rain", save.WorldState.AspectStates["weather"]);
    }

    [Fact]
    public void EndTurn_AdvancesWorldTimeAndLogs()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        var result = engine.EndTurnWithResult(project, save);

        Assert.Equal("night", save.WorldState.TimeSegmentId);
        Assert.Contains(result.LogLines, x => x.Contains("Ночь", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TravelAndAction_ApplyConfiguredTimeAdvance()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        project.Locations.Add(new GameLocation { Id = "location_start", Name = "Start", IsDiscovered = true });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");
        save.CurrentLocationId = "location_start";
        save.Flags.Add("door_open");

        Assert.True(engine.TravelToLocationWithResult(project, save, "locked_room").Success);
        Assert.Equal("night", save.WorldState.TimeSegmentId);

        project.Actions.Add(new GameActionDefinition { Id = "wait", Name = "Wait", Effects = { new GameEffect { Type = "log", Text = "wait" } } });
        Assert.True(engine.ExecuteAction(project, save, "wait").Success);
        Assert.Equal("morning", save.WorldState.TimeSegmentId);
        Assert.Equal(3, save.WorldState.DayNumber);
    }

    [Fact]
    public void Action_WithZeroConfiguredTimeAdvance_DoesNotMoveTime()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        project.WorldState.Time.AdvanceSegmentsOnAction = 0;
        project.Actions.Add(new GameActionDefinition { Id = "wait", Name = "Wait", Effects = { new GameEffect { Type = "log", Text = "wait" } } });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        Assert.True(engine.ExecuteAction(project, save, "wait").Success);

        Assert.Equal("morning", save.WorldState.TimeSegmentId);
        Assert.Equal(2, save.WorldState.DayNumber);
    }

    [Fact]
    public void Action_WithOneConfiguredTimeAdvance_MovesTime()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        project.WorldState.Time.AdvanceSegmentsOnAction = 1;
        project.Actions.Add(new GameActionDefinition { Id = "wait", Name = "Wait", Effects = { new GameEffect { Type = "log", Text = "wait" } } });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        Assert.True(engine.ExecuteAction(project, save, "wait").Success);

        Assert.Equal("night", save.WorldState.TimeSegmentId);
    }

    [Fact]
    public void Travel_WithOneConfiguredTimeAdvance_MovesTime()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        project.WorldState.Time.AdvanceSegmentsOnTravel = 1;
        project.Locations.Add(new GameLocation { Id = "location_start", Name = "Start", IsDiscovered = true });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");
        save.CurrentLocationId = "location_start";
        save.Flags.Add("door_open");

        Assert.True(engine.TravelToLocationWithResult(project, save, "locked_room").Success);

        Assert.Equal("night", save.WorldState.TimeSegmentId);
    }

    [Fact]
    public void AdvanceTimeEffect_WithZeroAmount_DoesNotMoveTime()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "advanceTime", Amount = 0 } });

        Assert.Equal("morning", save.WorldState.TimeSegmentId);
        Assert.Equal(2, save.WorldState.DayNumber);
    }

    [Fact]
    public void Action_WithWorldStateEffectBrokenOnEnter_DoesNotPayCostOrChangeState()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var rain = project.WorldState.Aspects.First(x => x.Id == "weather").States.First(x => x.Id == "rain");
        rain.OnEnterEffects.Add(new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "bad(" });
        project.Actions.Add(new GameActionDefinition
        {
            Id = "call_rain",
            Name = "Call Rain",
            Costs = { new GameCost { Type = "resource", TargetId = "stamina", Amount = 1 } },
            Effects = { new GameEffect { Type = "worldState", TargetId = "weather", StringValue = "rain" } }
        });
        project.Stats.Add(new GameStatDefinition { Id = "stamina", Name = "Stamina", InitialValue = 5, IsResource = true });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");
        save.PlayerStats["stamina"] = 5;

        var result = engine.ExecuteAction(project, save, "call_rain");

        Assert.False(result.Success);
        Assert.Equal(5, save.PlayerStats["stamina"]);
        Assert.Equal("clear", save.WorldState.AspectStates["weather"]);
        Assert.False(save.ActionCooldowns.ContainsKey("call_rain"));
    }

    [Fact]
    public void Action_WithTimeSegmentEffectBrokenOnEnter_DoesNotPayCostOrChangeTime()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var night = project.WorldState.Time.Segments.First(x => x.Id == "night");
        night.OnEnterEffects.Add(new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "bad(" });
        project.Actions.Add(new GameActionDefinition
        {
            Id = "force_night",
            Name = "Force Night",
            Costs = { new GameCost { Type = "resource", TargetId = "stamina", Amount = 1 } },
            Effects = { new GameEffect { Type = "timeSegment", TargetId = "night" } }
        });
        project.Stats.Add(new GameStatDefinition { Id = "stamina", Name = "Stamina", InitialValue = 5, IsResource = true });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");
        save.PlayerStats["stamina"] = 5;

        var result = engine.ExecuteAction(project, save, "force_night");

        Assert.False(result.Success);
        Assert.Equal(5, save.PlayerStats["stamina"]);
        Assert.Equal("morning", save.WorldState.TimeSegmentId);
        Assert.Equal(2, save.WorldState.DayNumber);
        Assert.False(save.ActionCooldowns.ContainsKey("force_night"));
    }

    [Fact]
    public void AmbientEvent_WithWorldStateBrokenOnEnter_DoesNotApplyCooldownRecentOrMainText()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var rain = project.WorldState.Aspects.First(x => x.Id == "weather").States.First(x => x.Id == "rain");
        rain.OnEnterEffects.Add(new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "bad(" });
        project.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
        {
            Id = "bad_rain",
            Name = "Bad rain",
            Text = "Rain starts.",
            Trigger = "turnEnd",
            ChancePercent = 100,
            Weight = 1,
            CooldownTurns = 3,
            Effects = { new GameEffect { Type = "worldState", TargetId = "weather", StringValue = "rain" } }
        });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        var result = engine.TryRollAmbientEvent(project, save, "turnEnd");

        Assert.False(result.Success);
        Assert.Equal("clear", save.WorldState.AspectStates["weather"]);
        Assert.DoesNotContain("bad_rain", save.WorldState.RecentAmbientEventIds);
        Assert.False(save.WorldState.AmbientEventCooldowns.ContainsKey("bad_rain"));
        Assert.DoesNotContain(save.EventLog, x => x.Contains("Rain starts.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(save.EventLog, x => x.Contains("bad_rain", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WorldRule_WithWorldStateBrokenOnEnter_DoesNotSetCooldown()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var rain = project.WorldState.Aspects.First(x => x.Id == "weather").States.First(x => x.Id == "rain");
        rain.OnEnterEffects.Add(new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "bad(" });
        project.WorldState.Rules.Add(new GameWorldRuleDefinition
        {
            Id = "force_rain",
            Name = "Force rain",
            Trigger = "turnEnd",
            ChancePercent = 100,
            CooldownTurns = 3,
            Effects = { new GameEffect { Type = "worldState", TargetId = "weather", StringValue = "rain" } }
        });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        var result = engine.RunWorldRules(project, save, "turnEnd");

        Assert.True(result.Success);
        Assert.Equal("clear", save.WorldState.AspectStates["weather"]);
        Assert.False(save.WorldState.RuleCooldowns.ContainsKey("force_rain"));
        Assert.DoesNotContain(save.EventLog, x => x.Contains("Правило мира: Force rain", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(save.EventLog, x => x.Contains("force_rain", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TimeSegmentOnEnterEffect_WithBrokenFormula_DoesNotEnterSegment()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var night = project.WorldState.Time.Segments.First(x => x.Id == "night");
        night.OnEnterEffects.Add(new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "bad(" });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        var result = engine.AdvanceTimeWithResult(project, save, 1, "test");

        Assert.False(result.Success);
        Assert.Equal("morning", save.WorldState.TimeSegmentId);
        Assert.Contains(save.EventLog, x => x.Contains("Ошибка смены сегмента времени", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WorldAspectOnEnterEffect_WithBrokenFormula_DoesNotEnterState()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var rain = project.WorldState.Aspects.First(x => x.Id == "weather").States.First(x => x.Id == "rain");
        rain.OnEnterEffects.Add(new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "bad(" });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        engine.ApplyEffects(project, save, new[] { new GameEffect { Type = "worldState", TargetId = "weather", StringValue = "rain" } });

        Assert.Equal("clear", save.WorldState.AspectStates["weather"]);
        Assert.Contains(save.EventLog, x => x.Contains("Ошибка изменения состояния мира", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidOnEnterEffects_StillApply()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        var night = project.WorldState.Time.Segments.First(x => x.Id == "night");
        night.OnEnterEffects.Add(new GameEffect { Type = "variable", TargetId = "alarm", Amount = 2 });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        var result = engine.AdvanceTimeWithResult(project, save, 1, "test");

        Assert.True(result.Success);
        Assert.Equal("night", save.WorldState.TimeSegmentId);
        Assert.Equal(2, save.Variables["alarm"]);
    }

    [Fact]
    public void AmbientEvent_WithBrokenEffect_DoesNotApplyOrSetRecentAndCooldown()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        project.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
        {
            Id = "bad_rain",
            Name = "Bad rain",
            Text = "Rain starts.",
            Trigger = "turnEnd",
            ChancePercent = 100,
            Weight = 1,
            CooldownTurns = 3,
            Effects = { new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "bad(" } }
        });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        var result = engine.TryRollAmbientEvent(project, save, "turnEnd");

        Assert.False(result.Success);
        Assert.DoesNotContain("bad_rain", save.WorldState.RecentAmbientEventIds);
        Assert.False(save.WorldState.AmbientEventCooldowns.ContainsKey("bad_rain"));
        Assert.Equal(0, save.Variables.GetValueOrDefault("alarm"));
        Assert.Contains(save.EventLog, x => x.Contains("Ошибка фонового события bad_rain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(save.EventLog, x => x.Contains("Rain starts.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AmbientEventAndWorldRule_ApplyEffects()
    {
        var engine = new GameRuntimeEngine();
        var project = TestProjects.CreateWorldStateProject();
        project.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
        {
            Id = "rain_starts",
            Name = "Rain starts",
            Trigger = "turnEnd",
            ChancePercent = 100,
            Weight = 1,
            Effects = { new GameEffect { Type = "worldState", TargetId = "weather", StringValue = "rain" } }
        });
        project.WorldState.Rules.Add(new GameWorldRuleDefinition
        {
            Id = "night_alarm",
            Name = "Night alarm",
            Trigger = "turnEnd",
            ChancePercent = 100,
            Requirements = { new GameRequirement { Type = "timeSegment", TargetId = "night" } },
            Effects = { new GameEffect { Type = "variable", TargetId = "alarm", Amount = 1 } }
        });
        var save = new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService().CreateInitialSave(project, "autosave");

        engine.EndTurnWithResult(project, save);

        Assert.Equal("rain", save.WorldState.AspectStates["weather"]);
        Assert.Equal(1, save.Variables["alarm"]);
    }

    [Fact]
    public void StartEncounterCombat_CreatesOrderedCombatState()
    {
        var engine = new GameRuntimeEngine();
        var project = CreateCombatProject(10, 10, 100);
        var save = TestProjects.CreateSave(project);

        var result = engine.StartEncounterCombatWithResult(project, save, "encounter_battle");

        Assert.True(result.Success);
        Assert.True(save.Combat.IsActive);
        Assert.Contains(save.Combat.Combatants, x => x.IsPlayer);
        Assert.Contains(save.Combat.Combatants, x => x.Team == "enemy");
    }

    [Fact]
    public void ExecuteCombatAction_DamagesEnemy_AndEndsWithVictory()
    {
        var engine = new GameRuntimeEngine();
        var project = CreateCombatProject(10, 5, 100);
        project.Encounters[0].OnWinEffects.Add(new GameEffect { Type = "playerExperience", Amount = 7 });
        var save = TestProjects.CreateSave(project);
        engine.StartEncounterCombatWithResult(project, save, "encounter_battle");
        var enemy = save.Combat.Combatants.Single(x => x.Team == "enemy");

        var result = engine.ExecuteCombatActionWithResult(project, save, "strike", enemy.RuntimeId);

        Assert.True(result.Success);
        Assert.True(result.CombatEnded);
        Assert.True(result.PlayerWon);
        Assert.False(save.Combat.IsActive);
        Assert.Equal(7, save.PlayerExperience);
    }

    [Fact]
    public void ExecuteCombatAction_MissDoesNotDamage()
    {
        var engine = new GameRuntimeEngine();
        var project = CreateCombatProject(10, 20, 0);
        var save = TestProjects.CreateSave(project);
        engine.StartEncounterCombatWithResult(project, save, "encounter_battle");
        var enemy = save.Combat.Combatants.Single(x => x.Team == "enemy");

        var result = engine.ExecuteCombatActionWithResult(project, save, "strike", enemy.RuntimeId);

        Assert.True(result.Success);
        Assert.Equal(20, enemy.Stats["health"]);
    }

    [Fact]
    public void ExecuteCombatAction_BlockReducesDamage()
    {
        var engine = new GameRuntimeEngine();
        var project = CreateCombatProject(10, 20, 100);
        project.Actions.Single(x => x.Id == "strike").BlockChanceFormulaExpression = "100";
        project.Actions.Single(x => x.Id == "strike").BlockDamagePercent = 50;
        var save = TestProjects.CreateSave(project);
        engine.StartEncounterCombatWithResult(project, save, "encounter_battle");
        var enemy = save.Combat.Combatants.Single(x => x.Team == "enemy");

        var result = engine.ExecuteCombatActionWithResult(project, save, "strike", enemy.RuntimeId);

        Assert.True(result.Success);
        Assert.Equal(15, enemy.Stats["health"]);
    }

    [Fact]
    public void CombatVictoryEffects_AreAtomic_WhenBrokenFormula()
    {
        var engine = new GameRuntimeEngine();
        var project = CreateCombatProject(10, 5, 100);
        project.Currencies.Add(new GameCurrencyDefinition { Id = "gold", Name = "Gold" });
        project.Encounters[0].OnWinEffects.Add(new GameEffect { Type = "currency", TargetId = "gold", FormulaExpression = "stat.unknown + 1" });
        var save = TestProjects.CreateSave(project);
        save.Currencies["gold"] = 0;
        engine.StartEncounterCombatWithResult(project, save, "encounter_battle");
        var enemy = save.Combat.Combatants.Single(x => x.Team == "enemy");

        var result = engine.ExecuteCombatActionWithResult(project, save, "strike", enemy.RuntimeId);

        Assert.False(result.Success);
        Assert.True(save.Combat.IsActive);
        Assert.Equal(0, save.Currencies["gold"]);
    }

    private static GameProjectData CreateCombatProject(int damage, int enemyHealth, int hitChance)
    {
        var project = TestProjects.CreatePlayableProject();
        project.Stats.Add(new GameStatDefinition { Id = "health", Name = "Health", InitialValue = 20, IsResource = true });
        project.Stats.Add(new GameStatDefinition { Id = "agility", Name = "Agility", InitialValue = 10 });
        project.Combat = new GameCombatDefinition
        {
            Enabled = true,
            PlayerHealthStatId = "health",
            DefaultDodgeChanceFormulaExpression = "0",
            DefaultBlockChanceFormulaExpression = "0",
            DefaultCritChanceFormulaExpression = "0"
        };
        project.Scenes[0].StartsCombat = true;
        project.Actions.Add(new GameActionDefinition
        {
            Id = "strike",
            Name = "Strike",
            AvailableInCombat = true,
            ActorTeam = "player",
            TargetScope = "enemy",
            HitChanceFormulaExpression = hitChance.ToString(),
            DodgeChanceFormulaExpression = "0",
            BlockChanceFormulaExpression = "0",
            CritChanceFormulaExpression = "0",
            Effects = { new GameEffect { Type = "combatDamage", Amount = damage } }
        });
        project.Encounters.Add(new GameEncounterDefinition
        {
            Id = "encounter_battle",
            Name = "Battle",
            Kind = "combat",
            SceneId = "scene_start",
            Combatants =
            {
                new GameEncounterCombatantDefinition { Id = "player", Name = "Player", Team = "player", IsPlayer = true, Stats = { ["health"] = 20, ["agility"] = 10 }, ActionIds = { "strike" } },
                new GameEncounterCombatantDefinition { Id = "enemy", Name = "Enemy", Team = "enemy", Stats = { ["health"] = enemyHealth, ["agility"] = 5 } }
            }
        });
        return project;
    }
}
