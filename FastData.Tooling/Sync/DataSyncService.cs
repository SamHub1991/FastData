using System;
using System.Data;
using System.Data.Common;

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
            using (var target = CreateConnection(options.TargetProvider, options.TargetConnectionString))
            {
                target.Open();
                foreach (DataRow row in table.Rows)
                {
                    int rowRetryCount;
                    if (TryInsertRow(target, options.TargetTable, table, row, maxRetryCount, out rowRetryCount))
                        writeCount++;
                    else
                        failedCount++;

                    retryCount += rowRetryCount;
                    if (writeCount >= batchSize && batchSize > 0)
                    {
                    }
                }

                if (options.CleanIntermediateData)
                    CleanIntermediateData(target);
            }

            return new DataSyncResult
            {
                ReadCount = table.Rows.Count,
                WriteCount = writeCount,
                FailedCount = failedCount,
                RetryCount = retryCount,
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

        private static void CleanIntermediateData(DbConnection connection)
        {
            ExecuteIgnoreError(connection, "delete from fd_sync_record where status = 'Success'");
            ExecuteIgnoreError(connection, "delete from fd_sync_batch where status = 'Success'");
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
