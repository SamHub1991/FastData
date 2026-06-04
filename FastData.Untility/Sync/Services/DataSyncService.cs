using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Xml;

namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 数据同步日志级别
    /// </summary>
    public enum SyncLogLevel
    {
        Info,
        Warn,
        Error,
        Debug
    }

    /// <summary>
    /// 数据同步日志回调
    /// </summary>
    public class DataSyncService
    {
        /// <summary>
        /// 日志回调函数，外部可注入自定义日志处理逻辑
        /// </summary>
        public Action<SyncLogLevel, string> LogCallback { get; set; }

        /// <summary>
        /// 写入日志
        /// </summary>
        private void Log(SyncLogLevel level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var fullMessage = string.Format("[{0}] [{1}] {2}", timestamp, level.ToString().ToUpperInvariant(), message);
            
            // 调用外部回调
            LogCallback?.Invoke(level, fullMessage);
            
            // 同时输出到 Debug 输出窗口
            System.Diagnostics.Debug.WriteLine(fullMessage);
        }

        /// <summary>
        /// 同步单个表的数据
        /// </summary>
        /// <param name="options">同步选项</param>
        /// <returns>同步结果</returns>
        public DataSyncResult SyncTable(DataSyncOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            Log(SyncLogLevel.Info, string.Format("========== 开始同步任务 =========="));
            Log(SyncLogLevel.Info, string.Format("任务ID: {0}", string.IsNullOrWhiteSpace(options.TaskId) ? options.SourceTable + "_to_" + options.TargetTable : options.TaskId));
            Log(SyncLogLevel.Info, string.Format("源表: {0} -> 目标表: {1}", options.SourceTable, options.TargetTable));
            Log(SyncLogLevel.Info, string.Format("同步模式: {0}", options.EnableTimeRange ? "时间范围增量" : (options.IsAutoIncrementKey ? "主键增量" : "全量同步")));
            Log(SyncLogLevel.Info, string.Format("去重模式: {0}", options.AlwaysDeduplicate ? "是（逐行插入/更新）" : "否（批量插入）"));
            Log(SyncLogLevel.Info, string.Format("批次大小: {0}", options.BatchSize <= 0 ? 500 : options.BatchSize));
            Log(SyncLogLevel.Info, string.Format("最大重试次数: {0}", options.RetryCount < 0 ? 0 : options.RetryCount));

            var syncStartTime = DateTime.Now;
            ValidateOptions(options);
            var batchSize = options.BatchSize <= 0 ? 500 : options.BatchSize;
            var maxRetryCount = options.RetryCount < 0 ? 0 : options.RetryCount;
            var taskId = string.IsNullOrWhiteSpace(options.TaskId) ? options.SourceTable + "_to_" + options.TargetTable : options.TaskId;

            // 步骤 1: 创建中间库表结构
            if (options.AutoCreateIntermediateSchema)
            {
                Log(SyncLogLevel.Info, "[步骤 1/7] 正在创建中间库表结构...");
                CreateIntermediateSchema(options);
                Log(SyncLogLevel.Info, "[步骤 1/7] 中间库表结构创建完成");
            }
            else
            {
                Log(SyncLogLevel.Debug, "[步骤 1/7] 跳过中间库表结构创建");
            }

            // 步骤 2: 应用全局配置
            Log(SyncLogLevel.Info, "[步骤 2/7] 应用同步配置...");
            ApplyGlobalConfig(options);
            Log(SyncLogLevel.Info, string.Format("[步骤 2/7] 配置应用完成 - 时间范围: {0}天, 去重: {1}", options.RangeDays, options.AlwaysDeduplicate ? "是" : "否"));

            // 步骤 3: 从源库读取数据
            var table = new DataTable();
            var sourceSql = string.Empty;
            Log(SyncLogLevel.Info, "[步骤 3/7] 正在连接源数据库并读取数据...");
            using (var source = CreateConnection(options.SourceProvider, options.SourceConnectionString))
            using (var command = source.CreateCommand())
            {
                Log(SyncLogLevel.Debug, string.Format("源库Provider: {0}", options.SourceProvider));
                Log(SyncLogLevel.Debug, string.Format("源库连接状态: 准备连接"));
                
                source.Open();
                Log(SyncLogLevel.Debug, "源库连接成功");

                sourceSql = BuildSourceSql(options, command);
                command.CommandText = sourceSql;
                Log(SyncLogLevel.Info, string.Format("执行查询SQL: {0}", sourceSql));
                if (command.Parameters.Count > 0)
                {
                    foreach (DbParameter param in command.Parameters)
                    {
                        Log(SyncLogLevel.Debug, string.Format("SQL参数: {0} = {1}", param.ParameterName, param.Value));
                    }
                }

                using (var reader = command.ExecuteReader())
                {
                    table.Load(reader);
                }
                Log(SyncLogLevel.Info, string.Format("[步骤 3/7] 数据读取完成，共 {0} 行", table.Rows.Count));
            }

            // 步骤 4: 连接目标库并执行同步
            var writeCount = 0;
            var failedCount = 0;
            var retryCount = 0;
            var recoveredCount = 0;
            
            Log(SyncLogLevel.Info, "[步骤 4/7] 正在连接目标数据库...");
            using (var target = CreateConnection(options.TargetProvider, options.TargetConnectionString))
            {
                Log(SyncLogLevel.Debug, string.Format("目标库Provider: {0}", options.TargetProvider));
                target.Open();
                Log(SyncLogLevel.Info, "[步骤 4/7] 目标库连接成功");

                // 步骤 5: 恢复失败记录
                if (options.ResumeFailedRecords)
                {
                    Log(SyncLogLevel.Info, "[步骤 5/7] 正在恢复历史失败记录...");
                    recoveredCount = ResumeFailedRecords(options, target, maxRetryCount);
                    Log(SyncLogLevel.Info, string.Format("[步骤 5/7] 失败记录恢复完成，成功恢复 {0} 条", recoveredCount));
                }
                else
                {
                    Log(SyncLogLevel.Debug, "[步骤 5/7] 跳过失败记录恢复");
                }

                // 步骤 6: 同步数据行
                Log(SyncLogLevel.Info, "[步骤 6/7] 开始同步数据行...");
                var batchRows = new List<DataRow>();
                var processedRows = 0;
                var batchIndex = 0;

                foreach (DataRow row in table.Rows)
                {
                    processedRows++;
                    int rowRetryCount = 0;
                    
                    // 去重模式：逐行插入
                    if (options.AlwaysDeduplicate && !string.IsNullOrEmpty(options.PrimaryKeyColumns))
                    {
                        if (TryInsertRowWithDedup(target, options.TargetTable, table, row, maxRetryCount, out rowRetryCount, options.PrimaryKeyColumns))
                            writeCount++;
                        else
                        {
                            failedCount++;
                            Log(SyncLogLevel.Warn, string.Format("行插入失败 (行 {0}/{1})，已保存到失败记录", processedRows, table.Rows.Count));
                            SaveFailedRecord(options, taskId, table, row);
                        }
                    }
                    else
                    {
                        // 批量插入模式
                        batchRows.Add(row);
                        if (batchRows.Count >= batchSize)
                        {
                            batchIndex++;
                            Log(SyncLogLevel.Debug, string.Format("执行批量插入 批次 #{0}，共 {1} 行", batchIndex, batchRows.Count));
                            try
                            {
                                InsertRowBatch(target, options.TargetTable, table, batchRows);
                                writeCount += batchRows.Count;
                                Log(SyncLogLevel.Debug, string.Format("批量插入 批次 #{0} 成功", batchIndex));
                            }
                            catch (Exception ex)
                            {
                                Log(SyncLogLevel.Warn, string.Format("批量插入 批次 #{0} 失败，降级为逐行插入: {1}", batchIndex, ex.Message));
                                // 批量失败则逐行重试
                                var rowSuccessCount = 0;
                                var rowFailedCount = 0;
                                foreach (var batchRow in batchRows)
                                {
                                    try
                                    {
                                        InsertRow(target, options.TargetTable, table, batchRow);
                                        writeCount++;
                                        rowSuccessCount++;
                                    }
                                    catch (Exception rowEx)
                                    {
                                        Log(SyncLogLevel.Error, string.Format("逐行插入失败: {0}", rowEx.Message));
                                        failedCount++;
                                        rowFailedCount++;
                                        SaveFailedRecord(options, taskId, table, batchRow);
                                    }
                                }
                                Log(SyncLogLevel.Info, string.Format("逐行降级完成: 成功 {0} 行，失败 {1} 行", rowSuccessCount, rowFailedCount));
                            }
                            batchRows.Clear();
                        }
                    }

                    retryCount += rowRetryCount;

                    // 每处理 1000 行打印一次进度
                    if (processedRows % 1000 == 0)
                    {
                        Log(SyncLogLevel.Info, string.Format("同步进度: 已处理 {0}/{1} 行，已写入 {2} 行，失败 {3} 行", 
                            processedRows, table.Rows.Count, writeCount, failedCount));
                    }
                }

                // 处理剩余批次
                Log(SyncLogLevel.Info, string.Format("[步骤 6/7] 数据处理完成，正在处理剩余 {0} 行...", batchRows.Count));
                if (batchRows.Count > 0 && !options.AlwaysDeduplicate)
                {
                    batchIndex++;
                    Log(SyncLogLevel.Debug, string.Format("执行最终批量插入 批次 #{0}，共 {1} 行", batchIndex, batchRows.Count));
                    try
                    {
                        InsertRowBatch(target, options.TargetTable, table, batchRows);
                        writeCount += batchRows.Count;
                        Log(SyncLogLevel.Debug, string.Format("最终批量插入 批次 #{0} 成功", batchIndex));
                    }
                    catch (Exception ex)
                    {
                        Log(SyncLogLevel.Warn, string.Format("最终批量插入 批次 #{0} 失败，降级为逐行插入: {1}", batchIndex, ex.Message));
                        // 批量失败则逐行重试
                        var rowSuccessCount = 0;
                        var rowFailedCount = 0;
                        foreach (var batchRow in batchRows)
                        {
                            try
                            {
                                InsertRow(target, options.TargetTable, table, batchRow);
                                writeCount++;
                                rowSuccessCount++;
                            }
                            catch (Exception rowEx)
                            {
                                Log(SyncLogLevel.Error, string.Format("最终逐行插入失败: {0}", rowEx.Message));
                                failedCount++;
                                rowFailedCount++;
                                SaveFailedRecord(options, taskId, table, batchRow);
                            }
                        }
                        Log(SyncLogLevel.Info, string.Format("最终逐行降级完成: 成功 {0} 行，失败 {1} 行", rowSuccessCount, rowFailedCount));
                    }
                }

                // 步骤 7: 清理中间库数据
                if (options.CleanIntermediateData)
                {
                    Log(SyncLogLevel.Info, "[步骤 7/7] 正在清理中间库成功记录...");
                    CleanIntermediateData(options);
                    Log(SyncLogLevel.Info, "[步骤 7/7] 中间库清理完成");
                }
                else
                {
                    Log(SyncLogLevel.Debug, "[步骤 7/7] 跳过中间库清理");
                }
            }

            // 计算同步结果
            var syncDuration = DateTime.Now - syncStartTime;
            var result = new DataSyncResult
            {
                ReadCount = table.Rows.Count,
                WriteCount = writeCount,
                FailedCount = failedCount,
                RetryCount = retryCount,
                RecoveredCount = recoveredCount,
                Message = string.Format("同步完成 [读取 {0} 行，写入 {1} 行，失败 {2} 行，重试 {3} 次，恢复 {4} 条] [耗时 {5:F1}秒] [去重模式：{6}]",
                    table.Rows.Count, writeCount, failedCount, retryCount, recoveredCount, syncDuration.TotalSeconds, options.AlwaysDeduplicate ? "是" : "否"),
                MaxPkValue = GetMaxPrimaryKeyValueFromDb(options.SourceProvider, options.SourceConnectionString, options.SourceTable, options.PrimaryKeyColumns)
                    ?? GetMaxPrimaryKeyValue(table, options.PrimaryKeyColumns)
            };

            // 更新同步时间
            var syncTime = DateTime.Now;
            if (options.ConfigManager != null && !string.IsNullOrEmpty(options.TaskId))
            {
                if (options.EnableTimeRange && result.ReadCount > 0)
                {
                    options.ConfigManager.UpdateLastSyncTime(options.TaskId, syncTime);
                    Log(SyncLogLevel.Debug, string.Format("已更新任务同步时间 (时间范围模式): {0}", syncTime.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                else if (!options.EnableTimeRange && result.MaxPkValue.HasValue)
                {
                    var dt = DateTime.MinValue.AddTicks(result.MaxPkValue.Value);
                    options.ConfigManager.UpdateLastSyncTime(options.TaskId, dt);
                    Log(SyncLogLevel.Debug, string.Format("已更新任务同步时间 (主键增量模式): {0} (Ticks: {1})", dt.ToString("yyyy-MM-dd HH:mm:ss"), result.MaxPkValue.Value));
                }
            }

            Log(SyncLogLevel.Info, string.Format("========== 同步任务完成 =========="));
            Log(SyncLogLevel.Info, string.Format("最终统计: 读取 {0} 行，成功写入 {1} 行，失败 {2} 行", result.ReadCount, result.WriteCount, result.FailedCount));
            Log(SyncLogLevel.Info, string.Format("重试统计: 重试 {0} 次，恢复 {1} 条失败记录", result.RetryCount, result.RecoveredCount));
            Log(SyncLogLevel.Info, string.Format("耗时统计: {0:F1} 秒", syncDuration.TotalSeconds));
            Log(SyncLogLevel.Info, string.Format("源SQL: {0}", sourceSql));

            return result;
        }

        /// <summary>
        /// 应用全局配置
        /// </summary>
        private static void ApplyGlobalConfig(DataSyncOptions options)
        {
            if (options.EnableGlobalConfig)
            {
                // 使用全局配置的 RangeDays
                if (options.GlobalRangeDays > 0)
                    options.RangeDays = options.GlobalRangeDays;

                // 使用全局配置的去重模式
                // AlwaysDeduplicate 已经设置
            }
        }

        private static void ValidateOptions(DataSyncOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SourceProvider))
                throw new ArgumentException("源库 Provider 不能为空");
            if (string.IsNullOrWhiteSpace(options.SourceConnectionString))
                throw new ArgumentException("源库连接字符串不能为空");
            if (string.IsNullOrWhiteSpace(options.TargetProvider))
                throw new ArgumentException("目标库 Provider 不能为空");
            if (string.IsNullOrWhiteSpace(options.TargetConnectionString))
                throw new ArgumentException("目标库连接字符串不能为空");
            if (string.IsNullOrWhiteSpace(options.SourceTable))
                throw new ArgumentException("源表不能为空");
            if (string.IsNullOrWhiteSpace(options.TargetTable))
                throw new ArgumentException("目标表不能为空");
        }

        private static void CreateIntermediateSchema(DataSyncOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.IntermediateProvider) || string.IsNullOrWhiteSpace(options.IntermediateConnectionString))
                throw new ArgumentException("中间库 Provider 和连接字符串不能为空");

            using (var connection = CreateConnection(options.IntermediateProvider, options.IntermediateConnectionString))
            {
                connection.Open();
                var script = new IntermediateSchemaBuilder().BuildScript(options.IntermediateProvider);
                var statements = script.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var statement in statements)
                    ExecuteIgnoreError(connection, statement);
            }
        }

private static string BuildSourceSql(DataSyncOptions options, DbCommand command)
        {
            var columnList = options.SyncColumns != null && options.SyncColumns.Count > 0
                ? string.Join(",", options.SyncColumns)
                : "*";
            var sql = "select " + columnList + " from " + options.SourceTable;
            
            // 1. 如果启用了时间范围，按时间增量
            if (options.EnableTimeRange && !string.IsNullOrEmpty(options.TimeColumn))
            {
                var timeWhere = BuildTimeRangeFilter(options, command);
                if (!string.IsNullOrEmpty(timeWhere))
                    return sql + " where " + timeWhere;
            }
            
            // 2. 静态数据：按主键增量（如果有 LastSyncTime 记录上次的主键最大值）
            if (!options.EnableTimeRange && !string.IsNullOrEmpty(options.PrimaryKeyColumns) && options.IsAutoIncrementKey)
            {
                // 从配置中获取上次的 pk 最大值（存储为 DateTime.Ticks）
                var config = options.ConfigManager?.GetTaskConfig(options.TaskId);
                if (config?.LastSyncTime.HasValue == true && config.LastSyncTime.Value > DateTime.MinValue)
                {
                    var pkColumns = options.PrimaryKeyColumns.Split(',');
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@lastPkValue";
                    parameter.Value = config.LastSyncTime.Value.Ticks;
                    command.Parameters.Add(parameter);
                    return sql + " where " + pkColumns[0] + " > @lastPkValue";
                }
            }

            // 3. 没有时间范围且没有主键增量：全量查询
            return sql;
        }

        private static string BuildTimeRangeFilter(DataSyncOptions options, DbCommand command)
        {
            var timeColumn = options.TimeColumn;
            if (string.IsNullOrEmpty(timeColumn))
                return null;

            var startTime = DateTime.MinValue;
            var endTime = options.EndTime ?? DateTime.Now;

            // 首次同步：全量或从 0 开始
            var config = options.ConfigManager?.GetTaskConfig(options.TaskId);
            if (config == null || config.IsFirstSync)
            {
                if (options.IsFullSyncForFirstTime)
                    return null; // 首次全量
            }
            else if (config.LastSyncTime.HasValue)
            {
                // 后续同步：从 (上次同步时间 - rangeDays) 开始
                startTime = TimeRangeCalculator.GetSyncStartTime(config.LastSyncTime, options.RangeDays, false);
            }

            if (startTime == DateTime.MinValue)
                return null;

            var parameterStart = command.CreateParameter();
            parameterStart.ParameterName = "@startTime";
            parameterStart.Value = startTime;
            command.Parameters.Add(parameterStart);

            var parameterEnd = command.CreateParameter();
            parameterEnd.ParameterName = "@endTime";
            parameterEnd.Value = endTime;
            command.Parameters.Add(parameterEnd);

            return timeColumn + " >= @startTime AND " + timeColumn + " <= @endTime";
        }

        private static string BuildPrimaryKeyIncrementalSql(TablePrimaryKeyConfig pkConfig, string lastValue, DbCommand command)
        {
            if (pkConfig.IsAutoIncrement && pkConfig.PrimaryKeyColumns.Count == 1)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@lastValue";
                parameter.Value = lastValue;
                command.Parameters.Add(parameter);
                return pkConfig.PrimaryKeyColumns[0] + " > @lastValue";
            }

            var conditions = new System.Collections.Generic.List<string>();
            var lastValues = lastValue.Split('|');
            for (var i = 0; i < pkConfig.PrimaryKeyColumns.Count; i++)
            {
                var column = pkConfig.PrimaryKeyColumns[i];
                var value = i < lastValues.Length ? lastValues[i] : lastValues[lastValues.Length - 1];
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@lastValue" + i;
                parameter.Value = value;
                command.Parameters.Add(parameter);
                conditions.Add(column + " > @lastValue" + i);
            }

            return string.Join(" OR ", conditions);
        }

        private static DbConnection CreateConnection(string provider, string connectionString)
        {
            var factory = DbProviderFactories.GetFactory(provider);
            var connection = factory.CreateConnection();
            if (connection == null)
                throw new InvalidOperationException("无法创建数据库连接");

            connection.ConnectionString = connectionString;
            return connection;
        }

        private static bool TryInsertRowWithDedup(DbConnection connection, string tableName, DataTable table, DataRow row, int maxRetryCount, out int retryCount, string primaryKeyColumns = null)
        {
            retryCount = 0;
            for (var i = 0; i <= maxRetryCount; i++)
            {
                try
                {
                    var exists = CheckRowExists(connection, tableName, table, row, primaryKeyColumns);
                    if (exists)
                    {
                        return true;
                    }
                    else
                    {
                        InsertRow(connection, tableName, table, row);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("TryInsertRowWithDedup attempt {0} failed: {1}", i, ex.Message));
                    if (i == maxRetryCount)
                        return false;

                    retryCount++;
                }
            }

            return false;
        }

        private static bool TryInsertRow(DbConnection connection, string tableName, DataTable table, DataRow row, int maxRetryCount, out int retryCount)
        {
            retryCount = 0;
            for (var i = 0; i <= maxRetryCount; i++)
            {
                try
                {
                    UpsertRow(connection, tableName, table, row);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("TryInsertRow attempt {0} failed: {1}", i, ex.Message));
                    if (i == maxRetryCount)
                        return false;

                    retryCount++;
                }
            }

            return false;
        }

        private static void UpsertRow(DbConnection connection, string tableName, DataTable table, DataRow row, string primaryKeyColumns = null)
        {
            var exists = CheckRowExists(connection, tableName, table, row, primaryKeyColumns);
            if (exists)
                UpdateRow(connection, tableName, table, row, primaryKeyColumns);
            else
                InsertRow(connection, tableName, table, row);
        }

        private static bool CheckRowExists(DbConnection connection, string tableName, DataTable table, DataRow row, string primaryKeyColumns = null)
        {
            var pkColumns = GetPrimaryKeyColumns(table, primaryKeyColumns);
            if (pkColumns.Count == 0)
                return false;

            using (var command = connection.CreateCommand())
            {
                var conditions = new System.Collections.Generic.List<string>();
                for (var i = 0; i < pkColumns.Count; i++)
                {
                    var column = pkColumns[i];
                    var paramName = "@pk" + i;
                    conditions.Add(column + " = " + paramName);

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = paramName;
                    parameter.Value = row[column] == DBNull.Value ? DBNull.Value : row[column];
                    command.Parameters.Add(parameter);
                }

                command.CommandText = "select count(1) from " + tableName + " where " + string.Join(" AND ", conditions);
                var result = command.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
        }

        private static void UpdateRow(DbConnection connection, string tableName, DataTable table, DataRow row, string primaryKeyColumns = null)
        {
            using (var command = connection.CreateCommand())
            {
                var pkColumns = GetPrimaryKeyColumns(table, primaryKeyColumns);
                var setClauses = new System.Collections.Generic.List<string>();
                var whereClauses = new System.Collections.Generic.List<string>();

                for (var i = 0; i < table.Columns.Count; i++)
                {
                    var column = table.Columns[i].ColumnName;
                    var paramName = "@p" + i;
                    setClauses.Add(column + " = " + paramName);

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = paramName;
                    parameter.Value = row[column] == DBNull.Value ? DBNull.Value : row[column];
                    command.Parameters.Add(parameter);
                }

                for (var i = 0; i < pkColumns.Count; i++)
                {
                    var column = pkColumns[i];
                    var paramName = "@pk" + i;
                    whereClauses.Add(column + " = " + paramName);

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = paramName;
                    parameter.Value = row[column] == DBNull.Value ? DBNull.Value : row[column];
                    command.Parameters.Add(parameter);
                }

                command.CommandText = "update " + tableName + " set " + string.Join(",", setClauses) + " where " + string.Join(" AND ", whereClauses);
                command.ExecuteNonQuery();
            }
        }

        private static System.Collections.Generic.IList<string> GetPrimaryKeyColumns(DataTable table, string primaryKeyColumns = null)
        {
            var pkColumns = new System.Collections.Generic.List<string>();
            
            if (!string.IsNullOrEmpty(primaryKeyColumns))
            {
                foreach (var col in primaryKeyColumns.Split(','))
                {
                    var trimmed = col.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        pkColumns.Add(trimmed);
                }
                return pkColumns;
            }
            
            foreach (DataColumn column in table.PrimaryKey)
            {
                pkColumns.Add(column.ColumnName);
            }
            return pkColumns;
        }

        private static long? GetMaxPrimaryKeyValue(DataTable table, string pkColumnsStr)
        {
            if (table.Rows.Count == 0 || string.IsNullOrEmpty(pkColumnsStr))
                return null;

            var pkColumn = pkColumnsStr.Split(',')[0].Trim();
            long maxValue = 0;
            bool hasValue = false;

            foreach (DataRow row in table.Rows)
            {
                if (row[pkColumn] != DBNull.Value)
                {
                    var value = Convert.ToInt64(row[pkColumn]);
                    if (!hasValue || value > maxValue)
                    {
                        maxValue = value;
                        hasValue = true;
                    }
                }
            }

            return hasValue ? maxValue : (long?)null;
        }

        /// <summary>
        /// 直接从数据库查询最大主键值（优化大表性能）
        /// </summary>
        private static long? GetMaxPrimaryKeyValueFromDb(string provider, string connectionString, string tableName, string pkColumnsStr)
        {
            if (string.IsNullOrEmpty(pkColumnsStr))
                return null;

            var pkColumn = pkColumnsStr.Split(',')[0].Trim();

            try
            {
                using (var connection = CreateConnection(provider, connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format("SELECT MAX({0}) FROM {1}", pkColumn, tableName);
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return Convert.ToInt64(result);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("查询最大主键值失败: {0}", ex.Message));
            }

            return null;
        }

        private static void SaveFailedRecord(DataSyncOptions options, string taskId, DataTable table, DataRow row)
        {
            if (string.IsNullOrWhiteSpace(options.IntermediateProvider) || string.IsNullOrWhiteSpace(options.IntermediateConnectionString))
                return;

            using (var connection = CreateConnection(options.IntermediateProvider, options.IntermediateConnectionString))
            {
                connection.Open();
                SaveFailedRecord(connection, taskId, SerializeRow(table, row));
            }
        }

        private static void SaveFailedRecord(DbConnection connection, string taskId, string payload)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "insert into fd_sync_record (record_id, batch_id, record_key, payload, status, retry_count, error_message) values (@record_id, @batch_id, @record_key, @payload, @status, @retry_count, @error_message)";
                AddParameter(command, "@record_id", Guid.NewGuid().ToString("N"));
                AddParameter(command, "@batch_id", taskId);
                AddParameter(command, "@record_key", DBNull.Value);
                AddParameter(command, "@payload", payload);
                AddParameter(command, "@status", "Failed");
                AddParameter(command, "@retry_count", 0);
                AddParameter(command, "@error_message", "写入目标库失败");
                command.ExecuteNonQuery();
            }
        }

        private static int ResumeFailedRecords(DataSyncOptions options, DbConnection target, int maxRetryCount)
        {
            if (string.IsNullOrWhiteSpace(options.IntermediateProvider) || string.IsNullOrWhiteSpace(options.IntermediateConnectionString))
                return 0;

            var recoveredCount = 0;
            using (var intermediate = CreateConnection(options.IntermediateProvider, options.IntermediateConnectionString))
            {
                intermediate.Open();
                var failedRecords = LoadFailedRecords(intermediate);
                foreach (DataRow row in failedRecords.Rows)
                {
                    var payload = Convert.ToString(row["payload"]);
                    var data = BuildDataTableFromPayload(payload);
                    if (data.Rows.Count == 0)
                        continue;

                    int rowRetryCount;
                    if (TryInsertRow(target, options.TargetTable, data, data.Rows[0], maxRetryCount, out rowRetryCount))
                    {
                        MarkRecordSuccess(intermediate, Convert.ToString(row["record_id"]));
                        recoveredCount++;
                    }
                }
            }

            return recoveredCount;
        }

        private static DataTable LoadFailedRecords(DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "select record_id, payload from fd_sync_record where status = 'Failed'";
                var table = new DataTable();
                using (var reader = command.ExecuteReader())
                    table.Load(reader);
                return table;
            }
        }

        private static void MarkRecordSuccess(DbConnection connection, string recordId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "update fd_sync_record set status = 'Success' where record_id = @record_id";
                AddParameter(command, "@record_id", recordId);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertRowBatch(DbConnection connection, string tableName, DataTable table, List<DataRow> rows)
        {
            if (rows.Count == 0) return;

            using (var command = connection.CreateCommand())
            {
                var columns = new string[table.Columns.Count];
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    columns[i] = table.Columns[i].ColumnName;
                }

                var valueRows = new List<string>();
                var paramIndex = 0;

                foreach (var row in rows)
                {
                    var rowParams = new List<string>();
                    for (var i = 0; i < table.Columns.Count; i++)
                    {
                        var paramName = "@p" + paramIndex++;
                        rowParams.Add(paramName);

                        var parameter = command.CreateParameter();
                        parameter.ParameterName = paramName;
                        parameter.Value = row[columns[i]] == DBNull.Value ? DBNull.Value : row[columns[i]];
                        command.Parameters.Add(parameter);
                    }
                    valueRows.Add("(" + string.Join(",", rowParams) + ")");
                }

                command.CommandText = "insert into " + tableName + " (" + string.Join(",", columns) + ") values " + string.Join(",", valueRows);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertRow(DbConnection connection, string tableName, DataTable table, DataRow row)
        {
            using (var command = connection.CreateCommand())
            {
                var columns = new string[table.Columns.Count];
                var parameters = new string[table.Columns.Count];
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    var column = table.Columns[i].ColumnName;
                    var parameterName = "@p" + i;
                    columns[i] = column;
                    parameters[i] = parameterName;

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = parameterName;
                    parameter.Value = row[column] == DBNull.Value ? DBNull.Value : row[column];
                    command.Parameters.Add(parameter);
                }

                command.CommandText = "insert into " + tableName + " (" + string.Join(",", columns) + ") values (" + string.Join(",", parameters) + ")";
                command.ExecuteNonQuery();
            }
        }

        private static void AddParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static string SerializeRow(DataTable table, DataRow row)
        {
            return DataRowSerializer.Serialize(table, row);
        }

        private static DataTable BuildDataTableFromPayload(string payload)
        {
            return DataRowSerializer.Deserialize(payload);
        }

        private static void CleanIntermediateData(DataSyncOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.IntermediateProvider) || string.IsNullOrWhiteSpace(options.IntermediateConnectionString))
                return;

            using (var connection = CreateConnection(options.IntermediateProvider, options.IntermediateConnectionString))
            {
                connection.Open();
                ExecuteIgnoreError(connection, "delete from fd_sync_record where status = 'Success'");
                ExecuteIgnoreError(connection, "delete from fd_sync_batch where status = 'Success'");
            }
        }

        private static void ExecuteIgnoreError(DbConnection connection, string sql)
        {
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("清理中间库数据失败: {0}, 错误: {1}", sql, ex.Message));
            }
        }
    }
}
