using System;
using System.Collections.Generic;
using FastData;
using FastData.Property;

namespace FastData.Example.Example
{
    /// <summary>
    /// 批量操作示例
    /// 演示 BulkInsert、UpdateList、批量删除等高效操作
    /// </summary>
    public static class BulkOperationsExample
    {
        #region Model 定义

        [Table(Name = "BulkOp_Orders")]
        public class BulkOrder
        {
            [Primary]
            public long Id { get; set; }

            [Column(Comments = "订单号")]
            public string OrderNo { get; set; }

            [Column(Comments = "客户 ID")]
            public long CustomerId { get; set; }

            [Column(Comments = "订单金额")]
            public decimal Amount { get; set; }

            [Column(Comments = "订单状态")]
            public int Status { get; set; }

            [Column(Comments = "创建时间")]
            public DateTime CreatedTime { get; set; }

            [Column(Comments = "是否删除（软删除标记）")]
            public bool IsDeleted { get; set; }
        }

        #endregion

        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  批量操作示例 (Bulk Operations)");
            Console.WriteLine("========================================");
            Console.WriteLine();

            // 说明
            Console.WriteLine("【说明】");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("FastData 提供以下批量操作方法：");
            Console.WriteLine("  - FastWrite.BulkInsert<T>(List<T>)      批量插入（比循环 Add 快 10-100 倍）");
            Console.WriteLine("  - FastWrite.UpdateList<T>(List<T>)      批量更新");
            Console.WriteLine("  - FastWrite.Delete<T>(predicate)        批量删除（按条件）");
            Console.WriteLine();

            // 用法示例
            Console.WriteLine("【代码示例】");
            Console.WriteLine("----------------------------------------");
            ShowCodeExamples();
            Console.WriteLine();

            // 软删除说明
            Console.WriteLine("【软删除/逻辑删除】");
            Console.WriteLine("----------------------------------------");
            ShowSoftDeletePattern();
            Console.WriteLine();
        }

        /// <summary>
        /// 展示批量操作代码示例
        /// </summary>
        private static void ShowCodeExamples()
        {
            Console.WriteLine("// 1. 批量插入");
            Console.WriteLine("var orders = new List<BulkOrder>();");
            Console.WriteLine("for (int i = 0; i < 100; i++)");
            Console.WriteLine("    orders.Add(new BulkOrder { OrderNo = $\"ORD{i}\", Amount = 100m });");
            Console.WriteLine("FastWrite.BulkInsert(orders);  // 一条 SQL 插入所有数据");
            Console.WriteLine();

            Console.WriteLine("// 2. 批量更新");
            Console.WriteLine("var orders = FastRead.Query<BulkOrder>(o => o.Status == 0).ToList();");
            Console.WriteLine("foreach (var order in orders)");
            Console.WriteLine("    order.Status = 1;");
            Console.WriteLine("FastWrite.UpdateList(orders);  // 批量更新状态");
            Console.WriteLine();

            Console.WriteLine("// 3. 批量删除");
            Console.WriteLine("FastWrite.Delete<BulkOrder>(o => o.Status == 2);  // 按条件批量删除");
            Console.WriteLine();

            Console.WriteLine("// 4. 事务中的批量操作");
            Console.WriteLine("using (var tran = FastWrite.BeginTrans())");
            Console.WriteLine("{");
            Console.WriteLine("    FastWrite.BulkInsert(newOrders, tran);");
            Console.WriteLine("    FastWrite.UpdateList(updatedOrders, tran);");
            Console.WriteLine("    tran.Commit();");
            Console.WriteLine("}");
        }

        /// <summary>
        /// 展示软删除模式
        /// </summary>
        private static void ShowSoftDeletePattern()
        {
            Console.WriteLine("// 软删除通过在 Model 中添加 IsDeleted 标记实现：");
            Console.WriteLine("public class Entity");
            Console.WriteLine("{");
            Console.WriteLine("    public int Id { get; set; }");
            Console.WriteLine("    public bool IsDeleted { get; set; }  // 软删除标记");
            Console.WriteLine("}");
            Console.WriteLine();

            Console.WriteLine("// 查询时过滤已删除数据：");
            Console.WriteLine("var data = FastRead.Query<Entity>(e => e.IsDeleted == false).ToList();");
            Console.WriteLine();

            Console.WriteLine("// 删除时设置标记而非物理删除：");
            Console.WriteLine("entity.IsDeleted = true;");
            Console.WriteLine("FastWrite.Update(entity);");
            Console.WriteLine();

            Console.WriteLine("// 恢复数据：");
            Console.WriteLine("entity.IsDeleted = false;");
            Console.WriteLine("FastWrite.Update(entity);");
        }
    }
}
