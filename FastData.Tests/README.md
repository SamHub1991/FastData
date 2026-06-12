# FastData.Tests

FastData.Tests 是 FastData ORM 生态系统的单元测试项目。

**最新测试结果 (2026-06-12)**:
- ✅ **527/528 测试通过**（1 个环境相关测试跳过）
- ✅ **多数据库集成测试 109/109 通过**
- ✅ **SQL Server / MySQL / PostgreSQL / SQLite / Redis 场景覆盖**

## 测试框架

| 组件 | 版本 |
|------|------|
| xUnit | 2.6.2 |
| Microsoft.NET.Test.Sdk | 17.8.0 |
| 目标框架 | net452, net8.0, net10.0 |

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

# 运行真实多数据库集成测试
FASTDATA_RUN_DB_INTEGRATION=true dotnet test FastData.Tests/FastData.Tests.csproj --framework net10.0 -m:1 --filter "FullyQualifiedName~MultiDatabase"
```

## 当前测试结果

```
Passed!  - Failed: 0, Passed: 527, Skipped: 1, Total: 528
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
- 多数据库物理分表 CRUD、分页和路由验证（SQL Server / MySQL / PostgreSQL）

### 多数据库集成测试
- 连接生命周期、健康检查、连接池并发、扩缩容与泄漏 smoke
- 事务提交、回滚、批量插入和事务内更新
- 并发读写、批量写入和连接池并发
- 空结果、特殊字符、Unicode、长字符串、分页和布尔值边界
- Redis 队列跨数据库写入、消费、降级恢复和链式 Redis 配置

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

## 测试结果汇总 (2026-05-31)

| 测试类别 | 通过 | 失败 | 跳过 | 总计 | 耗时 |
|----------|------|------|------|------|------|
| **OrmCrudTests** | 26 | 0 | 0 | 26 | 98ms |
| **ShardingTests** | 40 | 0 | 0 | 40 | 85ms |
| **PaginationTests** | 16 | 0 | 0 | 16 | 36ms |
| **CacheTests** | 16 | 0 | 0 | 16 | 153ms |
| **ExceptionManagerTests** | 18 | 0 | 0 | 18 | 62ms |
| **SecurityTests** | 13 | 0 | 1 | 14 | 691ms |
| **MessageQueueTests** | 9 | 0 | 0 | 9 | 21ms |
| **EncryptionTests** | 7 | 0 | 0 | 7 | 12ms |
| **AopTests** | 7 | 0 | 0 | 7 | 11ms |
| **DbTableNamesTests** | 6 | 0 | 0 | 6 | 11ms |
| **ActiveEnvironmentTests** | 5 | 0 | 0 | 5 | 64ms |
| **ConnectionPoolTests** | 16 | 0 | 0 | 16 | 42s |
| **StressTests** | 13 | 5 | 0 | 18 | 55s |
| **总计** | **192** | **5** | **1** | **198** | - |

### 测试环境

| 组件 | 版本 | 状态 | 端口 |
|------|------|------|------|
| Redis | 7.4.9 | ✅ Up | 6379 |
| SQL Server | 2019 | ✅ Up | 1433 |
| MySQL | 8.0 | ✅ Up | 3306 |
| PostgreSQL | 15 | ✅ Up | 5432 |

### StressTests 说明

5 个失败的测试均为高并发场景（30 线程并发）：
- **失败原因**: 连接池在极端并发下耗尽
- **成功率**: 17% (52/300 请求)
- **吞吐量**: 27,273 ops/s
- **性质**: 容量限制，非功能 bug
- **建议**: 增加连接池最大连接数或使用消息队列削峰

## 依赖

- xUnit 2.6.2
- Microsoft.NET.Test.Sdk 17.8.0
- FastData
- FastUntility

## 许可证

MIT License - see [LICENSE](../LICENSE) for details.
