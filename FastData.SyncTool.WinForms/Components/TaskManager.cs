using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms
{
    public class TaskManager : UserControl
    {
        private readonly DataGridView taskGrid = new DataGridView();
        private readonly Button newTaskButton = new Button();
        private readonly Button deleteTaskButton = new Button();
        private readonly Button refreshButton = new Button();

        public event Action OnTaskSelected;
        public event Action OnTaskDeleted;

        private readonly SyncConfigManager configManager;

        public TaskManager(SyncConfigManager configMgr)
        {
            configManager = configMgr;
            InitControls();
            InitGrid();
            BindEvents();
            LoadTaskList();
        }

        private void InitControls()
        {
            Dock = DockStyle.Fill;
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 35, AutoSize = true };
            SetButton(newTaskButton, "新建", 60);
            SetButton(deleteTaskButton, "删除", 60);
            SetButton(refreshButton, "刷新", 60);
            buttonPanel.Controls.AddRange(new Control[] { newTaskButton, deleteTaskButton, refreshButton });
            taskGrid.Dock = DockStyle.Fill;
            Controls.Add(taskGrid);
            Controls.Add(buttonPanel);
        }

        private void SetButton(Button btn, string text, int width) { btn.Text = text; btn.Width = width; btn.Margin = new Padding(2); }

        private void InitGrid()
        {
            taskGrid.AllowUserToAddRows = false;
            taskGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            taskGrid.MultiSelect = false;
            taskGrid.ReadOnly = true;
            taskGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "TaskId", HeaderText = "任务ID", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "TaskName", HeaderText = "任务名称", Width = 150 },
                new DataGridViewCheckBoxColumn { Name = "IsEnabled", HeaderText = "启用", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "SourceTable", HeaderText = "源表", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "TargetTable", HeaderText = "目标表", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "LastSyncTime", HeaderText = "上次同步", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "LastSyncStatus", HeaderText = "状态", Width = 80 },
            });
        }

        private void BindEvents()
        {
            newTaskButton.Click += (s, e) => CreateNewTask();
            deleteTaskButton.Click += (s, e) => DeleteSelectedTask();
            refreshButton.Click += (s, e) => LoadTaskList();
            taskGrid.SelectionChanged += (s, e) => OnTaskSelected?.Invoke();
        }

        public void LoadTaskList()
        {
            taskGrid.Rows.Clear();
            var tasks = configManager.GetAllConfigs();
            foreach (var t in tasks)
            {
                taskGrid.Rows.Add(t.TaskId, t.TaskName, t.IsEnabled, t.SourceTable, t.TargetTable,
                    t.LastSyncTime.HasValue ? t.LastSyncTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    t.LastSyncStatus ?? "");
            }
        }

        private void CreateNewTask()
        {
            using (var form = new Form())
            {
                form.Text = "新建任务";
                form.Size = new System.Drawing.Size(400, 150);
                form.StartPosition = FormStartPosition.CenterParent;
                var label = new Label { Text = "任务ID:", Location = new System.Drawing.Point(20, 20), AutoSize = true };
                var textBox = new TextBox { Location = new System.Drawing.Point(100, 17), Width = 250 };
                var btnOk = new Button { Text = "确定", Location = new System.Drawing.Point(120, 60), Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Location = new System.Drawing.Point(210, 60), Width = 75, DialogResult = DialogResult.Cancel };
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;
                form.Controls.AddRange(new Control[] { label, textBox, btnOk, btnCancel });
                if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(textBox.Text))
                {
                    var id = textBox.Text.Trim();
                    var newTask = new SyncTaskConfig
                    {
                        TaskId = id,
                        TaskName = id,
                        IsEnabled = true,
                        RangeDays = 3,
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now,
                    };
                    configManager.SaveTaskConfig(newTask);
                    LoadTaskList();
                    SelectTask(id);
                }
            }
        }

        private void DeleteSelectedTask()
        {
            if (taskGrid.SelectedRows.Count == 0) return;
            var id = taskGrid.SelectedRows[0].Cells["TaskId"].Value.ToString();
            if (MessageBox.Show("确定要删除任务 [" + id + "] 吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                configManager.DeleteTaskConfig(id);
                LoadTaskList();
                OnTaskDeleted?.Invoke();
            }
        }

        public string GetSelectedTaskId()
        {
            if (taskGrid.SelectedRows.Count == 0) return null;
            return taskGrid.SelectedRows[0].Cells["TaskId"].Value.ToString();
        }

        public void SelectTask(string taskId)
        {
            foreach (DataGridViewRow row in taskGrid.Rows)
            {
                if (row.Cells["TaskId"].Value.ToString() == taskId)
                {
                    row.Selected = true;
                    taskGrid.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }
    }
}
