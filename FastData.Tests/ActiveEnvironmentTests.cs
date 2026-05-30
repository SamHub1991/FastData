using System;
using System.IO;
using FastData.Config;
using Xunit;

namespace FastData.Tests
{
    public class ActiveEnvironmentTests
    {
        [Fact]
        public void ActiveAttribute_Dev_ShouldLoadDevConfig()
        {
            // Arrange
            var dbConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db.config");
            var dbDevConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db.dev.config");
            
            // 确保配置文件存在
            Assert.True(File.Exists(dbConfigPath), "db.config 文件不存在");
            Assert.True(File.Exists(dbDevConfigPath), "db.dev.config 文件不存在");
            
            // Act
            var activeEnv = FastDataConfig.GetActiveEnvironment();
            
            // Assert
            Assert.NotNull(activeEnv);
            Assert.NotEmpty(activeEnv);
            Console.WriteLine($"Active Environment: {activeEnv}");
        }

        [Fact]
        public void ActiveAttribute_EnvVar_ShouldOverrideConfig()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("FASTDATA_ACTIVE");
            
            try
            {
                // 设置环境变量
                Environment.SetEnvironmentVariable("FASTDATA_ACTIVE", "pro");
                
                // Act
                var activeEnv = FastDataConfig.GetActiveEnvironment();
                
                // Assert
                Assert.Equal("pro", activeEnv);
                Console.WriteLine($"Active Environment (from env var): {activeEnv}");
            }
            finally
            {
                // 恢复原始环境变量
                Environment.SetEnvironmentVariable("FASTDATA_ACTIVE", originalEnv);
            }
        }

        [Fact]
        public void ActiveAttribute_Config_ShouldLoadConfig()
        {
            // Arrange
            var dbConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db.config");
            
            // 读取配置文件内容
            var configContent = File.ReadAllText(dbConfigPath);
            Console.WriteLine($"db.config content:\n{configContent}");
            
            // Act - 尝试加载配置
            var connStr = FastDataConfig.GetConnectionString("SqlServer");
            
            // Assert
            Assert.NotNull(connStr);
            Assert.NotEmpty(connStr);
            Console.WriteLine($"SqlServer ConnectionString: {connStr}");
        }

        [Fact]
        public void ActiveAttribute_DevConfig_ShouldHaveDifferentDatabase()
        {
            // Arrange
            var dbDevConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db.dev.config");
            
            // 读取 dev 配置文件内容
            var configContent = File.ReadAllText(dbDevConfigPath);
            Console.WriteLine($"db.dev.config content:\n{configContent}");
            
            // 验证 dev 配置包含 Dev 数据库
            Assert.Contains("FastDataTest_Dev", configContent);
        }

        [Fact]
        public void ActiveAttribute_ProConfig_ShouldHaveDifferentDatabase()
        {
            // Arrange
            var dbProConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db.pro.config");
            
            // 读取 pro 配置文件内容
            var configContent = File.ReadAllText(dbProConfigPath);
            Console.WriteLine($"db.pro.config content:\n{configContent}");
            
            // 验证 pro 配置包含 Pro 数据库
            Assert.Contains("FastDataTest_Pro", configContent);
        }
    }
}
