#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastData;
using FastData.Property;
using FastData.Queue;
using FastRedis;
using FastRedis.Messaging;

namespace FastData.Example.Example
{
    /// <summary>
    /// FastDataClient 统一入口示例
    /// 
    /// 演示使用 FastDataClient 作为统一入口进行：
    /// 1. 数据库查询和写入
    /// 2. Redis 缓存操作
    /// 3. 消息队列操作
    /// </summary>
    public static class FastDataClientExample
    {
        #region Model 定义

        /// <summary>
        /// 用户模型
        /// </summary>
        [Table(Name = "Users")]
        [Cache(IsEnable = true, ExpireTime = 300, Key = "user:{Id}", CacheType = "Redis")]
        public class User
        {
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            [Column(Length = 50)]
            public string UserName { get; set; }

            [Column(Length = 100)]
            public string Email { get; set; }

            public bool IsActive { get; set; }

            public DateTime CreateTime { get; set; }
        }

        /// <summary>
        /// 订单模型
        /// </summary>
        [Table(Name = "Orders")]
        [Cache(IsEnable = true, ExpireTime = 600, Key = "order:{Id}", CacheType = "Redis")]
        public class Order
        {
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            public int UserId { get; set; }

            [Column(Length = 50)]
            public string OrderNo { get; set; }

            public decimal TotalAmount { get; set; }

            [Column(Length = 20)]
            public string Status { get; set; }

            public DateTime CreateTime { get; set; }
        }

        /// <summary>
        /// 传感器数据模型
        /// </summary>
        public class SensorData
        {
            public string DeviceId { get; set; }
            public double Temperature { get; set; }
            public double Humidity { get; set; }
            public DateTime Timestamp { get; set; }
        }

        #endregion

        /// <summary>
        /// 运行所有示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  FastDataClient 统一入口示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 创建客户端实例
            var client = new FastDataClient("db1");

            // 启用 SQL 日志（可选）
            client.EnableSqlLog();

            RunCrudExample(client);
            Console.WriteLine();
            RunCacheExample(client);
            Console.WriteLine();
            RunQueueExample(client);
            Console.WriteLine();
            RunAdvancedExample(client);
        }

        #region CRUD 操作示例

        /// <summary>
        /// CRUD 操作示例
        /// </summary>
        private static void RunCrudExample(FastDataClient client)
        {
            Console.WriteLine("【1】CRUD 操作示例");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            // 1.1 添加数据
            Console.WriteLine("1.1 添加数据:");
            Console.WriteLine(@"
    // 单条添加
    var user = new User 
    { 
        UserName = ""张三"", 
        Email = ""zhangsan@example.com"", 
        IsActive = true, 
        CreateTime = DateTime.Now 
    };
    var result = client.Add(user);
    if (result.IsSuccess)
        Console.WriteLine($""新增成功，ID: {result.GetIdentity()}"");

    // 批量添加
    var users = new List<User>
    {
        new User { UserName = ""李四"", Email = ""lisi@example.com"" },
        new User { UserName = ""王五"", Email = ""wangwu@example.com"" }
    };
    result = client.AddList(users);
");

            // 1.2 查询数据
            Console.WriteLine("1.2 查询数据:");
            Console.WriteLine(@"
    // LINQ 查询
    var activeUsers = client.Query<User>(u => u.IsActive).ToList();

    // 带条件查询
    var recentUsers = client.Query<User>(u => u.CreateTime > DateTime.Now.AddDays(-7))
        .OrderBy<User>(u => u.CreateTime, isDesc: true)
        .Take(10)
        .ToList();

    // 分页查询
    var page = client.Query<User>(u => u.IsActive)
        .OrderBy<User>(u => u.Id)
        .ToPage(new PageModel { PageIndex = 1, PageSize = 10 });

    // 单条查询
    var user = client.Query<User>(u => u.Id == 1).ToItem();

    // 原生 SQL 查询
    var customUsers = client.ExecuteSql<User>(
        ""SELECT * FROM Users WHERE IsActive = @IsActive"", 
        new[] { new SqlParameter(""@IsActive"", true) }
    );
");

            // 1.3 更新数据
            Console.WriteLine("1.3 更新数据:");
            Console.WriteLine(@"
    // 根据主键更新
    user.UserName = ""新名字"";
    var result = client.Update(user);

    // 只更新指定字段
    result = client.Update(user, u => new { u.UserName, u.Email });

    // 根据条件更新
    result = client.Update(
        new User { IsActive = false }, 
        u => u.CreateTime < DateTime.Now.AddYears(-1)
    );

    // 批量更新
    result = client.UpdateList(users);
");

            // 1.4 删除数据
            Console.WriteLine("1.4 删除数据:");
            Console.WriteLine(@"
    // 根据条件删除
    var result = client.Delete<User>(u => !u.IsActive);

    // 根据主键删除
    result = client.Delete(user);
");

            // 1.5 CodeFirst 建表
            Console.WriteLine("1.5 CodeFirst 建表:");
            Console.WriteLine(@"
    // 根据实体类创建表
    var result = client.CodeFirst<User>();

    // 重建表（先删除再创建）
    result = client.CodeFirst<User>(isDropExists: true);
");
        }

        #endregion

        #region 缓存操作示例

        /// <summary>
        /// 缓存操作示例
        /// </summary>
        private static void RunCacheExample(FastDataClient client)
        {
            Console.WriteLine("【2】缓存操作示例");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            // 2.1 自动缓存（通过 CacheAttribute）
            Console.WriteLine("2.1 自动缓存（通过 CacheAttribute）:");
            Console.WriteLine(@"
    // 定义带缓存的模型
    [Table(Name = ""Users"")]
    [Cache(IsEnable = true, ExpireTime = 300, Key = ""user:{Id}"", CacheType = ""Redis"")]
    public class User
    {
        [Primary]
        public int Id { get; set; }
        public string UserName { get; set; }
        // ...
    }

    // 查询时自动读取缓存
    var users = client.Query<User>(u => u.IsActive).ToList();

    // 更新时自动清除缓存
    client.Update(user);
");

            // 2.2 主动缓存（手动管理）
            Console.WriteLine("2.2 主动缓存（手动管理）:");
            Console.WriteLine(@"
    // 查询并缓存
    var users = client.Query<User>(u => u.IsActive).ToList();
    RedisInfo.Set(""users:active"", users, 300);

    // 从缓存读取
    var cachedUsers = RedisInfo.Get<List<User>>(""users:active"");
    if (cachedUsers != null)
    {
        // 缓存命中
    }
    else
    {
        // 缓存未命中，查询数据库
        users = client.Query<User>(u => u.IsActive).ToList();
        RedisInfo.Set(""users:active"", users, 300);
    }

    // 更新后清除缓存
    client.Update(user);
    RedisInfo.Remove($""user:{user.Id}"");
    RedisInfo.Remove(""users:active"");
");

            // 2.3 缓存模式
            Console.WriteLine("2.3 常用缓存模式:");
            Console.WriteLine(@"
    // Cache-Aside（旁路缓存）
    public User GetUserById(FastDataClient client, int id)
    {
        var cacheKey = $""user:{id}"";
        var user = RedisInfo.Get<User>(cacheKey);
        
        if (user == null)
        {
            user = client.Query<User>(u => u.Id == id).ToItem();
            if (user != null)
                RedisInfo.Set(cacheKey, user, 300);
        }
        return user;
    }

    // 缓存预热
    public void WarmUpCache(FastDataClient client)
    {
        var hotUsers = client.Query<User>(u => u.IsActive)
            .OrderBy<User>(u => u.CreateTime)
            .Take(100)
            .ToList();

        foreach (var user in hotUsers)
        {
            RedisInfo.Set($""user:{user.Id}"", user, 3600);
        }
    }
");
        }

        #endregion

        #region 消息队列示例

        /// <summary>
        /// 消息队列示例
        /// </summary>
        private static void RunQueueExample(FastDataClient client)
        {
            Console.WriteLine("【3】消息队列示例");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            // 3.1 写入队列（降级模式）
            Console.WriteLine("3.1 写入队列（数据库异常自动降级）:");
            Console.WriteLine(@"
    // 配置表级别的消息队列
    FastWrite.ConfigureQueue<User>(new WriteBehindConfig
    {
        QueueType = WriteBehindQueueType.ReliableQueue,
        EnableFallback = true,      // 启用降级
        EnableAutoRecovery = true,  // 启用自动恢复
        Topic = ""users:write""
    });

    // 使用链式 API 写入
    var result = client.WriteQueue()
        .WithMetadata(new Dictionary<string, object>
        {
            {""source"", ""example-app""},
            {""batchId"", Guid.NewGuid().ToString()}
        })
        .Add(user1)
        .Add(user2, new Dictionary<string, object> { {""priority"", ""high""} })
        .Add(user3)
        .Execute();

    Console.WriteLine($""直接写入: {result.DirectWriteCount}"");
    Console.WriteLine($""队列写入: {result.QueuedCount}"");
    Console.WriteLine($""降级发生: {result.FallbackOccurred}"");
");

            // 3.2 读取队列（异步查询）
            Console.WriteLine("3.2 读取队列（异步查询推送）:");
            Console.WriteLine(@"
    // 配置查询队列
    FastRead.ConfigureQueue<User>(new WriteBehindConfig
    {
        QueueType = WriteBehindQueueType.ReliableQueue,
        Topic = ""users:queries""
    });

    // 使用链式 API 推送查询请求
    var result = client.ReadQueue<User>()
        .WithMetadata(new Dictionary<string, object>
        {
            {""requestId"", Guid.NewGuid().ToString()},
            {""source"", ""example-app""}
        })
        .QueryList(u => u.IsActive, metadata: new Dictionary<string, object> { {""queryType"", ""active-users"" } })
        .QueryCount(u => u.IsActive, metadata: new Dictionary<string, object> { {""queryType"", ""count-active"" } })
        .QueryPaging(1, 10, u => u.IsActive, u => u.CreateTime, false, 
            new Dictionary<string, object> { {""queryType"", ""paged-list"" } })
        .Execute();

    Console.WriteLine($""推送到队列: {result.QueuedCount}"");
");

            // 3.3 Redis 消息队列
            Console.WriteLine("3.3 Redis 消息队列（发布/订阅）:");
            Console.WriteLine(@"
    // 发布消息
    RedisInfo.Publish(""sensor:data"", jsonData, db: 0);

    // 订阅消息
    RedisInfo.Receive(""sensor:data"", (channel, message) =>
    {
        Console.WriteLine($""收到消息: {message}"");
        // 处理消息...
    }, 
    subscribe: channel => Console.WriteLine($""订阅: {channel}""),
    unSubscribe: channel => Console.WriteLine($""取消订阅: {channel}""));

    // 可信队列（点对点）
    RedisInfo.Send(""task:queue"", jsonData, db: 0);
    var message = RedisInfo.Receive(""task:queue"", db: 0);
");
        }

        #endregion

        #region 高级示例

        /// <summary>
        /// 高级示例
        /// </summary>
        private static void RunAdvancedExample(FastDataClient client)
        {
            Console.WriteLine("【4】高级示例");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            // 4.1 批量插入（高性能）
            Console.WriteLine("4.1 批量插入（高性能）:");
            Console.WriteLine(@"
    // 生成测试数据
    var users = Enumerable.Range(1, 10000)
        .Select(i => new User 
        { 
            UserName = $""User{i}"", 
            Email = $""user{i}@example.com"",
            IsActive = true,
            CreateTime = DateTime.Now
        })
        .ToList();

    // 使用 BulkInsert（高性能批量插入）
    var result = client.BulkInsert(users);
    Console.WriteLine($""插入 {result.GetIdentity()} 条数据"");
");

            // 4.2 原生 SQL 操作
            Console.WriteLine("4.2 原生 SQL 操作:");
            Console.WriteLine(@"
    // 原生 SQL 查询
    var users = client.ExecuteSql<User>(
        ""SELECT * FROM Users WHERE CreateTime > @StartDate"", 
        new[] { new SqlParameter(""@StartDate"", DateTime.Now.AddDays(-7)) }
    );

    // 原生 SQL 写入
    var result = client.ExecuteSqlWrite(
        ""UPDATE Users SET IsActive = @IsActive WHERE CreateTime < @CreateDate"", 
        new[] 
        { 
            new SqlParameter(""@IsActive"", false),
            new SqlParameter(""@CreateDate"", DateTime.Now.AddYears(-1))
        }
    );
");

            // 4.3 分片操作
            Console.WriteLine("4.3 分片操作:");
            Console.WriteLine(@"
    // 分片查询（按时间分片）
    var queryParams = new Dictionary<string, object>
    {
        { ""CreateTime"", DateTime.Now.AddMonths(-3) }
    };
    var results = client.ShardQuery<Order>(
        o => o.CreateTime > DateTime.Now.AddMonths(-3),
        queryParams
    );

    // 分片添加
    var result = client.ShardAdd(new Order 
    { 
        UserId = 1, 
        OrderNo = ""ORD001"", 
        TotalAmount = 100.50m 
    });
");

            // 4.4 完整业务场景
            Console.WriteLine("4.4 完整业务场景（下单流程）:");
            Console.WriteLine(@"
    public async Task<bool> CreateOrder(FastDataClient client, Order order, List<OrderItem> items)
    {
        // 1. 检查缓存中的库存
        var stockCacheKey = $""product:stock:{items[0].ProductId}"";
        var stock = RedisInfo.Get<int?>(stockCacheKey);
        
        if (stock == null)
        {
            // 缓存未命中，查询数据库
            var product = client.Query<Product>(p => p.Id == items[0].ProductId).ToItem();
            stock = product?.Stock ?? 0;
            RedisInfo.Set(stockCacheKey, stock, 60);
        }

        // 2. 检查库存
        if (stock < items.Sum(i => i.Quantity))
        {
            Console.WriteLine(""库存不足"");
            return false;
        }

        // 3. 创建订单（使用消息队列，支持降级）
        var result = client.WriteQueue()
            .WithMetadata(new Dictionary<string, object>
            {
                {""operation"", ""create-order""},
                {""userId"", order.UserId}
            })
            .Add(order)
            .Execute();

        if (!result.Success)
        {
            Console.WriteLine($""创建订单失败: {result.Message}"");
            return false;
        }

        // 4. 清除相关缓存
        RedisInfo.Remove($""user:orders:{order.UserId}"");
        RedisInfo.Remove(stockCacheKey);

        // 5. 发布订单创建消息
        RedisInfo.Publish(""order:created"", JsonConvert.SerializeObject(order));

        return true;
    }
");
        }

        #endregion
    }
}
#endif
