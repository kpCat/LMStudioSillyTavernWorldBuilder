using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Models;

public sealed class GameProjectRootDocument
{
    public int SchemaVersion { get; set; } = 2;
    public string ProjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string StartSceneId { get; set; } = string.Empty;
    public string Language { get; set; } = "ru";
    public string DataLayout { get; set; } = "split-json";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameProjectManifest
{
    public int SchemaVersion { get; set; } = 2;
    public List<string> Stats { get; set; } = new();
    public List<string> Skills { get; set; } = new();
    public List<string> Items { get; set; } = new();
    public List<string> EquipmentSlots { get; set; } = new();
    public List<string> Elements { get; set; } = new();
    public List<string> Currencies { get; set; } = new();
    public List<string> Variables { get; set; } = new();
    public List<string> Characters { get; set; } = new();
    public List<string> Relationships { get; set; } = new();
    public List<string> Locations { get; set; } = new();
    public List<string> LocationConnections { get; set; } = new();
    public List<string> LocationStates { get; set; } = new();
    public List<string> Scenes { get; set; } = new();
    public List<string> Quests { get; set; } = new();
    public List<string> Encounters { get; set; } = new();
    public List<string> Actions { get; set; } = new();
    public List<string> Formulas { get; set; } = new();
    public List<string> StatusEffects { get; set; } = new();
    public List<string> ProgressionNodes { get; set; } = new();
    public string WorldState { get; set; } = string.Empty;
    public string Mechanics { get; set; } = string.Empty;
    public string Combat { get; set; } = string.Empty;
    public string GenerationPreferences { get; set; } = string.Empty;
    public List<string> ImagePrompts { get; set; } = new();
    public List<string> GeneratedImageCandidates { get; set; } = new();
    public List<string> AssetLinks { get; set; } = new();
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameDraftSession
{
    public string SessionId { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string UserRequest { get; set; } = string.Empty;
    public string RawOutputFile { get; set; } = string.Empty;
    public string ReviewOutputFile { get; set; } = string.Empty;
    public DateTime? ReviewCreatedAtUtc { get; set; }
    public string ReviewSummary { get; set; } = string.Empty;
    public List<GameDraftFile> Files { get; set; } = new();
    public GameProjectValidationResult Validation { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameDraftFile
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
}

public sealed class GameChangeRecord
{
    public string OperationId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "user";
    public bool ApprovedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
}
