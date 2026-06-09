using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameProjectCloneService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    internal GameProjectData Clone(GameProjectData project)
    {
        var json = JsonSerializer.Serialize(project, _jsonOptions);
        return JsonSerializer.Deserialize<GameProjectData>(json, _jsonOptions)
            ?? throw new InvalidOperationException("GameProjectData clone JSON was empty.");
    }

    internal void CopyMutableData(GameProjectData source, GameProjectData target)
    {
        var summaryId = target.Summary.Id;
        var summaryProjectPath = target.Summary.ProjectPath;
        var summaryFolderName = target.Summary.FolderName;
        var metaId = target.Meta.Id;

        var copy = Clone(source);
        target.Summary = copy.Summary;
        target.Meta = copy.Meta;
        target.World = copy.World;
        target.Stats = copy.Stats;
        target.Skills = copy.Skills;
        target.Items = copy.Items;
        target.EquipmentSlots = copy.EquipmentSlots;
        target.Elements = copy.Elements;
        target.Variables = copy.Variables;
        target.Currencies = copy.Currencies;
        target.Characters = copy.Characters;
        target.Relationships = copy.Relationships;
        target.Locations = copy.Locations;
        target.LocationConnections = copy.LocationConnections;
        target.LocationStates = copy.LocationStates;
        target.Scenes = copy.Scenes;
        target.Quests = copy.Quests;
        target.Encounters = copy.Encounters;
        target.Actions = copy.Actions;
        target.Formulas = copy.Formulas;
        target.StatusEffects = copy.StatusEffects;
        target.ProgressionNodes = copy.ProgressionNodes;
        target.WorldState = copy.WorldState;
        target.Mechanics = copy.Mechanics;
        target.Combat = copy.Combat;
        target.ImagePrompts = copy.ImagePrompts;
        target.GeneratedImageCandidates = copy.GeneratedImageCandidates;
        target.AssetLinks = copy.AssetLinks;
        target.Brief = copy.Brief;
        target.Concept = copy.Concept;
        target.MvpPlan = copy.MvpPlan;
        target.ArchitecturePlan = copy.ArchitecturePlan;
        target.ContentPlan = copy.ContentPlan;
        target.PromptPlan = copy.PromptPlan;
        target.GenerationPreferences = copy.GenerationPreferences;

        target.Summary.Id = summaryId;
        target.Summary.ProjectPath = summaryProjectPath;
        target.Summary.FolderName = summaryFolderName;
        target.Meta.Id = metaId;
    }
}
