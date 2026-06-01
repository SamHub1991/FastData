using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FastData.Base;
using FastData.Context;
using FastData.DbTypes;
using FastData.Model;

namespace FastData
{
    /// <summary>
    /// FastData 异步扩展方法 - 提供异步 API 支持
    /// 
    /// 使用说明：
    /// - 将现有的同步方法包装为异步 API
    /// - 支持 CancellationToken 取消操作
    /// - 提供 IAsyncEnumerable 支持（流式处理）
    /// - 适用于需要异步语义的场景
    /// </summary>
    public static class FastReadAsync
    {
        /// <summary>
        /// 异步查询实体列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <param name="key">数据库Key</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体列表</returns>
        public static Task<List<T>> ToListAsync<T>(
            Expression<Func<T, bool>> predicate = null,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return Task.Run(() => FastRead.Query<T>(predicate).ToList(), cancellationToken);
        }

        /// <summary>
        /// 异步流式查询（支持大量数据）
        /// 使用分批查询实现流式处理，减少内存占用
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <param name="key">数据库Key</param>
        /// <param name="batchSize">批次大小（默认 1000）</param>
        /// <returns>异步枚举器</returns>
        public static async IAsyncEnumerable<T> ToListStreamingAsync<T>(
            Expression<Func<T, bool>> predicate = null,
            string key = null,
            int batchSize = 1000) where T : class, new()
        {
            int skip = 0;
            while (true)
            {
                var batch = await ToListAsync(predicate, key).ContinueWith(t => 
                    t.Result.Skip(skip).Take(batchSize).ToList());
                
                if (batch == null || !batch.Any())
                    yield break;
                
                foreach (var item in batch)
                {
                    yield return item;
                }
                
                skip += batchSize;
            }
        }

        /// <summary>
        /// 异步查询第一个实体
        /// </summary>
        public static Task<T> FirstOrDefaultAsync<T>(
            Expression<Func<T, bool>> predicate,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return Task.Run(() => FastRead.Query<T>(predicate).ToItem(), cancellationToken);
        }

        /// <summary>
        /// 异步查询数量
        /// </summary>
        public static Task<int> CountAsync<T>(
            Expression<Func<T, bool>> predicate = null,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return Task.Run(() => FastRead.Query<T>(predicate).Count(), cancellationToken);
        }

        /// <summary>
        /// 异步分页查询
        /// </summary>
        public static Task<(List<T> Items, int Total)> ToListPagedAsync<T>(
            Expression<Func<T, bool>> predicate,
            int pageIndex,
            int pageSize,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "页码不能小于 0");
            if (pageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "每页大小必须大于 0");

            return Task.Run(() => 
            {
                var allItems = FastRead.Query<T>(predicate).ToList();
                var total = allItems.Count;
                var items = allItems.Skip(pageIndex * pageSize).Take(pageSize).ToList();
                return (items, total);
            }, cancellationToken);
        }
    }

    /// <summary>
    /// FastWrite 异步扩展方法
    /// </summary>
    public static class FastWriteAsync
    {
        /// <summary>
        /// 异步添加实体
        /// </summary>
        public static Task<WriteReturn> AddAsync<T>(
            T model,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return Task.Run(() => FastWrite.Add(model), cancellationToken);
        }

        /// <summary>
        /// 异步批量添加（使用 SqlBulkCopy）
        /// </summary>
        public static Task<WriteReturn> BulkInsertAsync<T>(
            List<T> list,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            if (list == null || !list.Any())
                return Task.FromResult(new WriteReturn { IsSuccess = true });

            return Task.Run(() => FastWrite.BulkInsert(list), cancellationToken);
        }

        /// <summary>
        /// 异步更新实体
        /// </summary>
        public static Task<WriteReturn> UpdateAsync<T>(
            T model,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return Task.Run(() => FastWrite.Update(model), cancellationToken);
        }

        /// <summary>
        /// 异步删除实体
        /// </summary>
        public static Task<WriteReturn> DeleteAsync<T>(
            Expression<Func<T, bool>> predicate,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return Task.Run(() => FastWrite.Delete(predicate), cancellationToken);
        }

        /// <summary>
        /// 异步批量更新
        /// </summary>
        public static Task<WriteReturn> BulkUpdateAsync<T>(
            List<T> list,
            Expression<Func<T, bool>> predicate,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            if (list == null || !list.Any())
                return Task.FromResult(new WriteReturn { IsSuccess = true });

            return Task.Run(() => FastWrite.BulkUpdate(list, predicate), cancellationToken);
        }

        /// <summary>
        /// 异步批量删除
        /// </summary>
        public static Task<WriteReturn> BulkDeleteAsync<T>(
            Expression<Func<T, bool>> predicate,
            string key = null,
            CancellationToken cancellationToken = default) where T : class, new()
        {
            return Task.Run(() => FastWrite.BulkDelete(predicate), cancellationToken);
        }
    }
}