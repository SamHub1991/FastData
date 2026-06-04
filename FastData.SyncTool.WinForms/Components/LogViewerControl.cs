using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FastData.SyncTool.WinForms.Services;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 运行日志查看器 UserControl
    /// 提供日志级别筛选、着色显示、清空、导出功能
    /// </summary>
    public partial class LogViewerControl : UserControl
    {
        #region 颜色常量

        private static readonly Color ToolbarBgColor = Color.FromArgb(250, 251, 252);
        private static readonly Color DividerColor = Color.FromArgb(238, 238, 238);
        private static readonly Color LogBgColor = Color.FromArgb(30, 30, 30);
        private static readonly Color LogFgColor = Color.FromArgb(220, 220, 220);
        private static readonly Color PrimaryColor = Color.FromArgb(0, 120, 212);
        private static readonly Color PrimaryLightColor = Color.FromArgb(200, 220, 240);
        private static readonly Color TextSecondaryColor = Color.FromArgb(117, 117, 117);

        #endregion

        #region 控件

        private readonly RichTextBox _logBox = new RichTextBox();
        private readonly ComboBox _logLevelFilter = new ComboBox();
        private readonly LogService _logService;

        #endregion

        #region 属性

        /// <summary>
        /// 日志总条数（按行估算）
        /// </summary>
        public int LogCount => _logBox.Lines.Length - 1;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务实例</param>
        public LogViewerControl(LogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            InitializeComponent();
            BindEvents();
            LoadExistingLogs();
        }

        /// <summary>
        /// 初始化 UI 控件
        /// </summary>
        private void InitializeComponent()
        {
            BackColor = Color.White;
            Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            // ---- 工具栏 ----
            var toolbarPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ToolbarBgColor
            };
            toolbarPanel.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(DividerColor), 0, toolbarPanel.Height - 1, toolbarPanel.Width, toolbarPanel.Height - 1);

            var filterLabel = new Label
            {
                Text = "日志级别:",
                AutoSize = true,
                Location = new Point(12, 12),
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = TextSecondaryColor
            };
            toolbarPanel.Controls.Add(filterLabel);

            _logLevelFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _logLevelFilter.Width = 85;
            _logLevelFilter.Location = new Point(75, 9);
            _logLevelFilter.Font = new Font("Microsoft YaHei UI", 8.5F);
            _logLevelFilter.FlatStyle = FlatStyle.Flat;
            _logLevelFilter.Items.AddRange(new object[] { "全部", "DEBUG", "INFO", "WARN", "ERROR" });
            _logLevelFilter.SelectedIndex = 2;
            toolbarPanel.Controls.Add(_logLevelFilter);

            var clearBtn = CreateToolButton("清空", 60, 175, 8, ClearLog);
            toolbarPanel.Controls.Add(clearBtn);

            var exportBtn = CreateToolButton("导出", 60, 245, 8, ExportLog);
            toolbarPanel.Controls.Add(exportBtn);

            mainPanel.Controls.Add(toolbarPanel, 0, 0);

            // ---- 日志文本框 ----
            _logBox.ReadOnly = true;
            _logBox.Font = new Font("Consolas", 9F);
            _logBox.BackColor = LogBgColor;
            _logBox.ForeColor = LogFgColor;
            _logBox.WordWrap = false;
            _logBox.ScrollBars = RichTextBoxScrollBars.Both;
            _logBox.BorderStyle = BorderStyle.None;
            _logBox.Dock = DockStyle.Fill;
            mainPanel.Controls.Add(_logBox, 0, 1);

            // ---- 底部计数栏 ----
            var statusBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ToolbarBgColor
            };
            statusBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(DividerColor), 0, 0, statusBar.Width, 0);

            var countLabel = new Label
            {
                Name = "_countLabel",
                Text = "共 0 条日志",
                AutoSize = true,
                Location = new Point(12, 6),
                Font = new Font("Microsoft YaHei UI", 8F),
                ForeColor = TextSecondaryColor
            };
            statusBar.Controls.Add(countLabel);
            mainPanel.Controls.Add(statusBar, 0, 2);

            Controls.Add(mainPanel);
        }

        /// <summary>
        /// 绑定事件
        /// </summary>
        private void BindEvents()
        {
            _logLevelFilter.SelectedIndexChanged += OnLogLevelChanged;
            _logService.EntryAdded += OnLogEntryAdded;
        }

        /// <summary>
        /// 创建工具栏按钮
        /// </summary>
        private static Button CreateToolButton(string text, int width, int x, int y, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 26,
                Location = new Point(x, y),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = PrimaryColor,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = PrimaryColor;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = PrimaryLightColor;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 210, 235);
            if (onClick != null) btn.Click += (s, e) => onClick();
            return btn;
        }

        /// <summary>
        /// 加载已有日志
        /// </summary>
        private void LoadExistingLogs()
        {
            _logBox.Clear();
            foreach (var entry in _logService.GetEntries())
                AppendEntryNoEvent(entry);
            UpdateCountLabel();
        }

        /// <summary>
        /// 日志条目添加事件
        /// </summary>
        private void OnLogEntryAdded(object sender, LogEntry entry)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => AppendEntry(entry)));
                return;
            }
            AppendEntry(entry);
        }

        /// <summary>
        /// 追加日志条目（带着色）
        /// </summary>
        private void AppendEntry(LogEntry entry)
        {
            AppendEntryNoEvent(entry);
            UpdateCountLabel();
        }

        /// <summary>
        /// 追加日志条目（不触发计数更新）
        /// </summary>
        private void AppendEntryNoEvent(LogEntry entry)
        {
            var color = GetLogColor(entry.Level);
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor = color;
            _logBox.AppendText(entry.ToString() + Environment.NewLine);
            _logBox.SelectionColor = _logBox.ForeColor;
            _logBox.ScrollToCaret();
        }

        /// <summary>
        /// 获取日志级别对应颜色
        /// </summary>
        private static Color GetLogColor(LogEntryLevel level)
        {
            switch (level)
            {
                case LogEntryLevel.Debug: return Color.Gray;
                case LogEntryLevel.Info: return Color.FromArgb(220, 220, 220);
                case LogEntryLevel.Warn: return Color.Yellow;
                case LogEntryLevel.Error: return Color.Red;
                default: return Color.White;
            }
        }

        /// <summary>
        /// 日志级别筛选变更
        /// </summary>
        private void OnLogLevelChanged(object sender, EventArgs e)
        {
            switch (_logLevelFilter.SelectedIndex)
            {
                case 0: _logService.MinLevel = LogEntryLevel.Debug; break;
                case 1: _logService.MinLevel = LogEntryLevel.Debug; break;
                case 2: _logService.MinLevel = LogEntryLevel.Info; break;
                case 3: _logService.MinLevel = LogEntryLevel.Warn; break;
                case 4: _logService.MinLevel = LogEntryLevel.Error; break;
            }
            LoadExistingLogs();
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        private void ClearLog()
        {
            _logBox.Clear();
            _logService.Clear();
            UpdateCountLabel();
        }

        /// <summary>
        /// 导出日志到文件
        /// </summary>
        private void ExportLog()
        {
            using (var dlg = new SaveFileDialog
            {
                Filter = "日志文件|*.log|文本文件|*.txt",
                Title = "导出日志",
                FileName = string.Format("sync_log_{0:yyyyMMdd_HHmmss}.log", DateTime.Now)
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _logService.ExportLogs(dlg.FileName);
                    MessageBox.Show("日志已导出到: " + dlg.FileName, "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// 更新日志计数显示
        /// </summary>
        private void UpdateCountLabel()
        {
            var countLabel = Controls.Find("_countLabel", true).FirstOrDefault() as Label;
            if (countLabel != null)
                countLabel.Text = string.Format("共 {0} 条日志", LogCount);
        }
    }
}