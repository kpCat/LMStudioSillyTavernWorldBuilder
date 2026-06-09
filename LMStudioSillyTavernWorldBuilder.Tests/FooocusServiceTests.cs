using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class FooocusServiceTests
{
    [Fact]
    public void FooocusProfileDetector_DetectsArchiveRoot()
    {
        var root = TestPaths.CreateTempDirectory();
        File.WriteAllText(Path.Combine(root, "run.bat"), "");
        Directory.CreateDirectory(Path.Combine(root, "Fooocus"));
        File.WriteAllText(Path.Combine(root, "Fooocus", "config.txt"), "path_outputs = Z:\\missing\\outputs");

        var settings = new FooocusProfileDetector().DetectFromFolder(root);

        Assert.Equal(Path.Combine(root, "run.bat"), settings.LaunchFilePath);
        Assert.Equal(root, settings.WorkingDirectory);
        Assert.Equal(Path.Combine(root, "Fooocus", "outputs"), settings.OutputDirectory);
        Assert.Equal("", settings.WebEndpoint);
    }

    [Fact]
    public void FooocusQueueExport_WritesTxtAndJson()
    {
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        project.ImagePrompts.Add(TestProjects.CreateScenePrompt());
        var service = new FooocusService();

        service.ExportQueue(project, _ => { });

        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "prompts", "fooocus-queue.txt")));
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, "prompts", "fooocus-queue.json")));
    }

    [Fact]
    public void FooocusImport_DoesNotPutAllImagesIntoScenes()
    {
        var output = TestPaths.CreateTempDirectory();
        File.WriteAllBytes(Path.Combine(output, "image.png"), new byte[] { 1, 2, 3 });
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var service = new FooocusService();

        var count = service.ImportGeneratedImages(project, new Providers.FooocusSettings { OutputDirectory = output }, _ => { });

        Assert.Equal(1, count);
        var scenesFolder = Path.Combine(project.Summary.ProjectPath, "assets", "scenes");
        Assert.True(!Directory.Exists(scenesFolder) || !Directory.EnumerateFiles(scenesFolder, "*", SearchOption.AllDirectories).Any());
        Assert.Single(project.GeneratedImageCandidates);
        Assert.Contains("generated-imports", project.GeneratedImageCandidates[0].ProjectRelativePath);
    }

    [Fact]
    public void LinkPromptToEntity_CopiesToCorrectAssetFolder()
    {
        var source = Path.Combine(TestPaths.CreateTempDirectory(), "portrait.png");
        File.WriteAllBytes(source, new byte[] { 1, 2, 3 });
        var project = TestProjects.CreatePlayableProject();
        project.Summary.ProjectPath = TestPaths.CreateTempDirectory();
        var prompt = new ImagePromptDefinition
        {
            AssetId = "char_art",
            TargetType = ImageTargetType.Character,
            TargetEntityId = "npc"
        };

        var relative = new ImageAssetService().LinkPromptToImage(project, prompt, source);

        Assert.Contains(Path.Combine("assets", "characters"), relative);
        Assert.True(File.Exists(Path.Combine(project.Summary.ProjectPath, relative)));
        Assert.Equal("char_art", project.Characters.Single(x => x.Id == "npc").PortraitAssetId);
    }
}
