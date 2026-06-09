using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameDesignSlotCatalog
{
    public IReadOnlyList<GameDesignSlot> CreateDefaultSlots()
    {
        return new List<GameDesignSlot>
        {
            Slot("genre", "Жанр", "Основной жанр и опорные ожидания игрока.", true, true, 10, ["тёмное фэнтези", "космоопера", "постапокалипсис", "городское фэнтези", "мистический детектив"], ["meta", "world", "scenes", "mechanics"]),
            Slot("tone", "Тон", "Эмоциональная окраска игры.", true, true, 20, ["мрачный", "приключенческий", "ироничный", "напряжённый", "уютный"], ["meta", "world", "dialogue", "scenes"]),
            Slot("player_role", "Роль игрока", "Кем является игрок внутри мира.", true, true, 15, ["выживший", "детектив", "ученик", "наёмник", "капитан"], ["characters", "quests", "scenes", "stats"]),
            Slot("main_goal", "Главная цель", "Что игрок пытается достичь в MVP.", true, true, 18, ["выжить", "раскрыть тайну", "спасти поселение", "найти артефакт", "вернуться домой"], ["quests", "scenes", "progression"]),
            Slot("core_loop", "Игровой цикл", "Повторяющаяся петля действий игрока.", true, true, 22, ["исследовать -> находить ресурсы -> принимать решения", "говорить -> получать зацепки -> открывать сцены", "сражаться -> получать награды -> усиливаться"], ["runtime", "scenes", "items", "quests"]),
            Slot("world_scale", "Масштаб мира", "Размер и плотность мира для первой версии.", false, true, 45, ["одна локация", "район", "город", "регион", "корабль"], ["world", "locations", "map"]),
            Slot("map_type", "Тип карты", "Как игрок перемещается между местами.", true, true, 35, ["линейная цепочка сцен", "узловая карта", "хаб и ответвления", "открытая сеть"], ["locations", "travel", "world-state"]),
            Slot("time_system", "Система времени", "Нужно ли учитывать дни, фазы или ходы.", false, true, 55, ["нет", "ходы", "день/ночь", "сегменты дня", "таймеры событий"], ["world-state", "runtime/save system", "scenes"]),
            Slot("combat_style", "Боёвка", "Нужны ли бои и насколько они подробные.", true, true, 25, ["нет боёв", "редкие сюжетные столкновения", "простая пошаговая", "тактическая через характеристики"], ["combat", "encounters", "stats", "skills", "items", "balance"]),
            Slot("inventory_depth", "Глубина инвентаря", "Насколько важны предметы и ресурсы.", false, true, 60, ["нет", "минимальная", "средняя", "важная часть игры"], ["items", "loot", "runtime/save system"]),
            Slot("equipment_depth", "Глубина экипировки", "Нужна ли экипировка со слотами и модификаторами.", false, true, 65, ["нет", "минимальная", "несколько слотов", "важная система"], ["items", "equipment", "stats", "requirements"]),
            Slot("dialogue_depth", "Глубина диалогов", "Насколько важны персонажи, отношения и разговоры.", false, true, 50, ["минимальная", "средняя", "высокая", "ключевая механика"], ["characters", "relationships", "scenes", "quests"]),
            Slot("randomness_level", "Случайность", "Как много случайных проверок и вариативности нужно.", true, true, 30, ["нет", "низкая", "средняя", "высокая"], ["scenes", "travel", "encounters", "loot", "world-state"]),
            Slot("progression_type", "Прогрессия", "Как игрок становится сильнее или открывает возможности.", false, true, 70, ["нет", "уровни", "навыки", "репутация", "узлы прогрессии"], ["skills", "progression", "stats", "quests"]),
            Slot("economy_depth", "Экономика", "Нужны ли деньги, магазины, обмен и цены.", false, true, 75, ["нет", "минимальная", "простая валюта", "магазины и награды"], ["currencies", "items", "shops", "quests"]),
            Slot("quest_structure", "Структура квестов", "Как устроены задачи игрока.", true, true, 40, ["одна главная линия", "главная линия и побочные задачи", "цепочки квестов", "открытые цели"], ["quests", "scenes", "locations"]),
            Slot("save_load_policy", "Сохранения", "Какой стиль сохранения предполагает дизайн.", false, false, 80, ["обычные сохранения", "автосохранение", "контрольные точки", "один забег"], ["runtime/save system"]),
            Slot("failure_policy", "Провал", "Что происходит при поражении или неудаче.", true, true, 42, ["мягкий откат", "альтернативная сцена", "потеря ресурсов", "конец забега"], ["runtime", "combat", "scenes", "save system"]),
            Slot("visual_style", "Визуальный стиль", "Какой стиль нужен для изображений и атмосферы.", false, true, 85, ["иллюстративный", "реалистичный", "аниме", "пиксель-арт", "мрачная живопись"], ["image-prompts", "assets", "meta"])
        };
    }

    public void EnsureDefaultSlots(GameDesignProfile profile)
    {
        var existing = profile.Slots
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var slot in CreateDefaultSlots())
        {
            if (!existing.TryGetValue(slot.Id, out var current))
            {
                profile.Slots.Add(slot);
                continue;
            }

            current.Title = string.IsNullOrWhiteSpace(current.Title) ? slot.Title : current.Title;
            current.Description = string.IsNullOrWhiteSpace(current.Description) ? slot.Description : current.Description;
            current.IsRequired = slot.IsRequired;
            current.CanBeAssumedByLlm = slot.CanBeAssumedByLlm;
            current.Priority = current.Priority == 0 ? slot.Priority : current.Priority;
            if (current.SuggestedOptions.Count == 0) current.SuggestedOptions = slot.SuggestedOptions;
            if (current.AffectsSystems.Count == 0) current.AffectsSystems = slot.AffectsSystems;
            if (current.DependsOn.Count == 0) current.DependsOn = slot.DependsOn;
        }

        profile.Slots = profile.Slots
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static GameDesignSlot Slot(string id, string title, string description, bool required, bool canBeAssumed, int priority, List<string> options, List<string> systems)
    {
        return new GameDesignSlot
        {
            Id = id,
            Title = title,
            Description = description,
            IsRequired = required,
            CanBeAssumedByLlm = canBeAssumed,
            Priority = priority,
            SuggestedOptions = options,
            AffectsSystems = systems
        };
    }
}
