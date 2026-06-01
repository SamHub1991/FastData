using System;
using System.Linq;
using System.Linq.Expressions;
using FastData;
using FastData.Base;
using FastData.Model;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 表达式解析单元测试
    /// 覆盖边界情况和常见查询模式
    /// </summary>
    public class ExpressionParsingTests
    {
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public int Age { get; set; }
            public DateTime CreateTime { get; set; }
            public decimal? Salary { get; set; }
        }

        #region 布尔表达式测试

        [Fact]
        public void BooleanMember_EqualsTrue_ShouldGenerateCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.IsActive == true;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("IsActive=1", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        [Fact]
        public void BooleanMember_EqualsFalse_ShouldGenerateCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.IsActive == false;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("IsActive=0", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        [Fact]
        public void BooleanMember_Standalone_ShouldGenerateCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.IsActive;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("IsActive=1", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        #endregion

        #region 比较表达式测试

        [Fact]
        public void Integer_Equals_ShouldGenerateCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.Id == 1;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("Id=", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        [Fact]
        public void Integer_GreaterThan_ShouldGenerateCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.Age > 18;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("Age>", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        [Fact]
        public void NullableDecimal_HasValue_ShouldGenerateCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.Salary.HasValue;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        #endregion

        #region 逻辑组合测试

        [Fact]
        public void AndAlso_Combination_ShouldGenerateCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.IsActive && e.Age > 18;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("and", visitModel.Where.ToLower());
            Assert.True(visitModel.IsSuccess);
        }

        [Fact]
        public void OrElse_Combination_ShouldGenerateCorrectSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.Id == 1 || e.Id == 2;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("or", visitModel.Where.ToLower());
            Assert.True(visitModel.IsSuccess);
        }

        #endregion

        #region 字符串方法测试

        [Fact]
        public void String_Contains_ShouldGenerateLikeSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.Name.Contains("test");
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("like", visitModel.Where.ToLower());
            Assert.Contains("%test%", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        [Fact]
        public void String_StartsWith_ShouldGenerateLikeSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.Name.StartsWith("test");
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("like", visitModel.Where.ToLower());
            Assert.Contains("test%", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        [Fact]
        public void String_EndsWith_ShouldGenerateLikeSql()
        {
            // Arrange
            Expression<Func<TestEntity, bool>> expr = e => e.Name.EndsWith("test");
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("like", visitModel.Where.ToLower());
            Assert.Contains("%test", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        #endregion

        #region 空值处理测试

        [Fact]
        public void Null_Comparison_ShouldGenerateIsNullSql()
        {
            // Arrange
            string nullValue = null;
            Expression<Func<TestEntity, bool>> expr = e => e.Name == nullValue;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("IS NULL", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        [Fact]
        public void NotNull_Comparison_ShouldGenerateIsNotNullSql()
        {
            // Arrange
            string nullValue = null;
            Expression<Func<TestEntity, bool>> expr = e => e.Name != nullValue;
            
            // Act
            var config = FastDataConfig.GetConfig("SqlServer");
            var visitModel = VisitExpression.LambdaWhere<TestEntity>(expr, config);
            
            // Assert
            Assert.NotNull(visitModel.Where);
            Assert.Contains("IS NOT NULL", visitModel.Where);
            Assert.True(visitModel.IsSuccess);
        }

        #endregion
    }
}
