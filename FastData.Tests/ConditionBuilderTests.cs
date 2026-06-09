using System;
using System.Collections.Generic;
using FastData.Model;
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
            var values = new List<string> { "admin", "user", "guest" };
            var param = new List<DbParameter>();
            var dbType = DataDbType.SqlServer;
            
            // Act
            var sql = ConditionBuilder.BuildInSql("@role", values, param, dbType);
            
            // Assert
            Assert.Contains("@role_p0", sql);
            Assert.Contains("@role_p1", sql);
            Assert.Contains("@role_p2", sql);
            Assert.Equal(3, param.Count);
        }

        [Fact]
        public void TestConditionBuilder_ContainsEmpty()
        {
            // Arrange
            var values = new List<string>();
            var param = new List<DbParameter>();
            var dbType = DataDbType.SqlServer;
            
            // Act
            var sql = ConditionBuilder.BuildInSql("@role", values, param, dbType);
            
            // Assert
            Assert.Null(sql);
        }

        [Fact]
        public void TestConditionBuilder_ContainsNull()
        {
            // Arrange
            List<string> values = null;
            var param = new List<DbParameter>();
            var dbType = DataDbType.SqlServer;
            
            // Act
            var sql = ConditionBuilder.BuildInSql("@role", values, param, dbType);
            
            // Assert
            Assert.Null(sql);
        }
    }
}
