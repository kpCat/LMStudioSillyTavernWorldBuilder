using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal static class GameSceneSafety
{
    public const string TechnicalFallbackText = "Fallback scene created because generated content did not contain scenes.";

    public static bool IsTechnicalFallback(GameScene? scene)
    {
        return scene != null
            && string.Equals(scene.Text?.Trim(), TechnicalFallbackText, StringComparison.Ordinal);
    }

    public static GameScene? ResolvePlayableStartScene(GameProjectData project, string? preferredSceneId = null)
    {
        var preferred = FindScene(project, preferredSceneId);
        if (preferred != null && !IsTechnicalFallback(preferred))
        {
            return preferred;
        }

        var meta = FindScene(project, project.Meta.StartSceneId);
        if (meta != null && !IsTechnicalFallback(meta))
        {
            return meta;
        }

        var sceneStart = FindScene(project, "scene_start");
        if (sceneStart != null && !IsTechnicalFallback(sceneStart))
        {
            return sceneStart;
        }

        return project.Scenes.FirstOrDefault(x => !IsTechnicalFallback(x))
            ?? project.Scenes.FirstOrDefault();
    }

    public static int CountPlayableScenes(GameProjectData project)
    {
        return project.Scenes.Count(x => !IsTechnicalFallback(x));
    }

    public static bool HasPlayableFlow(GameProjectData project)
    {
        return project.Scenes.Any(x => !IsTechnicalFallback(x) && x.Choices.Count > 0);
    }

    private static GameScene? FindScene(GameProjectData project, string? sceneId)
    {
        return string.IsNullOrWhiteSpace(sceneId)
            ? null
            : project.Scenes.FirstOrDefault(x => string.Equals(x.Id, sceneId, StringComparison.OrdinalIgnoreCase));
    }
}
