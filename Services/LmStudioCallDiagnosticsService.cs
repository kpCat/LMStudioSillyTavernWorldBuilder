using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Providers;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class LmStudioCallDiagnosticsService
{
    private readonly PromptBudgetService _promptBudgetService = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public LmStudioCallDiagnosticRecord CreateSuccessRecord(
        string stage,
        LmStudioSettings lmSettings,
        GenerationSettings generationSettings,
        IReadOnlyList<ChatMessage> messages,
        int maxInputContextTokens,
        int approxCharsPerToken,
        long elapsedMilliseconds,
        string responseText)
    {
        var requestChars = CountMessageCharacters(messages);
        var safeResponseText = responseText ?? string.Empty;
        var responseChars = safeResponseText.Length;
        return CreateBaseRecord(stage, lmSettings, generationSettings, messages, requestChars, maxInputContextTokens, approxCharsPerToken, elapsedMilliseconds) with
        {
            Success = true,
            ResponseCharacterCount = responseChars,
            EstimatedResponseTokens = _promptBudgetService.EstimateTokensConservative(safeResponseText, approxCharsPerToken)
        };
    }

    public LmStudioCallDiagnosticRecord CreateFailureRecord(
        string stage,
        LmStudioSettings lmSettings,
        GenerationSettings generationSettings,
        IReadOnlyList<ChatMessage> messages,
        int maxInputContextTokens,
        int approxCharsPerToken,
        long elapsedMilliseconds,
        Exception exception)
    {
        var requestChars = CountMessageCharacters(messages);
        return CreateBaseRecord(stage, lmSettings, generationSettings, messages, requestChars, maxInputContextTokens, approxCharsPerToken, elapsedMilliseconds) with
        {
            Success = false,
            ErrorMessage = ShortError(exception.Message)
        };
    }

    public async Task AppendAsync(string projectPath, LmStudioCallDiagnosticRecord record, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return;
        }

        var folder = Path.Combine(projectPath, "prompts", "lm-calls");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "lm-call-log.jsonl");
        var line = JsonSerializer.Serialize(record, _jsonOptions);
        await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken);
    }

    public int EstimateTokens(string text, int approxCharsPerToken)
    {
        return _promptBudgetService.EstimateTokensConservative(text, approxCharsPerToken);
    }

    private LmStudioCallDiagnosticRecord CreateBaseRecord(
        string stage,
        LmStudioSettings lmSettings,
        GenerationSettings generationSettings,
        IReadOnlyList<ChatMessage> messages,
        int requestCharacterCount,
        int maxInputContextTokens,
        int approxCharsPerToken,
        long elapsedMilliseconds)
    {
        return new LmStudioCallDiagnosticRecord
        {
            TimestampUtc = DateTime.UtcNow,
            Stage = stage,
            Endpoint = LmStudioService.BuildChatCompletionsUrl(lmSettings.Endpoint),
            ModelId = lmSettings.ModelId.Trim(),
            RequestMessageCount = messages.Count,
            RequestCharacterCount = requestCharacterCount,
            EstimatedInputTokens = _promptBudgetService.EstimateTokensConservative(string.Concat(messages.Select(x => x.Content ?? string.Empty)), approxCharsPerToken),
            MaxInputContextTokens = maxInputContextTokens,
            MaxOutputTokens = generationSettings.MaxTokens,
            Temperature = generationSettings.Temperature,
            TopP = generationSettings.TopP,
            MinP = generationSettings.MinP,
            TopK = generationSettings.TopK,
            RepeatPenalty = generationSettings.RepeatPenalty,
            PresencePenalty = generationSettings.PresencePenalty,
            ElapsedMilliseconds = elapsedMilliseconds
        };
    }

    private static int CountMessageCharacters(IEnumerable<ChatMessage> messages)
    {
        return messages.Sum(x => (x.Content ?? string.Empty).Length);
    }

    private static string ShortError(string message)
    {
        var normalized = string.Join(" ", (message ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 300 ? normalized : normalized[..300];
    }
}

internal sealed record LmStudioCallDiagnosticRecord
{
    public DateTime TimestampUtc { get; init; }
    public string Stage { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public int RequestMessageCount { get; init; }
    public int RequestCharacterCount { get; init; }
    public int EstimatedInputTokens { get; init; }
    public int MaxInputContextTokens { get; init; }
    public int MaxOutputTokens { get; init; }
    public double Temperature { get; init; }
    public double TopP { get; init; }
    public double MinP { get; init; }
    public int TopK { get; init; }
    public double RepeatPenalty { get; init; }
    public double PresencePenalty { get; init; }
    public bool Success { get; init; }
    public long ElapsedMilliseconds { get; init; }
    public int ResponseCharacterCount { get; init; }
    public int EstimatedResponseTokens { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
