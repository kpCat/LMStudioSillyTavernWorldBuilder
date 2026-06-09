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
}
