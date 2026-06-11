using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameMvpOrchestratorTests
{
    [Fact]
    public void EmptyProject_ProducesLowCompletionAndEarlyRecommendation()
    {
        var project = new GameProjectData();

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.True(report.CompletionPercent < 35);
        Assert.Contains(report.OverallStatus, new[] { GameMvpReadinessStatus.Empty, GameMvpReadinessStatus.Skeleton });
        Assert.Equal("design_profile", report.NextRecommendedStage);
        Assert.Contains(report.Issues, x => x.Code == "meta_id_empty");
    }

    [Fact]
    public void DesignProfileWithoutContent_RecommendsStatsResources()
    {
        var project = new GameProjectData
        {
            Meta = new GameMeta { Id = "game_design", Title = "Design", Genre = "городское фэнтези", Description = "Магическая история с инвентарём." },
            DesignProfile = new GameDesignProfile { InitialIdea = "игра про побег через магический город" }
        };

        var service = new GameDesignInterviewService();
        service.SetUserAnswer(project.DesignProfile, "genre", "городское фэнтези");
        service.SetUserAnswer(project.DesignProfile, "combat_style", "нет боёв");

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.Equal("stats_resources", report.NextRecommendedStage);
        Assert.Equal("missing_mvp_stats", report.NextRecommendedCategory);
    }

    [Fact]
    public void ProjectWithCoreButTooFewScenes_RecommendsScenes()
    {
        var project = CreateCoreProjectWithoutEnoughScenes();

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.Equal("scenes", report.NextRecommendedStage);
        Assert.True(report.NextRecommendedCount >= 6);
    }

    [Fact]
    public void CombatRelevantProjectWithoutEncounters_RecommendsEncounters()
    {
        var project = CreateCoreProjectWithoutEnoughScenes();
        AddScenes(project, 6);
        project.DesignProfile.InitialIdea = "текстовая игра с боями и тактическими сражениями";
        project.Combat = new GameCombatDefinition { Enabled = true };
        project.Actions.Add(new GameActionDefinition { Id = "strike", Name = "Strike", AvailableInCombat = true, Effects = { new GameEffect { Type = "combatDamage", Amount = 5 } } });

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.Equal("encounters", report.NextRecommendedStage);
    }

    [Fact]
    public void RandomnessRelevantProjectWithPoorCoverage_RecommendsRandomEvents()
    {
        var project = CreateCoreProjectWithoutEnoughScenes();
        AddScenes(project, 6);
        project.DesignProfile.InitialIdea = "путешествия, рандом и случайные события";
        project.WorldState.Enabled = true;
        project.WorldState.Time.Enabled = true;
        project.WorldState.Time.Segments.Add(new GameTimeSegmentDefinition { Id = "morning", Name = "Утро" });

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.Equal("random_events", report.NextRecommendedStage);
        Assert.Equal("controlled_random_events", report.NextRecommendedCategory);
    }

    [Fact]
    public void ContentRichCombatProject_RecommendsBalanceOrPlayableReview()
    {
        var project = CreateContentRichCombatProject();

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.True(report.NextRecommendedStage == "balance"
            || report.OverallStatus == GameMvpReadinessStatus.Playable
            || report.OverallStatus == GameMvpReadinessStatus.NeedsReview);
    }

    [Fact]
    public void BuildReadinessReport_DoesNotMutateOriginalProject()
    {
        var project = CreateContentRichCombatProject();
        var before = Serialize(project);

        _ = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.Equal(before, Serialize(project));
    }

    [Fact]
    public void CombatChoicePointingToStartScene_AddsBlockingIssue()
    {
        var project = CreateContentRichCombatProject();
        project.Scenes[0].Choices.Add(new GameChoice { Id = "choice_attack", Text = "Приготовиться к бою", NextSceneId = "scene_start" });

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.Contains(report.Issues, x => x.Code == "combat_choice_points_to_start_scene" && x.Severity == GameMvpReadinessSeverity.Error);
        Assert.True(report.HasBlockingProblems);
    }

    [Fact]
    public void CombatEncounterReachableThroughChoiceEncounterId_HasNoUnreachableCombatIssue()
    {
        var project = CreateContentRichCombatProject();
        project.Scenes[0].Choices.Add(new GameChoice { Id = "choice_attack", Text = "Приготовиться к бою", EncounterId = "bandit_fight" });

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.DoesNotContain(report.Issues, x => x.Code == "combat_encounter_unreachable");
    }

    [Fact]
    public void FormatReportForUi_ContainsRussianStageAndRecommendationText()
    {
        var project = CreateCoreProjectWithoutEnoughScenes();
        var service = new GameMvpOrchestratorService();
        var report = service.BuildReadinessReport(project);

        var text = service.FormatReportForUi(report);

        Assert.Contains("Стадии", text);
        Assert.Contains("Рекомендации", text);
        Assert.Contains("Следующий шаг", text);
    }

    [Fact]
    public void BuildCompactMvpSummary_IsCompactAndDoesNotIncludeFullRawSceneText()
    {
        var project = CreateContentRichCombatProject();
        project.Scenes[0].Text = new string('s', 9000);
        var service = new GameMvpOrchestratorService();
        var report = service.BuildReadinessReport(project);

        var summary = service.BuildCompactMvpSummary(project, report);

        Assert.Contains("nextRecommendedStage", summary);
        Assert.Contains("counts", summary);
        Assert.DoesNotContain(new string('s', 1000), summary);
        Assert.True(summary.Length < 20000);
    }

    private static GameProjectData CreateCoreProjectWithoutEnoughScenes()
    {
        var project = new GameProjectData
        {
            Summary = new GameProjectSummary { Id = "game_mvp", Title = "MVP", FolderName = "MVP" },
            Meta = new GameMeta
            {
                Id = "game_mvp",
                Title = "MVP",
                Genre = "приключение",
                Description = "Исследование города без боёв.",
                StartSceneId = "scene_start"
            },
            DesignProfile = new GameDesignProfile { InitialIdea = "городское приключение без боёв" },
            Brief = new ProjectBrief { Text = "Игрок исследует город и собирает ресурсы." },
            Concept = new GameConcept { Text = "Небольшой playable MVP." },
            MvpPlan = new GameMvpPlan { Text = "Нужны локации, сцены, предметы и действия." },
            WorldState = new GameWorldStateDefinition
            {
                Enabled = true,
                Time = new GameTimeSystemDefinition
                {
                    Enabled = true,
                    Segments = { new GameTimeSegmentDefinition { Id = "day", Name = "День" } }
                }
            }
        };
        project.Stats.AddRange(new[]
        {
            new GameStatDefinition { Id = "health", Name = "Health", IsResource = true },
            new GameStatDefinition { Id = "focus", Name = "Focus" },
            new GameStatDefinition { Id = "stamina", Name = "Stamina", IsResource = true },
            new GameStatDefinition { Id = "reputation", Name = "Reputation" }
        });
        project.Formulas.AddRange(new[]
        {
            new GameFormulaDefinition { Id = "check_focus", Name = "Focus", Expression = "focus + 1" },
            new GameFormulaDefinition { Id = "restore_stamina", Name = "Restore", Expression = "2" }
        });
        project.Actions.AddRange(new[]
        {
            new GameActionDefinition { Id = "search", Name = "Search", Effects = { new GameEffect { Type = "stat", TargetId = "focus", Amount = 1 } } },
            new GameActionDefinition { Id = "rest", Name = "Rest", Effects = { new GameEffect { Type = "stat", TargetId = "stamina", Amount = 1 } } },
            new GameActionDefinition { Id = "talk", Name = "Talk", Effects = { new GameEffect { Type = "stat", TargetId = "reputation", Amount = 1 } } }
        });
        project.Locations.AddRange(new[]
        {
            new GameLocation { Id = "square", Name = "Square" },
            new GameLocation { Id = "market", Name = "Market" },
            new GameLocation { Id = "gate", Name = "Gate" }
        });
        project.Items.AddRange(new[]
        {
            new GameItemDefinition { Id = "map", Name = "Map" },
            new GameItemDefinition { Id = "coin", Name = "Coin" },
            new GameItemDefinition { Id = "cloak", Name = "Cloak" },
            new GameItemDefinition { Id = "meal", Name = "Meal" },
            new GameItemDefinition { Id = "letter", Name = "Letter" }
        });
        project.Scenes.Add(new GameScene { Id = "scene_start", Title = "Start", Text = "Start" });
        return project;
    }

    private static GameProjectData CreateContentRichCombatProject()
    {
        var project = CreateCoreProjectWithoutEnoughScenes();
        AddScenes(project, 6);
        project.DesignProfile.InitialIdea = "боевое приключение с прогрессией";
        project.Mechanics.EnableProgression = true;
        project.Mechanics.Experience.EnablePlayerExperience = true;
        project.Combat = new GameCombatDefinition { Enabled = true, PlayerHealthStatId = "health" };
        project.Actions.Add(new GameActionDefinition
        {
            Id = "strike",
            Name = "Strike",
            AvailableInCombat = true,
            ActorTeam = "player",
            TargetScope = "enemy",
            Effects = { new GameEffect { Type = "combatDamage", Amount = 5 } }
        });
        project.Encounters.Add(new GameEncounterDefinition
        {
            Id = "bandit_fight",
            Name = "Bandit fight",
            Kind = "combat",
            SceneId = "scene_start",
            Combatants =
            {
                new GameEncounterCombatantDefinition { Id = "player", Name = "Player", Team = "player", IsPlayer = true, Stats = { ["health"] = 20 }, ActionIds = { "strike" } },
                new GameEncounterCombatantDefinition { Id = "bandit", Name = "Bandit", Team = "enemy", Stats = { ["health"] = 10 } }
            }
        });
        project.ProgressionNodes.AddRange(new[]
        {
            new GameProgressionNodeDefinition { Id = "node_focus", Name = "Focus", IsUnlockedByDefault = true },
            new GameProgressionNodeDefinition { Id = "node_strike", Name = "Strike", ParentNodeIds = { "node_focus" } },
            new GameProgressionNodeDefinition { Id = "node_guard", Name = "Guard", ParentNodeIds = { "node_focus" } }
        });
        return project;
    }

    private static void AddScenes(GameProjectData project, int targetCount)
    {
        for (var i = project.Scenes.Count; i < targetCount; i++)
        {
            project.Scenes.Add(new GameScene
            {
                Id = "scene_" + i,
                Title = "Scene " + i,
                Text = "Scene text " + i,
                LocationId = project.Locations[i % project.Locations.Count].Id
            });
        }
    }

    private static string Serialize(GameProjectData project)
    {
        return JsonSerializer.Serialize(project, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
