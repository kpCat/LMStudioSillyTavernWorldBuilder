using System.Text;
using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameDesignKnowledgeBaseService
{
    private static readonly GameDesignKnowledgeEntryStatus[] DefaultStatuses =
    {
        GameDesignKnowledgeEntryStatus.Accepted,
        GameDesignKnowledgeEntryStatus.Proposed,
        GameDesignKnowledgeEntryStatus.NeedsClarification
    };

    public void AddOrUpdateEntry(GameDesignKnowledgeBase knowledgeBase, GameDesignKnowledgeEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
        {
            entry.Id = NewId();
        }

        var now = DateTime.UtcNow;
        if (entry.CreatedUtc == default)
        {
            entry.CreatedUtc = now;
        }

        entry.UpdatedUtc = now;
        Normalize(entry);

        var index = knowledgeBase.Entries.FindIndex(x => string.Equals(x.Id, entry.Id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            entry.CreatedUtc = knowledgeBase.Entries[index].CreatedUtc == default ? entry.CreatedUtc : knowledgeBase.Entries[index].CreatedUtc;
            knowledgeBase.Entries[index] = entry;
        }
        else
        {
            knowledgeBase.Entries.Add(entry);
        }

        knowledgeBase.UpdatedUtc = now;
    }

    public GameDesignKnowledgeEntry AddAcceptedDecision(GameDesignKnowledgeBase knowledgeBase, string category, string topic, string summary, string details = "", GameDesignKnowledgeImportance importance = GameDesignKnowledgeImportance.High, string source = "user")
    {
        return AddEntry(knowledgeBase, GameDesignKnowledgeEntryKind.Decision, GameDesignKnowledgeEntryStatus.Accepted, category, topic, summary, details, importance, source);
    }

    public GameDesignKnowledgeEntry AddConstraint(GameDesignKnowledgeBase knowledgeBase, string category, string topic, string summary, string details = "", GameDesignKnowledgeImportance importance = GameDesignKnowledgeImportance.High, string source = "user")
    {
        return AddEntry(knowledgeBase, GameDesignKnowledgeEntryKind.Constraint, GameDesignKnowledgeEntryStatus.Accepted, category, topic, summary, details, importance, source);
    }

    public GameDesignKnowledgeEntry AddPreference(GameDesignKnowledgeBase knowledgeBase, string category, string topic, string summary, string details = "", GameDesignKnowledgeImportance importance = GameDesignKnowledgeImportance.Normal, string source = "user")
    {
        return AddEntry(knowledgeBase, GameDesignKnowledgeEntryKind.Preference, GameDesignKnowledgeEntryStatus.Accepted, category, topic, summary, details, importance, source);
    }

    public GameDesignKnowledgeEntry AddRejection(GameDesignKnowledgeBase knowledgeBase, string category, string topic, string summary, string details = "", GameDesignKnowledgeImportance importance = GameDesignKnowledgeImportance.Normal, string source = "user")
    {
        return AddEntry(knowledgeBase, GameDesignKnowledgeEntryKind.Rejection, GameDesignKnowledgeEntryStatus.Rejected, category, topic, summary, details, importance, source);
    }

    public GameDesignKnowledgeEntry AddAssumption(GameDesignKnowledgeBase knowledgeBase, string category, string topic, string summary, string details = "", GameDesignKnowledgeImportance importance = GameDesignKnowledgeImportance.Normal, string source = "system")
    {
        return AddEntry(knowledgeBase, GameDesignKnowledgeEntryKind.Assumption, GameDesignKnowledgeEntryStatus.Proposed, category, topic, summary, details, importance, source);
    }

    public GameDesignKnowledgeEntry AddQuestion(GameDesignKnowledgeBase knowledgeBase, string category, string topic, string summary, string details = "", GameDesignKnowledgeImportance importance = GameDesignKnowledgeImportance.Normal, string source = "system")
    {
        return AddEntry(knowledgeBase, GameDesignKnowledgeEntryKind.Question, GameDesignKnowledgeEntryStatus.NeedsClarification, category, topic, summary, details, importance, source);
    }

    public GameDesignKnowledgeEntry AddAnswer(GameDesignKnowledgeBase knowledgeBase, string category, string topic, string summary, string details = "", GameDesignKnowledgeImportance importance = GameDesignKnowledgeImportance.Normal, string source = "user")
    {
        return AddEntry(knowledgeBase, GameDesignKnowledgeEntryKind.Answer, GameDesignKnowledgeEntryStatus.Accepted, category, topic, summary, details, importance, source);
    }

    public IReadOnlyList<GameDesignKnowledgeEntry> GetRelevantEntries(GameDesignKnowledgeBase knowledgeBase, GameDesignKnowledgeQuery query, int maxEntries)
    {
        IReadOnlyCollection<GameDesignKnowledgeEntryStatus> statuses = query.IncludeStatuses.Count > 0 ? query.IncludeStatuses : DefaultStatuses;
        var result = knowledgeBase.Entries
            .Where(x => statuses.Contains(x.Status))
            .Where(x => query.IncludeKinds.Count == 0 || query.IncludeKinds.Contains(x.Kind))
            .Select(x => new { Entry = x, Score = Score(x, query) })
            .Where(x => x.Score > 0 || IsEmpty(query))
            .OrderByDescending(x => ImportanceRank(x.Entry.Importance))
            .ThenByDescending(x => x.Entry.Status == GameDesignKnowledgeEntryStatus.Accepted)
            .ThenByDescending(x => x.Score)
            .ThenByDescending(x => x.Entry.UpdatedUtc)
            .ThenBy(x => x.Entry.Id, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, maxEntries))
            .Select(x => x.Entry)
            .ToList();

        return result;
    }

    public string BuildCompactSummary(GameDesignKnowledgeBase knowledgeBase, GameDesignKnowledgeQuery query, int maxCharacters)
    {
        if (maxCharacters <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var entry in GetRelevantEntries(knowledgeBase, query, 20))
        {
            var line = $"- {entry.Category}/{entry.Subcategory}/{entry.Topic}; status={entry.Status}; kind={entry.Kind}; {entry.Summary}".Replace("//", "/");
            if (builder.Length + line.Length + Environment.NewLine.Length > maxCharacters)
            {
                break;
            }

            builder.AppendLine(line);
        }

        return builder.ToString().Trim();
    }

    public string FormatRussianReport(GameDesignKnowledgeBase knowledgeBase)
    {
        var builder = new StringBuilder();
        builder.AppendLine("База знаний дизайна:");
        builder.AppendLine("Записей: " + knowledgeBase.Entries.Count);

        foreach (var entry in GetRelevantEntries(knowledgeBase, new GameDesignKnowledgeQuery(), 50))
        {
            builder.AppendLine($"- [{entry.Status}/{entry.Kind}/{entry.Importance}] {entry.Category}/{entry.Topic}: {entry.Summary}");
        }

        return builder.ToString().Trim();
    }

    private GameDesignKnowledgeEntry AddEntry(
        GameDesignKnowledgeBase knowledgeBase,
        GameDesignKnowledgeEntryKind kind,
        GameDesignKnowledgeEntryStatus status,
        string category,
        string topic,
        string summary,
        string details,
        GameDesignKnowledgeImportance importance,
        string source)
    {
        var entry = new GameDesignKnowledgeEntry
        {
            Id = NewId(),
            Kind = kind,
            Status = status,
            Category = category,
            Topic = topic,
            Summary = summary,
            Details = details,
            Importance = importance,
            Source = source
        };
        AddOrUpdateEntry(knowledgeBase, entry);
        return entry;
    }

    private static int Score(GameDesignKnowledgeEntry entry, GameDesignKnowledgeQuery query)
    {
        var score = 0;
        if (MatchesText(query.Category, entry.Category)) score += 8;
        if (MatchesText(query.Subcategory, entry.Subcategory)) score += 6;
        if (MatchesText(query.Topic, entry.Topic)) score += 5;
        score += CountMatches(query.Tags, entry.Tags) * 4;
        score += CountMatches(query.RelatedEntityIds, entry.RelatedEntityIds) * 4;
        score += CountMatches(query.AffectsSystems, entry.AffectsSystems) * 4;
        return score;
    }

    private static bool IsEmpty(GameDesignKnowledgeQuery query)
    {
        return string.IsNullOrWhiteSpace(query.Category)
            && string.IsNullOrWhiteSpace(query.Subcategory)
            && string.IsNullOrWhiteSpace(query.Topic)
            && query.Tags.Count == 0
            && query.RelatedEntityIds.Count == 0
            && query.AffectsSystems.Count == 0;
    }

    private static bool MatchesText(string query, string value)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        return value.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static int CountMatches(IReadOnlyList<string> queryValues, IReadOnlyList<string> entryValues)
    {
        return queryValues.Count(query => entryValues.Any(value => string.Equals(value, query, StringComparison.OrdinalIgnoreCase)));
    }

    private static int ImportanceRank(GameDesignKnowledgeImportance importance)
    {
        return importance switch
        {
            GameDesignKnowledgeImportance.Critical => 4,
            GameDesignKnowledgeImportance.High => 3,
            GameDesignKnowledgeImportance.Normal => 2,
            GameDesignKnowledgeImportance.Low => 1,
            _ => 0
        };
    }

    private static void Normalize(GameDesignKnowledgeEntry entry)
    {
        entry.Id = entry.Id.Trim();
        entry.Category = entry.Category.Trim();
        entry.Subcategory = entry.Subcategory.Trim();
        entry.Topic = entry.Topic.Trim();
        entry.Summary = entry.Summary.Trim();
        entry.Details = entry.Details.Trim();
        entry.Source = entry.Source.Trim();
        entry.Tags = CleanList(entry.Tags);
        entry.RelatedEntityIds = CleanList(entry.RelatedEntityIds);
        entry.AffectsSystems = CleanList(entry.AffectsSystems);
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

    private static string NewId()
    {
        return $"knowledge_{Guid.NewGuid():N}"[..23];
    }
}
