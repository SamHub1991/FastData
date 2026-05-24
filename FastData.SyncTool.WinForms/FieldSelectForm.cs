using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Windows.Forms;

namespace FastData.SyncTool.WinForms
{
    public class FieldSelectForm : Form
    {
        private readonly string provider;
        private readonly string connectionString;
        private readonly string tableName;
        private readonly CheckedListBox columnListBox = new CheckedListBox();
        private readonly Button okButton = new Button();
        private readonly Button cancelButton = new Button();
        private readonly Button selectAllButton = new Button();
        private readonly Button invertButton = new Button();

        public IList<string> SelectedColumns { get; private set; } = new List<string>();
        public string PrimaryKeyColumn { get; private set; }

        public FieldSelectForm(string provider, string connectionString, string tableName, IList<string> existingColumns = null)
        {
            this.provider = provider;
            this.connectionString = connectionString;
            this.tableName = tableName;
            Text = string.Format("选择同步字段 - {0}", tableName);
            Width = 500;
            Height = 600;
            
            if (existingColumns != null)
            {
                SelectedColumns = new List<string>(existingColumns);
            }
            
            BuildLayout();
            LoadColumns();
        }

        private void BuildLayout()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            Controls.Add(panel);

            var tipLabel = new Label 
            { 
                Text = "请选择要同步的字段（至少选择一个，主键字段必须选择）", 
                Dock = DockStyle.Fill, 
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                ForeColor = System.Drawing.Color.Blue
            };
            panel.Controls.Add(tipLabel, 0, 0);

            columnListBox.CheckOnClick = true;
            columnListBox.SelectionMode = SelectionMode.MultiExtended;
            columnListBox.Dock = DockStyle.Fill;
            panel.Controls.Add(columnListBox, 0, 1);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            selectAllButton.Text = "全选";
            selectAllButton.Width = 60;
            invertButton.Text = "反选";
            invertButton.Width = 60;
            okButton.Text = "确定";
            okButton.Width = 80;
            cancelButton.Text = "取消";
            cancelButton.Width = 80;
            buttonPanel.Controls.Add(selectAllButton);
            buttonPanel.Controls.Add(invertButton);
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);
            panel.Controls.Add(buttonPanel, 0, 2);

            selectAllButton.Click += delegate 
            {
                for (int i = 0; i < columnListBox.Items.Count; i++)
                    columnListBox.SetItemChecked(i, true);
            };

            invertButton.Click += delegate 
            {
                for (int i = 0; i < columnListBox.Items.Count; i++)
                    columnListBox.SetItemChecked(i, !columnListBox.GetItemChecked(i));
            };

            okButton.Click += delegate
            {
                if (ValidateSelection())
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
        }

        private void LoadColumns()
        {
            try
            {
                var cursor = Cursor;
                Cursor = Cursors.WaitCursor;

                using (var conn = DbProviderFactories.GetFactory(provider).CreateConnection())
                {
                    conn.ConnectionString = connectionString;
                    conn.Open();

                    var columns = conn.GetSchema("Columns", new[] { null, null, tableName }).Select();
                    var columnNames = new List<string>();
                    
                    foreach (var row in columns)
                    {
                        var columnName = row["COLUMN_NAME"].ToString();
                        var isPrimaryKey = Convert.ToString(row["COLUMN_KEY"]) == "PRI";
                        
                        if (isPrimaryKey)
                        {
                            PrimaryKeyColumn = columnName;
                        }
                        
                        columnNames.Add(columnName);
                    }

                    columnNames.Sort();
                    columnListBox.Items.Clear();
                    foreach (var name in columnNames)
                    {
                        var index = columnListBox.Items.Add(name);
                        if (SelectedColumns != null && SelectedColumns.Count > 0)
                        {
                            columnListBox.SetItemChecked(index, SelectedColumns.Contains(name));
                        }
                        else if (name == PrimaryKeyColumn)
                        {
                            columnListBox.SetItemChecked(index, true);
                        }
                    }
                }

                Cursor = cursor;
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载字段列表失败：" + ex.Message);
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private bool ValidateSelection()
        {
            var checkedItems = new List<string>();
            for (int i = 0; i < columnListBox.Items.Count; i++)
            {
                if (columnListBox.GetItemChecked(i))
                {
                    checkedItems.Add(columnListBox.Items[i].ToString());
                }
            }

            if (checkedItems.Count == 0)
            {
                MessageBox.Show("请至少选择一个字段");
                return false;
            }

            if (!string.IsNullOrEmpty(PrimaryKeyColumn) && !checkedItems.Contains(PrimaryKeyColumn))
            {
                var result = MessageBox.Show(
                    string.Format("主键字段 [{0}] 未被选择，这可能导致数据重复。确定继续吗？", PrimaryKeyColumn),
                    "确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.No)
                    return false;
            }

            SelectedColumns = checkedItems;
            return true;
        }
    }
}
