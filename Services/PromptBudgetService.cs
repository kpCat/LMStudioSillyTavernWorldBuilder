using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class PromptBudgetService
{
    public int EstimateTokens(string text, int approxCharsPerToken = 4)
    {
        var divisor = Math.Max(1, approxCharsPerToken);
        return (int)Math.Ceiling((text ?? string.Empty).Length / (double)divisor);
    }

    public int EstimateTokensConservative(string text, int configuredApproxCharsPerToken)
    {
        var safeText = text ?? string.Empty;
        var divisor = Math.Max(1, configuredApproxCharsPerToken);
        if (ContainsSignificantCyrillic(safeText))
        {
            divisor = Math.Min(divisor, 2);
        }

        return (int)Math.Ceiling(safeText.Length / (double)Math.Max(1, divisor));
    }

    public int EstimateMessagesConservative(IEnumerable<ChatMessage> messages, int configuredApproxCharsPerToken)
    {
        var total = 2;
        foreach (var message in messages)
        {
            total += 4;
            total += EstimateTokensConservative(message.Role, configuredApproxCharsPerToken);
            total += EstimateTokensConservative(message.Content, configuredApproxCharsPerToken);
        }

        return total;
    }

    public int CalculateSafePromptBudgetTokens(int contextTokens, int maxOutputTokens)
    {
        var context = Math.Max(1024, contextTokens);
        var outputReserve = Math.Clamp(maxOutputTokens > 0 ? maxOutputTokens : 4096, 512, Math.Max(512, context / 2));
        var safetyMargin = Math.Max(1024, context / 6);
        return Math.Max(512, context - outputReserve - safetyMargin);
    }

    public string SerializeWithinBudget(Func<int, object> buildContext, int maxInputContextTokens, int approxCharsPerToken, JsonSerializerOptions jsonOptions)
    {
        var maxTokens = Math.Max(128, maxInputContextTokens);
        var budgetItems = 100;
        var text = JsonSerializer.Serialize(buildContext(budgetItems), jsonOptions);
        while (budgetItems > 1 && EstimateTokensConservative(text, approxCharsPerToken) > maxTokens)
        {
            budgetItems = Math.Max(1, budgetItems / 2);
            text = JsonSerializer.Serialize(buildContext(budgetItems), jsonOptions);
        }

        return text;
    }

    private static bool ContainsSignificantCyrillic(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var cyrillic = 0;
        foreach (var ch in text)
        {
            if (ch >= '\u0400' && ch <= '\u04FF')
            {
                cyrillic++;
                if (cyrillic >= 8)
                {
                    return true;
                }
            }
        }

        return cyrillic > 0 && cyrillic >= text.Length / 8;
    }
}
