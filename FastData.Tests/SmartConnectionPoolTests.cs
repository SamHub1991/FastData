using System;
using System.Data.Common;
using System.Threading.Tasks;
using FastData.ConnectionPool;
using Microsoft.Data.SqlClient;
using Xunit;

namespace FastData.Tests
{
    public class SmartConnectionPoolTests
    {
        private readonly string _connStr = "server=localhost;database=FastDataTest;uid=sa;pwd=FastData@Test123;TrustServerCertificate=true";

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
            Assert.Equal(2, metrics.IdleConnections);
            Assert.Equal(0, metrics.ActiveConnections);
            Assert.Equal(2, metrics.TotalConnections);
        }

        [Fact]
        public async Task Pool_Should_Track_Active_Connections()
        {
            // Arrange
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10
            };

            using var pool = new SmartConnectionPool("TestPool", CreateConnectionFactory, config);

            // Act
            var connection = await pool.GetConnectionAsync();
            var metrics = pool.GetMetrics();

            // Assert
            Assert.Equal(1, metrics.ActiveConnections);
            Assert.Equal(1, metrics.IdleConnections);

            // Cleanup
            connection.Dispose();
        }

        [Fact]
        public void Factory_Should_Manage_Multiple_Pools()
        {
            // Arrange
            var factory = ConnectionPoolFactory.Instance;
            var config = new ConnectionPoolConfig { MinPoolSize = 1, MaxPoolSize = 5 };

            // Act
            var pool1 = factory.GetOrCreatePool("Pool1", CreateConnectionFactory, config);
            var pool2 = factory.GetOrCreatePool("Pool2", CreateConnectionFactory, config);

            // Assert
            Assert.NotNull(pool1);
            Assert.NotNull(pool2);
            Assert.NotSame(pool1, pool2);

            var metrics = factory.GetAllMetrics();
            Assert.True(metrics.ContainsKey("Pool1"));
            Assert.True(metrics.ContainsKey("Pool2"));
        }

        [Fact]
        public void Monitor_Should_Collect_Snapshots()
        {
            // Arrange
            var factory = ConnectionPoolFactory.Instance;
            var config = new ConnectionPoolConfig { MinPoolSize = 1, MaxPoolSize = 5 };
            var pool = factory.GetOrCreatePool("MonitorTestPool", CreateConnectionFactory, config);

            using var monitor = new ConnectionPoolMonitor(factory, 1);

            // Act
            var snapshot = monitor.TakeSnapshot();

            // Assert
            Assert.NotNull(snapshot);
            Assert.True(snapshot.Timestamp > DateTime.MinValue);
            Assert.True(snapshot.PoolMetrics.Count > 0);
        }

        private DbConnection CreateConnectionFactory()
        {
            var connection = new SqlConnection(_connStr);
            // 不要在这里打开连接，连接池会自动打开
            return connection;
        }
    }
}
