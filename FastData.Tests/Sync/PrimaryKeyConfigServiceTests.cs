using FastData.Tooling.Sync;
using System.Collections.Generic;
using Xunit;

namespace FastData.Tests.Sync
{
    /// <summary>
    /// 主键配置服务测试
    /// 
    /// 测试表主键配置的添加、删除和查询功能。
    /// </summary>
    public class PrimaryKeyConfigServiceTests
    {
        /// <summary>
        /// 测试添加有效配置
        /// </summary>
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
            Assert.Single(result.PrimaryKeyColumns);
            Assert.Equal("order_id", result.PrimaryKeyColumns[0]);
            Assert.True(result.IsAutoIncrement);
        }

        /// <summary>
        /// 测试添加空配置不生效
        /// </summary>
        [Fact]
        public void AddTableConfig_NullConfig_DoesNotAdd()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(null);

            var all = service.GetAllConfigs();
            Assert.Empty(all);
        }

        /// <summary>
        /// 测试添加空表名配置不生效
        /// </summary>
        [Fact]
        public void AddTableConfig_EmptyTableName_DoesNotAdd()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(new TablePrimaryKeyConfig { TableName = "" });

            var all = service.GetAllConfigs();
            Assert.Empty(all);
        }

        /// <summary>
        /// 测试添加重复表名配置覆盖
        /// </summary>
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

        /// <summary>
        /// 测试删除现有表配置
        /// </summary>
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

        /// <summary>
        /// 测试删除不存在的表配置无影响
        /// </summary>
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

        /// <summary>
        /// 测试获取所有配置
        /// </summary>
        [Fact]
        public void GetAllConfigs_ReturnsAllConfigs()
        {
            var service = new PrimaryKeyConfigService();
            service.AddTableConfig(new TablePrimaryKeyConfig { TableName = "table1", PrimaryKeyColumns = new List<string> { "id" } });
            service.AddTableConfig(new TablePrimaryKeyConfig { TableName = "table2", PrimaryKeyColumns = new List<string> { "id" } });
            service.AddTableConfig(new TablePrimaryKeyConfig { TableName = "table3", PrimaryKeyColumns = new List<string> { "id" } });

            var all = service.GetAllConfigs();
            Assert.Equal(3, all.Count);
        }

        /// <summary>
        /// 测试获取不存在的表配置返回空
        /// </summary>
        [Fact]
        public void GetTableConfig_NonExistingTable_ReturnsNull()
        {
            var service = new PrimaryKeyConfigService();
            var result = service.GetTableConfig("nonexistent");
            Assert.Null(result);
        }

        /// <summary>
        /// 测试复合主键配置
        /// </summary>
        [Fact]
        public void AddTableConfig_CompositePrimaryKey_WorksCorrectly()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "order_items",
                PrimaryKeyColumns = new List<string> { "order_id", "item_id" },
                IsAutoIncrement = false
            };
            service.AddTableConfig(config);

            var result = service.GetTableConfig("order_items");
            Assert.NotNull(result);
            Assert.Equal(2, result.PrimaryKeyColumns.Count);
            Assert.Contains("order_id", result.PrimaryKeyColumns);
            Assert.Contains("item_id", result.PrimaryKeyColumns);
            Assert.False(result.IsAutoIncrement);
        }

        /// <summary>
        /// 测试自增列配置
        /// </summary>
        [Fact]
        public void AddTableConfig_AutoIncrementColumn_WorksCorrectly()
        {
            var service = new PrimaryKeyConfigService();
            var config = new TablePrimaryKeyConfig
            {
                TableName = "users",
                PrimaryKeyColumns = new List<string> { "user_id" },
                IsAutoIncrement = true,
                IncrementalColumn = "user_id"
            };
            service.AddTableConfig(config);

            var result = service.GetTableConfig("users");
            Assert.NotNull(result);
            Assert.True(result.IsAutoIncrement);
            Assert.Equal("user_id", result.IncrementalColumn);
        }
    }
}
