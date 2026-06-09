using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameProjectRepairService
{
    public void ApplySafeRepairs(GameProjectData project, Action<string> log)
    {
        foreach (var status in project.StatusEffects)
        {
            if (status.MaxStacks <= 0)
            {
                status.MaxStacks = 1;
                log("Repair: status MaxStacks was set to 1: " + status.Id);
            }
            if (status.DefaultDurationTurns < 0)
            {
                status.DefaultDurationTurns = 0;
                log("Repair: status DefaultDurationTurns was set to 0: " + status.Id);
            }
            if (string.IsNullOrWhiteSpace(status.StackMode))
            {
                status.StackMode = "refresh";
                log("Repair: status StackMode was set to refresh: " + status.Id);
            }
            if (string.IsNullOrWhiteSpace(status.Kind))
            {
                status.Kind = "neutral";
                log("Repair: status Kind was set to neutral: " + status.Id);
            }

            RepairEffects(status.OnApplyEffects.Concat(status.PeriodicEffects).Concat(status.OnExpireEffects), log);
        }

        foreach (var effect in project.Items.SelectMany(x => x.UseEffects.Concat(x.EquipEffects).Concat(x.UnequipEffects))
            .Concat(project.Skills.SelectMany(x => x.Effects))
            .Concat(project.Locations.SelectMany(x => x.EnterEffects))
            .Concat(project.LocationConnections.SelectMany(x => x.TravelEffects))
            .Concat(project.Scenes.SelectMany(x => x.Choices.SelectMany(c => c.Effects)))
            .Concat(project.Encounters.SelectMany(x => x.OnStartEffects.Concat(x.OnWinEffects).Concat(x.OnLoseEffects).Concat(x.Choices.SelectMany(c => c.Effects))))
            .Concat(project.Actions.SelectMany(x => x.Effects))
            .Concat(project.ProgressionNodes.SelectMany(x => x.UnlockEffects)))
        {
            RepairEffect(effect, log);
        }

        if (project.Scenes.Count == 0)
        {
            project.Scenes.Add(new GameScene
            {
                Id = "scene_start",
                Title = "Start",
                Text = "Fallback scene created because generated content did not contain scenes."
            });
            log("Repair: fallback scene was created.");
        }

        if (string.IsNullOrWhiteSpace(project.Meta.StartSceneId) || project.Scenes.All(x => x.Id != project.Meta.StartSceneId))
        {
            project.Meta.StartSceneId = project.Scenes[0].Id;
            log("Repair: start scene was set to first scene: " + project.Meta.StartSceneId);
        }
    }

    private static void RepairEffects(IEnumerable<GameEffect> effects, Action<string> log)
    {
        foreach (var effect in effects)
        {
            RepairEffect(effect, log);
        }
    }

    private static void RepairEffect(GameEffect effect, Action<string> log)
    {
        if (effect.ChancePercent < 0)
        {
            effect.ChancePercent = 0;
            log("Repair: effect ChancePercent was set to 0.");
        }
        else if (effect.ChancePercent > 100)
        {
            effect.ChancePercent = 100;
            log("Repair: effect ChancePercent was set to 100.");
        }
    }

    public void PreserveIdentity(GameProjectData current, GameProjectData generated, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(generated.Summary.ProjectPath))
        {
            generated.Summary.ProjectPath = current.Summary.ProjectPath;
            log("Repair: project path was restored from current project.");
        }
        if (string.IsNullOrWhiteSpace(generated.Summary.Id))
        {
            generated.Summary.Id = current.Summary.Id;
        }
        if (string.IsNullOrWhiteSpace(generated.Summary.Title))
        {
            generated.Summary.Title = current.Summary.Title;
        }
        if (string.IsNullOrWhiteSpace(generated.Summary.FolderName))
        {
            generated.Summary.FolderName = current.Summary.FolderName;
        }
        if (string.IsNullOrWhiteSpace(generated.Meta.Id))
        {
            generated.Meta.Id = current.Meta.Id;
            log("Repair: Meta.Id was restored from current project.");
        }
        if (string.IsNullOrWhiteSpace(generated.Meta.Title))
        {
            generated.Meta.Title = current.Meta.Title;
        }
    }
}
