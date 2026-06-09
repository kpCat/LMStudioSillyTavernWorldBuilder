using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameRandomDirectorTests
{
    [Fact]
    public void BuildReport_WarnsWhenHighRandomnessButWorldStateDisabled()
    {
        var project = TestProjects.CreatePlayableProject();
        SetRandomness(project, "high");
        project.WorldState.Enabled = false;

        var report = new GameRandomDirectorService().BuildReport(project);

        Assert.Contains(report.Warnings, x => x.Code == "world_state_disabled" && x.Severity == "error");
    }

    [Fact]
    public void BuildReport_WarnsWhenHighRandomnessHasNoAmbientEvents()
    {
        var project = TestProjects.CreateWorldStateProject();
        SetRandomness(project, "high");
        project.WorldState.AmbientEvents.Clear();

        var report = new GameRandomDirectorService().BuildReport(project);

        Assert.Contains(report.Warnings, x => x.Code == "no_ambient_events");
    }

    [Fact]
    public void BuildReport_ReportsLocationCoverageUsingIdsAndTags()
    {
        var project = TestProjects.CreateWorldStateProject();
        project.Locations.Clear();
        project.Locations.Add(new GameLocation { Id = "market", Name = "Market", Tags = { "city" } });
        project.Locations.Add(new GameLocation { Id = "forest", Name = "Forest", Tags = { "wild" } });
        project.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
        {
            Id = "market_rumor",
            Name = "Rumor",
            Text = "Слух расходится по площади.",
            LocationIds = { "market" },
            Weight = 2
        });
        project.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
        {
            Id = "wild_wind",
            Name = "Wind",
            Text = "Ветер шумит в ветвях.",
            LocationTags = { "wild" },
            Weight = 3
        });

        var report = new GameRandomDirectorService().BuildReport(project);

        Assert.Contains(report.Coverage, x => x.ScopeType == "location" && x.ScopeId == "market" && x.EventIds.Contains("market_rumor"));
        Assert.Contains(report.Coverage, x => x.ScopeType == "location" && x.ScopeId == "forest" && x.EventIds.Contains("wild_wind"));
        Assert.Contains(report.Coverage, x => x.ScopeType == "locationTag" && x.ScopeId == "wild" && x.EventIds.Contains("wild_wind"));
    }

    [Fact]
    public void BuildReport_DetectsMissingLocationIdsAndTimeSegments()
    {
        var project = TestProjects.CreateWorldStateProject();
        project.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
        {
            Id = "broken",
            Name = "Broken",
            Text = "Событие с плохими ссылками.",
            LocationIds = { "missing_location" },
            TimeSegmentIds = { "missing_time" }
        });

        var report = new GameRandomDirectorService().BuildReport(project);

        Assert.Contains(report.Warnings, x => x.Code == "ambient_event_missing_location" && x.EntityIds.Contains("missing_location"));
        Assert.Contains(report.Warnings, x => x.Code == "ambient_event_missing_time_segment" && x.EntityIds.Contains("missing_time"));
    }

    [Fact]
    public void BuildReport_DetectsInvalidWeightAndChancePercent()
    {
        var project = TestProjects.CreateWorldStateProject();
        project.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
        {
            Id = "bad_roll",
            Name = "Bad roll",
            Text = "Некорректный шанс.",
            Weight = 0,
            ChancePercent = 120
        });

        var report = new GameRandomDirectorService().BuildReport(project);

        Assert.Contains(report.Warnings, x => x.Code == "ambient_event_invalid_weight" && x.EntityIds.Contains("bad_roll"));
        Assert.Contains(report.Warnings, x => x.Code == "ambient_event_invalid_chance" && x.EntityIds.Contains("bad_roll"));
    }

    [Fact]
    public void BuildGenerationUserPrompt_IncludesDesignReportRequestedCountAndExistingEventIds()
    {
        var project = TestProjects.CreateWorldStateProject();
        SetRandomness(project, "high");
        project.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
        {
            Id = "known_event",
            Name = "Known",
            Text = "Известное событие."
        });
        var service = new GameRandomDirectorService();
        var report = service.BuildReport(project);

        var prompt = service.BuildGenerationUserPrompt(project, report, 8);

        Assert.Contains("\"requestedEventCount\": 8", prompt);
        Assert.Contains("designSummary", prompt);
        Assert.Contains("randomDirectorReport", prompt);
        Assert.Contains("known_event", prompt);
    }

    [Fact]
    public void CompactContext_IncludesRandomDirectorSummaryWithinBudget()
    {
        var project = TestProjects.CreateWorldStateProject();
        SetRandomness(project, "high");
        for (var i = 0; i < 40; i++)
        {
            project.WorldState.AmbientEvents.Add(new GameAmbientEventDefinition
            {
                Id = "event_" + i,
                Name = "Event " + i,
                Text = "Событие " + i,
                Weight = 1
            });
        }

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService())
        {
            GenerationSettingsUi = new GenerationUiSettings
            {
                MaxInputContextTokens = 4096,
                ApproxCharsPerToken = 4
            }
        };

        var context = pipeline.BuildCompactProjectContextForTests(project, "random-director");
        var estimated = new PromptBudgetService().EstimateTokens(context, 4);

        Assert.Contains("randomDirectorSummary", context);
        Assert.True(estimated <= 4096 * 1.15, $"Estimated context was {estimated} tokens.");
    }

    private static void SetRandomness(GameProjectData project, string value)
    {
        var service = new GameDesignInterviewService();
        service.EnsureProfile(project.DesignProfile);
        service.SetUserAnswer(project.DesignProfile, "randomness_level", value);
    }
}
