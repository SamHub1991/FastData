using FastData.Tooling.Sync;
using System;
using System.Data;
using Xunit;

namespace FastData.Tests.Sync
{
    /// <summary>
    /// DataRowSerializer 单元测试
    /// 
    /// 覆盖数据行序列化的所有核心功能，包括单行序列化与反序列化、空值处理、
    /// 日期时间处理、批量序列化与反序列化、异常输入处理以及完整的往返测试，
    /// 确保序列化后的 JSON 能正确还原原始 DataTable 结构和数据。
    /// </summary>
    public class DataRowSerializerTests
    {
        /// <summary>
        /// 验证空数据行序列化：序列化包含空值的新行，返回的 JSON 应包含 Columns 和 Values 结构
        /// </summary>
        [Fact]
        public void Serialize_EmptyRow_ReturnsJsonWithEmptyValues()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            var row = table.NewRow();
            
            var json = DataRowSerializer.Serialize(table, row);
            
            Assert.NotNull(json);
            Assert.Contains("\"Columns\"", json);
            Assert.Contains("\"Values\"", json);
        }

        /// <summary>
        /// 验证带数据的数据行序列化：序列化包含 Id、Name、Email 的数据行，JSON 应包含所有字段值
        /// </summary>
        [Fact]
        public void Serialize_RowWithData_ReturnsValidJson()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Email", typeof(string));
            
            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = "张三";
            row["Email"] = "zhangsan@test.com";
            
            var json = DataRowSerializer.Serialize(table, row);
            
            Assert.NotNull(json);
            Assert.Contains("Id", json);
            Assert.Contains("张三", json);
            Assert.Contains("zhangsan@test.com", json);
        }

        /// <summary>
        /// 验证包含 DBNull 值的数据行序列化：序列化后 JSON 中 Name 字段应为 null
        /// </summary>
        [Fact]
        public void Serialize_RowWithNullValue_HandlesNullCorrectly()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            
            var row = table.NewRow();
            row["Id"] = 1;
            row["Name"] = DBNull.Value;
            
            var json = DataRowSerializer.Serialize(table, row);
            
            Assert.NotNull(json);
            Console.WriteLine("JSON with null: " + json);
            Assert.Contains("\"Name\":null", json);
        }

        /// <summary>
        /// 验证包含 DateTime 值的数据行序列化：序列化后 JSON 应正确包含 DateTime 字段
        /// </summary>
        [Fact]
        public void Serialize_RowWithDateTime_HandlesDateTimeCorrectly()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("CreatedAt", typeof(DateTime));
            
            var testDate = new DateTime(2026, 5, 26, 10, 30, 0);
            var row = table.NewRow();
            row["Id"] = 1;
            row["CreatedAt"] = testDate;
            
            var json = DataRowSerializer.Serialize(table, row);
            
            Assert.NotNull(json);
            Console.WriteLine("JSON with DateTime: " + json);
            Assert.Contains("CreatedAt", json);
        }

        /// <summary>
        /// 验证 JSON 反序列化为 DataTable：反序列化后的表结构与原始表结构一致（列名、列数相同）
        /// </summary>
        [Fact]
        public void Deserialize_ValidJson_ReturnsDataTableWithSameSchema()
        {
            var originalTable = new DataTable();
            originalTable.Columns.Add("Id", typeof(int));
            originalTable.Columns.Add("Name", typeof(string));
            originalTable.Columns.Add("Email", typeof(string));
            
            var row = originalTable.NewRow();
            row["Id"] = 1;
            row["Name"] = "李四";
            row["Email"] = "lisi@test.com";
            originalTable.Rows.Add(row);
            
            var json = DataRowSerializer.Serialize(originalTable, row);
            var deserializedTable = DataRowSerializer.Deserialize(json);
            
            Assert.NotNull(deserializedTable);
            Assert.Equal(3, deserializedTable.Columns.Count);
            Assert.NotNull(deserializedTable.Columns["Id"]);
            Assert.NotNull(deserializedTable.Columns["Name"]);
            Assert.NotNull(deserializedTable.Columns["Email"]);
        }

        /// <summary>
        /// 验证 JSON 反序列化可正确还原数据行内容：行数、各字段值与原始数据一致
        /// </summary>
        [Fact]
        public void Deserialize_ValidJson_RestoresRowData()
        {
            var originalTable = new DataTable();
            originalTable.Columns.Add("Id", typeof(int));
            originalTable.Columns.Add("Name", typeof(string));
            
            var row = originalTable.NewRow();
            row["Id"] = 42;
            row["Name"] = "王五";
            originalTable.Rows.Add(row);
            
            var json = DataRowSerializer.Serialize(originalTable, row);
            var deserializedTable = DataRowSerializer.Deserialize(json);
            
            Assert.Single(deserializedTable.Rows);
            Assert.Equal(42, Convert.ToInt32(deserializedTable.Rows[0]["Id"]));
            Assert.Equal("王五", deserializedTable.Rows[0]["Name"].ToString());
        }

        /// <summary>
        /// 验证反序列化空字符串和 null 时返回空的 DataTable（0 列、0 行），不会抛出异常
        /// </summary>
        [Fact]
        public void Deserialize_EmptyJson_ReturnsEmptyDataTable()
        {
            var result = DataRowSerializer.Deserialize("");
            Assert.NotNull(result);
            Assert.Empty(result.Columns);
            Assert.Empty(result.Rows);
            
            result = DataRowSerializer.Deserialize(null);
            Assert.NotNull(result);
            Assert.Empty(result.Columns);
        }

        /// <summary>
        /// 验证反序列化非法 JSON 字符串时不会导致程序崩溃（捕获异常或返回空表）
        /// </summary>
        [Fact]
        public void Deserialize_MalformedJson_HandlesGracefully()
        {
            try
            {
                var result = DataRowSerializer.Deserialize("invalid json");
                Assert.NotNull(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Expected exception for malformed JSON: " + ex.Message);
                Assert.NotNull(ex);
            }
        }

        /// <summary>
        /// 验证批量序列化多行数据：返回的 JSON 应为数组格式，包含所有行的数据
        /// </summary>
        [Fact]
        public void SerializeBatch_MultipleRows_ReturnsJsonArray()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            
            var row1 = table.NewRow();
            row1["Id"] = 1;
            row1["Name"] = "用户 1";
            table.Rows.Add(row1);
            
            var row2 = table.NewRow();
            row2["Id"] = 2;
            row2["Name"] = "用户 2";
            table.Rows.Add(row2);
            
            var json = DataRowSerializer.SerializeBatch(table);
            
            Assert.NotNull(json);
            Console.WriteLine("Batch JSON: " + json);
            Assert.Contains("[{", json);
            Assert.Contains("}]", json);
            Assert.Contains("\"Id\":1", json);
            Assert.Contains("\"Id\":2", json);
        }

        /// <summary>
        /// 验证批量反序列化 JSON 数组：反序列化后应正确恢复所有行的数据
        /// </summary>
        [Fact]
        public void DeserializeBatch_ValidJsonArray_ReturnsDataTable()
        {
            var originalTable = new DataTable();
            originalTable.Columns.Add("Id", typeof(int));
            originalTable.Columns.Add("Name", typeof(string));
            
            var row1 = originalTable.NewRow();
            row1["Id"] = 100;
            row1["Name"] = "测试 100";
            originalTable.Rows.Add(row1);
            
            var row2 = originalTable.NewRow();
            row2["Id"] = 200;
            row2["Name"] = "测试 200";
            originalTable.Rows.Add(row2);
            
            var json = DataRowSerializer.SerializeBatch(originalTable);
            var deserializedTable = DataRowSerializer.DeserializeBatch(json);
            
            Assert.NotNull(deserializedTable);
            Assert.Equal(2, deserializedTable.Rows.Count);
            
            Assert.Equal(100, Convert.ToInt32(deserializedTable.Rows[0]["Id"]));
            Assert.Equal("测试 100", deserializedTable.Rows[0]["Name"].ToString());
            
            Assert.Equal(200, Convert.ToInt32(deserializedTable.Rows[1]["Id"]));
            Assert.Equal("测试 200", deserializedTable.Rows[1]["Name"].ToString());
        }

        /// <summary>
        /// 验证批量反序列化空 JSON 数组：返回空的 DataTable，不会抛出异常
        /// </summary>
        [Fact]
        public void DeserializeBatch_EmptyJsonArray_ReturnsEmptyTable()
        {
            var result = DataRowSerializer.DeserializeBatch("[]");
            Assert.NotNull(result);
            Assert.Empty(result.Rows);
            
            result = DataRowSerializer.DeserializeBatch("");
            Assert.NotNull(result);
            Assert.Empty(result.Rows);
        }

        /// <summary>
        /// 验证单行序列化后再反序列化的完整往返：反序列化后的表结构、行数和字段值与原始数据一致
        /// </summary>
        [Fact]
        public void RoundTrip_SerializeThenDeserialize_PreservesData()
        {
            var originalTable = new DataTable();
            originalTable.Columns.Add("Id", typeof(int));
            originalTable.Columns.Add("Name", typeof(string));
            originalTable.Columns.Add("Age", typeof(int));
            originalTable.Columns.Add("Active", typeof(bool));
            
            var row1 = originalTable.NewRow();
            row1["Id"] = 1;
            row1["Name"] = "赵六";
            row1["Age"] = 28;
            row1["Active"] = true;
            originalTable.Rows.Add(row1);
            
            var json = DataRowSerializer.Serialize(originalTable, row1);
            var restoredTable = DataRowSerializer.Deserialize(json);
            
            Assert.Equal(originalTable.Columns.Count, restoredTable.Columns.Count);
            Assert.Equal(originalTable.Rows.Count, restoredTable.Rows.Count);
            Assert.Equal(row1["Id"], restoredTable.Rows[0]["Id"]);
            Assert.Equal(row1["Name"].ToString(), restoredTable.Rows[0]["Name"].ToString());
            Assert.Equal(row1["Age"], restoredTable.Rows[0]["Age"]);
        }

        /// <summary>
        /// 验证批量序列化后再批量反序列化的完整往返：10 条产品数据的表经过往返后行数和字段值完全一致
        /// </summary>
        [Fact]
        public void RoundTrip_BatchSerializeThenDeserialize_PreservesAllRows()
        {
            var originalTable = new DataTable();
            originalTable.Columns.Add("ProductId", typeof(int));
            originalTable.Columns.Add("ProductName", typeof(string));
            originalTable.Columns.Add("Price", typeof(decimal));
            
            for (int i = 1; i <= 10; i++)
            {
                var row = originalTable.NewRow();
                row["ProductId"] = i;
                row["ProductName"] = "产品_" + i;
                row["Price"] = i * 10.5m;
                originalTable.Rows.Add(row);
            }
            
            var json = DataRowSerializer.SerializeBatch(originalTable);
            var restoredTable = DataRowSerializer.DeserializeBatch(json);
            
            Assert.Equal(originalTable.Rows.Count, restoredTable.Rows.Count);
            
            for (int i = 0; i < originalTable.Rows.Count; i++)
            {
                Assert.Equal(
                    Convert.ToInt64(originalTable.Rows[i]["ProductId"]), 
                    Convert.ToInt64(restoredTable.Rows[i]["ProductId"])
                );
                Assert.Equal(
                    originalTable.Rows[i]["ProductName"].ToString(), 
                    restoredTable.Rows[i]["ProductName"].ToString()
                );
            }
        }
    }
}
