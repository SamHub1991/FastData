using FastUntility.Base;
using System;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 连接字符串加密测试
    /// </summary>
    public class EncryptionTests
    {
        /// <summary>
        /// 测试加密和解密
        /// </summary>
        [Fact]
        public void Encrypt_Decrypt_RoundTrip()
        {
            var original = "Server=127.0.0.1;Database=test_db;Uid=root;Pwd=Password123!";
            
            var encrypted = BaseSymmetric.Encrypto(original);
            var decrypted = BaseSymmetric.Decrypto(encrypted);
            
            Assert.NotEqual(original, encrypted);
            Assert.Equal(original, decrypted);
        }

        /// <summary>
        /// 测试加密结果不为空
        /// </summary>
        [Fact]
        public void Encrypt_Returns_NonEmpty()
        {
            var original = "test_connection_string";
            
            var encrypted = BaseSymmetric.Encrypto(original);
            
            Assert.NotNull(encrypted);
            Assert.NotEmpty(encrypted);
        }

        /// <summary>
        /// 测试相同输入产生相同加密结果
        /// </summary>
        [Fact]
        public void Encrypt_Same_Input_Same_Output()
        {
            var original = "same_input_string";
            
            var encrypted1 = BaseSymmetric.Encrypto(original);
            var encrypted2 = BaseSymmetric.Encrypto(original);
            
            Assert.Equal(encrypted1, encrypted2);
        }

        /// <summary>
        /// 测试不同输入产生不同加密结果
        /// </summary>
        [Fact]
        public void Encrypt_Different_Input_Different_Output()
        {
            var input1 = "connection_string_1";
            var input2 = "connection_string_2";
            
            var encrypted1 = BaseSymmetric.Encrypto(input1);
            var encrypted2 = BaseSymmetric.Encrypto(input2);
            
            Assert.NotEqual(encrypted1, encrypted2);
        }

        /// <summary>
        /// 测试解密空字符串
        /// </summary>
        [Fact]
        public void Decrypt_Empty_String()
        {
            var decrypted = BaseSymmetric.Decrypto("");
            
            Assert.NotNull(decrypted);
        }

        /// <summary>
        /// 测试加密特殊字符
        /// </summary>
        [Fact]
        public void Encrypt_Special_Characters()
        {
            var original = "Server=127.0.0.1;Pwd=P@ssw0rd!#$%";
            
            var encrypted = BaseSymmetric.Encrypto(original);
            var decrypted = BaseSymmetric.Decrypto(encrypted);
            
            Assert.Equal(original, decrypted);
        }

        /// <summary>
        /// 测试加密长字符串
        /// </summary>
        [Fact]
        public void Encrypt_Long_String()
        {
            var original = new string('A', 1000) + ";Server=long_connection_string_test";
            
            var encrypted = BaseSymmetric.Encrypto(original);
            var decrypted = BaseSymmetric.Decrypto(encrypted);
            
            Assert.Equal(original, decrypted);
        }
    }
}
