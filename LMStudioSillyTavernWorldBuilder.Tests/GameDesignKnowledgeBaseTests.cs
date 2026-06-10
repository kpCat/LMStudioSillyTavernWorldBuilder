using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Providers;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameDesignKnowledgeBaseTests
{
    [Fact]
    public void AddOrUpdateEntry_UpdatesExistingEntryAndRefreshesUpdatedUtc()
    {
        var knowledgeBase = new GameDesignKnowledgeBase();
        var service = new GameDesignKnowledgeBaseService();
        var entry = new GameDesignKnowledgeEntry
        {
            Id = "decision_core_loop",
            Category = "core",
            Topic = "loop",
            Summary = "Игрок исследует город.",
            Status = GameDesignKnowledgeEntryStatus.Accepted,
            Kind = GameDesignKnowledgeEntryKind.Decision
        };

        service.AddOrUpdateEntry(knowledgeBase, entry);
        var firstUpdated = knowledgeBase.Entries[0].UpdatedUtc;
        entry.Summary = "Игрок исследует город и собирает слухи.";
        service.AddOrUpdateEntry(knowledgeBase, entry);

        Assert.Single(knowledgeBase.Entries);
        Assert.Equal("Игрок исследует город и собирает слухи.", knowledgeBase.Entries[0].Summary);
        Assert.True(knowledgeBase.Entries[0].UpdatedUtc >= firstUpdated);
        Assert.True(knowledgeBase.UpdatedUtc >= knowledgeBase.Entries[0].UpdatedUtc);
    }

    [Fact]
    public void Retrieval_ExcludesRejectedAndSupersededByDefault()
    {
        var knowledgeBase = new GameDesignKnowledgeBase
        {
            Entries =
            {
                Entry("accepted", GameDesignKnowledgeEntryStatus.Accepted),
                Entry("rejected", GameDesignKnowledgeEntryStatus.Rejected),
                Entry("superseded", GameDesignKnowledgeEntryStatus.Superseded)
            }
        };
        var service = new GameDesignKnowledgeBaseService();

        var entries = service.GetRelevantEntries(knowledgeBase, new GameDesignKnowledgeQuery(), 10);

        Assert.Single(entries);
        Assert.Equal("accepted", entries[0].Id);
    }

    [Fact]
    public void Retrieval_PrioritizesAcceptedHighCriticalRelevantEntries()
    {
        var knowledgeBase = new GameDesignKnowledgeBase
        {
            Entries =
            {
                Entry("normal", GameDesignKnowledgeEntryStatus.Proposed, GameDesignKnowledgeImportance.Normal, "combat"),
                Entry("high", GameDesignKnowledgeEntryStatus.Accepted, GameDesignKnowledgeImportance.High, "combat"),
                Entry("critical", GameDesignKnowledgeEntryStatus.Accepted, GameDesignKnowledgeImportance.Critical, "combat")
            }
        };
        var service = new GameDesignKnowledgeBaseService();

        var entries = service.GetRelevantEntries(knowledgeBase, new GameDesignKnowledgeQuery { AffectsSystems = { "combat" } }, 10);

        Assert.Equal(new[] { "critical", "high", "normal" }, entries.Select(x => x.Id).ToArray());
    }

    [Fact]
    public void CompactSummary_RespectsMaxCharactersAndContainsNoRawConversationMarker()
    {
        var knowledgeBase = new GameDesignKnowledgeBase
        {
            Entries =
            {
                Entry("critical", GameDesignKnowledgeEntryStatus.Accepted, GameDesignKnowledgeImportance.Critical, "world", "Нельзя включать raw conversation marker CHAT_LOG.")
            }
        };
        var service = new GameDesignKnowledgeBaseService();

        var summary = service.BuildCompactSummary(knowledgeBase, new GameDesignKnowledgeQuery(), 80);

        Assert.True(summary.Length <= 80);
        Assert.DoesNotContain("CHAT_LOG", summary);
    }

    [Fact]
    public async Task Storage_RoundTripsKnowledgeBaseFile()
    {
        var storage = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = storage.CreateNewProject(root, "Knowledge");
        new GameDesignKnowledgeBaseService().AddAcceptedDecision(project.DesignKnowledgeBase, "world", "tone", "Мир должен быть камерным.");

        await storage.SaveProjectAsync(root, project);
        var loaded = await storage.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "design", "knowledge-base.json")));
        Assert.Contains(loaded.DesignKnowledgeBase.Entries, x => x.Summary == "Мир должен быть камерным.");
    }

    [Fact]
    public void CopyMutableData_CopiesKnowledgeBase()
    {
        var source = new GameProjectData();
        var target = new GameProjectData();
        new GameDesignKnowledgeBaseService().AddConstraint(source.DesignKnowledgeBase, "combat", "scope", "Бои должны быть редкими.");

        new GameProjectCloneService().CopyMutableData(source, target);

        Assert.Contains(target.DesignKnowledgeBase.Entries, x => x.Summary == "Бои должны быть редкими.");
        Assert.NotSame(source.DesignKnowledgeBase, target.DesignKnowledgeBase);
    }

    [Fact]
    public void PipelineCompactContext_IncludesKnowledgeSummary()
    {
        var project = new GameProjectData();
        new GameDesignKnowledgeBaseService().AddConstraint(project.DesignKnowledgeBase, "combat", "scope", "Combat must stay rare.");
        project.DesignKnowledgeBase.Entries[0].AffectsSystems.Add("combat");
        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService());

        var context = pipeline.BuildCompactProjectContextForTests(project, "combat");

        Assert.Contains("designKnowledgeSummary", context);
        Assert.Contains("Combat must stay rare.", context);
    }

    private static GameDesignKnowledgeEntry Entry(
        string id,
        GameDesignKnowledgeEntryStatus status,
        GameDesignKnowledgeImportance importance = GameDesignKnowledgeImportance.Normal,
        string affectsSystem = "",
        string summary = "summary")
    {
        return new GameDesignKnowledgeEntry
        {
            Id = id,
            Category = "design",
            Topic = "topic",
            Summary = summary,
            Status = status,
            Kind = GameDesignKnowledgeEntryKind.Decision,
            Importance = importance,
            AffectsSystems = string.IsNullOrWhiteSpace(affectsSystem) ? new List<string>() : new List<string> { affectsSystem },
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
    }
}
