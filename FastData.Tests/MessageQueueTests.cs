#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastData.Queue;
using FastRedis.Messaging;
using FastRedis.Services;
using NewLife.Caching;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 消息队列测试
    /// 覆盖消息队列的核心功能和各种场景
    /// </summary>
    public class MessageQueueTests
    {
        #region Model 默认值与自定义值综合测试

        [Fact]
        public void Models_DefaultValues_ShouldBeCorrect()
        {
            var writeOp = new WriteOperation();
            Assert.Equal(WriteOperationType.Add, writeOp.OperationType);
            Assert.Null(writeOp.TableName);
            Assert.Null(writeOp.EntityType);
            Assert.Null(writeOp.Data);
            Assert.Null(writeOp.DatabaseKey);
            Assert.NotNull(writeOp.OperationId);
            Assert.Equal(0, writeOp.RetryCount);
            Assert.Equal(3, writeOp.MaxRetries);
            Assert.Null(writeOp.Metadata);

            var readOp = new ReadOperation();
            Assert.Equal(ReadOperationType.QuerySingle, readOp.OperationType);
            Assert.Null(readOp.TableName);
            Assert.Null(readOp.EntityType);
            Assert.Null(readOp.Predicate);
            Assert.Null(readOp.Fields);
            Assert.Null(readOp.OrderBy);
            Assert.True(readOp.IsAscending);
            Assert.Equal(1, readOp.PageIndex);
            Assert.Equal(20, readOp.PageSize);
            Assert.Null(readOp.DatabaseKey);
            Assert.NotNull(readOp.OperationId);
            Assert.Null(readOp.Metadata);

            var config = new WriteBehindConfig();
            Assert.Equal(WriteBehindQueueType.ReliableQueue, config.QueueType);
            Assert.Null(config.Topic);
            Assert.True(config.EnableFallback);
            Assert.True(config.EnableAutoRecovery);
            Assert.Equal(30, config.RecoveryIntervalSeconds);
            Assert.Equal(100, config.BatchFlushSize);
            Assert.Null(config.RedisConnectionString);
            Assert.Equal(7, config.RedisDb);

            var wbResult = new WriteBehindResult();
            Assert.False(wbResult.Success);
            Assert.Null(wbResult.Message);
            Assert.Equal(0, wbResult.DirectWriteCount);
            Assert.Equal(0, wbResult.QueuedCount);
            Assert.Equal(0, wbResult.FailedCount);
            Assert.False(wbResult.FallbackOccurred);
            Assert.NotNull(wbResult.Details);
            Assert.Empty(wbResult.Details);

            var writeOpResult = new WriteOperationResult();
            Assert.Equal(WriteOperationType.Add, writeOpResult.OperationType);
            Assert.Null(writeOpResult.TableName);
            Assert.False(writeOpResult.Success);
            Assert.False(writeOpResult.UsedQueue);
            Assert.Null(writeOpResult.ErrorMessage);
            Assert.Null(writeOpResult.Metadata);

            var rqResult = new ReadQueueResult();
            Assert.False(rqResult.Success);
            Assert.Null(rqResult.Message);
            Assert.Equal(0, rqResult.QueuedCount);
            Assert.Equal(0, rqResult.FailedCount);
            Assert.NotNull(rqResult.Details);
            Assert.Empty(rqResult.Details);

            var readOpResult = new ReadOperationResult();
            Assert.Equal(ReadOperationType.QuerySingle, readOpResult.OperationType);
            Assert.Null(readOpResult.TableName);
            Assert.False(readOpResult.Success);
            Assert.Null(readOpResult.ErrorMessage);
            Assert.Null(readOpResult.Metadata);

            var envelope = new MessageEnvelope<TestEntity>();
            Assert.NotNull(envelope.MessageId);
            Assert.Null(envelope.Topic);
            Assert.Null(envelope.Body);
            Assert.Null(envelope.Source);
            Assert.Null(envelope.Tag);

            var mqOptions = new MessageQueueOptions();
            Assert.Equal(MessageQueueType.ReliableQueue, mqOptions.QueueType);
            Assert.Equal("fastdata", mqOptions.TopicPrefix);
            Assert.Equal(8, mqOptions.ConsumerConcurrency);
            Assert.Equal(10, mqOptions.BlockingTimeoutSeconds);
            Assert.Equal("default", mqOptions.ConsumerGroup);
            Assert.Equal(0, mqOptions.RedisDb);
            Assert.Equal(100, mqOptions.BatchSize);
            Assert.True(mqOptions.EnablePersistence);

            var qResult = new QueueOperationResult();
            Assert.False(qResult.Success);
            Assert.Null(qResult.Message);
            Assert.Null(qResult.OperationId);
            Assert.Null(qResult.Metadata);

            Assert.Equal(0, (int)WriteOperationType.Add);
            Assert.Equal(1, (int)WriteOperationType.Update);
            Assert.Equal(2, (int)WriteOperationType.Delete);

            Assert.Equal(0, (int)ReadOperationType.QuerySingle);
            Assert.Equal(1, (int)ReadOperationType.QueryList);
            Assert.Equal(2, (int)ReadOperationType.QueryCount);
            Assert.Equal(3, (int)ReadOperationType.QueryPaging);

            Assert.Equal(0, (int)WriteBehindQueueType.None);
            Assert.Equal(1, (int)WriteBehindQueueType.ReliableQueue);
            Assert.Equal(2, (int)WriteBehindQueueType.Stream);
        }

        [Fact]
        public void Models_CustomValues_ShouldBeSet()
        {
            var writeOp = new WriteOperation
            {
                OperationType = WriteOperationType.Update,
                TableName = "Users",
                EntityType = "FastData.Tests.User",
                Data = "{\"Id\":1,\"Name\":\"Test\"}",
                DatabaseKey = "TestDb",
                RetryCount = 1,
                MaxRetries = 5,
                Metadata = new Dictionary<string, object> { {"source", "test"}, {"batchId", "BATCH-001"} }
            };
            Assert.Equal(WriteOperationType.Update, writeOp.OperationType);
            Assert.Equal("Users", writeOp.TableName);
            Assert.Equal("FastData.Tests.User", writeOp.EntityType);
            Assert.Equal("{\"Id\":1,\"Name\":\"Test\"}", writeOp.Data);
            Assert.Equal("TestDb", writeOp.DatabaseKey);
            Assert.Equal(1, writeOp.RetryCount);
            Assert.Equal(5, writeOp.MaxRetries);
            Assert.Equal("test", writeOp.Metadata["source"]);
            Assert.Equal("BATCH-001", writeOp.Metadata["batchId"]);

            var readOp = new ReadOperation
            {
                OperationType = ReadOperationType.QueryPaging,
                TableName = "Users",
                EntityType = "FastData.Tests.User",
                Predicate = "IsActive == true",
                Fields = "Id,Name,Email",
                OrderBy = "CreateTime",
                IsAscending = false,
                PageIndex = 2,
                PageSize = 10,
                DatabaseKey = "TestDb",
                Metadata = new Dictionary<string, object> { {"requestId", "REQ-001"} }
            };
            Assert.Equal(ReadOperationType.QueryPaging, readOp.OperationType);
            Assert.Equal("Users", readOp.TableName);
            Assert.Equal("FastData.Tests.User", readOp.EntityType);
            Assert.Equal("IsActive == true", readOp.Predicate);
            Assert.Equal("Id,Name,Email", readOp.Fields);
            Assert.Equal("CreateTime", readOp.OrderBy);
            Assert.False(readOp.IsAscending);
            Assert.Equal(2, readOp.PageIndex);
            Assert.Equal(10, readOp.PageSize);
            Assert.Equal("TestDb", readOp.DatabaseKey);
            Assert.Equal("REQ-001", readOp.Metadata["requestId"]);

            var config = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.Stream,
                Topic = "test-topic",
                EnableFallback = false,
                EnableAutoRecovery = false,
                RecoveryIntervalSeconds = 60,
                BatchFlushSize = 200,
                RedisConnectionString = "192.168.1.100:6379",
                RedisDb = 5
            };
            Assert.Equal(WriteBehindQueueType.Stream, config.QueueType);
            Assert.Equal("test-topic", config.Topic);
            Assert.False(config.EnableFallback);
            Assert.False(config.EnableAutoRecovery);
            Assert.Equal(60, config.RecoveryIntervalSeconds);
            Assert.Equal(200, config.BatchFlushSize);
            Assert.Equal("192.168.1.100:6379", config.RedisConnectionString);
            Assert.Equal(5, config.RedisDb);

            var envelope = new MessageEnvelope<TestEntity>
            {
                Topic = "test-topic",
                Body = new TestEntity { Id = 1, Name = "Test" },
                Source = "test-source",
                Tag = "test-tag"
            };
            Assert.Equal("test-topic", envelope.Topic);
            Assert.NotNull(envelope.Body);
            Assert.Equal(1, envelope.Body.Id);
            Assert.Equal("Test", envelope.Body.Name);
            Assert.Equal("test-source", envelope.Source);
            Assert.Equal("test-tag", envelope.Tag);

            var mqOptions = new MessageQueueOptions
            {
                QueueType = MessageQueueType.Stream,
                TopicPrefix = "myapp",
                ConsumerConcurrency = 16,
                BlockingTimeoutSeconds = 30,
                ConsumerGroup = "analytics",
                RedisDb = 5,
                BatchSize = 200,
                EnablePersistence = false
            };
            Assert.Equal(MessageQueueType.Stream, mqOptions.QueueType);
            Assert.Equal("myapp", mqOptions.TopicPrefix);
            Assert.Equal(16, mqOptions.ConsumerConcurrency);
            Assert.Equal(30, mqOptions.BlockingTimeoutSeconds);
            Assert.Equal("analytics", mqOptions.ConsumerGroup);
            Assert.Equal(5, mqOptions.RedisDb);
            Assert.Equal(200, mqOptions.BatchSize);
            Assert.False(mqOptions.EnablePersistence);

            var qResult = new QueueOperationResult
            {
                Success = true,
                Message = "操作成功",
                OperationId = "OP-001",
                Metadata = new Dictionary<string, object> { {"consumer", "test-consumer"} }
            };
            Assert.True(qResult.Success);
            Assert.Equal("操作成功", qResult.Message);
            Assert.Equal("OP-001", qResult.OperationId);
            Assert.Equal("test-consumer", qResult.Metadata["consumer"]);
        }

        #endregion

        #region WriteBehindRegistry 测试

        /// <summary>
        /// 测试注册表基本操作
        /// 验证可以正确注册和获取配置
        /// </summary>
        [Fact]
        public void WriteBehindRegistry_RegisterAndGet_ShouldWork()
        {
            // Arrange
            var tableName = "TestTable_" + Guid.NewGuid().ToString("N");
            var config = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                Topic = "test-topic"
            };

            // Act
            WriteBehindRegistry.Register(tableName, config);
            var result = WriteBehindRegistry.GetConfig(tableName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(WriteBehindQueueType.ReliableQueue, result.QueueType);
            Assert.Equal("test-topic", result.Topic);

            // Cleanup
            WriteBehindRegistry.Unregister(tableName);
        }

        /// <summary>
        /// 测试注册表泛型操作
        /// 验证可以使用泛型方式注册和获取配置
        /// </summary>
        [Fact]
        public void WriteBehindRegistry_GenericRegister_ShouldWork()
        {
            // Arrange
            var config = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.Stream,
                Topic = "generic-topic"
            };

            // Act
            WriteBehindRegistry.Register<TestEntity>(config);
            var result = WriteBehindRegistry.GetConfig<TestEntity>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(WriteBehindQueueType.Stream, result.QueueType);
            Assert.Equal("generic-topic", result.Topic);

            // Cleanup
            WriteBehindRegistry.Unregister<TestEntity>();
        }

        /// <summary>
        /// 测试检查队列是否启用
        /// 验证 IsQueueEnabled 方法正确工作
        /// </summary>
        [Fact]
        public void WriteBehindRegistry_IsQueueEnabled_ShouldWork()
        {
            // Arrange
            var tableName = "EnabledTable_" + Guid.NewGuid().ToString("N");
            var config = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                Topic = "enabled-topic"
            };

            // Act
            WriteBehindRegistry.Register(tableName, config);

            // Assert
            Assert.True(WriteBehindRegistry.IsQueueEnabled(tableName));

            // Cleanup
            WriteBehindRegistry.Unregister(tableName);
        }

        /// <summary>
        /// 测试未注册的表应该返回 false
        /// 验证未注册的表不会被误判为已启用
        /// </summary>
        [Fact]
        public void WriteBehindRegistry_UnregisteredTable_ShouldReturnFalse()
        {
            // Arrange
            var tableName = "NonExistentTable_" + Guid.NewGuid().ToString("N");

            // Act
            var result = WriteBehindRegistry.IsQueueEnabled(tableName);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试取消注册
        /// 验证可以正确取消注册表配置
        /// </summary>
        [Fact]
        public void WriteBehindRegistry_Unregister_ShouldWork()
        {
            // Arrange
            var tableName = "UnregisterTable_" + Guid.NewGuid().ToString("N");
            var config = new WriteBehindConfig
            {
                QueueType = WriteBehindQueueType.ReliableQueue,
                Topic = "unregister-topic"
            };

            // Act
            WriteBehindRegistry.Register(tableName, config);
            Assert.True(WriteBehindRegistry.IsQueueEnabled(tableName));

            WriteBehindRegistry.Unregister(tableName);

            // Assert
            Assert.False(WriteBehindRegistry.IsQueueEnabled(tableName));
            Assert.Null(WriteBehindRegistry.GetConfig(tableName));
        }

        /// <summary>
        /// 测试获取所有配置
        /// 验证可以正确获取所有已注册的配置
        /// </summary>
        [Fact]
        public void WriteBehindRegistry_GetAllConfigs_ShouldWork()
        {
            // Arrange
            var table1 = "AllConfigsTable1_" + Guid.NewGuid().ToString("N");
            var table2 = "AllConfigsTable2_" + Guid.NewGuid().ToString("N");
            var config1 = new WriteBehindConfig { QueueType = WriteBehindQueueType.ReliableQueue };
            var config2 = new WriteBehindConfig { QueueType = WriteBehindQueueType.Stream };

            // Act
            WriteBehindRegistry.Register(table1, config1);
            WriteBehindRegistry.Register(table2, config2);
            var allConfigs = WriteBehindRegistry.GetAllConfigs();

            // Assert
            Assert.NotNull(allConfigs);
            Assert.True(allConfigs.ContainsKey(table1));
            Assert.True(allConfigs.ContainsKey(table2));

            // Cleanup
            WriteBehindRegistry.Unregister(table1);
            WriteBehindRegistry.Unregister(table2);
        }

        /// <summary>
        /// 测试清空所有配置
        /// 验证可以正确清空所有注册的配置
        /// </summary>
        [Fact]
        public void WriteBehindRegistry_Clear_ShouldWork()
        {
            // Arrange
            var table1 = "ClearTable1_" + Guid.NewGuid().ToString("N");
            var table2 = "ClearTable2_" + Guid.NewGuid().ToString("N");
            var config = new WriteBehindConfig { QueueType = WriteBehindQueueType.ReliableQueue };

            WriteBehindRegistry.Register(table1, config);
            WriteBehindRegistry.Register(table2, config);

            // Act
            WriteBehindRegistry.Clear();

            // Assert
            Assert.False(WriteBehindRegistry.IsQueueEnabled(table1));
            Assert.False(WriteBehindRegistry.IsQueueEnabled(table2));

            // Cleanup
            WriteBehindRegistry.Unregister(table1);
            WriteBehindRegistry.Unregister(table2);
        }

        #endregion



        #region 辅助类

        /// <summary>
        /// 测试实体类
        /// </summary>
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion
    }
}
#endif
