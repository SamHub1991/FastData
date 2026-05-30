using System;
using System.Collections.Generic;
using FastData.Example.Model;
using FastData.Property;

namespace FastData.Example.Example
{
    /// <summary>
    /// 多数据库表名映射示例
    /// 场景：同一个 Model 在不同数据库中表名不同
    /// </summary>
    [Table(DbTableNames = "SqlServer.Users,MySql.user_info,PostgreSQL.tb_users")]
    public class MultiDbUser
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 混合模式：优先使用 DbTableNames，回退到 Name
    /// </summary>
    [Table(Name = "default_orders", DbTableNames = "SqlServer.Orders,MySql.order_info")]
    public class MixedDbOrder
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime OrderDate { get; set; }
    }

    /// <summary>
    /// 多数据库使用示例
    /// 场景：多数据源、读写分离、跨库查询、多数据库表名映射
    /// </summary>
    public static class MultiDbExample
    {
        /// <summary>
        /// 运行所有多数据库示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("--- 多数据库使用示例 ---");
            Console.WriteLine();

            DemoUseDatabaseKey();
            DemoReadWriteSeparation();
            DemoCrossDatabaseQuery();
            DemoDefaultDatabase();
            DemoMultiDbTableNameMapping();
        }

        /// <summary>
        /// 示例 5: 多数据库表名映射
        /// 场景：同一个 Model 在不同数据库中表名不同
        /// </summary>
        private static void DemoMultiDbTableNameMapping()
        {
            Console.WriteLine("=== 示例 5: 多数据库表名映射 ===");
            Console.WriteLine("场景：同一个 Model 在不同数据库中表名不同");
            Console.WriteLine();

            Console.WriteLine("Model 定义：");
            Console.WriteLine(@"  // 格式: ""数据库Key.表名,数据库Key.表名""
  [Table(DbTableNames = ""SqlServer.Users,MySql.user_info,PostgreSQL.tb_users"")]
  public class MultiDbUser
  {
      [Column(IsIdentity = true)]
      public int Id { get; set; }
      public string UserName { get; set; }
      public string Email { get; set; }
      public DateTime CreatedAt { get; set; }
  }");
            Console.WriteLine();

            Console.WriteLine("混合模式（优先使用 DbTableNames，回退到 Name）：");
            Console.WriteLine(@"  [Table(Name = ""default_orders"", DbTableNames = ""SqlServer.Orders,MySql.order_info"")]
  public class MixedDbOrder
  {
      [Column(IsIdentity = true)]
      public int Id { get; set; }
      public int UserId { get; set; }
      public decimal Amount { get; set; }
      public DateTime OrderDate { get; set; }
  }");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 使用 FastDb.Use() 切换数据库上下文
  using (FastDb.Use(""SqlServer""))
  {
      var users = FastRead.Query<MultiDbUser>(u => u.Id > 0).ToList();
      // 实际查询的表名是 ""Users""
      Console.WriteLine($""SqlServer 表名: Users, 查询到 {users.Count} 条记录"");
  }

  using (FastDb.Use(""MySql""))
  {
      var users = FastRead.Query<MultiDbUser>(u => u.Id > 0).ToList();
      // 实际查询的表名是 ""user_info""
      Console.WriteLine($""MySql 表名: user_info, 查询到 {users.Count} 条记录"");
  }

  using (FastDb.Use(""PostgreSQL""))
  {
      var users = FastRead.Query<MultiDbUser>(u => u.Id > 0).ToList();
      // 实际查询的表名是 ""tb_users""
      Console.WriteLine($""PostgreSQL 表名: tb_users, 查询到 {users.Count} 条记录"");
  }

  // 混合模式示例
  using (FastDb.Use(""SqlServer""))
  {
      var orders = FastRead.Query<MixedDbOrder>(o => o.Id > 0).ToList();
      // 实际查询的表名是 ""Orders""（使用 DbTableNames 映射）
  }

  using (FastDb.Use(""Oracle""))
  {
      var orders = FastRead.Query<MixedDbOrder>(o => o.Id > 0).ToList();
      // 实际查询的表名是 ""default_orders""（回退到 Name 属性）
  }");
            Console.WriteLine();

            Console.WriteLine("说明：");
            Console.WriteLine("  - DbTableNames 格式: \"数据库Key.表名,数据库Key.表名\"");
            Console.WriteLine("  - 数据库 Key 区分大小写");
            Console.WriteLine("  - 优先级: DbTableNames 映射 > Name 属性 > 类名");
            Console.WriteLine("  - 混合模式: 同时设置 Name 和 DbTableNames，DbTableNames 优先");
            Console.WriteLine("  - 未匹配时回退到 Name 属性或类名");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 1: 使用数据库 Key
        /// 场景：连接多个数据库，通过 Key 指定
        /// </summary>
        private static void DemoUseDatabaseKey()
        {
            Console.WriteLine("=== 示例 1: 使用数据库 Key ===");
            Console.WriteLine("场景：连接多个数据库，通过 Key 指定目标库");
            Console.WriteLine();

            Console.WriteLine("配置文件（db.config）：");
            Console.WriteLine(@"  <configuration>
    <Connections>
      <Add Name=""Default"" DbType=""MySql""
           ConnectionString=""server=127.0.0.1;database=app_db;uid=root;pwd=123456""
           IsDefault=""true"" />
      <Add Name=""Log"" DbType=""MySql""
           ConnectionString=""server=127.0.0.1;database=log_db;uid=root;pwd=123456"" />
      <Add Name=""Archive"" DbType=""SqlServer""
           ConnectionString=""Server=192.168.1.100;Database=archive_db;User Id=sa;Password=123456"" />
    </Connections>
  </configuration>");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 方式 1: 使用 FastRead/FastWrite 的 key 参数
  var users = FastRead.Query<User>().ToList(key: ""Default"");
  var logs = FastRead.Query<Log>().ToList(key: ""Log"");

  // 方式 2: 使用 Use() 方法绑定数据库
  var db1 = FastRead.Use(""Default"");
  var defaultUsers = db1.Query<User>().ToList();

  var db2 = FastRead.Use(""Log"");
  var logEntries = db2.Query<Log>().ToList();

  // 方式 3: 写入时指定数据库
  FastWrite.Add(user, key: ""Default"");
  FastWrite.Add(log, key: ""Log"");");
            Console.WriteLine();

            Console.WriteLine("说明：");
            Console.WriteLine("  - Name 唯一标识数据库连接");
            Console.WriteLine("  - IsDefault=\"true\" 标记默认数据库");
            Console.WriteLine("  - 支持不同数据库类型（MySql/SqlServer/Oracle）");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 2: 读写分离
        /// 场景：读操作走从库，写操作走主库
        /// </summary>
        private static void DemoReadWriteSeparation()
        {
            Console.WriteLine("=== 示例 2: 读写分离 ===");
            Console.WriteLine("场景：读操作走从库，写操作走主库");
            Console.WriteLine();

            Console.WriteLine("配置文件（db.config）：");
            Console.WriteLine(@"  <configuration>
    <Connections>
      <Add Name=""Master"" DbType=""MySql""
           ConnectionString=""server=master.db.local;database=app_db;uid=root;pwd=123456"" />
      <Add Name=""Slave1"" DbType=""MySql""
           ConnectionString=""server=slave1.db.local;database=app_db;uid=root;pwd=123456"" />
      <Add Name=""Slave2"" DbType=""MySql""
           ConnectionString=""server=slave2.db.local;database=app_db;uid=root;pwd=123456"" />
    </Connections>
  </configuration>");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 读操作使用从库
  public List<User> GetUsers()
  {
      // 随机或轮询选择从库
      var slaveKey = GetRandomSlaveKey(); // ""Slave1"" 或 ""Slave2""
      return FastRead.Query<User>(u => u.IsActive)
                     .ToList(key: slaveKey);
  }

  // 写操作使用主库
  public bool CreateUser(User user)
  {
      var result = FastWrite.Add(user, key: ""Master"");
      return result.IsSuccess;
  }

  // 使用 Use() 方法简化
  public User GetUserById(int id)
  {
      var slaveDb = FastRead.Use(""Slave1"");
      return slaveDb.Query<User>(u => u.Id == id)
                    .FirstOrDefault();
  }");
            Console.WriteLine();

            Console.WriteLine("读写分离最佳实践：");
            Console.WriteLine("  - 写操作始终使用主库");
            Console.WriteLine("  - 读操作可以使用从库，降低主库压力");
            Console.WriteLine("  - 写后立即读的场景，使用主库避免数据不一致");
            Console.WriteLine("  - 可以通过负载均衡轮询多个从库");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 3: 跨库查询
        /// 场景：从不同数据库查询数据并关联
        /// </summary>
        private static void DemoCrossDatabaseQuery()
        {
            Console.WriteLine("=== 示例 3: 跨库查询 ===");
            Console.WriteLine("场景：从不同数据库查询数据并关联");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 从主库查询用户
  var users = FastRead.Query<User>().ToList(key: ""Default"");

  // 从日志库查询用户行为
  var userLogs = FastRead.Query<UserLog>(l => l.Action == ""Login"")
                         .ToList(key: ""Log"");

  // 在内存中关联（适合数据量小的场景）
  var result = from u in users
               join l in userLogs on u.Id equals l.UserId
               select new
               {
                   u.UserName,
                   l.Action,
                   l.CreateTime
               };

  // 使用 Map SQL 进行跨库查询（如果数据库支持）
  // 需要配置 DbLink 或使用同义词");
            Console.WriteLine();

            Console.WriteLine("跨库查询说明：");
            Console.WriteLine("  - 小数据量：在内存中关联");
            Console.WriteLine("  - 大数据量：使用数据库链接（DbLink）");
            Console.WriteLine("  - 频繁跨库：考虑数据同步到同一库");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 4: 默认数据库
        /// 场景：省略 key 参数，自动使用默认数据库
        /// </summary>
        private static void DemoDefaultDatabase()
        {
            Console.WriteLine("=== 示例 4: 默认数据库 ===");
            Console.WriteLine("场景：配置默认数据库，简化代码");
            Console.WriteLine();

            Console.WriteLine("配置方式：");
            Console.WriteLine(@"  // 方式 1: 在配置中设置 IsDefault
  <Add Name=""Default"" ... IsDefault=""true"" />

  // 方式 2: 在代码中设置
  FastDb.CurrentKey = ""Default"";");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 设置默认数据库
  FastDb.CurrentKey = ""Default"";

  // 不指定 key，使用默认数据库
  var users = FastRead.Query<User>().ToList();
  var result = FastWrite.Add(user);

  // 临时切换数据库
  var logs = FastRead.Query<Log>().ToList(key: ""Log"");

  // 恢复默认
  FastDb.CurrentKey = ""Default"";");
            Console.WriteLine();

            Console.WriteLine("说明：");
            Console.WriteLine("  - IsDefault=\"true\" 标记默认连接");
            Console.WriteLine("  - FastDb.CurrentKey 设置全局默认数据库");
            Console.WriteLine("  - 不传 key 参数时使用默认数据库");
            Console.WriteLine("  - 多个连接未指定默认时使用第一个");
            Console.WriteLine();
        }
    }
}
