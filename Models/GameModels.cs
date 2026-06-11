using System.Text.Json.Serialization;

namespace LMStudioSillyTavernWorldBuilder.Models;

public sealed class GameProjectIndex
{
    public string GamesRootPath { get; set; } = string.Empty;
    public List<GameProjectSummary> Projects { get; set; } = new();
}

public sealed class GameProjectSummary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = "New Game";
    public string FolderName { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameProjectData
{
    public GameProjectSummary Summary { get; set; } = new();
    public GameMeta Meta { get; set; } = new();
    public GameWorld World { get; set; } = new();
    public List<GameStatDefinition> Stats { get; set; } = new();
    public List<GameSkillDefinition> Skills { get; set; } = new();
    public List<GameItemDefinition> Items { get; set; } = new();
    public List<GameEquipmentSlotDefinition> EquipmentSlots { get; set; } = new();
    public List<GameElementDefinition> Elements { get; set; } = new();
    public List<GameVariableDefinition> Variables { get; set; } = new();
    public List<GameCurrencyDefinition> Currencies { get; set; } = new();
    public List<GameCharacter> Characters { get; set; } = new();
    public List<GameRelationshipDefinition> Relationships { get; set; } = new();
    public List<GameLocation> Locations { get; set; } = new();
    public List<GameLocationConnection> LocationConnections { get; set; } = new();
    public List<GameLocationStateDefinition> LocationStates { get; set; } = new();
    public List<GameScene> Scenes { get; set; } = new();
    public List<GameQuest> Quests { get; set; } = new();
    public List<GameEncounterDefinition> Encounters { get; set; } = new();
    public List<GameActionDefinition> Actions { get; set; } = new();
    public List<GameFormulaDefinition> Formulas { get; set; } = new();
    public List<GameStatusEffectDefinition> StatusEffects { get; set; } = new();
    public List<GameProgressionNodeDefinition> ProgressionNodes { get; set; } = new();
    public GameWorldStateDefinition WorldState { get; set; } = new();
    public GameMechanicsDefinition Mechanics { get; set; } = new();
    public GameCombatDefinition? Combat { get; set; }
    public List<ImagePromptDefinition> ImagePrompts { get; set; } = new();
    public List<ImageGeneratedCandidate> GeneratedImageCandidates { get; set; } = new();
    public List<ImageAssetLink> AssetLinks { get; set; } = new();
    public List<GenerationSessionSummary> GenerationSessions { get; set; } = new();
    public ProjectBrief Brief { get; set; } = new();
    public GameConcept Concept { get; set; } = new();
    public GameMvpPlan MvpPlan { get; set; } = new();
    public GameArchitecturePlan ArchitecturePlan { get; set; } = new();
    public ContentGenerationPlan ContentPlan { get; set; } = new();
    public PromptGenerationPlan PromptPlan { get; set; } = new();
    public GameGenerationPreferences GenerationPreferences { get; set; } = new();
    public GameDesignProfile DesignProfile { get; set; } = new();
    public GameCreationPlan CreationPlan { get; set; } = new();
    public GameDesignKnowledgeBase DesignKnowledgeBase { get; set; } = new();
    public GameDesignConversationHistory DesignConversationHistory { get; set; } = new();
}

public sealed class GameGenerationPreferences
{
    public string GeneralGameplayText { get; set; } = string.Empty;
    public string SkillDesignText { get; set; } = string.Empty;
    public string ProgressionDesignText { get; set; } = string.Empty;
    public string CombatDesignText { get; set; } = string.Empty;
    public string AtmosphereDesignText { get; set; } = string.Empty;
    public string BalanceText { get; set; } = string.Empty;
    public string ForbiddenDesignText { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class GameMeta
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = "New Game";
    public string Genre { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StartSceneId { get; set; } = "scene_start";
    public string VisualStyle { get; set; } = string.Empty;
    public string Language { get; set; } = "ru";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GameWorld
{
    public string Summary { get; set; } = string.Empty;
    public List<string> Lore { get; set; } = new();
    public List<string> Factions { get; set; } = new();
    public List<string> Rules { get; set; } = new();
}

public sealed class GameCharacter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string? PortraitAssetId { get; set; }
    public Dictionary<string, int> Stats { get; set; } = new();
}

public sealed class GameStatDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MinValue { get; set; }
    public int MaxValue { get; set; } = 100;
    public int InitialValue { get; set; } = 10;
    public bool IsResource { get; set; }
    public string Kind { get; set; } = "attribute";
    public bool ShowAsBar { get; set; }
    public string ColorHint { get; set; } = string.Empty;
    public int? RegenPerTurn { get; set; }
    public List<string> Tags { get; set; } = new();
}

public sealed class GameSkillDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int InitialLevel { get; set; }
    public string Kind { get; set; } = "passive";
    public string ElementId { get; set; } = string.Empty;
    public int MaxLevel { get; set; } = 1;
    public int ExperienceToNextLevel { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<GameRequirement> LearnRequirements { get; set; } = new();
    public List<GameRequirement> UseRequirements { get; set; } = new();
    public List<GameCost> Costs { get; set; } = new();
    public List<GameEffect> Effects { get; set; } = new();
    public List<GameModifier> PassiveModifiers { get; set; } = new();
    public int CooldownTurns { get; set; }
    public bool IsKnownByDefault { get; set; }
}

public sealed class GameItemDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "item";
    public bool IsStackable { get; set; } = true;
    public string? ImageAssetId { get; set; }
    public string Rarity { get; set; } = string.Empty;
    public string SlotId { get; set; } = string.Empty;
    public int MaxStack { get; set; } = 1;
    public bool IsEquippable { get; set; }
    public bool IsConsumable { get; set; }
    public bool IsUsable { get; set; }
    public int DurabilityMax { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<GameRequirement> Requirements { get; set; } = new();
    public List<GameModifier> Modifiers { get; set; } = new();
    public List<GameEffect> UseEffects { get; set; } = new();
    public List<GameEffect> EquipEffects { get; set; } = new();
    public List<GameEffect> UnequipEffects { get; set; } = new();
    public int Value { get; set; }
    public string CurrencyId { get; set; } = string.Empty;
}

public sealed class GameEquipmentSlotDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<string> AllowedItemTags { get; set; } = new();
}

public sealed class GameElementDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> StrongAgainst { get; set; } = new();
    public List<string> WeakAgainst { get; set; } = new();
    public string ColorHint { get; set; } = string.Empty;
    public string VisualPromptHint { get; set; } = string.Empty;
}

public sealed class GameVariableDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int InitialValue { get; set; }
    public bool IsHidden { get; set; }
}

public sealed class GameCurrencyDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int InitialAmount { get; set; }
    public bool IsHidden { get; set; }
    public string IconAssetId { get; set; } = string.Empty;
}

public sealed class GameInventoryEntry
{
    public string InstanceId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public int Durability { get; set; }
    public bool IsEquipped { get; set; }
    public string SlotId { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public sealed class GameKnownSkill
{
    public string SkillId { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int CooldownRemaining { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class GameRelationshipDefinition
{
    public string CharacterId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int InitialValue { get; set; }
    public int MinValue { get; set; } = -100;
    public int MaxValue { get; set; } = 100;
}

public sealed class GameLocation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageAssetId { get; set; }
    public string RegionId { get; set; } = string.Empty;
    public string StatusId { get; set; } = string.Empty;
    public bool IsDiscovered { get; set; }
    public string MapX { get; set; } = string.Empty;
    public string MapY { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<GameRequirement> AccessRequirements { get; set; } = new();
    public List<GameEffect> EnterEffects { get; set; } = new();
}

public sealed class GameLocationConnection
{
    public string Id { get; set; } = string.Empty;
    public string FromLocationId { get; set; } = string.Empty;
    public string ToLocationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsTwoWay { get; set; } = true;
    public List<GameRequirement> Requirements { get; set; } = new();
    public List<GameEffect> TravelEffects { get; set; } = new();
}

public sealed class GameLocationStateDefinition
{
    public string Id { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

public sealed class GameScene
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? ImageAssetId { get; set; }
    public bool StartsCombat { get; set; }
    public List<GameChoice> Choices { get; set; } = new();
}

public sealed class GameChoice
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? NextSceneId { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    public List<GameCondition> Conditions { get; set; } = new();
    public List<GameEffect> Effects { get; set; } = new();
}

public sealed class GameCondition
{
    public string Type { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Operator { get; set; } = ">=";
    public int Value { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed class GameEffect
{
    public string Type { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string FormulaId { get; set; } = string.Empty;
    public string FormulaExpression { get; set; } = string.Empty;
    public int ChancePercent { get; set; } = 100;
    public string StatusEffectId { get; set; } = string.Empty;
    public int DurationTurns { get; set; }
    public string? Text { get; set; }
    public string StringValue { get; set; } = string.Empty;
    public string Mode { get; set; } = "add";
    public string SourceId { get; set; } = string.Empty;
    public string TargetScope { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public sealed class GameRequirement
{
    public string Type { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Operator { get; set; } = ">=";
    public int Value { get; set; }
    public string FormulaId { get; set; } = string.Empty;
    public string FormulaExpression { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string StringValue { get; set; } = string.Empty;
}

public sealed class GameModifier
{
    public string Type { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Mode { get; set; } = "add";
    public string SourceId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class GameCost
{
    public string Type { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string FormulaId { get; set; } = string.Empty;
    public string FormulaExpression { get; set; } = string.Empty;
}

public sealed class GameQuest
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActiveByDefault { get; set; }
}

public sealed class GameCombatDefinition
{
    public bool Enabled { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string PlayerHealthStatId { get; set; } = "health";
    public string DefaultInitiativeFormulaId { get; set; } = string.Empty;
    public string DefaultInitiativeFormulaExpression { get; set; } = string.Empty;
    public string DefaultHitChanceFormulaId { get; set; } = string.Empty;
    public string DefaultHitChanceFormulaExpression { get; set; } = string.Empty;
    public string DefaultDodgeChanceFormulaId { get; set; } = string.Empty;
    public string DefaultDodgeChanceFormulaExpression { get; set; } = string.Empty;
    public string DefaultBlockChanceFormulaId { get; set; } = string.Empty;
    public string DefaultBlockChanceFormulaExpression { get; set; } = string.Empty;
    public string DefaultCritChanceFormulaId { get; set; } = string.Empty;
    public string DefaultCritChanceFormulaExpression { get; set; } = string.Empty;
    public int DefaultCritMultiplierPercent { get; set; } = 150;
    public int DefaultBlockDamagePercent { get; set; } = 50;
    public int MaxRounds { get; set; } = 200;
    public string Notes { get; set; } = string.Empty;
}

public sealed class GameEncounterDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "exploration";
    public string Description { get; set; } = string.Empty;
    public string SceneId { get; set; } = string.Empty;
    public List<GameRequirement> Requirements { get; set; } = new();
    public List<GameChoice> Choices { get; set; } = new();
    public List<GameEffect> OnStartEffects { get; set; } = new();
    public List<GameEffect> OnWinEffects { get; set; } = new();
    public List<GameEffect> OnLoseEffects { get; set; } = new();
    public string VictorySceneId { get; set; } = string.Empty;
    public string DefeatSceneId { get; set; } = string.Empty;
    public string CombatStartText { get; set; } = string.Empty;
    public List<GameEncounterCombatantDefinition> Combatants { get; set; } = new();
}

public sealed class GameEncounterCombatantDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Team { get; set; } = "enemy";
    public bool IsPlayer { get; set; }
    public Dictionary<string, int> Stats { get; set; } = new();
    public List<string> ActionIds { get; set; } = new();
    public List<GameEffect> OnDefeatEffects { get; set; } = new();
    public string InitiativeFormulaId { get; set; } = string.Empty;
    public string InitiativeFormulaExpression { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

public sealed class GameActionDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "custom";
    public string Description { get; set; } = string.Empty;
    public List<GameRequirement> Requirements { get; set; } = new();
    public List<GameCost> Costs { get; set; } = new();
    public List<GameEffect> Effects { get; set; } = new();
    public int CooldownTurns { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool AvailableInCombat { get; set; }
    public string ActorTeam { get; set; } = string.Empty;
    public string TargetScope { get; set; } = "enemy";
    public string HitChanceFormulaId { get; set; } = string.Empty;
    public string HitChanceFormulaExpression { get; set; } = string.Empty;
    public string DodgeChanceFormulaId { get; set; } = string.Empty;
    public string DodgeChanceFormulaExpression { get; set; } = string.Empty;
    public string BlockChanceFormulaId { get; set; } = string.Empty;
    public string BlockChanceFormulaExpression { get; set; } = string.Empty;
    public string CritChanceFormulaId { get; set; } = string.Empty;
    public string CritChanceFormulaExpression { get; set; } = string.Empty;
    public int CritMultiplierPercent { get; set; }
    public int BlockDamagePercent { get; set; }
}

public sealed class GameMechanicsDefinition
{
    public bool EnableTurns { get; set; }
    public bool EnableStatusEffects { get; set; }
    public bool EnableProgression { get; set; }
    public bool EnableActionPanel { get; set; }
    public bool EnableDiceRandomness { get; set; }
    public int DefaultActionPoints { get; set; } = 1;
    public string ActionPointStatId { get; set; } = string.Empty;
    public string InitiativeFormulaId { get; set; } = string.Empty;
    public GameExperienceDefinition Experience { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public sealed class GameExperienceDefinition
{
    public bool EnablePlayerExperience { get; set; }
    public bool EnableSkillExperience { get; set; }
    public int InitialPlayerLevel { get; set; } = 1;
    public int InitialPlayerExperience { get; set; }
    public int MaxPlayerLevel { get; set; } = 100;
    public string PlayerExperienceToNextLevelFormulaId { get; set; } = string.Empty;
    public string PlayerExperienceToNextLevelFormulaExpression { get; set; } = string.Empty;
    public string SkillExperienceToNextLevelFormulaId { get; set; } = string.Empty;
    public string SkillExperienceToNextLevelFormulaExpression { get; set; } = string.Empty;
    public string DefaultPlayerExperienceRewardFormulaId { get; set; } = string.Empty;
    public string DefaultPlayerExperienceRewardFormulaExpression { get; set; } = string.Empty;
    public List<GameEffect> PlayerLevelUpEffects { get; set; } = new();
    public List<GameEffect> SkillLevelUpEffects { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public sealed class GameWorldStateDefinition
{
    public bool Enabled { get; set; }
    public string GenreProfile { get; set; } = "generic";
    public GameTimeSystemDefinition Time { get; set; } = new();
    public List<GameWorldAspectDefinition> Aspects { get; set; } = new();
    public List<GameAmbientEventDefinition> AmbientEvents { get; set; } = new();
    public List<GameWorldRuleDefinition> Rules { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public sealed class GameTimeSystemDefinition
{
    public bool Enabled { get; set; }
    public int StartDayNumber { get; set; } = 1;
    public string DayLabel { get; set; } = "День";
    public string SegmentLabel { get; set; } = "Время";
    public string StartSegmentId { get; set; } = string.Empty;
    public int AdvanceSegmentsOnEndTurn { get; set; } = 1;
    public int AdvanceSegmentsOnTravel { get; set; } = 1;
    public int AdvanceSegmentsOnAction { get; set; }
    public List<GameTimeSegmentDefinition> Segments { get; set; } = new();
}

public sealed class GameTimeSegmentDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public string NextSegmentId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<GameModifier> Modifiers { get; set; } = new();
    public List<GameEffect> OnEnterEffects { get; set; } = new();
}

public sealed class GameWorldAspectDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "generic";
    public string Description { get; set; } = string.Empty;
    public string DefaultStateId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<GameWorldAspectStateDefinition> States { get; set; } = new();
}

public sealed class GameWorldAspectStateDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "generic";
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<GameModifier> Modifiers { get; set; } = new();
    public List<GameEffect> OnEnterEffects { get; set; } = new();
}

public sealed class GameAmbientEventDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "ambient";
    public string Trigger { get; set; } = "turnEnd";
    public string Description { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int Weight { get; set; } = 1;
    public int ChancePercent { get; set; } = 100;
    public int CooldownTurns { get; set; }
    public List<string> LocationIds { get; set; } = new();
    public List<string> LocationTags { get; set; } = new();
    public List<string> TimeSegmentIds { get; set; } = new();
    public List<GameRequirement> Requirements { get; set; } = new();
    public List<GameEffect> Effects { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public sealed class GameWorldRuleDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Trigger { get; set; } = "turnEnd";
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int ChancePercent { get; set; } = 100;
    public int CooldownTurns { get; set; }
    public List<GameRequirement> Requirements { get; set; } = new();
    public List<GameEffect> Effects { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public sealed class GameRuntimeWorldState
{
    public int DayNumber { get; set; } = 1;
    public string TimeSegmentId { get; set; } = string.Empty;
    public Dictionary<string, string> AspectStates { get; set; } = new();
    public Dictionary<string, int> RuleCooldowns { get; set; } = new();
    public Dictionary<string, int> AmbientEventCooldowns { get; set; } = new();
    public List<string> RecentAmbientEventIds { get; set; } = new();
}

public sealed class GameRuntimeCombatState
{
    public bool IsActive { get; set; }
    public string EncounterId { get; set; } = string.Empty;
    public int RoundNumber { get; set; } = 1;
    public int CurrentTurnIndex { get; set; }
    public List<GameRuntimeCombatant> Combatants { get; set; } = new();
    public string VictorySceneId { get; set; } = string.Empty;
    public string DefeatSceneId { get; set; } = string.Empty;
    public bool VictoryHandled { get; set; }
    public bool DefeatHandled { get; set; }
}

public sealed class GameRuntimeCombatant
{
    public string RuntimeId { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Team { get; set; } = "enemy";
    public bool IsPlayer { get; set; }
    public Dictionary<string, int> Stats { get; set; } = new();
    public List<string> ActionIds { get; set; } = new();
    public List<GameActiveStatusEffect> ActiveStatusEffects { get; set; } = new();
    public Dictionary<string, int> ActionCooldowns { get; set; } = new();
    public int Initiative { get; set; }
    public bool HasActedThisRound { get; set; }
}

public sealed class GameFormulaDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int? MinResult { get; set; }
    public int? MaxResult { get; set; }
    public List<string> Tags { get; set; } = new();
}

public sealed class GameStatusEffectDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Kind { get; set; } = "neutral";
    public int DefaultDurationTurns { get; set; } = 1;
    public int MaxStacks { get; set; } = 1;
    public string StackMode { get; set; } = "refresh";
    public bool IsHidden { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<GameEffect> OnApplyEffects { get; set; } = new();
    public List<GameEffect> PeriodicEffects { get; set; } = new();
    public List<GameEffect> OnExpireEffects { get; set; } = new();
    public List<GameModifier> Modifiers { get; set; } = new();
    public List<GameRequirement> RemoveRequirements { get; set; } = new();
}

public sealed class GameActiveStatusEffect
{
    public string InstanceId { get; set; } = string.Empty;
    public string StatusEffectId { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public int RemainingTurns { get; set; }
    public int Stacks { get; set; } = 1;
}

public sealed class GameProgressionNodeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Kind { get; set; } = "passive";
    public string SkillId { get; set; } = string.Empty;
    public bool IsUnlockedByDefault { get; set; }
    public List<string> ParentNodeIds { get; set; } = new();
    public List<GameRequirement> UnlockRequirements { get; set; } = new();
    public List<GameCost> UnlockCosts { get; set; } = new();
    public List<GameEffect> UnlockEffects { get; set; } = new();
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public List<string> Tags { get; set; } = new();
}

public sealed class SaveGame
{
    public string Id { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = "Save";
    public string CurrentSceneId { get; set; } = string.Empty;
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<string, int> PlayerStats { get; set; } = new();
    public Dictionary<string, int> Inventory { get; set; } = new();
    public List<GameInventoryEntry> InventoryEntries { get; set; } = new();
    public Dictionary<string, string> EquippedItems { get; set; } = new();
    public Dictionary<string, int> Currencies { get; set; } = new();
    public Dictionary<string, int> Relationships { get; set; } = new();
    public List<string> ActiveQuestIds { get; set; } = new();
    public List<string> CompletedQuestIds { get; set; } = new();
    public List<GameKnownSkill> KnownSkills { get; set; } = new();
    public string CurrentLocationId { get; set; } = string.Empty;
    public Dictionary<string, string> LocationStates { get; set; } = new();
    public List<string> DiscoveredLocationIds { get; set; } = new();
    public Dictionary<string, int> Variables { get; set; } = new();
    public List<string> Flags { get; set; } = new();
    public List<string> EventLog { get; set; } = new();
    public GameRuntimeWorldState WorldState { get; set; } = new();
    public GameRuntimeCombatState Combat { get; set; } = new();
    public int PlayerLevel { get; set; } = 1;
    public int PlayerExperience { get; set; }
    public int TurnNumber { get; set; }
    public List<GameActiveStatusEffect> ActiveStatusEffects { get; set; } = new();
    public List<string> UnlockedProgressionNodeIds { get; set; } = new();
    public Dictionary<string, int> ActionCooldowns { get; set; } = new();
}

public class GameRuntimeOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> LogLines { get; set; } = new();
    public List<string> AppliedEffectSummaries { get; set; } = new();
}

public sealed class GameActionExecutionResult : GameRuntimeOperationResult
{
}

public sealed class GameCombatActionResult : GameRuntimeOperationResult
{
    public bool CombatEnded { get; set; }
    public bool PlayerWon { get; set; }
    public bool PlayerLost { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
}

public sealed class GameActionAvailabilityResult
{
    public bool IsAvailable { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CostSummary { get; set; } = string.Empty;
    public string RequirementSummary { get; set; } = string.Empty;
}

public sealed class GameFormulaEvaluationResult
{
    public bool Success { get; set; }
    public int Value { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class GameTurnResult
{
    public int NewTurnNumber { get; set; }
    public List<string> LogLines { get; set; } = new();
    public List<string> CooldownChanges { get; set; } = new();
    public List<string> ExpiredStatusEffects { get; set; } = new();
    public List<string> PeriodicEffectMessages { get; set; } = new();
}

public sealed class ImagePromptDefinition
{
    public string AssetId { get; set; } = string.Empty;
    public ImageTargetType TargetType { get; set; }
    public string TargetEntityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PositivePrompt { get; set; } = string.Empty;
    public string NegativePrompt { get; set; } = string.Empty;
    public List<string> StyleTags { get; set; } = new();
    public int Count { get; set; } = 2;
    public int PreferredWidth { get; set; } = 1024;
    public int PreferredHeight { get; set; } = 768;
    public string OutputFolder { get; set; } = string.Empty;
    public string? SelectedImagePath { get; set; }
    public ImagePromptStatus Status { get; set; } = ImagePromptStatus.Draft;
    public string Notes { get; set; } = string.Empty;
}

public sealed class ImageAssetLink
{
    public string AssetId { get; set; } = string.Empty;
    public ImageTargetType TargetType { get; set; }
    public string TargetEntityId { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
}

public sealed class ImageGeneratedCandidate
{
    public string CandidateId { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string ProjectRelativePath { get; set; } = string.Empty;
    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public string SuggestedAssetId { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class GenerationSessionSummary
{
    public string Id { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class IdeaDiscussionSession
{
    public List<ChatMessage> Messages { get; set; } = new();
}

public sealed class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class ProjectBrief
{
    public string Text { get; set; } = string.Empty;
    public bool Approved { get; set; }
}

public sealed class GameConcept
{
    public string Text { get; set; } = string.Empty;
    public bool Approved { get; set; }
}

public sealed class GameMvpPlan
{
    public string Text { get; set; } = string.Empty;
    public bool Approved { get; set; }
}

public sealed class GameArchitecturePlan
{
    public string Text { get; set; } = string.Empty;
    public bool Approved { get; set; }
}

public sealed class ContentGenerationPlan
{
    public string Text { get; set; } = string.Empty;
    public bool Approved { get; set; }
}

public sealed class PromptGenerationPlan
{
    public string Text { get; set; } = string.Empty;
    public bool Approved { get; set; }
}

public sealed class ApprovalCheckpoint
{
    public string Stage { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string Notes { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageTargetType
{
    Scene,
    Character,
    Item,
    Location,
    Skill,
    Spell,
    Equipment,
    Encounter,
    Cover,
    Ui
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArtGenerationScope
{
    MinimalBackgrounds,
    MainScenesOnly,
    CharactersAndScenes,
    Everything
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImagePromptStatus
{
    Draft,
    Approved,
    Queued,
    Generating,
    Generated,
    NeedsReview,
    Accepted,
    Rejected,
    RegenerateRequested,
    Linked
}

public enum AppWorkflowStatus
{
    Idle,
    Discussing,
    BuildingBrief,
    BuildingConcept,
    BuildingMvp,
    BuildingStructure,
    GeneratingContent,
    PreparingPrompts,
    SwitchingToFooocus,
    GeneratingAssets,
    ImportingAssets,
    Playing,
    Saving,
    Loading,
    Error
}
