using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameDesignConversationService
{
    private readonly GameDesignInterviewService _designInterviewService = new();
    private readonly GameDesignKnowledgeBaseService _knowledgeBaseService = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public string BuildConversationUserPrompt(GameProjectData project, string userMessage, string? focusTopic = null)
    {
        var query = new GameDesignKnowledgeQuery
        {
            IncludeStatuses =
            {
                GameDesignKnowledgeEntryStatus.Accepted,
                GameDesignKnowledgeEntryStatus.Proposed,
                GameDesignKnowledgeEntryStatus.NeedsClarification
            }
        };

        if (!string.IsNullOrWhiteSpace(focusTopic))
        {
            query.Topic = focusTopic.Trim();
            query.Tags.Add(focusTopic.Trim());
            query.AffectsSystems.Add(focusTopic.Trim());
        }

        var model = new
        {
            Instruction = "Ответь пользователю по-русски и извлеки только краткие записи дизайн-памяти. Не возвращай ничего кроме JSON.",
            CurrentUserMessage = Preview(userMessage, 2500),
            FocusTopic = Preview(focusTopic ?? string.Empty, 120),
            Project = new
            {
                project.Meta.Title,
                project.Meta.Genre,
                project.Meta.Tone,
                Description = Preview(project.Meta.Description, 500),
                Brief = Preview(project.Brief.Text, 700),
                Concept = Preview(project.Concept.Text, 700),
                Mvp = Preview(project.MvpPlan.Text, 700),
                DesignSummary = Preview(_designInterviewService.BuildDesignSummary(project), 1500),
                CreationPlanSummary = BuildCreationPlanSummary(project.CreationPlan),
                DesignKnowledgeSummary = _knowledgeBaseService.BuildCompactSummary(project.DesignKnowledgeBase, query, 1200),
                RecentConversation = project.DesignConversationHistory.Turns
                    .OrderByDescending(x => x.TimestampUtc)
                    .Take(6)
                    .OrderBy(x => x.TimestampUtc)
                    .Select(x => new
                    {
                        x.FocusTopic,
                        UserMessage = Preview(x.UserMessage, 500),
                        AssistantReply = Preview(x.AssistantReply, 500),
                        x.ExtractedKnowledgeEntryIds
                    })
            }
        };

        return JsonSerializer.Serialize(model, _jsonOptions);
    }

    public GameDesignConversationResult ParseResult(string rawText)
    {
        var result = new GameDesignConversationResult();
        try
        {
            var json = ExtractJson(rawText);
            var parsed = JsonSerializer.Deserialize<GameDesignConversationResult>(json, _jsonOptions);
            if (parsed == null)
            {
                result.Errors.Add("LLM вернула пустой JSON.");
                return result;
            }

            Normalize(parsed);
            Validate(parsed);
            return parsed;
        }
        catch (Exception ex)
        {
            result.Errors.Add("Не удалось разобрать JSON дизайн-диалога: " + ex.Message);
            return result;
        }
    }

    public IReadOnlyList<string> ApplyResult(GameProjectData project, GameDesignConversationResult result, string userMessage, string? focusTopic = null)
    {
        if (!result.IsSuccess)
        {
            return Array.Empty<string>();
        }

        var ids = new List<string>();
        foreach (var memoryEntry in result.MemoryEntries)
        {
            var entry = ToKnowledgeEntry(memoryEntry);
            _knowledgeBaseService.AddOrUpdateEntry(project.DesignKnowledgeBase, entry);
            ids.Add(entry.Id);
        }

        project.DesignConversationHistory.Turns.Add(new GameDesignConversationTurn
        {
            Id = NewTurnId(),
            UserMessage = userMessage.Trim(),
            AssistantReply = result.AssistantReply,
            ExtractedKnowledgeEntryIds = ids,
            FollowUpQuestions = result.FollowUpQuestions,
            Warnings = result.Warnings,
            TimestampUtc = DateTime.UtcNow,
            FocusTopic = focusTopic?.Trim() ?? string.Empty,
            Category = result.MemoryEntries.FirstOrDefault()?.Category ?? string.Empty
        });
        project.DesignConversationHistory.UpdatedUtc = DateTime.UtcNow;
        return ids;
    }

    public string FormatRussianReport(GameDesignConversationResult result, IReadOnlyList<string>? savedEntryIds = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== Дизайн-диалог v1 ===");

        if (!result.IsSuccess)
        {
            builder.AppendLine("Ошибка: память проекта не изменена.");
            foreach (var error in result.Errors)
            {
                builder.AppendLine("- " + error);
            }

            return builder.ToString().Trim();
        }

        builder.AppendLine("Ответ:");
        builder.AppendLine(string.IsNullOrWhiteSpace(result.AssistantReply) ? "(пусто)" : result.AssistantReply);
        builder.AppendLine();
        builder.AppendLine("Сохранённые записи памяти:");
        var ids = savedEntryIds ?? Array.Empty<string>();
        if (result.MemoryEntries.Count == 0)
        {
            builder.AppendLine("- нет");
        }
        else
        {
            for (var i = 0; i < result.MemoryEntries.Count; i++)
            {
                var entry = result.MemoryEntries[i];
                var idText = i < ids.Count ? ids[i] : "(новая запись)";
                builder.AppendLine($"- {idText}: [{entry.Status}/{entry.Importance}/{entry.Source}] {entry.Category}/{entry.Topic}: {entry.Summary}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Уточняющие вопросы:");
        if (result.FollowUpQuestions.Count == 0)
        {
            builder.AppendLine("- нет");
        }
        else
        {
            foreach (var question in result.FollowUpQuestions)
            {
                builder.AppendLine($"- [{question.Priority}] {question.Topic}: {question.Question}");
                if (question.SuggestedOptions.Count > 0)
                {
                    builder.AppendLine("  Варианты: " + string.Join("; ", question.SuggestedOptions));
                }
            }
        }

        if (result.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Предупреждения:");
            foreach (var warning in result.Warnings)
            {
                builder.AppendLine("- " + warning);
            }
        }

        return builder.ToString().Trim();
    }

    private static GameDesignKnowledgeEntry ToKnowledgeEntry(GameDesignConversationMemoryEntry entry)
    {
        var status = entry.Status switch
        {
            GameDesignConversationMemoryStatus.Accepted => GameDesignKnowledgeEntryStatus.Accepted,
            GameDesignConversationMemoryStatus.Rejected => GameDesignKnowledgeEntryStatus.Rejected,
            GameDesignConversationMemoryStatus.NeedsClarification => GameDesignKnowledgeEntryStatus.NeedsClarification,
            _ => GameDesignKnowledgeEntryStatus.Proposed
        };
        var kind = entry.Status switch
        {
            GameDesignConversationMemoryStatus.Accepted => GameDesignKnowledgeEntryKind.Decision,
            GameDesignConversationMemoryStatus.Rejected => GameDesignKnowledgeEntryKind.Rejection,
            GameDesignConversationMemoryStatus.Assumption => GameDesignKnowledgeEntryKind.Assumption,
            GameDesignConversationMemoryStatus.NeedsClarification => GameDesignKnowledgeEntryKind.Question,
            _ => GameDesignKnowledgeEntryKind.Note
        };

        return new GameDesignKnowledgeEntry
        {
            Id = NewKnowledgeId(entry),
            Category = entry.Category,
            Subcategory = entry.Subcategory,
            Topic = entry.Topic,
            Summary = entry.Summary,
            Details = string.Empty,
            Kind = kind,
            Status = status,
            Importance = entry.Importance,
            Source = entry.Source,
            Tags = entry.Tags.ToList(),
            RelatedEntityIds = entry.RelatedEntityIds.ToList(),
            AffectsSystems = entry.AffectsSystems.ToList()
        };
    }

    private static void Normalize(GameDesignConversationResult result)
    {
        result.AssistantReply = result.AssistantReply.Trim();
        result.Warnings = CleanList(result.Warnings);
        result.Errors = CleanList(result.Errors);
        result.MemoryEntries = result.MemoryEntries
            .Where(x => !string.IsNullOrWhiteSpace(x.Summary))
            .Select(NormalizeMemoryEntry)
            .ToList();
        result.FollowUpQuestions = result.FollowUpQuestions
            .Where(x => !string.IsNullOrWhiteSpace(x.Question))
            .Select(NormalizeQuestion)
            .ToList();
    }

    private static GameDesignConversationMemoryEntry NormalizeMemoryEntry(GameDesignConversationMemoryEntry entry)
    {
        entry.Category = DefaultIfEmpty(entry.Category, "design");
        entry.Subcategory = entry.Subcategory.Trim();
        entry.Topic = DefaultIfEmpty(entry.Topic, "general");
        entry.Summary = entry.Summary.Trim();
        entry.Source = NormalizeSource(entry.Source);
        entry.Tags = CleanList(entry.Tags);
        entry.RelatedEntityIds = CleanList(entry.RelatedEntityIds);
        entry.AffectsSystems = CleanList(entry.AffectsSystems);
        return entry;
    }

    private static GameDesignConversationQuestion NormalizeQuestion(GameDesignConversationQuestion question)
    {
        question.Id = string.IsNullOrWhiteSpace(question.Id) ? NewQuestionId(question.Topic) : question.Id.Trim();
        question.Topic = DefaultIfEmpty(question.Topic, "general");
        question.Question = question.Question.Trim();
        question.SuggestedOptions = CleanList(question.SuggestedOptions);
        return question;
    }

    private static void Validate(GameDesignConversationResult result)
    {
        if (string.IsNullOrWhiteSpace(result.AssistantReply))
        {
            result.Errors.Add("В JSON отсутствует assistantReply.");
        }
    }

    private static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = trimmed.IndexOf('\n');
            if (firstLineEnd >= 0)
            {
                trimmed = trimmed[(firstLineEnd + 1)..];
            }

            var fenceIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceIndex >= 0)
            {
                trimmed = trimmed[..fenceIndex];
            }
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return trimmed[start..(end + 1)];
        }

        return trimmed;
    }

    private static string BuildCreationPlanSummary(GameCreationPlan plan)
    {
        var stages = plan.Steps
            .OrderBy(x => x.Priority)
            .Take(12)
            .Select(x => new { x.Id, x.Title, x.Stage, x.Priority, x.IsRequired, x.TargetSystems });
        return JsonSerializer.Serialize(new { plan.Summary, Stages = stages }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static string Preview(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "...";
    }

    private static List<string> CleanList(IEnumerable<string> values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string DefaultIfEmpty(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeSource(string source)
    {
        var value = source.Trim().ToLowerInvariant();
        return value is "user" or "assistant" or "inferred" ? value : "inferred";
    }

    private static string NewTurnId()
    {
        return $"turn_{Guid.NewGuid():N}"[..18];
    }

    private static string NewQuestionId(string topic)
    {
        var safe = GameProjectManifestService.SafeId(topic, "q");
        return safe.Length > 24 ? safe[..24] : safe;
    }

    private static string NewKnowledgeId(GameDesignConversationMemoryEntry entry)
    {
        var basis = string.Join("_", new[] { entry.Category, entry.Topic }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var safe = GameProjectManifestService.SafeId(basis, "knowledge");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var prefix = safe.Length > 32 ? safe[..32].Trim('_') : safe;
        return $"{prefix}_{suffix}";
    }
}
