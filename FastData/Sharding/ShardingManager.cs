using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using FastData.Base;
using FastData.Config;
using FastData.Context;
using FastData.Model;
using FastData.Sharding.Strategies;

namespace FastData.Sharding
{
    /// <summary>
    /// 分表管理器
    /// 负责分表策略的注册、配置和执行
    /// </summary>
    public class ShardingManager
    {
        private static readonly Dictionary<ShardingType, IShardingStrategy> _strategies = new Dictionary<ShardingType, IShardingStrategy>();
        private static readonly Dictionary<System.Type, ShardingConfig> _configs = new Dictionary<System.Type, ShardingConfig>();
        private static readonly object _lock = new object();

        static ShardingManager()
        {
            // 注册默认策略
            RegisterStrategy(new TimeShardingStrategy());
            RegisterStrategy(new HashShardingStrategy());
            RegisterStrategy(new ListShardingStrategy());
            RegisterStrategy(new CompositeShardingStrategy());
            RegisterStrategy(new QueryFrequencyShardingStrategy());
        }

        /// <summary>
        /// 注册分表策略
        /// </summary>
        public static void RegisterStrategy(IShardingStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            lock (_lock)
            {
                _strategies[strategy.Type] = strategy;
            }
        }

        /// <summary>
        /// 配置实体分表
        /// </summary>
        public static void Configure<T>(ShardingConfig config) where T : class
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.EntityType = typeof(T);

            if (string.IsNullOrEmpty(config.BaseTableName))
                config.BaseTableName = typeof(T).Name;

            lock (_lock)
            {
                _configs[typeof(T)] = config;
            }
        }

        /// <summary>
        /// 获取实体分表配置
        /// </summary>
        public static ShardingConfig GetConfig<T>() where T : class
        {
            return GetConfig(typeof(T));
        }

        /// <summary>
        /// 获取实体分表配置
        /// </summary>
        public static ShardingConfig GetConfig(System.Type entityType)
        {
            lock (_lock)
            {
                _configs.TryGetValue(entityType, out var config);
                return config;
            }
        }

        /// <summary>
        /// 检查实体是否配置了分表
        /// </summary>
        public static bool IsShardingEnabled<T>() where T : class
        {
            return GetConfig<T>() != null;
        }

        /// <summary>
        /// 检查实体是否配置了分表
        /// </summary>
        public static bool IsShardingEnabled(System.Type entityType)
        {
            return GetConfig(entityType) != null;
        }

        /// <summary>
        /// 根据实体获取分表名称
        /// </summary>
        public static string GetTableName<T>(T entity) where T : class
        {
            var config = GetConfig<T>();
            if (config == null)
                return typeof(T).Name;

            var strategy = GetStrategy(config.ShardingType);
            return strategy.GetTableName(config, entity);
        }

        /// <summary>
        /// 根据查询条件获取所有可能的分表名称
        /// </summary>
        public static List<string> GetTableNames<T>(Dictionary<string, object> queryParams) where T : class
        {
            var config = GetConfig<T>();
            if (config == null)
                return new List<string> { typeof(T).Name };

            var strategy = GetStrategy(config.ShardingType);
            return strategy.GetTableNames(config, queryParams);
        }

        /// <summary>
        /// 获取所有分表名称
        /// </summary>
        public static List<string> GetAllTableNames<T>() where T : class
        {
            var config = GetConfig<T>();
            if (config == null)
                return new List<string> { typeof(T).Name };

            var strategy = GetStrategy(config.ShardingType);
            return strategy.GetAllTableNames(config);
        }

        /// <summary>
        /// 获取分表策略
        /// </summary>
        public static IShardingStrategy GetStrategy(ShardingType type)
        {
            lock (_lock)
            {
                if (_strategies.TryGetValue(type, out var strategy))
                    return strategy;

                throw new InvalidOperationException($"未注册的分表策略类型: {type}");
            }
        }

        /// <summary>
        /// 创建分表
        /// </summary>
        public static bool CreateTable<T>(string tableName) where T : class
        {
            var config = GetConfig<T>();
            if (config == null)
                return false;

            return CreateTable(config, tableName);
        }

        /// <summary>
        /// 创建分表
        /// </summary>
        public static bool CreateTable(ShardingConfig config, string tableName)
        {
            try
            {
                var dbConfig = DataConfig.GetConfig(config.DatabaseKey);
                if (dbConfig == null)
                    return false;

                using (var db = new DataContext(config.DatabaseKey))
                {
                    var sql = GenerateCreateTableSql(config, tableName);
                    db.ExecuteSql(sql);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 生成创建分表的 SQL
        /// </summary>
        public static string GenerateCreateTableSql(ShardingConfig config, string tableName)
        {
            // 这里需要根据实体类型生成建表 SQL
            // 实际实现应该从实体的属性映射获取字段定义
            return $"CREATE TABLE IF NOT EXISTS [{tableName}] (Id INT PRIMARY KEY)";
        }

        /// <summary>
        /// 自动创建分表（如果配置了自动创建）
        /// </summary>
        public static void AutoCreateTable<T>(T entity) where T : class
        {
            var config = GetConfig<T>();
            if (config == null || !config.AutoCreateTable)
                return;

            var tableName = GetTableName(entity);
            CreateTable<T>(tableName);
        }

        /// <summary>
        /// 获取分表的数据库上下文
        /// </summary>
        public static DataContext GetDbContext<T>(T entity) where T : class
        {
            var config = GetConfig<T>();
            if (config == null)
                return new DataContext();

            return new DataContext(config.DatabaseKey);
        }

        /// <summary>
        /// 从分表查询数据
        /// </summary>
        public static List<T> QueryFromShard<T>(string tableName, DataQuery query) where T : class, new()
        {
            // 这里需要修改查询逻辑，将表名替换为分表名
            // 实际实现需要修改 DataContext 的查询方法
            return new List<T>();
        }

        /// <summary>
        /// 向分表插入数据
        /// </summary>
        public static bool InsertToShard<T>(T entity) where T : class
        {
            var tableName = GetTableName(entity);
            AutoCreateTable(entity);

            // 这里需要修改插入逻辑，将表名替换为分表名
            // 实际实现需要修改 DataContext 的插入方法
            return true;
        }

        /// <summary>
        /// 从分表更新数据
        /// </summary>
        public static bool UpdateInShard<T>(T entity) where T : class
        {
            var tableName = GetTableName(entity);

            // 这里需要修改更新逻辑，将表名替换为分表名
            // 实际实现需要修改 DataContext 的更新方法
            return true;
        }

        /// <summary>
        /// 从分表删除数据
        /// </summary>
        public static bool DeleteFromShard<T>(T entity) where T : class
        {
            var tableName = GetTableName(entity);

            // 这里需要修改删除逻辑，将表名替换为分表名
            // 实际实现需要修改 DataContext 的删除方法
            return true;
        }

        /// <summary>
        /// 清除所有配置
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _configs.Clear();
            }
        }
    }
}
