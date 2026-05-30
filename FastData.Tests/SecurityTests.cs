using FastUntility.Security;
using System;
using System.Threading;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 安全功能测试
    /// 
    /// 测试服务器监控、JWT 令牌、加密解密等安全相关功能。
    /// </summary>
    public class SecurityTests
    {
        #region Server Monitor 测试
        /// <summary>
        /// 测试获取服务器监控信息
        /// </summary>
        [Fact]
        public void Test_ServerMonitor_GetInfo()
        {
            var info = ServerMonitor.GetMonitorInfo();

            Assert.NotNull(info);
            Assert.NotNull(info.MachineName);
            Assert.NotNull(info.OsVersion);
            Assert.True(info.ProcessorCount > 0);
            Assert.True(info.TotalMemory > 0);
            Assert.True(info.Uptime.TotalSeconds > 0);
        }

        /// <summary>
        /// 测试获取 CPU 使用率
        /// </summary>
        [Fact]
        public void Test_ServerMonitor_GetCpuUsage()
        {
            var cpuUsage = ServerMonitor.GetCpuUsage();
            Assert.True(cpuUsage >= 0 && cpuUsage <= 100);
        }

        /// <summary>
        /// 测试获取内存信息
        /// </summary>
        [Fact]
        public void Test_ServerMonitor_GetMemoryInfo()
        {
            var (total, used, available, usage) = ServerMonitor.GetMemoryInfo();

            Assert.True(total > 0);
            Assert.True(used >= 0);
            Assert.True(available >= 0);
            Assert.True(usage >= 0 && usage <= 100);
        }

        /// <summary>
        /// 测试获取磁盘信息
        /// </summary>
        [Fact(Skip = "容器环境可能没有可用的磁盘分区")]
        public void Test_ServerMonitor_GetDiskInfo()
        {
            var disks = ServerMonitor.GetDiskInfo();

            Assert.NotNull(disks);
            foreach (var disk in disks)
            {
                Assert.NotNull(disk.Name);
                Assert.True(disk.TotalSize >= 0);
                Assert.True(disk.FreeSpace >= 0);
                Assert.True(disk.UsedSpace >= 0);
                Assert.True(disk.UsagePercentage >= 0 && disk.UsagePercentage <= 100);
            }
        }

        /// <summary>
        /// 测试获取系统运行时间
        /// </summary>
        [Fact]
        public void Test_ServerMonitor_GetUptime()
        {
            var uptime = ServerMonitor.GetUptime();
            Assert.True(uptime.TotalSeconds > 0);
        }
        #endregion

        #region JWT 测试
        /// <summary>
        /// 测试 JWT 令牌生成和验证
        /// </summary>
        [Fact]
        public void Test_Jwt_GenerateAndValidate()
        {
            var secret = "my-secret-key-1234567890";
            var payload = JwtPayload.Create(
                subject: "user123",
                issuer: "FastData",
                audience: "FastDataApp",
                expiry: TimeSpan.FromHours(1),
                claims: new System.Collections.Generic.Dictionary<string, object>
                {
                    { "role", "admin" },
                    { "email", "user@example.com" }
                }
            );

            var token = JwtHelper.GenerateToken(payload, secret);

            Assert.NotNull(token);
            Assert.True(token.Split('.').Length == 3);

            var validatedPayload = JwtHelper.ValidateToken(token, secret);

            Assert.Equal("user123", validatedPayload.Sub);
            Assert.Equal("FastData", validatedPayload.Iss);
            Assert.Equal("FastDataApp", validatedPayload.Aud);
            Assert.True(validatedPayload.Claims.ContainsKey("role"));
            Assert.True(validatedPayload.Claims.ContainsKey("email"));
        }

        /// <summary>
        /// 测试 JWT 令牌解码
        /// </summary>
        [Fact]
        public void Test_Jwt_DecodeToken()
        {
            var secret = "my-secret-key-1234567890";
            var payload = JwtPayload.Create(
                subject: "user456",
                issuer: "FastData",
                expiry: TimeSpan.FromMinutes(30)
            );

            var token = JwtHelper.GenerateToken(payload, secret);
            var decoded = JwtHelper.DecodeToken(token);

            Assert.NotNull(decoded);
            Assert.Equal("user456", decoded.Sub);
            Assert.Equal("FastData", decoded.Iss);
        }

        /// <summary>
        /// 测试 JWT 令牌过期验证
        /// </summary>
        [Fact]
        public void Test_Jwt_ExpiredToken()
        {
            var secret = "my-secret-key-1234567890";
            var payload = JwtPayload.Create(
                subject: "user789",
                issuer: "FastData",
                expiry: TimeSpan.FromSeconds(-1) // 已过期
            );

            var token = JwtHelper.GenerateToken(payload, secret);

            Assert.ThrowsAny<Exception>(() => JwtHelper.ValidateToken(token, secret));
        }

        /// <summary>
        /// 测试 JWT 令牌篡改检测
        /// </summary>
        [Fact]
        public void Test_Jwt_TamperedToken()
        {
            var secret = "my-secret-key-1234567890";
            var payload = JwtPayload.Create(
                subject: "user123",
                issuer: "FastData",
                expiry: TimeSpan.FromHours(1)
            );

            var token = JwtHelper.GenerateToken(payload, secret);
            var tampered = token.Substring(0, token.Length - 5) + "XXXXX";

            Assert.ThrowsAny<Exception>(() => JwtHelper.ValidateToken(tampered, secret));
        }
        #endregion

        #region 加密解密测试
        /// <summary>
        /// 测试 AES 加密解密
        /// </summary>
        [Fact]
        public void Test_Aes_EncryptDecrypt()
        {
            var key = "0123456789ABCDEF"; // 16 字节密钥
            var iv = "FEDCBA9876543210"; // 16 字节 IV
            var plaintext = "Hello, FastData! 你好，世界！";

            var encrypted = AesHelper.Encrypt(plaintext, key, iv);
            var decrypted = AesHelper.Decrypt(encrypted, key, iv);

            Assert.NotNull(encrypted);
            Assert.NotEqual(plaintext, encrypted);
            Assert.Equal(plaintext, decrypted);
        }

        /// <summary>
        /// 测试 AES 加密解密（带完整 IV）
        /// </summary>
        [Fact]
        public void Test_Aes_EncryptDecrypt_WithIV()
        {
            var key = "0123456789ABCDEF";
            var plaintext = "Test 数据 123!@#";

            var encrypted = AesHelper.EncryptWithIV(plaintext, key);
            var decrypted = AesHelper.DecryptWithIV(encrypted, key);

            Assert.NotNull(encrypted);
            Assert.Equal(plaintext, decrypted);
        }

        /// <summary>
        /// 测试 RSA 加密解密
        /// </summary>
        [Fact]
        public void Test_Rsa_EncryptDecrypt()
        {
            var (publicKey, privateKey) = RsaHelper.GenerateKeyPair(2048);
            var plaintext = "RSA 加密测试数据";

            var encrypted = RsaHelper.Encrypt(plaintext, publicKey);
            var decrypted = RsaHelper.Decrypt(encrypted, privateKey);

            Assert.NotNull(encrypted);
            Assert.Equal(plaintext, decrypted);
        }

        /// <summary>
        /// 测试 RSA 签名验证
        /// </summary>
        [Fact]
        public void Test_Rsa_SignVerify()
        {
            var (publicKey, privateKey) = RsaHelper.GenerateKeyPair(2048);
            var data = "需要签名的数据";

            var signature = RsaHelper.Sign(data, privateKey);
            var isValid = RsaHelper.Verify(data, signature, publicKey);

            Assert.NotNull(signature);
            Assert.True(isValid);
        }

        /// <summary>
        /// 测试 RSA 签名篡改检测
        /// </summary>
        [Fact]
        public void Test_Rsa_TamperedSignature()
        {
            var (publicKey, privateKey) = RsaHelper.GenerateKeyPair(2048);
            var data = "原始数据";
            var tamperedData = "篡改数据";

            var signature = RsaHelper.Sign(data, privateKey);
            var isValid = RsaHelper.Verify(tamperedData, signature, publicKey);

            Assert.False(isValid);
        }
        #endregion
    }
}
