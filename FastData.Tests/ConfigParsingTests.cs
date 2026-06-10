using System;
using System.Reflection;
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
            var result = InvokeIsTrue("true");
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TestIsTrue_WithFalseValue()
        {
            // Arrange & Act
            var result = InvokeIsTrue("false");
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TestIsTrue_WithCaseInsensitive()
        {
            // Arrange & Act
            var resultTrue = InvokeIsTrue("TRUE");
            
            var resultMixed = InvokeIsTrue("TrUe");
            
            // Assert
            Assert.True(resultTrue);
            Assert.True(resultMixed);
        }

        [Fact]
        public void TestIsTrue_WithNullOrEmpty()
        {
            // Arrange & Act
            var resultNull = InvokeIsTrue(null);
            
            var resultEmpty = InvokeIsTrue("");
            
            // Assert
            Assert.False(resultNull);
            Assert.False(resultEmpty);
        }

        private static bool InvokeIsTrue(string value)
        {
            var type = typeof(FastData.Config.FastDataConfig).Assembly.GetType("FastData.Config.DataConfig");
            var method = type.GetMethod("IsTrue", BindingFlags.NonPublic | BindingFlags.Static);
            return (bool)method.Invoke(null, new object[] { value });
        }
    }
}
