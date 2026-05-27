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
    /// 绑定数据库Key的查询入口
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

        public Task<List<T>> ExecuteSqlAsy<T>(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastRead.ExecuteSqlAsy<T>(sql, param, db, key, isOutSql);
        }

        public Lazy<List<T>> ExecuteLazySql<T>(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastRead.ExecuteLazySql<T>(sql, param, db, key, isOutSql);
        }

        public Task<Lazy<List<T>>> ExecuteLazySqlAsy<T>(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastRead.ExecuteLazySqlAsy<T>(sql, param, db, key, isOutSql);
        }

        public List<Dictionary<string, object>> ExecuteSql(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteSql(sql, param, db, key, isOutSql);
        }

        public Task<List<Dictionary<string, object>>> ExecuteSqlAsy(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteSqlAsy(sql, param, db, key, isOutSql);
        }

        public Lazy<List<Dictionary<string, object>>> ExecuteLazySql(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteLazySql(sql, param, db, key, isOutSql);
        }

        public Task<Lazy<List<Dictionary<string, object>>>> ExecuteLazySqlAsy(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteLazySqlAsy(sql, param, db, key, isOutSql);
        }
    }
}
