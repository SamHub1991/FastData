using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace FastData.SyncTool
{
    /// <summary>
    /// 数据补录服务
    /// </summary>
    public class ReplayService
    {
        private readonly DbConnection sourceConnection;
        private readonly DbConnection targetConnection;
        private readonly string sourceProvider;
        private readonly string targetProvider;

        public ReplayService(DbConnection sourceConn, DbConnection targetConn, string sourceProvider, string targetProvider)
        {
            this.sourceConnection = sourceConn;
            this.targetConnection = targetConn;
            this.sourceProvider = sourceProvider;
            this.targetProvider = targetProvider;
        }

        /// <summary>
        /// 补录结果
        /// </summary>
        public class ReplayResult
        {
            public int ReadCount { get; set; }
            public int UpdateCount { get; set; }
            public int InsertCount { get; set; }
            public int SkipCount { get; set; }
            public int FailedCount { get; set; }
        }

        /// <summary>
        /// 补录表数据
        /// </summary>
        public async Task<ReplayResult> ReplayTableAsync(
            string tableName,
            string primaryKeyColumns,
            bool enableTimeRange,
            DateTime? startTime,
            DateTime? endTime,
            Action<string> log = null)
        {
            var result = new ReplayResult();

            try
            {
                // 1. 获取表结构
                var sourceTable = GetTableSchema(sourceConnection, tableName);
                var targetTable = GetTableSchema(targetConnection, tableName);

                if (sourceTable == null || targetTable == null)
                {
                    log?.Invoke($"表 {tableName} 结构不存在");
                    return result;
                }

                log?.Invoke($"源表列数：{sourceTable.Columns.Count}, 目标表列数：{targetTable.Columns.Count}");

                // 2. 解析主键
                var primaryKeyList = ParsePrimaryKey(primaryKeyColumns);

                // 3. 构建查询 SQL
                var selectSql = BuildSelectSql(sourceTable, primaryKeyList, enableTimeRange, startTime, endTime);
                log?.Invoke($"查询 SQL: {selectSql}");

                // 4. 读取源数据
                var rows = new List<DataRow>();
                using (var cmd = sourceConnection.CreateCommand())
                {
                    cmd.CommandText = selectSql;
                    cmd.CommandTimeout = 300;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var loadTable = new DataTable();
                        loadTable.Load(reader);
                        foreach (DataRow row in loadTable.Rows)
                        {
                            rows.Add(row);
                        }
                        result.ReadCount = rows.Count;
                    }
                }

                log?.Invoke($"读取到 {result.ReadCount} 条记录");

                // 5. 获取目标表现有主键值（用于去重）
                var existingKeys = new HashSet<string>();
                if (!string.IsNullOrEmpty(primaryKeyColumns))
                {
                    log?.Invoke("正在加载目标表现有主键...");
                    existingKeys = LoadExistingKeys(targetConnection, tableName, primaryKeyList);
                    log?.Invoke($"目标表现有主键数：{existingKeys.Count}");
                }

                // 6. 处理每条记录
                var batchSize = 100;
                for (int i = 0; i < rows.Count; i += batchSize)
                {
                    var batch = rows.Skip(i).Take(batchSize).ToList();
                    foreach (var row in batch)
                    {
                        try
                        {
                            var key = BuildKey(row, primaryKeyList);
                            
                            if (existingKeys.Contains(key))
                            {
                                // 更新已有记录
                                await UpdateRowAsync(targetConnection, targetTable, row);
                                result.UpdateCount++;
                            }
                            else
                            {
                                // 插入新记录
                                await InsertRowAsync(targetConnection, targetTable, row);
                                result.InsertCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            result.FailedCount++;
                            log?.Invoke($"处理记录失败：{ex.Message}");
                        }
                    }

                    log?.Invoke($"进度：{Math.Min(i + batchSize, rows.Count)}/{rows.Count}");
                }

                log?.Invoke("补录完成");
            }
            catch (Exception ex)
            {
                log?.Invoke($"补录异常：{ex.Message}");
                throw;
            }

            return result;
        }

        private DataTable GetTableSchema(DbConnection connection, string tableName)
        {
            try
            {
                var schema = connection.GetSchema("Columns", new[] { null, null, tableName });
                var table = new DataTable(tableName);

                foreach (DataRow row in schema.Rows)
                {
                    var columnName = row["COLUMN_NAME"].ToString();
                    var dataType = row["DATA_TYPE"].ToString();
                    var isNullable = row["IS_NULLABLE"].ToString() == "YES";
                    var isPrimaryKey = row["COLUMN_KEY"] != null && row["COLUMN_KEY"].ToString() == "PRI";

                    table.Columns.Add(columnName, typeof(string));
                }

                return table;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private List<string> ParsePrimaryKey(string primaryKeyColumns)
        {
            if (string.IsNullOrEmpty(primaryKeyColumns))
                return new List<string>();

            return primaryKeyColumns.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .ToList();
        }

        private string BuildSelectSql(DataTable table, List<string> primaryKeyList, bool enableTimeRange, DateTime? startTime, DateTime? endTime)
        {
            var columns = string.Join(", ", table.Columns.Cast<DataColumn>().Select(c => GetQuotedColumnName(c.ColumnName)));
            var sql = $"SELECT {columns} FROM {GetQuotedTableName(table.TableName)}";

            if (enableTimeRange && startTime.HasValue && endTime.HasValue)
            {
                var timeColumn = GuessTimeColumn(table, primaryKeyList);
                if (!string.IsNullOrEmpty(timeColumn))
                {
                    sql += $" WHERE {GetQuotedColumnName(timeColumn)} >= '{startTime:yyyy-MM-dd HH:mm:ss}'";
                    sql += $" AND {GetQuotedColumnName(timeColumn)} <= '{endTime:yyyy-MM-dd HH:mm:ss}'";
                }
            }

            return sql;
        }

        private string GuessTimeColumn(DataTable table, List<string> primaryKeyList)
        {
            var timeColumnNames = new[] { "CreateTime", "UpdateTime", "CreateTime", "UpdatedTime", "CreateDate", "UpdateDate", "CreateDate", "UpdateDate" };
            
            foreach (var col in table.Columns.Cast<DataColumn>())
            {
                if (timeColumnNames.Any(t => t.Equals(col.ColumnName, StringComparison.OrdinalIgnoreCase)))
                    return col.ColumnName;
            }

            // 尝试找包含"Time"或"Date"的列
            foreach (var col in table.Columns.Cast<DataColumn>())
            {
                if (!primaryKeyList.Contains(col.ColumnName) && 
                    (col.ColumnName.IndexOf("Time", StringComparison.OrdinalIgnoreCase) >= 0 || 
                     col.ColumnName.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0))
                    return col.ColumnName;
            }

            return null;
        }

        private HashSet<string> LoadExistingKeys(DbConnection connection, string tableName, List<string> primaryKeyList)
        {
            var keys = new HashSet<string>();

            if (primaryKeyList.Count == 0)
                return keys;

            try
            {
                var keyColumns = string.Join(", ", primaryKeyList.Select(c => GetQuotedColumnName(c)));
                var sql = $"SELECT {keyColumns} FROM {GetQuotedTableName(tableName)}";

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 60;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var values = new List<string>();
                            foreach (var pkCol in primaryKeyList)
                            {
                                var value = reader[pkCol];
                                values.Add(value?.ToString() ?? "");
                            }
                            keys.Add(string.Join("|", values));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 忽略错误，返回空集合
            }

            return keys;
        }

        private string BuildKey(DataRow row, List<string> primaryKeyList)
        {
            var values = new List<string>();
            foreach (var pkCol in primaryKeyList)
            {
                var value = row[pkCol];
                values.Add(value?.ToString() ?? "");
            }
            return string.Join("|", values);
        }

        private async Task InsertRowAsync(DbConnection connection, DataTable table, DataRow row)
        {
            var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var columnNames = string.Join(", ", columns.Select(c => GetQuotedColumnName(c)));
            var parameters = string.Join(", ", columns.Select((c, i) => "@p" + i));

            var sql = $"INSERT INTO {GetQuotedTableName(table.TableName)} ({columnNames}) VALUES ({parameters})";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = 30;

                for (int i = 0; i < columns.Count; i++)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = "@p" + i;
                    param.Value = row[columns[i]] ?? DBNull.Value;
                    cmd.Parameters.Add(param);
                }

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task UpdateRowAsync(DbConnection connection, DataTable table, DataRow row, List<string> primaryKeyList = null)
        {
            // 从 table 中获取主键（如果有）
            if (primaryKeyList == null)
            {
                primaryKeyList = new List<string>();
                // 简单处理：假设第一列是主键
                if (table.Columns.Count > 0)
                    primaryKeyList.Add(table.Columns[0].ColumnName);
            }

            var setParts = new List<string>();
            var whereParts = new List<string>();
            var paramIndex = 0;

            var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "";
                cmd.CommandTimeout = 30;

                // 构建 SET 部分（排除主键）
                foreach (var col in columns)
                {
                    if (!primaryKeyList.Contains(col))
                    {
                        setParts.Add($"{GetQuotedColumnName(col)} = @p{paramIndex}");
                        var param = cmd.CreateParameter();
                        param.ParameterName = "@p" + paramIndex;
                        param.Value = row[col] ?? DBNull.Value;
                        cmd.Parameters.Add(param);
                        paramIndex++;
                    }
                }

                // 构建 WHERE 部分（主键）
                foreach (var pkCol in primaryKeyList)
                {
                    whereParts.Add($"{GetQuotedColumnName(pkCol)} = @p{paramIndex}");
                    var param = cmd.CreateParameter();
                    param.ParameterName = "@p" + paramIndex;
                    param.Value = row[pkCol] ?? DBNull.Value;
                    cmd.Parameters.Add(param);
                    paramIndex++;
                }

                var sql = $"UPDATE {GetQuotedTableName(table.TableName)} SET {string.Join(", ", setParts)} WHERE {string.Join(" AND ", whereParts)}";
                cmd.CommandText = sql;

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private string GetQuotedColumnName(string columnName)
        {
            // 根据数据库类型使用不同的引号
            if (sourceProvider.Contains("MySql"))
                return $"`{columnName}`";
            else if (sourceProvider.Contains("Oracle"))
                return $"\"{columnName}\"";
            else
                return $"[{columnName}]"; // SQL Server
        }

        private string GetQuotedTableName(string tableName)
        {
            if (sourceProvider.Contains("MySql"))
                return $"`{tableName}`";
            else if (sourceProvider.Contains("Oracle"))
                return $"\"{tableName}\"";
            else
                return $"[{tableName}]"; // SQL Server
        }
    }
}
