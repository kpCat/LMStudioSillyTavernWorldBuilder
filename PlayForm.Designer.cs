using System;
using System.Drawing;
using System.Windows.Forms;

#nullable enable

namespace LMStudioSillyTavernWorldBuilder;

internal partial class PlayForm
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel rootLayout = null!;
    private TableLayoutPanel headerLayout = null!;
    private Label lblTitle = null!;
    private Label lblStatus = null!;
    private FlowLayoutPanel toolbar = null!;
    private Button btnSave = null!;
    private Button btnLoad = null!;
    private Button btnInventory = null!;
    private Button btnCharacter = null!;
    private Button btnMap = null!;
    private Button btnEndTurn = null!;
    private Button btnClosePlay = null!;
    private SplitContainer mainSplit = null!;
    private TableLayoutPanel sceneLayout = null!;
    private Label lblSceneTitle = null!;
    private TextBox txtWorldStateSummary = null!;
    private PictureBox picScene = null!;
    private TextBox txtSceneText = null!;
    private FlowLayoutPanel pnlChoices = null!;
    private TabControl tabInfo = null!;
    private TabPage tabStats = null!;
    private TabPage tabCurrencies = null!;
    private TabPage tabInventory = null!;
    private TabPage tabEquipment = null!;
    private TabPage tabSkills = null!;
    private TabPage tabRelationships = null!;
    private TabPage tabQuests = null!;
    private TabPage tabMap = null!;
    private TabPage tabActions = null!;
    private TabPage tabCombat = null!;
    private TabPage tabEffects = null!;
    private TabPage tabProgression = null!;
    private TabPage tabLog = null!;
    private ListView lvStats = null!;
    private ListView lvCurrencies = null!;
    private ListView lvInventory = null!;
    private ListView lvEquipment = null!;
    private ListView lvSkills = null!;
    private ListView lvRelationships = null!;
    private ListView lvQuests = null!;
    private ListView lvMap = null!;
    private ListView lvActions = null!;
    private ListView lvCombatants = null!;
    private ListView lvCombatActions = null!;
    private ListView lvEffects = null!;
    private ListView lvProgression = null!;
    private TableLayoutPanel inventoryLayout = null!;
    private FlowLayoutPanel inventoryButtons = null!;
    private Button btnUseInventoryItem = null!;
    private Label lblInventoryHint = null!;
    private TableLayoutPanel skillsLayout = null!;
    private FlowLayoutPanel skillsButtons = null!;
    private Button btnUseSkill = null!;
    private Label lblSkillsHint = null!;
    private TableLayoutPanel mapLayout = null!;
    private FlowLayoutPanel mapButtons = null!;
    private Button btnTravelToLocation = null!;
    private Label lblMapHint = null!;
    private TableLayoutPanel actionsLayout = null!;
    private FlowLayoutPanel actionsButtons = null!;
    private Button btnExecuteAction = null!;
    private Button btnRefreshActions = null!;
    private TableLayoutPanel combatLayout = null!;
    private FlowLayoutPanel combatButtons = null!;
    private Button btnStartCombat = null!;
    private Button btnExecuteCombatAction = null!;
    private Button btnEndCombatTurn = null!;
    private Label lblCombatHint = null!;
    private SplitContainer combatSplit = null!;
    private TableLayoutPanel progressionLayout = null!;
    private FlowLayoutPanel progressionButtons = null!;
    private Button btnUnlockProgression = null!;
    private Button btnRefreshProgression = null!;
    private TextBox txtLog = null!;

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
        headerLayout = new TableLayoutPanel();
        lblTitle = new Label();
        lblStatus = new Label();
        toolbar = new FlowLayoutPanel();
        btnSave = new Button();
        btnLoad = new Button();
        btnInventory = new Button();
        btnCharacter = new Button();
        btnMap = new Button();
        btnEndTurn = new Button();
        btnClosePlay = new Button();
        mainSplit = new SplitContainer();
        sceneLayout = new TableLayoutPanel();
        lblSceneTitle = new Label();
        txtWorldStateSummary = new TextBox();
        picScene = new PictureBox();
        txtSceneText = new TextBox();
        pnlChoices = new FlowLayoutPanel();
        tabInfo = new TabControl();
        tabStats = new TabPage();
        lvStats = new ListView();
        tabCurrencies = new TabPage();
        lvCurrencies = new ListView();
        tabInventory = new TabPage();
        inventoryLayout = new TableLayoutPanel();
        inventoryButtons = new FlowLayoutPanel();
        btnUseInventoryItem = new Button();
        lblInventoryHint = new Label();
        lvInventory = new ListView();
        tabEquipment = new TabPage();
        lvEquipment = new ListView();
        tabSkills = new TabPage();
        skillsLayout = new TableLayoutPanel();
        skillsButtons = new FlowLayoutPanel();
        btnUseSkill = new Button();
        lblSkillsHint = new Label();
        lvSkills = new ListView();
        tabRelationships = new TabPage();
        lvRelationships = new ListView();
        tabQuests = new TabPage();
        lvQuests = new ListView();
        tabMap = new TabPage();
        mapLayout = new TableLayoutPanel();
        mapButtons = new FlowLayoutPanel();
        btnTravelToLocation = new Button();
        lblMapHint = new Label();
        lvMap = new ListView();
        tabActions = new TabPage();
        actionsLayout = new TableLayoutPanel();
        actionsButtons = new FlowLayoutPanel();
        btnExecuteAction = new Button();
        btnRefreshActions = new Button();
        lvActions = new ListView();
        tabCombat = new TabPage();
        combatLayout = new TableLayoutPanel();
        combatButtons = new FlowLayoutPanel();
        btnStartCombat = new Button();
        btnExecuteCombatAction = new Button();
        btnEndCombatTurn = new Button();
        lblCombatHint = new Label();
        combatSplit = new SplitContainer();
        lvCombatants = new ListView();
        lvCombatActions = new ListView();
        tabEffects = new TabPage();
        lvEffects = new ListView();
        tabProgression = new TabPage();
        progressionLayout = new TableLayoutPanel();
        progressionButtons = new FlowLayoutPanel();
        btnUnlockProgression = new Button();
        btnRefreshProgression = new Button();
        lvProgression = new ListView();
        tabLog = new TabPage();
        txtLog = new TextBox();
        rootLayout.SuspendLayout();
        headerLayout.SuspendLayout();
        toolbar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)mainSplit).BeginInit();
        mainSplit.Panel1.SuspendLayout();
        mainSplit.Panel2.SuspendLayout();
        mainSplit.SuspendLayout();
        sceneLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picScene).BeginInit();
        tabInfo.SuspendLayout();
        tabStats.SuspendLayout();
        tabCurrencies.SuspendLayout();
        tabInventory.SuspendLayout();
        inventoryLayout.SuspendLayout();
        inventoryButtons.SuspendLayout();
        tabEquipment.SuspendLayout();
        tabSkills.SuspendLayout();
        skillsLayout.SuspendLayout();
        skillsButtons.SuspendLayout();
        tabRelationships.SuspendLayout();
        tabQuests.SuspendLayout();
        tabMap.SuspendLayout();
        mapLayout.SuspendLayout();
        mapButtons.SuspendLayout();
        tabActions.SuspendLayout();
        actionsLayout.SuspendLayout();
        actionsButtons.SuspendLayout();
        tabCombat.SuspendLayout();
        combatLayout.SuspendLayout();
        combatButtons.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)combatSplit).BeginInit();
        combatSplit.Panel1.SuspendLayout();
        combatSplit.Panel2.SuspendLayout();
        combatSplit.SuspendLayout();
        tabEffects.SuspendLayout();
        tabProgression.SuspendLayout();
        progressionLayout.SuspendLayout();
        progressionButtons.SuspendLayout();
        tabLog.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(headerLayout, 0, 0);
        rootLayout.Controls.Add(toolbar, 0, 1);
        rootLayout.Controls.Add(mainSplit, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.Size = new Size(1200, 800);
        rootLayout.TabIndex = 0;
        // 
        // headerLayout
        // 
        headerLayout.ColumnCount = 2;
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        headerLayout.Controls.Add(lblTitle, 0, 0);
        headerLayout.Controls.Add(lblStatus, 1, 0);
        headerLayout.Dock = DockStyle.Fill;
        headerLayout.Location = new Point(3, 3);
        headerLayout.Name = "headerLayout";
        headerLayout.Padding = new Padding(10);
        headerLayout.RowCount = 1;
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        headerLayout.Size = new Size(1194, 52);
        headerLayout.TabIndex = 0;
        // 
        // lblTitle
        // 
        lblTitle.Dock = DockStyle.Fill;
        lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitle.Location = new Point(13, 10);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(698, 32);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Игра";
        lblTitle.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblStatus
        // 
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.Location = new Point(717, 10);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(464, 32);
        lblStatus.TabIndex = 1;
        lblStatus.Text = "Локация";
        lblStatus.TextAlign = ContentAlignment.MiddleRight;
        // 
        // toolbar
        // 
        toolbar.Controls.Add(btnSave);
        toolbar.Controls.Add(btnLoad);
        toolbar.Controls.Add(btnInventory);
        toolbar.Controls.Add(btnCharacter);
        toolbar.Controls.Add(btnMap);
        toolbar.Controls.Add(btnEndTurn);
        toolbar.Controls.Add(btnClosePlay);
        toolbar.Dock = DockStyle.Fill;
        toolbar.Location = new Point(3, 61);
        toolbar.Name = "toolbar";
        toolbar.Padding = new Padding(8, 5, 8, 5);
        toolbar.Size = new Size(1194, 36);
        toolbar.TabIndex = 1;
        // 
        // btnSave
        // 
        btnSave.Location = new Point(11, 8);
        btnSave.Name = "btnSave";
        btnSave.Size = new Size(90, 25);
        btnSave.TabIndex = 0;
        btnSave.Text = "Сохранить";
        btnSave.Click += btnSave_Click;
        // 
        // btnLoad
        // 
        btnLoad.Location = new Point(107, 8);
        btnLoad.Name = "btnLoad";
        btnLoad.Size = new Size(90, 25);
        btnLoad.TabIndex = 1;
        btnLoad.Text = "Загрузить";
        btnLoad.Click += btnLoad_Click;
        // 
        // btnInventory
        // 
        btnInventory.Location = new Point(203, 8);
        btnInventory.Name = "btnInventory";
        btnInventory.Size = new Size(90, 25);
        btnInventory.TabIndex = 2;
        btnInventory.Text = "Инвентарь";
        btnInventory.Click += btnInventory_Click;
        // 
        // btnCharacter
        // 
        btnCharacter.Location = new Point(299, 8);
        btnCharacter.Name = "btnCharacter";
        btnCharacter.Size = new Size(90, 25);
        btnCharacter.TabIndex = 3;
        btnCharacter.Text = "Персонаж";
        btnCharacter.Click += btnCharacter_Click;
        // 
        // btnMap
        // 
        btnMap.Location = new Point(395, 8);
        btnMap.Name = "btnMap";
        btnMap.Size = new Size(90, 25);
        btnMap.TabIndex = 4;
        btnMap.Text = "Карта";
        btnMap.Click += btnMap_Click;
        // 
        // btnEndTurn
        // 
        btnEndTurn.Location = new Point(491, 8);
        btnEndTurn.Name = "btnEndTurn";
        btnEndTurn.Size = new Size(100, 25);
        btnEndTurn.TabIndex = 5;
        btnEndTurn.Text = "Конец хода";
        btnEndTurn.Click += btnEndTurn_Click;
        // 
        // btnClosePlay
        // 
        btnClosePlay.Location = new Point(597, 8);
        btnClosePlay.Name = "btnClosePlay";
        btnClosePlay.Size = new Size(90, 25);
        btnClosePlay.TabIndex = 6;
        btnClosePlay.Text = "Закрыть";
        btnClosePlay.Click += btnClosePlay_Click;
        // 
        // mainSplit
        // 
        mainSplit.Dock = DockStyle.Fill;
        mainSplit.Location = new Point(3, 103);
        mainSplit.Name = "mainSplit";
        // 
        // mainSplit.Panel1
        // 
        mainSplit.Panel1.Controls.Add(sceneLayout);
        // 
        // mainSplit.Panel2
        // 
        mainSplit.Panel2.Controls.Add(tabInfo);
        mainSplit.Size = new Size(1194, 694);
        mainSplit.SplitterDistance = 760;
        mainSplit.TabIndex = 2;
        // 
        // sceneLayout
        // 
        sceneLayout.ColumnCount = 1;
        sceneLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sceneLayout.Controls.Add(lblSceneTitle, 0, 0);
        sceneLayout.Controls.Add(txtWorldStateSummary, 0, 1);
        sceneLayout.Controls.Add(picScene, 0, 2);
        sceneLayout.Controls.Add(txtSceneText, 0, 3);
        sceneLayout.Controls.Add(pnlChoices, 0, 4);
        sceneLayout.Dock = DockStyle.Fill;
        sceneLayout.Location = new Point(0, 0);
        sceneLayout.Name = "sceneLayout";
        sceneLayout.Padding = new Padding(8);
        sceneLayout.RowCount = 5;
        sceneLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        sceneLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        sceneLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
        sceneLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
        sceneLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 23F));
        sceneLayout.Size = new Size(760, 694);
        sceneLayout.TabIndex = 0;
        // 
        // lblSceneTitle
        // 
        lblSceneTitle.Dock = DockStyle.Fill;
        lblSceneTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblSceneTitle.Location = new Point(11, 8);
        lblSceneTitle.Name = "lblSceneTitle";
        lblSceneTitle.Size = new Size(738, 36);
        lblSceneTitle.TabIndex = 0;
        lblSceneTitle.Text = "Сцена";
        lblSceneTitle.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtWorldStateSummary
        // 
        txtWorldStateSummary.Dock = DockStyle.Fill;
        txtWorldStateSummary.Location = new Point(11, 47);
        txtWorldStateSummary.Name = "txtWorldStateSummary";
        txtWorldStateSummary.ReadOnly = true;
        txtWorldStateSummary.Size = new Size(738, 23);
        txtWorldStateSummary.TabIndex = 1;
        // 
        // picScene
        // 
        picScene.BackColor = Color.Black;
        picScene.Dock = DockStyle.Fill;
        picScene.Location = new Point(11, 47);
        picScene.Name = "picScene";
        picScene.Size = new Size(738, 278);
        picScene.SizeMode = PictureBoxSizeMode.Zoom;
        picScene.TabIndex = 1;
        picScene.TabStop = false;
        // 
        // txtSceneText
        // 
        txtSceneText.Dock = DockStyle.Fill;
        txtSceneText.Location = new Point(11, 331);
        txtSceneText.Multiline = true;
        txtSceneText.Name = "txtSceneText";
        txtSceneText.ReadOnly = true;
        txtSceneText.ScrollBars = ScrollBars.Vertical;
        txtSceneText.Size = new Size(738, 231);
        txtSceneText.TabIndex = 2;
        // 
        // pnlChoices
        // 
        pnlChoices.AutoScroll = true;
        pnlChoices.Dock = DockStyle.Fill;
        pnlChoices.FlowDirection = FlowDirection.TopDown;
        pnlChoices.Location = new Point(11, 568);
        pnlChoices.Name = "pnlChoices";
        pnlChoices.Size = new Size(738, 115);
        pnlChoices.TabIndex = 3;
        pnlChoices.WrapContents = false;
        // 
        // tabInfo
        // 
        tabInfo.Controls.Add(tabStats);
        tabInfo.Controls.Add(tabCurrencies);
        tabInfo.Controls.Add(tabInventory);
        tabInfo.Controls.Add(tabEquipment);
        tabInfo.Controls.Add(tabSkills);
        tabInfo.Controls.Add(tabRelationships);
        tabInfo.Controls.Add(tabQuests);
        tabInfo.Controls.Add(tabMap);
        tabInfo.Controls.Add(tabActions);
        tabInfo.Controls.Add(tabCombat);
        tabInfo.Controls.Add(tabEffects);
        tabInfo.Controls.Add(tabProgression);
        tabInfo.Controls.Add(tabLog);
        tabInfo.Dock = DockStyle.Fill;
        tabInfo.Location = new Point(0, 0);
        tabInfo.Name = "tabInfo";
        tabInfo.SelectedIndex = 0;
        tabInfo.Size = new Size(430, 694);
        tabInfo.TabIndex = 0;
        // 
        // tabStats
        // 
        tabStats.Controls.Add(lvStats);
        tabStats.Location = new Point(4, 24);
        tabStats.Name = "tabStats";
        tabStats.Size = new Size(422, 666);
        tabStats.TabIndex = 0;
        tabStats.Text = "Статы";
        // 
        // lvStats
        // 
        lvStats.Dock = DockStyle.Fill;
        lvStats.FullRowSelect = true;
        lvStats.Location = new Point(0, 0);
        lvStats.Name = "lvStats";
        lvStats.Size = new Size(422, 666);
        lvStats.TabIndex = 0;
        lvStats.UseCompatibleStateImageBehavior = false;
        lvStats.View = View.Details;
        lvStats.Columns.Add("Код", 120);
        lvStats.Columns.Add("Название", 140);
        lvStats.Columns.Add("Значение", 120);
        // 
        // tabCurrencies
        // 
        tabCurrencies.Controls.Add(lvCurrencies);
        tabCurrencies.Location = new Point(4, 24);
        tabCurrencies.Name = "tabCurrencies";
        tabCurrencies.Size = new Size(422, 666);
        tabCurrencies.TabIndex = 1;
        tabCurrencies.Text = "Валюты";
        // 
        // lvCurrencies
        // 
        lvCurrencies.Dock = DockStyle.Fill;
        lvCurrencies.FullRowSelect = true;
        lvCurrencies.Location = new Point(0, 0);
        lvCurrencies.Name = "lvCurrencies";
        lvCurrencies.Size = new Size(422, 666);
        lvCurrencies.TabIndex = 0;
        lvCurrencies.UseCompatibleStateImageBehavior = false;
        lvCurrencies.View = View.Details;
        lvCurrencies.Columns.Add("Код", 120);
        lvCurrencies.Columns.Add("Название", 140);
        lvCurrencies.Columns.Add("Значение", 120);
        // 
        // tabInventory
        // 
        tabInventory.Controls.Add(inventoryLayout);
        tabInventory.Location = new Point(4, 24);
        tabInventory.Name = "tabInventory";
        tabInventory.Size = new Size(422, 666);
        tabInventory.TabIndex = 2;
        tabInventory.Text = "Инвентарь";
        // 
        // inventoryLayout
        // 
        inventoryLayout.ColumnCount = 1;
        inventoryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        inventoryLayout.Controls.Add(inventoryButtons, 0, 0);
        inventoryLayout.Controls.Add(lvInventory, 0, 1);
        inventoryLayout.Dock = DockStyle.Fill;
        inventoryLayout.Location = new Point(0, 0);
        inventoryLayout.Name = "inventoryLayout";
        inventoryLayout.RowCount = 2;
        inventoryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        inventoryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        inventoryLayout.Size = new Size(422, 666);
        inventoryLayout.TabIndex = 0;
        // 
        // inventoryButtons
        // 
        inventoryButtons.Controls.Add(btnUseInventoryItem);
        inventoryButtons.Controls.Add(lblInventoryHint);
        inventoryButtons.Dock = DockStyle.Fill;
        inventoryButtons.Location = new Point(3, 3);
        inventoryButtons.Name = "inventoryButtons";
        inventoryButtons.Size = new Size(416, 32);
        inventoryButtons.TabIndex = 0;
        // 
        // btnUseInventoryItem
        // 
        btnUseInventoryItem.Enabled = false;
        btnUseInventoryItem.Location = new Point(3, 3);
        btnUseInventoryItem.Name = "btnUseInventoryItem";
        btnUseInventoryItem.Size = new Size(170, 25);
        btnUseInventoryItem.TabIndex = 0;
        btnUseInventoryItem.Text = "Использовать / надеть / снять";
        btnUseInventoryItem.Click += btnUseInventoryItem_Click;
        // 
        // lblInventoryHint
        // 
        lblInventoryHint.AutoSize = true;
        lblInventoryHint.Location = new Point(179, 8);
        lblInventoryHint.Margin = new Padding(3, 8, 3, 0);
        lblInventoryHint.Name = "lblInventoryHint";
        lblInventoryHint.Size = new Size(224, 15);
        lblInventoryHint.TabIndex = 1;
        lblInventoryHint.Text = "Выберите строку или дважды щёлкните.";
        // 
        // lvInventory
        // 
        lvInventory.Dock = DockStyle.Fill;
        lvInventory.FullRowSelect = true;
        lvInventory.Location = new Point(3, 41);
        lvInventory.Name = "lvInventory";
        lvInventory.Size = new Size(416, 622);
        lvInventory.TabIndex = 1;
        lvInventory.UseCompatibleStateImageBehavior = false;
        lvInventory.View = View.Details;
        lvInventory.SelectedIndexChanged += lvInventory_SelectedIndexChanged;
        lvInventory.DoubleClick += lvInventory_DoubleClick;
        lvInventory.Columns.Add("Экземпляр", 120);
        lvInventory.Columns.Add("Предмет", 160);
        lvInventory.Columns.Add("Состояние", 100);
        // 
        // tabEquipment
        // 
        tabEquipment.Controls.Add(lvEquipment);
        tabEquipment.Location = new Point(4, 24);
        tabEquipment.Name = "tabEquipment";
        tabEquipment.Size = new Size(422, 666);
        tabEquipment.TabIndex = 3;
        tabEquipment.Text = "Экипировка";
        // 
        // lvEquipment
        // 
        lvEquipment.Dock = DockStyle.Fill;
        lvEquipment.FullRowSelect = true;
        lvEquipment.Location = new Point(0, 0);
        lvEquipment.Name = "lvEquipment";
        lvEquipment.Size = new Size(422, 666);
        lvEquipment.TabIndex = 0;
        lvEquipment.UseCompatibleStateImageBehavior = false;
        lvEquipment.View = View.Details;
        lvEquipment.Columns.Add("Слот", 120);
        lvEquipment.Columns.Add("Название", 140);
        lvEquipment.Columns.Add("Экземпляр", 120);
        // 
        // tabSkills
        // 
        tabSkills.Controls.Add(skillsLayout);
        tabSkills.Location = new Point(4, 24);
        tabSkills.Name = "tabSkills";
        tabSkills.Size = new Size(422, 666);
        tabSkills.TabIndex = 4;
        tabSkills.Text = "Навыки";
        // 
        // skillsLayout
        // 
        skillsLayout.ColumnCount = 1;
        skillsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        skillsLayout.Controls.Add(skillsButtons, 0, 0);
        skillsLayout.Controls.Add(lvSkills, 0, 1);
        skillsLayout.Dock = DockStyle.Fill;
        skillsLayout.Location = new Point(0, 0);
        skillsLayout.Name = "skillsLayout";
        skillsLayout.RowCount = 2;
        skillsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        skillsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        skillsLayout.Size = new Size(422, 666);
        skillsLayout.TabIndex = 0;
        // 
        // skillsButtons
        // 
        skillsButtons.Controls.Add(btnUseSkill);
        skillsButtons.Controls.Add(lblSkillsHint);
        skillsButtons.Dock = DockStyle.Fill;
        skillsButtons.Location = new Point(3, 3);
        skillsButtons.Name = "skillsButtons";
        skillsButtons.Size = new Size(416, 32);
        skillsButtons.TabIndex = 0;
        // 
        // btnUseSkill
        // 
        btnUseSkill.Enabled = false;
        btnUseSkill.Location = new Point(3, 3);
        btnUseSkill.Name = "btnUseSkill";
        btnUseSkill.Size = new Size(130, 25);
        btnUseSkill.TabIndex = 0;
        btnUseSkill.Text = "Использовать навык";
        btnUseSkill.Click += btnUseSkill_Click;
        // 
        // lblSkillsHint
        // 
        lblSkillsHint.AutoSize = true;
        lblSkillsHint.Location = new Point(139, 8);
        lblSkillsHint.Margin = new Padding(3, 8, 3, 0);
        lblSkillsHint.Name = "lblSkillsHint";
        lblSkillsHint.Size = new Size(224, 15);
        lblSkillsHint.TabIndex = 1;
        lblSkillsHint.Text = "Выберите строку или дважды щёлкните.";
        // 
        // lvSkills
        // 
        lvSkills.Dock = DockStyle.Fill;
        lvSkills.FullRowSelect = true;
        lvSkills.Location = new Point(3, 41);
        lvSkills.Name = "lvSkills";
        lvSkills.Size = new Size(416, 622);
        lvSkills.TabIndex = 1;
        lvSkills.UseCompatibleStateImageBehavior = false;
        lvSkills.View = View.Details;
        lvSkills.SelectedIndexChanged += lvSkills_SelectedIndexChanged;
        lvSkills.DoubleClick += lvSkills_DoubleClick;
        lvSkills.Columns.Add("Код", 120);
        lvSkills.Columns.Add("Название", 140);
        lvSkills.Columns.Add("Состояние", 120);
        // 
        // tabRelationships
        // 
        tabRelationships.Controls.Add(lvRelationships);
        tabRelationships.Location = new Point(4, 24);
        tabRelationships.Name = "tabRelationships";
        tabRelationships.Size = new Size(422, 666);
        tabRelationships.TabIndex = 5;
        tabRelationships.Text = "Отношения";
        // 
        // lvRelationships
        // 
        lvRelationships.Dock = DockStyle.Fill;
        lvRelationships.FullRowSelect = true;
        lvRelationships.Location = new Point(0, 0);
        lvRelationships.Name = "lvRelationships";
        lvRelationships.Size = new Size(422, 666);
        lvRelationships.TabIndex = 0;
        lvRelationships.UseCompatibleStateImageBehavior = false;
        lvRelationships.View = View.Details;
        lvRelationships.Columns.Add("Код", 120);
        lvRelationships.Columns.Add("Название", 140);
        lvRelationships.Columns.Add("Значение", 120);
        // 
        // tabQuests
        // 
        tabQuests.Controls.Add(lvQuests);
        tabQuests.Location = new Point(4, 24);
        tabQuests.Name = "tabQuests";
        tabQuests.Size = new Size(422, 666);
        tabQuests.TabIndex = 6;
        tabQuests.Text = "Квесты";
        // 
        // lvQuests
        // 
        lvQuests.Dock = DockStyle.Fill;
        lvQuests.FullRowSelect = true;
        lvQuests.Location = new Point(0, 0);
        lvQuests.Name = "lvQuests";
        lvQuests.Size = new Size(422, 666);
        lvQuests.TabIndex = 0;
        lvQuests.UseCompatibleStateImageBehavior = false;
        lvQuests.View = View.Details;
        lvQuests.Columns.Add("Код", 120);
        lvQuests.Columns.Add("Название", 160);
        lvQuests.Columns.Add("Состояние", 100);
        // 
        // tabMap
        // 
        tabMap.Controls.Add(mapLayout);
        tabMap.Location = new Point(4, 24);
        tabMap.Name = "tabMap";
        tabMap.Size = new Size(422, 666);
        tabMap.TabIndex = 7;
        tabMap.Text = "Карта";
        // 
        // mapLayout
        // 
        mapLayout.ColumnCount = 1;
        mapLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mapLayout.Controls.Add(mapButtons, 0, 0);
        mapLayout.Controls.Add(lvMap, 0, 1);
        mapLayout.Dock = DockStyle.Fill;
        mapLayout.Location = new Point(0, 0);
        mapLayout.Name = "mapLayout";
        mapLayout.RowCount = 2;
        mapLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        mapLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mapLayout.Size = new Size(422, 666);
        mapLayout.TabIndex = 0;
        // 
        // mapButtons
        // 
        mapButtons.Controls.Add(btnTravelToLocation);
        mapButtons.Controls.Add(lblMapHint);
        mapButtons.Dock = DockStyle.Fill;
        mapButtons.Location = new Point(3, 3);
        mapButtons.Name = "mapButtons";
        mapButtons.Size = new Size(416, 32);
        mapButtons.TabIndex = 0;
        // 
        // btnTravelToLocation
        // 
        btnTravelToLocation.Enabled = false;
        btnTravelToLocation.Location = new Point(3, 3);
        btnTravelToLocation.Name = "btnTravelToLocation";
        btnTravelToLocation.Size = new Size(90, 25);
        btnTravelToLocation.TabIndex = 0;
        btnTravelToLocation.Text = "Перейти";
        btnTravelToLocation.Click += btnTravelToLocation_Click;
        // 
        // lblMapHint
        // 
        lblMapHint.AutoSize = true;
        lblMapHint.Location = new Point(99, 8);
        lblMapHint.Margin = new Padding(3, 8, 3, 0);
        lblMapHint.Name = "lblMapHint";
        lblMapHint.Size = new Size(224, 15);
        lblMapHint.TabIndex = 1;
        lblMapHint.Text = "Выберите строку или дважды щёлкните.";
        // 
        // lvMap
        // 
        lvMap.Dock = DockStyle.Fill;
        lvMap.FullRowSelect = true;
        lvMap.Location = new Point(3, 41);
        lvMap.Name = "lvMap";
        lvMap.Size = new Size(416, 622);
        lvMap.TabIndex = 1;
        lvMap.UseCompatibleStateImageBehavior = false;
        lvMap.View = View.Details;
        lvMap.SelectedIndexChanged += lvMap_SelectedIndexChanged;
        lvMap.DoubleClick += lvMap_DoubleClick;
        lvMap.Columns.Add("Код", 120);
        lvMap.Columns.Add("Название", 140);
        lvMap.Columns.Add("Описание", 160);
        // 
        // tabActions
        // 
        tabActions.Controls.Add(actionsLayout);
        tabActions.Location = new Point(4, 24);
        tabActions.Name = "tabActions";
        tabActions.Size = new Size(422, 666);
        tabActions.TabIndex = 8;
        tabActions.Text = "Действия";
        // 
        // actionsLayout
        // 
        actionsLayout.ColumnCount = 1;
        actionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionsLayout.Controls.Add(actionsButtons, 0, 0);
        actionsLayout.Controls.Add(lvActions, 0, 1);
        actionsLayout.Dock = DockStyle.Fill;
        actionsLayout.Location = new Point(0, 0);
        actionsLayout.Name = "actionsLayout";
        actionsLayout.RowCount = 2;
        actionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        actionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        actionsLayout.Size = new Size(422, 666);
        actionsLayout.TabIndex = 0;
        // 
        // actionsButtons
        // 
        actionsButtons.Controls.Add(btnExecuteAction);
        actionsButtons.Controls.Add(btnRefreshActions);
        actionsButtons.Dock = DockStyle.Fill;
        actionsButtons.Location = new Point(3, 3);
        actionsButtons.Name = "actionsButtons";
        actionsButtons.Size = new Size(416, 32);
        actionsButtons.TabIndex = 0;
        // 
        // btnExecuteAction
        // 
        btnExecuteAction.Enabled = false;
        btnExecuteAction.Location = new Point(3, 3);
        btnExecuteAction.Name = "btnExecuteAction";
        btnExecuteAction.Size = new Size(150, 25);
        btnExecuteAction.TabIndex = 0;
        btnExecuteAction.Text = "Выполнить действие";
        btnExecuteAction.Click += btnExecuteAction_Click;
        // 
        // btnRefreshActions
        // 
        btnRefreshActions.Location = new Point(159, 3);
        btnRefreshActions.Name = "btnRefreshActions";
        btnRefreshActions.Size = new Size(90, 25);
        btnRefreshActions.TabIndex = 1;
        btnRefreshActions.Text = "Обновить";
        btnRefreshActions.Click += btnRefreshActions_Click;
        // 
        // lvActions
        // 
        lvActions.Dock = DockStyle.Fill;
        lvActions.FullRowSelect = true;
        lvActions.Location = new Point(3, 41);
        lvActions.Name = "lvActions";
        lvActions.Size = new Size(416, 622);
        lvActions.TabIndex = 1;
        lvActions.UseCompatibleStateImageBehavior = false;
        lvActions.View = View.Details;
        lvActions.SelectedIndexChanged += lvActions_SelectedIndexChanged;
        lvActions.DoubleClick += lvActions_DoubleClick;
        lvActions.Columns.Add("Код", 100);
        lvActions.Columns.Add("Название", 120);
        lvActions.Columns.Add("Тип", 80);
        lvActions.Columns.Add("Доступно", 80);
        lvActions.Columns.Add("Причина", 180);
        lvActions.Columns.Add("Cooldown", 80);
        lvActions.Columns.Add("Стоимость", 120);
        lvActions.Columns.Add("Описание", 180);
        // 
        // tabCombat
        // 
        tabCombat.Controls.Add(combatLayout);
        tabCombat.Location = new Point(4, 24);
        tabCombat.Name = "tabCombat";
        tabCombat.Size = new Size(422, 666);
        tabCombat.TabIndex = 9;
        tabCombat.Text = "Бой";
        // 
        // combatLayout
        // 
        combatLayout.ColumnCount = 1;
        combatLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        combatLayout.Controls.Add(combatButtons, 0, 0);
        combatLayout.Controls.Add(lblCombatHint, 0, 1);
        combatLayout.Controls.Add(combatSplit, 0, 2);
        combatLayout.Dock = DockStyle.Fill;
        combatLayout.Location = new Point(0, 0);
        combatLayout.Name = "combatLayout";
        combatLayout.RowCount = 3;
        combatLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        combatLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        combatLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        combatLayout.Size = new Size(422, 666);
        combatLayout.TabIndex = 0;
        // 
        // combatButtons
        // 
        combatButtons.Controls.Add(btnStartCombat);
        combatButtons.Controls.Add(btnExecuteCombatAction);
        combatButtons.Controls.Add(btnEndCombatTurn);
        combatButtons.Dock = DockStyle.Fill;
        combatButtons.Location = new Point(3, 3);
        combatButtons.Name = "combatButtons";
        combatButtons.Size = new Size(416, 28);
        combatButtons.TabIndex = 0;
        // 
        // btnStartCombat
        // 
        btnStartCombat.Enabled = false;
        btnStartCombat.Location = new Point(3, 3);
        btnStartCombat.Name = "btnStartCombat";
        btnStartCombat.Size = new Size(105, 25);
        btnStartCombat.TabIndex = 0;
        btnStartCombat.Text = "Начать бой";
        btnStartCombat.Click += btnStartCombat_Click;
        // 
        // btnExecuteCombatAction
        // 
        btnExecuteCombatAction.Enabled = false;
        btnExecuteCombatAction.Location = new Point(114, 3);
        btnExecuteCombatAction.Name = "btnExecuteCombatAction";
        btnExecuteCombatAction.Size = new Size(95, 25);
        btnExecuteCombatAction.TabIndex = 1;
        btnExecuteCombatAction.Text = "Выполнить";
        btnExecuteCombatAction.Click += btnExecuteCombatAction_Click;
        // 
        // btnEndCombatTurn
        // 
        btnEndCombatTurn.Enabled = false;
        btnEndCombatTurn.Location = new Point(215, 3);
        btnEndCombatTurn.Name = "btnEndCombatTurn";
        btnEndCombatTurn.Size = new Size(90, 25);
        btnEndCombatTurn.TabIndex = 2;
        btnEndCombatTurn.Text = "Конец хода";
        btnEndCombatTurn.Click += btnEndCombatTurn_Click;
        // 
        // lblCombatHint
        // 
        lblCombatHint.Dock = DockStyle.Fill;
        lblCombatHint.Location = new Point(3, 34);
        lblCombatHint.Name = "lblCombatHint";
        lblCombatHint.Size = new Size(416, 24);
        lblCombatHint.TabIndex = 1;
        lblCombatHint.Text = "Выберите действие и цель.";
        lblCombatHint.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // combatSplit
        // 
        combatSplit.Dock = DockStyle.Fill;
        combatSplit.Location = new Point(3, 61);
        combatSplit.Name = "combatSplit";
        combatSplit.Orientation = Orientation.Horizontal;
        // 
        // combatSplit.Panel1
        // 
        combatSplit.Panel1.Controls.Add(lvCombatants);
        // 
        // combatSplit.Panel2
        // 
        combatSplit.Panel2.Controls.Add(lvCombatActions);
        combatSplit.Size = new Size(416, 602);
        combatSplit.SplitterDistance = 290;
        combatSplit.TabIndex = 2;
        // 
        // lvCombatants
        // 
        lvCombatants.Dock = DockStyle.Fill;
        lvCombatants.FullRowSelect = true;
        lvCombatants.Location = new Point(0, 0);
        lvCombatants.Name = "lvCombatants";
        lvCombatants.Size = new Size(416, 290);
        lvCombatants.TabIndex = 0;
        lvCombatants.UseCompatibleStateImageBehavior = false;
        lvCombatants.View = View.Details;
        lvCombatants.SelectedIndexChanged += lvCombatants_SelectedIndexChanged;
        lvCombatants.Columns.Add("Участник", 160);
        lvCombatants.Columns.Add("Команда", 80);
        lvCombatants.Columns.Add("HP", 70);
        lvCombatants.Columns.Add("Инициатива", 80);
        lvCombatants.Columns.Add("Статус", 180);
        // 
        // lvCombatActions
        // 
        lvCombatActions.Dock = DockStyle.Fill;
        lvCombatActions.FullRowSelect = true;
        lvCombatActions.Location = new Point(0, 0);
        lvCombatActions.Name = "lvCombatActions";
        lvCombatActions.Size = new Size(416, 308);
        lvCombatActions.TabIndex = 0;
        lvCombatActions.UseCompatibleStateImageBehavior = false;
        lvCombatActions.View = View.Details;
        lvCombatActions.SelectedIndexChanged += lvCombatActions_SelectedIndexChanged;
        lvCombatActions.Columns.Add("Действие", 180);
        lvCombatActions.Columns.Add("Цель", 100);
        lvCombatActions.Columns.Add("Описание", 260);
        // tabEffects
        // 
        tabEffects.Controls.Add(lvEffects);
        tabEffects.Location = new Point(4, 24);
        tabEffects.Name = "tabEffects";
        tabEffects.Size = new Size(422, 666);
        tabEffects.TabIndex = 9;
        tabEffects.Text = "Эффекты";
        // 
        // lvEffects
        // 
        lvEffects.Dock = DockStyle.Fill;
        lvEffects.FullRowSelect = true;
        lvEffects.Location = new Point(0, 0);
        lvEffects.Name = "lvEffects";
        lvEffects.Size = new Size(422, 666);
        lvEffects.TabIndex = 0;
        lvEffects.UseCompatibleStateImageBehavior = false;
        lvEffects.View = View.Details;
        lvEffects.Columns.Add("Код", 100);
        lvEffects.Columns.Add("Название", 120);
        lvEffects.Columns.Add("Тип", 90);
        lvEffects.Columns.Add("Длительность", 90);
        lvEffects.Columns.Add("Стаки", 70);
        // 
        // tabProgression
        // 
        tabProgression.Controls.Add(progressionLayout);
        tabProgression.Location = new Point(4, 24);
        tabProgression.Name = "tabProgression";
        tabProgression.Size = new Size(422, 666);
        tabProgression.TabIndex = 10;
        tabProgression.Text = "Прокачка";
        // 
        // progressionLayout
        // 
        progressionLayout.ColumnCount = 1;
        progressionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        progressionLayout.Controls.Add(progressionButtons, 0, 0);
        progressionLayout.Controls.Add(lvProgression, 0, 1);
        progressionLayout.Dock = DockStyle.Fill;
        progressionLayout.Location = new Point(0, 0);
        progressionLayout.Name = "progressionLayout";
        progressionLayout.RowCount = 2;
        progressionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        progressionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        progressionLayout.Size = new Size(422, 666);
        progressionLayout.TabIndex = 0;
        // 
        // progressionButtons
        // 
        progressionButtons.Controls.Add(btnUnlockProgression);
        progressionButtons.Controls.Add(btnRefreshProgression);
        progressionButtons.Dock = DockStyle.Fill;
        progressionButtons.Location = new Point(3, 3);
        progressionButtons.Name = "progressionButtons";
        progressionButtons.Size = new Size(416, 32);
        progressionButtons.TabIndex = 0;
        // 
        // btnUnlockProgression
        // 
        btnUnlockProgression.Enabled = false;
        btnUnlockProgression.Location = new Point(3, 3);
        btnUnlockProgression.Name = "btnUnlockProgression";
        btnUnlockProgression.Size = new Size(120, 25);
        btnUnlockProgression.TabIndex = 0;
        btnUnlockProgression.Text = "Открыть узел";
        btnUnlockProgression.Click += btnUnlockProgression_Click;
        // 
        // btnRefreshProgression
        // 
        btnRefreshProgression.Location = new Point(129, 3);
        btnRefreshProgression.Name = "btnRefreshProgression";
        btnRefreshProgression.Size = new Size(90, 25);
        btnRefreshProgression.TabIndex = 1;
        btnRefreshProgression.Text = "Обновить";
        btnRefreshProgression.Click += btnRefreshProgression_Click;
        // 
        // lvProgression
        // 
        lvProgression.Dock = DockStyle.Fill;
        lvProgression.FullRowSelect = true;
        lvProgression.Location = new Point(3, 41);
        lvProgression.Name = "lvProgression";
        lvProgression.Size = new Size(416, 622);
        lvProgression.TabIndex = 1;
        lvProgression.UseCompatibleStateImageBehavior = false;
        lvProgression.View = View.Details;
        lvProgression.SelectedIndexChanged += lvProgression_SelectedIndexChanged;
        lvProgression.DoubleClick += lvProgression_DoubleClick;
        lvProgression.Columns.Add("Код", 100);
        lvProgression.Columns.Add("Название", 120);
        lvProgression.Columns.Add("Тип", 80);
        lvProgression.Columns.Add("Состояние", 90);
        lvProgression.Columns.Add("Требования", 160);
        lvProgression.Columns.Add("Стоимость", 120);
        lvProgression.Columns.Add("Описание", 180);        // tabLog
        // 
        tabLog.Controls.Add(txtLog);
        tabLog.Location = new Point(4, 24);
        tabLog.Name = "tabLog";
        tabLog.Size = new Size(422, 666);
        tabLog.TabIndex = 11;
        tabLog.Text = "Журнал";
        // 
        // txtLog
        // 
        txtLog.Dock = DockStyle.Fill;
        txtLog.Location = new Point(0, 0);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(422, 666);
        txtLog.TabIndex = 0;
        // 
        // PlayForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 800);
        Controls.Add(rootLayout);
        MinimumSize = new Size(1000, 680);
        Name = "PlayForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Игра";
        rootLayout.ResumeLayout(false);
        headerLayout.ResumeLayout(false);
        toolbar.ResumeLayout(false);
        mainSplit.Panel1.ResumeLayout(false);
        mainSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)mainSplit).EndInit();
        mainSplit.ResumeLayout(false);
        sceneLayout.ResumeLayout(false);
        sceneLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)picScene).EndInit();
        tabInfo.ResumeLayout(false);
        tabStats.ResumeLayout(false);
        tabCurrencies.ResumeLayout(false);
        tabInventory.ResumeLayout(false);
        inventoryLayout.ResumeLayout(false);
        inventoryButtons.ResumeLayout(false);
        inventoryButtons.PerformLayout();
        tabEquipment.ResumeLayout(false);
        tabSkills.ResumeLayout(false);
        skillsLayout.ResumeLayout(false);
        skillsButtons.ResumeLayout(false);
        skillsButtons.PerformLayout();
        tabRelationships.ResumeLayout(false);
        tabQuests.ResumeLayout(false);
        tabMap.ResumeLayout(false);
        mapLayout.ResumeLayout(false);
        mapButtons.ResumeLayout(false);
        mapButtons.PerformLayout();
        tabActions.ResumeLayout(false);
        actionsLayout.ResumeLayout(false);
        actionsButtons.ResumeLayout(false);
        tabCombat.ResumeLayout(false);
        combatLayout.ResumeLayout(false);
        combatButtons.ResumeLayout(false);
        combatSplit.Panel1.ResumeLayout(false);
        combatSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)combatSplit).EndInit();
        combatSplit.ResumeLayout(false);
        tabEffects.ResumeLayout(false);
        tabProgression.ResumeLayout(false);
        progressionLayout.ResumeLayout(false);
        progressionButtons.ResumeLayout(false);
        tabLog.ResumeLayout(false);
        tabLog.PerformLayout();
        ResumeLayout(false);
    }
}
