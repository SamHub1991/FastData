using FastData.Context;
using FastUntility.Page;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FastData.Repository
{
    public abstract class IQuery
    {
        public abstract IQuery LeftJoin<T, T1>(Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false);

        public abstract IQuery RightJoin<T, T1>(Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false) where T1 : class, new();

        public abstract IQuery InnerJoin<T, T1>(Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false) where T1 : class, new();

        public abstract IQuery OrderBy<T>(Expression<Func<T, object>> field, bool isDesc = true);

        public abstract IQuery GroupBy<T>(Expression<Func<T, object>> field);

        public abstract IQuery Take(int i);

        public abstract string ToJson(DataContext db = null, bool isOutSql = false);

        public abstract Task<string> ToJsonAsync(DataContext db = null, bool isOutSql = false);

        public abstract Lazy<string> ToLazyJson(DataContext db = null, bool isOutSql = false);

        public abstract Task<Lazy<string>> ToLazyJsonAsync(DataContext db = null, bool isOutSql = false);

        public abstract T ToItem<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract Task<T> ToItemAsy<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract Lazy<T> ToLazyItem<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract Task<Lazy<T>> ToLazyItemAsy<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract int ToCount(DataContext db = null, bool isOutSql = false);

        public abstract Task<int> ToCountAsy<T, T1>(DataContext db = null, bool isOutSql = false);

        public abstract PageResult<T> ToPage<T>(PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract Task<PageResult<T>> ToPageAsync<T>(PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract Lazy<PageResult<T>> ToLazyPage<T>(PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract Task<Lazy<PageResult<T>>> ToLazyPageAsync<T>(PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract PageResult ToPage(PageModel pModel, DataContext db = null, bool isOutSql = false);

        public abstract Task<PageResult> ToPageAsync(PageModel pModel, DataContext db = null, bool isOutSql = false);

        public abstract Lazy<PageResult> ToLazyPage(PageModel pModel, DataContext db = null, bool isOutSql = false);

        public abstract Task<Lazy<PageResult>> ToLazyPageAsync(PageModel pModel, DataContext db = null, bool isOutSql = false);

        public abstract DataTable ToDataTable(DataContext db = null, bool isOutSql = false);

        public abstract Task<DataTable> ToDataTableAsync(DataContext db = null, bool isOutSql = false);

        public abstract Lazy<DataTable> ToLazyDataTable(DataContext db = null, bool isOutSql = false);

        public abstract Task<Lazy<DataTable>> ToLazyDataTableAsync(DataContext db = null, bool isOutSql = false);

        public abstract List<Dictionary<string, object>> ToDics(DataContext db = null, bool isOutSql = false);

        public abstract Task<List<Dictionary<string, object>>> ToDicsAsync(DataContext db = null, bool isOutSql = false);

        public abstract Lazy<List<Dictionary<string, object>>> ToLazyDics(DataContext db = null, bool isOutSql = false);

        public abstract Task<Lazy<List<Dictionary<string, object>>>> ToLazyDicsAsync(DataContext db = null, bool isOutSql = false);

        public abstract Dictionary<string, object> ToDic(DataContext db = null, bool isOutSql = false);

        public abstract Task<Dictionary<string, object>> ToDicAsync(DataContext db = null, bool isOutSql = false);

        public abstract Lazy<Dictionary<string, object>> ToLazyDic(DataContext db = null, bool isOutSql = false);

        public abstract Task<Lazy<Dictionary<string, object>>> ToLazyDicAsync(DataContext db = null, bool isOutSql = false);

        public abstract List<T> ToList<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract Task<List<T>> ToListAsy<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract Lazy<List<T>> ToLazyList<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        public abstract Task<Lazy<List<T>>> ToLazyListAsy<T>(DataContext db = null, bool isOutSql = false) where T : class, new();
    }
}
