using FastData.Demo.Repositories;
using FastData.Demo.Services;
using FastRedis;
using FastRedis.Messaging;
using FastRedis.Repository;
using FastRedis.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加配置文件
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 从配置读取 Redis 设置
var redisServer = builder.Configuration["Redis:Server"] ?? "127.0.0.1:6379";
var redisDb = int.Parse(builder.Configuration["Redis:Db"] ?? "7");

// 注册 Redis 服务（单例模式）
builder.Services.AddSingleton<IRedisRepository, RedisRepository>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IUserCacheService, UserCacheService>();

// 注册消息队列服务（单例模式）
builder.Services.AddSingleton<MessageQueueFactory>(sp =>
{
    var redis = new NewLife.Caching.FullRedis
    {
        Server = redisServer,
        Db = redisDb,
        Timeout = 15000
    };
    return new MessageQueueFactory(redis);
});
builder.Services.AddSingleton<MessageQueueIntegrationService>(sp =>
{
    var redis = new NewLife.Caching.FullRedis
    {
        Server = redisServer,
        Db = redisDb,
        Timeout = 15000
    };
    return new MessageQueueIntegrationService(redis);
});
builder.Services.AddSingleton<MessageQueueService>();

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
        "POST /api/mq/demo/reliable - 消息队列示例（可信队列）",
        "POST /api/mq/demo/stream - 消息队列示例（多消费组）",
        "POST /api/mq/demo/write-queue - FastWrite 链式 API 示例（写入后端队列）",
        "POST /api/mq/demo/read-queue - FastRead 链式 API 示例（查询队列）",
        "GET  /api/mq/status/{topic} - 获取队列状态",
        "GET  /api/health - 健康检查"
    }
});

// 消息队列示例端点
app.MapPost("/api/mq/demo/reliable", async (MessageQueueService mqService) =>
{
    try
    {
        var count = await mqService.DemoReliableQueueAsync();
        return Results.Ok(new
        {
            Success = true,
            Message = $"可信队列示例完成，发布了 {count} 条数据",
            QueueType = "ReliableQueue",
            Description = "场景：RTU 数据上传 → 队列缓冲 → 批量写入数据库（削峰）"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Error = ex.Message });
    }
});

app.MapPost("/api/mq/demo/stream", async (MessageQueueService mqService) =>
{
    try
    {
        var count = await mqService.DemoStreamMultiGroupAsync();
        return Results.Ok(new
        {
            Success = true,
            Message = $"多消费组示例完成，发布了 {count} 条数据",
            QueueType = "Stream",
            Description = "场景：RTU 数据 → Stream → [数据库存储, 告警系统, 数据分析]（多方推送）"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Error = ex.Message });
    }
});

app.MapGet("/api/mq/status/{topic}", (string topic, string type, MessageQueueService mqService) =>
{
    try
    {
        var queueType = type?.ToLower() == "stream" ? MessageQueueType.Stream : MessageQueueType.ReliableQueue;
        var status = mqService.GetQueueStatus(topic, queueType);
        return Results.Ok(status);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Error = ex.Message });
    }
});

// FastWrite 链式 API 示例（写入后端队列）
app.MapPost("/api/mq/demo/write-queue", (MessageQueueService mqService) =>
{
    try
    {
        var result = mqService.DemoFastWriteQueue();
        return Results.Ok(new
        {
            Success = result.Success,
            Message = "FastWrite 链式 API 示例完成",
            DirectWriteCount = result.DirectWriteCount,
            QueuedCount = result.QueuedCount,
            FailedCount = result.FailedCount,
            FallbackOccurred = result.FallbackOccurred,
            Description = "场景：数据库异常自动降级到可信队列，恢复后自动刷写"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Error = ex.Message });
    }
});

// FastRead 链式 API 示例（查询队列）
app.MapPost("/api/mq/demo/read-queue", (MessageQueueService mqService) =>
{
    try
    {
        var result = mqService.DemoFastReadQueue();
        return Results.Ok(new
        {
            Success = result.Success,
            Message = "FastRead 链式 API 示例完成",
            QueuedCount = result.QueuedCount,
            FailedCount = result.FailedCount,
            Description = "场景：将查询请求推送到消息队列，支持扩展元数据"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Success = false, Error = ex.Message });
    }
});

app.Run();
