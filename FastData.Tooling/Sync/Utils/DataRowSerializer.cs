using System;
using System.Collections.Generic;
using System.Data;

#if NETFRAMEWORK
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 数据行 JSON 序列化工具
    /// 用于失败记录的持久化存储
    /// </summary>
    public static class DataRowSerializer
    {
#if NETFRAMEWORK
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();
#endif

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

#if NETFRAMEWORK
            return Serializer.Serialize(data);
#else
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
#endif
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

            var table = new DataTable();

#if NETFRAMEWORK
            var data = Serializer.Deserialize<Dictionary<string, object>>(json);
            if (data == null)
                return table;

            if (data.ContainsKey("Columns"))
            {
                var columns = data["Columns"] as System.Collections.IEnumerable;
                if (columns != null)
                {
                    foreach (var colObj in columns)
                    {
                        var colDict = colObj as IDictionary<string, object>;
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

            if (data.ContainsKey("Values"))
            {
                var values = data["Values"] as IDictionary<string, object>;
                if (values != null && table.Columns.Count > 0)
                {
                    var row = table.NewRow();
                    foreach (var kvp in values)
                    {
                        row[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                    table.Rows.Add(row);
                }
            }
#else
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;

                if (root.TryGetProperty("Columns", out var columnsElement))
                {
                    foreach (var colElement in columnsElement.EnumerateArray())
                    {
                        if (colElement.TryGetProperty("ColumnName", out var nameElement) &&
                            colElement.TryGetProperty("DataType", out var typeElement))
                        {
                            var columnName = nameElement.GetString();
                            var dataTypeName = typeElement.GetString();
                            var type = Type.GetType(dataTypeName) ?? typeof(string);
                            table.Columns.Add(columnName, type);
                        }
                    }
                }

                if (root.TryGetProperty("Values", out var valuesElement) && table.Columns.Count > 0)
                {
                    var row = table.NewRow();
                    foreach (var prop in valuesElement.EnumerateObject())
                    {
                        row[prop.Name] = ConvertJsonValue(prop.Value);
                    }
                    table.Rows.Add(row);
                }
            }
#endif

            return table;
        }

#if !NETFRAMEWORK
        private static object ConvertJsonValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return DBNull.Value;
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                default:
                    return element.GetString();
            }
        }
#endif

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

#if NETFRAMEWORK
            return Serializer.Serialize(rows);
#else
            return JsonSerializer.Serialize(rows, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
#endif
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

            var table = new DataTable();

#if NETFRAMEWORK
            var rows = Serializer.Deserialize<object[]>(json);
            if (rows == null || rows.Length == 0)
                return table;

            var firstRow = rows[0] as IDictionary<string, object>;
            if (firstRow == null)
                return table;

            foreach (var kvp in firstRow)
            {
                table.Columns.Add(kvp.Key, typeof(object));
            }

            foreach (var rowData in rows)
            {
                var rowDict = rowData as IDictionary<string, object>;
                if (rowDict != null)
                {
                    var row = table.NewRow();
                    foreach (var kvp in rowDict)
                    {
                        row[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                    table.Rows.Add(row);
                }
            }
#else
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                if (root.GetArrayLength() == 0)
                    return table;

                var firstRow = root[0];
                foreach (var prop in firstRow.EnumerateObject())
                {
                    table.Columns.Add(prop.Name, typeof(object));
                }

                    foreach (var prop in root.EnumerateArray())
                    {
                        var row = table.NewRow();
                        foreach (var p in prop.EnumerateObject())
                        {
                            row[p.Name] = ConvertJsonValue(p.Value);
                        }
                        table.Rows.Add(row);
                    }
            }
#endif

            return table;
        }
    }
}
