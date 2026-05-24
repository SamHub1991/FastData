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

            var batchSize = options.BatchSize <= 0 ? 500 : options.BatchSize;
            var table = new DataTable();
            using (var source = CreateConnection(options.SourceProvider, options.SourceConnectionString))
            using (var command = source.CreateCommand())
            {
                source.Open();
                command.CommandText = "select * from " + options.SourceTable;
                using (var reader = command.ExecuteReader())
                {
                    table.Load(reader);
                }
            }

            var writeCount = 0;
            using (var target = CreateConnection(options.TargetProvider, options.TargetConnectionString))
            {
                target.Open();
                foreach (DataRow row in table.Rows)
                {
                    InsertRow(target, options.TargetTable, table, row);
                    writeCount++;
                    if (writeCount >= batchSize && batchSize > 0)
                    {
                    }
                }
            }

            return new DataSyncResult
            {
                ReadCount = table.Rows.Count,
                WriteCount = writeCount,
                Message = "同步完成"
            };
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
    }
}
