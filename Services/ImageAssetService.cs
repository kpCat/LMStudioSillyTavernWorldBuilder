using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class ImageAssetService
{
    public string LinkPromptToImage(GameProjectData project, ImagePromptDefinition prompt, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            throw new InvalidOperationException("Project path is not set.");
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Image file was not found.", sourcePath);
        }

        var folder = prompt.TargetType switch
        {
            ImageTargetType.Scene => "scenes",
            ImageTargetType.Character => "characters",
            ImageTargetType.Item => "items",
            ImageTargetType.Cover => "ui",
            ImageTargetType.Ui => "ui",
            _ => "ui"
        };

        var targetFolder = Path.Combine(project.Summary.ProjectPath, "assets", folder);
        Directory.CreateDirectory(targetFolder);
        var targetPath = GetUniquePath(targetFolder, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, targetPath, overwrite: false);

        var relativePath = Path.GetRelativePath(project.Summary.ProjectPath, targetPath);
        prompt.SelectedImagePath = relativePath;
        prompt.Status = ImagePromptStatus.Linked;

        project.AssetLinks.RemoveAll(x => x.AssetId == prompt.AssetId);
        project.AssetLinks.Add(new ImageAssetLink
        {
            AssetId = prompt.AssetId,
            TargetType = prompt.TargetType,
            TargetEntityId = prompt.TargetEntityId,
            ImagePath = relativePath
        });

        ApplyEntityAsset(project, prompt);
        return relativePath;
    }

    public static string ResolveProjectPath(GameProjectData project, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "";
        }

        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(project.Summary.ProjectPath, path);
    }

    private static void ApplyEntityAsset(GameProjectData project, ImagePromptDefinition prompt)
    {
        if (prompt.TargetType == ImageTargetType.Scene)
        {
            var scene = project.Scenes.FirstOrDefault(x => x.Id == prompt.TargetEntityId);
            if (scene != null) scene.ImageAssetId = prompt.AssetId;
        }
        else if (prompt.TargetType == ImageTargetType.Character)
        {
            var character = project.Characters.FirstOrDefault(x => x.Id == prompt.TargetEntityId);
            if (character != null) character.PortraitAssetId = prompt.AssetId;
        }
        else if (prompt.TargetType == ImageTargetType.Item)
        {
            var item = project.Items.FirstOrDefault(x => x.Id == prompt.TargetEntityId);
            if (item != null) item.ImageAssetId = prompt.AssetId;
        }
    }

    private static string GetUniquePath(string folder, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var target = Path.Combine(folder, fileName);
        var index = 1;
        while (File.Exists(target))
        {
            target = Path.Combine(folder, $"{baseName}_{index}{extension}");
            index++;
        }

        return target;
    }
}
