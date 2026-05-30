using System;
using System.Data.Common;
using System.Threading.Tasks;
using FastData.ConnectionPool;
using Microsoft.Data.SqlClient;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 智能连接池测试
    /// 
    /// 测试连接池的创建、复用、限制和监控功能。
    /// </summary>
    public class SmartConnectionPoolTests
    {
        private readonly string _connStr = "server=localhost;database=FastDataTest;uid=sa;pwd=FastData@Test123;TrustServerCertificate=true";

        /// <summary>
        /// 测试连接池创建并返回连接
        /// </summary>
        [Fact]
        public void Pool_Should_Create_And_Return_Connection()
        {
            // Arrange
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10,
                ConnectionTimeout = 5
            };

            using var pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);

            // Act
            using var connection = pool.GetConnection();

            // Assert
            Assert.NotNull(connection);
            Assert.NotNull(connection.Connection);
            Assert.Equal(System.Data.ConnectionState.Open, connection.Connection.State);
        }

        /// <summary>
        /// 测试连接池复用连接
        /// </summary>
        [Fact]
        public async Task Pool_Should_Reuse_Connections()
        {
            // Arrange
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10,
                ConnectionTimeout = 5
            };

            using var pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);

            // Act - 获取并归还连接
            var connection1 = await pool.GetConnectionAsync();
            var connectionId1 = connection1.Id;
            connection1.Dispose();

            var connection2 = await pool.GetConnectionAsync();
            var connectionId2 = connection2.Id;
            connection2.Dispose();

            // Assert - 应该复用同一个连接
            Assert.Equal(connectionId1, connectionId2);
        }

        /// <summary>
        /// 测试连接池最大连接数限制
        /// </summary>
        [Fact]
        public async Task Pool_Should_Respect_MaxPoolSize()
        {
            // Arrange
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 1,
                MaxPoolSize = 2,
                ConnectionTimeout = 1
            };

            using var pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);

            // Act - 获取最大连接数
            var conn1 = await pool.GetConnectionAsync();
            var conn2 = await pool.GetConnectionAsync();

            // Assert - 第三个连接应该超时
            await Assert.ThrowsAsync<TimeoutException>(() => pool.GetConnectionAsync());

            // Cleanup
            conn1.Dispose();
            conn2.Dispose();
        }

        /// <summary>
        /// 测试连接池指标
        /// </summary>
        [Fact]
        public void Pool_Should_Return_Metrics()
        {
            // Arrange
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10
            };

            using var pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);

            // Act
            var metrics = pool.GetMetrics();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalConnections >= 0);
            Assert.True(metrics.ActiveConnections >= 0);
            Assert.True(metrics.IdleConnections >= 0);
        }

        /// <summary>
        /// 测试连接池健康检查
        /// </summary>
        [Fact]
        public void Pool_Should_Return_PoolInfo()
        {
            // Arrange
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10
            };

            using var pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);

            // Act
            var metrics = pool.GetMetrics();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalConnections >= 0);
            Assert.True(metrics.ActiveConnections >= 0);
            Assert.True(metrics.IdleConnections >= 0);
            Assert.True(metrics.WaitingRequests >= 0);
        }

        /// <summary>
        /// 创建数据库连接工厂
        /// </summary>
        /// <returns>数据库连接</returns>
        private DbConnection CreateConnectionFactory()
        {
            var connection = new SqlConnection(_connStr);
            connection.Open();
            return connection;
        }
    }
}
