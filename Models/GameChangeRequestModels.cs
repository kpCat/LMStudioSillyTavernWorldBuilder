using System.Text.Json.Serialization;

namespace LMStudioSillyTavernWorldBuilder.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameChangeRequestIntent
{
    AddContent,
    ExpandContent,
    Rebalance,
    RewriteTone,
    ImproveRandomness,
    ImproveCombat,
    ImproveDialogue,
    ImproveInventory,
    ImproveProgression,
    ImproveMapTravel,
    ImproveEconomy,
    RemoveOrReduceContent,
    FixIssue,
    Other
}

public sealed class GameChangeRequestImpactReport
{
    public string UserRequest { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Intent { get; set; } = GameChangeRequestIntent.Other.ToString();
    public double Confidence { get; set; }
    public List<GameChangeRequestAffectedSystem> AffectedSystems { get; set; } = new();
    public List<string> AffectedEntityIds { get; set; } = new();
    public List<GameChangeRequestRisk> Risks { get; set; } = new();
    public List<string> MissingContextQuestions { get; set; } = new();
    public List<string> RecommendedPatchStages { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameChangeRequestAffectedSystem
{
    public string SystemId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public List<string> EntityIds { get; set; } = new();
}

public sealed class GameChangeRequestRisk
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public List<string> EntityIds { get; set; } = new();
}

public sealed class GameChangeRequestPatchPlan
{
    public string Title { get; set; } = string.Empty;
    public string UserRequest { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<GameChangeRequestPlanStep> Steps { get; set; } = new();
    public List<string> ContextNotes { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameChangeRequestPlanStep
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetStage { get; set; } = string.Empty;
    public int Priority { get; set; }
    public List<string> TargetSystems { get; set; } = new();
    public List<string> EntityIds { get; set; } = new();
    public bool MustUseDraftWorkflow { get; set; } = true;
}
