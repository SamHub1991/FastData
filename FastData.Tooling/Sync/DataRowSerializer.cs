using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Script.Serialization;

namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 数据行 JSON 序列化工具
    /// 用于失败记录的持久化存储
    /// </summary>
    public static class DataRowSerializer
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        /// <summary>
        /// 将 DataRow 序列化为 JSON 字符串
        /// </summary>
        /// <param name="table">数据表结构</param>
        /// <param name="row">数据行</param>
        /// <returns>JSON 字符串</returns>
        public static string Serialize(DataTable table, DataRow row)
        {
            var data = new Dictionary<string, object>();

            // 添加表结构信息
            var columns = new List<Dictionary<string, object>>();
            foreach (DataColumn col in table.Columns)
            {
                columns.Add(new Dictionary<string, object>
                {
                    { "ColumnName", col.ColumnName },
                    { "DataType", col.DataType.FullName },
                    { "IsNullable", col.AllowDBNull }
                });
            }
            data["Columns"] = columns;

            // 添加行数据
            var values = new Dictionary<string, object>();
            foreach (DataColumn col in table.Columns)
            {
                var value = row[col];
                values[col.ColumnName] = Convert.IsDBNull(value) ? null : value;
            }
            data["Values"] = values;

            return Serializer.Serialize(data);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化为 DataTable
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>DataTable</returns>
        public static DataTable Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new DataTable();

            var data = Serializer.Deserialize<Dictionary<string, object>>(json);
            if (data == null)
                return new DataTable();

            var table = new DataTable();

            // 重建列
            if (data.ContainsKey("Columns"))
            {
                var columns = data["Columns"] as IEnumerable<object>;
                if (columns != null)
                {
                    foreach (var colObj in columns)
                    {
                        var colDict = colObj as Dictionary<string, object>;
                        if (colDict != null && colDict.ContainsKey("ColumnName") && colDict.ContainsKey("DataType"))
                        {
                            var columnName = colDict["ColumnName"].ToString();
                            var dataTypeName = colDict["DataType"].ToString();
                            var type = Type.GetType(dataTypeName) ?? typeof(string);
                            table.Columns.Add(columnName, type);
                        }
                    }
                }
            }

            // 重建数据行
            if (data.ContainsKey("Values"))
            {
                var values = data["Values"] as Dictionary<string, object>;
                if (values != null)
                {
                    var row = table.NewRow();
                    foreach (var kvp in values)
                    {
                        if (table.Columns.Contains(kvp.Key))
                        {
                            row[kvp.Key] = kvp.Value ?? DBNull.Value;
                        }
                    }
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        /// <summary>
        /// 将 DataTable 批量序列化为 JSON 数组
        /// </summary>
        /// <param name="table">数据表</param>
        /// <returns>JSON 数组字符串</returns>
        public static string SerializeBatch(DataTable table)
        {
            var rows = new List<Dictionary<string, object>>();

            foreach (DataRow row in table.Rows)
            {
                var rowData = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    var value = row[col];
                    rowData[col.ColumnName] = Convert.IsDBNull(value) ? null : value;
                }
                rows.Add(rowData);
            }

            return Serializer.Serialize(rows);
        }

        /// <summary>
        /// 从 JSON 数组反序列化为 DataTable
        /// </summary>
        /// <param name="json">JSON 字符串</param>
        /// <returns>DataTable</returns>
        public static DataTable DeserializeBatch(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new DataTable();

            var rows = Serializer.Deserialize<List<Dictionary<string, object>>>(json);
            if (rows == null || rows.Count == 0)
                return new DataTable();

            var table = new DataTable();

            // 使用第一行的键创建列
            foreach (var kvp in rows.First())
            {
                table.Columns.Add(kvp.Key, typeof(object));
            }

            // 添加数据行
            foreach (var rowData in rows)
            {
                var row = table.NewRow();
                foreach (var kvp in rowData)
                {
                    row[kvp.Key] = kvp.Value ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }

            return table;
        }
    }
}
