# FastData.Example 使用示例

本项目包含 FastData ORM 的完整使用示例，覆盖所有主要功能。

## 示例列表

| 示例 | 文件 | 说明 |
|------|------|------|
| 基本 CRUD | `BasicCrudExample.cs` | 增删改查基本操作 |
| Lambda 查询 | `LambdaQueryExample.cs` | DataQuery<T> 链式查询、Where<T> 条件构建器 |
| 原始 SQL | `RawSqlExample.cs` | 原始 SQL 查询 |
| XML Map SQL | `MapSqlExample.cs` | XML 映射 SQL 使用 |
| 事务操作 | `TransactionExample.cs` | 事务使用 |
| 多数据库 | `MultiDbExample.cs` | 多数据库连接切换 |
| 数据同步 | `DataSyncExample.cs` | 数据同步工具 |
| 消息队列 | `MessageQueueExample.cs` | RTU 削峰/多方推送 |
| 分页查询 | `PaginationExample.cs` | 分页 API |

## 运行方式

```bash
# 运行所有示例
dotnet run

# 或选择特定示例
dotnet run -- 1  # 基本 CRUD
dotnet run -- 2  # Lambda 查询
```

## 核心 API 速查

### DataQuery<T> 链式查询

```csharp
// 只需写一次 <User>
var users = FastRead.Query<User>(u => u.IsActive)
    .And(u => u.Age > 18)
    .Or(u => u.Role == "Admin")
    .Like(u => u.UserName, "张%")
    .In(u => u.Department, new[] { "IT", "HR" })
    .Between(u => u.Age, 18, 65)
    .OrderBy(u => u.Id)
    .Select(u => new { u.Id, u.UserName, u.Department })
    .ToList();
```

### Where<T> 条件构建器

```csharp
// 分开写条件，更清晰
var where = new Where<User>();
where.Add(u => u.IsActive);
where.And(u => u.Age > 18);
where.Or(u => u.Role == "Admin");
where.Like(u => u.UserName, "张%");
where.In(u => u.Department, new[] { "IT", "HR" });
where.Between(u => u.Age, 18, 65);

var users = FastRead.Query<User>(u => true)
    .Where(where)
    .ToList();
```

### 动态条件构建

```csharp
var where = new Where<User>();
where.Add(u => u.IsActive);

if (!string.IsNullOrEmpty(keyword))
    where.Like(u => u.UserName, keyword + "%");

if (minAge > 0)
    where.And(u => u.Age >= minAge);

var users = FastRead.Query<User>(u => true)
    .Where(where)
    .ToList();
```

### 分页查询

```csharp
var result = FastRead.Query<User>(u => u.IsActive)
    .OrderBy(u => u.Id)
    .ToPagination(page: 1, pageSize: 10);

// result.Total      - 总记录数
// result.TotalPages - 总页数
// result.Data       - 当前页数据
```

### 匿名类型投影

```csharp
var users = FastRead.Query<User>(u => u.IsActive)
    .Select(u => new { u.Id, u.UserName, u.Email })
    .ToList();
```

## 配置数据库

在 `db.config` 中配置数据库连接：

```xml
<DataConfig Default="DefaultDb">
  <Connections>
    <Add Provider="SqlServer" 
         Key="DefaultDb" 
         ConnStr="server=.;database=demo;uid=sa;pwd=123456" 
         IsDefault="true" />
  </Connections>
</DataConfig>
```

## 模型定义

```csharp
public class User
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public string Department { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public decimal Salary { get; set; }
    public string Address { get; set; }
    public string Phone { get; set; }
    public DateTime CreateTime { get; set; }
}
```
