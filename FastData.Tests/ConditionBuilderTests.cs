using System;
using System.Collections.Generic;
using System.Data.Common;
using FastData.Base;
using FastData.DbTypes;
using FastData.Model;
using Microsoft.Data.SqlClient;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 条件构建器单元测试
    /// </summary>
    public class ConditionBuilderTests
    {
        [Fact]
        public void TestConditionBuilder_Contains()
        {
            // Arrange
            var config = CreateConfig();

            // Act
            var sql = new ConditionBuilder(config)
                .In<ConditionBuilderTestUser>(x => x.Role, new[] { "admin", "user", "guest" })
                .Build(out var param);
            
            // Assert
            Assert.Contains("Role IN (@p0,@p1,@p2)", sql);
            Assert.Equal(3, param.Count);
        }

        [Fact]
        public void TestConditionBuilder_ContainsEmpty()
        {
            // Arrange
            var config = CreateConfig();

            // Act
            var sql = new ConditionBuilder(config)
                .In<ConditionBuilderTestUser>(x => x.Role, new string[0])
                .Build(out var param);
            
            // Assert
            Assert.Equal("1=0", sql);
            Assert.Empty(param);
        }

        [Fact]
        public void TestConditionBuilder_ContainsNull()
        {
            // Arrange
            var config = CreateConfig();

            // Act + Assert
            Assert.Throws<ArgumentNullException>(() => new ConditionBuilder(config)
                .In<ConditionBuilderTestUser>(x => x.Role, null));
        }

        private static ConfigModel CreateConfig()
        {
            DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);

            return new ConfigModel
            {
                DbType = DataDbType.SqlServer,
                Flag = "@",
                ProviderName = "Microsoft.Data.SqlClient"
            };
        }

        private class ConditionBuilderTestUser
        {
            public string Role { get; set; }
        }
    }
}
