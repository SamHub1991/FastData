using System;
using System.Collections.Generic;
using System.Linq;
using FastData.Model;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 测试用实体类
    /// </summary>
    public class TestUser
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public int Age { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 链式 Where/Or 条件测试
    /// </summary>
    public class ChainableWhereTests
    {
        /// <summary>
        /// 测试 DataQuery.ChainedConditions 属性初始化
        /// </summary>
        [Fact]
        public void ChainedConditions_ShouldBeInitialized()
        {
            // Arrange & Act
            var query = new DataQuery();

            // Assert
            Assert.NotNull(query.ChainedConditions);
            Assert.Empty(query.ChainedConditions);
        }

        /// <summary>
        /// 测试 ChainedCondition 类属性
        /// </summary>
        [Fact]
        public void ChainedCondition_Properties_ShouldWork()
        {
            // Arrange & Act
            var condition = new ChainedCondition
            {
                Operator = "AND",
                Where = "Age > 18",
                Param = new List<System.Data.Common.DbParameter>()
            };

            // Assert
            Assert.Equal("AND", condition.Operator);
            Assert.Equal("Age > 18", condition.Where);
            Assert.NotNull(condition.Param);
        }

        /// <summary>
        /// 测试添加多个链式条件
        /// </summary>
        [Fact]
        public void ChainedConditions_AddMultiple_ShouldWork()
        {
            // Arrange
            var query = new DataQuery();

            // Act
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Age > 18"
            });
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "OR",
                Where = "Role = 'Admin'"
            });
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Department = 'IT'"
            });

            // Assert
            Assert.Equal(3, query.ChainedConditions.Count);
            Assert.Equal("AND", query.ChainedConditions[0].Operator);
            Assert.Equal("OR", query.ChainedConditions[1].Operator);
            Assert.Equal("AND", query.ChainedConditions[2].Operator);
        }

        /// <summary>
        /// 测试链式条件与初始条件组合
        /// </summary>
        [Fact]
        public void ChainedConditions_WithPredicate_ShouldCombine()
        {
            // Arrange
            var query = new DataQuery();
            query.Predicate.Add(new VisitModel { Where = "IsActive = 1" });

            // Act
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Age > 18"
            });

            // Assert
            Assert.Single(query.Predicate);
            Assert.Single(query.ChainedConditions);
            Assert.Equal("IsActive = 1", query.Predicate[0].Where);
            Assert.Equal("Age > 18", query.ChainedConditions[0].Where);
        }

        /// <summary>
        /// 测试 ChainedCondition 默认值
        /// </summary>
        [Fact]
        public void ChainedCondition_DefaultValues_ShouldBeNull()
        {
            // Arrange & Act
            var condition = new ChainedCondition();

            // Assert
            Assert.Null(condition.Operator);
            Assert.Null(condition.Where);
            Assert.NotNull(condition.Param);
            Assert.Empty(condition.Param);
        }

        /// <summary>
        /// 测试清空链式条件
        /// </summary>
        [Fact]
        public void ChainedConditions_Clear_ShouldWork()
        {
            // Arrange
            var query = new DataQuery();
            query.ChainedConditions.Add(new ChainedCondition { Operator = "AND", Where = "Age > 18" });
            query.ChainedConditions.Add(new ChainedCondition { Operator = "OR", Where = "Role = 'Admin'" });

            // Act
            query.ChainedConditions.Clear();

            // Assert
            Assert.Empty(query.ChainedConditions);
        }

        /// <summary>
        /// 测试链式条件数量统计
        /// </summary>
        [Fact]
        public void ChainedConditions_Count_ShouldBeAccurate()
        {
            // Arrange
            var query = new DataQuery();

            // Act & Assert
            Assert.Equal(0, query.ChainedConditions.Count);

            query.ChainedConditions.Add(new ChainedCondition { Operator = "AND", Where = "Age > 18" });
            Assert.Equal(1, query.ChainedConditions.Count);

            query.ChainedConditions.Add(new ChainedCondition { Operator = "OR", Where = "Role = 'Admin'" });
            Assert.Equal(2, query.ChainedConditions.Count);
        }

        /// <summary>
        /// 测试链式条件支持复杂 SQL 表达式
        /// </summary>
        [Fact]
        public void ChainedConditions_ComplexExpression_ShouldSupport()
        {
            // Arrange
            var query = new DataQuery();

            // Act
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Age BETWEEN 18 AND 65"
            });
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Department IN ('IT', 'HR', 'Finance')"
            });
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "OR",
                Where = "UserName LIKE '%admin%'"
            });

            // Assert
            Assert.Equal(3, query.ChainedConditions.Count);
            Assert.Contains("BETWEEN", query.ChainedConditions[0].Where);
            Assert.Contains("IN", query.ChainedConditions[1].Where);
            Assert.Contains("LIKE", query.ChainedConditions[2].Where);
        }
    }
}
