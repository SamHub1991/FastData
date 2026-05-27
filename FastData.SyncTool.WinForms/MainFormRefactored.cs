using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FastData.Tooling.Sync;
using FastData.SyncTool.WinForms.Services;

namespace FastData.SyncTool.WinForms
{
    public class MainFormRefactored : Form
    {
        private readonly MenuStrip menuStrip = new MenuStrip();
        private readonly TabControl tabControl = new TabControl();
        private readonly StatusStrip statusStrip = new StatusStrip();
        private readonly ToolStripStatusLabel statusLabel = new ToolStripStatusLabel();
        private readonly ToolStripProgressBar progressBar = new ToolStripProgressBar();
        private readonly ToolStripStatusLabel progressText = new ToolStripStatusLabel();

        private RichTextBox logBox;
        private ComboBox logLevelFilter;
        private Button clearLogButton;
        private Button exportLogButton;

        private DbConnectionPanel connectionPanel;
        private TableListManager tableListManager;
        private SyncConfigPanel syncConfigPanel;
        private TaskManager taskManager;

        private readonly SyncConfigManager configManager;
        private readonly SyncService syncService;
        private readonly LogService logService;
        private readonly PrimaryKeyConfigService pkConfigService;
        private readonly TaskSchedulerService schedulerService;
        private string currentTaskId;
        private bool isSyncing;

        public MainFormRefactored()
        {
            configManager = new SyncConfigManager();
            syncService = new SyncService();
            logService = new LogService();
            pkConfigService = new PrimaryKeyConfigService();
            schedulerService = new TaskSchedulerService();

            InitWindow();
            InitMenu();
            InitTabs();
            InitComponents();
            BindEvents();

            logService.Info("FastData 同步工具已启动");
        }

        private void InitWindow()
        {
            Text = "FastData 同步工具 v2.0";
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 600);
        }

        private void InitMenu()
        {
            var fileMenu = new ToolStripMenuItem("文件 (&F)");
            fileMenu.DropDownItems.Add("新建任务", null, (s, e) => CreateNewTask());
            fileMenu.DropDownItems.Add("保存任务", null, (s, e) => SaveCurrentTask());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("退出", null, (s, e) => Close());

            var syncMenu = new ToolStripMenuItem("同步 (&S)");
            syncMenu.DropDownItems.Add("开始同步", null, (s, e) => StartSync());
            syncMenu.DropDownItems.Add("停止同步", null, (s, e) => StopSync());

            var toolsMenu = new ToolStripMenuItem("工具 (&T)");
            toolsMenu.DropDownItems.Add("主键配置", null, (s, e) => OpenPrimaryKeyConfig());
            toolsMenu.DropDownItems.Add(new ToolStripSeparator());
            toolsMenu.DropDownItems.Add("定时任务管理", null, (s, e) => OpenSchedulerManager());

            var helpMenu = new ToolStripMenuItem("帮助 (&H)");
            helpMenu.DropDownItems.Add("关于", null, (s, e) => ShowAbout());

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, syncMenu, toolsMenu, helpMenu });
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }

        private void InitTabs()
        {
            tabControl.Dock = DockStyle.Fill;
            tabControl.TabPages.Add("数据库配置");
            tabControl.TabPages.Add("表列表管理");
            tabControl.TabPages.Add("同步配置");
            tabControl.TabPages.Add("任务管理");
            tabControl.TabPages.Add("日志");
            Controls.Add(tabControl);
        }

        private void InitComponents()
        {
            connectionPanel = new DbConnectionPanel();
            tabControl.TabPages[0].Controls.Add(connectionPanel);

            tableListManager = new TableListManager();
            tabControl.TabPages[1].Controls.Add(tableListManager);

            syncConfigPanel = new SyncConfigPanel();
            tabControl.TabPages[2].Controls.Add(syncConfigPanel);

            taskManager = new TaskManager(configManager);
            tabControl.TabPages[3].Controls.Add(taskManager);

            InitLogPanel();

            statusLabel.Text = "就绪";
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            progressBar.Width = 200;
            progressBar.Visible = false;
            progressText.Text = "";
            progressText.AutoSize = true;
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, progressBar, progressText });
            Controls.Add(statusStrip);
        }

        private void InitLogPanel()
        {
            var logPage = tabControl.TabPages[4];
            var logPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            logPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            var toolbarPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 35 };
            toolbarPanel.Controls.Add(new Label { Text = "日志级别:", AutoSize = true, Margin = new Padding(5, 8, 0, 0) });
            logLevelFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
            logLevelFilter.Items.AddRange(new object[] { "全部", "DEBUG", "INFO", "WARN", "ERROR" });
            logLevelFilter.SelectedIndex = 0;
            toolbarPanel.Controls.Add(logLevelFilter);

            clearLogButton = new Button { Text = "清空", Width = 60 };
            exportLogButton = new Button { Text = "导出", Width = 60 };
            toolbarPanel.Controls.Add(clearLogButton);
            toolbarPanel.Controls.Add(exportLogButton);
            logPanel.Controls.Add(toolbarPanel, 0, 0);

            logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9F),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };
            logPanel.Controls.Add(logBox, 0, 1);

            var statusBar = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 30 };
            var logCountLabel = new Label { Text = "共 0 条", AutoSize = true, Margin = new Padding(5, 6, 0, 0) };
            statusBar.Controls.Add(logCountLabel);
            logPanel.Controls.Add(statusBar, 0, 2);

            logPage.Controls.Add(logPanel);

            clearLogButton.Click += (s, e) => { logBox.Clear(); logService.Clear(); };
            exportLogButton.Click += (s, e) =>
            {
                using (var dlg = new SaveFileDialog { Filter = "日志文件|*.log|文本文件|*.txt", Title = "导出日志" })
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        logService.ExportLogs(dlg.FileName);
                        MessageBox.Show("日志已导出到: " + dlg.FileName, "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };
        }

        private void BindEvents()
        {
            connectionPanel.OnConfigChanged += () => statusLabel.Text = "配置已修改";
            tableListManager.OnTablesChanged += () => statusLabel.Text = "表列表已修改";
            syncConfigPanel.OnConfigChanged += () => statusLabel.Text = "同步配置已修改";
            taskManager.OnTaskSelected += () => LoadSelectedTask();
            taskManager.OnTaskDeleted += () => { currentTaskId = null; statusLabel.Text = "任务已删除"; };

            logService.EntryAdded += (s, entry) => AppendLogEntry(entry);

            syncService.ProgressChanged += (s, info) =>
            {
                if (InvokeRequired) { Invoke((Action)(() => OnSyncProgress(info))); return; }
                OnSyncProgress(info);
            };

            syncService.TableCompleted += (s, result) =>
            {
                if (InvokeRequired) { Invoke((Action)(() => OnTableCompleted(result))); return; }
                OnTableCompleted(result);
            };

            schedulerService.TaskCompleted += (s, task) => logService.Info(string.Format("定时任务完成: {0}", task.TaskId));
            schedulerService.TaskFailed += (s, task) => logService.Error(string.Format("定时任务失败: {0} - {1}", task.TaskId, task.LastError));

            tabControl.SelectedIndexChanged += (s, e) => { if (tabControl.SelectedIndex == 3) taskManager.LoadTaskList(); };
        }

        private void OnSyncProgress(SyncProgressInfo info)
        {
            progressBar.Value = info.TotalTables > 0 ? (int)((double)info.CompletedTables / info.TotalTables * 100) : 0;
            progressText.Text = string.Format("{0}/{1}", info.CompletedTables, info.TotalTables);
            statusLabel.Text = info.Status;
        }

        private void OnTableCompleted(SyncExecutionResult result)
        {
            if (result.Success)
                logService.Info(string.Format("表 {0} -> {1} 完成: 读取 {2}, 写入 {3}", result.SourceTable, result.TargetTable, result.ReadCount, result.WriteCount));
            else
                logService.Error(string.Format("表 {0} -> {1} 失败: {2}", result.SourceTable, result.TargetTable, result.ErrorMessage));
        }

        private void LoadSelectedTask()
        {
            var taskId = taskManager.GetSelectedTaskId();
            if (string.IsNullOrEmpty(taskId)) return;
            var config = configManager.GetTaskConfig(taskId);
            if (config == null) return;
            currentTaskId = taskId;

            if (!string.IsNullOrEmpty(config.SourceConnection))
                connectionPanel.SetSource("SqlServer", config.SourceConnection);
            if (!string.IsNullOrEmpty(config.TargetConnection))
                connectionPanel.SetTarget("SqlServer", config.TargetConnection);
            if (!string.IsNullOrEmpty(config.IntermediateConnection))
                connectionPanel.SetIntermediate("SqlServer", config.IntermediateConnection);

            tableListManager.SetTableConfigs(config.TableConfigs);
            syncConfigPanel.LoadFromTask(config);
            statusLabel.Text = "已加载任务: " + taskId;
            logService.Info("加载任务: " + taskId);
        }

        private void CreateNewTask()
        {
            using (var form = new Form())
            {
                form.Text = "新建任务";
                form.Size = new Size(400, 150);
                form.StartPosition = FormStartPosition.CenterParent;
                var label = new Label { Text = "任务ID:", Location = new Point(20, 20), AutoSize = true };
                var textBox = new TextBox { Location = new Point(100, 17), Width = 250 };
                var btnOk = new Button { Text = "确定", Location = new Point(120, 60), Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Location = new Point(210, 60), Width = 75, DialogResult = DialogResult.Cancel };
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;
                form.Controls.AddRange(new Control[] { label, textBox, btnOk, btnCancel });
                if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(textBox.Text))
                {
                    var id = textBox.Text.Trim();
                    var newTask = new SyncTaskConfig { TaskId = id, TaskName = id, IsEnabled = true, RangeDays = 3, CreatedTime = DateTime.Now, ModifiedTime = DateTime.Now };
                    configManager.SaveTaskConfig(newTask);
                    currentTaskId = id;
                    taskManager.LoadTaskList();
                    taskManager.SelectTask(id);
                    statusLabel.Text = "已创建任务: " + id;
                    logService.Info("创建任务: " + id);
                }
            }
        }

        private void SaveCurrentTask()
        {
            if (string.IsNullOrEmpty(currentTaskId))
            {
                ShowWarning("请先创建或加载任务");
                return;
            }
            var config = configManager.GetTaskConfig(currentTaskId) ?? new SyncTaskConfig { TaskId = currentTaskId };
            config.TaskName = currentTaskId;
            config.SourceConnection = connectionPanel.SourceConnectionString;
            config.TargetConnection = connectionPanel.TargetConnectionString;
            config.IntermediateConnection = connectionPanel.IntermediateConnectionString;
            config.TableConfigs = tableListManager.GetTableConfigs();
            config.ModifiedTime = DateTime.Now;
            if (config.CreatedTime == default(DateTime)) config.CreatedTime = DateTime.Now;
            configManager.SaveTaskConfig(config);
            taskManager.LoadTaskList();
            taskManager.SelectTask(currentTaskId);
            statusLabel.Text = "任务已保存: " + currentTaskId;
            logService.Info("保存任务: " + currentTaskId);
            ShowInfo("任务配置已保存");
        }

        private void StartSync()
        {
            if (isSyncing) { ShowWarning("同步正在进行中"); return; }
            if (string.IsNullOrEmpty(currentTaskId)) { ShowWarning("请先创建或加载任务"); return; }

            var config = configManager.GetTaskConfig(currentTaskId);
            if (config == null) { ShowWarning("任务配置不存在"); return; }

            var tableConfigs = tableListManager.GetTableConfigs();
            if (tableConfigs.Count == 0) { ShowWarning("没有配置同步表"); return; }

            isSyncing = true;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            progressText.Text = "0/" + tableConfigs.Count;
            statusLabel.Text = "正在同步...";
            logService.Info("开始同步任务: " + currentTaskId);

            var baseOptions = new DataSyncOptions
            {
                SourceProvider = connectionPanel.SourceProvider,
                SourceConnectionString = connectionPanel.SourceConnectionString,
                TargetProvider = connectionPanel.TargetProvider,
                TargetConnectionString = connectionPanel.TargetConnectionString,
                IntermediateProvider = connectionPanel.IntermediateProvider,
                IntermediateConnectionString = connectionPanel.IntermediateConnectionString,
                TaskId = currentTaskId,
            };
            syncConfigPanel.ApplyToOptions(baseOptions);

            var batchResult = syncService.ExecuteBatchSync(tableConfigs, baseOptions, (opts, tc) =>
            {
                var tableOpts = new DataSyncOptions
                {
                    SourceProvider = opts.SourceProvider,
                    SourceConnectionString = opts.SourceConnectionString,
                    TargetProvider = opts.TargetProvider,
                    TargetConnectionString = opts.TargetConnectionString,
                    IntermediateProvider = opts.IntermediateProvider,
                    IntermediateConnectionString = opts.IntermediateConnectionString,
                    TaskId = opts.TaskId,
                    SourceTable = tc.TableName,
                    TargetTable = string.IsNullOrEmpty(tc.TargetTableName) ? tc.TableName : tc.TargetTableName,
                    PrimaryKeyColumns = tc.PrimaryKeyColumns,
                    IsAutoIncrementKey = tc.IsAutoIncrementKey,
                    TimeColumn = tc.TimeColumn,
                    EnableTimeRange = tc.EnableTimeRange,
                    RangeDays = tc.RangeDays,
                    SyncColumns = tc.SyncColumns,
                    AlwaysDeduplicate = tc.AlwaysDeduplicate,
                    EnableGlobalConfig = tc.EnableGlobalConfig,
                    GlobalRangeDays = tc.GlobalRangeDays,
                    BatchSize = opts.BatchSize,
                    RetryCount = opts.RetryCount,
                    CleanIntermediateData = opts.CleanIntermediateData,
                    AutoCreateIntermediateSchema = opts.AutoCreateIntermediateSchema,
                    ResumeFailedRecords = opts.ResumeFailedRecords,
                };
                return tableOpts;
            });

            isSyncing = false;
            progressBar.Visible = false;
            progressText.Text = "";

            if (batchResult.Success)
            {
                statusLabel.Text = string.Format("同步完成: {0} 表, 读取 {1}, 写入 {2}", batchResult.SuccessTables, batchResult.TotalRead, batchResult.TotalWrite);
                logService.Info(string.Format("同步完成: 成功 {0} 表, 读取 {1}, 写入 {2}, 耗时 {3}", batchResult.SuccessTables, batchResult.TotalRead, batchResult.TotalWrite, batchResult.TotalDuration));
                ShowSyncResult(batchResult);
            }
            else
            {
                statusLabel.Text = string.Format("同步完成: 成功 {0}, 失败 {1}, 跳过 {2}", batchResult.SuccessTables, batchResult.FailedTables, batchResult.SkippedTables);
                logService.Error(string.Format("同步部分失败: 成功 {0}, 失败 {1}, 跳过 {2}", batchResult.SuccessTables, batchResult.FailedTables, batchResult.SkippedTables));
                ShowSyncResult(batchResult);
            }
        }

        private void ShowSyncResult(BatchSyncResult result)
        {
            var msg = string.Format("同步完成!\n\n成功表数: {0}\n失败表数: {1}\n跳过表数: {2}\n\n总读取: {3}\n总写入: {4}\n总失败: {5}\n\n耗时: {6}",
                result.SuccessTables, result.FailedTables, result.SkippedTables,
                result.TotalRead, result.TotalWrite, result.TotalFailed,
                result.TotalDuration);

            if (result.FailedTables > 0 || result.SkippedTables > 0)
            {
                msg += "\n\n失败详情:";
                foreach (var tr in result.TableResults)
                {
                    if (!tr.Success)
                        msg += string.Format("\n  {0} -> {1}: {2}", tr.SourceTable, tr.TargetTable, tr.ErrorMessage ?? "未知错误");
                }
            }

            var icon = result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
            MessageBox.Show(msg, "同步结果", MessageBoxButtons.OK, icon);
        }

        private void StopSync()
        {
            if (!isSyncing) return;
            syncService.CancelBatchSync();
            isSyncing = false;
            progressBar.Visible = false;
            progressText.Text = "";
            statusLabel.Text = "同步已停止";
            logService.Warn("同步被用户停止");
        }

        private void OpenPrimaryKeyConfig()
        {
            var dialog = new PrimaryKeyConfigDialog(pkConfigService);
            dialog.ShowDialog();
        }

        private void OpenSchedulerManager()
        {
            var tasks = configManager.GetAllConfigs();
            if (tasks.Count == 0)
            {
                ShowInfo("没有已配置的任务，请先创建任务");
                return;
            }

            using (var form = new Form())
            {
                form.Text = "定时任务管理";
                form.Size = new Size(600, 400);
                form.StartPosition = FormStartPosition.CenterParent;

                var grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
                grid.Columns.AddRange(new DataGridViewColumn[]
                {
                    new DataGridViewTextBoxColumn { Name = "TaskId", HeaderText = "任务ID", Width = 120 },
                    new DataGridViewTextBoxColumn { Name = "Enabled", HeaderText = "启用", Width = 50 },
                    new DataGridViewTextBoxColumn { Name = "Interval", HeaderText = "间隔", Width = 80 },
                    new DataGridViewTextBoxColumn { Name = "RunCount", HeaderText = "执行次数", Width = 80 },
                    new DataGridViewTextBoxColumn { Name = "LastRun", HeaderText = "上次执行", Width = 150 },
                    new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "状态", Width = 80 },
                });

                foreach (var t in schedulerService.GetAllTasks())
                {
                    grid.Rows.Add(t.TaskId, t.Enabled, t.Interval.ToString(), t.RunCount,
                        t.LastRunTime.HasValue ? t.LastRunTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                        t.LastStatus ?? "");
                }

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40 };
                var btnStart = new Button { Text = "启动调度", Width = 80 };
                var btnStop = new Button { Text = "停止调度", Width = 80 };
                var btnClose = new Button { Text = "关闭", Width = 80 };
                btnPanel.Controls.AddRange(new Control[] { btnStart, btnStop, btnClose });
                form.Controls.Add(grid);
                form.Controls.Add(btnPanel);

                btnStart.Click += (s, e) => { schedulerService.StartAll(); ShowInfo("定时调度已启动"); };
                btnStop.Click += (s, e) => { schedulerService.StopAll(); ShowInfo("定时调度已停止"); };
                btnClose.Click += (s, e) => form.Close();

                form.ShowDialog();
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show("FastData 同步工具 v2.0\n\n支持 SQL Server/MySQL/PostgreSQL\n异构成对 + 去重 + 增量同步\n定时任务调度\n日志过滤与导出", "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowWarning(string message) { MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        private void ShowInfo(string message) { MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void ShowError(string message) { MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }

        private void AppendLogEntry(LogEntry entry)
        {
            if (logBox.InvokeRequired) { logBox.Invoke((Action)(() => AppendLogEntry(entry))); return; }

            int start = logBox.TextLength;
            logBox.AppendText(entry.ToString() + Environment.NewLine);
            int end = logBox.TextLength;

            logBox.Select(start, end - start);
            switch (entry.Level)
            {
                case LogEntryLevel.Debug: logBox.SelectionColor = Color.Gray; break;
                case LogEntryLevel.Info: logBox.SelectionColor = Color.FromArgb(220, 220, 220); break;
                case LogEntryLevel.Warn: logBox.SelectionColor = Color.Yellow; break;
                case LogEntryLevel.Error: logBox.SelectionColor = Color.Red; break;
            }
            logBox.SelectionColor = logBox.ForeColor;
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isSyncing)
            {
                if (MessageBox.Show("同步正在进行中，确定退出吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                syncService.CancelBatchSync();
            }
            schedulerService.Dispose();
            syncService.Dispose();
            logService.Dispose();
            base.OnFormClosing(e);
        }
    }
}
