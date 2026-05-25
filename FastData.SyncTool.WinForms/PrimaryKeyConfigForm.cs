using FastData.Tooling.Sync;
using System;
using System.Windows.Forms;

namespace FastData.SyncTool.WinForms
{
    public class PrimaryKeyConfigForm : Form
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

        public PrimaryKeyConfigForm(PrimaryKeyConfigService service)
        {
            configService = service;
            Text = "表主键配置管理";
            Width = 700;
            Height = 500;
            BuildLayout();
            BindEvents();
            RefreshConfigList();
        }

        private void BuildLayout()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(panel);

            AddLabel(panel, "表名", 0);
            tableNameBox.Dock = DockStyle.Fill;
            panel.Controls.Add(tableNameBox, 1, 0);

            AddLabel(panel, "主键字段 (逗号分隔)", 1);
            pkColumnsBox.Dock = DockStyle.Fill;
            panel.Controls.Add(pkColumnsBox, 1, 1);

            AddLabel(panel, "自增主键", 2);
            autoIncrementBox.Dock = DockStyle.Fill;
            panel.Controls.Add(autoIncrementBox, 1, 2);

            AddLabel(panel, "增量字段", 3);
            incrementalColumnBox.Dock = DockStyle.Fill;
            panel.Controls.Add(incrementalColumnBox, 1, 3);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            addButton.Text = "添加";
            updateButton.Text = "修改";
            deleteButton.Text = "删除";
            exportButton.Text = "导出 SQL";
            buttonPanel.Controls.Add(addButton);
            buttonPanel.Controls.Add(updateButton);
            buttonPanel.Controls.Add(deleteButton);
            buttonPanel.Controls.Add(exportButton);
            panel.Controls.Add(buttonPanel, 1, 4);

            AddLabel(panel, "已配置表", 5);
            configListBox.Dock = DockStyle.Fill;
            configListBox.SelectionMode = SelectionMode.One;
            panel.Controls.Add(configListBox, 1, 6);

            var closePanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            closeButton.Text = "关闭";
            closePanel.Controls.Add(closeButton);
            panel.Controls.Add(closePanel, 1, 7);

            for (var i = 0; i < 7; i++)
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        }

        private static void AddLabel(TableLayoutPanel panel, string text, int row)
        {
            panel.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleRight }, 0, row);
        }

        private void BindEvents()
        {
            addButton.Click += delegate { AddConfig(); };
            updateButton.Click += delegate { UpdateConfig(); };
            deleteButton.Click += delegate { DeleteConfig(); };
            exportButton.Click += delegate { ExportSql(); };
            closeButton.Click += delegate { Close(); };
            configListBox.SelectedIndexChanged += delegate { LoadSelectedConfig(); };
        }

        private void RefreshConfigList()
        {
            configListBox.Items.Clear();
            foreach (var config in configService.GetAllConfigs())
            {
                configListBox.Items.Add(config.TableName);
            }
        }

        private void LoadSelectedConfig()
        {
            var selectedTableName = configListBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedTableName))
                return;

            var config = configService.GetTableConfig(selectedTableName);
            if (config == null)
                return;

            tableNameBox.Text = config.TableName;
            pkColumnsBox.Text = config.PrimaryKeyColumns != null ? string.Join(",", config.PrimaryKeyColumns) : "";
            autoIncrementBox.Checked = config.IsAutoIncrement;
            incrementalColumnBox.Text = config.IncrementalColumn ?? "";
        }

        private void AddConfig()
        {
            if (string.IsNullOrWhiteSpace(tableNameBox.Text))
            {
                MessageBox.Show("请输入表名");
                return;
            }

            if (string.IsNullOrWhiteSpace(pkColumnsBox.Text))
            {
                MessageBox.Show("请输入主键字段");
                return;
            }

            var config = new TablePrimaryKeyConfig
            {
                TableName = tableNameBox.Text,
                PrimaryKeyColumns = pkColumnsBox.Text.Split(','),
                IsAutoIncrement = autoIncrementBox.Checked,
                IncrementalColumn = incrementalColumnBox.Text
            };

            configService.AddTableConfig(config);
            RefreshConfigList();
            MessageBox.Show("添加成功");
        }

        private void UpdateConfig()
        {
            var selectedTableName = configListBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedTableName))
            {
                MessageBox.Show("请先选择要修改的表");
                return;
            }

            if (string.IsNullOrWhiteSpace(tableNameBox.Text))
            {
                MessageBox.Show("请输入表名");
                return;
            }

            if (string.IsNullOrWhiteSpace(pkColumnsBox.Text))
            {
                MessageBox.Show("请输入主键字段");
                return;
            }

            var config = new TablePrimaryKeyConfig
            {
                TableName = tableNameBox.Text,
                PrimaryKeyColumns = pkColumnsBox.Text.Split(','),
                IsAutoIncrement = autoIncrementBox.Checked,
                IncrementalColumn = incrementalColumnBox.Text
            };

            configService.AddTableConfig(config);
            RefreshConfigList();
            MessageBox.Show("修改成功");
        }

        private void DeleteConfig()
        {
            var selectedTableName = configListBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedTableName))
            {
                MessageBox.Show("请先选择要删除的表");
                return;
            }

            if (MessageBox.Show("确定要删除表 [" + selectedTableName + "] 的主键配置吗？", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                configService.RemoveTableConfig(selectedTableName);
                RefreshConfigList();
                MessageBox.Show("删除成功");
            }
        }

        private void ExportSql()
        {
            var sql = configService.ExportToSql();
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fd_pk_config.sql");
            System.IO.File.WriteAllText(path, sql);
            MessageBox.Show("已导出到：" + path);
        }
    }
}
