using System.Text;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameRandomDirectorService
{
    private static readonly string[] RuntimeTriggers = { "turnEnd", "travel", "action" };
    private readonly GameDesignInterviewService _designInterviewService = new();
    private readonly JsonSerializerOptions _jsonOptions = GenerationJsonOptions.PromptJson;

    public GameRandomDirectorReport BuildReport(GameProjectData project)
    {
        _designInterviewService.EnsureProfile(project.DesignProfile);

        var report = new GameRandomDirectorReport();
        var randomnessLevel = GetDesignSlot(project, "randomness_level");
        var highOrMediumRandomness = IsMediumOrHigh(randomnessLevel);
        var locationIds = project.Locations
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var timeSegmentIds = project.WorldState.Time.Segments
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        AddGlobalCoverage(report, project);
        AddTriggerCoverage(report, project);
        AddTimeSegmentCoverage(report, project);
        AddLocationCoverage(report, project);
        AddLocationTagCoverage(report, project);

        if (highOrMediumRandomness && !project.WorldState.Enabled)
        {
            AddWarning(report, "world_state_disabled", "В design slot randomness_level задан средний/высокий рандом, но WorldState.Enabled=false.", "error");
        }

        if (highOrMediumRandomness && project.WorldState.AmbientEvents.Count == 0)
        {
            AddWarning(report, "no_ambient_events", "В design slot randomness_level задан средний/высокий рандом, но ambientEvents пусты.", "warning");
        }

        foreach (var trigger in GetRelevantTriggers(project))
        {
            var eventCount = project.WorldState.AmbientEvents.Count(x => TriggerEquals(x.Trigger, trigger));
            if (eventCount == 0)
            {
                AddWarning(report, "trigger_without_events", "Для runtime-trigger '" + trigger + "' нет фоновых событий.", "info", trigger);
            }
        }

        foreach (var location in project.Locations.Where(x => !string.IsNullOrWhiteSpace(x.Id)))
        {
            if (!HasEventForLocation(project, location))
            {
                AddWarning(report, "location_without_events", "Локация '" + Display(location.Name, location.Id) + "' не покрыта ambientEvents через LocationIds или LocationTags.", "warning", location.Id);
            }
        }

        foreach (var ambientEvent in project.WorldState.AmbientEvents)
        {
            ValidateAmbientEvent(report, ambientEvent, locationIds, timeSegmentIds);
        }

        if (highOrMediumRandomness && project.Locations.Count < 2)
        {
            AddWarning(report, "high_randomness_without_travel_variation", "Высокий/средний рандом задан, но в проекте меньше двух локаций: travel-вариативность почти отсутствует.", "warning");
        }

        if (highOrMediumRandomness && project.Locations.Count >= 2 && project.WorldState.AmbientEvents.Count < Math.Max(2, project.Locations.Count))
        {
            AddWarning(report, "too_few_events_for_locations", "Событий меньше, чем локаций: вариативность мира будет ощущаться бедно.", "warning");
        }

        AddRecommendations(report, project, highOrMediumRandomness);
        report.Summary = BuildSummary(project, report, randomnessLevel);
        return report;
    }

    public string BuildGenerationUserPrompt(GameProjectData project, GameRandomDirectorReport report, int requestedEventCount)
    {
        var requested = Math.Clamp(requestedEventCount, 1, 30);
        var model = new
        {
            Instruction = "Сгенерируй controlled-randomness partial GameProjectData JSON как draft. Не применяй изменения напрямую.",
            RequestedEventCount = requested,
            DesignSummary = _designInterviewService.BuildDesignSummary(project),
            RandomDirectorReport = BuildCompactReportModel(report, 20),
            Existing = new
            {
                project.Meta.Title,
                project.Meta.Genre,
                project.Meta.Tone,
                Locations = project.Locations.Select(x => new { x.Id, x.Name, x.Tags }),
                LocationConnections = project.LocationConnections.Select(x => new { x.Id, x.FromLocationId, x.ToLocationId, x.IsTwoWay }),
                TimeSegments = project.WorldState.Time.Segments.OrderBy(x => x.Order).Select(x => new { x.Id, x.Name, x.Order, x.Tags }),
                WorldAspects = project.WorldState.Aspects.Select(x => new { x.Id, x.Name, x.Kind, x.DefaultStateId, States = x.States.Select(s => new { s.Id, s.Name, s.Kind }) }),
                AmbientEventIds = project.WorldState.AmbientEvents.Select(x => x.Id),
                RuleIds = project.WorldState.Rules.Select(x => x.Id),
                VariableIds = project.Variables.Select(x => x.Id),
                StatIds = project.Stats.Select(x => x.Id),
                CurrencyIds = project.Currencies.Select(x => x.Id),
                ItemIds = project.Items.Select(x => x.Id)
            }
        };

        return JsonSerializer.Serialize(model, _jsonOptions);
    }

    public string BuildCompactRandomDirectorSummary(GameProjectData project, GameRandomDirectorReport report)
    {
        var model = new
        {
            report.Summary,
            Counts = new
            {
                Locations = project.Locations.Count,
                AmbientEvents = project.WorldState.AmbientEvents.Count,
                WorldRules = project.WorldState.Rules.Count,
                Warnings = report.Warnings.Count,
                Recommendations = report.Recommendations.Count
            },
            Warnings = report.Warnings.Take(10).Select(x => new { x.Code, x.Severity, x.Message, x.EntityIds }),
            Recommendations = report.Recommendations.OrderBy(x => x.Priority).Take(8).Select(x => new { x.Id, x.Title, x.TargetStage, x.Priority, x.TargetSystems }),
            Coverage = report.Coverage.Take(16).Select(x => new { x.ScopeType, x.ScopeId, x.EventCount, x.RuleCount, x.AverageWeight, EventIds = x.EventIds.Take(8) })
        };

        return JsonSerializer.Serialize(model, _jsonOptions);
    }

    public string FormatReportForUi(GameRandomDirectorReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== Random Director v1 ===");
        builder.AppendLine(report.Summary);
        builder.AppendLine();
        builder.AppendLine("Покрытие:");
        foreach (var item in report.Coverage.OrderBy(x => x.ScopeType).ThenBy(x => x.ScopeId).Take(60))
        {
            builder.AppendLine($"- {item.ScopeType}:{item.ScopeId} -> events={item.EventCount}, rules={item.RuleCount}, avgWeight={item.AverageWeight}, ids={string.Join(", ", item.EventIds.Take(8))}");
        }

        builder.AppendLine();
        builder.AppendLine("Предупреждения:");
        if (report.Warnings.Count == 0)
        {
            builder.AppendLine("- нет");
        }
        else
        {
            foreach (var warning in report.Warnings)
            {
                builder.AppendLine($"- [{warning.Severity}] {warning.Code}: {warning.Message}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Рекомендации:");
        if (report.Recommendations.Count == 0)
        {
            builder.AppendLine("- нет");
        }
        else
        {
            foreach (var recommendation in report.Recommendations.OrderBy(x => x.Priority))
            {
                builder.AppendLine($"- {recommendation.Priority:00} {recommendation.Title} ({recommendation.TargetStage})");
                builder.AppendLine("  " + recommendation.Description);
            }
        }

        return builder.ToString();
    }

    private static void AddGlobalCoverage(GameRandomDirectorReport report, GameProjectData project)
    {
        report.Coverage.Add(new GameRandomDirectorCoverageItem
        {
            ScopeType = "global",
            ScopeId = "worldState",
            EventCount = project.WorldState.AmbientEvents.Count,
            RuleCount = project.WorldState.Rules.Count,
            AverageWeight = AverageWeight(project.WorldState.AmbientEvents),
            EventIds = project.WorldState.AmbientEvents.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
        });
    }

    private static void AddTriggerCoverage(GameRandomDirectorReport report, GameProjectData project)
    {
        foreach (var trigger in RuntimeTriggers)
        {
            var events = project.WorldState.AmbientEvents.Where(x => TriggerEquals(x.Trigger, trigger)).ToList();
            report.Coverage.Add(new GameRandomDirectorCoverageItem
            {
                ScopeType = "trigger",
                ScopeId = trigger,
                EventCount = events.Count,
                RuleCount = project.WorldState.Rules.Count(x => TriggerEquals(x.Trigger, trigger)),
                AverageWeight = AverageWeight(events),
                EventIds = events.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            });
        }
    }

    private static void AddTimeSegmentCoverage(GameRandomDirectorReport report, GameProjectData project)
    {
        foreach (var segment in project.WorldState.Time.Segments.Where(x => !string.IsNullOrWhiteSpace(x.Id)))
        {
            var events = project.WorldState.AmbientEvents
                .Where(x => x.TimeSegmentIds.Count == 0 || x.TimeSegmentIds.Contains(segment.Id, StringComparer.OrdinalIgnoreCase))
                .ToList();
            report.Coverage.Add(new GameRandomDirectorCoverageItem
            {
                ScopeType = "timeSegment",
                ScopeId = segment.Id,
                EventCount = events.Count,
                RuleCount = 0,
                AverageWeight = AverageWeight(events),
                EventIds = events.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            });
        }
    }

    private static void AddLocationCoverage(GameRandomDirectorReport report, GameProjectData project)
    {
        foreach (var location in project.Locations.Where(x => !string.IsNullOrWhiteSpace(x.Id)))
        {
            var events = project.WorldState.AmbientEvents.Where(x => EventMatchesLocation(location, x)).ToList();
            report.Coverage.Add(new GameRandomDirectorCoverageItem
            {
                ScopeType = "location",
                ScopeId = location.Id,
                EventCount = events.Count,
                RuleCount = 0,
                AverageWeight = AverageWeight(events),
                EventIds = events.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            });
        }
    }

    private static void AddLocationTagCoverage(GameRandomDirectorReport report, GameProjectData project)
    {
        foreach (var tag in project.Locations.SelectMany(x => x.Tags).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var events = project.WorldState.AmbientEvents.Where(x => x.LocationTags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
            report.Coverage.Add(new GameRandomDirectorCoverageItem
            {
                ScopeType = "locationTag",
                ScopeId = tag,
                EventCount = events.Count,
                RuleCount = 0,
                AverageWeight = AverageWeight(events),
                EventIds = events.Select(x => x.Id).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            });
        }
    }

    private static void ValidateAmbientEvent(GameRandomDirectorReport report, GameAmbientEventDefinition ambientEvent, HashSet<string> locationIds, HashSet<string> timeSegmentIds)
    {
        var eventId = string.IsNullOrWhiteSpace(ambientEvent.Id) ? "(empty)" : ambientEvent.Id;
        if (string.IsNullOrWhiteSpace(ambientEvent.Id))
        {
            AddWarning(report, "ambient_event_missing_id", "AmbientEvent без Id не сможет стабильно мержиться и отслеживаться.", "error", eventId);
        }
        if (string.IsNullOrWhiteSpace(ambientEvent.Name))
        {
            AddWarning(report, "ambient_event_missing_name", "AmbientEvent '" + eventId + "' без Name сложнее инспектировать.", "warning", eventId);
        }
        if (string.IsNullOrWhiteSpace(ambientEvent.Text))
        {
            AddWarning(report, "ambient_event_missing_text", "AmbientEvent '" + eventId + "' без Text не даст игроку понятного сообщения.", "warning", eventId);
        }
        if (ambientEvent.Weight <= 0)
        {
            AddWarning(report, "ambient_event_invalid_weight", "AmbientEvent '" + eventId + "' имеет Weight <= 0 и не будет выпадать.", "error", eventId);
        }
        if (ambientEvent.ChancePercent is < 0 or > 100)
        {
            AddWarning(report, "ambient_event_invalid_chance", "AmbientEvent '" + eventId + "' имеет ChancePercent вне диапазона 0..100.", "error", eventId);
        }
        foreach (var locationId in ambientEvent.LocationIds.Where(x => !locationIds.Contains(x)))
        {
            AddWarning(report, "ambient_event_missing_location", "AmbientEvent '" + eventId + "' ссылается на отсутствующую локацию '" + locationId + "'.", "error", eventId, locationId);
        }
        foreach (var segmentId in ambientEvent.TimeSegmentIds.Where(x => !timeSegmentIds.Contains(x)))
        {
            AddWarning(report, "ambient_event_missing_time_segment", "AmbientEvent '" + eventId + "' ссылается на отсутствующий сегмент времени '" + segmentId + "'.", "error", eventId, segmentId);
        }
        if (ambientEvent.CooldownTurns <= 0 && ambientEvent.ChancePercent >= 70 && ambientEvent.Weight >= 5)
        {
            AddWarning(report, "ambient_event_aggressive_repeat", "AmbientEvent '" + eventId + "' имеет высокий шанс/вес без cooldownTurns и может повторяться слишком часто.", "warning", eventId);
        }
    }

    private static void AddRecommendations(GameRandomDirectorReport report, GameProjectData project, bool highOrMediumRandomness)
    {
        if (!highOrMediumRandomness)
        {
            return;
        }

        if (!project.WorldState.Enabled)
        {
            AddRecommendation(report, "enable_world_state", "Включить WorldState", "Для управляемого рандома нужен WorldState.Enabled=true и минимальная time/aspect основа.", "world-rules", 10, "world-state");
        }
        if (project.WorldState.AmbientEvents.Count == 0)
        {
            AddRecommendation(report, "seed_ambient_events", "Сгенерировать базовые ambient events", "Добавить небольшую пачку событий на turnEnd/travel/action, привязанную к текущим локациям и времени.", "ambient-events", 20, "ambient-events", "world-rules");
        }
        if (project.Locations.Count >= 2 && project.LocationConnections.Count == 0)
        {
            AddRecommendation(report, "add_travel_hooks", "Связать travel-вариативность", "Локаций несколько, но переходы не описаны: travel-события будут менее полезны без маршрутов.", "travel", 30, "locations", "travel");
        }
        if (project.WorldState.AmbientEvents.Count < Math.Max(2, project.Locations.Count))
        {
            AddRecommendation(report, "improve_variety", "Увеличить разнообразие", "Событий мало относительно локаций. Лучше добавить локальные и tag-based события с cooldownTurns.", "ambient-events", 40, "ambient-events");
        }
    }

    private static IEnumerable<string> GetRelevantTriggers(GameProjectData project)
    {
        yield return "turnEnd";
        if (project.Locations.Count > 1 || project.LocationConnections.Count > 0)
        {
            yield return "travel";
        }
        if (project.Actions.Count > 0 || project.Mechanics.EnableActionPanel)
        {
            yield return "action";
        }
    }

    private static object BuildCompactReportModel(GameRandomDirectorReport report, int limit)
    {
        return new
        {
            report.Summary,
            Warnings = report.Warnings.Take(limit).Select(x => new { x.Code, x.Severity, x.Message, x.EntityIds }),
            Recommendations = report.Recommendations.OrderBy(x => x.Priority).Take(limit).Select(x => new { x.Id, x.Title, x.Description, x.TargetStage, x.Priority, x.TargetSystems }),
            Coverage = report.Coverage.Take(limit).Select(x => new { x.ScopeType, x.ScopeId, x.EventCount, x.RuleCount, x.AverageWeight, x.EventIds })
        };
    }

    private static string BuildSummary(GameProjectData project, GameRandomDirectorReport report, string randomnessLevel)
    {
        var errors = report.Warnings.Count(x => string.Equals(x.Severity, "error", StringComparison.OrdinalIgnoreCase));
        var warnings = report.Warnings.Count(x => string.Equals(x.Severity, "warning", StringComparison.OrdinalIgnoreCase));
        return $"Уровень рандома: {(string.IsNullOrWhiteSpace(randomnessLevel) ? "не задан" : randomnessLevel)}. Локаций: {project.Locations.Count}, ambientEvents: {project.WorldState.AmbientEvents.Count}, worldRules: {project.WorldState.Rules.Count}. Проблемы: errors={errors}, warnings={warnings}.";
    }

    private static bool HasEventForLocation(GameProjectData project, GameLocation location)
    {
        return project.WorldState.AmbientEvents.Any(x => EventMatchesLocation(location, x));
    }

    private static bool EventMatchesLocation(GameLocation location, GameAmbientEventDefinition ambientEvent)
    {
        if (ambientEvent.LocationIds.Count == 0 && ambientEvent.LocationTags.Count == 0)
        {
            return true;
        }
        return ambientEvent.LocationIds.Contains(location.Id, StringComparer.OrdinalIgnoreCase)
            || location.Tags.Any(tag => ambientEvent.LocationTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }

    private static int AverageWeight(IReadOnlyCollection<GameAmbientEventDefinition> events)
    {
        return events.Count == 0 ? 0 : (int)Math.Round(events.Average(x => x.Weight), MidpointRounding.AwayFromZero);
    }

    private static string GetDesignSlot(GameProjectData project, string slotId)
    {
        return project.DesignProfile.Slots.FirstOrDefault(x => string.Equals(x.Id, slotId, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
    }

    private static bool IsMediumOrHigh(string value)
    {
        return ContainsAny(value, "medium", "high", "сред", "выс", "важн", "ключ");
    }

    private static bool TriggerEquals(string actual, string expected)
    {
        return string.Equals(NormalizeTrigger(actual), NormalizeTrigger(expected), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTrigger(string trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger))
        {
            return "turnEnd";
        }
        return trigger.Equals("actionEnd", StringComparison.OrdinalIgnoreCase) ? "action" : trigger;
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static string Display(string name, string id)
    {
        return string.IsNullOrWhiteSpace(name) ? id : name + " (" + id + ")";
    }

    private static void AddWarning(GameRandomDirectorReport report, string code, string message, string severity, params string[] entityIds)
    {
        report.Warnings.Add(new GameRandomDirectorWarning
        {
            Code = code,
            Message = message,
            Severity = severity,
            EntityIds = entityIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        });
    }

    private static void AddRecommendation(GameRandomDirectorReport report, string id, string title, string description, string targetStage, int priority, params string[] targetSystems)
    {
        report.Recommendations.Add(new GameRandomDirectorRecommendation
        {
            Id = id,
            Title = title,
            Description = description,
            TargetStage = targetStage,
            Priority = priority,
            TargetSystems = targetSystems.ToList()
        });
    }
}
