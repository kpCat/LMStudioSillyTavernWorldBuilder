namespace LMStudioSillyTavernWorldBuilder.Models;

public static class GameBalanceSeverity
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
}

public sealed class GameBalanceReport
{
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int RequestedSimulationCount { get; set; }
    public string OverallSeverity { get; set; } = GameBalanceSeverity.Info;
    public List<GameBalanceIssue> Issues { get; set; } = new();
    public List<GameBalanceRecommendation> Recommendations { get; set; } = new();
    public GameCombatBalanceReport Combat { get; set; } = new();
    public GameEconomyBalanceReport Economy { get; set; } = new();
    public GameProgressionBalanceReport Progression { get; set; } = new();
    public GameResourceBalanceReport Resources { get; set; } = new();
}

public sealed class GameBalanceIssue
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = GameBalanceSeverity.Warning;
    public string Scope { get; set; } = string.Empty;
    public List<string> EntityIds { get; set; } = new();
}

public sealed class GameBalanceRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetStage { get; set; } = "rebalance";
    public int Priority { get; set; }
    public List<string> TargetSystems { get; set; } = new();
    public List<string> EntityIds { get; set; } = new();
}

public sealed class GameCombatBalanceReport
{
    public int EncounterCount { get; set; }
    public int SimulatedEncounterCount { get; set; }
    public List<GameCombatEncounterSimulationReport> Encounters { get; set; } = new();
}

public sealed class GameCombatEncounterSimulationReport
{
    public string EncounterId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RequestedRuns { get; set; }
    public int Runs { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Stalls { get; set; }
    public int Errors { get; set; }
    public double WinRatePercent { get; set; }
    public double AverageRounds { get; set; }
    public int MinRounds { get; set; }
    public int MaxRounds { get; set; }
    public double? AveragePlayerHealthEnd { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<GameCombatSimulationRunResult> RunResults { get; set; } = new();
}

public sealed class GameCombatSimulationRunResult
{
    public int RunIndex { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public int Rounds { get; set; }
    public int PlayerHealthEnd { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class GameEconomyBalanceReport
{
    public int CurrencyCount { get; set; }
    public int PricedItemCount { get; set; }
    public int CurrencySourceCount { get; set; }
    public int CurrencySinkCount { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public sealed class GameProgressionBalanceReport
{
    public bool ProgressionEnabled { get; set; }
    public int NodeCount { get; set; }
    public int ExperienceSourceCount { get; set; }
    public int UnlockSourceCount { get; set; }
    public int DisconnectedNodeCount { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public sealed class GameResourceBalanceReport
{
    public int ResourceStatCount { get; set; }
    public int ResourceCostCount { get; set; }
    public int ResourceRecoveryCount { get; set; }
    public string HealthStatId { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}
