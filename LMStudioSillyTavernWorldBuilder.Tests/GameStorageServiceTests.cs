using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameStorageServiceTests
{
    [Fact]
    public async Task SaveLoadProject_RoundTripsData()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = service.CreateNewProject(root, "Storage Test");

        await service.SaveProjectAsync(root, project);
        var loaded = await service.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.Equal(project.Meta.Id, loaded.Meta.Id);
        Assert.NotEmpty(loaded.Scenes);
        Assert.Equal(project.Summary.ProjectPath, loaded.Summary.ProjectPath);
    }

    [Fact]
    public void CreateNewProject_UsesAsciiTechnicalIdsForCyrillicTitle()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();

        var project = service.CreateNewProject(root, "Носитель Метамодулей");

        Assert.Equal("Носитель Метамодулей", project.Meta.Title);
        Assert.Matches("^[a-z0-9_-]+$", project.Meta.Id);
        Assert.Matches("^[a-z0-9_-]+$", project.Summary.FolderName);
    }

    [Fact]
    public async Task SaveLoadProject_PreservesSeparateGenerationPreferences()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = service.CreateNewProject(root, "Preferences");
        project.GenerationPreferences.GeneralGameplayText = "Общий геймплей";
        project.GenerationPreferences.SkillDesignText = "Навыки";
        project.GenerationPreferences.ProgressionDesignText = "Прокачка";
        project.GenerationPreferences.CombatDesignText = "Боёвка";
        project.GenerationPreferences.BalanceText = "Баланс";
        project.GenerationPreferences.ForbiddenDesignText = "Запреты";
        project.GenerationPreferences.Notes = "Заметки";

        await service.SaveProjectAsync(root, project);
        var loaded = await service.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.Equal("Общий геймплей", loaded.GenerationPreferences.GeneralGameplayText);
        Assert.Equal("Навыки", loaded.GenerationPreferences.SkillDesignText);
        Assert.Equal("Прокачка", loaded.GenerationPreferences.ProgressionDesignText);
        Assert.Equal("Боёвка", loaded.GenerationPreferences.CombatDesignText);
        Assert.Equal("Баланс", loaded.GenerationPreferences.BalanceText);
        Assert.Equal("Запреты", loaded.GenerationPreferences.ForbiddenDesignText);
        Assert.Equal("Заметки", loaded.GenerationPreferences.Notes);
    }

    [Fact]
    public async Task SaveProject_WritesSplitJsonFiles()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = Path.Combine(root, "Test");
        project.Summary.FolderName = "Test";

        await service.SaveProjectAsync(root, project);

        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "game-project.json")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "manifest.json")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "data", "scenes", "scene_start.json")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "data", "characters", "npc.json")));
    }

    [Fact]
    public async Task SaveProject_WritesReadableCyrillicJson()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = Path.Combine(root, "Readable");
        project.Meta.Title = "Носитель Метамодулей";
        project.Scenes[0].Title = "Начало";
        project.Scenes[0].Text = "Русский текст сцены.";

        await service.SaveProjectAsync(root, project);

        var sceneJson = await File.ReadAllTextAsync(Path.Combine(project.Summary.ProjectPath, "data", "scenes", "scene_start.json"));
        Assert.Contains("Русский текст сцены", sceneJson);
        Assert.DoesNotContain("\\u0420", sceneJson);
    }

    [Fact]
    public async Task LoadProject_RepairsTechnicalFallbackBridge()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = Path.Combine(root, "Contaminated");
        project.Scenes[0].Text = "Fallback scene created because generated content did not contain scenes.";
        project.Scenes[0].Choices.Clear();
        project.Scenes.Add(new GameScene { Id = "scene_border_glitch_discovery", Title = "Разрыв", Text = "Реальная сцена." });
        await service.SaveProjectAsync(root, project);

        var loaded = await service.LoadProjectAsync(project.Summary.ProjectPath);
        var start = loaded.Scenes.First(x => x.Id == "scene_start");

        Assert.Equal("Начало", start.Title);
        Assert.DoesNotContain("Fallback scene", start.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("scene_next", Assert.Single(start.Choices).NextSceneId);
        Assert.Equal("scene_start", loaded.Meta.StartSceneId);
    }

    [Fact]
    public void SyncSaveWithProject_AddsGeneratedContentAndRepairsScene()
    {
        var service = new GameStorageService();
        var project = TestProjects.CreatePlayableProject();
        project.Scenes[0].Text = "Fallback scene created because generated content did not contain scenes.";
        project.Scenes[0].Choices.Clear();
        project.Scenes[1].LocationId = "location_start";
        project.Locations.Add(new GameLocation { Id = "location_start", Name = "Старт" });
        project.Scenes.Add(new GameScene { Id = "scene_real", Title = "Реальная", Text = "Игровая сцена.", LocationId = "location_start" });
        project.Stats.Add(new GameStatDefinition { Id = "stamina", Name = "Stamina", InitialValue = 25 });
        project.Currencies.Add(new GameCurrencyDefinition { Id = "fragments", Name = "Фрагменты", InitialAmount = 3 });
        project.Variables.Add(new GameVariableDefinition { Id = "metamodule_sync", Name = "Синхронизация", InitialValue = 7 });
        project.Skills.Add(new GameSkillDefinition { Id = "scan", Name = "Сканирование", IsKnownByDefault = true });
        var save = TestProjects.CreateSave(project);
        save.CurrentSceneId = "scene_start";
        save.PlayerStats.Remove("stamina");

        service.SyncSaveWithProject(project, save);

        Assert.Equal(25, save.PlayerStats["stamina"]);
        Assert.Equal(3, save.Currencies["fragments"]);
        Assert.Equal(7, save.Variables["metamodule_sync"]);
        Assert.Contains(save.KnownSkills, x => x.SkillId == "scan");
        Assert.Equal("scene_next", save.CurrentSceneId);
        Assert.Equal("location_start", save.CurrentLocationId);
    }

    [Fact]
    public async Task LoadProject_ReadsSplitJsonFiles()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = Path.Combine(root, "Test");
        await service.SaveProjectAsync(root, project);

        var loaded = await service.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.Equal("scene_start", loaded.Scenes[0].Id);
        Assert.Equal("npc", loaded.Characters[0].Id);
    }

    [Fact]
    public async Task SaveProject_WritesAdvancedSystemFiles()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreateAdvancedProject();
        project.Summary.ProjectPath = Path.Combine(root, "Advanced");

        await service.SaveProjectAsync(root, project);

        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "data", "equipment-slots", "hand.json")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "data", "elements", "fire.json")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "data", "currencies", "gold.json")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "data", "variables", "alarm.json")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "data", "location-connections", "start_locked.json")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "data", "location-states", "burning.json")));
    }

    [Fact]
    public async Task SaveLoadProject_RoundTripsWorldState()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreateWorldStateProject();
        project.Summary.ProjectPath = Path.Combine(root, "WorldState");

        await service.SaveProjectAsync(root, project);
        var loaded = await service.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "data", "world-state.json")));
        Assert.True(loaded.WorldState.Enabled);
        Assert.Equal("morning", loaded.WorldState.Time.StartSegmentId);
        Assert.Contains(loaded.WorldState.Time.Segments, x => x.Id == "night");
        Assert.Contains(loaded.WorldState.Aspects, x => x.Id == "weather" && x.States.Any(s => s.Id == "rain"));
    }

    [Fact]
    public async Task LoadProject_ReadsAdvancedSystemFiles()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreateAdvancedProject();
        project.Summary.ProjectPath = Path.Combine(root, "Advanced");
        await service.SaveProjectAsync(root, project);

        var loaded = await service.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.Contains(loaded.EquipmentSlots, x => x.Id == "hand");
        Assert.Contains(loaded.Elements, x => x.Id == "fire");
        Assert.Contains(loaded.Currencies, x => x.Id == "gold");
        Assert.Contains(loaded.Variables, x => x.Id == "alarm");
        Assert.Contains(loaded.LocationConnections, x => x.Id == "start_locked");
        Assert.Contains(loaded.LocationStates, x => x.Id == "burning");
    }

    [Fact]
    public async Task SaveProject_WritesManifest()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = Path.Combine(root, "Test");

        await service.SaveProjectAsync(root, project);

        var manifest = JsonSerializer.Deserialize<GameProjectManifest>(await File.ReadAllTextAsync(Path.Combine(project.Summary.ProjectPath, "manifest.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(manifest);
        Assert.Contains("data/scenes/scene_start.json", manifest!.Scenes);
    }

    [Fact]
    public async Task LoadProject_UsesManifest()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = Path.Combine(root, "Test");
        await service.SaveProjectAsync(root, project);
        await File.WriteAllTextAsync(Path.Combine(project.Summary.ProjectPath, "data", "scenes", "orphan.json"),
            JsonSerializer.Serialize(new GameScene { Id = "orphan", Title = "Orphan" }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        var loaded = await service.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.DoesNotContain(loaded.Scenes, x => x.Id == "orphan");
    }

    [Fact]
    public async Task LegacyFullProject_LoadsAndSavesAsSplit()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var folder = Path.Combine(root, "Legacy");
        Directory.CreateDirectory(folder);
        var legacy = TestProjects.CreatePlayableProject();
        legacy.Summary.ProjectPath = folder;
        await File.WriteAllTextAsync(Path.Combine(folder, "game-project.json"),
            JsonSerializer.Serialize(legacy, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

        var loaded = await service.LoadProjectAsync(folder);
        await service.SaveProjectAsync(root, loaded);

        Assert.True(service.IsSplitProject(folder));
        Assert.True(Directory.EnumerateFiles(folder, "game-project.legacy-backup.*.json").Any());
        Assert.True(File.Exists(Path.Combine(folder, "data", "scenes", "scene_start.json")));
    }

    [Fact]
    public async Task SaveProject_DoesNotStoreAllEntitiesInRootGameProjectJson()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = Path.Combine(root, "Test");

        await service.SaveProjectAsync(root, project);

        var rootJson = await File.ReadAllTextAsync(Path.Combine(project.Summary.ProjectPath, "game-project.json"));
        Assert.Contains("split-json", rootJson);
        Assert.DoesNotContain("\"scenes\"", rootJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"characters\"", rootJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveLoadProgress_RoundTripsSaveGame()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = service.CreateNewProject(root, "Save Test");
        await service.SaveProjectAsync(root, project);
        var save = service.CreateInitialSave(project, "autosave");
        save.PlayerStats["health"] = 77;

        await service.SaveProgressAsync(project, save, "autosave.json");
        var loaded = await service.LoadProgressAsync(project, "autosave.json");

        Assert.Equal(77, loaded.PlayerStats["health"]);
        Assert.Equal(project.Meta.StartSceneId, loaded.CurrentSceneId);
    }

    [Fact]
    public async Task SaveProgress_AfterSuccessfulChoice_UpdatesAutosaveSceneAndEventLog()
    {
        var service = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = Path.Combine(root, "AutosaveChoice");
        await service.SaveProjectAsync(root, project);
        var save = service.CreateInitialSave(project, "autosave");
        var engine = new LMStudioSillyTavernWorldBuilder.Runtime.GameRuntimeEngine();

        var result = engine.ApplyChoiceWithResult(project, save, "choice_go");
        await service.SaveProgressAsync(project, save, "autosave.json");
        var loaded = await service.LoadProgressAsync(project, "autosave.json");

        Assert.True(result.Success);
        Assert.Equal("scene_next", loaded.CurrentSceneId);
        Assert.NotEmpty(loaded.EventLog);
    }
}
