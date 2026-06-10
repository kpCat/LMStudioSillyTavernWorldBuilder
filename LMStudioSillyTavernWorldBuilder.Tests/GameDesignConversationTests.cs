using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameDesignConversationTests
{
    [Fact]
    public void ParseResult_ValidJsonExtractsAssistantReplyMemoryAndQuestions()
    {
        var result = new GameDesignConversationService().ParseResult(ValidJson("accepted", "user"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Звучит хорошо: фиксируем камерный детектив.", result.AssistantReply);
        Assert.Single(result.MemoryEntries);
        Assert.Equal(GameDesignConversationMemoryStatus.Accepted, result.MemoryEntries[0].Status);
        Assert.Single(result.FollowUpQuestions);
        Assert.Equal("q-001", result.FollowUpQuestions[0].Id);
    }

    [Fact]
    public void ApplyResult_UserStatedFactBecomesAcceptedUserKnowledge()
    {
        var project = new GameProjectData();
        var service = new GameDesignConversationService();
        var result = service.ParseResult(ValidJson("accepted", "user"));

        var ids = service.ApplyResult(project, result, "Мир должен быть камерным детективом.", "tone");

        Assert.Single(ids);
        var entry = Assert.Single(project.DesignKnowledgeBase.Entries);
        Assert.Equal(GameDesignKnowledgeEntryStatus.Accepted, entry.Status);
        Assert.Equal(GameDesignKnowledgeEntryKind.Decision, entry.Kind);
        Assert.Equal("user", entry.Source);
        Assert.Contains("detective", entry.Tags);
        Assert.Single(project.DesignConversationHistory.Turns);
        Assert.Equal(ids[0], project.DesignConversationHistory.Turns[0].ExtractedKnowledgeEntryIds[0]);
    }

    [Fact]
    public void ApplyResult_AssistantSuggestionRemainsProposed()
    {
        var project = new GameProjectData();
        var service = new GameDesignConversationService();
        var result = service.ParseResult(ValidJson("proposed", "assistant"));

        service.ApplyResult(project, result, "Что можно добавить?");

        var entry = Assert.Single(project.DesignKnowledgeBase.Entries);
        Assert.Equal(GameDesignKnowledgeEntryStatus.Proposed, entry.Status);
        Assert.Equal(GameDesignKnowledgeEntryKind.Note, entry.Kind);
        Assert.Equal("assistant", entry.Source);
    }

    [Fact]
    public void MalformedJson_DoesNotMutateKnowledgeBaseOrHistory()
    {
        var project = new GameProjectData();
        var service = new GameDesignConversationService();
        var result = service.ParseResult("{ not-json ");

        var ids = service.ApplyResult(project, result, "сломанный ответ");

        Assert.False(result.IsSuccess);
        Assert.Empty(ids);
        Assert.Empty(project.DesignKnowledgeBase.Entries);
        Assert.Empty(project.DesignConversationHistory.Turns);
    }

    [Fact]
    public async Task Storage_RoundTripsConversationHistoryFile()
    {
        var storage = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = storage.CreateNewProject(root, "Conversation");
        var service = new GameDesignConversationService();
        var result = service.ParseResult(ValidJson("accepted", "user"));
        service.ApplyResult(project, result, "Запомни камерный детектив.", "tone");

        await storage.SaveProjectAsync(root, project);
        var loaded = await storage.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "design", "conversation-history.json")));
        Assert.Single(loaded.DesignConversationHistory.Turns);
        Assert.Equal("Запомни камерный детектив.", loaded.DesignConversationHistory.Turns[0].UserMessage);
        Assert.Contains(loaded.DesignKnowledgeBase.Entries, x => x.Summary.Contains("камерн", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CopyMutableData_CopiesConversationHistory()
    {
        var source = new GameProjectData();
        source.DesignConversationHistory.Turns.Add(new GameDesignConversationTurn
        {
            Id = "turn_1",
            UserMessage = "сообщение",
            AssistantReply = "ответ",
            ExtractedKnowledgeEntryIds = { "knowledge_1" }
        });
        var target = new GameProjectData();

        new GameProjectCloneService().CopyMutableData(source, target);

        Assert.Single(target.DesignConversationHistory.Turns);
        Assert.NotSame(source.DesignConversationHistory, target.DesignConversationHistory);
        Assert.Equal("knowledge_1", target.DesignConversationHistory.Turns[0].ExtractedKnowledgeEntryIds[0]);
    }

    [Fact]
    public void BuildConversationUserPrompt_IncludesKnowledgeSummaryButNotFullRawProjectDump()
    {
        var project = new GameProjectData();
        project.Meta.Title = "Тест";
        project.World.Summary = new string('w', 9000);
        var entry = new GameDesignKnowledgeBaseService().AddConstraint(project.DesignKnowledgeBase, "tone", "scope", "Камерный детектив без эпического спасения мира.");
        entry.Tags.Add("tone");
        var prompt = new GameDesignConversationService().BuildConversationUserPrompt(project, "обсудим тон", "tone");

        Assert.Contains("designKnowledgeSummary", prompt);
        Assert.Contains("Камерный детектив", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("currentUserMessage", prompt);
        Assert.DoesNotContain(new string('w', 1000), prompt);
    }

    private static string ValidJson(string status, string source)
    {
        return $$"""
{
  "assistantReply": "Звучит хорошо: фиксируем камерный детектив.",
  "memoryEntries": [
    {
      "category": "tone",
      "subcategory": "scope",
      "topic": "core_tone",
      "summary": "Игра должна быть камерным детективом без эпического спасения мира.",
      "status": "{{status}}",
      "importance": "high",
      "source": "{{source}}",
      "tags": ["detective", "tone"],
      "relatedEntityIds": [],
      "affectsSystems": ["narrative"]
    }
  ],
  "followUpQuestions": [
    {
      "id": "q-001",
      "topic": "protagonist",
      "question": "Кто главный герой?",
      "priority": "normal",
      "canSkip": true,
      "suggestedOptions": ["сыщик", "журналист"]
    }
  ],
  "warnings": []
}
""";
    }
}
