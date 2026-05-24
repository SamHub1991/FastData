using FastData.Tooling.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Windows.Forms;

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
        
        private readonly RadioButton smartRangeRadio = new RadioButton();
        private readonly RadioButton manualRangeRadio = new RadioButton();
        private readonly RadioButton fullSyncRadio = new RadioButton();
        private readonly DateTimePicker startTimePicker = new DateTimePicker();
        private readonly DateTimePicker endTimePicker = new DateTimePicker();
        private readonly NumericUpDown rangeDaysBox = new NumericUpDown();
        private readonly Label lastSyncTimeLabel = new Label();
        private readonly Button quickSelectButton = new Button();
        
        private readonly TextBox primaryKeyColumnsBox = new TextBox();
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
        
        private System.Timers.Timer syncTimer;
        private bool isSyncing;
        private readonly PrimaryKeyConfigService pkConfigService = new PrimaryKeyConfigService();
        private readonly SyncConfigManager configManager;
        private System.Collections.Generic.IList<TableSyncConfig> tableConfigs = new List<TableSyncConfig>();

        public MainForm()
        {
            Text = "FastData 数据同步工具";
            Width = 1100;
            Height = 800;
            configManager = new SyncConfigManager();
            BuildLayout();
            BindEvents();
            InitializeTimer();
            LoadLastSyncTime();
        }

        private void BuildLayout()
        {
            var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 24 };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(mainPanel);

            int row = 0;

            // 数据库配置
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

            AddLabel(mainPanel, "同步任务 ID", row++);
            taskIdBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(taskIdBox, 1, row - 1);

            // 表配置列表
            AddLabel(mainPanel, "同步表配置", row++);
            var tablePanel = new Panel { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(tablePanel, 1, row - 1);
            BuildTablePanel(tablePanel);

            // 时间范围配置
            AddLabel(mainPanel, "同步范围", row++);
            var rangePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            mainPanel.Controls.Add(rangePanel, 1, row - 1);
            BuildRangePanel(rangePanel);

            // 高级选项
            AddLabel(mainPanel, "主键字段", row++);
            primaryKeyColumnsBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(primaryKeyColumnsBox, 1, row - 1);

            AddLabel(mainPanel, "批量大小", row++);
            batchSizeBox.Minimum = 1;
            batchSizeBox.Maximum = 100000;
            batchSizeBox.Value = 500;
            mainPanel.Controls.Add(batchSizeBox, 1, row - 1);

            AddLabel(mainPanel, "失败重试次数", row++);
            retryCountBox.Minimum = 0;
            retryCountBox.Maximum = 100;
            retryCountBox.Value = 1;
            mainPanel.Controls.Add(retryCountBox, 1, row - 1);

            AddLabel(mainPanel, "定时同步间隔 (秒)", row++);
            intervalBox.Minimum = 5;
            intervalBox.Maximum = 3600;
            intervalBox.Value = 30;
            mainPanel.Controls.Add(intervalBox, 1, row - 1);

            // 复选框
            AddLabel(mainPanel, "自动创建中间库表", row++);
            autoCreateIntermediateBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(autoCreateIntermediateBox, 1, row - 1);

            AddLabel(mainPanel, "恢复失败记录", row++);
            resumeFailedRecordsBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(resumeFailedRecordsBox, 1, row - 1);

            AddLabel(mainPanel, "清理中间库成功记录", row++);
            cleanIntermediateBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(cleanIntermediateBox, 1, row - 1);

            AddLabel(mainPanel, "启用定时同步", row++);
            enableTimerBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(enableTimerBox, 1, row - 1);

            // 按钮
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            exportSchemaButton.Text = "导出中间库 SQL";
            syncButton.Text = "开始同步";
            pkConfigButton.Text = "主键配置";
            buttonPanel.Controls.Add(exportSchemaButton);
            buttonPanel.Controls.Add(syncButton);
            buttonPanel.Controls.Add(pkConfigButton);
            mainPanel.Controls.Add(buttonPanel, 1, row++);

            // 状态
            var statusPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            syncStatusLabel.Text = "状态：就绪";
            syncStatusLabel.AutoSize = true;
            syncStatusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10, System.Drawing.FontStyle.Bold);
            statusPanel.Controls.Add(syncStatusLabel);
            mainPanel.Controls.Add(statusPanel, 1, row++);

            // 日志
            AddLabel(mainPanel, "运行日志", row++);
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Both;
            logBox.Font = new System.Drawing.Font("Consolas", 10);
            mainPanel.Controls.Add(logBox, 1, row);

            for (var i = 0; i < 23; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        }

        private void BuildTablePanel(Panel panel)
        {
            var tableLayoutPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            panel.Controls.Add(tableLayoutPanel);

            tableListGrid.Dock = DockStyle.Fill;
            tableListGrid.AllowUserToAddRows = false;
            tableListGrid.MultiSelect = true;
            tableListGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "表名", Width = 150 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrimaryKey", HeaderText = "主键字段", Width = 120 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "IncrementalColumn", HeaderText = "增量字段", Width = 120 });
            tableListGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "启用", Width = 50 });
            tableListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "状态", Width = 80 });
            tableLayoutPanel.Controls.Add(tableListGrid, 0, 0);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            addTableButton.Text = "添加表";
            removeTableButton.Text = "删除";
            loadTablesButton.Text = "从数据库加载";
            moveUpButton.Text = "上移";
            moveDownButton.Text = "下移";
            buttonPanel.Controls.Add(addTableButton);
            buttonPanel.Controls.Add(removeTableButton);
            buttonPanel.Controls.Add(loadTablesButton);
            buttonPanel.Controls.Add(moveUpButton);
            buttonPanel.Controls.Add(moveDownButton);
            tableLayoutPanel.Controls.Add(buttonPanel, 0, 1);
        }

        private void BuildRangePanel(FlowLayoutPanel panel)
        {
            smartRangeRadio.Text = "智能范围";
            smartRangeRadio.AutoSize = true;
            smartRangeRadio.Checked = true;
            manualRangeRadio.Text = "手动范围";
            manualRangeRadio.AutoSize = true;
            fullSyncRadio.Text = "全量同步";
            fullSyncRadio.AutoSize = true;

            var rangeLabel = new Label { Text = "范围天数:", AutoSize = true };
            rangeDaysBox.Minimum = 1;
            rangeDaysBox.Maximum = 365;
            rangeDaysBox.Value = 3;
            rangeDaysBox.Width = 50;

            lastSyncTimeLabel.Text = "上次同步：从未";
            lastSyncTimeLabel.AutoSize = true;

            startTimePicker.Format = DateTimePickerFormat.Custom;
            startTimePicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            startTimePicker.Width = 150;
            endTimePicker.Format = DateTimePickerFormat.Custom;
            endTimePicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            endTimePicker.Width = 150;

            quickSelectButton.Text = "快速选择";
            quickSelectButton.Width = 80;

            startTimePicker.Visible = false;
            endTimePicker.Visible = false;

            panel.Controls.Add(smartRangeRadio);
            panel.Controls.Add(manualRangeRadio);
            panel.Controls.Add(fullSyncRadio);
            panel.Controls.Add(rangeLabel);
            panel.Controls.Add(rangeDaysBox);
            panel.Controls.Add(lastSyncTimeLabel);
            panel.Controls.Add(startTimePicker);
            panel.Controls.Add(endTimePicker);
            panel.Controls.Add(quickSelectButton);
        }

        private static void InitProviderBox(ComboBox box)
        {
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Items.AddRange(new object[] { "System.Data.SqlClient", "MySql.Data.MySqlClient", "Oracle.ManagedDataAccess.Client" });
            box.SelectedIndex = 0;
            box.Dock = DockStyle.Fill;
        }

        private static void AddLabel(TableLayoutPanel panel, string text, int row)
        {
            panel.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight }, 0, row);
        }

        private void BindEvents()
        {
            exportSchemaButton.Click += delegate { ExportSchema(); };
            syncButton.Click += delegate { ToggleSync(); };
            pkConfigButton.Click += delegate { OpenPkConfig(); };
            enableTimerBox.CheckedChanged += delegate { UpdateSyncStatus(); };
            
            addTableButton.Click += delegate { AddTable(); };
            removeTableButton.Click += delegate { RemoveTable(); };
            loadTablesButton.Click += async delegate { await LoadTablesFromDatabase(); };
            moveUpButton.Click += delegate { MoveTable(-1); };
            moveDownButton.Click += delegate { MoveTable(1); };
            
            smartRangeRadio.CheckedChanged += delegate { UpdateRangeUI(); };
            manualRangeRadio.CheckedChanged += delegate { UpdateRangeUI(); };
            fullSyncRadio.CheckedChanged += delegate { UpdateRangeUI(); };
            quickSelectButton.Click += delegate { ShowQuickSelectMenu(); };
        }

        private void InitializeTimer()
        {
            syncTimer = new System.Timers.Timer();
            syncTimer.Elapsed += OnTimerElapsed;
            syncTimer.AutoReset = true;
            syncTimer.Enabled = false;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!enableTimerBox.Checked || isSyncing)
                return;

            Invoke(new Action(() => ExecuteSync()));
        }

        private void ToggleSync()
        {
            if (enableTimerBox.Checked)
            {
                if (syncTimer.Enabled)
                {
                    syncTimer.Stop();
                    UpdateSyncStatus();
                    Log("定时同步已停止");
                }
                else
                {
                    syncTimer.Interval = Convert.ToDouble(intervalBox.Value) * 1000;
                    syncTimer.Start();
                    UpdateSyncStatus();
                    Log(string.Format("定时同步已启动，间隔 {0} 秒", intervalBox.Value));
                    ExecuteSync();
                }
            }
            else
            {
                ExecuteSync();
            }
        }

        private void UpdateSyncStatus()
        {
            if (isSyncing)
            {
                syncStatusLabel.Text = "状态：同步中...";
                syncStatusLabel.ForeColor = System.Drawing.Color.Orange;
            }
            else if (enableTimerBox.Checked && syncTimer.Enabled)
            {
                syncStatusLabel.Text = string.Format("状态：定时同步中 (每{0}秒)", intervalBox.Value);
                syncStatusLabel.ForeColor = System.Drawing.Color.Green;
            }
            else if (enableTimerBox.Checked && !syncTimer.Enabled)
            {
                syncStatusLabel.Text = "状态：定时同步已暂停";
                syncStatusLabel.ForeColor = System.Drawing.Color.Gray;
            }
            else
            {
                syncStatusLabel.Text = "状态：就绪";
                syncStatusLabel.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void OpenPkConfig()
        {
            var form = new PrimaryKeyConfigForm(pkConfigService);
            form.ShowDialog(this);
        }

        private void ExportSchema()
        {
            var script = new IntermediateSchemaBuilder().BuildScript(Convert.ToString(intermediateProviderBox.SelectedItem));
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fd_intermediate_schema.sql");
            File.WriteAllText(path, script);
            Log("已导出中间库 SQL: " + path);
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
                        tableListGrid.Rows.Add(tableName, "Id", "", true, "待同步");
                        tableConfigs.Add(new TableSyncConfig
                        {
                            TableName = tableName,
                            PrimaryKeyColumns = "Id",
                            IsEnabled = true,
                            Status = TableSyncStatus.Pending
                        });
                    }
                }
            }
        }

        private void RemoveTable()
        {
            foreach (DataGridViewRow row in tableListGrid.SelectedRows)
            {
                tableListGrid.Rows.Remove(row);
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

                using (var conn = System.Data.Common.DbProviderFactories.GetFactory(provider).CreateConnection())
                {
                    conn.ConnectionString = connectionString;
                    await conn.OpenAsync();
                    
                    var tables = conn.GetSchema("Tables").Select();
                    tableListGrid.Rows.Clear();
                    tableConfigs.Clear();

                    foreach (var row in tables)
                    {
                        var tableName = row["TABLE_NAME"].ToString();
                        if (!tableName.StartsWith("fd_"))
                        {
                            tableListGrid.Rows.Add(tableName, "Id", "", true, "待同步");
                            tableConfigs.Add(new TableSyncConfig
                            {
                                TableName = tableName,
                                PrimaryKeyColumns = "Id",
                                IsEnabled = true,
                                Status = TableSyncStatus.Pending
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
            if (tableListGrid.SelectedRows.Count == 0)
                return;

            var index = tableListGrid.SelectedRows[0].Index;
            var newIndex = index + direction;

            if (newIndex >= 0 && newIndex < tableListGrid.Rows.Count)
            {
                var row = tableListGrid.Rows[index];
                tableListGrid.Rows.RemoveAt(index);
                tableListGrid.Rows.Insert(newIndex, row);
                tableListGrid.ClearSelection();
                tableListGrid.Rows[newIndex].Selected = true;
            }
        }

        private bool TableExists(string tableName)
        {
            foreach (DataGridViewRow row in tableListGrid.Rows)
            {
                if (Convert.ToString(row.Cells["TableName"].Value) == tableName)
                    return true;
            }
            return false;
        }

        private void UpdateRangeUI()
        {
            bool isManual = manualRangeRadio.Checked;
            startTimePicker.Visible = isManual;
            endTimePicker.Visible = isManual;
            rangeDaysBox.Visible = smartRangeRadio.Checked;
        }

        private void ShowQuickSelectMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("最近 1 天", null, (s, e) => ApplyQuickSelect(PresetRangeType.Last1Day));
            menu.Items.Add("最近 3 天", null, (s, e) => ApplyQuickSelect(PresetRangeType.Last3Days));
            menu.Items.Add("最近 7 天", null, (s, e) => ApplyQuickSelect(PresetRangeType.Last7Days));
            menu.Items.Add("最近 30 天", null, (s, e) => ApplyQuickSelect(PresetRangeType.Last30Days));
            menu.Items.Add("本月", null, (s, e) => ApplyQuickSelect(PresetRangeType.ThisMonth));
            menu.Items.Add("上月", null, (s, e) => ApplyQuickSelect(PresetRangeType.LastMonth));
            menu.Show(quickSelectButton, new System.Drawing.Point(0, quickSelectButton.Height));
        }

        private void ApplyQuickSelect(PresetRangeType preset)
        {
            var range = TimeRangeCalculator.GetPresetRange(preset);
            startTimePicker.Value = range.Item1;
            endTimePicker.Value = range.Item2;
            manualRangeRadio.Checked = true;
            Log(string.Format("已选择时间范围：{0:yyyy-MM-dd} 至 {1:yyyy-MM-dd}", range.Item1, range.Item2));
        }

        private void LoadLastSyncTime()
        {
            var taskId = taskIdBox.Text;
            if (!string.IsNullOrEmpty(taskId))
            {
                var config = configManager.GetTaskConfig(taskId);
                if (config != null && config.LastSyncTime.HasValue)
                {
                    lastSyncTimeLabel.Text = string.Format("上次同步：{0:yyyy-MM-dd HH:mm:ss}", config.LastSyncTime.Value);
                }
            }
        }

        private void ExecuteSync()
        {
            if (isSyncing)
            {
                Log("同步正在进行中，请稍候...");
                return;
            }

            try
            {
                isSyncing = true;
                UpdateSyncStatus();

                if (tableListGrid.Rows.Count == 0)
                {
                    MessageBox.Show("请至少添加一个要同步的表");
                    return;
                }

                var syncMode = smartRangeRadio.Checked ? SyncMode.Smart 
                    : manualRangeRadio.Checked ? SyncMode.Manual : SyncMode.Full;

                foreach (DataGridViewRow row in tableListGrid.Rows)
                {
                    var tableName = Convert.ToString(row.Cells["TableName"].Value);
                    var pkColumns = Convert.ToString(row.Cells["PrimaryKey"].Value);
                    var incrementalColumn = Convert.ToString(row.Cells["IncrementalColumn"].Value);
                    var isEnabled = Convert.ToBoolean(row.Cells["Enabled"].Value);

                    if (!isEnabled)
                        continue;

                    row.Cells["Status"].Value = "同步中";

                    var taskConfig = configManager.GetTaskConfig(taskIdBox.Text + "_" + tableName);
                    var syncOptions = new DataSyncOptions
                    {
                        SourceProvider = Convert.ToString(sourceProviderBox.SelectedItem),
                        SourceConnectionString = sourceConnectionBox.Text,
                        TargetProvider = Convert.ToString(targetProviderBox.SelectedItem),
                        TargetConnectionString = targetConnectionBox.Text,
                        IntermediateProvider = Convert.ToString(intermediateProviderBox.SelectedItem),
                        IntermediateConnectionString = intermediateConnectionBox.Text,
                        TaskId = taskIdBox.Text + "_" + tableName,
                        SourceTable = tableName,
                        TargetTable = tableName,
                        PrimaryKeyColumns = pkColumns,
                        IsAutoIncrementKey = !string.IsNullOrEmpty(pkColumns) && pkColumns.Split(',').Length == 1,
                        IncrementalColumn = incrementalColumn,
                        BatchSize = Convert.ToInt32(batchSizeBox.Value),
                        RetryCount = Convert.ToInt32(retryCountBox.Value),
                        AutoCreateIntermediateSchema = autoCreateIntermediateBox.Checked,
                        ResumeFailedRecords = resumeFailedRecordsBox.Checked,
                        CleanIntermediateData = cleanIntermediateBox.Checked,
                        SyncMode = syncMode,
                        IsFirstSync = taskConfig == null || taskConfig.IsFirstSync,
                        RangeDays = Convert.ToInt32(rangeDaysBox.Value),
                        LastSyncTime = taskConfig?.LastSyncTime,
                        StartTime = manualRangeRadio.Checked ? startTimePicker.Value : (DateTime?)null,
                        EndTime = manualRangeRadio.Checked ? endTimePicker.Value : (DateTime?)null,
                        PrimaryKeyConfigService = pkConfigService,
                        ConfigManager = configManager
                    };

                    var result = new DataSyncService().SyncTable(syncOptions);

                    row.Cells["Status"].Value = result.FailedCount > 0 ? "部分失败" : "成功";
                    row.Cells["Status"].Style.ForeColor = result.FailedCount > 0 ? System.Drawing.Color.Red : System.Drawing.Color.Green;

                    Log(string.Format("[{2}] {0}，读取 {1} 条，写入 {3} 条，失败 {4} 条",
                        tableName, result.Message, result.LastSyncTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                        result.WriteCount, result.FailedCount));
                }

                LoadLastSyncTime();
            }
            catch (Exception ex)
            {
                Log("同步失败：" + ex.Message);
            }
            finally
            {
                isSyncing = false;
                UpdateSyncStatus();
            }
        }

        private void Log(string message)
        {
            logBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
        }
    }
}
