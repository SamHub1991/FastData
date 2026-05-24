using FastData.Tooling.CodeGeneration;
using FastData.Tooling.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace FastData.ModelGenerator.WinForms
{
    public class MainForm : Form
    {
        private readonly ComboBox providerBox = new ComboBox();
        private readonly TextBox connectionBox = new TextBox();
        private readonly TextBox namespaceBox = new TextBox();
        private readonly TextBox outputBox = new TextBox();
        private readonly ListBox tableList = new ListBox();
        private readonly TextBox previewBox = new TextBox();
        private readonly Button testButton = new Button();
        private readonly Button loadButton = new Button();
        private readonly Button previewButton = new Button();
        private readonly Button generateButton = new Button();
        private IList<DatabaseTable> tables = new List<DatabaseTable>();

        public MainForm()
        {
            Text = "FastData Model 生成工具";
            Width = 1000;
            Height = 700;
            BuildLayout();
            BindEvents();
        }

        private void BuildLayout()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(panel);

            AddLabel(panel, "Provider", 0);
            providerBox.DropDownStyle = ComboBoxStyle.DropDownList;
            providerBox.Items.AddRange(new object[] { "System.Data.SqlClient", "MySql.Data.MySqlClient", "Oracle.ManagedDataAccess.Client" });
            providerBox.SelectedIndex = 0;
            panel.Controls.Add(providerBox, 1, 0);

            AddLabel(panel, "连接字符串", 1);
            connectionBox.Dock = DockStyle.Fill;
            panel.Controls.Add(connectionBox, 1, 1);

            AddLabel(panel, "命名空间", 2);
            namespaceBox.Text = "FastData.Generated.Models";
            namespaceBox.Dock = DockStyle.Fill;
            panel.Controls.Add(namespaceBox, 1, 2);

            AddLabel(panel, "输出目录", 3);
            outputBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
            outputBox.Dock = DockStyle.Fill;
            panel.Controls.Add(outputBox, 1, 3);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            testButton.Text = "测试连接";
            loadButton.Text = "加载表";
            previewButton.Text = "预览代码";
            generateButton.Text = "生成文件";
            buttonPanel.Controls.Add(testButton);
            buttonPanel.Controls.Add(loadButton);
            buttonPanel.Controls.Add(previewButton);
            buttonPanel.Controls.Add(generateButton);
            panel.Controls.Add(buttonPanel, 1, 4);

            AddLabel(panel, "数据表", 5);
            tableList.Dock = DockStyle.Fill;
            tableList.SelectionMode = SelectionMode.MultiExtended;
            panel.Controls.Add(tableList, 1, 5);

            AddLabel(panel, "代码预览", 6);
            previewBox.Dock = DockStyle.Fill;
            previewBox.Multiline = true;
            previewBox.ScrollBars = ScrollBars.Both;
            previewBox.Font = new System.Drawing.Font("Consolas", 10);
            panel.Controls.Add(previewBox, 1, 6);

            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        }

        private static void AddLabel(TableLayoutPanel panel, string text, int row)
        {
            panel.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight }, 0, row);
        }

        private void BindEvents()
        {
            testButton.Click += delegate { TestConnection(); };
            loadButton.Click += delegate { LoadTables(); };
            previewButton.Click += delegate { PreviewCode(); };
            generateButton.Click += delegate { GenerateFiles(); };
        }

        private DatabaseConnectionOptions GetOptions()
        {
            return new DatabaseConnectionOptions
            {
                Provider = Convert.ToString(providerBox.SelectedItem),
                ConnectionString = connectionBox.Text
            };
        }

        private void TestConnection()
        {
            try
            {
                MetadataReaderFactory.Create(GetOptions()).TestConnection();
                MessageBox.Show("连接成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "连接失败");
            }
        }

        private void LoadTables()
        {
            try
            {
                tables = MetadataReaderFactory.Create(GetOptions()).GetTables();
                tableList.Items.Clear();
                foreach (var table in tables)
                    tableList.Items.Add(table.FullName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "加载失败");
            }
        }

        private void PreviewCode()
        {
            var table = GetSelectedTable();
            if (table == null)
                return;

            var reader = MetadataReaderFactory.Create(GetOptions());
            var columns = reader.GetColumns(table.FullName);
            previewBox.Text = new ModelCodeGenerator().Generate(namespaceBox.Text, table, columns);
        }

        private void GenerateFiles()
        {
            Directory.CreateDirectory(outputBox.Text);
            var reader = MetadataReaderFactory.Create(GetOptions());
            var generator = new ModelCodeGenerator();
            foreach (var item in tableList.SelectedItems)
            {
                var table = FindTable(Convert.ToString(item));
                if (table == null)
                    continue;

                var code = generator.Generate(namespaceBox.Text, table, reader.GetColumns(table.FullName));
                File.WriteAllText(Path.Combine(outputBox.Text, table.Name + ".cs"), code);
            }
            MessageBox.Show("生成完成");
        }

        private DatabaseTable GetSelectedTable()
        {
            if (tableList.SelectedItem == null)
            {
                MessageBox.Show("请选择数据表");
                return null;
            }
            return FindTable(Convert.ToString(tableList.SelectedItem));
        }

        private DatabaseTable FindTable(string fullName)
        {
            foreach (var table in tables)
            {
                if (table.FullName == fullName)
                    return table;
            }
            return null;
        }
    }
}
