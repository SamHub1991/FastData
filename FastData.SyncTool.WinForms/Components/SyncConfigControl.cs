using System;
using System.Windows.Forms;
using FastData.Database;
using FastData.SyncTool.WinForms.IoC;
using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 同步配置 UserControl
    /// </summary>
    public class SyncConfigControl : UserControl
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

        private readonly Button exportSchemaButton = new Button();
        private readonly Button syncButton = new Button();
        private readonly Button pkConfigButton = new Button();
        private readonly Button saveTaskButton = new Button();

        private readonly PrimaryKeyConfigService pkConfigService;
        private readonly SyncConfigManager configManager;

        public SyncConfigControl(PrimaryKeyConfigService pkConfigService, SyncConfigManager configManager)
        {
            this.pkConfigService = pkConfigService;
            this.configManager = configManager;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 23
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

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

            Controls.Add(mainPanel);
        }

        private void AddLabel(TableLayoutPanel panel, string text, int row)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(5, 5, 0, 0)
            };
            panel.Controls.Add(label, 0, row);
        }

        private void InitProviderBox(ComboBox box)
        {
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Dock = DockStyle.Fill;
            box.Items.AddRange(DatabaseProviderMappings.AllProviderNames);
            box.SelectedIndex = 0;
        }

        private void InitTableGrid()
        {
            tableListGrid.AllowUserToAddRows = false;
            tableListGrid.AllowUserToDeleteRows = false;
            tableListGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tableListGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            tableListGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsEnabled", HeaderText = "启用", Width = 50 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "源表名", Width = 150 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TargetTableName", HeaderText = "目标表名", Width = 150 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrimaryKeyColumns", HeaderText = "主键字段", Width = 100 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TimeColumn", HeaderText = "时间字段", Width = 100 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "状态", Width = 80 });
        }
    }
}
