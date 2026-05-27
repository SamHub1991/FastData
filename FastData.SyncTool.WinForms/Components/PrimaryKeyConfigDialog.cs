using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms
{
    public class PrimaryKeyConfigDialog : Form
    {
        private readonly PrimaryKeyConfigService configService;
        private readonly TextBox tableNameBox = new TextBox();
        private readonly TextBox pkColumnsBox = new TextBox();
        private readonly CheckBox autoIncrementBox = new CheckBox();
        private readonly TextBox incrementalColumnBox = new TextBox();
        private readonly ListBox configListBox = new ListBox();
        private readonly Button addButton = new Button();
        private readonly Button updateButton = new Button();
        private readonly Button deleteButton = new Button();
        private readonly Button exportButton = new Button();
        private readonly Button closeButton = new Button();

        public PrimaryKeyConfigDialog(PrimaryKeyConfigService service)
        {
            configService = service;
            Text = "表主键配置管理";
            Width = 700; Height = 500;
            StartPosition = FormStartPosition.CenterParent;
            BuildLayout();
            BindEvents();
            RefreshList();
        }

        public PrimaryKeyConfigDialog() : this(new PrimaryKeyConfigService()) { }

        private void BuildLayout()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(panel);

            int r = 0;
            AddLabel(panel, "表名", r); panel.Controls.Add(tableNameBox, 1, r++);
            AddLabel(panel, "主键字段(逗号分隔)", r); panel.Controls.Add(pkColumnsBox, 1, r++);
            AddLabel(panel, "自增主键", r); panel.Controls.Add(autoIncrementBox, 1, r++);
            AddLabel(panel, "增量字段", r); panel.Controls.Add(incrementalColumnBox, 1, r++);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            addButton.Text = "添加"; addButton.Width = 70;
            updateButton.Text = "修改"; updateButton.Width = 70;
            deleteButton.Text = "删除"; deleteButton.Width = 70;
            exportButton.Text = "导出SQL"; exportButton.Width = 80;
            btnPanel.Controls.AddRange(new Control[] { addButton, updateButton, deleteButton, exportButton });
            panel.Controls.Add(btnPanel, 1, r++);

            AddLabel(panel, "已配置表", r); panel.Controls.Add(configListBox, 1, r++);
            configListBox.Dock = DockStyle.Fill;
            configListBox.SelectionMode = SelectionMode.One;

            var closePanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            closeButton.Text = "关闭"; closeButton.Width = 70;
            closePanel.Controls.Add(closeButton);
            panel.Controls.Add(closePanel, 1, r++);

            for (int i = 0; i < 7; i++) panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        }

        private void AddLabel(TableLayoutPanel p, string text, int row)
        {
            p.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight }, 0, row);
        }

        private void BindEvents()
        {
            addButton.Click += (s, e) => AddConfig();
            updateButton.Click += (s, e) => UpdateConfig();
            deleteButton.Click += (s, e) => DeleteConfig();
            exportButton.Click += (s, e) => ExportSql();
            closeButton.Click += (s, e) => Close();
            configListBox.SelectedIndexChanged += (s, e) => LoadSelected();
        }

        private void RefreshList()
        {
            configListBox.Items.Clear();
            foreach (var c in configService.GetAllConfigs())
                configListBox.Items.Add(c.TableName + " (" + string.Join(",", c.PrimaryKeyColumns) + ")");
        }

        private void LoadSelected()
        {
            if (configListBox.SelectedItem == null) return;
            var name = configListBox.SelectedItem.ToString();
            var spaceIdx = name.IndexOf(' ');
            if (spaceIdx > 0) name = name.Substring(0, spaceIdx);
            var config = configService.GetTableConfig(name);
            if (config != null)
            {
                tableNameBox.Text = config.TableName;
                pkColumnsBox.Text = config.PrimaryKeyColumns != null ? string.Join(",", config.PrimaryKeyColumns) : "";
                autoIncrementBox.Checked = config.IsAutoIncrement;
                incrementalColumnBox.Text = config.IncrementalColumn ?? "";
            }
        }

        private void AddConfig()
        {
            if (string.IsNullOrEmpty(tableNameBox.Text)) { MessageBox.Show("请输入表名"); return; }
            configService.AddTableConfig(BuildConfig());
            RefreshList();
            ClearInputs();
        }

        private void UpdateConfig()
        {
            if (configListBox.SelectedItem == null) { MessageBox.Show("请选择要修改的配置"); return; }
            configService.AddTableConfig(BuildConfig());
            RefreshList();
            ClearInputs();
        }

        private void DeleteConfig()
        {
            if (string.IsNullOrEmpty(tableNameBox.Text)) { MessageBox.Show("请选择要删除的配置"); return; }
            if (MessageBox.Show("确定删除？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                configService.RemoveTableConfig(tableNameBox.Text);
                RefreshList();
                ClearInputs();
            }
        }

        private TablePrimaryKeyConfig BuildConfig()
        {
            var cols = pkColumnsBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return new TablePrimaryKeyConfig
            {
                TableName = tableNameBox.Text.Trim(),
                PrimaryKeyColumns = new List<string>(cols),
                IsAutoIncrement = autoIncrementBox.Checked,
                IncrementalColumn = incrementalColumnBox.Text
            };
        }

        private void ExportSql()
        {
            using (var dlg = new SaveFileDialog { Filter = "SQL Files|*.sql" })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(dlg.FileName, configService.ExportToSql());
                    MessageBox.Show("SQL 已导出");
                }
            }
        }

        private void ClearInputs()
        {
            tableNameBox.Clear(); pkColumnsBox.Clear(); autoIncrementBox.Checked = false; incrementalColumnBox.Clear();
        }
    }
}
