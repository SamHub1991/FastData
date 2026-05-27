using System;
using System.Windows.Forms;
using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms
{
    public class SyncConfigPanel : UserControl
    {
        private readonly CheckBox enableTimeRangeBox = new CheckBox();
        private readonly NumericUpDown rangeDaysBox = new NumericUpDown();
        private readonly TextBox timeColumnBox = new TextBox();
        private readonly Label lastSyncTimeLabel = new Label();

        private readonly CheckBox enableGlobalConfigBox = new CheckBox();
        private readonly NumericUpDown globalRangeDaysBox = new NumericUpDown();

        private readonly CheckBox alwaysDeduplicateBox = new CheckBox();
        private readonly TextBox primaryKeyColumnsBox = new TextBox();

        private readonly NumericUpDown batchSizeBox = new NumericUpDown();
        private readonly NumericUpDown retryCountBox = new NumericUpDown();

        private readonly CheckBox autoCreateIntermediateBox = new CheckBox();
        private readonly CheckBox resumeFailedRecordsBox = new CheckBox();
        private readonly CheckBox cleanIntermediateBox = new CheckBox();

        public event Action OnConfigChanged;

        public SyncConfigPanel()
        {
            InitControls();
            BindEvents();
        }

        private void InitControls()
        {
            Dock = DockStyle.Fill;
            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 14 };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int r = 0;
            AddHeader(main, "时间范围", r++);
            AddRow(main, r++, "启用时间范围:", enableTimeRangeBox);
            rangeDaysBox.Minimum = 1; rangeDaysBox.Maximum = 365; rangeDaysBox.Value = 3; rangeDaysBox.Width = 60;
            AddRow(main, r++, "同步天数:", rangeDaysBox);
            AddRow(main, r++, "时间字段:", timeColumnBox);
            AddRow(main, r++, "上次同步:", lastSyncTimeLabel);

            AddHeader(main, "全局配置", r++);
            AddRow(main, r++, "启用全局配置:", enableGlobalConfigBox);
            globalRangeDaysBox.Minimum = 0; globalRangeDaysBox.Maximum = 365; globalRangeDaysBox.Value = 0; globalRangeDaysBox.Width = 60;
            AddRow(main, r++, "全局天数:", globalRangeDaysBox);

            AddHeader(main, "去重 / 主键", r++);
            AddRow(main, r++, "始终去重:", alwaysDeduplicateBox);
            primaryKeyColumnsBox.Width = 200;
            AddRow(main, r++, "业务主键:", primaryKeyColumnsBox);

            AddHeader(main, "批量 / 重试", r++);
            batchSizeBox.Minimum = 100; batchSizeBox.Maximum = 10000; batchSizeBox.Increment = 100; batchSizeBox.Value = 500; batchSizeBox.Width = 80;
            AddRow(main, r++, "批量大小:", batchSizeBox);
            retryCountBox.Minimum = 0; retryCountBox.Maximum = 10; retryCountBox.Value = 3; retryCountBox.Width = 60;
            AddRow(main, r++, "重试次数:", retryCountBox);

            AddHeader(main, "中间库", r++);
            var interPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            autoCreateIntermediateBox.Text = "自动创建"; autoCreateIntermediateBox.AutoSize = true;
            resumeFailedRecordsBox.Text = "断点续传"; resumeFailedRecordsBox.AutoSize = true;
            cleanIntermediateBox.Text = "完成后清理"; cleanIntermediateBox.AutoSize = true;
            interPanel.Controls.Add(autoCreateIntermediateBox);
            interPanel.Controls.Add(resumeFailedRecordsBox);
            interPanel.Controls.Add(cleanIntermediateBox);
            AddRow(main, r++, "选项:", interPanel);

            Controls.Add(main);
        }

        private void AddHeader(TableLayoutPanel p, string text, int row)
        {
            var l = new Label { Text = "=== " + text + " ===", Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold), AutoSize = true };
            p.Controls.Add(l, 0, row); p.SetColumnSpan(l, 2);
        }

        private void AddRow(TableLayoutPanel p, int row, string label, Control ctrl)
        {
            p.Controls.Add(new Label { Text = label, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row);
            p.Controls.Add(ctrl, 1, row);
        }

        private void BindEvents()
        {
            enableTimeRangeBox.CheckedChanged += (s, e) => OnConfigChanged?.Invoke();
            rangeDaysBox.ValueChanged += (s, e) => OnConfigChanged?.Invoke();
            timeColumnBox.TextChanged += (s, e) => OnConfigChanged?.Invoke();
            enableGlobalConfigBox.CheckedChanged += (s, e) => OnConfigChanged?.Invoke();
            globalRangeDaysBox.ValueChanged += (s, e) => OnConfigChanged?.Invoke();
            alwaysDeduplicateBox.CheckedChanged += (s, e) => OnConfigChanged?.Invoke();
            primaryKeyColumnsBox.TextChanged += (s, e) => OnConfigChanged?.Invoke();
            batchSizeBox.ValueChanged += (s, e) => OnConfigChanged?.Invoke();
            retryCountBox.ValueChanged += (s, e) => OnConfigChanged?.Invoke();
        }

        public void LoadFromTask(SyncTaskConfig config)
        {
            if (config == null) return;
            enableTimeRangeBox.Checked = config.EnableTimeRange;
            rangeDaysBox.Value = config.RangeDays;
            timeColumnBox.Text = config.TimeColumn ?? "";
            lastSyncTimeLabel.Text = config.LastSyncTime.HasValue ? config.LastSyncTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "未同步";
            primaryKeyColumnsBox.Text = config.PrimaryKeyColumns ?? "";
        }

        public void LoadFromTableConfig(TableSyncConfig tc)
        {
            if (tc == null) return;
            enableTimeRangeBox.Checked = tc.EnableTimeRange;
            rangeDaysBox.Value = tc.RangeDays;
            timeColumnBox.Text = tc.TimeColumn ?? "";
            enableGlobalConfigBox.Checked = tc.EnableGlobalConfig;
            globalRangeDaysBox.Value = tc.GlobalRangeDays;
            alwaysDeduplicateBox.Checked = tc.AlwaysDeduplicate;
            if (tc.SyncColumns != null)
                primaryKeyColumnsBox.Text = string.Join(",", tc.SyncColumns);
        }

        public void ApplyToOptions(DataSyncOptions options)
        {
            options.EnableTimeRange = enableTimeRangeBox.Checked;
            options.RangeDays = (int)rangeDaysBox.Value;
            options.TimeColumn = timeColumnBox.Text;
            options.EnableGlobalConfig = enableGlobalConfigBox.Checked;
            options.GlobalRangeDays = (int)globalRangeDaysBox.Value;
            options.AlwaysDeduplicate = alwaysDeduplicateBox.Checked;
            options.PrimaryKeyColumns = primaryKeyColumnsBox.Text;
            options.BatchSize = (int)batchSizeBox.Value;
            options.RetryCount = (int)retryCountBox.Value;
            options.AutoCreateIntermediateSchema = autoCreateIntermediateBox.Checked;
            options.ResumeFailedRecords = resumeFailedRecordsBox.Checked;
            options.CleanIntermediateData = cleanIntermediateBox.Checked;
        }

        public void LoadFromOptions(DataSyncOptions options)
        {
            if (options == null) return;
            enableTimeRangeBox.Checked = options.EnableTimeRange;
            rangeDaysBox.Value = options.RangeDays;
            timeColumnBox.Text = options.TimeColumn ?? "";
            enableGlobalConfigBox.Checked = options.EnableGlobalConfig;
            globalRangeDaysBox.Value = options.GlobalRangeDays;
            alwaysDeduplicateBox.Checked = options.AlwaysDeduplicate;
            primaryKeyColumnsBox.Text = options.PrimaryKeyColumns ?? "";
            batchSizeBox.Value = options.BatchSize;
            retryCountBox.Value = options.RetryCount;
            autoCreateIntermediateBox.Checked = options.AutoCreateIntermediateSchema;
            resumeFailedRecordsBox.Checked = options.ResumeFailedRecords;
            cleanIntermediateBox.Checked = options.CleanIntermediateData;
        }
    }
}
