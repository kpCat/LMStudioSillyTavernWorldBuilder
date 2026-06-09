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

    private static GameDraftFile CreateDraftFile(GameProjectData project, string entityType, string entityId, string path)
    {
        return new GameDraftFile
        {
            EntityType = entityType,
            EntityId = entityId,
            RelativePath = Path.GetRelativePath(project.Summary.ProjectPath, path).Replace('\\', '/')
        };
    }
}
