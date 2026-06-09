# FastData ORM 最佳实践

本文档说明 FastData ORM 的推荐用法和已过时的 API。

---

## API 优先级（推荐 → 过时）

### ✅ 推荐：FastDataClient（实例级门面）

```csharp
var client = new FastDataClient("DefaultDb");

// 新增
client.Add(user);
await client.AddAsync(user);

// 查询
var users = client.Query<User>(u => u.IsActive).ToList();
var user = client.Query<User>(u => u.Id == 1).ToItem();

// 更新
client.Update(user);
await client.UpdateAsync(user);

// 删除
client.Delete<User>(u => u.Id == 1);
await client.DeleteAsync<User>(u => u.Id == 1);
```

**优点**：
- 统一实例级 API，无需重复传递 Key
- 支持链式调用
- 完整的类型推断
- 支持依赖注入

---

### ⚠️ 过时：静态方法（FastRead / FastWrite）

```csharp
// ❌ 不推荐：需要重复传递 Key
FastRead.Query<User>(u => u.IsActive, key: "DefaultDb").ToList();
FastWrite.Add(user, key: "DefaultDb");

// ✅ 推荐：使用 FastDataClient
var client = new FastDataClient("DefaultDb");
client.Query<User>(u => u.IsActive).ToList();
client.Add(user);
```

---

### ⚠️ 过时：Asy 后缀方法

| 过时方法 | 推荐替代 |
|---------|---------|
| `AddAsy()` | `AddAsync()` |
| `AddListAsy()` | `AddListAsync()` |
| `UpdateAsy()` | `UpdateAsync()` |
| `UpdateListAsy()` | `UpdateListAsync()` |
| `DeleteAsy()` | `DeleteAsync()` |

```csharp
// ❌ 过时
await client.AddAsy(user);
await client.UpdateListAsy(list);

// ✅ 推荐
await client.AddAsync(user);
await client.UpdateListAsync(list);
```

---

## ORM CRUD 最佳实践

### 1. 新增操作

```csharp
var client = new FastDataClient("DefaultDb");

// 单条新增
var user = new User { Name = "张三", Age = 25 };
var result = client.Add(user);
// result.IsSuccess 为 true 表示成功
// user.Id 包含自增主键值

// 批量新增（使用事务）
var list = new List<User> { ... };
var result = client.AddList(list, isTrans: true);

// 异步新增
await client.AddAsync(user);
await client.AddListAsync(list);
```

### 2. 查询操作

```csharp
// 条件查询
var users = client.Query<User>(u => u.Age > 18).ToList();

// 链式查询
var result = client.Query<User>(u => u.IsActive)
    .Where<User>(u => u.Age > 18)
    .OrderBy<User>(u => u.CreateTime)
    .Take(10)
    .ToList();

// 单个实体
var user = client.Query<User>(u => u.Id == 1).ToItem();

// 计数
var count = client.Query<User>(u => u.IsActive).ToCount();

// 分页
var page = client.Query<User>(u => u.IsActive)
    .OrderBy<User>(u => u.Id)
    .ToPage(new PageModel { PageIndex = 1, PageSize = 10 });
```

### 3. 更新操作

```csharp
// 根据主键更新
user.Name = "新名字";
client.Update(user);

// 指定更新字段
client.Update(user, u => new { u.Name });

// 条件更新
client.Update(
    new User { IsActive = false },
    u => u.Age < 18
);

// 批量更新
foreach (var item in list)
{
    item.IsActive = false;
}
client.UpdateList(list);

// 异步更新
await client.UpdateAsync(user);
await client.UpdateListAsync(list);
```

### 4. 删除操作

```csharp
// 条件删除
client.Delete<User>(u => u.Age < 18);

// 根据实体删除（需包含主键）
client.Delete(user);

// 异步删除
await client.DeleteAsync<User>(u => u.Id == 1);
```

---

## 常见操作符

| 操作 | 方法 | 示例 |
|------|------|------|
| 相等 | `Query<T>(u => u.Id == 1)` | `u => u.Id == 1` |
| 范围 | `Between()` | `.Between<User>(u => u.Age, 18, 60)` |
| 包含 | `In()` | `.In<User>(u => u.Status, new[] { 1, 2, 3 })` |
| 模糊 | `Like()` | `.Like<User>(u => u.Name, "张%")` |
| 开头 | `StartsWith()` | `.StartsWith<User>(u => u.Name, "Admin")` |
| 结尾 | `EndsWith()` | `.EndsWith<User>(u => u.Email, "@example.com")` |
| 包含 | `Contains()` | `.Contains<User>(u => u.Name, "test")` |

---

## 事务处理

```csharp
// 使用 DataContext 进行事务
using (var db = new DataContext("DefaultDb"))
{
    db.BeginTransaction();
    try
    {
        client.Add(user1, db);
        client.Add(user2, db);
        db.Commit();
    }
    catch
    {
        db.Rollback();
        throw;
    }
}
```

---

## 批量操作

### BulkInsert（高性能）

```csharp
// 适用于大量数据插入（1000+ 条）
var list = Enumerable.Range(1, 10000)
    .Select(i => new User { Name = $"User{i}", Age = i % 100 })
    .ToList();

var result = client.BulkInsert(list);
Console.WriteLine($"插入 {list.Count} 条记录");
```

**注意**：
- ✅ 性能极高（使用 SqlBulkCopy/MySqlBulkLoader）
- ⚠️ 不触发 AOP 事件
- ⚠️ 不支持事务回滚

---

## 依赖注入

```csharp
// Program.cs / Startup.cs
builder.Services.AddFastData();
builder.Services.AddFastDataWithKey("DefaultDb");

// 控制器中
public class UserController : ControllerBase
{
    private readonly FastDataClient _client;
    
    public UserController(FastDataClient client)
    {
        _client = client;
    }
}
```

---

## 错误处理

```csharp
var result = client.Add(user);

if (!result.IsSuccess)
{
    Console.WriteLine($"操作失败：{result.Message}");
}

// 异步
var result = await client.AddAsync(user);
if (!result.IsSuccess)
{
    Console.WriteLine($"操作失败：{result.Message}");
}
```

---

## 性能建议

| 场景 | 推荐做法 |
|------|---------|
| 单条查询 | `Query<T>().ToItem()` |
| 列表查询 | `Query<T>().ToList()` |
| 仅计数 | `Query<T>().ToCount()`（避免 `ToList().Count`） |
| 分页 | `Query<T>().ToPage()` |
| 大量插入 | `BulkInsert()`（1000+ 条） |
| 批量更新 | `UpdateList()` + 事务 |
| 复杂条件 | `ConditionBuilder`（防 SQL 注入） |

---

## 已过时 API 迁移指南

### 从静态方法迁移

**迁移前**：
```csharp
var users = FastRead.Query<User>(u => u.IsActive, key: "db1").ToList();
FastWrite.Add(user, key: "db1");
```

**迁移后**：
```csharp
var client = new FastDataClient("db1");
var users = client.Query<User>(u => u.IsActive).ToList();
client.Add(user);
```

### 从 Asy 后缀迁移

**迁移前**：
```csharp
await client.AddAsy(user);
await client.UpdateListAsy(list);
```

**迁移后**：
```csharp
await client.AddAsync(user);
await client.UpdateListAsync(list);
```

---

## 测试验证

ORM 核心功能测试覆盖率：

| 测试类别 | 状态 |
|---------|------|
| 单表 CRUD | ✅ 100% |
| Lambda 查询 | ✅ 100% |
| 分页查询 | ✅ 100% |
| 批量操作 | ✅ 100% |
| 事务处理 | ✅ 100% |
| 异步操作 | ✅ 100% |

运行测试：
```bash
dotnet test --filter "FullyQualifiedName~OrmCrudTests"
```
