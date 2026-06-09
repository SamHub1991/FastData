using System;
using System.Collections.Generic;
using FastData.Queue;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 写入操作模型单元测试
    /// </summary>
    public class WriteOperationTests
    {
        [Fact]
        public void TestWriteOperation_Creation()
        {
            // Arrange & Act
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Add,
                TableName = "Users",
                EntityType = "FastData.Model.User",
                Data = "{\"Id\":1,\"Name\":\"Test\"}",
                DatabaseKey = "default"
            };

            // Assert
            Assert.Equal(WriteOperationType.Add, operation.OperationType);
            Assert.Equal("Users", operation.TableName);
            Assert.Equal("FastData.Model.User", operation.EntityType);
            Assert.NotNull(operation.OperationId);
            Assert.Equal(0, operation.RetryCount);
            Assert.Equal(3, operation.MaxRetries);
        }

        [Fact]
        public void TestWriteOperation_RetryCountIncrements()
        {
            // Arrange
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Update,
                TableName = "Users",
                EntityType = "FastData.Model.User",
                Data = "{\"Id\":1,\"Name\":\"Updated\"}"
            };

            // Act
            operation.RetryCount++;
            operation.RetryCount++;

            // Assert
            Assert.Equal(2, operation.RetryCount);
            Assert.True(operation.RetryCount < operation.MaxRetries);
        }

        [Fact]
        public void TestWriteOperation_MaxRetriesReached()
        {
            // Arrange
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Delete,
                TableName = "Users",
                EntityType = "FastData.Model.User",
                Data = "{\"Id\":1}",
                MaxRetries = 3
            };

            // Act
            operation.RetryCount = 3;

            // Assert
            Assert.True(operation.RetryCount >= operation.MaxRetries);
        }

        [Fact]
        public void TestWriteOperation_MetadataTracking()
        {
            // Arrange
            var operation = new WriteOperation
            {
                OperationType = WriteOperationType.Add,
                TableName = "Orders",
                EntityType = "FastData.Model.Order",
                Data = "{}"
            };

            // Act
            operation.Metadata = new Dictionary<string, object>
            {
                ["DeadLetterReason"] = "超时",
                ["DeadLetterTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["OriginalTopic"] = "orders"
            };

            // Assert
            Assert.NotNull(operation.Metadata);
            Assert.True(operation.Metadata.ContainsKey("DeadLetterReason"));
            Assert.Equal("超时", operation.Metadata["DeadLetterReason"]);
        }
    }
}
