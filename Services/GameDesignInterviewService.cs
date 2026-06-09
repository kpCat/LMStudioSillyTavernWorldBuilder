using System.Text;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameDesignInterviewService
{
    private static readonly HashSet<string> QuickPrototypeCriticalSlots = new(StringComparer.OrdinalIgnoreCase)
    {
        "genre",
        "player_role",
        "main_goal",
        "core_loop",
        "combat_style",
        "randomness_level"
    };

    private readonly GameDesignSlotCatalog _catalog = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public void EnsureProfile(GameDesignProfile profile)
    {
        _catalog.EnsureDefaultSlots(profile);
        profile.UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyInitialIdea(GameDesignProfile profile, string initialIdea)
    {
        EnsureProfile(profile);
        profile.InitialIdea = initialIdea.Trim();
        profile.UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetCreationMode(GameDesignProfile profile, GameCreationMode mode)
    {
        EnsureProfile(profile);
        profile.CreationMode = mode;
        profile.UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetUserAnswer(GameDesignProfile profile, string slotId, string value)
    {
        var slot = GetKnownSlot(profile, slotId);
        slot.Value = value.Trim();
        slot.Source = string.IsNullOrWhiteSpace(slot.Value) ? GameDesignSlotValueSource.Empty : GameDesignSlotValueSource.User;
        slot.Confidence = string.IsNullOrWhiteSpace(slot.Value) ? 0 : 1;
        slot.Notes = string.Empty;
        slot.UpdatedAtUtc = DateTime.UtcNow;
        profile.UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkSlotAsLlmAssumption(GameDesignProfile profile, string slotId, string value, double confidence = 0.5, string notes = "")
    {
        var slot = GetKnownSlot(profile, slotId);
        if (slot.Source == GameDesignSlotValueSource.User)
        {
            return;
        }

        slot.Value = value.Trim();
        slot.Source = string.IsNullOrWhiteSpace(slot.Value) ? GameDesignSlotValueSource.Empty : GameDesignSlotValueSource.LlmAssumption;
        slot.Confidence = Clamp01(confidence);
        slot.Notes = notes.Trim();
        slot.UpdatedAtUtc = DateTime.UtcNow;
        profile.UpdatedAtUtc = DateTime.UtcNow;
    }

    public IReadOnlyList<GameDesignSlot> GetMissingSlots(GameDesignProfile profile)
    {
        EnsureProfile(profile);
        return profile.CreationMode switch
        {
            GameCreationMode.Manual => Missing(profile).Where(x => x.IsRequired || x.Priority <= 70).ToList(),
            GameCreationMode.Collaborative => Missing(profile).Where(x => x.IsRequired).ToList(),
            GameCreationMode.AutopilotWithReview => Missing(profile).Where(x => !x.CanBeAssumedByLlm || x.Source == GameDesignSlotValueSource.Empty && x.IsRequired && !x.CanBeAssumedByLlm).ToList(),
            GameCreationMode.QuickPrototype => Missing(profile).Where(x => QuickPrototypeCriticalSlots.Contains(x.Id)).ToList(),
            _ => Missing(profile).Where(x => x.IsRequired).ToList()
        };
    }

    public IReadOnlyList<GameDesignQuestion> GetQuestions(GameDesignProfile profile)
    {
        return GetMissingSlots(profile)
            .Select(x => new GameDesignQuestion
            {
                SlotId = x.Id,
                Question = $"Уточните: {x.Title.ToLowerInvariant()}?",
                HelpText = x.Description,
                SuggestedOptions = x.SuggestedOptions.ToList(),
                Priority = x.Priority
            })
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.SlotId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string BuildDesignSummary(GameProjectData project)
    {
        EnsureProfile(project.DesignProfile);
        var profile = project.DesignProfile;
        var builder = new StringBuilder();
        builder.AppendLine("Дизайн-досье игры:");
        builder.AppendLine("Исходная идея: " + (string.IsNullOrWhiteSpace(profile.InitialIdea) ? "не задана" : profile.InitialIdea));
        builder.AppendLine("Режим создания: " + profile.CreationMode);

        var filled = profile.Slots.Where(x => !string.IsNullOrWhiteSpace(x.Value)).OrderBy(x => x.Priority).ToList();
        builder.AppendLine("Заполненные слоты:");
        if (filled.Count == 0)
        {
            builder.AppendLine("- нет");
        }
        else
        {
            foreach (var slot in filled)
            {
                builder.AppendLine($"- {slot.Id}: {slot.Value} (source={slot.Source}, confidence={slot.Confidence:0.##})");
            }
        }

        var unresolved = profile.Slots.Where(IsMissing).Where(x => x.IsRequired).OrderBy(x => x.Priority).ToList();
        builder.AppendLine("Нерешённые обязательные слоты:");
        builder.AppendLine(unresolved.Count == 0 ? "- нет" : string.Join(Environment.NewLine, unresolved.Select(x => "- " + x.Id + ": " + x.Title)));

        if (project.CreationPlan.Steps.Count > 0)
        {
            builder.AppendLine("План создания:");
            foreach (var step in project.CreationPlan.Steps.OrderBy(x => x.Priority).Take(20))
            {
                builder.AppendLine($"- {step.Id}: {step.Title}");
            }
        }

        return builder.ToString().Trim();
    }

    public string BuildLlmAssumptionPrompt(GameProjectData project)
    {
        EnsureProfile(project.DesignProfile);
        var slots = GetAssumableMissingSlots(project.DesignProfile)
            .Select(x => new
            {
                slotId = x.Id,
                title = x.Title,
                description = x.Description,
                suggestedOptions = x.SuggestedOptions,
                affectsSystems = x.AffectsSystems,
                required = x.IsRequired,
                priority = x.Priority
            })
            .ToList();

        return JsonSerializer.Serialize(new
        {
            instruction = "Заполни только перечисленные missingSlots. Не перезаписывай пользовательские решения. Верни только JSON формата { \"assumptions\": [ { \"slotId\": \"genre\", \"value\": \"...\", \"confidence\": 0.82, \"notes\": \"...\" } ] }.",
            initialIdea = project.DesignProfile.InitialIdea,
            creationMode = project.DesignProfile.CreationMode.ToString(),
            existingDesignSummary = BuildDesignSummary(project),
            missingSlots = slots
        }, _jsonOptions);
    }

    public int ApplyLlmAssumptionsFromJson(GameDesignProfile profile, string rawJson)
    {
        EnsureProfile(profile);
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            throw new InvalidOperationException("LM assumption JSON is empty.");
        }

        var json = ExtractJson(rawJson);
        LlmAssumptionResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<LlmAssumptionResponse>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("LM assumption JSON is invalid: " + ex.Message, ex);
        }

        if (response?.Assumptions == null)
        {
            throw new InvalidOperationException("LM assumption JSON does not contain assumptions array.");
        }

        var slots = profile.Slots.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var applied = 0;
        foreach (var assumption in response.Assumptions)
        {
            if (string.IsNullOrWhiteSpace(assumption.SlotId) || string.IsNullOrWhiteSpace(assumption.Value))
            {
                continue;
            }

            if (!slots.TryGetValue(assumption.SlotId, out var slot) || slot.Source == GameDesignSlotValueSource.User || !slot.CanBeAssumedByLlm)
            {
                continue;
            }

            slot.Value = assumption.Value.Trim();
            slot.Source = GameDesignSlotValueSource.LlmAssumption;
            slot.Confidence = Clamp01(assumption.Confidence);
            slot.Notes = assumption.Notes.Trim();
            slot.UpdatedAtUtc = DateTime.UtcNow;
            applied++;
        }

        if (applied > 0)
        {
            profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        return applied;
    }

    private IReadOnlyList<GameDesignSlot> GetAssumableMissingSlots(GameDesignProfile profile)
    {
        var missing = Missing(profile).Where(x => x.CanBeAssumedByLlm);
        if (profile.CreationMode == GameCreationMode.Manual)
        {
            missing = missing.Where(x => !x.IsRequired);
        }

        return missing.ToList();
    }

    private List<GameDesignSlot> Missing(GameDesignProfile profile)
    {
        return profile.Slots
            .Where(IsMissing)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private GameDesignSlot GetKnownSlot(GameDesignProfile profile, string slotId)
    {
        EnsureProfile(profile);
        return profile.Slots.FirstOrDefault(x => string.Equals(x.Id, slotId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Unknown game design slot: " + slotId);
    }

    private static bool IsMissing(GameDesignSlot slot)
    {
        return string.IsNullOrWhiteSpace(slot.Value) || slot.Source == GameDesignSlotValueSource.Empty;
    }

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value)) return 0;
        return Math.Min(1, Math.Max(0, value));
    }

    private static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        var firstObject = trimmed.IndexOf('{');
        if (firstObject < 0)
        {
            return trimmed;
        }

        var endObject = trimmed.LastIndexOf('}');
        return endObject > firstObject ? trimmed[firstObject..(endObject + 1)] : trimmed;
    }

    private sealed class LlmAssumptionResponse
    {
        public List<LlmAssumptionItem> Assumptions { get; set; } = new();
    }

    private sealed class LlmAssumptionItem
    {
        public string SlotId { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
