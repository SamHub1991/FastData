# 用户指令记忆

本文件记录用户指令、偏好和项目知识，用于在未来的交互中提供参考。

---

## 工作流协作

**文档维护**
- Date: 2026-05-29
- Context: 项目文档大整理
- Instructions:
  - 每个子项目只保留一个 README.md
  - 需求/设计/任务文档位于 .monkeycode/specs/
  - REMOOM.md 为项目入口，CHANGELOG.md 记录版本变更
  - MEMORY.md 只记录行为指令和项目知识（运维/构建/排错/协作/环境）
  - 中文输出，所有回复使用简体中文

**git 提交排除项**
- Date: 2026-05-25
- Context: Agent 在执行 git 提交时发现
- Instructions:
  - dotnet-install.sh 已排除在 .gitignore 中，不可提交
  - .bak 备份文件不可提交
  - 提交前需构建验证（0 Warning, 0 Error）

**FastData.Shared 已删除**
- Date: 2026-05-29
- Context: Agent 在执行项目重构时
- Instructions:
  - FastData.Shared 项目已删除，连接管理代码已合并到 FastData.ModelGenerator.WinForms/Components/
  - 命名空间从 FastData.Shared 改为 FastData.ModelGenerator.WinForms

---

## 构建方法

**Linux 构建命令**
- Date: 2026-05-28
- Context: Agent 在执行构建验证时发现
- Instructions:
  - Linux 环境构建需设置 FrameworkPathOverride
  - DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 仅限 net45 构建，运行时绝不能设置
  - 完整 net45 构建: `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 FrameworkPathOverride="..." dotnet build FastData.sln /p:RegisterForComInterop=false`
  - Windows 构建直接使用 `dotnet build FastData.sln`
  - RestSharp 版本: net452 用 106.11.7，net6.0+ 用 108.0.0

**多目标框架**
- Date: 2026-05-27
- Context: Agent 在执行多目标框架迁移时发现
- Instructions:
  - 所有项目使用 SDK-style csproj（`<Project Sdbk="Microsoft.NET.Sdk">`）
  - 多目标框架: net45;net6.0;net8.0;net10.0
  - 条件编译: NETFRAMEWORK（net45）、NET6_0_OR_GREATER
  - Redis: net45 用 NServiceKit.Redis，net6.0+ 用 NewLife.Redis
  - Redis 单例: `Lazy<FullRedis>` 实现线程安全
  - xUnit 测试框架（73 个测试全部通过）

**NuGet 包生成**
- Date: 2026-05-27
- Context: Agent 在执行 NuGet 包生成时发现
- Instructions:
  - 脚本: generate-nupkg.sh
  - 输出: nupkgs/
  - 包: FastUtaility.1.0.0.nupkg、FastData.Tooling.1.0.0.nupkg、FastData.1.0.0.nupkg、FastRedis.1.0.0.nupkg
  - 先 Release 构建再 pack

**综合验证**
- Date: 2026-05-27
- Context: Agent 在执行综合验证时发现
- Instructions:
  - 脚本: verify-all.sh
  - 测试: 34 项（构建/测试/NuGet/Redis/Demo/文档/条件编译）

---

## 排错调试

**数据源 Key 回退机制**
- Date: 2026-05-28
- Context: Agent 在执行 Demo 端点测试和修复时发现
- Instructions:
  - FastDb.CurrentKey 是 AsyncLocal 变量，可能被污染导致 "数据库配置 Key 不存在"
  - FastRead.Query 走三级回退: key 参数 → FastDb.CurrentKey → Default 配置
  - 单数据库项目: 只需在配置中设 Default，无需传 key

**Sharding 连接配置三级回退**
- Date: 2026-05-28
- Context: Agent 在执行 Sharding 测试时发现
- Instructions:
  - 连接字符串读取顺序: IConfiguration(appsettings.json) → FastDataConfig.GetConnectionString(db.config) → hardcode 兜底
  - appsettings.json 需 CopyToOutputDirectory>PreserveNewest

---

## 运维部署

**Redis Docker 部署**
- Date: 2026-05-28
- Context: Agent 在执行消息队列测试时发现
- Instructions:
  - Redis 容器: `docker run -d --name redis -p 6379:6379 redis:7-alpine redis-server --save "" --appendonly no`
  - 验证: `docker exec redis redis-cli ping` → PONG
  - 配置: Server=127.0.0.1:6379, Db=7

**数据库容器**
- Date: 2026-05-28
- Context: 开发环境
- Instructions:
  - SQL Server: 1433, MySQL: 3306, PostgreSQL: 5432, Redis: 6379
  - Docker Compose 统一管理

---

## 环境配置

**项目包管理器**
- Date: 2026-05-25
- Context: Agent 在执行依赖安装时发现
- Instructions:
  - 使用 NuGet 包管理，SDK-style csproj 格式
  - .NET Framework 4.5 目标框架

**项目结构**
- Date: 2026-05-29
- Context: 项目重构后
- Instructions:
  - FastData: 核心 ORM
  - FastData.Tooling: 工具库（net452/net6.0/net8.0/net10.0）
  - FastData.ModelGenerator.WinForms: 代码生成工具（6 Tab）
  - FastData.SyncTool.WinForms: 数据同步工具
  - FastRedis: Redis 缓存
  - FastUntaility: 通用工具
  - FastData.Tests: 单元测试
  - FastData.Demo: Web API 示例
  - FastData.Example: 控制台示例
