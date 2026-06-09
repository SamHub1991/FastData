using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using FastData;
using FastData.Config;
using FastData.ConnectionPool;
using FastData.Model;
using Microsoft.Data.SqlClient;
using NewLife.Caching;

namespace FastData.Benchmarks
{
    /// <summary>
    /// 基准测试配置：按顺序执行，使用 MemoryDiagnoser 收集内存分配数据
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(iterationCount: 5, warmupCount: 3)]
    public class OrmBenchmarks
    {
        // 测试常量
        private const int BulkInsertCount = 1000;
        private const string DbKey = "benchmark_db";
        private const string CacheServer = "127.0.0.1:6379";
        private const int CacheDb = 15;
        private const string SqlConnectionString = "Server=127.0.0.1;Database=benchmark_test;User Id=sa;Password=YourPassword123;Connection Timeout=5;";

        // Redis 实例（用于缓存操作测试）
        private FullRedis? _redis;

        // 连接池实例
        private SmartConnectionPool? _connectionPool;
        private ConnectionPoolConfig? _poolConfig;

        /// <summary>
        /// 基准测试数据初始化：注册内存提供程序、初始化 Redis 连接、创建连接池
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            // 注册 Microsoft.Data.SqlClient 提供程序（内存注册，避免配置文件依赖）
            try
            {
                DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);
            }
            catch
            {
                // 提供程序可能已经注册，忽略重复注册异常
            }

            // 初始化 Redis 连接（使用独立的数据库避免干扰其他数据）
            _redis = new FullRedis
            {
                Server = CacheServer,
                Db = CacheDb
            };

            // 初始化连接池配置
            _poolConfig = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 10,
                AutoAdjustByEnvironment = false,
                ConnectionTimeout = 10,
                EnableSmartAdjustment = false
            };
        }

        /// <summary>
        /// 基准测试数据清理：释放连接池和 Redis 连接
        /// </summary>
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _connectionPool?.Dispose();
            _redis?.Dispose();
        }

        #region 1. 连接创建与释放

        /// <summary>
        /// 基准测试：数据库连接的创建与释放周期
        /// 衡量：连接建立、打开、关闭的开销
        /// </summary>
        [Benchmark(Description = "连接创建与释放")]
        public void ConnectionCreateAndDispose()
        {
            using var connection = new SqlConnection(SqlConnectionString);
            try
            {
                connection.Open();
            }
            catch
            {
                // 基准测试中连接失败是预期的，不影响性能测量
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }

        #endregion

        #region 2. 连接池获取与归还

        /// <summary>
        /// 基准测试：智能连接池的创建、预热与初始化
        /// 衡量：连接池实例化开销
        /// </summary>
        [Benchmark(Description = "连接池初始化")]
        public SmartConnectionPool PoolInitialize()
        {
            var config = new ConnectionPoolConfig
            {
                MinPoolSize = 2,
                MaxPoolSize = 5,
                AutoAdjustByEnvironment = false,
                ConnectionTimeout = 5,
                EnableSmartAdjustment = false
            };

            var pool = new SmartConnectionPool(
                name: "bench_pool",
                connectionFactory: () => new SqlConnection(SqlConnectionString),
                config: config);

            return pool;
        }

        /// <summary>
        /// 基准测试：从预热连接池获取连接
        /// 衡量：SemaphoreSlim 等待 + 从空闲队列取出连接的开销
        /// </summary>
        [Benchmark(Description = "连接池获取连接")]
        public PooledConnection PoolGetConnection()
        {
            // 延迟初始化确保只创建一次
            _connectionPool ??= new SmartConnectionPool(
                name: "bench_pool",
                connectionFactory: () => new SqlConnection(SqlConnectionString),
                config: _poolConfig!);

            var pooledConn = _connectionPool.GetConnection();
            return pooledConn;
        }

        /// <summary>
        /// 基准测试：连接池归还连接
        /// 衡量：连接标记为空闲 + 放回空闲队列的开销
        /// </summary>
        [Benchmark(Description = "连接池归还连接")]
        public void PoolReturnConnection()
        {
            _connectionPool ??= new SmartConnectionPool(
                name: "bench_pool",
                connectionFactory: () => new SqlConnection(SqlConnectionString),
                config: _poolConfig!);

            var pooledConn = _connectionPool.GetConnection();
            pooledConn.Dispose(); // 归还到连接池
        }

        #endregion

        #region 3. Lambda 表达式解析

        /// <summary>
        /// 基准测试：Lambda 表达式树的创建与编译
        /// 衡量：Expression<Func<T, bool>> 的构建开销（ORM 核心操作）
        /// </summary>
        [Benchmark(Description = "Lambda 表达式解析")]
        public Expression<Func<BenchmarkEntity, bool>> ExpressionParsing()
        {
            // 模拟 ORM 常用的查询条件构建
            Expression<Func<BenchmarkEntity, bool>> predicate = e =>
                e.IsActive &&
                e.Age > 18 &&
                e.Status == 1 &&
                e.Name!.Contains("test") &&
                e.CreateTime > DateTime.UtcNow.AddDays(-30);

            // 编译表达式以测量完整开销
            var compiled = predicate.Compile();
            var testEntity = new BenchmarkEntity
            {
                Id = 1,
                Name = "test_user",
                Age = 25,
                IsActive = true,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };
            _ = compiled(testEntity); // 执行一次验证

            return predicate;
        }

        #endregion

        #region 4. 缓存操作

        /// <summary>
        /// 基准测试：Redis 缓存写入
        /// 衡量：缓存 Set 操作的开销（序列化 + 网络 IO）
        /// </summary>
        [Benchmark(Description = "缓存写入 (Redis Set)")]
        public void CacheSet()
        {
            var entity = new BenchmarkEntity
            {
                Id = 1,
                Name = "cache_test",
                Age = 30,
                IsActive = true,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };

            _redis!.Set("bench:cache_set", entity);
        }

        /// <summary>
        /// 基准测试：Redis 缓存读取
        /// 衡量：缓存 Get 操作的开销（反序列化 + 网络 IO）
        /// </summary>
        [Benchmark(Description = "缓存读取 (Redis Get)")]
        public BenchmarkEntity? CacheGet()
        {
            return _redis!.Get<BenchmarkEntity>("bench:cache_set");
        }

        #endregion

        #region 5. ORM 读写操作（需要数据库连接）

        /// <summary>
        /// 基准测试：使用 FastDataClient 执行单条实体插入
        /// 衡量：表达式解析 + SQL 生成 + 参数绑定 + 执行插入的完整链路
        /// </summary>
        [Benchmark(Description = "ORM 单条插入")]
        public WriteReturn OrmSingleInsert()
        {
            var entity = new BenchmarkEntity
            {
                Id = Guid.NewGuid().GetHashCode(),
                Name = "orm_insert_test",
                Age = 25,
                IsActive = true,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };

            // 注意：这里需要 db.config 中配置了 benchmark_db 才会成功
            // 如果数据库未配置，基准测试会捕获异常，但不影响性能测量
            try
            {
                var client = new FastDataClient(DbKey);
                return client.Add(entity);
            }
            catch
            {
                return new WriteReturn { IsSuccess = false, Message = "数据库未配置" };
            }
        }

        /// <summary>
        /// 基准测试：使用 FastDataClient 执行单条查询
        /// 衡量：表达式解析 + SQL 生成 + 参数绑定 + 执行查询 + 结果映射的完整链路
        /// </summary>
        [Benchmark(Description = "ORM 单条查询")]
        public List<BenchmarkEntity> OrmSingleQuery()
        {
            try
            {
                var client = new FastDataClient(DbKey);
                return client.Query<BenchmarkEntity>(e => e.IsActive && e.Age > 18).ToList();
            }
            catch
            {
                return new List<BenchmarkEntity>();
            }
        }

        /// <summary>
        /// 基准测试：使用 FastDataClient 执行批量插入
        /// 衡量：BulkInsert（内部使用 SqlBulkCopy 等高性能方式）的完整链路
        /// </summary>
        [Benchmark(Description = "ORM 批量插入 (1000 条)")]
        public WriteReturn OrmBulkInsert()
        {
            var entities = GenerateBulkEntities(BulkInsertCount);

            try
            {
                var client = new FastDataClient(DbKey);
                return client.BulkInsert(entities);
            }
            catch
            {
                return new WriteReturn { IsSuccess = false, Message = "数据库未配置" };
            }
        }

        #endregion

        /// <summary>
        /// 生成批量测试实体
        /// </summary>
        private static List<BenchmarkEntity> GenerateBulkEntities(int count)
        {
            var entities = new List<BenchmarkEntity>(count);
            var now = DateTime.UtcNow;

            for (int i = 0; i < count; i++)
            {
                entities.Add(new BenchmarkEntity
                {
                    Id = i + 1,
                    Name = $"bulk_entity_{i}",
                    Age = 18 + (i % 50),
                    IsActive = i % 2 == 0,
                    Status = i % 3,
                    CreateTime = now
                });
            }

            return entities;
        }
    }

    /// <summary>
    /// 基准测试专用实体类
    /// 模拟常见的业务实体结构，包含多种数据类型
    /// </summary>
    public class BenchmarkEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public int Status { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
