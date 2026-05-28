# 用户指令记忆

本文件记录了用户的指令、偏好和教导，用于在未来的交互中提供参考。

## 格式

### 用户指令条目
用户指令条目应遵循以下格式：

[用户指令摘要]
- Date: [YYYY-MM-DD]
- Context: [提及的场景或时间]
- Instructions:
  - [用户教导或指示的内容，逐行描述]

### 项目知识条目
Agent 在任务执行过程中发现的条目应遵循以下格式：

[项目知识摘要]
- Date: [YYYY-MM-DD]
- Context: Agent 在执行 [具体任务描述] 时发现
- Category: [运维部署|构建方法|排错调试|工作流协作|环境配置]
- Instructions:
  - [具体的知识点，逐行描述]

## 去重策略
- 添加新条目前，检查是否存在相似或相同的指令
- 若发现重复，跳过新条目或与已有条目合并
- 合并时，更新上下文或日期信息

## 条目

[Linux 构建命令]
- Date: 2026-05-28
- Context: Agent 在执行构建验证时发现
- Category: 构建方法
- Instructions:
  - Linux 环境构建需设置 FrameworkPathOverride
  - DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 **仅限 net45 构建**，运行时绝不能设置此变量（会导致 Microsoft.Data.SqlClient 抛出 NotSupportedException）
  - 完整构建命令（net45 专用）: `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" /root/.dotnet/dotnet build FastData.sln /p:RegisterForComInterop=false`
  - 运行时启动命令（net6.0+）不应包含 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: `/root/.dotnet/dotnet run`
  - RegisterForComInterop=false 绕过 COM 注册限制（已在 FastUntility.csproj 中永久修复）

[项目包管理器]
- Date: 2026-05-25
- Context: Agent 在执行依赖安装时发现
- Category: 环境配置
- Instructions:
  - 项目使用 NuGet 包管理，packages.config 格式（非 SDK 风格）
  - .NET Framework 4.5 目标框架，不支持 dotnet add package
  - NuGet 包需通过 nuget restore 或 msbuild /t:Restore 还原

[文档维护规范]
- Date: 2026-05-25
- Context: Agent 在执行文档更新时发现
- Category: 工作流协作
- Instructions:
  - 项目文档位于 .monkeycode/docs/ 目录
  - 需求文档位于 .monkeycode/specs/ 目录
  - README.md 为项目入口，需同步更新文档链接
  - progress.md 需同步更新功能完成状态
  - 中文输出，所有回复和推理使用简体中文

[git 提交排除项]
- Date: 2026-05-25
- Context: Agent 在执行 git 提交时发现
- Category: 工作流协作
- Instructions:
  - dotnet-install.sh 已排除在 .gitignore 中，不可提交
  - .bak 备份文件不可提交
  - 提交前需构建验证（0 Warning, 0 Error）

[同步工具核心架构]
- Date: 2026-05-25
- Context: Agent 在执行数据同步工具开发时发现
- Category: 排错调试
- Instructions:
  - 静态数据（无时间字段）按主键增量同步
  - 动态数据（有时间字段）按时间范围增量同步
  - 中间库模式：源库 -> 中间库 -> 目标库
  - 字段选择时主键必须包含在同步字段中
  - 任务配置持久化到 sync_tasks.json（JSON 数组格式）

[项目结构和测试]
- Date: 2026-05-25
- Context: Agent 在执行项目改进时发现
- Category: 环境配置
- Instructions:
  - 项目结构：FastData（核心ORM）、FastData.Tooling（工具库）、FastData.ModelGenerator.WinForms（模型生成器）、FastData.SyncTool.WinForms（同步工具）、FastData.Tests（单元测试）、FastData.Example（示例）
  - FastData.Tests 使用自定义测试框架，不依赖外部测试库（xUnit/MSTest）
  - Linux 环境无法运行 .NET Framework 可执行文件（Mono 不可用）
  - GitHub Actions CI 在 windows-latest 上运行（MSBuild + NuGet）
[数据同步全局去重配置]
- Date: 2026-05-25
- Context: Agent 在执行同步工具功能完善时发现
- Category: 构建方法
- Instructions:
  - 新增 AlwaysDeduplicate 配置（默认 true）：始终根据业务主键去重，只插入不存在的记录
  - 新增 EnableGlobalConfig 配置：启用全局统一配置
  - 新增 GlobalRangeDays 配置（默认 0）：全局同步范围天数，0=使用任务配置
  - 同步策略：
    * AlwaysDeduplicate=true + 有主键 → 检查存在性，存在则跳过，不存在则 INSERT
    * AlwaysDeduplicate=false + 有主键 → UPSERT 模式，存在则 UPDATE，不存在则 INSERT
    * AlwaysDeduplicate=true + 无主键 → 直接 INSERT（无法去重）
  - 配置位置：TableSyncConfig 类（EnableGlobalConfig, GlobalRangeDays, AlwaysDeduplicate）
  - 同步结果消息会显示：「同步完成 [去重模式：是/否]」

[数据补录和数据库配置]
- Date: 2026-05-25
- Context: Agent 在执行数据补录功能开发时发现
- Category: 构建方法
- Instructions:
  - 新增"数据库配置"Tab 页：保存/测试/删除数据库连接，配置文件 db_connections.json
  - 新增"数据补录"Tab 页：手动补录指定表的历史数据，支持跨库同步
  - 新增共享日志面板：统一日志显示（INFO/WARN/ERROR 级别），支持导出
  - 补录重试机制：数据库连接失败自动重试 3 次，间隔 5 秒
  - ReplayService.cs 处理补录逻辑：根据业务主键判断 UPDATE/INSERT
  - .NET Framework 4.5 兼容性：使用 JavaScriptSerializer 替代 System.Text.Json
  - 补录时间范围：自动识别时间字段（CreateTime/UpdateTime 等）
  - 复合主键支持：多个字段用逗号分隔（如：UserId,OrderDate）

[多目标框架迁移]
- Date: 2026-05-27
- Context: Agent 在执行多目标框架迁移时发现
- Category: 构建方法
- Instructions:
  - SDK-style csproj 格式：所有项目使用 `<Project Sdk="Microsoft.NET.Sdk">`
  - 多目标框架：`<TargetFrameworks>net45;net6.0;net8.0;net10.0</TargetFrameworks>`
  - 条件编译符号：`NETFRAMEWORK`（net45）、`NET6_0_OR_GREATER`（net6.0+）、`!NETFRAMEWORK`（net6.0+）
  - CallContext → AsyncLocal：FastDb.cs 使用 `#if !NETFRAMEWORK` 条件编译
  - Redis 替换：net45 使用 NServiceKit.Redis，net6.0+ 使用 NewLife.Redis 6.0.2024.1006
  - Redis 单例模式：`Lazy<FullRedis>` 实现线程安全单例
  - 接口拆分：IFastRepository 拆分为 IReadRepository + IWriteRepository + IMapRepository
  - 连接字符串加密：DataContext 自动检测 IsEncrypt 并解密
  - Newtonsoft.Json 升级：6.0.8 → 13.0.3
  - NPOI 分版本：2.5.6(net45) / 2.7.0(net6.0+)
  - xUnit 测试框架：替代自定义 Assert 类，使用 [Fact] 属性
  - 测试结果：73 个测试全部通过（net10.0）
  - 构建验证：6 个项目 x 4 框架全部构建成功

[NuGet 包生成]
- Date: 2026-05-27
- Context: Agent 在执行 NuGet 包生成时发现
- Category: 构建方法
- Instructions:
  - 生成脚本：generate-nupkg.sh
  - 输出目录：nupkgs/
  - 生成的包：FastUntility.1.0.0.nupkg、FastData.Tooling.1.0.0.nupkg、FastData.1.0.0.nupkg、FastRedis.1.0.0.nupkg
  - 构建顺序：先 Release 构建，再 pack
  - 注意：需要先构建再打包，否则会报 NU5026 错误

[综合验证测试]
- Date: 2026-05-27
- Context: Agent 在执行综合验证时发现
- Category: 构建方法
- Instructions:
  - 验证脚本：verify-all.sh
  - 测试数量：34 项
  - 测试类别：构建验证、单元测试、NuGet 包验证、多目标框架验证、接口拆分验证、Redis 单例模式验证、连接字符串加密验证、Demo 项目验证、文档验证、条件编译验证
  - 测试结果：全部通过

[大表主键优化]
- Date: 2026-05-27
- Context: Agent 在执行性能优化时发现
- Category: 构建方法
- Instructions:
  - 新增方法：GetMaxPrimaryKeyValueFromDb
  - 优化原理：直接从数据库查询 SELECT MAX(pk_column) FROM table
  - 优势：避免加载所有行到内存
  - 回退机制：优先使用数据库查询，失败时回退到内存计算

[项目状态总结]
- Date: 2026-05-27
- Context: Agent 在执行任务总结时发现
- Category: 工作流协作
- Instructions:
  - 所有主要任务已完成并推送到 GitHub（commit 05c3ee6）
  - 多目标框架迁移：net45/net6.0/net8.0/net10.0
  - 单元测试：73 个全部通过（net10.0）
  - 综合验证测试：34 项全部通过
  - NuGet 包：4 个已生成（FastUntility/FastData.Tooling/FastData/FastRedis）
  - MainForm 拆分：4 个 UserControl + 依赖注入
  - 文档已整合：CHANGELOG/README/DEVELOPMENT_PROGRESS/REFACTOR_SUMMARY
  - 项目已推送到 GitHub：https://github.com/SamHub1991/FastData
  - 待验证：真实数据库环境下的端到端测试

[消息队列实现]
- Date: 2026-05-27
- Context: Agent 在执行 RTU 数据上传场景优化时发现
- Category: 构建方法
- Instructions:
  - 基于 NewLife.Redis 实现两种消息队列模式
  - RedisReliableQueue（可信队列）：单消费、消费确认、消息不丢失，适合数据库存储削峰
  - RedisStream（多消费组队列）：多消费组独立消费，适合多方推送解耦
  - 核心接口：IMessageProducer/IMessageConsumer
  - 工厂类：MessageQueueFactory
  - 集成服务：MessageQueueIntegrationService
  - 配置驱动：TableSyncConfig.EnableMessageQueue/MessageQueueType/MessageQueueTopic
  - NewLife.Redis 队列类型位于 NewLife.Caching.Queues 命名空间
  - RedisReliableQueue.Acknowledge 方法接受 string[] 参数
  - RedisStream.ConsumeAsync 支持 Action<T> 和 Func<T, Task> 回调
  - API 端点：/api/mq/demo/reliable, /api/mq/demo/stream
  - 示例代码：FastData.Example/Example/MessageQueueExample.cs
