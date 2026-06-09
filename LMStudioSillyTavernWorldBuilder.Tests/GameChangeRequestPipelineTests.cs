using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class GameChangeRequestPipelineTests
{
    [Fact]
    public void AnalyzeRequest_MapsCombatRequestToCombatSystems()
    {
        var project = TestProjects.CreateAdvancedProject();
        var report = new GameChangeRequestService().AnalyzeRequest(project, "бой слишком простой, хочу больше тактики и опасных врагов");

        Assert.Contains(report.AffectedSystems, x => x.SystemId == "combat");
        Assert.Contains(report.AffectedSystems, x => x.SystemId == "encounters");
        Assert.Contains(report.AffectedSystems, x => x.SystemId == "actions");
        Assert.Contains(report.AffectedSystems, x => x.SystemId == "formulas");
    }

    [Fact]
    public void AnalyzeRequest_MapsClothesArmorInventoryRequestToExistingSystems()
    {
        var project = TestProjects.CreateAdvancedProject();
        var report = new GameChangeRequestService().AnalyzeRequest(project, "одежда должна влиять на социальные проверки и доступ в локации");
        var systems = report.AffectedSystems.Select(x => x.SystemId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("items", systems);
        Assert.Contains("equipmentSlots", systems);
        Assert.Contains("stats", systems);
        Assert.Contains("requirements", systems);
        Assert.Contains("locations", systems);
        Assert.Contains("scenes", systems);
    }

    [Fact]
    public void AnalyzeRequest_MapsRandomnessRequestToWorldStateAmbientEventsAndRules()
    {
        var project = TestProjects.CreateWorldStateProject();
        var report = new GameChangeRequestService().AnalyzeRequest(project, "хочу больше опасных случайных событий в путешествиях");
        var systems = report.AffectedSystems.Select(x => x.SystemId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("worldState", systems);
        Assert.Contains("ambientEvents", systems);
        Assert.Contains("worldRules", systems);
        Assert.Contains("travel", systems);
    }

    [Fact]
    public void AnalyzeRequest_DetectsExistingEntityIdsAndNames()
    {
        var project = TestProjects.CreateAdvancedProject();
        project.Locations.Add(new() { Id = "city_gate", Name = "Северные ворота" });
        project.Items.Add(new() { Id = "noble_cloak", Name = "Плащ знати" });

        var report = new GameChangeRequestService().AnalyzeRequest(project, "Плащ знати должен открывать city_gate");

        Assert.Contains("noble_cloak", report.AffectedEntityIds);
        Assert.Contains("city_gate", report.AffectedEntityIds);
        Assert.Contains(report.AffectedSystems, x => x.SystemId == "items" && x.EntityIds.Contains("noble_cloak"));
        Assert.Contains(report.AffectedSystems, x => x.SystemId == "locations" && x.EntityIds.Contains("city_gate"));
    }

    [Fact]
    public void AnalyzeRequest_AddsDeleteRiskForDestructiveRequest()
    {
        var project = TestProjects.CreateAdvancedProject();

        var report = new GameChangeRequestService().AnalyzeRequest(project, "удали слишком много лута");

        Assert.Equal("RemoveOrReduceContent", report.Intent);
        Assert.Contains(report.Risks, x => x.Code == "destructive_delete_not_supported");
    }

    [Fact]
    public void BuildPatchPlan_ProducesDraftWorkflowStepsForValidRequest()
    {
        var project = TestProjects.CreateWorldStateProject();
        var service = new GameChangeRequestService();
        var report = service.AnalyzeRequest(project, "добавь больше travel event в дороге");

        var plan = service.BuildPatchPlan(project, report);

        Assert.NotEmpty(plan.Steps);
        Assert.All(plan.Steps, step => Assert.True(step.MustUseDraftWorkflow));
        Assert.Contains(plan.Steps, x => x.TargetStage == "world-state");
    }

    [Fact]
    public void BuildGenerationUserPrompt_IncludesRequestReportsDesignIdsAndNotFullDump()
    {
        var project = TestProjects.CreateWorldStateProject();
        project.World.Summary = new string('w', 9000);
        project.Scenes[0].Text = new string('s', 9000);
        var service = new GameChangeRequestService();
        var report = service.AnalyzeRequest(project, "хочу больше опасных случайных событий в путешествиях около location_start");
        var plan = service.BuildPatchPlan(project, report);

        var prompt = service.BuildGenerationUserPrompt(project, report, plan);

        Assert.Contains("хочу больше опасных случайных событий", prompt);
        Assert.Contains("impactReport", prompt);
        Assert.Contains("patchPlan", prompt);
        Assert.Contains("designSummary", prompt);
        Assert.Contains("location_start", prompt);
        Assert.DoesNotContain(new string('w', 1000), prompt);
        Assert.DoesNotContain(new string('s', 1000), prompt);
    }
}
