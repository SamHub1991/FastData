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
    /// 通过 FastRead.Use("dbKey") 获取，推荐使用 FastDataClient 作为统一入口
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

        public List<Dictionary<string, object>> ExecuteSql(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteSql(sql, param, db, key, isOutSql);
        }

        public Task<List<Dictionary<string, object>>> ExecuteSqlAsync(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastRead.ExecuteSqlAsync(sql, param, db, key, isOutSql);
        }
    }
}
