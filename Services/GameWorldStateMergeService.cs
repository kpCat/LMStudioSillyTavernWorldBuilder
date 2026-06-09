using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal static class GameWorldStateMergeService
{
    public static void MergeInto(GameWorldStateDefinition target, GameWorldStateDefinition source)
    {
        if (GameDraftService.HasWorldStateData(source))
        {
            target.Enabled = target.Enabled || source.Enabled || HasWorldStateDataBeyondEnabled(source);
        }

        if (!string.IsNullOrWhiteSpace(source.GenreProfile)
            && !string.Equals(source.GenreProfile, "generic", StringComparison.OrdinalIgnoreCase))
        {
            target.GenreProfile = source.GenreProfile;
        }

        AppendNotes(target, source.Notes);
        MergeTime(target.Time, source.Time);
        UpsertMany(target.Aspects, source.Aspects, x => x.Id, MergeAspect);
        UpsertMany(target.AmbientEvents, source.AmbientEvents, x => x.Id, (_, incoming) => incoming);
        UpsertMany(target.Rules, source.Rules, x => x.Id, (_, incoming) => incoming);
    }

    private static bool HasWorldStateDataBeyondEnabled(GameWorldStateDefinition source)
    {
        return !string.Equals(source.GenreProfile, "generic", StringComparison.OrdinalIgnoreCase)
            || source.Time.Enabled
            || source.Time.Segments.Count > 0
            || source.Aspects.Count > 0
            || source.AmbientEvents.Count > 0
            || source.Rules.Count > 0
            || !string.IsNullOrWhiteSpace(source.Notes);
    }

    private static void MergeTime(GameTimeSystemDefinition target, GameTimeSystemDefinition source)
    {
        var hasTimeData = source.TimeEnabledOrHasSegments();
        if (!hasTimeData)
        {
            return;
        }

        if (source.Enabled)
        {
            target.Enabled = true;
        }
        if (source.StartDayNumber > 0 && source.StartDayNumber != 1)
        {
            target.StartDayNumber = source.StartDayNumber;
        }
        if (!string.IsNullOrWhiteSpace(source.DayLabel))
        {
            target.DayLabel = source.DayLabel;
        }
        if (!string.IsNullOrWhiteSpace(source.SegmentLabel))
        {
            target.SegmentLabel = source.SegmentLabel;
        }
        if (!string.IsNullOrWhiteSpace(source.StartSegmentId))
        {
            target.StartSegmentId = source.StartSegmentId;
        }
        if (source.AdvanceSegmentsOnEndTurn != 0 || source.Enabled && source.Segments.Count > 0)
        {
            target.AdvanceSegmentsOnEndTurn = source.AdvanceSegmentsOnEndTurn;
        }
        if (source.AdvanceSegmentsOnTravel != 0 || source.Enabled && source.Segments.Count > 0)
        {
            target.AdvanceSegmentsOnTravel = source.AdvanceSegmentsOnTravel;
        }
        if (source.AdvanceSegmentsOnAction != 0 || source.Enabled && source.Segments.Count > 0)
        {
            target.AdvanceSegmentsOnAction = source.AdvanceSegmentsOnAction;
        }

        UpsertMany(target.Segments, source.Segments, x => x.Id, MergeTimeSegment);
    }

    private static bool TimeEnabledOrHasSegments(this GameTimeSystemDefinition source)
    {
        return source.Enabled
            || source.Segments.Count > 0
            || !string.IsNullOrWhiteSpace(source.StartSegmentId);
    }

    private static GameTimeSegmentDefinition MergeTimeSegment(GameTimeSegmentDefinition target, GameTimeSegmentDefinition source)
    {
        if (!string.IsNullOrWhiteSpace(source.Name)) target.Name = source.Name;
        if (!string.IsNullOrWhiteSpace(source.Description)) target.Description = source.Description;
        if (!string.IsNullOrWhiteSpace(source.NextSegmentId)) target.NextSegmentId = source.NextSegmentId;
        if (source.Order != 0) target.Order = source.Order;
        MergeTags(target.Tags, source.Tags);
        if (source.Modifiers.Count > 0) target.Modifiers = source.Modifiers;
        if (source.OnEnterEffects.Count > 0) target.OnEnterEffects = source.OnEnterEffects;
        return target;
    }

    private static GameWorldAspectDefinition MergeAspect(GameWorldAspectDefinition target, GameWorldAspectDefinition source)
    {
        if (!string.IsNullOrWhiteSpace(source.Name)) target.Name = source.Name;
        if (!string.IsNullOrWhiteSpace(source.Kind)) target.Kind = source.Kind;
        if (!string.IsNullOrWhiteSpace(source.Description)) target.Description = source.Description;
        if (!string.IsNullOrWhiteSpace(source.DefaultStateId)) target.DefaultStateId = source.DefaultStateId;
        MergeTags(target.Tags, source.Tags);
        UpsertMany(target.States, source.States, x => x.Id, MergeAspectState);
        return target;
    }

    private static GameWorldAspectStateDefinition MergeAspectState(GameWorldAspectStateDefinition target, GameWorldAspectStateDefinition source)
    {
        if (!string.IsNullOrWhiteSpace(source.Name)) target.Name = source.Name;
        if (!string.IsNullOrWhiteSpace(source.Kind)) target.Kind = source.Kind;
        if (!string.IsNullOrWhiteSpace(source.Description)) target.Description = source.Description;
        MergeTags(target.Tags, source.Tags);
        if (source.Modifiers.Count > 0) target.Modifiers = source.Modifiers;
        if (source.OnEnterEffects.Count > 0) target.OnEnterEffects = source.OnEnterEffects;
        return target;
    }

    private static void AppendNotes(GameWorldStateDefinition target, string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return;
        }

        var incoming = notes.Trim();
        if (string.IsNullOrWhiteSpace(target.Notes))
        {
            target.Notes = incoming;
            return;
        }
        if (!target.Notes.Contains(incoming, StringComparison.OrdinalIgnoreCase))
        {
            target.Notes = target.Notes.TrimEnd() + Environment.NewLine + Environment.NewLine + incoming;
        }
    }

    private static void MergeTags(List<string> target, IEnumerable<string> source)
    {
        foreach (var tag in source.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (!target.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                target.Add(tag);
            }
        }
    }

    private static void UpsertMany<T>(List<T> target, IEnumerable<T> source, Func<T, string> getId, Func<T, T, T> merge)
    {
        foreach (var item in source)
        {
            var id = getId(item);
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var index = target.FindIndex(x => string.Equals(getId(x), id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                target[index] = merge(target[index], item);
            }
            else
            {
                target.Add(item);
            }
        }
    }
}
