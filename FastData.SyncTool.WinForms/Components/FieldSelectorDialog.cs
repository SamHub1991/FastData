using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FastData.SyncTool.WinForms
{
    public class FieldSelectorDialog : Form
    {
        private readonly CheckedListBox columnListBox = new CheckedListBox();
        private readonly Button okButton = new Button();
        private readonly Button cancelButton = new Button();
        private readonly Button selectAllButton = new Button();
        private readonly Button invertButton = new Button();

        public IList<string> SelectedColumns { get; private set; } = new List<string>();

        public FieldSelectorDialog(string tableName, IList<string> availableColumns, IList<string> existingColumns = null)
        {
            Text = "选择同步字段 - " + tableName;
            Width = 500; Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            if (existingColumns != null) SelectedColumns = new List<string>(existingColumns);
            BuildLayout(availableColumns);
        }

        private void BuildLayout(IList<string> availableColumns)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            var tip = new Label { Text = "请选择要同步的字段", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, ForeColor = System.Drawing.Color.Blue };
            panel.Controls.Add(tip, 0, 0);

            columnListBox.CheckOnClick = true;
            columnListBox.Dock = DockStyle.Fill;
            if (availableColumns != null)
            {
                foreach (var col in availableColumns)
                {
                    int idx = columnListBox.Items.Add(col);
                    if (SelectedColumns.Contains(col)) columnListBox.SetItemChecked(idx, true);
                }
            }
            panel.Controls.Add(columnListBox, 0, 1);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            selectAllButton.Text = "全选"; selectAllButton.Width = 60;
            invertButton.Text = "反选"; invertButton.Width = 60;
            okButton.Text = "确定"; okButton.Width = 80;
            cancelButton.Text = "取消"; cancelButton.Width = 80;
            btnPanel.Controls.AddRange(new Control[] { selectAllButton, invertButton, okButton, cancelButton });
            panel.Controls.Add(btnPanel, 0, 2);

            selectAllButton.Click += (s, e) => { for (int i = 0; i < columnListBox.Items.Count; i++) columnListBox.SetItemChecked(i, true); };
            invertButton.Click += (s, e) => { for (int i = 0; i < columnListBox.Items.Count; i++) columnListBox.SetItemChecked(i, !columnListBox.GetItemChecked(i)); };
            okButton.Click += (s, e) => { SelectedColumns = GetChecked(); DialogResult = DialogResult.OK; Close(); };
            cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(panel);
        }

        private List<string> GetChecked()
        {
            var list = new List<string>();
            for (int i = 0; i < columnListBox.Items.Count; i++)
                if (columnListBox.GetItemChecked(i)) list.Add(columnListBox.Items[i].ToString());
            return list;
        }
    }
}
