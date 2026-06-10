using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;
using System.Text.Json;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameProjectValidationTests
{
    [Fact]
    public void Validator_FailsWhenStartSceneMissing()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Meta.StartSceneId = "missing";

        var result = new GameProjectValidator().Validate(project);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("StartSceneId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_FailsWhenChoicePointsToMissingScene()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Scenes[0].Choices[0].NextSceneId = "missing";

        var result = new GameProjectValidator().Validate(project);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("missing scene", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Repair_SetsStartSceneToFirstSceneWhenMissing()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Meta.StartSceneId = "missing";

        new GameProjectRepairService().ApplySafeRepairs(project, _ => { });

        Assert.Equal("scene_start", project.Meta.StartSceneId);
    }

    [Fact]
    public void ApplyGeneratedProjectJson_DoesNotLoseProjectPath()
    {
        var current = TestProjects.CreatePlayableProject();
        current.Summary.ProjectPath = @"C:\Games\Existing";
        var generated = TestProjects.CreatePlayableProject();
        generated.Summary.ProjectPath = "";

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new LMStudioSillyTavernWorldBuilder.Storage.GameStorageService());
        pipeline.ApplyGeneratedProjectJson(current, System.Text.Json.JsonSerializer.Serialize(generated), _ => { });

        Assert.Equal(@"C:\Games\Existing", current.Summary.ProjectPath);
    }

    [Fact]
    public void ApplyGeneratedProjectJson_DoesNotDeleteExistingSceneWhenGeneratedOmitsIt()
    {
        var current = TestProjects.CreatePlayableProject();
        var generated = TestProjects.CreatePlayableProject();
        generated.Scenes.RemoveAll(x => x.Id == "scene_next");

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService());
        pipeline.ApplyGeneratedProjectJson(current, System.Text.Json.JsonSerializer.Serialize(generated), _ => { });

        Assert.Contains(current.Scenes, x => x.Id == "scene_next");
    }

    [Fact]
    public void ApplyGeneratedProjectJson_UpsertsSceneById()
    {
        var current = TestProjects.CreatePlayableProject();
        var generated = TestProjects.CreatePlayableProject();
        generated.Scenes[0].Title = "Updated";

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService());
        pipeline.ApplyGeneratedProjectJson(current, System.Text.Json.JsonSerializer.Serialize(generated), _ => { });

        Assert.Equal("Updated", current.Scenes.Single(x => x.Id == "scene_start").Title);
    }

    [Fact]
    public async Task ApplyGeneratedProjectJson_SavesDraftRawOutput()
    {
        var current = TestProjects.CreatePlayableProject();
        current.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var generated = TestProjects.CreatePlayableProject();
        var raw = System.Text.Json.JsonSerializer.Serialize(generated, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService());
        await pipeline.ApplyGeneratedProjectJsonAsync(current, raw, _ => { });

        Assert.True(Directory.EnumerateFiles(Path.Combine(current.Summary.ProjectPath, "drafts"), "raw-output.txt", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task ApplyGeneratedProjectJson_WhenValidationFails_DoesNotMutateCurrentProject()
    {
        var current = TestProjects.CreatePlayableProject();
        current.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var generated = TestProjects.CreatePlayableProject();
        generated.Scenes.Add(new GameScene
        {
            Id = "scene_bad",
            Title = "Bad",
            Text = "Invalid scene",
            Choices =
            {
                new GameChoice { Id = "choice_missing", Text = "Missing", NextSceneId = "scene_missing" }
            }
        });
        var logs = new List<string>();
        var raw = System.Text.Json.JsonSerializer.Serialize(generated, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService());
        await pipeline.ApplyGeneratedProjectJsonAsync(current, raw, logs.Add);

        Assert.DoesNotContain(current.Scenes, x => x.Id == "scene_bad");
        Assert.Equal("scene_start", current.Meta.StartSceneId);
        Assert.Contains(logs, x => x.Contains("Generated project error", StringComparison.OrdinalIgnoreCase));
        Assert.True(Directory.EnumerateFiles(Path.Combine(current.Summary.ProjectPath, "drafts"), "raw-output.txt", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task ApplyGeneratedProjectJson_WhenValidationPasses_AppliesAfterValidation()
    {
        var current = TestProjects.CreatePlayableProject();
        current.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var generated = TestProjects.CreatePlayableProject();
        generated.Scenes.Add(new GameScene
        {
            Id = "scene_new",
            Title = "New",
            Text = "New scene",
            Choices =
            {
                new GameChoice { Id = "choice_back", Text = "Back", NextSceneId = "scene_start" }
            }
        });
        var raw = System.Text.Json.JsonSerializer.Serialize(generated, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService());
        await pipeline.ApplyGeneratedProjectJsonAsync(current, raw, _ => { });
        var manifest = await ReadLatestDraftManifestAsync(current);

        Assert.Contains(current.Scenes, x => x.Id == "scene_new");
        Assert.Contains(current.Scenes, x => x.Id == "scene_next");
        Assert.NotEmpty(manifest.Files);
        Assert.All(manifest.Files, file => Assert.Equal("Applied", file.Status));
    }

    [Fact]
    public async Task ApplyGeneratedProjectJson_FailedValidation_DoesNotMarkDraftApplied()
    {
        var current = TestProjects.CreatePlayableProject();
        current.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var generated = TestProjects.CreatePlayableProject();
        generated.Scenes[0].Choices[0].NextSceneId = "scene_missing";
        var raw = System.Text.Json.JsonSerializer.Serialize(generated, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService());
        await pipeline.ApplyGeneratedProjectJsonAsync(current, raw, _ => { });
        var manifest = await ReadLatestDraftManifestAsync(current);

        Assert.NotEmpty(manifest.Files);
        Assert.DoesNotContain(manifest.Files, file => string.Equals(file.Status, "Applied", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GeneratedActionsJsonNormalizer_AllowsStringAmountsBeforeDeserialize()
    {
        var raw = """
        {
          "actions": [
            {
              "id": "tune_metamodule",
              "name": "Tune metamodule",
              "costs": [
                { "type": "currency", "targetId": "credits", "amount": "random(0, 15)" },
                { "type": "variable", "targetId": "metamodule_sync", "amount": "3" }
              ],
              "effects": [
                { "type": "skillExperience", "targetId": "skill_metamodule_tuning", "amount": "5 + dice(1, 4)" },
                { "type": "variable", "targetId": "reputation_svetograd", "amount": "dice(-2, 4)" },
                { "type": "stat", "targetId": "stability", "amount": "formula_stability_drain" }
              ]
            }
          ],
          "formulas": [
            { "id": "formula_stability_drain", "expression": "0" }
          ]
        }
        """;
        var warnings = new List<string>();

        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonAmountsForTests(raw, warnings);
        var project = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(project);
        var action = Assert.Single(project!.Actions);
        Assert.Equal(0, action.Costs[0].Amount);
        Assert.Equal("random(0, 15)", action.Costs[0].FormulaExpression);
        Assert.Equal(3, action.Costs[1].Amount);
        Assert.Equal(0, action.Effects[0].Amount);
        Assert.Equal("5 + dice(1, 4)", action.Effects[0].FormulaExpression);
        Assert.Equal(0, action.Effects[1].Amount);
        Assert.Equal("dice(-2, 4)", action.Effects[1].FormulaExpression);
        Assert.Equal(0, action.Effects[2].Amount);
        Assert.Equal("formula_stability_drain", action.Effects[2].FormulaId);
        Assert.True(warnings.Count >= 4);
    }

    [Fact]
    public void GeneratedWorldStateJsonNormalizer_AllowsObjectTriggersBeforeDeserialize()
    {
        var raw = """
        {
          "worldState": {
            "enabled": true,
            "genreProfile": "fantasy",
            "aspects": [
              {
                "id": "aspect_border_instability",
                "name": "Border instability",
                "stateId": "stable",
                "states": [
                  { "id": "stable", "name": "Stable" },
                  { "id": "fluctuating", "name": "Fluctuating" }
                ]
              }
            ],
            "ambientEvents": [
              {
                "id": "event_border_shimmer",
                "name": "Border shimmer",
                "description": "The border shimmers.",
                "trigger": { "type": "locationState", "targetId": "aspect_border_instability", "operator": "==", "value": "fluctuating" },
                "probability": 0.3,
                "requirements": [
                  { "type": "locationState", "targetId": "aspect_border_instability", "operator": "==", "value": "fluctuating" }
                ]
              },
              {
                "id": "event_sync_whisper",
                "name": "Sync whisper",
                "text": "The module whispers.",
                "trigger": "actionEnd",
                "chancePercent": 15
              },
              {
                "id": "event_missing_trigger",
                "name": "Missing trigger",
                "text": "Something happens."
              }
            ],
            "rules": [
              {
                "id": "rule_instability_drain",
                "name": "Instability drain",
                "probability": 0.5,
                "requirements": [
                  { "type": "worldAspect", "targetId": "aspect_border_instability", "operator": "==", "value": "unstable" },
                  { "type": "variable", "targetId": "metamodule_sync", "operator": ">=", "value": "10" }
                ],
                "effect": { "type": "stat", "targetId": "stability", "formulaExpression": "-5" }
              }
            ]
          }
        }
        """;
        var warnings = new List<string>();

        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonAmountsForTests(raw, warnings);
        var project = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(project);
        var worldState = project!.WorldState;
        Assert.True(worldState.Enabled);
        Assert.Equal("stable", Assert.Single(worldState.Aspects).DefaultStateId);
        Assert.Equal("travel", worldState.AmbientEvents[0].Trigger);
        Assert.Equal(30, worldState.AmbientEvents[0].ChancePercent);
        Assert.Equal("The border shimmers.", worldState.AmbientEvents[0].Text);
        Assert.Equal("action", worldState.AmbientEvents[1].Trigger);
        Assert.Equal("turnEnd", worldState.AmbientEvents[2].Trigger);
        Assert.Equal("worldAspect", worldState.AmbientEvents[0].Requirements[0].Type);
        Assert.Equal(0, worldState.AmbientEvents[0].Requirements[0].Value);
        Assert.Equal("fluctuating", worldState.AmbientEvents[0].Requirements[0].StringValue);
        Assert.Equal("turnEnd", worldState.Rules[0].Trigger);
        Assert.Equal(50, worldState.Rules[0].ChancePercent);
        Assert.Equal(0, worldState.Rules[0].Requirements[0].Value);
        Assert.Equal("unstable", worldState.Rules[0].Requirements[0].StringValue);
        Assert.Equal(10, worldState.Rules[0].Requirements[1].Value);
        Assert.Single(worldState.Rules[0].Effects);
        Assert.Equal("stability", worldState.Rules[0].Effects[0].TargetId);
        Assert.True(warnings.Count >= 6);
    }

    [Fact]
    public void GeneratedWorldStateJsonNormalizer_AddsMissingAspectStates()
    {
        var raw = """
        {
          "worldState": {
            "enabled": true,
            "genreProfile": "fantasy",
            "time": {
              "enabled": true,
              "startSegmentId": "hour_00",
              "segments": [
                { "id": "hour_00", "name": "Midnight" }
              ]
            },
            "aspects": [
              { "id": "aspect_border_instability", "name": "Border", "stateId": "stable" },
              { "id": "aspect_world_tension", "name": "Tension", "defaultStateId": "low" }
            ],
            "ambientEvents": [
              {
                "id": "event_border_hum",
                "name": "Border hum",
                "trigger": "turnEnd",
                "chancePercent": 30,
                "text": "The border hums.",
                "requirements": [
                  { "type": "worldAspect", "targetId": "aspect_border_instability", "operator": "==", "value": 0, "stringValue": "unstable" }
                ]
              }
            ],
            "rules": [
              {
                "id": "rule_border_drain",
                "name": "Border drain",
                "trigger": "turnEnd",
                "effects": [
                  { "type": "worldAspect", "targetId": "aspect_world_tension", "amount": 0, "stringValue": "high" }
                ],
                "requirements": [
                  { "type": "worldAspect", "targetId": "aspect_border_instability", "operator": "==", "value": 0, "stringValue": "unstable" }
                ]
              }
            ]
          }
        }
        """;
        var warnings = new List<string>();

        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonAmountsForTests(raw, warnings);
        var project = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var result = new GameProjectValidator().Validate(project!);

        Assert.NotNull(project);
        var border = Assert.Single(project!.WorldState.Aspects, x => x.Id == "aspect_border_instability");
        Assert.Equal("stable", border.DefaultStateId);
        Assert.Contains(border.States, x => x.Id == "stable");
        Assert.Contains(border.States, x => x.Id == "unstable");
        var tension = Assert.Single(project.WorldState.Aspects, x => x.Id == "aspect_world_tension");
        Assert.Contains(tension.States, x => x.Id == "low");
        Assert.Contains(tension.States, x => x.Id == "high");
        Assert.DoesNotContain(result.Errors, x => x.Contains("missing DefaultStateId", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Errors, x => x.Contains("points to missing state", StringComparison.OrdinalIgnoreCase));
        Assert.True(warnings.Any(x => x.Contains("added missing world aspect state", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void CloneService_PreservesIdentityOnCopyMutableData()
    {
        var target = TestProjects.CreatePlayableProject();
        target.Summary.Id = "summary_current";
        target.Summary.ProjectPath = @"C:\Games\Current";
        target.Summary.FolderName = "CurrentFolder";
        target.Meta.Id = "meta_current";
        var source = TestProjects.CreatePlayableProject();
        source.Summary.Id = "summary_generated";
        source.Summary.ProjectPath = @"C:\Games\Generated";
        source.Summary.FolderName = "GeneratedFolder";
        source.Meta.Id = "meta_generated";
        source.Meta.Title = "Generated Title";
        source.Scenes.Add(new GameScene { Id = "scene_new", Title = "New", Text = "New scene" });

        new GameProjectCloneService().CopyMutableData(source, target);

        Assert.Equal("summary_current", target.Summary.Id);
        Assert.Equal(@"C:\Games\Current", target.Summary.ProjectPath);
        Assert.Equal("CurrentFolder", target.Summary.FolderName);
        Assert.Equal("meta_current", target.Meta.Id);
        Assert.Equal("Generated Title", target.Meta.Title);
        Assert.Contains(target.Scenes, x => x.Id == "scene_new");
    }

    [Fact]
    public void Validator_WarnsAboutManifestMissingFile()
    {
        var folder = TestPaths.CreateTempDirectory();
        var manifest = new GameProjectManifest { Scenes = { "data/scenes/missing.json" } };

        var result = new GameProjectValidator().ValidateStorage(folder, manifest);

        Assert.Contains(result.Warnings, x => x.Contains("missing file", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_WarnsAboutOrphanFile()
    {
        var folder = TestPaths.CreateTempDirectory();
        Directory.CreateDirectory(Path.Combine(folder, "data", "scenes"));
        File.WriteAllText(Path.Combine(folder, "data", "scenes", "orphan.json"), "{}");

        var result = new GameProjectValidator().ValidateStorage(folder, new GameProjectManifest());

        Assert.Contains(result.Warnings, x => x.Contains("orphan", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_WarnsAboutMissingSelectedImage()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        project.ImagePrompts.Add(TestProjects.CreateScenePrompt());
        project.ImagePrompts[0].SelectedImagePath = "assets/scenes/missing.png";

        var result = new GameProjectValidator().Validate(project);

        Assert.Contains(result.Warnings, x => x.Contains("selected image", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_AllowsRuntimeSupportedMechanicTypes()
    {
        var project = TestProjects.CreateAdvancedProject();
        project.Formulas.Add(new GameFormulaDefinition { Id = "can_act", Expression = "stat.will + 1" });
        project.StatusEffects.Add(new GameStatusEffectDefinition { Id = "focused", Name = "Focused" });
        project.ProgressionNodes.Add(new GameProgressionNodeDefinition { Id = "node_one", Name = "Node One" });
        project.Actions.Add(new GameActionDefinition
        {
            Id = "mechanics_action",
            Name = "Mechanics Action",
            Requirements =
            {
                new GameRequirement { Type = "status", TargetId = "focused", Operator = ">=", Value = 1 },
                new GameRequirement { Type = "progression", TargetId = "node_one", Operator = ">=", Value = 1 },
                new GameRequirement { Type = "formula", FormulaId = "can_act", Operator = ">=", Value = 1 },
                new GameRequirement { Type = "resource", TargetId = "mana", Operator = ">=", Value = 1 },
                new GameRequirement { Type = "effectiveStat", TargetId = "will", Operator = ">=", Value = 1 }
            },
            Effects =
            {
                new GameEffect { Type = "status", StatusEffectId = "focused" },
                new GameEffect { Type = "statusEffect", TargetId = "focused" },
                new GameEffect { Type = "progression", TargetId = "node_one" },
                new GameEffect { Type = "unlockProgression", TargetId = "node_one" }
            }
        });
        project.Scenes[0].Choices.Add(new GameChoice
        {
            Id = "choice_mechanics",
            Text = "Mechanics",
            Conditions =
            {
                new GameCondition { Type = "status", TargetId = "focused", Operator = ">=", Value = 1 },
                new GameCondition { Type = "progression", TargetId = "node_one", Operator = ">=", Value = 1 },
                new GameCondition { Type = "resource", TargetId = "mana", Operator = ">=", Value = 1 },
                new GameCondition { Type = "effectiveStat", TargetId = "will", Operator = ">=", Value = 1 }
            }
        });

        var result = new GameProjectValidator().Validate(project);

        Assert.DoesNotContain(result.Warnings, x => x.Contains("Unknown or unresolved", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_WarnsAboutRandomDiceInRequirementsAndCostsButNotEffectError()
    {
        var project = TestProjects.CreateAdvancedProject();
        project.Formulas.Add(new GameFormulaDefinition { Id = "random_cost", Expression = "random(1, 3)" });
        project.Actions.Add(new GameActionDefinition
        {
            Id = "random_action",
            Name = "Random Action",
            Requirements =
            {
                new GameRequirement { Type = "formula", FormulaExpression = "dice(1, 6)", Operator = ">=", Value = 1 }
            },
            Costs =
            {
                new GameCost { Type = "currency", TargetId = "gold", FormulaId = "random_cost" }
            },
            Effects =
            {
                new GameEffect { Type = "variable", TargetId = "alarm", FormulaExpression = "random(1, 4)" }
            }
        });

        var result = new GameProjectValidator().Validate(project);

        Assert.Contains(result.Warnings, x => x.Contains("Requirement uses random()/dice()", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, x => x.Contains("Cost uses random()/dice()", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Errors, x => x.Contains("random", StringComparison.OrdinalIgnoreCase) || x.Contains("dice", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_ErrorsWhenWorldStateStartSegmentMissing()
    {
        var project = TestProjects.CreateWorldStateProject();
        project.WorldState.Time.StartSegmentId = "missing";

        var result = new GameProjectValidator().Validate(project);

        Assert.Contains(result.Errors, x => x.Contains("StartSegmentId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_ErrorsWhenWorldAspectStateEffectMissing()
    {
        var project = TestProjects.CreateWorldStateProject();
        project.Actions.Add(new GameActionDefinition
        {
            Id = "bad_weather",
            Name = "Bad Weather",
            Effects = { new GameEffect { Type = "worldState", TargetId = "weather", StringValue = "storm" } }
        });

        var result = new GameProjectValidator().Validate(project);

        Assert.Contains(result.Errors, x => x.Contains("weather/storm", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_FantasyProfileWithoutAtmosphereWarnsButDoesNotError()
    {
        var project = TestProjects.CreatePlayableProject();
        project.WorldState.Enabled = true;
        project.WorldState.GenreProfile = "fantasy";
        project.WorldState.Aspects.Add(new GameWorldAspectDefinition
        {
            Id = "politics",
            Name = "Politics",
            Kind = "faction",
            States = { new GameWorldAspectStateDefinition { Id = "quiet", Name = "Quiet" } }
        });

        var result = new GameProjectValidator().Validate(project);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, x => x.Contains("Fantasy WorldState", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_CombatEncounterWithoutEnemyWarns()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Stats.Add(new GameStatDefinition { Id = "health", Name = "Health", InitialValue = 10 });
        project.Combat = new GameCombatDefinition { Enabled = true, PlayerHealthStatId = "health" };
        project.Encounters.Add(new GameEncounterDefinition
        {
            Id = "combat_without_enemy",
            Kind = "combat",
            Combatants =
            {
                new GameEncounterCombatantDefinition { Id = "player", Name = "Player", Team = "player", IsPlayer = true, Stats = { ["health"] = 10 } }
            }
        });

        var result = new GameProjectValidator().Validate(project);

        Assert.Contains(result.Warnings, x => x.Contains("no enemy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_CombatDamageEffectIsKnown()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Stats.Add(new GameStatDefinition { Id = "health", Name = "Health", InitialValue = 10 });
        project.Combat = new GameCombatDefinition { Enabled = true, PlayerHealthStatId = "health" };
        project.Actions.Add(new GameActionDefinition
        {
            Id = "strike",
            Name = "Strike",
            AvailableInCombat = true,
            TargetScope = "enemy",
            Effects = { new GameEffect { Type = "combatDamage", Amount = 3 } }
        });

        var result = new GameProjectValidator().Validate(project);

        Assert.DoesNotContain(result.Warnings, x => x.Contains("Unknown or unresolved effect 'combatDamage", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<GameDraftSession> ReadLatestDraftManifestAsync(GameProjectData project)
    {
        var manifestPath = Directory.EnumerateFiles(Path.Combine(project.Summary.ProjectPath, "drafts"), "draft-manifest.json", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .First();
        var json = await File.ReadAllTextAsync(manifestPath);
        return System.Text.Json.JsonSerializer.Deserialize<GameDraftSession>(json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Draft manifest JSON is empty.");
    }
}
