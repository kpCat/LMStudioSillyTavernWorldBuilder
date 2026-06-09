namespace LMStudioSillyTavernWorldBuilder.Models;

public static class GameMvpReadinessSeverity
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
}

public static class GameMvpReadinessStatus
{
    public const string Empty = "empty";
    public const string Skeleton = "skeleton";
    public const string Draftable = "draftable";
    public const string Playable = "playable";
    public const string NeedsReview = "needsReview";
}

public sealed class GameMvpReadinessReport
{
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string OverallStatus { get; set; } = GameMvpReadinessStatus.Empty;
    public int CompletionPercent { get; set; }
    public List<GameMvpReadinessIssue> Issues { get; set; } = new();
    public List<GameMvpRecommendation> Recommendations { get; set; } = new();
    public List<GameMvpStageStatus> Stages { get; set; } = new();
    public string? NextRecommendedStage { get; set; }
    public string? NextRecommendedCategory { get; set; }
    public int NextRecommendedCount { get; set; }
    public bool HasBlockingProblems { get; set; }
}

public sealed class GameMvpReadinessIssue
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = GameMvpReadinessSeverity.Warning;
    public string Scope { get; set; } = string.Empty;
    public List<string> EntityIds { get; set; } = new();
}

public sealed class GameMvpRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int SuggestedCount { get; set; }
    public string SuggestedCategory { get; set; } = string.Empty;
    public List<string> TargetSystems { get; set; } = new();
}

public sealed class GameMvpStageStatus
{
    public string Stage { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ExistingCount { get; set; }
    public int TargetMinimum { get; set; }
    public bool IsSatisfied { get; set; }
    public int Priority { get; set; }
    public string Reason { get; set; } = string.Empty;
}
