using System.Text.Json.Serialization;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed record GenerationSettings(double Temperature, double TopP, double MinP, int TopK, double RepeatPenalty, double PresencePenalty, int MaxTokens);

internal sealed record PromptPreset(string Name, string SystemPrompt, GenerationSettings Settings);

internal sealed class ApiMessage
{
    public ApiMessage()
    {
    }

    public ApiMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ApiMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public double TopP { get; set; }

    [JsonPropertyName("min_p")]
    public double MinP { get; set; }

    [JsonPropertyName("top_k")]
    public int TopK { get; set; }

    [JsonPropertyName("repeat_penalty")]
    public double RepeatPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    public double PresencePenalty { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

internal sealed class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<Choice>? Choices { get; set; }
}

internal sealed class Choice
{
    [JsonPropertyName("message")]
    public ResponseMessage? Message { get; set; }
}

internal sealed class ResponseMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
