# FastData

[![CI](https://github.com/SamHub1991/FastData/actions/workflows/ci.yml/badge.svg)](https://github.com/SamHub1991/FastData/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/SamHub1991/FastData/blob/master/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-Fast.Data-blue.svg)](https://www.nuget.org/packages/Fast.Data/)

FastData 是一个面向 .NET Framework 的轻量 ORM 组件，支持 Lambda 查询、XML Map SQL、Code First、Db First、AOP、缓存、Redis 辅助能力和多数据库连接配置。

NuGet 地址：<https://www.nuget.org/packages/Fast.Data/>

## 项目结构

FastData 是一个完整的生态系统，包含核心 ORM、工具库和辅助工具：

```
FastData/
├── FastData/                          # 核心 ORM 组件
│   ├── FastRead.cs                    # 查询入口（Lambda/XML SQL）
│   ├── FastWrite.cs                   # 写入入口（INSERT/UPDATE/DELETE）
│   ├── FastDb.cs                      # 数据库上下文切换
│   ├── FastMap.cs                     # XML Map SQL 解析
│   └── Repository/                    # Repository 模式实现
│
├── FastData.Tooling/                  # 公共工具库
│   ├── Database/                      # 数据库适配器和元数据读取
│   ├── CodeGeneration/                # 代码生成器
│   └── Sync/                          # 数据同步服务
│
├── FastData.ModelGenerator.WinForms/  # Model 生成工具（可视化）
│   └── MainForm.cs                    # 表选择、代码预览、批量生成
│
├── FastData.SyncTool.WinForms/        # 数据同步工具（可视化）
│   └── MainForm.cs                    # 同步配置、任务管理、定时同步
│
├── FastData.Example/                  # 使用示例项目
│   ├── Model/                         # 示例实体
│   └── Example/                       # CRUD/Lambda/同步示例
│
├── FastData.Tests/                    # 单元测试项目
│   └── Program.cs                     # 自定义测试运行器
│
└── FastUntility/                      # 通用工具库
    └── Base/                          # 日志、Excel、HTTP 等工具类
```

## 子项目详细介绍

### 1. FastData（核心 ORM）

**定位**：面向 .NET Framework 的轻量级 ORM 框架

**核心功能**：
- **Lambda 查询**：支持强类型 Lambda 表达式，编译时检查
- **XML Map SQL**：XML 文件管理 SQL，支持动态标签和 AOP
- **多数据库**：Oracle、MySQL、SQL Server、SQLite、PostgreSQL、DB2
- **数据库切换**：`FastDb.Use(key)` 作用域切换，支持多数据源
- **Code First/Db First**：支持两种开发模式
- **AOP 扩展**：支持 SQL 执行前后拦截器
- **缓存支持**：内置查询缓存机制
- **Repository 模式**：`IFastRepository` 和工厂模式

**适用场景**：
- 需要快速开发的企业级应用
- 多数据库兼容的项目
- 需要灵活 SQL 控制的场景
- .NET Framework 4.0+ 项目

---

### 2. FastData.Tooling（工具库）

**定位**：为可视化工具提供公共能力

**核心模块**：
- **Database 适配器**：统一数据库操作接口
- **SQL 方言抽象**：处理不同数据库的 SQL 差异
- **元数据读取器**：获取表结构、字段信息
- **代码生成器**：根据表结构生成 Model 类
- **数据同步服务**：跨数据库数据同步核心逻辑

**适用场景**：
- 需要扩展 FastData 工具链
- 开发自定义数据库工具
- 需要跨数据库元数据访问

---

### 3. FastData.ModelGenerator.WinForms（Model 生成工具）

**定位**：可视化 Model 类生成工具，Db First 开发模式必备

**核心功能**：
- ✅ 支持 SQL Server、MySQL、Oracle 数据库
- ✅ 可视化连接测试和表加载
- ✅ 表搜索过滤（支持表名模糊搜索）
- ✅ 多选表批量生成
- ✅ 代码预览（生成前查看 C# 代码）
- ✅ 字段预览（查看字段类型、主键、可空性）
- ✅ 自定义命名空间（全局和单表覆盖）
- ✅ 自定义输出目录

**使用流程**：
1. 选择数据库 Provider
2. 输入连接字符串，点击"测试连接"
3. 点击"加载表"，从数据库加载表列表
4. 搜索或选择目标表（支持 Ctrl/Cmd 多选）
5. 点击"预览代码"查看生成的 Model
6. 点击"生成文件"保存到指定目录

**代码示例**：
```csharp
// 生成前预览
public class User
{
    public int Id { get; set; }          // 主键
    public string UserName { get; set; }
    public string Email { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsActive { get; set; }
}
```

**适用场景**：
- 已有数据库，快速生成实体类
- 数据库表结构频繁变更
- Db First 开发模式
- 需要批量生成数百个表

**文档**：[完整使用指南](/.monkeycode/docs/model-generator.md)

---

### 4. FastData.SyncTool.WinForms（数据同步工具）

**定位**：企业级跨数据库数据同步工具

**核心功能**：
- ✅ **全量同步**：一次性同步所有数据
- ✅ **增量同步**：按时间范围或主键增量
- ✅ **定时同步**：支持准实时同步（最小 1 分钟间隔）
- ✅ **UPSERT 模式**：自动判断 INSERT 或 UPDATE
- ✅ **复合主键**：支持多字段主键配置
- ✅ **字段选择**：只同步指定的字段
- ✅ **批量操作**：多表同时同步
- ✅ **失败重试**：自动重试失败记录
- ✅ **断点续传**：记录失败记录，下次恢复
- ✅ **任务管理**：增删改查、导入导出配置
- ✅ **中间库模式**：源库 → 中间库 → 目标库

**同步模式**：
| 模式 | 说明 | 适用场景 |
|------|------|----------|
| InsertOnly | 只插入新数据 | 日志表、流水表 |
| UpdateOnly | 只更新已存在数据 | 配置表、字典表 |
| Upsert | 存在则更新，不存在则插入 | 大多数业务表 |
| Full | 全量同步（先删除再插入） | 维度表、快照表 |

**使用流程**：
1. 配置源库、目标库、中间库连接
2. 从源库加载表，选择要同步的表
3. 配置主键字段（UPSERT 模式必需）
4. 配置时间字段（增量同步可选）
5. 选择同步模式和高级选项
6. 保存任务配置
7. 手动执行或启用定时同步

**高级特性**：
- **智能范围**：首次全量，后续按最近 N 天增量
- **时间范围计算器**：快速选择 1/3/7/30 天/本月/上月
- **批量操作**：批量启用/禁用/删除任务
- **导入导出**：JSON 格式配置，便于迁移和备份
- **实时日志**：同步过程实时显示进度和错误

**适用场景**：
- 数据仓库 ETL
- 跨数据库数据迁移
- 生产环境到测试环境数据同步
- 多系统数据集成
- 准实时数据同步（CDC 替代方案）

**文档**：[完整使用指南](/.monkeycode/docs/sync-tool.md)

---

### 5. FastData.Example（示例项目）

**定位**：FastData ORM 使用示例和最佳实践

**包含示例**：
- **基本 CRUD**：INSERT、SELECT、UPDATE、DELETE、UPSERT
- **Lambda 查询**：条件查询、多条件、排序、分页、聚合、关联查询
- **数据同步**：同步配置、同步模式、时间范围、字段选择、复合主键

**运行方式**：
```bash
# Windows（.NET Framework）
FastData.Example.exe

# Linux（需要 Mono）
mono FastData.Example.exe
```

**适用场景**：
- 快速了解 FastData 用法
- 参考示例代码
- 学习数据同步工具 API

---

### 6. FastData.Tests（单元测试）

**定位**：核心功能单元测试

**测试框架**：自定义测试运行器（不依赖 xUnit/MSTest）

**测试覆盖**：
- `TimeRangeCalculator`：时间范围计算器
- `DatabaseAdapterFactory`：数据库适配器工厂
- `DataConfig`：数据配置测试

**运行方式**：
```bash
# Windows
FastData.Tests.exe

# CI/CD（GitHub Actions）
自动运行测试并生成报告
```

**适用场景**：
- 回归测试
- 功能验证
- CI/CD 流水线

---

### 7. FastUntility（通用工具库）

**定位**：通用工具类库

**核心模块**：
- **BaseLog**：日志记录
- **BaseExcel**：Excel 操作
- **BaseUrl**：HTTP 请求工具
- **BaseXml**：XML 操作
- **BaseDic**：字典操作
- **FastCache**：缓存管理
- **WebApiHost**：Web API 自托管

**适用场景**：
- 通用工具类复用
- 日志、Excel、HTTP 等常见操作


## 快速安装

```powershell
Install-Package Fast.Data
```

```bash
dotnet add package Fast.Data
```

## 快速配置

推荐使用统一 `Connections` 配置：

```xml
<configSections>
  <section name="DataConfig" type="FastData.Config.DataConfig,FastData" />
</configSections>

<DataConfig Default="DefaultDb">
  <Connections>
    <Add Provider="SqlServer" Key="DefaultDb" ConnStr="server=.;database=demo;uid=sa;pwd=123456" IsDefault="true" DesignModel="DbFirst" CacheType="web" />
    <Add Provider="MySql" Key="ReportDb" ConnStr="server=127.0.0.1;database=report;uid=root;pwd=123456" DesignModel="DbFirst" CacheType="web" />
  </Connections>
</DataConfig>
```

## 快速使用

默认库查询：

```csharp
var users = FastRead.Query<User>(a => a.IsEnabled == true);
```

指定库查询：

```csharp
var reports = FastRead.Use("ReportDb").Query<Report>(a => a.Year == 2026);
```

作用域切换：

```csharp
using (FastDb.Use("ArchiveDb"))
{
    var logs = FastRead.Query<Log>(a => a.CreatedTime >= beginTime);
    FastWrite.Add(new ArchiveLog());
}
```

Repository 工厂：

```csharp
services.AddTransient<IFastRepositoryFactory, FastRepositoryFactory>();

var defaultRepository = factory.Default();
var reportRepository = factory.Use("ReportDb");
```

## Redis 缓存说明

FastData 支持两种缓存模式，Redis 是可选的分布式缓存提供者。

### 是否需要安装 Redis？

| CacheType | 是否需要 Redis | 适用场景 |
|-----------|---------------|----------|
| `web` | ❌ **不需要** | 单服务器部署，默认使用 .NET `MemoryCache` |
| `redis` | ✅ **需要** | 多服务器集群，需要分布式缓存 |

**默认情况**：FastData 使用 `web` 缓存模式，**无需安装 Redis**。

### 何时需要 Redis？

| 需要 Redis | 不需要 Redis |
|------------|--------------|
| ✅ 多服务器部署，共享缓存 | ✅ 单服务器部署 |
| ✅ 缓存持久化（重启不丢失） | ✅ 应用重启可接受缓存清空 |
| ✅ 缓存数据量超出单机内存 | ✅ 缓存数据量不大 |
| ✅ 分布式系统 | ✅ 独立应用 |

### 配置缓存模式

#### 使用内存缓存（默认，无需 Redis）

```xml
<DataConfig Default="DefaultDb">
  <Connections>
    <Add 
      Provider="SqlServer" 
      Key="DefaultDb" 
      ConnStr="server=.;database=demo;uid=sa;pwd=123456"
      CacheType="web" />
  </Connections>
</DataConfig>
```

#### 使用 Redis 缓存（需要 Redis 服务器）

```xml
<DataConfig Default="DefaultDb">
  <Connections>
    <Add 
      Provider="SqlServer" 
      Key="DefaultDb" 
      ConnStr="server=.;database=demo;uid=sa;pwd=123456"
      CacheType="redis" />
  </Connections>
</DataConfig>

<!-- Redis 配置 -->
<RedisConfig 
  AutoStart="true"
  ReadServerList="127.0.0.1:6379"
  WriteServerList="127.0.0.1:6379"
  MaxReadPoolSize="60"
  MaxWritePoolSize="60" />
```

### Redis 安装方式

#### Docker 安装（推荐）

```bash
docker run -d --name redis -p 6379:6379 redis:latest
```

#### Linux 安装

```bash
sudo apt-get update
sudo apt-get install redis-server
sudo systemctl start redis
redis-cli ping  # 应返回 PONG
```

#### Windows 安装

```powershell
# 使用 Chocolatey
choco install redis-64
```

### FastData 中的 Redis 使用

FastData 的 Redis 操作是**透明的**，无需直接调用 Redis API：

```csharp
// FastData 会自动根据 CacheType 选择缓存提供者
var user = FastRead.Query<User>(a => a.Id == 1).FirstOrDefault();
// CacheType="redis" → 缓存到 Redis
// CacheType="web" → 缓存到 MemoryCache
```

直接使用 Redis（高级用法）：

```csharp
// 依赖注入配置
services.AddTransient<IRedisRepository, RedisRepository>();

// 使用示例
public class UserService
{
    private readonly IRedisRepository _redis;
    
    public UserService(IRedisRepository redis)
    {
        _redis = redis;
    }
    
    public void CacheUser(User user)
    {
        _redis.Set("user:" + user.Id, user, TimeSpan.FromHours(1));
        var cachedUser = _redis.Get<User>("user:" + user.Id);
        _redis.Remove("user:" + user.Id);
    }
}
```

### 环境差异化配置

开发环境用内存缓存，生产环境用 Redis：

```xml
<!-- 开发环境 -->
<Add Provider="SqlServer" CacheType="web" ... />

<!-- 生产环境 -->
<!-- <Add Provider="SqlServer" CacheType="redis" ... /> -->
```

### 项目依赖

- `FastData` → `FastRedis`（可选引用，仅当使用 Redis 缓存时）
- `FastRedis` → `ServiceStack.Redis`（NuGet 包）
- `FastRedis` 项目包含：
  - `RedisConfig.cs`：Redis 配置读取
  - `RedisInfo.cs`：Redis 操作核心类
  - `Repository/IRedisRepository.cs`：Redis 仓库接口
  - `Repository/RedisRepository.cs`：Redis 仓库实现

详细文档：[FastRedis 项目源码](/FastRedis/README.md)



## 文档

### 快速开始
- [中文使用说明](.monkeycode/docs/usage.md) - 完整的功能介绍和使用指南
- [README](.monkeycode/docs/README.md) - 项目概览和快速入门

### 工具文档
- [Model 生成工具使用指南](.monkeycode/docs/model-generator.md) - Db First 开发必备工具
- [数据同步工具使用指南](.monkeycode/docs/sync-tool.md) - 企业级数据同步解决方案

### 项目文档
- [当前进度](.monkeycode/docs/progress.md) - 功能完成状态和待办事项
- [2026 年 5 月需求文档](.monkeycode/specs/项目需求 2026 年 5 月/requirements.md) - EARS 模式需求规格
- [2026 年 5 月技术方案](.monkeycode/specs/项目需求 2026 年 5 月/design.md) - 技术架构设计
- [2026 年 5 月任务清单](.monkeycode/specs/项目需求 2026 年 5 月/tasklist.md) - 实施任务列表

### 外部链接
- [NuGet 包](https://www.nuget.org/packages/Fast.Data/) - 官方包下载
- [GitHub 仓库](https://github.com/SamHub1991/FastData) - 源代码和 Issues
- [CI/CD](https://github.com/SamHub1991/FastData/actions) - 持续集成状态
## 构建验证

当前解决方案包含旧式 `.NET Framework 4.5` 项目。在 Linux 环境中可通过 .NET SDK 和 reference assemblies 验证构建：

```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" /root/.dotnet/dotnet build FastData.sln /p:RegisterForComInterop=false
```

当前构建状态：`0 Warning(s)`, `0 Error(s)`。

## 代码质量（2026-05-25 审查）

| 指标 | 数值 | 状态 |
|------|------|------|
| C# 文件总数 | 145 个 | - |
| XML 注释覆盖 | 65 个文件 | 45% |
| 临时标记 (TODO/FIXME) | 0 个 | ✅ |
| 异常处理点 | 53 个 | ✅ |
| 参数验证 | 完整 | ✅ |
| 资源释放模式 | 18 处 `using` | ✅ |
| SQL 注入防护 | 参数化查询 | ✅ |
| 构建警告 | 0 个 | ✅ |
| 构建错误 | 0 个 | ✅ |

核心代码健壮性良好，参数验证、资源释放、异常处理覆盖完整。可读性方面命名规范、XML 注释符合标准，部分业务逻辑可增加解释性注释。
