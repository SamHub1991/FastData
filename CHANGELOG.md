# Changelog

本文档记录 FastData 的所有重要变更。格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)。

## [2.2.0] - 2026-05-29

### Added

- **ModelGenerator 新增功能**
  - Tab 5: JSON 转 Model 转换器
    - 支持粘贴 JSON 文本或加载 JSON 文件
    - 自动类型推断（string/long/double/bool/List）
    - 支持嵌套对象和数组
    - 生成带 JsonPropertyName 特性的 C# Model 类
  - Tab 6: API 代码生成器
    - 使用 RestSharp 生成 API 客户端代码
    - 支持多种认证方式（Bearer/JWT/API Key/Token/Basic Auth）
    - 自动生成响应 Model（从 JSON 响应）
    - 生成 Service 层接口
  
- **代码生成器完善**
  - JsonToModelConverter 类 - JSON 解析和 Model 生成
  - ApiClientGenerator 类 - RestSharp API 客户端代码生成
  - 支持自定义类名、命名空间
  - 实时预览功能

- **文档更新**
  - FastData.ModelGenerator.WinForms/USER_GUIDE.md - 详细使用手册
  - 更新 README.md - Model Generator 使用说明

### Changed

- **API 代码生成器改用 RestSharp**
  - 替换 HttpClient 为 RestSharp
  - 支持更简洁的 API 调用语法
  - 内置多种认证方式支持
  - RestSharp 108.0.0 (.NET 6+/8/10)
  - RestSharp 106.11.7 (.NET Framework 4.5.2)

- **目标框架调整**
  - FastData.Tooling 支持 net452/net6.0/net8.0/net10.0
  - 保持与 .NET Framework 4.5.2 兼容

### Technical Details

- **JsonToModelConverter**
  - 自动解析 JSON 对象、数组、基本类型
  - PascalCase 命名转换
  - 特殊字符转义处理
  - 支持 System.Text.Json.JsonPropertyName 特性

- **ApiClientGenerator**
  - 生成 RestClient 封装类
  - 支持异步方法（async/await）
  - JWT Bearer 认证集成
  - 自动生成响应 Model

## [2.1.0] - 2026-05-28

### Added

- **MySQL 批量插入性能测试**
  - 单条插入测试
  - 批量插入测试（事务）
  - 不同批量大小性能对比
  - FastWrite 批量写入测试

- **MySQL → PostgreSQL 同步测试**
  - 数据同步测试
  - 批量同步性能测试
  - 数据一致性验证
  - 增量同步测试

- **ORM API 完整性验证**
  - Read API 测试（Query, Where, Contains, StartsWith, EndsWith）
  - Chainable API 测试（Where, Or, And, Select, OrderBy）
  - Write API 测试（Add, AddList, Update, Delete）
  - DataQuery Generic API 测试
  - Pagination API 测试
  - SQL Log API 测试
  - Sharding API 测试
  - Where Builder 测试

- **连接管理功能**
  - FastData.Shared 共享库
  - ConnectionManagerForm 连接管理界面
  - 支持新增、修改、删除连接
  - 连接配置持久化（JSON）
  - ModelGenerator 集成连接管理

- **Docker 支持**
  - docker-compose.yml 统一管理所有数据库
  - SQL Server, MySQL, PostgreSQL, SQLite 容器化

- **同步工具架构图**
  - 整体架构图
  - 同步流程图
  - 状态管理图
  - 数据流向图
  - 组件关系图
  - 部署架构图

### Changed

- 更新 CHANGELOG.md 记录最新变更
- 合并开发进度文档

## [2.0.0] - 2026-05-27

### Added

- **分表功能完整示例和演示**（bc832f7, 6304250, b1d0216, d2efa79, 89783ea, b0f4334）
  - ShardingFullExample.cs：SQL Server 分表完整示例
    - 批量数据插入（10000 条日志、5000 条订单）
    - 时间/哈希/列表/查询频率分表策略
    - 链式 API 演示
  - ShardingController.cs：Demo API 端点
    - /api/sharding/init：初始化分表环境
    - /api/sharding/insert-data：插入批量测试数据
    - /api/sharding/time/configure, /time/query：时间分表
    - /api/sharding/hash/configure, /hash/query：哈希分表
    - /api/sharding/list/configure, /list/query：列表分表
    - /api/sharding/frequency/configure, /frequency/record, /frequency/simulate, /frequency/hot：查询频率分表
    - /api/sharding/sync：数据同步到分表
    - /api/sharding/stats：分表统计信息
  - ShardingSyncControl.cs：SyncTool 分表同步 UI
    - 源数据库配置
    - 分表类型选择（时间/哈希/列表）
    - 创建分表
    - 数据同步
    - 分表统计
  - ShardingCrudControl.cs：分表 CRUD 操作界面
    - 查询：执行 SQL 查询并显示结果
    - 插入：向分表插入数据
    - 更新：更新分表数据
    - 删除：删除分表数据
    - 双击表名自动填充
  - ShardingTaskService.cs：后台分表任务服务
    - 启动分表任务（后台线程执行）
    - 暂停/恢复任务
    - 取消任务
    - 删除已完成任务
    - 分批处理（每批1000条）
    - 自动创建分表结构
  - ShardingTaskControl.cs：任务列表管理界面
    - DataGridView 显示所有任务进度
    - 颜色状态标识（运行中=蓝/暂停=黄/完成=绿/失败=红）
    - 暂停/恢复/取消/删除按钮
    - 每秒自动刷新进度
  - ShardingSyncControl.cs：增强的分表同步控制
    - 5种分表策略可视化配置
    - 预览分表结构
    - 启动后台任务
  - ShardingConfigSnapshot.cs：分表配置快照模型
    - 配置导入/导出（JSON格式）
    - 跨环境一致性验证
    - 验证结果（错误/警告）
  - ShardingConfigVisualizer.cs：可视化配置编辑器
    - DataGridView 显示所有配置
    - PropertyGrid 显示配置详情
    - 添加/编辑/删除配置
    - 颜色标识分表类型
    - 验证与开发环境一致性
    - 应用配置到分表管理器
  - ShardingConfigEditDialog.cs：配置编辑对话框
    - 5种分表策略独立配置面板
    - 可视化值映射编辑
  - ShardingDataControl.cs：数据操作控件
    - 分表统计：显示所有表名、记录数、类型、大小
    - 跨表查询：支持UNION ALL跨表查询，导出结果
    - 批量增删改：批量插入/更新/删除，危险操作确认
    - 数据导出：CSV格式导出
  - ShardingImportControl.cs：数据导入控件
    - 文件选择：支持CSV/Excel格式
    - 编码选择：UTF-8/GBK/GB2312
    - 表头检测：首行作为表头
    - 主键配置：指定主键字段
    - 数据预览：最多显示100条
    - 后台导入：带进度条的异步导入
    - 取消导入：支持中途取消
    - 自动分表路由：根据策略自动路由到目标表
    - Upsert逻辑：不存在则插入，存在则更新
    - 数据库独有记录保持不变
  - DataQuery.cs：分表属性改为 public
  - MainForm.cs：集成 ShardingSyncControl

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
- 162 个单元测试全部通过（net10.0）
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
- **Lambda 查询扩展 API**
  - FastRead.Query<T>() Lambda 表达式查询
  - FastRead.Select<T, TResult>() 匿名类型投影查询
  - 支持复杂条件组合（Where/And/Or/In/Like/Between）
  - ProjectedQuery<T, TResult> 投影查询类
- **分页查询 API**
  - FastRead.ToPagination<T>() / ToPaginationAsync<T>() 泛型版本
  - FastRead.ToPagination() / ToPaginationAsync() 字典版本
  - PaginationRequest/PaginationResult<T> 模型
  - PaginationController Web API 示例
- **链式 WHERE 条件**
  - DataQuery.ChainedConditions 链式条件列表
  - FastRead.Where<T>() / Or<T>() 扩展方法
  - FastRead.And<T>() - Where 的别名
  - FastRead.Like<T>() / Contains<T>() / StartsWith<T>() / EndsWith<T>() - LIKE 条件
  - FastRead.In<T>() - IN 条件
  - FastRead.Between<T>() - BETWEEN 条件
  - WhereBuilder 统一 WHERE 子句构建
  - DataQuery.EntityType 属性（存储实体类型）
- **FastWrite 匿名类型支持**
  - Add/AddRange/Update/Delete 支持 tableName 参数
  - 无 new() 约束，支持匿名类型推断
- **XML Map SQL 生成器**
  - Model Generator 新增 XML Map SQL 生成功能
  - 支持多表关联查询配置

### Fixed

- DataRowSerializer 反序列化 IDictionary vs Dictionary 类型问题
- 业务主键配置参数未传递到同步方法（GetPrimaryKeyColumns 优先使用配置参数）

### Added (Latest)

- **FastWrite 队列匿名类型支持**
  - Add<T>(tableName, model)：支持匿名类型 INSERT
  - AddRange<T>(tableName, models)：支持匿名类型批量 INSERT
  - Update<T>(tableName, model)：支持匿名类型 UPDATE
  - Delete<T>(tableName, model)：支持匿名类型 DELETE
  - 用法：`FastWrite.QueueBuilder().Add("TableName", new { ... }).Execute()`
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
- **分表功能（数据分片）**
  - IShardingStrategy 接口：分表策略抽象
  - ShardingConfig：分表配置模型（Time/Hash/List/Composite/Geo/QueryFrequency）
  - ShardingManager：分表管理器（策略注册/配置/管理）
  - TimeShardingStrategy：时间分表策略（Day/Week/Month/Quarter/Year 粒度）
  - HashShardingStrategy：哈希分表策略（Modulo/Consistent/CRC32 算法）
  - ListShardingStrategy：列表分表策略（枚举值映射）
  - CompositeShardingStrategy：组合键分表策略（组合键哈希/拼接）
  - QueryFrequencyShardingStrategy：查询频率分表策略（热数据/冷数据分离）
  - ShardingReadHelper：分表查询助手（Query/QueryPage）
  - ShardingWriteHelper：分表写入助手（Add/AddList/Update/Delete）
  - ShardingTests：21 个分表单元测试
  - README.md：分表功能完整文档
- **SyncTool 分表迁移组件**
  - ShardingMigrationControl：分表迁移管理组件
  - 支持 Time/Hash/List/Composite 分表类型
  - 批量迁移、进度显示、状态监控
  - 自动创建分表配置
- **链式分表查询 API**
  - DataQuery<T>.UseSharding()：启用分表查询
  - DataQuery<T>.WithShardingParam()：添加分表参数
  - DataQuery<T>.WithTimeRange()：时间范围分表参数
  - DataQuery<T>.WithHashField()：哈希字段分表参数
  - DataQuery<T>.WithListField()：列表字段分表参数
  - DataQuery<T>.WithShardingConfig()：覆盖全局分表配置
  - 默认不开启分表，需显式调用 UseSharding()
- **FastData.Example 分表示例**
  - ShardingExample.cs：覆盖所有分表策略和 API
  - Program.cs 新增选项 11：分表（数据分片）
- **FastData.Demo 分表 API**
  - ShardingController：分表功能演示控制器
  - 支持 Time/Hash/List/Composite/QueryFrequency 配置和查询
  - 查询频率记录和热数据查询 API
- **SyncTool 分表 CRUD 界面**
  - ShardingCrudControl：分表管理组件
  - 支持配置所有分表策略（Time/Hash/List/Composite/QueryFrequency）
  - 分表列表查询和条件查询
  - 数据查询和导出功能
- **分表 CRUD 测试**
  - ShardingCrudTests：33 个分表 CRUD 测试用例
  - 覆盖配置、启用/禁用、增删改查、链式 API
  - 总计 162 个单元测试全部通过

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
