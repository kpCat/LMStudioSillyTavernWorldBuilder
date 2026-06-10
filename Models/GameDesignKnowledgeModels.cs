using System.Text.Json.Serialization;

namespace LMStudioSillyTavernWorldBuilder.Models;

public sealed class GameDesignKnowledgeBase
{
    public List<GameDesignKnowledgeEntry> Entries { get; set; } = new();
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameDesignKnowledgeEntry
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public GameDesignKnowledgeEntryKind Kind { get; set; } = GameDesignKnowledgeEntryKind.Note;
    public GameDesignKnowledgeEntryStatus Status { get; set; } = GameDesignKnowledgeEntryStatus.Proposed;
    public GameDesignKnowledgeImportance Importance { get; set; } = GameDesignKnowledgeImportance.Normal;
    public string Source { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> RelatedEntityIds { get; set; } = new();
    public List<string> AffectsSystems { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameDesignKnowledgeQuery
{
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> RelatedEntityIds { get; set; } = new();
    public List<string> AffectsSystems { get; set; } = new();
    public List<GameDesignKnowledgeEntryStatus> IncludeStatuses { get; set; } = new();
    public List<GameDesignKnowledgeEntryKind> IncludeKinds { get; set; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameDesignKnowledgeEntryKind
{
    Decision,
    Constraint,
    Preference,
    Rejection,
    Assumption,
    Question,
    Answer,
    EntityRequirement,
    InteractionRule,
    Note
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameDesignKnowledgeEntryStatus
{
    Accepted,
    Proposed,
    Rejected,
    Superseded,
    NeedsClarification
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameDesignKnowledgeImportance
{
    Low,
    Normal,
    High,
    Critical
}
