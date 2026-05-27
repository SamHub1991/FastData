using System;
using System.Drawing;
using System.Windows.Forms;
using FastData.Sharding;
using FastData.SyncTool.WinForms.Models;
using FastData.SyncTool.WinForms.Services;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 分表任务管理控件
    /// 支持后台执行、暂停、取消、删除
    /// </summary>
    public class ShardingTaskControl : UserControl
    {
        private readonly ShardingTaskService _taskService;
        private readonly LogService _logService;

        // 任务列表
        private DataGridView _taskGrid;

        // 操作按钮
        private Button _pauseButton;
        private Button _resumeButton;
        private Button _cancelButton;
        private Button _deleteButton;
        private Button _refreshButton;

        // 状态栏
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;

        // 定时刷新
        private Timer _refreshTimer;

        public ShardingTaskControl(ShardingTaskService taskService, LogService logService)
        {
            _taskService = taskService;
            _logService = logService;

            InitializeComponent();

            // 订阅事件
            _taskService.TaskStatusChanged += OnTaskStatusChanged;
            _taskService.TaskProgressChanged += OnTaskProgressChanged;

            // 定时刷新
            _refreshTimer = new Timer { Interval = 1000 };
            _refreshTimer.Tick += (s, e) => RefreshTaskList();
            _refreshTimer.Start();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // 主布局
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            // 标题和操作按钮
            var headerPanel = new Panel { Dock = DockStyle.Fill };

            var titleLabel = new Label
            {
                Text = "分表任务列表",
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 15)
            };
            headerPanel.Controls.Add(titleLabel);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight
            };

            _refreshButton = CreateButton("刷新", Color.FromArgb(0, 120, 215), Color.White);
            _refreshButton.Click += (s, e) => RefreshTaskList();
            buttonPanel.Controls.Add(_refreshButton);

            _pauseButton = CreateButton("暂停", Color.FromArgb(255, 185, 0), Color.Black);
            _pauseButton.Click += PauseTask;
            buttonPanel.Controls.Add(_pauseButton);

            _resumeButton = CreateButton("恢复", Color.FromArgb(76, 175, 80), Color.White);
            _resumeButton.Click += ResumeTask;
            buttonPanel.Controls.Add(_resumeButton);

            _cancelButton = CreateButton("取消", Color.FromArgb(244, 67, 54), Color.White);
            _cancelButton.Click += CancelTask;
            buttonPanel.Controls.Add(_cancelButton);

            _deleteButton = CreateButton("删除", Color.FromArgb(158, 158, 158), Color.White);
            _deleteButton.Click += DeleteTask;
            buttonPanel.Controls.Add(_deleteButton);

            headerPanel.Controls.Add(buttonPanel);
            mainLayout.Controls.Add(headerPanel, 0, 0);

            // 任务列表
            _taskGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            _taskGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TaskId",
                HeaderText = "任务ID",
                Visible = false
            });
            _taskGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TaskName",
                HeaderText = "任务名称",
                Width = 150
            });
            _taskGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "SourceTable",
                HeaderText = "源表",
                Width = 100
            });
            _taskGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ShardingType",
                HeaderText = "分表类型",
                Width = 80
            });
            _taskGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "状态",
                Width = 80
            });
            _taskGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Progress",
                HeaderText = "进度",
                Width = 80
            });
            _taskGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Records",
                HeaderText = "记录数",
                Width = 120
            });
            _taskGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Time",
                HeaderText = "耗时",
                Width = 100
            });

            _taskGrid.SelectionChanged += TaskGrid_SelectionChanged;
            mainLayout.Controls.Add(_taskGrid, 0, 1);

            // 状态栏
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel { Text = "就绪" };
            _statusStrip.Items.Add(_statusLabel);
            mainLayout.Controls.Add(_statusStrip, 0, 2);

            Controls.Add(mainLayout);
            ResumeLayout();
        }

        private Button CreateButton(string text, Color backColor, Color foreColor)
        {
            return new Button
            {
                Text = text,
                Width = 60,
                Height = 30,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
        }

        private void TaskGrid_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (_taskGrid.SelectedRows.Count == 0)
            {
                _pauseButton.Enabled = false;
                _resumeButton.Enabled = false;
                _cancelButton.Enabled = false;
                _deleteButton.Enabled = false;
                return;
            }

            var taskId = _taskGrid.SelectedRows[0].Cells["TaskId"].Value?.ToString();
            var task = _taskService.GetTask(taskId);

            if (task == null)
            {
                _pauseButton.Enabled = false;
                _resumeButton.Enabled = false;
                _cancelButton.Enabled = false;
                _deleteButton.Enabled = false;
                return;
            }

            _pauseButton.Enabled = task.Status == ShardingTaskStatus.Running;
            _resumeButton.Enabled = task.Status == ShardingTaskStatus.Paused;
            _cancelButton.Enabled = task.Status == ShardingTaskStatus.Running || task.Status == ShardingTaskStatus.Paused;
            _deleteButton.Enabled = task.Status != ShardingTaskStatus.Running;
        }

        private void PauseTask(object sender, EventArgs e)
        {
            if (_taskGrid.SelectedRows.Count > 0)
            {
                var taskId = _taskGrid.SelectedRows[0].Cells["TaskId"].Value?.ToString();
                _taskService.PauseTask(taskId);
            }
        }

        private void ResumeTask(object sender, EventArgs e)
        {
            if (_taskGrid.SelectedRows.Count > 0)
            {
                var taskId = _taskGrid.SelectedRows[0].Cells["TaskId"].Value?.ToString();
                _taskService.ResumeTask(taskId);
            }
        }

        private void CancelTask(object sender, EventArgs e)
        {
            if (_taskGrid.SelectedRows.Count > 0)
            {
                var taskId = _taskGrid.SelectedRows[0].Cells["TaskId"].Value?.ToString();
                var task = _taskService.GetTask(taskId);

                if (task != null && (task.Status == ShardingTaskStatus.Running || task.Status == ShardingTaskStatus.Paused))
                {
                    var result = MessageBox.Show(
                        $"确定要取消任务 [{task.TaskName}] 吗？",
                        "确认取消",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        _taskService.CancelTask(taskId);
                    }
                }
            }
        }

        private void DeleteTask(object sender, EventArgs e)
        {
            if (_taskGrid.SelectedRows.Count > 0)
            {
                var taskId = _taskGrid.SelectedRows[0].Cells["TaskId"].Value?.ToString();
                var task = _taskService.GetTask(taskId);

                if (task != null)
                {
                    if (task.Status == ShardingTaskStatus.Running)
                    {
                        MessageBox.Show("运行中的任务不能删除，请先取消任务。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"确定要删除任务 [{task.TaskName}] 吗？",
                        "确认删除",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        _taskService.DeleteTask(taskId);
                        RefreshTaskList();
                    }
                }
            }
        }

        private void RefreshTaskList()
        {
            try
            {
                var selectedTaskId = _taskGrid.SelectedRows.Count > 0
                    ? _taskGrid.SelectedRows[0].Cells["TaskId"].Value?.ToString()
                    : null;

                var tasks = _taskService.GetAllTasks();

                _taskGrid.Rows.Clear();

                foreach (var task in tasks)
                {
                    var rowIndex = _taskGrid.Rows.Add(
                        task.TaskId,
                        task.TaskName,
                        task.SourceTable,
                        task.ShardingType,
                        GetStatusText(task.Status),
                        $"{task.ProgressPercentage:F1}%",
                        $"{task.SuccessRecords}/{task.TotalRecords}",
                        GetElapsedTime(task)
                    );

                    // 设置状态颜色
                    var statusCell = _taskGrid.Rows[rowIndex].Cells["Status"];
                    switch (task.Status)
                    {
                        case ShardingTaskStatus.Running:
                            statusCell.Style.ForeColor = Color.FromArgb(0, 120, 215);
                            break;
                        case ShardingTaskStatus.Paused:
                            statusCell.Style.ForeColor = Color.FromArgb(255, 185, 0);
                            break;
                        case ShardingTaskStatus.Completed:
                            statusCell.Style.ForeColor = Color.FromArgb(76, 175, 80);
                            break;
                        case ShardingTaskStatus.Cancelled:
                        case ShardingTaskStatus.Failed:
                            statusCell.Style.ForeColor = Color.FromArgb(244, 67, 54);
                            break;
                    }

                    // 恢复选中
                    if (task.TaskId == selectedTaskId)
                    {
                        _taskGrid.Rows[rowIndex].Selected = true;
                    }
                }

                _statusLabel.Text = $"共 {tasks.Count} 个任务";
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                _logService.Error($"刷新任务列表失败: {ex.Message}");
            }
        }

        private string GetStatusText(ShardingTaskStatus status)
        {
            switch (status)
            {
                case ShardingTaskStatus.Pending:
                    return "等待中";
                case ShardingTaskStatus.Running:
                    return "运行中";
                case ShardingTaskStatus.Paused:
                    return "已暂停";
                case ShardingTaskStatus.Completed:
                    return "已完成";
                case ShardingTaskStatus.Cancelled:
                    return "已取消";
                case ShardingTaskStatus.Failed:
                    return "失败";
                default:
                    return status.ToString();
            }
        }

        private string GetElapsedTime(ShardingTaskInfo task)
        {
            if (!task.StartTime.HasValue)
                return "-";

            var endTime = task.CompleteTime ?? DateTime.Now;
            var elapsed = endTime - task.StartTime.Value;

            if (elapsed.TotalHours >= 1)
                return $"{elapsed.TotalHours:F1}小时";
            if (elapsed.TotalMinutes >= 1)
                return $"{elapsed.TotalMinutes:F1}分钟";
            return $"{elapsed.TotalSeconds:F0}秒";
        }

        private void OnTaskStatusChanged(ShardingTaskInfo task)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnTaskStatusChanged(task)));
                return;
            }

            RefreshTaskList();
            _logService.Info($"任务状态变更: {task.TaskName} -> {task.Status}");
        }

        private void OnTaskProgressChanged(ShardingTaskInfo task)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnTaskProgressChanged(task)));
                return;
            }

            // 更新对应行的进度
            foreach (DataGridViewRow row in _taskGrid.Rows)
            {
                if (row.Cells["TaskId"].Value?.ToString() == task.TaskId)
                {
                    row.Cells["Progress"].Value = $"{task.ProgressPercentage:F1}%";
                    row.Cells["Records"].Value = $"{task.SuccessRecords}/{task.TotalRecords}";
                    row.Cells["Time"].Value = GetElapsedTime(task);
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _taskService.TaskStatusChanged -= OnTaskStatusChanged;
                _taskService.TaskProgressChanged -= OnTaskProgressChanged;
            }
            base.Dispose(disposing);
        }
    }
}
