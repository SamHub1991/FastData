using System;
using System.Collections.Generic;
using System.Data.Common;
using FastData.Base;
using FastData.Model;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// WhereBuilder 单元测试
    /// </summary>
    public class WhereBuilderTests
    {
        /// <summary>
        /// 测试没有条件时返回空字符串
        /// </summary>
        [Fact]
        public void BuildWhereClause_NoConditions_ReturnsEmptyString()
        {
            // Arrange
            var query = new DataQuery();
            var param = new List<DbParameter>();

            // Act
            var result = WhereBuilder.BuildWhereClause(query, ref param);

            // Assert
            Assert.Equal("", result);
            Assert.Empty(param);
        }

        /// <summary>
        /// 测试只有初始条件（无链式条件）
        /// </summary>
        [Fact]
        public void BuildWhereClause_OnlyInitialCondition_ReturnsInitialWhere()
        {
            // Arrange
            var query = new DataQuery();
            query.Predicate.Add(new VisitModel { Where = "Age > 18" });
            var param = new List<DbParameter>();

            // Act
            var result = WhereBuilder.BuildWhereClause(query, ref param);

            // Assert
            Assert.Equal("Age > 18", result);
        }

        /// <summary>
        /// 测试只有链式条件（无初始条件）
        /// </summary>
        [Fact]
        public void BuildWhereClause_OnlyChainedConditions_ReturnsChainedWhere()
        {
            // Arrange
            var query = new DataQuery();
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
            var param = new List<DbParameter>();

            // Act
            var result = WhereBuilder.BuildWhereClause(query, ref param);

            // Assert
            Assert.Equal("Age > 18 OR Role = 'Admin'", result);
        }

        /// <summary>
        /// 测试初始条件 + 链式条件组合
        /// </summary>
        [Fact]
        public void BuildWhereClause_InitialAndChainedConditions_ReturnsCombinedWhere()
        {
            // Arrange
            var query = new DataQuery();
            query.Predicate.Add(new VisitModel { Where = "IsActive = 1" });
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
            var param = new List<DbParameter>();

            // Act
            var result = WhereBuilder.BuildWhereClause(query, ref param);

            // Assert
            Assert.Equal("IsActive = 1 AND Age > 18 OR Role = 'Admin'", result);
        }

        /// <summary>
        /// 测试 HasWhereClause 无条件返回 false
        /// </summary>
        [Fact]
        public void HasWhereClause_NoConditions_ReturnsFalse()
        {
            // Arrange
            var query = new DataQuery();

            // Act
            var result = WhereBuilder.HasWhereClause(query);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试 HasWhereClause 有初始条件返回 true
        /// </summary>
        [Fact]
        public void HasWhereClause_HasInitialCondition_ReturnsTrue()
        {
            // Arrange
            var query = new DataQuery();
            query.Predicate.Add(new VisitModel { Where = "Age > 18" });

            // Act
            var result = WhereBuilder.HasWhereClause(query);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试 HasWhereClause 有链式条件返回 true
        /// </summary>
        [Fact]
        public void HasWhereClause_HasChainedCondition_ReturnsTrue()
        {
            // Arrange
            var query = new DataQuery();
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Age > 18"
            });

            // Act
            var result = WhereBuilder.HasWhereClause(query);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试多个 AND 条件组合
        /// </summary>
        [Fact]
        public void BuildWhereClause_MultipleAndConditions_ReturnsCorrectSql()
        {
            // Arrange
            var query = new DataQuery();
            query.Predicate.Add(new VisitModel { Where = "Status = 1" });
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Age > 18"
            });
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Department = 'IT'"
            });
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Salary > 10000"
            });
            var param = new List<DbParameter>();

            // Act
            var result = WhereBuilder.BuildWhereClause(query, ref param);

            // Assert
            Assert.Equal("Status = 1 AND Age > 18 AND Department = 'IT' AND Salary > 10000", result);
        }

        /// <summary>
        /// 测试混合 AND/OR 条件
        /// </summary>
        [Fact]
        public void BuildWhereClause_MixedAndOrConditions_ReturnsCorrectSql()
        {
            // Arrange
            var query = new DataQuery();
            query.Predicate.Add(new VisitModel { Where = "IsActive = 1" });
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
            var param = new List<DbParameter>();

            // Act
            var result = WhereBuilder.BuildWhereClause(query, ref param);

            // Assert
            Assert.Equal("IsActive = 1 AND Age > 18 OR Role = 'Admin' AND Department = 'IT'", result);
        }

        /// <summary>
        /// 测试初始条件为空字符串但有链式条件
        /// </summary>
        [Fact]
        public void BuildWhereClause_EmptyInitialWithChained_ReturnsChainedWhere()
        {
            // Arrange
            var query = new DataQuery();
            query.Predicate.Add(new VisitModel { Where = "" });
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Age > 18"
            });
            var param = new List<DbParameter>();

            // Act
            var result = WhereBuilder.BuildWhereClause(query, ref param);

            // Assert
            Assert.Equal("Age > 18", result);
        }

        /// <summary>
        /// 测试 HasWhereClause 空初始条件有链式条件返回 true
        /// </summary>
        [Fact]
        public void HasWhereClause_EmptyInitialWithChained_ReturnsTrue()
        {
            // Arrange
            var query = new DataQuery();
            query.Predicate.Add(new VisitModel { Where = "" });
            query.ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = "Age > 18"
            });

            // Act
            var result = WhereBuilder.HasWhereClause(query);

            // Assert
            Assert.True(result);
        }
    }
}
