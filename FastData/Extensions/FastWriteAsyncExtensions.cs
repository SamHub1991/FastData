using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FastData.Context;
using FastData.Model;

namespace FastData
{
    /// <summary>
    /// FastWrite 异步扩展方法（统一命名）
    /// </summary>
    public static class FastWriteAsyncExtensions
    {
        /// <summary>
        /// 异步添加实体（推荐使用）
        /// </summary>
        public static async Task<Result<WriteReturn>> AddAsync<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            var result = await FastWrite.AddAsy(model, db, key, isOutSql).ConfigureAwait(false);
            return result.IsSuccess 
                ? Result<WriteReturn>.Ok(result) 
                : Result<WriteReturn>.Fail(result.Message ?? "添加失败");
        }

        /// <summary>
        /// 异步更新实体字段（推荐使用）- 需要传入实体模型
        /// </summary>
        public static async Task<Result<WriteReturn>> UpdateFieldsAsync<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            var result = await FastWrite.UpdateAsy(model, predicate, field, db, key, isOutSql).ConfigureAwait(false);
            return result.IsSuccess 
                ? Result<WriteReturn>.Ok(result) 
                : Result<WriteReturn>.Fail(result.Message ?? "更新失败");
        }

        /// <summary>
        /// 异步更新实体（推荐使用）- 更新整个实体
        /// </summary>
        public static async Task<Result<WriteReturn>> UpdateEntityAsync<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            var result = await FastWrite.UpdateAsy(model, null, db, key, isOutSql).ConfigureAwait(false);
            return result.IsSuccess 
                ? Result<WriteReturn>.Ok(result) 
                : Result<WriteReturn>.Fail(result.Message ?? "更新失败");
        }

        /// <summary>
        /// 异步删除实体（推荐使用）
        /// </summary>
        public static async Task<Result<WriteReturn>> DeleteAsync<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            var result = await FastWrite.DeleteAsy(predicate, db, key, isOutSql).ConfigureAwait(false);
            return result.IsSuccess 
                ? Result<WriteReturn>.Ok(result) 
                : Result<WriteReturn>.Fail(result.Message ?? "删除失败");
        }

        /// <summary>
        /// 异步批量添加列表（推荐使用）
        /// </summary>
        public static async Task<Result<WriteReturn>> AddListAsync<T>(List<T> list, string key = null, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            var result = await FastWrite.AddListAsy(list, key, IsTrans, isLog).ConfigureAwait(false);
            return result.IsSuccess 
                ? Result<WriteReturn>.Ok(result) 
                : Result<WriteReturn>.Fail(result.Message ?? "批量添加失败");
        }

        /// <summary>
        /// 异步批量更新列表（推荐使用）
        /// </summary>
        public static async Task<Result<WriteReturn>> UpdateListAsync<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new()
        {
            var result = await FastWrite.UpdateListAsy(list, field, db, key, isOutSql).ConfigureAwait(false);
            return result.IsSuccess 
                ? Result<WriteReturn>.Ok(result) 
                : Result<WriteReturn>.Fail(result.Message ?? "批量更新失败");
        }

        /// <summary>
        /// 异步执行 SQL（推荐使用）
        /// </summary>
        public static async Task<Result<WriteReturn>> ExecuteSqlAsync(string sql, DataContext db = null, string key = null, bool isOutSql = false)
        {
            var result = await FastWrite.ExecuteSqlAsync(sql, null, db, key, isOutSql);
            return result.IsSuccess 
                ? Result<WriteReturn>.Ok(result) 
                : Result<WriteReturn>.Fail(result.Message ?? "SQL执行失败");
        }
    }
}
