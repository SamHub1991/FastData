using System;
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

                foreach (DataRow row in table.Rows)
                {
                    int rowRetryCount;
                    if (TryInsertRow(target, options.TargetTable, table, row, maxRetryCount, out rowRetryCount))
                        writeCount++;
                    else
                    {
                        failedCount++;
                        SaveFailedRecord(options, taskId, table, row);
                    }

                    retryCount += rowRetryCount;
                    if (writeCount >= batchSize && batchSize > 0)
                    {
                    }
                }

                if (options.CleanIntermediateData)
                    CleanIntermediateData(options);
            }

            return new DataSyncResult
            {
                ReadCount = table.Rows.Count,
                WriteCount = writeCount,
                FailedCount = failedCount,
                RetryCount = retryCount,
                RecoveredCount = recoveredCount,
                Message = "同步完成",
                LastSyncValue = options.LastValue
            };
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
            var sql = "select * from " + options.SourceTable;
            var whereClauses = new System.Collections.Generic.List<string>();

            // 1. 使用配置服务获取主键配置
            if (options.PrimaryKeyConfigService != null)
            {
                var pkConfig = options.PrimaryKeyConfigService.GetTableConfig(options.SourceTable);
                if (pkConfig != null && pkConfig.PrimaryKeyColumns != null && pkConfig.PrimaryKeyColumns.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(options.LastValue))
                    {
                        whereClauses.Add(BuildPrimaryKeyIncrementalSql(pkConfig, options.LastValue, command));
                    }
                }
            }

            // 2. 使用内联主键配置（向后兼容）
            if (whereClauses.Count == 0 && !string.IsNullOrWhiteSpace(options.PrimaryKeyColumns))
            {
                var pkColumns = options.PrimaryKeyColumns.Split(',');
                if (options.IsAutoIncrementKey && pkColumns.Length == 1 && !string.IsNullOrWhiteSpace(options.LastValue))
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@lastValue";
                    parameter.Value = options.LastValue;
                    command.Parameters.Add(parameter);
                    whereClauses.Add(pkColumns[0] + " > @lastValue");
                }
            }

            // 3. 使用增量字段配置（向后兼容）
            if (whereClauses.Count == 0 && !string.IsNullOrWhiteSpace(options.IncrementalColumn) && !string.IsNullOrWhiteSpace(options.LastValue))
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@lastValue";
                parameter.Value = options.LastValue;
                command.Parameters.Add(parameter);
                whereClauses.Add(options.IncrementalColumn + " > @lastValue");
            }

            if (whereClauses.Count > 0)
                return sql + " where " + string.Join(" AND ", whereClauses);

            return sql;
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
                catch
                {
                    if (i == maxRetryCount)
                        return false;

                    retryCount++;
                }
            }

            return false;
        }

        private static void UpsertRow(DbConnection connection, string tableName, DataTable table, DataRow row)
        {
            // 先检查记录是否存在
            var exists = CheckRowExists(connection, tableName, table, row);
            if (exists)
                UpdateRow(connection, tableName, table, row);
            else
                InsertRow(connection, tableName, table, row);
        }

        private static bool CheckRowExists(DbConnection connection, string tableName, DataTable table, DataRow row)
        {
            var pkColumns = GetPrimaryKeyColumns(table);
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

        private static void UpdateRow(DbConnection connection, string tableName, DataTable table, DataRow row)
        {
            using (var command = connection.CreateCommand())
            {
                var pkColumns = GetPrimaryKeyColumns(table);
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

        private static System.Collections.Generic.IList<string> GetPrimaryKeyColumns(DataTable table)
        {
            var pkColumns = new System.Collections.Generic.List<string>();
            foreach (DataColumn column in table.PrimaryKey)
            {
                pkColumns.Add(column.ColumnName);
            }
            return pkColumns;
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
            var data = table.Clone();
            data.ImportRow(row);
            using (var writer = new StringWriter())
            {
                data.WriteXml(writer, XmlWriteMode.WriteSchema);
                return writer.ToString();
            }
        }

        private static DataTable BuildDataTableFromPayload(string payload)
        {
            var table = new DataTable();
            using (var reader = new StringReader(payload ?? string.Empty))
                table.ReadXml(reader);
            return table;
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
            catch
            {
            }
        }
    }
}
