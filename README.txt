LMStudioSillyTavernWorldBuilder

Первая рабочая версия AI Game Builder для создания и прохождения data-driven текстовых игр с иллюстрациями.

WinForms Designer rules
-----------------------

- Do not use helper methods inside InitializeComponent to create controls.
- Do not use loops inside InitializeComponent.
- Create all static UI controls explicitly in Designer.cs.
- Add new tabs as explicit TabPage fields.
- Runtime content may be created dynamically, for example scene choice buttons.
- Codex must not launch Visual Studio/devenv or View.ViewDesigner automatically. Use text checks and dotnet build; the user opens Designer manually.
- Codex must not run git commands unless the user explicitly asks for them.

Главная идея:
- игра хранится как JSON-данные, а не как сгенерированный C# код;
- runtime прохождения работает без LLM;
- LM Studio нужен для обсуждения идеи, генерации брифа, структуры, контента и image prompt-ов;
- Fooocus подключён в semi-automatic workflow, без неподтверждённого API-режима.

Как хранится игра
-----------------

Каждая игра хранится в отдельной папке. Основной формат хранения — split-json:

<GameName>\
  game-project.json
  manifest.json
  design\
  data\
    stats\
    skills\
    items\
    equipment-slots\
    elements\
    currencies\
    variables\
    characters\
    relationships\
    locations\
    location-connections\
    location-states\
    scenes\
    quests\
    encounters\
    actions\
    formulas\
    status-effects\
    progression\
    mechanics.json
    combat\
  prompts\
    image-prompts\
    generated-candidates\
    asset-links\
    prompt-history\
    fooocus-queue.txt
    fooocus-queue.json
  drafts\
  changes\
  assets\
    scenes\
    characters\
    items\
    ui\
    generated-imports\
  saves\
  logs\

game-project.json теперь маленький root document. Он содержит только метаданные проекта: schemaVersion, projectId, title, folderName, startSceneId, language, dataLayout и даты создания/обновления.

manifest.json содержит индекс актуальных entity-файлов: stats, skills, items, equipment slots, elements, currencies, variables, characters, relationships, locations, location connections, location states, scenes, quests, encounters, actions, formulas, status effects, progression nodes, mechanics, combat, image prompts, generated candidates и asset links.

data содержит маленькие JSON-файлы игровых сущностей. Каждая сцена, персонаж, предмет, стат, локация и квест сохраняются отдельно. Runtime по-прежнему получает единый GameProjectData в памяти и не знает, как именно проект разложен на диске.

drafts содержит AI-черновики. Raw output модели сохраняется отдельно, затем валидный GameProjectData раскладывается на маленькие draft JSON. Черновики не должны автоматически заменять утверждённые данные при ошибках validation.

Базовый слой механик v1
----------------------

- formulas: безопасные целочисленные формулы без C#/скриптов. Поддержаны арифметика, скобки, min/max/clamp/abs/percent/random/dice и переменные stat/effectiveStat/currency/variable/relationship/item/skill/status/turn.
- status-effects: баффы, дебаффы и нейтральные статусы с длительностью, стаками, periodic/on apply/on expire effects и stat modifiers.
- progression: узлы прокачки с parent-зависимостями, требованиями, стоимостью, эффектами и привязкой к навыкам.
- gameplay actions: runtime-действия игрока с requirements, costs, effects, cooldownTurns и tags.
- mechanics: флаги включения ходов, статусов, прокачки, панели действий, dice randomness и заметки по механикам.

Картинки опциональны. Игра должна оставаться проходящей как data-driven текстовая игра даже без image prompts и импортированных ассетов.

Рекомендуемый порядок генерации
-------------------------------

1. идея/бриф;
2. концепт;
3. MVP;
4. структура;
5. статы/ресурсы/валюты/переменные;
6. формулы;
7. статусы;
8. навыки/заклинания;
9. дерево прокачки;
10. предметы/экипировка;
11. локации;
12. сцены;
13. игровые действия;
14. encounters/actions;
15. image prompts только если нужны.

Batch generation создаёт draft. Пользователь проверяет draft и применяет его вручную через вкладку "Пайплайн"; автоматическое применение batch-данных не является нормальным workflow.

changes содержит историю применённых операций: создание проекта, импорт/применение draft, привязку ассетов и ручной save_project. Rollback в этой версии не реализован.

saves содержит прохождения: autosave.json и save_*.json.

Старый monolithic формат game-project.json автоматически загружается. При первом сохранении такой проект мигрируется в split-json, а старый файл сохраняется как game-project.legacy-backup.<timestamp>.json.

Практический workflow game-builder
----------------------------------

Не нужно генерировать всю игру одним запросом. Нормальный порядок работы: идея -> бриф -> концепт -> MVP -> структура -> начальный контент -> маленькие batch -> draft review/apply -> механический отчёт -> тестовый запуск.

На вкладке "Пайплайн" можно сохранить пожелания генерации: общие пожелания к геймплею, навыки и способности, прокачка и опыт, будущая боёвка, баланс и стиль, запреты/избегаемые решения, заметки. Эти тексты хранятся в отдельных полях GenerationPreferences, попадают в split-json и compact context, а batch prompt-ы учитывают их при создании навыков, прокачки, предметов, действий и encounters.

Опыт и уровни описываются в mechanics.experience. Механика необязательная: можно включить опыт игрока, опыт навыков или не использовать их вовсе. Runtime поддерживает effects experience/playerExperience и skillExperience, формулы порогов уровня, fallback-пороги 100 * currentLevel для игрока и ExperienceToNextLevel или 50 * currentSkillLevel для навыка. Use-based прокачка делается через actions/items/choices/effects: тренировка даёт skillExperience, книга учит навык или даёт опыт навыка, квест выдаёт playerExperience или открывает progression node. DefaultPlayerExperienceRewardFormulaId и DefaultPlayerExperienceRewardFormulaExpression пока являются резервом для будущей боёвки/encounters и не начисляются автоматически.

Настройки LM Studio включают "Максимум входного контекста, токены" и "Максимум ответа LM, токены". Оценка токенов приблизительная: text length / 4. Если compact context превышает бюджет, длинные списки сущностей и длинные текстовые поля усекаются, а в ContextBudget добавляется Trimmed/HardTrimmed и пояснение. Это защита от слишком больших prompt-ов, а не точная токенизация. "Максимум ответа LM" используется как желаемый верхний лимит ответа запроса, поэтому его можно повышать для больших batch/revision JSON-ответов.

"Сформировать правки" теперь создаёт draft-исправление revision-fix. Модель должна вернуть partial GameProjectData JSON с точечными изменениями; проект не меняется автоматически. После генерации нужно нажать "Проверить последний draft" и затем "Применить draft", если правки подходят.

Настройки
---------

- настройки приложения сохраняются в %LOCALAPPDATA%\AiGameBuilder\settings.json;
- если каталог игр пустой, используется Documents\AiGameBuilder\Games;
- LM Studio timeout = 0 означает: без HTTP timeout, ждать до отмены операцией "Остановить";
- LM Studio unload можно настроить через unload URL или unload command;
- unload command поддерживает .bat, .cmd, .exe, .ps1 и обычную command line строку;
- если включено "Continue if unload fails", ошибка unload не прерывает запуск Fooocus.

Fooocus semi-automatic workflow
-------------------------------

1. На вкладке "Настройки" выбрать папку Fooocus_win64-2-5-0 или run.bat.
2. Нажать "Определить", чтобы заполнить launch file, working dir и output dir.
3. На вкладке "Ассеты" сформировать image prompt-ы.
4. Нажать "Экспортировать очередь и запустить Fooocus".
5. Приложение сохранит prompts\fooocus-queue.txt и prompts\fooocus-queue.json.
6. Открыть Fooocus и сгенерировать изображения вручную.
7. Нажать "Импорт": изображения попадут в assets\generated-imports\<timestamp>\ как кандидаты.
8. Выбрать prompt, нажать "Привязать изображение" и выбрать подходящий файл.

Автоматическая отправка prompt-ов в Fooocus API пока не включена. Эта версия использует честный полуавтоматический режим: export queue, start Fooocus, manual generation, import images, link selected images.

Сборка
------

dotnet build LMStudioSillyTavernWorldBuilder.sln

Тесты
-----

dotnet test LMStudioSillyTavernWorldBuilder.sln

Game systems 2.0
----------------

split-json remains the primary storage. SQLite is not used in this pass because the current workflow is optimized for AI-friendly small JSON files, per-entity validation, drafts, review and human-readable edits. SQLite can be added later as an optional runtime index/cache if split-json becomes a bottleneck, but it is not the source of truth now.

Stats now support attributes, resources, derived and hidden values. Resources can be shown as bars, have color hints and regenerate per turn. Currencies and variables are first-class project data and are stored in saves as dictionaries.

Inventory now supports legacy item counters and new inventory entries. Runtime prefers InventoryEntries, where each entry has an instance id, quantity, durability, equipped state, slot and metadata. Items can define rarity, slot, stack size, tags, requirements, modifiers, use/equip/unequip effects, value and currency.

Equipment uses GameEquipmentSlotDefinition. Equippable items reference SlotId and can apply stat modifiers through GetEffectiveStats. Equip/unequip also supports effects.

Skills now support passive, active, spell, craft and social kinds. Saves store KnownSkills with level, experience, cooldown and enabled state. Passive skills contribute modifiers to effective stats. Active skills and spells can have requirements, costs, effects, elements and cooldowns.

Elements model magic schools or damage types with strengths, weaknesses, color hints and visual prompt hints. Spells are represented as skill definitions with Kind=spell and optional ElementId.

Locations now support region/status, discovery, map coordinates, tags, access requirements and enter effects. Location connections define directed or two-way travel with requirements and travel effects. Location states are stored per save and can be changed by effects.

Effects and requirements now cover stat, item, skill, relationship, quest, flag, currency, locationState and variable. Unknown effect/requirement types are validator warnings rather than fatal errors.

PlayForm
--------

The Play tab remains available as a quick preview. The Saves tab now also has "Open game in window", which opens a separate PlayForm for a fuller runtime view.

PlayForm shows title/status/current location, scene image, scene text, choices and dynamic info tabs. Tabs for currencies, inventory, equipment, skills, relationships, quests and map are hidden when the current game does not use those systems.

Batch generation
----------------

GameCreationPipelineService includes small-batch methods for stats/resources, items, equipment, skills, spells, locations, scenes, encounters and batch review against existing content.

Recommended workflow:
1. Discuss the idea on the AI discussion tab.
2. Build the brief, concept, MVP and data structure.
3. Generate initial JSON/MVP content.
4. Open the Pipeline tab and work through the ordered steps.
5. Generate small batches as drafts.
6. Review the latest draft.
7. Apply the draft manually only after checking it, or reject it.

Batch generation now creates a draft in drafts and does not change project data immediately. The current project changes only after "Применить draft". "Проверить последний draft" reviews the latest applicable draft without applying it. "Отклонить draft" marks draft files as rejected and keeps them on disk for history.

Each batch prompt uses compact context instead of the full project JSON: metadata, brief/concept/MVP previews, counts, existing IDs and short entity summaries. Prompts require Russian player-facing text, snake_case Latin IDs and small controlled batches.

Pipeline draft/review/apply workflow
------------------------------------

The Pipeline tab is the preferred place for controlled generation. It shows the ordered generation plan, lets the user run small batch stages manually, and keeps the latest applicable draft visible before any decision.

Batch stages do not apply generated data automatically. They save raw model output and per-entity JSON files under drafts\<sessionId>\. The project is changed only when the user explicitly applies the draft. The user can reject a draft instead; rejected draft files stay on disk for history.

Image prompts follow the same draft-only rule. Generating image prompts creates an image-prompts draft; prompts appear in the project and asset queue only after applying that draft.

Images are optional. A game should remain playable without image prompts, generated assets or linked images. Image prompt generation is an optional pipeline stage for projects that need illustrations.

Review is stored next to the draft being reviewed as drafts\<sessionId>\review.txt and referenced from draft-manifest.json. Review does not create a separate review-batch draft.

Codex must not run Visual Studio/devenv/View.ViewDesigner, open WinForms Designer, or run git commands unless the user explicitly asks for that exact action.

Mechanical runtime stabilization notes
--------------------------------------

Formulas are evaluated by the local whitelist evaluator only. Supported syntax: integer numbers, + - * / %, parentheses, min/max/clamp/abs/percent/random/dice and variables stat.<id>, effectiveStat.<id>, currency.<id>, variable.<id>, relationship.<id>, item.<id>.quantity, skill.<id>.level, skill.<id>.experience, player.level, player.experience, status.<id>.stacks and turn. Runtime exposes TryEvaluateFormula for diagnostics; EvaluateFormula remains a compatibility wrapper and returns 0 on errors.

Actions are local runtime commands with requirements, costs, effects and optional cooldownTurns. The UI uses ExecuteAction, which returns Success, Message, LogLines and AppliedEffectSummaries, so unavailable actions can show a reason instead of failing silently.

Statuses are buffs, debuffs and neutral effects. StackMode supports ignore, refresh, stack and replace. MaxStacks <= 0 is treated as 1 by runtime repair/validation logic. RemainingTurns <= 0 means the status is indefinite and does not expire on EndTurn. OnExpireEffects are applied both on natural expiration and forced remove, so generated mechanics stay predictable.

Progression nodes are unlocked only after parents, requirements and costs are checked. Costs are paid only after all checks pass. If a node has SkillId, the skill must exist and be learnable before the node is opened.

EndTurn advances TurnNumber, regenerates per-turn stats, ticks skill/action cooldowns, applies periodic status effects, expires finite statuses and returns GameTurnResult with log lines.

Recommended generation order:
1. Structure and initial content.
2. Stats/resources/currencies/variables.
3. Formulas.
4. Status effects.
5. Progression.
6. Gameplay actions.
7. Scenes/locations/encounters.
8. Image prompts only when needed.

Full turn-based combat, enemies, targets and initiative are a separate future layer. Do not run Visual Studio/devenv/Designer/git unless explicitly requested by the user.

World State / Atmosphere v1
---------------------------

World-state is a data-driven layer stored in split-json as data/world-state.json and referenced from manifest.json. It describes time segments, world aspects, ambient events and world rules. LLM generation creates this layer only as small partial GameProjectData draft JSON; the user reviews and applies the draft manually through the existing Pipeline workflow. It must not generate code or a combat system.

Runtime, not the LLM, moves time, validates requirements, applies effects, runs world rules and rolls ambient events. Supported atmosphere requirement/effect types include timeSegment, dayNumber, worldState/worldAspect and advanceTime. World-state should affect requirements/effects/actions/scenes/ambientEvents, not remain decorative. For worldState/worldAspect, TargetId is the aspect id and StringValue is the state id.

Examples:
- fantasy: morning/day/evening/night, weather, moon phase, season, magical background;
- space: watch cycle, oxygen, energy, alarm, radiation, communication and ship state;
- social/work/romance: weekday, time of day, mood, NPC schedule, fatigue, money and reputation.

Images remain optional. Full turn-based combat, balance simulator, SQLite, Fooocus API and vision review are future tasks, not part of this layer. Codex must not run git commands, Visual Studio/devenv or WinForms Designer unless explicitly requested.
