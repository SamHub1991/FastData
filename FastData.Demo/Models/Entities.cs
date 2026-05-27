using System;
using System.ComponentModel.DataAnnotations;

namespace FastData.Demo.Models
{
    /// <summary>
    /// 用户实体
    /// </summary>
    public class User
    {
        /// <summary>
        /// 用户ID
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
        /// 手机号
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// 部门
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// 薪资
        /// </summary>
        public decimal Salary { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 订单实体
    /// </summary>
    public class Order
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 商品名称
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 状态：0-待支付 1-已支付 2-已发货 3-已完成 4-已取消
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime? PayTime { get; set; }
    }

    /// <summary>
    /// 商品实体
    /// </summary>
    public class Product
    {
        /// <summary>
        /// 商品ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 商品名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 分类
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 库存
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否上架
        /// </summary>
        public bool IsOnSale { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 操作日志实体
    /// </summary>
    public class OperationLog
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 操作模块
        /// </summary>
        public string Module { get; set; }

        /// <summary>
        /// 操作详情
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
