using FastData.Tooling.Sync;
using System;
using System.IO;
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
        private readonly TextBox incrementalColumnBox = new TextBox();
        private readonly TextBox lastValueBox = new TextBox();
        private readonly NumericUpDown batchSizeBox = new NumericUpDown();
        private readonly NumericUpDown retryCountBox = new NumericUpDown();
        private readonly CheckBox autoCreateIntermediateBox = new CheckBox();
        private readonly CheckBox resumeFailedRecordsBox = new CheckBox();
        private readonly CheckBox cleanIntermediateBox = new CheckBox();
        private readonly TextBox logBox = new TextBox();
        private readonly Button exportSchemaButton = new Button();
        private readonly Button syncButton = new Button();

        public MainForm()
        {
            Text = "FastData 数据同步工具";
            Width = 1000;
            Height = 640;
            BuildLayout();
            BindEvents();
        }

        private void BuildLayout()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 18 };
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

            AddLabel(panel, "增量字段", 9);
            incrementalColumnBox.Dock = DockStyle.Fill;
            panel.Controls.Add(incrementalColumnBox, 1, 9);

            AddLabel(panel, "增量起点", 10);
            lastValueBox.Dock = DockStyle.Fill;
            panel.Controls.Add(lastValueBox, 1, 10);

            AddLabel(panel, "批量大小", 11);
            batchSizeBox.Minimum = 1;
            batchSizeBox.Maximum = 100000;
            batchSizeBox.Value = 500;
            panel.Controls.Add(batchSizeBox, 1, 11);

            AddLabel(panel, "失败重试次数", 12);
            retryCountBox.Minimum = 0;
            retryCountBox.Maximum = 100;
            retryCountBox.Value = 1;
            panel.Controls.Add(retryCountBox, 1, 12);

            AddLabel(panel, "自动创建中间库表", 13);
            autoCreateIntermediateBox.Dock = DockStyle.Fill;
            panel.Controls.Add(autoCreateIntermediateBox, 1, 13);

            AddLabel(panel, "恢复失败记录", 14);
            resumeFailedRecordsBox.Dock = DockStyle.Fill;
            panel.Controls.Add(resumeFailedRecordsBox, 1, 14);

            AddLabel(panel, "清理中间库成功记录", 15);
            cleanIntermediateBox.Dock = DockStyle.Fill;
            panel.Controls.Add(cleanIntermediateBox, 1, 15);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            exportSchemaButton.Text = "导出中间库 SQL";
            syncButton.Text = "执行同步";
            buttonPanel.Controls.Add(exportSchemaButton);
            buttonPanel.Controls.Add(syncButton);
            panel.Controls.Add(buttonPanel, 1, 16);

            AddLabel(panel, "运行日志", 17);
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Both;
            logBox.Font = new System.Drawing.Font("Consolas", 10);
            panel.Controls.Add(logBox, 1, 17);

            for (var i = 0; i < 17; i++)
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
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
            syncButton.Click += delegate { ExecuteSync(); };
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
            try
            {
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
                    IncrementalColumn = incrementalColumnBox.Text,
                    LastValue = lastValueBox.Text,
                    BatchSize = Convert.ToInt32(batchSizeBox.Value),
                    RetryCount = Convert.ToInt32(retryCountBox.Value),
                    AutoCreateIntermediateSchema = autoCreateIntermediateBox.Checked,
                    ResumeFailedRecords = resumeFailedRecordsBox.Checked,
                    CleanIntermediateData = cleanIntermediateBox.Checked
                });

                Log(string.Format("{0}，读取 {1} 条，写入 {2} 条，失败 {3} 条，重试 {4} 次，恢复 {5} 条", result.Message, result.ReadCount, result.WriteCount, result.FailedCount, result.RetryCount, result.RecoveredCount));
            }
            catch (Exception ex)
            {
                Log("同步失败: " + ex.Message);
            }
        }

        private void Log(string message)
        {
            logBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
        }
    }
}
