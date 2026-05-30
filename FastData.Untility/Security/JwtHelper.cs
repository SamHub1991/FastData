using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FastUntility.Security
{
    /// <summary>
    /// JWT Token 工具
    /// </summary>
    public static class JwtHelper
    {
        /// <summary>
        /// 生成 JWT Token
        /// </summary>
        public static string GenerateToken(JwtPayload payload, string secret, string algorithm = "HS256")
        {
            var header = new Dictionary<string, object>
            {
                { "alg", algorithm },
                { "typ", "JWT" }
            };

            var headerJson = JsonSerializer.Serialize(header);
            var payloadJson = JsonSerializer.Serialize(payload);

            var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            var signature = algorithm switch
            {
                "HS256" => ComputeHmacSha256($"{headerBase64}.{payloadBase64}", secret),
                "HS384" => ComputeHmacSha384($"{headerBase64}.{payloadBase64}", secret),
                "HS512" => ComputeHmacSha512($"{headerBase64}.{payloadBase64}", secret),
                _ => throw new NotSupportedException($"不支持的算法: {algorithm}")
            };

            return $"{headerBase64}.{payloadBase64}.{signature}";
        }

        /// <summary>
        /// 验证并解析 JWT Token
        /// </summary>
        public static JwtPayload ValidateToken(string token, string secret, bool validateExpiry = true)
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                throw new ArgumentException("无效的 JWT Token 格式");

            var headerBase64 = parts[0];
            var payloadBase64 = parts[1];
            var signature = parts[2];

            // 解析 header 获取算法
            var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(headerBase64));
            var header = JsonSerializer.Deserialize<Dictionary<string, object>>(headerJson);
            var algorithm = header.ContainsKey("alg") ? header["alg"].ToString() : "HS256";

            // 验证签名
            var expectedSignature = algorithm switch
            {
                "HS256" => ComputeHmacSha256($"{headerBase64}.{payloadBase64}", secret),
                "HS384" => ComputeHmacSha384($"{headerBase64}.{payloadBase64}", secret),
                "HS512" => ComputeHmacSha512($"{headerBase64}.{payloadBase64}", secret),
                _ => throw new NotSupportedException($"不支持的算法: {algorithm}")
            };

            if (signature != expectedSignature)
                throw new UnauthorizedAccessException("JWT Token 签名无效");

            // 解析 payload
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadBase64));
            var payload = JsonSerializer.Deserialize<JwtPayload>(payloadJson);

            // 验证过期时间
            if (validateExpiry && payload.Exp.HasValue)
            {
                var expiry = DateTimeOffset.FromUnixTimeSeconds(payload.Exp.Value);
                if (expiry < DateTimeOffset.UtcNow)
                    throw new UnauthorizedAccessException("JWT Token 已过期");
            }

            return payload;
        }

        /// <summary>
        /// 解析 JWT Token（不验证签名）
        /// </summary>
        public static JwtPayload DecodeToken(string token)
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                throw new ArgumentException("无效的 JWT Token 格式");

            var payloadBase64 = parts[1];
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadBase64));
            return JsonSerializer.Deserialize<JwtPayload>(payloadJson);
        }

        /// <summary>
        /// 计算 HMAC-SHA256
        /// </summary>
        private static string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Base64UrlEncode(hash);
        }

        /// <summary>
        /// 计算 HMAC-SHA384
        /// </summary>
        private static string ComputeHmacSha384(string data, string key)
        {
            using var hmac = new HMACSHA384(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Base64UrlEncode(hash);
        }

        /// <summary>
        /// 计算 HMAC-SHA512
        /// </summary>
        private static string ComputeHmacSha512(string data, string key)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Base64UrlEncode(hash);
        }

        /// <summary>
        /// Base64 URL 编码
        /// </summary>
        private static string Base64UrlEncode(byte[] data)
        {
            var base64 = Convert.ToBase64String(data);
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        /// <summary>
        /// Base64 URL 解码
        /// </summary>
        private static byte[] Base64UrlDecode(string base64Url)
        {
            var base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }

    /// <summary>
    /// JWT Payload
    /// </summary>
    public class JwtPayload
    {
        /// <summary>
        /// 发行者
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("iss")]
        public string Iss { get; set; }

        /// <summary>
        /// 主题
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("sub")]
        public string Sub { get; set; }

        /// <summary>
        /// 受众
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("aud")]
        public string Aud { get; set; }

        /// <summary>
        /// 过期时间（Unix 时间戳）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("exp")]
        public long? Exp { get; set; }

        /// <summary>
        /// 生效时间（Unix 时间戳）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("nbf")]
        public long? Nbf { get; set; }

        /// <summary>
        /// 签发时间（Unix 时间戳）
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("iat")]
        public long? Iat { get; set; }

        /// <summary>
        /// JWT ID
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("jti")]
        public string Jti { get; set; }

        /// <summary>
        /// 自定义声明
        /// </summary>
        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, object> Claims { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 创建默认 Payload
        /// </summary>
        public static JwtPayload Create(string subject, string issuer = null, string audience = null, 
            TimeSpan? expiry = null, Dictionary<string, object> claims = null)
        {
            var payload = new JwtPayload
            {
                Sub = subject,
                Iss = issuer,
                Aud = audience,
                Iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Jti = Guid.NewGuid().ToString()
            };

            if (expiry.HasValue)
                payload.Exp = DateTimeOffset.UtcNow.Add(expiry.Value).ToUnixTimeSeconds();

            if (claims != null)
                payload.Claims = claims;

            return payload;
        }
    }
}
