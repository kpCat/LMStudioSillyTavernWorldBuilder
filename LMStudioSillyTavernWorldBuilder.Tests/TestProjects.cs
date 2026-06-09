using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Tests;

internal static class TestProjects
{
    public static GameProjectData CreatePlayableProject()
    {
        return new GameProjectData
        {
            Summary = new GameProjectSummary { Id = "game_test", Title = "Test", FolderName = "Test" },
            Meta = new GameMeta { Id = "game_test", Title = "Test", StartSceneId = "scene_start" },
            Stats = { new GameStatDefinition { Id = "will", Name = "Will", InitialValue = 10 } },
            Items = { new GameItemDefinition { Id = "key", Name = "Key" } },
            Characters = { new GameCharacter { Id = "npc", Name = "NPC" } },
            Relationships = { new GameRelationshipDefinition { CharacterId = "npc", Name = "NPC", InitialValue = 0 } },
            Quests = { new GameQuest { Id = "quest_main", Title = "Main" } },
            Scenes =
            {
                new GameScene
                {
                    Id = "scene_start",
                    Title = "Start",
                    Text = "Start text",
                    Choices =
                    {
                        new GameChoice
                        {
                            Id = "choice_go",
                            Text = "Go",
                            NextSceneId = "scene_next",
                            Conditions = { new GameCondition { Type = "stat", TargetId = "will", Operator = ">=", Value = 5 } },
                            Effects =
                            {
                                new GameEffect { Type = "stat", TargetId = "will", Amount = 1 },
                                new GameEffect { Type = "item", TargetId = "key", Amount = 1 },
                                new GameEffect { Type = "relationship", TargetId = "npc", Amount = 5 },
                                new GameEffect { Type = "quest", TargetId = "quest_main", Amount = 1 }
                            }
                        }
                    }
                },
                new GameScene { Id = "scene_next", Title = "Next", Text = "Next text" }
            }
        };
    }

    public static SaveGame CreateSave(GameProjectData project)
    {
        return new SaveGame
        {
            ProjectId = project.Meta.Id,
            CurrentSceneId = project.Meta.StartSceneId,
            PlayerStats = project.Stats.ToDictionary(x => x.Id, x => x.InitialValue),
            Inventory = project.Items.ToDictionary(x => x.Id, _ => 0),
            Relationships = project.Relationships.ToDictionary(x => x.CharacterId, x => x.InitialValue)
        };
    }

    public static ImagePromptDefinition CreateScenePrompt()
    {
        return new ImagePromptDefinition
        {
            AssetId = "scene_art",
            TargetType = ImageTargetType.Scene,
            TargetEntityId = "scene_start",
            Title = "Start",
            PositivePrompt = "scene",
            NegativePrompt = "bad",
            Status = ImagePromptStatus.Queued
        };
    }

    public static GameProjectData CreateAdvancedProject()
    {
        var project = CreatePlayableProject();
        project.Stats.Add(new GameStatDefinition { Id = "mana", Name = "Mana", InitialValue = 10, MaxValue = 20, Kind = "resource", IsResource = true });
        project.Currencies.Add(new GameCurrencyDefinition { Id = "gold", Name = "Gold", InitialAmount = 5 });
        project.Variables.Add(new GameVariableDefinition { Id = "alarm", Name = "Alarm", InitialValue = 0 });
        project.EquipmentSlots.Add(new GameEquipmentSlotDefinition { Id = "hand", Name = "Hand", AllowedItemTags = { "weapon" } });
        project.Elements.Add(new GameElementDefinition { Id = "fire", Name = "Fire" });
        project.Items.Add(new GameItemDefinition
        {
            Id = "potion",
            Name = "Potion",
            IsStackable = true,
            IsUsable = true,
            IsConsumable = true,
            UseEffects = { new GameEffect { Type = "stat", TargetId = "will", Amount = 3 } }
        });
        project.Items.Add(new GameItemDefinition
        {
            Id = "sword",
            Name = "Sword",
            IsStackable = false,
            IsEquippable = true,
            SlotId = "hand",
            Tags = { "weapon" },
            Modifiers = { new GameModifier { Type = "stat", TargetId = "will", Amount = 2 } }
        });
        project.Skills.Add(new GameSkillDefinition
        {
            Id = "focus",
            Name = "Focus",
            Kind = "passive",
            IsKnownByDefault = true,
            PassiveModifiers = { new GameModifier { Type = "stat", TargetId = "will", Amount = 4 } }
        });
        project.Skills.Add(new GameSkillDefinition
        {
            Id = "firebolt",
            Name = "Firebolt",
            Kind = "spell",
            ElementId = "fire",
            Costs = { new GameCost { Type = "stat", TargetId = "mana", Amount = 3 } },
            Effects = { new GameEffect { Type = "variable", TargetId = "alarm", Amount = 1 } },
            CooldownTurns = 2
        });
        project.Locations.Add(new GameLocation { Id = "locked_room", Name = "Locked Room" });
        project.LocationConnections.Add(new GameLocationConnection
        {
            Id = "start_locked",
            FromLocationId = "location_start",
            ToLocationId = "locked_room",
            Requirements = { new GameRequirement { Type = "flag", TargetId = "door_open", Operator = "==" } }
        });
        project.LocationStates.Add(new GameLocationStateDefinition { Id = "burning", LocationId = "location_start", Name = "Burning" });
        return project;
    }

    public static GameProjectData CreateWorldStateProject()
    {
        var project = CreateAdvancedProject();
        project.WorldState = new GameWorldStateDefinition
        {
            Enabled = true,
            GenreProfile = "fantasy",
            Time = new GameTimeSystemDefinition
            {
                Enabled = true,
                StartDayNumber = 2,
                StartSegmentId = "morning",
                AdvanceSegmentsOnEndTurn = 1,
                AdvanceSegmentsOnTravel = 1,
                AdvanceSegmentsOnAction = 1,
                Segments =
                {
                    new GameTimeSegmentDefinition { Id = "morning", Name = "Утро", Order = 1, Modifiers = { new GameModifier { Type = "stat", TargetId = "will", Amount = 1 } } },
                    new GameTimeSegmentDefinition { Id = "night", Name = "Ночь", Order = 2 }
                }
            },
            Aspects =
            {
                new GameWorldAspectDefinition
                {
                    Id = "weather",
                    Name = "Погода",
                    Kind = "weather",
                    DefaultStateId = "clear",
                    States =
                    {
                        new GameWorldAspectStateDefinition { Id = "clear", Name = "Ясно" },
                        new GameWorldAspectStateDefinition { Id = "rain", Name = "Дождь", Modifiers = { new GameModifier { Type = "stat", TargetId = "will", Amount = -1 } } }
                    }
                }
            }
        };
        return project;
    }
}
