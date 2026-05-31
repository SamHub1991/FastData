# FastData.Demo

FastData.Demo 是 ASP.NET Core Web API 示例程序，演示 FastData 技术栈的完整使用：Repository 模式、Redis 缓存、数据同步、消息队列、分页和分表。

**最新更新 (2026-05-31)**:
- ✅ 新增 `ReportController` - 报表统计 (GroupBy/Join/聚合/导出)
- ✅ 新增 `DataExportController` - 数据导出 (ToDics/ToDataTable/ToArray)
- ✅ 新增 `DynamicQueryController` - 动态查询 (Where 构建器/Any/All/First/Single)
- ✅ 新增 `DataValidationController` - 数据校验 (NullSafety/字段验证/异常处理)

## 目标框架

`net10.0` (.NET 10)

## 功能特性

- **FastDataClient**: 统一门面，整合所有功能
- **Repository 模式**: IFastRepository 读写分离
- **Redis 缓存**: 分布式缓存，支持 TTL
- **数据同步**: 后台数据同步
- **消息队列**: ReliableQueue 和 Stream，使用链式 API
- **分页查询**: `PaginationResult<T>` 和 `ToPage()` 方法
- **分表**: 时间/哈希/列表/组合/查询频率策略
- **Swagger UI**: 交互式 API 文档

## API 端点

### Users

| 方法 | 端点 | 说明 |
|------|------|------|
| `GET` | `/api/users` | 获取所有用户 |
| `GET` | `/api/users/{id}` | 根据 ID 获取用户（缓存） |
| `GET` | `/api/users/active` | 获取活跃用户（缓存） |
| `GET` | `/api/users/department/{dept}` | 根据部门获取用户 |
| `GET` | `/api/users/paged` | 分页用户列表 |
| `GET` | `/api/users/search` | 动态 Where<T> 搜索 |
| `POST` | `/api/users` | 创建用户 |
| `PUT` | `/api/users/{id}` | 更新用户 |
| `DELETE` | `/api/users/{id}` | 删除用户 |

### Orders

| 方法 | 端点 | 说明 |
|------|------|------|
| `GET` | `/api/orders` | 获取所有订单 |
| `GET` | `/api/orders/{id}` | 根据 ID 获取订单（缓存） |
| `GET` | `/api/orders/user/{userId}` | 根据用户获取订单 |
| `POST` | `/api/orders` | 创建订单 |
| `PUT` | `/api/orders/{id}/status` | 更新订单状态 |

### Sync

| 方法 | 端点 | 说明 |
|------|------|------|
| `POST` | `/api/sync/all` | 同步所有表 |
| `POST` | `/api/sync/users` | 同步用户表 |
| `POST` | `/api/sync/orders` | 同步订单表 |

### Pagination

| 方法 | 端点 | 说明 |
|------|------|------|
| `GET` | `/api/pagination/users` | 分页用户 |
| `POST` | `/api/pagination/users/search` | 分页搜索 |
| `GET` | `/api/pagination/users/department/{dept}` | 部门分页 |
| `GET` | `/api/pagination/users/async` | 异步分页 |
| `GET` | `/api/pagination/users/dictionary` | 字典分页 |

### Sharding

| 方法 | 端点 | 说明 |
|------|------|------|
| `POST` | `/api/sharding/init` | 初始化分表 |
| `POST` | `/api/sharding/insert-data` | 插入测试数据 |
| `POST` | `/api/sharding/time/configure` | 配置时间分表 |
| `GET` | `/api/sharding/time/query` | 查询时间分表数据 |
| `POST` | `/api/sharding/hash/configure` | 配置哈希分表 |
| `GET` | `/api/sharding/hash/query` | 查询哈希分表数据 |
| `POST` | `/api/sharding/list/configure` | 配置列表分表 |
| `GET` | `/api/sharding/list/query` | 查询列表分表数据 |
| `POST` | `/api/sharding/frequency/configure` | 配置查询频率分表 |
| `POST` | `/api/sharding/frequency/record` | 记录查询频率 |
| `POST` | `/api/sharding/frequency/simulate` | 模拟查询 |
| `GET` | `/api/sharding/frequency/hot` | 获取热数据值 |
| `POST` | `/api/sharding/sync` | 同步分表数据 |
| `GET` | `/api/sharding/stats` | 获取分表统计 |

### Message Queue

| 方法 | 端点 | 说明 |
|------|------|------|
| `POST` | `/api/mq/demo/reliable` | ReliableQueue 示例 |
| `POST` | `/api/mq/demo/stream` | Stream 示例 |
| `POST` | `/api/mq/demo/write-queue` | 写入队列示例 |
| `POST` | `/api/mq/demo/read-queue` | 读取队列示例 |
| `GET` | `/api/mq/status/{topic}` | 获取队列状态 |

### Health

| 方法 | 端点 | 说明 |
|------|------|------|
| `GET` | `/api/health` | 健康检查 |
| `GET` | `/` | 服务信息 |

### Report (新增)

| 方法 | 端点 | 说明 |
|------|------|------|
| `GET` | `/api/report/user-stats` | 用户统计报表 (GroupBy) |
| `GET` | `/api/report/order-stats` | 订单统计报表 |
| `GET` | `/api/report/monthly-trend` | 月度趋势报表 |
| `GET` | `/api/report/user-order-report` | 用户订单报表 (Join) |
| `GET` | `/api/report/export/json` | 导出为 JSON |
| `GET` | `/api/report/export/datatable` | 导出为 DataTable |
| `GET` | `/api/report/export/dics` | 导出为字典列表 |
| `GET` | `/api/report/projection` | 投影查询 |
| `GET` | `/api/report/aggregate` | 聚合统计 |
| `GET` | `/api/report/groupby-json` | 分组聚合 JSON |
| `GET` | `/api/report/paged-report` | 分页统计报表 |

### DataExport (新增)

| 方法 | 端点 | 说明 |
|------|------|------|
| `GET` | `/api/dataexport/dics` | 导出为字典列表 |
| `GET` | `/api/dataexport/datatable` | 导出为 DataTable |
| `GET` | `/api/dataexport/array` | 导出为数组 |
| `GET` | `/api/dataexport/projection` | 投影导出 |
| `GET` | `/api/dataexport/orders` | 订单导出 |
| `GET` | `/api/dataexport/json` | 导出为 JSON |
| `GET` | `/api/dataexport/paged` | 分页导出 |
| `GET` | `/api/dataexport/dictionary` | 字典导出 |
| `GET` | `/api/dataexport/stats` | 统计导出 |

### DynamicQuery (新增)

| 方法 | 端点 | 说明 |
|------|------|------|
| `GET` | `/api/dynamicquery/search` | 动态条件查询 |
| `GET` | `/api/dynamicquery/or-query` | OR 条件查询 |
| `GET` | `/api/dynamicquery/any` | Any 存在性判断 |
| `GET` | `/api/dynamicquery/all` | All 全量判断 |
| `GET` | `/api/dynamicquery/first` | First 查询 |
| `GET` | `/api/dynamicquery/single` | Single 查询 |
| `GET` | `/api/dynamicquery/in` | In 查询 |
| `GET` | `/api/dynamicquery/between` | Between 范围查询 |
| `GET` | `/api/dynamicquery/like` | Like 模糊查询 |
| `GET` | `/api/dynamicquery/complex` | 复杂动态查询 |
| `GET` | `/api/dynamicquery/count` | 统计查询 |

### DataValidation (新增)

| 方法 | 端点 | 说明 |
|------|------|------|
| `GET` | `/api/datavalidation/null-safety` | 空值安全查询 |
| `GET` | `/api/datavalidation/null-safety-batch` | 批量空值安全查询 |
| `POST` | `/api/datavalidation/validate-add` | 字段验证 - 添加用户 |
| `PUT` | `/api/datavalidation/validate-update/{id}` | 字段验证 - 更新用户 |
| `GET` | `/api/datavalidation/safe-query` | 异常处理 - 安全查询 |
| `POST` | `/api/datavalidation/safe-write` | 异常处理 - 安全写入 |
| `POST` | `/api/datavalidation/safe-batch-write` | 批量写入异常处理 |
| `GET` | `/api/datavalidation/write-return` | WriteReturn 信息展示 |
| `GET` | `/api/datavalidation/null-safety-collection` | 空值安全 - 集合操作 |
| `POST` | `/api/datavalidation/integrity-check` | 数据完整性验证 |

## 配置

### appsettings.json
```json
{
  "DataConfig": {
    "Default": "DefaultDb",
    "Connections": [
      {
        "Provider": "SqlServer",
        "Key": "DefaultDb",
        "ConnStr": "Server=.;Database=FastDataDemo;Trusted_Connection=true;"
      }
    ]
  },
  "Redis": {
    "Server": "localhost:6379",
    "Db": "0"
  }
}
```

## 运行

```bash
# 运行示例
dotnet run --project FastData.Demo --urls "http://0.0.0.0:5000"

# 访问 Swagger UI
# http://localhost:5000/swagger
```

## 构建

```bash
dotnet build FastData.Demo
```

## 依赖

- FastData
- FastRedis
- FastUntility
- FastData.Tooling
- Swashbuckle.AspNetCore 6.5.0
- Microsoft.Data.SqlClient 5.2.0

## 许可证

MIT License - see [LICENSE](../LICENSE) for details.
