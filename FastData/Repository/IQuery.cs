using FastData.Context;
using FastUntility.Page;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FastData.Repository
{
    /// <summary>
    /// 查询抽象基类
    /// 
    /// 定义查询操作的抽象方法，支持多表连接、排序、分组、分页等功能。
    /// </summary>
    public abstract class IQuery
    {
        /// <summary>
        /// 左连接
        /// </summary>
        /// <typeparam name="T">主表类型</typeparam>
        /// <typeparam name="T1">连接表类型</typeparam>
        /// <param name="predicate">连接条件</param>
        /// <param name="field">连接表字段选择</param>
        /// <param name="isDblink">是否使用数据库链接</param>
        /// <returns>查询对象</returns>
        public abstract IQuery LeftJoin<T, T1>(Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false);

        /// <summary>
        /// 右连接
        /// </summary>
        /// <typeparam name="T">主表类型</typeparam>
        /// <typeparam name="T1">连接表类型</typeparam>
        /// <param name="predicate">连接条件</param>
        /// <param name="field">连接表字段选择</param>
        /// <param name="isDblink">是否使用数据库链接</param>
        /// <returns>查询对象</returns>
        public abstract IQuery RightJoin<T, T1>(Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false) where T1 : class, new();

        /// <summary>
        /// 内连接
        /// </summary>
        /// <typeparam name="T">主表类型</typeparam>
        /// <typeparam name="T1">连接表类型</typeparam>
        /// <param name="predicate">连接条件</param>
        /// <param name="field">连接表字段选择</param>
        /// <param name="isDblink">是否使用数据库链接</param>
        /// <returns>查询对象</returns>
        public abstract IQuery InnerJoin<T, T1>(Expression<Func<T, T1, bool>> predicate, Expression<Func<T1, object>> field = null, bool isDblink = false) where T1 : class, new();

        /// <summary>
        /// 排序
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="field">排序字段</param>
        /// <param name="isDesc">是否降序</param>
        /// <returns>查询对象</returns>
        public abstract IQuery OrderBy<T>(Expression<Func<T, object>> field, bool isDesc = true);

        /// <summary>
        /// 分组
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="field">分组字段</param>
        /// <returns>查询对象</returns>
        public abstract IQuery GroupBy<T>(Expression<Func<T, object>> field);

        /// <summary>
        /// 获取前 N 条记录
        /// </summary>
        /// <param name="i">记录数</param>
        /// <returns>查询对象</returns>
        public abstract IQuery Take(int i);

        /// <summary>
        /// 转换为 JSON 字符串
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>JSON 字符串</returns>
        public abstract string ToJson(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步转换为 JSON 字符串
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>JSON 字符串</returns>
        public abstract Task<string> ToJsonAsync(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 惰性转换为 JSON 字符串
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性 JSON 字符串</returns>
        public abstract Lazy<string> ToLazyJson(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步惰性转换为 JSON 字符串
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性 JSON 字符串</returns>
        public abstract Task<Lazy<string>> ToLazyJsonAsync(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 查询单条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>实体对象</returns>
        public abstract T ToItem<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步查询单条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>实体对象</returns>
        public abstract Task<T> ToItemAsync<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 惰性查询单条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性实体对象</returns>
        public abstract Lazy<T> ToLazyItem<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步惰性查询单条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性实体对象</returns>
        public abstract Task<Lazy<T>> ToLazyItemAsync<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 查询记录数
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>记录数</returns>
        public abstract int ToCount(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步查询记录数
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>记录数</returns>
        public abstract Task<int> ToCountAsync<T, T1>(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pModel">分页参数</param>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>分页结果</returns>
        public abstract PageResult<T> ToPage<T>(PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步分页查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pModel">分页参数</param>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>分页结果</returns>
        public abstract Task<PageResult<T>> ToPageAsync<T>(PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 惰性分页查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pModel">分页参数</param>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性分页结果</returns>
        public abstract Lazy<PageResult<T>> ToLazyPage<T>(PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步惰性分页查询
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pModel">分页参数</param>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性分页结果</returns>
        public abstract Task<Lazy<PageResult<T>>> ToLazyPageAsync<T>(PageModel pModel, DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 分页查询（返回字典）
        /// </summary>
        /// <param name="pModel">分页参数</param>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>分页结果</returns>
        public abstract PageResult ToPage(PageModel pModel, DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步分页查询（返回字典）
        /// </summary>
        /// <param name="pModel">分页参数</param>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>分页结果</returns>
        public abstract Task<PageResult> ToPageAsync(PageModel pModel, DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 惰性分页查询（返回字典）
        /// </summary>
        /// <param name="pModel">分页参数</param>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性分页结果</returns>
        public abstract Lazy<PageResult> ToLazyPage(PageModel pModel, DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步惰性分页查询（返回字典）
        /// </summary>
        /// <param name="pModel">分页参数</param>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性分页结果</returns>
        public abstract Task<Lazy<PageResult>> ToLazyPageAsync(PageModel pModel, DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 查询为 DataTable
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>DataTable</returns>
        public abstract DataTable ToDataTable(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步查询为 DataTable
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>DataTable</returns>
        public abstract Task<DataTable> ToDataTableAsync(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 惰性查询为 DataTable
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性 DataTable</returns>
        public abstract Lazy<DataTable> ToLazyDataTable(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步惰性查询为 DataTable
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性 DataTable</returns>
        public abstract Task<Lazy<DataTable>> ToLazyDataTableAsync(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 查询为字典列表
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>字典列表</returns>
        public abstract List<Dictionary<string, object>> ToDics(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步查询为字典列表
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>字典列表</returns>
        public abstract Task<List<Dictionary<string, object>>> ToDicsAsync(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 惰性查询为字典列表
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性字典列表</returns>
        public abstract Lazy<List<Dictionary<string, object>>> ToLazyDics(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步惰性查询为字典列表
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性字典列表</returns>
        public abstract Task<Lazy<List<Dictionary<string, object>>>> ToLazyDicsAsync(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 查询为单个字典
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>字典</returns>
        public abstract Dictionary<string, object> ToDic(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步查询为单个字典
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>字典</returns>
        public abstract Task<Dictionary<string, object>> ToDicAsync(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 惰性查询为单个字典
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性字典</returns>
        public abstract Lazy<Dictionary<string, object>> ToLazyDic(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 异步惰性查询为单个字典
        /// </summary>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性字典</returns>
        public abstract Task<Lazy<Dictionary<string, object>>> ToLazyDicAsync(DataContext db = null, bool isOutSql = false);

        /// <summary>
        /// 查询为实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>实体列表</returns>
        public abstract List<T> ToList<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步查询为实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>实体列表</returns>
        public abstract Task<List<T>> ToListAsync<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 惰性查询为实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性实体列表</returns>
        public abstract Lazy<List<T>> ToLazyList<T>(DataContext db = null, bool isOutSql = false) where T : class, new();

        /// <summary>
        /// 异步惰性查询为实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="db">数据库上下文</param>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <returns>惰性实体列表</returns>
        public abstract Task<Lazy<List<T>>> ToLazyListAsync<T>(DataContext db = null, bool isOutSql = false) where T : class, new();
    }
}
