# FastData 项目需求规格

## 1. 产品定位

FastData 是一款面向 .NET 生态的轻量级多目标框架 ORM，核心能力包括 Lambda 查询、XML Map SQL、Code First / Db First、AOP、缓存、Redis、消息队列和数据同步。

## 2. 功能需求

### 2.1 ORM 核心能力

| 需求编号 | 描述 | 状态 |
|---------|------|------|
| R-ORM-01 | Lambda 查询支持（Where/Or/And/Like/Contains/In/Between） | ✅ 完成 |
| R-ORM-02 | 链式查询（Select/OrderBy/GroupBy/UseSharding） | ✅ 完成 |
| R-ORM-03 | 分页查询（ToPagination 简化 API） | ✅ 完成 |
| R-ORM-04 | XML Map SQL 动态查询 | ✅ 完成 |
| R-ORM-05 | Repository 模式分层接口（IRead/IWrite/IMap） | ✅ 完成 |
| R-ORM-06 | 多数据库切换（默认库/指定库/作用域） | ✅ 完成 |
| R-ORM-07 | AOP 拦截器（Before/After/Exception） | ✅ 完成 |
| R-ORM-08 | 消息队列（ReliableQueue/Stream） | ✅ 完成 |
| R-ORM-09 | 分表（时间/哈希/列表/组合键/查询频率） | ✅ 完成 |
| R-ORM-10 | 数据同步（中间库模式/增量/全量） | ✅ 完成 |

### 2.2 多目标框架

| 需求编号 | 描述 | 状态 |
|---------|------|------|
| R-FWK-01 | 支持 .NET Framework 4.5 | ✅ 完成 |
| R-FWK-02 | 支持 .NET 6.0 | ✅ 完成 |
| R-FWK-03 | 支持 .NET 8.0 | ✅ 完成 |
| R-FWK-04 | 支持 .NET 10.0 | ✅ 完成 |
| R-FWK-05 | 条件编译（NETFRAMEWORK/NET6_0_OR_GREATER） | ✅ 完成 |
| R-FWK-06 | Framework 分版本 Redis 实现（NServiceKit/NewLife） | ✅ 完成 |

### 2.3 数据库支持

| 需求编号 | 描述 | 状态 |
|---------|------|------|
| R-DB-01 | SQL Server 支持 | ✅ 完成 |
| R-DB-02 | MySQL 支持 | ✅ 完成 |
| R-DB-03 | PostgreSQL 支持 | ✅ 完成 |
| R-DB-04 | Oracle 支持 | ✅ 完成 |
| R-DB-05 | SQLite 支持 | ✅ 完成 |
| R-DB-06 | DB2 支持 | ✅ 完成 |

### 2.4 ModelGenerator（代码生成工具）

| 需求编号 | 描述 | 状态 |
|---------|------|------|
| R-GEN-01 | 连接管理（保存/测试/删除） | ✅ 完成 |
| R-GEN-02 | 从数据库表生成 C# Model | ✅ 完成 |
| R-GEN-03 | 生成 XML Map SQL | ✅ 完成 |
| R-GEN-04 | 分层代码生成（Repository/Service/Controller/Demo） | ✅ 完成 |
| R-GEN-05 | JSON 转 C# Model | ✅ 完成 |
| R-GEN-06 | API 代码生成（RestSharp + 认证） | ✅ 完成 |
| R-GEN-07 | 代码预览 + 批量生成 | ✅ 完成 |

### 2.5 SyncTool（数据同步工具）

| 需求编号 | 描述 | 状态 |
|---------|------|------|
| R-SYNC-01 | 跨数据库同步（SQL Server/MySQL/PG/SQLite） | ✅ 完成 |
| R-SYNC-02 | 中间库模式（源库→中间库→目标库） | ✅ 完成 |
| R-SYNC-03 | 增量同步（时间字段） | ✅ 完成 |
| R-SYNC-04 | 全量同步（主键） | ✅ 完成 |
| R-SYNC-05 | 失败重试 + 失败记录恢复 | ✅ 完成 |
| R-SYNC-06 | 定时调度 | ✅ 完成 |
| R-SYNC-07 | 去重模式（AlwaysDeduplicate） | ✅ 完成 |
| R-SYNC-08 | 分表同步 | ✅ 完成 |
| R-SYNC-09 | 数据补录（时间范围重放） | ✅ 完成 |

---

## 3. 非功能性需求

| 需求编号 | 描述 | 状态 |
|---------|------|------|
| NF-01 | Linux 环境构建支持（net45 需 FrameworkPathOverride） | ✅ 完成 |
| NF-02 | NuGet 包发布（Fast.Data） | ✅ 完成 |
| NF-03 | xUnit 测试框架（73 个测试全部通过） | ✅ 完成 |
| NF-04 | 接口与实现分离（核心包不引用 WinForms） | ✅ 完成 |
| NF-05 | 代码去重（AsyncHelper 减少 90% 重复） | ✅ 完成 |
| NF-06 | 包体积控制（工具独立于核心包） | ✅ 完成 |

---

## 4. 验收标准

| 编号 | 标准 | 状态 |
|------|------|------|
| AC-01 | ORM 核心 0 错误编译 | ✅ |
| AC-02 | 73 个单元测试全部通过 | ✅ |
| AC-03 | ModelGenerator 可编译并运行 | ✅ |
| AC-04 | SyncTool 可编译并运行 | ✅ |
| AC-05 | 四个数据库 CRUD 全部正常 | ✅ |
| AC-06 | 分表查询写入正常 | ✅ |
| AC-07 | 消息队列生产消费正常 | ✅ |
| AC-08 | 文档覆盖所有功能 | ✅ |

---

**最后更新**: 2026-05-29
