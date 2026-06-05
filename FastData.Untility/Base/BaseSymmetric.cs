using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FastUntility.Base
{
    /// <summary>
    /// 对称加密处理类
    /// 提供 AES 加密解密、MD5 哈希功能
    /// </summary>
    public static class BaseSymmetric
    {
        private const string AesKey = "Guz(%&hj7x89H$yuBI012345maT5&fvHUFCy76*h%(HilJ$lhj!y6&(*jkP~!@#$";
        private const string AesIV = "E4ghj*Ghg7!rNIfb&95GUY86GfghUb#er57HBh(u%g6HJ($jhWk7&!~!@#$%^&*(";
        private const string DesKey = "Weizz_2015";

        /// <summary>
        /// 获取合法的 AES 密钥（按算法要求长度截断或补齐）
        /// </summary>
        /// <param name="aes">AES 实例</param>
        /// <returns>符合密钥长度要求的字节数组</returns>
        private static byte[] GetLegalKey(Aes aes)
        {
            aes.GenerateKey();
            var keyLength = aes.Key.Length;

            var key = AesKey;
            if (key.Length > keyLength)
                key = key.Substring(0, keyLength);
            else if (key.Length < keyLength)
                key = key.PadRight(keyLength, ' ');

            return Encoding.ASCII.GetBytes(key);
        }

        /// <summary>
        /// 获取合法的 AES 初始化向量（按算法要求长度截断或补齐）
        /// </summary>
        /// <param name="aes">AES 实例</param>
        /// <returns>符合 IV 长度要求的字节数组</returns>
        private static byte[] GetLegalIV(Aes aes)
        {
            aes.GenerateIV();
            var ivLength = aes.IV.Length;

            var iv = AesIV;
            if (iv.Length > ivLength)
                iv = iv.Substring(0, ivLength);
            else if (iv.Length < ivLength)
                iv = iv.PadRight(ivLength, ' ');

            return Encoding.ASCII.GetBytes(iv);
        }

        /// <summary>
        /// AES 加密
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <returns>Base64 编码的密文</returns>
        public static string Encrypto(string source)
        {
            if (source == null)
                return string.Empty;

            var inputBytes = Encoding.UTF8.GetBytes(source);

            using (var aes = Aes.Create())
            {
                aes.Key = GetLegalKey(aes);
                aes.IV = GetLegalIV(aes);

                using (var encryptor = aes.CreateEncryptor())
                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(inputBytes, 0, inputBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        /// <summary>
        /// AES 解密
        /// </summary>
        /// <param name="source">Base64 编码的密文</param>
        /// <param name="defaultValue">解密失败时的默认返回值</param>
        /// <returns>解密后的明文，失败时返回 defaultValue</returns>
        public static string Decrypto(string source, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(source))
                return defaultValue;

            try
            {
                var inputBytes = Convert.FromBase64String(source);

                using (var aes = Aes.Create())
                using (var memoryStream = new MemoryStream(inputBytes, 0, inputBytes.Length))
                {
                    aes.Key = GetLegalKey(aes);
                    aes.IV = GetLegalIV(aes);

                    using (var decryptor = aes.CreateDecryptor())
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    using (var streamReader = new StreamReader(cryptoStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// DES 加密（已废弃，建议使用 AES）
        /// </summary>
        /// <param name="source">待加密的明文（GB2312 编码）</param>
        /// <returns>十六进制编码的密文</returns>
        [Obsolete("DES 算法已不安全，请使用 AES (Encrypto/Decrypto)")]
        public static string EncodeGB2312(string source)
        {
#pragma warning disable SYSLIB0021
            using (var provider = new DESCryptoServiceProvider())
#pragma warning restore SYSLIB0021
            {
                var keyBytes = Encoding.ASCII.GetBytes(DesKey.Substring(0, 8));
                provider.Key = keyBytes;
                provider.IV = keyBytes;

                var inputBytes = Encoding.GetEncoding("GB2312").GetBytes(source);

                using (var memoryStream = new MemoryStream())
                using (var cryptoStream = new CryptoStream(memoryStream, provider.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(inputBytes, 0, inputBytes.Length);
                    cryptoStream.FlushFinalBlock();

                    var result = new StringBuilder();
                    foreach (var b in memoryStream.ToArray())
                    {
                        result.AppendFormat("{0:X2}", b);
                    }
                    return result.ToString();
                }
            }
        }

        /// <summary>
        /// DES 解密（已废弃，建议使用 AES）
        /// </summary>
        /// <param name="source">十六进制编码的密文</param>
        /// <param name="defaultValue">解密失败时的默认返回值</param>
        /// <returns>解密后的明文（GB2312 编码）</returns>
        [Obsolete("DES 算法已不安全，请使用 AES (Encrypto/Decrypto)")]
        public static string DecodeGB2312(string source, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(source))
                return defaultValue;

            try
            {
#pragma warning disable SYSLIB0021
                using (var provider = new DESCryptoServiceProvider())
#pragma warning restore SYSLIB0021
                {
                    var keyBytes = Encoding.ASCII.GetBytes(DesKey.Substring(0, 8));
                    provider.Key = keyBytes;
                    provider.IV = keyBytes;

                    var bufferLength = source.Length / 2;
                    var buffer = new byte[bufferLength];
                    for (var i = 0; i < bufferLength; i++)
                    {
                        buffer[i] = (byte)Convert.ToInt32(source.Substring(i * 2, 2), 16);
                    }

                    using (var memoryStream = new MemoryStream())
                    using (var cryptoStream = new CryptoStream(memoryStream, provider.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(buffer, 0, buffer.Length);
                        cryptoStream.FlushFinalBlock();
                        return Encoding.GetEncoding("GB2312").GetString(memoryStream.ToArray());
                    }
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// MD5 加密
        /// </summary>
        /// <param name="hashLength">哈希长度，16 或 32</param>
        /// <param name="source">待加密的明文</param>
        /// <returns>MD5 哈希值（小写十六进制）</returns>
        public static string Md5(int hashLength, string source)
        {
            if (source == null)
                return string.Empty;

            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(source);
                var hashBytes = md5.ComputeHash(inputBytes);

                var hashBuilder = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    hashBuilder.Append(b.ToString("x2"));
                }

                var fullHash = hashBuilder.ToString();

                if (hashLength == 16)
                {
                    return fullHash.Substring(8, 16);
                }

                return fullHash;
            }
        }

        /// <summary>
        /// 计算字符串的 MD5 哈希值（32 位）
        /// </summary>
        /// <param name="input">待哈希的字符串</param>
        /// <returns>MD5 哈希值（小写十六进制）</returns>
        public static string Generate(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using (var md5 = MD5.Create())
            {
                var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

                var hashBuilder = new StringBuilder(hashBytes.Length * 2);
                for (var i = 0; i < hashBytes.Length; i++)
                {
                    hashBuilder.Append(hashBytes[i].ToString("x2"));
                }

                return hashBuilder.ToString();
            }
        }

        /// <summary>
        /// MD5 加密（旧方法名，为了向后兼容）
        /// </summary>
        /// <param name="code">哈希长度，16 或 32</param>
        /// <param name="Source">待加密的明文</param>
        /// <returns>MD5 哈希值（小写十六进制）</returns>
        public static string md5(int code, string Source)
        {
            return Md5(code, Source);
        }
    }
}
