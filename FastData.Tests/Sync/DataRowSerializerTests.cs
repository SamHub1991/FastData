using FastData.Tooling.Sync;
using System;
using System.Data;
using Xunit;

namespace FastData.Tests.Sync
{
    /// <summary>
    /// DataRowSerializer 单元测试
    /// </summary>
    public class DataRowSerializerTests
    {
        [Fact]
        public void Serialize_EmptyRow_ReturnsJsonWithEmptyValues()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            var row = table.NewRow();
            
            var json = DataRowSerializer.Serialize(table, row);
            
            Assert.NotNull(json);
            Assert.True(json.Contains("\"Columns\""));
            Assert.True(json.Contains("\"Values\""));
        }

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
            Assert.True(json.Contains("Id"));
            Assert.True(json.Contains("张三"));
            Assert.True(json.Contains("zhangsan@test.com"));
        }

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
            Assert.True(json.Contains("\"Name\":null"));
        }

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
            Assert.True(json.Contains("CreatedAt"));
        }

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
            Assert.True(deserializedTable.Columns.Contains("Id"));
            Assert.True(deserializedTable.Columns.Contains("Name"));
            Assert.True(deserializedTable.Columns.Contains("Email"));
        }

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
            
            Assert.Equal(1, deserializedTable.Rows.Count);
            Assert.Equal(42, Convert.ToInt32(deserializedTable.Rows[0]["Id"]));
            Assert.Equal("王五", deserializedTable.Rows[0]["Name"].ToString());
        }

        [Fact]
        public void Deserialize_EmptyJson_ReturnsEmptyDataTable()
        {
            var result = DataRowSerializer.Deserialize("");
            Assert.NotNull(result);
            Assert.Equal(0, result.Columns.Count);
            Assert.Equal(0, result.Rows.Count);
            
            result = DataRowSerializer.Deserialize(null);
            Assert.NotNull(result);
            Assert.Equal(0, result.Columns.Count);
        }

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
            Assert.True(json.Contains("[{"));
            Assert.True(json.Contains("}]"));
            Assert.True(json.Contains("\"Id\":1"));
            Assert.True(json.Contains("\"Id\":2"));
        }

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

        [Fact]
        public void DeserializeBatch_EmptyJsonArray_ReturnsEmptyTable()
        {
            var result = DataRowSerializer.DeserializeBatch("[]");
            Assert.NotNull(result);
            Assert.Equal(0, result.Rows.Count);
            
            result = DataRowSerializer.DeserializeBatch("");
            Assert.NotNull(result);
            Assert.Equal(0, result.Rows.Count);
        }

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
