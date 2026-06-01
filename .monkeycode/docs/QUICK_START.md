# FastData ORM 快速启动指南

## 5 分钟快速上手

### 1. 基本查询

```csharp
using FastData;

// 查询所有用户
var users = FastRead.Query<User>().ToList();

// 条件查询
var activeUsers = FastRead.Query<User>(u => u.IsActive == true).ToList();

// 分页查询
var (items, total) = FastRead.Query<User>(u => u.Age > 18)
    .ToPage(new PageModel { PageId = 1, PageSize = 20 });
```

### 2. 基本写入

```csharp
// 添加用户
var user = new User { Name = "张三", Email = "zhangsan@example.com", Age = 25 };
var result = FastWrite.Add(user);

if (result.IsSuccess())
{
    Console.WriteLine("添加成功");
}
else
{
    Console.WriteLine($"添加失败: {result.GetErrorMessage()}");
}

// 批量插入（高性能）
var users = new List<User> { /* 10,000 条数据 */ };
var bulkResult = FastWrite.BulkInsert(users);
```

### 3. 异步操作

```csharp
// 异步查询
var users = await FastReadAsync.ToListAsync<User>(u => u.IsActive == true);

// 异步批量插入
await FastWriteAsync.BulkInsertAsync(users);
```

### 4. 扩展方法

```csharp
// 链式条件
var query = FastRead.Query<User>(u => true)
    .WhereIf(hasNameFilter, q => q.Where<User>(u => u.Name.Contains("张")))
    .WhereIf(hasAgeFilter, q => q.Where<User>(u => u.Age > 18));

// 重试机制
var result = new Func<List<User>>(() => 
    FastRead.Query<User>(u => true).ToList()
).Retry(maxRetries: 3);

// 批量处理
foreach (var batch in largeList.Batch(100))
{
    // 处理每批 100 条数据
}
```

### 5. 变更跟踪

```csharp
using FastData.ChangeTracking;

var tracker = new ChangeTracker();

// 开始跟踪
tracker.Track(user);

// 修改属性
user.Name = "新名称";
user.Age = 30;

// 检查变更
if (tracker.HasChanges(user))
{
    var changes = tracker.GetChanges(user);
    foreach (var change in changes)
    {
        Console.WriteLine($"{change.PropertyName}: {change.OriginalValue} → {change.CurrentValue}");
    }
}
```

### 6. 性能监控

```csharp
using FastData.Monitoring;

// 启用监控
var monitor = new PerformanceMonitor();
PerformanceHelper.SetMonitor(monitor);

// 监控查询
var users = PerformanceHelper.MonitorOperation(
    "Query",
    "SELECT * FROM Users",
    () => FastRead.Query<User>().ToList()
);

// 获取性能报告
var report = PerformanceHelper.GetPerformanceReport();
Console.WriteLine(report);
```

## 配置文件示例

### db.config
```xml
<?xml version="1.0" encoding="utf-8"?>
<Root>
  <DbItem Key="SqlServer" IsDefault="true" IsOutSql="true" IsOutError="true" CacheType="web">
    <DbType>SqlServer</DbType>
    <Connection>Server=localhost;Database=MyDb;User Id=sa;Password=123;</Connection>
  </DbItem>
</Root>
```

## 实体类示例

```csharp
using FastData.ChangeTracking;

[TableName("sys_user")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int? Age { get; set; }
    public bool IsActive { get; set; }
}
```

## 常见问题

### Q: 如何处理查询错误？
```csharp
try
{
    var result = FastRead.Query<User>(u => u.Id == 999).ToItem();
    if (result == null)
    {
        throw new FastDataException(FastDataErrorCode.EntityNotFound, "用户不存在");
    }
}
catch (FastDataException ex)
{
    Console.WriteLine($"错误: {ex.Message}");
}
```

### Q: 如何优化批量操作？
```csharp
// 使用批量插入而不是循环插入
// ❌ 不推荐（慢）
foreach (var user in users)
{
    FastWrite.Add(user);
}

// ✅ 推荐（快 100 倍）
FastWrite.BulkInsert(users);
```

### Q: 如何启用 SQL 日志？
```csharp
// 方法 1：配置文件
// db.config: IsOutSql="true"

// 方法 2：代码配置
FastDb.EnableSqlLog = true;

// 方法 3：使用增强日志
FastDb.ConfigureLogging(loggerFactory);
FastDb.EnableSqlLog = true;
```

### Q: 如何使用异步 API？
```csharp
// 所有同步方法都有对应的异步方法
// 同步: FastRead.Query<User>().ToList()
// 异步: await FastReadAsync.ToListAsync<User>()

// 带取消令牌
var cts = new CancellationTokenSource();
var users = await FastReadAsync.ToListAsync<User>(
    u => u.IsActive == true, 
    cancellationToken: cts.Token
);
```

## 性能优化技巧

### 1. 批量操作
```csharp
// 大数据量插入使用 BulkInsert
FastWrite.BulkInsert(users);

// 大数据量更新使用 BulkUpdate
FastWrite.BulkUpdate(updates, u => u.IsActive == true);
```

### 2. 索引优化
```csharp
// 在数据库中创建索引
CREATE INDEX idx_user_email ON sys_user(email);
CREATE INDEX idx_user_age ON sys_user(age);
```

### 3. 查询优化
```csharp
// 只查询需要的字段
var users = FastRead.Query<User>(u => u.Id > 0, u => new { u.Id, u.Name }).ToList();

// 分页查询减少数据传输
var page = FastRead.Query<User>().ToPage(new PageModel { PageId = 1, PageSize = 20 });
```

### 4. 连接池配置
```csharp
// 在配置文件中调整连接池大小
<Connection PoolSize="100" />
```

## 调试技巧

### 1. 查看 SQL
```csharp
// 启用 SQL 日志
FastDb.EnableSqlLog = true;

// 查看生成的 SQL
var query = FastRead.Query<User>(u => u.Id == 1);
Console.WriteLine(query); // 会输出 SQL
```

### 2. 性能分析
```csharp
// 获取性能报告
var report = PerformanceHelper.GetPerformanceReport();
Console.WriteLine(report);

// 查看慢查询
var slowQueries = monitor.GetSlowQueries(1000); // 1秒阈值
foreach (var query in slowQueries)
{
    Console.WriteLine($"{query.Duration.TotalMilliseconds}ms: {query.Sql}");
}
```

### 3. 错误诊断
```csharp
try
{
    var result = FastWrite.Add(user);
    result.ThrowIfFailed("添加用户");
}
catch (FastDataException ex)
{
    Console.WriteLine($"错误码: {ex.ErrorCode}");
    Console.WriteLine($"错误信息: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"内部异常: {ex.InnerException.Message}");
    }
}
```

## 进阶用法

### 1. 多表联查
```csharp
var results = FastRead.Query<Order>()
    .InnerJoin<Order, User>((o, u) => o.UserId == u.Id)
    .Where<Order>(o => o.CreateTime > DateTime.Now.AddMonths(-3))
    .ToList();
```

### 2. 分组聚合
```csharp
var grouped = FastRead.Query<User>()
    .GroupBy(u => u.Department)
    .Select(g => new { Department = g.Key, Count = g.Count() })
    .ToList();
```

### 3. 事务处理
```csharp
using (var db = new DataContext("SqlServer"))
{
    db.BeginTrans();
    
    try
    {
        // 执行多个操作
        db.Add<User>(user1);
        db.Add<User>(user2);
        
        db.SubmitTrans();
    }
    catch
    {
        db.RollbackTrans();
        throw;
    }
}
```

### 4. 自定义 SQL
```csharp
var results = FastRead.Query<User>()
    .ExecuteSql("SELECT * FROM Users WHERE Age > @Age", new { Age = 18 });
```

## 更多资源

- 📖 [API 文档](./FastData/Documentation/README.md)
- 🎨 [代码风格指南](./FastData/CODE_STYLE.md)
- 🚀 [现代 ORM 特性](./FastData/MODERN_ORM_FEATURES.md)
- 📋 [未来改进规划](./FUTURE_IMPROVEMENTS.md)
- 📊 [改进总结报告](./IMPROVEMENTS_SUMMARY.md)
- 📝 [最终报告](./FINAL_REPORT.md)

## 支持

- 🐛 报告问题：GitHub Issues
- 💬 讨论：GitHub Discussions
- 📧 邮件：support@fastdata.com

---

**开始使用 FastData ORM，享受高效的数据库操作体验！** 🚀