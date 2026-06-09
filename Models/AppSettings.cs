using LMStudioSillyTavernWorldBuilder.Providers;

namespace LMStudioSillyTavernWorldBuilder.Models;

public sealed class AppSettings
{
    public string GamesRootPath { get; set; } = "";
    public LmStudioSettings LmStudio { get; set; } = new();
    public List<LmStudioModelProfile> LmStudioProfiles { get; set; } = new();
    public string ActiveLmStudioProfileId { get; set; } = string.Empty;
    public bool AutoSelectLmStudioProfile { get; set; }
    public FooocusSettings Fooocus { get; set; } = new();
    public GenerationUiSettings Generation { get; set; } = new();
}

public static class LmStudioProfileRole
{
    public const string Default = "default";
    public const string Discussion = "discussion";
    public const string JsonStrict = "json_strict";
    public const string Creative = "creative";
    public const string LargeContext = "large_context";
    public const string Review = "review";
    public const string Balance = "balance";
}

public sealed class LmStudioModelProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = LmStudioProfileRole.Default;
    public bool IsDefault { get; set; }
    public LmStudioSettings Settings { get; set; } = new();
    public GenerationUiSettings Generation { get; set; } = new();

    public override string ToString()
    {
        return IsDefault ? $"{Name} [default, {Role}]" : $"{Name} [{Role}]";
    }
}

public sealed class GenerationUiSettings
{
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.9;
    public double MinP { get; set; } = 0.05;
    public int TopK { get; set; } = 40;
    public double RepeatPenalty { get; set; } = 1.05;
    public double PresencePenalty { get; set; } = 0.0;
    public int MaxTokens { get; set; } = 4096;
    public int MaxInputContextTokens { get; set; } = 32768;
    public int MaxOutputTokens { get; set; } = 4096;
    public int ApproxCharsPerToken { get; set; } = 4;
}
