using System;
using System.Collections.Generic;

namespace FastUntility.Page
{
    /// <summary>
    /// 分页结果
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class PaginationResult<T>
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 数据列表
        /// </summary>
        public List<T> Data { get; set; } = new List<T>();

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPrevious => Page > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNext => Page < TotalPages;
    }

    /// <summary>
    /// 分页结果（非泛型版本，返回字典）
    /// </summary>
    public class PaginationResult
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 数据列表
        /// </summary>
        public List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPrevious => Page > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNext => Page < TotalPages;

        /// <summary>
        /// 从 PageResult 转换
        /// </summary>
        public static PaginationResult FromPageResult(PageResult pageResult, int page, int pageSize)
        {
            var total = pageResult.pModel.TotalRecord;
            return new PaginationResult
            {
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Page = page,
                PageSize = pageSize,
                Data = pageResult.list ?? new List<Dictionary<string, object>>()
            };
        }
    }
}
