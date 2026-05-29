# FastData 项目任务清单

## 1. ORM 核心能力

| 编号 | 任务 | 状态 |
|------|------|------|
| T-001 | Lambda 查询（Where/Or/And/Like/In/Between） | ✅ |
| T-002 | 链式查询（Where/Select/OrderBy/GroupBy） | ✅ |
| T-003 | 分页查询（ToPagination） | ✅ |
| T-004 | 匿名类型投影查询 | ✅ |
| T-005 | XML Map SQL 动态查询 | ✅ |
| T-006 | Repository 分层接口（IRead/IWrite/IMap） | ✅ |
| T-007 | 多数据库切换（Use/作用域/Repository 工厂） | ✅ |
| T-008 | AOP 拦截器 | ✅ |
| T-009 | 连接字符串加密（BaseSymmetric） | ✅ |
| T-010 | 数据同步服务 | ✅ |

## 2. 多目标框架

| 编号 | 任务 | 状态 |
|------|------|------|
| T-100 | 统一 SDbectory-style csproj | ✅ |
| T-101 | 条件编译（NETFRAMEWORK/NET6_0_OR_GREATER） | ✅ |
| T-102 | CallContext → AsyncLocal 迁移 | ✅ |
| T-103 | Redis 双实现（NServiceKit/NewLife） | ✅ |
| T-104 | Newtonsoft.Json 升级（6.0.8 → 13.0.3） | ✅ |
| T-105 | NPOI 分版本（2.5.6/2.7.0） | ✅ |
| T-106 | xUnit 测试框架迁移 | ✅ |
| T-107 | NuGet 包生成脚本 | ✅ |
| T-108 | Linux 构建支持（FrameworkPathOverride） | ✅ |

## 3. Redis 与消息队列

| 编号 | 任务 | 状态 |
|------|------|------|
| T-200 | Redis 单例模式（Lazy<FullRedis>） | ✅ |
| T-201 | Rudis 缓存操作（Get/Set/Remove/GetOrAdd） | ✅ |
| T-202 | RunnableQueue 可信队列 | ✅ |
| T-203 | RedisStream 多消费组队列 | ✅ |
| T-204 | MessageQueueFactory 工厂 | ✅ |
| T-205 | MessageQueueIntegrationService 集成服务 | ✅ |
| T-206 | Redis Docker 部署指南 | ✅ |

## 4. 分表

| 编号 | 任务 | 状态 |
|------|------|------|
| T-300 | ShardingManager 分表管理器 | ✅ |
| T-301 | TimeShardingStrategy 时间分表 | ✅ |
| T-302 | HashShardingStrategy 哈希分表 | ✅ |
| T-303 | ListShardingStrategy 列表分表 | ✅ |
| T-304 | CompositeShardingStrategy 组合键分表 | ✅ |
| T-305 | QueryFrequencyShardingStrategy 查询频率分表 | ✅ |
| T-306 | 链式分表查询 API（UseSharding/WithTimeRange） | ✅ |
| T-307 | 自定义分表策略（IShardingStrategy） | ✅ |

## 5. ModelGenerator（代码生成工具）

| 编号 | 任务 | 状态 |
|------|------|------|
| T-400 | Tab 1: 连接管理（保存/测试/删除/加载） | ✅ |
| T-401 | Tab 2: Model 生成（批量选择/预览/导出） | ✅ |
| T-402 | Tab 3: XML Map 生成（CRUD/动态条件） | ✅ |
| T-403 | Tab 4: 代码生成（Repository/Service/Controller/Demo） | ✅ |
| T-404 | EnhancedCodeGenerator（全功能选项） | ✅ |
| T-405 | Tab 5: JSON 转 Model（类型推断/嵌套/数组） | ✅ |
| T-406 | Tab 6: API 代码生成（RestSharp/认证/响应 Model） | ✅ |
| T-407 | 连接持久化（db_connections.json） | ✅ |

## 6. SyncTool（数据同步工具）

| 编号 | 任务 | 状态 |
|------|------|------|
| T-500 | 跨数据库同步（SQL Server/MySQL/PG/SQLite） | ✅ |
| T-501 | 中间库模式（源库→中间库→目标库） | ✅ |
| T-502 | 增量同步 + 全量同步 | ✅ |
| T-503 | 失败重试 + 失败记录恢复 | ✅ |
| T-504 | 定时调度（Timer） | ✅ |
| T-505 | AlwaysDeduplicate 去重模式 | ✅ |
| T-506 | 分表同步（5 种策略） | ✅ |
| T-507 | 数据补录（时间范围重放） | ✅ |
| T-508 | SyncTool 代码重构（UserControl 模块化） | ✅ |

## 7. 测试与验证

| 编号 | 任务 | 状态 |
|------|------|------|
| T-600 | xUnit 测试框架（73 个测试） | ✅ |
| T-601 | Docker 数据库环境 | ✅ |
| T-602 | 多目标框架构建验证 | ✅ |
| T-603 | 综合验证测试（34 项） | ✅ |
| T-604 | 30 线程全端点覆盖测试（99.4% 成功率） | ✅ |

## 8. 文档

| 编号 | 任务 | 状态 |
|------|------|------|
| T-700 | 主 README 重写 | ✅ |
| T-701 | ModelGenerator 使用手册 | ✅ |
| T-702 | SyncTool 使用手册 | ✅ |
| T-703 | 需求/设计/任务文档重写 | ✅ |
| T-704 | MEMORY.md 更新 | ✅ |
| T-705 | 项目重构（删除 FastData.Shared） | ✅ |

---

## 当前状态

| 类别 | 完成 | 总计 | 进度 |
|------|------|------|------|
| ORM 核心 | 10 | 10 | 100% |
| 多目标框架 | 9 | 9 | 100% |
| Redis/消息队列 | 7 | 7 | 100% |
| 分表 | 8 | 8 | 100% |
| ModelGenerator | 8 | 8 | 100% |
| SyncTool | 9 | 9 | 100% |
| 测试/验证 | 5 | 5 | 100% |
| 文档 | 5 | 5 | 100% |
| **总计** | **61** | **61** | **100%** |

---

**最后更新**: 2026-05-29
