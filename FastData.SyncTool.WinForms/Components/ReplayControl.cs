using System;
using System.Windows.Forms;
using FastData.Database;
using FastData.SyncTool.WinForms.IoC;
using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 数据补录 UserControl
    /// </summary>
    public class ReplayControl : UserControl
    {
        private readonly ComboBox sourceProviderBox = new ComboBox();
        private readonly TextBox sourceConnectionBox = new TextBox();
        private readonly ComboBox targetProviderBox = new ComboBox();
        private readonly TextBox targetConnectionBox = new TextBox();
        private readonly TextBox tableNameBox = new TextBox();
        private readonly DateTimePicker startTimeBox = new DateTimePicker();
        private readonly DateTimePicker endTimeBox = new DateTimePicker();
        private readonly CheckBox enableTimeRangeBox = new CheckBox();
        private readonly TextBox primaryKeyBox = new TextBox();
        private readonly Button loadTablesButton = new Button();
        private readonly Button startButton = new Button();
        private readonly TextBox logBox = new TextBox();
        private readonly Label statusLabel = new Label();

        private readonly SyncConfigManager configManager;

        public ReplayControl(SyncConfigManager configManager)
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
                RowCount = 12
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;

            // 源库配置
            AddLabel(mainPanel, "源库 Provider", row++);
            InitProviderBox(sourceProviderBox);
            mainPanel.Controls.Add(sourceProviderBox, 1, row - 1);

            AddLabel(mainPanel, "源库连接字符串", row++);
            sourceConnectionBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(sourceConnectionBox, 1, row - 1);

            // 目标库配置
            AddLabel(mainPanel, "目标库 Provider", row++);
            InitProviderBox(targetProviderBox);
            mainPanel.Controls.Add(targetProviderBox, 1, row - 1);

            AddLabel(mainPanel, "目标库连接字符串", row++);
            targetConnectionBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(targetConnectionBox, 1, row - 1);

            // 表名
            AddLabel(mainPanel, "表名", row++);
            var tablePanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            tableNameBox.Width = 200;
            tablePanel.Controls.Add(tableNameBox);
            loadTablesButton.Text = "加载表";
            loadTablesButton.Margin = new Padding(10, 0, 0, 0);
            tablePanel.Controls.Add(loadTablesButton);
            mainPanel.Controls.Add(tablePanel, 1, row - 1);

            // 主键字段
            AddLabel(mainPanel, "主键字段 (逗号分隔)", row++);
            primaryKeyBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(primaryKeyBox, 1, row - 1);

            // 时间范围
            AddLabel(mainPanel, "启用时间范围", row++);
            enableTimeRangeBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(enableTimeRangeBox, 1, row - 1);

            AddLabel(mainPanel, "开始时间", row++);
            startTimeBox.Dock = DockStyle.Fill;
            startTimeBox.Format = DateTimePickerFormat.Custom;
            startTimeBox.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            mainPanel.Controls.Add(startTimeBox, 1, row - 1);

            AddLabel(mainPanel, "结束时间", row++);
            endTimeBox.Dock = DockStyle.Fill;
            endTimeBox.Format = DateTimePickerFormat.Custom;
            endTimeBox.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            mainPanel.Controls.Add(endTimeBox, 1, row - 1);

            // 按钮
            AddLabel(mainPanel, "操作", row++);
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
            startButton.Text = "开始补录";
            startButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            startButton.ForeColor = System.Drawing.Color.White;
            buttonPanel.Controls.Add(startButton);
            mainPanel.Controls.Add(buttonPanel, 1, row - 1);

            // 状态
            AddLabel(mainPanel, "状态", row++);
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Text = "就绪";
            mainPanel.Controls.Add(statusLabel, 1, row - 1);

            // 日志
            AddLabel(mainPanel, "补录日志", row++);
            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Both;
            logBox.ReadOnly = true;
            logBox.Font = new System.Drawing.Font("Consolas", 9);
            logBox.Height = 200;
            mainPanel.Controls.Add(logBox, 1, row++);

            for (var i = 0; i < 11; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(mainPanel);

            // 绑定事件
            loadTablesButton.Click += LoadTablesButton_Click;
            startButton.Click += StartButton_Click;
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

        private void InitProviderBox(ComboBox box)
        {
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Dock = DockStyle.Fill;
            box.Items.AddRange(DatabaseProviderMappings.AllProviderNames);
            box.SelectedIndex = 0;
        }

        private void LoadTablesButton_Click(object sender, EventArgs e)
        {
            // 加载表逻辑
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            // 开始补录逻辑
        }
    }
}
