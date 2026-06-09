using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameBalanceSimulatorTests
{
    [Fact]
    public void BuildReport_ProducesCombatReportForSimpleEncounter()
    {
        var project = CreateCombatProject(damage: 10, enemyHealth: 5);
        var report = new GameBalanceSimulatorService().BuildReport(project, 3);

        var encounter = Assert.Single(report.Combat.Encounters);
        Assert.Equal("encounter_battle", encounter.EncounterId);
        Assert.Equal(3, encounter.Runs);
        Assert.True(encounter.Wins > 0);
        Assert.True(encounter.WinRatePercent > 0);
    }

    [Fact]
    public void BuildReport_DoesNotMutateOriginalProject()
    {
        var project = CreateCombatProject(damage: 10, enemyHealth: 5);
        var before = Serialize(project);

        _ = new GameBalanceSimulatorService().BuildReport(project, 5);

        Assert.Equal(before, Serialize(project));
    }

    [Fact]
    public void BuildReport_NoActionEncounterProducesWarningOrIssue()
    {
        var project = CreateCombatProject(damage: 0, enemyHealth: 20);
        project.Actions.Clear();
        project.Encounters[0].Combatants[0].ActionIds.Clear();
        project.Combat!.MaxRounds = 3;

        var report = new GameBalanceSimulatorService().BuildReport(project, 1);

        Assert.Contains(report.Combat.Encounters[0].Warnings, x => x.Contains("доступных combat action", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Issues, x => x.Scope == "combat");
    }

    [Fact]
    public void BuildReport_EconomyDetectsMissingSourcesSinksAndCurrencyMismatch()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Currencies.Add(new GameCurrencyDefinition { Id = "gold", Name = "Gold" });
        project.Items.Add(new GameItemDefinition { Id = "priced_without_currency", Name = "Priced", Value = 10 });
        project.Items.Add(new GameItemDefinition { Id = "wrong_currency", Name = "Wrong", Value = 5, CurrencyId = "silver" });

        var report = new GameBalanceSimulatorService().BuildReport(project, 1);

        Assert.Contains(report.Economy.Warnings, x => x.Contains("источники", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Economy.Warnings, x => x.Contains("CurrencyId", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Economy.Warnings, x => x.Contains("неизвестную валюту", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildReport_ProgressionDetectsDisconnectedProgressionOrMissingXpSource()
    {
        var project = TestProjects.CreateAdvancedProject();
        project.Mechanics.EnableProgression = true;
        project.ProgressionNodes.Add(new GameProgressionNodeDefinition { Id = "node_a", Name = "A" });
        project.ProgressionNodes.Add(new GameProgressionNodeDefinition { Id = "node_b", Name = "B" });
        project.ProgressionNodes.Add(new GameProgressionNodeDefinition { Id = "node_c", Name = "C" });

        var report = new GameBalanceSimulatorService().BuildReport(project, 1);

        Assert.True(report.Progression.DisconnectedNodeCount >= 3);
        Assert.Contains(report.Progression.Warnings, x => x.Contains("XP", StringComparison.OrdinalIgnoreCase) || x.Contains("Много progression nodes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildGenerationUserPrompt_IncludesBalanceReportAndNotHugeFullDump()
    {
        var project = CreateCombatProject(damage: 10, enemyHealth: 5);
        project.World.Summary = new string('w', 9000);
        project.Scenes[0].Text = new string('s', 9000);
        var service = new GameBalanceSimulatorService();
        var report = service.BuildReport(project, 1);

        var prompt = service.BuildGenerationUserPrompt(project, report);

        Assert.Contains("balanceReport", prompt);
        Assert.Contains("encounter_battle", prompt);
        Assert.Contains("partial GameProjectData", prompt);
        Assert.DoesNotContain(new string('w', 1000), prompt);
        Assert.DoesNotContain(new string('s', 1000), prompt);
    }

    private static GameProjectData CreateCombatProject(int damage, int enemyHealth)
    {
        var project = TestProjects.CreatePlayableProject();
        project.Stats.Add(new GameStatDefinition { Id = "health", Name = "Health", InitialValue = 20, MinValue = 0, MaxValue = 100, IsResource = true });
        project.Stats.Add(new GameStatDefinition { Id = "agility", Name = "Agility", InitialValue = 10 });
        project.Combat = new GameCombatDefinition
        {
            Enabled = true,
            PlayerHealthStatId = "health",
            DefaultDodgeChanceFormulaExpression = "0",
            DefaultBlockChanceFormulaExpression = "0",
            DefaultCritChanceFormulaExpression = "0",
            MaxRounds = 20
        };
        project.Actions.Add(new GameActionDefinition
        {
            Id = "strike",
            Name = "Strike",
            AvailableInCombat = true,
            ActorTeam = "player",
            TargetScope = "enemy",
            HitChanceFormulaExpression = "100",
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

    private static string Serialize(GameProjectData project)
    {
        return JsonSerializer.Serialize(project, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
