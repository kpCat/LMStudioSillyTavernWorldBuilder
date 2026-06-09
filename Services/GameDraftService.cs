using System.Text;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameDraftService
{
    private readonly GameProjectValidator _validator = new();
    private readonly GameChangeLogService _changeLogService = new();
    private readonly GameProjectCloneService _cloneService = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<GameDraftSession> SaveRawDraftAsync(GameProjectData project, string stage, string userRequest, string rawOutput, CancellationToken token)
    {
        var folder = CreateDraftFolder(project, stage, out var sessionId);
        var rawPath = Path.Combine(folder, "raw-output.txt");
        await File.WriteAllTextAsync(rawPath, rawOutput, new UTF8Encoding(false), token);

        var draft = new GameDraftSession
        {
            SessionId = sessionId,
            Stage = stage,
            UserRequest = userRequest,
            RawOutputFile = ToProjectRelativePath(project, rawPath)
        };

        await SaveDraftManifestAsync(project, draft, token);
        return draft;
    }

    public async Task<GameDraftSession> ExtractEntityDraftsAsync(GameProjectData project, string stage, string rawOutput, CancellationToken token)
    {
        var draft = await SaveRawDraftAsync(project, stage, string.Empty, rawOutput, token);
        var folder = Path.Combine(project.Summary.ProjectPath, "drafts", draft.SessionId);
        var json = ExtractJson(rawOutput);

        try
        {
            var generated = JsonSerializer.Deserialize<GameProjectData>(json, _jsonOptions);
            if (generated == null)
            {
                draft.Validation.Errors.Add("Draft JSON is empty.");
                draft.Validation.IsValid = false;
                await SaveDraftManifestAsync(project, draft, token);
                return draft;
            }

            await WriteEntitiesAsync(project, generated, draft, folder, token);
            draft.Validation = _validator.Validate(generated);
            foreach (var file in draft.Files)
            {
                file.Status = draft.Validation.IsValid ? "Valid" : "Invalid";
            }
        }
        catch (Exception ex)
        {
            draft.Validation.Errors.Add("Could not parse draft as GameProjectData: " + ex.Message);
            draft.Validation.IsValid = false;
        }

        await SaveDraftManifestAsync(project, draft, token);
        await SaveValidationReportAsync(project, draft, token);
        return draft;
    }

    public Task<GameProjectValidationResult> ValidateDraftAsync(GameProjectData project, GameDraftSession draft, CancellationToken token)
    {
        draft.Validation.IsValid = draft.Validation.Errors.Count == 0;
        return Task.FromResult(draft.Validation);
    }

    public async Task<List<GameDraftSession>> LoadDraftsAsync(GameProjectData project, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            return new List<GameDraftSession>();
        }

        var draftsFolder = Path.Combine(project.Summary.ProjectPath, "drafts");
        if (!Directory.Exists(draftsFolder))
        {
            return new List<GameDraftSession>();
        }

        var drafts = new List<GameDraftSession>();
        foreach (var manifestPath in Directory.EnumerateFiles(draftsFolder, "draft-manifest.json", SearchOption.AllDirectories))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, token);
                var draft = JsonSerializer.Deserialize<GameDraftSession>(json, _jsonOptions);
                if (draft != null)
                {
                    if (string.IsNullOrWhiteSpace(draft.SessionId))
                    {
                        draft.SessionId = Path.GetFileName(Path.GetDirectoryName(manifestPath)) ?? string.Empty;
                    }

                    drafts.Add(draft);
                }
            }
            catch
            {
                // Ignore broken draft manifests; validation reports keep the details for manual inspection.
            }
        }

        return drafts
            .OrderByDescending(x => x.CreatedAtUtc == default ? DateTime.MinValue : x.CreatedAtUtc)
            .ThenByDescending(x => x.SessionId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<GameDraftSession?> LoadLatestDraftAsync(GameProjectData project, CancellationToken token = default)
    {
        var drafts = await LoadDraftsAsync(project, token);
        return drafts.FirstOrDefault(x => x.Files.Any(file =>
            string.Equals(file.Status, "Draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(file.Status, "Valid", StringComparison.OrdinalIgnoreCase)));
    }

    public async Task RejectDraftAsync(GameProjectData project, GameDraftSession draft, CancellationToken token = default)
    {
        foreach (var file in draft.Files.Where(x =>
            string.Equals(x.Status, "Draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Status, "Valid", StringComparison.OrdinalIgnoreCase)))
        {
            file.Status = "Rejected";
        }

        await SaveDraftManifestAsync(project, draft, token);
    }

    public async Task SaveDraftReviewAsync(GameProjectData project, GameDraftSession draft, string reviewText, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            throw new InvalidOperationException("Project path is not set. Save the project before saving draft review.");
        }
        if (string.IsNullOrWhiteSpace(draft.SessionId))
        {
            throw new InvalidOperationException("Draft session id is empty. Cannot save review.");
        }

        var folder = Path.Combine(project.Summary.ProjectPath, "drafts", draft.SessionId);
        Directory.CreateDirectory(folder);
        var reviewPath = Path.Combine(folder, "review.txt");
        await File.WriteAllTextAsync(reviewPath, reviewText, new UTF8Encoding(false), token);

        draft.ReviewOutputFile = ToProjectRelativePath(project, reviewPath);
        draft.ReviewCreatedAtUtc = DateTime.UtcNow;
        draft.ReviewSummary = BuildReviewSummary(reviewText);
        await SaveDraftManifestAsync(project, draft, token);
    }

    public async Task ApplyDraftAsync(GameProjectData project, GameDraftSession draft, CancellationToken token)
    {
        if (!draft.Validation.IsValid)
        {
            throw new InvalidOperationException("Draft has validation errors and cannot be applied.");
        }

        foreach (var file in draft.Files)
        {
            var path = Path.Combine(project.Summary.ProjectPath, file.RelativePath);
            if (!File.Exists(path))
            {
                file.Status = "Applied";
                continue;
            }

            switch (file.EntityType)
            {
                case "stats":
                    Upsert(project.Stats, await ReadJsonAsync<GameStatDefinition>(path, token), x => x.Id);
                    break;
                case "skills":
                    Upsert(project.Skills, await ReadJsonAsync<GameSkillDefinition>(path, token), x => x.Id);
                    break;
                case "items":
                    Upsert(project.Items, await ReadJsonAsync<GameItemDefinition>(path, token), x => x.Id);
                    break;
                case "equipment-slots":
                    Upsert(project.EquipmentSlots, await ReadJsonAsync<GameEquipmentSlotDefinition>(path, token), x => x.Id);
                    break;
                case "elements":
                    Upsert(project.Elements, await ReadJsonAsync<GameElementDefinition>(path, token), x => x.Id);
                    break;
                case "currencies":
                    Upsert(project.Currencies, await ReadJsonAsync<GameCurrencyDefinition>(path, token), x => x.Id);
                    break;
                case "variables":
                    Upsert(project.Variables, await ReadJsonAsync<GameVariableDefinition>(path, token), x => x.Id);
                    break;
                case "characters":
                    Upsert(project.Characters, await ReadJsonAsync<GameCharacter>(path, token), x => x.Id);
                    break;
                case "relationships":
                    Upsert(project.Relationships, await ReadJsonAsync<GameRelationshipDefinition>(path, token), x => x.CharacterId);
                    break;
                case "locations":
                    Upsert(project.Locations, await ReadJsonAsync<GameLocation>(path, token), x => x.Id);
                    break;
                case "location-connections":
                    Upsert(project.LocationConnections, await ReadJsonAsync<GameLocationConnection>(path, token), x => x.Id);
                    break;
                case "location-states":
                    Upsert(project.LocationStates, await ReadJsonAsync<GameLocationStateDefinition>(path, token), x => x.Id);
                    break;
                case "scenes":
                    Upsert(project.Scenes, await ReadJsonAsync<GameScene>(path, token), x => x.Id);
                    break;
                case "quests":
                    Upsert(project.Quests, await ReadJsonAsync<GameQuest>(path, token), x => x.Id);
                    break;
                case "encounters":
                    Upsert(project.Encounters, await ReadJsonAsync<GameEncounterDefinition>(path, token), x => x.Id);
                    break;
                case "actions":
                    Upsert(project.Actions, await ReadJsonAsync<GameActionDefinition>(path, token), x => x.Id);
                    break;
                case "formulas":
                    Upsert(project.Formulas, await ReadJsonAsync<GameFormulaDefinition>(path, token), x => x.Id);
                    break;
                case "status-effects":
                    Upsert(project.StatusEffects, await ReadJsonAsync<GameStatusEffectDefinition>(path, token), x => x.Id);
                    break;
                case "progression":
                    Upsert(project.ProgressionNodes, await ReadJsonAsync<GameProgressionNodeDefinition>(path, token), x => x.Id);
                    break;
                case "world-state":
                    GameWorldStateMergeService.MergeInto(project.WorldState, await ReadJsonAsync<GameWorldStateDefinition>(path, token));
                    break;
                case "mechanics":
                    project.Mechanics = await ReadJsonAsync<GameMechanicsDefinition>(path, token);
                    break;
                case "generation-preferences":
                    project.GenerationPreferences = await ReadJsonAsync<GameGenerationPreferences>(path, token);
                    break;
                case "image-prompts":
                    Upsert(project.ImagePrompts, await ReadJsonAsync<ImagePromptDefinition>(path, token), x => x.AssetId);
                    break;
            }

            file.Status = "Applied";
            await _changeLogService.AppendChangeAsync(project, new GameChangeRecord
            {
                Operation = "import",
                EntityType = file.EntityType,
                EntityId = file.EntityId,
                RelativePath = file.RelativePath,
                CreatedBy = "ai",
                ApprovedByUser = true,
                Notes = "Draft applied from session " + draft.SessionId
            }, token);
        }

        await SaveDraftManifestAsync(project, draft, token);
    }

    internal async Task<GameDraftSession> ExtractGeneratedProjectAsync(GameProjectData project, string stage, GameProjectData generated, string rawOutput, CancellationToken token)
    {
        var draft = await SaveRawDraftAsync(project, stage, string.Empty, rawOutput, token);
        var folder = Path.Combine(project.Summary.ProjectPath, "drafts", draft.SessionId);
        await WriteEntitiesAsync(project, generated, draft, folder, token);
        draft.Validation = _validator.Validate(generated);
        foreach (var file in draft.Files)
        {
            file.Status = draft.Validation.IsValid ? "Valid" : "Invalid";
        }

        await SaveDraftManifestAsync(project, draft, token);
        await SaveValidationReportAsync(project, draft, token);
        return draft;
    }

    internal async Task<GameDraftSession> ExtractImagePromptDraftsAsync(
        GameProjectData project,
        string stage,
        IReadOnlyCollection<ImagePromptDefinition> prompts,
        string rawOutput,
        CancellationToken token)
    {
        var draft = await SaveRawDraftAsync(project, stage, string.Empty, rawOutput, token);
        var folder = Path.Combine(project.Summary.ProjectPath, "drafts", draft.SessionId);

        foreach (var prompt in prompts)
        {
            if (string.IsNullOrWhiteSpace(prompt.AssetId))
            {
                prompt.AssetId = Ids.New("asset");
            }

            await WriteDraftEntityAsync(project, draft, folder, "image-prompts", prompt.AssetId, prompt, token);
        }

        var candidate = _cloneService.Clone(project);
        foreach (var prompt in prompts)
        {
            Upsert(candidate.ImagePrompts, prompt, x => x.AssetId);
        }

        draft.Validation = _validator.Validate(candidate);
        foreach (var file in draft.Files)
        {
            file.Status = draft.Validation.IsValid ? "Draft" : "Invalid";
        }

        await SaveDraftManifestAsync(project, draft, token);
        await SaveValidationReportAsync(project, draft, token);
        return draft;
    }

    private async Task WriteEntitiesAsync(GameProjectData project, GameProjectData generated, GameDraftSession draft, string folder, CancellationToken token)
    {
        foreach (var item in generated.Stats) await WriteDraftEntityAsync(project, draft, folder, "stats", item.Id, item, token);
        foreach (var item in generated.Skills) await WriteDraftEntityAsync(project, draft, folder, "skills", item.Id, item, token);
        foreach (var item in generated.Items) await WriteDraftEntityAsync(project, draft, folder, "items", item.Id, item, token);
        foreach (var item in generated.EquipmentSlots) await WriteDraftEntityAsync(project, draft, folder, "equipment-slots", item.Id, item, token);
        foreach (var item in generated.Elements) await WriteDraftEntityAsync(project, draft, folder, "elements", item.Id, item, token);
        foreach (var item in generated.Currencies) await WriteDraftEntityAsync(project, draft, folder, "currencies", item.Id, item, token);
        foreach (var item in generated.Variables) await WriteDraftEntityAsync(project, draft, folder, "variables", item.Id, item, token);
        foreach (var item in generated.Characters) await WriteDraftEntityAsync(project, draft, folder, "characters", item.Id, item, token);
        foreach (var item in generated.Relationships) await WriteDraftEntityAsync(project, draft, folder, "relationships", item.CharacterId, item, token);
        foreach (var item in generated.Locations) await WriteDraftEntityAsync(project, draft, folder, "locations", item.Id, item, token);
        foreach (var item in generated.LocationConnections) await WriteDraftEntityAsync(project, draft, folder, "location-connections", item.Id, item, token);
        foreach (var item in generated.LocationStates) await WriteDraftEntityAsync(project, draft, folder, "location-states", item.Id, item, token);
        foreach (var item in generated.Scenes) await WriteDraftEntityAsync(project, draft, folder, "scenes", item.Id, item, token);
        foreach (var item in generated.Quests) await WriteDraftEntityAsync(project, draft, folder, "quests", item.Id, item, token);
        foreach (var item in generated.Encounters) await WriteDraftEntityAsync(project, draft, folder, "encounters", item.Id, item, token);
        foreach (var item in generated.Actions) await WriteDraftEntityAsync(project, draft, folder, "actions", item.Id, item, token);
        foreach (var item in generated.Formulas) await WriteDraftEntityAsync(project, draft, folder, "formulas", item.Id, item, token);
        foreach (var item in generated.StatusEffects) await WriteDraftEntityAsync(project, draft, folder, "status-effects", item.Id, item, token);
        foreach (var item in generated.ProgressionNodes) await WriteDraftEntityAsync(project, draft, folder, "progression", item.Id, item, token);
        if (HasWorldStateData(generated.WorldState)) await WriteDraftEntityAsync(project, draft, folder, "world-state", "world-state", generated.WorldState, token);
        if (HasMechanicsData(generated.Mechanics)) await WriteDraftEntityAsync(project, draft, folder, "mechanics", "mechanics", generated.Mechanics, token);
        if (HasGenerationPreferencesData(generated.GenerationPreferences)) await WriteDraftEntityAsync(project, draft, folder, "generation-preferences", "generation-preferences", generated.GenerationPreferences, token);
        foreach (var item in generated.ImagePrompts) await WriteDraftEntityAsync(project, draft, folder, "image-prompts", item.AssetId, item, token);
    }

    private async Task WriteDraftEntityAsync<T>(GameProjectData project, GameDraftSession draft, string folder, string entityType, string entityId, T value, CancellationToken token)
    {
        var safeId = GameProjectManifestService.SafeId(entityId, entityType);
        var path = Path.Combine(folder, entityType, safeId + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, _jsonOptions), new UTF8Encoding(false), token);
        draft.Files.Add(new GameDraftFile
        {
            EntityType = entityType,
            EntityId = entityId,
            RelativePath = ToProjectRelativePath(project, path)
        });
    }

    private string CreateDraftFolder(GameProjectData project, string stage, out string sessionId)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            throw new InvalidOperationException("Project path is not set. Save the project before creating drafts.");
        }

        sessionId = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{GameProjectManifestService.SafeId(stage, "draft")}";
        var folder = Path.Combine(project.Summary.ProjectPath, "drafts", sessionId);
        Directory.CreateDirectory(folder);
        return folder;
    }

    internal async Task SaveDraftManifestAsync(GameProjectData project, GameDraftSession draft, CancellationToken token)
    {
        var folder = Path.Combine(project.Summary.ProjectPath, "drafts", draft.SessionId);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "draft-manifest.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(draft, _jsonOptions), new UTF8Encoding(false), token);
    }

    internal async Task SaveValidationReportAsync(GameProjectData project, GameDraftSession draft, CancellationToken token)
    {
        var folder = Path.Combine(project.Summary.ProjectPath, "drafts", draft.SessionId);
        var lines = new List<string>
        {
            "Valid: " + draft.Validation.IsValid,
            "Errors:",
        };
        lines.AddRange(draft.Validation.Errors.Select(x => "- " + x));
        lines.Add("Warnings:");
        lines.AddRange(draft.Validation.Warnings.Select(x => "- " + x));
        await File.WriteAllLinesAsync(Path.Combine(folder, "validation-report.txt"), lines, new UTF8Encoding(false), token);
    }

    private async Task<T> ReadJsonAsync<T>(string path, CancellationToken token)
    {
        var json = await File.ReadAllTextAsync(path, token);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Draft entity JSON is empty: " + path);
    }

    private static void Upsert<T>(List<T> target, T item, Func<T, string> getId)
    {
        var id = getId(item);
        var index = target.FindIndex(x => string.Equals(getId(x), id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            target[index] = item;
        }
        else
        {
            target.Add(item);
        }
    }

    private static string ToProjectRelativePath(GameProjectData project, string path)
    {
        return Path.GetRelativePath(project.Summary.ProjectPath, path).Replace('\\', '/');
    }

    private static string BuildReviewSummary(string reviewText)
    {
        var summary = reviewText
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?
            .Trim() ?? string.Empty;
        return summary.Length <= 200 ? summary : summary[..200];
    }

    private static bool HasMechanicsData(GameMechanicsDefinition mechanics)
    {
        return mechanics.EnableTurns
            || mechanics.EnableStatusEffects
            || mechanics.EnableProgression
            || mechanics.EnableActionPanel
            || mechanics.EnableDiceRandomness
            || mechanics.DefaultActionPoints != 1
            || !string.IsNullOrWhiteSpace(mechanics.ActionPointStatId)
            || !string.IsNullOrWhiteSpace(mechanics.InitiativeFormulaId)
            || HasExperienceData(mechanics.Experience)
            || !string.IsNullOrWhiteSpace(mechanics.Notes);
    }

    private static bool HasExperienceData(GameExperienceDefinition experience)
    {
        return experience.EnablePlayerExperience
            || experience.EnableSkillExperience
            || experience.InitialPlayerLevel != 1
            || experience.InitialPlayerExperience != 0
            || experience.MaxPlayerLevel != 100
            || !string.IsNullOrWhiteSpace(experience.PlayerExperienceToNextLevelFormulaId)
            || !string.IsNullOrWhiteSpace(experience.PlayerExperienceToNextLevelFormulaExpression)
            || !string.IsNullOrWhiteSpace(experience.SkillExperienceToNextLevelFormulaId)
            || !string.IsNullOrWhiteSpace(experience.SkillExperienceToNextLevelFormulaExpression)
            || !string.IsNullOrWhiteSpace(experience.DefaultPlayerExperienceRewardFormulaId)
            || !string.IsNullOrWhiteSpace(experience.DefaultPlayerExperienceRewardFormulaExpression)
            || experience.PlayerLevelUpEffects.Count > 0
            || experience.SkillLevelUpEffects.Count > 0
            || !string.IsNullOrWhiteSpace(experience.Notes);
    }

    private static bool HasGenerationPreferencesData(GameGenerationPreferences preferences)
    {
        return !string.IsNullOrWhiteSpace(preferences.GeneralGameplayText)
            || !string.IsNullOrWhiteSpace(preferences.SkillDesignText)
            || !string.IsNullOrWhiteSpace(preferences.ProgressionDesignText)
            || !string.IsNullOrWhiteSpace(preferences.CombatDesignText)
            || !string.IsNullOrWhiteSpace(preferences.AtmosphereDesignText)
            || !string.IsNullOrWhiteSpace(preferences.BalanceText)
            || !string.IsNullOrWhiteSpace(preferences.ForbiddenDesignText)
            || !string.IsNullOrWhiteSpace(preferences.Notes);
    }

    internal static bool HasWorldStateData(GameWorldStateDefinition worldState)
    {
        return worldState.Enabled
            || !string.Equals(worldState.GenreProfile, "generic", StringComparison.OrdinalIgnoreCase)
            || worldState.Time.Enabled
            || worldState.Time.Segments.Count > 0
            || worldState.Aspects.Count > 0
            || worldState.AmbientEvents.Count > 0
            || worldState.Rules.Count > 0
            || !string.IsNullOrWhiteSpace(worldState.Notes);
    }

    private static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        var firstObject = trimmed.IndexOf('{');
        var firstArray = trimmed.IndexOf('[');
        var start = firstArray >= 0 && (firstObject < 0 || firstArray < firstObject) ? firstArray : firstObject;
        if (start < 0)
        {
            return trimmed;
        }

        var endObject = trimmed.LastIndexOf('}');
        var endArray = trimmed.LastIndexOf(']');
        var end = Math.Max(endObject, endArray);
        return end > start ? trimmed[start..(end + 1)] : trimmed;
    }
}
