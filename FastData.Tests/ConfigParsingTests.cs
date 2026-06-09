using System;
using FastData.Model;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 配置解析单元测试
    /// </summary>
    public class ConfigParsingTests
    {
        [Fact]
        public void TestIsTrue_WithTrueValue()
        {
            // Arrange & Act
            var result = typeof(FastData.Config.DataConfig.DataConfig)
                .GetMethod("IsTrue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { "true" }) as bool?;
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TestIsTrue_WithFalseValue()
        {
            // Arrange & Act
            var result = typeof(FastData.Config.DataConfig.DataConfig)
                .GetMethod("IsTrue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { "false" }) as bool?;
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TestIsTrue_WithCaseInsensitive()
        {
            // Arrange & Act
            var resultTrue = typeof(FastData.Config.DataConfig.DataConfig)
                .GetMethod("IsTrue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { "TRUE" }) as bool?;
            
            var resultMixed = typeof(FastData.Config.DataConfig.DataConfig)
                .GetMethod("IsTrue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { "TrUe" }) as bool?;
            
            // Assert
            Assert.True(resultTrue);
            Assert.True(resultMixed);
        }

        [Fact]
        public void TestIsTrue_WithNullOrEmpty()
        {
            // Arrange & Act
            var resultNull = typeof(FastData.Config.DataConfig.DataConfig)
                .GetMethod("IsTrue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { null }) as bool?;
            
            var resultEmpty = typeof(FastData.Config.DataConfig.DataConfig)
                .GetMethod("IsTrue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Invoke(null, new object[] { "" }) as bool?;
            
            // Assert
            Assert.False(resultNull);
            Assert.False(resultEmpty);
        }
    }
}
