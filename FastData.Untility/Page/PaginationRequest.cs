using System.ComponentModel.DataAnnotations;

namespace FastUntility.Page
{
    /// <summary>
    /// 分页请求参数
    /// </summary>
    public class PaginationRequest
    {
        /// <summary>
        /// 页码（从 1 开始）
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "页码必须大于 0")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        [Range(1, 1000, ErrorMessage = "每页条数必须在 1-1000 之间")]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 转换为 PageModel
        /// </summary>
        internal PageModel ToPageModel()
        {
            return new PageModel
            {
                PageId = Page < 1 ? 1 : Page,
                PageSize = PageSize < 1 ? 10 : PageSize
            };
        }
    }
}
