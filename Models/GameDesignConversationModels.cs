namespace LMStudioSillyTavernWorldBuilder.Models;

public sealed class GameDesignConversationHistory
{
    public List<GameDesignConversationTurn> Turns { get; set; } = new();
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameDesignConversationTurn
{
    public string Id { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string AssistantReply { get; set; } = string.Empty;
    public List<string> ExtractedKnowledgeEntryIds { get; set; } = new();
    public List<GameDesignConversationQuestion> FollowUpQuestions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string FocusTopic { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public sealed class GameDesignConversationResult
{
    public string AssistantReply { get; set; } = string.Empty;
    public List<GameDesignConversationMemoryEntry> MemoryEntries { get; set; } = new();
    public List<GameDesignConversationQuestion> FollowUpQuestions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool IsSuccess => Errors.Count == 0;
}

public sealed class GameDesignConversationMemoryEntry
{
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public GameDesignConversationMemoryStatus Status { get; set; } = GameDesignConversationMemoryStatus.Proposed;
    public GameDesignKnowledgeImportance Importance { get; set; } = GameDesignKnowledgeImportance.Normal;
    public string Source { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> RelatedEntityIds { get; set; } = new();
    public List<string> AffectsSystems { get; set; } = new();
}

public sealed class GameDesignConversationQuestion
{
    public string Id { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public GameDesignConversationQuestionPriority Priority { get; set; } = GameDesignConversationQuestionPriority.Normal;
    public bool CanSkip { get; set; } = true;
    public List<string> SuggestedOptions { get; set; } = new();
}

public enum GameDesignConversationMemoryStatus
{
    Accepted,
    Proposed,
    Rejected,
    Assumption,
    NeedsClarification
}

public enum GameDesignConversationQuestionPriority
{
    Low,
    Normal,
    High
}
