# FastData.Demo

FastData.Demo 是 ASP.NET Core Web API 示例程序，演示 FastData 技术栈的完整使用：Repository 模式、Redis 缓存、数据同步、消息队列、分页和分表。

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
