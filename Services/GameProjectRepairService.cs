using LMStudioSillyTavernWorldBuilder.Models;
using System.Text.RegularExpressions;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal enum GameProjectRepairMode
{
    FullProject,
    GeneratedPartialDraft
}

internal sealed class GameProjectRepairService
{
    public void ApplySafeRepairs(GameProjectData project, Action<string> log)
    {
        ApplySafeRepairs(project, log, GameProjectRepairMode.FullProject);
    }

    public void ApplySafeRepairs(GameProjectData project, Action<string> log, GameProjectRepairMode mode)
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

            RepairEffects(project, status.OnApplyEffects.Concat(status.PeriodicEffects).Concat(status.OnExpireEffects), log);
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
            RepairEffect(project, effect, log);
        }

        foreach (var formula in project.Formulas)
        {
            RepairFormula(project, formula, log);
        }
        RepairRuntimeFormulaReferences(project, log);

        RepairSceneTitles(project, log);
        RepairEncounterChoices(project, log);

        if (mode == GameProjectRepairMode.GeneratedPartialDraft)
        {
            return;
        }

        RepairTechnicalFallbackScenes(project, log);

        if (project.Scenes.Count == 0)
        {
            project.Scenes.Add(new GameScene
            {
                Id = "scene_start",
                Title = "Start",
                Text = GameSceneSafety.TechnicalFallbackText
            });
            log("Repair: fallback scene was created.");
        }

        if (string.IsNullOrWhiteSpace(project.Meta.StartSceneId) || project.Scenes.All(x => x.Id != project.Meta.StartSceneId))
        {
            project.Meta.StartSceneId = GameSceneSafety.ResolvePlayableStartScene(project)?.Id ?? project.Scenes[0].Id;
            log("Repair: start scene was set to first scene: " + project.Meta.StartSceneId);
        }
    }

    private static void RepairTechnicalFallbackScenes(GameProjectData project, Action<string> log)
    {
        var fallbackScenes = project.Scenes.Where(GameSceneSafety.IsTechnicalFallback).ToList();
        if (fallbackScenes.Count == 0 || project.Scenes.All(GameSceneSafety.IsTechnicalFallback))
        {
            return;
        }

        var realStart = project.Scenes.FirstOrDefault(x => !GameSceneSafety.IsTechnicalFallback(x));
        if (realStart == null)
        {
            return;
        }

        foreach (var fallback in fallbackScenes)
        {
            if (!string.Equals(fallback.Id, "scene_start", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            fallback.Title = "Начало";
            fallback.Text = "Вы приходите в себя на границе Светограда. Впереди дрожит пространство, и Метамодуль откликается внутри вас.";
            fallback.Choices.Clear();
            fallback.Choices.Add(new GameChoice
            {
                Id = "choice_begin_real_route",
                Text = "Двинуться к пограничному разрыву",
                NextSceneId = realStart.Id
            });
            if (string.IsNullOrWhiteSpace(fallback.LocationId))
            {
                fallback.LocationId = realStart.LocationId;
            }

            log("Repair: technical fallback scene_start was replaced with playable bridge to " + realStart.Id + ".");
        }

        if (GameSceneSafety.IsTechnicalFallback(project.Scenes.FirstOrDefault(x => string.Equals(x.Id, project.Meta.StartSceneId, StringComparison.OrdinalIgnoreCase))))
        {
            project.Meta.StartSceneId = "scene_start";
            log("Repair: start scene remains scene_start after playable bridge migration.");
        }
    }

    private static void RepairSceneTitles(GameProjectData project, Action<string> log)
    {
        foreach (var scene in project.Scenes.Where(x => string.IsNullOrWhiteSpace(x.Title)))
        {
            scene.Title = BuildSceneTitle(scene);
            log("Repair: scene title was filled: " + scene.Id);
        }
    }

    private static string BuildSceneTitle(GameScene scene)
    {
        var text = (scene.Text ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text.Length <= 56 ? text : text[..56].Trim() + "...";
        }

        return string.IsNullOrWhiteSpace(scene.Id) ? "Сцена" : scene.Id;
    }

    private static void RepairEffects(GameProjectData project, IEnumerable<GameEffect> effects, Action<string> log)
    {
        foreach (var effect in effects)
        {
            RepairEffect(project, effect, log);
        }
    }

    private static void RepairEffect(GameProjectData project, GameEffect effect, Action<string> log)
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

        if (string.Equals(effect.Type, "status", StringComparison.OrdinalIgnoreCase))
        {
            effect.Type = "statusEffect";
            log("Repair: effect type status was normalized to statusEffect.");
        }

        if (string.Equals(effect.Type, "item", StringComparison.OrdinalIgnoreCase)
            && project.Currencies.Any(x => string.Equals(x.Id, effect.TargetId, StringComparison.OrdinalIgnoreCase)))
        {
            effect.Type = "currency";
            log("Repair: item effect target matched currency and was normalized: " + effect.TargetId);
        }

        if (string.IsNullOrWhiteSpace(effect.FormulaId)
            && !string.IsNullOrWhiteSpace(effect.FormulaExpression)
            && project.Formulas.Any(x => string.Equals(x.Id, effect.FormulaExpression, StringComparison.OrdinalIgnoreCase)))
        {
            effect.FormulaId = effect.FormulaExpression;
            effect.FormulaExpression = string.Empty;
            log("Repair: effect FormulaExpression id was moved to FormulaId: " + effect.FormulaId);
        }
    }

    private static void RepairFormula(GameProjectData project, GameFormulaDefinition formula, Action<string> log)
    {
        var updated = formula.Expression;
        if (!project.Stats.Any(x => string.Equals(x.Id, "agility", StringComparison.OrdinalIgnoreCase)))
        {
            updated = updated.Replace("stat.agility", "stat.will", StringComparison.OrdinalIgnoreCase);
        }
        if (!project.Stats.Any(x => string.Equals(x.Id, "strength", StringComparison.OrdinalIgnoreCase)))
        {
            updated = updated.Replace("stat.strength", "stat.stamina", StringComparison.OrdinalIgnoreCase);
        }
        updated = NormalizeIntegerSafeFormula(updated);

        if (!string.Equals(updated, formula.Expression, StringComparison.Ordinal))
        {
            formula.Expression = updated;
            log("Repair: generated formula was normalized for runtime evaluator: " + formula.Id);
        }
    }

    private static void RepairRuntimeFormulaReferences(GameProjectData project, Action<string> log)
    {
        if (project.Combat != null)
        {
            RepairFormulaString(project.Combat.DefaultInitiativeFormulaExpression, value => project.Combat.DefaultInitiativeFormulaExpression = value, "combat.defaultInitiativeFormulaExpression", log);
            RepairFormulaString(project.Combat.DefaultHitChanceFormulaExpression, value => project.Combat.DefaultHitChanceFormulaExpression = value, "combat.defaultHitChanceFormulaExpression", log);
            RepairFormulaString(project.Combat.DefaultDodgeChanceFormulaExpression, value => project.Combat.DefaultDodgeChanceFormulaExpression = value, "combat.defaultDodgeChanceFormulaExpression", log);
            RepairFormulaString(project.Combat.DefaultBlockChanceFormulaExpression, value => project.Combat.DefaultBlockChanceFormulaExpression = value, "combat.defaultBlockChanceFormulaExpression", log);
            RepairFormulaString(project.Combat.DefaultCritChanceFormulaExpression, value => project.Combat.DefaultCritChanceFormulaExpression = value, "combat.defaultCritChanceFormulaExpression", log);
        }

        foreach (var action in project.Actions)
        {
            RepairFormulaString(action.HitChanceFormulaExpression, value => action.HitChanceFormulaExpression = value, "action." + action.Id + ".hitChanceFormulaExpression", log);
            RepairFormulaString(action.DodgeChanceFormulaExpression, value => action.DodgeChanceFormulaExpression = value, "action." + action.Id + ".dodgeChanceFormulaExpression", log);
            RepairFormulaString(action.BlockChanceFormulaExpression, value => action.BlockChanceFormulaExpression = value, "action." + action.Id + ".blockChanceFormulaExpression", log);
            RepairFormulaString(action.CritChanceFormulaExpression, value => action.CritChanceFormulaExpression = value, "action." + action.Id + ".critChanceFormulaExpression", log);
            foreach (var effect in action.Effects)
            {
                RepairFormulaString(effect.FormulaExpression, value => effect.FormulaExpression = value, "action." + action.Id + ".effectFormulaExpression", log);
            }
            foreach (var cost in action.Costs)
            {
                RepairFormulaString(cost.FormulaExpression, value => cost.FormulaExpression = value, "action." + action.Id + ".costFormulaExpression", log);
            }
        }
    }

    private static void RepairFormulaString(string expression, Action<string> assign, string label, Action<string> log)
    {
        var updated = NormalizeIntegerSafeFormula(expression);
        if (string.Equals(updated, expression, StringComparison.Ordinal))
        {
            return;
        }

        assign(updated);
        log("Repair: decimal formula reference was normalized for runtime evaluator: " + label);
    }

    private static void RepairEncounterChoices(GameProjectData project, Action<string> log)
    {
        var encounterIds = project.Encounters.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var scene in project.Scenes)
        {
            foreach (var choice in scene.Choices)
            {
                if (!string.IsNullOrWhiteSpace(choice.EncounterId))
                {
                    continue;
                }

                var nextSceneId = choice.NextSceneId?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(nextSceneId) && encounterIds.Contains(nextSceneId))
                {
                    choice.EncounterId = nextSceneId;
                    choice.NextSceneId = string.Empty;
                    log("Repair: choice nextSceneId encounter was moved to EncounterId: " + choice.Id + " -> " + choice.EncounterId);
                    continue;
                }

                if (!IsCombatChoice(choice) || !string.Equals(nextSceneId, "scene_start", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var encounter = SelectCombatEncounterForScene(project, scene);
                if (encounter == null)
                {
                    log("Repair warning: combat-like choice points to scene_start but no combat encounter could be selected: " + choice.Id);
                    continue;
                }

                choice.EncounterId = encounter.Id;
                choice.NextSceneId = string.Empty;
                log("Repair: combat-like choice was linked to encounter: " + choice.Id + " -> " + encounter.Id);
            }
        }
    }

    private static GameEncounterDefinition? SelectCombatEncounterForScene(GameProjectData project, GameScene scene)
    {
        return project.Encounters.FirstOrDefault(x => IsCombatEncounter(x) && string.Equals(x.SceneId, scene.Id, StringComparison.OrdinalIgnoreCase))
            ?? project.Encounters.FirstOrDefault(x => IsCombatEncounter(x) && x.Combatants.Count > 0);
    }

    private static bool IsCombatEncounter(GameEncounterDefinition encounter)
    {
        return string.Equals(encounter.Kind, "combat", StringComparison.OrdinalIgnoreCase)
            || encounter.Combatants.Count > 0;
    }

    private static bool IsCombatChoice(GameChoice choice)
    {
        var text = string.Join(" ", choice.Id, choice.Text).ToLowerInvariant();
        return text.Contains("бой", StringComparison.Ordinal)
            || text.Contains("схват", StringComparison.Ordinal)
            || text.Contains("attack", StringComparison.Ordinal)
            || text.Contains("fight", StringComparison.Ordinal)
            || text.Contains("combat", StringComparison.Ordinal)
            || text.Contains("приготовиться к бою", StringComparison.Ordinal);
    }

    private static string NormalizeIntegerSafeFormula(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return expression;
        }

        var updated = expression;
        updated = Regex.Replace(updated, @"\*\s*0\.5\b", "/ 2", RegexOptions.IgnoreCase);
        updated = Regex.Replace(updated, @"\*\s*0\.1\b", "/ 10", RegexOptions.IgnoreCase);
        updated = Regex.Replace(updated, @"\*\s*1\.5\b", "* 3 / 2", RegexOptions.IgnoreCase);
        updated = Regex.Replace(updated, @"\*\s*1\.2\b", "* 12 / 10", RegexOptions.IgnoreCase);
        updated = Regex.Replace(updated, @"(?<![\w.])0\.7(?![\w.])", "70", RegexOptions.IgnoreCase);
        updated = Regex.Replace(updated, @"(?<![\w.])0\.95(?![\w.])", "95", RegexOptions.IgnoreCase);
        updated = Regex.Replace(updated, @"(?<![\w.])0\.1(?![\w.])", "10", RegexOptions.IgnoreCase);
        updated = Regex.Replace(updated, @"(?<![\w.])0\.05(?![\w.])", "5", RegexOptions.IgnoreCase);
        updated = Regex.Replace(updated, @"(?<![\w.])0\.01(?![\w.])", "1", RegexOptions.IgnoreCase);
        return updated;
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
