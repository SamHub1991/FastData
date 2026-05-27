using System;
using System.Windows.Forms;
using FastData.Database;
using FastData.SyncTool.WinForms.IoC;
using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 数据库配置 UserControl
    /// </summary>
    public class DbConfigControl : UserControl
    {
        private readonly TextBox connectionNameBox = new TextBox();
        private readonly ComboBox providerBox = new ComboBox();
        private readonly TextBox connectionStringBox = new TextBox();
        private readonly Button testButton = new Button();
        private readonly Button saveButton = new Button();
        private readonly Button deleteButton = new Button();
        private readonly DataGridView connectionGrid = new DataGridView();
        private readonly TextBox logBox = new TextBox();

        private readonly SyncConfigManager configManager;

        public DbConfigControl(SyncConfigManager configManager)
        {
            this.configManager = configManager;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;

            // 连接名称
            AddLabel(mainPanel, "连接名称", row++);
            connectionNameBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(connectionNameBox, 1, row - 1);

            // Provider
            AddLabel(mainPanel, "数据库类型", row++);
            providerBox.DropDownStyle = ComboBoxStyle.DropDownList;
            providerBox.Dock = DockStyle.Fill;
            providerBox.Items.AddRange(DatabaseProviderMappings.AllProviderNames);
            providerBox.SelectedIndex = 0;
            mainPanel.Controls.Add(providerBox, 1, row - 1);

            // 连接字符串
            AddLabel(mainPanel, "连接字符串", row++);
            connectionStringBox.Dock = DockStyle.Fill;
            connectionStringBox.Multiline = true;
            connectionStringBox.ScrollBars = ScrollBars.Vertical;
            connectionStringBox.Height = 80;
            mainPanel.Controls.Add(connectionStringBox, 1, row - 1);

            // 按钮
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            testButton.Text = "测试连接";
            testButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            testButton.ForeColor = System.Drawing.Color.White;
            testButton.Margin = new Padding(0, 0, 10, 0);
            buttonPanel.Controls.Add(testButton);

            saveButton.Text = "保存连接";
            saveButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            saveButton.ForeColor = System.Drawing.Color.White;
            saveButton.Margin = new Padding(0, 0, 10, 0);
            buttonPanel.Controls.Add(saveButton);

            deleteButton.Text = "删除连接";
            deleteButton.BackColor = System.Drawing.Color.FromArgb(200, 50, 50);
            deleteButton.ForeColor = System.Drawing.Color.White;
            buttonPanel.Controls.Add(deleteButton);

            mainPanel.Controls.Add(buttonPanel, 1, row++);

            // 连接列表
            AddLabel(mainPanel, "已保存的连接", row++);
            InitConnectionGrid(connectionGrid);
            connectionGrid.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(connectionGrid, 1, row++);

            // 日志
            AddLabel(mainPanel, "数据库配置日志", row++);
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Both;
            logBox.ReadOnly = true;
            logBox.Font = new System.Drawing.Font("Consolas", 9);
            logBox.Height = 100;
            mainPanel.Controls.Add(logBox, 1, row++);

            for (var i = 0; i < 7; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(mainPanel);

            // 绑定事件
            testButton.Click += TestButton_Click;
            saveButton.Click += SaveButton_Click;
            deleteButton.Click += DeleteButton_Click;
        }

        private void AddLabel(TableLayoutPanel panel, string text, int row)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(5, 5, 0, 0)
            };
            panel.Controls.Add(label, 0, row);
        }

        private void InitConnectionGrid(DataGridView grid)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "连接名称", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Provider", HeaderText = "数据库类型", Width = 200 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ConnectionString", HeaderText = "连接字符串", Width = 400 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastTestTime", HeaderText = "最后测试时间", Width = 150 });
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            // 测试连接逻辑
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // 保存连接逻辑
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            // 删除连接逻辑
        }
    }
}
