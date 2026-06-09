using System;
using FastData.ConnectionPool;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 连接池配置单元测试
    /// </summary>
    public class ConnectionPoolConfigTests
    {
        [Fact]
        public void TestConfig_AutoCalculateDefaults()
        {
            // Arrange
            var config = new ConnectionPoolConfig();

            // Act
            config.AutoCalculate();

            // Assert
            Assert.True(config.MinPoolSize >= 2);
            Assert.True(config.MaxPoolSize >= 10);
            Assert.True(config.MaxPoolSize <= 200);
            Assert.True(config.MinPoolSize < config.MaxPoolSize);
        }

        [Fact]
        public void TestConfig_CustomValuesPreserved()
        {
            // Arrange
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 5,
                MaxPoolSize = 50
            };

            // Act
            config.AutoCalculate();

            // Assert
            Assert.Equal(5, config.MinPoolSize);
            Assert.Equal(50, config.MaxPoolSize);
        }

        [Fact]
        public void TestConfig_ConnectionTimeout()
        {
            // Arrange
            var config = new ConnectionPoolConfig();

            // Assert
            Assert.Equal(30, config.ConnectionTimeoutSeconds);
        }

        [Fact]
        public void TestConfig_HealthCheckInterval()
        {
            // Arrange
            var config = new ConnectionPoolConfig();

            // Assert
            Assert.Equal(60, config.HealthCheckIntervalSeconds);
        }
    }
}
