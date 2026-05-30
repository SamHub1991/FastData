# FastData.Tests

FastData.Tests 是 FastData ORM 生态系统的单元测试项目。

## 测试框架

| 组件 | 版本 |
|------|------|
| xUnit | 2.6.2 |
| Microsoft.NET.Test.Sdk | 17.8.0 |
| 目标框架 | net462, net6.0, net8.0, net10.0 |

## 测试结构

### 根级测试

| 文件 | 测试内容 |
|------|----------|
| `ChainableWhereTests.cs` | DataQuery 链式条件、AND/OR、清除/计数 |
| `WhereBuilderTests.cs` | WhereBuilder 条件构建、初始+链式组合 |
| `PaginationTests.cs` | 分页结果、总页数、HasPrevious/HasNext |
| `ShardingTests.cs` | 分表策略（时间/哈希/列表/组合/查询频率） |
| `ShardingCrudTests.cs` | 分表 CRUD、配置/启用/禁用、GetTableName |

### 子目录测试

| 目录 | 文件 | 测试内容 |
|------|------|----------|
| `Abstractions/` | `DateTimeProviderTests.cs` | DateTime 提供者抽象 |
| `Adapter/` | `DatabaseAdapterFactoryTests.cs` | 数据库适配器工厂 |
| `Config/` | `DataConfigTests.cs` | 配置加载 |
| `Config/` | `DataSyncOptionsTests.cs` | 同步选项 |
| `Config/` | `SyncConfigManagerTests.cs` | 同步配置管理 |
| `Sync/` | `DataRowSerializerTests.cs` | DataRow 序列化 |
| `Sync/` | `PrimaryKeyConfigServiceTests.cs` | 主键配置服务 |
| `Sync/` | `TimeRangeCalculatorTests.cs` | 时间范围计算 |

## 运行测试

```bash
# 运行所有测试
dotnet test FastData.Tests

# 运行特定测试类
dotnet test FastData.Tests --filter "ShardingTests"

# 运行详细输出
dotnet test FastData.Tests --verbosity normal

# 运行特定框架
dotnet test FastData.Tests --framework net10.0
```

## 当前测试结果

```
Passed!  - Failed: 0, Passed: 224, Skipped: 1, Total: 225
```

## 测试覆盖

### 分表测试
- 时间分表：日/周/月粒度
- 哈希分表：取模/一致性/CRC32 算法
- 列表分表：值映射
- 组合分表：时间+哈希组合
- 查询频率分表：热数据检测
- 冷数据迁移到独立表
- ShardingManager 生命周期（配置/启用/禁用/重置）

### 查询测试
- AND/OR 条件链式调用
- 复杂 SQL 表达式构建
- Where<T> 谓词组合
- 动态条件组合

### 分页测试
- 页码计算准确性
- HasPrevious/HasNext 标志
- 带投影的分页查询
- 字典结果分页

### 配置测试
- 数据库配置加载
- 同步选项配置
- 配置管理器生命周期

### 序列化测试
- DataRow 序列化/反序列化
- 主键配置服务
- 时间范围计算

## 依赖

- xUnit 2.6.2
- Microsoft.NET.Test.Sdk 17.8.0
- FastData
- FastUntility

## 许可证

MIT License - see [LICENSE](../LICENSE) for details.
