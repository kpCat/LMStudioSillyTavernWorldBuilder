using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Providers;
using LMStudioSillyTavernWorldBuilder.Runtime;
using LMStudioSillyTavernWorldBuilder.Services;
using LMStudioSillyTavernWorldBuilder.Storage;

namespace LMStudioSillyTavernWorldBuilder;

public partial class MainForm : Form
{
    private readonly HttpClient _httpClient = new();
    private readonly GameStorageService _storageService = new();
    private readonly AppSettingsStore _settingsStore = new();
    private readonly GameRuntimeEngine _runtimeEngine = new();
    private readonly LmStudioService _lmStudioService;
    private readonly FooocusService _fooocusService = new();
    private readonly FooocusProfileDetector _fooocusProfileDetector = new();
    private readonly ImageAssetService _imageAssetService = new();
    private readonly GameProjectValidator _validator = new();
    private readonly GameChangeLogService _changeLogService = new();
    private readonly GameGenerationWorkflowService _workflowService = new();
    private readonly GameMechanicsReportService _mechanicsReportService = new();
    private readonly GameDraftService _draftService = new();
    private readonly GameDesignInterviewService _designInterviewService = new();
    private readonly GameDesignPlannerService _designPlannerService = new();
    private readonly GameRandomDirectorService _randomDirectorService = new();
    private readonly GameChangeRequestService _changeRequestService = new();
    private readonly GameBalanceSimulatorService _balanceSimulatorService = new();
    private readonly GameMvpOrchestratorService _mvpOrchestratorService = new();
    private readonly LmStudioProfileService _lmProfileService = new();
    private readonly GameCreationPipelineService _pipelineService;
    private readonly ExternalToolOrchestratorService _orchestratorService;
    private readonly IdeaDiscussionSession _discussionSession = new();
    private static readonly JsonSerializerOptions UiJsonOptions = GenerationJsonOptions.UiJson;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private GameProjectData? _currentProject;
    private SaveGame? _currentSave;
    private AppSettings _appSettings = new();
    private CancellationTokenSource? _currentOperationCts;
    private AppWorkflowStatus _status = AppWorkflowStatus.Idle;
    private readonly Dictionary<string, string> _pipelineRulesByCategory = new(StringComparer.OrdinalIgnoreCase);
    private string _activePipelineRulesCategory = string.Empty;
    private bool _loadingSettingsUi;
    private bool _loadingDesignSelection;

    public MainForm()
    {
        InitializeComponent();
        _lmStudioService = new LmStudioService(_httpClient);
        _pipelineService = new GameCreationPipelineService(_lmStudioService, _storageService);
        _orchestratorService = new ExternalToolOrchestratorService(_lmStudioService, _fooocusService);
        _appSettings = _settingsStore.LoadOrCreate();
        ConfigureLmProfileUi();
        ApplySettingsToUi(_appSettings);
        ConfigureDesignBrainUi();
        ConfigurePipelineListView();
        RefreshProjectList();
        SetStatus(AppWorkflowStatus.Idle);
    }

    private void btnBrowseGamesRoot_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите каталог игр",
            UseDescriptionForTitle = true,
            SelectedPath = txtGamesRoot.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtGamesRoot.Text = dialog.SelectedPath;
            RefreshProjectList();
            SaveSettingsFromUi();
        }
    }

    private void btnOpenGameFolder_Click(object? sender, EventArgs e)
    {
        var folder = _currentProject?.Summary.ProjectPath;
        if (string.IsNullOrWhiteSpace(folder))
        {
            folder = GetGamesRoot();
        }

        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    private void btnRefreshProjects_Click(object? sender, EventArgs e)
    {
        RefreshProjectList();
    }

    private async void btnNewGame_Click(object? sender, EventArgs e)
    {
        var title = PromptForText("Название новой игры", "Новая игра");
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            SetStatus(AppWorkflowStatus.Saving);
            _currentProject = _storageService.CreateNewProject(GetGamesRoot(), title.Trim());
            _storageService.EnsureProjectDirectories(_storageService.GetProjectFolder(GetGamesRoot(), _currentProject));
            await _storageService.SaveProjectAsync(GetGamesRoot(), _currentProject);
            await _changeLogService.AppendChangeAsync(_currentProject, new GameChangeRecord
            {
                Operation = "create",
                EntityType = "project",
                EntityId = _currentProject.Meta.Id,
                RelativePath = "game-project.json",
                CreatedBy = "user",
                ApprovedByUser = true,
                Notes = "Project created."
            }, CurrentOperationToken);
            _currentSave = _storageService.CreateInitialSave(_currentProject, "autosave");
            await _storageService.SaveProgressAsync(_currentProject, _currentSave, "autosave.json");
            AppendLog("Создан новый проект: " + _currentProject.Meta.Title);
            RefreshAllViews();
            RefreshProjectList();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnLoadGame_Click(object? sender, EventArgs e)
    {
        if (lstProjects.SelectedItem is not GameProjectSummary summary)
        {
            MessageBox.Show(this, "Выберите проект в списке.", "Проект", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await LoadProjectAsync(summary.ProjectPath);
    }

    private async void btnDeleteProject_Click(object? sender, EventArgs e)
    {
        if (lstProjects.SelectedItem is not GameProjectSummary summary)
        {
            MessageBox.Show(this, "Выберите проект в списке.", "Проект", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var title = string.IsNullOrWhiteSpace(summary.Title) ? summary.FolderName : summary.Title;
        var confirmation = $"Удалить проект \"{title}\"? Папка будет перемещена в _deleted внутри каталога игр.";
        if (MessageBox.Show(this, confirmation, "Удаление проекта", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            var gamesRoot = Path.GetFullPath(GetGamesRoot());
            var deletedRoot = Path.Combine(gamesRoot, "_deleted");
            var projectPath = Path.GetFullPath(summary.ProjectPath);

            if (!Directory.Exists(projectPath))
            {
                throw new InvalidOperationException("Папка проекта не найдена: " + projectPath);
            }

            if (!IsPathUnderDirectory(projectPath, gamesRoot) || IsPathUnderDirectory(projectPath, deletedRoot))
            {
                throw new InvalidOperationException("Удаление отменено: папка проекта не находится в активном каталоге игр.");
            }

            Directory.CreateDirectory(deletedRoot);
            var safeFolderName = MakeSafeDeletedFolderName(Path.GetFileName(projectPath));
            var destination = GetUniqueDeletedProjectPath(deletedRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + safeFolderName);
            Directory.Move(projectPath, destination);

            if (_currentProject != null
                && (string.Equals(_currentProject.Summary.Id, summary.Id, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFullPath(_currentProject.Summary.ProjectPath), projectPath, StringComparison.OrdinalIgnoreCase)))
            {
                _currentProject = null;
                _currentSave = null;
                pgProject.SelectedObject = null;
                lstSaves.Items.Clear();
            }

            RefreshProjectList();
            AppendLog("Проект перемещён в _deleted: " + destination);
            await Task.CompletedTask;
        }, AppWorkflowStatus.Idle);
    }

    private async void btnSaveGame_Click(object? sender, EventArgs e)
    {
        await SaveCurrentProjectAsync();
    }

    private async void btnDesignApplyIdea_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            _designInterviewService.ApplyInitialIdea(project.DesignProfile, txtGameCrafterIdea.Text);
            _designInterviewService.SetCreationMode(project.DesignProfile, GetSelectedCreationMode());
            RefreshDesignBrainView();
            await _storageService.SaveProjectAsync(GetGamesRoot(), project, CurrentOperationToken);
            AppendLog("Дизайн-досье обновлено: идея и режим создания сохранены.");
        }, AppWorkflowStatus.Idle);
    }

    private void btnDesignRefreshQuestions_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            MessageBox.Show(this, "Сначала создайте или откройте проект.", "Проект", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _designInterviewService.EnsureProfile(_currentProject.DesignProfile);
        RefreshDesignBrainView();
        AppendLog("Список недостающих дизайн-вопросов обновлён.");
    }

    private async void btnDesignApplyAnswer_Click(object? sender, EventArgs e)
    {
        var selectedQuestion = lvDesignQuestions.SelectedItems.Count == 0 ? null : lvDesignQuestions.SelectedItems[0].Tag as GameDesignQuestion;
        var selectedSlot = lvDesignSlots.SelectedItems.Count == 0 ? null : lvDesignSlots.SelectedItems[0].Tag as GameDesignSlot;
        var slotId = selectedQuestion?.SlotId ?? selectedSlot?.Id;
        if (string.IsNullOrWhiteSpace(slotId))
        {
            MessageBox.Show(this, "Выберите вопрос или слот дизайна.", "Крафтер игры", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = txtDesignAnswer.Text;
        if (string.IsNullOrWhiteSpace(answer))
        {
            var confirm = MessageBox.Show(this, "Ответ пустой. Очистить выбранный слот дизайна?", "Крафтер игры", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
            {
                return;
            }
        }

        await EnsureProjectThenRunAsync(async project =>
        {
            _designInterviewService.SetUserAnswer(project.DesignProfile, slotId, answer);
            RefreshDesignBrainView();
            SelectDesignSlot(slotId);
            await _storageService.SaveProjectAsync(GetGamesRoot(), project, CurrentOperationToken);
            AppendLog((selectedQuestion != null ? "Ответ сохранён в слот дизайна: " : "Слот дизайна обновлён: ") + slotId);
        }, AppWorkflowStatus.Idle);
    }

    private async void btnDesignAskLlmAssumptions_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            _designInterviewService.EnsureProfile(project.DesignProfile);
            var prompt = _designInterviewService.BuildLlmAssumptionPrompt(project);
            AppendLog("Запрашиваю LLM-допущения для недостающих слотов дизайна.");
            var answer = await _lmStudioService.SendAsync(GetLmSettingsForPurpose("json-draft"), new[]
            {
                new ApiMessage("system", Prompts.GameDesignAssumptions.SystemPrompt),
                new ApiMessage("user", prompt)
            }, Prompts.GameDesignAssumptions.Settings, CurrentOperationToken);
            var applied = _designInterviewService.ApplyLlmAssumptionsFromJson(project.DesignProfile, answer);
            RefreshDesignBrainView();
            await _storageService.SaveProjectAsync(GetGamesRoot(), project, CurrentOperationToken);
            AppendLog("LLM-допущения применены: " + applied);
        }, AppWorkflowStatus.Idle);
    }

    private async void btnDesignBuildPlan_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            project.CreationPlan = _designPlannerService.BuildPlan(project);
            RefreshDesignBrainView();
            await _storageService.SaveProjectAsync(GetGamesRoot(), project, CurrentOperationToken);
            AppendLog("План создания игры построен и сохранён.");
        }, AppWorkflowStatus.Idle);
    }

    private async void btnDesignSave_Click(object? sender, EventArgs e)
    {
        await SaveCurrentProjectAsync();
    }

    private void btnRandomDirectorCheck_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            MessageBox.Show(this, "Сначала создайте или откройте проект.", "Random Director", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var report = _randomDirectorService.BuildReport(_currentProject);
        txtDesignPreview.Text = _randomDirectorService.FormatReportForUi(report);
        AppendLog("Random Director report построен: предупреждений " + report.Warnings.Count + ", рекомендаций " + report.Recommendations.Count + ".");
    }

    private async void btnRandomDirectorGenerate_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.GeneratingContent);
            var requestedCount = ParseRandomDirectorEventCount();
            var report = _randomDirectorService.BuildReport(project);
            txtDesignPreview.Text = _randomDirectorService.FormatReportForUi(report);
            await _pipelineService.BuildRandomDirectorDraftAsync(project, GetLmSettingsForPurpose("random-director"), requestedCount, AppendLog, CurrentOperationToken);
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private void btnChangeRequestAnalyze_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            MessageBox.Show(this, "Сначала создайте или откройте проект.", "Запрос на изменение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var request = txtChangeRequest.Text.Trim();
        var report = _changeRequestService.AnalyzeRequest(_currentProject, request);
        var plan = _changeRequestService.BuildPatchPlan(_currentProject, report);
        txtDesignPreview.Text = _changeRequestService.FormatReportForUi(report, plan);
        AppendLog("Change Request report построен: систем " + report.AffectedSystems.Count + ", рисков " + report.Risks.Count + ".");
    }

    private async void btnChangeRequestGenerate_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            var request = txtChangeRequest.Text.Trim();
            if (string.IsNullOrWhiteSpace(request))
            {
                MessageBox.Show(this, "Напишите запрос на изменение игры.", "Запрос на изменение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetStatus(AppWorkflowStatus.GeneratingContent);
            var report = _changeRequestService.AnalyzeRequest(project, request);
            var plan = _changeRequestService.BuildPatchPlan(project, report);
            txtDesignPreview.Text = _changeRequestService.FormatReportForUi(report, plan);
            await _pipelineService.BuildChangeRequestDraftAsync(project, GetLmSettingsForPurpose("change-request"), request, AppendLog, CurrentOperationToken);
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnDesignConversationSend_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            var message = txtDesignConversation.Text.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show(this, "Напишите сообщение для дизайн-диалога.", "Дизайн-диалог", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetStatus(AppWorkflowStatus.GeneratingContent);
            var report = await _pipelineService.ProcessDesignConversationTurnAsync(
                project,
                GetLmSettingsForPurpose("review"),
                message,
                txtDesignConversationFocus.Text,
                AppendLog,
                CurrentOperationToken);
            txtDesignPreview.Text = report;
            RefreshDesignBrainView();
            await _storageService.SaveProjectAsync(GetGamesRoot(), project, CurrentOperationToken);
            AppendLog("Дизайн-диалог обработан и сохранён.");
        }, AppWorkflowStatus.Idle);
    }

    private void btnBalanceCheck_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            MessageBox.Show(this, "Сначала создайте или откройте проект.", "Balance Simulator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var runs = ParseBalanceSimulationRunCount();
        var report = _balanceSimulatorService.BuildReport(_currentProject, runs);
        txtDesignPreview.Text = _balanceSimulatorService.FormatReportForUi(report);
        AppendLog("Balance Simulator report построен: issues " + report.Issues.Count + ", recommendations " + report.Recommendations.Count + ".");
    }

    private async void btnBalanceGenerateDraft_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.GeneratingContent);
            var runs = ParseBalanceSimulationRunCount();
            var report = _balanceSimulatorService.BuildReport(project, runs);
            txtDesignPreview.Text = _balanceSimulatorService.FormatReportForUi(report);
            await _pipelineService.BuildBalanceRebalanceDraftAsync(project, GetLmSettingsForPurpose("balance"), runs, AppendLog, CurrentOperationToken);
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private void btnMvpCheck_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            MessageBox.Show(this, "Сначала создайте или откройте проект.", "MVP Orchestrator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var report = _mvpOrchestratorService.BuildReadinessReport(_currentProject);
        txtDesignPreview.Text = _mvpOrchestratorService.FormatReportForUi(report);
        AppendLog("MVP Orchestrator report построен: готовность " + report.CompletionPercent + "%, следующий этап " + (report.NextRecommendedStage ?? "не требуется") + ".");
    }

    private async void btnMvpGenerateNextDraft_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.GeneratingContent);
            var report = _mvpOrchestratorService.BuildReadinessReport(project);
            txtDesignPreview.Text = _mvpOrchestratorService.FormatReportForUi(report);
            await _pipelineService.BuildNextMvpDraftAsync(project, GetLmSettingsForPurpose("mvp-orchestrator"), AppendLog, CurrentOperationToken);
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private void lvDesignQuestions_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loadingDesignSelection)
        {
            return;
        }

        if (lvDesignQuestions.SelectedItems.Count == 0 || lvDesignQuestions.SelectedItems[0].Tag is not GameDesignQuestion question)
        {
            return;
        }

        _loadingDesignSelection = true;
        try
        {
            lvDesignSlots.SelectedItems.Clear();
            txtDesignAnswer.Text = string.Join(Environment.NewLine, question.SuggestedOptions);
        }
        finally
        {
            _loadingDesignSelection = false;
        }
    }

    private void lvDesignSlots_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loadingDesignSelection)
        {
            return;
        }

        if (lvDesignSlots.SelectedItems.Count == 0 || lvDesignSlots.SelectedItems[0].Tag is not GameDesignSlot slot)
        {
            return;
        }

        _loadingDesignSelection = true;
        try
        {
            lvDesignQuestions.SelectedItems.Clear();
            txtDesignAnswer.Text = slot.Value;
        }
        finally
        {
            _loadingDesignSelection = false;
        }
    }

    private async void btnSaveGameAs_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            return;
        }

        var title = PromptForText("Новое название проекта", _currentProject.Meta.Title);
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        _currentProject.Meta.Title = title.Trim();
        _currentProject.Summary.Title = title.Trim();
        _currentProject.Summary.FolderName = title.Trim();
        _currentProject.Summary.ProjectPath = Path.Combine(GetGamesRoot(), title.Trim());
        await SaveCurrentProjectAsync();
    }

    private void btnOpenDraftsFolder_Click(object? sender, EventArgs e)
    {
        OpenProjectSubfolder("drafts");
    }

    private void btnOpenDataFolder_Click(object? sender, EventArgs e)
    {
        OpenProjectSubfolder("data");
    }

    private async void btnResaveSplitJson_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            await _storageService.SaveProjectAsync(GetGamesRoot(), project, CurrentOperationToken);
            await _changeLogService.AppendChangeAsync(project, new GameChangeRecord
            {
                Operation = "save_project",
                EntityType = "project",
                EntityId = project.Meta.Id,
                RelativePath = "game-project.json",
                CreatedBy = "user",
                ApprovedByUser = true,
                Notes = "Manual split-json save."
            }, CurrentOperationToken);
            AppendLog("Project resaved as split-json. Root game-project.json contains project metadata; entities are in data/prompts folders.");
            RefreshProjectList();
        }, AppWorkflowStatus.Idle);
    }

    private void btnValidateProject_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            return;
        }

        var result = _validator.Validate(_currentProject);
        AppendValidationResult("Project validation", result);
        var manifestPath = Path.Combine(_currentProject.Summary.ProjectPath, "manifest.json");
        if (File.Exists(manifestPath))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<GameProjectManifest>(File.ReadAllText(manifestPath), _jsonOptions);
                if (manifest != null)
                {
                    AppendValidationResult("Storage validation", _validator.ValidateStorage(_currentProject.Summary.ProjectPath, manifest));
                }
            }
            catch (Exception ex)
            {
                AppendLog("Storage validation error: " + ex.Message);
            }
        }
    }

    private async void btnTestLm_Click(object? sender, EventArgs e)
    {
        await RunSafeAsync(async () =>
        {
            SetStatus(AppWorkflowStatus.Discussing);
            var answer = await _lmStudioService.SendAsync(GetLmSettingsForPurpose("active"), new[]
            {
                new ApiMessage("system", "Отвечай кратко на русском языке."),
                new ApiMessage("user", "Проверка связи. Напиши: подключение работает.")
            }, new GenerationSettings(0.2, 0.9, 0.03, 30, 1.05, 0.0, 128), CurrentOperationToken);
            AppendDiscussion("LM Studio", answer);
            AppendLog("LM Studio test completed.");
        }, AppWorkflowStatus.Idle);
    }

    private async void btnStartDiscussion_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            var idea = string.IsNullOrWhiteSpace(txtUserInput.Text)
                ? $"Помоги сформировать идею текстовой игры для проекта \"{project.Meta.Title}\"."
                : txtUserInput.Text.Trim();
            txtUserInput.Clear();
            SetStatus(AppWorkflowStatus.Discussing);
            AppendDiscussion("Пользователь", idea);
            var answer = await _pipelineService.StartDiscussionAsync(project, GetLmSettingsForPurpose("discussion"), _discussionSession, idea, AppendLog, CurrentOperationToken);
            AppendDiscussion("Модель", answer);
        }, AppWorkflowStatus.Idle);
    }

    private async void btnSend_Click(object? sender, EventArgs e)
    {
        await SendDiscussionTextAsync(txtUserInput.Text.Trim());
    }

    private async void btnStructuredPrompt_Click(object? sender, EventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var structuredPrompt = GetStructuredDiscussionPrompt(button);
        if (structuredPrompt != null)
        {
            await SendDiscussionTextAsync(structuredPrompt);
            return;
        }

        var text = button.Text switch
        {
            "Уточнить жанр" => "Уточни жанр, тон, темп и аудиторию будущей текстовой игры. Предложи 2-3 направления.",
            "Уточнить мир" => "Уточни мир, лор, конфликт, локации и ограничения сеттинга.",
            "Уточнить героя" => "Уточни роль игрока, героя, стартовую ситуацию и личную мотивацию.",
            "Уточнить механику" => "Уточни игровые механики: статы, навыки, инвентарь, выборы, отношения, боевку и прогрессию.",
            "Визуальный стиль" => "Уточни визуальный стиль иллюстраций для Fooocus: жанр изображения, цвет, композицию, запреты.",
            _ => button.Text
        };

        await SendDiscussionTextAsync(text);
    }

    private string? GetStructuredDiscussionPrompt(Button button)
    {
        if (ReferenceEquals(button, btnAskGenre))
        {
            return "Уточни жанр, тон, темп и аудиторию будущей текстовой игры. Предложи 2-3 направления.";
        }

        if (ReferenceEquals(button, btnAskWorld))
        {
            return "Уточни мир, лор, конфликт, локации и ограничения сеттинга.";
        }

        if (ReferenceEquals(button, btnAskHero))
        {
            return "Уточни роль игрока, героя, стартовую ситуацию и личную мотивацию.";
        }

        if (ReferenceEquals(button, btnAskMechanics))
        {
            return "Уточни игровые механики: статы, навыки, инвентарь, выборы, отношения, боевку и прогрессию.";
        }

        if (ReferenceEquals(button, btnAskVisualStyle))
        {
            return "Уточни визуальный стиль иллюстраций для Fooocus: жанр изображения, цвет, композицию, запреты.";
        }

        return null;
    }

    private async void btnBuildBrief_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.BuildingBrief);
            var text = await _pipelineService.BuildBriefAsync(project, GetLmSettingsForPurpose("brief"), _discussionSession, AppendLog, CurrentOperationToken);
            txtBriefConcept.Text = text;
            RefreshContentViews();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnBuildConcept_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.BuildingConcept);
            var text = await _pipelineService.BuildConceptAsync(project, GetLmSettingsForPurpose("concept"), AppendLog, CurrentOperationToken);
            txtBriefConcept.Text = text;
            RefreshContentViews();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnBuildMvp_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.BuildingMvp);
            var text = await _pipelineService.BuildMvpAsync(project, GetLmSettingsForPurpose("mvp"), AppendLog, CurrentOperationToken);
            txtBriefConcept.Text = text;
            RefreshContentViews();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnBuildStructure_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.BuildingStructure);
            var text = await _pipelineService.BuildGameStructureAsync(project, GetLmSettingsForPurpose("structure"), AppendLog, CurrentOperationToken);
            txtBriefConcept.Text = text;
            RefreshContentViews();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnGenerateContent_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.GeneratingContent);
            var text = await _pipelineService.BuildInitialContentAsync(project, GetLmSettingsForPurpose("initial-content"), AppendLog, CurrentOperationToken);
            txtBriefConcept.Text = text;
            _currentSave = _storageService.CreateInitialSave(project, "autosave");
            await _storageService.SaveProjectAsync(GetGamesRoot(), project);
            await _storageService.SaveProgressAsync(project, _currentSave, "autosave.json");
            RefreshAllViews();
        }, AppWorkflowStatus.Idle);
    }

    private void btnApproveBrief_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null) return;
        _currentProject.Brief.Text = txtBriefConcept.Text;
        _currentProject.Brief.Approved = true;
        AppendLog("Brief approved.");
        RefreshContentViews();
    }

    private void btnApproveConcept_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null) return;
        _currentProject.Concept.Text = txtBriefConcept.Text;
        _currentProject.Concept.Approved = true;
        AppendLog("Concept approved.");
        RefreshContentViews();
    }

    private async void btnApplyRevision_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            var revision = txtUserInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(revision))
            {
                MessageBox.Show(this, "Напишите правку в поле ввода на вкладке AI-обсуждение.", "Правка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetStatus(AppWorkflowStatus.GeneratingContent);
            var text = await _pipelineService.ApplyRevisionAsync(project, GetLmSettingsForPurpose("revision-fix"), revision, AppendLog, CurrentOperationToken);
            txtBriefConcept.Text = text;
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnBuildImagePrompts_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.PreparingPrompts);
            var text = await _pipelineService.BuildImagePromptPlanAsync(project, GetLmSettingsForPurpose("image-prompts"), AppendLog, CurrentOperationToken);
            txtPromptDetails.Text = text;
            RefreshGenerationPlanView();
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnSaveGenerationPreferences_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            return;
        }

        ReadGenerationPreferencesFromUi(_currentProject);
        await _storageService.SaveProjectAsync(GetGamesRoot(), _currentProject, CurrentOperationToken);
        AppendLog("Пожелания генерации сохранены в проект.");
        RefreshGenerationPlanView();
    }

    private async void btnGenerateStatsResourcesBatch_Click(object? sender, EventArgs e)
    {
        await RunBatchGenerationAsync(
            (project, settings, rules, count, category, log, token) =>
                _pipelineService.BuildStatsAndResourcesBatchAsync(project, settings, rules, count, category, log, token),
            "stats-resources");
    }

    private async void btnGenerateItemsBatch_Click(object? sender, EventArgs e)
    {
        await RunBatchGenerationAsync(
            (project, settings, rules, count, category, log, token) =>
                _pipelineService.BuildItemsBatchAsync(project, settings, rules, count, category, log, token),
            "items");
    }

    private async void btnGenerateEquipmentBatch_Click(object? sender, EventArgs e)
    {
        await RunBatchGenerationAsync(
            (project, settings, rules, count, category, log, token) =>
                _pipelineService.BuildEquipmentBatchAsync(project, settings, rules, count, category, log, token),
            "equipment");
    }

    private async void btnGenerateSkillsBatch_Click(object? sender, EventArgs e)
    {
        await RunBatchGenerationAsync(
            (project, settings, rules, count, category, log, token) =>
                _pipelineService.BuildSkillsBatchAsync(project, settings, rules, count, category, log, token),
            "skills");
    }

    private async void btnGenerateSpellsBatch_Click(object? sender, EventArgs e)
    {
        await RunBatchGenerationAsync(
            (project, settings, rules, count, category, log, token) =>
                _pipelineService.BuildSpellsBatchAsync(project, settings, rules, count, category, log, token),
            "spells");
    }

    private async void btnGenerateLocationsBatch_Click(object? sender, EventArgs e)
    {
        await RunBatchGenerationAsync(
            (project, settings, rules, count, category, log, token) =>
                _pipelineService.BuildLocationsBatchAsync(project, settings, rules, count, category, log, token),
            "locations");
    }

    private async void btnGenerateScenesBatch_Click(object? sender, EventArgs e)
    {
        await RunBatchGenerationAsync(
            (project, settings, rules, count, category, log, token) =>
                _pipelineService.BuildScenesBatchAsync(project, settings, rules, count, category, log, token),
            "scenes");
    }

    private async void btnGenerateEncountersBatch_Click(object? sender, EventArgs e)
    {
        await RunBatchGenerationAsync(
            (project, settings, rules, count, category, log, token) =>
                _pipelineService.BuildEncountersBatchAsync(project, settings, rules, count, category, log, token),
            "encounters");
    }

    private async Task RunBatchGenerationAsync(
        Func<GameProjectData, LmStudioSettings, string, int, string, Action<string>, CancellationToken, Task<string>> action,
        string defaultCategory)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.GeneratingContent);
            var rules = txtBatchRules.Text.Trim();
            var count = (int)nudBatchCount.Value;
            var category = string.IsNullOrWhiteSpace(cmbBatchCategory.Text) ? defaultCategory : cmbBatchCategory.Text.Trim();
            var text = await action(project, GetLmSettingsForPurpose("json-draft"), rules, count, category, AppendLog, CurrentOperationToken);
            txtPromptDetails.Text = text;
            await _storageService.SaveProjectAsync(GetGamesRoot(), project, CurrentOperationToken);
            RefreshAllViews();
        }, AppWorkflowStatus.Idle);
    }

    private void btnRefreshGenerationPlan_Click(object? sender, EventArgs e)
    {
        RefreshGenerationPlanView();
        _ = RefreshPipelineDraftInfoAsync();
    }

    private void btnCheckMechanics_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            return;
        }

        txtPipelineDetails.Text = _mechanicsReportService.BuildReport(_currentProject);
        AppendLog("Проверка механик выполнена локально.");
    }

    private async void btnRunSelectedPipelineStep_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            var step = GetSelectedPipelineStep();
            if (step == null)
            {
                MessageBox.Show(this, "Выберите этап в списке пайплайна.", "Пайплайн", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!step.CanRunFromPipeline)
            {
                MessageBox.Show(this, step.NextAction, "Пайплайн", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetStatus(AppWorkflowStatus.GeneratingContent);
            SaveCurrentPipelineRules();
            var count = (int)nudPipelineBatchCount.Value;
            var category = !string.IsNullOrWhiteSpace(step.BatchCategory)
                ? step.BatchCategory
                : cmbPipelineCategory.SelectedItem?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show(this, "Для выбранного этапа нет batch-категории.", "Пайплайн", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rules = txtPipelineRules.Text.Trim();
            string text;
            if (string.Equals(category, "image-prompts", StringComparison.OrdinalIgnoreCase))
            {
                text = await _pipelineService.BuildImagePromptPlanAsync(project, GetLmSettingsForPurpose("image-prompts"), AppendLog, CurrentOperationToken);
            }
            else
            {
                text = await RunPipelineBatchByCategoryAsync(project, category, rules, count);
                await _storageService.SaveProjectAsync(GetGamesRoot(), project, CurrentOperationToken);
            }

            txtPipelineDetails.Text = text;
            txtPromptDetails.Text = text;
            RefreshAllViews();
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnReviewLatestDraft_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            var draft = await _draftService.LoadLatestDraftAsync(project, CurrentOperationToken);
            if (draft == null)
            {
                MessageBox.Show(this, "Нет применяемого draft для проверки.", "Draft", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetStatus(AppWorkflowStatus.GeneratingContent);
            var rawText = await LoadDraftRawTextAsync(project, draft, CurrentOperationToken);
            var text = await _pipelineService.ReviewBatchAsync(project, GetLmSettingsForPurpose("review"), rawText, AppendLog, CurrentOperationToken);
            txtPipelineDetails.Text = text;
            txtPromptDetails.Text = text;
            await _draftService.SaveDraftReviewAsync(project, draft, text, CurrentOperationToken);
            AppendLog("Review сохранён в draft " + draft.SessionId);
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnApplyLatestDraft_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            var draft = await _draftService.LoadLatestDraftAsync(project, CurrentOperationToken);
            if (draft == null)
            {
                MessageBox.Show(this, "Нет применяемого draft.", "Draft", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!draft.Validation.IsValid)
            {
                MessageBox.Show(this, "Draft содержит ошибки валидации и не будет применён.", "Draft", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var confirmation = BuildDraftConfirmationText("Будет применён draft:", project, draft) + Environment.NewLine + "Применить?";
            if (MessageBox.Show(this, confirmation, "Draft", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            SetStatus(AppWorkflowStatus.Saving);
            var beforeCombat = BuildCombatImpactSnapshot(project);
            var beforeMvp = _mvpOrchestratorService.BuildReadinessReport(project);
            await _draftService.ApplyDraftAsync(project, draft, CurrentOperationToken);
            var afterCombat = BuildCombatImpactSnapshot(project);
            var afterMvp = _mvpOrchestratorService.BuildReadinessReport(project);
            await _storageService.SaveProjectAsync(GetGamesRoot(), project, CurrentOperationToken);
            AppendLog("Draft применён: " + draft.SessionId);
            AppendLog(BuildDraftApplyImpactText(draft, beforeMvp, afterMvp));
            AppendLog(BuildCombatApplyImpactText(draft, beforeCombat, afterCombat));
            RefreshAllViews();
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnRejectLatestDraft_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            var draft = await _draftService.LoadLatestDraftAsync(project, CurrentOperationToken);
            if (draft == null)
            {
                MessageBox.Show(this, "Нет применяемого draft.", "Draft", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var confirmation = BuildDraftConfirmationText("Отклонить draft?", draft);
            if (MessageBox.Show(this, confirmation, "Draft", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            await _draftService.RejectDraftAsync(project, draft, CurrentOperationToken);
            AppendLog("Draft отклонён: " + draft.SessionId);
            RefreshGenerationPlanView();
            await RefreshPipelineDraftInfoAsync();
        }, AppWorkflowStatus.Idle);
    }

    private void btnOpenDraftsFolderPipeline_Click(object? sender, EventArgs e)
    {
        OpenProjectSubfolder("drafts");
    }

    private async void btnOpenCurrentDraft_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            var draft = await _draftService.LoadLatestDraftAsync(project, CurrentOperationToken);
            if (draft == null)
            {
                MessageBox.Show(this, "Нет применяемого draft.", "Draft", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var path = ResolveDraftOpenPath(project, draft);
                if (string.IsNullOrWhiteSpace(path))
                {
                    MessageBox.Show(this, "У draft нет файлов для открытия.", "Draft", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                AppendLog("Открыт draft: " + path);
            }
            catch (Exception ex)
            {
                AppendLog("Не удалось открыть draft: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Draft", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }, AppWorkflowStatus.Idle);
    }

    private void btnApprovePrompt_Click(object? sender, EventArgs e)
    {
        var prompt = GetSelectedPrompt();
        if (prompt == null) return;
        prompt.Status = ImagePromptStatus.Queued;
        AppendLog("Prompt queued: " + prompt.Title);
        RefreshAssetViews();
    }

    private async void btnRunFooocusQueue_Click(object? sender, EventArgs e)
    {
        await EnsureProjectThenRunAsync(async project =>
        {
            SetStatus(AppWorkflowStatus.SwitchingToFooocus);
            await _orchestratorService.PrepareAndRunAssetQueueAsync(project, GetLmSettingsForPurpose("image-prompts"), GetFooocusSettings(), AppendLog, CurrentOperationToken);
            await _storageService.SaveProjectAsync(GetGamesRoot(), project);
            RefreshAssetViews();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnImportAssets_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null) return;
        SetStatus(AppWorkflowStatus.ImportingAssets);
        _fooocusService.ImportGeneratedImages(_currentProject, GetFooocusSettings(), AppendLog);
        await _storageService.SaveProjectAsync(GetGamesRoot(), _currentProject);
        RefreshAssetViews();
        SetStatus(AppWorkflowStatus.Idle);
    }

    private void btnSelectImage_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null) return;
        var prompt = GetSelectedPrompt();
        if (prompt == null) return;
        using var dialog = new OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.webp|All files|*.*",
            Title = "Выберите изображение для ассета"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _imageAssetService.LinkPromptToImage(_currentProject, prompt, dialog.FileName);
        _ = _changeLogService.AppendChangeAsync(_currentProject, new GameChangeRecord
        {
            Operation = "link",
            EntityType = prompt.TargetType.ToString(),
            EntityId = prompt.TargetEntityId,
            RelativePath = prompt.SelectedImagePath ?? string.Empty,
            CreatedBy = "user",
            ApprovedByUser = true,
            Notes = "Image linked to prompt " + prompt.AssetId
        });
        AppendLog("Image linked: " + prompt.AssetId);
        RefreshAllViews();
    }

    private void lvPrompts_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var prompt = GetSelectedPrompt();
        txtPromptDetails.Text = prompt == null ? "" : JsonSerializer.Serialize(prompt, UiJsonOptions);
    }

    private async void btnNewRun_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null) return;
        _currentSave = _storageService.CreateInitialSave(_currentProject, "autosave");
        _storageService.SyncSaveWithProject(_currentProject, _currentSave);
        await SaveAutosaveProgressAsync();
        RefreshRuntimeViews();
    }

    private void btnOpenPlayWindow_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null)
        {
            MessageBox.Show(this, "Сначала откройте проект.", "Игра", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _currentSave ??= _storageService.CreateInitialSave(_currentProject, "autosave");
        _storageService.SyncSaveWithProject(_currentProject, _currentSave);
        using var form = new PlayForm(_currentProject, _currentSave, _runtimeEngine, _storageService);
        form.ShowDialog(this);
        RefreshRuntimeViews();
    }

    private async void btnSaveProgress_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null || _currentSave == null) return;
        await RunSafeAsync(async () =>
        {
            SetStatus(AppWorkflowStatus.Saving);
            var fileName = $"save_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            await _storageService.SaveProgressAsync(_currentProject, _currentSave, fileName);
            AppendLog("Progress saved: " + fileName);
            RefreshSaves();
        }, AppWorkflowStatus.Idle);
    }

    private async void btnLoadProgress_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null || lstSaves.SelectedItem is not string fileName) return;
        await RunSafeAsync(async () =>
        {
            SetStatus(AppWorkflowStatus.Loading);
            _currentSave = await _storageService.LoadProgressAsync(_currentProject, fileName);
            AppendLog("Progress loaded: " + fileName);
            RefreshRuntimeViews();
        }, AppWorkflowStatus.Idle);
    }

    private void btnDeleteSave_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null || lstSaves.SelectedItem is not string fileName) return;
        var path = Path.Combine(_currentProject.Summary.ProjectPath, "saves", fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            AppendLog("Save deleted: " + fileName);
            RefreshSaves();
        }
    }

    private void btnBrowseFooocusLaunch_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Executable or script|*.exe;*.bat;*.cmd;*.ps1|All files|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtFooocusLaunch.Text = dialog.FileName;
            txtFooocusWorkingDir.Text = Path.GetDirectoryName(dialog.FileName) ?? "";
            if (string.IsNullOrWhiteSpace(txtFooocusOutput.Text))
            {
                DetectFooocusFromFolder(txtFooocusWorkingDir.Text);
            }
            SaveSettingsFromUi();
        }
    }

    private void btnBrowseFooocusOutput_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Папка результатов Fooocus", UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtFooocusOutput.Text = dialog.SelectedPath;
            SaveSettingsFromUi();
        }
    }

    private void btnBrowseFooocusFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Выберите папку Fooocus_win64-2-5-0 или папку Fooocus",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(txtFooocusWorkingDir.Text) ? txtFooocusWorkingDir.Text : Environment.CurrentDirectory
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            DetectFooocusFromFolder(dialog.SelectedPath);
            SaveSettingsFromUi();
        }
    }

    private void btnDetectFooocus_Click(object? sender, EventArgs e)
    {
        var folder = Directory.Exists(txtFooocusWorkingDir.Text)
            ? txtFooocusWorkingDir.Text
            : Path.GetDirectoryName(txtFooocusLaunch.Text) ?? "";
        DetectFooocusFromFolder(folder);
        SaveSettingsFromUi();
    }

    private void btnCheckFooocusPaths_Click(object? sender, EventArgs e)
    {
        var launchOk = !string.IsNullOrWhiteSpace(txtFooocusLaunch.Text) && File.Exists(txtFooocusLaunch.Text);
        var workingOk = !string.IsNullOrWhiteSpace(txtFooocusWorkingDir.Text) && Directory.Exists(txtFooocusWorkingDir.Text);
        var output = txtFooocusOutput.Text.Trim();
        var outputOk = !string.IsNullOrWhiteSpace(output);
        if (outputOk)
        {
            Directory.CreateDirectory(output);
        }

        AppendLog($"Fooocus paths: launch={(launchOk ? "OK" : "missing")}, workingDir={(workingOk ? "OK" : "missing")}, output={(outputOk ? "OK" : "missing")}.");
    }

    private void btnSaveSettings_Click(object? sender, EventArgs e)
    {
        SaveSettingsFromUi();
        AppendLog("Settings saved: " + _settingsStore.SettingsPath);
    }

    private void cmbLmProfiles_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_loadingSettingsUi || cmbLmProfiles.SelectedItem is not LmStudioModelProfile profile)
        {
            return;
        }

        SaveActiveProfileFromUi();
        _appSettings.ActiveLmStudioProfileId = profile.Id;
        _lmProfileService.ApplyProfileToLegacySettings(_appSettings, profile);
        ApplyLmProfileToUi(profile);
        RefreshLmProfileList(profile.Id);
    }

    private void cmbLmProfiles_Format(object? sender, ListControlConvertEventArgs e)
    {
        if (e.ListItem is not LmStudioModelProfile profile)
        {
            return;
        }

        var markers = new List<string>();
        if (string.Equals(profile.Id, _appSettings.ActiveLmStudioProfileId, StringComparison.OrdinalIgnoreCase))
        {
            markers.Add("активный");
        }
        if (profile.IsDefault)
        {
            markers.Add("default");
        }
        markers.Add(profile.Role);
        e.Value = $"{profile.Name} [{string.Join(", ", markers)}]";
    }

    private void btnAddLmProfile_Click(object? sender, EventArgs e)
    {
        SaveActiveProfileFromUi();
        var profile = new LmStudioModelProfile
        {
            Id = "profile_" + Guid.NewGuid().ToString("N")[..8],
            Name = "Новый LM Studio профиль",
            Role = LmStudioProfileRole.Default,
            Settings = GetLmSettingsFromUi(),
            Generation = GetGenerationSettingsFromUi()
        };
        _appSettings.LmStudioProfiles.Add(profile);
        _appSettings.ActiveLmStudioProfileId = profile.Id;
        _lmProfileService.NormalizeProfiles(_appSettings);
        ApplyLmProfileToUi(profile);
        RefreshLmProfileList(profile.Id);
        AppendLog("Добавлен LM Studio профиль: " + profile.Name);
    }

    private void btnSaveLmProfile_Click(object? sender, EventArgs e)
    {
        SaveActiveProfileFromUi();
        _settingsStore.Save(_appSettings);
        RefreshLmProfileList(_appSettings.ActiveLmStudioProfileId);
        AppendLog("LM Studio профиль сохранён: " + (_lmProfileService.GetActiveProfile(_appSettings).Name));
    }

    private void btnDeleteLmProfile_Click(object? sender, EventArgs e)
    {
        var selectedId = (cmbLmProfiles.SelectedItem as LmStudioModelProfile)?.Id ?? _appSettings.ActiveLmStudioProfileId;
        _lmProfileService.DeleteProfile(_appSettings, selectedId);
        var active = _lmProfileService.GetActiveProfile(_appSettings);
        ApplyLmProfileToUi(active);
        RefreshLmProfileList(active.Id);
        AppendLog("LM Studio профиль удалён или оставлен как единственный активный.");
    }

    private void btnSetDefaultLmProfile_Click(object? sender, EventArgs e)
    {
        SaveActiveProfileFromUi();
        var selectedId = (cmbLmProfiles.SelectedItem as LmStudioModelProfile)?.Id ?? _appSettings.ActiveLmStudioProfileId;
        foreach (var profile in _appSettings.LmStudioProfiles)
        {
            profile.IsDefault = string.Equals(profile.Id, selectedId, StringComparison.OrdinalIgnoreCase);
        }

        _appSettings.ActiveLmStudioProfileId = selectedId;
        _lmProfileService.NormalizeProfiles(_appSettings);
        RefreshLmProfileList(selectedId);
        AppendLog("Профиль LM Studio назначен default: " + _lmProfileService.GetActiveProfile(_appSettings).Name);
    }

    private void btnStopOperation_Click(object? sender, EventArgs e)
    {
        _currentOperationCts?.Cancel();
        AppendLog("Stop requested for current operation.");
    }

    private async Task SendDiscussionTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, "Введите текст для модели.", "Пустой ввод", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await EnsureProjectThenRunAsync(async project =>
        {
            txtUserInput.Clear();
            SetStatus(AppWorkflowStatus.Discussing);
            AppendDiscussion("Пользователь", text);
            var answer = await _pipelineService.ContinueDiscussionAsync(project, GetLmSettingsForPurpose("discussion"), _discussionSession, text, AppendLog, CurrentOperationToken);
            AppendDiscussion("Модель", answer);
        }, AppWorkflowStatus.Idle);
    }

    private async Task LoadProjectAsync(string projectPath)
    {
        await RunSafeAsync(async () =>
        {
            SetStatus(AppWorkflowStatus.Loading);
            _currentProject = await _storageService.LoadProjectAsync(projectPath);
            var autosave = Path.Combine(_currentProject.Summary.ProjectPath, "saves", "autosave.json");
            _currentSave = File.Exists(autosave)
                ? await _storageService.LoadProgressAsync(_currentProject, "autosave.json")
                : _storageService.CreateInitialSave(_currentProject, "autosave");
            _storageService.SyncSaveWithProject(_currentProject, _currentSave);
            await _storageService.SaveProgressAsync(_currentProject, _currentSave, "autosave.json");
            AppendLog("Project loaded: " + _currentProject.Meta.Title);
            RefreshAllViews();
        }, AppWorkflowStatus.Idle);
    }

    private async Task SaveCurrentProjectAsync()
    {
        if (_currentProject == null)
        {
            return;
        }

        await RunSafeAsync(async () =>
        {
            SetStatus(AppWorkflowStatus.Saving);
            await _storageService.SaveProjectAsync(GetGamesRoot(), _currentProject);
            if (_currentSave != null)
            {
                await _storageService.SaveProgressAsync(_currentProject, _currentSave, "autosave.json");
            }
            AppendLog("Project saved: " + _currentProject.Meta.Title);
            RefreshProjectList();
            RefreshSaves();
        }, AppWorkflowStatus.Idle);
    }

    private async Task EnsureProjectThenRunAsync(Func<GameProjectData, Task> action, AppWorkflowStatus finalStatus)
    {
        if (_currentProject == null)
        {
            MessageBox.Show(this, "Сначала создайте или откройте проект.", "Проект", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await RunSafeAsync(() => action(_currentProject), finalStatus);
    }

    private async Task RunSafeAsync(Func<Task> action, AppWorkflowStatus finalStatus)
    {
        _currentOperationCts?.Dispose();
        _currentOperationCts = new CancellationTokenSource();
        try
        {
            SetBusy(true);
            await action();
        }
        catch (OperationCanceledException)
        {
            AppendLog("Operation cancelled.");
            SetStatus(AppWorkflowStatus.Idle);
        }
        catch (Exception ex)
        {
            SetStatus(AppWorkflowStatus.Error);
            AppendLog("Ошибка: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            if (_status != AppWorkflowStatus.Error)
            {
                SetStatus(finalStatus);
            }
            _currentOperationCts.Dispose();
            _currentOperationCts = null;
        }
    }

    private CancellationToken CurrentOperationToken => _currentOperationCts?.Token ?? CancellationToken.None;

    private void RefreshProjectList()
    {
        var selectedId = (lstProjects.SelectedItem as GameProjectSummary)?.Id;
        lstProjects.DataSource = null;
        lstProjects.DataSource = _storageService.ListProjects(GetGamesRoot());
        lstProjects.DisplayMember = nameof(GameProjectSummary.Title);
        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            var index = lstProjects.Items.Cast<GameProjectSummary>().ToList().FindIndex(x => x.Id == selectedId);
            if (index >= 0) lstProjects.SelectedIndex = index;
        }
    }

    private void RefreshAllViews()
    {
        pgProject.SelectedObject = _currentProject?.Meta;
        RefreshDesignBrainView();
        RefreshBriefView();
        RefreshGenerationPreferencesView();
        RefreshContentViews();
        RefreshGenerationPlanView();
        _ = RefreshPipelineDraftInfoAsync();
        RefreshAssetViews();
        RefreshRuntimeViews();
        RefreshSaves();
    }

    private void RefreshDesignBrainView()
    {
        lvDesignSlots.Items.Clear();
        lvDesignQuestions.Items.Clear();
        txtDesignPreview.Clear();

        if (_currentProject == null)
        {
            txtGameCrafterIdea.Clear();
            return;
        }

        _designInterviewService.EnsureProfile(_currentProject.DesignProfile);
        txtGameCrafterIdea.Text = _currentProject.DesignProfile.InitialIdea;
        SelectCreationMode(_currentProject.DesignProfile.CreationMode);

        foreach (var slot in _currentProject.DesignProfile.Slots.OrderBy(x => x.Priority).ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
        {
            var item = new ListViewItem(slot.Id);
            item.SubItems.Add(slot.Value);
            item.SubItems.Add(slot.Source.ToString());
            item.SubItems.Add(slot.Confidence.ToString("0.##"));
            item.SubItems.Add(slot.IsRequired ? "да" : "нет");
            item.Tag = slot;
            lvDesignSlots.Items.Add(item);
        }

        foreach (var question in _designInterviewService.GetQuestions(_currentProject.DesignProfile))
        {
            var item = new ListViewItem(question.SlotId);
            item.SubItems.Add(question.Question);
            item.Tag = question;
            lvDesignQuestions.Items.Add(item);
        }

        txtDesignPreview.Text = BuildDesignPreview(_currentProject);
    }

    private void SelectDesignSlot(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
        {
            return;
        }

        foreach (ListViewItem item in lvDesignSlots.Items)
        {
            if (item.Tag is GameDesignSlot slot && string.Equals(slot.Id, slotId, StringComparison.OrdinalIgnoreCase))
            {
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
                break;
            }
        }
    }

    private string BuildDesignPreview(GameProjectData project)
    {
        var builder = new StringBuilder();
        builder.AppendLine(_designInterviewService.BuildDesignSummary(project));
        if (project.CreationPlan.Steps.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("=== План создания ===");
            foreach (var step in project.CreationPlan.Steps.OrderBy(x => x.Priority))
            {
                builder.AppendLine($"{step.Priority:000} {step.Id}: {step.Title}");
                builder.AppendLine(step.Description);
            }
        }

        return builder.ToString();
    }

    private int ParseRandomDirectorEventCount()
    {
        return int.TryParse(txtRandomDirectorEventCount.Text.Trim(), out var count)
            ? Math.Clamp(count, 1, 30)
            : 8;
    }

    private int ParseBalanceSimulationRunCount()
    {
        return int.TryParse(txtBalanceSimulationRuns.Text.Trim(), out var count)
            ? Math.Clamp(count, 1, 100)
            : 30;
    }

    private void RefreshBriefView()
    {
        if (_currentProject == null)
        {
            txtBriefConcept.Clear();
            return;
        }

        txtBriefConcept.Text = $"=== БРИФ ===\r\n{_currentProject.Brief.Text}\r\n\r\n=== КОНЦЕПТ ===\r\n{_currentProject.Concept.Text}\r\n\r\n=== MVP ===\r\n{_currentProject.MvpPlan.Text}\r\n\r\n=== СТРУКТУРА ===\r\n{_currentProject.ArchitecturePlan.Text}";
    }

    private void RefreshContentViews()
    {
        if (_currentProject == null) return;
        txtWorld.Text = JsonSerializer.Serialize(_currentProject.World, UiJsonOptions);
        FillList(lvCharacters, _currentProject.Characters.Select(x => (x.Id, x.Name, x.Description)));
        FillList(lvScenes, _currentProject.Scenes.Select(x => (x.Id, x.Title, x.Text)));
        FillList(lvItems, _currentProject.Items.Select(x => (x.Id, x.Name, x.Description)));
        FillList(lvStats, _currentProject.Stats.Select(x => (x.Id, x.Name, x.Description)));
        FillList(lvRelationships, _currentProject.Relationships.Select(x => (x.CharacterId, x.Name, x.InitialValue.ToString())));
        txtCombat.Text = JsonSerializer.Serialize(_currentProject.Combat, UiJsonOptions);
    }

    private void RefreshGenerationPreferencesView()
    {
        if (_currentProject == null)
        {
            txtPreferenceGeneral.Clear();
            txtPreferenceSkills.Clear();
            txtPreferenceProgression.Clear();
            txtPreferenceCombat.Clear();
            txtPreferenceAtmosphere.Clear();
            txtPreferenceBalance.Clear();
            txtPreferenceForbidden.Clear();
            txtPreferenceNotes.Clear();
            return;
        }

        txtPreferenceGeneral.Text = _currentProject.GenerationPreferences.GeneralGameplayText;
        txtPreferenceSkills.Text = _currentProject.GenerationPreferences.SkillDesignText;
        txtPreferenceProgression.Text = _currentProject.GenerationPreferences.ProgressionDesignText;
        txtPreferenceCombat.Text = _currentProject.GenerationPreferences.CombatDesignText;
        txtPreferenceAtmosphere.Text = _currentProject.GenerationPreferences.AtmosphereDesignText;
        txtPreferenceBalance.Text = _currentProject.GenerationPreferences.BalanceText;
        txtPreferenceForbidden.Text = _currentProject.GenerationPreferences.ForbiddenDesignText;
        txtPreferenceNotes.Text = _currentProject.GenerationPreferences.Notes;
    }

    private void ReadGenerationPreferencesFromUi(GameProjectData project)
    {
        project.GenerationPreferences.GeneralGameplayText = txtPreferenceGeneral.Text.Trim();
        project.GenerationPreferences.SkillDesignText = txtPreferenceSkills.Text.Trim();
        project.GenerationPreferences.ProgressionDesignText = txtPreferenceProgression.Text.Trim();
        project.GenerationPreferences.CombatDesignText = txtPreferenceCombat.Text.Trim();
        project.GenerationPreferences.AtmosphereDesignText = txtPreferenceAtmosphere.Text.Trim();
        project.GenerationPreferences.BalanceText = txtPreferenceBalance.Text.Trim();
        project.GenerationPreferences.ForbiddenDesignText = txtPreferenceForbidden.Text.Trim();
        project.GenerationPreferences.Notes = txtPreferenceNotes.Text.Trim();
    }

    private void ReadGenerationSettingsFromUi()
    {
        _appSettings.Generation = GetGenerationSettingsFromUi();
    }

    private void RefreshAssetViews()
    {
        lvPrompts.Items.Clear();
        if (_currentProject == null) return;
        foreach (var prompt in _currentProject.ImagePrompts)
        {
            var item = new ListViewItem(prompt.AssetId);
            item.SubItems.Add(prompt.TargetType.ToString());
            item.SubItems.Add(prompt.Status.ToString());
            item.Tag = prompt;
            lvPrompts.Items.Add(item);
        }
    }

    private void RefreshGenerationPlanView()
    {
        lvGenerationPlan.Items.Clear();
        if (_currentProject == null)
        {
            txtPipelineDetails.Clear();
            txtPipelineDraftInfo.Text = "Текущий draft: проект не открыт";
            return;
        }

        foreach (var step in _workflowService.BuildSteps(_currentProject))
        {
            var item = new ListViewItem(step.Order.ToString("00"));
            item.SubItems.Add(step.Title);
            item.SubItems.Add(step.Status);
            item.SubItems.Add(step.CurrentState);
            item.SubItems.Add(step.NextAction);
            item.Tag = step;
            lvGenerationPlan.Items.Add(item);
        }
    }

    private void lvGenerationPlan_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var step = GetSelectedPipelineStep();
        if (step == null)
        {
            return;
        }

        SetPipelineCategory(step.BatchCategory);
        txtPipelineDetails.Text = $"{step.Order:00}. {step.Title}{Environment.NewLine}Статус: {step.Status}{Environment.NewLine}{step.CurrentState}{Environment.NewLine}{Environment.NewLine}{step.NextAction}";
    }

    private void SaveCurrentPipelineRules()
    {
        if (!string.IsNullOrWhiteSpace(_activePipelineRulesCategory))
        {
            _pipelineRulesByCategory[_activePipelineRulesCategory] = txtPipelineRules.Text;
        }
    }

    private void SetPipelineCategory(string category)
    {
        SaveCurrentPipelineRules();

        _activePipelineRulesCategory = category ?? string.Empty;
        if (string.IsNullOrWhiteSpace(category))
        {
            cmbPipelineCategory.SelectedIndex = -1;
            txtPipelineRules.Clear();
            return;
        }

        var index = cmbPipelineCategory.Items.IndexOf(category);
        if (index >= 0)
        {
            cmbPipelineCategory.SelectedIndex = index;
        }
        else
        {
            AppendLog("Неизвестная категория пайплайна: " + category);
            cmbPipelineCategory.SelectedIndex = -1;
        }

        txtPipelineRules.Text = _pipelineRulesByCategory.TryGetValue(category, out var savedRules)
            ? savedRules
            : GetPipelineRuleHint(category);
    }

    private async Task RefreshPipelineDraftInfoAsync()
    {
        if (_currentProject == null)
        {
            txtPipelineDraftInfo.Text = "Текущий draft: проект не открыт";
            return;
        }

        try
        {
            var draft = await _draftService.LoadLatestDraftAsync(_currentProject, CurrentOperationToken);
            txtPipelineDraftInfo.Text = draft == null
                ? "Текущий draft: нет применяемых draft-ов"
                : BuildDraftInfoText(draft);
        }
        catch (Exception ex)
        {
            txtPipelineDraftInfo.Text = "Текущий draft: не удалось загрузить (" + ex.Message + ")";
        }
    }

    private static string BuildDraftInfoText(GameDraftSession draft)
    {
        var statuses = draft.Files
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Status) ? "без статуса" : x.Status)
            .Select(x => $"{x.Key}: {x.Count()}");

        var builder = new StringBuilder();
        builder.AppendLine("Текущий draft:");
        builder.AppendLine("Stage: " + draft.Stage);
        builder.AppendLine("SessionId: " + draft.SessionId);
        builder.AppendLine("Создан UTC: " + draft.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"));
        builder.AppendLine("Файлов: " + draft.Files.Count + " (" + string.Join(", ", statuses) + ")");
        builder.AppendLine("Ошибок: " + draft.Validation.Errors.Count + ", предупреждений: " + draft.Validation.Warnings.Count);
        builder.AppendLine("Review: " + (string.IsNullOrWhiteSpace(draft.ReviewOutputFile) ? "нет" : draft.ReviewOutputFile));

        foreach (var file in draft.Files.Take(8))
        {
            builder.AppendLine("- " + file.EntityType + ": " + file.EntityId + " [" + file.Status + "]");
        }
        if (draft.Files.Count > 8)
        {
            builder.AppendLine("... ещё " + (draft.Files.Count - 8));
        }

        return builder.ToString();
    }

    private static string BuildDraftConfirmationText(string title, GameDraftSession draft)
    {
        return title + Environment.NewLine
            + "Stage: " + draft.Stage + Environment.NewLine
            + "SessionId: " + draft.SessionId + Environment.NewLine
            + "Файлов: " + draft.Files.Count + Environment.NewLine
            + "Ошибок: " + draft.Validation.Errors.Count + ", предупреждений: " + draft.Validation.Warnings.Count + Environment.NewLine;
    }

    private string BuildDraftConfirmationText(string title, GameProjectData project, GameDraftSession draft)
    {
        return BuildDraftConfirmationText(title, draft)
            + BuildDraftApplySummary(project, draft);
    }

    private string BuildDraftApplySummary(GameProjectData project, GameDraftSession draft)
    {
        var summary = ReadDraftCombatSummary(project, draft);
        var fileCounts = draft.Files
            .GroupBy(x => x.EntityType)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => "- " + x.Key + ": +" + x.Count());
        var builder = new StringBuilder();
        builder.AppendLine();
        builder.AppendLine("Draft stage: " + draft.Stage);
        builder.AppendLine("Will add/update:");
        foreach (var line in fileCounts)
        {
            builder.AppendLine(line);
        }
        builder.AppendLine("- combat.enabled: " + (summary.CombatEnabled?.ToString().ToLowerInvariant() ?? "нет данных"));
        builder.AppendLine("- actions: +" + summary.Actions);
        builder.AppendLine("- availableInCombat actions: +" + summary.CombatActions);
        builder.AppendLine("- encounters: +" + summary.Encounters);
        builder.AppendLine("- combatants: +" + summary.Combatants);
        if (draft.Validation.Warnings.Count > 0 || summary.Warnings.Count > 0)
        {
            builder.AppendLine("Warnings:");
            foreach (var warning in draft.Validation.Warnings.Concat(summary.Warnings).Take(8))
            {
                builder.AppendLine("- " + warning);
            }
            if (draft.Validation.Warnings.Count + summary.Warnings.Count > 8)
            {
                builder.AppendLine("- ... ещё " + (draft.Validation.Warnings.Count + summary.Warnings.Count - 8));
            }
        }

        return builder.ToString();
    }

    private static string BuildDraftApplyImpactText(GameDraftSession draft, GameMvpReadinessReport before, GameMvpReadinessReport after)
    {
        var stage = NormalizeDraftStageForMvp(draft.Stage);
        var beforeStage = before.Stages.FirstOrDefault(x => string.Equals(x.Stage, stage, StringComparison.OrdinalIgnoreCase));
        var afterStage = after.Stages.FirstOrDefault(x => string.Equals(x.Stage, stage, StringComparison.OrdinalIgnoreCase));
        var builder = new StringBuilder();
        builder.AppendLine("Draft applied: " + draft.SessionId);
        if (beforeStage != null && afterStage != null)
        {
            builder.AppendLine("MVP stage " + stage + ": " + beforeStage.ExistingCount + "/" + beforeStage.TargetMinimum
                + " -> " + afterStage.ExistingCount + "/" + afterStage.TargetMinimum
                + ", " + (afterStage.IsSatisfied ? "satisfied" : "not satisfied"));
        }

        builder.AppendLine("Next stage: " + (after.NextRecommendedStage ?? "none"));
        return builder.ToString().TrimEnd();
    }

    private static string NormalizeDraftStageForMvp(string stage)
    {
        return stage switch
        {
            "stats-resources" => "stats_resources",
            "gameplay-actions" => "actions",
            "world-state" => "world_state",
            "random-director" or "random-events" or "ambient-events" => "random_events",
            _ => stage.Replace('-', '_')
        };
    }

    private DraftCombatSummary ReadDraftCombatSummary(GameProjectData project, GameDraftSession draft)
    {
        var summary = new DraftCombatSummary();
        foreach (var file in draft.Files)
        {
            var path = Path.Combine(project.Summary.ProjectPath, file.RelativePath);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                switch (file.EntityType)
                {
                    case "combat":
                        var combat = JsonSerializer.Deserialize<GameCombatDefinition>(File.ReadAllText(path), _jsonOptions);
                        summary.CombatEnabled = combat?.Enabled;
                        break;
                    case "actions":
                        summary.Actions++;
                        var action = JsonSerializer.Deserialize<GameActionDefinition>(File.ReadAllText(path), _jsonOptions);
                        if (action?.AvailableInCombat == true)
                        {
                            summary.CombatActions++;
                        }
                        break;
                    case "encounters":
                        summary.Encounters++;
                        var encounter = JsonSerializer.Deserialize<GameEncounterDefinition>(File.ReadAllText(path), _jsonOptions);
                        if (encounter != null)
                        {
                            summary.Combatants += encounter.Combatants.Count;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                summary.Warnings.Add(file.EntityType + "/" + file.EntityId + ": не удалось прочитать summary (" + ex.Message + ")");
            }
        }

        return summary;
    }

    private static CombatImpactSnapshot BuildCombatImpactSnapshot(GameProjectData project)
    {
        return new CombatImpactSnapshot(
            project.Combat?.Enabled == true,
            project.Actions.Count,
            project.Actions.Count(x => x.AvailableInCombat),
            project.Encounters.Count,
            project.Encounters.Count(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) || x.Combatants.Count > 0),
            project.Encounters.SelectMany(x => x.Combatants).Count());
    }

    private static string BuildCombatApplyImpactText(GameDraftSession draft, CombatImpactSnapshot before, CombatImpactSnapshot after)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Impact: actions +" + Math.Max(0, after.Actions - before.Actions)
            + ", combatActions +" + Math.Max(0, after.CombatActions - before.CombatActions)
            + ", encounters +" + Math.Max(0, after.Encounters - before.Encounters)
            + ", combat.enabled=" + after.CombatEnabled.ToString().ToLowerInvariant());
        builder.AppendLine("Impact details: combat encounters " + before.CombatEncounters + " -> " + after.CombatEncounters
            + ", combatants " + before.Combatants + " -> " + after.Combatants + ".");

        if (string.Equals(draft.Stage, "combat", StringComparison.OrdinalIgnoreCase))
        {
            var satisfied = after.CombatEnabled || after.CombatActions > 0 || after.CombatEncounters > 0;
            builder.AppendLine("MVP stage combat: " + (satisfied ? "satisfied" : "not satisfied"));
            if (!satisfied)
            {
                builder.AppendLine("Applied combat draft did not satisfy combat stage: no combat.enabled, no availableInCombat actions, no combat encounters.");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private sealed class DraftCombatSummary
    {
        public bool? CombatEnabled { get; set; }
        public int Actions { get; set; }
        public int CombatActions { get; set; }
        public int Encounters { get; set; }
        public int Combatants { get; set; }
        public List<string> Warnings { get; } = new();
    }

    private sealed record CombatImpactSnapshot(
        bool CombatEnabled,
        int Actions,
        int CombatActions,
        int Encounters,
        int CombatEncounters,
        int Combatants);

    private void RefreshRuntimeViews()
    {
        pnlChoices.Controls.Clear();
        picScene.Image = null;
        if (_currentProject == null || _currentSave == null)
        {
            lblGameTitle.Text = "Нет открытой игры";
            lblSceneTitle.Text = "";
            txtSceneText.Text = "";
            return;
        }

        SetStatus(AppWorkflowStatus.Playing);
        var scene = _runtimeEngine.GetCurrentScene(_currentProject, _currentSave);
        lblGameTitle.Text = _currentProject.Meta.Title;
        lblSceneTitle.Text = scene.Title;
        txtSceneText.Text = scene.Text;
        LoadSceneImage(scene);

        if (_currentSave.Combat.IsActive)
        {
            var button = new Button
            {
                Text = "Идёт бой. Выберите действие и цель на вкладке 'Бой' или нажмите 'Конец хода'.",
                Width = 820,
                Height = 38,
                Enabled = false
            };
            pnlChoices.Controls.Add(button);
        }
        else
        {
            foreach (var choice in _runtimeEngine.GetAvailableChoices(_currentProject, _currentSave))
            {
                var button = new Button
                {
                    Text = choice.Text,
                    Width = 520,
                    Height = 38,
                    Tag = choice.Id
                };
                button.Click += ChoiceButton_Click;
                pnlChoices.Controls.Add(button);
            }
        }

        FillList(lvRuntimeStats, BuildRuntimeCharacterRows(_currentProject, _currentSave));
        FillList(lvRuntimeInventory, _runtimeEngine.GetInventory(_currentProject, _currentSave).Select(x => (x.ItemId, FindName(_currentProject.Items, x.ItemId), x.IsEquipped ? "надето" : x.Quantity.ToString())), ("empty", "Инвентарь пуст", "Нет предметов"));
        FillList(lvRuntimeRelationships, _currentSave.Relationships.Select(x => (x.Key, FindRelationshipName(_currentProject, x.Key), x.Value.ToString())), ("empty", "Нет отношений", "Нет данных"));
        FillList(lvRuntimeQuests, _currentSave.ActiveQuestIds.Select(x => (x, FindQuestName(_currentProject, x), "active")), ("empty", "Активных заданий нет", "Нет данных"));
        RefreshRuntimeCombatTab();
        if (_currentSave.Combat.IsActive && tabRuntimeInfo.TabPages.Contains(tabRuntimeCombatPage))
        {
            tabRuntimeInfo.SelectedTab = tabRuntimeCombatPage;
        }
        txtRuntimeLog.Text = string.Join(Environment.NewLine, _currentSave.EventLog);
        SetStatus(AppWorkflowStatus.Idle);
    }

    private async void ChoiceButton_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null || _currentSave == null || sender is not Button { Tag: string choiceId }) return;
        if (_currentSave.Combat.IsActive)
        {
            AppendLog("Сейчас идёт бой. Используйте вкладку 'Бой'.");
            RefreshRuntimeViews();
            return;
        }

        var result = _runtimeEngine.ApplyChoiceWithResult(_currentProject, _currentSave, choiceId);
        AddOperationLog(result);
        if (result.Success)
        {
            await SaveAutosaveProgressAsync();
        }
        RefreshRuntimeViews();
    }

    private async void btnRuntimeExecuteCombatAction_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null || _currentSave == null || lvRuntimeCombatActions.SelectedItems.Count == 0 || lvRuntimeCombatants.SelectedItems.Count == 0)
        {
            return;
        }

        var actionId = lvRuntimeCombatActions.SelectedItems[0].Tag as string ?? lvRuntimeCombatActions.SelectedItems[0].Text;
        var targetRuntimeId = lvRuntimeCombatants.SelectedItems[0].Tag as string ?? lvRuntimeCombatants.SelectedItems[0].Text;
        var result = _runtimeEngine.ExecuteCombatActionWithResult(_currentProject, _currentSave, actionId, targetRuntimeId);
        AddOperationLog(result);
        if (result.Success)
        {
            await SaveAutosaveProgressAsync();
        }
        RefreshRuntimeViews();
    }

    private async void btnRuntimeEndCombatTurn_Click(object? sender, EventArgs e)
    {
        if (_currentProject == null || _currentSave == null)
        {
            return;
        }

        var result = _runtimeEngine.EndCombatTurnWithResult(_currentProject, _currentSave);
        AddOperationLog(result);
        if (result.Success)
        {
            await SaveAutosaveProgressAsync();
        }
        RefreshRuntimeViews();
    }

    private void lvRuntimeCombatSelectionChanged(object? sender, EventArgs e)
    {
        RefreshRuntimeCombatButtons();
    }

    private void RefreshRuntimeCombatTab()
    {
        if (_currentProject == null || _currentSave == null)
        {
            return;
        }

        lvRuntimeCombatants.Items.Clear();
        lvRuntimeCombatActions.Items.Clear();
        var healthStat = string.IsNullOrWhiteSpace(_currentProject.Combat?.PlayerHealthStatId) ? "health" : _currentProject.Combat.PlayerHealthStatId;
        foreach (var combatant in _runtimeEngine.GetCombatants(_currentProject, _currentSave))
        {
            var item = new ListViewItem(string.IsNullOrWhiteSpace(combatant.Name) ? combatant.RuntimeId : combatant.Name);
            item.SubItems.Add(combatant.Team);
            item.SubItems.Add(combatant.Stats.GetValueOrDefault(healthStat).ToString());
            item.SubItems.Add(combatant.Initiative.ToString());
            item.SubItems.Add(string.Join(", ", combatant.ActiveStatusEffects.Select(x => x.StatusEffectId)));
            item.Tag = combatant.RuntimeId;
            lvRuntimeCombatants.Items.Add(item);
        }

        var actor = _runtimeEngine.GetCurrentCombatant(_currentProject, _currentSave);
        foreach (var action in _runtimeEngine.GetAvailableCombatActions(_currentProject, _currentSave, actor))
        {
            var item = new ListViewItem(string.IsNullOrWhiteSpace(action.Name) ? action.Id : action.Name);
            item.SubItems.Add(action.TargetScope);
            item.SubItems.Add(action.Description);
            item.Tag = action.Id;
            lvRuntimeCombatActions.Items.Add(item);
        }

        SelectDefaultRuntimeCombatItems();
        RefreshRuntimeCombatButtons();
    }

    private void SelectDefaultRuntimeCombatItems()
    {
        if (_currentProject == null || _currentSave == null || !_currentSave.Combat.IsActive)
        {
            return;
        }

        if (lvRuntimeCombatActions.SelectedItems.Count == 0 && lvRuntimeCombatActions.Items.Count > 0)
        {
            lvRuntimeCombatActions.Items[0].Selected = true;
        }

        var actor = _runtimeEngine.GetCurrentCombatant(_currentProject, _currentSave);
        var selectedActionId = lvRuntimeCombatActions.SelectedItems.Count > 0
            ? lvRuntimeCombatActions.SelectedItems[0].Tag as string ?? lvRuntimeCombatActions.SelectedItems[0].Text
            : string.Empty;
        var action = _currentProject.Actions.FirstOrDefault(x => string.Equals(x.Id, selectedActionId, StringComparison.OrdinalIgnoreCase));
        var targetRuntimeId = ResolvePreferredCombatTargetRuntimeId(_currentSave, actor, action);
        if (string.IsNullOrWhiteSpace(targetRuntimeId) && lvRuntimeCombatants.Items.Count > 0)
        {
            targetRuntimeId = lvRuntimeCombatants.Items[0].Tag as string ?? lvRuntimeCombatants.Items[0].Text;
        }

        if (!string.IsNullOrWhiteSpace(targetRuntimeId))
        {
            foreach (ListViewItem item in lvRuntimeCombatants.Items)
            {
                var runtimeId = item.Tag as string ?? item.Text;
                item.Selected = string.Equals(runtimeId, targetRuntimeId, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static string ResolvePreferredCombatTargetRuntimeId(SaveGame save, GameRuntimeCombatant? actor, GameActionDefinition? action)
    {
        if (actor == null)
        {
            return string.Empty;
        }

        var scope = action?.TargetScope ?? string.Empty;
        if (scope.Contains("self", StringComparison.OrdinalIgnoreCase) || scope.Contains("actor", StringComparison.OrdinalIgnoreCase))
        {
            return actor.RuntimeId;
        }

        var target = save.Combat.Combatants.FirstOrDefault(x =>
            x.RuntimeId != actor.RuntimeId
            && !string.Equals(x.Team, actor.Team, StringComparison.OrdinalIgnoreCase)
            && x.Stats.Values.Any(value => value > 0));
        if (target != null)
        {
            return target.RuntimeId;
        }

        target = string.Equals(actor.Team, "enemy", StringComparison.OrdinalIgnoreCase)
            ? save.Combat.Combatants.FirstOrDefault(x => !string.Equals(x.Team, "enemy", StringComparison.OrdinalIgnoreCase))
            : save.Combat.Combatants.FirstOrDefault(x => string.Equals(x.Team, "enemy", StringComparison.OrdinalIgnoreCase));

        return target?.RuntimeId ?? actor.RuntimeId;
    }

    private void RefreshRuntimeCombatButtons()
    {
        if (_currentProject == null || _currentSave == null)
        {
            btnRuntimeExecuteCombatAction.Enabled = false;
            btnRuntimeEndCombatTurn.Enabled = false;
            lblRuntimeCombatHint.Text = "Бой не активен.";
            return;
        }

        var actor = _runtimeEngine.GetCurrentCombatant(_currentProject, _currentSave);
        var isPlayerTurn = actor != null && !string.Equals(actor.Team, "enemy", StringComparison.OrdinalIgnoreCase);
        btnRuntimeExecuteCombatAction.Enabled = _currentSave.Combat.IsActive && isPlayerTurn && lvRuntimeCombatActions.SelectedItems.Count > 0 && lvRuntimeCombatants.SelectedItems.Count > 0;
        btnRuntimeEndCombatTurn.Enabled = _currentSave.Combat.IsActive;
        lblRuntimeCombatHint.Text = actor == null
            ? "Бой не активен."
            : "Ход: " + (string.IsNullOrWhiteSpace(actor.Name) ? actor.RuntimeId : actor.Name);
    }

    private async Task SaveAutosaveProgressAsync()
    {
        if (_currentProject == null || _currentSave == null)
        {
            return;
        }

        try
        {
            await _storageService.SaveProgressAsync(_currentProject, _currentSave, "autosave.json");
            RefreshSaves();
        }
        catch (Exception ex)
        {
            AppendLog("Autosave failed: " + ex.Message);
        }
    }

    private void AddOperationLog(GameRuntimeOperationResult result)
    {
        if (result.LogLines.Count > 0)
        {
            foreach (var line in result.LogLines)
            {
                AppendLog(line);
            }
        }
        else if (!string.IsNullOrWhiteSpace(result.Message))
        {
            AppendLog(result.Message);
        }
        if (!result.Success && !string.IsNullOrWhiteSpace(result.Message) && !result.LogLines.Contains(result.Message))
        {
            AppendLog(result.Message);
        }
    }

    private IEnumerable<(string Id, string Name, string Description)> BuildRuntimeCharacterRows(GameProjectData project, SaveGame save)
    {
        yield return ("player_level", "Уровень", Math.Max(1, save.PlayerLevel).ToString());
        yield return ("player_xp", "Опыт", save.PlayerExperience.ToString());
        foreach (var stat in _runtimeEngine.GetEffectiveStats(project, save))
        {
            yield return (stat.Key, FindName(project.Stats, stat.Key), stat.Value.ToString());
        }
    }

    private void RefreshSaves()
    {
        lstSaves.Items.Clear();
        if (_currentProject == null) return;
        foreach (var save in _storageService.ListSaveFiles(_currentProject))
        {
            lstSaves.Items.Add(save);
        }
    }

    private void LoadSceneImage(GameScene scene)
    {
        if (_currentProject == null) return;

        var oldImage = picScene.Image;
        picScene.Image = null;
        oldImage?.Dispose();

        var imagePath = _currentProject.AssetLinks.FirstOrDefault(x => x.AssetId == scene.ImageAssetId)?.ImagePath
            ?? _currentProject.ImagePrompts.FirstOrDefault(x => x.AssetId == scene.ImageAssetId)?.SelectedImagePath;
        imagePath = ImageAssetService.ResolveProjectPath(_currentProject, imagePath ?? "");
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(imagePath);
            using var sourceImage = Image.FromStream(stream);
            picScene.Image = new Bitmap(sourceImage);
        }
        catch (Exception ex)
        {
            AppendLog("Не удалось загрузить изображение сцены: " + ex.Message);
        }
    }

    private void LinkPromptToEntity(ImagePromptDefinition prompt)
    {
        if (_currentProject == null || string.IsNullOrWhiteSpace(prompt.SelectedImagePath)) return;
        _currentProject.AssetLinks.RemoveAll(x => x.AssetId == prompt.AssetId);
        _currentProject.AssetLinks.Add(new ImageAssetLink
        {
            AssetId = prompt.AssetId,
            TargetType = prompt.TargetType,
            TargetEntityId = prompt.TargetEntityId,
            ImagePath = prompt.SelectedImagePath
        });

        if (prompt.TargetType == ImageTargetType.Scene)
        {
            var scene = _currentProject.Scenes.FirstOrDefault(x => x.Id == prompt.TargetEntityId);
            if (scene != null) scene.ImageAssetId = prompt.AssetId;
        }
        else if (prompt.TargetType == ImageTargetType.Character)
        {
            var character = _currentProject.Characters.FirstOrDefault(x => x.Id == prompt.TargetEntityId);
            if (character != null) character.PortraitAssetId = prompt.AssetId;
        }
        else if (prompt.TargetType == ImageTargetType.Item)
        {
            var item = _currentProject.Items.FirstOrDefault(x => x.Id == prompt.TargetEntityId);
            if (item != null) item.ImageAssetId = prompt.AssetId;
        }
    }

    private ImagePromptDefinition? GetSelectedPrompt()
    {
        return lvPrompts.SelectedItems.Count == 0 ? null : lvPrompts.SelectedItems[0].Tag as ImagePromptDefinition;
    }

    private LmStudioSettings GetLmSettings()
    {
        return GetLmSettingsForPurpose("active");
    }

    private LmStudioSettings GetLmSettingsForPurpose(string purpose)
    {
        SaveActiveProfileFromUi();
        var profile = string.Equals(purpose, "active", StringComparison.OrdinalIgnoreCase)
            ? _lmProfileService.GetActiveProfile(_appSettings)
            : _lmProfileService.ResolveProfileForPurpose(_appSettings, purpose);
        if (string.Equals(purpose, "active", StringComparison.OrdinalIgnoreCase))
        {
            _lmProfileService.ApplyProfileToLegacySettings(_appSettings, profile);
        }
        else
        {
            _lmProfileService.ApplyProfileRuntimeSettings(_appSettings, profile);
        }

        _pipelineService.GenerationSettingsUi = LmStudioProfileService.Clone(profile.Generation);
        LogLmProfileUse(profile, purpose);
        return LmStudioProfileService.Clone(profile.Settings);
    }

    private LmStudioSettings GetLmSettingsFromUi()
    {
        return new LmStudioSettings
        {
            Endpoint = txtEndpoint.Text.Trim(),
            ApiKey = txtApiKey.Text.Trim(),
            ModelId = txtModel.Text.Trim(),
            RequestTimeoutSeconds = (int)nudTimeout.Value,
            UnloadUrl = txtLmUnloadUrl.Text.Trim(),
            UnloadCommand = txtLmUnloadCommand.Text.Trim(),
            UnloadCommandTimeoutSeconds = (int)nudLmUnloadTimeout.Value,
            ContinueIfUnloadFails = chkContinueIfUnloadFails.Checked
        };
    }

    private GenerationUiSettings GetGenerationSettingsFromUi()
    {
        return new GenerationUiSettings
        {
            MaxInputContextTokens = (int)nudMaxInputContextTokens.Value,
            MaxOutputTokens = (int)nudMaxOutputTokens.Value,
            MaxTokens = (int)nudMaxOutputTokens.Value,
            ApproxCharsPerToken = _appSettings.Generation.ApproxCharsPerToken > 0 ? _appSettings.Generation.ApproxCharsPerToken : 4,
            Temperature = _appSettings.Generation.Temperature,
            TopP = _appSettings.Generation.TopP,
            MinP = _appSettings.Generation.MinP,
            TopK = _appSettings.Generation.TopK,
            RepeatPenalty = _appSettings.Generation.RepeatPenalty,
            PresencePenalty = _appSettings.Generation.PresencePenalty
        };
    }

    private void SaveActiveProfileFromUi()
    {
        if (_loadingSettingsUi)
        {
            return;
        }

        _lmProfileService.NormalizeProfiles(_appSettings);
        var active = _lmProfileService.GetActiveProfile(_appSettings);
        active.Name = string.IsNullOrWhiteSpace(txtLmProfileName.Text) ? active.Name : txtLmProfileName.Text.Trim();
        active.Role = cmbLmProfileRole.SelectedItem?.ToString() ?? active.Role;
        active.Settings = GetLmSettingsFromUi();
        active.Generation = GetGenerationSettingsFromUi();
        _appSettings.AutoSelectLmStudioProfile = chkAutoSelectLmProfile.Checked;
        _lmProfileService.ApplyProfileToLegacySettings(_appSettings, active);
    }

    private void ApplyLmProfileToUi(LmStudioModelProfile profile)
    {
        _loadingSettingsUi = true;
        try
        {
            txtLmProfileName.Text = profile.Name;
            cmbLmProfileRole.SelectedItem = profile.Role;
            if (cmbLmProfileRole.SelectedIndex < 0)
            {
                cmbLmProfileRole.Text = profile.Role;
            }

            txtEndpoint.Text = profile.Settings.Endpoint;
            txtApiKey.Text = profile.Settings.ApiKey;
            txtModel.Text = profile.Settings.ModelId;
            nudTimeout.Value = Clamp(profile.Settings.RequestTimeoutSeconds, (int)nudTimeout.Minimum, (int)nudTimeout.Maximum);
            nudMaxInputContextTokens.Value = Clamp(profile.Generation.MaxInputContextTokens, (int)nudMaxInputContextTokens.Minimum, (int)nudMaxInputContextTokens.Maximum);
            nudMaxOutputTokens.Value = Clamp(profile.Generation.MaxOutputTokens > 0 ? profile.Generation.MaxOutputTokens : profile.Generation.MaxTokens, (int)nudMaxOutputTokens.Minimum, (int)nudMaxOutputTokens.Maximum);
            txtLmUnloadUrl.Text = profile.Settings.UnloadUrl;
            txtLmUnloadCommand.Text = profile.Settings.UnloadCommand;
            nudLmUnloadTimeout.Value = Clamp(profile.Settings.UnloadCommandTimeoutSeconds, (int)nudLmUnloadTimeout.Minimum, (int)nudLmUnloadTimeout.Maximum);
            chkContinueIfUnloadFails.Checked = profile.Settings.ContinueIfUnloadFails;
            chkAutoSelectLmProfile.Checked = _appSettings.AutoSelectLmStudioProfile;
        }
        finally
        {
            _loadingSettingsUi = false;
        }
    }

    private void RefreshLmProfileList(string? selectedId = null)
    {
        _loadingSettingsUi = true;
        try
        {
            var id = selectedId ?? _appSettings.ActiveLmStudioProfileId;
            cmbLmProfiles.Items.Clear();
            foreach (var profile in _appSettings.LmStudioProfiles)
            {
                cmbLmProfiles.Items.Add(profile);
            }

            for (var i = 0; i < cmbLmProfiles.Items.Count; i++)
            {
                if (cmbLmProfiles.Items[i] is LmStudioModelProfile profile
                    && string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    cmbLmProfiles.SelectedIndex = i;
                    break;
                }
            }
        }
        finally
        {
            _loadingSettingsUi = false;
        }
    }

    private void ConfigureLmProfileUi()
    {
        cmbLmProfileRole.Items.Clear();
        cmbLmProfileRole.Items.Add(LmStudioProfileRole.Default);
        cmbLmProfileRole.Items.Add(LmStudioProfileRole.Discussion);
        cmbLmProfileRole.Items.Add(LmStudioProfileRole.JsonStrict);
        cmbLmProfileRole.Items.Add(LmStudioProfileRole.Creative);
        cmbLmProfileRole.Items.Add(LmStudioProfileRole.LargeContext);
        cmbLmProfileRole.Items.Add(LmStudioProfileRole.Review);
        cmbLmProfileRole.Items.Add(LmStudioProfileRole.Balance);
    }

    private void LogLmProfileUse(LmStudioModelProfile profile, string purpose)
    {
        var activeProfile = _lmProfileService.GetActiveProfile(_appSettings);
        var route = _appSettings.AutoSelectLmStudioProfile && !string.Equals(profile.Id, activeProfile.Id, StringComparison.OrdinalIgnoreCase)
            ? "auto-select"
            : "active";
        AppendLog($"LM Studio профиль: {profile.Name}; роль={profile.Role}; purpose={purpose}; route={route}; active={activeProfile.Name}; endpoint={profile.Settings.Endpoint}; model={profile.Settings.ModelId}; context={profile.Generation.MaxInputContextTokens}; output={profile.Generation.MaxOutputTokens}.");
    }

    private FooocusSettings GetFooocusSettings()
    {
        return new FooocusSettings
        {
            LaunchFilePath = txtFooocusLaunch.Text.Trim(),
            WorkingDirectory = txtFooocusWorkingDir.Text.Trim(),
            OutputDirectory = txtFooocusOutput.Text.Trim(),
            WebEndpoint = txtFooocusEndpoint.Text.Trim(),
            StartupTimeoutSeconds = (int)nudFooocusStartup.Value,
            ShutdownTimeoutSeconds = (int)nudFooocusShutdown.Value
        };
    }

    private void ApplySettingsToUi(AppSettings settings)
    {
        _lmProfileService.NormalizeProfiles(settings);
        txtGamesRoot.Text = settings.GamesRootPath;
        RefreshLmProfileList(settings.ActiveLmStudioProfileId);
        ApplyLmProfileToUi(_lmProfileService.GetActiveProfile(settings));

        txtFooocusLaunch.Text = settings.Fooocus.LaunchFilePath;
        txtFooocusWorkingDir.Text = settings.Fooocus.WorkingDirectory;
        txtFooocusOutput.Text = settings.Fooocus.OutputDirectory;
        txtFooocusEndpoint.Text = settings.Fooocus.WebEndpoint;
        nudFooocusStartup.Value = Clamp(settings.Fooocus.StartupTimeoutSeconds, (int)nudFooocusStartup.Minimum, (int)nudFooocusStartup.Maximum);
        nudFooocusShutdown.Value = Clamp(settings.Fooocus.ShutdownTimeoutSeconds, (int)nudFooocusShutdown.Minimum, (int)nudFooocusShutdown.Maximum);
    }

    private void SaveSettingsFromUi()
    {
        _appSettings.GamesRootPath = GetGamesRoot();
        SaveActiveProfileFromUi();
        _appSettings.Fooocus = GetFooocusSettings();
        if (string.IsNullOrWhiteSpace(_appSettings.Fooocus.OutputDirectory))
        {
            _appSettings.Fooocus.OutputDirectory = DetectFooocusOutputFromUi();
            txtFooocusOutput.Text = _appSettings.Fooocus.OutputDirectory;
        }
        _settingsStore.Save(_appSettings);
    }

    private void DetectFooocusFromFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            AppendLog("Fooocus folder was not found.");
            return;
        }

        var detected = _fooocusProfileDetector.DetectFromFolder(folder);
        txtFooocusLaunch.Text = detected.LaunchFilePath;
        txtFooocusWorkingDir.Text = detected.WorkingDirectory;
        txtFooocusOutput.Text = detected.OutputDirectory;
        if (string.IsNullOrWhiteSpace(txtFooocusEndpoint.Text))
        {
            txtFooocusEndpoint.Text = detected.WebEndpoint;
        }
        AppendLog("Fooocus profile detected from folder: " + folder);
    }

    private string DetectFooocusOutputFromUi()
    {
        var folder = !string.IsNullOrWhiteSpace(txtFooocusWorkingDir.Text)
            ? txtFooocusWorkingDir.Text
            : Path.GetDirectoryName(txtFooocusLaunch.Text) ?? "";
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return "";
        }

        return _fooocusProfileDetector.DetectFromFolder(folder).OutputDirectory;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private string GetGamesRoot()
    {
        var root = txtGamesRoot.Text.Trim();
        if (string.IsNullOrWhiteSpace(root))
        {
            root = _storageService.GetDefaultGamesRoot();
            txtGamesRoot.Text = root;
        }
        Directory.CreateDirectory(root);
        return root;
    }

    private static bool IsPathUnderDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath.Equals(fullDirectory, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(fullDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(fullDirectory + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string MakeSafeDeletedFolderName(string folderName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = (folderName ?? string.Empty)
            .Select(ch => invalid.Contains(ch) ? '_' : ch)
            .ToArray();
        var safe = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "project" : safe;
    }

    private static string GetUniqueDeletedProjectPath(string deletedRoot, string folderName)
    {
        var destination = Path.Combine(deletedRoot, folderName);
        if (!Directory.Exists(destination))
        {
            return destination;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = Path.Combine(deletedRoot, folderName + "_" + suffix);
            if (!Directory.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveSettingsFromUi();
        base.OnFormClosing(e);
    }

    private void AppendDiscussion(string author, string text)
    {
        txtDiscussion.AppendText($"===== {author} ====={Environment.NewLine}");
        txtDiscussion.AppendText(text);
        txtDiscussion.AppendText(Environment.NewLine + Environment.NewLine);
    }

    private void AppendLog(string text)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }

    private void AppendValidationResult(string title, GameProjectValidationResult result)
    {
        AppendLog($"{title}: {(result.IsValid ? "valid" : "invalid")}, errors={result.Errors.Count}, warnings={result.Warnings.Count}");
        foreach (var error in result.Errors)
        {
            AppendLog("Validation error: " + error);
        }
        foreach (var warning in result.Warnings)
        {
            AppendLog("Validation warning: " + warning);
        }
    }

    private void OpenProjectSubfolder(string relativePath)
    {
        if (_currentProject == null || string.IsNullOrWhiteSpace(_currentProject.Summary.ProjectPath))
        {
            return;
        }

        var folder = Path.Combine(_currentProject.Summary.ProjectPath, relativePath);
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    private void ConfigurePipelineListView()
    {
        lvGenerationPlan.Columns.Clear();
        lvGenerationPlan.Columns.Add("№", 42);
        lvGenerationPlan.Columns.Add("Этап", 190);
        lvGenerationPlan.Columns.Add("Статус", 90);
        lvGenerationPlan.Columns.Add("Что уже есть", 220);
        lvGenerationPlan.Columns.Add("Следующее действие", 320);
    }

    private void ConfigureDesignBrainUi()
    {
        cmbGameCreationMode.Items.Clear();
        cmbGameCreationMode.Items.Add(new GameCreationModeItem(GameCreationMode.Manual, "Ручной"));
        cmbGameCreationMode.Items.Add(new GameCreationModeItem(GameCreationMode.Collaborative, "Совместный"));
        cmbGameCreationMode.Items.Add(new GameCreationModeItem(GameCreationMode.AutopilotWithReview, "Автопилот с проверкой"));
        cmbGameCreationMode.Items.Add(new GameCreationModeItem(GameCreationMode.QuickPrototype, "Быстрый прототип"));
        cmbGameCreationMode.DisplayMember = nameof(GameCreationModeItem.Title);
        cmbGameCreationMode.SelectedIndex = 1;

        lvDesignSlots.Columns.Clear();
        lvDesignSlots.Columns.Add("Slot", 130);
        lvDesignSlots.Columns.Add("Value", 280);
        lvDesignSlots.Columns.Add("Source", 110);
        lvDesignSlots.Columns.Add("Confidence", 85);
        lvDesignSlots.Columns.Add("Required", 75);

        lvDesignQuestions.Columns.Clear();
        lvDesignQuestions.Columns.Add("Slot", 130);
        lvDesignQuestions.Columns.Add("Question", 520);
    }

    private GameCreationMode GetSelectedCreationMode()
    {
        return cmbGameCreationMode.SelectedItem is GameCreationModeItem item
            ? item.Mode
            : GameCreationMode.Collaborative;
    }

    private void SelectCreationMode(GameCreationMode mode)
    {
        for (var i = 0; i < cmbGameCreationMode.Items.Count; i++)
        {
            if (cmbGameCreationMode.Items[i] is GameCreationModeItem item && item.Mode == mode)
            {
                cmbGameCreationMode.SelectedIndex = i;
                return;
            }
        }

        cmbGameCreationMode.SelectedIndex = Math.Min(1, cmbGameCreationMode.Items.Count - 1);
    }

    private sealed class GameCreationModeItem
    {
        public GameCreationModeItem(GameCreationMode mode, string title)
        {
            Mode = mode;
            Title = title;
        }

        public GameCreationMode Mode { get; }
        public string Title { get; }

        public override string ToString()
        {
            return Title;
        }
    }

    private GameGenerationStepView? GetSelectedPipelineStep()
    {
        return lvGenerationPlan.SelectedItems.Count == 0 ? null : lvGenerationPlan.SelectedItems[0].Tag as GameGenerationStepView;
    }

    private Task<string> RunPipelineBatchByCategoryAsync(GameProjectData project, string category, string rules, int count)
    {
        var settings = GetLmSettingsForPurpose(string.Equals(category, "image-prompts", StringComparison.OrdinalIgnoreCase) ? "image-prompts" : "json-draft");
        return category switch
        {
            "stats-resources" => _pipelineService.BuildStatsAndResourcesBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "formulas" => _pipelineService.BuildFormulasBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "status-effects" => _pipelineService.BuildStatusEffectsBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "progression" => _pipelineService.BuildProgressionBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "gameplay-actions" => _pipelineService.BuildGameplayActionsBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "combat" => _pipelineService.BuildCombatBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "world-state" => _pipelineService.BuildWorldStateBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "equipment" => _pipelineService.BuildEquipmentBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "items" => _pipelineService.BuildItemsBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "skills" => _pipelineService.BuildSkillsBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "spells" => _pipelineService.BuildSpellsBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "locations" => _pipelineService.BuildLocationsBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "scenes" => _pipelineService.BuildScenesBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "encounters" => _pipelineService.BuildEncountersBatchAsync(project, settings, rules, count, category, AppendLog, CurrentOperationToken),
            "image-prompts" => _pipelineService.BuildImagePromptPlanAsync(project, settings, AppendLog, CurrentOperationToken),
            _ => throw new InvalidOperationException("Неизвестная batch-категория: " + category)
        };
    }

    private async Task<string> LoadDraftRawTextAsync(GameProjectData project, GameDraftSession draft, CancellationToken token)
    {
        if (!string.IsNullOrWhiteSpace(draft.RawOutputFile))
        {
            var rawPath = Path.Combine(project.Summary.ProjectPath, draft.RawOutputFile);
            if (File.Exists(rawPath))
            {
                return await File.ReadAllTextAsync(rawPath, token);
            }
        }

        var builder = new StringBuilder();
        foreach (var file in draft.Files)
        {
            var path = Path.Combine(project.Summary.ProjectPath, file.RelativePath);
            if (File.Exists(path))
            {
                builder.AppendLine(await File.ReadAllTextAsync(path, token));
            }
        }

        return builder.ToString();
    }

    private static string ResolveDraftOpenPath(GameProjectData project, GameDraftSession draft)
    {
        if (!string.IsNullOrWhiteSpace(draft.RawOutputFile))
        {
            var rawPath = Path.Combine(project.Summary.ProjectPath, draft.RawOutputFile);
            if (File.Exists(rawPath))
            {
                return rawPath;
            }
        }

        foreach (var file in draft.Files)
        {
            var path = Path.Combine(project.Summary.ProjectPath, file.RelativePath);
            if (File.Exists(path))
            {
                return path;
            }
        }

        var folder = Path.Combine(project.Summary.ProjectPath, "drafts", draft.SessionId);
        return Directory.Exists(folder) ? folder : string.Empty;
    }

    private static string GetPipelineRuleHint(string category)
    {
        return category switch
        {
            "stats-resources" => "Опишите, какие параметры, ресурсы, валюты или скрытые переменные нужны для этого жанра.",
            "formulas" => "Опишите, какие безопасные формулы расчётов нужны: проверки, урон, восстановление, шанс, эффективность.",
            "status-effects" => "Опишите баффы, дебаффы, длительность, стаки, периодические эффекты и модификаторы.",
            "progression" => "Опишите узлы прокачки, зависимости, требования, стоимость и какие навыки они открывают.",
            "gameplay-actions" => "Опишите доступные действия игрока, требования, стоимость, эффекты, cooldown и теги.",
            "equipment" => "Опишите слоты экипировки, редкость, требования, бонусы и ограничения.",
            "items" => "Опишите предметы, расходники, награды, стоимость и эффекты использования.",
            "skills" => "Опишите активные/пассивные/социальные/ремесленные навыки и их ограничения.",
            "spells" => "Опишите стихии, школы магии, стоимость, кулдауны и эффекты заклинаний.",
            "locations" => "Опишите регионы, переходы, закрытые зоны, статусы локаций и условия доступа.",
            "scenes" => "Опишите, какие игровые сцены и развилки нужны, какие проверки/эффекты должны быть.",
            "encounters" => "Опишите события: бой, дуэль, социальная проверка, торговля, работа, расследование, романтика, скрытность.",
            "image-prompts" => "Опишите стиль, какие сцены/персонажи/предметы сейчас нужно иллюстрировать.",
            "world-state" => "Опишите жанр атмосферы: для фэнтези - время суток, погода, сезон, фаза луны, магический фон; для космоса - корабль, кислород, энергия, тревога, радиация, связь, экипаж; для социальной игры - день недели, время дня, настроение, расписание NPC, усталость, репутация.",
            _ => string.Empty
        };
    }

    private void SetStatus(AppWorkflowStatus status)
    {
        _status = status;
        lblStatus.Text = status.ToString();
    }

    private void SetBusy(bool busy)
    {
        btnStartDiscussion.Enabled = !busy;
        btnSend.Enabled = !busy;
        btnBuildBrief.Enabled = !busy;
        btnBuildConcept.Enabled = !busy;
        btnBuildMvp.Enabled = !busy;
        btnBuildStructure.Enabled = !busy;
        btnGenerateContent.Enabled = !busy;
        btnBuildImagePrompts.Enabled = !busy;
        btnRandomDirectorCheck.Enabled = !busy;
        btnRandomDirectorGenerate.Enabled = !busy;
        txtRandomDirectorEventCount.Enabled = !busy;
        btnChangeRequestAnalyze.Enabled = !busy;
        btnChangeRequestGenerate.Enabled = !busy;
        txtChangeRequest.Enabled = !busy;
        btnDesignConversationSend.Enabled = !busy;
        txtDesignConversation.Enabled = !busy;
        txtDesignConversationFocus.Enabled = !busy;
        btnBalanceCheck.Enabled = !busy;
        btnBalanceGenerateDraft.Enabled = !busy;
        txtBalanceSimulationRuns.Enabled = !busy;
        btnMvpCheck.Enabled = !busy;
        btnMvpGenerateNextDraft.Enabled = !busy;
        btnGenerateStatsResourcesBatch.Enabled = !busy;
        btnGenerateItemsBatch.Enabled = !busy;
        btnGenerateEquipmentBatch.Enabled = !busy;
        btnGenerateSkillsBatch.Enabled = !busy;
        btnGenerateSpellsBatch.Enabled = !busy;
        btnGenerateLocationsBatch.Enabled = !busy;
        btnGenerateScenesBatch.Enabled = !busy;
        btnGenerateEncountersBatch.Enabled = !busy;
        btnRefreshGenerationPlan.Enabled = !busy;
        btnCheckMechanics.Enabled = !busy;
        btnRunSelectedPipelineStep.Enabled = !busy;
        btnReviewLatestDraft.Enabled = !busy;
        btnApplyLatestDraft.Enabled = !busy;
        btnRejectLatestDraft.Enabled = !busy;
        btnOpenDraftsFolderPipeline.Enabled = !busy;
        btnOpenCurrentDraft.Enabled = !busy;
        nudPipelineBatchCount.Enabled = !busy;
        cmbPipelineCategory.Enabled = !busy;
        txtPipelineRules.Enabled = !busy;
        nudBatchCount.Enabled = !busy;
        cmbBatchCategory.Enabled = !busy;
        txtBatchRules.Enabled = !busy;
        btnOpenPlayWindow.Enabled = !busy;
        btnRunFooocusQueue.Enabled = !busy;
        btnTestLm.Enabled = !busy;
        btnResaveSplitJson.Enabled = !busy;
        btnValidateProject.Enabled = !busy;
        btnDeleteProject.Enabled = !busy;
        btnStopOperation.Enabled = busy;
        btnSaveSettings.Enabled = !busy;
        cmbLmProfiles.Enabled = !busy;
        txtLmProfileName.Enabled = !busy;
        cmbLmProfileRole.Enabled = !busy;
        chkAutoSelectLmProfile.Enabled = !busy;
        btnAddLmProfile.Enabled = !busy;
        btnSaveLmProfile.Enabled = !busy;
        btnDeleteLmProfile.Enabled = !busy;
        btnSetDefaultLmProfile.Enabled = !busy;
    }

    private static void FillList(ListView listView, IEnumerable<(string Id, string Name, string Description)> rows, (string Id, string Name, string Description)? emptyRow = null)
    {
        PlayListViewHelper.FillList(listView, rows, emptyRow);
    }

    private static string FindName(IEnumerable<GameStatDefinition> source, string id)
    {
        return source.FirstOrDefault(x => x.Id == id)?.Name ?? id;
    }

    private static string FindName(IEnumerable<GameItemDefinition> source, string id)
    {
        return source.FirstOrDefault(x => x.Id == id)?.Name ?? id;
    }

    private static string FindRelationshipName(GameProjectData project, string id)
    {
        return project.Relationships.FirstOrDefault(x => x.CharacterId == id)?.Name
            ?? project.Characters.FirstOrDefault(x => x.Id == id)?.Name
            ?? id;
    }

    private static string FindQuestName(GameProjectData project, string id)
    {
        return project.Quests.FirstOrDefault(x => x.Id == id)?.Title ?? id;
    }

    private static string? PromptForText(string title, string defaultText)
    {
        using var form = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(420, 120),
            MinimizeBox = false,
            MaximizeBox = false
        };
        var textBox = new TextBox { Left = 12, Top = 12, Width = 396, Text = defaultText };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 252, Width = 75, Top = 52 };
        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Left = 333, Width = 75, Top = 52 };
        form.Controls.Add(textBox);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog() == DialogResult.OK ? textBox.Text : null;
    }
}
