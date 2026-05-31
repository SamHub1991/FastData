using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FastData;
using FastData.Example.Model;
using FastUntility.Page;
using Newtonsoft.Json;

namespace FastData.Example.Example
{
    /// <summary>
    /// API 认证与安全示例
    /// 
    /// 覆盖功能：
    /// - 统一返回数据格式（ApiResponse）
    /// - ToJson() 扩展
    /// - Token 认证
    /// - JWT 认证
    /// - RSA 加密
    /// - AES 加密
    /// - 模拟客户端请求
    /// </summary>
    public static class ApiAuthenticationExample
    {
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  API 认证与安全示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            RunUnifiedResponse();
            Console.WriteLine();
            RunTokenAuthentication();
            Console.WriteLine();
            RunJwtAuthentication();
            Console.WriteLine();
            RunRsaEncryption();
            Console.WriteLine();
            RunAesEncryption();
            Console.WriteLine();
            RunClientRequestSimulation();
        }

        #region 统一返回格式

        /// <summary>
        /// 统一 API 响应格式
        /// </summary>
        public class ApiResponse<T>
        {
            /// <summary>
            /// 状态码
            /// </summary>
            public int Code { get; set; }

            /// <summary>
            /// 消息
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// 数据
            /// </summary>
            public T Data { get; set; }

            /// <summary>
            /// 时间戳
            /// </summary>
            public long Timestamp { get; set; }

            /// <summary>
            /// 请求ID
            /// </summary>
            public string RequestId { get; set; }

            public ApiResponse()
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                RequestId = Guid.NewGuid().ToString("N").Substring(0, 8);
            }

            public static ApiResponse<T> Success(T data, string message = "success")
            {
                return new ApiResponse<T>
                {
                    Code = 200,
                    Message = message,
                    Data = data
                };
            }

            public static ApiResponse<T> Error(int code, string message)
            {
                return new ApiResponse<T>
                {
                    Code = code,
                    Message = message,
                    Data = default
                };
            }
        }

        /// <summary>
        /// ToJson 扩展方法
        /// </summary>
        public static string ToJson(this object obj, bool indented = false)
        {
            if (obj == null) return "null";
            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// 演示统一返回格式
        /// </summary>
        private static void RunUnifiedResponse()
        {
            Console.WriteLine("【1】统一返回格式");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 成功响应 - 返回用户列表
                var users = FastRead.Query<User>(u => u.IsActive)
                    .Take(5)
                    .ToList<User>();

                var successResponse = ApiResponse<List<User>>.Success(users, "查询成功");
                Console.WriteLine($"  成功响应: {successResponse.ToJson()}");

                // 2. 成功响应 - 返回单个用户
                var user = FastRead.Query<User>(u => u.Id == 1)
                    .FirstOrDefault<User>();

                if (user != null)
                {
                    var singleResponse = ApiResponse<User>.Success(user);
                    Console.WriteLine($"  单条响应: {singleResponse.ToJson()}");
                }

                // 3. 错误响应
                var errorResponse = ApiResponse<object>.Error(404, "用户不存在");
                Console.WriteLine($"  错误响应: {errorResponse.ToJson()}");

                // 4. 分页响应
                var pageData = FastRead.Query<User>(u => u.IsActive)
                    .ToPage<User>(new PageModel { PageId = 1, PageSize = 10 });

                var pageResponse = ApiResponse<object>.Success(new
                {
                    Total = pageData.pModel.TotalRecord,
                    Page = pageData.pModel.PageId,
                    PageSize = pageData.pModel.PageSize,
                    Items = pageData.list
                });
                Console.WriteLine($"  分页响应: {pageResponse.ToJson().Substring(0, Math.Min(200, pageResponse.ToJson().Length))}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion

        #region Token 认证

        /// <summary>
        /// Token 认证模拟
        /// 
        /// 流程：
        /// 1. 客户端发送用户名/密码
        /// 2. 服务器验证后生成 Token
        /// 3. 客户端携带 Token 请求 API
        /// 4. 服务器验证 Token
        /// </summary>
        private static void RunTokenAuthentication()
        {
            Console.WriteLine("【2】Token 认证");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 模拟用户登录获取 Token
                var token = GenerateToken("admin", "admin123");
                Console.WriteLine($"  生成 Token: {token.Substring(0, 32)}...");

                // 2. 验证 Token
                var isValid = ValidateToken(token);
                Console.WriteLine($"  Token 验证: {(isValid ? "有效" : "无效")}");

                // 3. 从 Token 解析用户信息
                var userInfo = ParseToken(token);
                Console.WriteLine($"  解析用户: {userInfo?.UserName}, 过期时间: {userInfo?.ExpireTime}");

                // 4. 模拟携带 Token 的 API 请求
                var request = new ApiRequest
                {
                    Token = token,
                    Method = "GET",
                    Path = "/api/users",
                    Body = null
                };

                var response = ProcessRequest(request);
                Console.WriteLine($"  API 响应: {response.ToJson()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成 Token
        /// </summary>
        private static string GenerateToken(string username, string password)
        {
            // 简单 Token: 用户名 + 过期时间 + 签名
            var expireTime = DateTime.UtcNow.AddHours(2);
            var payload = $"{username}|{expireTime.Ticks}";
            var signature = ComputeSha256Hash(payload + "secret_key");
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}|{signature}"));
        }

        /// <summary>
        /// 验证 Token
        /// </summary>
        private static bool ValidateToken(string token)
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split('|');
                if (parts.Length != 3) return false;

                var payload = $"{parts[0]}|{parts[1]}";
                var signature = parts[2];
                var expectedSignature = ComputeSha256Hash(payload + "secret_key");

                if (signature != expectedSignature) return false;

                var expireTime = new DateTime(long.Parse(parts[1]), DateTimeKind.Utc);
                return expireTime > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析 Token
        /// </summary>
        private static TokenInfo ParseToken(string token)
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split('|');
                return new TokenInfo
                {
                    UserName = parts[0],
                    ExpireTime = new DateTime(long.Parse(parts[1]), DateTimeKind.Utc).ToLocalTime()
                };
            }
            catch
            {
                return null;
            }
        }

        private class TokenInfo
        {
            public string UserName { get; set; }
            public DateTime ExpireTime { get; set; }
        }

        #endregion

        #region JWT 认证

        /// <summary>
        /// JWT 认证模拟
        /// 
        /// JWT 结构: Header.Payload.Signature
        /// - Header: 算法类型
        /// - Payload: 用户信息
        /// - Signature: 签名
        /// </summary>
        private static void RunJwtAuthentication()
        {
            Console.WriteLine("【3】JWT 认证");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 生成 JWT
                var jwt = GenerateJwt("admin", new Dictionary<string, object>
                {
                    { "role", "admin" },
                    { "permissions", new[] { "read", "write", "delete" } }
                });
                Console.WriteLine($"  生成 JWT: {jwt.Substring(0, 50)}...");

                // 2. 验证 JWT
                var jwtValid = ValidateJwt(jwt);
                Console.WriteLine($"  JWT 验证: {(jwtValid ? "有效" : "无效")}");

                // 3. 解析 JWT Payload
                var payload = ParseJwt(jwt);
                Console.WriteLine($"  JWT Payload:");
                foreach (var kv in payload)
                {
                    Console.WriteLine($"    {kv.Key}: {kv.Value}");
                }

                // 4. 模拟 JWT 认证的 API 请求
                var jwtRequest = new ApiRequest
                {
                    Authorization = $"Bearer {jwt}",
                    Method = "POST",
                    Path = "/api/orders",
                    Body = new { userId = 1, productId = 100, quantity = 2 }
                };

                var jwtResponse = ProcessJwtRequest(jwtRequest);
                Console.WriteLine($"  JWT API 响应: {jwtResponse.ToJson()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成 JWT
        /// </summary>
        private static string GenerateJwt(string username, Dictionary<string, object> claims)
        {
            // Header
            var header = new { alg = "HS256", typ = "JWT" };
            var headerJson = JsonConvert.SerializeObject(header);
            var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson));

            // Payload
            var payload = new Dictionary<string, object>(claims)
            {
                { "sub", username },
                { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "exp", DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds() }
            };
            var payloadJson = JsonConvert.SerializeObject(payload);
            var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

            // Signature
            var signatureInput = $"{headerBase64}.{payloadBase64}";
            var signature = ComputeHmacSha256(signatureInput, "jwt_secret_key");
            var signatureBase64 = Convert.ToBase64String(signature);

            return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
        }

        /// <summary>
        /// 验证 JWT
        /// </summary>
        private static bool ValidateJwt(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length != 3) return false;

                // 验证签名
                var signatureInput = $"{parts[0]}.{parts[1]}";
                var expectedSignature = Convert.ToBase64String(
                    ComputeHmacSha256(signatureInput, "jwt_secret_key"));

                if (parts[2] != expectedSignature) return false;

                // 验证过期时间
                var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson);
                var exp = Convert.ToInt64(payload["exp"]);
                var expireTime = DateTimeOffset.FromUnixTimeSeconds(exp);

                return expireTime > DateTimeOffset.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析 JWT Payload
        /// </summary>
        private static Dictionary<string, object> ParseJwt(string jwt)
        {
            var parts = jwt.Split('.');
            var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson);
        }

        #endregion

        #region RSA 加密

        /// <summary>
        /// RSA 加密示例
        /// 
        /// 用途：
        /// - 敏感数据传输加密
        /// - 数字签名
        /// - 密钥交换
        /// </summary>
        private static void RunRsaEncryption()
        {
            Console.WriteLine("【4】RSA 加密");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 生成 RSA 密钥对
                using var rsa = RSA.Create(2048);
                var publicKey = rsa.ExportRSAPublicKey();
                var privateKey = rsa.ExportRSAPrivateKey();

                Console.WriteLine($"  公钥长度: {publicKey.Length} 字节");
                Console.WriteLine($"  私钥长度: {privateKey.Length} 字节");

                // 2. RSA 加密
                var plainText = "这是一条敏感信息: 密码=Admin@123";
                var encrypted = RsaEncrypt(publicKey, plainText);
                Console.WriteLine($"  原文: {plainText}");
                Console.WriteLine($"  密文: {Convert.ToBase64String(encrypted).Substring(0, 50)}...");

                // 3. RSA 解密
                var decrypted = RsaDecrypt(privateKey, encrypted);
                Console.WriteLine($"  解密: {decrypted}");

                // 4. RSA 数字签名
                var dataToSign = "需要签名的数据";
                var signature = RsaSign(privateKey, dataToSign);
                Console.WriteLine($"  签名: {Convert.ToBase64String(signature).Substring(0, 50)}...");

                // 5. 验证签名
                var isSignatureValid = RsaVerify(publicKey, dataToSign, signature);
                Console.WriteLine($"  签名验证: {(isSignatureValid ? "有效" : "无效")}");

                // 6. 模拟 API 请求加密
                var apiRequest = new { userId = 1, amount = 1000.50m, timestamp = DateTime.UtcNow.Ticks };
                var requestJson = JsonConvert.SerializeObject(apiRequest);
                var encryptedRequest = RsaEncrypt(publicKey, requestJson);
                Console.WriteLine($"  加密请求: {Convert.ToBase64String(encryptedRequest).Substring(0, 50)}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        private static byte[] RsaEncrypt(byte[] publicKey, string data)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(publicKey, out _);
            return rsa.Encrypt(Encoding.UTF8.GetBytes(data), RSAEncryptionPadding.OaepSHA256);
        }

        private static string RsaDecrypt(byte[] privateKey, byte[] encryptedData)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);
            var decryptedBytes = rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        private static byte[] RsaSign(byte[] privateKey, string data)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(privateKey, out _);
            return rsa.SignData(Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        private static bool RsaVerify(byte[] publicKey, string data, byte[] signature)
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(publicKey, out _);
            return rsa.VerifyData(Encoding.UTF8.GetBytes(data), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        #endregion

        #region AES 加密

        /// <summary>
        /// AES 加密示例
        /// 
        /// 用途：
        /// - 大量数据加密
        /// - 数据库字段加密
        /// - 文件加密
        /// </summary>
        private static void RunAesEncryption()
        {
            Console.WriteLine("【5】AES 加密");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 生成 AES 密钥和 IV
                using var aes = Aes.Create();
                aes.GenerateKey();
                aes.GenerateIV();

                Console.WriteLine($"  密钥长度: {aes.Key.Length * 8} 位");
                Console.WriteLine($"  IV 长度: {aes.IV.Length * 8} 位");

                // 2. AES 加密
                var plainText = "用户敏感数据: 身份证号=110101199001011234, 手机号=13800138000";
                var encrypted = AesEncrypt(aes.Key, aes.IV, plainText);
                Console.WriteLine($"  原文: {plainText}");
                Console.WriteLine($"  密文: {Convert.ToBase64String(encrypted).Substring(0, 50)}...");

                // 3. AES 解密
                var decrypted = AesDecrypt(aes.Key, aes.IV, encrypted);
                Console.WriteLine($"  解密: {decrypted}");

                // 4. 模拟数据库字段加密
                var user = new User
                {
                    UserName = "张三",
                    Email = "zhangsan@example.com",
                    Phone = "13800138000"
                };

                // 加密敏感字段
                var encryptedPhone = AesEncrypt(aes.Key, aes.IV, user.Phone);
                Console.WriteLine($"  加密手机号: {Convert.ToBase64String(encryptedPhone)}");

                // 解密敏感字段
                var decryptedPhone = AesDecrypt(aes.Key, aes.IV, encryptedPhone);
                Console.WriteLine($"  解密手机号: {decryptedPhone}");

                // 5. 模拟 API 请求体加密
                var requestBody = JsonConvert.SerializeObject(new
                {
                    cardNo = "6222021234567890123",
                    amount = 5000.00m,
                    timestamp = DateTime.UtcNow.Ticks
                });

                var encryptedBody = AesEncrypt(aes.Key, aes.IV, requestBody);
                Console.WriteLine($"  加密请求体: {Convert.ToBase64String(encryptedBody).Substring(0, 50)}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        private static byte[] AesEncrypt(byte[] key, byte[] iv, string data)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new System.IO.MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var writer = new System.IO.StreamWriter(cs))
            {
                writer.Write(data);
            }
            return ms.ToArray();
        }

        private static string AesDecrypt(byte[] key, byte[] iv, byte[] encryptedData)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new System.IO.MemoryStream(encryptedData);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new System.IO.StreamReader(cs);
            return reader.ReadToEnd();
        }

        #endregion

        #region 模拟客户端请求

        /// <summary>
        /// API 请求模型
        /// </summary>
        private class ApiRequest
        {
            public string Token { get; set; }
            public string Authorization { get; set; }
            public string Method { get; set; }
            public string Path { get; set; }
            public object Body { get; set; }
            public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// 模拟客户端请求
        /// 
        /// 展示完整的 API 认证流程：
        /// 1. 获取 Token/JWT
        /// 2. 构建请求
        /// 3. 发送请求
        /// 4. 验证响应
        /// </summary>
        private static void RunClientRequestSimulation()
        {
            Console.WriteLine("【6】模拟客户端请求");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 1. 获取 Token
                var token = GenerateToken("client_app", "client_secret");
                Console.WriteLine($"  获取 Token: {token.Substring(0, 32)}...");

                // 2. 模拟多个 API 请求
                var requests = new[]
                {
                    new ApiRequest { Method = "GET", Path = "/api/users", Token = token },
                    new ApiRequest { Method = "GET", Path = "/api/users/1", Token = token },
                    new ApiRequest { Method = "POST", Path = "/api/users", Token = token, Body = new { UserName = "newuser", Email = "new@example.com" } },
                    new ApiRequest { Method = "PUT", Path = "/api/users/1", Token = token, Body = new { Email = "updated@example.com" } },
                    new ApiRequest { Method = "DELETE", Path = "/api/users/999", Token = token }
                };

                Console.WriteLine("  模拟 API 请求:");
                foreach (var request in requests)
                {
                    var response = SimulateApiClient(request);
                    Console.WriteLine($"    {request.Method} {request.Path} -> {response.Code}: {response.Message}");
                }

                // 3. 模拟带加密的请求
                Console.WriteLine("\n  带加密的请求:");
                using var aes = Aes.Create();
                aes.GenerateKey();
                aes.GenerateIV();

                var sensitiveData = new { password = "P@ssw0rd123", creditCard = "4111111111111111" };
                var encryptedBody = AesEncrypt(aes.Key, aes.IV, JsonConvert.SerializeObject(sensitiveData));

                var secureRequest = new ApiRequest
                {
                    Method = "POST",
                    Path = "/api/payments",
                    Token = token,
                    Headers = new Dictionary<string, string>
                    {
                        { "X-Encryption", "AES-256-CBC" },
                        { "X-Key", Convert.ToBase64String(aes.Key) },
                        { "X-IV", Convert.ToBase64String(aes.IV) }
                    },
                    Body = Convert.ToBase64String(encryptedBody)
                };

                var secureResponse = SimulateApiClient(secureRequest);
                Console.WriteLine($"    POST /api/payments (加密) -> {secureResponse.Code}: {secureResponse.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理 Token 认证请求
        /// </summary>
        private static ApiResponse<object> ProcessRequest(ApiRequest request)
        {
            // 验证 Token
            if (string.IsNullOrEmpty(request.Token) || !ValidateToken(request.Token))
            {
                return ApiResponse<object>.Error(401, "未授权: Token 无效或已过期");
            }

            // 模拟处理请求
            return ApiResponse<object>.Success(new { message = "请求成功", path = request.Path });
        }

        /// <summary>
        /// 处理 JWT 认证请求
        /// </summary>
        private static ApiResponse<object> ProcessJwtRequest(ApiRequest request)
        {
            // 验证 JWT
            if (string.IsNullOrEmpty(request.Authorization) || !request.Authorization.StartsWith("Bearer "))
            {
                return ApiResponse<object>.Error(401, "未授权: 缺少 Authorization 头");
            }

            var jwt = request.Authorization.Substring(7);
            if (!ValidateJwt(jwt))
            {
                return ApiResponse<object>.Error(401, "未授权: JWT 无效或已过期");
            }

            // 解析用户信息
            var payload = ParseJwt(jwt);
            var username = payload.ContainsKey("sub") ? payload["sub"].ToString() : "unknown";

            // 模拟处理请求
            return ApiResponse<object>.Success(new
            {
                message = "请求成功",
                user = username,
                path = request.Path,
                body = request.Body
            });
        }

        /// <summary>
        /// 模拟 API 客户端
        /// </summary>
        private static ApiResponse<object> SimulateApiClient(ApiRequest request)
        {
            // 验证 Token
            if (string.IsNullOrEmpty(request.Token))
            {
                return ApiResponse<object>.Error(401, "未授权: 缺少 Token");
            }

            if (!ValidateToken(request.Token))
            {
                return ApiResponse<object>.Error(401, "未授权: Token 无效或已过期");
            }

            // 模拟不同 API 端点的处理
            return request.Path switch
            {
                "/api/users" when request.Method == "GET" => ApiResponse<object>.Success(new { users = new[] { "user1", "user2" } }),
                "/api/users/1" when request.Method == "GET" => ApiResponse<object>.Success(new { id = 1, name = "张三" }),
                "/api/users" when request.Method == "POST" => ApiResponse<object>.Success(new { id = 100, message = "创建成功" }),
                "/api/users/1" when request.Method == "PUT" => ApiResponse<object>.Success(new { message = "更新成功" }),
                "/api/users/999" when request.Method == "DELETE" => ApiResponse<object>.Success(new { message = "删除成功" }),
                "/api/payments" when request.Method == "POST" => ApiResponse<object>.Success(new { transactionId = Guid.NewGuid().ToString("N"), message = "支付成功" }),
                _ => ApiResponse<object>.Error(404, "接口不存在")
            };
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// SHA256 哈希
        /// </summary>
        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// HMAC-SHA256 签名
        /// </summary>
        private static byte[] ComputeHmacSha256(string input, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        #endregion
    }
}
