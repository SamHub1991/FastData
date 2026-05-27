using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms
{
    public class TableListManager : UserControl
    {
        private readonly DataGridView tableGrid = new DataGridView();
        private readonly Button addTableButton = new Button();
        private readonly Button removeTableButton = new Button();
        private readonly Button moveUpButton = new Button();
        private readonly Button moveDownButton = new Button();

        public event Action OnTablesChanged;

        public TableListManager()
        {
            InitControls();
            InitGrid();
            BindEvents();
        }

        private void InitControls()
        {
            Dock = DockStyle.Fill;
            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 35, AutoSize = true };
            SetButton(addTableButton, "添加", 60);
            SetButton(removeTableButton, "删除", 60);
            SetButton(moveUpButton, "↑", 40);
            SetButton(moveDownButton, "↓", 40);
            buttonPanel.Controls.AddRange(new Control[] { addTableButton, removeTableButton, moveUpButton, moveDownButton });
            tableGrid.Dock = DockStyle.Fill;
            Controls.Add(tableGrid);
            Controls.Add(buttonPanel);
        }

        private void SetButton(Button btn, string text, int width) { btn.Text = text; btn.Width = width; btn.Margin = new Padding(2); }

        private void InitGrid()
        {
            tableGrid.AllowUserToAddRows = false;
            tableGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tableGrid.MultiSelect = false;
            tableGrid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "表名", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "TargetTableName", HeaderText = "目标表名", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "PrimaryKeyColumns", HeaderText = "主键字段", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "TimeColumn", HeaderText = "时间字段", Width = 100 },
                new DataGridViewCheckBoxColumn { Name = "IsEnabled", HeaderText = "启用", Width = 50 },
                new DataGridViewCheckBoxColumn { Name = "EnableTimeRange", HeaderText = "时间范围", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "RangeDays", HeaderText = "天数", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "SyncColumns", HeaderText = "同步列", Width = 150 },
                new DataGridViewCheckBoxColumn { Name = "AlwaysDeduplicate", HeaderText = "去重", Width = 50 },
            });
        }

        private void BindEvents()
        {
            addTableButton.Click += (s, e) => AddTable();
            removeTableButton.Click += (s, e) => RemoveTable();
            moveUpButton.Click += (s, e) => MoveTable(-1);
            moveDownButton.Click += (s, e) => MoveTable(1);
            tableGrid.SelectionChanged += (s, e) => UpdateButtonState();
            tableGrid.CellValueChanged += (s, e) => OnTablesChanged?.Invoke();
        }

        private void AddTable()
        {
            var dlg = new TableSelectorDialog();
            if (dlg.ShowDialog() == DialogResult.OK && dlg.SelectedTables.Count > 0)
            {
                foreach (var t in dlg.SelectedTables)
                    tableGrid.Rows.Add(t, "", "", "", true, false, 3, "*", true);
                OnTablesChanged?.Invoke();
            }
        }

        private void RemoveTable()
        {
            if (tableGrid.SelectedRows.Count > 0)
            {
                tableGrid.Rows.RemoveAt(tableGrid.SelectedRows[0].Index);
                OnTablesChanged?.Invoke();
            }
        }

        private void MoveTable(int dir)
        {
            if (tableGrid.SelectedRows.Count == 0) return;
            int idx = tableGrid.SelectedRows[0].Index;
            int newIdx = idx + dir;
            if (newIdx < 0 || newIdx >= tableGrid.Rows.Count) return;
            var row = tableGrid.Rows[idx];
            tableGrid.Rows.RemoveAt(idx);
            tableGrid.Rows.Insert(newIdx, row);
            tableGrid.Rows[newIdx].Selected = true;
            OnTablesChanged?.Invoke();
        }

        private void UpdateButtonState()
        {
            bool sel = tableGrid.SelectedRows.Count > 0;
            removeTableButton.Enabled = sel;
            moveUpButton.Enabled = sel && tableGrid.SelectedRows[0].Index > 0;
            moveDownButton.Enabled = sel && tableGrid.SelectedRows[0].Index < tableGrid.Rows.Count - 1;
        }

        public List<TableSyncConfig> GetTableConfigs()
        {
            var list = new List<TableSyncConfig>();
            foreach (DataGridViewRow row in tableGrid.Rows)
            {
                if (row.IsNewRow) continue;
                var syncColsStr = row.Cells["SyncColumns"].Value?.ToString() ?? "*";
                list.Add(new TableSyncConfig
                {
                    TableName = row.Cells["TableName"].Value?.ToString(),
                    TargetTableName = row.Cells["TargetTableName"].Value?.ToString(),
                    PrimaryKeyColumns = row.Cells["PrimaryKeyColumns"].Value?.ToString(),
                    TimeColumn = row.Cells["TimeColumn"].Value?.ToString(),
                    IsEnabled = row.Cells["IsEnabled"].Value as bool? ?? true,
                    EnableTimeRange = row.Cells["EnableTimeRange"].Value as bool? ?? false,
                    RangeDays = row.Cells["RangeDays"].Value as int? ?? 3,
                    SyncColumns = syncColsStr == "*" ? null : new List<string>(syncColsStr.Split(',')),
                    AlwaysDeduplicate = row.Cells["AlwaysDeduplicate"].Value as bool? ?? true,
                });
            }
            return list;
        }

        public void SetTableConfigs(IList<TableSyncConfig> configs)
        {
            tableGrid.Rows.Clear();
            if (configs == null) return;
            foreach (var c in configs)
            {
                var syncColsStr = c.SyncColumns != null && c.SyncColumns.Count > 0 ? string.Join(",", c.SyncColumns) : "*";
                tableGrid.Rows.Add(c.TableName, c.TargetTableName ?? "", c.PrimaryKeyColumns ?? "", c.TimeColumn ?? "", c.IsEnabled, c.EnableTimeRange, c.RangeDays, syncColsStr, c.AlwaysDeduplicate);
            }
        }
    }
}
