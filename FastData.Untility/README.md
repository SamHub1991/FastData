# FastData.Untility

FastData.Untility 是 FastData 生态系统的通用工具库，提供日志、加密、HTTP、Excel、Redis、缓存等功能。

## 目标框架

| 框架 | 说明 |
|------|------|
| `net45` | .NET Framework 4.5 |
| `net6.0` / `net8.0` / `net10.0` | Modern .NET |

## 安装

```bash
dotnet add package FastUntility
```

## 功能模块

### Redis 缓存

分布式缓存和消息队列：

```csharp
// 设置缓存
RedisInfo.Set("user:1", user, 3600);

// 获取缓存
var cached = RedisInfo.Get<User>("user:1");

// 删除缓存
RedisInfo.Remove("user:1");

// 缓存穿透防护
var user = RedisInfo.GetOrAdd("user:1", () => db.FindUser(1), hours: 24);
```

### 消息队列

支持 ReliableQueue 和 Stream：

```csharp
// 创建工厂
var factory = new MessageQueueFactory(redis);

// ReliableQueue（单消费，削峰）
var producer = factory.CreateReliableProducer("fastdata");
producer.Publish("topic:sensor", data);

// Stream（多消费组，解耦）
var streamProducer = factory.CreateStreamProducer("fastdata");
streamProducer.Publish("topic:realtime", data);
```

### 日志

```csharp
// 文件日志
LogHelper.Info("User created", "UserService");
LogHelper.Error("Database error", exception, "UserService");

// 数据库日志
LogHelper.DbInfo("SQL executed", sql, parameters);
```

### 加密

```csharp
// MD5
var hash = CryptoHelper.MD5("password");

// AES
var encrypted = CryptoHelper.AES.Encrypt(data, key);
var decrypted = CryptoHelper.AES.Decrypt(encrypted, key);

// RSA
var (publicKey, privateKey) = RSAHelper.GenerateKeys(2048);
var signature = RSAHelper.Sign(data, privateKey);
var isValid = RSAHelper.Verify(data, signature, publicKey);
```

### HTTP 客户端

```csharp
// GET
var response = await HttpHelper.GetAsync<User>("https://api.example.com/users/1");

// POST
var user = new User { Name = "张三" };
var result = await HttpHelper.PostAsync<User>("https://api.example.com/users", user);
```

### Excel 操作

```csharp
// 导出
ExcelHelper.Export(users, "users.xlsx");

// 导入
var data = ExcelHelper.Import<User>("users.xlsx");
```

### 缓存工具类

提供 CacheHelper 和 CacheKey 工具：

```csharp
// 使用 CacheKey 生成器
var cacheKey = CacheKey.ForEntity<User>(1);
var cacheKey = CacheKey.ForList<User>(u => u.IsActive);
var cacheKey = CacheKey.ForCount<User>(u => u.Age > 18);

// 使用 CacheHelper
CacheHelper.GetOrSet(cacheKey, () => db.FindUser(1), 300);
CacheHelper.Remove(cacheKey);
```

## 命名空间

| 命名空间 | 用途 |
|----------|------|
| `FastUntility` | 顶层入口（RedisInfo, LogHelper） |
| `FastUntility.Base` | 基础工具类 |
| `FastUntility.Cache` | 缓存工具（CacheHelper, CacheKey） |
| `FastUntility.Redis` | Redis 配置和操作 |
| `FastUntility.Security` | 加密和安全 |
| `FastUntility.Services` | 服务类 |
| `FastUntility.Repository` | Repository 模式 |
| `FastUntility.CodeGeneration` | 代码生成 |
| `FastUntility.Database` | 数据库元数据 |
| `FastUntility.Sync` | 数据同步 |
| `FastUntility.Messaging` | 消息队列 |

## 支持的数据库

| 数据库 | Provider Name |
|--------|---------------|
| SQL Server | `System.Data.SqlClient` |
| MySQL | `MySql.Data.MySqlClient` |
| SQLite | `System.Data.SQLite` |
| Oracle | `Oracle.ManagedDataAccess.Client` |
| DB2 | `IBM.Data.DB2.iSeries` |
| PostgreSQL | `Npgsql` |

## 依赖

- Newtonsoft.Json 13.0.3
- NPOI 2.5.6 / 2.7.0
- NServiceKit.Redis 1.0.17 (net45)
- NewLife.Redis 6.0.2024.1006 (net6.0+)

## 许可证

MIT License - see [LICENSE](../LICENSE) for details.
