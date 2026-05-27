using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FastData.SyncTool.WinForms
{
    public class TableSelectorDialog : Form
    {
        private readonly ListBox tableListBox = new ListBox();
        private readonly TextBox searchBox = new TextBox();
        private readonly Button okButton = new Button();
        private readonly Button cancelButton = new Button();
        private List<string> allTables = new List<string>();

        public IList<string> SelectedTables { get; private set; } = new List<string>();

        public TableSelectorDialog(IList<string> availableTables = null)
        {
            Text = "选择要同步的表";
            Width = 500; Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            BuildLayout(availableTables);
        }

        private void BuildLayout(IList<string> availableTables)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            panel.Controls.Add(new Label { Text = "搜索表名:", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight }, 0, 0);
            searchBox.Dock = DockStyle.Fill;
            searchBox.TextChanged += (s, e) => FilterTables();
            panel.Controls.Add(searchBox, 0, 1);

            tableListBox.SelectionMode = SelectionMode.MultiExtended;
            tableListBox.Dock = DockStyle.Fill;
            if (availableTables != null)
            {
                allTables = new List<string>(availableTables);
                allTables.Sort();
                foreach (var t in allTables) tableListBox.Items.Add(t);
            }
            panel.Controls.Add(tableListBox, 0, 2);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            okButton.Text = "确定"; okButton.Width = 80;
            cancelButton.Text = "取消"; cancelButton.Width = 80;
            btnPanel.Controls.AddRange(new Control[] { okButton, cancelButton });
            panel.Controls.Add(btnPanel, 0, 3);

            okButton.Click += (s, e) => { foreach (var item in tableListBox.SelectedItems) SelectedTables.Add(item.ToString()); DialogResult = DialogResult.OK; Close(); };
            cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(panel);
        }

        private void FilterTables()
        {
            var kw = searchBox.Text.ToLower();
            tableListBox.Items.Clear();
            foreach (var t in allTables)
                if (string.IsNullOrEmpty(kw) || t.ToLower().Contains(kw)) tableListBox.Items.Add(t);
        }
    }
}
