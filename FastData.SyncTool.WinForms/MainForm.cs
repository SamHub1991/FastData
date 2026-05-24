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
        private readonly TextBox sourceTableBox = new TextBox();
        private readonly TextBox targetTableBox = new TextBox();
        private readonly NumericUpDown batchSizeBox = new NumericUpDown();
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
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9 };
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

            AddLabel(panel, "源表", 4);
            sourceTableBox.Dock = DockStyle.Fill;
            panel.Controls.Add(sourceTableBox, 1, 4);

            AddLabel(panel, "目标表", 5);
            targetTableBox.Dock = DockStyle.Fill;
            panel.Controls.Add(targetTableBox, 1, 5);

            AddLabel(panel, "批量大小", 6);
            batchSizeBox.Minimum = 1;
            batchSizeBox.Maximum = 100000;
            batchSizeBox.Value = 500;
            panel.Controls.Add(batchSizeBox, 1, 6);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            exportSchemaButton.Text = "导出中间库 SQL";
            syncButton.Text = "执行同步";
            buttonPanel.Controls.Add(exportSchemaButton);
            buttonPanel.Controls.Add(syncButton);
            panel.Controls.Add(buttonPanel, 1, 7);

            AddLabel(panel, "运行日志", 8);
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Both;
            logBox.Font = new System.Drawing.Font("Consolas", 10);
            panel.Controls.Add(logBox, 1, 8);

            for (var i = 0; i < 8; i++)
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
            var script = new IntermediateSchemaBuilder().BuildSqlServerScript();
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
                    SourceTable = sourceTableBox.Text,
                    TargetTable = string.IsNullOrEmpty(targetTableBox.Text) ? sourceTableBox.Text : targetTableBox.Text,
                    BatchSize = Convert.ToInt32(batchSizeBox.Value)
                });

                Log(string.Format("{0}，读取 {1} 条，写入 {2} 条", result.Message, result.ReadCount, result.WriteCount));
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
