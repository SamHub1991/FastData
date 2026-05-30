using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace FastData.Tooling.Database
{
    public class ProviderMetadataReader : IDatabaseMetadataReader
    {
        private readonly DatabaseConnectionOptions options;

        public ProviderMetadataReader(DatabaseConnectionOptions options)
        {
            this.options = options;
        }

        public bool TestConnection()
        {
            using (var connection = CreateConnection())
            {
                connection.Open();
                return connection.State == ConnectionState.Open;
            }
        }

        public IList<DatabaseTable> GetTables()
        {
            using (var connection = CreateConnection())
            {
                connection.Open();
                var schema = connection.GetSchema("Tables");
                var result = new List<DatabaseTable>();
                foreach (DataRow row in schema.Rows)
                {
                    var tableType = GetValue(row, "TABLE_TYPE");
                    
                    // 支持表（Table）和视图（View）
                    var isView = !string.IsNullOrEmpty(tableType) && tableType.ToLower().Contains("view");
                    
                    result.Add(new DatabaseTable
                    {
                        Schema = GetValue(row, "TABLE_SCHEMA"),
                        Name = GetValue(row, "TABLE_NAME"),
                        Comment = GetValue(row, "DESCRIPTION"),
                        IsView = isView
                    });
                }
                return result;
            }
        }

        public IList<DatabaseColumn> GetColumns(string tableName)
        {
            using (var connection = CreateConnection())
            {
                connection.Open();
                var restrictions = new string[4];
                var parts = tableName.Split('.');
                if (parts.Length == 2)
                {
                    restrictions[1] = parts[0];
                    restrictions[2] = parts[1];
                }
                else
                    restrictions[2] = tableName;

                var schema = connection.GetSchema("Columns", restrictions);
                var keys = GetPrimaryKeys(connection, tableName);
                var result = new List<DatabaseColumn>();
                foreach (DataRow row in schema.Rows)
                {
                    var name = GetValue(row, "COLUMN_NAME");
                    result.Add(new DatabaseColumn
                    {
                        Name = name,
                        DbType = GetValue(row, "DATA_TYPE"),
                        Length = ToInt(row, "CHARACTER_MAXIMUM_LENGTH"),
                        Precision = ToInt(row, "NUMERIC_PRECISION"),
                        Scale = ToInt(row, "NUMERIC_SCALE"),
                        IsNullable = IsNullable(row),
                        IsPrimaryKey = keys.Contains(name),
                        Comment = GetValue(row, "DESCRIPTION")
                    });
                }
                return result;
            }
        }

        private DbConnection CreateConnection()
        {
            var factory = DbProviderFactories.GetFactory(options.Provider);
            var connection = factory.CreateConnection();
            if (connection == null)
                throw new InvalidOperationException("无法创建数据库连接");

            connection.ConnectionString = options.ConnectionString;
            return connection;
        }

        private static List<string> GetPrimaryKeys(DbConnection connection, string tableName)
        {
            var result = new List<string>();
            try
            {
                var restrictions = new string[4];
                var parts = tableName.Split('.');
                if (parts.Length == 2)
                {
                    restrictions[1] = parts[0];
                    restrictions[2] = parts[1];
                }
                else
                    restrictions[2] = tableName;

                var schema = connection.GetSchema("IndexColumns", restrictions);
                foreach (DataRow row in schema.Rows)
                {
                    var constraint = GetValue(row, "CONSTRAINT_NAME");
                    var index = GetValue(row, "INDEX_NAME");
                    if (ContainsPrimaryKeyName(constraint) || ContainsPrimaryKeyName(index))
                    {
                        var column = GetValue(row, "COLUMN_NAME");
                        if (!string.IsNullOrEmpty(column) && !result.Contains(column))
                            result.Add(column);
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        private static bool ContainsPrimaryKeyName(string value)
        {
            return !string.IsNullOrEmpty(value) && value.ToLower().Contains("pk");
        }

        private static string GetValue(DataRow row, string name)
        {
            if (!row.Table.Columns.Contains(name) || row[name] == DBNull.Value)
                return null;

            return Convert.ToString(row[name]);
        }

        private static int ToInt(DataRow row, string name)
        {
            var value = GetValue(row, name);
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }

        private static bool IsNullable(DataRow row)
        {
            var value = GetValue(row, "IS_NULLABLE");
            return string.IsNullOrEmpty(value) || value.Equals("YES", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
