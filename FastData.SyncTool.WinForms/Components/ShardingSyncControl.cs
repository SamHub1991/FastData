using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FastData.Sharding;
using FastData.Sharding.Strategies;

namespace FastData.SyncTool.WinForms.Components
{
    /// <summary>
    /// 分表数据同步控件
    /// 支持从源表同步数据到分表
    /// </summary>
    public class ShardingSyncControl : UserControl
    {
        private readonly TextBox sourceConnectionStringBox = new TextBox();
        private readonly ComboBox sourceProviderBox = new ComboBox();
        private readonly TextBox sourceTableBox = new TextBox();
        private readonly Button loadSourceTablesButton = new Button();

        private readonly ComboBox shardingTypeBox = new ComboBox();
        private readonly ComboBox granularityBox = new ComboBox();
        private readonly TextBox hashFieldBox = new TextBox();
        private readonly NumericUpDown shardCountBox = new NumericUpDown();
        private readonly TextBox listFieldBox = new TextBox();
        private readonly TextBox timeFieldBox = new TextBox();

        private readonly Button configureButton = new Button();
        private readonly Button syncButton = new Button();
        private readonly Button statsButton = new Button();
        private readonly Button createShardingTablesButton = new Button();

        private readonly DataGridView statsGrid = new DataGridView();
        private readonly TextBox logBox = new TextBox();
        private readonly Label statusLabel = new Label();

        private ShardingConfig currentConfig;
        private string currentConnectionString;

        public ShardingSyncControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(900, 700);

            // 源数据库配置
            var sourceGroup = new GroupBox
            {
                Text = "源数据库配置",
                Location = new Point(10, 10),
                Size = new Size(430, 150)
            };

            var sourceProviderLabel = new Label { Text = "数据库类型:", Location = new Point(10, 25), AutoSize = true };
            sourceProviderBox.DropDownStyle = ComboBoxStyle.DropDownList;
            sourceProviderBox.Items.AddRange(new object[] { "SqlServer", "MySql", "SQLite" });
            sourceProviderBox.SelectedIndex = 0;
            sourceProviderBox.Location = new Point(120, 22);
            sourceProviderBox.Width = 150;

            var sourceConnectionLabel = new Label { Text = "连接字符串:", Location = new Point(10, 55), AutoSize = true };
            sourceConnectionStringBox.Location = new Point(120, 52);
            sourceConnectionStringBox.Width = 290;
            sourceConnectionStringBox.Text = "server=.;database=FastDataDemo;uid=sa;pwd=YourPassword123";

            var sourceTableLabel = new Label { Text = "源表名:", Location = new Point(10, 85), AutoSize = true };
            sourceTableBox.Location = new Point(120, 82);
            sourceTableBox.Width = 150;
            sourceTableBox.Text = "UserLog";

            loadSourceTablesButton.Text = "加载表";
            loadSourceTablesButton.Location = new Point(280, 80);
            loadSourceTablesButton.Width = 80;
            loadSourceTablesButton.Click += LoadSourceTablesButton_Click;

            sourceGroup.Controls.AddRange(new Control[]
            {
                sourceProviderLabel, sourceProviderBox,
                sourceConnectionLabel, sourceConnectionStringBox,
                sourceTableLabel, sourceTableBox,
                loadSourceTablesButton
            });

            // 分表配置
            var shardingGroup = new GroupBox
            {
                Text = "分表配置",
                Location = new Point(450, 10),
                Size = new Size(430, 150)
            };

            var shardingTypeLabel = new Label { Text = "分表类型:", Location = new Point(10, 25), AutoSize = true };
            shardingTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            shardingTypeBox.Items.AddRange(new object[] { "Time", "Hash", "List" });
            shardingTypeBox.SelectedIndex = 0;
            shardingTypeBox.Location = new Point(120, 22);
            shardingTypeBox.Width = 150;
            shardingTypeBox.SelectedIndexChanged += ShardingTypeBox_SelectedIndexChanged;

            var timeFieldLabel = new Label { Text = "时间字段:", Location = new Point(10, 55), AutoSize = true };
            timeFieldBox.Location = new Point(120, 52);
            timeFieldBox.Width = 150;
            timeFieldBox.Text = "CreateTime";

            var granularityLabel = new Label { Text = "时间粒度:", Location = new Point(10, 85), AutoSize = true };
            granularityBox.DropDownStyle = ComboBoxStyle.DropDownList;
            granularityBox.Items.AddRange(new object[] { "Day", "Week", "Month", "Quarter", "Year" });
            granularityBox.SelectedIndex = 2; // Month
            granularityBox.Location = new Point(120, 82);
            granularityBox.Width = 150;

            var hashFieldLabel = new Label { Text = "哈希字段:", Location = new Point(10, 55), AutoSize = true };
            hashFieldBox.Location = new Point(120, 52);
            hashFieldBox.Width = 150;
            hashFieldBox.Text = "UserId";
            hashFieldBox.Visible = false;

            var shardCountLabel = new Label { Text = "分表数量:", Location = new Point(10, 85), AutoSize = true };
            shardCountBox.Minimum = 1;
            shardCountBox.Maximum = 100;
            shardCountBox.Value = 4;
            shardCountBox.Location = new Point(120, 82);
            shardCountBox.Width = 80;
            shardCountBox.Visible = false;

            var listFieldLabel = new Label { Text = "列表字段:", Location = new Point(10, 55), AutoSize = true };
            listFieldBox.Location = new Point(120, 52);
            listFieldBox.Width = 150;
            listFieldBox.Text = "Status";
            listFieldBox.Visible = false;

            shardingGroup.Controls.AddRange(new Control[]
            {
                shardingTypeLabel, shardingTypeBox,
                timeFieldLabel, timeFieldBox,
                granularityLabel, granularityBox,
                hashFieldLabel, hashFieldBox,
                shardCountLabel, shardCountBox,
                listFieldLabel, listFieldBox
            });

            // 操作按钮
            var buttonPanel = new Panel
            {
                Location = new Point(10, 170),
                Size = new Size(870, 40)
            };

            configureButton.Text = "配置分表";
            configureButton.Location = new Point(0, 5);
            configureButton.Width = 100;
            configureButton.Click += ConfigureButton_Click;

            createShardingTablesButton.Text = "创建分表";
            createShardingTablesButton.Location = new Point(110, 5);
            createShardingTablesButton.Width = 100;
            createShardingTablesButton.Click += CreateShardingTablesButton_Click;

            syncButton.Text = "同步数据";
            syncButton.Location = new Point(220, 5);
            syncButton.Width = 100;
            syncButton.Click += SyncButton_Click;

            statsButton.Text = "统计信息";
            statsButton.Location = new Point(330, 5);
            statsButton.Width = 100;
            statsButton.Click += StatsButton_Click;

            buttonPanel.Controls.AddRange(new Control[]
            {
                configureButton, createShardingTablesButton, syncButton, statsButton
            });

            // 统计信息
            var statsGroup = new GroupBox
            {
                Text = "分表统计",
                Location = new Point(10, 220),
                Size = new Size(430, 200)
            };

            statsGrid.Dock = DockStyle.Fill;
            statsGrid.AllowUserToAddRows = false;
            statsGrid.ReadOnly = true;
            statsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            statsGroup.Controls.Add(statsGrid);

            // 日志
            var logGroup = new GroupBox
            {
                Text = "操作日志",
                Location = new Point(450, 220),
                Size = new Size(430, 200)
            };

            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logGroup.Controls.Add(logBox);

            // 状态栏
            statusLabel.Text = "就绪";
            statusLabel.Location = new Point(10, 430);
            statusLabel.AutoSize = true;

            this.Controls.AddRange(new Control[]
            {
                sourceGroup, shardingGroup,
                buttonPanel,
                statsGroup, logGroup,
                statusLabel
            });
        }

        private void ShardingTypeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var shardingType = shardingTypeBox.SelectedItem.ToString();

            timeFieldBox.Visible = shardingType == "Time";
            granularityBox.Visible = shardingType == "Time";
            hashFieldBox.Visible = shardingType == "Hash";
            shardCountBox.Visible = shardingType == "Hash";
            listFieldBox.Visible = shardingType == "List";
        }

        private void LoadSourceTablesButton_Click(object sender, EventArgs e)
        {
            try
            {
                var connectionString = sourceConnectionStringBox.Text;
                var provider = sourceProviderBox.SelectedItem.ToString();

                if (provider != "SqlServer")
                {
                    MessageBox.Show("目前仅支持 SQL Server", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var tables = new List<string>();

                    var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }

                    if (tables.Count > 0)
                    {
                        sourceTableBox.Items.Clear();
                        sourceTableBox.Items.AddRange(tables.ToArray());
                        sourceTableBox.SelectedIndex = 0;
                        Log($"已加载 {tables.Count} 个表");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"加载表失败: {ex.Message}");
                MessageBox.Show($"加载表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureButton_Click(object sender, EventArgs e)
        {
            try
            {
                var shardingType = shardingTypeBox.SelectedItem.ToString();
                var baseTableName = sourceTableBox.Text;

                switch (shardingType)
                {
                    case "Time":
                        currentConfig = new ShardingConfig
                        {
                            BaseTableName = baseTableName,
                            ShardingType = ShardingType.Time,
                            TimeConfig = new TimeShardingConfig
                            {
                                TimeField = timeFieldBox.Text,
                                Granularity = Enum.Parse<TimeGranularity>(granularityBox.SelectedItem.ToString())
                            }
                        };
                        break;

                    case "Hash":
                        currentConfig = new ShardingConfig
                        {
                            BaseTableName = baseTableName,
                            ShardingType = ShardingType.Hash,
                            HashConfig = new HashShardingConfig
                            {
                                HashField = hashFieldBox.Text,
                                ShardCount = (int)shardCountBox.Value
                            }
                        };
                        break;

                    case "List":
                        currentConfig = new ShardingConfig
                        {
                            BaseTableName = baseTableName,
                            ShardingType = ShardingType.List,
                            ListConfig = new ListShardingConfig
                            {
                                ListField = listFieldBox.Text,
                                ValueMapping = new Dictionary<string, string>
                                {
                                    { "Pending", "pending" },
                                    { "Processing", "processing" },
                                    { "Completed", "completed" },
                                    { "Cancelled", "cancelled" }
                                }
                            }
                        };
                        break;
                }

                currentConnectionString = sourceConnectionStringBox.Text;

                Log($"分表配置完成: {shardingType}");
                statusLabel.Text = "分表配置完成";

                // 显示分表信息
                var tableNames = ShardingManager.GetAllTableNames<object>();
                Log($"将创建 {tableNames.Count} 个分表");
            }
            catch (Exception ex)
            {
                Log($"配置失败: {ex.Message}");
                MessageBox.Show($"配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateShardingTablesButton_Click(object sender, EventArgs e)
        {
            if (currentConfig == null)
            {
                MessageBox.Show("请先配置分表", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var tableNames = ShardingManager.GetAllTableNames<object>();

                using (var conn = new SqlConnection(currentConnectionString))
                {
                    conn.Open();

                    foreach (var tableName in tableNames)
                    {
                        // 获取源表结构
                        var schemaSql = $@"
                            SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
                            FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_NAME = '{currentConfig.BaseTableName}'
                            ORDER BY ORDINAL_POSITION";

                        var columns = new List<string>();
                        using (var cmd = new SqlCommand(schemaSql, conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var colName = reader.GetString(0);
                                var dataType = reader.GetString(1);
                                var maxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                                var isNullable = reader.GetString(3) == "YES";

                                var columnDef = $"[{colName}] {dataType}";
                                if (maxLength.HasValue && maxLength.Value > 0)
                                {
                                    columnDef += $"({maxLength.Value})";
                                }
                                if (!isNullable)
                                {
                                    columnDef += " NOT NULL";
                                }

                                columns.Add(columnDef);
                            }
                        }

                        if (columns.Count > 0)
                        {
                            var createSql = $@"
                                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{tableName}' AND xtype='U')
                                CREATE TABLE [{tableName}] (
                                    {string.Join(",\n                                    ", columns)}
                                );";

                            using (var cmd = new SqlCommand(createSql, conn))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            Log($"创建分表: {tableName}");
                        }
                    }
                }

                Log($"分表创建完成: {tableNames.Count} 个表");
                statusLabel.Text = "分表创建完成";
            }
            catch (Exception ex)
            {
                Log($"创建分表失败: {ex.Message}");
                MessageBox.Show($"创建分表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyncButton_Click(object sender, EventArgs e)
        {
            if (currentConfig == null)
            {
                MessageBox.Show("请先配置分表", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                syncButton.Enabled = false;
                statusLabel.Text = "正在同步...";
                Application.DoEvents();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var sourceTable = sourceTableBox.Text;

                using (var conn = new SqlConnection(currentConnectionString))
                {
                    conn.Open();

                    // 查询源数据
                    var selectSql = $"SELECT * FROM [{sourceTable}]";
                    var totalRecords = 0;
                    var syncedRecords = 0;
                    var errorRecords = 0;

                    using (var cmd = new SqlCommand(selectSql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var schemaTable = reader.GetSchemaTable();
                        var columnNames = new List<string>();
                        foreach (DataRow row in schemaTable.Rows)
                        {
                            columnNames.Add(row["ColumnName"].ToString());
                        }

                        while (reader.Read())
                        {
                            totalRecords++;

                            try
                            {
                                // 构建实体对象用于分表计算
                                var entity = new Dictionary<string, object>();
                                foreach (var colName in columnNames)
                                {
                                    entity[colName] = reader[colName];
                                }

                                // 根据分表类型确定目标表
                                string targetTable;
                                switch (currentConfig.ShardingType)
                                {
                                    case ShardingType.Time:
                                        var timeField = currentConfig.TimeConfig.TimeField;
                                        var timeValue = Convert.ToDateTime(entity[timeField]);
                                        targetTable = GetTimeTableName(currentConfig.BaseTableName, timeValue, currentConfig.TimeConfig.Granularity);
                                        break;

                                    case ShardingType.Hash:
                                        var hashField = currentConfig.HashConfig.HashField;
                                        var hashValue = entity[hashField]?.ToString();
                                        var hash = Math.Abs(hashValue.GetHashCode()) % currentConfig.HashConfig.ShardCount;
                                        targetTable = $"{currentConfig.BaseTableName}_{hash}";
                                        break;

                                    case ShardingType.List:
                                        var listField = currentConfig.ListConfig.ListField;
                                        var listValue = entity[listField]?.ToString();
                                        if (currentConfig.ListConfig.ValueMapping.TryGetValue(listValue, out var suffix))
                                        {
                                            targetTable = $"{currentConfig.BaseTableName}_{suffix}";
                                        }
                                        else
                                        {
                                            targetTable = $"{currentConfig.BaseTableName}_other";
                                        }
                                        break;

                                    default:
                                        targetTable = currentConfig.BaseTableName;
                                        break;
                                }

                                // 插入到分表
                                InsertRecordToShardingTable(conn, targetTable, entity, columnNames);
                                syncedRecords++;
                            }
                            catch (Exception ex)
                            {
                                errorRecords++;
                                if (errorRecords <= 10) // 只记录前10个错误
                                {
                                    Log($"同步记录失败: {ex.Message}");
                                }
                            }

                            if (totalRecords % 1000 == 0)
                            {
                                statusLabel.Text = $"正在同步... {totalRecords} 条";
                                Application.DoEvents();
                            }
                        }
                    }

                    stopwatch.Stop();

                    Log($"同步完成: 总计 {totalRecords} 条, 成功 {syncedRecords} 条, 失败 {errorRecords} 条");
                    Log($"耗时: {stopwatch.Elapsed.TotalSeconds:F2} 秒");

                    statusLabel.Text = $"同步完成: {syncedRecords}/{totalRecords}";
                }
            }
            catch (Exception ex)
            {
                Log($"同步失败: {ex.Message}");
                MessageBox.Show($"同步失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "同步失败";
            }
            finally
            {
                syncButton.Enabled = true;
            }
        }

        private void StatsButton_Click(object sender, EventArgs e)
        {
            if (currentConfig == null)
            {
                MessageBox.Show("请先配置分表", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var tableNames = ShardingManager.GetAllTableNames<object>();
                var stats = new DataTable();
                stats.Columns.Add("分表名称", typeof(string));
                stats.Columns.Add("记录数", typeof(int));

                using (var conn = new SqlConnection(currentConnectionString))
                {
                    conn.Open();

                    foreach (var tableName in tableNames)
                    {
                        try
                        {
                            var sql = $"SELECT COUNT(*) FROM [{tableName}]";
                            using (var cmd = new SqlCommand(sql, conn))
                            {
                                var count = (int)cmd.ExecuteScalar();
                                stats.Rows.Add(tableName, count);
                            }
                        }
                        catch
                        {
                            stats.Rows.Add(tableName, -1);
                        }
                    }
                }

                statsGrid.DataSource = stats;

                var totalRecords = stats.AsEnumerable().Sum(r => r.Field<int>("记录数"));
                Log($"统计完成: 共 {tableNames.Count} 个分表, {totalRecords} 条记录");
                statusLabel.Text = $"统计完成: {tableNames.Count} 个分表";
            }
            catch (Exception ex)
            {
                Log($"统计失败: {ex.Message}");
                MessageBox.Show($"统计失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetTimeTableName(string baseTableName, DateTime time, TimeGranularity granularity)
        {
            switch (granularity)
            {
                case TimeGranularity.Day:
                    return $"{baseTableName}_{time:yyyyMMdd}";
                case TimeGranularity.Week:
                    var weekStart = time.AddDays(-(int)time.DayOfWeek);
                    return $"{baseTableName}_{weekStart:yyyyMMdd}";
                case TimeGranularity.Month:
                    return $"{baseTableName}_{time:yyyyMM}";
                case TimeGranularity.Quarter:
                    var quarter = (time.Month - 1) / 3 + 1;
                    return $"{baseTableName}_{time.Year}Q{quarter}";
                case TimeGranularity.Year:
                    return $"{baseTableName}_{time.Year}";
                default:
                    return $"{baseTableName}_{time:yyyyMM}";
            }
        }

        private void InsertRecordToShardingTable(SqlConnection conn, string tableName, Dictionary<string, object> entity, List<string> columnNames)
        {
            var columns = string.Join(", ", columnNames.Select(c => $"[{c}]"));
            var parameters = string.Join(", ", columnNames.Select(c => $"@{c}"));
            var sql = $"INSERT INTO [{tableName}] ({columns}) VALUES ({parameters})";

            using (var cmd = new SqlCommand(sql, conn))
            {
                foreach (var colName in columnNames)
                {
                    var value = entity[colName] ?? DBNull.Value;
                    cmd.Parameters.AddWithValue($"@{colName}", value);
                }
                cmd.ExecuteNonQuery();
            }
        }

        private void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logBox.AppendText($"[{timestamp}] {message}\r\n");
        }
    }
}
