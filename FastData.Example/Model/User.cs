using System;

namespace FastData.Example.Model
{
    /// <summary>
    /// 用户实体示例
    /// </summary>
    public class User
    {
        /// <summary>
        /// 用户ID（主键）
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }
    }
}