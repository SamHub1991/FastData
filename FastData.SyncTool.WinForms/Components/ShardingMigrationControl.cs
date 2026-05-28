using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FastData.Sharding;
using FastData.SyncTool.WinForms.Services;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 分表迁移管理组件
    /// 提供历史数据迁移到分表的功能
    /// </summary>
    public class ShardingMigrationControl : UserControl
    {
        private readonly DatabaseService _dbService;
        private readonly LogService _logService;

        // UI 控件
        private GroupBox _configGroup;
        private GroupBox _migrationGroup;
        private GroupBox _statusGroup;

        // 配置控件
        private ComboBox _sourceTableCombo;
        private ComboBox _shardingTypeCombo;
        private TextBox _baseTableNameText;
        private TextBox _timeFieldText;
        private ComboBox _timeGranularityCombo;
        private TextBox _hashFieldText;
        private NumericUpDown _shardCountNumeric;
        private TextBox _listFieldText;
        private DataGridView _listMappingGrid;

        // 迁移控件
        private NumericUpDown _batchSizeNumeric;
        private NumericUpDown _maxRecordsNumeric;
        private CheckBox _createTableCheck;
        private Button _startButton;
        private Button _cancelButton;
        private ProgressBar _progressBar;
        private Label _progressLabel;

        // 状态控件
        private DataGridView _statusGrid;
        private TextBox _logText;

        private CancellationTokenSource _cancellationTokenSource;
        private readonly List<MigrationStatus> _migrationStatuses = new List<MigrationStatus>();

        public ShardingMigrationControl(DatabaseService dbService, LogService logService)
        {
            _dbService = dbService;
            _logService = logService;
            InitializeComponent();
            LoadSourceTables();
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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 250)); // 配置
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200)); // 迁移
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 状态

            // 配置组
            _configGroup = new GroupBox
            {
                Text = "分表配置",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            var configLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6
            };

            // 源表选择
            configLayout.Controls.Add(new Label { Text = "源表:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            _sourceTableCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            configLayout.Controls.Add(_sourceTableCombo, 1, 0);

            // 分表类型
            configLayout.Controls.Add(new Label { Text = "分表类型:", TextAlign = ContentAlignment.MiddleRight }, 2, 0);
            _shardingTypeCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = Enum.GetValues(typeof(ShardingType))
            };
            _shardingTypeCombo.SelectedIndexChanged += ShardingTypeChanged;
            configLayout.Controls.Add(_shardingTypeCombo, 3, 0);

            // 基础表名
            configLayout.Controls.Add(new Label { Text = "基础表名:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            _baseTableNameText = new TextBox { Dock = DockStyle.Fill };
            configLayout.Controls.Add(_baseTableNameText, 1, 1);

            // 时间字段
            configLayout.Controls.Add(new Label { Text = "时间字段:", TextAlign = ContentAlignment.MiddleRight }, 2, 1);
            _timeFieldText = new TextBox { Dock = DockStyle.Fill };
            configLayout.Controls.Add(_timeFieldText, 3, 1);

            // 时间粒度
            configLayout.Controls.Add(new Label { Text = "时间粒度:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            _timeGranularityCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = Enum.GetValues(typeof(TimeGranularity))
            };
            configLayout.Controls.Add(_timeGranularityCombo, 1, 2);

            // 哈希字段
            configLayout.Controls.Add(new Label { Text = "哈希字段:", TextAlign = ContentAlignment.MiddleRight }, 2, 2);
            _hashFieldText = new TextBox { Dock = DockStyle.Fill };
            configLayout.Controls.Add(_hashFieldText, 3, 2);

            // 分表数量
            configLayout.Controls.Add(new Label { Text = "分表数量:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
            _shardCountNumeric = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 1000,
                Value = 8
            };
            configLayout.Controls.Add(_shardCountNumeric, 1, 3);

            // 列表字段
            configLayout.Controls.Add(new Label { Text = "列表字段:", TextAlign = ContentAlignment.MiddleRight }, 2, 3);
            _listFieldText = new TextBox { Dock = DockStyle.Fill };
            configLayout.Controls.Add(_listFieldText, 3, 3);

            // 列表映射
            configLayout.Controls.Add(new Label { Text = "列表映射:", TextAlign = ContentAlignment.MiddleRight }, 0, 4);
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
            configLayout.Controls.Add(_listMappingGrid, 1, 4);
            configLayout.SetColumnSpan(_listMappingGrid, 3);

            _configGroup.Controls.Add(configLayout);
            mainLayout.Controls.Add(_configGroup, 0, 0);

            // 迁移组
            _migrationGroup = new GroupBox
            {
                Text = "迁移设置",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            var migrationLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            migrationLayout.Controls.Add(new Label { Text = "批次大小:", TextAlign = ContentAlignment.MiddleRight });
            _batchSizeNumeric = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 100000,
                Value = 1000,
                Width = 100
            };
            migrationLayout.Controls.Add(_batchSizeNumeric);

            migrationLayout.Controls.Add(new Label { Text = "最大记录数:", TextAlign = ContentAlignment.MiddleRight });
            _maxRecordsNumeric = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 10000000,
                Value = 0,
                Width = 100
            };
            migrationLayout.Controls.Add(_maxRecordsNumeric);

            _createTableCheck = new CheckBox
            {
                Text = "自动创建分表",
                Checked = true
            };
            migrationLayout.Controls.Add(_createTableCheck);

            _startButton = new Button
            {
                Text = "开始迁移",
                Width = 100,
                Height = 30
            };
            _startButton.Click += StartMigration;
            migrationLayout.Controls.Add(_startButton);

            _cancelButton = new Button
            {
                Text = "取消",
                Width = 80,
                Height = 30,
                Enabled = false
            };
            _cancelButton.Click += CancelMigration;
            migrationLayout.Controls.Add(_cancelButton);

            _progressBar = new ProgressBar
            {
                Width = 300,
                Height = 25
            };
            migrationLayout.Controls.Add(_progressBar);

            _progressLabel = new Label
            {
                Text = "就绪",
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 200
            };
            migrationLayout.Controls.Add(_progressLabel);

            _migrationGroup.Controls.Add(migrationLayout);
            mainLayout.Controls.Add(_migrationGroup, 0, 1);

            // 状态组
            _statusGroup = new GroupBox
            {
                Text = "迁移状态",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            var statusLayout = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 150
            };

            _statusGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                Columns =
                {
                    new DataGridViewTextBoxColumn { HeaderText = "分表", Name = "TableName", Width = 150 },
                    new DataGridViewTextBoxColumn { HeaderText = "状态", Name = "Status", Width = 100 },
                    new DataGridViewTextBoxColumn { HeaderText = "记录数", Name = "RecordCount", Width = 100 },
                    new DataGridViewTextBoxColumn { HeaderText = "耗时", Name = "Duration", Width = 100 },
                    new DataGridViewTextBoxColumn { HeaderText = "错误信息", Name = "ErrorMessage", Width = 300 }
                }
            };
            statusLayout.Panel1.Controls.Add(_statusGrid);

            _logText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };
            statusLayout.Panel2.Controls.Add(_logText);

            _statusGroup.Controls.Add(statusLayout);
            mainLayout.Controls.Add(_statusGroup, 0, 2);

            Controls.Add(mainLayout);
            ResumeLayout();
        }

        private void LoadSourceTables()
        {
            try
            {
                var tables = _dbService.GetTableList();
                _sourceTableCombo.DataSource = tables;
                _sourceTableCombo.SelectedIndexChanged += SourceTableChanged;
            }
            catch (Exception ex)
            {
                _logService.Error($"加载表列表失败: {ex.Message}");
            }
        }

        private void SourceTableChanged(object sender, EventArgs e)
        {
            if (_sourceTableCombo.SelectedItem is string tableName)
            {
                _baseTableNameText.Text = tableName;
            }
        }

        private void ShardingTypeChanged(object sender, EventArgs e)
        {
            if (_shardingTypeCombo.SelectedItem is ShardingType shardingType)
            {
                // 根据分表类型显示/隐藏相关配置
                _timeFieldText.Enabled = shardingType == ShardingType.Time || shardingType == ShardingType.QueryFrequency;
                _timeGranularityCombo.Enabled = shardingType == ShardingType.Time;
                _hashFieldText.Enabled = shardingType == ShardingType.Hash;
                _shardCountNumeric.Enabled = shardingType == ShardingType.Hash || shardingType == ShardingType.Composite;
                _listFieldText.Enabled = shardingType == ShardingType.List;
                _listMappingGrid.Enabled = shardingType == ShardingType.List;
            }
        }

        private async void StartMigration(object sender, EventArgs e)
        {
            try
            {
                await StartMigrationAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"迁移失败: {ex.Message}");
                MessageBox.Show($"迁移失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StartMigrationAsync()
        {
            if (_sourceTableCombo.SelectedItem == null)
            {
                MessageBox.Show("请选择源表", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var config = BuildShardingConfig();
            if (config == null)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            _startButton.Enabled = false;
            _cancelButton.Enabled = true;
            _progressBar.Value = 0;
            _progressLabel.Text = "迁移中...";
            _migrationStatuses.Clear();
            _statusGrid.Rows.Clear();
            _logText.Clear();

            try
            {
                await ExecuteMigration(config, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                LogMessage("迁移已取消");
            }
            finally
            {
                _startButton.Enabled = true;
                _cancelButton.Enabled = false;
                _cancellationTokenSource?.Dispose();
            }
        }

        private void CancelMigration(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        private ShardingConfig BuildShardingConfig()
        {
            var shardingType = (ShardingType)_shardingTypeCombo.SelectedItem;
            var config = new ShardingConfig
            {
                BaseTableName = _baseTableNameText.Text,
                ShardingType = shardingType,
                AutoCreateTable = _createTableCheck.Checked
            };

            switch (shardingType)
            {
                case ShardingType.Time:
                    if (string.IsNullOrEmpty(_timeFieldText.Text))
                    {
                        MessageBox.Show("请输入时间字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                    config.TimeConfig = new TimeShardingConfig
                    {
                        TimeField = _timeFieldText.Text,
                        Granularity = (TimeGranularity)_timeGranularityCombo.SelectedItem
                    };
                    break;

                case ShardingType.Hash:
                    if (string.IsNullOrEmpty(_hashFieldText.Text))
                    {
                        MessageBox.Show("请输入哈希字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                    config.HashConfig = new HashShardingConfig
                    {
                        HashField = _hashFieldText.Text,
                        ShardCount = (int)_shardCountNumeric.Value
                    };
                    break;

                case ShardingType.List:
                    if (string.IsNullOrEmpty(_listFieldText.Text))
                    {
                        MessageBox.Show("请输入列表字段", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                    var valueMapping = new Dictionary<string, string>();
                    foreach (DataGridViewRow row in _listMappingGrid.Rows)
                    {
                        if (row.Cells["Value"].Value != null && row.Cells["Suffix"].Value != null)
                        {
                            valueMapping[row.Cells["Value"].Value.ToString()] = row.Cells["Suffix"].Value.ToString();
                        }
                    }
                    config.ListConfig = new ListShardingConfig
                    {
                        ListField = _listFieldText.Text,
                        ValueMapping = valueMapping
                    };
                    break;

                case ShardingType.Composite:
                    config.CompositeConfig = new CompositeShardingConfig
                    {
                        ShardCount = (int)_shardCountNumeric.Value
                    };
                    break;
            }

            return config;
        }

        private async Task ExecuteMigration(ShardingConfig config, CancellationToken cancellationToken)
        {
            var sourceTable = _sourceTableCombo.SelectedItem.ToString();
            var batchSize = (int)_batchSizeNumeric.Value;
            var maxRecords = (int)_maxRecordsNumeric.Value;

            LogMessage($"开始迁移: {sourceTable} -> {config.BaseTableName}");
            LogMessage($"分表类型: {config.ShardingType}");

            // 获取源数据总数
            var totalCount = await _dbService.GetTableCountAsync(sourceTable);
            if (maxRecords > 0 && maxRecords < totalCount)
            {
                totalCount = maxRecords;
            }

            LogMessage($"源表记录数: {totalCount}");

            // 注册分表配置
            ShardingManager.Configure<object>(config);

            // 获取所有分表名称
            var tableNames = ShardingManager.GetAllTableNames<object>();
            LogMessage($"分表数量: {tableNames.Count}");

            // 初始化状态
            foreach (var tableName in tableNames)
            {
                _migrationStatuses.Add(new MigrationStatus
                {
                    TableName = tableName,
                    Status = "等待中"
                });
            }

            // 更新 UI
            UpdateStatusGrid();

            // 分批读取源数据并写入分表
            var processedCount = 0;
            var offset = 0;

            while (offset < totalCount)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentBatchSize = Math.Min(batchSize, totalCount - offset);
                var dataTable = await _dbService.GetTableDataAsync(sourceTable, offset, currentBatchSize);

                // 按分表分组数据
                var groupedData = GroupDataByTable(dataTable, config);

                // 写入分表
                foreach (var group in groupedData)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var status = _migrationStatuses.FirstOrDefault(s => s.TableName == group.Key);
                    if (status != null)
                    {
                        status.Status = "迁移中";
                        UpdateStatusGrid();
                    }

                    try
                    {
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        await WriteDataToTable(group.Key, group.Value);
                        stopwatch.Stop();

                        if (status != null)
                        {
                            status.Status = "完成";
                            status.RecordCount = group.Value.Rows.Count;
                            status.Duration = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                        }

                        LogMessage($"写入 {group.Key}: {group.Value.Rows.Count} 条记录");
                    }
                    catch (Exception ex)
                    {
                        if (status != null)
                        {
                            status.Status = "失败";
                            status.ErrorMessage = ex.Message;
                        }
                        LogMessage($"写入 {group.Key} 失败: {ex.Message}");
                    }

                    UpdateStatusGrid();
                }

                processedCount += dataTable.Rows.Count;
                offset += currentBatchSize;

                // 更新进度
                var progress = (int)((double)processedCount / totalCount * 100);
                _progressBar.Value = progress;
                _progressLabel.Text = $"进度: {progress}% ({processedCount}/{totalCount})";

                await Task.Delay(10); // 让 UI 有机会更新
            }

            _progressBar.Value = 100;
            _progressLabel.Text = "迁移完成";
            LogMessage($"迁移完成: 共处理 {processedCount} 条记录");
        }

        private Dictionary<string, DataTable> GroupDataByTable(DataTable dataTable, ShardingConfig config)
        {
            var result = new Dictionary<string, DataTable>();

            foreach (DataRow row in dataTable.Rows)
            {
                string tableName;

                switch (config.ShardingType)
                {
                    case ShardingType.Time:
                        var timeField = config.TimeConfig.TimeField;
                        var timeValue = Convert.ToDateTime(row[timeField]);
                        tableName = GetTimeTableName(config.BaseTableName, timeValue, config.TimeConfig.Granularity);
                        break;

                    case ShardingType.Hash:
                        var hashField = config.HashConfig.HashField;
                        var hashValue = row[hashField]?.ToString() ?? "";
                        var hash = Math.Abs(hashValue.GetHashCode()) % config.HashConfig.ShardCount;
                        tableName = $"{config.BaseTableName}_{hash:D4}";
                        break;

                    case ShardingType.List:
                        var listField = config.ListConfig.ListField;
                        var listValue = row[listField]?.ToString() ?? "";
                        if (config.ListConfig.ValueMapping.ContainsKey(listValue))
                        {
                            tableName = $"{config.BaseTableName}_{config.ListConfig.ValueMapping[listValue]}";
                        }
                        else
                        {
                            tableName = $"{config.BaseTableName}_default";
                        }
                        break;

                    default:
                        tableName = config.BaseTableName;
                        break;
                }

                if (!result.ContainsKey(tableName))
                {
                    result[tableName] = dataTable.Clone();
                }

                result[tableName].ImportRow(row);
            }

            return result;
        }

        private string GetTimeTableName(string baseName, DateTime time, TimeGranularity granularity)
        {
            switch (granularity)
            {
                case TimeGranularity.Day:
                    return $"{baseName}_{time:yyyyMMdd}";
                case TimeGranularity.Week:
                    var weekStart = time.AddDays(-(int)time.DayOfWeek);
                    return $"{baseName}_{weekStart:yyyyMMdd}";
                case TimeGranularity.Month:
                    return $"{baseName}_{time:yyyyMM}";
                case TimeGranularity.Quarter:
                    var quarter = (time.Month - 1) / 3 + 1;
                    return $"{baseName}_{time:yyyy}Q{quarter}";
                case TimeGranularity.Year:
                    return $"{baseName}_{time:yyyy}";
                default:
                    return $"{baseName}_{time:yyyyMM}";
            }
        }

        private async Task WriteDataToTable(string tableName, DataTable data)
        {
            // 这里需要实现实际的数据写入逻辑
            // 可以使用 SqlBulkCopy 或者逐行插入
            await Task.Run(() =>
            {
                // 模拟写入
                System.Threading.Thread.Sleep(100);
            });
        }

        private void UpdateStatusGrid()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateStatusGrid));
                return;
            }

            _statusGrid.Rows.Clear();
            foreach (var status in _migrationStatuses)
            {
                _statusGrid.Rows.Add(
                    status.TableName,
                    status.Status,
                    status.RecordCount,
                    status.Duration,
                    status.ErrorMessage
                );
            }
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
                return;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logText.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            _logService.Info(message);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 迁移状态
    /// </summary>
    public class MigrationStatus
    {
        public string TableName { get; set; }
        public string Status { get; set; }
        public int RecordCount { get; set; }
        public string Duration { get; set; }
        public string ErrorMessage { get; set; }
    }
}
