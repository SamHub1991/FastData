using FastData.Property;
using FastUntility.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace FastData.Base
{
    /// <summary>
    /// 标签：2015.9.6，魏中针
    /// 说明：Command操作类
    /// </summary>
    internal static class CommandParam
    {
        #region 获取列类型
        /// <summary>
        /// 获取列类型
        /// </summary>
        /// <param name="list"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public static DbType GetOracleDbType(string type)
        {
            switch (type.ToLower())
            {
                case "string":
                    return DbType.String;
                case "datetime":
                    return DbType.DateTime;
                case "decimal":
                    return DbType.Decimal;
                case "int32":
                    return DbType.Decimal;
                case "int64":
                    return DbType.Decimal;
                case "byte[]":
                    return DbType.Byte;
                case "float":
                    return DbType.Double;
                case "double":
                    return DbType.Double;
                default:
                    return DbType.Object;
            }
        }
        #endregion

        #region tvsps sql
        /// <summary>
        /// tvsps sql
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dyn"></param>
        /// <returns></returns>
        public static string GetTvps<T>()
        {
            var sql1 = new StringBuilder();
            var sql2 = new StringBuilder();

            // 获取属性信息，排除 Identity 列
            var properties = PropertyCache.GetPropertyInfo<T>();
            var entityType = typeof(T);
            var nonIdentityProperties = properties.Where(p => 
            {
                var propInfo = entityType.GetProperty(p.Name);
                if (propInfo == null) return true;
                var columnAttr = propInfo.GetCustomAttributes(typeof(ColumnAttribute), false)
                    .FirstOrDefault() as ColumnAttribute;
                return columnAttr == null || !columnAttr.IsIdentity;
            }).ToList();

            sql1.AppendFormat("insert into {0} (", TableNameHelper.GetTableName<T>());
            sql2.Append("select ");
            nonIdentityProperties.ForEach(a => {
                sql1.AppendFormat("{0},", a.Name);
                sql2.AppendFormat("tb.{0},", a.Name);
            });
            sql1.Append(")");
            sql2.AppendFormat("from @{0}_TVP as tb", typeof(T).Name);

            return string.Format("{0}{1}", sql1.ToString().Replace(",)", ") "), sql2.ToString().Replace(",from", " from"));
        }
        #endregion

        #region 获取datatabel
        /// <summary>
        /// 获取datatabel
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="db"></param>
        /// <returns></returns>
        public static DataTable GetTable<T>(DbCommand cmd, List<T> list)
        {
            var dyn = new Property.DynamicGet<T>();
            var dt = new DataTable();
            
            // 获取属性信息，排除 Identity 列
            var properties = PropertyCache.GetPropertyInfo<T>();
            var entityType = typeof(T);
            var nonIdentityProperties = properties.Where(p => 
            {
                var propInfo = entityType.GetProperty(p.Name);
                if (propInfo == null) return true;
                var columnAttr = propInfo.GetCustomAttributes(typeof(ColumnAttribute), false)
                    .FirstOrDefault() as ColumnAttribute;
                return columnAttr == null || !columnAttr.IsIdentity;
            }).ToList();
            
            // 创建 DataTable 结构
            foreach (var prop in nonIdentityProperties)
            {
                dt.Columns.Add(prop.Name, prop.PropertyType);
            }
            
            // 填充数据
            list.ForEach(p => {
                var row = dt.NewRow();
                nonIdentityProperties.ForEach(a => { row[a.Name] = dyn.GetValue(p, a.Name, true) ?? DBNull.Value; });
                dt.Rows.Add(row);
            });
            return dt;
        }
        #endregion

        #region tvps
        /// <summary>
        /// tvps
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd"></param>
        public static void InitTvps<T>(DbCommand cmd)
        {
            var sql = new StringBuilder();
            // 查询表结构，排除 Identity 列
            cmd.CommandText = string.Format("select a.name,(select top 1 name from sys.systypes c where a.xtype=c.xtype) as type,length,isnullable,prec,scale,(a.status & 0x80) as is_identity from syscolumns a where a.id=object_id('{0}') order by a.colid asc", TableNameHelper.GetTableName<T>());
            var dr = cmd.ExecuteReader();
            var dic = BaseJson.DataReaderToDic(dr);
            dr.Close();

            // 过滤掉 Identity 列
            var nonIdentityColumns = dic.Where(item => item.GetValue("is_identity").ToStr() != "128").ToList();

            sql.AppendFormat("if not exists(SELECT 1 FROM sys.table_types where name='{0}_TVP')", typeof(T).Name);
            sql.AppendFormat("CREATE TYPE {0}_TVP AS TABLE(", typeof(T).Name);

            foreach (var item in nonIdentityColumns)
            {
                switch (item.GetValue("type").ToStr())
                {
                    case "char":
                    case "nchar":
                    case "varchar":
                    case "nvarchar":
                    case "varchar2":
                    case "nvarchar2":
                        sql.AppendFormat("[{0}] [{1}]({2}) {3},", item.GetValue("name"), item.GetValue("type"), item.GetValue("length"), item.GetValue("isnullable").ToStr() == "1" ? "NULL" : "NOT NULL");
                        break;
                    case "decimal":
                    case "numeric":
                    case "number":
                        if (item.GetValue("prec").ToStr() == "0" && item.GetValue("scale").ToStr() == "0")
                            sql.AppendFormat("[{0}] [{1}] {2},", item.GetValue("name"), item.GetValue("type"), item.GetValue("isnullable").ToStr() == "1" ? "NULL" : "NOT NULL");
                        else
                            sql.AppendFormat("[{0}] [{1}]({2},{3}) {4},", item.GetValue("name"), item.GetValue("type"), item.GetValue("prec"), item.GetValue("scale"), item.GetValue("isnullable").ToStr() == "1" ? "NULL" : "NOT NULL");
                        break;
                    default:
                        sql.AppendFormat("[{0}] [{1}] {2},", item.GetValue("name"), item.GetValue("type"), item.GetValue("isnullable").ToStr() == "1" ? "NULL" : "NOT NULL");
                        break;
                }
            }

            sql.Append(")").Replace(",)", ")");
            cmd.CommandText = sql.ToString();
            cmd.ExecuteNonQuery();
        }
        #endregion

        #region mysql 
        /// <summary>
        /// mysql
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string GetMySql<T>(List<T> list)
        {
            var sql = new StringBuilder();
            sql.AppendFormat("insert into {0}(", TableNameHelper.GetTableName<T>());
            var dyn = new Property.DynamicGet<T>();
            PropertyCache.GetPropertyInfo<T>().ForEach(a => { sql.AppendFormat("{0},", a.Name); });
            sql.Append(")").Replace(",)", ")");
            list.ForEach(a => {
                sql.Append("(");
                PropertyCache.GetPropertyInfo<T>().ForEach(p => {
                    var value = dyn.GetValue(a, p.Name, true);
                    // 处理布尔值，MySQL 使用 0/1
                    if (value is bool boolVal)
                        sql.AppendFormat("{0},", boolVal ? 1 : 0);
                    else if (value == null)
                        sql.Append("NULL,");
                    else
                        sql.AppendFormat("'{0}',", value);
                });
                sql.Append("),").Replace(",)", ")");
            });

            return sql.ToStr().Substring(0, sql.ToStr().Length - 1);
        }
        #endregion
    }
}
