using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using FastData.Property;
using FastUntility.Base;

namespace FastData.Base
{
    /// <summary>
    /// Command 参数处理类
    /// 提供 Oracle 类型映射、TVP（表值参数）生成、DataTable 转换等功能
    /// </summary>
    internal static class CommandParam
    {
        /// <summary>
        /// 将 Oracle 列类型名称转换为 DbType 枚举
        /// </summary>
        /// <param name="typeName">Oracle 列类型名称（如 "string"、"datetime"、"decimal"）</param>
        /// <returns>对应的 DbType 枚举值</returns>
        public static DbType GetOracleDbType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return DbType.Object;

            switch (typeName.ToLowerInvariant())
            {
                case "string":
                    return DbType.String;
                case "datetime":
                    return DbType.DateTime;
                case "decimal":
                    return DbType.Decimal;
                case "int32":
                    return DbType.Int32;
                case "int64":
                    return DbType.Int64;
                case "byte[]":
                    return DbType.Byte;
                case "float":
                case "double":
                    return DbType.Double;
                default:
                    return DbType.Object;
            }
        }

        /// <summary>
        /// 生成 SQL Server TVP（表值参数）插入语句
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>TVP 插入 SQL 语句</returns>
        public static string GetTvps<T>() where T : class, new()
        {
            var columns = new StringBuilder();
            var select = new StringBuilder();
            // 使用缓存的非 Identity 属性数组
            var filteredProperties = PropertyCache.GetNonIdentityProperties<T>();

            columns.AppendFormat("insert into {0} (", TableNameHelper.GetTableName<T>());
            select.Append("select ");

            foreach (var prop in filteredProperties)
            {
                columns.AppendFormat("{0},", prop.Name);
                select.AppendFormat("tb.{0},", prop.Name);
            }

            columns.Append(")");
            select.AppendFormat("from @{0}_TVP as tb", typeof(T).Name);

            var columnSql = columns.ToString().Replace(",)", ") ");
            var selectSql = select.ToString().Replace(",from", " from");
            return string.Concat(columnSql, selectSql);
        }

        /// <summary>
        /// 将实体列表转换为 DataTable
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">数据库命令对象</param>
        /// <param name="dataList">实体列表</param>
        /// <returns>填充了数据的 DataTable</returns>
        public static DataTable GetTable<T>(DbCommand cmd, List<T> dataList) where T : class, new()
        {
            var entityGetter = new Property.DynamicGet<T>();
            var dataTable = new DataTable();
            // 使用缓存的非 Identity 属性数组
            var filteredProperties = PropertyCache.GetNonIdentityProperties<T>();

            foreach (var prop in filteredProperties)
            {
                dataTable.Columns.Add(prop.Name, prop.PropertyType);
            }

            foreach (var entity in dataList)
            {
                var row = dataTable.NewRow();
                foreach (var prop in filteredProperties)
                {
                    row[prop.Name] = entityGetter.GetValue(entity, prop.Name, true) ?? (object)DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        /// <summary>
        /// 初始化 SQL Server TVP（表值参数）类型定义
        /// 从数据库查询表结构并生成 CREATE TYPE 语句
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cmd">数据库命令对象</param>
        public static void InitTvps<T>(DbCommand cmd)
        {
            var entityType = typeof(T);
            var tableName = TableNameHelper.GetTableName<T>();

            // 查询表结构
            cmd.CommandText = string.Format(
                "select a.name,(select top 1 name from sys.systypes c where a.xtype=c.xtype) as type,length,isnullable,prec,scale,(a.status & 0x80) as is_identity from syscolumns a where a.id=object_id('{0}') order by a.colid asc",
                tableName);

            var dataReader = cmd.ExecuteReader();
            var columnData = BaseJson.DataReaderToDic(dataReader);
            dataReader.Close();

            // 过滤掉 Identity 自增列
            var nonIdentityColumns = columnData.Where(item => item.GetValue("is_identity").ToStr() != "128").ToList();

            var typeBuilder = new StringBuilder();
            typeBuilder.AppendFormat("if not exists(SELECT 1 FROM sys.table_types where name='{0}_TVP')", entityType.Name);
            typeBuilder.AppendFormat("CREATE TYPE {0}_TVP AS TABLE(", entityType.Name);

            foreach (var column in nonIdentityColumns)
            {
                var columnName = column.GetValue("name");
                var columnType = column.GetValue("type");
                var isNullable = column.GetValue("isnullable").ToStr() == "1" ? "NULL" : "NOT NULL";

                switch (columnType.ToStr())
                {
                    case "char":
                    case "nchar":
                    case "varchar":
                    case "nvarchar":
                    case "varchar2":
                    case "nvarchar2":
                        var length = column.GetValue("length");
                        typeBuilder.AppendFormat("[{0}] [{1}]({2}) {3},", columnName, columnType, length, isNullable);
                        break;

                    case "decimal":
                    case "numeric":
                    case "number":
                        var precision = column.GetValue("prec");
                        var scale = column.GetValue("scale");
                        if (precision.ToStr() == "0" && scale.ToStr() == "0")
                            typeBuilder.AppendFormat("[{0}] [{1}] {2},", columnName, columnType, isNullable);
                        else
                            typeBuilder.AppendFormat("[{0}] [{1}]({2},{3}) {4},", columnName, columnType, precision, scale, isNullable);
                        break;

                    default:
                        typeBuilder.AppendFormat("[{0}] [{1}] {2},", columnName, columnType, isNullable);
                        break;
                }
            }

            typeBuilder.Append(")").Replace(",)", ")");
            cmd.CommandText = typeBuilder.ToString();
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 生成 MySQL 批量插入 SQL（已弃用）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="dataList">实体列表</param>
        /// <returns>批量插入 SQL 语句</returns>
        [Obsolete("该方法存在 SQL 注入风险，请使用参数化查询方式", true)]
        public static string GetMySql<T>(List<T> dataList) where T : class, new()
        {
            var sql = new StringBuilder();
            var tableName = TableNameHelper.GetTableName<T>();
            var entityGetter = new Property.DynamicGet<T>();
            var properties = PropertyCache.GetNonIdentityProperties<T>();

            sql.AppendFormat("insert into {0}(", tableName);

            foreach (var prop in properties)
            {
                sql.AppendFormat("{0},", prop.Name);
            }
            sql.Append(")").Replace(",)", ")");

            foreach (var entity in dataList)
            {
                sql.Append("(");

                foreach (var prop in properties)
                {
                    var value = entityGetter.GetValue(entity, prop.Name, true);

                    if (value is bool boolValue)
                        sql.AppendFormat("{0},", boolValue ? 1 : 0);
                    else if (value == null)
                        sql.Append("NULL,");
                    else
                        sql.AppendFormat("'{0}',", value);
                }

                sql.Append("),").Replace(",)", ")");
            }

            var result = sql.ToStr();
            return result.Substring(0, result.Length - 1);
        }
    }
}
