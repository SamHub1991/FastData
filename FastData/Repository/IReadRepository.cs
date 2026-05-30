using FastData.Context;
using FastData.Model;
using FastUntility.Page;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace FastData.Repository
{
    /// <summary>
    /// 读取仓储接口 - 查询相关操作
    /// </summary>
    public interface IReadRepository
    {
        /// <summary>
        /// 查询返回实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>实体列表</returns>
        List<T> Query<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步查询返回实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>实体列表</returns>
        Task<List<T>> QueryAsync<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 懒加载查询返回实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载实体列表</returns>
        Lazy<List<T>> QueryLazy<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步懒加载查询返回实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载实体列表</returns>
        Task<Lazy<List<T>>> QueryLazyAsync<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 查询返回字典列表
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>字典列表</returns>
        List<Dictionary<string, object>> Query(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 异步查询返回字典列表
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>字典列表</returns>
        Task<List<Dictionary<string, object>>> QueryAsync(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 懒加载查询返回字典列表
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载字典列表</returns>
        Lazy<List<Dictionary<string, object>>> QueryLazy(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 异步懒加载查询返回字典列表
        /// </summary>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载字典列表</returns>
        Task<Lazy<List<Dictionary<string, object>>>> QueryLazyAsync(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 分页查询返回字典结果
        /// </summary>
        /// <param name="pModel">分页模型</param>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>分页结果</returns>
        PageResult QueryPage(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 异步分页查询返回字典结果
        /// </summary>
        /// <param name="pModel">分页模型</param>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>分页结果</returns>
        Task<PageResult> QueryPageAsync(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 懒加载分页查询返回字典结果
        /// </summary>
        /// <param name="pModel">分页模型</param>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载分页结果</returns>
        Lazy<PageResult> QueryPageLazy(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 异步懒加载分页查询返回字典结果
        /// </summary>
        /// <param name="pModel">分页模型</param>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载分页结果</returns>
        Task<Lazy<PageResult>> QueryPageLazyAsync(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        /// <summary>
        /// 分页查询返回实体结果
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pModel">分页模型</param>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>分页结果</returns>
        PageResult<T> QueryPage<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步分页查询返回实体结果
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pModel">分页模型</param>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>分页结果</returns>
        Task<PageResult<T>> QueryPageAsync<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 懒加载分页查询返回实体结果
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pModel">分页模型</param>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载分页结果</returns>
        Lazy<PageResult<T>> QueryPageLazy<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步懒加载分页查询返回实体结果
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pModel">分页模型</param>
        /// <param name="name">SQL 名称</param>
        /// <param name="param">参数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="key">数据库 key</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>懒加载分页结果</returns>
        Task<Lazy<PageResult<T>>> QueryPageLazyAsync<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 链式查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <param name="field">字段选择</param>
        /// <param name="key">数据库 key</param>
        /// <param name="dbFile">配置文件</param>
        /// <returns>查询对象</returns>
        IQuery Query<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate, System.Linq.Expressions.Expression<Func<T, object>> field = null, string key = null, string dbFile = "db.config");
    }
}
