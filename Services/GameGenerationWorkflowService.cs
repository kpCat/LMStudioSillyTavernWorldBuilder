using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameGenerationWorkflowService
{
    private readonly GameProjectValidator _validator = new();

    public List<GameGenerationStepView> BuildSteps(GameProjectData project)
    {
        var steps = new List<GameGenerationStepView>
        {
            TextStep("idea_brief", 1, "Идея и бриф", project.Brief.Text, "Сформируйте бриф на вкладке AI-обсуждение."),
            TextStep("concept", 2, "Концепт мира", project.Concept.Text, "Сформируйте концепт на вкладке AI-обсуждение."),
            TextStep("mvp", 3, "MVP и рамки игры", project.MvpPlan.Text, "Сформируйте MVP-план на вкладке AI-обсуждение."),
            TextStep("structure", 4, "Структура данных", project.ArchitecturePlan.Text, "Сформируйте структуру данных на вкладке AI-обсуждение."),
            InitialContentStep(project),
            CountStep("stats_resources", 6, "Показатели, ресурсы, валюты, переменные", "stats-resources", project.Stats.Count + project.Currencies.Count + project.Variables.Count, "Сгенерируйте маленькую пачку показателей, ресурсов, валют или переменных."),
            CountStep("formulas", 7, "Формулы механик", "formulas", project.Formulas.Count, "Сгенерируйте маленькую пачку безопасных формул."),
            CountStep("status_effects", 8, "Статусы, баффы и дебаффы", "status-effects", project.StatusEffects.Count, "Сгенерируйте маленькую пачку статусов и эффектов."),
            CountStep("progression", 9, "Дерево прокачки", "progression", project.ProgressionNodes.Count, "Сгенерируйте маленькую пачку узлов прокачки."),
            CountStep("gameplay_actions", 10, "Игровые действия", "gameplay-actions", project.Actions.Count, "Сгенерируйте маленькую пачку действий для runtime."),
            CombatStep(project),
            WorldStateStep(project),
            EquipmentStep(project),
            ItemsStep(project),
            SkillsStep(project),
            SpellsStep(project),
            CountStep("locations_map", 15, "Локации, карта, связи, состояния", "locations", project.Locations.Count + project.LocationConnections.Count + project.LocationStates.Count, "Сгенерируйте пачку локаций, связей и состояний."),
            ScenesStep(project),
            CountStep("encounters_actions", 17, "События, проверки, actions", "encounters", project.Encounters.Count + project.Actions.Count, "Сгенерируйте пачку событий, проверок и действий."),
            ImagePromptsStep(project),
            ValidationStep(project)
        };

        return steps.OrderBy(x => x.Order).ToList();
    }

    private static GameGenerationStepView TextStep(string id, int order, string title, string text, string nextAction)
    {
        var done = !string.IsNullOrWhiteSpace(text);
        return new GameGenerationStepView
        {
            Id = id,
            Order = order,
            Title = title,
            Status = done ? "Готово" : "Не начато",
            CurrentState = done ? "Текст подготовлен" : "Текст отсутствует",
            NextAction = done ? "Переходите к следующему этапу." : nextAction,
            CanRunFromPipeline = false
        };
    }

    private static GameGenerationStepView InitialContentStep(GameProjectData project)
    {
        var hasScenes = project.Scenes.Count > 0;
        var hasStart = !string.IsNullOrWhiteSpace(project.Meta.StartSceneId)
            && project.Scenes.Any(x => string.Equals(x.Id, project.Meta.StartSceneId, StringComparison.OrdinalIgnoreCase));
        return new GameGenerationStepView
        {
            Id = "initial_content",
            Order = 5,
            Title = "Начальный JSON-контент",
            Status = hasStart ? "Готово" : hasScenes ? "Частично" : "Не начато",
            CurrentState = hasScenes
                ? $"Сцен: {project.Scenes.Count}, стартовая сцена: {(hasStart ? project.Meta.StartSceneId : "не задана или не найдена")}"
                : "Сцен ещё нет",
            NextAction = hasStart ? "Переходите к batch-пачкам." : "Сгенерируйте начальный JSON-каркас на вкладке AI-обсуждение.",
            CanRunFromPipeline = false
        };
    }

    private static GameGenerationStepView EquipmentStep(GameProjectData project)
    {
        var slots = project.EquipmentSlots.Count;
        var equipment = project.Items.Count(x => x.IsEquippable);
        var status = slots > 0 && equipment > 0 ? "Готово" : slots > 0 || equipment > 0 ? "Частично" : "Не начато";
        return BatchStep("equipment", 11, "Слоты экипировки и экипировка", status, $"Слотов: {slots}, экипируемых предметов: {equipment}", "equipment", "Сгенерируйте слоты и экипируемые предметы.");
    }

    private static GameGenerationStepView CombatStep(GameProjectData project)
    {
        var combatEncounters = project.Encounters.Count(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) || x.Combatants.Count > 0);
        var combatActions = project.Actions.Count(x => x.AvailableInCombat);
        var requested = project.Combat?.Enabled == true || !string.IsNullOrWhiteSpace(project.GenerationPreferences.CombatDesignText);
        var status = combatEncounters > 0 && combatActions > 0 ? "Готово" : requested ? "Не начато" : "Опционально";
        return BatchStep("combat", 11, "Боёвка v1", status, $"encounters: {combatEncounters}, actions: {combatActions}", "combat", "Сгенерируйте data-driven боёвку v1: combat definition, combat encounters, actions и формулы.");
    }

    private static GameGenerationStepView WorldStateStep(GameProjectData project)
    {
        var count = project.WorldState.Time.Segments.Count + project.WorldState.Aspects.Count;
        var status = project.WorldState.Enabled && count > 0 ? "Готово" : count > 0 ? "Частично" : "Не начато";
        return BatchStep("world_state", 10, "Мир, время и атмосфера", status, $"Сегментов: {project.WorldState.Time.Segments.Count}, аспектов: {project.WorldState.Aspects.Count}, событий: {project.WorldState.AmbientEvents.Count}, правил: {project.WorldState.Rules.Count}", "world-state", "Опишите слой времени, состояний мира, ambient events и правил мира.");
    }

    private static GameGenerationStepView ItemsStep(GameProjectData project)
    {
        var usable = project.Items.Count(x => x.IsConsumable || x.IsUsable);
        var status = project.Items.Count > 0 && usable > 0 ? "Готово" : project.Items.Count > 0 ? "Частично" : "Не начато";
        return BatchStep("items", 12, "Предметы и расходники", status, $"Предметов: {project.Items.Count}, расходников/используемых: {usable}", "items", "Сгенерируйте предметы и расходники.");
    }

    private static GameGenerationStepView SkillsStep(GameProjectData project)
    {
        var count = project.Skills.Count(x => !string.Equals(x.Kind, "spell", StringComparison.OrdinalIgnoreCase));
        return CountStep("skills", 13, "Навыки", "skills", count, "Сгенерируйте пачку не-магических навыков.");
    }

    private static GameGenerationStepView SpellsStep(GameProjectData project)
    {
        var spells = project.Skills.Count(x => string.Equals(x.Kind, "spell", StringComparison.OrdinalIgnoreCase));
        var status = project.Elements.Count > 0 && spells > 0 ? "Готово" : project.Elements.Count > 0 || spells > 0 ? "Частично" : "Не начато";
        return BatchStep("spells_elements", 14, "Заклинания и стихии", status, $"Стихий: {project.Elements.Count}, заклинаний: {spells}", "spells", "Сгенерируйте стихии и заклинания.");
    }

    private static GameGenerationStepView ScenesStep(GameProjectData project)
    {
        var choices = project.Scenes.Sum(x => x.Choices.Count);
        var status = project.Scenes.Count > 0 && choices > 0 ? "Готово" : project.Scenes.Count > 0 ? "Частично" : "Не начато";
        return BatchStep("scenes", 16, "Сцены и ветвления", status, $"Сцен: {project.Scenes.Count}, выборов: {choices}", "scenes", "Сгенерируйте сцены и развилки.");
    }

    private static GameGenerationStepView ImagePromptsStep(GameProjectData project)
    {
        var count = project.ImagePrompts.Count;
        return new GameGenerationStepView
        {
            Id = "image_prompts",
            Order = 18,
            Title = "Опционально: промпты изображений",
            Status = count == 0 ? "Опционально" : "Есть",
            CurrentState = "Промптов: " + count,
            NextAction = count == 0
                ? "Этот этап можно пропустить. Сгенерируйте промпты только если для игры нужны изображения."
                : "Можно дополнять или сразу работать с очередью изображений на вкладке Ассеты.",
            BatchCategory = "image-prompts",
            CanRunFromPipeline = true
        };
    }

    private static GameGenerationStepView CountStep(string id, int order, string title, string category, int count, string nextAction)
    {
        var status = count >= 5 ? "Готово" : count > 0 ? "Частично" : "Не начато";
        return BatchStep(id, order, title, status, "Сущностей: " + count, category, nextAction);
    }

    private static GameGenerationStepView BatchStep(string id, int order, string title, string status, string currentState, string category, string nextAction)
    {
        return new GameGenerationStepView
        {
            Id = id,
            Order = order,
            Title = title,
            Status = status,
            CurrentState = currentState,
            NextAction = status == "Готово" ? "Можно дополнять маленькими draft-пачками." : nextAction,
            BatchCategory = category,
            CanRunFromPipeline = true
        };
    }

    private GameGenerationStepView ValidationStep(GameProjectData project)
    {
        var validation = _validator.Validate(project);
        return new GameGenerationStepView
        {
            Id = "validation_playable",
            Order = 19,
            Title = "Проверка проходимости",
            Status = validation.IsValid ? "Готово" : "Есть проблемы",
            CurrentState = $"Ошибок: {validation.Errors.Count}, предупреждений: {validation.Warnings.Count}",
            NextAction = validation.IsValid ? "Проект проходит базовую проверку." : "Исправьте ошибки в данных или примените валидный draft.",
            CanRunFromPipeline = false
        };
    }
}

internal sealed class GameGenerationStepView
{
    public string Id { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string NextAction { get; set; } = string.Empty;
    public string BatchCategory { get; set; } = string.Empty;
    public bool CanRunFromPipeline { get; set; }
}
