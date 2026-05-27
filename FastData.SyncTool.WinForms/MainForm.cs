using FastData.Base;
using FastData.Database;
using FastData.Tooling.Sync;
using FastData.SyncTool;
using FastData.SyncTool.WinForms.IoC;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace FastData.SyncTool.WinForms
{
    public class MainForm : Form
    {
        private readonly ComboBox sourceProviderBox = new ComboBox();
        private readonly TextBox sourceConnectionBox = new TextBox();
        private readonly ComboBox targetProviderBox = new ComboBox();
        private readonly TextBox targetConnectionBox = new TextBox();
        private readonly ComboBox intermediateProviderBox = new ComboBox();
        private readonly TextBox intermediateConnectionBox = new TextBox();
        private readonly TextBox taskIdBox = new TextBox();

        private readonly DataGridView tableListGrid = new DataGridView();
        private readonly Button addTableButton = new Button();
        private readonly Button removeTableButton = new Button();
        private readonly Button loadTablesButton = new Button();
        private readonly Button moveUpButton = new Button();
        private readonly Button moveDownButton = new Button();

        private readonly NumericUpDown rangeDaysBox = new NumericUpDown();
        private readonly Label lastSyncTimeLabel = new Label();

        private readonly TextBox primaryKeyColumnsBox = new TextBox();
        private readonly TextBox timeColumnBox = new TextBox();
        private readonly CheckBox enableTimeRangeBox = new CheckBox();
        private readonly CheckBox enableGlobalConfigBox = new CheckBox();
        private readonly NumericUpDown globalRangeDaysBox = new NumericUpDown();
        private readonly CheckBox alwaysDeduplicateBox = new CheckBox();
        private readonly NumericUpDown batchSizeBox = new NumericUpDown();
        private readonly NumericUpDown retryCountBox = new NumericUpDown();
        private readonly CheckBox autoCreateIntermediateBox = new CheckBox();
        private readonly CheckBox resumeFailedRecordsBox = new CheckBox();
        private readonly CheckBox cleanIntermediateBox = new CheckBox();

        private readonly CheckBox enableTimerBox = new CheckBox();
        private readonly NumericUpDown intervalBox = new NumericUpDown();
        private readonly TextBox logBox = new TextBox();
        private readonly Label syncStatusLabel = new Label();

        private readonly Button exportSchemaButton = new Button();
        private readonly Button syncButton = new Button();
        private readonly Button pkConfigButton = new Button();
        private readonly Button saveTaskButton = new Button();

        private readonly DataGridView taskListGrid = new DataGridView();
        private readonly Button newTaskButton = new Button();
        private readonly Button deleteTaskButton = new Button();
        private readonly Button loadTaskButton = new Button();
        private readonly Button refreshTaskButton = new Button();
        private readonly Button editTaskButton = new Button();
        private readonly Button batchDeleteButton = new Button();
        private readonly Button batchEnableButton = new Button();
        private readonly Button batchDisableButton = new Button();
        private readonly Button exportTaskButton = new Button();
        private readonly Button importTaskButton = new Button();

        private System.Timers.Timer syncTimer;
        private bool isSyncing;
        private readonly ServiceContainer serviceProvider;
        private readonly PrimaryKeyConfigService pkConfigService;
        private readonly SyncConfigManager configManager;
        private List<TableSyncConfig> tableConfigs = new List<TableSyncConfig>();
        private List<SyncTaskConfig> taskConfigs = new List<SyncTaskConfig>();
        private TabControl tabControl;
        private TabPage replayTab;
        private TabPage dbConfigTab;

        // 补录功能控件
        private readonly ComboBox replaySourceProviderBox = new ComboBox();
        private readonly TextBox replaySourceConnectionBox = new TextBox();
        private readonly ComboBox replayTargetProviderBox = new ComboBox();
        private readonly TextBox replayTargetConnectionBox = new TextBox();
        private readonly TextBox replayTableNameBox = new TextBox();
        private readonly DateTimePicker replayStartTimeBox = new DateTimePicker();
        private readonly DateTimePicker replayEndTimeBox = new DateTimePicker();
        private readonly CheckBox replayEnableTimeRangeBox = new CheckBox();
        private readonly TextBox replayPrimaryKeyBox = new TextBox();
        private readonly Button replayLoadTablesButton = new Button();
        private readonly Button replayStartButton = new Button();
        private readonly TextBox replayLogBox = new TextBox();
        private readonly Label replayStatusLabel = new Label();

        // 数据库配置控件
        private readonly TextBox dbConnectionNameBox = new TextBox();
        private readonly ComboBox dbProviderBox = new ComboBox();
        private readonly TextBox dbConnectionStringBox = new TextBox();
        private readonly Button dbTestButton = new Button();
        private readonly Button dbSaveButton = new Button();
        private readonly Button dbDeleteButton = new Button();
        private readonly DataGridView dbConnectionGrid = new DataGridView();
        private readonly TextBox dbLogBox = new TextBox();

        // 共享日志控件
        private readonly SplitContainer mainSplitContainer = new SplitContainer();
        private readonly TextBox sharedLogBox = new TextBox();
        private readonly Button clearLogButton = new Button();
        private readonly Button exportLogButton = new Button();

        public MainForm()
        {
            Text = "FastData 数据同步工具";
            Width = 1200;
            Height = 800;
            
            // 初始化依赖注入容器
            serviceProvider = new ServiceContainer();
            serviceProvider.RegisterSyncToolServices();
            
            // 从容器解析服务
            pkConfigService = serviceProvider.Resolve<PrimaryKeyConfigService>();
            configManager = serviceProvider.Resolve<SyncConfigManager>();
            
            tabControl = new TabControl();
            BuildLayout();
            BindEvents();
            InitializeTimer();
            LoadTaskList();
        }

        private void BuildLayout()
        {
            // 主布局：上部分是 TabControl，下部分是共享日志
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.Orientation = Orientation.Horizontal;
            mainSplitContainer.SplitterDistance = Height - 200;

            Controls.Add(mainSplitContainer);

            // 上部分：TabControl
            tabControl = new TabControl { Dock = DockStyle.Fill };
            dbConfigTab = new TabPage("数据库配置");
            var configTab = new TabPage("同步配置");
            var taskTab = new TabPage("任务管理");
            replayTab = new TabPage("数据补录");
            var shardingTab = new TabPage("分表同步");
            tabControl.TabPages.Add(dbConfigTab);
            tabControl.TabPages.Add(configTab);
            tabControl.TabPages.Add(taskTab);
            tabControl.TabPages.Add(replayTab);
            tabControl.TabPages.Add(shardingTab);
            mainSplitContainer.Panel1.Controls.Add(tabControl);

            // 下部分：共享日志
            var logPanel = new Panel { Dock = DockStyle.Fill };
            
            var logHeaderPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30 };
            var logLabel = new Label { Text = "运行日志", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(5, 5, 0, 0) };
            logHeaderPanel.Controls.Add(logLabel);
            
            clearLogButton.Text = "清空日志";
            clearLogButton.Anchor = AnchorStyles.Right;
            clearLogButton.Margin = new Padding(5, 3, 0, 0);
            logHeaderPanel.Controls.Add(clearLogButton);
            
            exportLogButton.Text = "导出日志";
            exportLogButton.Anchor = AnchorStyles.Right;
            exportLogButton.Margin = new Padding(5, 3, 0, 0);
            logHeaderPanel.Controls.Add(exportLogButton);

            sharedLogBox.Dock = DockStyle.Fill;
            sharedLogBox.Multiline = true;
            sharedLogBox.ScrollBars = ScrollBars.Both;
            sharedLogBox.ReadOnly = true;
            sharedLogBox.Font = new System.Drawing.Font("Consolas", 9);
            sharedLogBox.WordWrap = false;
            sharedLogBox.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            sharedLogBox.ForeColor = System.Drawing.Color.FromArgb(220, 220, 220);
            
            logPanel.Controls.Add(sharedLogBox);
            logPanel.Controls.Add(logHeaderPanel);
            mainSplitContainer.Panel2.Controls.Add(logPanel);

            BuildDbConfigTab(dbConfigTab);
            BuildConfigTab(configTab);
            BuildTaskTab(taskTab);
            BuildReplayTab(replayTab);
            BuildShardingTab(shardingTab);
        }

        private void BuildDbConfigTab(TabPage tab)
        {
            var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8 };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;

            // 连接名称
            AddLabel(mainPanel, "连接名称", row++);
            dbConnectionNameBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(dbConnectionNameBox, 1, row - 1);

            // Provider
            AddLabel(mainPanel, "数据库类型", row++);
            dbProviderBox.DropDownStyle = ComboBoxStyle.DropDownList;
            dbProviderBox.Dock = DockStyle.Fill;
            dbProviderBox.Items.AddRange(DatabaseProviderMappings.AllProviderNames);
            dbProviderBox.SelectedIndex = 0;
            mainPanel.Controls.Add(dbProviderBox, 1, row - 1);

            // 连接字符串
            AddLabel(mainPanel, "连接字符串", row++);
            dbConnectionStringBox.Dock = DockStyle.Fill;
            dbConnectionStringBox.Multiline = true;
            dbConnectionStringBox.ScrollBars = ScrollBars.Vertical;
            dbConnectionStringBox.Height = 80;
            mainPanel.Controls.Add(dbConnectionStringBox, 1, row - 1);

            // 按钮
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            dbTestButton.Text = "测试连接";
            dbTestButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            dbTestButton.ForeColor = System.Drawing.Color.White;
            dbTestButton.Margin = new Padding(0, 0, 10, 0);
            buttonPanel.Controls.Add(dbTestButton);

            dbSaveButton.Text = "保存连接";
            dbSaveButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            dbSaveButton.ForeColor = System.Drawing.Color.White;
            dbSaveButton.Margin = new Padding(0, 0, 10, 0);
            buttonPanel.Controls.Add(dbSaveButton);

            dbDeleteButton.Text = "删除连接";
            dbDeleteButton.BackColor = System.Drawing.Color.FromArgb(200, 50, 50);
            dbDeleteButton.ForeColor = System.Drawing.Color.White;
            buttonPanel.Controls.Add(dbDeleteButton);

            mainPanel.Controls.Add(buttonPanel, 1, row++);

            // 连接列表
            AddLabel(mainPanel, "已保存的连接", row++);
            InitDbConnectionGrid(dbConnectionGrid);
            dbConnectionGrid.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(dbConnectionGrid, 1, row++);

            // 日志
            AddLabel(mainPanel, "数据库配置日志", row++);
            dbLogBox.Dock = DockStyle.Fill;
            dbLogBox.Multiline = true;
            dbLogBox.ScrollBars = ScrollBars.Both;
            dbLogBox.ReadOnly = true;
            dbLogBox.Font = new System.Drawing.Font("Consolas", 9);
            dbLogBox.Height = 100;
            mainPanel.Controls.Add(dbLogBox, 1, row++);

            for (var i = 0; i < 7; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            tab.Controls.Add(mainPanel);
        }

        private void InitDbConnectionGrid(DataGridView grid)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "连接名称", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Provider", HeaderText = "数据库类型", Width = 200 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ConnectionString", HeaderText = "连接字符串", Width = 400 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastTestTime", HeaderText = "最后测试时间", Width = 150 });
        }

        private void BuildConfigTab(TabPage tab)
        {
            var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 23 };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tab.Controls.Add(mainPanel);

            int row = 0;

            AddLabel(mainPanel, "源库 Provider", row++);
            InitProviderBox(sourceProviderBox);
            mainPanel.Controls.Add(sourceProviderBox, 1, row - 1);

            AddLabel(mainPanel, "源库连接字符串", row++);
            sourceConnectionBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(sourceConnectionBox, 1, row - 1);

            AddLabel(mainPanel, "目标库 Provider", row++);
            InitProviderBox(targetProviderBox);
            mainPanel.Controls.Add(targetProviderBox, 1, row - 1);

            AddLabel(mainPanel, "目标库连接字符串", row++);
            targetConnectionBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(targetConnectionBox, 1, row - 1);

            AddLabel(mainPanel, "中间库 Provider", row++);
            InitProviderBox(intermediateProviderBox);
            mainPanel.Controls.Add(intermediateProviderBox, 1, row - 1);

            AddLabel(mainPanel, "中间库连接字符串", row++);
            intermediateConnectionBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(intermediateConnectionBox, 1, row - 1);

            AddLabel(mainPanel, "任务 ID", row++);
            taskIdBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(taskIdBox, 1, row - 1);

            // 表配置区域
            row++;
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            buttonPanel.Controls.Add(loadTablesButton);
            buttonPanel.Controls.Add(addTableButton);
            buttonPanel.Controls.Add(removeTableButton);
            buttonPanel.Controls.Add(moveUpButton);
            buttonPanel.Controls.Add(moveDownButton);

            var tablePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            tablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            tablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.Controls.Add(tablePanel, 1, row);
            tablePanel.Controls.Add(buttonPanel, 0, 0);

            InitTableGrid();
            tablePanel.Controls.Add(tableListGrid, 0, 1);

            // 高级配置
            row++;
            AddLabel(mainPanel, "主键字段", row);
            primaryKeyColumnsBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(primaryKeyColumnsBox, 1, row++);

            AddLabel(mainPanel, "时间字段", row);
            timeColumnBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(timeColumnBox, 1, row++);

            AddLabel(mainPanel, "启用时间范围", row);
            enableTimeRangeBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(enableTimeRangeBox, 1, row++);

            AddLabel(mainPanel, "时间范围 (天)", row);
            rangeDaysBox.Dock = DockStyle.Fill;
            rangeDaysBox.Minimum = 1;
            rangeDaysBox.Maximum = 365;
            rangeDaysBox.Value = 3;
            mainPanel.Controls.Add(rangeDaysBox, 1, row++);

            // 全局配置
            AddLabel(mainPanel, "启用全局配置", row);
            enableGlobalConfigBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(enableGlobalConfigBox, 1, row++);

            AddLabel(mainPanel, "全局范围天数 (0=使用任务配置)", row);
            globalRangeDaysBox.Dock = DockStyle.Fill;
            globalRangeDaysBox.Minimum = 0;
            globalRangeDaysBox.Maximum = 365;
            globalRangeDaysBox.Value = 0;
            mainPanel.Controls.Add(globalRangeDaysBox, 1, row++);

            AddLabel(mainPanel, "始终去重（只插入不存在的记录）", row);
            alwaysDeduplicateBox.Dock = DockStyle.Fill;
            alwaysDeduplicateBox.Checked = true;
            mainPanel.Controls.Add(alwaysDeduplicateBox, 1, row++);

            AddLabel(mainPanel, "批次大小", row);
            batchSizeBox.Dock = DockStyle.Fill;
            batchSizeBox.Minimum = 100;
            batchSizeBox.Maximum = 10000;
            batchSizeBox.Increment = 100;
            batchSizeBox.Value = 1000;
            mainPanel.Controls.Add(batchSizeBox, 1, row++);

            AddLabel(mainPanel, "重试次数", row);
            retryCountBox.Dock = DockStyle.Fill;
            retryCountBox.Minimum = 0;
            retryCountBox.Maximum = 5;
            retryCountBox.Value = 3;
            mainPanel.Controls.Add(retryCountBox, 1, row++);

            AddLabel(mainPanel, "自动创建中间库表结构", row);
            autoCreateIntermediateBox.Dock = DockStyle.Fill;
            autoCreateIntermediateBox.Checked = true;
            mainPanel.Controls.Add(autoCreateIntermediateBox, 1, row++);

            AddLabel(mainPanel, "失败记录续传", row);
            resumeFailedRecordsBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(resumeFailedRecordsBox, 1, row++);

            AddLabel(mainPanel, "同步后清理中间库", row);
            cleanIntermediateBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(cleanIntermediateBox, 1, row++);

            // 定时同步
            row++;
            var timerPanel = new GroupBox { Text = "定时同步", Dock = DockStyle.Fill };
            var timerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
            timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            timerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            timerPanel.Controls.Add(timerLayout);

            timerLayout.Controls.Add(new Label { Text = "启用定时同步", AutoSize = true }, 0, 0);
            timerLayout.Controls.Add(enableTimerBox, 1, 0);
            timerLayout.Controls.Add(new Label { Text = "间隔 (秒):", AutoSize = true, Margin = new Padding(20, 0, 0, 0) }, 2, 0);
            intervalBox.Minimum = 5;
            intervalBox.Maximum = 3600;
            intervalBox.Value = 60;
            intervalBox.Width = 60;
            timerLayout.Controls.Add(intervalBox, 3, 0);
            mainPanel.Controls.Add(timerPanel, 1, row++);
            mainPanel.SetColumnSpan(timerPanel, 1);

            // 按钮区域
            row++;
            var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            actionPanel.Controls.Add(syncButton);
            actionPanel.Controls.Add(exportSchemaButton);
            actionPanel.Controls.Add(pkConfigButton);
            actionPanel.Controls.Add(saveTaskButton);
            mainPanel.Controls.Add(actionPanel, 1, row++);
            mainPanel.SetColumnSpan(actionPanel, 2);

            // 状态栏
            row++;
            syncStatusLabel.Dock = DockStyle.Fill;
            syncStatusLabel.BackColor = System.Drawing.Color.LightGray;
            syncStatusLabel.Text = "就绪";
            syncStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            mainPanel.Controls.Add(syncStatusLabel, 1, row++);
            mainPanel.SetColumnSpan(syncStatusLabel, 2);

            // 日志区域
            row++;
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.Height = 150;
            logBox.ReadOnly = true;
            mainPanel.Controls.Add(logBox, 1, row++);
            mainPanel.SetColumnSpan(logBox, 2);
        }

        private void BuildTaskTab(TabPage tab)
        {
            var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tab.Controls.Add(mainPanel);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            buttonPanel.Controls.Add(newTaskButton);
            buttonPanel.Controls.Add(editTaskButton);
            buttonPanel.Controls.Add(loadTaskButton);
            buttonPanel.Controls.Add(refreshTaskButton);
            buttonPanel.Controls.Add(new Label { Width = 20 });
            buttonPanel.Controls.Add(batchEnableButton);
            buttonPanel.Controls.Add(batchDisableButton);
            buttonPanel.Controls.Add(batchDeleteButton);
            buttonPanel.Controls.Add(new Label { Width = 20 });
            buttonPanel.Controls.Add(exportTaskButton);
            buttonPanel.Controls.Add(importTaskButton);
            mainPanel.Controls.Add(buttonPanel, 0, 0);

            InitTaskGrid();
            mainPanel.Controls.Add(taskListGrid, 0, 1);
        }

        private void BuildReplayTab(TabPage tab)
        {
            var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 13 };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // 源库配置
            AddLabel(mainPanel, "源库 Provider", 0);
            replaySourceProviderBox.DropDownStyle = ComboBoxStyle.DropDownList;
            replaySourceProviderBox.Items.AddRange(DatabaseProviderMappings.AllProviderNames);
            replaySourceProviderBox.SelectedIndex = 0;
            mainPanel.Controls.Add(replaySourceProviderBox, 1, 0);

            AddLabel(mainPanel, "源库连接字符串", 1);
            replaySourceConnectionBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(replaySourceConnectionBox, 1, 1);

            // 目标库配置
            AddLabel(mainPanel, "目标库 Provider", 2);
            replayTargetProviderBox.DropDownStyle = ComboBoxStyle.DropDownList;
            replayTargetProviderBox.Items.AddRange(DatabaseProviderMappings.AllProviderNames);
            replayTargetProviderBox.SelectedIndex = 0;
            mainPanel.Controls.Add(replayTargetProviderBox, 1, 2);

            AddLabel(mainPanel, "目标库连接字符串", 3);
            replayTargetConnectionBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(replayTargetConnectionBox, 1, 3);

            // 表名
            AddLabel(mainPanel, "表名", 4);
            replayTableNameBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(replayTableNameBox, 1, 4);

            // 主键配置
            AddLabel(mainPanel, "业务主键", 5);
            replayPrimaryKeyBox.Dock = DockStyle.Fill;
            replayPrimaryKeyBox.Text = "Id";
            mainPanel.Controls.Add(replayPrimaryKeyBox, 1, 5);

            // 加载表按钮
            var loadButtonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            replayLoadTablesButton.Text = "加载表列表";
            loadButtonPanel.Controls.Add(replayLoadTablesButton);
            mainPanel.Controls.Add(loadButtonPanel, 1, 6);

            // 时间范围
            AddLabel(mainPanel, "启用时间范围", 7);
            replayEnableTimeRangeBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(replayEnableTimeRangeBox, 1, 7);

            AddLabel(mainPanel, "开始时间", 8);
            replayStartTimeBox.Dock = DockStyle.Fill;
            replayStartTimeBox.Format = DateTimePickerFormat.Short;
            replayStartTimeBox.Enabled = false;
            mainPanel.Controls.Add(replayStartTimeBox, 1, 8);

            AddLabel(mainPanel, "结束时间", 9);
            replayEndTimeBox.Dock = DockStyle.Fill;
            replayEndTimeBox.Format = DateTimePickerFormat.Short;
            replayEndTimeBox.Enabled = false;
            mainPanel.Controls.Add(replayEndTimeBox, 1, 9);

            // 执行按钮
            var executePanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            replayStartButton.Text = "开始补录";
            replayStartButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            replayStartButton.ForeColor = System.Drawing.Color.White;
            executePanel.Controls.Add(replayStartButton);
            mainPanel.Controls.Add(executePanel, 1, 10);

            // 状态标签
            AddLabel(mainPanel, "状态", 11);
            replayStatusLabel.Dock = DockStyle.Fill;
            replayStatusLabel.Text = "就绪";
            mainPanel.Controls.Add(replayStatusLabel, 1, 11);

            // 日志框
            AddLabel(mainPanel, "补录日志", 12);
            replayLogBox.Dock = DockStyle.Fill;
            replayLogBox.Multiline = true;
            replayLogBox.ScrollBars = ScrollBars.Both;
            replayLogBox.ReadOnly = true;
            replayLogBox.Font = new System.Drawing.Font("Consolas", 9);
            mainPanel.Controls.Add(replayLogBox, 1, 12);

            for (var i = 0; i < 12; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        }

        private void BuildShardingTab(TabPage tab)
        {
            var logService = serviceProvider.GetService<LogService>();
            var shardingTaskService = new FastData.SyncTool.WinForms.Services.ShardingTaskService(logService);

            // 使用 TabControl 组织分表功能
            var shardingTabControl = new TabControl { Dock = DockStyle.Fill };

            // 配置管理标签页
            var configTab = new TabPage("分表配置");
            var configVisualizer = new FastData.SyncTool.WinForms.Components.ShardingConfigVisualizer
            {
                Dock = DockStyle.Fill
            };
            configTab.Controls.Add(configVisualizer);
            shardingTabControl.TabPages.Add(configTab);

            // 数据同步标签页
            var syncTab = new TabPage("数据同步");
            var shardingSyncControl = new FastData.SyncTool.WinForms.Components.ShardingSyncControl(shardingTaskService, logService)
            {
                Dock = DockStyle.Fill
            };
            syncTab.Controls.Add(shardingSyncControl);
            shardingTabControl.TabPages.Add(syncTab);

            // 数据操作标签页
            var dataTab = new TabPage("数据操作");
            var shardingDataControl = new FastData.SyncTool.WinForms.Components.ShardingDataControl
            {
                Dock = DockStyle.Fill
            };
            dataTab.Controls.Add(shardingDataControl);
            shardingTabControl.TabPages.Add(dataTab);

            tab.Controls.Add(shardingTabControl);
        }

        private void InitTaskGrid()
        {
            taskListGrid.Dock = DockStyle.Fill;
            taskListGrid.AllowUserToAddRows = false;
            taskListGrid.AllowUserToDeleteRows = false;
            taskListGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            taskListGrid.MultiSelect = true;

            taskListGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Select", HeaderText = "选择", Width = 50 });
            taskListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaskName", HeaderText = "任务名称", Width = 180 });
            taskListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "表名", Width = 150 });
            taskListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SyncColumns", HeaderText = "同步字段", Width = 200 });
            taskListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DataType", HeaderText = "数据类型", Width = 100 });
            taskListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TimeRange", HeaderText = "时间范围", Width = 100 });
            taskListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "状态", Width = 80 });
            taskListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastSyncTime", HeaderText = "最后同步时间", Width = 150 });
        }

        private void AddLabel(TableLayoutPanel panel, string text, int row)
        {
            var label = new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 5, 0, 5) };
            panel.Controls.Add(label, 0, row);
        }

        private void InitProviderBox(ComboBox box)
        {
            box.Items.AddRange(DatabaseProviderMappings.AllProviderNames);
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Dock = DockStyle.Fill;
        }

        private void InitTableGrid()
        {
            tableListGrid.Dock = DockStyle.Fill;
            tableListGrid.AllowUserToAddRows = false;
            tableListGrid.AllowUserToDeleteRows = false;
            tableListGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tableListGrid.MultiSelect = false;

            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "表名", Width = 200 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrimaryKeyColumns", HeaderText = "主键字段", Width = 120 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TimeColumn", HeaderText = "时间字段", Width = 120 });
            tableListGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "EnableTimeRange", HeaderText = "启用时间范围", Width = 80 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SyncColumns", HeaderText = "同步字段", Width = 150, ReadOnly = true });
            tableListGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsEnabled", HeaderText = "启用", Width = 50 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "状态", Width = 80, ReadOnly = true });
            tableListGrid.Columns.Add(new DataGridViewButtonColumn { Name = "SelectFields", HeaderText = "选择字段", Text = "选择", UseColumnTextForButtonValue = true, Width = 70 });
        }

        private void LoadTaskList()
        {
            try
            {
                var allTasks = configManager.GetAllTaskConfigs();
                taskListGrid.Rows.Clear();

                foreach (var task in allTasks)
                {
                    var timeRangeText = task.EnableTimeRange ? string.Format("最近{0}天", task.RangeDays) : "按主键增量";
                    var dataTypeText = task.DataType == SyncDataType.Dynamic ? "动态数据" : "静态数据";
                    var syncColumnsText = !string.IsNullOrEmpty(task.SyncColumns)
                        ? task.SyncColumns
                        : "(所有字段)";
                    var statusText = task.LastSyncStatus ?? "已配置";

                    taskListGrid.Rows.Add(
                        false,
                        task.TaskName,
                        task.SourceTable,
                        syncColumnsText,
                        dataTypeText,
                        timeRangeText,
                        statusText,
                        task.LastSyncTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未同步"
                    );
                }

                Log(string.Format("已加载 {0} 个任务配置", allTasks.Count));
            }
            catch (Exception ex)
            {
                Log("加载任务列表失败：" + ex.Message);
            }
        }

        private void BindEvents()
        {
            loadTablesButton.Text = "加载表列表";
            loadTablesButton.Click += async delegate { await LoadTablesFromDatabase(); };

            addTableButton.Text = "添加表";
            addTableButton.Click += delegate { AddTable(); };

            removeTableButton.Text = "移除表";
            removeTableButton.Click += delegate { RemoveTable(); };

            moveUpButton.Text = "上移";
            moveUpButton.Click += delegate { MoveTable(-1); };

            moveDownButton.Text = "下移";
            moveDownButton.Click += delegate { MoveTable(1); };

            syncButton.Text = "立即同步";
            syncButton.Click += delegate { ExecuteSync(); };

            exportSchemaButton.Text = "导出源库表结构到中间库";
            exportSchemaButton.Click += async delegate { await ExportSchema(); };

            pkConfigButton.Text = "主键配置管理";
            pkConfigButton.Click += delegate { OpenPkConfigDialog(); };

            saveTaskButton.Text = "保存任务配置";
            saveTaskButton.Click += delegate { SaveTask(); };

            newTaskButton.Text = "新建任务";
            newTaskButton.Click += delegate { CreateNewTask(); };

            loadTaskButton.Text = "加载任务";
            loadTaskButton.Click += delegate { LoadSelectedTask(); };

            editTaskButton.Text = "编辑任务";
            editTaskButton.Click += delegate { EditSelectedTask(); };

            refreshTaskButton.Text = "刷新列表";
            refreshTaskButton.Click += delegate { LoadTaskList(); };

            deleteTaskButton.Text = "删除任务";
            deleteTaskButton.Click += delegate { DeleteSelectedTask(); };

            batchDeleteButton.Text = "批量删除";
            batchDeleteButton.Click += delegate { BatchDeleteTasks(); };

            batchEnableButton.Text = "批量启用";
            batchEnableButton.Click += delegate { BatchEnableTasks(true); };

            batchDisableButton.Text = "批量禁用";
            batchDisableButton.Click += delegate { BatchEnableTasks(false); };

            exportTaskButton.Text = "导出配置";
            exportTaskButton.Click += delegate { ExportTaskConfig(); };

            importTaskButton.Text = "导入配置";
            importTaskButton.Click += delegate { ImportTaskConfig(); };

            tableListGrid.SelectionChanged += delegate { OnTableSelectionChanged(); };
            timeColumnBox.TextChanged += delegate { OnTimeColumnChanged(); };
            enableTimeRangeBox.CheckedChanged += delegate { OnEnableTimeRangeChanged(); };
            enableGlobalConfigBox.CheckedChanged += delegate { OnGlobalConfigChanged(); };
            globalRangeDaysBox.ValueChanged += delegate { OnGlobalConfigChanged(); };
            alwaysDeduplicateBox.CheckedChanged += delegate { OnGlobalConfigChanged(); };
            tableListGrid.CellContentClick += delegate (object s, DataGridViewCellEventArgs e) { OnCellContentClick(e.RowIndex); };

            // 补录功能事件
            replayLoadTablesButton.Click += async delegate { await ReplayLoadTables(); };
            replayStartButton.Click += async delegate { await ExecuteReplay(); };
            replayEnableTimeRangeBox.CheckedChanged += delegate { OnReplayTimeRangeChanged(); };

            // 数据库配置事件
            dbTestButton.Click += async delegate { await TestDatabaseConnection(); };
            dbSaveButton.Click += delegate { SaveDbConnection(); };
            dbDeleteButton.Click += delegate { DeleteDbConnection(); };
            dbConnectionGrid.SelectionChanged += delegate { OnDbConnectionSelected(); };
            
            // 共享日志事件
            clearLogButton.Click += delegate { sharedLogBox.Clear(); };
            exportLogButton.Click += delegate { ExportLog(); };
        }

        private void CreateNewTask()
        {
            var inputForm = new Form
            {
                Text = "新建任务",
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };

            var textBox = new TextBox { Left = 20, Top = 20, Width = 340, Text = "task_" + DateTime.Now.ToString("yyyyMMddHHmmss") };
            var okButton = new Button { Text = "确定", Left = 220, Top = 60, Width = 75, DialogResult = DialogResult.OK };
            var cancelButton = new Button { Text = "取消", Left = 300, Top = 60, Width = 75, DialogResult = DialogResult.Cancel };

            inputForm.Controls.Add(textBox);
            inputForm.Controls.Add(okButton);
            inputForm.Controls.Add(cancelButton);
            inputForm.AcceptButton = okButton;
            inputForm.CancelButton = cancelButton;

            if (inputForm.ShowDialog(this) == DialogResult.OK)
            {
                var taskName = textBox.Text;
                if (string.IsNullOrWhiteSpace(taskName))
                    return;

                var existingTask = taskConfigs.Find(t => t.TaskName == taskName);
                if (existingTask != null)
                {
                    MessageBox.Show("任务名称已存在");
                    return;
                }

                var newTask = new SyncTaskConfig
                {
                    TaskId = taskName,
                    TaskName = taskName,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now,
                    IsEnabled = true
                };

                taskConfigs.Add(newTask);
                configManager.SaveTaskConfig(newTask);
                LoadTaskList();

                MessageBox.Show("任务已创建，请在同步配置 Tab 页添加表并配置");
            }
        }

        private void LoadSelectedTask()
        {
            if (taskListGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个任务");
                return;
            }

            var row = taskListGrid.SelectedRows[0];
            var taskName = Convert.ToString(row.Cells["TaskName"].Value);

            LoadTaskToConfig(taskName);
            tabControl.SelectedIndex = 0;
        }

        private void EditSelectedTask()
        {
            if (taskListGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个任务");
                return;
            }

            var row = taskListGrid.SelectedRows[0];
            var taskName = Convert.ToString(row.Cells["TaskName"].Value);

            LoadTaskToConfig(taskName);
            tabControl.SelectedIndex = 0;

            MessageBox.Show("任务已加载到同步配置页，您可以修改后重新保存");
        }

        private void LoadTaskToConfig(string taskName)
        {
            try
            {
                var taskConfig = configManager.GetTaskConfig(taskName);

                taskIdBox.Text = taskConfig?.TaskName ?? taskName;
                sourceConnectionBox.Text = taskConfig?.SourceConnection ?? "";
                targetConnectionBox.Text = taskConfig?.TargetConnection ?? "";
                intermediateConnectionBox.Text = taskConfig?.IntermediateConnection ?? "";

                if (taskConfig?.TableConfigs != null)
                {
                    tableConfigs = new List<TableSyncConfig>(taskConfig.TableConfigs);
                    tableListGrid.Rows.Clear();

                    foreach (var config in tableConfigs)
                    {
                        var syncColumnsText = config.SyncColumns != null && config.SyncColumns.Count > 0
                            ? string.Join(",", config.SyncColumns)
                            : "(所有字段)";

                        tableListGrid.Rows.Add(
                            config.TableName,
                            config.PrimaryKeyColumns ?? "Id",
                            config.TimeColumn ?? "",
                            config.EnableTimeRange,
                            syncColumnsText,
                            config.IsEnabled,
                            "已配置",
                            "选择"
                        );
                    }

                    Log(string.Format("已加载任务 {0}，包含 {1} 个表", taskName, tableConfigs.Count));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载任务失败：" + ex.Message);
            }
        }

        private void DeleteSelectedTask()
        {
            if (taskListGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个任务");
                return;
            }

            var row = taskListGrid.SelectedRows[0];
            var taskName = Convert.ToString(row.Cells["TaskName"].Value);

            if (MessageBox.Show(string.Format("确认删除任务 {0}？", taskName), "确认删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                configManager.DeleteTaskConfig(taskName);
                LoadTaskList();
                Log(string.Format("任务 {0} 已删除", taskName));
            }
        }

        private void BatchDeleteTasks()
        {
            var selectedTasks = new List<string>();
            foreach (DataGridViewRow row in taskListGrid.Rows)
            {
                if (row.Cells["Select"].Value != null && Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    selectedTasks.Add(Convert.ToString(row.Cells["TaskName"].Value));
                }
            }

            if (selectedTasks.Count == 0)
            {
                MessageBox.Show("请先选择要删除的任务");
                return;
            }

            if (MessageBox.Show(string.Format("确认删除选中的 {0} 个任务？", selectedTasks.Count), "批量删除", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach (var taskName in selectedTasks)
                {
                    configManager.DeleteTaskConfig(taskName);
                }
                LoadTaskList();
                Log(string.Format("已批量删除 {0} 个任务", selectedTasks.Count));
            }
        }

        private void BatchEnableTasks(bool enable)
        {
            var selectedTasks = new List<string>();
            foreach (DataGridViewRow row in taskListGrid.Rows)
            {
                if (row.Cells["Select"].Value != null && Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    selectedTasks.Add(Convert.ToString(row.Cells["TaskName"].Value));
                }
            }

            if (selectedTasks.Count == 0)
            {
                MessageBox.Show("请先选择要操作的任务");
                return;
            }

            foreach (var taskName in selectedTasks)
            {
                var config = configManager.GetTaskConfig(taskName);
                if (config != null)
                {
                    foreach (var tableConfig in config.TableConfigs)
                    {
                        tableConfig.IsEnabled = enable;
                    }
                    configManager.SaveTaskConfig(config);
                }
            }

            LoadTaskList();
            Log(string.Format("已批量{0} {1} 个任务", enable ? "启用" : "禁用", selectedTasks.Count));
        }

        private void ExportTaskConfig()
        {
            if (taskListGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择一个任务");
                return;
            }

            var row = taskListGrid.SelectedRows[0];
            var taskName = Convert.ToString(row.Cells["TaskName"].Value);
            var config = configManager.GetTaskConfig(taskName);

            if (config == null)
            {
                MessageBox.Show("任务配置不存在");
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON 文件|*.json",
                FileName = string.Format("{0}_config.json", taskName)
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                var serializer = new JavaScriptSerializer();
                var json = serializer.Serialize(config);
                File.WriteAllText(saveDialog.FileName, json);
                Log(string.Format("任务配置已导出到 {0}", saveDialog.FileName));
                MessageBox.Show("导出成功");
            }
        }

        private void ImportTaskConfig()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON 文件|*.json"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var json = File.ReadAllText(openFileDialog.FileName);
                    var serializer = new JavaScriptSerializer();
                    var config = serializer.Deserialize<SyncTaskConfig>(json);

                    if (config != null && !string.IsNullOrEmpty(config.TaskId))
                    {
                        configManager.SaveTaskConfig(config);
                        LoadTaskList();
                        Log(string.Format("任务配置已从 {0} 导入", openFileDialog.FileName));
                        MessageBox.Show("导入成功");
                    }
                    else
                    {
                        MessageBox.Show("配置文件格式错误");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导入失败：" + ex.Message);
                }
            }
        }

        private void OnCellContentClick(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= tableConfigs.Count)
                return;

            if (tableListGrid.Columns[tableListGrid.CurrentCell.ColumnIndex].Name == "SelectFields")
            {
                OpenFieldSelector(rowIndex);
            }
        }

        private void OpenFieldSelector(int rowIndex)
        {
            var config = tableConfigs[rowIndex];
            var provider = Convert.ToString(sourceProviderBox.SelectedItem);
            var connectionString = sourceConnectionBox.Text;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show("请先填写源库连接字符串");
                return;
            }

            var form = new FieldSelectForm(provider, connectionString, config.TableName, config.SyncColumns);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                config.SyncColumns = form.SelectedColumns;
                var syncColumnsText = form.SelectedColumns.Count > 0
                    ? string.Join(",", form.SelectedColumns)
                    : "(所有字段)";
                tableListGrid.Rows[rowIndex].Cells["SyncColumns"].Value = syncColumnsText;
                Log(string.Format("已配置 {0} 表同步字段：{1}", config.TableName, syncColumnsText));
            }
        }

        private void SaveTask()
        {
            if (string.IsNullOrWhiteSpace(taskIdBox.Text))
            {
                MessageBox.Show("请输入任务 ID");
                return;
            }

            UpdateTableConfigsFromGrid();
            ValidatePrimaryKeySelection();

            var taskConfig = new SyncTaskConfig
            {
                TaskId = taskIdBox.Text,
                TaskName = taskIdBox.Text,
                SourceConnection = sourceConnectionBox.Text,
                TargetConnection = targetConnectionBox.Text,
                IntermediateConnection = intermediateConnectionBox.Text,
                TableConfigs = tableConfigs
            };

            configManager.SaveTaskConfig(taskConfig);

            MessageBox.Show("任务配置已保存到 sync_tasks.json");
        }

        private void AddTable()
        {
            var tableForm = new TableSelectForm(sourceProviderBox.Text, sourceConnectionBox.Text);
            if (tableForm.ShowDialog(this) == DialogResult.OK)
            {
                var selectedTables = tableForm.SelectedTables;
                foreach (var tableName in selectedTables)
                {
                    if (!TableExists(tableName))
                    {
                        int rowIndex = tableListGrid.Rows.Add(tableName, "Id", "", false, "", true, "待同步", "选择");
                        tableConfigs.Add(new TableSyncConfig
                        {
                            TableName = tableName,
                            PrimaryKeyColumns = "Id",
                            IsAutoIncrementKey = true,
                            EnableTimeRange = false,
                            IsEnabled = true,
                            SyncColumns = new List<string>(),
                            Status = TableSyncStatus.Pending,
                            DataType = SyncDataType.Static
                        });
                    }
                }
            }
        }

        private void RemoveTable()
        {
            for (int i = tableListGrid.SelectedRows.Count - 1; i >= 0; i--)
            {
                var row = tableListGrid.SelectedRows[i];
                var tableName = Convert.ToString(row.Cells["TableName"].Value);
                tableListGrid.Rows.Remove(row);
                tableConfigs.RemoveAll(c => c.TableName == tableName);
            }
        }

        private async System.Threading.Tasks.Task LoadTablesFromDatabase()
        {
            try
            {
                Log("正在加载表列表...");
                var provider = Convert.ToString(sourceProviderBox.SelectedItem);
                var connectionString = sourceConnectionBox.Text;

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    MessageBox.Show("请先填写源库连接字符串");
                    return;
                }

                using (var conn = DbProviderFactories.GetFactory(provider).CreateConnection())
                {
                    conn.ConnectionString = connectionString;
                    await conn.OpenAsync();

                    var tables = conn.GetSchema("Tables").Select();
                    tableListGrid.Rows.Clear();
                    tableConfigs.Clear();

                    foreach (var row in tables)
                    {
                        var tableName = row["TABLE_NAME"].ToString();
                        if (!tableName.StartsWith("fd_") && !tableName.StartsWith("sys"))
                        {
                            tableListGrid.Rows.Add(tableName, "Id", "", false, "", true, "待同步", "选择");
                            tableConfigs.Add(new TableSyncConfig
                            {
                                TableName = tableName,
                                PrimaryKeyColumns = "Id",
                                IsAutoIncrementKey = true,
                                EnableTimeRange = false,
                                IsEnabled = true,
                                SyncColumns = new List<string>(),
                                Status = TableSyncStatus.Pending,
                                DataType = SyncDataType.Static
                            });
                        }
                    }

                    Log(string.Format("已加载 {0} 个表", tables.Length));
                }
            }
            catch (Exception ex)
            {
                Log("加载表失败：" + ex.Message);
                MessageBox.Show(ex.Message);
            }
        }

        private void MoveTable(int direction)
        {
            if (tableListGrid.SelectedRows.Count != 1)
                return;

            var row = tableListGrid.SelectedRows[0];
            var index = row.Index;
            var newIndex = index + direction;

            if (newIndex < 0 || newIndex >= tableListGrid.Rows.Count)
                return;

            tableConfigs.Swap(index, newIndex);
            tableListGrid.Rows.RemoveAt(index);
            tableListGrid.Rows.Insert(newIndex, row);
            row.Selected = true;
        }

        private bool TableExists(string tableName)
        {
            foreach (var config in tableConfigs)
            {
                if (config.TableName == tableName)
                    return true;
            }
            return false;
        }

        private void OnTableSelectionChanged()
        {
            if (tableListGrid.SelectedRows.Count != 1)
                return;

            var row = tableListGrid.SelectedRows[0];
            var index = row.Index;
            if (index < 0 || index >= tableConfigs.Count)
                return;

            var config = tableConfigs[index];
            timeColumnBox.Text = config.TimeColumn ?? "";
            enableTimeRangeBox.Checked = config.EnableTimeRange;
            primaryKeyColumnsBox.Text = config.PrimaryKeyColumns ?? "Id";
            rangeDaysBox.Value = config.RangeDays;
            enableGlobalConfigBox.Checked = config.EnableGlobalConfig;
            globalRangeDaysBox.Value = config.GlobalRangeDays;
            alwaysDeduplicateBox.Checked = config.AlwaysDeduplicate;

            UpdateLastSyncTimeLabel(config);
        }

        private void OnTimeColumnChanged()
        {
            if (tableListGrid.SelectedRows.Count != 1)
                return;

            var index = tableListGrid.SelectedRows[0].Index;
            if (index < 0 || index >= tableConfigs.Count)
                return;

            tableConfigs[index].TimeColumn = timeColumnBox.Text;
            if (!string.IsNullOrEmpty(timeColumnBox.Text))
            {
                tableConfigs[index].DataType = SyncDataType.Dynamic;
            }
        }

        private void OnEnableTimeRangeChanged()
        {
            if (tableListGrid.SelectedRows.Count != 1)
                return;

            var index = tableListGrid.SelectedRows[0].Index;
            if (index < 0 || index >= tableConfigs.Count)
                return;

            tableConfigs[index].EnableTimeRange = enableTimeRangeBox.Checked;
        }

        private void OnGlobalConfigChanged()
        {
            if (tableListGrid.SelectedRows.Count != 1)
                return;

            var index = tableListGrid.SelectedRows[0].Index;
            if (index < 0 || index >= tableConfigs.Count)
                return;

            tableConfigs[index].EnableGlobalConfig = enableGlobalConfigBox.Checked;
            tableConfigs[index].GlobalRangeDays = (int)globalRangeDaysBox.Value;
            tableConfigs[index].AlwaysDeduplicate = alwaysDeduplicateBox.Checked;
        }

        private void UpdateLastSyncTimeLabel(TableSyncConfig config)
        {
            lastSyncTimeLabel.Text = config.LastSyncTime.HasValue
                ? "最后同步：" + config.LastSyncTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                : "尚未同步";
        }

        private void UpdateTableConfigsFromGrid()
        {
            for (int i = 0; i < tableListGrid.Rows.Count; i++)
            {
                var row = tableListGrid.Rows[i];
                tableConfigs[i].PrimaryKeyColumns = Convert.ToString(row.Cells["PrimaryKeyColumns"].Value);
                tableConfigs[i].TimeColumn = Convert.ToString(row.Cells["TimeColumn"].Value);
                tableConfigs[i].EnableTimeRange = Convert.ToBoolean(row.Cells["EnableTimeRange"].Value);
                tableConfigs[i].IsEnabled = Convert.ToBoolean(row.Cells["IsEnabled"].Value);
            }
        }

        private void ValidatePrimaryKeySelection()
        {
            foreach (var config in tableConfigs)
            {
                if (config.SyncColumns != null && config.SyncColumns.Count > 0)
                {
                    foreach (var pk in config.PrimaryKeyColumns.Split(','))
                    {
                        if (!config.SyncColumns.Contains(pk.Trim()))
                        {
                            MessageBox.Show(string.Format("表 {0} 的同步字段必须包含主键 {1}", config.TableName, pk));
                            throw new InvalidOperationException("主键字段不在同步字段列表中");
                        }
                    }
                }
            }
        }

        private void OpenPkConfigDialog()
        {
            new PrimaryKeyConfigForm(pkConfigService).ShowDialog(this);
        }

        private async System.Threading.Tasks.Task ExportSchema()
        {
            MessageBox.Show("导出表结构功能开发中");
        }

        private void InitializeTimer()
        {
            syncTimer = new System.Timers.Timer();
            syncTimer.Elapsed += OnTimerElapsed;
            syncTimer.AutoReset = false;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ExecuteSync));
            }
            else
            {
                ExecuteSync();
            }
        }

        private void UpdateSyncStatus()
        {
            syncStatusLabel.Text = isSyncing ? "同步中..." : "就绪";
            syncStatusLabel.BackColor = isSyncing ? System.Drawing.Color.Yellow : System.Drawing.Color.LightGray;
        }

        private void ExecuteSync()
        {
            if (isSyncing)
            {
                Log("同步正在进行中，请稍候...");
                return;
            }

            if (tableListGrid.Rows.Count == 0)
            {
                MessageBox.Show("请至少添加一个要同步的表");
                return;
            }

            try
            {
                isSyncing = true;
                UpdateSyncStatus();

                for (int i = 0; i < tableConfigs.Count; i++)
                {
                    var config = tableConfigs[i];
                    if (!config.IsEnabled)
                        continue;

                    var row = tableListGrid.Rows[i];
                    row.Cells["Status"].Value = "同步中";

                    var taskConfig = configManager.GetTaskConfig(taskIdBox.Text + "_" + config.TableName);

                    var syncOptions = new DataSyncOptions
                    {
                        SourceProvider = Convert.ToString(sourceProviderBox.SelectedItem),
                        SourceConnectionString = sourceConnectionBox.Text,
                        TargetProvider = Convert.ToString(targetProviderBox.SelectedItem),
                        TargetConnectionString = targetConnectionBox.Text,
                        IntermediateProvider = Convert.ToString(intermediateProviderBox.SelectedItem),
                        IntermediateConnectionString = intermediateConnectionBox.Text,
                        TaskId = taskIdBox.Text + "_" + config.TableName,
                        SourceTable = config.TableName,
                        TargetTable = config.TargetTableName ?? config.TableName,
                        PrimaryKeyColumns = config.PrimaryKeyColumns,
                        IsAutoIncrementKey = config.IsAutoIncrementKey,
                        TimeColumn = config.TimeColumn,
                        EnableTimeRange = config.EnableTimeRange,
                        IsFullSyncForFirstTime = true,
                        BatchSize = Convert.ToInt32(batchSizeBox.Value),
                        RetryCount = Convert.ToInt32(retryCountBox.Value),
                        AutoCreateIntermediateSchema = autoCreateIntermediateBox.Checked,
                        ResumeFailedRecords = resumeFailedRecordsBox.Checked,
                        CleanIntermediateData = cleanIntermediateBox.Checked,
                        RangeDays = config.RangeDays,
                        PrimaryKeyConfigService = pkConfigService,
                        ConfigManager = configManager
                    };

                    var syncService = serviceProvider.Resolve<DataSyncService>();
                    var result = syncService.SyncTable(syncOptions);

                    row.Cells["Status"].Value = result.FailedCount > 0 ? "部分失败" : "成功";
                    row.Cells["Status"].Style.ForeColor = result.FailedCount > 0 ? System.Drawing.Color.Red : System.Drawing.Color.Green;

                    var dataTypeText = config.DataType == SyncDataType.Dynamic ? "[动态数据]" : "[静态数据]";
                    var timeRangeText = config.EnableTimeRange ? string.Format("最近{0}天", config.RangeDays) : "按主键增量";

                    Log(string.Format("{4} {0} {1}，读取 {2} 条，写入 {3} 条，失败 {5} 条",
                        config.TableName,
                        dataTypeText + " " + timeRangeText,
                        result.ReadCount,
                        result.WriteCount,
                        result.LastSyncTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                        result.FailedCount));

                    config.LastSyncTime = result.LastSyncTime;
                    config.LastResultMessage = result.Message;
                    UpdateLastSyncTimeLabel(config);

                    configManager.UpdateLastSyncTime(taskIdBox.Text + "_" + config.TableName, result.LastSyncTime ?? DateTime.Now);
                    configManager.UpdateTaskStatus(taskIdBox.Text + "_" + config.TableName,
                        result.FailedCount > 0 ? "部分失败" : "成功",
                        string.Format("读取 {0} 条，写入 {1} 条", result.ReadCount, result.WriteCount));
                }

                SaveTask();
            }
            catch (Exception ex)
            {
                Log("同步失败：" + ex.Message);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                isSyncing = false;
                UpdateSyncStatus();
            }
        }

        #region 数据库配置方法

        private static DbConnection CreateDbConnection(string provider, string connectionString)
        {
            var factory = DbProviderFactories.GetFactory(provider);
            var connection = factory.CreateConnection();
            if (connection == null)
                throw new InvalidOperationException("无法创建数据库连接");
            connection.ConnectionString = connectionString;
            return connection;
        }

        private async Task TestDatabaseConnection()
        {
            try
            {
                dbTestButton.Enabled = false;
                dbTestButton.Text = "测试中...";
                DbLog("正在连接数据库...");

                var provider = dbProviderBox.Text;
                var connectionString = dbConnectionStringBox.Text;

                if (string.IsNullOrEmpty(connectionString))
                {
                    DbLog("错误：连接字符串为空");
                    MessageBox.Show("请输入连接字符串");
                    return;
                }

                using (var connection = CreateDbConnection(provider, connectionString))
                {
                    await connection.OpenAsync();
                    var serverVersion = connection.ServerVersion;
                    DbLog($"连接成功！服务器版本：{serverVersion}");
                    DbLog("测试完成：可用");
                }

                MessageBox.Show("数据库连接测试成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DbLog($"连接失败：{ex.Message}");
                MessageBox.Show($"数据库连接失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                dbTestButton.Enabled = true;
                dbTestButton.Text = "测试连接";
            }
        }

        private void SaveDbConnection()
        {
            try
            {
                var name = dbConnectionNameBox.Text.Trim();
                var provider = dbProviderBox.Text;
                var connectionString = dbConnectionStringBox.Text;

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("请输入连接名称");
                    return;
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    MessageBox.Show("请输入连接字符串");
                    return;
                }

                // 保存到配置文件
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_connections.json");
                var configs = LoadDbConnections();

                var existing = configs.FirstOrDefault(c => c.Name == name);
                if (existing != null)
                {
                    existing.Provider = provider;
                    existing.ConnectionString = connectionString;
                    existing.LastTestTime = DateTime.Now;
                    DbLog($"更新连接：{name}");
                }
                else
                {
                    configs.Add(new DbConnectionConfig
                    {
                        Name = name,
                        Provider = provider,
                        ConnectionString = connectionString,
                        CreatedTime = DateTime.Now,
                        LastTestTime = DateTime.Now
                    });
                    DbLog($"新建连接：{name}");
                }

                SaveDbConnections(configs);
                RefreshDbConnectionGrid();
                DbLog("连接配置已保存");
                MessageBox.Show($"连接 \"{name}\" 已保存", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DbLog($"保存失败：{ex.Message}");
                MessageBox.Show($"保存失败：{ex.Message}");
            }
        }

        private void DeleteDbConnection()
        {
            try
            {
                if (dbConnectionGrid.SelectedRows.Count == 0)
                {
                    MessageBox.Show("请先选择要删除的连接");
                    return;
                }

                var row = dbConnectionGrid.SelectedRows[0];
                var name = row.Cells["Name"].Value?.ToString();

                if (string.IsNullOrEmpty(name))
                    return;

                var configs = LoadDbConnections();
                var config = configs.FirstOrDefault(c => c.Name == name);
                if (config != null)
                {
                    configs.Remove(config);
                    SaveDbConnections(configs);
                    RefreshDbConnectionGrid();
                    DbLog($"删除连接：{name}");
                    MessageBox.Show($"连接 \"{name}\" 已删除", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                DbLog($"删除失败：{ex.Message}");
                MessageBox.Show($"删除失败：{ex.Message}");
            }
        }

        private void OnDbConnectionSelected()
        {
            if (dbConnectionGrid.SelectedRows.Count == 0)
                return;

            var row = dbConnectionGrid.SelectedRows[0];
            var name = row.Cells["Name"].Value?.ToString();

            if (string.IsNullOrEmpty(name))
                return;

            var configs = LoadDbConnections();
            var config = configs.FirstOrDefault(c => c.Name == name);
            if (config != null)
            {
                dbConnectionNameBox.Text = config.Name;
                dbProviderBox.Text = config.Provider;
                dbConnectionStringBox.Text = config.ConnectionString;
                DbLog($"加载连接配置：{name}");
            }
        }

        private List<DbConnectionConfig> LoadDbConnections()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_connections.json");
                if (File.Exists(configPath))
                {
                    var content = File.ReadAllText(configPath);
                    var serializer = new JavaScriptSerializer();
                    return serializer.Deserialize<List<DbConnectionConfig>>(content) ?? new List<DbConnectionConfig>();
                }
            }
            catch (Exception)
            {
                // 忽略错误
            }
            return new List<DbConnectionConfig>();
        }

        private void SaveDbConnections(List<DbConnectionConfig> configs)
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_connections.json");
            var serializer = new JavaScriptSerializer();
            var content = serializer.Serialize(configs);
            File.WriteAllText(configPath, content);
        }

        private void RefreshDbConnectionGrid()
        {
            dbConnectionGrid.Rows.Clear();
            var configs = LoadDbConnections();
            foreach (var config in configs)
            {
                dbConnectionGrid.Rows.Add(
                    config.Name,
                    config.Provider,
                    config.ConnectionString.Length > 50 ? config.ConnectionString.Substring(0, 50) + "..." : config.ConnectionString,
                    config.LastTestTime.ToString("yyyy-MM-dd HH:mm:ss")
                );
            }
        }

        private void DbLog(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] {message}{Environment.NewLine}";
            dbLogBox.AppendText(logLine);
            dbLogBox.ScrollToCaret();
        }

        #endregion

        #region 共享日志方法

        private void Log(string message)
        {
            LogInternal("INFO", message);
        }

        private void LogInfo(string message)
        {
            LogInternal("INFO", message);
        }

        private void LogWarn(string message)
        {
            LogInternal("WARN", message);
        }

        private void LogError(string message)
        {
            LogInternal("ERROR", message);
        }

        private void LogInternal(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
            sharedLogBox.AppendText(logLine);
            sharedLogBox.ScrollToCaret();
        }

        private void ReplayLog(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] [补录] {message}{Environment.NewLine}";
            replayLogBox.AppendText(logLine);
            replayLogBox.ScrollToCaret();
        }

        private void SharedLog(string message)
        {
            LogInternal("INFO", message);
        }

        private void ExportLog()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "日志文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    FileName = $"sync_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, sharedLogBox.Text);
                    MessageBox.Show($"日志已导出到：{saveDialog.FileName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出日志失败：{ex.Message}");
            }
        }

        #endregion

        private void OnReplayTimeRangeChanged()
        {
            replayStartTimeBox.Enabled = replayEnableTimeRangeBox.Checked;
            replayEndTimeBox.Enabled = replayEnableTimeRangeBox.Checked;
        }

        private async Task ReplayLoadTables()
        {
            try
            {
                var provider = replaySourceProviderBox.SelectedValue?.ToString() ?? replaySourceProviderBox.Text;
                var connectionString = replaySourceConnectionBox.Text;

                if (string.IsNullOrEmpty(connectionString))
                {
                    MessageBox.Show("请先输入源库连接字符串");
                    return;
                }

                ReplayLog("正在连接源库...");
                using (var connection = CreateDbConnection(provider, connectionString))
                {
                    await connection.OpenAsync();
                    ReplayLog("连接成功，正在加载表列表...");

                    var tables = connection.GetSchema("Tables");
                    ReplayLog($"加载到 {tables.Rows.Count} 个表");

                    if (tables.Rows.Count > 0)
                    {
                        var tableNames = new List<string>();
                        foreach (System.Data.DataRow row in tables.Rows)
                        {
                            var tableName = row["TABLE_NAME"].ToString();
                            if (!string.IsNullOrEmpty(tableName))
                                tableNames.Add(tableName);
                        }

                        if (tableNames.Count > 0)
                        {
                            replayTableNameBox.AutoCompleteCustomSource.Clear();
                            replayTableNameBox.AutoCompleteCustomSource.AddRange(tableNames.ToArray());
                            replayTableNameBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                            replayTableNameBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
                            ReplayLog($"已加载 {tableNames.Count} 个表名到自动完成");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReplayLog("加载失败：" + ex.Message);
                MessageBox.Show("加载表列表失败：" + ex.Message);
            }
        }

        private async Task ExecuteReplay()
        {
            try
            {
                replayStartButton.Enabled = false;
                replayStatusLabel.Text = "正在补录...";

                var sourceProvider = replaySourceProviderBox.SelectedValue?.ToString() ?? replaySourceProviderBox.Text;
                var targetProvider = replayTargetProviderBox.SelectedValue?.ToString() ?? replayTargetProviderBox.Text;
                var sourceConnection = replaySourceConnectionBox.Text;
                var targetConnection = replayTargetConnectionBox.Text;
                var tableName = replayTableNameBox.Text;
                var primaryKey = replayPrimaryKeyBox.Text;
                var enableTimeRange = replayEnableTimeRangeBox.Checked;
                var startTime = replayStartTimeBox.Value;
                var endTime = replayEndTimeBox.Value;

                if (string.IsNullOrEmpty(tableName))
                {
                    MessageBox.Show("请输入表名");
                    return;
                }

                ReplayLog($"开始补录表：{tableName}");
                ReplayLog($"源库：{sourceProvider}");
                ReplayLog($"目标库：{targetProvider}");

                var retryCount = 0;
                var maxRetries = 3;
                var retryDelay = TimeSpan.FromSeconds(5);

                while (retryCount < maxRetries)
                {
                    try
                    {
                        using (var sourceConn = CreateDbConnection(sourceProvider, sourceConnection))
                        using (var targetConn = CreateDbConnection(targetProvider, targetConnection))
                        {
                            ReplayLog("正在连接数据库...");
                            await sourceConn.OpenAsync();
                            await targetConn.OpenAsync();
                            ReplayLog("数据库连接成功");

                            var replayService = new ReplayService(sourceConn, targetConn, sourceProvider, targetProvider);
                            
                            var result = await replayService.ReplayTableAsync(
                                tableName,
                                primaryKey,
                                enableTimeRange,
                                enableTimeRange ? startTime : (DateTime?)null,
                                enableTimeRange ? endTime : (DateTime?)null,
                                (msg) => ReplayLog(msg)
                            );

                            ReplayLog($"补录完成：读取 {result.ReadCount} 条，更新 {result.UpdateCount} 条，插入 {result.InsertCount} 条，跳过 {result.SkipCount} 条");
                            replayStatusLabel.Text = "补录完成";
                            MessageBox.Show($"补录完成！\n读取：{result.ReadCount} 条\n更新：{result.UpdateCount} 条\n插入：{result.InsertCount} 条\n跳过：{result.SkipCount} 条");
                        }
                        break;
                    }
                    catch (System.Data.Common.DbException dbEx)
                    {
                        retryCount++;
                        ReplayLog($"数据库错误 (重试 {retryCount}/{maxRetries})：{dbEx.Message}");
                        
                        if (retryCount >= maxRetries)
                            throw;

                        ReplayLog($"等待 {retryDelay.TotalSeconds} 秒后重试...");
                        await Task.Delay(retryDelay);
                    }
                }
            }
            catch (Exception ex)
            {
                ReplayLog("补录失败：" + ex.Message);
                replayStatusLabel.Text = "补录失败";
                MessageBox.Show("补录失败：" + ex.Message);
            }
            finally
            {
                replayStartButton.Enabled = true;
                replayStatusLabel.Text = "就绪";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            syncTimer?.Stop();
            syncTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }

    }
