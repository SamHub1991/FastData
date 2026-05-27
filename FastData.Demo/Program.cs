using FastData.Demo.Repositories;
using FastData.Demo.Services;
using FastRedis;
using FastRedis.Repository;

var builder = WebApplication.CreateBuilder(args);

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册 Redis 服务（单例模式）
builder.Services.AddSingleton<IRedisRepository, RedisRepository>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IUserCacheService, UserCacheService>();

// 注册仓储服务
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 注册数据同步服务
builder.Services.AddScoped<IDataSyncService, DataSyncService>();

// 初始化 Redis
RedisInfo.Init();

var app = builder.Build();

// 配置中间件
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// 健康检查端点
app.MapGet("/", () => new
{
    Service = "FastData Demo API",
    Version = "1.0.0",
    Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    Timestamp = DateTime.Now,
    Endpoints = new[]
    {
        "GET  /api/users - 获取所有用户",
        "GET  /api/users/{id} - 获取用户详情",
        "GET  /api/users/active - 获取活跃用户",
        "POST /api/users - 创建用户",
        "PUT  /api/users/{id} - 更新用户",
        "DELETE /api/users/{id} - 删除用户",
        "GET  /api/orders - 获取所有订单",
        "GET  /api/orders/{id} - 获取订单详情",
        "POST /api/orders - 创建订单",
        "POST /api/sync/all - 同步所有表",
        "GET  /api/health - 健康检查"
    }
});

app.Run();
