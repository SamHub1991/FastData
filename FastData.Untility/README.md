# FastData.Untility

FastData.Untility 是 FastData 生态系统的通用工具库，提供日志、加密、HTTP、Excel、Redis、缓存等功能。

**最新更新 (2026-05-31)**:
- ✅ 新增 `Base/DateHelper.cs` - 日期时间工具类
- ✅ 新增 `Base/CollectionHelper.cs` - 集合扩展工具类
- ✅ 新增 `Page/ApiResponse.cs` - 统一 API 响应格式
- ✅ 新增 `Page/Result.cs` - 统一结果类型

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

### 日期时间工具 (新增)

```csharp
// 时间戳转换
var ts = DateHelper.GetTimestamp();
var tsMs = DateHelper.GetTimestampMs();
var dt = DateHelper.FromTimestamp(ts);

// 相对时间
var ago = DateHelper.GetRelativeTime(DateTime.Now.AddMinutes(-5)); // "5 分钟前"
var future = DateHelper.GetRelativeTimeFuture(DateTime.Now.AddHours(2)); // "2 小时后"

// 日期计算
var dayStart = DateHelper.GetDayStart(DateTime.Now);
var weekStart = DateHelper.GetWeekStart(DateTime.Now);
var monthStart = DateHelper.GetMonthStart(DateTime.Now);
var quarterStart = DateHelper.GetQuarterStart(DateTime.Now);
var yearStart = DateHelper.GetYearStart(DateTime.Now);

// 格式化
var chineseDate = DateHelper.ToChineseDate(DateTime.Now);
var friendlyTime = DateHelper.ToFriendlyDateTime(DateTime.Now);
```

### 集合工具 (新增)

```csharp
// 空值安全
list.IsNullOrEmpty(); // true/false
list.HasValue(); // true/false

// 安全遍历
list.ForEachSafe(item => Console.WriteLine(item));
list.ForEachSafe((item, index) => Console.WriteLine($"{index}: {item}"));

// 分页
var page = list.Page(pageIndex: 1, pageSize: 10);
var (data, total) = list.PageWithTotal(1, 10);

// 去重与分组
var distinct = list.DistinctBy(x => x.Id);
var grouped = list.GroupToDictionary(x => x.Category);

// 批量操作
list.Batch(100).ForEach(batch => ProcessBatch(batch));
list.BatchExecute(100, batch => SaveBatch(batch));

// 统计
var sum = list.SumSafe(x => x.Amount);
var max = list.MaxSafe(x => x.Price, defaultValue: 0);
var avg = list.AverageSafe(x => x.Quantity);

// 随机
var shuffled = list.Shuffle();
var randomItem = list.Random();
var randomItems = list.Random(5);
```

### 统一 API 响应 (新增)

```csharp
// 成功响应
ApiResponse.Ok(data, "操作成功");
ApiResponse<T>.Ok(data, "操作成功");

// 失败响应
ApiResponse.Fail("错误信息");
ApiResponse<T>.Fail("错误信息", code: -1);

// 快捷响应
ApiResponse.NotFound("数据不存在");
ApiResponse.Unauthorized("未授权");
ApiResponse.Forbidden("禁止访问");
```

### 统一结果类型 (新增)

```csharp
// 成功
Result.Ok("操作成功");
Result<T>.Ok(data, "操作成功");

// 失败
Result.Fail("错误信息", errorCode: "CODE_001");
Result<T>.Fail(ex, errorCode: "CODE_002");

// 转换
result.ToResult(data); // 转为带数据的结果
result.ToApiResponse(); // 转为 ApiResponse
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
