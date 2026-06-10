using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameDraftServiceTests
{
    [Fact]
    public async Task DraftService_SavesRawOutput()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var service = new GameDraftService();

        var draft = await service.SaveRawDraftAsync(project, "stage", "request", "raw text", CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, draft.RawOutputFile)));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "drafts", draft.SessionId, "draft-manifest.json")));
    }

    [Fact]
    public async Task DraftService_ExtractsGameProjectDataIntoEntityDrafts()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var generated = TestProjects.CreatePlayableProject();
        var raw = JsonSerializer.Serialize(generated, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var draft = await new GameDraftService().ExtractEntityDraftsAsync(project, "initial", raw, CancellationToken.None);

        Assert.Contains(draft.Files, x => x.EntityType == "scenes" && x.EntityId == "scene_start");
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, draft.Files.First(x => x.EntityType == "scenes").RelativePath)));
    }

    [Fact]
    public async Task DraftService_RevisionFixDraft_DoesNotApplyAutomatically()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var originalTitle = project.Scenes[0].Title;
        var generated = new GameProjectData
        {
            Scenes =
            {
                new GameScene
                {
                    Id = "scene_start",
                    Title = "Исправленная сцена",
                    Text = "Новый текст."
                }
            }
        };
        var raw = JsonSerializer.Serialize(generated, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var draft = await new GameDraftService().ExtractEntityDraftsAsync(project, "revision-fix", raw, CancellationToken.None);

        Assert.Contains(draft.Files, x => x.EntityType == "scenes" && x.EntityId == "scene_start");
        Assert.Equal(originalTitle, project.Scenes[0].Title);
        Assert.DoesNotContain(draft.Files, x => string.Equals(x.Status, "Applied", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WorldStateMerge_AddsPartialDataWithoutRemovingExistingTimeAndAspects()
    {
        var project = TestProjects.CreateWorldStateProject();
        var generated = new GameWorldStateDefinition
        {
            Enabled = true,
            Aspects =
            {
                new GameWorldAspectDefinition
                {
                    Id = "moon",
                    Name = "Moon",
                    DefaultStateId = "full",
                    States = { new GameWorldAspectStateDefinition { Id = "full", Name = "Full" } }
                }
            },
            AmbientEvents =
            {
                new GameAmbientEventDefinition { Id = "moon_glow", Name = "Moon glow", Text = "The moon glows." }
            }
        };

        GameWorldStateMergeService.MergeInto(project.WorldState, generated);

        Assert.Contains(project.WorldState.Time.Segments, x => x.Id == "morning");
        Assert.Contains(project.WorldState.Time.Segments, x => x.Id == "night");
        Assert.Contains(project.WorldState.Aspects, x => x.Id == "weather");
        Assert.Contains(project.WorldState.Aspects, x => x.Id == "moon");
        Assert.Contains(project.WorldState.AmbientEvents, x => x.Id == "moon_glow");
    }

    [Fact]
    public async Task ApplyDraft_WorldState_MergesPartialDataWithoutRemovingExistingData()
    {
        var project = TestProjects.CreateWorldStateProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var folder = Path.Combine(project.Summary.ProjectPath, "drafts", "draft_world_state", "world-state");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "world-state.json");
        var generated = new GameWorldStateDefinition
        {
            Enabled = true,
            Aspects =
            {
                new GameWorldAspectDefinition
                {
                    Id = "weather",
                    States = { new GameWorldAspectStateDefinition { Id = "fog", Name = "Fog" } }
                },
                new GameWorldAspectDefinition
                {
                    Id = "moon",
                    Name = "Moon",
                    DefaultStateId = "new",
                    States = { new GameWorldAspectStateDefinition { Id = "new", Name = "New moon" } }
                }
            },
            AmbientEvents =
            {
                new GameAmbientEventDefinition { Id = "moon_rise", Name = "Moon rise", Text = "The moon rises." }
            }
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(generated, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var draft = new GameDraftSession
        {
            SessionId = "draft_world_state",
            Validation = { IsValid = true },
            Files =
            {
                new GameDraftFile
                {
                    EntityType = "world-state",
                    EntityId = "world-state",
                    RelativePath = Path.GetRelativePath(project.Summary.ProjectPath, path).Replace('\\', '/')
                }
            }
        };

        await new GameDraftService().ApplyDraftAsync(project, draft, CancellationToken.None);

        Assert.Contains(project.WorldState.Time.Segments, x => x.Id == "morning");
        Assert.Contains(project.WorldState.Time.Segments, x => x.Id == "night");
        var weather = project.WorldState.Aspects.Single(x => x.Id == "weather");
        Assert.Contains(weather.States, x => x.Id == "clear");
        Assert.Contains(weather.States, x => x.Id == "rain");
        Assert.Contains(weather.States, x => x.Id == "fog");
        Assert.Contains(project.WorldState.Aspects, x => x.Id == "moon");
        Assert.Contains(project.WorldState.AmbientEvents, x => x.Id == "moon_rise");
    }

    [Fact]
    public async Task ApplyDraft_WhenLaterDraftFileIsBroken_DoesNotMutateProjectOrStatuses()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var originalName = project.Stats.Single(x => x.Id == "will").Name;
        var originalInitialValue = project.Stats.Single(x => x.Id == "will").InitialValue;
        var draftFolder = Path.Combine(project.Summary.ProjectPath, "drafts", "draft_broken");
        var statPath = Path.Combine(draftFolder, "stats", "will.json");
        var scenePath = Path.Combine(draftFolder, "scenes", "scene_start.json");
        Directory.CreateDirectory(Path.GetDirectoryName(statPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath)!);
        await File.WriteAllTextAsync(statPath, JsonSerializer.Serialize(
            new GameStatDefinition { Id = "will", Name = "Changed Will", InitialValue = 99 },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        await File.WriteAllTextAsync(scenePath, "{ invalid json");
        var draft = new GameDraftSession
        {
            SessionId = "draft_broken",
            Validation = { IsValid = true },
            Files =
            {
                CreateDraftFile(project, "stats", "will", statPath),
                CreateDraftFile(project, "scenes", "scene_start", scenePath)
            }
        };

        await Assert.ThrowsAsync<JsonException>(() => new GameDraftService().ApplyDraftAsync(project, draft, CancellationToken.None));

        var stat = project.Stats.Single(x => x.Id == "will");
        Assert.Equal(originalName, stat.Name);
        Assert.Equal(originalInitialValue, stat.InitialValue);
        Assert.DoesNotContain(draft.Files, x => string.Equals(x.Status, "Applied", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyDraft_WhenCandidateValidationFails_DoesNotMutateProjectOrStatuses()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var originalChoiceTarget = project.Scenes.Single(x => x.Id == "scene_start").Choices.Single(x => x.Id == "choice_go").NextSceneId;
        var draftFolder = Path.Combine(project.Summary.ProjectPath, "drafts", "draft_invalid_candidate", "scenes");
        Directory.CreateDirectory(draftFolder);
        var scenePath = Path.Combine(draftFolder, "scene_start.json");
        await File.WriteAllTextAsync(scenePath, JsonSerializer.Serialize(
            new GameScene
            {
                Id = "scene_start",
                Title = "Invalid Start",
                Text = "Invalid text",
                Choices =
                {
                    new GameChoice { Id = "choice_missing", Text = "Missing", NextSceneId = "scene_missing" }
                }
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var draft = new GameDraftSession
        {
            SessionId = "draft_invalid_candidate",
            Validation = { IsValid = true },
            Files = { CreateDraftFile(project, "scenes", "scene_start", scenePath) }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => new GameDraftService().ApplyDraftAsync(project, draft, CancellationToken.None));

        var scene = project.Scenes.Single(x => x.Id == "scene_start");
        Assert.Equal(originalChoiceTarget, scene.Choices.Single(x => x.Id == "choice_go").NextSceneId);
        Assert.DoesNotContain(draft.Files, x => string.Equals(x.Status, "Applied", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyDraft_WhenCandidateValidationPasses_AppliesAndMarksFiles()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var draftFolder = Path.Combine(project.Summary.ProjectPath, "drafts", "draft_valid", "stats");
        Directory.CreateDirectory(draftFolder);
        var statPath = Path.Combine(draftFolder, "will.json");
        await File.WriteAllTextAsync(statPath, JsonSerializer.Serialize(
            new GameStatDefinition { Id = "will", Name = "Focused Will", InitialValue = 12 },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var draft = new GameDraftSession
        {
            SessionId = "draft_valid",
            Validation = { IsValid = true },
            Files = { CreateDraftFile(project, "stats", "will", statPath) }
        };

        await new GameDraftService().ApplyDraftAsync(project, draft, CancellationToken.None);

        var stat = project.Stats.Single(x => x.Id == "will");
        Assert.Equal("Focused Will", stat.Name);
        Assert.Equal(12, stat.InitialValue);
        Assert.All(draft.Files, file => Assert.Equal("Applied", file.Status));
        var manifestPath = Path.Combine(project.Summary.ProjectPath, "drafts", draft.SessionId, "draft-manifest.json");
        Assert.True(File.Exists(manifestPath));
        var savedDraft = JsonSerializer.Deserialize<GameDraftSession>(
            await File.ReadAllTextAsync(manifestPath),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(savedDraft);
        Assert.All(savedDraft.Files, file => Assert.Equal("Applied", file.Status));
    }

    [Fact]
    public void CombatJsonNormalizer_MovesLegacyCollectionsAndFixesCombatShape()
    {
        var raw = """
        {
          "stats": [
            { "id": "health", "name": "Health" },
            { "id": "will", "name": "Will" },
            { "id": "stamina", "name": "Stamina" },
            { "id": "stability", "name": "Stability" }
          ],
          "combat": {
            "enabled": true,
            "playerHealthStatId": "health",
            "defaultHitFormula": "clamp(stat.agility * 1.2 + dice(1, 6), 1, 100)",
            "defaultBlockFormula": "clamp(stat.strength * 0.5, 1, 100)"
          },
          "combatActions": [
            {
              "id": "combat_strike",
              "name": "Удар",
              "kind": "active",
              "effects": [
                { "type": "combatDamage", "targetId": "target", "amount": 0 }
              ]
            }
          ],
          "combatEncounters": [
            {
              "id": "encounter_glitch",
              "name": "Схватка",
              "sceneId": "scene_start",
              "combatants": [
                {
                  "id": "player",
                  "name": "Носитель",
                  "role": "player",
                  "stats": { "health": "stat.health" }
                },
                {
                  "id": "enemy_glitch",
                  "name": "Искажение",
                  "role": "enemy",
                  "stats": { "health": 45 },
                  "actions": [
                    {
                      "id": "enemy_glitch_attack",
                      "name": "Искажающий удар",
                      "effects": [
                        { "type": "combatDamage", "targetId": "actor", "amount": 5 }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;
        var warnings = new List<string>();

        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonAmountsForTests(raw, warnings);
        var project = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(project);
        Assert.Equal(2, project!.Actions.Count);
        Assert.All(project.Actions, action => Assert.True(action.AvailableInCombat));
        Assert.All(project.Actions, action => Assert.Equal("enemy", action.TargetScope));
        Assert.Contains(project.Actions, action => action.Id == "enemy_glitch_attack" && action.ActorTeam == "enemy");
        var encounter = Assert.Single(project.Encounters);
        Assert.Equal("combat", encounter.Kind);
        var player = Assert.Single(encounter.Combatants, x => x.Id == "player");
        Assert.Equal("player", player.Team);
        Assert.True(player.IsPlayer);
        Assert.Equal(100, player.Stats["health"]);
        var enemy = Assert.Single(encounter.Combatants, x => x.Id == "enemy_glitch");
        Assert.Equal("enemy", enemy.Team);
        Assert.False(enemy.IsPlayer);
        Assert.Contains("enemy_glitch_attack", enemy.ActionIds);
        Assert.Equal("clamp(stat.will * 1.2 + dice(1, 6), 1, 100)", project.Combat!.DefaultHitChanceFormulaExpression);
        Assert.Equal("clamp(stat.stamina * 0.5, 1, 100)", project.Combat.DefaultBlockChanceFormulaExpression);
        Assert.DoesNotContain("combatActions", normalized);
        Assert.DoesNotContain("combatEncounters", normalized);
        Assert.Contains(warnings, x => x.Contains("combatActions", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(warnings, x => x.Contains("stat.health", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CombatPrompt_ProhibitsLegacyCombatCollections()
    {
        var prompt = Prompts.GenerateCombatBatch.SystemPrompt;

        Assert.Contains("combatActions", prompt);
        Assert.Contains("combatEncounters", prompt);
        Assert.Contains("Запрещены", prompt);
        Assert.Contains("actions", prompt);
        Assert.Contains("encounters", prompt);
        Assert.Contains("availableInCombat=true", prompt);
        Assert.Contains("Every combatant actionIds entry must point to an action in top-level actions", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("enemy_glitch_attack", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not create actionIds without defining matching actions", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyDraft_CombatEntity_AppliesCombatDefinition()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var draftFolder = Path.Combine(project.Summary.ProjectPath, "drafts", "draft_combat", "combat");
        Directory.CreateDirectory(draftFolder);
        var combatPath = Path.Combine(draftFolder, "combat.json");
        await File.WriteAllTextAsync(combatPath, JsonSerializer.Serialize(
            new GameCombatDefinition { Enabled = true, PlayerHealthStatId = "health", MaxRounds = 12 },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var draft = new GameDraftSession
        {
            SessionId = "draft_combat",
            Stage = "combat",
            Validation = { IsValid = true },
            Files = { CreateDraftFile(project, "combat", "combat", combatPath) }
        };

        await new GameDraftService().ApplyDraftAsync(project, draft, CancellationToken.None);

        Assert.NotNull(project.Combat);
        Assert.True(project.Combat!.Enabled);
        Assert.Equal("health", project.Combat.PlayerHealthStatId);
        Assert.Equal(12, project.Combat.MaxRounds);
        Assert.All(draft.Files, file => Assert.Equal("Applied", file.Status));
    }

    [Fact]
    public async Task ApplyDraft_NormalizedCombatDraft_SatisfiesMvpCombatStage()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        project.DesignProfile.InitialIdea = "игра с боевыми схватками";
        project.Stats.Add(new GameStatDefinition { Id = "health", Name = "Health", InitialValue = 100, IsResource = true });
        project.Stats.Add(new GameStatDefinition { Id = "stamina", Name = "Stamina", InitialValue = 10 });
        project.Stats.Add(new GameStatDefinition { Id = "stability", Name = "Stability", InitialValue = 10 });
        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonAmountsForTests("""
        {
          "combat": { "enabled": true, "playerHealthStatId": "health" },
          "combatActions": [
            { "id": "combat_strike", "name": "Удар", "effects": [ { "type": "combatDamage", "targetId": "target", "amount": 6 } ] }
          ],
          "combatEncounters": [
            {
              "id": "encounter_glitch",
              "name": "Схватка",
              "kind": "combat",
              "sceneId": "scene_start",
              "combatants": [
                { "id": "player", "name": "Игрок", "role": "player", "stats": { "health": "stat.health" }, "actionIds": ["combat_strike"] },
                { "id": "enemy_glitch", "name": "Искажение", "role": "enemy", "stats": { "health": 20 } }
              ]
            }
          ]
        }
        """, new List<string>());
        var generated = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Generated combat JSON is empty.");
        var draftFolder = Path.Combine(project.Summary.ProjectPath, "drafts", "draft_combat_pipeline");
        Directory.CreateDirectory(draftFolder);
        var combatPath = await WriteDraftEntityAsync(draftFolder, "combat", "combat", generated.Combat!);
        var actionPath = await WriteDraftEntityAsync(draftFolder, "actions", generated.Actions[0].Id, generated.Actions[0]);
        var encounterPath = await WriteDraftEntityAsync(draftFolder, "encounters", generated.Encounters[0].Id, generated.Encounters[0]);
        var draft = new GameDraftSession
        {
            SessionId = "draft_combat_pipeline",
            Stage = "combat",
            Validation = { IsValid = true },
            Files =
            {
                CreateDraftFile(project, "combat", "combat", combatPath),
                CreateDraftFile(project, "actions", generated.Actions[0].Id, actionPath),
                CreateDraftFile(project, "encounters", generated.Encounters[0].Id, encounterPath)
            }
        };

        await new GameDraftService().ApplyDraftAsync(project, draft, CancellationToken.None);

        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);
        var combatStage = Assert.Single(report.Stages, x => x.Stage == "combat");
        Assert.True(combatStage.IsSatisfied);
        Assert.NotEqual("combat", report.NextRecommendedStage);
    }

    [Fact]
    public async Task CombatJsonNormalizer_CreatesFallbackActionForMissingCombatantActionId()
    {
        var warnings = new List<string>();
        var normalized = GameCreationPipelineService.NormalizeGeneratedProjectJsonAmountsForTests("""
        {
          "combat": {
            "enabled": true,
            "playerHealthStatId": "health"
          },
          "actions": [
            {
              "id": "combat_strike",
              "availableInCombat": true,
              "actorTeam": "player",
              "targetScope": "enemy",
              "costs": [
                { "type": "stamina", "amount": 5 }
              ],
              "effects": [
                { "type": "combatDamage", "targetId": "target", "amount": 6 }
              ]
            }
          ],
          "encounters": [
            {
              "id": "encounter_glitch_entity_fight",
              "kind": "combat",
              "combatants": [
                {
                  "id": "player",
                  "isPlayer": true,
                  "team": "player",
                  "actionIds": [ "combat_strike" ],
                  "stats": { "health": 100 }
                },
                {
                  "id": "enemy_glitch_entity",
                  "isPlayer": false,
                  "team": "enemy",
                  "actionIds": [ "enemy_glitch_attack" ],
                  "stats": { "health": 45 }
                }
              ]
            }
          ]
        }
        """, warnings);
        var generated = JsonSerializer.Deserialize<GameProjectData>(normalized, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Generated combat JSON is empty.");

        var fallback = Assert.Single(generated.Actions, x => x.Id == "enemy_glitch_attack");
        Assert.True(fallback.AvailableInCombat);
        Assert.Equal("enemy", fallback.ActorTeam);
        Assert.Equal("player", fallback.TargetScope);
        Assert.Equal("combat", fallback.Kind);
        Assert.Contains(fallback.Effects, x => x.Type == "combatDamage" && x.TargetId == "target" && x.Amount == 5);
        Assert.Contains(fallback.Tags, x => x == "enemy");
        Assert.Contains(warnings, x => x == "Created fallback combat action 'enemy_glitch_attack' for combatant 'enemy_glitch_entity'.");

        var playerAction = Assert.Single(generated.Actions, x => x.Id == "combat_strike");
        var staminaCost = Assert.Single(playerAction.Costs);
        Assert.Equal("stat", staminaCost.Type);
        Assert.Equal("stamina", staminaCost.TargetId);
        Assert.Equal(5, staminaCost.Amount);

        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        project.DesignProfile.InitialIdea = "combat test";
        project.Stats.Add(new GameStatDefinition { Id = "health", Name = "Health", InitialValue = 100, IsResource = true });
        project.Stats.Add(new GameStatDefinition { Id = "stamina", Name = "Stamina", InitialValue = 10, IsResource = true });
        project.Stats.Add(new GameStatDefinition { Id = "stability", Name = "Stability", InitialValue = 10, IsResource = true });
        var draftFolder = Path.Combine(project.Summary.ProjectPath, "drafts", "draft_missing_combat_action");
        Directory.CreateDirectory(draftFolder);
        var draft = new GameDraftSession
        {
            SessionId = "draft_missing_combat_action",
            Stage = "combat",
            Validation = { IsValid = true }
        };

        var combatPath = await WriteDraftEntityAsync(draftFolder, "combat", "combat", generated.Combat!);
        draft.Files.Add(CreateDraftFile(project, "combat", "combat", combatPath));
        foreach (var action in generated.Actions)
        {
            var actionPath = await WriteDraftEntityAsync(draftFolder, "actions", action.Id, action);
            draft.Files.Add(CreateDraftFile(project, "actions", action.Id, actionPath));
        }

        var encounter = Assert.Single(generated.Encounters);
        var encounterPath = await WriteDraftEntityAsync(draftFolder, "encounters", encounter.Id, encounter);
        draft.Files.Add(CreateDraftFile(project, "encounters", encounter.Id, encounterPath));

        await new GameDraftService().ApplyDraftAsync(project, draft, CancellationToken.None);

        Assert.Contains(project.Actions, x => x.Id == "enemy_glitch_attack");
        var report = new GameMvpOrchestratorService().BuildReadinessReport(project);
        var combatStage = Assert.Single(report.Stages, x => x.Stage == "combat");
        Assert.True(combatStage.IsSatisfied);
    }

    private static GameDraftFile CreateDraftFile(GameProjectData project, string entityType, string entityId, string path)
    {
        return new GameDraftFile
        {
            EntityType = entityType,
            EntityId = entityId,
            RelativePath = Path.GetRelativePath(project.Summary.ProjectPath, path).Replace('\\', '/')
        };
    }

    private static async Task<string> WriteDraftEntityAsync<T>(string draftFolder, string entityType, string entityId, T value)
    {
        var folder = Path.Combine(draftFolder, entityType);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, entityId + ".json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        return path;
    }
}
