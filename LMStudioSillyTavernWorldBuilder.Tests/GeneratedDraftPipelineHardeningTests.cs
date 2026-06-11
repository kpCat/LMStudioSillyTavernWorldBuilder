using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GeneratedDraftPipelineHardeningTests
{
    [Fact]
    public void PartialDraftWithoutScenes_DoesNotCreateFallbackScene()
    {
        var current = TestProjects.CreatePlayableProject();
        var generated = new GameProjectData
        {
            Stats = { new GameStatDefinition { Id = "stamina", Name = "Выносливость", InitialValue = 20 } }
        };
        var repair = new GameProjectRepairService();

        repair.ApplySafeRepairs(generated, _ => { }, GameProjectRepairMode.GeneratedPartialDraft);

        Assert.Empty(generated.Scenes);
        Assert.DoesNotContain(generated.Scenes, x => x.Id == "scene_start");
        Assert.DoesNotContain(generated.Scenes, x => x.Text.Contains("Fallback scene", StringComparison.OrdinalIgnoreCase));

        current.Stats.AddRange(generated.Stats);
        Assert.Contains(current.Stats, x => x.Id == "stamina");
        Assert.Equal(2, current.Scenes.Count);
    }

    [Fact]
    public void MvpOrchestrator_BlocksTechnicalFallbackStartScene()
    {
        var project = CreateRandomnessMvpProject();
        project.Scenes[0].Text = "Fallback scene created because generated content did not contain scenes.";
        project.Scenes[0].Choices.Clear();

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);

        Assert.True(report.HasBlockingProblems);
        Assert.Contains(report.Issues, x => x.Code == "start_scene_is_fallback" && x.Severity == GameMvpReadinessSeverity.Error);
        Assert.False(Assert.Single(report.Stages, x => x.Stage == "scenes").ExistingCount == project.Scenes.Count);
    }

    [Fact]
    public void GeneratedSceneNormalizer_FillsMissingTitleFromText()
    {
        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonForTests("""
        {
          "scenes": [
            {
              "id": "scene_border",
              "text": "Вы стоите у пограничного разрыва между районами Светограда."
            }
          ]
        }
        """, "scenes", new List<string>());

        var project = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Normalized project JSON is empty.");

        Assert.Equal("Вы стоите у пограничного разрыва между районами Светогра...", Assert.Single(project.Scenes).Title);
    }

    [Fact]
    public void GeneratedEffectNormalizer_CleansStatusCurrencyAndFormulaReferences()
    {
        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonForTests("""
        {
          "currencies": [
            { "id": "fragments", "name": "Фрагменты" }
          ],
          "formulas": [
            { "id": "formula_gain", "name": "Gain", "expression": "stat.strength + stat.agility" }
          ],
          "actions": [
            {
              "id": "collect",
              "name": "Собрать",
              "effects": [
                { "type": "status", "targetId": "syncing", "amount": 1 },
                { "type": "item", "targetId": "fragments", "amount": 2 },
                { "type": "stat", "targetId": "will", "formulaExpression": "formula_gain" }
              ]
            }
          ]
        }
        """, "gameplay-actions", new List<string>());

        var project = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Normalized project JSON is empty.");
        var effects = Assert.Single(project.Actions).Effects;

        Assert.Contains(effects, x => x.Type == "statusEffect");
        Assert.Contains(effects, x => x.Type == "currency" && x.TargetId == "fragments");
        Assert.Contains(effects, x => x.FormulaId == "formula_gain" && string.IsNullOrWhiteSpace(x.FormulaExpression));
        Assert.Equal("stat.stamina + stat.will", Assert.Single(project.Formulas).Expression);
    }

    [Fact]
    public void PromptJsonSerialization_PreservesCyrillic()
    {
        var json = JsonSerializer.Serialize(new { Text = "Сгенерируй событие" }, GenerationJsonOptions.PromptJson);

        Assert.Contains("Сгенерируй событие", json);
        Assert.DoesNotContain("\\u0421", json);
    }

    [Fact]
    public void RandomDirectorUserPrompt_PreservesCyrillic()
    {
        var project = TestProjects.CreateWorldStateProject();
        project.Meta.Title = "Светоград";
        var service = new GameRandomDirectorService();
        var prompt = service.BuildGenerationUserPrompt(project, service.BuildReport(project), 4);

        Assert.Contains("Светоград", prompt);
        Assert.DoesNotContain("\\u0421", prompt);
    }

    [Fact]
    public void RandomEventsAliases_NormalizeIntoWorldStateAmbientEvents()
    {
        var warnings = new List<string>();
        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonForTests("""
        {
          "randomEvents": [
            {
              "title": "Туман у ворот",
              "description": "Туман меняет маршрут.",
              "chance": 0.25,
              "trigger": { "type": "travel", "targetId": "gate" },
              "effect": { "type": "log", "text": "Туман сгущается." }
            }
          ]
        }
        """, "random-director", warnings);

        var project = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Normalized project JSON is empty.");

        var ambientEvent = Assert.Single(project.WorldState.AmbientEvents);
        Assert.NotEmpty(ambientEvent.Id);
        Assert.Equal("Туман у ворот", ambientEvent.Name);
        Assert.Equal("Туман меняет маршрут.", ambientEvent.Text);
        Assert.Equal(25, ambientEvent.ChancePercent);
        Assert.Equal("travel", ambientEvent.Trigger);
        Assert.Contains(ambientEvent.Effects, x => x.Type == "log" && x.Text == "Туман сгущается.");
        Assert.DoesNotContain("randomEvents", normalized);
        Assert.Contains("Туман у ворот", normalized);
        Assert.Contains(warnings, x => x.Contains("moved to $.worldState.ambientEvents", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RandomEventsNormalizer_FillsMissingNameFromText()
    {
        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonForTests("""
        {
          "ambientEvents": [
            {
              "id": "fog",
              "text": "В переулке вспыхивает зелёный свет.",
              "probability": 50,
              "trigger": "actionEnd"
            }
          ]
        }
        """, "random-director", new List<string>());

        var project = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Normalized project JSON is empty.");

        var ambientEvent = Assert.Single(project.WorldState.AmbientEvents);
        Assert.Equal("В переулке вспыхивает зелёный свет.", ambientEvent.Name);
        Assert.Equal(50, ambientEvent.ChancePercent);
        Assert.Equal("action", ambientEvent.Trigger);
    }

    [Fact]
    public void RandomEventsDraftImprovesMvpStage()
    {
        var before = CreateRandomnessMvpProject();
        var after = Clone(before);
        for (var i = 0; i < 4; i++)
        {
            after.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
            {
                Id = "event_" + i,
                Name = "Событие " + i,
                Text = "Описание события " + i
            });
        }

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService());

        Assert.False(pipeline.IsGeneratedDraftNoOpForStageForTests(before, after, "random-director"));
        var report = new GameMvpOrchestratorService().BuildReadinessReport(after);
        Assert.True(Assert.Single(report.Stages, x => x.Stage == "random_events").IsSatisfied);
    }

    [Fact]
    public void NoOpDraftForRequestedStage_IsRejectedByStageGate()
    {
        var before = CreateRandomnessMvpProject();
        var after = Clone(before);
        after.Items.Add(new GameItemDefinition { Id = "unrelated_item", Name = "Лишний предмет" });
        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService());

        Assert.True(pipeline.IsGeneratedDraftNoOpForStageForTests(before, after, "random-director"));
    }

    [Fact]
    public void ProgressionPrompt_UsesRealModelFields()
    {
        var prompt = Prompts.GenerateProgressionBatch.SystemPrompt;

        Assert.Contains("progressionNodes", prompt);
        Assert.Contains("mechanics.enableProgression", prompt);
        Assert.Contains("mechanics.experience", prompt);
        Assert.Contains("unlockRequirements", prompt);
        Assert.Contains("unlockCosts", prompt);
        Assert.Contains("unlockEffects", prompt);
        Assert.Contains("Do not output invented top-level progression", prompt);
    }

    private static GameProjectData CreateRandomnessMvpProject()
    {
        return new GameProjectData
        {
            Meta = new GameMeta { Id = "random_game", Title = "Random Game", StartSceneId = "scene_start" },
            DesignProfile = new GameDesignProfile { InitialIdea = "travel random events" },
            Brief = new ProjectBrief { Text = "Путешествие со случайными событиями." },
            WorldState = new GameWorldStateDefinition { Enabled = true },
            Stats =
            {
                new GameStatDefinition { Id = "health", Name = "Health" },
                new GameStatDefinition { Id = "stamina", Name = "Stamina" },
                new GameStatDefinition { Id = "will", Name = "Will" },
                new GameStatDefinition { Id = "focus", Name = "Focus" }
            },
            Formulas =
            {
                new GameFormulaDefinition { Id = "check", Name = "Check", Expression = "will + 1" },
                new GameFormulaDefinition { Id = "restore", Name = "Restore", Expression = "stamina + 1" }
            },
            Actions =
            {
                new GameActionDefinition { Id = "search", Name = "Search" },
                new GameActionDefinition { Id = "rest", Name = "Rest" },
                new GameActionDefinition { Id = "talk", Name = "Talk" }
            },
            Locations =
            {
                new GameLocation { Id = "gate", Name = "Gate" },
                new GameLocation { Id = "market", Name = "Market" },
                new GameLocation { Id = "tower", Name = "Tower" }
            },
            Items =
            {
                new GameItemDefinition { Id = "map", Name = "Map" },
                new GameItemDefinition { Id = "coin", Name = "Coin" },
                new GameItemDefinition { Id = "cloak", Name = "Cloak" },
                new GameItemDefinition { Id = "meal", Name = "Meal" },
                new GameItemDefinition { Id = "letter", Name = "Letter" }
            },
            Scenes =
            {
                new GameScene { Id = "scene_start", Title = "Start", Text = "Start" },
                new GameScene { Id = "scene_1", Title = "Scene 1", Text = "Scene 1" },
                new GameScene { Id = "scene_2", Title = "Scene 2", Text = "Scene 2" },
                new GameScene { Id = "scene_3", Title = "Scene 3", Text = "Scene 3" },
                new GameScene { Id = "scene_4", Title = "Scene 4", Text = "Scene 4" },
                new GameScene { Id = "scene_5", Title = "Scene 5", Text = "Scene 5" }
            }
        };
    }

    private static GameProjectData Clone(GameProjectData project)
    {
        return JsonSerializer.Deserialize<GameProjectData>(
            JsonSerializer.Serialize(project, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
