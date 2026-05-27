using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Xml;

namespace FastData.Tooling.Sync
{
    public class DataSyncService
    {
        public DataSyncResult SyncTable(DataSyncOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            ValidateOptions(options);
            var batchSize = options.BatchSize <= 0 ? 500 : options.BatchSize;
            var maxRetryCount = options.RetryCount < 0 ? 0 : options.RetryCount;
            var taskId = string.IsNullOrWhiteSpace(options.TaskId) ? options.SourceTable + "_to_" + options.TargetTable : options.TaskId;
            if (options.AutoCreateIntermediateSchema)
                CreateIntermediateSchema(options);

            // 应用全局配置
            ApplyGlobalConfig(options);

            var table = new DataTable();
            using (var source = CreateConnection(options.SourceProvider, options.SourceConnectionString))
            using (var command = source.CreateCommand())
            {
                source.Open();
                command.CommandText = BuildSourceSql(options, command);
                using (var reader = command.ExecuteReader())
                {
                    table.Load(reader);
                }
            }

            var writeCount = 0;
            var failedCount = 0;
            var retryCount = 0;
            var recoveredCount = 0;
            using (var target = CreateConnection(options.TargetProvider, options.TargetConnectionString))
            {
                target.Open();

                if (options.ResumeFailedRecords)
                    recoveredCount = ResumeFailedRecords(options, target, maxRetryCount);

                var batchRows = new List<DataRow>();
                foreach (DataRow row in table.Rows)
                {
                    int rowRetryCount = 0;
                    if (options.AlwaysDeduplicate && !string.IsNullOrEmpty(options.PrimaryKeyColumns))
                    {
                        if (TryInsertRowWithDedup(target, options.TargetTable, table, row, maxRetryCount, out rowRetryCount, options.PrimaryKeyColumns))
                            writeCount++;
                        else
                        {
                            failedCount++;
                            SaveFailedRecord(options, taskId, table, row);
                        }
                    }
                    else
                    {
                        // 批量插入模式
                        batchRows.Add(row);
                        if (batchRows.Count >= batchSize)
                        {
                            try
                            {
                                InsertRowBatch(target, options.TargetTable, table, batchRows);
                                writeCount += batchRows.Count;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("Batch insert failed, falling back to row-by-row: {0}", ex.Message));
                                // 批量失败则逐行重试
                                foreach (var batchRow in batchRows)
                                {
                                    try
                                    {
                                        InsertRow(target, options.TargetTable, table, batchRow);
                                        writeCount++;
                                    }
                                    catch (Exception rowEx)
                                    {
                                        System.Diagnostics.Debug.WriteLine(string.Format("Row insert failed: {0}", rowEx.Message));
                                        failedCount++;
                                        SaveFailedRecord(options, taskId, table, batchRow);
                                    }
                                }
                            }
                            batchRows.Clear();
                        }
                    }

                    retryCount += rowRetryCount;
                }

                // 处理剩余批次
                if (batchRows.Count > 0 && !options.AlwaysDeduplicate)
                {
                    try
                    {
                        InsertRowBatch(target, options.TargetTable, table, batchRows);
                        writeCount += batchRows.Count;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format("Final batch insert failed, falling back to row-by-row: {0}", ex.Message));
                        // 批量失败则逐行重试
                        foreach (var batchRow in batchRows)
                        {
                            try
                            {
                                InsertRow(target, options.TargetTable, table, batchRow);
                                writeCount++;
                            }
                            catch (Exception rowEx)
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("Final row insert failed: {0}", rowEx.Message));
                                failedCount++;
                                SaveFailedRecord(options, taskId, table, batchRow);
                            }
                        }
                    }
                }

                if (options.CleanIntermediateData)
                    CleanIntermediateData(options);
            }

            var result = new DataSyncResult
            {
                ReadCount = table.Rows.Count,
                WriteCount = writeCount,
                FailedCount = failedCount,
                RetryCount = retryCount,
                RecoveredCount = recoveredCount,
                Message = string.Format("同步完成 [去重模式：{0}]", options.AlwaysDeduplicate ? "是" : "否"),
                MaxPkValue = GetMaxPrimaryKeyValue(table, options.PrimaryKeyColumns)
            };

            var syncTime = DateTime.Now;
            if (options.ConfigManager != null && !string.IsNullOrEmpty(options.TaskId))
            {
                if (options.EnableTimeRange && result.ReadCount > 0)
                {
                    options.ConfigManager.UpdateLastSyncTime(options.TaskId, syncTime);
                }
                else if (!options.EnableTimeRange && result.MaxPkValue.HasValue)
                {
                    var dt = DateTime.MinValue.AddTicks(result.MaxPkValue.Value);
                    options.ConfigManager.UpdateLastSyncTime(options.TaskId, dt);
                }
            }

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
