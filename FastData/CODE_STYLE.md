# FastData 代码风格指南

本文档定义了 FastData ORM 项目的代码风格规范。

## 命名规范

### 类和接口
- **类名**：使用 PascalCase（大驼峰）
  ```csharp
  public class FastRead { }
  public class DataContext { }
  ```

- **接口名**：使用 PascalCase，以 `I` 开头
  ```csharp
  public interface IQuery { }
  public interface IRepository { }
  ```

### 方法
- **公共方法**：使用 PascalCase
  ```csharp
  public List<T> ToList<T>() { }
  public WriteReturn Add<T>(T model) { }
  ```

- **私有方法**：使用 PascalCase（与公共方法保持一致）
  ```csharp
  private void BuildQuery() { }
  ```

### 属性
- **属性名**：使用 PascalCase
  ```csharp
  public int Count { get; set; }
  public string Name { get; set; }
  ```

### 字段
- **私有字段**：使用 camelCase（小驼峰），可选下划线前缀
  ```csharp
  private string connectionString;
  private int _count;
  ```

- **公共字段**：避免使用，优先使用属性
  ```csharp
  // 不推荐
  public string name;
  // 推荐
  public string Name { get; set; }
  ```

### 参数
- **参数名**：使用 camelCase
  ```csharp
  public void AddUser(string userName, int age) { }
  ```

### 局部变量
- **局部变量名**：使用 camelCase
  ```csharp
  var userList = new List<User>();
  var totalItems = 10;
  ```

### 常量
- **常量名**：使用 PascalCase
  ```csharp
  public const int MaxRetryCount = 3;
  public const string DefaultKey = "Default";
  ```

## 方法长度

### 推荐长度
- **最佳实践**：单个方法不超过 50 行
- **可接受**：50-100 行（需要注释说明）
- **避免**：超过 100 行（应拆分为多个小方法）

### 示例：拆分长方法

**不推荐（过长方法）**
```csharp
public void ProcessUserData()
{
    // 150+ 行代码...
    // 读取数据
    // 验证数据
    // 转换数据
    // 保存数据
    // 记录日志
}
```

**推荐（拆分为小方法）**
```csharp
public void ProcessUserData()
{
    var data = ReadUserData();
    ValidateData(data);
    var transformed = TransformData(data);
    SaveData(transformed);
    LogProcess(transformed);
}
```

## 代码组织

### 类成员顺序
```csharp
public class ClassName
{
    // 1. 常量
    public const int MaxCount = 100;

    // 2. 静态字段
    private static readonly object _lock = new object();

    // 3. 实例字段
    private string _name;

    // 4. 构造函数
    public ClassName() { }

    // 5. 属性
    public string Name { get; set; }

    // 6. 公共方法
    public void DoSomething() { }

    // 7. 受保护方法
    protected void DoProtected() { }

    // 8. 私有方法
    private void DoPrivate() { }

    // 9. 嵌套类型
    private class NestedClass { }
}
```

### 文件组织
- **单类原则**：每个文件只包含一个公共类（内部类除外）
- **文件命名**：文件名与类名相同
  - `FastRead.cs` → `public class FastRead { }`
  - `DataContext.cs` → `public class DataContext { }`

## 注释规范

### XML 文档注释
```csharp
/// <summary>
/// 添加实体到数据库
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
/// <param name="model">要添加的实体</param>
/// <param name="key">数据库 Key（可选）</param>
/// <returns>操作结果</returns>
public WriteReturn Add<T>(T model, string key = null) where T : class, new()
{
    // 实现...
}
```

### 行内注释
```csharp
// 使用 SqlBulkCopy 批量插入（性能优化）
var bulkCopy = new SqlBulkCopy(connection);

// 检查参数是否有效
if (model == null)
    throw new ArgumentNullException(nameof(model));
```

### TODO 注释
```csharp
// TODO: 添加缓存支持
// FIXME: 修复分页问题
// HACK: 临时解决方案，需要重构
```

## 其他规范

### 使用 var 关键字
```csharp
// 推荐：类型显而易见时使用 var
var userList = new List<User>();
var config = GetConfig();

// 不推荐：类型不明显时使用具体类型
Dictionary<string, object> result = new Dictionary<string, object>();
```

### 字符串连接
```csharp
// 推荐：字符串插值
var message = $"用户 {name} 已添加，ID: {id}";

// 推荐：StringBuilder 处理大量字符串
var sb = new StringBuilder();
for (int i = 0; i < 1000; i++)
{
    sb.AppendLine($"Line {i}");
}

// 不推荐：大量使用 + 连接
var message = "用户" + name + "已添加，ID:" + id;
```

### null 检查
```csharp
// 推荐：使用 null 条件运算符
var count = userList?.Count ?? 0;

// 推荐：使用 null 合并运算符
var key = providedKey ?? FastDb.CurrentKey;

// 推荐：使用 null 检查模式
if (model is User user && user.Age > 18)
{
    // 处理逻辑...
}
```

### 异常处理
```csharp
// 推荐：捕获特定异常
try
{
    FastWrite.Add(model);
}
catch (SqlException ex) when (ex.Number == 2627)
{
    // 主键冲突
}

// 推荐：抛出有意义的异常
if (string.IsNullOrEmpty(key))
    throw new ArgumentException("数据库 Key 不能为空", nameof(key));

// 推荐：记录异常日志
catch (Exception ex)
{
    DbLog.LogError(ex, "添加用户失败");
    throw;
}
```

## 项目特定规范

### 表名映射
```csharp
// 推荐：使用 TableName 特性指定表名
[TableName("sys_user")]
public class User { }

// 推荐：使用 PascalCase 类名自动映射到 snake_case 表名
public class UserProfile { }  // → user_profile
```

### 主键命名
```csharp
// 推荐：使用 Id 作为主键名
public class User
{
    public int Id { get; set; }  // 主键
    public string Name { get; set; }
}

// 推荐：使用特性和主键
public class Order
{
    [Key]
    public int OrderId { get; set; }  // 主键
}
```

### 布尔字段命名
```csharp
// 推荐：使用 Is/Has/Can 前缀
public class User
{
    public bool IsActive { get; set; }      // 推荐
    public bool HasPermission { get; set; } // 推荐
    public bool CanEdit { get; set; }       // 推荐

    public bool Active { get; set; }        // 不推荐
}
```

## 代码格式化

### 缩进
- 使用 4 个空格缩进（不使用 Tab）

### 大括号
```csharp
// 推荐：Allman 风格（大括号另起一行）
public void Method()
{
    if (condition)
    {
        // 代码...
    }
}

// 可接受：K&R 风格（左大括号不另起一行）
public void Method() {
    if (condition) {
        // 代码...
    }
}
```

### 空行
```csharp
public class ClassName
{
    // 方法之间空一行
    public void Method1() { }

    public void Method2() { }
}
```

## 性能优化建议

### 避免 LINQ 过度使用
```csharp
// 不推荐：嵌套 LINQ
var result = list1.Where(x => x.Id > 0)
                  .Select(x => list2.FirstOrDefault(y => y.Id == x.Id))
                  .ToList();

// 推荐：使用 Join
var result = list1.Join(list2, x => x.Id, y => y.Id, (x, y) => y)
                  .ToList();
```

### 使用 StringBuilder
```csharp
// 不推荐：大量字符串连接
var sql = "SELECT * FROM Users WHERE ";
sql += "Name = '" + name + "' ";
sql += "AND Age > " + age;

// 推荐：使用 StringBuilder
var sql = new StringBuilder();
sql.Append("SELECT * FROM Users WHERE ");
sql.Append("Name = '").Append(name).Append("' ");
sql.Append("AND Age > ").Append(age);
```

### 批量操作
```csharp
// 不推荐：循环添加
foreach (var item in items)
{
    FastWrite.Add(item);
}

// 推荐：批量插入
FastWrite.BulkInsert(items);
```

## 工具配置

建议在项目中使用以下工具：

### EditorConfig
```
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true
```

### .editorconfig
创建项目根目录下的 `.editorconfig` 文件以统一团队代码风格。

## 总结

遵循本指南可以确保代码：
- 易读易懂
- 易于维护
- 一致性强
- 性能优化

建议定期进行代码审查，确保团队成员遵循这些规范。