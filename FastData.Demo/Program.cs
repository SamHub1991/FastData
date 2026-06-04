using FastData.Demo.Repositories;
using FastData.Demo.Services;
using FastData.Repository;
using FastData.Config;
using FastData;
using System.Data.Common;

DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySql.Data.MySqlClient.MySqlClientFactory.Instance);
DbProviderFactories.RegisterFactory("System.Data.SQLite", System.Data.SQLite.SQLiteFactory.Instance);
DbProviderFactories.RegisterFactory("Npgsql", Npgsql.NpgsqlFactory.Instance);

var builder = WebApplication.CreateBuilder(args);

var _ = FastDataConfig.GetConnectionSummaries();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
builder.Services.AddSingleton<IUserCacheService, UserCacheService>();

builder.Services.AddScoped<IFastRepository, FastRepository>();
builder.Services.AddScoped<IReadRepository>(sp => sp.GetRequiredService<IFastRepository>());
builder.Services.AddScoped<IWriteRepository>(sp => sp.GetRequiredService<IFastRepository>());
builder.Services.AddScoped<IMapRepository>(sp => sp.GetRequiredService<IFastRepository>());

builder.Services.AddScoped<IUserRepository>(sp => new UserRepository("SqlServer"));
builder.Services.AddScoped<IOrderRepository>(sp => new OrderRepository("SqlServer"));

builder.Services.AddScoped<IDataSyncService, DataSyncService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () =>
{
    var active = Environment.GetEnvironmentVariable("FASTDATA_ACTIVE") ?? "dev";
    return new
    {
        Service = "FastData Demo API",
        Version = "1.0.0",
        Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        Environment = active,
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
            "GET  /api/config/environment - 获取当前环境",
            "GET  /api/config/connections - 获取数据库连接配置（脱敏）",
            "GET  /api/config/connections/{key} - 获取指定连接配置（脱敏）",
            "GET  /api/config/redis - 获取 Redis 配置（脱敏）",
            "GET  /api/config/summary - 获取完整配置概览",
        }
    };
});

FastDb.ConfigureLogging(app.Services.GetRequiredService<ILoggerFactory>());
FastDb.EnableSqlLog = true;
FastDb.SlowQueryThresholdMs = 500;

app.Run();