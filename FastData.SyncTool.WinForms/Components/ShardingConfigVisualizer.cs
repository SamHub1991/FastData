using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FastData.Sharding;
using FastData.SyncTool.WinForms.Models;
using Newtonsoft.Json;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 分表配置可视化编辑器
    /// 提供配置的导入、导出、验证、编辑功能
    /// </summary>
    public class ShardingConfigVisualizer : UserControl
    {
        // 配置列表
        private DataGridView _configGrid;

        // 配置详情
        private PropertyGrid _detailGrid;

        // 操作按钮
        private Button _importButton;
        private Button _exportButton;
        private Button _validateButton;
        private Button _addButton;
        private Button _editButton;
        private Button _deleteButton;
        private Button _applyButton;

        // 状态
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;

        // 当前快照
        private ShardingConfigSnapshot _currentSnapshot = new ShardingConfigSnapshot();

        /// <summary>
        /// 配置变更事件
        /// </summary>
        public event Action<ShardingConfigSnapshot> ConfigChanged;

        public ShardingConfigVisualizer()
        {
            InitializeComponent();
            LoadDefaultConfigs();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // 主布局
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 工具栏
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };

            _addButton = CreateButton("添加配置", Color.FromArgb(76, 175, 80), Color.White);
            _addButton.Click += AddConfig;
            toolbar.Controls.Add(_addButton);

            _editButton = CreateButton("编辑配置", Color.FromArgb(0, 120, 215), Color.White);
            _editButton.Click += EditConfig;
            toolbar.Controls.Add(_editButton);

            _deleteButton = CreateButton("删除配置", Color.FromArgb(244, 67, 54), Color.White);
            _deleteButton.Click += DeleteConfig;
            toolbar.Controls.Add(_deleteButton);

            toolbar.Controls.Add(new Label { Width = 20 }); // 分隔符

            _importButton = CreateButton("导入配置", Color.FromArgb(156, 39, 176), Color.White);
            _importButton.Click += ImportConfig;
            toolbar.Controls.Add(_importButton);

            _exportButton = CreateButton("导出配置", Color.FromArgb(255, 152, 0), Color.White);
            _exportButton.Click += ExportConfig;
            toolbar.Controls.Add(_exportButton);

            toolbar.Controls.Add(new Label { Width = 20 }); // 分隔符

            _validateButton = CreateButton("验证一致性", Color.FromArgb(63, 81, 181), Color.White);
            _validateButton.Click += ValidateConfig;
            toolbar.Controls.Add(_validateButton);

            _applyButton = CreateButton("应用到分表", Color.FromArgb(0, 150, 136), Color.White);
            _applyButton.Click += ApplyConfig;
            toolbar.Controls.Add(_applyButton);

            mainLayout.Controls.Add(toolbar, 0, 0);
            mainLayout.SetColumnSpan(toolbar, 2);

            // 配置列表
            var configGroup = new GroupBox
            {
                Text = "分表配置列表",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            _configGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            _configGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "表名", Width = 120 });
            _configGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShardingType", HeaderText = "分表类型", Width = 100 });
            _configGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "配置描述", Width = 200 });
            _configGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "状态", Width = 80 });

            _configGrid.SelectionChanged += ConfigGrid_SelectionChanged;
            configGroup.Controls.Add(_configGrid);
            mainLayout.Controls.Add(configGroup, 0, 1);

            // 配置详情
            var detailGroup = new GroupBox
            {
                Text = "配置详情",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };

            _detailGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.Categorized
            };

            detailGroup.Controls.Add(_detailGrid);
            mainLayout.Controls.Add(detailGroup, 1, 1);

            // 状态栏
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel { Text = "就绪" };
            _statusStrip.Items.Add(_statusLabel);

            Controls.Add(mainLayout);
            Controls.Add(_statusStrip);

            ResumeLayout();
        }

        private Button CreateButton(string text, Color backColor, Color foreColor)
        {
            return new Button
            {
                Text = text,
                Width = 90,
                Height = 35,
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
        }

        private void LoadDefaultConfigs()
        {
            _currentSnapshot = new ShardingConfigSnapshot
            {
                Name = "默认配置",
                Description = "示例分表配置",
                Environment = "开发环境"
            };

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _configGrid.Rows.Clear();

            foreach (var config in _currentSnapshot.Configs)
            {
                var rowIndex = _configGrid.Rows.Add(
                    config.BaseTableName,
                    config.ShardingType,
                    config.GetDescription(),
                    "已配置"
                );

                // 设置类型颜色
                var typeCell = _configGrid.Rows[rowIndex].Cells["ShardingType"];
                switch (config.ShardingType)
                {
                    case "Time":
                        typeCell.Style.ForeColor = Color.FromArgb(0, 120, 215);
                        break;
                    case "Hash":
                        typeCell.Style.ForeColor = Color.FromArgb(76, 175, 80);
                        break;
                    case "List":
                        typeCell.Style.ForeColor = Color.FromArgb(255, 152, 0);
                        break;
                    case "Composite":
                        typeCell.Style.ForeColor = Color.FromArgb(156, 39, 176);
                        break;
                    case "QueryFrequency":
                        typeCell.Style.ForeColor = Color.FromArgb(244, 67, 54);
                        break;
                }
            }

            _statusLabel.Text = $"共 {_currentSnapshot.Configs.Count} 个配置";
        }

        private void ConfigGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (_configGrid.SelectedRows.Count > 0)
            {
                var tableName = _configGrid.SelectedRows[0].Cells["TableName"].Value?.ToString();
                var config = _currentSnapshot.Configs.FirstOrDefault(c => c.BaseTableName == tableName);

                if (config != null)
                {
                    _detailGrid.SelectedObject = config;
                }
            }
        }

        private void AddConfig(object sender, EventArgs e)
        {
            var dialog = new ShardingConfigEditDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var config = dialog.GetConfigItem();

                // 检查是否已存在
                if (_currentSnapshot.Configs.Any(c => c.BaseTableName == config.BaseTableName))
                {
                    MessageBox.Show($"表 {config.BaseTableName} 的配置已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _currentSnapshot.Configs.Add(config);
                RefreshGrid();
                ConfigChanged?.Invoke(_currentSnapshot);

                _statusLabel.Text = $"已添加配置: {config.BaseTableName}";
            }
        }

        private void EditConfig(object sender, EventArgs e)
        {
            if (_configGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择要编辑的配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tableName = _configGrid.SelectedRows[0].Cells["TableName"].Value?.ToString();
            var config = _currentSnapshot.Configs.FirstOrDefault(c => c.BaseTableName == tableName);

            if (config != null)
            {
                var dialog = new ShardingConfigEditDialog(config);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var updatedConfig = dialog.GetConfigItem();
                    var index = _currentSnapshot.Configs.IndexOf(config);
                    _currentSnapshot.Configs[index] = updatedConfig;

                    RefreshGrid();
                    ConfigChanged?.Invoke(_currentSnapshot);

                    _statusLabel.Text = $"已更新配置: {updatedConfig.BaseTableName}";
                }
            }
        }

        private void DeleteConfig(object sender, EventArgs e)
        {
            if (_configGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择要删除的配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tableName = _configGrid.SelectedRows[0].Cells["TableName"].Value?.ToString();
            var result = MessageBox.Show($"确定要删除表 {tableName} 的配置吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _currentSnapshot.Configs.RemoveAll(c => c.BaseTableName == tableName);
                RefreshGrid();
                ConfigChanged?.Invoke(_currentSnapshot);

                _statusLabel.Text = $"已删除配置: {tableName}";
            }
        }

        private void ImportConfig(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "JSON 文件|*.json|所有文件|*.*";
                dialog.Title = "导入分表配置";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var json = File.ReadAllText(dialog.FileName);
                        var snapshot = JsonConvert.DeserializeObject<ShardingConfigSnapshot>(json);

                        if (snapshot != null)
                        {
                            var confirmResult = MessageBox.Show(
                                $"导入配置:\n\n" +
                                $"名称: {snapshot.Name}\n" +
                                $"环境: {snapshot.Environment}\n" +
                                $"配置数量: {snapshot.Configs.Count}\n\n" +
                                $"是否导入？",
                                "确认导入",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (confirmResult == DialogResult.Yes)
                            {
                                _currentSnapshot = snapshot;
                                RefreshGrid();
                                ConfigChanged?.Invoke(_currentSnapshot);

                                _statusLabel.Text = $"已导入配置: {snapshot.Configs.Count} 个";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportConfig(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "JSON 文件|*.json";
                dialog.Title = "导出分表配置";
                dialog.FileName = $"sharding-config-{DateTime.Now:yyyyMMddHHmmss}.json";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var json = JsonConvert.SerializeObject(_currentSnapshot, Formatting.Indented);
                        File.WriteAllText(dialog.FileName, json);

                        _statusLabel.Text = $"已导出配置: {dialog.FileName}";
                        MessageBox.Show($"配置已导出到:\n{dialog.FileName}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ValidateConfig(object sender, EventArgs e)
        {
            // 弹出文件选择对话框，选择开发环境的配置文件
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "JSON 文件|*.json|所有文件|*.*";
                dialog.Title = "选择开发环境配置文件";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var json = File.ReadAllText(dialog.FileName);
                        var devSnapshot = JsonConvert.DeserializeObject<ShardingConfigSnapshot>(json);

                        if (devSnapshot != null)
                        {
                            var result = _currentSnapshot.Validate(devSnapshot);

                            var message = $"配置一致性验证结果:\n\n" +
                                         $"当前环境: {_currentSnapshot.Environment}\n" +
                                         $"目标环境: {devSnapshot.Environment}\n\n" +
                                         $"{result.GetSummary()}";

                            if (result.IsValid)
                            {
                                MessageBox.Show(message, "验证通过", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show(message, "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }

                            _statusLabel.Text = result.IsValid ? "验证通过" : "验证失败";
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"验证失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ApplyConfig(object sender, EventArgs e)
        {
            if (_currentSnapshot.Configs.Count == 0)
            {
                MessageBox.Show("没有配置可应用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"确定要应用 {_currentSnapshot.Configs.Count} 个分表配置吗？\n\n" +
                $"这将覆盖当前的分表配置。",
                "确认应用",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    foreach (var configItem in _currentSnapshot.Configs)
                    {
                        var config = configItem.ToShardingConfig();
                        ShardingManager.Configure<object>(config);
                    }

                    _statusLabel.Text = $"已应用 {_currentSnapshot.Configs.Count} 个配置";
                    MessageBox.Show($"已成功应用 {_currentSnapshot.Configs.Count} 个分表配置", "应用成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ConfigChanged?.Invoke(_currentSnapshot);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"应用配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 设置当前快照
        /// </summary>
        public void SetSnapshot(ShardingConfigSnapshot snapshot)
        {
            _currentSnapshot = snapshot;
            RefreshGrid();
        }

        /// <summary>
        /// 获取当前快照
        /// </summary>
        public ShardingConfigSnapshot GetSnapshot()
        {
            return _currentSnapshot;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 分表配置编辑对话框
    /// </summary>
    public class ShardingConfigEditDialog : Form
    {
        private ComboBox _shardingTypeCombo;
        private TextBox _tableNameText;

        // 时间分表
        private Panel _timePanel;
        private TextBox _timeFieldText;
        private ComboBox _timeGranularityCombo;
        private DateTimePicker _startTimePicker;

        // 哈希分表
        private Panel _hashPanel;
        private TextBox _hashFieldText;
        private NumericUpDown _shardCountNumeric;
        private ComboBox _hashAlgorithmCombo;

        // 列表分表
        private Panel _listPanel;
        private TextBox _listFieldText;
        private DataGridView _listMappingGrid;

        // 组合键分表
        private Panel _compositePanel;
        private TextBox _compositeFieldsText;
        private NumericUpDown _compositeShardCountNumeric;

        // 查询频率分表
        private Panel _frequencyPanel;
        private TextBox _frequencyFieldText;
        private NumericUpDown _hotThresholdNumeric;
        private TextBox _hotSuffixText;
        private TextBox _coldSuffixText;

        private Button _okButton;
        private Button _cancelButton;

        private ShardingConfigItem _editingConfig;

        public ShardingConfigEditDialog(ShardingConfigItem existingConfig = null)
        {
            _editingConfig = existingConfig;
            InitializeComponent();

            if (existingConfig != null)
            {
                LoadConfig(existingConfig);
            }
        }

        private void InitializeComponent()
        {
            Text = _editingConfig == null ? "添加分表配置" : "编辑分表配置";
            Size = new Size(500, 450);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(15)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 表名
            layout.Controls.Add(new Label { Text = "表名:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            _tableNameText = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(_tableNameText, 1, 0);

            // 分表类型
            layout.Controls.Add(new Label { Text = "分表类型:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            _shardingTypeCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _shardingTypeCombo.Items.AddRange(new object[] { "时间分表", "哈希分表", "列表分表", "组合键分表", "查询频率分表" });
            _shardingTypeCombo.SelectedIndex = 0;
            _shardingTypeCombo.SelectedIndexChanged += ShardingTypeChanged;
            layout.Controls.Add(_shardingTypeCombo, 1, 1);

            // 配置面板
            var configPanel = new Panel { Dock = DockStyle.Fill };
            InitializeConfigPanels(configPanel);
            layout.Controls.Add(configPanel, 0, 2);
            layout.SetColumnSpan(configPanel, 2);

            // 按钮
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40
            };

            _cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 80 };
            buttonPanel.Controls.Add(_cancelButton);

            _okButton = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 80 };
            _okButton.Click += OkButton_Click;
            buttonPanel.Controls.Add(_okButton);

            Controls.Add(layout);
            Controls.Add(buttonPanel);

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        private void InitializeConfigPanels(Panel container)
        {
            // 时间分表面板
            _timePanel = new Panel { Dock = DockStyle.Fill, Visible = true };
            var timeLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(5) };
            timeLayout.Controls.Add(new Label { Text = "时间字段:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            _timeFieldText = new TextBox { Dock = DockStyle.Fill, Text = "CreateTime" };
            timeLayout.Controls.Add(_timeFieldText, 1, 0);
            timeLayout.Controls.Add(new Label { Text = "时间粒度:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            _timeGranularityCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _timeGranularityCombo.Items.AddRange(new object[] { "Day", "Week", "Month", "Quarter", "Year" });
            _timeGranularityCombo.SelectedIndex = 2;
            timeLayout.Controls.Add(_timeGranularityCombo, 1, 1);
            timeLayout.Controls.Add(new Label { Text = "起始时间:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            _startTimePicker = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            timeLayout.Controls.Add(_startTimePicker, 1, 2);
            _timePanel.Controls.Add(timeLayout);
            container.Controls.Add(_timePanel);

            // 哈希分表面板
            _hashPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            var hashLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(5) };
            hashLayout.Controls.Add(new Label { Text = "哈希字段:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            _hashFieldText = new TextBox { Dock = DockStyle.Fill, Text = "UserId" };
            hashLayout.Controls.Add(_hashFieldText, 1, 0);
            hashLayout.Controls.Add(new Label { Text = "分表数量:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            _shardCountNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 100, Value = 4 };
            hashLayout.Controls.Add(_shardCountNumeric, 1, 1);
            hashLayout.Controls.Add(new Label { Text = "哈希算法:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            _hashAlgorithmCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _hashAlgorithmCombo.Items.AddRange(new object[] { "Modulo", "Consistent", "Crc32" });
            _hashAlgorithmCombo.SelectedIndex = 0;
            hashLayout.Controls.Add(_hashAlgorithmCombo, 1, 2);
            _hashPanel.Controls.Add(hashLayout);
            container.Controls.Add(_hashPanel);

            // 列表分表面板
            _listPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            var listLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(5) };
            listLayout.Controls.Add(new Label { Text = "列表字段:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            _listFieldText = new TextBox { Dock = DockStyle.Fill, Text = "Status" };
            listLayout.Controls.Add(_listFieldText, 1, 0);
            listLayout.Controls.Add(new Label { Text = "值映射:", TextAlign = ContentAlignment.TopRight }, 0, 1);
            _listMappingGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                ColumnCount = 2,
                Columns =
                {
                    new DataGridViewTextBoxColumn { HeaderText = "值", Name = "Value" },
                    new DataGridViewTextBoxColumn { HeaderText = "表后缀", Name = "Suffix" }
                }
            };
            _listMappingGrid.Rows.Add("Pending", "pending");
            _listMappingGrid.Rows.Add("Processing", "processing");
            _listMappingGrid.Rows.Add("Completed", "completed");
            _listMappingGrid.Rows.Add("Cancelled", "cancelled");
            listLayout.Controls.Add(_listMappingGrid, 1, 1);
            _listPanel.Controls.Add(listLayout);
            container.Controls.Add(_listPanel);

            // 组合键分表面板
            _compositePanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            var compositeLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(5) };
            compositeLayout.Controls.Add(new Label { Text = "组合字段:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            _compositeFieldsText = new TextBox { Dock = DockStyle.Fill, Text = "Region,CustomerType" };
            compositeLayout.Controls.Add(_compositeFieldsText, 1, 0);
            compositeLayout.Controls.Add(new Label { Text = "分表数量:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            _compositeShardCountNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 100, Value = 16 };
            compositeLayout.Controls.Add(_compositeShardCountNumeric, 1, 1);
            _compositePanel.Controls.Add(compositeLayout);
            container.Controls.Add(_compositePanel);

            // 查询频率分表面板
            _frequencyPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            var frequencyLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(5) };
            frequencyLayout.Controls.Add(new Label { Text = "频率字段:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            _frequencyFieldText = new TextBox { Dock = DockStyle.Fill, Text = "UserId" };
            frequencyLayout.Controls.Add(_frequencyFieldText, 1, 0);
            frequencyLayout.Controls.Add(new Label { Text = "热数据阈值:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            _hotThresholdNumeric = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 1000000, Value = 50 };
            frequencyLayout.Controls.Add(_hotThresholdNumeric, 1, 1);
            frequencyLayout.Controls.Add(new Label { Text = "热数据后缀:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            _hotSuffixText = new TextBox { Dock = DockStyle.Fill, Text = "_hot" };
            frequencyLayout.Controls.Add(_hotSuffixText, 1, 2);
            frequencyLayout.Controls.Add(new Label { Text = "冷数据后缀:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            _coldSuffixText = new TextBox { Dock = DockStyle.Fill, Text = "_cold" };
            frequencyLayout.Controls.Add(_coldSuffixText, 1, 3);
            _frequencyPanel.Controls.Add(frequencyLayout);
            container.Controls.Add(_frequencyPanel);
        }

        private void ShardingTypeChanged(object sender, EventArgs e)
        {
            _timePanel.Visible = _shardingTypeCombo.SelectedIndex == 0;
            _hashPanel.Visible = _shardingTypeCombo.SelectedIndex == 1;
            _listPanel.Visible = _shardingTypeCombo.SelectedIndex == 2;
            _compositePanel.Visible = _shardingTypeCombo.SelectedIndex == 3;
            _frequencyPanel.Visible = _shardingTypeCombo.SelectedIndex == 4;
        }

        private void LoadConfig(ShardingConfigItem config)
        {
            _tableNameText.Text = config.BaseTableName;

            switch (config.ShardingType)
            {
                case "Time":
                    _shardingTypeCombo.SelectedIndex = 0;
                    _timeFieldText.Text = config.TimeField;
                    _timeGranularityCombo.SelectedItem = config.TimeGranularity;
                    if (config.StartTime.HasValue) _startTimePicker.Value = config.StartTime.Value;
                    break;

                case "Hash":
                    _shardingTypeCombo.SelectedIndex = 1;
                    _hashFieldText.Text = config.HashField;
                    _shardCountNumeric.Value = config.ShardCount;
                    _hashAlgorithmCombo.SelectedItem = config.HashAlgorithm;
                    break;

                case "List":
                    _shardingTypeCombo.SelectedIndex = 2;
                    _listFieldText.Text = config.ListField;
                    _listMappingGrid.Rows.Clear();
                    if (config.ValueMapping != null)
                    {
                        foreach (var mapping in config.ValueMapping)
                        {
                            _listMappingGrid.Rows.Add(mapping.Key, mapping.Value);
                        }
                    }
                    break;

                case "Composite":
                    _shardingTypeCombo.SelectedIndex = 3;
                    _compositeFieldsText.Text = config.CompositeFields;
                    _compositeShardCountNumeric.Value = config.ShardCount;
                    break;

                case "QueryFrequency":
                    _shardingTypeCombo.SelectedIndex = 4;
                    _frequencyFieldText.Text = config.FrequencyField;
                    _hotThresholdNumeric.Value = config.HotThreshold;
                    _hotSuffixText.Text = config.HotSuffix;
                    _coldSuffixText.Text = config.ColdSuffix;
                    break;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_tableNameText.Text))
            {
                MessageBox.Show("请输入表名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
        }

        public ShardingConfigItem GetConfigItem()
        {
            var config = new ShardingConfigItem
            {
                BaseTableName = _tableNameText.Text.Trim()
            };

            switch (_shardingTypeCombo.SelectedIndex)
            {
                case 0: // 时间分表
                    config.ShardingType = "Time";
                    config.TimeField = _timeFieldText.Text;
                    config.TimeGranularity = _timeGranularityCombo.SelectedItem.ToString();
                    config.StartTime = _startTimePicker.Value;
                    break;

                case 1: // 哈希分表
                    config.ShardingType = "Hash";
                    config.HashField = _hashFieldText.Text;
                    config.ShardCount = (int)_shardCountNumeric.Value;
                    config.HashAlgorithm = _hashAlgorithmCombo.SelectedItem.ToString();
                    break;

                case 2: // 列表分表
                    config.ShardingType = "List";
                    config.ListField = _listFieldText.Text;
                    config.ValueMapping = new Dictionary<string, string>();
                    foreach (DataGridViewRow row in _listMappingGrid.Rows)
                    {
                        if (row.Cells[0].Value != null && row.Cells[1].Value != null)
                        {
                            config.ValueMapping[row.Cells[0].Value.ToString()] = row.Cells[1].Value.ToString();
                        }
                    }
                    break;

                case 3: // 组合键分表
                    config.ShardingType = "Composite";
                    config.CompositeFields = _compositeFieldsText.Text;
                    config.ShardCount = (int)_compositeShardCountNumeric.Value;
                    break;

                case 4: // 查询频率分表
                    config.ShardingType = "QueryFrequency";
                    config.FrequencyField = _frequencyFieldText.Text;
                    config.HotThreshold = (long)_hotThresholdNumeric.Value;
                    config.HotSuffix = _hotSuffixText.Text;
                    config.ColdSuffix = _coldSuffixText.Text;
                    break;
            }

            return config;
        }
    }
}
