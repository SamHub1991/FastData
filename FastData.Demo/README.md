# FastData Demo 项目

这是一个完整的 FastData 技术栈验证项目，演示了从后端 API、Model 生成到数据同步的完整流程。

## 项目结构

```
FastData.Demo/
├── Controllers/           # API 控制器
│   └── UsersController.cs # 用户、订单、同步、健康检查 API
├── Models/                # 实体模型
│   └── Entities.cs        # User, Order, Product, OperationLog
├── Repositories/          # 仓储层
│   └── UserRepository.cs  # 用户和订单仓储实现
├── Services/              # 服务层
│   ├── CacheService.cs    # Redis 缓存服务
│   └── DataSyncService.cs # 数据同步服务
├── Configs/               # 配置文件
│   ├── db.config          # 数据库和 Redis 配置
│   └── init-database.sql  # 数据库初始化脚本
├── Program.cs             # 应用入口
└── FastData.Demo.csproj   # 项目文件
```

## 技术栈验证点

### 1. 多目标框架支持
- .NET 10.0 运行时
- 条件编译处理框架差异

### 2. 数据库配置
- 多数据库支持（SQL Server, MySQL, SQLite）
- 连接字符串配置
- 连接字符串加密（IsEncrypt）

### 3. Repository 模式
- IReadRepository / IWriteRepository 分层接口
- 依赖注入
- 异步操作

### 4. Redis 缓存集成
- NewLife.Redis 单例模式
- 缓存穿透防护（GetOrAdd）
- 计数器操作（Increment）
- 过期时间管理

### 5. 数据同步
- 同步配置（TableSyncConfig）
- 增量同步（时间范围）
- 批量同步

## 快速开始

### 1. 初始化数据库

```bash
# SQL Server
sqlcmd -S . -U sa -P YourPassword123 -i Configs/init-database.sql

# 或使用 SSMS 执行 init-database.sql
```

### 2. 修改配置

编辑 `Configs/db.config`，修改数据库连接字符串：

```xml
<Add Provider="SqlServer" 
     Key="DefaultDb" 
     ConnStr="server=.;database=FastDataDemo;uid=sa;pwd=你的密码" 
     IsDefault="true" 
     DesignModel="DbFirst" 
     CacheType="web" />
```

### 3. 运行项目

```bash
cd FastData.Demo
dotnet run
```

### 4. 访问 API

打开浏览器访问：`http://localhost:5000`

## API 端点

### 用户 API

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | /api/users | 获取所有用户 |
| GET | /api/users/{id} | 获取用户详情（带缓存） |
| GET | /api/users/active | 获取活跃用户（带缓存） |
| GET | /api/users/department/{dept} | 按部门查询 |
| GET | /api/users/paged?pageIndex=1&pageSize=20 | 分页查询 |
| POST | /api/users | 创建用户 |
| PUT | /api/users/{id} | 更新用户 |
| DELETE | /api/users/{id} | 删除用户 |

### 订单 API

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | /api/orders | 获取所有订单 |
| GET | /api/orders/{id} | 获取订单详情（带缓存） |
| GET | /api/orders/user/{userId} | 获取用户订单 |
| POST | /api/orders | 创建订单 |
| PUT | /api/orders/{id}/status?status=1 | 更新订单状态 |

### 数据同步 API

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | /api/sync/all | 同步所有表 |
| POST | /api/sync/users | 同步用户表 |
| POST | /api/sync/orders | 同步订单表 |

### 健康检查

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | /api/health | 健康检查 |
| GET | / | 服务信息 |

## 测试示例

### 创建用户

```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "测试用户",
    "email": "test@example.com",
    "phone": "13900139000",
    "age": 25,
    "department": "技术部",
    "salary": 15000.00
  }'
```

### 获取用户

```bash
# 获取所有用户
curl http://localhost:5000/api/users

# 获取单个用户（带缓存）
curl http://localhost:5000/api/users/1

# 获取活跃用户（带缓存）
curl http://localhost:5000/api/users/active
```

### 创建订单

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "productName": "测试商品",
    "quantity": 2,
    "unitPrice": 99.99,
    "totalAmount": 199.98
  }'
```

### 执行数据同步

```bash
# 同步所有表
curl -X POST http://localhost:5000/api/sync/all

# 同步用户表
curl -X POST http://localhost:5000/api/sync/users
```

## 架构说明

### 依赖注入配置

```csharp
// 注册 Redis 服务（单例模式）
builder.Services.AddSingleton<IRedisRepository, RedisRepository>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IUserCacheService, UserCacheService>();

// 注册仓储服务
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 注册数据同步服务
builder.Services.AddScoped<IDataSyncService, DataSyncService>();
```

### 缓存策略

```csharp
// 缓存穿透防护
var user = await _cacheService.GetUserAsync(id, async () =>
{
    return await _userRepository.GetByIdAsync(id);
});

// 写入缓存
await _cacheService.SetAsync(key, value, hours: 2);

// 删除缓存
await _cacheService.RemoveAsync(key);
```

### Repository 模式

```csharp
public class UserRepository : IUserRepository
{
    private readonly IReadRepository _readRepo;
    private readonly IWriteRepository _writeRepo;

    public UserRepository(IReadRepository readRepo, IWriteRepository writeRepo)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _readRepo.QueryAsy<User>("SELECT * FROM Users", null);
    }

    public async Task<int> AddAsync(User user)
    {
        var result = await _writeRepo.AddAsy(user);
        return result.IsSuccess ? 1 : 0;
    }
}
```

## 验证清单

| 验证项 | 状态 | 说明 |
|--------|------|------|
| 项目构建 | ✅ | net10.0 构建成功 |
| 依赖注入 | ✅ | Repository、Service 注册 |
| Repository 模式 | ✅ | IReadRepository/IWriteRepository |
| Redis 缓存 | ✅ | 单例模式、GetOrAdd |
| API 控制器 | ✅ | CRUD 操作 |
| 数据同步服务 | ✅ | 配置和执行同步 |
| 健康检查 | ✅ | 服务状态和 Redis 连接 |

## 下一步

1. 连接真实数据库执行 API 测试
2. 验证 Redis 缓存命中
3. 执行数据同步验证
4. 性能测试和压力测试
