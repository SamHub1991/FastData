using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Windows.Forms;

namespace FastData.SyncTool.WinForms
{
    public class TableSelectForm : Form
    {
        private readonly string provider;
        private readonly string connectionString;
        private readonly ListBox tableListBox = new ListBox();
        private readonly Button okButton = new Button();
        private readonly Button cancelButton = new Button();
        private readonly TextBox searchBox = new TextBox();

        public IList<string> SelectedTables { get; private set; } = new List<string>();

        public TableSelectForm(string provider, string connectionString)
        {
            this.provider = provider;
            this.connectionString = connectionString;
            Text = "选择要同步的表";
            Width = 500;
            Height = 600;
            BuildLayout();
            LoadTables();
        }

        private void BuildLayout()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            Controls.Add(panel);

            var searchLabel = new Label { Text = "搜索表名:", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight };
            panel.Controls.Add(searchLabel, 0, 0);

            searchBox.Dock = DockStyle.Fill;
            panel.Controls.Add(searchBox, 0, 1);

            tableListBox.SelectionMode = SelectionMode.MultiExtended;
            tableListBox.Dock = DockStyle.Fill;
            panel.Controls.Add(tableListBox, 0, 2);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            okButton.Text = "确定";
            okButton.Width = 80;
            cancelButton.Text = "取消";
            cancelButton.Width = 80;
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            panel.Controls.Add(buttonPanel, 0, 3);

            okButton.Click += delegate
            {
                foreach (var item in tableListBox.SelectedItems)
                {
                    SelectedTables.Add(item.ToString());
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            searchBox.TextChanged += delegate { FilterTables(); };
        }

        private void LoadTables()
        {
            try
            {
                var cursor = Cursor;
                Cursor = Cursors.WaitCursor;

                using (var conn = DbProviderFactories.GetFactory(provider).CreateConnection())
                {
                    conn.ConnectionString = connectionString;
                    conn.Open();

                    var tables = conn.GetSchema("Tables").Select();
                    var tableNames = new List<string>();
                    foreach (var row in tables)
                    {
                        var tableName = row["TABLE_NAME"].ToString();
                        if (!tableName.StartsWith("fd_"))
                        {
                            tableNames.Add(tableName);
                        }
                    }

                    tableNames.Sort();
                    tableListBox.Items.Clear();
                    foreach (var name in tableNames)
                    {
                        tableListBox.Items.Add(name);
                    }
                }

                Cursor = cursor;
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载表列表失败：" + ex.Message);
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void FilterTables()
        {
            var keyword = searchBox.Text.ToLower();
            var filtered = new System.Collections.Generic.List<string>();

            foreach (var item in tableListBox.Items)
            {
                var tableName = item.ToString().ToLower();
                if (string.IsNullOrEmpty(keyword) || tableName.Contains(keyword))
                {
                    filtered.Add(item.ToString());
                }
            }

            var selectedIndex = tableListBox.SelectedIndex;
            tableListBox.Items.Clear();
            foreach (var name in filtered)
            {
                tableListBox.Items.Add(name);
            }

            if (selectedIndex >= 0 && selectedIndex < tableListBox.Items.Count)
            {
                tableListBox.SelectedIndex = selectedIndex;
            }
        }
    }
}
