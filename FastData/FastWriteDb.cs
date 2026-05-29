using FastData.Context;
using FastData.Model;
#if !NETFRAMEWORK
using FastData.Queue;
#endif
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FastData
{
    /// <summary>
    /// 绑定数据库Key的写入入口
    /// </summary>
    public sealed class FastWriteDb
    {
        private readonly string key;
        private bool enableSqlLog;

        /// <summary>
        /// 获取数据库 Key
        /// </summary>
        public string Key => key;

        internal FastWriteDb(string key)
        {
            this.key = key;
        }

        /// <summary>
        /// 启用当前写入操作的SQL日志（覆盖全局设置）
        /// </summary>
        /// <returns>当前对象（支持链式调用）</returns>
        public FastWriteDb EnableSqlLog()
        {
            this.enableSqlLog = true;
            return this;
        }

#if !NETFRAMEWORK
        /// <summary>
        /// 创建链式写入构建器（带消息队列支持）
        /// </summary>
        /// <returns>链式构建器</returns>
        public FastWriteQueueBuilder Queue()
        {
            return new FastWriteQueueBuilder(key);
        }
#endif

        public WriteReturn AddList<T>(List<T> list, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            return FastWrite.AddList(list, key, IsTrans, isLog);
        }

        public Task<WriteReturn> AddListAsy<T>(List<T> list, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            return FastWrite.AddListAsy(list, key, IsTrans, isLog);
        }

        public WriteReturn Add<T>(T model, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Add(model, db, key, isOutSql);
        }

        public Task<WriteReturn> AddAsy<T>(T model, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.AddAsy(model, db, key, isOutSql);
        }

        public WriteReturn Delete<T>(Expression<Func<T, bool>> predicate, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Delete(predicate, db, key, isOutSql);
        }

        public WriteReturn Delete<T>(T model, DataContext db = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Delete(model, db, key, isTrans, isOutSql);
        }

        public Task<WriteReturn> DeleteAsy<T>(Expression<Func<T, bool>> predicate, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.DeleteAsy(predicate, db, key, isOutSql);
        }

        public Task<WriteReturn> UpdateAsy<T>(T model, DataContext db = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, db, key, isTrans, isOutSql);
        }

        public WriteReturn Update<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Update(model, predicate, field, db, key, isOutSql);
        }

        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, predicate, field, db, key, isOutSql);
        }

        public WriteReturn Update<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Update(model, field, db, key, isOutSql);
        }

        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, field, db, key, isOutSql);
        }

        public WriteReturn UpdateList<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateList(list, field, db, key, isOutSql);
        }

        public Task<WriteReturn> UpdateListAsy<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateListAsy(list, field, db, key, isOutSql);
        }

        public WriteReturn ExecuteSql(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastWrite.ExecuteSql(sql, param, db, key, isOutSql);
        }

        public Task<WriteReturn> ExecuteSqlAsy(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastWrite.ExecuteSqlAsy(sql, param, db, key, isOutSql);
        }

        public WriteReturn BulkInsert<T>(List<T> list, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkInsert(list, db, key);
        }

        public Task<WriteReturn> BulkInsertAsync<T>(List<T> list, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkInsertAsync(list, db, key);
        }
    }
}
