using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class PromptBudgetServiceTests
{
    [Fact]
    public void ConservativeEstimator_CountsCyrillicMoreStrictly()
    {
        var service = new PromptBudgetService();
        var text = string.Concat(Enumerable.Repeat("жанр мистика город снег ", 40));

        var oldEstimate = service.EstimateTokens(text, 4);
        var conservativeEstimate = service.EstimateTokensConservative(text, 4);

        Assert.True(conservativeEstimate > oldEstimate, $"Expected conservative={conservativeEstimate} to exceed old={oldEstimate}.");
    }

    [Fact]
    public void BatchPrompt_PreflightRejectsOversizedFullPromptBeforeLmCall()
    {
        var project = TestProjects.CreateAdvancedProject();
        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService())
        {
            GenerationSettingsUi = new GenerationUiSettings
            {
                MaxInputContextTokens = 2048,
                MaxOutputTokens = 1024,
                MaxTokens = 1024,
                ApproxCharsPerToken = 4
            }
        };
        var hugeRules = string.Concat(Enumerable.Repeat("Русские правила генерации должны остаться в полном prompt. ", 600));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            pipeline.BuildContentBatchUserContentWithinBudgetForTests(project, Prompts.GenerateStatsAndResourcesBatch, hugeRules, 3, "stats_resources", "stats-resources"));

        Assert.Contains("Prompt too large after compaction", ex.Message);
    }

    [Fact]
    public void BatchPrompt_UsesConfiguredProfileContextBudget()
    {
        var project = TestProjects.CreateAdvancedProject();
        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService())
        {
            GenerationSettingsUi = new GenerationUiSettings
            {
                MaxInputContextTokens = 12000,
                MaxOutputTokens = 4096,
                MaxTokens = 4096,
                ApproxCharsPerToken = 4
            }
        };

        var userContent = pipeline.BuildContentBatchUserContentWithinBudgetForTests(project, Prompts.GenerateStatsAndResourcesBatch, string.Empty, 3, "stats_resources", "stats-resources");
        var estimated = pipeline.EstimateFullPromptTokensForTests(new[]
        {
            new ChatMessage { Role = "system", Content = Prompts.GenerateStatsAndResourcesBatch.SystemPrompt },
            new ChatMessage { Role = "user", Content = userContent }
        });
        var safeBudget = pipeline.CalculateSafePromptBudgetTokensForTests(Prompts.GenerateStatsAndResourcesBatch.Settings.MaxTokens);

        Assert.Contains("\"maxInputContextTokens\":12000", userContent);
        Assert.True(estimated <= safeBudget, $"Estimated prompt was {estimated}, safe budget was {safeBudget}.");
    }

    [Fact]
    public void BatchPrompt_DoesNotUnicodeEscapeRussianPromptContent()
    {
        var project = TestProjects.CreateAdvancedProject();
        project.Meta.Title = "РЎРІРµС‚РѕРіСЂР°Рґ";
        project.Meta.Description = "РќРѕСЃРёС‚РµР»СЊ РёСЃРєСЂС‹ РёС‰РµС‚ РњРµС‚Р°РјРѕРґСѓР»СЊ.";
        project.Brief.Text = "РњРµС‚Р°РјРѕРґСѓР»СЊ РґРµСЂР¶РёС‚ РіРѕСЂРѕРґ РІ СЂР°РІРЅРѕРІРµСЃРёРё.";
        project.Concept.Text = "РЎРІРµС‚РѕРіСЂР°Рґ Р¶РёРІС‘С‚ РЅР° СЌРЅРµСЂРіРёРё РЅРѕСЃРёС‚РµР»РµР№.";
        project.MvpPlan.Text = "РЎРѕР±СЂР°С‚СЊ СЂРµСЃСѓСЂСЃС‹ Рё РЅР°Р№С‚Рё РёСЃС‚РѕС‡РЅРёРє.";
        project.DesignProfile.InitialIdea = "РќРѕСЃРёС‚РµР»СЊ РїСѓС‚РµС€РµСЃС‚РІСѓРµС‚ РїРѕ РЎРІРµС‚РѕРіСЂР°РґСѓ.";
        project.GenerationPreferences.GeneralGameplayText = "РџРёСЃР°С‚СЊ РЅР° СЂСѓСЃСЃРєРѕРј, Р±РµР· Р»Р°С‚РёРЅРёР·Р°С†РёРё.";
        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService())
        {
            GenerationSettingsUi = new GenerationUiSettings
            {
                MaxInputContextTokens = 12000,
                MaxOutputTokens = 4096,
                MaxTokens = 4096,
                ApproxCharsPerToken = 4
            }
        };

        var userContent = pipeline.BuildContentBatchUserContentWithinBudgetForTests(project, Prompts.GenerateStatsAndResourcesBatch, string.Empty, 3, "stats_resources", "stats-resources");
        var estimated = pipeline.EstimateFullPromptTokensForTests(new[]
        {
            new ChatMessage { Role = "system", Content = Prompts.GenerateStatsAndResourcesBatch.SystemPrompt },
            new ChatMessage { Role = "user", Content = userContent }
        });
        var safeBudget = pipeline.CalculateSafePromptBudgetTokensForTests(Prompts.GenerateStatsAndResourcesBatch.Settings.MaxTokens);

        Assert.Contains("РќРѕСЃРёС‚РµР»СЊ", userContent);
        Assert.Contains("РњРµС‚Р°РјРѕРґСѓР»СЊ", userContent);
        Assert.DoesNotContain("\\u041", userContent);
        Assert.DoesNotContain("\\u043", userContent);
        Assert.DoesNotContain("\\u04", userContent);
        Assert.True(estimated <= safeBudget, $"Estimated prompt was {estimated}, safe budget was {safeBudget}.");
    }

    [Fact]
    public void GameplayActionsPrompt_ForbidsFormulaStringsInAmount()
    {
        var prompt = Prompts.GenerateGameplayActionsBatch.SystemPrompt;

        Assert.Contains("amount must be integer", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Never put formulas", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"amount\": \"5 + dice(1, 4)\"", prompt);
        Assert.Contains("\"amount\": 5", prompt);
    }

    [Fact]
    public void WorldStatePrompt_ForbidsObjectTriggersAndSingularEffect()
    {
        var prompt = Prompts.GenerateWorldStateBatch.SystemPrompt;

        Assert.Contains("trigger must be a string", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Allowed trigger strings", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"trigger\": {", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"trigger\": \"turnEnd\"", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Use effects array", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not output singular effect object", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Requirement.value must be integer", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stringValue", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"value\": \"unstable\"", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScenesPrompt_ForbidsLocationIdsAsNextSceneIds()
    {
        var prompt = Prompts.GenerateScenesBatch.SystemPrompt;

        Assert.Contains("text", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nextSceneId", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("location id", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"nextSceneId\": \"location_border_checkpoint\"", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scene_border_checkpoint_return", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("conditions", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("effects", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Текст эффекта не является state id", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aspect_border_stability", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"type\": \"log\"", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SpellsPrompt_RequiresSkillsKeyAndSupportedStackModes()
    {
        var prompt = Prompts.GenerateSpellsBatch.SystemPrompt;

        Assert.Contains("Top-level JSON key must be skills", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("There is no top-level spells collection", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kind", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("spell", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("stackMode must be one of refresh, stack, ignore, replace", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BAD: { \"type\": \"will\"", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GOOD: { \"type\": \"stat\"", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SerializeWithinBudget_UsesConservativeEstimatorForRussianText()
    {
        var service = new PromptBudgetService();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        var text = service.SerializeWithinBudget(limit => new
        {
            Items = Enumerable.Range(0, limit).Select(i => new
            {
                Id = "item_" + i,
                Text = string.Concat(Enumerable.Repeat("РќРѕСЃРёС‚РµР»СЊ РњРµС‚Р°РјРѕРґСѓР»СЊ РЎРІРµС‚РѕРіСЂР°Рґ ", 6))
            })
        }, 220, 4, jsonOptions);
        var estimate = service.EstimateTokensConservative(text, 4);

        Assert.True(estimate <= 220, $"Estimated prompt was {estimate}.");
        Assert.Contains("РќРѕСЃРёС‚РµР»СЊ", text);
        Assert.DoesNotContain("item_2", text);
    }

    [Fact]
    public void CompactContext_StaysNearConfiguredBudget()
    {
        var project = TestProjects.CreateAdvancedProject();
        project.GenerationPreferences.SkillDesignText = new string('s', 3000);
        project.GenerationPreferences.ProgressionDesignText = new string('p', 3000);
        for (var i = 0; i < 180; i++)
        {
            project.Skills.Add(new GameSkillDefinition
            {
                Id = "skill_" + i,
                Name = "Skill " + i,
                Kind = "active",
                Description = new string('x', 200)
            });
            project.Items.Add(new GameItemDefinition
            {
                Id = "item_" + i,
                Name = "Item " + i,
                Description = new string('y', 200)
            });
            project.Actions.Add(new GameActionDefinition
            {
                Id = "action_" + i,
                Name = "Action " + i,
                Description = new string('z', 200),
                Effects = { new GameEffect { Type = "skillExperience", TargetId = "focus", Amount = 1 } }
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

        var context = pipeline.BuildCompactProjectContextForTests(project, "skills");
        var estimated = new PromptBudgetService().EstimateTokens(context, 4);

        Assert.True(estimated <= 4096 * 1.15, $"Estimated context was {estimated} tokens.");
        Assert.Contains("contextBudget", context);
    }

    [Fact]
    public void CompactContext_WithTinyBudgetHardTrimsLongText()
    {
        var project = TestProjects.CreateAdvancedProject();
        project.Meta.Description = new string('d', 8000);
        project.Brief.Text = new string('b', 8000);
        project.Concept.Text = new string('c', 8000);
        project.MvpPlan.Text = new string('m', 8000);
        project.ArchitecturePlan.Text = new string('a', 8000);
        project.Mechanics.Notes = new string('n', 8000);
        project.GenerationPreferences.GeneralGameplayText = new string('g', 8000);
        project.GenerationPreferences.SkillDesignText = new string('s', 8000);
        project.GenerationPreferences.ForbiddenDesignText = new string('f', 8000);
        project.GenerationPreferences.Notes = new string('o', 8000);
        for (var i = 0; i < 120; i++)
        {
            project.Skills.Add(new GameSkillDefinition { Id = "skill_" + i, Name = "Skill " + i, Description = new string('x', 400) });
            project.Items.Add(new GameItemDefinition { Id = "item_" + i, Name = "Item " + i, Description = new string('y', 400) });
        }

        var pipeline = new GameCreationPipelineService(new LmStudioService(new HttpClient()), new GameStorageService())
        {
            GenerationSettingsUi = new GenerationUiSettings
            {
                MaxInputContextTokens = 512,
                ApproxCharsPerToken = 4
            }
        };

        var context = pipeline.BuildCompactProjectContextForTests(project, "revision-fix");
        var estimated = new PromptBudgetService().EstimateTokens(context, 4);

        Assert.True(estimated < 2500, $"Estimated compact context was still too large: {estimated} tokens.");
        Assert.Contains("\"hardTrimmed\":true", context);
        Assert.DoesNotContain(new string('b', 1000), context);
    }
}
