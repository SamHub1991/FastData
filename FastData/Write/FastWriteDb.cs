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
    /// FastData 绑定数据库 Key 的写入操作（实例方法）
    /// 
    /// 职责：
    /// 1. 绑定特定数据库 Key，避免重复传递 key 参数
    /// 2. 提供数据添加、更新、删除等写入操作
    /// 3. 支持 SQL 日志开关（覆盖全局设置）
    /// 4. 所有方法内部委托给 FastWrite 静态方法
    /// 
    /// 使用示例：
    /// <code>
    /// // 创建绑定 Key 的写入实例
    /// var db1 = FastWrite.Use("db1");
    /// 
    /// // 启用 SQL 日志（链式调用）
    /// db1.EnableSqlLog().Add(user);
    /// 
    /// // 添加数据
    /// var result = db1.Add(user);
    /// var result = db1.AddList(userList);
    /// 
    /// // 更新数据
    /// var result = db1.Update(user);
    /// var result = db1.Update(user, u =&gt; new { u.Name });
    /// 
    /// // 删除数据
    /// var result = db1.Delete&lt;User&gt;(u =&gt; u.Age &lt; 18);
    /// 
    /// // 推荐使用 FastDataClient 代替
    /// var client = new FastDataClient("db1");
    /// client.EnableSqlLog().Add(user);
    /// </code>
    /// 
    /// 相关类：
    /// - FastWrite: 写入操作（静态方法，需显式传递 key）
    /// - FastDataClient: 统一门面（推荐，整合所有功能）
    /// - FastReadDb: 绑定 Key 的读取操作
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
            var builder = new FastWriteQueueBuilder(key);
            if (enableSqlLog)
                builder.EnableSqlLog();
            return builder;
        }
#endif

        public WriteReturn AddList<T>(List<T> list, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            return FastWrite.AddList(list, key, IsTrans, isLog || enableSqlLog);
        }

        public Task<WriteReturn> AddListAsy<T>(List<T> list, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            return FastWrite.AddListAsy(list, key, IsTrans, isLog || enableSqlLog);
        }

        public WriteReturn Add<T>(T model, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Add(model, db, key, isOutSql || enableSqlLog);
        }

        public Task<WriteReturn> AddAsy<T>(T model, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.AddAsy(model, db, key, isOutSql || enableSqlLog);
        }

        public WriteReturn Delete<T>(Expression<Func<T, bool>> predicate, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Delete(predicate, db, key, isOutSql || enableSqlLog);
        }

        public WriteReturn Delete<T>(T model, DataContext db = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Delete(model, db, key, isTrans, isOutSql || enableSqlLog);
        }

        public Task<WriteReturn> DeleteAsy<T>(Expression<Func<T, bool>> predicate, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.DeleteAsy(predicate, db, key, isOutSql || enableSqlLog);
        }

        public Task<WriteReturn> UpdateAsy<T>(T model, DataContext db = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, null, db, key, isOutSql || enableSqlLog);
        }

        public WriteReturn Update<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Update(model, predicate, field, db, key, isOutSql || enableSqlLog);
        }

        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, predicate, field, db, key, isOutSql || enableSqlLog);
        }

        public WriteReturn Update<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Update(model, field, db, key, isOutSql || enableSqlLog);
        }

        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, field, db, key, isOutSql || enableSqlLog);
        }

        public WriteReturn UpdateList<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateList(list, field, db, key, isOutSql || enableSqlLog);
        }

        public Task<WriteReturn> UpdateListAsy<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateListAsy(list, field, db, key, isOutSql || enableSqlLog);
        }

        public WriteReturn ExecuteSql(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastWrite.ExecuteSql(sql, param, db, key, isOutSql || enableSqlLog);
        }

        public Task<WriteReturn> ExecuteSqlAsync(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastWrite.ExecuteSqlAsync(sql, param, db, key, isOutSql || enableSqlLog);
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
