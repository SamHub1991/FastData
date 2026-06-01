using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FastData.Base;
using FastData.Context;
using FastData.Model;
using FastData.Sharding;
using FastUntility.Page;

namespace FastData
{
    /// <summary>
    /// FastData 分片读取操作助手（静态方法）
    /// 
    /// 职责：
    /// 1. 分片表查询（根据分片策略自动路由到对应的表）
    /// 2. 分片分页查询
    /// 3. 合并多个分片表的查询结果
    /// 
    /// 使用示例：
    /// <code>
    /// // 分片查询（自动路由到对应的分片表）
    /// var results = ShardingReadHelper.Query&lt;Order&gt;(
    ///     predicate: o =&gt; o.CreateTime &gt; DateTime.Now.AddMonths(-3),
    ///     queryParams: new Dictionary&lt;string, object&gt; { { "CreateTime", DateTime.Now.AddMonths(-3) } },
    ///     key: "db1"
    /// );
    /// 
    /// // 分片分页查询
    /// var page = ShardingReadHelper.QueryPage&lt;Order&gt;(
    ///     pModel: new PageModel { PageIndex = 1, PageSize = 10 },
    ///     predicate: o =&gt; o.CreateTime &gt; DateTime.Now.AddMonths(-3),
    ///     queryParams: new Dictionary&lt;string, object&gt; { { "CreateTime", DateTime.Now.AddMonths(-3) } },
    ///     key: "db1"
    /// );
    /// 
    /// // 推荐使用 FastDataClient 代替
    /// var client = new FastDataClient("db1");
    /// var results = client.ShardQuery&lt;Order&gt;(o =&gt; o.CreateTime &gt; DateTime.Now.AddMonths(-3));
    /// </code>
    /// 
    /// 相关类：
    /// - ShardingWriteHelper: 分片写入操作
    /// - FastDataClient: 统一门面（推荐，整合所有功能）
    /// - ShardingManager: 分片管理器
    /// - IShardingStrategy: 分片策略接口
    /// </summary>
    public static class ShardingReadHelper
    {
        /// <summary>
        /// 从分表读取数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <param name="queryParams">分表查询参数</param>
        /// <param name="key">数据库 Key</param>
        /// <returns>合并后的数据列表</returns>
        public static List<T> Query<T>(
            Expression<Func<T, bool>> predicate,
            Dictionary<string, object> queryParams,
            string key = null) where T : class, new()
        {
            if (!ShardingManager.IsShardingEnabled<T>())
            {
                throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured for sharding.");
            }

            var tableNames = ShardingManager.GetTableNames<T>(queryParams);
            var allResults = new List<T>();

            foreach (var tableName in tableNames)
            {
                key = key ?? FastDb.CurrentKey;
                using (var db = new DataContext(key))
                {
                    var query = new DataQuery();
                    query = query.Where<T>(predicate);
                    var results = db.GetList<T>(query);
                    allResults.AddRange(results.List);
                }
            }

            return allResults;
        }

        /// <summary>
        /// 从分表读取数据（分页）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <param name="queryParams">分表查询参数</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="key">数据库 Key</param>
        /// <returns>分页数据</returns>
        public static PageResult<T> QueryPage<T>(
            Expression<Func<T, bool>> predicate,
            Dictionary<string, object> queryParams,
            int pageIndex,
            int pageSize,
            string key = null) where T : class, new()
        {
            if (!ShardingManager.IsShardingEnabled<T>())
            {
                throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured for sharding.");
            }

            var tableNames = ShardingManager.GetTableNames<T>(queryParams);
            var allResults = new List<T>();

            foreach (var tableName in tableNames)
            {
                key = key ?? FastDb.CurrentKey;
                using (var db = new DataContext(key))
                {
                    var query = new DataQuery();
                    query = query.Where<T>(predicate);
                    var results = db.GetList<T>(query);
                    allResults.AddRange(results.List);
                }
            }

            // 手动分页
            var totalCount = allResults.Count;
            var pagedResults = allResults
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pageResult = new PageResult<T>();
            pageResult.pModel.PageId = pageIndex;
            pageResult.pModel.PageSize = pageSize;
            pageResult.pModel.TotalRecord = totalCount;
            pageResult.pModel.TotalPage = (int)Math.Ceiling((double)totalCount / pageSize);
            pageResult.list = pagedResults;

            return pageResult;
        }
    }

    /// <summary>
    /// FastData 分片写入操作助手（静态方法）
    /// 
    /// 职责：
    /// 1. 分片表数据添加（根据分片策略自动路由到对应的表）
    /// 2. 分片表批量添加
    /// 3. 分片表数据删除
    /// 4. 分片表数据更新
    /// 
    /// 使用示例：
    /// <code>
    /// // 分片添加
    /// var result = ShardingWriteHelper.Add&lt;Order&gt;(order, key: "db1");
    /// 
    /// // 分片批量添加
    /// var result = ShardingWriteHelper.AddList&lt;Order&gt;(orders, key: "db1");
    /// 
    /// // 分片删除
    /// var result = ShardingWriteHelper.Delete&lt;Order&gt;(
    ///     predicate: o =&gt; o.CreateTime &lt; DateTime.Now.AddYears(-1),
    ///     key: "db1"
    /// );
    /// 
    /// // 分片更新
    /// var result = ShardingWriteHelper.Update&lt;Order&gt;(
    ///     entity: new Order { Status = "Completed" },
    ///     predicate: o =&gt; o.Id == orderId,
    ///     key: "db1"
    /// );
    /// 
    /// // 推荐使用 FastDataClient 代替
    /// var client = new FastDataClient("db1");
    /// var result = client.ShardAdd(order);
    /// </code>
    /// 
    /// 相关类：
    /// - ShardingReadHelper: 分片读取操作
    /// - FastDataClient: 统一门面（推荐，整合所有功能）
    /// - ShardingManager: 分片管理器
    /// - IShardingStrategy: 分片策略接口
    /// </summary>
    public static class ShardingWriteHelper
    {
        /// <summary>
        /// 向分表写入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <param name="key">数据库 Key</param>
        /// <returns>写入结果</returns>
        public static WriteReturn Add<T>(T entity, string key = null) where T : class, new()
        {
            if (!ShardingManager.IsShardingEnabled<T>())
            {
                throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured for sharding.");
            }

            key = key ?? FastDb.CurrentKey;
            using (var db = new DataContext(key))
            {
                return db.Add<T>(entity, false).WriteReturn;
            }
        }

        /// <summary>
        /// 向分表批量写入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">实体列表</param>
        /// <param name="key">数据库 Key</param>
        /// <returns>写入结果</returns>
        public static WriteReturn AddList<T>(List<T> entities, string key = null) where T : class, new()
        {
            if (!ShardingManager.IsShardingEnabled<T>())
            {
                throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured for sharding.");
            }

            key = key ?? FastDb.CurrentKey;
            using (var db = new DataContext(key))
            {
                return db.AddList<T>(entities, false, true).WriteReturn;
            }
        }

        /// <summary>
        /// 从分表删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件</param>
        /// <param name="queryParams">分表查询参数</param>
        /// <param name="key">数据库 Key</param>
        /// <returns>删除结果</returns>
        public static WriteReturn Delete<T>(
            Expression<Func<T, bool>> predicate,
            Dictionary<string, object> queryParams,
            string key = null) where T : class, new()
        {
            if (!ShardingManager.IsShardingEnabled<T>())
            {
                throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured for sharding.");
            }

            var tableNames = ShardingManager.GetTableNames<T>(queryParams);
            var lastResult = new WriteReturn();

            foreach (var tableName in tableNames)
            {
                key = key ?? FastDb.CurrentKey;
                using (var db = new DataContext(key))
                {
                    lastResult = db.Delete<T>(predicate).WriteReturn;
                }
            }

            return lastResult;
        }

        /// <summary>
        /// 更新分表数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <param name="predicate">更新条件</param>
        /// <param name="field">要更新的字段</param>
        /// <param name="key">数据库 Key</param>
        /// <returns>更新结果</returns>
        public static WriteReturn Update<T>(T entity, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, string key = null) where T : class, new()
        {
            if (!ShardingManager.IsShardingEnabled<T>())
            {
                throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured for sharding.");
            }

            key = key ?? FastDb.CurrentKey;
            using (var db = new DataContext(key))
            {
                return db.Update<T>(entity, predicate, field).WriteReturn;
            }
        }
    }
}
