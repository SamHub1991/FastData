using System;
using System.Windows.Forms;
using FastData.SyncTool.WinForms.IoC;
using FastData.SyncTool.WinForms.Services;
using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 任务管理 UserControl
    /// </summary>
    public class TaskManagerControl : UserControl
    {
        private readonly DataGridView taskListGrid = new DataGridView();
        private readonly Button newTaskButton = new Button();
        private readonly Button deleteTaskButton = new Button();
        private readonly Button loadTaskButton = new Button();
        private readonly Button refreshTaskButton = new Button();
        private readonly Button editTaskButton = new Button();
        private readonly Button batchDeleteButton = new Button();
        private readonly Button batchEnableButton = new Button();
        private readonly Button batchDisableButton = new Button();
        private readonly Button exportTaskButton = new Button();
        private readonly Button importTaskButton = new Button();

        private readonly SyncConfigManager configManager;
        private readonly TaskSchedulerService schedulerService;

        public TaskManagerControl(SyncConfigManager configManager, TaskSchedulerService schedulerService)
        {
            this.configManager = configManager;
            this.schedulerService = schedulerService;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 按钮面板
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            newTaskButton.Text = "新建任务";
            newTaskButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            newTaskButton.ForeColor = System.Drawing.Color.White;
            newTaskButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(newTaskButton);

            deleteTaskButton.Text = "删除任务";
            deleteTaskButton.BackColor = System.Drawing.Color.FromArgb(200, 50, 50);
            deleteTaskButton.ForeColor = System.Drawing.Color.White;
            deleteTaskButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(deleteTaskButton);

            loadTaskButton.Text = "加载任务";
            loadTaskButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(loadTaskButton);

            editTaskButton.Text = "编辑任务";
            editTaskButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(editTaskButton);

            refreshTaskButton.Text = "刷新";
            refreshTaskButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(refreshTaskButton);

            batchDeleteButton.Text = "批量删除";
            batchDeleteButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(batchDeleteButton);

            batchEnableButton.Text = "批量启用";
            batchEnableButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(batchEnableButton);

            batchDisableButton.Text = "批量禁用";
            batchDisableButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(batchDisableButton);

            exportTaskButton.Text = "导出任务";
            exportTaskButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(exportTaskButton);

            importTaskButton.Text = "导入任务";
            importTaskButton.Margin = new Padding(0, 0, 5, 0);
            buttonPanel.Controls.Add(importTaskButton);

            mainPanel.Controls.Add(buttonPanel, 0, 0);

            // 任务列表
            InitTaskGrid(taskListGrid);
            taskListGrid.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(taskListGrid, 0, 1);

            Controls.Add(mainPanel);

            // 绑定事件
            newTaskButton.Click += NewTaskButton_Click;
            deleteTaskButton.Click += DeleteTaskButton_Click;
            loadTaskButton.Click += LoadTaskButton_Click;
            editTaskButton.Click += EditTaskButton_Click;
            refreshTaskButton.Click += RefreshTaskButton_Click;
            batchDeleteButton.Click += BatchDeleteButton_Click;
            batchEnableButton.Click += BatchEnableButton_Click;
            batchDisableButton.Click += BatchDisableButton_Click;
            exportTaskButton.Click += ExportTaskButton_Click;
            importTaskButton.Click += ImportTaskButton_Click;
        }

        private void InitTaskGrid(DataGridView grid)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.MultiSelect = true;

            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsEnabled", HeaderText = "启用", Width = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaskId", HeaderText = "任务 ID", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaskName", HeaderText = "任务名称", Width = 200 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SourceTable", HeaderText = "源表", Width = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TargetTable", HeaderText = "目标表", Width = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastSyncTime", HeaderText = "最后同步时间", Width = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "状态", Width = 80 });
        }

        private void NewTaskButton_Click(object sender, EventArgs e)
        {
            // 新建任务逻辑
        }

        private void DeleteTaskButton_Click(object sender, EventArgs e)
        {
            // 删除任务逻辑
        }

        private void LoadTaskButton_Click(object sender, EventArgs e)
        {
            // 加载任务逻辑
        }

        private void EditTaskButton_Click(object sender, EventArgs e)
        {
            // 编辑任务逻辑
        }

        private void RefreshTaskButton_Click(object sender, EventArgs e)
        {
            // 刷新任务列表
        }

        private void BatchDeleteButton_Click(object sender, EventArgs e)
        {
            // 批量删除逻辑
        }

        private void BatchEnableButton_Click(object sender, EventArgs e)
        {
            // 批量启用逻辑
        }

        private void BatchDisableButton_Click(object sender, EventArgs e)
        {
            // 批量禁用逻辑
        }

        private void ExportTaskButton_Click(object sender, EventArgs e)
        {
            // 导出任务逻辑
        }

        private void ImportTaskButton_Click(object sender, EventArgs e)
        {
            // 导入任务逻辑
        }
    }
}
