using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FastData.Base;
using FastData.Context;
using FastData.Model;
using FastUntility.Page;

namespace FastData
{
    /// <summary>
    /// 投影查询（支持匿名类型）
    /// </summary>
    /// <typeparam name="T">源实体类型</typeparam>
    /// <typeparam name="TResult">投影结果类型</typeparam>
    public sealed class ProjectedQuery<T, TResult> where T : class, new()
    {
        private readonly DataQuery _query;
        private readonly Func<T, TResult> _selector;

        internal ProjectedQuery(DataQuery query, Expression<Func<T, TResult>> selector)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _selector = selector?.Compile() ?? throw new ArgumentNullException(nameof(selector));
        }

        /// <summary>
        /// 执行查询并返回投影后的列表
        /// </summary>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>投影结果列表</returns>
        public List<TResult> ToList(DataContext db = null, bool isOutSql = false)
        {
            var stopwatch = new Stopwatch();
            var result = new DataReturn<T>();

            if (_query.Predicate.Exists(a => a.IsSuccess == false))
                return new List<TResult>();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(_query.Key))
                {
                    result = tempDb.GetList<T>(_query);
                }
            }
            else
                result = db.GetList<T>(_query);

            stopwatch.Stop();

            _query.Config.IsOutSql = _query.Config.IsOutSql || isOutSql;
            DbLog.LogSql(_query.Config.IsOutSql, result.Sql, _query.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.List.Select(_selector).ToList();
        }

        /// <summary>
        /// 执行查询并返回投影后的列表（异步）
        /// </summary>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>投影结果列表任务</returns>
        public Task<List<TResult>> ToListAsync(DataContext db = null, bool isOutSql = false)
        {
            return Task.Run(() => ToList(db, isOutSql));
        }

        /// <summary>
        /// 分页查询并返回投影后的结果
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页条数</param>
        /// <param name="db">数据上下文</param>
        /// <param name="isOutSql">是否输出SQL</param>
        /// <returns>分页结果</returns>
        public PaginationResult<TResult> ToPagination(int page, int pageSize, DataContext db = null, bool isOutSql = false)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var pModel = new PageModel
            {
                PageId = page,
                PageSize = pageSize
            };

            var stopwatch = new Stopwatch();
            var result = new DataReturn<T>();

            if (_query.Predicate.Exists(a => a.IsSuccess == false))
                return new PaginationResult<TResult>();

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(_query.Key))
                {
                    result = tempDb.GetPage<T>(_query, pModel);
                }
            }
            else
                result = db.GetPage<T>(_query, pModel);

            stopwatch.Stop();

            _query.Config.IsOutSql = _query.Config.IsOutSql || isOutSql;
            DbLog.LogSql(_query.Config.IsOutSql, result.Sql, _query.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            var total = result.PageResult.pModel.TotalRecord;
            var projectedData = result.PageResult.list.Select(_selector).ToList();

            return new PaginationResult<TResult>
            {
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Page = page,
                PageSize = pageSize,
                Data = projectedData
            };
        }

        /// <summary>
        /// 分页查询并返回投影后的结果（异步）
        /// </summary>
        public Task<PaginationResult<TResult>> ToPaginationAsync(int page, int pageSize, DataContext db = null, bool isOutSql = false)
        {
            return Task.Run(() => ToPagination(page, pageSize, db, isOutSql));
        }

        /// <summary>
        /// 分页查询（使用 PaginationRequest）
        /// </summary>
        public PaginationResult<TResult> ToPagination(PaginationRequest request, DataContext db = null, bool isOutSql = false)
        {
            return ToPagination(request.Page, request.PageSize, db, isOutSql);
        }

        /// <summary>
        /// 分页查询（使用 PaginationRequest，异步）
        /// </summary>
        public Task<PaginationResult<TResult>> ToPaginationAsync(PaginationRequest request, DataContext db = null, bool isOutSql = false)
        {
            return Task.Run(() => ToPagination(request, db, isOutSql));
        }

        /// <summary>
        /// 返回单个实体
        /// </summary>
        public TResult ToItem(DataContext db = null, bool isOutSql = false)
        {
            var stopwatch = new Stopwatch();
            var result = new DataReturn<T>();

            if (_query.Predicate.Exists(a => a.IsSuccess == false))
                return default(TResult);

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(_query.Key))
                {
                    result = tempDb.GetList<T>(_query);
                }
            }
            else
                result = db.GetList<T>(_query);

            stopwatch.Stop();

            _query.Config.IsOutSql = _query.Config.IsOutSql || isOutSql;
            DbLog.LogSql(_query.Config.IsOutSql, result.Sql, _query.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            var item = result.List.FirstOrDefault();
            return item != null ? _selector(item) : default(TResult);
        }

        /// <summary>
        /// 返回单个实体（异步）
        /// </summary>
        public Task<TResult> ToItemAsync(DataContext db = null, bool isOutSql = false)
        {
            return Task.Run(() => ToItem(db, isOutSql));
        }

        /// <summary>
        /// 返回数量
        /// </summary>
        public int ToCount(DataContext db = null, bool isOutSql = false)
        {
            var stopwatch = new Stopwatch();
            var result = new DataReturn<T>();

            if (_query.Predicate.Exists(a => a.IsSuccess == false))
                return 0;

            stopwatch.Start();

            if (db == null)
            {
                using (var tempDb = new DataContext(_query.Key))
                {
                    result = tempDb.GetList<T>(_query);
                }
            }
            else
                result = db.GetList<T>(_query);

            stopwatch.Stop();

            _query.Config.IsOutSql = _query.Config.IsOutSql || isOutSql;
            DbLog.LogSql(_query.Config.IsOutSql, result.Sql, _query.Config.DbType, stopwatch.Elapsed.TotalMilliseconds);

            return result.List.Count;
        }
    }
}
