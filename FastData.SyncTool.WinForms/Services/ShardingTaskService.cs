using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastData.Sharding;
using FastData.SyncTool.WinForms.Models;

namespace FastData.SyncTool.WinForms.Services
{
    /// <summary>
    /// 分表任务服务
    /// 管理后台分表任务的执行、暂停、取消
    /// </summary>
    public class ShardingTaskService
    {
        private readonly ConcurrentDictionary<string, ShardingTaskInfo> _tasks = new ConcurrentDictionary<string, ShardingTaskInfo>();
        private readonly LogService _logService;

        /// <summary>
        /// 任务状态变更事件
        /// </summary>
        public event Action<ShardingTaskInfo> TaskStatusChanged;

        /// <summary>
        /// 任务进度变更事件
        /// </summary>
        public event Action<ShardingTaskInfo> TaskProgressChanged;

        public ShardingTaskService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        public List<ShardingTaskInfo> GetAllTasks()
        {
            return _tasks.Values.OrderByDescending(t => t.CreatedTime).ToList();
        }

        /// <summary>
        /// 获取任务
        /// </summary>
        public ShardingTaskInfo GetTask(string taskId)
        {
            _tasks.TryGetValue(taskId, out var task);
            return task;
        }

        /// <summary>
        /// 启动分表任务
        /// </summary>
        public ShardingTaskInfo StartTask(
            string taskName,
            string sourceTable,
            string connectionString,
            ShardingConfig shardingConfig)
        {
            var task = new ShardingTaskInfo
            {
                TaskName = taskName,
                SourceTable = sourceTable,
                ShardingType = shardingConfig.ShardingType.ToString(),
                ConfigDescription = GetConfigDescription(shardingConfig),
                ConnectionString = connectionString
            };

            task.CancellationTokenSource = new CancellationTokenSource();
            _tasks[task.TaskId] = task;

            // 后台启动任务
            Task.Run(() => ExecuteShardingTaskAsync(task, shardingConfig));

            _logService.Info($"分表任务已启动: {taskName} ({task.TaskId})");
            return task;
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        public void PauseTask(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                if (task.Status == ShardingTaskStatus.Running)
                {
                    task.PauseEvent.Reset();
                    task.Status = ShardingTaskStatus.Paused;
                    TaskStatusChanged?.Invoke(task);
                    _logService.Info($"分表任务已暂停: {task.TaskName}");
                }
            }
        }

        /// <summary>
        /// 恢复任务
        /// </summary>
        public void ResumeTask(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                if (task.Status == ShardingTaskStatus.Paused)
                {
                    task.PauseEvent.Set();
                    task.Status = ShardingTaskStatus.Running;
                    TaskStatusChanged?.Invoke(task);
                    _logService.Info($"分表任务已恢复: {task.TaskName}");
                }
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public void CancelTask(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                if (task.Status == ShardingTaskStatus.Running || task.Status == ShardingTaskStatus.Paused)
                {
                    task.CancellationTokenSource?.Cancel();
                    task.PauseEvent.Set(); // 解除暂停状态
                    task.Status = ShardingTaskStatus.Cancelled;
                    TaskStatusChanged?.Invoke(task);
                    _logService.Info($"分表任务已取消: {task.TaskName}");
                }
            }
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        public bool DeleteTask(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                if (task.Status == ShardingTaskStatus.Running)
                {
                    return false; // 运行中的任务不能删除
                }

                _tasks.TryRemove(taskId, out _);
                task.CancellationTokenSource?.Dispose();
                task.PauseEvent?.Dispose();
                _logService.Info($"分表任务已删除: {task.TaskName}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 执行分表任务
        /// </summary>
        private async Task ExecuteShardingTaskAsync(ShardingTaskInfo task, ShardingConfig shardingConfig)
        {
            try
            {
                task.Status = ShardingTaskStatus.Running;
                task.StartTime = DateTime.Now;
                TaskStatusChanged?.Invoke(task);

                var cancellationToken = task.CancellationTokenSource.Token;

                using (var conn = new SqlConnection(task.ConnectionString))
                {
                    await conn.OpenAsync(cancellationToken);

                    // 1. 统计源表总记录数
                    var countSql = $"SELECT COUNT(*) FROM [{task.SourceTable}]";
                    using (var cmd = new SqlCommand(countSql, conn))
                    {
                        task.TotalRecords = (int)await cmd.ExecuteScalarAsync(cancellationToken);
                    }
                    TaskProgressChanged?.Invoke(task);

                    // 2. 创建分表
                    var tableNames = ShardingManager.GetAllTableNames<object>();
                    foreach (var tableName in tableNames)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await CreateShardingTableAsync(conn, task.SourceTable, tableName, cancellationToken);
                    }

                    // 3. 分批读取源数据并插入分表
                    var batchSize = 1000;
                    var offset = 0;

                    while (offset < task.TotalRecords)
                    {
                        // 检查暂停
                        task.PauseEvent.Wait(cancellationToken);

                        // 检查取消
                        cancellationToken.ThrowIfCancellationRequested();

                        // 分批查询
                        var selectSql = $@"
                            SELECT * FROM [{task.SourceTable}]
                            ORDER BY (SELECT NULL)
                            OFFSET {offset} ROWS
                            FETCH NEXT {batchSize} ROWS ONLY";

                        using (var cmd = new SqlCommand(selectSql, conn))
                        using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                        {
                            var schemaTable = reader.GetSchemaTable();
                            var columnNames = new List<string>();
                            foreach (DataRow row in schemaTable.Rows)
                            {
                                columnNames.Add(row["ColumnName"].ToString());
                            }

                            while (await reader.ReadAsync(cancellationToken))
                            {
                                // 检查暂停
                                task.PauseEvent.Wait(cancellationToken);

                                // 检查取消
                                cancellationToken.ThrowIfCancellationRequested();

                                try
                                {
                                    // 构建实体数据
                                    var entity = new Dictionary<string, object>();
                                    foreach (var colName in columnNames)
                                    {
                                        entity[colName] = reader[colName];
                                    }

                                    // 确定目标分表
                                    var targetTable = GetTargetTableName(shardingConfig, entity);

                                    // 插入分表
                                    await InsertToShardingTableAsync(conn, targetTable, entity, columnNames, cancellationToken);

                                    task.SuccessRecords++;
                                }
                                catch (Exception ex)
                                {
                                    task.FailedRecords++;
                                    if (task.FailedRecords <= 100) // 只记录前100个错误
                                    {
                                        _logService.Warn($"分表插入失败: {ex.Message}");
                                    }
                                }

                                task.ProcessedRecords++;

                                // 每100条更新一次进度
                                if (task.ProcessedRecords % 100 == 0)
                                {
                                    TaskProgressChanged?.Invoke(task);
                                }
                            }
                        }

                        offset += batchSize;
                    }
                }

                task.Status = ShardingTaskStatus.Completed;
                task.CompleteTime = DateTime.Now;
                TaskStatusChanged?.Invoke(task);
                TaskProgressChanged?.Invoke(task);

                _logService.Info($"分表任务完成: {task.TaskName}, 成功: {task.SuccessRecords}, 失败: {task.FailedRecords}");
            }
            catch (OperationCanceledException)
            {
                task.Status = ShardingTaskStatus.Cancelled;
                task.CompleteTime = DateTime.Now;
                TaskStatusChanged?.Invoke(task);
            }
            catch (Exception ex)
            {
                task.Status = ShardingTaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.CompleteTime = DateTime.Now;
                TaskStatusChanged?.Invoke(task);
                _logService.Error($"分表任务失败: {task.TaskName}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建分表
        /// </summary>
        private async Task CreateShardingTableAsync(SqlConnection conn, string sourceTable, string targetTable, CancellationToken cancellationToken)
        {
            var schemaSql = $@"
                SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = '{sourceTable}'
                ORDER BY ORDINAL_POSITION";

            var columns = new List<string>();
            using (var cmd = new SqlCommand(schemaSql, conn))
            using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
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
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{targetTable}' AND xtype='U')
                    CREATE TABLE [{targetTable}] (
                        {string.Join(",\n                        ", columns)}
                    );";

                using (var cmd = new SqlCommand(createSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        /// <summary>
        /// 插入数据到分表
        /// </summary>
        private async Task InsertToShardingTableAsync(
            SqlConnection conn,
            string tableName,
            Dictionary<string, object> entity,
            List<string> columnNames,
            CancellationToken cancellationToken)
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
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 根据分表配置获取目标表名
        /// </summary>
        private string GetTargetTableName(ShardingConfig config, Dictionary<string, object> entity)
        {
            switch (config.ShardingType)
            {
                case ShardingType.Time:
                    var timeField = config.TimeConfig.TimeField;
                    var timeValue = Convert.ToDateTime(entity[timeField]);
                    return GetTimeTableName(config.BaseTableName, timeValue, config.TimeConfig.Granularity);

                case ShardingType.Hash:
                    var hashField = config.HashConfig.HashField;
                    var hashValue = entity[hashField]?.ToString() ?? "";
                    var hash = Math.Abs(hashValue.GetHashCode()) % config.HashConfig.ShardCount;
                    return $"{config.BaseTableName}_{hash}";

                case ShardingType.List:
                    var listField = config.ListConfig.ListField;
                    var listValue = entity[listField]?.ToString() ?? "";
                    if (config.ListConfig.ValueMapping.TryGetValue(listValue, out var suffix))
                    {
                        return $"{config.BaseTableName}_{suffix}";
                    }
                    return $"{config.BaseTableName}_other";

                case ShardingType.Composite:
                    var compositeKey = string.Join("_", config.CompositeConfig.CompositeFields.Select(f => entity[f]?.ToString() ?? ""));
                    var compositeHash = Math.Abs(compositeKey.GetHashCode()) % config.CompositeConfig.ShardCount;
                    return $"{config.BaseTableName}_{compositeHash}";

                default:
                    return config.BaseTableName;
            }
        }

        /// <summary>
        /// 获取时间分表名
        /// </summary>
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

        /// <summary>
        /// 获取配置描述
        /// </summary>
        private string GetConfigDescription(ShardingConfig config)
        {
            switch (config.ShardingType)
            {
                case ShardingType.Time:
                    return $"时间分表: {config.TimeConfig.TimeField} ({config.TimeConfig.Granularity})";
                case ShardingType.Hash:
                    return $"哈希分表: {config.HashConfig.HashField} ({config.HashConfig.ShardCount}个分表)";
                case ShardingType.List:
                    return $"列表分表: {config.ListConfig.ListField}";
                case ShardingType.Composite:
                    return $"组合分表: {string.Join(",", config.CompositeConfig.CompositeFields)}";
                case ShardingType.QueryFrequency:
                    return $"频率分表: {config.FrequencyConfig.Field}";
                default:
                    return config.ShardingType.ToString();
            }
        }
    }
}
