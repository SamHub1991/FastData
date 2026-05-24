using FastData.Tooling.Sync;
using System;
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
        private readonly TextBox sourceTableBox = new TextBox();
        private readonly TextBox targetTableBox = new TextBox();
        private readonly TextBox primaryKeyColumnsBox = new TextBox();
        private readonly TextBox incrementalColumnBox = new TextBox();
        private readonly TextBox lastValueBox = new TextBox();
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

        public MainForm()
        {
            Text = "FastData 数据同步工具";
            Width = 1000;
            Height = 720;
            BuildLayout();
            BindEvents();
            InitializeTimer();
        }

        private void BuildLayout()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 22 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(panel);

            AddLabel(panel, "源库 Provider", 0);
            InitProviderBox(sourceProviderBox);
            panel.Controls.Add(sourceProviderBox, 1, 0);

            AddLabel(panel, "源库连接字符串", 1);
            sourceConnectionBox.Dock = DockStyle.Fill;
            panel.Controls.Add(sourceConnectionBox, 1, 1);

            AddLabel(panel, "目标库 Provider", 2);
            InitProviderBox(targetProviderBox);
            panel.Controls.Add(targetProviderBox, 1, 2);

            AddLabel(panel, "目标库连接字符串", 3);
            targetConnectionBox.Dock = DockStyle.Fill;
            panel.Controls.Add(targetConnectionBox, 1, 3);

            AddLabel(panel, "中间库 Provider", 4);
            InitProviderBox(intermediateProviderBox);
            panel.Controls.Add(intermediateProviderBox, 1, 4);

            AddLabel(panel, "中间库连接字符串", 5);
            intermediateConnectionBox.Dock = DockStyle.Fill;
            panel.Controls.Add(intermediateConnectionBox, 1, 5);

            AddLabel(panel, "同步任务 ID", 6);
            taskIdBox.Dock = DockStyle.Fill;
            panel.Controls.Add(taskIdBox, 1, 6);

            AddLabel(panel, "源表", 7);
            sourceTableBox.Dock = DockStyle.Fill;
            panel.Controls.Add(sourceTableBox, 1, 7);

            AddLabel(panel, "目标表", 8);
            targetTableBox.Dock = DockStyle.Fill;
            panel.Controls.Add(targetTableBox, 1, 8);

            AddLabel(panel, "主键字段 (逗号分隔)", 9);
            primaryKeyColumnsBox.Dock = DockStyle.Fill;
            panel.Controls.Add(primaryKeyColumnsBox, 1, 9);

            AddLabel(panel, "增量字段", 10);
            incrementalColumnBox.Dock = DockStyle.Fill;
            panel.Controls.Add(incrementalColumnBox, 1, 10);

            AddLabel(panel, "增量起点", 11);
            lastValueBox.Dock = DockStyle.Fill;
            panel.Controls.Add(lastValueBox, 1, 11);

            AddLabel(panel, "批量大小", 12);
            batchSizeBox.Minimum = 1;
            batchSizeBox.Maximum = 100000;
            batchSizeBox.Value = 500;
            panel.Controls.Add(batchSizeBox, 1, 12);

            AddLabel(panel, "失败重试次数", 13);
            retryCountBox.Minimum = 0;
            retryCountBox.Maximum = 100;
            retryCountBox.Value = 1;
            panel.Controls.Add(retryCountBox, 1, 13);

            AddLabel(panel, "定时同步间隔 (秒)", 14);
            intervalBox.Minimum = 5;
            intervalBox.Maximum = 3600;
            intervalBox.Value = 30;
            panel.Controls.Add(intervalBox, 1, 14);

            AddLabel(panel, "自动创建中间库表", 15);
            autoCreateIntermediateBox.Dock = DockStyle.Fill;
            panel.Controls.Add(autoCreateIntermediateBox, 1, 15);

            AddLabel(panel, "恢复失败记录", 16);
            resumeFailedRecordsBox.Dock = DockStyle.Fill;
            panel.Controls.Add(resumeFailedRecordsBox, 1, 16);

            AddLabel(panel, "清理中间库成功记录", 17);
            cleanIntermediateBox.Dock = DockStyle.Fill;
            panel.Controls.Add(cleanIntermediateBox, 1, 17);

            AddLabel(panel, "启用定时同步", 18);
            enableTimerBox.Dock = DockStyle.Fill;
            panel.Controls.Add(enableTimerBox, 1, 18);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            exportSchemaButton.Text = "导出中间库 SQL";
            syncButton.Text = "执行同步";
            pkConfigButton.Text = "主键配置";
            buttonPanel.Controls.Add(exportSchemaButton);
            buttonPanel.Controls.Add(syncButton);
            buttonPanel.Controls.Add(pkConfigButton);
            panel.Controls.Add(buttonPanel, 1, 19);

            var statusPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            syncStatusLabel.Text = "状态：就绪";
            syncStatusLabel.AutoSize = true;
            syncStatusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10, System.Drawing.FontStyle.Bold);
            statusPanel.Controls.Add(syncStatusLabel);
            panel.Controls.Add(statusPanel, 1, 20);

            AddLabel(panel, "运行日志", 21);
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Both;
            logBox.Font = new System.Drawing.Font("Consolas", 10);
            panel.Controls.Add(logBox, 1, 21);

            for (var i = 0; i < 21; i++)
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
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

                var result = new DataSyncService().SyncTable(new DataSyncOptions
                {
                    SourceProvider = Convert.ToString(sourceProviderBox.SelectedItem),
                    SourceConnectionString = sourceConnectionBox.Text,
                    TargetProvider = Convert.ToString(targetProviderBox.SelectedItem),
                    TargetConnectionString = targetConnectionBox.Text,
                    IntermediateProvider = Convert.ToString(intermediateProviderBox.SelectedItem),
                    IntermediateConnectionString = intermediateConnectionBox.Text,
                    TaskId = taskIdBox.Text,
                    SourceTable = sourceTableBox.Text,
                    TargetTable = string.IsNullOrEmpty(targetTableBox.Text) ? sourceTableBox.Text : targetTableBox.Text,
                    PrimaryKeyColumns = primaryKeyColumnsBox.Text,
                    IsAutoIncrementKey = !string.IsNullOrEmpty(primaryKeyColumnsBox.Text) && primaryKeyColumnsBox.Text.Split(',').Length == 1,
                    IncrementalColumn = incrementalColumnBox.Text,
                    LastValue = lastValueBox.Text,
                    BatchSize = Convert.ToInt32(batchSizeBox.Value),
                    RetryCount = Convert.ToInt32(retryCountBox.Value),
                    AutoCreateIntermediateSchema = autoCreateIntermediateBox.Checked,
                    ResumeFailedRecords = resumeFailedRecordsBox.Checked,
                    CleanIntermediateData = cleanIntermediateBox.Checked,
                    EnableTimer = enableTimerBox.Checked,
                    SyncIntervalSeconds = Convert.ToInt32(intervalBox.Value),
                    PrimaryKeyConfigService = pkConfigService
                });

                Log(string.Format("{0}，读取 {1} 条，写入 {2} 条，失败 {3} 条，重试 {4} 次，恢复 {5} 条",
                    result.Message, result.ReadCount, result.WriteCount, result.FailedCount, result.RetryCount, result.RecoveredCount));
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
