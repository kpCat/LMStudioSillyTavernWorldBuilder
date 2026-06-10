using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameDesignBrainTests
{
    [Fact]
    public void DefaultCatalog_IncludesRequiredBaselineSlots()
    {
        var slots = new GameDesignSlotCatalog().CreateDefaultSlots();
        var ids = slots.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var id in new[]
        {
            "genre", "tone", "player_role", "main_goal", "core_loop", "world_scale", "map_type",
            "time_system", "combat_style", "inventory_depth", "equipment_depth", "dialogue_depth",
            "randomness_level", "progression_type", "economy_depth", "quest_structure",
            "save_load_policy", "failure_policy", "visual_style"
        })
        {
            Assert.Contains(id, ids);
        }
    }

    [Fact]
    public void EnsureDefaultSlots_PreservesExistingUserValues()
    {
        var profile = new GameDesignProfile
        {
            Slots =
            {
                new GameDesignSlot
                {
                    Id = "genre",
                    Value = "мистический детектив",
                    Source = GameDesignSlotValueSource.User,
                    Confidence = 1
                }
            }
        };

        new GameDesignSlotCatalog().EnsureDefaultSlots(profile);

        var genre = profile.Slots.Single(x => x.Id == "genre");
        Assert.Equal("мистический детектив", genre.Value);
        Assert.Equal(GameDesignSlotValueSource.User, genre.Source);
        Assert.Contains(profile.Slots, x => x.Id == "combat_style");
    }

    [Fact]
    public void ManualMode_ReturnsRequiredMissingQuestions()
    {
        var profile = new GameDesignProfile { CreationMode = GameCreationMode.Manual };
        var service = new GameDesignInterviewService();

        var questions = service.GetQuestions(profile);

        Assert.Contains(questions, x => x.SlotId == "genre");
        Assert.Contains(questions, x => x.SlotId == "main_goal");
        Assert.Contains(questions, x => x.SlotId == "combat_style");
    }

    [Fact]
    public void QuickPrototype_ReturnsFewerCriticalQuestionsThanManual()
    {
        var service = new GameDesignInterviewService();
        var manual = new GameDesignProfile { CreationMode = GameCreationMode.Manual };
        var quick = new GameDesignProfile { CreationMode = GameCreationMode.QuickPrototype };

        var manualQuestions = service.GetQuestions(manual);
        var quickQuestions = service.GetQuestions(quick);

        Assert.True(quickQuestions.Count < manualQuestions.Count);
        Assert.Contains(quickQuestions, x => x.SlotId == "genre");
        Assert.Contains(quickQuestions, x => x.SlotId == "player_role");
        Assert.Contains(quickQuestions, x => x.SlotId == "main_goal");
        Assert.Contains(quickQuestions, x => x.SlotId == "core_loop");
        Assert.Contains(quickQuestions, x => x.SlotId == "combat_style");
        Assert.Contains(quickQuestions, x => x.SlotId == "randomness_level");
    }

    [Fact]
    public void UserAnswer_IsNotOverwrittenByLlmAssumption()
    {
        var profile = new GameDesignProfile();
        var service = new GameDesignInterviewService();
        service.SetUserAnswer(profile, "genre", "космоопера");

        service.ApplyLlmAssumptionsFromJson(profile, """
        {
          "assumptions": [
            { "slotId": "genre", "value": "тёмное фэнтези", "confidence": 0.9, "notes": "assumed" }
          ]
        }
        """);

        var genre = profile.Slots.Single(x => x.Id == "genre");
        Assert.Equal("космоопера", genre.Value);
        Assert.Equal(GameDesignSlotValueSource.User, genre.Source);
    }

    [Fact]
    public void UserAnswer_CanUpdateAlreadyFilledSlot()
    {
        var profile = new GameDesignProfile();
        var service = new GameDesignInterviewService();

        service.SetUserAnswer(profile, "genre", "мистический детектив");
        service.SetUserAnswer(profile, "genre", "научная фантастика");

        var genre = profile.Slots.Single(x => x.Id == "genre");
        Assert.Equal("научная фантастика", genre.Value);
        Assert.Equal(GameDesignSlotValueSource.User, genre.Source);
    }

    [Fact]
    public void LlmAssumptionJson_FillsKnownEmptySlotsAndIgnoresUnknown()
    {
        var profile = new GameDesignProfile();
        var service = new GameDesignInterviewService();

        var applied = service.ApplyLlmAssumptionsFromJson(profile, """
        {
          "assumptions": [
            { "slotId": "genre", "value": "тёмное фэнтези", "confidence": 1.5, "notes": "по идее" },
            { "slotId": "unknown_slot", "value": "x", "confidence": 0.5, "notes": "ignore" }
          ]
        }
        """);

        var genre = profile.Slots.Single(x => x.Id == "genre");
        Assert.Equal(1, applied);
        Assert.Equal("тёмное фэнтези", genre.Value);
        Assert.Equal(GameDesignSlotValueSource.LlmAssumption, genre.Source);
        Assert.Equal(1, genre.Confidence);
    }

    [Fact]
    public void Planner_IncludesCombatStepWhenCombatIsEnabled()
    {
        var project = TestProjects.CreatePlayableProject();
        var service = new GameDesignInterviewService();
        service.SetUserAnswer(project.DesignProfile, "combat_style", "простая пошаговая");

        var plan = new GameDesignPlannerService().BuildPlan(project);

        Assert.Contains(plan.Steps, x => x.Id == "build_combat_encounters");
    }

    [Fact]
    public void Planner_OmitsCombatStepWhenCombatIsDisabled()
    {
        var project = TestProjects.CreatePlayableProject();
        var service = new GameDesignInterviewService();
        service.SetUserAnswer(project.DesignProfile, "combat_style", "нет боёв");

        var plan = new GameDesignPlannerService().BuildPlan(project);

        Assert.DoesNotContain(plan.Steps, x => x.Id == "build_combat_encounters");
    }

    [Fact]
    public async Task Storage_SavesAndLoadsDesignProfileAndCreationPlan()
    {
        var storage = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = storage.CreateNewProject(root, "Design Brain");
        var interview = new GameDesignInterviewService();
        interview.ApplyInitialIdea(project.DesignProfile, "игра о городе под снегом");
        interview.SetUserAnswer(project.DesignProfile, "genre", "мистический детектив");
        project.CreationPlan = new GameDesignPlannerService().BuildPlan(project);

        await storage.SaveProjectAsync(root, project);
        var loaded = await storage.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "design", "design-profile.json")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "design", "creation-plan.json")));
        Assert.Equal("игра о городе под снегом", loaded.DesignProfile.InitialIdea);
        Assert.Contains(loaded.DesignProfile.Slots, x => x.Id == "genre" && x.Value == "мистический детектив");
        Assert.NotEmpty(loaded.CreationPlan.Steps);
    }

    [Fact]
    public async Task Storage_LoadsProjectsWithoutDesignFiles()
    {
        var storage = new GameStorageService();
        var root = TestPaths.CreateTempDirectory();
        var project = storage.CreateNewProject(root, "Old Project");
        await storage.SaveProjectAsync(root, project);
        File.Delete(Path.Combine(project.Summary.ProjectPath, "design", "design-profile.json"));
        File.Delete(Path.Combine(project.Summary.ProjectPath, "design", "creation-plan.json"));

        var loaded = await storage.LoadProjectAsync(project.Summary.ProjectPath);

        Assert.NotNull(loaded.DesignProfile);
        Assert.NotNull(loaded.CreationPlan);
    }
}
