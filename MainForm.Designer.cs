using System;
using System.Drawing;
using System.Windows.Forms;

#nullable enable

namespace LMStudioSillyTavernWorldBuilder;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel rootLayout = null!;
    private TableLayoutPanel topLayout = null!;
    private Label lblGamesRoot = null!;
    private TextBox txtGamesRoot = null!;
    private Button btnBrowseGamesRoot = null!;
    private Button btnOpenGameFolder = null!;
    private Button btnRefreshProjects = null!;
    private Button btnStopOperation = null!;
    private Label lblStatus = null!;
    private TabControl tabMain = null!;
    private TabPage tabProjects = null!;
    private TabPage tabDiscussion = null!;
    private TabPage tabGameCrafter = null!;
    private TabPage tabBrief = null!;
    private TabPage tabContent = null!;
    private TabPage tabPipeline = null!;
    private TabPage tabAssets = null!;
    private TabPage tabPlay = null!;
    private TabPage tabSaves = null!;
    private TabPage tabLogs = null!;
    private TabPage tabSettings = null!;

    private SplitContainer projectsSplit = null!;
    private ListBox lstProjects = null!;
    private TableLayoutPanel projectButtonsLayout = null!;
    private Button btnNewGame = null!;
    private Button btnLoadGame = null!;
    private Button btnSaveGame = null!;
    private Button btnSaveGameAs = null!;
    private Button btnOpenDraftsFolder = null!;
    private Button btnOpenDataFolder = null!;
    private Button btnResaveSplitJson = null!;
    private Button btnValidateProject = null!;
    private Button btnDeleteProject = null!;
    private Label lblValidationResult = null!;
    private PropertyGrid pgProject = null!;

    private TableLayoutPanel discussionLayout = null!;
    private TextBox txtDiscussion = null!;
    private TextBox txtUserInput = null!;
    private FlowLayoutPanel discussionButtons = null!;
    private Button btnStartDiscussion = null!;
    private Button btnSend = null!;
    private Button btnAskGenre = null!;
    private Button btnAskWorld = null!;
    private Button btnAskHero = null!;
    private Button btnAskMechanics = null!;
    private Button btnAskVisualStyle = null!;
    private Button btnBuildBrief = null!;
    private Button btnBuildConcept = null!;
    private Button btnBuildMvp = null!;
    private Button btnBuildStructure = null!;
    private Button btnGenerateContent = null!;

    private TableLayoutPanel gameCrafterLayout = null!;
    private TableLayoutPanel gameCrafterTopLayout = null!;
    private Label lblGameCrafterIdea = null!;
    private TextBox txtGameCrafterIdea = null!;
    private Label lblGameCreationMode = null!;
    private ComboBox cmbGameCreationMode = null!;
    private FlowLayoutPanel gameCrafterButtons = null!;
    private Button btnDesignApplyIdea = null!;
    private Button btnDesignRefreshQuestions = null!;
    private Button btnDesignAskLlmAssumptions = null!;
    private Button btnDesignBuildPlan = null!;
    private Button btnDesignSave = null!;
    private Button btnRandomDirectorCheck = null!;
    private Button btnRandomDirectorGenerate = null!;
    private Label lblRandomDirectorEventCount = null!;
    private TextBox txtRandomDirectorEventCount = null!;
    private Label lblBalanceSimulationRuns = null!;
    private TextBox txtBalanceSimulationRuns = null!;
    private Button btnBalanceCheck = null!;
    private Button btnBalanceGenerateDraft = null!;
    private Button btnMvpCheck = null!;
    private Button btnMvpGenerateNextDraft = null!;
    private Label lblChangeRequest = null!;
    private TextBox txtChangeRequest = null!;
    private Button btnChangeRequestAnalyze = null!;
    private Button btnChangeRequestGenerate = null!;
    private Label lblDesignConversation = null!;
    private TextBox txtDesignConversation = null!;
    private Label lblDesignConversationFocus = null!;
    private TextBox txtDesignConversationFocus = null!;
    private Button btnDesignConversationSend = null!;
    private SplitContainer gameCrafterSplit = null!;
    private TableLayoutPanel gameCrafterLeftLayout = null!;
    private ListView lvDesignSlots = null!;
    private ListView lvDesignQuestions = null!;
    private TextBox txtDesignAnswer = null!;
    private Button btnDesignApplyAnswer = null!;
    private TextBox txtDesignPreview = null!;

    private TableLayoutPanel briefLayout = null!;
    private TextBox txtBriefConcept = null!;
    private FlowLayoutPanel briefButtons = null!;
    private Button btnApproveBrief = null!;
    private Button btnApproveConcept = null!;
    private Button btnApplyRevision = null!;

    private TabControl tabContentInner = null!;
    private TabPage tabWorld = null!;
    private TabPage tabCharacters = null!;
    private TabPage tabScenes = null!;
    private TabPage tabItems = null!;
    private TabPage tabStats = null!;
    private TabPage tabRelationships = null!;
    private TabPage tabCombat = null!;
    private TextBox txtWorld = null!;
    private ListView lvCharacters = null!;
    private ListView lvScenes = null!;
    private ListView lvItems = null!;
    private ListView lvStats = null!;
    private ListView lvRelationships = null!;
    private TextBox txtCombat = null!;

    private TableLayoutPanel pipelineLayout = null!;
    private Label lblPipelineIntro = null!;
    private SplitContainer pipelineSplit = null!;
    private ListView lvGenerationPlan = null!;
    private TableLayoutPanel pipelineControlsLayout = null!;
    private Label lblPipelineBatchCount = null!;
    private NumericUpDown nudPipelineBatchCount = null!;
    private Label lblPipelineCategory = null!;
    private ComboBox cmbPipelineCategory = null!;
    private Label lblPipelineRules = null!;
    private TextBox txtPipelineRules = null!;
    private Label lblGenerationPreferences = null!;
    private TextBox txtPreferenceGeneral = null!;
    private TextBox txtPreferenceSkills = null!;
    private TextBox txtPreferenceProgression = null!;
    private TextBox txtPreferenceCombat = null!;
    private TextBox txtPreferenceAtmosphere = null!;
    private TextBox txtPreferenceBalance = null!;
    private TextBox txtPreferenceForbidden = null!;
    private TextBox txtPreferenceNotes = null!;
    private Button btnSaveGenerationPreferences = null!;
    private FlowLayoutPanel pipelineButtons = null!;
    private Button btnRefreshGenerationPlan = null!;
    private Button btnCheckMechanics = null!;
    private Button btnRunSelectedPipelineStep = null!;
    private Button btnReviewLatestDraft = null!;
    private Button btnApplyLatestDraft = null!;
    private Button btnRejectLatestDraft = null!;
    private Button btnOpenDraftsFolderPipeline = null!;
    private Button btnOpenCurrentDraft = null!;
    private TextBox txtPipelineDraftInfo = null!;
    private TextBox txtPipelineDetails = null!;

    private TableLayoutPanel assetsLayout = null!;
    private ListView lvPrompts = null!;
    private TextBox txtPromptDetails = null!;
    private TableLayoutPanel assetsBottomLayout = null!;
    private FlowLayoutPanel assetsPromptButtons = null!;
    private FlowLayoutPanel assetsBatchOptions = null!;
    private FlowLayoutPanel assetsBatchButtons = null!;
    private Label lblBatchCount = null!;
    private NumericUpDown nudBatchCount = null!;
    private Label lblBatchCategory = null!;
    private ComboBox cmbBatchCategory = null!;
    private Label lblBatchRules = null!;
    private TextBox txtBatchRules = null!;
    private FlowLayoutPanel assetsButtons = null!;
    private Button btnBuildImagePrompts = null!;
    private Button btnGenerateStatsResourcesBatch = null!;
    private Button btnGenerateItemsBatch = null!;
    private Button btnGenerateEquipmentBatch = null!;
    private Button btnGenerateSkillsBatch = null!;
    private Button btnGenerateSpellsBatch = null!;
    private Button btnGenerateLocationsBatch = null!;
    private Button btnGenerateScenesBatch = null!;
    private Button btnGenerateEncountersBatch = null!;
    private Button btnApprovePrompt = null!;
    private Button btnRunFooocusQueue = null!;
    private Button btnImportAssets = null!;
    private Button btnSelectImage = null!;

    private TableLayoutPanel playLayout = null!;
    private Label lblGameTitle = null!;
    private Label lblSceneTitle = null!;
    private PictureBox picScene = null!;
    private TextBox txtSceneText = null!;
    private FlowLayoutPanel pnlChoices = null!;
    private TabControl tabRuntimeInfo = null!;
    private TabPage tabRuntimeStatsPage = null!;
    private TabPage tabRuntimeInventoryPage = null!;
    private TabPage tabRuntimeRelationshipsPage = null!;
    private TabPage tabRuntimeQuestsPage = null!;
    private TabPage tabRuntimeLogPage = null!;
    private ListView lvRuntimeStats = null!;
    private ListView lvRuntimeInventory = null!;
    private ListView lvRuntimeRelationships = null!;
    private ListView lvRuntimeQuests = null!;
    private TextBox txtRuntimeLog = null!;

    private TableLayoutPanel savesLayout = null!;
    private ListBox lstSaves = null!;
    private FlowLayoutPanel savesButtons = null!;
    private Button btnNewRun = null!;
    private Button btnOpenPlayWindow = null!;
    private Button btnSaveProgress = null!;
    private Button btnLoadProgress = null!;
    private Button btnDeleteSave = null!;

    private TextBox txtLog = null!;

    private TableLayoutPanel settingsLayout = null!;
    private Label lblLmProfile = null!;
    private ComboBox cmbLmProfiles = null!;
    private Button btnAddLmProfile = null!;
    private Button btnSaveLmProfile = null!;
    private Label lblLmProfileName = null!;
    private TextBox txtLmProfileName = null!;
    private Label lblLmProfileRole = null!;
    private ComboBox cmbLmProfileRole = null!;
    private Label lblAutoSelectLmProfile = null!;
    private CheckBox chkAutoSelectLmProfile = null!;
    private Button btnDeleteLmProfile = null!;
    private Button btnSetDefaultLmProfile = null!;
    private TextBox txtEndpoint = null!;
    private TextBox txtApiKey = null!;
    private TextBox txtModel = null!;
    private NumericUpDown nudTimeout = null!;
    private NumericUpDown nudMaxInputContextTokens = null!;
    private NumericUpDown nudMaxOutputTokens = null!;
    private TextBox txtLmUnloadUrl = null!;
    private TextBox txtLmUnloadCommand = null!;
    private TextBox txtFooocusLaunch = null!;
    private TextBox txtFooocusWorkingDir = null!;
    private TextBox txtFooocusOutput = null!;
    private TextBox txtFooocusEndpoint = null!;
    private NumericUpDown nudFooocusStartup = null!;
    private NumericUpDown nudFooocusShutdown = null!;
    private Label lblEndpoint = null!;
    private Label lblApiKey = null!;
    private Label lblModel = null!;
    private Label lblTimeout = null!;
    private Label lblMaxInputContextTokens = null!;
    private Label lblMaxOutputTokens = null!;
    private Label lblLmUnloadUrl = null!;
    private Label lblLmUnloadCommand = null!;
    private Label lblLmUnloadTimeout = null!;
    private Label lblContinueIfUnloadFails = null!;
    private Label lblFooocusLaunch = null!;
    private Label lblFooocusWorkingDir = null!;
    private Label lblFooocusOutput = null!;
    private Label lblFooocusEndpoint = null!;
    private Label lblFooocusStartup = null!;
    private Label lblFooocusShutdown = null!;
    private Label lblSettings = null!;
    private Button btnTestLm = null!;
    private Button btnSaveSettings = null!;
    private Button btnBrowseFooocusLaunch = null!;
    private Button btnBrowseFooocusFolder = null!;
    private Button btnDetectFooocus = null!;
    private Button btnCheckFooocusPaths = null!;
    private Button btnBrowseFooocusOutput = null!;
    private NumericUpDown nudLmUnloadTimeout = null!;
    private CheckBox chkContinueIfUnloadFails = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        rootLayout = new TableLayoutPanel();
        topLayout = new TableLayoutPanel();
        lblGamesRoot = new Label();
        txtGamesRoot = new TextBox();
        btnBrowseGamesRoot = new Button();
        btnOpenGameFolder = new Button();
        btnRefreshProjects = new Button();
        btnStopOperation = new Button();
        lblStatus = new Label();
        tabMain = new TabControl();
        tabProjects = new TabPage();
        projectsSplit = new SplitContainer();
        lstProjects = new ListBox();
        projectButtonsLayout = new TableLayoutPanel();
        btnNewGame = new Button();
        btnLoadGame = new Button();
        btnSaveGame = new Button();
        btnSaveGameAs = new Button();
        btnOpenDraftsFolder = new Button();
        btnOpenDataFolder = new Button();
        btnResaveSplitJson = new Button();
        btnValidateProject = new Button();
        btnDeleteProject = new Button();
        pgProject = new PropertyGrid();
        tabDiscussion = new TabPage();
        discussionLayout = new TableLayoutPanel();
        txtDiscussion = new TextBox();
        txtUserInput = new TextBox();
        discussionButtons = new FlowLayoutPanel();
        btnStartDiscussion = new Button();
        btnSend = new Button();
        btnAskGenre = new Button();
        btnAskWorld = new Button();
        btnAskHero = new Button();
        btnAskMechanics = new Button();
        btnAskVisualStyle = new Button();
        btnBuildBrief = new Button();
        btnBuildConcept = new Button();
        btnBuildMvp = new Button();
        btnBuildStructure = new Button();
        btnGenerateContent = new Button();
        tabGameCrafter = new TabPage();
        gameCrafterLayout = new TableLayoutPanel();
        gameCrafterTopLayout = new TableLayoutPanel();
        lblGameCrafterIdea = new Label();
        txtGameCrafterIdea = new TextBox();
        lblGameCreationMode = new Label();
        cmbGameCreationMode = new ComboBox();
        lblChangeRequest = new Label();
        txtChangeRequest = new TextBox();
        btnChangeRequestAnalyze = new Button();
        btnChangeRequestGenerate = new Button();
        lblDesignConversation = new Label();
        txtDesignConversation = new TextBox();
        lblDesignConversationFocus = new Label();
        txtDesignConversationFocus = new TextBox();
        gameCrafterButtons = new FlowLayoutPanel();
        btnDesignApplyIdea = new Button();
        btnDesignRefreshQuestions = new Button();
        btnDesignAskLlmAssumptions = new Button();
        btnDesignBuildPlan = new Button();
        btnDesignSave = new Button();
        btnRandomDirectorCheck = new Button();
        btnRandomDirectorGenerate = new Button();
        lblRandomDirectorEventCount = new Label();
        txtRandomDirectorEventCount = new TextBox();
        lblBalanceSimulationRuns = new Label();
        txtBalanceSimulationRuns = new TextBox();
        btnBalanceCheck = new Button();
        btnBalanceGenerateDraft = new Button();
        btnMvpCheck = new Button();
        btnMvpGenerateNextDraft = new Button();
        btnDesignConversationSend = new Button();
        gameCrafterSplit = new SplitContainer();
        gameCrafterLeftLayout = new TableLayoutPanel();
        lvDesignSlots = new ListView();
        lvDesignQuestions = new ListView();
        txtDesignAnswer = new TextBox();
        btnDesignApplyAnswer = new Button();
        txtDesignPreview = new TextBox();
        tabBrief = new TabPage();
        briefLayout = new TableLayoutPanel();
        txtBriefConcept = new TextBox();
        briefButtons = new FlowLayoutPanel();
        btnApproveBrief = new Button();
        btnApproveConcept = new Button();
        btnApplyRevision = new Button();
        tabContent = new TabPage();
        tabContentInner = new TabControl();
        tabWorld = new TabPage();
        txtWorld = new TextBox();
        tabCharacters = new TabPage();
        lvCharacters = new ListView();
        tabScenes = new TabPage();
        lvScenes = new ListView();
        tabItems = new TabPage();
        lvItems = new ListView();
        tabStats = new TabPage();
        lvStats = new ListView();
        tabRelationships = new TabPage();
        lvRelationships = new ListView();
        tabCombat = new TabPage();
        txtCombat = new TextBox();
        tabPipeline = new TabPage();
        pipelineLayout = new TableLayoutPanel();
        lblPipelineIntro = new Label();
        pipelineSplit = new SplitContainer();
        lvGenerationPlan = new ListView();
        pipelineControlsLayout = new TableLayoutPanel();
        lblPipelineBatchCount = new Label();
        nudPipelineBatchCount = new NumericUpDown();
        lblPipelineCategory = new Label();
        cmbPipelineCategory = new ComboBox();
        lblPipelineRules = new Label();
        txtPipelineRules = new TextBox();
        lblGenerationPreferences = new Label();
        txtPreferenceGeneral = new TextBox();
        txtPreferenceSkills = new TextBox();
        txtPreferenceProgression = new TextBox();
        txtPreferenceCombat = new TextBox();
        txtPreferenceAtmosphere = new TextBox();
        txtPreferenceBalance = new TextBox();
        txtPreferenceForbidden = new TextBox();
        txtPreferenceNotes = new TextBox();
        btnSaveGenerationPreferences = new Button();
        pipelineButtons = new FlowLayoutPanel();
        btnRefreshGenerationPlan = new Button();
        btnCheckMechanics = new Button();
        btnRunSelectedPipelineStep = new Button();
        btnReviewLatestDraft = new Button();
        btnApplyLatestDraft = new Button();
        btnRejectLatestDraft = new Button();
        btnOpenDraftsFolderPipeline = new Button();
        btnOpenCurrentDraft = new Button();
        txtPipelineDraftInfo = new TextBox();
        txtPipelineDetails = new TextBox();
        tabAssets = new TabPage();
        assetsLayout = new TableLayoutPanel();
        lvPrompts = new ListView();
        txtPromptDetails = new TextBox();
        assetsBottomLayout = new TableLayoutPanel();
        assetsBatchOptions = new FlowLayoutPanel();
        lblBatchCount = new Label();
        nudBatchCount = new NumericUpDown();
        lblBatchCategory = new Label();
        cmbBatchCategory = new ComboBox();
        assetsBatchButtons = new FlowLayoutPanel();
        btnGenerateStatsResourcesBatch = new Button();
        btnGenerateItemsBatch = new Button();
        btnGenerateEquipmentBatch = new Button();
        btnGenerateSkillsBatch = new Button();
        btnGenerateSpellsBatch = new Button();
        btnGenerateLocationsBatch = new Button();
        btnGenerateScenesBatch = new Button();
        btnGenerateEncountersBatch = new Button();
        lblBatchRules = new Label();
        txtBatchRules = new TextBox();
        assetsButtons = new FlowLayoutPanel();
        btnBuildImagePrompts = new Button();
        btnApprovePrompt = new Button();
        btnRunFooocusQueue = new Button();
        btnImportAssets = new Button();
        btnSelectImage = new Button();
        tabPlay = new TabPage();
        playLayout = new TableLayoutPanel();
        lblGameTitle = new Label();
        lblSceneTitle = new Label();
        picScene = new PictureBox();
        txtSceneText = new TextBox();
        pnlChoices = new FlowLayoutPanel();
        tabRuntimeInfo = new TabControl();
        tabRuntimeStatsPage = new TabPage();
        lvRuntimeStats = new ListView();
        tabRuntimeInventoryPage = new TabPage();
        lvRuntimeInventory = new ListView();
        tabRuntimeRelationshipsPage = new TabPage();
        lvRuntimeRelationships = new ListView();
        tabRuntimeQuestsPage = new TabPage();
        lvRuntimeQuests = new ListView();
        tabRuntimeLogPage = new TabPage();
        txtRuntimeLog = new TextBox();
        tabSaves = new TabPage();
        savesLayout = new TableLayoutPanel();
        lstSaves = new ListBox();
        savesButtons = new FlowLayoutPanel();
        btnNewRun = new Button();
        btnOpenPlayWindow = new Button();
        btnSaveProgress = new Button();
        btnLoadProgress = new Button();
        btnDeleteSave = new Button();
        tabLogs = new TabPage();
        txtLog = new TextBox();
        tabSettings = new TabPage();
        settingsLayout = new TableLayoutPanel();
        lblLmProfile = new Label();
        cmbLmProfiles = new ComboBox();
        btnAddLmProfile = new Button();
        btnSaveLmProfile = new Button();
        lblLmProfileName = new Label();
        txtLmProfileName = new TextBox();
        lblLmProfileRole = new Label();
        cmbLmProfileRole = new ComboBox();
        lblAutoSelectLmProfile = new Label();
        chkAutoSelectLmProfile = new CheckBox();
        btnDeleteLmProfile = new Button();
        btnSetDefaultLmProfile = new Button();
        lblEndpoint = new Label();
        txtEndpoint = new TextBox();
        btnTestLm = new Button();
        lblApiKey = new Label();
        txtApiKey = new TextBox();
        lblModel = new Label();
        txtModel = new TextBox();
        lblTimeout = new Label();
        nudTimeout = new NumericUpDown();
        lblMaxInputContextTokens = new Label();
        nudMaxInputContextTokens = new NumericUpDown();
        lblMaxOutputTokens = new Label();
        nudMaxOutputTokens = new NumericUpDown();
        lblLmUnloadUrl = new Label();
        txtLmUnloadUrl = new TextBox();
        lblLmUnloadCommand = new Label();
        txtLmUnloadCommand = new TextBox();
        lblLmUnloadTimeout = new Label();
        nudLmUnloadTimeout = new NumericUpDown();
        lblContinueIfUnloadFails = new Label();
        chkContinueIfUnloadFails = new CheckBox();
        lblFooocusLaunch = new Label();
        txtFooocusLaunch = new TextBox();
        btnBrowseFooocusLaunch = new Button();
        btnBrowseFooocusFolder = new Button();
        lblFooocusWorkingDir = new Label();
        txtFooocusWorkingDir = new TextBox();
        btnDetectFooocus = new Button();
        lblFooocusOutput = new Label();
        txtFooocusOutput = new TextBox();
        btnBrowseFooocusOutput = new Button();
        btnCheckFooocusPaths = new Button();
        lblFooocusEndpoint = new Label();
        txtFooocusEndpoint = new TextBox();
        lblFooocusStartup = new Label();
        nudFooocusStartup = new NumericUpDown();
        lblFooocusShutdown = new Label();
        nudFooocusShutdown = new NumericUpDown();
        lblSettings = new Label();
        btnSaveSettings = new Button();
        assetsPromptButtons = new FlowLayoutPanel();
        lblValidationResult = new Label();
        rootLayout.SuspendLayout();
        topLayout.SuspendLayout();
        tabMain.SuspendLayout();
        tabProjects.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)projectsSplit).BeginInit();
        projectsSplit.Panel1.SuspendLayout();
        projectsSplit.Panel2.SuspendLayout();
        projectsSplit.SuspendLayout();
        projectButtonsLayout.SuspendLayout();
        tabDiscussion.SuspendLayout();
        discussionLayout.SuspendLayout();
        discussionButtons.SuspendLayout();
        tabGameCrafter.SuspendLayout();
        gameCrafterLayout.SuspendLayout();
        gameCrafterTopLayout.SuspendLayout();
        gameCrafterButtons.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)gameCrafterSplit).BeginInit();
        gameCrafterSplit.Panel1.SuspendLayout();
        gameCrafterSplit.Panel2.SuspendLayout();
        gameCrafterSplit.SuspendLayout();
        gameCrafterLeftLayout.SuspendLayout();
        tabBrief.SuspendLayout();
        briefLayout.SuspendLayout();
        briefButtons.SuspendLayout();
        tabContent.SuspendLayout();
        tabContentInner.SuspendLayout();
        tabWorld.SuspendLayout();
        tabCharacters.SuspendLayout();
        tabScenes.SuspendLayout();
        tabItems.SuspendLayout();
        tabStats.SuspendLayout();
        tabRelationships.SuspendLayout();
        tabCombat.SuspendLayout();
        tabPipeline.SuspendLayout();
        pipelineLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pipelineSplit).BeginInit();
        pipelineSplit.Panel1.SuspendLayout();
        pipelineSplit.Panel2.SuspendLayout();
        pipelineSplit.SuspendLayout();
        pipelineControlsLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudPipelineBatchCount).BeginInit();
        pipelineButtons.SuspendLayout();
        tabAssets.SuspendLayout();
        assetsLayout.SuspendLayout();
        assetsBottomLayout.SuspendLayout();
        assetsBatchOptions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudBatchCount).BeginInit();
        assetsBatchButtons.SuspendLayout();
        assetsButtons.SuspendLayout();
        tabPlay.SuspendLayout();
        playLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picScene).BeginInit();
        tabRuntimeInfo.SuspendLayout();
        tabRuntimeStatsPage.SuspendLayout();
        tabRuntimeInventoryPage.SuspendLayout();
        tabRuntimeRelationshipsPage.SuspendLayout();
        tabRuntimeQuestsPage.SuspendLayout();
        tabRuntimeLogPage.SuspendLayout();
        tabSaves.SuspendLayout();
        savesLayout.SuspendLayout();
        savesButtons.SuspendLayout();
        tabLogs.SuspendLayout();
        tabSettings.SuspendLayout();
        settingsLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudTimeout).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxInputContextTokens).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxOutputTokens).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudLmUnloadTimeout).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudFooocusStartup).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nudFooocusShutdown).BeginInit();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(topLayout, 0, 0);
        rootLayout.Controls.Add(tabMain, 0, 1);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 2;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.Size = new Size(1280, 860);
        rootLayout.TabIndex = 0;
        // 
        // topLayout
        // 
        topLayout.ColumnCount = 7;
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        topLayout.Controls.Add(lblGamesRoot, 0, 0);
        topLayout.Controls.Add(txtGamesRoot, 1, 0);
        topLayout.Controls.Add(btnBrowseGamesRoot, 2, 0);
        topLayout.Controls.Add(btnOpenGameFolder, 3, 0);
        topLayout.Controls.Add(btnRefreshProjects, 4, 0);
        topLayout.Controls.Add(btnStopOperation, 5, 0);
        topLayout.Controls.Add(lblStatus, 6, 0);
        topLayout.Dock = DockStyle.Fill;
        topLayout.Location = new Point(3, 3);
        topLayout.Name = "topLayout";
        topLayout.Padding = new Padding(8);
        topLayout.RowCount = 1;
        topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        topLayout.Size = new Size(1274, 58);
        topLayout.TabIndex = 0;
        // 
        // lblGamesRoot
        // 
        lblGamesRoot.Dock = DockStyle.Fill;
        lblGamesRoot.Location = new Point(11, 8);
        lblGamesRoot.Name = "lblGamesRoot";
        lblGamesRoot.Size = new Size(119, 42);
        lblGamesRoot.TabIndex = 0;
        lblGamesRoot.Text = "Каталог игр:";
        lblGamesRoot.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtGamesRoot
        // 
        txtGamesRoot.Dock = DockStyle.Fill;
        txtGamesRoot.Location = new Point(136, 11);
        txtGamesRoot.Name = "txtGamesRoot";
        txtGamesRoot.Size = new Size(492, 23);
        txtGamesRoot.TabIndex = 1;
        // 
        // btnBrowseGamesRoot
        // 
        btnBrowseGamesRoot.Dock = DockStyle.Fill;
        btnBrowseGamesRoot.Location = new Point(634, 11);
        btnBrowseGamesRoot.Name = "btnBrowseGamesRoot";
        btnBrowseGamesRoot.Size = new Size(94, 36);
        btnBrowseGamesRoot.TabIndex = 2;
        btnBrowseGamesRoot.Text = "Выбрать";
        btnBrowseGamesRoot.Click += btnBrowseGamesRoot_Click;
        // 
        // btnOpenGameFolder
        // 
        btnOpenGameFolder.Dock = DockStyle.Fill;
        btnOpenGameFolder.Location = new Point(734, 11);
        btnOpenGameFolder.Name = "btnOpenGameFolder";
        btnOpenGameFolder.Size = new Size(119, 36);
        btnOpenGameFolder.TabIndex = 3;
        btnOpenGameFolder.Text = "Открыть папку";
        btnOpenGameFolder.Click += btnOpenGameFolder_Click;
        // 
        // btnRefreshProjects
        // 
        btnRefreshProjects.Dock = DockStyle.Fill;
        btnRefreshProjects.Location = new Point(859, 11);
        btnRefreshProjects.Name = "btnRefreshProjects";
        btnRefreshProjects.Size = new Size(119, 36);
        btnRefreshProjects.TabIndex = 4;
        btnRefreshProjects.Text = "Обновить";
        btnRefreshProjects.Click += btnRefreshProjects_Click;
        // 
        // btnStopOperation
        // 
        btnStopOperation.Dock = DockStyle.Fill;
        btnStopOperation.Enabled = false;
        btnStopOperation.Location = new Point(984, 11);
        btnStopOperation.Name = "btnStopOperation";
        btnStopOperation.Size = new Size(99, 36);
        btnStopOperation.TabIndex = 5;
        btnStopOperation.Text = "Остановить";
        btnStopOperation.Click += btnStopOperation_Click;
        // 
        // lblStatus
        // 
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.Location = new Point(1089, 8);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(174, 42);
        lblStatus.TabIndex = 6;
        lblStatus.Text = "Idle";
        lblStatus.TextAlign = ContentAlignment.MiddleRight;
        // 
        // tabMain
        // 
        tabMain.Controls.Add(tabProjects);
        tabMain.Controls.Add(tabDiscussion);
        tabMain.Controls.Add(tabGameCrafter);
        tabMain.Controls.Add(tabBrief);
        tabMain.Controls.Add(tabContent);
        tabMain.Controls.Add(tabPipeline);
        tabMain.Controls.Add(tabAssets);
        tabMain.Controls.Add(tabPlay);
        tabMain.Controls.Add(tabSaves);
        tabMain.Controls.Add(tabLogs);
        tabMain.Controls.Add(tabSettings);
        tabMain.Dock = DockStyle.Fill;
        tabMain.Location = new Point(3, 67);
        tabMain.Name = "tabMain";
        tabMain.SelectedIndex = 0;
        tabMain.Size = new Size(1274, 790);
        tabMain.TabIndex = 1;
        // 
        // tabProjects
        // 
        tabProjects.Controls.Add(projectsSplit);
        tabProjects.Location = new Point(4, 24);
        tabProjects.Name = "tabProjects";
        tabProjects.Size = new Size(1266, 762);
        tabProjects.TabIndex = 0;
        tabProjects.Text = "Проекты";
        // 
        // projectsSplit
        // 
        projectsSplit.Dock = DockStyle.Fill;
        projectsSplit.Location = new Point(0, 0);
        projectsSplit.Name = "projectsSplit";
        // 
        // projectsSplit.Panel1
        // 
        projectsSplit.Panel1.Controls.Add(lstProjects);
        projectsSplit.Panel1.Controls.Add(projectButtonsLayout);
        // 
        // projectsSplit.Panel2
        // 
        projectsSplit.Panel2.Controls.Add(pgProject);
        projectsSplit.Size = new Size(1266, 762);
        projectsSplit.SplitterDistance = 1021;
        projectsSplit.TabIndex = 0;
        // 
        // lstProjects
        // 
        lstProjects.DisplayMember = "Title";
        lstProjects.Dock = DockStyle.Fill;
        lstProjects.ItemHeight = 15;
        lstProjects.Location = new Point(0, 0);
        lstProjects.Name = "lstProjects";
        lstProjects.Size = new Size(1021, 678);
        lstProjects.TabIndex = 0;
        // 
        // projectButtonsLayout
        // 
        projectButtonsLayout.ColumnCount = 5;
        projectButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        projectButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        projectButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        projectButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        projectButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        projectButtonsLayout.Controls.Add(btnNewGame, 0, 0);
        projectButtonsLayout.Controls.Add(btnLoadGame, 1, 0);
        projectButtonsLayout.Controls.Add(btnSaveGame, 2, 0);
        projectButtonsLayout.Controls.Add(btnSaveGameAs, 3, 0);
        projectButtonsLayout.Controls.Add(btnOpenDraftsFolder, 0, 1);
        projectButtonsLayout.Controls.Add(btnOpenDataFolder, 1, 1);
        projectButtonsLayout.Controls.Add(btnResaveSplitJson, 2, 1);
        projectButtonsLayout.Controls.Add(btnValidateProject, 3, 1);
        projectButtonsLayout.Controls.Add(btnDeleteProject, 4, 1);
        projectButtonsLayout.Dock = DockStyle.Bottom;
        projectButtonsLayout.Location = new Point(0, 678);
        projectButtonsLayout.Name = "projectButtonsLayout";
        projectButtonsLayout.RowCount = 2;
        projectButtonsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        projectButtonsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        projectButtonsLayout.Size = new Size(1021, 84);
        projectButtonsLayout.TabIndex = 1;
        // 
        // btnNewGame
        // 
        btnNewGame.Dock = DockStyle.Fill;
        btnNewGame.Location = new Point(3, 3);
        btnNewGame.Name = "btnNewGame";
        btnNewGame.Size = new Size(249, 36);
        btnNewGame.TabIndex = 0;
        btnNewGame.Text = "Новая";
        btnNewGame.Click += btnNewGame_Click;
        // 
        // btnLoadGame
        // 
        btnLoadGame.Dock = DockStyle.Fill;
        btnLoadGame.Location = new Point(258, 3);
        btnLoadGame.Name = "btnLoadGame";
        btnLoadGame.Size = new Size(249, 36);
        btnLoadGame.TabIndex = 1;
        btnLoadGame.Text = "Открыть";
        btnLoadGame.Click += btnLoadGame_Click;
        // 
        // btnSaveGame
        // 
        btnSaveGame.Dock = DockStyle.Fill;
        btnSaveGame.Location = new Point(513, 3);
        btnSaveGame.Name = "btnSaveGame";
        btnSaveGame.Size = new Size(249, 36);
        btnSaveGame.TabIndex = 2;
        btnSaveGame.Text = "Сохранить";
        btnSaveGame.Click += btnSaveGame_Click;
        // 
        // btnSaveGameAs
        // 
        btnSaveGameAs.Dock = DockStyle.Fill;
        btnSaveGameAs.Location = new Point(768, 3);
        btnSaveGameAs.Name = "btnSaveGameAs";
        btnSaveGameAs.Size = new Size(250, 36);
        btnSaveGameAs.TabIndex = 3;
        btnSaveGameAs.Text = "Сохранить как";
        btnSaveGameAs.Click += btnSaveGameAs_Click;
        // 
        // btnOpenDraftsFolder
        // 
        btnOpenDraftsFolder.Dock = DockStyle.Fill;
        btnOpenDraftsFolder.Location = new Point(3, 45);
        btnOpenDraftsFolder.Name = "btnOpenDraftsFolder";
        btnOpenDraftsFolder.Size = new Size(249, 36);
        btnOpenDraftsFolder.TabIndex = 4;
        btnOpenDraftsFolder.Text = "Drafts";
        btnOpenDraftsFolder.Click += btnOpenDraftsFolder_Click;
        // 
        // btnOpenDataFolder
        // 
        btnOpenDataFolder.Dock = DockStyle.Fill;
        btnOpenDataFolder.Location = new Point(258, 45);
        btnOpenDataFolder.Name = "btnOpenDataFolder";
        btnOpenDataFolder.Size = new Size(249, 36);
        btnOpenDataFolder.TabIndex = 5;
        btnOpenDataFolder.Text = "Data";
        btnOpenDataFolder.Click += btnOpenDataFolder_Click;
        // 
        // btnResaveSplitJson
        // 
        btnResaveSplitJson.Dock = DockStyle.Fill;
        btnResaveSplitJson.Location = new Point(513, 45);
        btnResaveSplitJson.Name = "btnResaveSplitJson";
        btnResaveSplitJson.Size = new Size(249, 36);
        btnResaveSplitJson.TabIndex = 6;
        btnResaveSplitJson.Text = "Split-json";
        btnResaveSplitJson.Click += btnResaveSplitJson_Click;
        // 
        // btnValidateProject
        // 
        btnValidateProject.Dock = DockStyle.Fill;
        btnValidateProject.Location = new Point(768, 45);
        btnValidateProject.Name = "btnValidateProject";
        btnValidateProject.Size = new Size(250, 36);
        btnValidateProject.TabIndex = 7;
        btnValidateProject.Text = "Проверить";
        btnValidateProject.Click += btnValidateProject_Click;
        // 
        // btnDeleteProject
        // 
        btnDeleteProject.Dock = DockStyle.Fill;
        btnDeleteProject.Location = new Point(819, 45);
        btnDeleteProject.Name = "btnDeleteProject";
        btnDeleteProject.Size = new Size(199, 36);
        btnDeleteProject.TabIndex = 8;
        btnDeleteProject.Text = "Удалить";
        btnDeleteProject.Click += btnDeleteProject_Click;
        // 
        // pgProject
        // 
        pgProject.Dock = DockStyle.Fill;
        pgProject.Location = new Point(0, 0);
        pgProject.Name = "pgProject";
        pgProject.Size = new Size(241, 762);
        pgProject.TabIndex = 0;
        // 
        // tabDiscussion
        // 
        tabDiscussion.Controls.Add(discussionLayout);
        tabDiscussion.Location = new Point(4, 24);
        tabDiscussion.Name = "tabDiscussion";
        tabDiscussion.Size = new Size(1266, 762);
        tabDiscussion.TabIndex = 1;
        tabDiscussion.Text = "AI-обсуждение";
        // 
        // discussionLayout
        // 
        discussionLayout.ColumnCount = 1;
        discussionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        discussionLayout.Controls.Add(txtDiscussion, 0, 0);
        discussionLayout.Controls.Add(txtUserInput, 0, 1);
        discussionLayout.Controls.Add(discussionButtons, 0, 2);
        discussionLayout.Dock = DockStyle.Fill;
        discussionLayout.Location = new Point(0, 0);
        discussionLayout.Name = "discussionLayout";
        discussionLayout.RowCount = 3;
        discussionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        discussionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 105F));
        discussionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        discussionLayout.Size = new Size(1266, 762);
        discussionLayout.TabIndex = 0;
        // 
        // txtDiscussion
        // 
        txtDiscussion.Dock = DockStyle.Fill;
        txtDiscussion.Font = new Font("Consolas", 10F);
        txtDiscussion.Location = new Point(3, 3);
        txtDiscussion.Multiline = true;
        txtDiscussion.Name = "txtDiscussion";
        txtDiscussion.ReadOnly = true;
        txtDiscussion.ScrollBars = ScrollBars.Both;
        txtDiscussion.Size = new Size(1260, 565);
        txtDiscussion.TabIndex = 0;
        // 
        // txtUserInput
        // 
        txtUserInput.Dock = DockStyle.Fill;
        txtUserInput.Font = new Font("Consolas", 10F);
        txtUserInput.Location = new Point(3, 574);
        txtUserInput.Multiline = true;
        txtUserInput.Name = "txtUserInput";
        txtUserInput.ScrollBars = ScrollBars.Vertical;
        txtUserInput.Size = new Size(1260, 99);
        txtUserInput.TabIndex = 1;
        // 
        // discussionButtons
        // 
        discussionButtons.Controls.Add(btnStartDiscussion);
        discussionButtons.Controls.Add(btnSend);
        discussionButtons.Controls.Add(btnAskGenre);
        discussionButtons.Controls.Add(btnAskWorld);
        discussionButtons.Controls.Add(btnAskHero);
        discussionButtons.Controls.Add(btnAskMechanics);
        discussionButtons.Controls.Add(btnAskVisualStyle);
        discussionButtons.Controls.Add(btnBuildBrief);
        discussionButtons.Controls.Add(btnBuildConcept);
        discussionButtons.Controls.Add(btnBuildMvp);
        discussionButtons.Controls.Add(btnBuildStructure);
        discussionButtons.Controls.Add(btnGenerateContent);
        discussionButtons.Dock = DockStyle.Fill;
        discussionButtons.Location = new Point(3, 679);
        discussionButtons.Name = "discussionButtons";
        discussionButtons.Size = new Size(1260, 80);
        discussionButtons.TabIndex = 2;
        // 
        // btnStartDiscussion
        // 
        btnStartDiscussion.Location = new Point(3, 3);
        btnStartDiscussion.Name = "btnStartDiscussion";
        btnStartDiscussion.Size = new Size(75, 23);
        btnStartDiscussion.TabIndex = 0;
        btnStartDiscussion.Text = "Начать обсуждение";
        btnStartDiscussion.Click += btnStartDiscussion_Click;
        // 
        // btnSend
        // 
        btnSend.Location = new Point(84, 3);
        btnSend.Name = "btnSend";
        btnSend.Size = new Size(75, 23);
        btnSend.TabIndex = 1;
        btnSend.Text = "Отправить";
        btnSend.Click += btnSend_Click;
        // 
        // btnAskGenre
        // 
        btnAskGenre.Location = new Point(165, 3);
        btnAskGenre.Name = "btnAskGenre";
        btnAskGenre.Size = new Size(75, 23);
        btnAskGenre.TabIndex = 2;
        btnAskGenre.Text = "Уточнить жанр";
        btnAskGenre.Click += btnStructuredPrompt_Click;
        // 
        // btnAskWorld
        // 
        btnAskWorld.Location = new Point(246, 3);
        btnAskWorld.Name = "btnAskWorld";
        btnAskWorld.Size = new Size(75, 23);
        btnAskWorld.TabIndex = 3;
        btnAskWorld.Text = "Уточнить мир";
        btnAskWorld.Click += btnStructuredPrompt_Click;
        // 
        // btnAskHero
        // 
        btnAskHero.Location = new Point(327, 3);
        btnAskHero.Name = "btnAskHero";
        btnAskHero.Size = new Size(75, 23);
        btnAskHero.TabIndex = 4;
        btnAskHero.Text = "Уточнить героя";
        btnAskHero.Click += btnStructuredPrompt_Click;
        // 
        // btnAskMechanics
        // 
        btnAskMechanics.Location = new Point(408, 3);
        btnAskMechanics.Name = "btnAskMechanics";
        btnAskMechanics.Size = new Size(75, 23);
        btnAskMechanics.TabIndex = 5;
        btnAskMechanics.Text = "Уточнить механику";
        btnAskMechanics.Click += btnStructuredPrompt_Click;
        // 
        // btnAskVisualStyle
        // 
        btnAskVisualStyle.Location = new Point(489, 3);
        btnAskVisualStyle.Name = "btnAskVisualStyle";
        btnAskVisualStyle.Size = new Size(75, 23);
        btnAskVisualStyle.TabIndex = 6;
        btnAskVisualStyle.Text = "Визуальный стиль";
        btnAskVisualStyle.Click += btnStructuredPrompt_Click;
        // 
        // btnBuildBrief
        // 
        btnBuildBrief.Location = new Point(570, 3);
        btnBuildBrief.Name = "btnBuildBrief";
        btnBuildBrief.Size = new Size(75, 23);
        btnBuildBrief.TabIndex = 7;
        btnBuildBrief.Text = "Сформировать бриф";
        btnBuildBrief.Click += btnBuildBrief_Click;
        // 
        // btnBuildConcept
        // 
        btnBuildConcept.Location = new Point(651, 3);
        btnBuildConcept.Name = "btnBuildConcept";
        btnBuildConcept.Size = new Size(75, 23);
        btnBuildConcept.TabIndex = 8;
        btnBuildConcept.Text = "Концепт";
        btnBuildConcept.Click += btnBuildConcept_Click;
        // 
        // btnBuildMvp
        // 
        btnBuildMvp.Location = new Point(732, 3);
        btnBuildMvp.Name = "btnBuildMvp";
        btnBuildMvp.Size = new Size(75, 23);
        btnBuildMvp.TabIndex = 9;
        btnBuildMvp.Text = "MVP";
        btnBuildMvp.Click += btnBuildMvp_Click;
        // 
        // btnBuildStructure
        // 
        btnBuildStructure.Location = new Point(813, 3);
        btnBuildStructure.Name = "btnBuildStructure";
        btnBuildStructure.Size = new Size(75, 23);
        btnBuildStructure.TabIndex = 10;
        btnBuildStructure.Text = "Структура";
        btnBuildStructure.Click += btnBuildStructure_Click;
        // 
        // btnGenerateContent
        // 
        btnGenerateContent.Location = new Point(894, 3);
        btnGenerateContent.Name = "btnGenerateContent";
        btnGenerateContent.Size = new Size(75, 23);
        btnGenerateContent.TabIndex = 11;
        btnGenerateContent.Text = "Данные игры";
        btnGenerateContent.Click += btnGenerateContent_Click;
        // 
        // tabGameCrafter
        // 
        tabGameCrafter.Controls.Add(gameCrafterLayout);
        tabGameCrafter.Location = new Point(4, 24);
        tabGameCrafter.Name = "tabGameCrafter";
        tabGameCrafter.Size = new Size(1266, 762);
        tabGameCrafter.TabIndex = 2;
        tabGameCrafter.Text = "Крафтер игры";
        // 
        // gameCrafterLayout
        // 
        gameCrafterLayout.ColumnCount = 1;
        gameCrafterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        gameCrafterLayout.Controls.Add(gameCrafterTopLayout, 0, 0);
        gameCrafterLayout.Controls.Add(gameCrafterButtons, 0, 1);
        gameCrafterLayout.Controls.Add(gameCrafterSplit, 0, 2);
        gameCrafterLayout.Dock = DockStyle.Fill;
        gameCrafterLayout.Location = new Point(0, 0);
        gameCrafterLayout.Name = "gameCrafterLayout";
        gameCrafterLayout.RowCount = 3;
        gameCrafterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 292F));
        gameCrafterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
        gameCrafterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        gameCrafterLayout.Size = new Size(1266, 762);
        gameCrafterLayout.TabIndex = 0;
        // 
        // gameCrafterTopLayout
        // 
        gameCrafterTopLayout.ColumnCount = 4;
        gameCrafterTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        gameCrafterTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        gameCrafterTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
        gameCrafterTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
        gameCrafterTopLayout.Controls.Add(lblGameCrafterIdea, 0, 0);
        gameCrafterTopLayout.Controls.Add(txtGameCrafterIdea, 1, 0);
        gameCrafterTopLayout.Controls.Add(lblGameCreationMode, 2, 0);
        gameCrafterTopLayout.Controls.Add(cmbGameCreationMode, 3, 0);
        gameCrafterTopLayout.Controls.Add(lblChangeRequest, 0, 1);
        gameCrafterTopLayout.Controls.Add(txtChangeRequest, 1, 1);
        gameCrafterTopLayout.Controls.Add(btnChangeRequestAnalyze, 2, 1);
        gameCrafterTopLayout.Controls.Add(btnChangeRequestGenerate, 3, 1);
        gameCrafterTopLayout.Controls.Add(lblDesignConversation, 0, 2);
        gameCrafterTopLayout.Controls.Add(txtDesignConversation, 1, 2);
        gameCrafterTopLayout.Controls.Add(lblDesignConversationFocus, 2, 2);
        gameCrafterTopLayout.Controls.Add(txtDesignConversationFocus, 3, 2);
        gameCrafterTopLayout.Dock = DockStyle.Fill;
        gameCrafterTopLayout.Location = new Point(3, 3);
        gameCrafterTopLayout.Name = "gameCrafterTopLayout";
        gameCrafterTopLayout.Padding = new Padding(8);
        gameCrafterTopLayout.RowCount = 3;
        gameCrafterTopLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 98F));
        gameCrafterTopLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        gameCrafterTopLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        gameCrafterTopLayout.Size = new Size(1260, 286);
        gameCrafterTopLayout.TabIndex = 0;
        // 
        // lblGameCrafterIdea
        // 
        lblGameCrafterIdea.Dock = DockStyle.Fill;
        lblGameCrafterIdea.Location = new Point(11, 8);
        lblGameCrafterIdea.Name = "lblGameCrafterIdea";
        lblGameCrafterIdea.Size = new Size(114, 98);
        lblGameCrafterIdea.TabIndex = 0;
        lblGameCrafterIdea.Text = "Идея игры:";
        lblGameCrafterIdea.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtGameCrafterIdea
        // 
        txtGameCrafterIdea.Dock = DockStyle.Fill;
        txtGameCrafterIdea.Font = new Font("Consolas", 10F);
        txtGameCrafterIdea.Location = new Point(131, 11);
        txtGameCrafterIdea.Multiline = true;
        txtGameCrafterIdea.Name = "txtGameCrafterIdea";
        txtGameCrafterIdea.ScrollBars = ScrollBars.Vertical;
        txtGameCrafterIdea.Size = new Size(638, 92);
        txtGameCrafterIdea.TabIndex = 1;
        // 
        // lblGameCreationMode
        // 
        lblGameCreationMode.Dock = DockStyle.Fill;
        lblGameCreationMode.Location = new Point(775, 8);
        lblGameCreationMode.Name = "lblGameCreationMode";
        lblGameCreationMode.Size = new Size(214, 98);
        lblGameCreationMode.TabIndex = 2;
        lblGameCreationMode.Text = "Режим:";
        lblGameCreationMode.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbGameCreationMode
        // 
        cmbGameCreationMode.Dock = DockStyle.Top;
        cmbGameCreationMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbGameCreationMode.FormattingEnabled = true;
        cmbGameCreationMode.Location = new Point(995, 11);
        cmbGameCreationMode.Name = "cmbGameCreationMode";
        cmbGameCreationMode.Size = new Size(254, 23);
        cmbGameCreationMode.TabIndex = 3;
        // 
        // lblChangeRequest
        // 
        lblChangeRequest.Dock = DockStyle.Fill;
        lblChangeRequest.Location = new Point(11, 106);
        lblChangeRequest.Name = "lblChangeRequest";
        lblChangeRequest.Size = new Size(114, 86);
        lblChangeRequest.TabIndex = 4;
        lblChangeRequest.Text = "Запрос на изменение игры:";
        lblChangeRequest.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtChangeRequest
        // 
        txtChangeRequest.Dock = DockStyle.Fill;
        txtChangeRequest.Font = new Font("Consolas", 10F);
        txtChangeRequest.Location = new Point(131, 109);
        txtChangeRequest.Multiline = true;
        txtChangeRequest.Name = "txtChangeRequest";
        txtChangeRequest.ScrollBars = ScrollBars.Vertical;
        txtChangeRequest.Size = new Size(638, 80);
        txtChangeRequest.TabIndex = 5;
        // 
        // btnChangeRequestAnalyze
        // 
        btnChangeRequestAnalyze.Dock = DockStyle.Top;
        btnChangeRequestAnalyze.Location = new Point(775, 109);
        btnChangeRequestAnalyze.Name = "btnChangeRequestAnalyze";
        btnChangeRequestAnalyze.Size = new Size(214, 32);
        btnChangeRequestAnalyze.TabIndex = 6;
        btnChangeRequestAnalyze.Text = "Проанализировать правку";
        btnChangeRequestAnalyze.Click += btnChangeRequestAnalyze_Click;
        // 
        // btnChangeRequestGenerate
        // 
        btnChangeRequestGenerate.Dock = DockStyle.Top;
        btnChangeRequestGenerate.Location = new Point(995, 109);
        btnChangeRequestGenerate.Name = "btnChangeRequestGenerate";
        btnChangeRequestGenerate.Size = new Size(254, 32);
        btnChangeRequestGenerate.TabIndex = 7;
        btnChangeRequestGenerate.Text = "Сгенерировать draft правки";
        btnChangeRequestGenerate.Click += btnChangeRequestGenerate_Click;
        // 
        // lblDesignConversation
        // 
        lblDesignConversation.Dock = DockStyle.Fill;
        lblDesignConversation.Location = new Point(11, 192);
        lblDesignConversation.Name = "lblDesignConversation";
        lblDesignConversation.Size = new Size(114, 86);
        lblDesignConversation.TabIndex = 8;
        lblDesignConversation.Text = "Дизайн-диалог:";
        lblDesignConversation.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtDesignConversation
        // 
        txtDesignConversation.Dock = DockStyle.Fill;
        txtDesignConversation.Font = new Font("Consolas", 10F);
        txtDesignConversation.Location = new Point(131, 195);
        txtDesignConversation.Multiline = true;
        txtDesignConversation.Name = "txtDesignConversation";
        txtDesignConversation.ScrollBars = ScrollBars.Vertical;
        txtDesignConversation.Size = new Size(638, 80);
        txtDesignConversation.TabIndex = 9;
        // 
        // lblDesignConversationFocus
        // 
        lblDesignConversationFocus.Dock = DockStyle.Fill;
        lblDesignConversationFocus.Location = new Point(775, 192);
        lblDesignConversationFocus.Name = "lblDesignConversationFocus";
        lblDesignConversationFocus.Size = new Size(214, 86);
        lblDesignConversationFocus.TabIndex = 10;
        lblDesignConversationFocus.Text = "Фокус/категория:";
        lblDesignConversationFocus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtDesignConversationFocus
        // 
        txtDesignConversationFocus.Dock = DockStyle.Top;
        txtDesignConversationFocus.Location = new Point(995, 195);
        txtDesignConversationFocus.Name = "txtDesignConversationFocus";
        txtDesignConversationFocus.Size = new Size(254, 23);
        txtDesignConversationFocus.TabIndex = 11;
        // 
        // gameCrafterButtons
        // 
        gameCrafterButtons.Controls.Add(btnDesignApplyIdea);
        gameCrafterButtons.Controls.Add(btnDesignRefreshQuestions);
        gameCrafterButtons.Controls.Add(btnDesignAskLlmAssumptions);
        gameCrafterButtons.Controls.Add(btnDesignBuildPlan);
        gameCrafterButtons.Controls.Add(btnDesignSave);
        gameCrafterButtons.Controls.Add(btnRandomDirectorCheck);
        gameCrafterButtons.Controls.Add(btnRandomDirectorGenerate);
        gameCrafterButtons.Controls.Add(lblRandomDirectorEventCount);
        gameCrafterButtons.Controls.Add(txtRandomDirectorEventCount);
        gameCrafterButtons.Controls.Add(lblBalanceSimulationRuns);
        gameCrafterButtons.Controls.Add(txtBalanceSimulationRuns);
        gameCrafterButtons.Controls.Add(btnBalanceCheck);
        gameCrafterButtons.Controls.Add(btnBalanceGenerateDraft);
        gameCrafterButtons.Controls.Add(btnMvpCheck);
        gameCrafterButtons.Controls.Add(btnMvpGenerateNextDraft);
        gameCrafterButtons.Controls.Add(btnDesignConversationSend);
        gameCrafterButtons.Dock = DockStyle.Fill;
        gameCrafterButtons.Location = new Point(3, 295);
        gameCrafterButtons.Name = "gameCrafterButtons";
        gameCrafterButtons.Padding = new Padding(8, 5, 8, 5);
        gameCrafterButtons.Size = new Size(1260, 70);
        gameCrafterButtons.TabIndex = 1;
        // 
        // btnDesignApplyIdea
        // 
        btnDesignApplyIdea.Location = new Point(11, 8);
        btnDesignApplyIdea.Name = "btnDesignApplyIdea";
        btnDesignApplyIdea.Size = new Size(135, 25);
        btnDesignApplyIdea.TabIndex = 0;
        btnDesignApplyIdea.Text = "Применить идею";
        btnDesignApplyIdea.Click += btnDesignApplyIdea_Click;
        // 
        // btnDesignRefreshQuestions
        // 
        btnDesignRefreshQuestions.Location = new Point(152, 8);
        btnDesignRefreshQuestions.Name = "btnDesignRefreshQuestions";
        btnDesignRefreshQuestions.Size = new Size(210, 25);
        btnDesignRefreshQuestions.TabIndex = 1;
        btnDesignRefreshQuestions.Text = "Найти недостающие вопросы";
        btnDesignRefreshQuestions.Click += btnDesignRefreshQuestions_Click;
        // 
        // btnDesignAskLlmAssumptions
        // 
        btnDesignAskLlmAssumptions.Location = new Point(368, 8);
        btnDesignAskLlmAssumptions.Name = "btnDesignAskLlmAssumptions";
        btnDesignAskLlmAssumptions.Size = new Size(245, 25);
        btnDesignAskLlmAssumptions.TabIndex = 2;
        btnDesignAskLlmAssumptions.Text = "Поручить LLM заполнить пропуски";
        btnDesignAskLlmAssumptions.Click += btnDesignAskLlmAssumptions_Click;
        // 
        // btnDesignBuildPlan
        // 
        btnDesignBuildPlan.Location = new Point(619, 8);
        btnDesignBuildPlan.Name = "btnDesignBuildPlan";
        btnDesignBuildPlan.Size = new Size(155, 25);
        btnDesignBuildPlan.TabIndex = 3;
        btnDesignBuildPlan.Text = "Создать план игры";
        btnDesignBuildPlan.Click += btnDesignBuildPlan_Click;
        // 
        // btnDesignSave
        // 
        btnDesignSave.Location = new Point(780, 8);
        btnDesignSave.Name = "btnDesignSave";
        btnDesignSave.Size = new Size(150, 25);
        btnDesignSave.TabIndex = 4;
        btnDesignSave.Text = "Сохранить досье";
        btnDesignSave.Click += btnDesignSave_Click;
        // 
        // btnRandomDirectorCheck
        // 
        btnRandomDirectorCheck.Location = new Point(936, 8);
        btnRandomDirectorCheck.Name = "btnRandomDirectorCheck";
        btnRandomDirectorCheck.Size = new Size(140, 25);
        btnRandomDirectorCheck.TabIndex = 5;
        btnRandomDirectorCheck.Text = "Проверить рандом";
        btnRandomDirectorCheck.Click += btnRandomDirectorCheck_Click;
        // 
        // btnRandomDirectorGenerate
        // 
        btnRandomDirectorGenerate.Location = new Point(1082, 8);
        btnRandomDirectorGenerate.Name = "btnRandomDirectorGenerate";
        btnRandomDirectorGenerate.Size = new Size(165, 25);
        btnRandomDirectorGenerate.TabIndex = 6;
        btnRandomDirectorGenerate.Text = "Сгенерировать события";
        btnRandomDirectorGenerate.Click += btnRandomDirectorGenerate_Click;
        // 
        // lblRandomDirectorEventCount
        // 
        lblRandomDirectorEventCount.AutoSize = true;
        lblRandomDirectorEventCount.Location = new Point(11, 42);
        lblRandomDirectorEventCount.Margin = new Padding(3, 6, 3, 0);
        lblRandomDirectorEventCount.Name = "lblRandomDirectorEventCount";
        lblRandomDirectorEventCount.Size = new Size(60, 15);
        lblRandomDirectorEventCount.TabIndex = 7;
        lblRandomDirectorEventCount.Text = "Событий:";
        // 
        // txtRandomDirectorEventCount
        // 
        txtRandomDirectorEventCount.Location = new Point(77, 39);
        txtRandomDirectorEventCount.Name = "txtRandomDirectorEventCount";
        txtRandomDirectorEventCount.Size = new Size(48, 23);
        txtRandomDirectorEventCount.TabIndex = 8;
        txtRandomDirectorEventCount.Text = "8";
        // 
        // lblBalanceSimulationRuns
        // 
        lblBalanceSimulationRuns.AutoSize = true;
        lblBalanceSimulationRuns.Location = new Point(131, 42);
        lblBalanceSimulationRuns.Margin = new Padding(3, 6, 3, 0);
        lblBalanceSimulationRuns.Name = "lblBalanceSimulationRuns";
        lblBalanceSimulationRuns.Size = new Size(74, 15);
        lblBalanceSimulationRuns.TabIndex = 9;
        lblBalanceSimulationRuns.Text = "Симуляций:";
        // 
        // txtBalanceSimulationRuns
        // 
        txtBalanceSimulationRuns.Location = new Point(211, 39);
        txtBalanceSimulationRuns.Name = "txtBalanceSimulationRuns";
        txtBalanceSimulationRuns.Size = new Size(48, 23);
        txtBalanceSimulationRuns.TabIndex = 10;
        txtBalanceSimulationRuns.Text = "30";
        // 
        // btnBalanceCheck
        // 
        btnBalanceCheck.Location = new Point(265, 39);
        btnBalanceCheck.Name = "btnBalanceCheck";
        btnBalanceCheck.Size = new Size(135, 25);
        btnBalanceCheck.TabIndex = 11;
        btnBalanceCheck.Text = "Проверить баланс";
        btnBalanceCheck.Click += btnBalanceCheck_Click;
        // 
        // btnBalanceGenerateDraft
        // 
        btnBalanceGenerateDraft.Location = new Point(406, 39);
        btnBalanceGenerateDraft.Name = "btnBalanceGenerateDraft";
        btnBalanceGenerateDraft.Size = new Size(205, 25);
        btnBalanceGenerateDraft.TabIndex = 12;
        btnBalanceGenerateDraft.Text = "Сгенерировать draft баланса";
        btnBalanceGenerateDraft.Click += btnBalanceGenerateDraft_Click;
        // 
        // btnMvpCheck
        // 
        btnMvpCheck.Location = new Point(617, 39);
        btnMvpCheck.Name = "btnMvpCheck";
        btnMvpCheck.Size = new Size(120, 25);
        btnMvpCheck.TabIndex = 13;
        btnMvpCheck.Text = "Проверить MVP";
        btnMvpCheck.Click += btnMvpCheck_Click;
        // 
        // btnMvpGenerateNextDraft
        // 
        btnMvpGenerateNextDraft.Location = new Point(743, 39);
        btnMvpGenerateNextDraft.Name = "btnMvpGenerateNextDraft";
        btnMvpGenerateNextDraft.Size = new Size(250, 25);
        btnMvpGenerateNextDraft.TabIndex = 14;
        btnMvpGenerateNextDraft.Text = "Сгенерировать следующий draft MVP";
        btnMvpGenerateNextDraft.Click += btnMvpGenerateNextDraft_Click;
        // 
        // btnDesignConversationSend
        // 
        btnDesignConversationSend.Location = new Point(999, 39);
        btnDesignConversationSend.Name = "btnDesignConversationSend";
        btnDesignConversationSend.Size = new Size(220, 25);
        btnDesignConversationSend.TabIndex = 15;
        btnDesignConversationSend.Text = "Отправить в дизайн-диалог";
        btnDesignConversationSend.Click += btnDesignConversationSend_Click;
        // 
        // gameCrafterSplit
        // 
        gameCrafterSplit.Dock = DockStyle.Fill;
        gameCrafterSplit.Location = new Point(3, 371);
        gameCrafterSplit.Name = "gameCrafterSplit";
        // 
        // gameCrafterSplit.Panel1
        // 
        gameCrafterSplit.Panel1.Controls.Add(gameCrafterLeftLayout);
        // 
        // gameCrafterSplit.Panel2
        // 
        gameCrafterSplit.Panel2.Controls.Add(txtDesignPreview);
        gameCrafterSplit.Size = new Size(1260, 388);
        gameCrafterSplit.SplitterDistance = 720;
        gameCrafterSplit.TabIndex = 2;
        // 
        // gameCrafterLeftLayout
        // 
        gameCrafterLeftLayout.ColumnCount = 1;
        gameCrafterLeftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        gameCrafterLeftLayout.Controls.Add(lvDesignSlots, 0, 0);
        gameCrafterLeftLayout.Controls.Add(lvDesignQuestions, 0, 1);
        gameCrafterLeftLayout.Controls.Add(txtDesignAnswer, 0, 2);
        gameCrafterLeftLayout.Controls.Add(btnDesignApplyAnswer, 0, 3);
        gameCrafterLeftLayout.Dock = DockStyle.Fill;
        gameCrafterLeftLayout.Location = new Point(0, 0);
        gameCrafterLeftLayout.Name = "gameCrafterLeftLayout";
        gameCrafterLeftLayout.RowCount = 4;
        gameCrafterLeftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        gameCrafterLeftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
        gameCrafterLeftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        gameCrafterLeftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        gameCrafterLeftLayout.Size = new Size(720, 388);
        gameCrafterLeftLayout.TabIndex = 0;
        // 
        // lvDesignSlots
        // 
        lvDesignSlots.Dock = DockStyle.Fill;
        lvDesignSlots.FullRowSelect = true;
        lvDesignSlots.GridLines = true;
        lvDesignSlots.Location = new Point(3, 3);
        lvDesignSlots.MultiSelect = false;
        lvDesignSlots.Name = "lvDesignSlots";
        lvDesignSlots.Size = new Size(714, 161);
        lvDesignSlots.TabIndex = 0;
        lvDesignSlots.UseCompatibleStateImageBehavior = false;
        lvDesignSlots.View = View.Details;
        lvDesignSlots.SelectedIndexChanged += lvDesignSlots_SelectedIndexChanged;
        // 
        // lvDesignQuestions
        // 
        lvDesignQuestions.Dock = DockStyle.Fill;
        lvDesignQuestions.FullRowSelect = true;
        lvDesignQuestions.GridLines = true;
        lvDesignQuestions.Location = new Point(3, 170);
        lvDesignQuestions.MultiSelect = false;
        lvDesignQuestions.Name = "lvDesignQuestions";
        lvDesignQuestions.Size = new Size(714, 90);
        lvDesignQuestions.TabIndex = 1;
        lvDesignQuestions.UseCompatibleStateImageBehavior = false;
        lvDesignQuestions.View = View.Details;
        lvDesignQuestions.SelectedIndexChanged += lvDesignQuestions_SelectedIndexChanged;
        // 
        // txtDesignAnswer
        // 
        txtDesignAnswer.Dock = DockStyle.Fill;
        txtDesignAnswer.Font = new Font("Consolas", 10F);
        txtDesignAnswer.Location = new Point(3, 266);
        txtDesignAnswer.Multiline = true;
        txtDesignAnswer.Name = "txtDesignAnswer";
        txtDesignAnswer.ScrollBars = ScrollBars.Vertical;
        txtDesignAnswer.Size = new Size(714, 82);
        txtDesignAnswer.TabIndex = 2;
        // 
        // btnDesignApplyAnswer
        // 
        btnDesignApplyAnswer.Dock = DockStyle.Fill;
        btnDesignApplyAnswer.Location = new Point(3, 354);
        btnDesignApplyAnswer.Name = "btnDesignApplyAnswer";
        btnDesignApplyAnswer.Size = new Size(714, 31);
        btnDesignApplyAnswer.TabIndex = 3;
        btnDesignApplyAnswer.Text = "Применить ответ";
        btnDesignApplyAnswer.Click += btnDesignApplyAnswer_Click;
        // 
        // txtDesignPreview
        // 
        txtDesignPreview.Dock = DockStyle.Fill;
        txtDesignPreview.Font = new Font("Consolas", 10F);
        txtDesignPreview.Location = new Point(0, 0);
        txtDesignPreview.Multiline = true;
        txtDesignPreview.Name = "txtDesignPreview";
        txtDesignPreview.ReadOnly = true;
        txtDesignPreview.ScrollBars = ScrollBars.Both;
        txtDesignPreview.Size = new Size(536, 388);
        txtDesignPreview.TabIndex = 0;
        // 
        // tabBrief
        // 
        tabBrief.Controls.Add(briefLayout);
        tabBrief.Location = new Point(4, 24);
        tabBrief.Name = "tabBrief";
        tabBrief.Size = new Size(1266, 762);
        tabBrief.TabIndex = 2;
        tabBrief.Text = "Бриф / Концепт";
        // 
        // briefLayout
        // 
        briefLayout.ColumnCount = 1;
        briefLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        briefLayout.Controls.Add(txtBriefConcept, 0, 0);
        briefLayout.Controls.Add(briefButtons, 0, 1);
        briefLayout.Dock = DockStyle.Fill;
        briefLayout.Location = new Point(0, 0);
        briefLayout.Name = "briefLayout";
        briefLayout.RowCount = 2;
        briefLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        briefLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        briefLayout.Size = new Size(1266, 762);
        briefLayout.TabIndex = 0;
        // 
        // txtBriefConcept
        // 
        txtBriefConcept.Dock = DockStyle.Fill;
        txtBriefConcept.Font = new Font("Consolas", 10F);
        txtBriefConcept.Location = new Point(3, 3);
        txtBriefConcept.Multiline = true;
        txtBriefConcept.Name = "txtBriefConcept";
        txtBriefConcept.ScrollBars = ScrollBars.Both;
        txtBriefConcept.Size = new Size(1260, 710);
        txtBriefConcept.TabIndex = 0;
        // 
        // briefButtons
        // 
        briefButtons.Controls.Add(btnApproveBrief);
        briefButtons.Controls.Add(btnApproveConcept);
        briefButtons.Controls.Add(btnApplyRevision);
        briefButtons.Dock = DockStyle.Fill;
        briefButtons.Location = new Point(3, 719);
        briefButtons.Name = "briefButtons";
        briefButtons.Size = new Size(1260, 40);
        briefButtons.TabIndex = 1;
        // 
        // btnApproveBrief
        // 
        btnApproveBrief.Location = new Point(3, 3);
        btnApproveBrief.Name = "btnApproveBrief";
        btnApproveBrief.Size = new Size(75, 23);
        btnApproveBrief.TabIndex = 0;
        btnApproveBrief.Text = "Утвердить бриф";
        btnApproveBrief.Click += btnApproveBrief_Click;
        // 
        // btnApproveConcept
        // 
        btnApproveConcept.Location = new Point(84, 3);
        btnApproveConcept.Name = "btnApproveConcept";
        btnApproveConcept.Size = new Size(75, 23);
        btnApproveConcept.TabIndex = 1;
        btnApproveConcept.Text = "Утвердить концепт";
        btnApproveConcept.Click += btnApproveConcept_Click;
        // 
        // btnApplyRevision
        // 
        btnApplyRevision.Location = new Point(165, 3);
        btnApplyRevision.Name = "btnApplyRevision";
        btnApplyRevision.Size = new Size(75, 23);
        btnApplyRevision.TabIndex = 2;
        btnApplyRevision.Text = "Сформировать правки";
        btnApplyRevision.Click += btnApplyRevision_Click;
        // 
        // tabContent
        // 
        tabContent.Controls.Add(tabContentInner);
        tabContent.Location = new Point(4, 24);
        tabContent.Name = "tabContent";
        tabContent.Size = new Size(1266, 762);
        tabContent.TabIndex = 3;
        tabContent.Text = "Контент";
        // 
        // tabContentInner
        // 
        tabContentInner.Controls.Add(tabWorld);
        tabContentInner.Controls.Add(tabCharacters);
        tabContentInner.Controls.Add(tabScenes);
        tabContentInner.Controls.Add(tabItems);
        tabContentInner.Controls.Add(tabStats);
        tabContentInner.Controls.Add(tabRelationships);
        tabContentInner.Controls.Add(tabCombat);
        tabContentInner.Dock = DockStyle.Fill;
        tabContentInner.Location = new Point(0, 0);
        tabContentInner.Name = "tabContentInner";
        tabContentInner.SelectedIndex = 0;
        tabContentInner.Size = new Size(1266, 762);
        tabContentInner.TabIndex = 0;
        // 
        // tabWorld
        // 
        tabWorld.Controls.Add(txtWorld);
        tabWorld.Location = new Point(4, 24);
        tabWorld.Name = "tabWorld";
        tabWorld.Size = new Size(1258, 734);
        tabWorld.TabIndex = 0;
        tabWorld.Text = "Мир";
        // 
        // txtWorld
        // 
        txtWorld.Dock = DockStyle.Fill;
        txtWorld.Font = new Font("Consolas", 10F);
        txtWorld.Location = new Point(0, 0);
        txtWorld.Multiline = true;
        txtWorld.Name = "txtWorld";
        txtWorld.ScrollBars = ScrollBars.Both;
        txtWorld.Size = new Size(1258, 734);
        txtWorld.TabIndex = 0;
        // 
        // tabCharacters
        // 
        tabCharacters.Controls.Add(lvCharacters);
        tabCharacters.Location = new Point(4, 24);
        tabCharacters.Name = "tabCharacters";
        tabCharacters.Size = new Size(1258, 734);
        tabCharacters.TabIndex = 1;
        tabCharacters.Text = "Персонажи";
        // 
        // lvCharacters
        // 
        lvCharacters.Dock = DockStyle.Fill;
        lvCharacters.FullRowSelect = true;
        lvCharacters.Location = new Point(0, 0);
        lvCharacters.Name = "lvCharacters";
        lvCharacters.Size = new Size(1258, 734);
        lvCharacters.TabIndex = 0;
        lvCharacters.UseCompatibleStateImageBehavior = false;
        lvCharacters.View = View.Details;
        // 
        // tabScenes
        // 
        tabScenes.Controls.Add(lvScenes);
        tabScenes.Location = new Point(4, 24);
        tabScenes.Name = "tabScenes";
        tabScenes.Size = new Size(1258, 734);
        tabScenes.TabIndex = 2;
        tabScenes.Text = "Сцены";
        // 
        // lvScenes
        // 
        lvScenes.Dock = DockStyle.Fill;
        lvScenes.FullRowSelect = true;
        lvScenes.Location = new Point(0, 0);
        lvScenes.Name = "lvScenes";
        lvScenes.Size = new Size(1258, 734);
        lvScenes.TabIndex = 0;
        lvScenes.UseCompatibleStateImageBehavior = false;
        lvScenes.View = View.Details;
        // 
        // tabItems
        // 
        tabItems.Controls.Add(lvItems);
        tabItems.Location = new Point(4, 24);
        tabItems.Name = "tabItems";
        tabItems.Size = new Size(1258, 734);
        tabItems.TabIndex = 3;
        tabItems.Text = "Предметы";
        // 
        // lvItems
        // 
        lvItems.Dock = DockStyle.Fill;
        lvItems.FullRowSelect = true;
        lvItems.Location = new Point(0, 0);
        lvItems.Name = "lvItems";
        lvItems.Size = new Size(1258, 734);
        lvItems.TabIndex = 0;
        lvItems.UseCompatibleStateImageBehavior = false;
        lvItems.View = View.Details;
        // 
        // tabStats
        // 
        tabStats.Controls.Add(lvStats);
        tabStats.Location = new Point(4, 24);
        tabStats.Name = "tabStats";
        tabStats.Size = new Size(1258, 734);
        tabStats.TabIndex = 4;
        tabStats.Text = "Статы";
        // 
        // lvStats
        // 
        lvStats.Dock = DockStyle.Fill;
        lvStats.FullRowSelect = true;
        lvStats.Location = new Point(0, 0);
        lvStats.Name = "lvStats";
        lvStats.Size = new Size(1258, 734);
        lvStats.TabIndex = 0;
        lvStats.UseCompatibleStateImageBehavior = false;
        lvStats.View = View.Details;
        // 
        // tabRelationships
        // 
        tabRelationships.Controls.Add(lvRelationships);
        tabRelationships.Location = new Point(4, 24);
        tabRelationships.Name = "tabRelationships";
        tabRelationships.Size = new Size(1258, 734);
        tabRelationships.TabIndex = 5;
        tabRelationships.Text = "Отношения";
        // 
        // lvRelationships
        // 
        lvRelationships.Dock = DockStyle.Fill;
        lvRelationships.FullRowSelect = true;
        lvRelationships.Location = new Point(0, 0);
        lvRelationships.Name = "lvRelationships";
        lvRelationships.Size = new Size(1258, 734);
        lvRelationships.TabIndex = 0;
        lvRelationships.UseCompatibleStateImageBehavior = false;
        lvRelationships.View = View.Details;
        // 
        // tabCombat
        // 
        tabCombat.Controls.Add(txtCombat);
        tabCombat.Location = new Point(4, 24);
        tabCombat.Name = "tabCombat";
        tabCombat.Size = new Size(1258, 734);
        tabCombat.TabIndex = 6;
        tabCombat.Text = "Боевка";
        // 
        // txtCombat
        // 
        txtCombat.Dock = DockStyle.Fill;
        txtCombat.Font = new Font("Consolas", 10F);
        txtCombat.Location = new Point(0, 0);
        txtCombat.Multiline = true;
        txtCombat.Name = "txtCombat";
        txtCombat.ScrollBars = ScrollBars.Both;
        txtCombat.Size = new Size(1258, 734);
        txtCombat.TabIndex = 0;
        // 
        // tabPipeline
        // 
        tabPipeline.Controls.Add(pipelineLayout);
        tabPipeline.Location = new Point(4, 24);
        tabPipeline.Name = "tabPipeline";
        tabPipeline.Size = new Size(1266, 762);
        tabPipeline.TabIndex = 9;
        tabPipeline.Text = "Пайплайн";
        // 
        // pipelineLayout
        // 
        pipelineLayout.ColumnCount = 1;
        pipelineLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pipelineLayout.Controls.Add(lblPipelineIntro, 0, 0);
        pipelineLayout.Controls.Add(pipelineSplit, 0, 1);
        pipelineLayout.Dock = DockStyle.Fill;
        pipelineLayout.Location = new Point(0, 0);
        pipelineLayout.Name = "pipelineLayout";
        pipelineLayout.Padding = new Padding(8);
        pipelineLayout.RowCount = 2;
        pipelineLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        pipelineLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pipelineLayout.Size = new Size(1266, 762);
        pipelineLayout.TabIndex = 0;
        // 
        // lblPipelineIntro
        // 
        lblPipelineIntro.Dock = DockStyle.Fill;
        lblPipelineIntro.Location = new Point(11, 8);
        lblPipelineIntro.Name = "lblPipelineIntro";
        lblPipelineIntro.Size = new Size(1244, 54);
        lblPipelineIntro.TabIndex = 0;
        lblPipelineIntro.Text = "Генерируйте игру маленькими управляемыми этапами. Batch-этапы сохраняются как draft и применяются только после ручного подтверждения. Изображения опциональны: игру можно проходить и без них.";
        lblPipelineIntro.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // pipelineSplit
        // 
        pipelineSplit.Dock = DockStyle.Fill;
        pipelineSplit.Location = new Point(11, 65);
        pipelineSplit.Name = "pipelineSplit";
        // 
        // pipelineSplit.Panel1
        // 
        pipelineSplit.Panel1.Controls.Add(lvGenerationPlan);
        // 
        // pipelineSplit.Panel2
        // 
        pipelineSplit.Panel2.Controls.Add(pipelineControlsLayout);
        pipelineSplit.Size = new Size(1244, 686);
        pipelineSplit.SplitterDistance = 780;
        pipelineSplit.TabIndex = 1;
        // 
        // lvGenerationPlan
        // 
        lvGenerationPlan.Dock = DockStyle.Fill;
        lvGenerationPlan.FullRowSelect = true;
        lvGenerationPlan.Location = new Point(0, 0);
        lvGenerationPlan.MultiSelect = false;
        lvGenerationPlan.Name = "lvGenerationPlan";
        lvGenerationPlan.Size = new Size(780, 686);
        lvGenerationPlan.TabIndex = 0;
        lvGenerationPlan.UseCompatibleStateImageBehavior = false;
        lvGenerationPlan.View = View.Details;
        lvGenerationPlan.SelectedIndexChanged += lvGenerationPlan_SelectedIndexChanged;
        // 
        // pipelineControlsLayout
        // 
        pipelineControlsLayout.ColumnCount = 2;
        pipelineControlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        pipelineControlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pipelineControlsLayout.Controls.Add(lblPipelineBatchCount, 0, 0);
        pipelineControlsLayout.Controls.Add(nudPipelineBatchCount, 1, 0);
        pipelineControlsLayout.Controls.Add(lblPipelineCategory, 0, 1);
        pipelineControlsLayout.Controls.Add(cmbPipelineCategory, 1, 1);
        pipelineControlsLayout.Controls.Add(lblPipelineRules, 0, 2);
        pipelineControlsLayout.Controls.Add(txtPipelineRules, 1, 2);
        pipelineControlsLayout.Controls.Add(lblGenerationPreferences, 0, 3);
        pipelineControlsLayout.Controls.Add(txtPreferenceGeneral, 1, 3);
        pipelineControlsLayout.Controls.Add(txtPreferenceSkills, 1, 4);
        pipelineControlsLayout.Controls.Add(txtPreferenceProgression, 1, 5);
        pipelineControlsLayout.Controls.Add(txtPreferenceCombat, 1, 6);
        pipelineControlsLayout.Controls.Add(txtPreferenceAtmosphere, 1, 7);
        pipelineControlsLayout.Controls.Add(txtPreferenceBalance, 1, 8);
        pipelineControlsLayout.Controls.Add(txtPreferenceForbidden, 1, 9);
        pipelineControlsLayout.Controls.Add(txtPreferenceNotes, 1, 10);
        pipelineControlsLayout.Controls.Add(btnSaveGenerationPreferences, 1, 11);
        pipelineControlsLayout.Controls.Add(pipelineButtons, 0, 12);
        pipelineControlsLayout.Controls.Add(txtPipelineDraftInfo, 0, 13);
        pipelineControlsLayout.Controls.Add(txtPipelineDetails, 0, 14);
        pipelineControlsLayout.Dock = DockStyle.Fill;
        pipelineControlsLayout.Location = new Point(0, 0);
        pipelineControlsLayout.Name = "pipelineControlsLayout";
        pipelineControlsLayout.RowCount = 15;
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        pipelineControlsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pipelineControlsLayout.Size = new Size(460, 686);
        pipelineControlsLayout.TabIndex = 0;
        // 
        // lblPipelineBatchCount
        // 
        lblPipelineBatchCount.Dock = DockStyle.Fill;
        lblPipelineBatchCount.Location = new Point(3, 0);
        lblPipelineBatchCount.Name = "lblPipelineBatchCount";
        lblPipelineBatchCount.Size = new Size(124, 34);
        lblPipelineBatchCount.TabIndex = 0;
        lblPipelineBatchCount.Text = "Количество:";
        lblPipelineBatchCount.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nudPipelineBatchCount
        // 
        nudPipelineBatchCount.Dock = DockStyle.Fill;
        nudPipelineBatchCount.Location = new Point(133, 3);
        nudPipelineBatchCount.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
        nudPipelineBatchCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudPipelineBatchCount.Name = "nudPipelineBatchCount";
        nudPipelineBatchCount.Size = new Size(324, 23);
        nudPipelineBatchCount.TabIndex = 1;
        nudPipelineBatchCount.Value = new decimal(new int[] { 5, 0, 0, 0 });
        // 
        // lblPipelineCategory
        // 
        lblPipelineCategory.Dock = DockStyle.Fill;
        lblPipelineCategory.Location = new Point(3, 34);
        lblPipelineCategory.Name = "lblPipelineCategory";
        lblPipelineCategory.Size = new Size(124, 34);
        lblPipelineCategory.TabIndex = 2;
        lblPipelineCategory.Text = "Категория:";
        lblPipelineCategory.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbPipelineCategory
        // 
        cmbPipelineCategory.Dock = DockStyle.Fill;
        cmbPipelineCategory.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPipelineCategory.Items.AddRange(new object[] { "stats-resources", "formulas", "status-effects", "progression", "gameplay-actions", "world-state", "equipment", "items", "skills", "spells", "locations", "scenes", "encounters", "image-prompts" });
        cmbPipelineCategory.Location = new Point(133, 37);
        cmbPipelineCategory.Name = "cmbPipelineCategory";
        cmbPipelineCategory.Size = new Size(324, 23);
        cmbPipelineCategory.TabIndex = 3;
        // 
        // lblPipelineRules
        // 
        lblPipelineRules.Dock = DockStyle.Fill;
        lblPipelineRules.Location = new Point(3, 68);
        lblPipelineRules.Name = "lblPipelineRules";
        lblPipelineRules.Size = new Size(124, 92);
        lblPipelineRules.TabIndex = 4;
        lblPipelineRules.Text = "Правила пачки:";
        lblPipelineRules.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtPipelineRules
        // 
        txtPipelineRules.Dock = DockStyle.Fill;
        txtPipelineRules.Location = new Point(133, 71);
        txtPipelineRules.Multiline = true;
        txtPipelineRules.Name = "txtPipelineRules";
        txtPipelineRules.ScrollBars = ScrollBars.Vertical;
        txtPipelineRules.Size = new Size(324, 86);
        txtPipelineRules.TabIndex = 5;
        // 
        // lblGenerationPreferences
        // 
        lblGenerationPreferences.Dock = DockStyle.Fill;
        lblGenerationPreferences.Location = new Point(3, 160);
        lblGenerationPreferences.Name = "lblGenerationPreferences";
        pipelineControlsLayout.SetRowSpan(lblGenerationPreferences, 8);
        lblGenerationPreferences.Size = new Size(124, 304);
        lblGenerationPreferences.TabIndex = 6;
        lblGenerationPreferences.Text = "Пожелания генерации:\r\nгеймплей\r\nнавыки\r\nпрокачка\r\nбоёвка\r\nбаланс\r\nзапреты\r\nзаметки";
        lblGenerationPreferences.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtPreferenceGeneral
        // 
        txtPreferenceGeneral.Dock = DockStyle.Fill;
        txtPreferenceGeneral.Location = new Point(133, 163);
        txtPreferenceGeneral.Multiline = true;
        txtPreferenceGeneral.Name = "txtPreferenceGeneral";
        txtPreferenceGeneral.PlaceholderText = "Общие пожелания к геймплею";
        txtPreferenceGeneral.ScrollBars = ScrollBars.Vertical;
        txtPreferenceGeneral.Size = new Size(324, 32);
        txtPreferenceGeneral.TabIndex = 7;
        // 
        // txtPreferenceSkills
        // 
        txtPreferenceSkills.Dock = DockStyle.Fill;
        txtPreferenceSkills.Location = new Point(133, 201);
        txtPreferenceSkills.Multiline = true;
        txtPreferenceSkills.Name = "txtPreferenceSkills";
        txtPreferenceSkills.PlaceholderText = "Навыки и способности";
        txtPreferenceSkills.ScrollBars = ScrollBars.Vertical;
        txtPreferenceSkills.Size = new Size(324, 32);
        txtPreferenceSkills.TabIndex = 8;
        // 
        // txtPreferenceProgression
        // 
        txtPreferenceProgression.Dock = DockStyle.Fill;
        txtPreferenceProgression.Location = new Point(133, 239);
        txtPreferenceProgression.Multiline = true;
        txtPreferenceProgression.Name = "txtPreferenceProgression";
        txtPreferenceProgression.PlaceholderText = "Прокачка и опыт";
        txtPreferenceProgression.ScrollBars = ScrollBars.Vertical;
        txtPreferenceProgression.Size = new Size(324, 32);
        txtPreferenceProgression.TabIndex = 9;
        // 
        // txtPreferenceCombat
        // 
        txtPreferenceCombat.Dock = DockStyle.Fill;
        txtPreferenceCombat.Location = new Point(133, 277);
        txtPreferenceCombat.Multiline = true;
        txtPreferenceCombat.Name = "txtPreferenceCombat";
        txtPreferenceCombat.PlaceholderText = "Будущая боёвка";
        txtPreferenceCombat.ScrollBars = ScrollBars.Vertical;
        txtPreferenceCombat.Size = new Size(324, 32);
        txtPreferenceCombat.TabIndex = 10;
        // 
        // txtPreferenceAtmosphere
        // 
        txtPreferenceAtmosphere.Dock = DockStyle.Fill;
        txtPreferenceAtmosphere.Location = new Point(133, 315);
        txtPreferenceAtmosphere.Multiline = true;
        txtPreferenceAtmosphere.Name = "txtPreferenceAtmosphere";
        txtPreferenceAtmosphere.PlaceholderText = "Атмосфера/мир";
        txtPreferenceAtmosphere.ScrollBars = ScrollBars.Vertical;
        txtPreferenceAtmosphere.Size = new Size(324, 32);
        txtPreferenceAtmosphere.TabIndex = 11;
        // 
        // txtPreferenceBalance
        // 
        txtPreferenceBalance.Dock = DockStyle.Fill;
        txtPreferenceBalance.Location = new Point(133, 353);
        txtPreferenceBalance.Multiline = true;
        txtPreferenceBalance.Name = "txtPreferenceBalance";
        txtPreferenceBalance.PlaceholderText = "Баланс и стиль";
        txtPreferenceBalance.ScrollBars = ScrollBars.Vertical;
        txtPreferenceBalance.Size = new Size(324, 32);
        txtPreferenceBalance.TabIndex = 12;
        // 
        // txtPreferenceForbidden
        // 
        txtPreferenceForbidden.Dock = DockStyle.Fill;
        txtPreferenceForbidden.Location = new Point(133, 391);
        txtPreferenceForbidden.Multiline = true;
        txtPreferenceForbidden.Name = "txtPreferenceForbidden";
        txtPreferenceForbidden.PlaceholderText = "Запрещать / избегать";
        txtPreferenceForbidden.ScrollBars = ScrollBars.Vertical;
        txtPreferenceForbidden.Size = new Size(324, 32);
        txtPreferenceForbidden.TabIndex = 13;
        // 
        // txtPreferenceNotes
        // 
        txtPreferenceNotes.Dock = DockStyle.Fill;
        txtPreferenceNotes.Location = new Point(133, 429);
        txtPreferenceNotes.Multiline = true;
        txtPreferenceNotes.Name = "txtPreferenceNotes";
        txtPreferenceNotes.PlaceholderText = "Заметки";
        txtPreferenceNotes.ScrollBars = ScrollBars.Vertical;
        txtPreferenceNotes.Size = new Size(324, 32);
        txtPreferenceNotes.TabIndex = 14;
        // 
        // btnSaveGenerationPreferences
        // 
        btnSaveGenerationPreferences.Dock = DockStyle.Fill;
        btnSaveGenerationPreferences.Location = new Point(133, 467);
        btnSaveGenerationPreferences.Name = "btnSaveGenerationPreferences";
        btnSaveGenerationPreferences.Size = new Size(324, 28);
        btnSaveGenerationPreferences.TabIndex = 15;
        btnSaveGenerationPreferences.Text = "Сохранить пожелания";
        btnSaveGenerationPreferences.Click += btnSaveGenerationPreferences_Click;
        // 
        // pipelineButtons
        // 
        pipelineButtons.AutoScroll = true;
        pipelineControlsLayout.SetColumnSpan(pipelineButtons, 2);
        pipelineButtons.Controls.Add(btnRefreshGenerationPlan);
        pipelineButtons.Controls.Add(btnCheckMechanics);
        pipelineButtons.Controls.Add(btnRunSelectedPipelineStep);
        pipelineButtons.Controls.Add(btnReviewLatestDraft);
        pipelineButtons.Controls.Add(btnApplyLatestDraft);
        pipelineButtons.Controls.Add(btnRejectLatestDraft);
        pipelineButtons.Controls.Add(btnOpenDraftsFolderPipeline);
        pipelineButtons.Controls.Add(btnOpenCurrentDraft);
        pipelineButtons.Dock = DockStyle.Fill;
        pipelineButtons.Location = new Point(3, 501);
        pipelineButtons.Name = "pipelineButtons";
        pipelineButtons.Size = new Size(454, 64);
        pipelineButtons.TabIndex = 15;
        // 
        // btnRefreshGenerationPlan
        // 
        btnRefreshGenerationPlan.Location = new Point(3, 3);
        btnRefreshGenerationPlan.Name = "btnRefreshGenerationPlan";
        btnRefreshGenerationPlan.Size = new Size(120, 28);
        btnRefreshGenerationPlan.TabIndex = 0;
        btnRefreshGenerationPlan.Text = "Обновить план";
        btnRefreshGenerationPlan.Click += btnRefreshGenerationPlan_Click;
        // 
        // btnCheckMechanics
        // 
        btnCheckMechanics.Location = new Point(129, 3);
        btnCheckMechanics.Name = "btnCheckMechanics";
        btnCheckMechanics.Size = new Size(140, 28);
        btnCheckMechanics.TabIndex = 1;
        btnCheckMechanics.Text = "Проверить механики";
        btnCheckMechanics.Click += btnCheckMechanics_Click;
        // 
        // btnRunSelectedPipelineStep
        // 
        btnRunSelectedPipelineStep.Location = new Point(3, 37);
        btnRunSelectedPipelineStep.Name = "btnRunSelectedPipelineStep";
        btnRunSelectedPipelineStep.Size = new Size(230, 28);
        btnRunSelectedPipelineStep.TabIndex = 2;
        btnRunSelectedPipelineStep.Text = "Сгенерировать выбранный этап как draft";
        btnRunSelectedPipelineStep.Click += btnRunSelectedPipelineStep_Click;
        // 
        // btnReviewLatestDraft
        // 
        btnReviewLatestDraft.Location = new Point(239, 37);
        btnReviewLatestDraft.Name = "btnReviewLatestDraft";
        btnReviewLatestDraft.Size = new Size(150, 28);
        btnReviewLatestDraft.TabIndex = 2;
        btnReviewLatestDraft.Text = "Проверить последний draft";
        btnReviewLatestDraft.Click += btnReviewLatestDraft_Click;
        // 
        // btnApplyLatestDraft
        // 
        btnApplyLatestDraft.Location = new Point(3, 71);
        btnApplyLatestDraft.Name = "btnApplyLatestDraft";
        btnApplyLatestDraft.Size = new Size(145, 28);
        btnApplyLatestDraft.TabIndex = 3;
        btnApplyLatestDraft.Text = "Применить draft";
        btnApplyLatestDraft.Click += btnApplyLatestDraft_Click;
        // 
        // btnRejectLatestDraft
        // 
        btnRejectLatestDraft.Location = new Point(154, 71);
        btnRejectLatestDraft.Name = "btnRejectLatestDraft";
        btnRejectLatestDraft.Size = new Size(130, 28);
        btnRejectLatestDraft.TabIndex = 4;
        btnRejectLatestDraft.Text = "Отклонить draft";
        btnRejectLatestDraft.Click += btnRejectLatestDraft_Click;
        // 
        // btnOpenDraftsFolderPipeline
        // 
        btnOpenDraftsFolderPipeline.Location = new Point(290, 71);
        btnOpenDraftsFolderPipeline.Name = "btnOpenDraftsFolderPipeline";
        btnOpenDraftsFolderPipeline.Size = new Size(120, 28);
        btnOpenDraftsFolderPipeline.TabIndex = 5;
        btnOpenDraftsFolderPipeline.Text = "Папка drafts";
        btnOpenDraftsFolderPipeline.Click += btnOpenDraftsFolderPipeline_Click;
        // 
        // btnOpenCurrentDraft
        // 
        btnOpenCurrentDraft.Location = new Point(3, 105);
        btnOpenCurrentDraft.Name = "btnOpenCurrentDraft";
        btnOpenCurrentDraft.Size = new Size(150, 28);
        btnOpenCurrentDraft.TabIndex = 6;
        btnOpenCurrentDraft.Text = "Открыть текущий draft";
        btnOpenCurrentDraft.Click += btnOpenCurrentDraft_Click;
        // 
        // txtPipelineDraftInfo
        // 
        pipelineControlsLayout.SetColumnSpan(txtPipelineDraftInfo, 2);
        txtPipelineDraftInfo.Dock = DockStyle.Fill;
        txtPipelineDraftInfo.Font = new Font("Consolas", 9F);
        txtPipelineDraftInfo.Location = new Point(3, 571);
        txtPipelineDraftInfo.Multiline = true;
        txtPipelineDraftInfo.Name = "txtPipelineDraftInfo";
        txtPipelineDraftInfo.ReadOnly = true;
        txtPipelineDraftInfo.ScrollBars = ScrollBars.Vertical;
        txtPipelineDraftInfo.Size = new Size(454, 64);
        txtPipelineDraftInfo.TabIndex = 16;
        txtPipelineDraftInfo.Text = "Текущий draft: нет";
        // 
        // txtPipelineDetails
        // 
        pipelineControlsLayout.SetColumnSpan(txtPipelineDetails, 2);
        txtPipelineDetails.Dock = DockStyle.Fill;
        txtPipelineDetails.Font = new Font("Consolas", 10F);
        txtPipelineDetails.Location = new Point(3, 641);
        txtPipelineDetails.Multiline = true;
        txtPipelineDetails.Name = "txtPipelineDetails";
        txtPipelineDetails.ScrollBars = ScrollBars.Both;
        txtPipelineDetails.Size = new Size(454, 42);
        txtPipelineDetails.TabIndex = 17;
        // 
        // tabAssets
        // 
        tabAssets.Controls.Add(assetsLayout);
        tabAssets.Location = new Point(4, 24);
        tabAssets.Name = "tabAssets";
        tabAssets.Size = new Size(1266, 762);
        tabAssets.TabIndex = 4;
        tabAssets.Text = "Ассеты";
        // 
        // assetsLayout
        // 
        assetsLayout.ColumnCount = 2;
        assetsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        assetsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
        assetsLayout.Controls.Add(lvPrompts, 0, 0);
        assetsLayout.Controls.Add(txtPromptDetails, 1, 0);
        assetsLayout.Controls.Add(assetsBottomLayout, 0, 1);
        assetsLayout.Dock = DockStyle.Fill;
        assetsLayout.Location = new Point(0, 0);
        assetsLayout.Name = "assetsLayout";
        assetsLayout.RowCount = 2;
        assetsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        assetsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 205F));
        assetsLayout.Size = new Size(1266, 762);
        assetsLayout.TabIndex = 0;
        // 
        // lvPrompts
        // 
        lvPrompts.Dock = DockStyle.Fill;
        lvPrompts.FullRowSelect = true;
        lvPrompts.Location = new Point(3, 3);
        lvPrompts.Name = "lvPrompts";
        lvPrompts.Size = new Size(563, 551);
        lvPrompts.TabIndex = 0;
        lvPrompts.UseCompatibleStateImageBehavior = false;
        lvPrompts.View = View.Details;
        lvPrompts.SelectedIndexChanged += lvPrompts_SelectedIndexChanged;
        // 
        // txtPromptDetails
        // 
        txtPromptDetails.Dock = DockStyle.Fill;
        txtPromptDetails.Font = new Font("Consolas", 10F);
        txtPromptDetails.Location = new Point(572, 3);
        txtPromptDetails.Multiline = true;
        txtPromptDetails.Name = "txtPromptDetails";
        txtPromptDetails.ScrollBars = ScrollBars.Both;
        txtPromptDetails.Size = new Size(691, 551);
        txtPromptDetails.TabIndex = 1;
        // 
        // assetsBottomLayout
        // 
        assetsBottomLayout.ColumnCount = 2;
        assetsLayout.SetColumnSpan(assetsBottomLayout, 2);
        assetsBottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
        assetsBottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        assetsBottomLayout.Controls.Add(assetsBatchOptions, 0, 0);
        assetsBottomLayout.Controls.Add(assetsBatchButtons, 0, 1);
        assetsBottomLayout.Controls.Add(lblBatchRules, 0, 2);
        assetsBottomLayout.Controls.Add(txtBatchRules, 1, 2);
        assetsBottomLayout.Controls.Add(assetsButtons, 0, 3);
        assetsBottomLayout.Dock = DockStyle.Fill;
        assetsBottomLayout.Location = new Point(3, 560);
        assetsBottomLayout.Name = "assetsBottomLayout";
        assetsBottomLayout.RowCount = 4;
        assetsBottomLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        assetsBottomLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        assetsBottomLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        assetsBottomLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        assetsBottomLayout.Size = new Size(1260, 199);
        assetsBottomLayout.TabIndex = 2;
        // 
        // assetsBatchOptions
        // 
        assetsBottomLayout.SetColumnSpan(assetsBatchOptions, 2);
        assetsBatchOptions.Controls.Add(lblBatchCount);
        assetsBatchOptions.Controls.Add(nudBatchCount);
        assetsBatchOptions.Controls.Add(lblBatchCategory);
        assetsBatchOptions.Controls.Add(cmbBatchCategory);
        assetsBatchOptions.Dock = DockStyle.Fill;
        assetsBatchOptions.Location = new Point(3, 3);
        assetsBatchOptions.Name = "assetsBatchOptions";
        assetsBatchOptions.Size = new Size(1254, 28);
        assetsBatchOptions.TabIndex = 0;
        // 
        // lblBatchCount
        // 
        lblBatchCount.AutoSize = true;
        lblBatchCount.Location = new Point(3, 6);
        lblBatchCount.Margin = new Padding(3, 6, 3, 0);
        lblBatchCount.Name = "lblBatchCount";
        lblBatchCount.Size = new Size(75, 15);
        lblBatchCount.TabIndex = 0;
        lblBatchCount.Text = "Количество:";
        // 
        // nudBatchCount
        // 
        nudBatchCount.Location = new Point(84, 3);
        nudBatchCount.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
        nudBatchCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudBatchCount.Name = "nudBatchCount";
        nudBatchCount.Size = new Size(60, 23);
        nudBatchCount.TabIndex = 1;
        nudBatchCount.Value = new decimal(new int[] { 5, 0, 0, 0 });
        // 
        // lblBatchCategory
        // 
        lblBatchCategory.AutoSize = true;
        lblBatchCategory.Location = new Point(150, 6);
        lblBatchCategory.Margin = new Padding(3, 6, 3, 0);
        lblBatchCategory.Name = "lblBatchCategory";
        lblBatchCategory.Size = new Size(66, 15);
        lblBatchCategory.TabIndex = 2;
        lblBatchCategory.Text = "Категория:";
        // 
        // cmbBatchCategory
        // 
        cmbBatchCategory.Items.AddRange(new object[] { "general", "stats-resources", "formulas", "status-effects", "progression", "gameplay-actions", "items", "equipment", "skills", "spells", "locations", "scenes", "encounters" });
        cmbBatchCategory.Location = new Point(222, 3);
        cmbBatchCategory.Name = "cmbBatchCategory";
        cmbBatchCategory.Size = new Size(130, 23);
        cmbBatchCategory.TabIndex = 3;
        cmbBatchCategory.Text = "general";
        // 
        // assetsBatchButtons
        // 
        assetsBatchButtons.AutoScroll = true;
        assetsBottomLayout.SetColumnSpan(assetsBatchButtons, 2);
        assetsBatchButtons.Controls.Add(btnGenerateStatsResourcesBatch);
        assetsBatchButtons.Controls.Add(btnGenerateItemsBatch);
        assetsBatchButtons.Controls.Add(btnGenerateEquipmentBatch);
        assetsBatchButtons.Controls.Add(btnGenerateSkillsBatch);
        assetsBatchButtons.Controls.Add(btnGenerateSpellsBatch);
        assetsBatchButtons.Controls.Add(btnGenerateLocationsBatch);
        assetsBatchButtons.Controls.Add(btnGenerateScenesBatch);
        assetsBatchButtons.Controls.Add(btnGenerateEncountersBatch);
        assetsBatchButtons.Dock = DockStyle.Fill;
        assetsBatchButtons.Location = new Point(3, 37);
        assetsBatchButtons.Name = "assetsBatchButtons";
        assetsBatchButtons.Size = new Size(1254, 28);
        assetsBatchButtons.TabIndex = 1;
        assetsBatchButtons.WrapContents = false;
        // 
        // btnGenerateStatsResourcesBatch
        // 
        btnGenerateStatsResourcesBatch.Location = new Point(3, 3);
        btnGenerateStatsResourcesBatch.Name = "btnGenerateStatsResourcesBatch";
        btnGenerateStatsResourcesBatch.Size = new Size(112, 23);
        btnGenerateStatsResourcesBatch.TabIndex = 4;
        btnGenerateStatsResourcesBatch.Text = "Статы/ресурсы";
        btnGenerateStatsResourcesBatch.Click += btnGenerateStatsResourcesBatch_Click;
        // 
        // btnGenerateItemsBatch
        // 
        btnGenerateItemsBatch.Location = new Point(121, 3);
        btnGenerateItemsBatch.Name = "btnGenerateItemsBatch";
        btnGenerateItemsBatch.Size = new Size(82, 23);
        btnGenerateItemsBatch.TabIndex = 5;
        btnGenerateItemsBatch.Text = "Предметы";
        btnGenerateItemsBatch.Click += btnGenerateItemsBatch_Click;
        // 
        // btnGenerateEquipmentBatch
        // 
        btnGenerateEquipmentBatch.Location = new Point(209, 3);
        btnGenerateEquipmentBatch.Name = "btnGenerateEquipmentBatch";
        btnGenerateEquipmentBatch.Size = new Size(92, 23);
        btnGenerateEquipmentBatch.TabIndex = 6;
        btnGenerateEquipmentBatch.Text = "Экипировка";
        btnGenerateEquipmentBatch.Click += btnGenerateEquipmentBatch_Click;
        // 
        // btnGenerateSkillsBatch
        // 
        btnGenerateSkillsBatch.Location = new Point(307, 3);
        btnGenerateSkillsBatch.Name = "btnGenerateSkillsBatch";
        btnGenerateSkillsBatch.Size = new Size(80, 23);
        btnGenerateSkillsBatch.TabIndex = 7;
        btnGenerateSkillsBatch.Text = "Навыки";
        btnGenerateSkillsBatch.Click += btnGenerateSkillsBatch_Click;
        // 
        // btnGenerateSpellsBatch
        // 
        btnGenerateSpellsBatch.Location = new Point(393, 3);
        btnGenerateSpellsBatch.Name = "btnGenerateSpellsBatch";
        btnGenerateSpellsBatch.Size = new Size(96, 23);
        btnGenerateSpellsBatch.TabIndex = 8;
        btnGenerateSpellsBatch.Text = "Заклинания";
        btnGenerateSpellsBatch.Click += btnGenerateSpellsBatch_Click;
        // 
        // btnGenerateLocationsBatch
        // 
        btnGenerateLocationsBatch.Location = new Point(495, 3);
        btnGenerateLocationsBatch.Name = "btnGenerateLocationsBatch";
        btnGenerateLocationsBatch.Size = new Size(82, 23);
        btnGenerateLocationsBatch.TabIndex = 9;
        btnGenerateLocationsBatch.Text = "Локации";
        btnGenerateLocationsBatch.Click += btnGenerateLocationsBatch_Click;
        // 
        // btnGenerateScenesBatch
        // 
        btnGenerateScenesBatch.Location = new Point(583, 3);
        btnGenerateScenesBatch.Name = "btnGenerateScenesBatch";
        btnGenerateScenesBatch.Size = new Size(70, 23);
        btnGenerateScenesBatch.TabIndex = 10;
        btnGenerateScenesBatch.Text = "Сцены";
        btnGenerateScenesBatch.Click += btnGenerateScenesBatch_Click;
        // 
        // btnGenerateEncountersBatch
        // 
        btnGenerateEncountersBatch.Location = new Point(659, 3);
        btnGenerateEncountersBatch.Name = "btnGenerateEncountersBatch";
        btnGenerateEncountersBatch.Size = new Size(96, 23);
        btnGenerateEncountersBatch.TabIndex = 11;
        btnGenerateEncountersBatch.Text = "Энкаунтеры";
        btnGenerateEncountersBatch.Click += btnGenerateEncountersBatch_Click;
        // 
        // lblBatchRules
        // 
        lblBatchRules.Dock = DockStyle.Fill;
        lblBatchRules.Location = new Point(3, 68);
        lblBatchRules.Name = "lblBatchRules";
        lblBatchRules.Size = new Size(154, 86);
        lblBatchRules.TabIndex = 1;
        lblBatchRules.Text = "Правила для генерации пачки:";
        lblBatchRules.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtBatchRules
        // 
        txtBatchRules.Dock = DockStyle.Fill;
        txtBatchRules.Location = new Point(163, 71);
        txtBatchRules.Multiline = true;
        txtBatchRules.Name = "txtBatchRules";
        txtBatchRules.ScrollBars = ScrollBars.Vertical;
        txtBatchRules.Size = new Size(1094, 80);
        txtBatchRules.TabIndex = 2;
        // 
        // assetsButtons
        // 
        assetsBottomLayout.SetColumnSpan(assetsButtons, 2);
        assetsButtons.Controls.Add(btnBuildImagePrompts);
        assetsButtons.Controls.Add(btnApprovePrompt);
        assetsButtons.Controls.Add(btnRunFooocusQueue);
        assetsButtons.Controls.Add(btnImportAssets);
        assetsButtons.Controls.Add(btnSelectImage);
        assetsButtons.Dock = DockStyle.Fill;
        assetsButtons.Location = new Point(3, 157);
        assetsButtons.Name = "assetsButtons";
        assetsButtons.Size = new Size(1254, 39);
        assetsButtons.TabIndex = 3;
        // 
        // btnBuildImagePrompts
        // 
        btnBuildImagePrompts.Location = new Point(3, 3);
        btnBuildImagePrompts.Name = "btnBuildImagePrompts";
        btnBuildImagePrompts.Size = new Size(130, 23);
        btnBuildImagePrompts.TabIndex = 0;
        btnBuildImagePrompts.Text = "Image prompt-ы";
        btnBuildImagePrompts.Click += btnBuildImagePrompts_Click;
        // 
        // btnApprovePrompt
        // 
        btnApprovePrompt.Location = new Point(139, 3);
        btnApprovePrompt.Name = "btnApprovePrompt";
        btnApprovePrompt.Size = new Size(90, 23);
        btnApprovePrompt.TabIndex = 1;
        btnApprovePrompt.Text = "В очередь";
        btnApprovePrompt.Click += btnApprovePrompt_Click;
        // 
        // btnRunFooocusQueue
        // 
        btnRunFooocusQueue.Location = new Point(235, 3);
        btnRunFooocusQueue.Name = "btnRunFooocusQueue";
        btnRunFooocusQueue.Size = new Size(140, 23);
        btnRunFooocusQueue.TabIndex = 2;
        btnRunFooocusQueue.Text = "Экспорт + Fooocus";
        btnRunFooocusQueue.Click += btnRunFooocusQueue_Click;
        // 
        // btnImportAssets
        // 
        btnImportAssets.Location = new Point(381, 3);
        btnImportAssets.Name = "btnImportAssets";
        btnImportAssets.Size = new Size(90, 23);
        btnImportAssets.TabIndex = 3;
        btnImportAssets.Text = "Импорт";
        btnImportAssets.Click += btnImportAssets_Click;
        // 
        // btnSelectImage
        // 
        btnSelectImage.Location = new Point(477, 3);
        btnSelectImage.Name = "btnSelectImage";
        btnSelectImage.Size = new Size(160, 23);
        btnSelectImage.TabIndex = 4;
        btnSelectImage.Text = "Привязать изображение";
        btnSelectImage.Click += btnSelectImage_Click;
        // 
        // tabPlay
        // 
        tabPlay.Controls.Add(playLayout);
        tabPlay.Location = new Point(4, 24);
        tabPlay.Name = "tabPlay";
        tabPlay.Size = new Size(1266, 762);
        tabPlay.TabIndex = 5;
        tabPlay.Text = "Играть";
        // 
        // playLayout
        // 
        playLayout.ColumnCount = 2;
        playLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
        playLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        playLayout.Controls.Add(lblGameTitle, 0, 0);
        playLayout.Controls.Add(lblSceneTitle, 0, 1);
        playLayout.Controls.Add(picScene, 0, 2);
        playLayout.Controls.Add(txtSceneText, 0, 3);
        playLayout.Controls.Add(pnlChoices, 0, 4);
        playLayout.Controls.Add(tabRuntimeInfo, 1, 0);
        playLayout.Dock = DockStyle.Fill;
        playLayout.Location = new Point(0, 0);
        playLayout.Name = "playLayout";
        playLayout.RowCount = 5;
        playLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        playLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        playLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        playLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        playLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        playLayout.Size = new Size(1266, 762);
        playLayout.TabIndex = 0;
        // 
        // lblGameTitle
        // 
        lblGameTitle.Dock = DockStyle.Fill;
        lblGameTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblGameTitle.Location = new Point(3, 0);
        lblGameTitle.Name = "lblGameTitle";
        lblGameTitle.Size = new Size(829, 34);
        lblGameTitle.TabIndex = 0;
        // 
        // lblSceneTitle
        // 
        lblSceneTitle.Dock = DockStyle.Fill;
        lblSceneTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblSceneTitle.Location = new Point(3, 34);
        lblSceneTitle.Name = "lblSceneTitle";
        lblSceneTitle.Size = new Size(829, 34);
        lblSceneTitle.TabIndex = 1;
        // 
        // picScene
        // 
        picScene.BackColor = Color.FromArgb(28, 28, 28);
        picScene.Dock = DockStyle.Fill;
        picScene.Location = new Point(3, 71);
        picScene.Name = "picScene";
        picScene.Size = new Size(829, 306);
        picScene.SizeMode = PictureBoxSizeMode.Zoom;
        picScene.TabIndex = 2;
        picScene.TabStop = false;
        // 
        // txtSceneText
        // 
        txtSceneText.Dock = DockStyle.Fill;
        txtSceneText.Location = new Point(3, 383);
        txtSceneText.Multiline = true;
        txtSceneText.Name = "txtSceneText";
        txtSceneText.ReadOnly = true;
        txtSceneText.ScrollBars = ScrollBars.Vertical;
        txtSceneText.Size = new Size(829, 236);
        txtSceneText.TabIndex = 3;
        // 
        // pnlChoices
        // 
        pnlChoices.AutoScroll = true;
        pnlChoices.Dock = DockStyle.Fill;
        pnlChoices.Location = new Point(3, 625);
        pnlChoices.Name = "pnlChoices";
        pnlChoices.Size = new Size(829, 134);
        pnlChoices.TabIndex = 4;
        // 
        // tabRuntimeInfo
        // 
        tabRuntimeInfo.Controls.Add(tabRuntimeStatsPage);
        tabRuntimeInfo.Controls.Add(tabRuntimeInventoryPage);
        tabRuntimeInfo.Controls.Add(tabRuntimeRelationshipsPage);
        tabRuntimeInfo.Controls.Add(tabRuntimeQuestsPage);
        tabRuntimeInfo.Controls.Add(tabRuntimeLogPage);
        tabRuntimeInfo.Dock = DockStyle.Fill;
        tabRuntimeInfo.Location = new Point(838, 3);
        tabRuntimeInfo.Name = "tabRuntimeInfo";
        playLayout.SetRowSpan(tabRuntimeInfo, 5);
        tabRuntimeInfo.SelectedIndex = 0;
        tabRuntimeInfo.Size = new Size(425, 756);
        tabRuntimeInfo.TabIndex = 5;
        // 
        // tabRuntimeStatsPage
        // 
        tabRuntimeStatsPage.Controls.Add(lvRuntimeStats);
        tabRuntimeStatsPage.Location = new Point(4, 24);
        tabRuntimeStatsPage.Name = "tabRuntimeStatsPage";
        tabRuntimeStatsPage.Size = new Size(417, 728);
        tabRuntimeStatsPage.TabIndex = 0;
        tabRuntimeStatsPage.Text = "Статы";
        // 
        // lvRuntimeStats
        // 
        lvRuntimeStats.Dock = DockStyle.Fill;
        lvRuntimeStats.FullRowSelect = true;
        lvRuntimeStats.Location = new Point(0, 0);
        lvRuntimeStats.Name = "lvRuntimeStats";
        lvRuntimeStats.Size = new Size(417, 728);
        lvRuntimeStats.TabIndex = 0;
        lvRuntimeStats.UseCompatibleStateImageBehavior = false;
        lvRuntimeStats.View = View.Details;
        // 
        // tabRuntimeInventoryPage
        // 
        tabRuntimeInventoryPage.Controls.Add(lvRuntimeInventory);
        tabRuntimeInventoryPage.Location = new Point(4, 24);
        tabRuntimeInventoryPage.Name = "tabRuntimeInventoryPage";
        tabRuntimeInventoryPage.Size = new Size(417, 728);
        tabRuntimeInventoryPage.TabIndex = 1;
        tabRuntimeInventoryPage.Text = "Инвентарь";
        // 
        // lvRuntimeInventory
        // 
        lvRuntimeInventory.Dock = DockStyle.Fill;
        lvRuntimeInventory.FullRowSelect = true;
        lvRuntimeInventory.Location = new Point(0, 0);
        lvRuntimeInventory.Name = "lvRuntimeInventory";
        lvRuntimeInventory.Size = new Size(417, 728);
        lvRuntimeInventory.TabIndex = 0;
        lvRuntimeInventory.UseCompatibleStateImageBehavior = false;
        lvRuntimeInventory.View = View.Details;
        // 
        // tabRuntimeRelationshipsPage
        // 
        tabRuntimeRelationshipsPage.Controls.Add(lvRuntimeRelationships);
        tabRuntimeRelationshipsPage.Location = new Point(4, 24);
        tabRuntimeRelationshipsPage.Name = "tabRuntimeRelationshipsPage";
        tabRuntimeRelationshipsPage.Size = new Size(417, 728);
        tabRuntimeRelationshipsPage.TabIndex = 2;
        tabRuntimeRelationshipsPage.Text = "Отношения";
        // 
        // lvRuntimeRelationships
        // 
        lvRuntimeRelationships.Dock = DockStyle.Fill;
        lvRuntimeRelationships.FullRowSelect = true;
        lvRuntimeRelationships.Location = new Point(0, 0);
        lvRuntimeRelationships.Name = "lvRuntimeRelationships";
        lvRuntimeRelationships.Size = new Size(417, 728);
        lvRuntimeRelationships.TabIndex = 0;
        lvRuntimeRelationships.UseCompatibleStateImageBehavior = false;
        lvRuntimeRelationships.View = View.Details;
        // 
        // tabRuntimeQuestsPage
        // 
        tabRuntimeQuestsPage.Controls.Add(lvRuntimeQuests);
        tabRuntimeQuestsPage.Location = new Point(4, 24);
        tabRuntimeQuestsPage.Name = "tabRuntimeQuestsPage";
        tabRuntimeQuestsPage.Size = new Size(417, 728);
        tabRuntimeQuestsPage.TabIndex = 3;
        tabRuntimeQuestsPage.Text = "Задания";
        // 
        // lvRuntimeQuests
        // 
        lvRuntimeQuests.Dock = DockStyle.Fill;
        lvRuntimeQuests.FullRowSelect = true;
        lvRuntimeQuests.Location = new Point(0, 0);
        lvRuntimeQuests.Name = "lvRuntimeQuests";
        lvRuntimeQuests.Size = new Size(417, 728);
        lvRuntimeQuests.TabIndex = 0;
        lvRuntimeQuests.UseCompatibleStateImageBehavior = false;
        lvRuntimeQuests.View = View.Details;
        // 
        // tabRuntimeLogPage
        // 
        tabRuntimeLogPage.Controls.Add(txtRuntimeLog);
        tabRuntimeLogPage.Location = new Point(4, 24);
        tabRuntimeLogPage.Name = "tabRuntimeLogPage";
        tabRuntimeLogPage.Size = new Size(417, 728);
        tabRuntimeLogPage.TabIndex = 4;
        tabRuntimeLogPage.Text = "Лог";
        // 
        // txtRuntimeLog
        // 
        txtRuntimeLog.Dock = DockStyle.Fill;
        txtRuntimeLog.Font = new Font("Consolas", 10F);
        txtRuntimeLog.Location = new Point(0, 0);
        txtRuntimeLog.Multiline = true;
        txtRuntimeLog.Name = "txtRuntimeLog";
        txtRuntimeLog.ScrollBars = ScrollBars.Both;
        txtRuntimeLog.Size = new Size(417, 728);
        txtRuntimeLog.TabIndex = 0;
        // 
        // tabSaves
        // 
        tabSaves.Controls.Add(savesLayout);
        tabSaves.Location = new Point(4, 24);
        tabSaves.Name = "tabSaves";
        tabSaves.Size = new Size(1266, 762);
        tabSaves.TabIndex = 6;
        tabSaves.Text = "Сохранения";
        // 
        // savesLayout
        // 
        savesLayout.ColumnCount = 1;
        savesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        savesLayout.Controls.Add(lstSaves, 0, 0);
        savesLayout.Controls.Add(savesButtons, 0, 1);
        savesLayout.Dock = DockStyle.Fill;
        savesLayout.Location = new Point(0, 0);
        savesLayout.Name = "savesLayout";
        savesLayout.RowCount = 2;
        savesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        savesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        savesLayout.Size = new Size(1266, 762);
        savesLayout.TabIndex = 0;
        // 
        // lstSaves
        // 
        lstSaves.Dock = DockStyle.Fill;
        lstSaves.ItemHeight = 15;
        lstSaves.Location = new Point(3, 3);
        lstSaves.Name = "lstSaves";
        lstSaves.Size = new Size(1260, 710);
        lstSaves.TabIndex = 0;
        // 
        // savesButtons
        // 
        savesButtons.Controls.Add(btnNewRun);
        savesButtons.Controls.Add(btnOpenPlayWindow);
        savesButtons.Controls.Add(btnSaveProgress);
        savesButtons.Controls.Add(btnLoadProgress);
        savesButtons.Controls.Add(btnDeleteSave);
        savesButtons.Dock = DockStyle.Fill;
        savesButtons.Location = new Point(3, 719);
        savesButtons.Name = "savesButtons";
        savesButtons.Size = new Size(1260, 40);
        savesButtons.TabIndex = 1;
        // 
        // btnNewRun
        // 
        btnNewRun.Location = new Point(3, 3);
        btnNewRun.Name = "btnNewRun";
        btnNewRun.Size = new Size(75, 23);
        btnNewRun.TabIndex = 0;
        btnNewRun.Text = "Новый проход";
        btnNewRun.Click += btnNewRun_Click;
        // 
        // btnOpenPlayWindow
        // 
        btnOpenPlayWindow.Location = new Point(84, 3);
        btnOpenPlayWindow.Name = "btnOpenPlayWindow";
        btnOpenPlayWindow.Size = new Size(160, 23);
        btnOpenPlayWindow.TabIndex = 1;
        btnOpenPlayWindow.Text = "Открыть игру в окне";
        btnOpenPlayWindow.Click += btnOpenPlayWindow_Click;
        // 
        // btnSaveProgress
        // 
        btnSaveProgress.Location = new Point(250, 3);
        btnSaveProgress.Name = "btnSaveProgress";
        btnSaveProgress.Size = new Size(75, 23);
        btnSaveProgress.TabIndex = 2;
        btnSaveProgress.Text = "Сохранить";
        btnSaveProgress.Click += btnSaveProgress_Click;
        // 
        // btnLoadProgress
        // 
        btnLoadProgress.Location = new Point(331, 3);
        btnLoadProgress.Name = "btnLoadProgress";
        btnLoadProgress.Size = new Size(75, 23);
        btnLoadProgress.TabIndex = 3;
        btnLoadProgress.Text = "Загрузить";
        btnLoadProgress.Click += btnLoadProgress_Click;
        // 
        // btnDeleteSave
        // 
        btnDeleteSave.Location = new Point(412, 3);
        btnDeleteSave.Name = "btnDeleteSave";
        btnDeleteSave.Size = new Size(75, 23);
        btnDeleteSave.TabIndex = 4;
        btnDeleteSave.Text = "Удалить";
        btnDeleteSave.Click += btnDeleteSave_Click;
        // 
        // tabLogs
        // 
        tabLogs.Controls.Add(txtLog);
        tabLogs.Location = new Point(4, 24);
        tabLogs.Name = "tabLogs";
        tabLogs.Size = new Size(1266, 762);
        tabLogs.TabIndex = 7;
        tabLogs.Text = "Логи";
        // 
        // txtLog
        // 
        txtLog.Dock = DockStyle.Fill;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.Location = new Point(0, 0);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Both;
        txtLog.Size = new Size(1266, 762);
        txtLog.TabIndex = 0;
        // 
        // tabSettings
        // 
        tabSettings.Controls.Add(settingsLayout);
        tabSettings.Location = new Point(4, 24);
        tabSettings.Name = "tabSettings";
        tabSettings.Size = new Size(1266, 762);
        tabSettings.TabIndex = 8;
        tabSettings.Text = "Настройки";
        // 
        // settingsLayout
        // 
        settingsLayout.ColumnCount = 4;
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        settingsLayout.Controls.Add(lblLmProfile, 0, 0);
        settingsLayout.Controls.Add(cmbLmProfiles, 1, 0);
        settingsLayout.Controls.Add(btnAddLmProfile, 2, 0);
        settingsLayout.Controls.Add(btnSaveLmProfile, 3, 0);
        settingsLayout.Controls.Add(lblLmProfileName, 0, 1);
        settingsLayout.Controls.Add(txtLmProfileName, 1, 1);
        settingsLayout.Controls.Add(lblLmProfileRole, 2, 1);
        settingsLayout.Controls.Add(cmbLmProfileRole, 3, 1);
        settingsLayout.Controls.Add(lblAutoSelectLmProfile, 0, 2);
        settingsLayout.Controls.Add(chkAutoSelectLmProfile, 1, 2);
        settingsLayout.Controls.Add(btnDeleteLmProfile, 2, 2);
        settingsLayout.Controls.Add(btnSetDefaultLmProfile, 3, 2);
        settingsLayout.Controls.Add(lblEndpoint, 0, 3);
        settingsLayout.Controls.Add(txtEndpoint, 1, 3);
        settingsLayout.Controls.Add(btnTestLm, 2, 3);
        settingsLayout.Controls.Add(lblApiKey, 0, 4);
        settingsLayout.Controls.Add(txtApiKey, 1, 4);
        settingsLayout.Controls.Add(lblModel, 0, 5);
        settingsLayout.Controls.Add(txtModel, 1, 5);
        settingsLayout.Controls.Add(lblTimeout, 0, 6);
        settingsLayout.Controls.Add(nudTimeout, 1, 6);
        settingsLayout.Controls.Add(lblMaxInputContextTokens, 0, 7);
        settingsLayout.Controls.Add(nudMaxInputContextTokens, 1, 7);
        settingsLayout.Controls.Add(lblMaxOutputTokens, 0, 8);
        settingsLayout.Controls.Add(nudMaxOutputTokens, 1, 8);
        settingsLayout.Controls.Add(lblLmUnloadUrl, 0, 9);
        settingsLayout.Controls.Add(txtLmUnloadUrl, 1, 9);
        settingsLayout.Controls.Add(lblLmUnloadCommand, 0, 10);
        settingsLayout.Controls.Add(txtLmUnloadCommand, 1, 10);
        settingsLayout.Controls.Add(lblLmUnloadTimeout, 0, 11);
        settingsLayout.Controls.Add(nudLmUnloadTimeout, 1, 11);
        settingsLayout.Controls.Add(lblContinueIfUnloadFails, 0, 12);
        settingsLayout.Controls.Add(chkContinueIfUnloadFails, 1, 12);
        settingsLayout.Controls.Add(lblFooocusLaunch, 0, 13);
        settingsLayout.Controls.Add(txtFooocusLaunch, 1, 13);
        settingsLayout.Controls.Add(btnBrowseFooocusLaunch, 2, 13);
        settingsLayout.Controls.Add(btnBrowseFooocusFolder, 3, 13);
        settingsLayout.Controls.Add(lblFooocusWorkingDir, 0, 14);
        settingsLayout.Controls.Add(txtFooocusWorkingDir, 1, 14);
        settingsLayout.Controls.Add(btnDetectFooocus, 2, 14);
        settingsLayout.Controls.Add(lblFooocusOutput, 0, 15);
        settingsLayout.Controls.Add(txtFooocusOutput, 1, 15);
        settingsLayout.Controls.Add(btnBrowseFooocusOutput, 2, 15);
        settingsLayout.Controls.Add(btnCheckFooocusPaths, 3, 15);
        settingsLayout.Controls.Add(lblFooocusEndpoint, 0, 16);
        settingsLayout.Controls.Add(txtFooocusEndpoint, 1, 16);
        settingsLayout.Controls.Add(lblFooocusStartup, 0, 17);
        settingsLayout.Controls.Add(nudFooocusStartup, 1, 17);
        settingsLayout.Controls.Add(lblFooocusShutdown, 0, 18);
        settingsLayout.Controls.Add(nudFooocusShutdown, 1, 18);
        settingsLayout.Controls.Add(lblSettings, 0, 19);
        settingsLayout.Controls.Add(btnSaveSettings, 1, 19);
        settingsLayout.Dock = DockStyle.Top;
        settingsLayout.Location = new Point(0, 0);
        settingsLayout.Name = "settingsLayout";
        settingsLayout.Padding = new Padding(8);
        settingsLayout.RowCount = 21;
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        settingsLayout.Size = new Size(1266, 722);
        settingsLayout.TabIndex = 0;
        // 
        // lblLmProfile
        // 
        lblLmProfile.Dock = DockStyle.Fill;
        lblLmProfile.Location = new Point(11, 8);
        lblLmProfile.Name = "lblLmProfile";
        lblLmProfile.Size = new Size(164, 34);
        lblLmProfile.TabIndex = 0;
        lblLmProfile.Text = "LM профиль:";
        lblLmProfile.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbLmProfiles
        // 
        cmbLmProfiles.Dock = DockStyle.Fill;
        cmbLmProfiles.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLmProfiles.FormattingEnabled = true;
        cmbLmProfiles.Location = new Point(181, 11);
        cmbLmProfiles.Name = "cmbLmProfiles";
        cmbLmProfiles.Size = new Size(834, 23);
        cmbLmProfiles.TabIndex = 1;
        cmbLmProfiles.SelectedIndexChanged += cmbLmProfiles_SelectedIndexChanged;
        cmbLmProfiles.Format += cmbLmProfiles_Format;
        // 
        // btnAddLmProfile
        // 
        btnAddLmProfile.Dock = DockStyle.Fill;
        btnAddLmProfile.Location = new Point(1021, 11);
        btnAddLmProfile.Name = "btnAddLmProfile";
        btnAddLmProfile.Size = new Size(114, 28);
        btnAddLmProfile.TabIndex = 2;
        btnAddLmProfile.Text = "Добавить";
        btnAddLmProfile.Click += btnAddLmProfile_Click;
        // 
        // btnSaveLmProfile
        // 
        btnSaveLmProfile.Dock = DockStyle.Fill;
        btnSaveLmProfile.Location = new Point(1141, 11);
        btnSaveLmProfile.Name = "btnSaveLmProfile";
        btnSaveLmProfile.Size = new Size(114, 28);
        btnSaveLmProfile.TabIndex = 3;
        btnSaveLmProfile.Text = "Сохранить";
        btnSaveLmProfile.Click += btnSaveLmProfile_Click;
        // 
        // lblLmProfileName
        // 
        lblLmProfileName.Dock = DockStyle.Fill;
        lblLmProfileName.Location = new Point(11, 42);
        lblLmProfileName.Name = "lblLmProfileName";
        lblLmProfileName.Size = new Size(164, 34);
        lblLmProfileName.TabIndex = 4;
        lblLmProfileName.Text = "Имя профиля:";
        lblLmProfileName.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtLmProfileName
        // 
        txtLmProfileName.Dock = DockStyle.Fill;
        txtLmProfileName.Location = new Point(181, 45);
        txtLmProfileName.Name = "txtLmProfileName";
        txtLmProfileName.Size = new Size(834, 23);
        txtLmProfileName.TabIndex = 5;
        // 
        // lblLmProfileRole
        // 
        lblLmProfileRole.Dock = DockStyle.Fill;
        lblLmProfileRole.Location = new Point(1021, 42);
        lblLmProfileRole.Name = "lblLmProfileRole";
        lblLmProfileRole.Size = new Size(114, 34);
        lblLmProfileRole.TabIndex = 6;
        lblLmProfileRole.Text = "Роль:";
        lblLmProfileRole.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbLmProfileRole
        // 
        cmbLmProfileRole.Dock = DockStyle.Fill;
        cmbLmProfileRole.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLmProfileRole.FormattingEnabled = true;
        cmbLmProfileRole.Location = new Point(1141, 45);
        cmbLmProfileRole.Name = "cmbLmProfileRole";
        cmbLmProfileRole.Size = new Size(114, 23);
        cmbLmProfileRole.TabIndex = 7;
        // 
        // lblAutoSelectLmProfile
        // 
        lblAutoSelectLmProfile.Dock = DockStyle.Fill;
        lblAutoSelectLmProfile.Location = new Point(11, 76);
        lblAutoSelectLmProfile.Name = "lblAutoSelectLmProfile";
        lblAutoSelectLmProfile.Size = new Size(164, 34);
        lblAutoSelectLmProfile.TabIndex = 8;
        lblAutoSelectLmProfile.Text = "Автовыбор:";
        lblAutoSelectLmProfile.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // chkAutoSelectLmProfile
        // 
        chkAutoSelectLmProfile.Dock = DockStyle.Fill;
        chkAutoSelectLmProfile.Location = new Point(181, 79);
        chkAutoSelectLmProfile.Name = "chkAutoSelectLmProfile";
        chkAutoSelectLmProfile.Size = new Size(834, 28);
        chkAutoSelectLmProfile.TabIndex = 9;
        chkAutoSelectLmProfile.Text = "Выбирать профиль по задаче";
        // 
        // btnDeleteLmProfile
        // 
        btnDeleteLmProfile.Dock = DockStyle.Fill;
        btnDeleteLmProfile.Location = new Point(1021, 79);
        btnDeleteLmProfile.Name = "btnDeleteLmProfile";
        btnDeleteLmProfile.Size = new Size(114, 28);
        btnDeleteLmProfile.TabIndex = 10;
        btnDeleteLmProfile.Text = "Удалить";
        btnDeleteLmProfile.Click += btnDeleteLmProfile_Click;
        // 
        // btnSetDefaultLmProfile
        // 
        btnSetDefaultLmProfile.Dock = DockStyle.Fill;
        btnSetDefaultLmProfile.Location = new Point(1141, 79);
        btnSetDefaultLmProfile.Name = "btnSetDefaultLmProfile";
        btnSetDefaultLmProfile.Size = new Size(114, 28);
        btnSetDefaultLmProfile.TabIndex = 11;
        btnSetDefaultLmProfile.Text = "Default";
        btnSetDefaultLmProfile.Click += btnSetDefaultLmProfile_Click;
        // 
        // lblEndpoint
        // 
        lblEndpoint.Dock = DockStyle.Fill;
        lblEndpoint.Location = new Point(11, 110);
        lblEndpoint.Name = "lblEndpoint";
        lblEndpoint.Size = new Size(164, 34);
        lblEndpoint.TabIndex = 0;
        lblEndpoint.Text = "LM endpoint:";
        lblEndpoint.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtEndpoint
        // 
        txtEndpoint.Dock = DockStyle.Fill;
        txtEndpoint.Location = new Point(181, 113);
        txtEndpoint.Name = "txtEndpoint";
        txtEndpoint.Size = new Size(834, 23);
        txtEndpoint.TabIndex = 1;
        txtEndpoint.Text = "http://127.0.0.1:1234/v1";
        // 
        // btnTestLm
        // 
        btnTestLm.Dock = DockStyle.Fill;
        btnTestLm.Location = new Point(1021, 113);
        btnTestLm.Name = "btnTestLm";
        btnTestLm.Size = new Size(114, 28);
        btnTestLm.TabIndex = 2;
        btnTestLm.Text = "Тест";
        btnTestLm.Click += btnTestLm_Click;
        // 
        // lblApiKey
        // 
        lblApiKey.Dock = DockStyle.Fill;
        lblApiKey.Location = new Point(11, 144);
        lblApiKey.Name = "lblApiKey";
        lblApiKey.Size = new Size(164, 34);
        lblApiKey.TabIndex = 3;
        lblApiKey.Text = "LM API key:";
        lblApiKey.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtApiKey
        // 
        txtApiKey.Dock = DockStyle.Fill;
        txtApiKey.Location = new Point(181, 147);
        txtApiKey.Name = "txtApiKey";
        txtApiKey.Size = new Size(834, 23);
        txtApiKey.TabIndex = 4;
        txtApiKey.Text = "lm-studio";
        // 
        // lblModel
        // 
        lblModel.Dock = DockStyle.Fill;
        lblModel.Location = new Point(11, 178);
        lblModel.Name = "lblModel";
        lblModel.Size = new Size(164, 34);
        lblModel.TabIndex = 5;
        lblModel.Text = "LM model id:";
        lblModel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtModel
        // 
        txtModel.Dock = DockStyle.Fill;
        txtModel.Location = new Point(181, 181);
        txtModel.Name = "txtModel";
        txtModel.Size = new Size(834, 23);
        txtModel.TabIndex = 6;
        // 
        // lblTimeout
        // 
        lblTimeout.Dock = DockStyle.Fill;
        lblTimeout.Location = new Point(11, 212);
        lblTimeout.Name = "lblTimeout";
        lblTimeout.Size = new Size(164, 34);
        lblTimeout.TabIndex = 7;
        lblTimeout.Text = "LM timeout sec (0 = wait):";
        lblTimeout.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nudTimeout
        // 
        nudTimeout.Dock = DockStyle.Fill;
        nudTimeout.Location = new Point(181, 215);
        nudTimeout.Maximum = new decimal(new int[] { 1800, 0, 0, 0 });
        nudTimeout.Name = "nudTimeout";
        nudTimeout.Size = new Size(834, 23);
        nudTimeout.TabIndex = 8;
        // 
        // lblMaxInputContextTokens
        // 
        lblMaxInputContextTokens.Dock = DockStyle.Fill;
        lblMaxInputContextTokens.Location = new Point(11, 246);
        lblMaxInputContextTokens.Name = "lblMaxInputContextTokens";
        lblMaxInputContextTokens.Size = new Size(164, 34);
        lblMaxInputContextTokens.TabIndex = 9;
        lblMaxInputContextTokens.Text = "Входной контекст, токены:";
        lblMaxInputContextTokens.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nudMaxInputContextTokens
        // 
        nudMaxInputContextTokens.Dock = DockStyle.Fill;
        nudMaxInputContextTokens.Increment = new decimal(new int[] { 1024, 0, 0, 0 });
        nudMaxInputContextTokens.Location = new Point(181, 249);
        nudMaxInputContextTokens.Maximum = new decimal(new int[] { 131072, 0, 0, 0 });
        nudMaxInputContextTokens.Minimum = new decimal(new int[] { 4096, 0, 0, 0 });
        nudMaxInputContextTokens.Name = "nudMaxInputContextTokens";
        nudMaxInputContextTokens.Size = new Size(834, 23);
        nudMaxInputContextTokens.TabIndex = 10;
        nudMaxInputContextTokens.Value = new decimal(new int[] { 32768, 0, 0, 0 });
        // 
        // lblMaxOutputTokens
        // 
        lblMaxOutputTokens.Dock = DockStyle.Fill;
        lblMaxOutputTokens.Location = new Point(11, 280);
        lblMaxOutputTokens.Name = "lblMaxOutputTokens";
        lblMaxOutputTokens.Size = new Size(164, 34);
        lblMaxOutputTokens.TabIndex = 11;
        lblMaxOutputTokens.Text = "Ответ LM, токены:";
        lblMaxOutputTokens.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nudMaxOutputTokens
        // 
        nudMaxOutputTokens.Dock = DockStyle.Fill;
        nudMaxOutputTokens.Increment = new decimal(new int[] { 512, 0, 0, 0 });
        nudMaxOutputTokens.Location = new Point(181, 283);
        nudMaxOutputTokens.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
        nudMaxOutputTokens.Minimum = new decimal(new int[] { 512, 0, 0, 0 });
        nudMaxOutputTokens.Name = "nudMaxOutputTokens";
        nudMaxOutputTokens.Size = new Size(834, 23);
        nudMaxOutputTokens.TabIndex = 12;
        nudMaxOutputTokens.Value = new decimal(new int[] { 4096, 0, 0, 0 });
        // 
        // lblLmUnloadUrl
        // 
        lblLmUnloadUrl.Dock = DockStyle.Fill;
        lblLmUnloadUrl.Location = new Point(11, 314);
        lblLmUnloadUrl.Name = "lblLmUnloadUrl";
        lblLmUnloadUrl.Size = new Size(164, 34);
        lblLmUnloadUrl.TabIndex = 9;
        lblLmUnloadUrl.Text = "LM unload URL:";
        lblLmUnloadUrl.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtLmUnloadUrl
        // 
        txtLmUnloadUrl.Dock = DockStyle.Fill;
        txtLmUnloadUrl.Location = new Point(181, 317);
        txtLmUnloadUrl.Name = "txtLmUnloadUrl";
        txtLmUnloadUrl.Size = new Size(834, 23);
        txtLmUnloadUrl.TabIndex = 10;
        // 
        // lblLmUnloadCommand
        // 
        lblLmUnloadCommand.Dock = DockStyle.Fill;
        lblLmUnloadCommand.Location = new Point(11, 348);
        lblLmUnloadCommand.Name = "lblLmUnloadCommand";
        lblLmUnloadCommand.Size = new Size(164, 34);
        lblLmUnloadCommand.TabIndex = 11;
        lblLmUnloadCommand.Text = "LM unload command:";
        lblLmUnloadCommand.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtLmUnloadCommand
        // 
        txtLmUnloadCommand.Dock = DockStyle.Fill;
        txtLmUnloadCommand.Location = new Point(181, 351);
        txtLmUnloadCommand.Name = "txtLmUnloadCommand";
        txtLmUnloadCommand.Size = new Size(834, 23);
        txtLmUnloadCommand.TabIndex = 12;
        // 
        // lblLmUnloadTimeout
        // 
        lblLmUnloadTimeout.Dock = DockStyle.Fill;
        lblLmUnloadTimeout.Location = new Point(11, 382);
        lblLmUnloadTimeout.Name = "lblLmUnloadTimeout";
        lblLmUnloadTimeout.Size = new Size(164, 34);
        lblLmUnloadTimeout.TabIndex = 13;
        lblLmUnloadTimeout.Text = "Unload timeout sec:";
        lblLmUnloadTimeout.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nudLmUnloadTimeout
        // 
        nudLmUnloadTimeout.Dock = DockStyle.Fill;
        nudLmUnloadTimeout.Location = new Point(181, 385);
        nudLmUnloadTimeout.Maximum = new decimal(new int[] { 1800, 0, 0, 0 });
        nudLmUnloadTimeout.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudLmUnloadTimeout.Name = "nudLmUnloadTimeout";
        nudLmUnloadTimeout.Size = new Size(834, 23);
        nudLmUnloadTimeout.TabIndex = 14;
        nudLmUnloadTimeout.Value = new decimal(new int[] { 60, 0, 0, 0 });
        // 
        // lblContinueIfUnloadFails
        // 
        lblContinueIfUnloadFails.Dock = DockStyle.Fill;
        lblContinueIfUnloadFails.Location = new Point(11, 416);
        lblContinueIfUnloadFails.Name = "lblContinueIfUnloadFails";
        lblContinueIfUnloadFails.Size = new Size(164, 34);
        lblContinueIfUnloadFails.TabIndex = 15;
        lblContinueIfUnloadFails.Text = "Continue if unload fails:";
        lblContinueIfUnloadFails.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // chkContinueIfUnloadFails
        // 
        chkContinueIfUnloadFails.Checked = true;
        chkContinueIfUnloadFails.CheckState = CheckState.Checked;
        chkContinueIfUnloadFails.Dock = DockStyle.Fill;
        chkContinueIfUnloadFails.Location = new Point(181, 419);
        chkContinueIfUnloadFails.Name = "chkContinueIfUnloadFails";
        chkContinueIfUnloadFails.Size = new Size(834, 28);
        chkContinueIfUnloadFails.TabIndex = 16;
        chkContinueIfUnloadFails.Text = "Да";
        // 
        // lblFooocusLaunch
        // 
        lblFooocusLaunch.Dock = DockStyle.Fill;
        lblFooocusLaunch.Location = new Point(11, 450);
        lblFooocusLaunch.Name = "lblFooocusLaunch";
        lblFooocusLaunch.Size = new Size(164, 34);
        lblFooocusLaunch.TabIndex = 17;
        lblFooocusLaunch.Text = "Fooocus launch:";
        lblFooocusLaunch.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtFooocusLaunch
        // 
        txtFooocusLaunch.Dock = DockStyle.Fill;
        txtFooocusLaunch.Location = new Point(181, 453);
        txtFooocusLaunch.Name = "txtFooocusLaunch";
        txtFooocusLaunch.Size = new Size(834, 23);
        txtFooocusLaunch.TabIndex = 18;
        // 
        // btnBrowseFooocusLaunch
        // 
        btnBrowseFooocusLaunch.Dock = DockStyle.Fill;
        btnBrowseFooocusLaunch.Location = new Point(1021, 453);
        btnBrowseFooocusLaunch.Name = "btnBrowseFooocusLaunch";
        btnBrowseFooocusLaunch.Size = new Size(114, 28);
        btnBrowseFooocusLaunch.TabIndex = 19;
        btnBrowseFooocusLaunch.Text = "Выбрать";
        btnBrowseFooocusLaunch.Click += btnBrowseFooocusLaunch_Click;
        // 
        // btnBrowseFooocusFolder
        // 
        btnBrowseFooocusFolder.Dock = DockStyle.Fill;
        btnBrowseFooocusFolder.Location = new Point(1141, 453);
        btnBrowseFooocusFolder.Name = "btnBrowseFooocusFolder";
        btnBrowseFooocusFolder.Size = new Size(114, 28);
        btnBrowseFooocusFolder.TabIndex = 20;
        btnBrowseFooocusFolder.Text = "Папка";
        btnBrowseFooocusFolder.Click += btnBrowseFooocusFolder_Click;
        // 
        // lblFooocusWorkingDir
        // 
        lblFooocusWorkingDir.Dock = DockStyle.Fill;
        lblFooocusWorkingDir.Location = new Point(11, 484);
        lblFooocusWorkingDir.Name = "lblFooocusWorkingDir";
        lblFooocusWorkingDir.Size = new Size(164, 34);
        lblFooocusWorkingDir.TabIndex = 21;
        lblFooocusWorkingDir.Text = "Fooocus working dir:";
        lblFooocusWorkingDir.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtFooocusWorkingDir
        // 
        txtFooocusWorkingDir.Dock = DockStyle.Fill;
        txtFooocusWorkingDir.Location = new Point(181, 487);
        txtFooocusWorkingDir.Name = "txtFooocusWorkingDir";
        txtFooocusWorkingDir.Size = new Size(834, 23);
        txtFooocusWorkingDir.TabIndex = 22;
        // 
        // btnDetectFooocus
        // 
        btnDetectFooocus.Dock = DockStyle.Fill;
        btnDetectFooocus.Location = new Point(1021, 487);
        btnDetectFooocus.Name = "btnDetectFooocus";
        btnDetectFooocus.Size = new Size(114, 28);
        btnDetectFooocus.TabIndex = 23;
        btnDetectFooocus.Text = "Определить";
        btnDetectFooocus.Click += btnDetectFooocus_Click;
        // 
        // lblFooocusOutput
        // 
        lblFooocusOutput.Dock = DockStyle.Fill;
        lblFooocusOutput.Location = new Point(11, 518);
        lblFooocusOutput.Name = "lblFooocusOutput";
        lblFooocusOutput.Size = new Size(164, 34);
        lblFooocusOutput.TabIndex = 24;
        lblFooocusOutput.Text = "Fooocus output:";
        lblFooocusOutput.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtFooocusOutput
        // 
        txtFooocusOutput.Dock = DockStyle.Fill;
        txtFooocusOutput.Location = new Point(181, 521);
        txtFooocusOutput.Name = "txtFooocusOutput";
        txtFooocusOutput.Size = new Size(834, 23);
        txtFooocusOutput.TabIndex = 25;
        // 
        // btnBrowseFooocusOutput
        // 
        btnBrowseFooocusOutput.Dock = DockStyle.Fill;
        btnBrowseFooocusOutput.Location = new Point(1021, 521);
        btnBrowseFooocusOutput.Name = "btnBrowseFooocusOutput";
        btnBrowseFooocusOutput.Size = new Size(114, 28);
        btnBrowseFooocusOutput.TabIndex = 26;
        btnBrowseFooocusOutput.Text = "Выбрать";
        btnBrowseFooocusOutput.Click += btnBrowseFooocusOutput_Click;
        // 
        // btnCheckFooocusPaths
        // 
        btnCheckFooocusPaths.Dock = DockStyle.Fill;
        btnCheckFooocusPaths.Location = new Point(1141, 521);
        btnCheckFooocusPaths.Name = "btnCheckFooocusPaths";
        btnCheckFooocusPaths.Size = new Size(114, 28);
        btnCheckFooocusPaths.TabIndex = 27;
        btnCheckFooocusPaths.Text = "Проверить";
        btnCheckFooocusPaths.Click += btnCheckFooocusPaths_Click;
        // 
        // lblFooocusEndpoint
        // 
        lblFooocusEndpoint.Dock = DockStyle.Fill;
        lblFooocusEndpoint.Location = new Point(11, 552);
        lblFooocusEndpoint.Name = "lblFooocusEndpoint";
        lblFooocusEndpoint.Size = new Size(164, 34);
        lblFooocusEndpoint.TabIndex = 28;
        lblFooocusEndpoint.Text = "Fooocus endpoint:";
        lblFooocusEndpoint.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtFooocusEndpoint
        // 
        txtFooocusEndpoint.Dock = DockStyle.Fill;
        txtFooocusEndpoint.Location = new Point(181, 555);
        txtFooocusEndpoint.Name = "txtFooocusEndpoint";
        txtFooocusEndpoint.Size = new Size(834, 23);
        txtFooocusEndpoint.TabIndex = 29;
        // 
        // lblFooocusStartup
        // 
        lblFooocusStartup.Dock = DockStyle.Fill;
        lblFooocusStartup.Location = new Point(11, 586);
        lblFooocusStartup.Name = "lblFooocusStartup";
        lblFooocusStartup.Size = new Size(164, 34);
        lblFooocusStartup.TabIndex = 30;
        lblFooocusStartup.Text = "Fooocus startup sec:";
        lblFooocusStartup.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nudFooocusStartup
        // 
        nudFooocusStartup.Dock = DockStyle.Fill;
        nudFooocusStartup.Location = new Point(181, 589);
        nudFooocusStartup.Maximum = new decimal(new int[] { 1800, 0, 0, 0 });
        nudFooocusStartup.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudFooocusStartup.Name = "nudFooocusStartup";
        nudFooocusStartup.Size = new Size(834, 23);
        nudFooocusStartup.TabIndex = 31;
        nudFooocusStartup.Value = new decimal(new int[] { 180, 0, 0, 0 });
        // 
        // lblFooocusShutdown
        // 
        lblFooocusShutdown.Dock = DockStyle.Fill;
        lblFooocusShutdown.Location = new Point(11, 620);
        lblFooocusShutdown.Name = "lblFooocusShutdown";
        lblFooocusShutdown.Size = new Size(164, 34);
        lblFooocusShutdown.TabIndex = 32;
        lblFooocusShutdown.Text = "Fooocus shutdown sec:";
        lblFooocusShutdown.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nudFooocusShutdown
        // 
        nudFooocusShutdown.Dock = DockStyle.Fill;
        nudFooocusShutdown.Location = new Point(181, 623);
        nudFooocusShutdown.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        nudFooocusShutdown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        nudFooocusShutdown.Name = "nudFooocusShutdown";
        nudFooocusShutdown.Size = new Size(834, 23);
        nudFooocusShutdown.TabIndex = 33;
        nudFooocusShutdown.Value = new decimal(new int[] { 30, 0, 0, 0 });
        // 
        // lblSettings
        // 
        lblSettings.Dock = DockStyle.Fill;
        lblSettings.Location = new Point(11, 654);
        lblSettings.Name = "lblSettings";
        lblSettings.Size = new Size(164, 34);
        lblSettings.TabIndex = 34;
        lblSettings.Text = "Settings:";
        lblSettings.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnSaveSettings
        // 
        btnSaveSettings.Dock = DockStyle.Fill;
        btnSaveSettings.Location = new Point(181, 657);
        btnSaveSettings.Name = "btnSaveSettings";
        btnSaveSettings.Size = new Size(834, 28);
        btnSaveSettings.TabIndex = 35;
        btnSaveSettings.Text = "Сохранить настройки";
        btnSaveSettings.Click += btnSaveSettings_Click;
        // 
        // assetsPromptButtons
        // 
        assetsPromptButtons.Location = new Point(0, 0);
        assetsPromptButtons.Name = "assetsPromptButtons";
        assetsPromptButtons.Size = new Size(200, 100);
        assetsPromptButtons.TabIndex = 0;
        // 
        // lblValidationResult
        // 
        lblValidationResult.Location = new Point(0, 0);
        lblValidationResult.Name = "lblValidationResult";
        lblValidationResult.Size = new Size(100, 23);
        lblValidationResult.TabIndex = 0;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1280, 860);
        Controls.Add(rootLayout);
        MinimumSize = new Size(1100, 760);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "AI Game Builder";
        rootLayout.ResumeLayout(false);
        topLayout.ResumeLayout(false);
        topLayout.PerformLayout();
        tabMain.ResumeLayout(false);
        tabProjects.ResumeLayout(false);
        projectsSplit.Panel1.ResumeLayout(false);
        projectsSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)projectsSplit).EndInit();
        projectsSplit.ResumeLayout(false);
        projectButtonsLayout.ResumeLayout(false);
        tabDiscussion.ResumeLayout(false);
        discussionLayout.ResumeLayout(false);
        discussionLayout.PerformLayout();
        discussionButtons.ResumeLayout(false);
        tabGameCrafter.ResumeLayout(false);
        gameCrafterLayout.ResumeLayout(false);
        gameCrafterTopLayout.ResumeLayout(false);
        gameCrafterTopLayout.PerformLayout();
        gameCrafterButtons.ResumeLayout(false);
        gameCrafterButtons.PerformLayout();
        gameCrafterSplit.Panel1.ResumeLayout(false);
        gameCrafterSplit.Panel2.ResumeLayout(false);
        gameCrafterSplit.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)gameCrafterSplit).EndInit();
        gameCrafterSplit.ResumeLayout(false);
        gameCrafterLeftLayout.ResumeLayout(false);
        gameCrafterLeftLayout.PerformLayout();
        tabBrief.ResumeLayout(false);
        briefLayout.ResumeLayout(false);
        briefLayout.PerformLayout();
        briefButtons.ResumeLayout(false);
        tabContent.ResumeLayout(false);
        tabContentInner.ResumeLayout(false);
        tabWorld.ResumeLayout(false);
        tabWorld.PerformLayout();
        tabCharacters.ResumeLayout(false);
        tabScenes.ResumeLayout(false);
        tabItems.ResumeLayout(false);
        tabStats.ResumeLayout(false);
        tabRelationships.ResumeLayout(false);
        tabCombat.ResumeLayout(false);
        tabCombat.PerformLayout();
        tabPipeline.ResumeLayout(false);
        pipelineLayout.ResumeLayout(false);
        pipelineSplit.Panel1.ResumeLayout(false);
        pipelineSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)pipelineSplit).EndInit();
        pipelineSplit.ResumeLayout(false);
        pipelineControlsLayout.ResumeLayout(false);
        pipelineControlsLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudPipelineBatchCount).EndInit();
        pipelineButtons.ResumeLayout(false);
        tabAssets.ResumeLayout(false);
        assetsLayout.ResumeLayout(false);
        assetsLayout.PerformLayout();
        assetsBottomLayout.ResumeLayout(false);
        assetsBottomLayout.PerformLayout();
        assetsBatchOptions.ResumeLayout(false);
        assetsBatchOptions.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudBatchCount).EndInit();
        assetsBatchButtons.ResumeLayout(false);
        assetsButtons.ResumeLayout(false);
        tabPlay.ResumeLayout(false);
        playLayout.ResumeLayout(false);
        playLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)picScene).EndInit();
        tabRuntimeInfo.ResumeLayout(false);
        tabRuntimeStatsPage.ResumeLayout(false);
        tabRuntimeInventoryPage.ResumeLayout(false);
        tabRuntimeRelationshipsPage.ResumeLayout(false);
        tabRuntimeQuestsPage.ResumeLayout(false);
        tabRuntimeLogPage.ResumeLayout(false);
        tabRuntimeLogPage.PerformLayout();
        tabSaves.ResumeLayout(false);
        savesLayout.ResumeLayout(false);
        savesButtons.ResumeLayout(false);
        tabLogs.ResumeLayout(false);
        tabLogs.PerformLayout();
        tabSettings.ResumeLayout(false);
        settingsLayout.ResumeLayout(false);
        settingsLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nudTimeout).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxInputContextTokens).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudMaxOutputTokens).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudLmUnloadTimeout).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudFooocusStartup).EndInit();
        ((System.ComponentModel.ISupportInitialize)nudFooocusShutdown).EndInit();
        ResumeLayout(false);
    }

}
