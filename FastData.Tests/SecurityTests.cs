using FastUntility.Security;
using System;
using System.Threading;
using Xunit;

namespace FastData.Tests
{
    public class SecurityTests
    {
        #region Server Monitor 测试
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

        [Fact]
        public void Test_ServerMonitor_GetCpuUsage()
        {
            var cpuUsage = ServerMonitor.GetCpuUsage();
            Assert.True(cpuUsage >= 0 && cpuUsage <= 100);
        }

        [Fact]
        public void Test_ServerMonitor_GetMemoryInfo()
        {
            var (total, used, available, usage) = ServerMonitor.GetMemoryInfo();

            Assert.True(total > 0);
            Assert.True(used >= 0);
            Assert.True(available >= 0);
            Assert.True(usage >= 0 && usage <= 100);
        }

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

        [Fact]
        public void Test_ServerMonitor_GetUptime()
        {
            var uptime = ServerMonitor.GetUptime();
            Assert.True(uptime.TotalSeconds > 0);
        }
        #endregion

        #region JWT 测试
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

        [Fact]
        public void Test_Jwt_DecodeToken()
        {
            var secret = "my-secret-key-1234567890";
            var payload = JwtPayload.Create(subject: "user123", expiry: TimeSpan.FromHours(1));

            var token = JwtHelper.GenerateToken(payload, secret);
            var decodedPayload = JwtHelper.DecodeToken(token);

            Assert.Equal("user123", decodedPayload.Sub);
        }

        [Fact]
        public void Test_Jwt_InvalidSignature()
        {
            var secret = "my-secret-key-1234567890";
            var payload = JwtPayload.Create(subject: "user123", expiry: TimeSpan.FromHours(1));

            var token = JwtHelper.GenerateToken(payload, secret);

            Assert.Throws<UnauthorizedAccessException>(() => JwtHelper.ValidateToken(token, "wrong-secret"));
        }

        [Fact]
        public void Test_Jwt_ExpiredToken()
        {
            var secret = "my-secret-key-1234567890";
            var payload = JwtPayload.Create(subject: "user123", expiry: TimeSpan.FromHours(-1));

            var token = JwtHelper.GenerateToken(payload, secret);

            Assert.Throws<UnauthorizedAccessException>(() => JwtHelper.ValidateToken(token, secret));
        }

        [Fact]
        public void Test_Jwt_HS384Algorithm()
        {
            var secret = "my-secret-key-1234567890";
            var payload = JwtPayload.Create(subject: "user123", expiry: TimeSpan.FromHours(1));

            var token = JwtHelper.GenerateToken(payload, secret, "HS384");

            Assert.NotNull(token);

            var validatedPayload = JwtHelper.ValidateToken(token, secret);
            Assert.Equal("user123", validatedPayload.Sub);
        }

        [Fact]
        public void Test_Jwt_HS512Algorithm()
        {
            var secret = "my-secret-key-1234567890";
            var payload = JwtPayload.Create(subject: "user123", expiry: TimeSpan.FromHours(1));

            var token = JwtHelper.GenerateToken(payload, secret, "HS512");

            Assert.NotNull(token);

            var validatedPayload = JwtHelper.ValidateToken(token, secret);
            Assert.Equal("user123", validatedPayload.Sub);
        }
        #endregion

        #region AES 测试
        [Fact]
        public void Test_Aes_EncryptDecrypt()
        {
            var key = "12345678901234567890123456789012"; // 32 字节
            var iv = "1234567890123456"; // 16 字节
            var plainText = "Hello, World! 你好世界";

            var encrypted = AesHelper.Encrypt(plainText, key, iv);
            var decrypted = AesHelper.Decrypt(encrypted, key, iv);

            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Test_Aes_EncryptWithIV()
        {
            var key = "12345678901234567890123456789012";
            var plainText = "Hello, World! 你好世界";

            var encrypted = AesHelper.EncryptWithIV(plainText, key);
            var decrypted = AesHelper.DecryptWithIV(encrypted, key);

            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Test_Aes_GenerateKey()
        {
            var key = AesHelper.GenerateKey();
            Assert.NotNull(key);
            Assert.True(key.Length > 0);
        }

        [Fact]
        public void Test_Aes_GenerateIV()
        {
            var iv = AesHelper.GenerateIV();
            Assert.NotNull(iv);
            Assert.Equal(16, iv.Length);
        }

        [Fact]
        public void Test_Aes_NullPlainText()
        {
            Assert.Throws<ArgumentNullException>(() => AesHelper.Encrypt(null, "12345678901234567890123456789012"));
        }

        [Fact]
        public void Test_Aes_NullKey()
        {
            Assert.Throws<ArgumentNullException>(() => AesHelper.Encrypt("test", null));
        }
        #endregion

        #region RSA 测试
        [Fact]
        public void Test_Rsa_EncryptDecrypt()
        {
            var (publicKey, privateKey) = RsaHelper.GenerateKeyPair(2048);
            var plainText = "Hello, World! 你好世界";

            var encrypted = RsaHelper.Encrypt(plainText, publicKey);
            var decrypted = RsaHelper.Decrypt(encrypted, privateKey);

            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Test_Rsa_SignVerify()
        {
            var (publicKey, privateKey) = RsaHelper.GenerateKeyPair(2048);
            var data = "Important data to sign";

            var signature = RsaHelper.Sign(data, privateKey);
            var isValid = RsaHelper.Verify(data, signature, publicKey);

            Assert.True(isValid);
        }

        [Fact]
        public void Test_Rsa_InvalidSignature()
        {
            var (publicKey, privateKey) = RsaHelper.GenerateKeyPair(2048);
            var data = "Important data to sign";

            var signature = RsaHelper.Sign(data, privateKey);
            var isValid = RsaHelper.Verify("Modified data", signature, publicKey);

            Assert.False(isValid);
        }

        [Fact]
        public void Test_Rsa_GenerateKeyPair()
        {
            var (publicKey, privateKey) = RsaHelper.GenerateKeyPair();

            Assert.NotNull(publicKey);
            Assert.NotNull(privateKey);
            Assert.Contains("<RSAKeyValue>", publicKey);
            Assert.Contains("<RSAKeyValue>", privateKey);
        }
        #endregion

        #region HMAC 测试
        [Fact]
        public void Test_Hmac_Sha256()
        {
            var data = "Hello, World!";
            var key = "secret-key";

            var hash = HmacHelper.HmacSha256(data, key);

            Assert.NotNull(hash);
            Assert.Equal(64, hash.Length); // SHA256 = 32 bytes = 64 hex chars
        }

        [Fact]
        public void Test_Hmac_Sha384()
        {
            var data = "Hello, World!";
            var key = "secret-key";

            var hash = HmacHelper.HmacSha384(data, key);

            Assert.NotNull(hash);
            Assert.Equal(96, hash.Length); // SHA384 = 48 bytes = 96 hex chars
        }

        [Fact]
        public void Test_Hmac_Sha512()
        {
            var data = "Hello, World!";
            var key = "secret-key";

            var hash = HmacHelper.HmacSha512(data, key);

            Assert.NotNull(hash);
            Assert.Equal(128, hash.Length); // SHA512 = 64 bytes = 128 hex chars
        }

        [Fact]
        public void Test_Hmac_Md5()
        {
            var data = "Hello, World!";
            var key = "secret-key";

            var hash = HmacHelper.HmacMd5(data, key);

            Assert.NotNull(hash);
            Assert.Equal(32, hash.Length); // MD5 = 16 bytes = 32 hex chars
        }

        [Fact]
        public void Test_Hmac_Verify()
        {
            var data = "Hello, World!";
            var key = "secret-key";

            var hash = HmacHelper.HmacSha256(data, key);
            var isValid = HmacHelper.VerifyHmac(data, key, hash, "SHA256");

            Assert.True(isValid);
        }

        [Fact]
        public void Test_Hmac_VerifyFailed()
        {
            var data = "Hello, World!";
            var key = "secret-key";

            var hash = HmacHelper.HmacSha256(data, key);
            var isValid = HmacHelper.VerifyHmac("Modified data", key, hash, "SHA256");

            Assert.False(isValid);
        }
        #endregion

        #region API Key 测试
        [Fact]
        public void Test_ApiKey_Generate()
        {
            var apiKey = ApiKeyHelper.GenerateApiKey();

            Assert.NotNull(apiKey);
            Assert.Equal(32, apiKey.Length);
        }

        [Fact]
        public void Test_ApiKey_GenerateWithPrefix()
        {
            var apiKey = ApiKeyHelper.GenerateApiKey("fast");

            Assert.NotNull(apiKey);
            Assert.StartsWith("fast_", apiKey);
        }

        [Fact]
        public void Test_ApiKey_HashAndVerify()
        {
            var apiKey = ApiKeyHelper.GenerateApiKey();
            var hashedKey = ApiKeyHelper.HashApiKey(apiKey);

            Assert.NotNull(hashedKey);
            Assert.True(ApiKeyHelper.VerifyApiKey(apiKey, hashedKey));
        }

        [Fact]
        public void Test_ApiKey_VerifyFailed()
        {
            var apiKey = ApiKeyHelper.GenerateApiKey();
            var hashedKey = ApiKeyHelper.HashApiKey(apiKey);

            Assert.False(ApiKeyHelper.VerifyApiKey("wrong-key", hashedKey));
        }

        [Fact]
        public void Test_ApiKey_UniqueKeys()
        {
            var key1 = ApiKeyHelper.GenerateApiKey();
            var key2 = ApiKeyHelper.GenerateApiKey();

            Assert.NotEqual(key1, key2);
        }
        #endregion
    }
}
