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

    public string SerializeWithinBudget(Func<int, object> buildContext, int maxInputContextTokens, int approxCharsPerToken, JsonSerializerOptions jsonOptions)
    {
        var maxTokens = Math.Max(128, maxInputContextTokens);
        var budgetItems = 100;
        var text = JsonSerializer.Serialize(buildContext(budgetItems), jsonOptions);
        while (budgetItems > 1 && EstimateTokens(text, approxCharsPerToken) > maxTokens)
        {
            budgetItems = Math.Max(1, budgetItems / 2);
            text = JsonSerializer.Serialize(buildContext(budgetItems), jsonOptions);
        }

        return text;
    }
}
