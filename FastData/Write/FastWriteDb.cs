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
    /// 通过 FastWrite.Use("dbKey") 获取，推荐使用 FastDataClient 作为统一入口
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

        /// <summary>
        /// 批量添加实体到数据库
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">要添加的实体列表</param>
        /// <param name="IsTrans">是否使用事务，默认 false</param>
        /// <param name="isLog">是否记录日志，默认 true</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn AddList<T>(List<T> list, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            return FastWrite.AddList(list, key, IsTrans, isLog || enableSqlLog);
        }

        /// <summary>
        /// 批量添加实体到数据库（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">要添加的实体列表</param>
        /// <param name="IsTrans">是否使用事务，默认 false</param>
        /// <param name="isLog">是否记录日志，默认 true</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> AddListAsy<T>(List<T> list, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            return FastWrite.AddListAsy(list, key, IsTrans, isLog || enableSqlLog);
        }

        /// <summary>
        /// 添加单个实体到数据库
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">要添加的实体对象</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn Add<T>(T model, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Add(model, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 添加单个实体到数据库（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">要添加的实体对象</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> AddAsy<T>(T model, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.AddAsy(model, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 根据条件删除实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件表达式</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn Delete<T>(Expression<Func<T, bool>> predicate, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Delete(predicate, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 根据实体主键删除记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">包含主键值的实体对象</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isTrans">是否使用事务，默认 false</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn Delete<T>(T model, DataContext db = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Delete(model, db, key, isTrans, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 根据条件删除实体（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件表达式</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> DeleteAsy<T>(Expression<Func<T, bool>> predicate, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.DeleteAsy(predicate, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 更新单个实体（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">要更新的实体对象</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isTrans">是否使用事务，默认 false</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> UpdateAsy<T>(T model, DataContext db = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, null, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 根据条件更新实体的指定字段
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">包含更新值的实体对象</param>
        /// <param name="predicate">更新条件表达式</param>
        /// <param name="field">要更新的字段表达式，为 null 时更新所有字段</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn Update<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Update(model, predicate, field, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 根据条件更新实体的指定字段（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">包含更新值的实体对象</param>
        /// <param name="predicate">更新条件表达式</param>
        /// <param name="field">要更新的字段表达式，为 null 时更新所有字段</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, predicate, field, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 根据实体主键更新记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">包含更新值的实体对象</param>
        /// <param name="field">要更新的字段表达式，为 null 时更新所有字段</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn Update<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Update(model, field, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 根据实体主键更新记录（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">包含更新值的实体对象</param>
        /// <param name="field">要更新的字段表达式，为 null 时更新所有字段</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, field, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 批量更新实体列表的指定字段
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">要更新的实体列表</param>
        /// <param name="field">要更新的字段表达式，为 null 时更新所有字段</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn UpdateList<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateList(list, field, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 批量更新实体列表的指定字段（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">要更新的实体列表</param>
        /// <param name="field">要更新的字段表达式，为 null 时更新所有字段</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> UpdateListAsy<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateListAsy(list, field, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 执行原生SQL语句
        /// </summary>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="param">SQL参数数组，可为 null</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn ExecuteSql(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastWrite.ExecuteSql(sql, param, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 执行原生SQL语句（异步）
        /// </summary>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="param">SQL参数数组，可为 null</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <param name="isOutSql">是否输出SQL语句，默认 false</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> ExecuteSqlAsync(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastWrite.ExecuteSqlAsync(sql, param, db, key, isOutSql || enableSqlLog);
        }

        /// <summary>
        /// 批量插入大量数据（高性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">要插入的实体列表</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn BulkInsert<T>(List<T> list, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkInsert(list, db, key);
        }

        /// <summary>
        /// 批量插入大量数据（异步，高性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">要插入的实体列表</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> BulkInsertAsync<T>(List<T> list, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkInsertAsync(list, db, key);
        }

        /// <summary>
        /// 批量更新大量数据（高性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">要更新的实体列表</param>
        /// <param name="predicate">更新条件表达式</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn BulkUpdate<T>(List<T> list, Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkUpdate(list, predicate, db, key);
        }

        /// <summary>
        /// 批量更新大量数据（异步，高性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">要更新的实体列表</param>
        /// <param name="predicate">更新条件表达式</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> BulkUpdateAsync<T>(List<T> list, Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkUpdateAsync(list, predicate, db, key);
        }

        /// <summary>
        /// 批量删除数据（高性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件表达式</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <returns>写入操作结果</returns>
        public WriteReturn BulkDelete<T>(Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkDelete(predicate, db, key);
        }

        /// <summary>
        /// 批量删除数据（异步，高性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件表达式</param>
        /// <param name="db">数据上下文，可为 null 使用默认上下文</param>
        /// <returns>写入操作结果的异步任务</returns>
        public Task<WriteReturn> BulkDeleteAsync<T>(Expression<Func<T, bool>> predicate, DataContext db = null) where T : class, new()
        {
            return FastWrite.BulkDeleteAsync(predicate, db, key);
        }
    }
}
