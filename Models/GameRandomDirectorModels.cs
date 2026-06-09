namespace LMStudioSillyTavernWorldBuilder.Models;

public sealed class GameRandomDirectorReport
{
    public string Summary { get; set; } = string.Empty;
    public List<GameRandomDirectorCoverageItem> Coverage { get; set; } = new();
    public List<GameRandomDirectorWarning> Warnings { get; set; } = new();
    public List<GameRandomDirectorRecommendation> Recommendations { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameRandomDirectorCoverageItem
{
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeId { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public int RuleCount { get; set; }
    public int AverageWeight { get; set; }
    public List<string> EventIds { get; set; } = new();
}

public sealed class GameRandomDirectorWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public List<string> EntityIds { get; set; } = new();
}

public sealed class GameRandomDirectorRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetStage { get; set; } = string.Empty;
    public int Priority { get; set; }
    public List<string> TargetSystems { get; set; } = new();
}
