# 数据库提供程序自动注册机制

## 概述

FastData 内置了数据库提供程序自动扫描注册机制，在 .NET Core/.NET 5+ 环境中无需手动注册数据库提供程序。

## 工作原理

`DbProviderAutoRegistrar` 类会在首次调用 `FastDataConfig.GetConfig()` 时自动执行以下操作：

1. **扫描已加载的程序集**：查找可能包含数据库提供程序的程序集
2. **匹配已知提供程序**：根据预定义的列表匹配 SQL Server、MySQL、PostgreSQL、SQLite、Oracle 等
3. **自动注册**：通过 `DbProviderFactories.RegisterFactory()` 注册发现的提供程序
4. **幂等操作**：确保只注册一次，多次调用不会产生副作用

## 支持的数据库

| 数据库 | 程序集关键词 | Factory 类型 | Invariant Name |
|--------|-------------|-------------|----------------|
| SQL Server | Microsoft.Data.SqlClient | SqlClientFactory | Microsoft.Data.SqlClient |
| MySQL | MySql.Data, Pomelo | MySqlClientFactory | MySql.Data.MySqlClient |
| PostgreSQL | Npgsql | NpgsqlFactory | Npgsql |
| SQLite | Microsoft.Data.Sqlite, System.Data.SQLite | SqliteFactory/SQLiteFactory | Microsoft.Data.Sqlite/System.Data.SQLite |
| Oracle | Oracle.ManagedDataAccess | OracleClientFactory | Oracle.ManagedDataAccess.Client |

## 使用示例

### 传统方式（需要手动注册）

```csharp
// 在 Program.cs 或测试配置中
DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", 
    Microsoft.Data.SqlClient.SqlClientFactory.Instance);
DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", 
    MySql.Data.MySqlClient.MySqlClientFactory.Instance);
// ... 每个数据库都需要注册
```

### 自动注册方式（推荐）

```csharp
// 无需任何注册代码，直接使用
var config = FastDataConfig.GetConfig("SqlServer");
using var db = new DataContext(config);
```

## 内部实现

```csharp
namespace FastData.Infrastructure;

public static class DbProviderAutoRegistrar
{
    // 首次调用 GetConfig() 时自动注册
    public static void Register(); 
}
```

## 调试模式

设置环境变量可查看注册详情：

```bash
export FASTDATA_DEBUG=true
```

输出示例：
```
[FastData] Auto-registered 3 database provider(s)
```

## 高级用法

### 手动触发注册

如果需要在获取配置前显式注册：

```csharp
FastData.Infrastructure.DbProviderAutoRegistrar.Register();
```

### 自定义提供程序

如果使用非标准数据库提供程序，可以手动注册：

```csharp
// 自动注册标准提供程序
FastData.Infrastructure.DbProviderAutoRegistrar.Register();

// 手动注册自定义提供程序
DbProviderFactories.RegisterFactory("Custom.DB", CustomFactory.Instance);
```

## 技术细节

1. **扫描范围**：只扫描名称中包含已知关键词的程序集，避免全量扫描性能开销
2. **懒加载**：如果程序集未加载，会尝试加载以发现提供程序
3. **异常处理**：单个提供程序注册失败不影响其他提供程序
4. **线程安全**：使用锁确保多线程环境下的幂等性

## 兼容性

- ✅ .NET Framework 4.x（不产生影响，使用 machine.config）
- ✅ .NET Core 3.1
- ✅ .NET 5/6/7/8/9/10
- ✅ .NET Standard 2.0/2.1

## 注意事项

1. **程序集必须被引用**：自动扫描只能发现已引用的程序集
   - 确保在 `.csproj` 中添加了相应的 NuGet 包引用
   
2. **发布时可能需要保留程序集**：如果使用裁剪发布（PublishTrimmed），确保数据库提供程序程序集不被裁剪

```xml
<ItemGroup>
  <IsTrimmable Include="Microsoft.Data.SqlClient" />
</ItemGroup>
```

## 性能影响

- 首次调用：约 1-5ms（取决于已加载程序集数量）
- 后续调用：0ms（已缓存注册状态）
- 内存开销：< 1KB

## 故障排除

### 问题：无法找到数据库提供程序

**解决方案**：
1. 检查是否安装了相应的 NuGet 包
2. 设置 `FASTDATA_DEBUG=true` 查看注册详情
3. 手动注册作为备选方案

### 问题：注册多个相同提供程序

**解答**：自动注册机制具有幂等性，不会重复注册。如果手动注册过，自动注册会检测到并跳过。
