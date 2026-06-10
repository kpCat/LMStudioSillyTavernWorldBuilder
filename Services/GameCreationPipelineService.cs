using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Providers;
using LMStudioSillyTavernWorldBuilder.Storage;
using System.Diagnostics;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class GameCreationPipelineService
{
    private readonly LmStudioService _lmStudioService;
    private readonly GameStorageService _storageService;
    private readonly GameProjectValidator _validator = new();
    private readonly GameProjectRepairService _repairService = new();
    private readonly GameProjectCloneService _cloneService = new();
    private readonly GameDraftService _draftService = new();
    private readonly PromptBudgetService _promptBudgetService = new();
    private readonly GameDesignInterviewService _designInterviewService = new();
    private readonly GameRandomDirectorService _randomDirectorService = new();
    private readonly GameChangeRequestService _changeRequestService = new();
    private readonly GameBalanceSimulatorService _balanceSimulatorService = new();
    private readonly GameMvpOrchestratorService _mvpOrchestratorService = new();
    private readonly LmStudioCallDiagnosticsService _lmCallDiagnosticsService = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public GenerationUiSettings GenerationSettingsUi { get; set; } = new();

    public GameCreationPipelineService(LmStudioService lmStudioService, GameStorageService storageService)
    {
        _lmStudioService = lmStudioService;
        _storageService = storageService;
    }

    public async Task<string> StartDiscussionAsync(GameProjectData project, LmStudioSettings settings, IdeaDiscussionSession session, string initialIdea, Action<string> log, CancellationToken cancellationToken = default)
    {
        session.Messages.Clear();
        session.Messages.Add(new ChatMessage { Role = "system", Content = Prompts.GameIdeaDiscussion.SystemPrompt });
        session.Messages.Add(new ChatMessage { Role = "user", Content = initialIdea });
        log("AI discussion started.");
        var answer = await SendPresetAsync(project, settings, Prompts.GameIdeaDiscussion, session.Messages, log, "discussion", cancellationToken);
        session.Messages.Add(new ChatMessage { Role = "assistant", Content = answer });
        await SaveStageTextAsync(project, "discussion_start.txt", answer, cancellationToken);
        return answer;
    }

    public async Task<string> ContinueDiscussionAsync(GameProjectData project, LmStudioSettings settings, IdeaDiscussionSession session, string userText, Action<string> log, CancellationToken cancellationToken = default)
    {
        if (session.Messages.Count == 0)
        {
            session.Messages.Add(new ChatMessage { Role = "system", Content = Prompts.GameIdeaDiscussion.SystemPrompt });
        }

        session.Messages.Add(new ChatMessage { Role = "user", Content = userText });
        log("AI discussion continued.");
        var answer = await SendPresetAsync(project, settings, Prompts.GameIdeaDiscussion, session.Messages, log, "discussion", cancellationToken);
        session.Messages.Add(new ChatMessage { Role = "assistant", Content = answer });
        await SaveStageTextAsync(project, $"discussion_{DateTime.Now:yyyyMMdd_HHmmss}.txt", BuildConversation(session), cancellationToken);
        return answer;
    }

    public Task<string> BuildBriefAsync(GameProjectData project, LmStudioSettings settings, IdeaDiscussionSession session, Action<string> log, CancellationToken cancellationToken = default)
    {
        return RunTextStageAsync(project, settings, Prompts.GameBrief, BuildConversation(session), "brief.txt", text => project.Brief.Text = text, log, cancellationToken);
    }

    public Task<string> BuildConceptAsync(GameProjectData project, LmStudioSettings settings, Action<string> log, CancellationToken cancellationToken = default)
    {
        return RunTextStageAsync(project, settings, Prompts.GameConcept, project.Brief.Text, "concept.txt", text => project.Concept.Text = text, log, cancellationToken);
    }

    public Task<string> BuildMvpAsync(GameProjectData project, LmStudioSettings settings, Action<string> log, CancellationToken cancellationToken = default)
    {
        return RunTextStageAsync(project, settings, Prompts.GameMvp, $"BRIEF:\n{project.Brief.Text}\n\nCONCEPT:\n{project.Concept.Text}", "mvp.txt", text => project.MvpPlan.Text = text, log, cancellationToken);
    }

    public Task<string> BuildGameStructureAsync(GameProjectData project, LmStudioSettings settings, Action<string> log, CancellationToken cancellationToken = default)
    {
        return RunTextStageAsync(project, settings, Prompts.GameStructure, $"BRIEF:\n{project.Brief.Text}\n\nCONCEPT:\n{project.Concept.Text}\n\nMVP:\n{project.MvpPlan.Text}", "structure.txt", text => project.ArchitecturePlan.Text = text, log, cancellationToken);
    }

    public async Task<string> BuildInitialContentAsync(GameProjectData project, LmStudioSettings settings, Action<string> log, CancellationToken cancellationToken = default)
    {
        log("Generating initial data-driven game content.");
        var context = BuildCompactProjectContext(project, "initial-content");
        LogContextBudget(log, "initial-content", context);
        var text = await SendPresetAsync(project, settings, Prompts.GameInitialContentJson, new[]
        {
            new ChatMessage { Role = "system", Content = Prompts.GameInitialContentJson.SystemPrompt },
            new ChatMessage { Role = "user", Content = context }
        }, log, "initial-content", cancellationToken);

        await ApplyGeneratedProjectJsonAsync(project, text, log, cancellationToken);
        project.ContentPlan.Text = text;
        await SaveStageTextAsync(project, "initial-content.json.txt", text, cancellationToken);
        return text;
    }

    public async Task<string> BuildImagePromptPlanAsync(GameProjectData project, LmStudioSettings settings, Action<string> log, CancellationToken cancellationToken = default)
    {
        log("Generating image prompt plan.");
        var text = await SendPresetAsync(project, settings, Prompts.GameImagePromptJson, new[]
        {
            new ChatMessage { Role = "system", Content = Prompts.GameImagePromptJson.SystemPrompt },
            new ChatMessage { Role = "user", Content = BuildCompactProjectContext(project, "image-prompts") }
        }, log, "image-prompts", cancellationToken);

        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            log("Project path is not set; image prompts cannot be saved as draft.");
            return text;
        }

        var json = ExtractJson(text);
        try
        {
            var prompts = JsonSerializer.Deserialize<List<ImagePromptDefinition>>(json, _jsonOptions);
            if (prompts == null)
            {
                var emptyDraft = await _draftService.SaveRawDraftAsync(project, "image-prompts", string.Empty, text, cancellationToken);
                emptyDraft.Validation.IsValid = false;
                emptyDraft.Validation.Errors.Add("Generated image prompt JSON was empty.");
                await _draftService.SaveDraftManifestAsync(project, emptyDraft, cancellationToken);
                await _draftService.SaveValidationReportAsync(project, emptyDraft, cancellationToken);
                log("Image prompt output saved as invalid draft: JSON was empty.");
                return text;
            }

            var draft = await _draftService.ExtractImagePromptDraftsAsync(project, "image-prompts", prompts, text, cancellationToken);
            if (draft.Validation.IsValid)
            {
                log("Image prompts saved as draft and will be applied only after the Apply draft button: " + draft.SessionId);
            }
            else
            {
                foreach (var error in draft.Validation.Errors)
                {
                    log("Image prompt draft error: " + error);
                }

                log("Image prompts saved as invalid draft and were not applied: " + draft.SessionId);
            }
        }
        catch (Exception ex)
        {
            var draft = await _draftService.SaveRawDraftAsync(project, "image-prompts", string.Empty, text, cancellationToken);
            draft.Validation.IsValid = false;
            draft.Validation.Errors.Add("Could not parse generated image prompt JSON: " + ex.Message);
            await _draftService.SaveDraftManifestAsync(project, draft, cancellationToken);
            await _draftService.SaveValidationReportAsync(project, draft, cancellationToken);
            log("Could not parse generated image prompt JSON; raw text saved as invalid draft. " + ex.Message);
        }

        await SaveStageTextAsync(project, "image-prompts.json.txt", text, cancellationToken);
        return text;
    }

    public async Task<string> ApplyRevisionAsync(GameProjectData project, LmStudioSettings settings, string revisionRequest, Action<string> log, CancellationToken cancellationToken = default)
    {
        log("Формирую draft-исправление. Автоприменения не будет.");
        var context = BuildCompactProjectContext(project, "revision-fix");
        LogContextBudget(log, "revision-fix", context);
        var text = await SendPresetAsync(project, settings, Prompts.GameRevision, new[]
        {
            new ChatMessage { Role = "system", Content = Prompts.GameRevision.SystemPrompt },
            new ChatMessage { Role = "user", Content = JsonSerializer.Serialize(new { RevisionRequest = revisionRequest, ProjectContext = JsonSerializer.Deserialize<object>(context) }, _jsonOptions) }
        }, log, "revision-fix", cancellationToken);

        var draft = await CreateGeneratedProjectDraftAsync(project, "revision-fix", text, log, cancellationToken);
        if (draft != null)
        {
            log($"Draft-исправление сохранено: {draft.SessionId}. Файлов: {draft.Files.Count}. Ошибок: {draft.Validation.Errors.Count}, предупреждений: {draft.Validation.Warnings.Count}.");
            log("Проверьте последний draft и примените его вручную, если всё подходит.");
        }

        await SaveStageTextAsync(project, $"revision_{DateTime.Now:yyyyMMdd_HHmmss}.json.txt", text, cancellationToken);
        return text;
    }

    public Task<string> BuildStatsAndResourcesBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateStatsAndResourcesBatch, "stats-resources", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildFormulasBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateFormulasBatch, "formulas", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildStatusEffectsBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateStatusEffectsBatch, "status-effects", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildProgressionBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateProgressionBatch, "progression", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildGameplayActionsBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateGameplayActionsBatch, "gameplay-actions", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildCombatBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateCombatBatch, "combat", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildWorldStateBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateWorldStateBatch, "world-state", userRules, count, category, log, cancellationToken);
    }

    public async Task<string> BuildRandomDirectorDraftAsync(GameProjectData project, LmStudioSettings settings, int requestedEventCount, Action<string> log, CancellationToken cancellationToken = default)
    {
        log("Random Director: строю deterministic report и запрашиваю draft controlled-randomness данных.");
        var report = _randomDirectorService.BuildReport(project);
        var userContent = _randomDirectorService.BuildGenerationUserPrompt(project, report, requestedEventCount);
        LogContextBudget(log, "random-director", userContent);
        var text = await SendPresetAsync(project, settings, Prompts.GameRandomDirectorJson, new[]
        {
            new ChatMessage { Role = "system", Content = Prompts.GameRandomDirectorJson.SystemPrompt },
            new ChatMessage { Role = "user", Content = userContent }
        }, log, "random-director", cancellationToken);

        var draft = await CreateGeneratedProjectDraftAsync(project, "random-director", text, log, cancellationToken);
        if (draft != null)
        {
            log($"Random Director draft сохранён: {draft.SessionId}. Файлов: {draft.Files.Count}. Ошибок: {draft.Validation.Errors.Count}, предупреждений: {draft.Validation.Warnings.Count}.");
        }

        await SaveStageTextAsync(project, $"random-director_{DateTime.Now:yyyyMMdd_HHmmss}.json.txt", text, cancellationToken);
        return text;
    }

    public async Task<string> BuildChangeRequestDraftAsync(
        GameProjectData project,
        LmStudioSettings settings,
        string userRequest,
        Action<string> log,
        CancellationToken cancellationToken = default)
    {
        log("Change Request: анализирую запрос и строю безопасный draft patch plan.");
        var report = _changeRequestService.AnalyzeRequest(project, userRequest);
        var plan = _changeRequestService.BuildPatchPlan(project, report);
        var userContent = _changeRequestService.BuildGenerationUserPrompt(project, report, plan);
        LogContextBudget(log, "change-request", userContent);
        log("Change Request: затронутые системы: " + string.Join(", ", report.AffectedSystems.Select(x => x.SystemId).Distinct(StringComparer.OrdinalIgnoreCase)) + ".");
        log("Change Request: рисков " + report.Risks.Count + ", шагов плана " + plan.Steps.Count + ".");

        var text = await SendPresetAsync(project, settings, Prompts.GameChangeRequestPatchJson, new[]
        {
            new ChatMessage { Role = "system", Content = Prompts.GameChangeRequestPatchJson.SystemPrompt },
            new ChatMessage { Role = "user", Content = userContent }
        }, log, "change-request", cancellationToken);

        var draft = await CreateGeneratedProjectDraftAsync(project, "change-request", text, log, cancellationToken);
        if (draft != null)
        {
            log($"Change Request draft сохранён: {draft.SessionId}. Файлов: {draft.Files.Count}. Ошибок: {draft.Validation.Errors.Count}, предупреждений: {draft.Validation.Warnings.Count}.");
            log("Draft не применён автоматически. Проверьте и примените вручную, если он подходит.");
        }

        await SaveStageTextAsync(project, $"change-request_{DateTime.Now:yyyyMMdd_HHmmss}.json.txt", text, cancellationToken);
        return text;
    }

    public async Task<string> BuildBalanceRebalanceDraftAsync(
        GameProjectData project,
        LmStudioSettings settings,
        int simulationRunsPerEncounter,
        Action<string> log,
        CancellationToken cancellationToken = default)
    {
        log("Balance Simulator: строю authoring-time report и запрашиваю маленький draft rebalance patch.");
        var report = _balanceSimulatorService.BuildReport(project, simulationRunsPerEncounter);
        var userContent = _balanceSimulatorService.BuildGenerationUserPrompt(project, report);
        LogContextBudget(log, "balance-simulator", userContent);
        log("Balance Simulator: issues=" + report.Issues.Count + ", recommendations=" + report.Recommendations.Count + ", combat encounters=" + report.Combat.SimulatedEncounterCount + ".");

        var text = await SendPresetAsync(project, settings, Prompts.GameBalanceRebalancePatchJson, new[]
        {
            new ChatMessage { Role = "system", Content = Prompts.GameBalanceRebalancePatchJson.SystemPrompt },
            new ChatMessage { Role = "user", Content = userContent }
        }, log, "balance-simulator", cancellationToken);

        var draft = await CreateGeneratedProjectDraftAsync(project, "balance-simulator", text, log, cancellationToken);
        if (draft != null)
        {
            log($"Balance Simulator draft сохранён: {draft.SessionId}. Файлов: {draft.Files.Count}. Ошибок: {draft.Validation.Errors.Count}, предупреждений: {draft.Validation.Warnings.Count}.");
            log("Draft не применён автоматически. Проверьте и примените вручную, если он подходит.");
        }

        await SaveStageTextAsync(project, $"balance-simulator_{DateTime.Now:yyyyMMdd_HHmmss}.json.txt", text, cancellationToken);
        return text;
    }

    public async Task<string> BuildNextMvpDraftAsync(
        GameProjectData project,
        LmStudioSettings settings,
        Action<string> log,
        CancellationToken cancellationToken = default)
    {
        log("MVP Orchestrator: проверяю готовность playable MVP.");
        var report = _mvpOrchestratorService.BuildReadinessReport(project);
        var recommendation = _mvpOrchestratorService.DetermineNextStage(project, report);
        log("MVP Orchestrator: " + report.Summary);

        if (recommendation == null)
        {
            var message = "MVP Orchestrator: генерация не требуется. Проверьте playable flow и drafts вручную.";
            log(message);
            return message;
        }

        if (recommendation.Stage == "design_profile")
        {
            var message = "MVP Orchestrator: сначала заполните дизайн-досье/brief/MVP plan. Эта стадия не автоматизирована как JSON draft в v1.";
            log(message);
            return message;
        }

        log($"MVP Orchestrator: выбран следующий draft stage={recommendation.Stage}, category={recommendation.SuggestedCategory}, count={recommendation.SuggestedCount}.");
        var userRules = _mvpOrchestratorService.BuildNextStageUserRules(project, report, recommendation);
        return recommendation.Stage switch
        {
            "stats_resources" => await BuildStatsAndResourcesBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "formulas" => await BuildFormulasBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "actions" => await BuildGameplayActionsBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "world_state" => await BuildWorldStateBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "locations" => await BuildLocationsBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "scenes" => await BuildScenesBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "items" => await BuildItemsBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "equipment" => await BuildEquipmentBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "skills" => await BuildSkillsBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "spells" => await BuildSpellsBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "encounters" => await BuildEncountersBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "combat" => await BuildCombatBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "progression" => await BuildProgressionBatchAsync(project, settings, userRules, recommendation.SuggestedCount, recommendation.SuggestedCategory, log, cancellationToken),
            "random_events" => await BuildRandomDirectorDraftAsync(project, settings, recommendation.SuggestedCount, log, cancellationToken),
            "balance" => await BuildBalanceRebalanceDraftAsync(project, settings, 10, log, cancellationToken),
            _ => BuildUnsupportedMvpStageMessage(recommendation, log)
        };
    }

    public Task<string> BuildItemsBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateItemsBatch, "items", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildEquipmentBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateEquipmentBatch, "equipment", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildSkillsBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateSkillsBatch, "skills", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildSpellsBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateSpellsBatch, "spells", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildLocationsBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateLocationsBatch, "locations", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildScenesBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateScenesBatch, "scenes", userRules, count, category, log, cancellationToken);
    }

    public Task<string> BuildEncountersBatchAsync(GameProjectData project, LmStudioSettings settings, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken = default)
    {
        return BuildContentBatchAsync(project, settings, Prompts.GenerateEncountersBatch, "encounters", userRules, count, category, log, cancellationToken);
    }

    public async Task<string> ReviewBatchAsync(GameProjectData project, LmStudioSettings settings, string batchJson, Action<string> log, CancellationToken cancellationToken = default)
    {
        log("Reviewing generated batch against existing content.");
        return await SendPresetAsync(project, settings, Prompts.ReviewGeneratedBatchAgainstExistingContent, new[]
        {
            new ChatMessage { Role = "system", Content = Prompts.ReviewGeneratedBatchAgainstExistingContent.SystemPrompt },
            new ChatMessage { Role = "user", Content = BuildBatchUserContent(project, string.Empty, 0, "review", "review", batchJson) }
        }, log, "review", cancellationToken);
    }

    private async Task<string> BuildContentBatchAsync(GameProjectData project, LmStudioSettings settings, PromptPreset preset, string stage, string userRules, int count, string category, Action<string> log, CancellationToken cancellationToken)
    {
        log("Generating content batch: " + stage);
        var userContent = BuildBatchUserContent(project, userRules, count, category, stage);
        LogContextBudget(log, stage, userContent);
        var text = await SendPresetAsync(project, settings, preset, new[]
        {
            new ChatMessage { Role = "system", Content = preset.SystemPrompt },
            new ChatMessage { Role = "user", Content = userContent }
        }, log, stage, cancellationToken);

        await CreateGeneratedProjectDraftAsync(project, stage, text, log, cancellationToken);
        await SaveStageTextAsync(project, $"{stage}_{DateTime.Now:yyyyMMdd_HHmmss}.json.txt", text, cancellationToken);
        return text;
    }

    private async Task<string> RunTextStageAsync(GameProjectData project, LmStudioSettings settings, PromptPreset preset, string userContent, string fileName, Action<string> assign, Action<string> log, CancellationToken cancellationToken)
    {
        log($"Running pipeline stage: {preset.Name}");
        var stage = Path.GetFileNameWithoutExtension(fileName);
        var text = await SendPresetAsync(project, settings, preset, new[]
        {
            new ChatMessage { Role = "system", Content = preset.SystemPrompt },
            new ChatMessage { Role = "user", Content = userContent }
        }, log, stage, cancellationToken);
        assign(text);
        await SaveStageTextAsync(project, fileName, text, cancellationToken);
        return text;
    }

    private async Task<string> SendPresetAsync(GameProjectData? project, LmStudioSettings settings, PromptPreset preset, IEnumerable<ChatMessage> messages, Action<string>? log, string stage, CancellationToken cancellationToken)
    {
        var generationSettings = ApplyOutputTokenLimit(preset.Settings);
        var messageList = messages.ToList();
        var estimatedInputTokens = _lmCallDiagnosticsService.EstimateTokens(string.Concat(messageList.Select(x => x.Content ?? string.Empty)), GetApproxCharsPerToken());
        log?.Invoke($"LLM-вызов {stage}: старт, вход ~{estimatedInputTokens} токенов, лимит контекста {GetMaxInputContextTokens()}, max output {generationSettings.MaxTokens}.");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var text = await _lmStudioService.SendAsync(settings, messageList.Select(x => new ApiMessage(x.Role, x.Content)).ToList(), generationSettings, cancellationToken);
            stopwatch.Stop();
            var record = _lmCallDiagnosticsService.CreateSuccessRecord(stage, settings, generationSettings, messageList, GetMaxInputContextTokens(), GetApproxCharsPerToken(), stopwatch.ElapsedMilliseconds, text);
            await TryAppendLmCallDiagnosticsAsync(project, record, log, CancellationToken.None);
            log?.Invoke($"LLM-вызов {stage}: успешно за {stopwatch.ElapsedMilliseconds} мс, ответ {record.ResponseCharacterCount} символов (~{record.EstimatedResponseTokens} токенов).");
            return text;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var record = _lmCallDiagnosticsService.CreateFailureRecord(stage, settings, generationSettings, messageList, GetMaxInputContextTokens(), GetApproxCharsPerToken(), stopwatch.ElapsedMilliseconds, ex);
            await TryAppendLmCallDiagnosticsAsync(project, record, log, CancellationToken.None);
            log?.Invoke($"LLM-вызов {stage}: ошибка за {stopwatch.ElapsedMilliseconds} мс: {record.ErrorMessage}");
            throw;
        }
    }

    private async Task TryAppendLmCallDiagnosticsAsync(GameProjectData? project, LmStudioCallDiagnosticRecord record, Action<string>? log, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(project?.Summary.ProjectPath))
        {
            return;
        }

        try
        {
            await _lmCallDiagnosticsService.AppendAsync(project.Summary.ProjectPath, record, cancellationToken);
        }
        catch (Exception ex)
        {
            log?.Invoke("Не удалось записать диагностику LLM-вызова: " + ex.Message);
        }
    }

    private async Task SaveStageTextAsync(GameProjectData project, string fileName, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            return;
        }

        var folder = Path.Combine(project.Summary.ProjectPath, "prompts", "prompt-history");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);
        await File.WriteAllTextAsync(path, text, cancellationToken);
        project.GenerationSessions.Add(new GenerationSessionSummary
        {
            Id = Ids.New("gen"),
            Stage = fileName,
            OutputPath = path
        });
    }

    private static string BuildConversation(IdeaDiscussionSession session)
    {
        return string.Join(Environment.NewLine + Environment.NewLine, session.Messages.Select(x => $"[{x.Role}]\n{x.Content}"));
    }

    private string BuildProjectContext(GameProjectData project)
    {
        return JsonSerializer.Serialize(new
        {
            project.Meta,
            project.Brief,
            project.Concept,
            project.MvpPlan,
            project.ArchitecturePlan,
            DesignSummary = _designInterviewService.BuildDesignSummary(project),
            RandomDirectorSummary = _randomDirectorService.BuildCompactRandomDirectorSummary(project, _randomDirectorService.BuildReport(project)),
            project.World,
            project.GenerationPreferences,
            project.Stats,
            project.Currencies,
            project.Variables,
            project.Items,
            project.EquipmentSlots,
            project.Elements,
            project.LocationConnections,
            project.LocationStates,
            project.Encounters,
            project.Actions,
            project.Formulas,
            project.StatusEffects,
            project.ProgressionNodes,
            project.Mechanics,
            project.Combat,
            project.Characters,
            project.Locations,
            project.Scenes
        }, _jsonOptions);
    }

    private string BuildBatchUserContent(GameProjectData project, string userRules, int count, string category, string stage, string proposedBatchRaw = "")
    {
        return JsonSerializer.Serialize(new
        {
            Rules = userRules,
            Count = count,
            Category = category,
            Stage = stage,
            ProjectContext = JsonSerializer.Deserialize<object>(BuildCompactProjectContext(project, stage, userRules.Length + proposedBatchRaw.Length)),
            ProposedBatchRaw = proposedBatchRaw
        }, _jsonOptions);
    }

    private string BuildCompactProjectContext(GameProjectData project, string stage, int reservedChars = 0)
    {
        var reservedTokens = _promptBudgetService.EstimateTokens(new string('x', Math.Max(0, reservedChars)), GetApproxCharsPerToken());
        var maxInputTokens = Math.Max(128, GetMaxInputContextTokens() - reservedTokens);
        return _promptBudgetService.SerializeWithinBudget(limit => BuildCompactProjectContextModel(project, stage, limit), maxInputTokens, GetApproxCharsPerToken(), _jsonOptions);
    }

    internal string BuildCompactProjectContextForTests(GameProjectData project, string stage)
    {
        return BuildCompactProjectContext(project, stage);
    }

    private object BuildCompactProjectContextModel(GameProjectData project, string stage, int itemLimit)
    {
        var wasTrimmed = itemLimit < 100;
        var hardTrimmed = itemLimit <= 5;
        var descriptionLimit = hardTrimmed ? 200 : itemLimit <= 12 ? 600 : 1000;
        var sectionLimit = hardTrimmed ? 350 : itemLimit <= 12 ? 900 : 1500;
        var detailLimit = hardTrimmed ? Math.Max(1, itemLimit) : Math.Max(5, itemLimit * 4 / 5);
        var randomDirectorReport = _randomDirectorService.BuildReport(project);
        var includeBalanceSummary = ShouldIncludeBalanceSummary(stage) && !hardTrimmed;
        var balanceReport = includeBalanceSummary ? _balanceSimulatorService.BuildReport(project, 10) : null;
        var includeMvpSummary = ShouldIncludeMvpSummary(stage) && !hardTrimmed;
        var mvpReport = includeMvpSummary ? _mvpOrchestratorService.BuildReadinessReport(project) : null;
        return new
        {
            Stage = stage,
            ContextBudget = new
            {
                MaxInputContextTokens = GetMaxInputContextTokens(),
                ApproxCharsPerToken = GetApproxCharsPerToken(),
                ItemLimit = itemLimit,
                Trimmed = wasTrimmed,
                HardTrimmed = hardTrimmed,
                StagePriority = GetStagePriority(stage),
                Note = hardTrimmed
                    ? "Контекст жёстко ужат: длинные списки и текстовые поля сокращены по бюджету входного контекста."
                    : wasTrimmed ? "Часть длинных списков усечена по бюджету входного контекста." : string.Empty
            },
            Meta = new
            {
                project.Meta.Id,
                project.Meta.Title,
                project.Meta.Genre,
                project.Meta.Tone,
                Description = Preview(project.Meta.Description, descriptionLimit),
                project.Meta.StartSceneId,
                project.Meta.Language
            },
            Brief = Preview(project.Brief.Text, sectionLimit),
            Concept = Preview(project.Concept.Text, sectionLimit),
            Mvp = Preview(project.MvpPlan.Text, sectionLimit),
            Architecture = Preview(project.ArchitecturePlan.Text, sectionLimit),
            DesignSummary = hardTrimmed ? "trimmed" : Preview(_designInterviewService.BuildDesignSummary(project), sectionLimit),
            RandomDirectorSummary = hardTrimmed
                ? "trimmed"
                : JsonSerializer.Deserialize<object>(_randomDirectorService.BuildCompactRandomDirectorSummary(project, randomDirectorReport)),
            BalanceSummary = balanceReport == null
                ? null
                : JsonSerializer.Deserialize<object>(_balanceSimulatorService.BuildCompactBalanceSummary(project, balanceReport)),
            MvpSummary = mvpReport == null
                ? null
                : JsonSerializer.Deserialize<object>(_mvpOrchestratorService.BuildCompactMvpSummary(project, mvpReport)),
            GenerationPreferences = BuildCompactGenerationPreferences(project.GenerationPreferences, itemLimit),
            Counts = new
            {
                Stats = project.Stats.Count,
                Currencies = project.Currencies.Count,
                Variables = project.Variables.Count,
                Items = project.Items.Count,
                EquipmentSlots = project.EquipmentSlots.Count,
                Elements = project.Elements.Count,
                Skills = project.Skills.Count,
                Locations = project.Locations.Count,
                LocationConnections = project.LocationConnections.Count,
                LocationStates = project.LocationStates.Count,
                Scenes = project.Scenes.Count,
                Quests = project.Quests.Count,
                Encounters = project.Encounters.Count,
                Actions = project.Actions.Count,
                Formulas = project.Formulas.Count,
                StatusEffects = project.StatusEffects.Count,
                ProgressionNodes = project.ProgressionNodes.Count,
                WorldStateEnabled = project.WorldState.Enabled,
                TimeSegments = project.WorldState.Time.Segments.Count,
                WorldAspects = project.WorldState.Aspects.Count,
                AmbientEvents = project.WorldState.AmbientEvents.Count,
                WorldRules = project.WorldState.Rules.Count,
                CombatEnabled = project.Combat?.Enabled == true,
                CombatEncounters = project.Encounters.Count(x => string.Equals(x.Kind, "combat", StringComparison.OrdinalIgnoreCase) || x.Combatants.Count > 0),
                CombatActions = project.Actions.Count(x => x.AvailableInCombat),
                ImagePrompts = project.ImagePrompts.Count
            },
            ExistingIds = new
            {
                Stats = project.Stats.Take(itemLimit).Select(x => x.Id),
                Currencies = project.Currencies.Take(itemLimit).Select(x => x.Id),
                Variables = project.Variables.Take(itemLimit).Select(x => x.Id),
                EquipmentSlots = project.EquipmentSlots.Take(itemLimit).Select(x => x.Id),
                Elements = project.Elements.Take(itemLimit).Select(x => x.Id),
                Items = project.Items.Take(itemLimit).Select(x => x.Id),
                Skills = project.Skills.Take(itemLimit).Select(x => x.Id),
                Locations = project.Locations.Take(itemLimit).Select(x => x.Id),
                LocationConnections = project.LocationConnections.Take(itemLimit).Select(x => x.Id),
                LocationStates = project.LocationStates.Take(itemLimit).Select(x => x.Id),
                Scenes = project.Scenes.Take(itemLimit).Select(x => x.Id),
                Quests = project.Quests.Take(itemLimit).Select(x => x.Id),
                Encounters = project.Encounters.Take(itemLimit).Select(x => x.Id),
                Actions = project.Actions.Take(itemLimit).Select(x => x.Id),
                Formulas = project.Formulas.Take(itemLimit).Select(x => x.Id),
                StatusEffects = project.StatusEffects.Take(itemLimit).Select(x => x.Id),
                ProgressionNodes = project.ProgressionNodes.Take(itemLimit).Select(x => x.Id),
                TimeSegments = project.WorldState.Time.Segments.Take(itemLimit).Select(x => x.Id),
                WorldAspects = project.WorldState.Aspects.Take(itemLimit).Select(x => x.Id),
                AmbientEvents = project.WorldState.AmbientEvents.Take(itemLimit).Select(x => x.Id),
                WorldRules = project.WorldState.Rules.Take(itemLimit).Select(x => x.Id),
                ImagePrompts = project.ImagePrompts.Take(itemLimit).Select(x => x.AssetId)
            },
            WorldState = BuildCompactWorldState(project.WorldState, detailLimit),
            Stats = project.Stats.Take(itemLimit).Select(x => new { x.Id, x.Name, x.Kind, x.IsResource }),
            Currencies = project.Currencies.Take(itemLimit).Select(x => new { x.Id, x.Name }),
            Variables = project.Variables.Take(itemLimit).Select(x => new { x.Id, x.Name, x.IsHidden }),
            EquipmentSlots = project.EquipmentSlots.Take(itemLimit).Select(x => new { x.Id, x.Name, AllowedTags = x.AllowedItemTags }),
            Elements = project.Elements.Take(itemLimit).Select(x => new { x.Id, x.Name }),
            Items = project.Items.Take(itemLimit).Select(x => new { x.Id, x.Name, x.Type, x.SlotId, x.Tags, x.IsEquippable, x.IsConsumable, x.IsUsable }),
            Skills = project.Skills.Take(itemLimit).Select(x => new { x.Id, x.Name, x.Kind, x.ElementId, x.Tags, x.ExperienceToNextLevel }),
            Locations = project.Locations.Take(itemLimit).Select(x => new { x.Id, x.Name, x.StatusId, x.Tags }),
            LocationConnections = project.LocationConnections.Take(itemLimit).Select(x => new { x.Id, x.FromLocationId, x.ToLocationId, x.IsTwoWay }),
            LocationStates = project.LocationStates.Take(itemLimit).Select(x => new { x.Id, x.LocationId, x.Name }),
            Scenes = project.Scenes.Take(detailLimit).Select(x => new
            {
                x.Id,
                x.Title,
                x.LocationId,
                ChoiceCount = x.Choices.Count,
                NextSceneIds = x.Choices.Select(c => c.NextSceneId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct()
            }),
            Quests = project.Quests.Take(detailLimit).Select(x => new { x.Id, x.Title }),
            Combat = project.Combat,
            Encounters = project.Encounters.Take(detailLimit).Select(x => new { x.Id, x.Name, x.Kind, x.SceneId, x.VictorySceneId, x.DefeatSceneId, Combatants = x.Combatants.Select(c => new { c.Id, c.Name, c.Team, c.IsPlayer, c.ActionIds }) }),
            Actions = project.Actions.Take(detailLimit).Select(x => new { x.Id, x.Name, x.Kind, x.Tags, x.AvailableInCombat, x.ActorTeam, x.TargetScope, Effects = x.Effects.Select(e => new { e.Type, e.TargetId }) }),
            Formulas = project.Formulas.Take(detailLimit).Select(x => new { x.Id, x.Name, x.Expression }),
            StatusEffects = project.StatusEffects.Take(detailLimit).Select(x => new { x.Id, x.Name, x.Kind, x.Tags }),
            ProgressionNodes = project.ProgressionNodes.Take(detailLimit).Select(x => new { x.Id, x.Name, x.Kind, x.SkillId, x.ParentNodeIds }),
            Mechanics = new
            {
                project.Mechanics.EnableTurns,
                project.Mechanics.EnableStatusEffects,
                project.Mechanics.EnableProgression,
                project.Mechanics.EnableActionPanel,
                project.Mechanics.EnableDiceRandomness,
                project.Mechanics.DefaultActionPoints,
                project.Mechanics.ActionPointStatId,
                project.Mechanics.InitiativeFormulaId,
                Experience = BuildCompactExperience(project.Mechanics.Experience, hardTrimmed),
                Notes = Preview(project.Mechanics.Notes, hardTrimmed ? 300 : 1000)
            }
        };
    }

    private static object BuildCompactGenerationPreferences(GameGenerationPreferences preferences, int itemLimit)
    {
        var maxLength = itemLimit <= 5 ? 300 : itemLimit <= 12 ? 900 : 1500;
        return new
        {
            GeneralGameplayText = Preview(preferences.GeneralGameplayText, maxLength),
            SkillDesignText = Preview(preferences.SkillDesignText, maxLength),
            ProgressionDesignText = Preview(preferences.ProgressionDesignText, maxLength),
            CombatDesignText = Preview(preferences.CombatDesignText, maxLength),
            AtmosphereDesignText = Preview(preferences.AtmosphereDesignText, maxLength),
            BalanceText = Preview(preferences.BalanceText, maxLength),
            ForbiddenDesignText = Preview(preferences.ForbiddenDesignText, maxLength),
            Notes = Preview(preferences.Notes, maxLength)
        };
    }

    private static object BuildCompactWorldState(GameWorldStateDefinition worldState, int itemLimit)
    {
        return new
        {
            worldState.Enabled,
            worldState.GenreProfile,
            Time = new
            {
                worldState.Time.Enabled,
                worldState.Time.DayLabel,
                worldState.Time.SegmentLabel,
                worldState.Time.StartSegmentId,
                worldState.Time.AdvanceSegmentsOnEndTurn,
                worldState.Time.AdvanceSegmentsOnTravel,
                worldState.Time.AdvanceSegmentsOnAction,
                Segments = worldState.Time.Segments
                    .OrderBy(x => x.Order)
                    .Take(itemLimit)
                    .Select(x => new { x.Id, x.Name, x.Order, x.NextSegmentId, x.Tags })
            },
            Aspects = worldState.Aspects.Take(itemLimit).Select(x => new
            {
                x.Id,
                x.Name,
                x.Kind,
                x.DefaultStateId,
                x.Tags,
                States = x.States.Take(itemLimit).Select(s => new { s.Id, s.Name, s.Kind, s.Tags })
            }),
            AmbientEventsCount = worldState.AmbientEvents.Count,
            RulesCount = worldState.Rules.Count,
            WorldStateRequirementEffectUsage = new
            {
                Requirements = worldState.Rules.SelectMany(x => x.Requirements)
                    .Concat(worldState.AmbientEvents.SelectMany(x => x.Requirements))
                    .Where(x => IsWorldStateDslType(x.Type))
                    .Take(itemLimit)
                    .Select(x => new { x.Type, x.TargetId, x.Operator, x.StringValue, x.Text }),
                Effects = worldState.Rules.SelectMany(x => x.Effects)
                    .Concat(worldState.AmbientEvents.SelectMany(x => x.Effects))
                    .Where(x => IsWorldStateDslType(x.Type))
                    .Take(itemLimit)
                    .Select(x => new { x.Type, x.TargetId, x.Amount, x.StringValue, x.Text })
            }
        };
    }

    private static bool IsWorldStateDslType(string type)
    {
        return type.Equals("timeSegment", StringComparison.OrdinalIgnoreCase)
            || type.Equals("dayNumber", StringComparison.OrdinalIgnoreCase)
            || type.Equals("worldState", StringComparison.OrdinalIgnoreCase)
            || type.Equals("worldAspect", StringComparison.OrdinalIgnoreCase)
            || type.Equals("advanceTime", StringComparison.OrdinalIgnoreCase);
    }

    private static object BuildCompactExperience(GameExperienceDefinition experience, bool hardTrimmed)
    {
        var notesLimit = hardTrimmed ? 300 : 1000;
        return new
        {
            experience.EnablePlayerExperience,
            experience.EnableSkillExperience,
            experience.InitialPlayerLevel,
            experience.InitialPlayerExperience,
            experience.MaxPlayerLevel,
            experience.PlayerExperienceToNextLevelFormulaId,
            experience.PlayerExperienceToNextLevelFormulaExpression,
            experience.SkillExperienceToNextLevelFormulaId,
            experience.SkillExperienceToNextLevelFormulaExpression,
            experience.DefaultPlayerExperienceRewardFormulaId,
            experience.DefaultPlayerExperienceRewardFormulaExpression,
            PlayerLevelUpEffectCount = experience.PlayerLevelUpEffects.Count,
            SkillLevelUpEffectCount = experience.SkillLevelUpEffects.Count,
            Notes = Preview(experience.Notes, notesLimit)
        };
    }

    private static string GetStagePriority(string stage)
    {
        return stage switch
        {
            "initial-content" => "Всегда учитывать meta, brief, concept, MVP, architecture, generationPreferences и минимальный играбельный набор сущностей.",
            "skills" or "spells" => "Приоритет: stats, elements, formulas, statusEffects, existing skills, progression summary, generationPreferences.",
            "progression" => "Приоритет: stats, currencies, variables, skills, formulas, actions summary, generationPreferences.",
            "gameplay-actions" => "Приоритет: stats, skills, items, formulas, statusEffects, locations, generationPreferences.",
            "scenes" => "Приоритет: locations, characters, quests, known actions summary, generationPreferences.",
            "items" or "equipment" => "Приоритет: stats, equipmentSlots, currencies, tags, generationPreferences.",
            "encounters" => "Приоритет: scenes, actions, statusEffects, formulas, generationPreferences.",
            "mvp-orchestrator" => "Приоритет: compact MVP readiness, next recommended stage, existing ids и draft-only workflow.",
            "revision-fix" => "Приоритет: явно запрошенные исправления, existing ids, generationPreferences, связанные сущности и валидность проекта.",
            "change-request" => "Приоритет: user change request, deterministic impact report, patch plan, affected systems, existing ids и draft-only workflow.",
            _ => "Приоритет: stage-specific данные, existing ids, counts, generationPreferences и связанные механики."
        };
    }

    private static bool ShouldIncludeBalanceSummary(string stage)
    {
        return stage.Equals("balance-simulator", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("rebalance", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("combat", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("progression", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("stats-resources", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("items", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldIncludeMvpSummary(string stage)
    {
        return stage.Equals("mvp-orchestrator", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("stats-resources", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("formulas", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("gameplay-actions", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("world-state", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("locations", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("scenes", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("items", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("equipment", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("skills", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("spells", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("encounters", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("combat", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("progression", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("random-director", StringComparison.OrdinalIgnoreCase)
            || stage.Equals("balance-simulator", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUnsupportedMvpStageMessage(GameMvpRecommendation recommendation, Action<string> log)
    {
        var message = "MVP Orchestrator: стадия '" + recommendation.Stage + "' не автоматизирована в v1. Рекомендация сохранена в отчёте, draft не создан.";
        log(message);
        return message;
    }

    private static string Preview(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "...";
    }

    internal void ApplyGeneratedProjectJson(GameProjectData project, string text, Action<string> log)
    {
        ApplyGeneratedProjectJsonCore(project, text, log, null).GetAwaiter().GetResult();
    }

    internal async Task ApplyGeneratedProjectJsonAsync(GameProjectData project, string text, Action<string> log, CancellationToken cancellationToken = default)
    {
        await ApplyGeneratedProjectJsonCore(project, text, log, cancellationToken);
    }

    private async Task ApplyGeneratedProjectJsonCore(GameProjectData project, string text, Action<string> log, CancellationToken? cancellationToken)
    {
        var json = ExtractJson(text);
        try
        {
            var generated = JsonSerializer.Deserialize<GameProjectData>(json, _jsonOptions);
            if (generated == null)
            {
                log("Generated project JSON was empty; keeping current data.");
                return;
            }

            _repairService.PreserveIdentity(project, generated, log);
            _repairService.ApplySafeRepairs(generated, log);
            GameDraftSession? draft = null;
            if (cancellationToken.HasValue && !string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
            {
                draft = await _draftService.ExtractGeneratedProjectAsync(project, "initial-content", generated, text, cancellationToken.Value);
                log("Generated raw output saved to draft: " + draft.RawOutputFile);
            }

            var candidate = _cloneService.Clone(project);
            MergeGeneratedProjectData(candidate, generated);
            var validation = _validator.Validate(candidate);
            foreach (var warning in validation.Warnings)
            {
                log("Generated project warning: " + warning);
            }
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    log("Generated project error: " + error);
                }
                if (draft != null && cancellationToken.HasValue)
                {
                    if (draft.Validation.IsValid)
                    {
                        foreach (var file in draft.Files)
                        {
                            file.Status = "Draft";
                        }

                        draft.Validation.Warnings.Add("Merged project validation failed; draft was not applied.");
                    }

                    await _draftService.SaveDraftManifestAsync(project, draft, cancellationToken.Value);
                }
                log("Generated content JSON was not applied because validation failed. Raw output was saved for manual review.");
                return;
            }

            _cloneService.CopyMutableData(candidate, project);
            if (draft != null)
            {
                foreach (var file in draft.Files)
                {
                    file.Status = "Applied";
                }

                if (cancellationToken.HasValue)
                {
                    await _draftService.SaveDraftManifestAsync(project, draft, cancellationToken.Value);
                }
            }

            log("Generated content JSON applied transactionally.");
        }
        catch (Exception ex)
        {
            log("Could not parse generated project JSON; raw text saved for manual review. " + ex.Message);
        }
    }

    private async Task<GameDraftSession?> CreateGeneratedProjectDraftAsync(GameProjectData project, string stage, string rawText, Action<string> log, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            log("Project path is not set; generated batch cannot be saved as draft.");
            return null;
        }

        var json = ExtractJson(rawText);
        try
        {
            var generated = JsonSerializer.Deserialize<GameProjectData>(json, _jsonOptions);
            if (generated == null)
            {
                var emptyDraft = await _draftService.SaveRawDraftAsync(project, stage, string.Empty, rawText, cancellationToken);
                emptyDraft.Validation.IsValid = false;
                emptyDraft.Validation.Errors.Add("Generated batch JSON was empty.");
                await _draftService.SaveDraftManifestAsync(project, emptyDraft, cancellationToken);
                await _draftService.SaveValidationReportAsync(project, emptyDraft, cancellationToken);
                log("Пачка сохранена как невалидный draft: JSON пустой.");
                return emptyDraft;
            }

            _repairService.PreserveIdentity(project, generated, log);
            _repairService.ApplySafeRepairs(generated, log);
            var draft = await _draftService.ExtractGeneratedProjectAsync(project, stage, generated, rawText, cancellationToken);

            var candidate = _cloneService.Clone(project);
            MergeGeneratedProjectData(candidate, generated);
            var validation = _validator.Validate(candidate);
            draft.Validation = validation;
            foreach (var warning in validation.Warnings)
            {
                log("Draft candidate warning: " + warning);
            }

            if (validation.IsValid)
            {
                foreach (var file in draft.Files)
                {
                    file.Status = "Draft";
                }

                log("Пачка сохранена как draft и не применена. Для внесения в проект нажмите Применить последний draft.");
            }
            else
            {
                foreach (var error in validation.Errors)
                {
                    log("Draft candidate error: " + error);
                }
                foreach (var file in draft.Files)
                {
                    file.Status = "Invalid";
                }

                log("Пачка сохранена как невалидный draft и не применена.");
            }

            await _draftService.SaveDraftManifestAsync(project, draft, cancellationToken);
            await _draftService.SaveValidationReportAsync(project, draft, cancellationToken);
            return draft;
        }
        catch (Exception ex)
        {
            var draft = await _draftService.SaveRawDraftAsync(project, stage, string.Empty, rawText, cancellationToken);
            draft.Validation.IsValid = false;
            draft.Validation.Errors.Add("Could not parse generated batch as GameProjectData: " + ex.Message);
            await _draftService.SaveDraftManifestAsync(project, draft, cancellationToken);
            await _draftService.SaveValidationReportAsync(project, draft, cancellationToken);
            log("Could not parse generated batch JSON; raw text saved as invalid draft. " + ex.Message);
            return draft;
        }
    }

    private void MergeGeneratedProjectData(GameProjectData current, GameProjectData generated)
    {
        if (!string.IsNullOrWhiteSpace(generated.Meta.Genre)) current.Meta.Genre = generated.Meta.Genre;
        if (!string.IsNullOrWhiteSpace(generated.Meta.Tone)) current.Meta.Tone = generated.Meta.Tone;
        if (!string.IsNullOrWhiteSpace(generated.Meta.Description)) current.Meta.Description = generated.Meta.Description;
        if (!string.IsNullOrWhiteSpace(generated.Meta.VisualStyle)) current.Meta.VisualStyle = generated.Meta.VisualStyle;
        if (!string.IsNullOrWhiteSpace(generated.Meta.Language)) current.Meta.Language = generated.Meta.Language;
        if (!string.IsNullOrWhiteSpace(generated.World.Summary) || generated.World.Lore.Count > 0 || generated.World.Factions.Count > 0 || generated.World.Rules.Count > 0)
        {
            current.World = generated.World;
        }

        UpsertMany(current.Stats, generated.Stats, x => x.Id, id => id);
        UpsertMany(current.Skills, generated.Skills, x => x.Id, id => id);
        UpsertMany(current.Items, generated.Items, x => x.Id, id => id);
        UpsertMany(current.EquipmentSlots, generated.EquipmentSlots, x => x.Id, id => id);
        UpsertMany(current.Elements, generated.Elements, x => x.Id, id => id);
        UpsertMany(current.Currencies, generated.Currencies, x => x.Id, id => id);
        UpsertMany(current.Variables, generated.Variables, x => x.Id, id => id);
        UpsertMany(current.Characters, generated.Characters, x => x.Id, id => id);
        UpsertMany(current.Relationships, generated.Relationships, x => x.CharacterId, id => id);
        UpsertMany(current.Locations, generated.Locations, x => x.Id, id => id);
        UpsertMany(current.LocationConnections, generated.LocationConnections, x => x.Id, id => id);
        UpsertMany(current.LocationStates, generated.LocationStates, x => x.Id, id => id);
        UpsertMany(current.Scenes, generated.Scenes, x => x.Id, id => id);
        UpsertMany(current.Quests, generated.Quests, x => x.Id, id => id);
        UpsertMany(current.Encounters, generated.Encounters, x => x.Id, id => id);
        UpsertMany(current.Actions, generated.Actions, x => x.Id, id => id);
        UpsertMany(current.Formulas, generated.Formulas, x => x.Id, id => id);
        UpsertMany(current.StatusEffects, generated.StatusEffects, x => x.Id, id => id);
        UpsertMany(current.ProgressionNodes, generated.ProgressionNodes, x => x.Id, id => id);
        UpsertMany(current.GeneratedImageCandidates, generated.GeneratedImageCandidates, x => x.CandidateId, id => id);
        UpsertMany(current.AssetLinks, generated.AssetLinks, x => x.AssetId, id => id);

        if (HasMechanicsData(generated.Mechanics))
        {
            current.Mechanics = generated.Mechanics;
        }
        if (GameDraftService.HasWorldStateData(generated.WorldState))
        {
            GameWorldStateMergeService.MergeInto(current.WorldState, generated.WorldState);
        }
        if (HasGenerationPreferencesData(generated.GenerationPreferences))
        {
            current.GenerationPreferences = generated.GenerationPreferences;
        }

        if (generated.Combat != null)
        {
            current.Combat = generated.Combat;
        }

        if (!string.IsNullOrWhiteSpace(generated.Meta.StartSceneId) && current.Scenes.Any(x => x.Id == generated.Meta.StartSceneId))
        {
            current.Meta.StartSceneId = generated.Meta.StartSceneId;
        }
    }

    private static void UpsertMany<T>(List<T> target, List<T> source, Func<T, string> getId, Func<string, string> normalize)
    {
        foreach (var item in source)
        {
            var id = normalize(getId(item));
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var index = target.FindIndex(x => string.Equals(getId(x), id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                target[index] = item;
            }
            else
            {
                target.Add(item);
            }
        }
    }

    private static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        var firstObject = trimmed.IndexOf('{');
        var firstArray = trimmed.IndexOf('[');
        var start = firstArray >= 0 && (firstObject < 0 || firstArray < firstObject) ? firstArray : firstObject;
        if (start < 0)
        {
            return trimmed;
        }

        var endObject = trimmed.LastIndexOf('}');
        var endArray = trimmed.LastIndexOf(']');
        var end = Math.Max(endObject, endArray);
        return end > start ? trimmed[start..(end + 1)] : trimmed;
    }

    private static bool HasMechanicsData(GameMechanicsDefinition mechanics)
    {
        return mechanics.EnableTurns
            || mechanics.EnableStatusEffects
            || mechanics.EnableProgression
            || mechanics.EnableActionPanel
            || mechanics.EnableDiceRandomness
            || mechanics.DefaultActionPoints != 1
            || !string.IsNullOrWhiteSpace(mechanics.ActionPointStatId)
            || !string.IsNullOrWhiteSpace(mechanics.InitiativeFormulaId)
            || HasExperienceData(mechanics.Experience)
            || !string.IsNullOrWhiteSpace(mechanics.Notes);
    }

    private static bool HasExperienceData(GameExperienceDefinition experience)
    {
        return experience.EnablePlayerExperience
            || experience.EnableSkillExperience
            || experience.InitialPlayerLevel != 1
            || experience.InitialPlayerExperience != 0
            || experience.MaxPlayerLevel != 100
            || !string.IsNullOrWhiteSpace(experience.PlayerExperienceToNextLevelFormulaId)
            || !string.IsNullOrWhiteSpace(experience.PlayerExperienceToNextLevelFormulaExpression)
            || !string.IsNullOrWhiteSpace(experience.SkillExperienceToNextLevelFormulaId)
            || !string.IsNullOrWhiteSpace(experience.SkillExperienceToNextLevelFormulaExpression)
            || !string.IsNullOrWhiteSpace(experience.DefaultPlayerExperienceRewardFormulaId)
            || !string.IsNullOrWhiteSpace(experience.DefaultPlayerExperienceRewardFormulaExpression)
            || experience.PlayerLevelUpEffects.Count > 0
            || experience.SkillLevelUpEffects.Count > 0
            || !string.IsNullOrWhiteSpace(experience.Notes);
    }

    private static bool HasGenerationPreferencesData(GameGenerationPreferences preferences)
    {
        return !string.IsNullOrWhiteSpace(preferences.GeneralGameplayText)
            || !string.IsNullOrWhiteSpace(preferences.SkillDesignText)
            || !string.IsNullOrWhiteSpace(preferences.ProgressionDesignText)
            || !string.IsNullOrWhiteSpace(preferences.CombatDesignText)
            || !string.IsNullOrWhiteSpace(preferences.AtmosphereDesignText)
            || !string.IsNullOrWhiteSpace(preferences.BalanceText)
            || !string.IsNullOrWhiteSpace(preferences.ForbiddenDesignText)
            || !string.IsNullOrWhiteSpace(preferences.Notes);
    }

    private GenerationSettings ApplyOutputTokenLimit(GenerationSettings preset)
    {
        var maxOutputTokens = GenerationSettingsUi.MaxOutputTokens > 0 ? GenerationSettingsUi.MaxOutputTokens : GenerationSettingsUi.MaxTokens;
        return maxOutputTokens > 0 ? preset with { MaxTokens = maxOutputTokens } : preset;
    }

    private void LogContextBudget(Action<string> log, string stage, string context)
    {
        var estimatedTokens = _promptBudgetService.EstimateTokens(context, GetApproxCharsPerToken());
        log($"Контекст {stage}: примерно {estimatedTokens} токенов из лимита {GetMaxInputContextTokens()}.");
        if (estimatedTokens > GetMaxInputContextTokens())
        {
            log($"Предупреждение: итоговый prompt {stage} оценивается выше лимита входного контекста.");
        }
    }

    private int GetMaxInputContextTokens()
    {
        return GenerationSettingsUi.MaxInputContextTokens > 0 ? GenerationSettingsUi.MaxInputContextTokens : 32768;
    }

    private int GetApproxCharsPerToken()
    {
        return GenerationSettingsUi.ApproxCharsPerToken > 0 ? GenerationSettingsUi.ApproxCharsPerToken : 4;
    }
}
