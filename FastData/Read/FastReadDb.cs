using FastData.Context;
using FastData.Model;
#if !NETFRAMEWORK
using FastData.Queue;
#endif
using FastUntility.Page;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FastData
{
    /// <summary>
    /// FastData 绑定数据库 Key 的读取操作（实例方法）
    /// 
    /// 职责：
    /// 1. 绑定特定数据库 Key，避免重复传递 key 参数
    /// 2. 提供 LINQ 查询和原生 SQL 查询的便捷方法
    /// 3. 所有方法内部委托给 FastRead 静态方法
    /// 
    /// 使用示例：
    /// <code>
    /// // 创建绑定 Key 的读取实例
    /// var db1 = FastRead.Use("db1");
    /// 
    /// // LINQ 查询
    /// var users = db1.Query&lt;User&gt;(u =&gt; u.Age &gt; 18).ToList();
    /// 
    /// // 原生 SQL 查询
    /// var users = db1.ExecuteSql&lt;User&gt;("SELECT * FROM Users WHERE Age &gt; @Age", param);
    /// var dicts = db1.ExecuteSql("SELECT * FROM Users WHERE Age &gt; @Age", param);
    /// 
    /// // 推荐使用 FastDataClient 代替
    /// var client = new FastDataClient("db1");
    /// var users = client.Query&lt;User&gt;(u =&gt; u.Age &gt; 18).ToList();
    /// </code>
    /// 
    /// 相关类：
    /// - FastRead: 读取操作（静态方法，需显式传递 key）
    /// - FastDataClient: 统一门面（推荐，整合所有功能）
    /// - FastWriteDb: 绑定 Key 的写入操作
    /// </summary>
    public sealed class FastReadDb
    {
        private readonly string key;

        /// <summary>
        /// 获取数据库 Key
        /// </summary>
        public string Key => key;

        internal FastReadDb(string key)
        {
            this.key = key;
        }

#if !NETFRAMEWORK
        /// <summary>
        /// 创建链式读取构建器（带消息队列支持）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns>链式构建器</returns>
        public FastReadQueueBuilder<T> Queue<T>() where T : class, new()
        {
            return new FastReadQueueBuilder<T>(key);
        }
#endif

        public DataQuery<T> Query<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, string dbFile = "db.config") where T : class, new()
        {
            return FastRead.Query(predicate, field, key, dbFile);
        }

        public List<T> ExecuteSql<T>(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastRead.ExecuteSql<T>(sql, param, db, key, isOutSql);
        }

        public Task<List<T>> ExecuteSqlAsync<T>(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastRead.ExecuteSqlAsync<T>(sql, param, db, key, isOutSql);
        }

        public Lazy<List<T>> ExecuteLazySql<T>(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastRead.ExecuteLazySql<T>(sql, param, db, key, isOutSql);
        }

        public Task<Lazy<List<T>>> ExecuteLazySqlAsync<T>(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastRead.ExecuteLazySqlAsync<T>(sql, param, db, key, isOutSql);
        }

        public List<Dictionary<string, object>> ExecuteSql(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteSql(sql, param, db, key, isOutSql);
        }

        public Task<List<Dictionary<string, object>>> ExecuteSqlAsync(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteSqlAsync(sql, param, db, key, isOutSql);
        }

        public Lazy<List<Dictionary<string, object>>> ExecuteLazySql(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteLazySql(sql, param, db, key, isOutSql);
        }

        public Task<Lazy<List<Dictionary<string, object>>>> ExecuteLazySqlAsync(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteLazySqlAsync(sql, param, db, key, isOutSql);
        }
    }
}
