using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameDesignPlannerService
{
    private readonly GameDesignInterviewService _interviewService = new();

    public GameCreationPlan BuildPlan(GameProjectData project)
    {
        _interviewService.EnsureProfile(project.DesignProfile);
        var plan = new GameCreationPlan
        {
            Summary = BuildSummary(project),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        Add(plan, "finalize_design_profile", "Зафиксировать дизайн-досье", "Проверить обязательные слоты и явные пользовательские решения.", "design", 10, [], ["design-profile"]);
        Add(plan, "build_brief", "Собрать краткий бриф", "Свести идею, режим создания и дизайн-слоты в рабочий бриф генерации.", "brief", 20, ["finalize_design_profile"], ["brief", "concept"]);
        Add(plan, "build_core_stats_resources", "Собрать характеристики и ресурсы", "Определить базовые параметры, ресурсы и скрытые переменные под выбранный игровой цикл.", "stats-resources", 30, ["build_brief"], ["stats", "variables", "currencies"]);
        Add(plan, "build_world_state", "Собрать состояние мира", "Подготовить время, аспекты мира и правила атмосферы без реализации random director.", "world-state", 40, ["build_brief"], ["world-state"]);
        Add(plan, "build_map_locations", "Собрать карту и локации", "Создать стартовую карту, локации и связи с учётом масштаба и типа карты.", "locations", 50, ["build_world_state"], ["locations", "location-connections"]);

        var dialogueDepth = SlotValue(project, "dialogue_depth");
        Add(plan, "build_characters_relationships", "Собрать персонажей и отношения", "Подготовить персонажей, роли и связи для сцен и квестов.", "characters", IsHigh(dialogueDepth) ? 45 : 60, ["build_brief"], ["characters", "relationships"]);

        var inventoryDepth = SlotValue(project, "inventory_depth");
        var equipmentDepth = SlotValue(project, "equipment_depth");
        Add(plan, "build_items_equipment", "Собрать предметы и экипировку", "Создать предметы, слоты экипировки и награды только в нужной глубине.", "items-equipment", IsNoneOrMinimal(inventoryDepth) && IsNoneOrMinimal(equipmentDepth) ? 95 : 65, ["build_core_stats_resources"], ["items", "equipment"]);

        Add(plan, "build_skills_formulas_actions", "Собрать навыки, формулы и действия", "Создать действия и формулы, которые runtime уже умеет применять.", "skills-actions", 70, ["build_core_stats_resources"], ["skills", "formulas", "actions"]);

        Add(plan, "build_quests_scenes", "Собрать квесты и сцены", "Создать игровые сцены, выборы и квестовую структуру под главную цель.", "scenes", IsHigh(dialogueDepth) ? 55 : 80, ["build_map_locations", "build_characters_relationships"], ["quests", "scenes"]);

        if (HasCombat(project))
        {
            Add(plan, "build_combat_encounters", "Собрать боевые столкновения", "Подготовить data-driven боёвку, encounters, действия и формулы без нового runtime.", "combat", 75, ["build_skills_formulas_actions"], ["combat", "encounters", "actions", "formulas"]);
        }

        if (IsHigh(SlotValue(project, "randomness_level")))
        {
            Add(plan, "build_random_event_foundation", "Запланировать основу случайных событий", "Описать будущую основу вариативности через существующие данные, не реализуя runtime random director.", "world-state", 85, ["build_world_state"], ["world-state", "ambient-events", "encounters"]);
        }

        if (!IsNoneOrMinimal(SlotValue(project, "economy_depth")))
        {
            Add(plan, "build_economy_rewards", "Собрать экономику и награды", "Уточнить валюты, цены, награды и обмен в рамках существующих моделей.", "items", 88, ["build_items_equipment"], ["currencies", "items", "quests"]);
        }

        Add(plan, "validate_project", "Проверить проект", "Запустить локальную валидацию ссылок, ID и поддерживаемых DSL-типов.", "validation", 110, [], ["validation"]);
        Add(plan, "create_playable_mvp", "Собрать playable MVP", "Довести стартовую сцену, карту, выборы и сохранение до первого игрового прохода.", "mvp", 120, ["validate_project"], ["runtime", "save system", "scenes"]);
        plan.Steps = plan.Steps.OrderBy(x => x.Priority).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
        return plan;
    }

    private static void Add(GameCreationPlan plan, string id, string title, string description, string stage, int priority, List<string> dependsOn, List<string> systems)
    {
        plan.Steps.Add(new GameCreationPlanStep
        {
            Id = id,
            Title = title,
            Description = description,
            Stage = stage,
            Priority = priority,
            DependsOn = dependsOn,
            TargetSystems = systems
        });
    }

    private static string BuildSummary(GameProjectData project)
    {
        var idea = string.IsNullOrWhiteSpace(project.DesignProfile.InitialIdea) ? project.Meta.Description : project.DesignProfile.InitialIdea;
        return "План создания MVP: " + idea.Trim();
    }

    private static bool HasCombat(GameProjectData project)
    {
        var value = SlotValue(project, "combat_style");
        return !(value.Contains("нет", StringComparison.OrdinalIgnoreCase)
            || value.Contains("none", StringComparison.OrdinalIgnoreCase)
            || value.Contains("no combat", StringComparison.OrdinalIgnoreCase)
            || value.Contains("без бо", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsHigh(string value)
    {
        return value.Contains("выс", StringComparison.OrdinalIgnoreCase)
            || value.Contains("high", StringComparison.OrdinalIgnoreCase)
            || value.Contains("ключ", StringComparison.OrdinalIgnoreCase)
            || value.Contains("важн", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNoneOrMinimal(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.Contains("нет", StringComparison.OrdinalIgnoreCase)
            || value.Contains("none", StringComparison.OrdinalIgnoreCase)
            || value.Contains("миним", StringComparison.OrdinalIgnoreCase)
            || value.Contains("minimal", StringComparison.OrdinalIgnoreCase);
    }

    private static string SlotValue(GameProjectData project, string slotId)
    {
        return project.DesignProfile.Slots.FirstOrDefault(x => string.Equals(x.Id, slotId, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
    }
}
