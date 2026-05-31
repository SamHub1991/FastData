using System;
using FastData.Property;

namespace FastData.Example.Model
{
    /// <summary>
    /// 用户实体 - 覆盖 ORM 所有特性标注
    /// </summary>
    [Table(Name = "Users")]
    public class User
    {
        [Primary]
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        [Column(Length = 50, IsNull = false)]
        public string UserName { get; set; }

        [Column(Length = 100)]
        public string Email { get; set; }

        [Column(Length = 20)]
        public string Phone { get; set; }

        [Column(Length = 200)]
        public string PasswordHash { get; set; }

        [Column(Length = 20)]
        public string Role { get; set; }

        public int Age { get; set; }

        public decimal Salary { get; set; }

        [Column(Length = 50)]
        public string Department { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }

        public DateTime? LastLoginTime { get; set; }
    }

    /// <summary>
    /// 商品实体
    /// </summary>
    [Table(Name = "Products")]
    public class Product
    {
        [Primary]
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        [Column(Length = 100, IsNull = false)]
        public string ProductName { get; set; }

        [Column(Length = 50)]
        public string Category { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 订单实体
    /// </summary>
    [Table(Name = "Orders")]
    public class Order
    {
        [Primary]
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Column(Length = 50, IsNull = false)]
        public string OrderNo { get; set; }

        public decimal TotalAmount { get; set; }

        /// <summary>
        /// 订单状态: 0=待支付, 1=已支付, 2=已发货, 3=已完成, 4=已取消
        /// </summary>
        public int Status { get; set; }

        [Column(Length = 200)]
        public string Remark { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? PayTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 订单明细实体
    /// </summary>
    [Table(Name = "OrderItems")]
    public class OrderItem
    {
        [Primary]
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        [Column(Length = 100)]
        public string ProductName { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal Subtotal { get; set; }
    }

    /// <summary>
    /// 操作日志实体（用于审计和分表演示）
    /// </summary>
    [Table(Name = "OperationLogs")]
    public class OperationLog
    {
        [Primary]
        [Column(IsIdentity = true)]
        public long Id { get; set; }

        [Column(Length = 50)]
        public string OperatorName { get; set; }

        [Column(Length = 50)]
        public string OperationType { get; set; }

        [Column(Length = 200)]
        public string Description { get; set; }

        [Column(Length = 50)]
        public string Module { get; set; }

        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 传感器数据实体（消息队列演示）
    /// </summary>
    [Table(Name = "SensorData")]
    public class SensorData
    {
        [Primary]
        [Column(IsIdentity = true)]
        public long Id { get; set; }

        [Column(Length = 50)]
        public string DeviceId { get; set; }

        [Column(Length = 50)]
        public string SensorType { get; set; }

        public decimal Value { get; set; }

        [Column(Length = 20)]
        public string Unit { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
