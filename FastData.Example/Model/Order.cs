using System;

namespace FastData.Example.Model
{
    /// <summary>
    /// 订单实体示例
    /// </summary>
    public class Order
    {
        /// <summary>
        /// 订单ID（主键）
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 用户ID（外键）
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// 订单金额
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}