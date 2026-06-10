using System.Text;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Storage;

internal sealed class GameStorageService
{
    private readonly GameProjectManifestService _manifestService = new();
    private readonly GameChangeLogService _changeLogService = new();
    private readonly GameProjectValidator _validator = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public string GetDefaultGamesRoot()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AiGameBuilder", "Games");
    }

    public GameProjectData CreateNewProject(string gamesRoot, string title)
    {
        var id = Ids.FromTitle(title, "game");
        var folderName = MakeSafeFolderName(title, id);
        var projectFolder = Path.Combine(gamesRoot, folderName);

        var project = new GameProjectData
        {
            Summary = new GameProjectSummary
            {
                Id = id,
                Title = title,
                FolderName = folderName,
                ProjectPath = projectFolder
            },
            Meta = new GameMeta
            {
                Id = id,
                Title = title,
                Description = "Новая текстовая игра."
            }
        };

        SeedStarterContent(project);
        return project;
    }

    public async Task SaveProjectAsync(string gamesRoot, GameProjectData project, CancellationToken cancellationToken = default)
    {
        var projectFolder = GetProjectFolder(gamesRoot, project);
        EnsureProjectDirectories(projectFolder);
        BackupLegacyRootIfNeeded(projectFolder);

        project.Summary.ProjectPath = projectFolder;
        project.Summary.UpdatedAtUtc = DateTime.UtcNow;
        project.Meta.UpdatedAtUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(project.Summary.Id)) project.Summary.Id = project.Meta.Id;
        if (string.IsNullOrWhiteSpace(project.Meta.Id)) project.Meta.Id = project.Summary.Id;
        if (string.IsNullOrWhiteSpace(project.Summary.Title)) project.Summary.Title = project.Meta.Title;
        if (string.IsNullOrWhiteSpace(project.Meta.Title)) project.Meta.Title = project.Summary.Title;
        if (string.IsNullOrWhiteSpace(project.Summary.FolderName)) project.Summary.FolderName = Path.GetFileName(projectFolder);

        var manifest = _manifestService.BuildManifest(project);
        await WriteJsonAsync(Path.Combine(projectFolder, "game-project.json"), CreateRootDocument(project), cancellationToken);
        await WriteDesignAsync(projectFolder, project, cancellationToken);
        await WriteJsonAsync(Path.Combine(projectFolder, manifest.GenerationPreferences), project.GenerationPreferences, cancellationToken);
        await WriteJsonAsync(Path.Combine(projectFolder, "data", "game-meta.json"), project.Meta, cancellationToken);
        await WriteJsonAsync(Path.Combine(projectFolder, "data", "world.json"), project.World, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Stats, project.Stats, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Skills, project.Skills, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Items, project.Items, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.EquipmentSlots, project.EquipmentSlots, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Elements, project.Elements, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Currencies, project.Currencies, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Variables, project.Variables, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Characters, project.Characters, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Relationships, project.Relationships, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Locations, project.Locations, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.LocationConnections, project.LocationConnections, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.LocationStates, project.LocationStates, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Scenes, project.Scenes, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Quests, project.Quests, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Encounters, project.Encounters, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Actions, project.Actions, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.Formulas, project.Formulas, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.StatusEffects, project.StatusEffects, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.ProgressionNodes, project.ProgressionNodes, cancellationToken);
        await WriteJsonAsync(Path.Combine(projectFolder, manifest.WorldState), project.WorldState, cancellationToken);
        await WriteJsonAsync(Path.Combine(projectFolder, manifest.Mechanics), project.Mechanics, cancellationToken);
        if (project.Combat != null)
        {
            await WriteJsonAsync(Path.Combine(projectFolder, manifest.Combat), project.Combat, cancellationToken);
        }

        await WriteEntitiesAsync(projectFolder, manifest.ImagePrompts, project.ImagePrompts, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.GeneratedImageCandidates, project.GeneratedImageCandidates, cancellationToken);
        await WriteEntitiesAsync(projectFolder, manifest.AssetLinks, project.AssetLinks, cancellationToken);
        await _manifestService.SaveManifestAsync(projectFolder, manifest, cancellationToken);
    }

    public async Task<GameProjectData> LoadProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var rootPath = Directory.Exists(projectPath)
            ? Path.Combine(projectPath, "game-project.json")
            : projectPath;
        var projectFolder = Path.GetDirectoryName(rootPath) ?? string.Empty;

        if (!File.Exists(rootPath))
        {
            throw new FileNotFoundException("Game project file was not found.", rootPath);
        }

        var project = IsSplitProject(projectFolder)
            ? await LoadSplitProjectAsync(projectFolder, cancellationToken)
            : await LoadLegacyProjectAsync(rootPath, cancellationToken);

        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            project.Summary.ProjectPath = projectFolder;
        }

        _validator.Validate(project);
        return project;
    }

    public bool IsSplitProject(string projectPath)
    {
        var projectFolder = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath) ?? string.Empty;
        var manifestPath = Path.Combine(projectFolder, "manifest.json");
        var rootPath = Path.Combine(projectFolder, "game-project.json");
        if (!File.Exists(manifestPath) || !File.Exists(rootPath))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(rootPath));
            var root = document.RootElement;
            var schemaVersion = root.TryGetProperty("schemaVersion", out var versionElement) ? versionElement.GetInt32() : 1;
            var dataLayout = root.TryGetProperty("dataLayout", out var layoutElement) ? layoutElement.GetString() : string.Empty;
            return schemaVersion >= 2 && string.Equals(dataLayout, "split-json", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async Task MigrateLegacyToSplitAsync(GameProjectData project, string gamesRoot, CancellationToken cancellationToken = default)
    {
        await SaveProjectAsync(gamesRoot, project, cancellationToken);
        await _changeLogService.AppendChangeAsync(project, new GameChangeRecord
        {
            Operation = "import",
            EntityType = "project",
            EntityId = project.Meta.Id,
            CreatedBy = "user",
            ApprovedByUser = true,
            Notes = "Legacy monolithic project migrated to split-json."
        }, cancellationToken);
    }

    public List<GameProjectSummary> ListProjects(string gamesRoot)
    {
        Directory.CreateDirectory(gamesRoot);
        var result = new List<GameProjectSummary>();
        var deletedRoot = Path.Combine(Path.GetFullPath(gamesRoot), "_deleted");

        foreach (var file in Directory.EnumerateFiles(gamesRoot, "game-project.json", SearchOption.AllDirectories))
        {
            try
            {
                var folder = Path.GetDirectoryName(file) ?? string.Empty;
                if (IsPathUnderDirectory(folder, deletedRoot))
                {
                    continue;
                }

                if (IsSplitProject(folder))
                {
                    var root = JsonSerializer.Deserialize<GameProjectRootDocument>(File.ReadAllText(file), _jsonOptions);
                    if (root != null)
                    {
                        result.Add(new GameProjectSummary
                        {
                            Id = root.ProjectId,
                            Title = root.Title,
                            FolderName = string.IsNullOrWhiteSpace(root.FolderName) ? Path.GetFileName(folder) : root.FolderName,
                            ProjectPath = folder,
                            CreatedAtUtc = root.CreatedAtUtc,
                            UpdatedAtUtc = root.UpdatedAtUtc
                        });
                    }
                }
                else
                {
                    var project = JsonSerializer.Deserialize<GameProjectData>(File.ReadAllText(file), _jsonOptions);
                    if (project?.Summary != null)
                    {
                        project.Summary.ProjectPath = folder;
                        result.Add(project.Summary);
                    }
                }
            }
            catch
            {
                // A broken project should not hide other games.
            }
        }

        return result.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Title).ToList();
    }

    public SaveGame CreateInitialSave(GameProjectData project, string name)
    {
        var save = new SaveGame
        {
            Id = Ids.New("save"),
            ProjectId = project.Meta.Id,
            Name = name,
            CurrentSceneId = project.Meta.StartSceneId,
            PlayerStats = project.Stats.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToDictionary(x => x.Id, x => x.InitialValue),
            Currencies = project.Currencies.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToDictionary(x => x.Id, x => x.InitialAmount),
            Relationships = project.Relationships.Where(x => !string.IsNullOrWhiteSpace(x.CharacterId)).ToDictionary(x => x.CharacterId, x => x.InitialValue),
            ActiveQuestIds = project.Quests.Where(x => x.IsActiveByDefault).Select(x => x.Id).ToList(),
            KnownSkills = project.Skills.Where(x => x.IsKnownByDefault || x.InitialLevel > 0)
                .Select(x => new GameKnownSkill { SkillId = x.Id, Level = Math.Max(1, x.InitialLevel), IsEnabled = true })
                .ToList(),
            PlayerLevel = Math.Max(1, project.Mechanics.Experience.InitialPlayerLevel),
            PlayerExperience = Math.Max(0, project.Mechanics.Experience.InitialPlayerExperience),
            UnlockedProgressionNodeIds = project.ProgressionNodes.Where(x => x.IsUnlockedByDefault).Select(x => x.Id).ToList(),
            CurrentLocationId = project.Scenes.FirstOrDefault(x => x.Id == project.Meta.StartSceneId)?.LocationId
                ?? project.Locations.FirstOrDefault(x => x.IsDiscovered)?.Id
                ?? project.Locations.FirstOrDefault()?.Id
                ?? string.Empty,
            DiscoveredLocationIds = project.Locations.Where(x => x.IsDiscovered).Select(x => x.Id).ToList(),
            Variables = project.Variables.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToDictionary(x => x.Id, x => x.InitialValue)
        };
        InitializeWorldState(project, save);
        return save;
    }

    public async Task SaveProgressAsync(GameProjectData project, SaveGame saveGame, string fileName, CancellationToken cancellationToken = default)
    {
        var projectFolder = GetProjectFolderFromProject(project);
        Directory.CreateDirectory(Path.Combine(projectFolder, "saves"));
        saveGame.SavedAtUtc = DateTime.UtcNow;
        await WriteJsonAsync(Path.Combine(projectFolder, "saves", fileName), saveGame, cancellationToken);
    }

    public async Task<SaveGame> LoadProgressAsync(GameProjectData project, string fileName, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(GetProjectFolderFromProject(project), "saves", fileName);
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<SaveGame>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Save JSON is empty or invalid.");
    }

    public List<string> ListSaveFiles(GameProjectData project)
    {
        var savesPath = Path.Combine(GetProjectFolderFromProject(project), "saves");
        if (!Directory.Exists(savesPath))
        {
            return new List<string>();
        }

        return Directory.EnumerateFiles(savesPath, "*.json")
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .OrderBy(x => x)
            .ToList();
    }

    public async Task AppendLogAsync(GameProjectData project, string relativeLogName, string text, CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(GetProjectFolderFromProject(project), "logs");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, relativeLogName);
        await File.AppendAllTextAsync(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}", Encoding.UTF8, cancellationToken);
    }

    public string GetProjectFolder(string gamesRoot, GameProjectData project)
    {
        return string.IsNullOrWhiteSpace(project.Summary.ProjectPath)
            ? Path.Combine(gamesRoot, project.Summary.FolderName)
            : project.Summary.ProjectPath;
    }

    public void EnsureProjectDirectories(string projectFolder)
    {
        Directory.CreateDirectory(projectFolder);
        Directory.CreateDirectory(Path.Combine(projectFolder, "design"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "stats"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "skills"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "items"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "equipment-slots"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "elements"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "currencies"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "variables"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "characters"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "relationships"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "locations"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "location-connections"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "location-states"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "scenes"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "quests"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "encounters"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "actions"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "formulas"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "status-effects"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "progression"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "data", "combat"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "assets", "scenes"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "assets", "characters"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "assets", "items"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "assets", "ui"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "assets", "generated-imports"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "prompts", "image-prompts"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "prompts", "generated-candidates"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "prompts", "asset-links"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "prompts", "prompt-history"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "drafts"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "changes"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "saves"));
        Directory.CreateDirectory(Path.Combine(projectFolder, "logs"));
    }

    private async Task<GameProjectData> LoadSplitProjectAsync(string projectFolder, CancellationToken cancellationToken)
    {
        var root = await ReadJsonAsync<GameProjectRootDocument>(Path.Combine(projectFolder, "game-project.json"), cancellationToken);
        var manifest = await _manifestService.LoadManifestAsync(projectFolder, cancellationToken);
        var project = new GameProjectData
        {
            Summary = new GameProjectSummary
            {
                Id = root.ProjectId,
                Title = root.Title,
                FolderName = root.FolderName,
                ProjectPath = projectFolder,
                CreatedAtUtc = root.CreatedAtUtc,
                UpdatedAtUtc = root.UpdatedAtUtc
            },
            Meta = new GameMeta
            {
                Id = root.ProjectId,
                Title = root.Title,
                StartSceneId = root.StartSceneId,
                Language = root.Language,
                CreatedAtUtc = root.CreatedAtUtc,
                UpdatedAtUtc = root.UpdatedAtUtc
            }
        };

        await ReadDesignAsync(projectFolder, project, cancellationToken);
        if (!string.IsNullOrWhiteSpace(manifest.GenerationPreferences))
        {
            var preferencesPath = Path.Combine(projectFolder, manifest.GenerationPreferences);
            if (File.Exists(preferencesPath))
            {
                project.GenerationPreferences = await ReadJsonAsync<GameGenerationPreferences>(preferencesPath, cancellationToken);
            }
        }
        var metaPath = Path.Combine(projectFolder, "data", "game-meta.json");
        if (File.Exists(metaPath))
        {
            project.Meta = await ReadJsonAsync<GameMeta>(metaPath, cancellationToken);
            project.Meta.Id = string.IsNullOrWhiteSpace(project.Meta.Id) ? root.ProjectId : project.Meta.Id;
            project.Meta.Title = string.IsNullOrWhiteSpace(project.Meta.Title) ? root.Title : project.Meta.Title;
            project.Meta.StartSceneId = string.IsNullOrWhiteSpace(project.Meta.StartSceneId) ? root.StartSceneId : project.Meta.StartSceneId;
        }
        var worldPath = Path.Combine(projectFolder, "data", "world.json");
        if (File.Exists(worldPath))
        {
            project.World = await ReadJsonAsync<GameWorld>(worldPath, cancellationToken);
        }

        project.Stats = await ReadEntitiesAsync<GameStatDefinition>(projectFolder, manifest.Stats, cancellationToken);
        project.Skills = await ReadEntitiesAsync<GameSkillDefinition>(projectFolder, manifest.Skills, cancellationToken);
        project.Items = await ReadEntitiesAsync<GameItemDefinition>(projectFolder, manifest.Items, cancellationToken);
        project.EquipmentSlots = await ReadEntitiesAsync<GameEquipmentSlotDefinition>(projectFolder, manifest.EquipmentSlots, cancellationToken);
        project.Elements = await ReadEntitiesAsync<GameElementDefinition>(projectFolder, manifest.Elements, cancellationToken);
        project.Currencies = await ReadEntitiesAsync<GameCurrencyDefinition>(projectFolder, manifest.Currencies, cancellationToken);
        project.Variables = await ReadEntitiesAsync<GameVariableDefinition>(projectFolder, manifest.Variables, cancellationToken);
        project.Characters = await ReadEntitiesAsync<GameCharacter>(projectFolder, manifest.Characters, cancellationToken);
        project.Relationships = await ReadEntitiesAsync<GameRelationshipDefinition>(projectFolder, manifest.Relationships, cancellationToken);
        project.Locations = await ReadEntitiesAsync<GameLocation>(projectFolder, manifest.Locations, cancellationToken);
        project.LocationConnections = await ReadEntitiesAsync<GameLocationConnection>(projectFolder, manifest.LocationConnections, cancellationToken);
        project.LocationStates = await ReadEntitiesAsync<GameLocationStateDefinition>(projectFolder, manifest.LocationStates, cancellationToken);
        project.Scenes = await ReadEntitiesAsync<GameScene>(projectFolder, manifest.Scenes, cancellationToken);
        project.Quests = await ReadEntitiesAsync<GameQuest>(projectFolder, manifest.Quests, cancellationToken);
        project.Encounters = await ReadEntitiesAsync<GameEncounterDefinition>(projectFolder, manifest.Encounters, cancellationToken);
        project.Actions = await ReadEntitiesAsync<GameActionDefinition>(projectFolder, manifest.Actions, cancellationToken);
        project.Formulas = await ReadEntitiesAsync<GameFormulaDefinition>(projectFolder, manifest.Formulas, cancellationToken);
        project.StatusEffects = await ReadEntitiesAsync<GameStatusEffectDefinition>(projectFolder, manifest.StatusEffects, cancellationToken);
        project.ProgressionNodes = await ReadEntitiesAsync<GameProgressionNodeDefinition>(projectFolder, manifest.ProgressionNodes, cancellationToken);
        if (!string.IsNullOrWhiteSpace(manifest.WorldState))
        {
            var worldStatePath = Path.Combine(projectFolder, manifest.WorldState);
            if (File.Exists(worldStatePath))
            {
                project.WorldState = await ReadJsonAsync<GameWorldStateDefinition>(worldStatePath, cancellationToken);
            }
        }
        if (!string.IsNullOrWhiteSpace(manifest.Mechanics))
        {
            var mechanicsPath = Path.Combine(projectFolder, manifest.Mechanics);
            if (File.Exists(mechanicsPath))
            {
                project.Mechanics = await ReadJsonAsync<GameMechanicsDefinition>(mechanicsPath, cancellationToken);
            }
        }
        if (!string.IsNullOrWhiteSpace(manifest.Combat))
        {
            var combatPath = Path.Combine(projectFolder, manifest.Combat);
            if (File.Exists(combatPath))
            {
                project.Combat = await ReadJsonAsync<GameCombatDefinition>(combatPath, cancellationToken);
            }
        }

        project.ImagePrompts = await ReadEntitiesAsync<ImagePromptDefinition>(projectFolder, manifest.ImagePrompts, cancellationToken);
        project.GeneratedImageCandidates = await ReadEntitiesAsync<ImageGeneratedCandidate>(projectFolder, manifest.GeneratedImageCandidates, cancellationToken);
        project.AssetLinks = await ReadEntitiesAsync<ImageAssetLink>(projectFolder, manifest.AssetLinks, cancellationToken);
        return project;
    }

    private async Task<GameProjectData> LoadLegacyProjectAsync(string rootPath, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(rootPath, cancellationToken);
        var project = JsonSerializer.Deserialize<GameProjectData>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Game project JSON is empty or invalid.");
        project.Summary.ProjectPath = Path.GetDirectoryName(rootPath) ?? string.Empty;
        return project;
    }

    private async Task<List<T>> ReadEntitiesAsync<T>(string projectFolder, IEnumerable<string> relativePaths, CancellationToken cancellationToken)
    {
        var result = new List<T>();
        foreach (var relativePath in relativePaths)
        {
            var path = Path.Combine(projectFolder, relativePath);
            if (!File.Exists(path))
            {
                continue;
            }

            result.Add(await ReadJsonAsync<T>(path, cancellationToken));
        }

        return result;
    }

    private async Task WriteEntitiesAsync<T>(string projectFolder, IReadOnlyList<string> relativePaths, IReadOnlyList<T> entities, CancellationToken cancellationToken)
    {
        for (var i = 0; i < relativePaths.Count && i < entities.Count; i++)
        {
            await WriteJsonAsync(Path.Combine(projectFolder, relativePaths[i]), entities[i], cancellationToken);
        }
    }

    private async Task WriteDesignAsync(string projectFolder, GameProjectData project, CancellationToken cancellationToken)
    {
        await WriteTextAsync(Path.Combine(projectFolder, "design", "brief.md"), project.Brief.Text, cancellationToken);
        await WriteTextAsync(Path.Combine(projectFolder, "design", "concept.md"), project.Concept.Text, cancellationToken);
        await WriteTextAsync(Path.Combine(projectFolder, "design", "mvp.md"), project.MvpPlan.Text, cancellationToken);
        await WriteTextAsync(Path.Combine(projectFolder, "design", "architecture.md"), project.ArchitecturePlan.Text, cancellationToken);
        await WriteTextAsync(Path.Combine(projectFolder, "design", "content-plan.md"), project.ContentPlan.Text, cancellationToken);
        await WriteTextAsync(Path.Combine(projectFolder, "design", "prompt-plan.md"), project.PromptPlan.Text, cancellationToken);
        await WriteJsonAsync(Path.Combine(projectFolder, "design", "design-profile.json"), project.DesignProfile, cancellationToken);
        await WriteJsonAsync(Path.Combine(projectFolder, "design", "creation-plan.json"), project.CreationPlan, cancellationToken);
        await WriteJsonAsync(Path.Combine(projectFolder, "design", "knowledge-base.json"), project.DesignKnowledgeBase, cancellationToken);
        await WriteJsonAsync(Path.Combine(projectFolder, "design", "conversation-history.json"), project.DesignConversationHistory, cancellationToken);
    }

    private async Task ReadDesignAsync(string projectFolder, GameProjectData project, CancellationToken cancellationToken)
    {
        project.Brief.Text = await ReadTextIfExistsAsync(Path.Combine(projectFolder, "design", "brief.md"), cancellationToken);
        project.Concept.Text = await ReadTextIfExistsAsync(Path.Combine(projectFolder, "design", "concept.md"), cancellationToken);
        project.MvpPlan.Text = await ReadTextIfExistsAsync(Path.Combine(projectFolder, "design", "mvp.md"), cancellationToken);
        project.ArchitecturePlan.Text = await ReadTextIfExistsAsync(Path.Combine(projectFolder, "design", "architecture.md"), cancellationToken);
        project.ContentPlan.Text = await ReadTextIfExistsAsync(Path.Combine(projectFolder, "design", "content-plan.md"), cancellationToken);
        project.PromptPlan.Text = await ReadTextIfExistsAsync(Path.Combine(projectFolder, "design", "prompt-plan.md"), cancellationToken);
        var designProfilePath = Path.Combine(projectFolder, "design", "design-profile.json");
        if (File.Exists(designProfilePath))
        {
            project.DesignProfile = await ReadJsonAsync<GameDesignProfile>(designProfilePath, cancellationToken);
        }

        var creationPlanPath = Path.Combine(projectFolder, "design", "creation-plan.json");
        if (File.Exists(creationPlanPath))
        {
            project.CreationPlan = await ReadJsonAsync<GameCreationPlan>(creationPlanPath, cancellationToken);
        }

        var knowledgeBasePath = Path.Combine(projectFolder, "design", "knowledge-base.json");
        if (File.Exists(knowledgeBasePath))
        {
            project.DesignKnowledgeBase = await ReadJsonAsync<GameDesignKnowledgeBase>(knowledgeBasePath, cancellationToken);
        }

        var conversationHistoryPath = Path.Combine(projectFolder, "design", "conversation-history.json");
        if (File.Exists(conversationHistoryPath))
        {
            project.DesignConversationHistory = await ReadJsonAsync<GameDesignConversationHistory>(conversationHistoryPath, cancellationToken);
        }
    }

    private static GameProjectRootDocument CreateRootDocument(GameProjectData project)
    {
        return new GameProjectRootDocument
        {
            ProjectId = project.Meta.Id,
            Title = project.Meta.Title,
            FolderName = project.Summary.FolderName,
            StartSceneId = project.Meta.StartSceneId,
            Language = project.Meta.Language,
            CreatedAtUtc = project.Meta.CreatedAtUtc,
            UpdatedAtUtc = project.Meta.UpdatedAtUtc
        };
    }

    private static void InitializeWorldState(GameProjectData project, SaveGame save)
    {
        var definition = project.WorldState;
        save.WorldState.DayNumber = Math.Max(1, definition.Time.StartDayNumber);

        var orderedSegments = definition.Time.Segments
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .OrderBy(x => x.Order)
            .ToList();
        var startSegment = orderedSegments.FirstOrDefault(x => string.Equals(x.Id, definition.Time.StartSegmentId, StringComparison.OrdinalIgnoreCase))
            ?? orderedSegments.FirstOrDefault();
        save.WorldState.TimeSegmentId = startSegment?.Id ?? string.Empty;

        save.WorldState.AspectStates.Clear();
        foreach (var aspect in definition.Aspects.Where(x => !string.IsNullOrWhiteSpace(x.Id)))
        {
            var state = aspect.States.FirstOrDefault(x => string.Equals(x.Id, aspect.DefaultStateId, StringComparison.OrdinalIgnoreCase))
                ?? aspect.States.FirstOrDefault();
            if (state != null && !string.IsNullOrWhiteSpace(state.Id))
            {
                save.WorldState.AspectStates[aspect.Id] = state.Id;
            }
        }
    }

    private void BackupLegacyRootIfNeeded(string projectFolder)
    {
        var rootPath = Path.Combine(projectFolder, "game-project.json");
        var manifestPath = Path.Combine(projectFolder, "manifest.json");
        if (!File.Exists(rootPath) || File.Exists(manifestPath))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(rootPath));
            var root = document.RootElement;
            if (root.TryGetProperty("dataLayout", out var layout) && string.Equals(layout.GetString(), "split-json", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var backupPath = Path.Combine(projectFolder, $"game-project.legacy-backup.{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            File.Copy(rootPath, backupPath, overwrite: false);
        }
        catch
        {
            // Backup is best-effort and should not block saving a recoverable project.
        }
    }

    private async Task<T> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException("JSON is empty or invalid: " + path);
    }

    private async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), cancellationToken);
    }

    private static async Task WriteTextAsync(string path, string value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, value ?? string.Empty, new UTF8Encoding(false), cancellationToken);
    }

    private static async Task<string> ReadTextIfExistsAsync(string path, CancellationToken cancellationToken)
    {
        return File.Exists(path) ? await File.ReadAllTextAsync(path, cancellationToken) : string.Empty;
    }

    private static string GetProjectFolderFromProject(GameProjectData project)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            throw new InvalidOperationException("Project path is not set. Save the project first.");
        }

        return project.Summary.ProjectPath;
    }

    private static bool IsPathUnderDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.Equals(fullDirectory, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(fullDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(fullDirectory + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string MakeSafeFolderName(string title, string fallback)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(title.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = fallback;
        }

        return safe.Length > 64 ? safe[..64] : safe;
    }

    private static void SeedStarterContent(GameProjectData project)
    {
        project.World.Summary = "Черновой мир. Его можно уточнить через AI-пайплайн.";
        project.Stats.AddRange(new[]
        {
            new GameStatDefinition { Id = "health", Name = "Здоровье", Description = "Физическое состояние героя.", MinValue = 0, MaxValue = 100, InitialValue = 100, IsResource = true },
            new GameStatDefinition { Id = "will", Name = "Воля", Description = "Решимость и устойчивость.", MinValue = 0, MaxValue = 100, InitialValue = 50, IsResource = true }
        });
        project.Locations.Add(new GameLocation { Id = "location_start", Name = "Стартовая локация", Description = "Место, с которого начинается история." });
        project.Scenes.Add(new GameScene
        {
            Id = "scene_start",
            Title = "Начало",
            LocationId = "location_start",
            Text = "Игра создана. Сгенерируйте структуру и контент, чтобы заменить эту стартовую сцену.",
            Choices =
            {
                new GameChoice { Id = "choice_wait", Text = "Осмотреться", NextSceneId = "scene_start", Effects = { new GameEffect { Type = "log", Text = "Вы осматриваетесь и отмечаете первые детали мира." } } }
            }
        });
        project.Meta.StartSceneId = "scene_start";
    }
}

internal static class Ids
{
    public static string New(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 13, prefix.Length + 33)];
    }

    public static string FromTitle(string title, string prefix)
    {
        var letters = new string(title.ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray());
        while (letters.Contains("__", StringComparison.Ordinal))
        {
            letters = letters.Replace("__", "_", StringComparison.Ordinal);
        }

        letters = letters.Trim('_');
        if (string.IsNullOrWhiteSpace(letters))
        {
            letters = Guid.NewGuid().ToString("N")[..8];
        }

        if (letters.Length > 32)
        {
            letters = letters[..32].Trim('_');
        }

        return $"{prefix}_{letters}";
    }
}
