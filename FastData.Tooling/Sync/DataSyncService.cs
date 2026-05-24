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
                Message = "同步完成"
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
            if (string.IsNullOrWhiteSpace(options.IncrementalColumn) || string.IsNullOrWhiteSpace(options.LastValue))
                return sql;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@lastValue";
            parameter.Value = options.LastValue;
            command.Parameters.Add(parameter);
            return sql + " where " + options.IncrementalColumn + " > @lastValue";
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
                    InsertRow(connection, tableName, table, row);
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
