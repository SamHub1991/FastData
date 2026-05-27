using FastData.Tooling.Sync;
using System.Collections.Generic;
using Xunit;

namespace FastData.Tests.Sync
{
    public class PrimaryKeyConfigServiceTests
    {
        [Fact]
        public void AddTableConfig_ValidConfig_AddsSuccessfully()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "orders",
                PrimaryKeyColumns = new List<string> { "order_id" },
                IsAutoIncrement = true,
                IncrementalColumn = "order_id"
            };
            service.AddTableConfig(config);

            var result = service.GetTableConfig("orders");
            Assert.NotNull(result);
            Assert.Equal("orders", result.TableName);
            Assert.Equal(1, result.PrimaryKeyColumns.Count);
            Assert.Equal("order_id", result.PrimaryKeyColumns[0]);
            Assert.True(result.IsAutoIncrement);
        }

        [Fact]
        public void AddTableConfig_NullConfig_DoesNotAdd()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(null);

            var all = service.GetAllConfigs();
            Assert.Equal(0, all.Count);
        }

        [Fact]
        public void AddTableConfig_EmptyTableName_DoesNotAdd()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(new TablePrimaryKeyConfig { TableName = "" });

            var all = service.GetAllConfigs();
            Assert.Equal(0, all.Count);
        }

        [Fact]
        public void AddTableConfig_DuplicateTableName_Overwrites()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(new TablePrimaryKeyConfig
            {
                TableName = "users",
                PrimaryKeyColumns = new List<string> { "id" },
                IsAutoIncrement = true
            });
            service.AddTableConfig(new TablePrimaryKeyConfig
            {
                TableName = "users",
                PrimaryKeyColumns = new List<string> { "user_id", "dept_id" },
                IsAutoIncrement = false
            });

            var result = service.GetTableConfig("users");
            Assert.Equal(2, result.PrimaryKeyColumns.Count);
            Assert.False(result.IsAutoIncrement);
        }

        [Fact]
        public void RemoveTableConfig_ExistingTable_RemovesSuccessfully()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(new TablePrimaryKeyConfig
            {
                TableName = "products",
                PrimaryKeyColumns = new List<string> { "id" }
            });
            service.RemoveTableConfig("products");

            var result = service.GetTableConfig("products");
            Assert.Null(result);
        }

        [Fact]
        public void RemoveTableConfig_NonExistingTable_NoEffect()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(new TablePrimaryKeyConfig
            {
                TableName = "products",
                PrimaryKeyColumns = new List<string> { "id" }
            });
            service.RemoveTableConfig("nonexistent");

            Assert.NotNull(service.GetTableConfig("products"));
        }

        [Fact]
        public void RemoveTableConfig_NullTableName_NoEffect()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(new TablePrimaryKeyConfig
            {
                TableName = "products",
                PrimaryKeyColumns = new List<string> { "id" }
            });
            service.RemoveTableConfig(null);

            Assert.NotNull(service.GetTableConfig("products"));
        }

        [Fact]
        public void GetTableConfig_UnknownTable_ReturnsNull()
        {
            var service = new PrimaryKeyConfigService();
            var result = service.GetTableConfig("unknown");
            Assert.Null(result);
        }

        [Fact]
        public void GetTableConfig_NullTableName_ReturnsNull()
        {
            var service = new PrimaryKeyConfigService();
            var result = service.GetTableConfig(null);
            Assert.Null(result);
        }

        [Fact]
        public void GetAllConfigs_ReturnsAllAddedConfigs()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(new TablePrimaryKeyConfig { TableName = "t1", PrimaryKeyColumns = new List<string> { "id" } });
            service.AddTableConfig(new TablePrimaryKeyConfig { TableName = "t2", PrimaryKeyColumns = new List<string> { "id" } });
            service.AddTableConfig(new TablePrimaryKeyConfig { TableName = "t3", PrimaryKeyColumns = new List<string> { "id" } });

            var all = service.GetAllConfigs();
            Assert.Equal(3, all.Count);
        }

        [Fact]
        public void BuildPrimaryKeyWhereClause_SingleKey_ReturnsCorrectClause()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "orders",
                PrimaryKeyColumns = new List<string> { "order_id" }
            };
            var result = service.BuildPrimaryKeyWhereClause(config, null);
            Assert.Equal("order_id = @pk0", result);
        }

        [Fact]
        public void BuildPrimaryKeyWhereClause_CompositeKey_ReturnsAndClause()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "order_items",
                PrimaryKeyColumns = new List<string> { "order_id", "item_id" }
            };
            var result = service.BuildPrimaryKeyWhereClause(config, null);
            Assert.Equal("order_id = @pk0 AND item_id = @pk1", result);
        }

        [Fact]
        public void BuildPrimaryKeyWhereClause_CustomParamNames_ReturnsCustomClause()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "orders",
                PrimaryKeyColumns = new List<string> { "order_id", "item_seq" }
            };
            var paramNames = new List<string> { "@oid", "@seq" };
            var result = service.BuildPrimaryKeyWhereClause(config, paramNames);
            Assert.Equal("order_id = @oid AND item_seq = @seq", result);
        }

        [Fact]
        public void BuildPrimaryKeyWhereClause_NullConfig_ReturnsFallback()
        {
            var service = new PrimaryKeyConfigService();
            var result = service.BuildPrimaryKeyWhereClause(null, null);
            Assert.Equal("1=1", result);
        }

        [Fact]
        public void BuildPrimaryKeyWhereClause_EmptyColumns_ReturnsFallback()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "orders",
                PrimaryKeyColumns = new List<string>()
            };
            var result = service.BuildPrimaryKeyWhereClause(config, null);
            Assert.Equal("1=1", result);
        }

        [Fact]
        public void BuildIncrementalWhereClause_AutoIncrementSingleKey_ReturnsSimpleClause()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "orders",
                PrimaryKeyColumns = new List<string> { "id" },
                IsAutoIncrement = true
            };
            var result = service.BuildIncrementalWhereClause(config, 100);
            Assert.Equal("id > @lastValue", result);
        }

        [Fact]
        public void BuildIncrementalWhereClause_CompositeKey_ReturnsOrClause()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "order_items",
                PrimaryKeyColumns = new List<string> { "order_id", "item_id" },
                IsAutoIncrement = false
            };
            var result = service.BuildIncrementalWhereClause(config, 0);
            Assert.Equal("order_id > @lastValue0 OR item_id IS NOT NULL", result);
        }

        [Fact]
        public void BuildIncrementalWhereClause_NullConfig_ReturnsFallback()
        {
            var service = new PrimaryKeyConfigService();
            var result = service.BuildIncrementalWhereClause(null, 0);
            Assert.Equal("1=1", result);
        }

        [Fact]
        public void BuildIncrementalWhereClause_EmptyColumns_ReturnsFallback()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "orders",
                PrimaryKeyColumns = new List<string>()
            };
            var result = service.BuildIncrementalWhereClause(config, 0);
            Assert.Equal("1=1", result);
        }

        [Fact]
        public void ExportToSql_GeneratesCreateTableAndInserts()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(new TablePrimaryKeyConfig
            {
                TableName = "users",
                PrimaryKeyColumns = new List<string> { "id" },
                IsAutoIncrement = true,
                IncrementalColumn = "id"
            });
            service.AddTableConfig(new TablePrimaryKeyConfig
            {
                TableName = "orders",
                PrimaryKeyColumns = new List<string> { "order_id", "dept_id" },
                IsAutoIncrement = false,
                IncrementalColumn = "update_time"
            });

            var sql = service.ExportToSql();
            Assert.True(sql.Contains("fd_table_pk_config"));
            Assert.True(sql.Contains("users"));
            Assert.True(sql.Contains("orders"));
            Assert.True(sql.Contains("id,dept_id"));
            Assert.True(sql.Contains("update_time"));
        }
    }
}
