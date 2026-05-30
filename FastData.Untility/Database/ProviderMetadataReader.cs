using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace FastData.Tooling.Database
{
    /// <summary>
    /// 基于 DbProviderFactories 的数据库元数据读取器
    /// 
    /// 使用 ADO.NET 的 DbProviderFactories 读取数据库元数据信息。
    /// </summary>
    public class ProviderMetadataReader : IDatabaseMetadataReader
    {
        private readonly DatabaseConnectionOptions options;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">数据库连接选项</param>
        public ProviderMetadataReader(DatabaseConnectionOptions options)
        {
            this.options = options;
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        /// <returns>连接是否成功</returns>
        public bool TestConnection()
        {
            using (var connection = CreateConnection())
            {
                connection.Open();
                return connection.State == ConnectionState.Open;
            }
        }

        /// <summary>
        /// 获取所有表信息
        /// </summary>
        /// <returns>表列表</returns>
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

        /// <summary>
        /// 获取指定表的列信息
        /// </summary>
        /// <param name="tableName">表名（支持 schema.table 格式）</param>
        /// <returns>列列表</returns>
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

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <returns>数据库连接对象</returns>
        private DbConnection CreateConnection()
        {
            var factory = DbProviderFactories.GetFactory(options.Provider);
            var connection = factory.CreateConnection();
            if (connection == null)
                throw new InvalidOperationException("无法创建数据库连接");

            connection.ConnectionString = options.ConnectionString;
            return connection;
        }

        /// <summary>
        /// 获取主键列名列表
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="tableName">表名</param>
        /// <returns>主键列名列表</returns>
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

        /// <summary>
        /// 判断是否包含主键名称
        /// </summary>
        /// <param name="value">约束或索引名称</param>
        /// <returns>是否包含 pk</returns>
        private static bool ContainsPrimaryKeyName(string value)
        {
            return !string.IsNullOrEmpty(value) && value.ToLower().Contains("pk");
        }

        /// <summary>
        /// 安全获取 DataRow 中的值
        /// </summary>
        /// <param name="row">数据行</param>
        /// <param name="name">列名</param>
        /// <returns>字符串值，不存在或为 DBNull 则返回 null</returns>
        private static string GetValue(DataRow row, string name)
        {
            if (!row.Table.Columns.Contains(name) || row[name] == DBNull.Value)
                return null;

            return Convert.ToString(row[name]);
        }

        /// <summary>
        /// 安全获取 DataRow 中的整数值
        /// </summary>
        /// <param name="row">数据行</param>
        /// <param name="name">列名</param>
        /// <returns>整数值，解析失败返回 0</returns>
        private static int ToInt(DataRow row, string name)
        {
            var value = GetValue(row, name);
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }

        /// <summary>
        /// 判断列是否允许为空
        /// </summary>
        /// <returns>是否允许为空</returns>
        private static bool IsNullable(DataRow row)
        {
            var value = GetValue(row, "IS_NULLABLE");
            return string.IsNullOrEmpty(value) || value.Equals("YES", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
