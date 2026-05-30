using FastData.Context;
using FastData.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FastData.Repository
{
    /// <summary>
    /// 写入仓储接口 - 增删改相关操作
    /// </summary>
    public interface IWriteRepository
    {
        /// <summary>
        /// 执行写入操作
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        WriteReturn Write(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 异步执行写入操作
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        Task<WriteReturn> WriteAsync(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 懒加载执行写入操作
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载写入结果</returns>
        Lazy<WriteReturn> WriteLazy(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 异步懒加载执行写入操作
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载写入结果</returns>
        Task<Lazy<WriteReturn>> WriteLazyAsync(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 批量添加实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="key">数据库 key</param>
        /// <param name="IsTrans">是否使用事务</param>
        /// <param name="isLog">是否记录日志</param>
        /// <returns>写入结果</returns>
        WriteReturn AddList<T>(List<T> list, string key = null, bool IsTrans = false, bool isLog = true) where T : class, new();

        /// <summary>
        /// 异步批量添加实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="key">数据库 key</param>
        /// <param name="IsTrans">是否使用事务</param>
        /// <param name="isLog">是否记录日志</param>
        /// <returns>写入结果</returns>
        Task<WriteReturn> AddListAsync<T>(List<T> list, string key = null, bool IsTrans = false, bool isLog = true) where T : class, new();

        /// <summary>
        /// 添加实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        WriteReturn Add<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步添加实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        Task<WriteReturn> AddAsync<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 根据条件删除
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        WriteReturn Delete<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步根据条件删除
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">删除条件</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        Task<WriteReturn> DeleteAsync<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isTrans">是否使用事务</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        WriteReturn Delete<T>(T model, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步更新实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isTrans">是否使用事务</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        Task<WriteReturn> UpdateAsync<T>(T model, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 根据条件更新实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="predicate">更新条件</param>
        /// <param name="field">更新字段</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        WriteReturn Update<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步根据条件更新实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="predicate">更新条件</param>
        /// <param name="field">更新字段</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        Task<WriteReturn> UpdateAsync<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 更新实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="field">更新字段</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isTrans">是否使用事务</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        WriteReturn Update<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步更新实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="model">实体</param>
        /// <param name="field">更新字段</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isTrans">是否使用事务</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        Task<WriteReturn> UpdateAsync<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 批量更新实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="field">更新字段</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        WriteReturn UpdateList<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步批量更新实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">实体列表</param>
        /// <param name="field">更新字段</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        Task<WriteReturn> UpdateListAsync<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 执行 SQL
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        WriteReturn ExecuteSql(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 异步执行 SQL
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>写入结果</returns>
        Task<WriteReturn> ExecuteSqlAsync(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);
    }
}
