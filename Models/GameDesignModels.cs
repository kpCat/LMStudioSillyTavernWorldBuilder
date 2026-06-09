using System.Text.Json.Serialization;

namespace LMStudioSillyTavernWorldBuilder.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameCreationMode
{
    Manual,
    Collaborative,
    AutopilotWithReview,
    QuickPrototype
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameDesignSlotValueSource
{
    Empty,
    User,
    LlmAssumption,
    ProgramDefault
}

public sealed class GameDesignProfile
{
    public string InitialIdea { get; set; } = string.Empty;
    public GameCreationMode CreationMode { get; set; } = GameCreationMode.Collaborative;
    public List<GameDesignSlot> Slots { get; set; } = new();
    public List<string> UserPriorities { get; set; } = new();
    public List<string> UserRestrictions { get; set; } = new();
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameDesignSlot
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public GameDesignSlotValueSource Source { get; set; } = GameDesignSlotValueSource.Empty;
    public double Confidence { get; set; }
    public bool IsRequired { get; set; }
    public bool CanBeAssumedByLlm { get; set; } = true;
    public int Priority { get; set; } = 100;
    public List<string> SuggestedOptions { get; set; } = new();
    public List<string> AffectsSystems { get; set; } = new();
    public List<string> DependsOn { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameDesignQuestion
{
    public string SlotId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string HelpText { get; set; } = string.Empty;
    public List<string> SuggestedOptions { get; set; } = new();
    public int Priority { get; set; } = 100;
}

public sealed class GameCreationPlan
{
    public string Summary { get; set; } = string.Empty;
    public List<GameCreationPlanStep> Steps { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameCreationPlanStep
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public int Priority { get; set; } = 100;
    public List<string> DependsOn { get; set; } = new();
    public List<string> TargetSystems { get; set; } = new();
}
