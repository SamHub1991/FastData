# Changelog

本文档记录 FastData 的所有重要变更。格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)。

## [2.0.0] - 2026-05-27

### Added

- 多目标框架支持（net45/net6.0/net8.0/net10.0）
- SDK-style csproj 格式迁移（所有项目）
- 条件编译处理框架差异（NETFRAMEWORK/NET6_0_OR_GREATER）
- IFastRepository 接口拆分：IReadRepository、IWriteRepository、IMapRepository
- 连接字符串加密支持（DataContext.IsEncrypt + BaseSymmetric.Decrypto）
- Redis 单例模式（Lazy<FullRedis>）
- NewLife.Redis 6.0.2024.1006 替换 StackExchange.Redis（net6.0+）
- Newtonsoft.Json 升级至 13.0.3
- NPOI 分版本：2.5.6(net45) / 2.7.0(net6.0+)
- System.CodeDom 8.0.0（net6.0+ 条件编译）
- BinaryFormatter 替换为 System.Text.Json（net6.0+）
- FastData.Demo 验证项目（完整技术栈验证）
- 业务主键配置支持复合主键（如 `order_id,line_no`）
- 同步工具组件化重构：MainForm 从 1856 行拆分为 509 行 + 7 个组件 + 3 个服务
- 批量同步支持（BatchSyncResult 结果统计）
- 同步取消功能（CancelBatchSync）
- 百分比进度条（替代滚动条）
- 颜色日志输出（DEBUG 灰/INFO 白/WARN 黄/ERROR 红）
- 日志级别过滤、任务 ID 过滤、日志导出
- 定时任务管理对话框（执行历史、暂停/恢复）
- 同步结果详情对话框（成功/失败/跳过表数、读写统计）
- IoC 依赖注入容器（ServiceContainer）
- Logger 日志工具（Debug/Info/Warn/Error，按日期/任务分割文件）
- 数据库容器内存优化（SQL Server 1.5GB + MySQL 512MB + PostgreSQL 256MB）
- xUnit 2.6.2 测试框架迁移
- 73 个单元测试全部通过（net10.0）
- NuGet 包生成（FastUntility/FastData.Tooling/FastData/FastRedis）
- 综合验证测试脚本（verify-all.sh，34 项测试）
- 大表主键加载优化（GetMaxPrimaryKeyValueFromDb 直接查询数据库）
- 依赖注入服务注册扩展（SyncService/LogService/TaskSchedulerService）
- MainForm 拆分为 4 个 UserControl（DbConfigControl/SyncConfigControl/TaskManagerControl/ReplayControl）
- **消息队列支持**（基于 NewLife.Redis）
  - RedisReliableQueue 可信队列（单消费、消费确认、消息不丢失，适合数据库存储削峰）
  - RedisStream 多消费组队列（多消费组独立消费，适合多方推送解耦）
  - IMessageProducer/IMessageConsumer 接口抽象
  - MessageQueueFactory 工厂类
  - MessageQueueIntegrationService 集成服务
  - TableSyncConfig 配置驱动（EnableMessageQueue/MessageQueueType/MessageQueueTopic）
  - FastData.Demo API 端点（/api/mq/demo/reliable, /api/mq/demo/stream）
  - FastData.Example 示例代码

### Fixed

- DataRowSerializer 反序列化 IDictionary vs Dictionary 类型问题
- 业务主键配置参数未传递到同步方法（GetPrimaryKeyColumns 优先使用配置参数）

### Added (Latest)

- **匿名类型投影查询（Select）**
  - ProjectedQuery<T, TResult>：投影查询类，支持匿名类型
  - FastRead.Select()：扩展方法，支持 `.Select(p => new { p.Id, p.Name })` 语法
  - 支持 ToList()、ToPagination()、ToItem()、ToCount() 等操作
  - 16 个单元测试验证分页计算和投影逻辑
- **分页查询 API（简化版）**
  - PaginationRequest：分页请求参数（Page/PageSize）
  - PaginationResult<T>：分页结果（Total/TotalPages/Page/PageSize/HasPrevious/HasNext/Data）
  - FastRead.ToPagination()：扩展方法，传入 page 和 pageSize 返回分页结果
  - FastRead.ToPaginationAsync()：异步版本
  - 支持泛型版本（PaginationResult<T>）和字典版本（PaginationResult）
  - FastData.Demo PaginationController：Web API 分页示例
  - FastData.Example PaginationExample：分页查询使用教程
- **Model Generator XML Map SQL 生成器**
  - XmlMapSqlGenerator.cs：从数据库表结构生成 FastData XML Map SQL
  - 自动生成 Select All / Select By PK / Select Dynamic / Insert / Update / Delete
  - MainForm 新增「预览XML」和「生成XML Map」按钮
- **FastData.Example 场景化教程示例**
  - MapSqlExample.cs：XML Map SQL 使用示例
  - TransactionExample.cs：事务使用示例
  - MultiDbExample.cs：多数据库使用示例
  - RawSqlExample.cs：原始 SQL 示例
  - Program.cs 菜单更新：覆盖全部 11 种 ORM 功能场景

### Changed

- FastData.Tooling 目录重构：按功能分层（Logging/Models/Services/Utils）
- DataSyncService.CheckRowExists/UpdateRow/UpsertRow 接受 primaryKeyColumns 参数
- GetPrimaryKeyColumns() 优先使用 primaryKeyColumns 参数，回退到 DataTable.PrimaryKey
- CallContext → AsyncLocal（FastDb.cs 条件编译）
- 自定义 Assert 类 → xUnit 断言
- RedisInfo StackExchange 实现删除，统一使用 NewLife.Redis
- 文档整合：DEVELOPMENT_PROGRESS.md 合并开发进度文档

## [1.5.0] - 2026-05-26

### Added

- 代码质量优化完成（AsyncHelper、批量插入、JSON 修复）
- 单元测试从 3 个扩展到 69 个
- Docker 数据库环境搭建（SQL Server Express + MySQL 8.0 + PostgreSQL 15）
- DatabaseProviderMappings 统一数据库提供程序引用
- 可测试性抽象（DateTimeProvider）
- 完整同步测试脚本（Python）

### Fixed

- 手动 JSON 解析改用 JavaScriptSerializer
- SyncTool 数据补录 Tab 布局 row 索引越界
- ModelGenerator UI 布局冲突

### Changed

- 消除魔法字符串，使用 DatabaseProviderMappings 统一引用
- 提取 AsyncHelper 类，统一处理 Task.Run 反模式
- 批量插入优化，减少数据库往返次数

## [1.0.0] - 2026-05-25

### Added

- 核心 ORM 引擎（FastRead/FastWrite/FastDb/FastMap）
- Lambda 强类型查询
- XML Map SQL 管理
- 多数据库支持（Oracle/MySQL/SQL Server/SQLite/PostgreSQL/DB2）
- Code First / Db First 双模式
- AOP 拦截器
- Repository 模式
- 数据同步工具（SyncTool.WinForms）
- Model 生成工具（ModelGenerator.WinForms）
- FastRedis 缓存提供者
- FastUntility 通用工具库（日志/Excel/HTTP/XML/缓存）
