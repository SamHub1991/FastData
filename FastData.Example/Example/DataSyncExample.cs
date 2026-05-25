using System;
using FastData.Tooling.Sync;

namespace FastData.Example.Example
{
    /// <summary>
    /// 数据同步工具使用示例
    /// </summary>
    public class DataSyncExample
    {
        /// <summary>
        /// 演示数据同步功能
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("=== Data Sync Example ===");
            Console.WriteLine();

            Console.WriteLine("1. 创建同步配置");
            Console.WriteLine("   var options = new DataSyncOptions");
            Console.WriteLine("   {");
            Console.WriteLine("       SourceConnection = \"SourceDb\",");
            Console.WriteLine("       TargetConnection = \"TargetDb\",");
            Console.WriteLine("       Tables = new List<TableSyncConfig>");
            Console.WriteLine("       {");
            Console.WriteLine("           new TableSyncConfig { TableName = \"User\", SyncMode = SyncMode.Upsert }");
            Console.WriteLine("       }");
            Console.WriteLine("   };");
            Console.WriteLine();

            Console.WriteLine("2. 执行同步");
            Console.WriteLine("   var service = new DataSyncService(options);");
            Console.WriteLine("   var result = service.ExecuteSync();");
            Console.WriteLine();

            Console.WriteLine("3. 同步模式说明");
            Console.WriteLine("   - InsertOnly: 只插入新数据");
            Console.WriteLine("   - UpdateOnly: 只更新已存在数据");
            Console.WriteLine("   - Upsert: 存在则更新，不存在则插入");
            Console.WriteLine("   - Full: 全量同步（先删除目标表数据再插入）");
            Console.WriteLine();

            Console.WriteLine("4. 时间范围同步（增量同步）");
            Console.WriteLine("   var startTime = TimeRangeCalculator.GetSyncStartTime(lastSyncTime, 7, false);");
            Console.WriteLine("   var endTime = TimeRangeCalculator.GetSyncEndTime();");
            Console.WriteLine("   options.SyncStartTime = startTime;");
            Console.WriteLine("   options.SyncEndTime = endTime;");
            Console.WriteLine();

            Console.WriteLine("5. 字段选择同步");
            Console.WriteLine("   var tableConfig = new TableSyncConfig");
            Console.WriteLine("   {");
            Console.WriteLine("       TableName = \"User\",");
            Console.WriteLine("       SyncColumns = new List<string> { \"Id\", \"UserName\", \"Email\" }");
            Console.WriteLine("   };");
            Console.WriteLine("   // 只同步指定字段，忽略其他字段");
            Console.WriteLine();

            Console.WriteLine("6. 复合主键支持");
            Console.WriteLine("   var pkConfig = new TablePrimaryKeyConfig");
            Console.WriteLine("   {");
            Console.WriteLine("       TableName = \"OrderItem\",");
            Console.WriteLine("       PrimaryKeyColumns = new List<string> { \"OrderId\", \"ItemId\" }");
            Console.WriteLine("   };");
            Console.WriteLine("   // 用于 Upsert 模式的存在性检查");
            Console.WriteLine();

            Console.WriteLine("提示：数据同步工具 FastData.SyncTool.WinForms 提供可视化配置界面");
            Console.WriteLine();
        }
    }
}