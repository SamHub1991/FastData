using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FastUntility.Base;

namespace FastUntility.Security
{
    /// <summary>
    /// AES 加解密工具
    /// </summary>
    public static class AesHelper
    {
        /// <summary>
        /// AES 加密
        /// </summary>
        public static string Encrypt(string plainText, string key, string iv = null)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32, '\0').Substring(0, 32));
            aes.IV = !string.IsNullOrEmpty(iv) 
                ? Encoding.UTF8.GetBytes(iv.PadRight(16, '\0').Substring(0, 16))
                : GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// AES 解密
        /// </summary>
        public static string Decrypt(string cipherText, string key, string iv = null)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32, '\0').Substring(0, 32));
            aes.IV = !string.IsNullOrEmpty(iv)
                ? Encoding.UTF8.GetBytes(iv.PadRight(16, '\0').Substring(0, 16))
                : GenerateIV(); // 使用与加密相同的 IV

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// AES 加密（带完整 IV）
        /// </summary>
        public static string EncryptWithIV(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32, '\0').Substring(0, 32));
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            // 写入 IV
            ms.Write(aes.IV, 0, aes.IV.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// AES 解密（带完整 IV）
        /// </summary>
        public static string DecryptWithIV(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var cipherBytes = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32, '\0').Substring(0, 32));

            // 读取 IV
            var iv = new byte[16];
            Array.Copy(cipherBytes, 0, iv, 0, 16);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// 生成随机 IV
        /// </summary>
        public static byte[] GenerateIV()
        {
            using var aes = Aes.Create();
            aes.GenerateIV();
            return aes.IV;
        }

        /// <summary>
        /// 生成随机密钥
        /// </summary>
        public static string GenerateKey(int keySize = 256)
        {
            using var aes = Aes.Create();
            aes.KeySize = keySize;
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }
    }

    /// <summary>
    /// RSA 加解密工具
    /// </summary>
    public static class RsaHelper
    {
        /// <summary>
        /// 生成 RSA 密钥对
        /// </summary>
        public static (string PublicKey, string PrivateKey) GenerateKeyPair(int keySize = 2048)
        {
            using var rsa = RSA.Create();
            rsa.KeySize = keySize;
            var publicKey = rsa.ToXmlString(false);
            var privateKey = rsa.ToXmlString(true);
            return (publicKey, privateKey);
        }

        /// <summary>
        /// RSA 加密
        /// </summary>
        public static string Encrypt(string plainText, string publicKey)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentNullException(nameof(publicKey));

            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKey);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = FrameworkCompat.RsaEncrypt(plainBytes, rsa);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// RSA 解密
        /// </summary>
        public static string Decrypt(string cipherText, string privateKey)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));
            if (string.IsNullOrEmpty(privateKey))
                throw new ArgumentNullException(nameof(privateKey));

            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKey);
            var cipherBytes = Convert.FromBase64String(cipherText);
            var decryptedBytes = FrameworkCompat.RsaDecrypt(cipherBytes, rsa);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// RSA 签名
        /// </summary>
        public static string Sign(string data, string privateKey, string hashAlgorithm = "SHA256")
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(privateKey))
                throw new ArgumentNullException(nameof(privateKey));

            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = FrameworkCompat.RsaSignData(dataBytes, hashAlgorithm, rsa);
            return Convert.ToBase64String(signatureBytes);
        }

        /// <summary>
        /// RSA 验证签名
        /// </summary>
        public static bool Verify(string data, string signature, string publicKey, string hashAlgorithm = "SHA256")
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(signature))
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentNullException(nameof(publicKey));

            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);
            return FrameworkCompat.RsaVerifyData(dataBytes, signatureBytes, hashAlgorithm, rsa);
        }
    }

    /// <summary>
    /// HMAC 工具
    /// </summary>
    public static class HmacHelper
    {
        /// <summary>
        /// HMAC-SHA256 签名
        /// </summary>
        public static string HmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return FrameworkCompat.ToHexString(hash);
        }

        /// <summary>
        /// HMAC-SHA384 签名
        /// </summary>
        public static string HmacSha384(string data, string key)
        {
            using var hmac = new HMACSHA384(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return FrameworkCompat.ToHexString(hash);
        }

        /// <summary>
        /// HMAC-SHA512 签名
        /// </summary>
        public static string HmacSha512(string data, string key)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return FrameworkCompat.ToHexString(hash);
        }

        /// <summary>
        /// HMAC-MD5 签名
        /// </summary>
        public static string HmacMd5(string data, string key)
        {
            using var hmac = new HMACMD5(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return FrameworkCompat.ToHexString(hash);
        }

        /// <summary>
        /// 验证 HMAC 签名
        /// </summary>
        public static bool VerifyHmac(string data, string key, string expectedHash, string algorithm = "SHA256")
        {
            var actualHash = algorithm.ToUpper() switch
            {
                "SHA256" => HmacSha256(data, key),
                "SHA384" => HmacSha384(data, key),
                "SHA512" => HmacSha512(data, key),
                "MD5" => HmacMd5(data, key),
                _ => throw new NotSupportedException($"不支持的算法: {algorithm}")
            };
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// API Key 工具
    /// </summary>
    public static class ApiKeyHelper
    {
        /// <summary>
        /// 生成 API Key
        /// </summary>
        public static string GenerateApiKey(int length = 32)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Substring(0, length);
        }

        /// <summary>
        /// 生成带前缀的 API Key
        /// </summary>
        public static string GenerateApiKey(string prefix, int length = 32)
        {
            var key = GenerateApiKey(length);
            return $"{prefix}_{key}";
        }

        /// <summary>
        /// 哈希 API Key（用于存储）
        /// </summary>
        public static string HashApiKey(string apiKey)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// 验证 API Key
        /// </summary>
        public static bool VerifyApiKey(string apiKey, string hashedApiKey)
        {
            var hashedInput = HashApiKey(apiKey);
            return string.Equals(hashedInput, hashedApiKey, StringComparison.Ordinal);
        }
    }
}
