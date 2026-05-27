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
        List<T> Query<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Task<List<T>> QueryAsy<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Lazy<List<T>> QueryLazy<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Task<Lazy<List<T>>> QueryLazyAsy<T>(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        List<Dictionary<string, object>> Query(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Task<List<Dictionary<string, object>>> QueryAsy(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Lazy<List<Dictionary<string, object>>> QueryLazy(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Task<Lazy<List<Dictionary<string, object>>>> QueryLazyAsy(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        PageResult QueryPage(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Task<PageResult> QueryPageAsy(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Lazy<PageResult> QueryPageLazy(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Task<Lazy<PageResult>> QueryPageLazyAsy(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        PageResult<T> QueryPage<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Task<PageResult<T>> QueryPageAsy<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Lazy<PageResult<T>> QueryPageLazy<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Task<Lazy<PageResult<T>>> QueryPageLazyAsy<T>(PageModel pModel, string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        IQuery Query<T>(System.Linq.Expressions.Expression<Func<T, bool>> predicate, System.Linq.Expressions.Expression<Func<T, object>> field = null, string key = null, string dbFile = "db.config");
    }
}
