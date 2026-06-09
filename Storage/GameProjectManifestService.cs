using System.Text;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Storage;

internal sealed class GameProjectManifestService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public GameProjectManifest BuildManifest(GameProjectData project)
    {
        return new GameProjectManifest
        {
            Stats = project.Stats.Select(x => GetEntityRelativePath("stats", EnsureId(x.Id, "stat", id => x.Id = id))).ToList(),
            Skills = project.Skills.Select(x => GetEntityRelativePath("skills", EnsureId(x.Id, "skill", id => x.Id = id))).ToList(),
            Items = project.Items.Select(x => GetEntityRelativePath("items", EnsureId(x.Id, "item", id => x.Id = id))).ToList(),
            EquipmentSlots = project.EquipmentSlots.Select(x => GetEntityRelativePath("equipment-slots", EnsureId(x.Id, "slot", id => x.Id = id))).ToList(),
            Elements = project.Elements.Select(x => GetEntityRelativePath("elements", EnsureId(x.Id, "element", id => x.Id = id))).ToList(),
            Currencies = project.Currencies.Select(x => GetEntityRelativePath("currencies", EnsureId(x.Id, "currency", id => x.Id = id))).ToList(),
            Variables = project.Variables.Select(x => GetEntityRelativePath("variables", EnsureId(x.Id, "variable", id => x.Id = id))).ToList(),
            Characters = project.Characters.Select(x => GetEntityRelativePath("characters", EnsureId(x.Id, "character", id => x.Id = id))).ToList(),
            Relationships = project.Relationships.Select(x => GetEntityRelativePath("relationships", EnsureId(x.CharacterId, "relationship", id => x.CharacterId = id))).ToList(),
            Locations = project.Locations.Select(x => GetEntityRelativePath("locations", EnsureId(x.Id, "location", id => x.Id = id))).ToList(),
            LocationConnections = project.LocationConnections.Select(x => GetEntityRelativePath("location-connections", EnsureId(x.Id, "connection", id => x.Id = id))).ToList(),
            LocationStates = project.LocationStates.Select(x => GetEntityRelativePath("location-states", EnsureId(x.Id, "location_state", id => x.Id = id))).ToList(),
            Scenes = project.Scenes.Select(x => GetEntityRelativePath("scenes", EnsureId(x.Id, "scene", id => x.Id = id))).ToList(),
            Quests = project.Quests.Select(x => GetEntityRelativePath("quests", EnsureId(x.Id, "quest", id => x.Id = id))).ToList(),
            Encounters = project.Encounters.Select(x => GetEntityRelativePath("encounters", EnsureId(x.Id, "encounter", id => x.Id = id))).ToList(),
            Actions = project.Actions.Select(x => GetEntityRelativePath("actions", EnsureId(x.Id, "action", id => x.Id = id))).ToList(),
            Formulas = project.Formulas.Select(x => GetEntityRelativePath("formulas", EnsureId(x.Id, "formula", id => x.Id = id))).ToList(),
            StatusEffects = project.StatusEffects.Select(x => GetEntityRelativePath("status-effects", EnsureId(x.Id, "status", id => x.Id = id))).ToList(),
            ProgressionNodes = project.ProgressionNodes.Select(x => GetEntityRelativePath("progression", EnsureId(x.Id, "progression", id => x.Id = id))).ToList(),
            WorldState = "data/world-state.json",
            Mechanics = "data/mechanics.json",
            Combat = project.Combat == null ? string.Empty : "data/combat/combat.json",
            GenerationPreferences = "design/generation-preferences.json",
            ImagePrompts = project.ImagePrompts.Select(x => GetPromptRelativePath("image-prompts", EnsureId(x.AssetId, "asset", id => x.AssetId = id))).ToList(),
            GeneratedImageCandidates = project.GeneratedImageCandidates.Select(x => GetPromptRelativePath("generated-candidates", EnsureId(x.CandidateId, "candidate", id => x.CandidateId = id))).ToList(),
            AssetLinks = project.AssetLinks.Select(x => GetPromptRelativePath("asset-links", EnsureId(x.AssetId, "asset", id => x.AssetId = id))).ToList(),
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public async Task SaveManifestAsync(string projectFolder, GameProjectManifest manifest, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(projectFolder, "manifest.json");
        var json = JsonSerializer.Serialize(manifest, _jsonOptions);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), cancellationToken);
    }

    public async Task<GameProjectManifest> LoadManifestAsync(string projectFolder, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(projectFolder, "manifest.json");
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<GameProjectManifest>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Manifest JSON is empty or invalid.");
    }

    public string GetEntityRelativePath(string entityType, string entityId)
    {
        return Path.Combine("data", entityType, SafeId(entityId, entityType) + ".json").Replace('\\', '/');
    }

    public string GetPromptRelativePath(string promptType, string entityId)
    {
        return Path.Combine("prompts", promptType, SafeId(entityId, promptType) + ".json").Replace('\\', '/');
    }

    public static string SafeId(string id, string fallbackPrefix)
    {
        var source = string.IsNullOrWhiteSpace(id) ? Ids.New(fallbackPrefix) : id.Trim();
        var builder = new StringBuilder(source.Length);
        foreach (var ch in source.ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_');
        }

        var safe = builder.ToString();
        while (safe.Contains("__", StringComparison.Ordinal))
        {
            safe = safe.Replace("__", "_", StringComparison.Ordinal);
        }

        safe = safe.Trim('_', '-', '.', ' ');
        return string.IsNullOrWhiteSpace(safe) ? Ids.New(fallbackPrefix) : safe;
    }

    private static string EnsureId(string id, string fallbackPrefix, Action<string> assign)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        var created = Ids.New(fallbackPrefix);
        assign(created);
        return created;
    }
}
