# FastData 项目开发进度

更新时间：2026-05-27

## 项目概述

FastData 是一个轻量级 ORM 框架，支持 .NET Framework 4.5 / .NET 6.0 / .NET 8.0 / .NET 10.0 多目标框架。

## 版本历史

### v2.0.0 (2026-05-27) - 多目标框架迁移

#### 核心变更

- 多目标框架支持（net45/net6.0/net8.0/net10.0）
- SDK-style csproj 格式迁移
- 条件编译处理框架差异
- Redis 客户端替换（StackExchange.Redis → NewLife.Redis）
- IFastRepository 接口拆分
- 连接字符串加密支持
- xUnit 测试框架迁移

#### 构建验证

| 项目 | net45 | net6.0 | net8.0 | net10.0 |
|------|-------|--------|--------|---------|
| FastUntility | ✅ | ✅ | ✅ | ✅ |
| FastData.Tooling | ✅ | ✅ | ✅ | ✅ |
| FastData | ✅ | ✅ | ✅ | ✅ |
| FastRedis | ✅ | ✅ | ✅ | ✅ |
| FastData.Tests | ✅ | - | - | ✅ 73/73 |

#### NuGet 包

- FastUntility.1.0.0.nupkg
- FastData.Tooling.1.0.0.nupkg
- FastData.1.0.0.nupkg
- FastRedis.1.0.0.nupkg

### v1.5.0 (2026-05-26) - 代码质量优化

- AsyncHelper 提取
- 批量插入优化
- JSON 修复
- 单元测试扩展（69 个）
- Docker 数据库环境搭建

### v1.0.0 (2026-05-25) - 初始版本

- 核心 ORM 引擎
- Lambda 强类型查询
- XML Map SQL 管理
- 多数据库支持
- Code First / Db First
- AOP 拦截器
- Repository 模式
- 数据同步工具
- Model 生成工具

## 目录结构

```
FastData/
├── FastData/                          # 核心 ORM 组件
├── FastData.Tooling/                  # 公共工具库
│   └── Sync/                          # 数据同步服务
│       ├── Logging/                   # 日志工具
│       ├── Models/                    # 数据模型
│       ├── Services/                  # 业务服务
│       └── Utils/                     # 工具类
├── FastRedis/                         # Redis 缓存组件
├── FastUntility/                      # 通用工具库
├── FastData.Tests/                    # 单元测试
├── FastData.Demo/                     # 验证项目
├── FastData.SyncTool.WinForms/        # 数据同步工具
│   ├── Components/                    # 模块化组件
│   ├── Forms/                         # 表单对话框
│   └── Services/                      # 业务服务
└── FastData.ModelGenerator.WinForms/  # Model 生成工具
```

## 接口拆分

### IFastRepository 组合接口

```csharp
public interface IFastRepository : IReadRepository, IWriteRepository, IMapRepository
{
}
```

### IReadRepository 读取接口

```csharp
public interface IReadRepository
{
    List<T> Query<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();
    Task<List<T>> QueryAsy<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();
    // ... 更多方法
}
```

### IWriteRepository 写入接口

```csharp
public interface IWriteRepository
{
    WriteReturn Add<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();
    Task<WriteReturn> AddAsy<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();
    WriteReturn Update<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new();
    // ... 更多方法
}
```

## Redis 单例模式

```csharp
public static class RedisInfo
{
    private static readonly Lazy<FullRedis> _redisLazy = new Lazy<FullRedis>(() =>
    {
        var config = RedisConfig.GetConfig();
        var redis = new FullRedis();
        redis.Init(config.WriteServerList);
        return redis;
    });

    public static FullRedis Redis => _redisLazy.Value;
}
```

## 连接字符串加密

```xml
<Add Provider="SqlServer" 
     Key="SecureDb" 
     ConnStr="加密后的连接字符串" 
     IsEncrypt="true" />
```

```csharp
// 加密连接字符串
var encrypted = BaseSymmetric.Encrypto("server=.;database=demo;uid=sa;pwd=123456");
```

## 条件编译

```csharp
// .NET Framework 4.5
#if NETFRAMEWORK
    using System.Runtime.Remoting.Messaging;
    // 使用 CallContext
#endif

// .NET 6.0+
#if NET6_0_OR_GREATER
    using System.Threading;
    // 使用 AsyncLocal
#endif

// 非 .NET Framework
#if !NETFRAMEWORK
    using NewLife.Caching;
    // 使用 NewLife.Redis
#endif
```

## 依赖库版本

| 依赖库 | net45 | net6.0+ | 说明 |
|--------|-------|---------|------|
| Newtonsoft.Json | 13.0.3 | 13.0.3 | JSON 序列化 |
| NPOI | 2.5.6 | 2.7.0 | Excel 操作 |
| NServiceKit.Redis | 1.0.17 | - | Redis 客户端（net45） |
| NewLife.Redis | - | 6.0.2024.1006 | Redis 客户端（net6.0+） |
| System.CodeDom | - | 8.0.0 | 动态编译 |
| xUnit | - | 2.6.2 | 单元测试框架 |

## 数据同步工具重构

### 组件化拆分

MainForm 从 1856 行拆分为 509 行 + 7 个组件 + 3 个服务：

| 组件 | 行数 | 职责 |
|------|------|------|
| DbConnectionPanel | 126 | 数据库连接配置 |
| SyncConfigPanel | 160 | 同步配置面板 |
| TableListManager | 150 | 表列表管理 |
| LogPanel | 100 | 日志面板 |
| ProgressPanel | 80 | 进度面板 |
| TaskManager | 120 | 任务管理 |
| BatchSyncManager | 100 | 批量同步管理 |

### 服务层

| 服务 | 职责 |
|------|------|
| SyncService | 同步执行服务 |
| ReplayService | 数据补录服务 |
| IoC/ServiceContainer | 依赖注入容器 |

## 测试结果

### 单元测试

- 测试框架：xUnit 2.6.2
- 测试数量：73 个
- 通过率：100%
- 运行时间：~3 秒

### 性能测试

| 场景 | 记录数 | 耗时 | 速率 |
|------|--------|------|------|
| 基础同步 | 1000 | 0.18s | 5467 条/秒 |
| 批量同步 | 10000 | 1.2s | 8333 条/秒 |
| 增量同步 | 500 | 0.1s | 5000 条/秒 |

## 优化改进

### 已完成

1. **大表主键加载优化**
   - 新增 `GetMaxPrimaryKeyValueFromDb` 方法
   - 直接从数据库查询 `SELECT MAX(pk_column) FROM table`
   - 避免加载所有行到内存

2. **失败记录序列化**
   - 使用 JSON 格式（System.Text.Json / JavaScriptSerializer）
   - 支持 .NET Framework 和 .NET 6.0+

3. **Redis 单例模式**
   - 使用 `Lazy<FullRedis>` 实现线程安全单例
   - 避免重复创建连接

### 待优化

1. 为 SyncTool MainForm 引入依赖注入
2. 拆分 MainForm 为 Tab UserControl

## 相关文档

- [README.md](README.md) - 项目主文档
- [CHANGELOG.md](CHANGELOG.md) - 版本变更记录
- [FastData.Demo/README.md](FastData.Demo/README.md) - Demo 项目说明
- [FastData.SyncTool.WinForms/REFACTOR_README.md](FastData.SyncTool.WinForms/REFACTOR_README.md) - 同步工具重构说明
