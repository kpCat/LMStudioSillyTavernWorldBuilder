using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class PromptBudgetServiceTests
{
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
        Assert.Contains("\"hardTrimmed\": true", context);
        Assert.DoesNotContain(new string('b', 1000), context);
    }
}
