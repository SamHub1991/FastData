using System;
using System.Collections.Generic;
using System.IO;
using FastData;
using FastData.Context;
using FastData.Model;
using FastData.Property;

namespace FastData.Example.Example
{
    /// <summary>
    /// 事务使用示例
    /// 场景：基本事务、异常回滚、批量写入、多表事务
    /// 使用 SQLite 数据库动态演示
    /// </summary>
    public static class TransactionExample
    {
        /// <summary>
        /// 运行所有事务示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  FastData ORM 事务使用示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // 初始化数据库和表结构
                SetupDatabase();

                // 依次演示各种事务场景
                DemoBasicTransaction();
                DemoErrorRollback();
                DemoBatchTransaction();
                DemoMultiTableTransaction();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"示例运行异常: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("--- 事务示例演示完成 ---");
        }

        #region 数据库初始化

        /// <summary>
        /// 初始化 SQLite 数据库并创建测试表
        /// </summary>
        private static void SetupDatabase()
        {
            Console.WriteLine("--- 正在初始化数据库 ---");

            // 每次运行时删除旧数据库，保证环境干净
            var dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "transaction_example.db");
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
                Console.WriteLine("已删除旧数据库文件");
            }

            // 创建两张测试表：TransUsers 和 TransUserLogs
            var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS TransUsers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserName TEXT NOT NULL UNIQUE,
                    Email TEXT NOT NULL,
                    Age INTEGER NOT NULL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS TransUserLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    ActionType TEXT NOT NULL,
                    Description TEXT,
                    CreatedAt TEXT NOT NULL
                );
            ";

            using (var db = new DataContext("Sqlite"))
            {
                try
                {
                    // 执行建表语句
                    var result = db.ExecuteSql(createTablesSql, isTrans: false);
                    if (result.WriteReturn.IsSuccess)
                    {
                        Console.WriteLine("数据库表创建成功: TransUsers, TransUserLogs");
                    }
                    else
                    {
                        Console.WriteLine($"建表失败: {result.WriteReturn.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"建表异常: {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine();
        }

        #endregion

        #region 示例 1: 基本事务操作

        /// <summary>
        /// 演示基本事务：批量插入 3 个用户，失败时回滚
        /// </summary>
        private static void DemoBasicTransaction()
        {
            Console.WriteLine("=== 示例 1: 基本事务操作 ===");
            Console.WriteLine("场景：批量插入用户，失败时回滚所有操作");
            Console.WriteLine();

            var users = new List<TransUser>
            {
                new TransUser { UserName = "张三", Email = "zhangsan@example.com", Age = 25, IsActive = 1, CreatedAt = DateTime.Now },
                new TransUser { UserName = "李四", Email = "lisi@example.com", Age = 30, IsActive = 1, CreatedAt = DateTime.Now },
                new TransUser { UserName = "王五", Email = "wangwu@example.com", Age = 28, IsActive = 1, CreatedAt = DateTime.Now }
            };

            using (var db = new DataContext("Sqlite"))
            {
                try
                {
                    // 开始事务
                    db.BeginTrans();

                    var totalAffected = 0;

                    // 批量插入（使用同一个 DataContext，共享事务）
                    foreach (var user in users)
                    {
                        var result = db.Add(user, isTrans: false);
                        if (!result.WriteReturn.IsSuccess)
                        {
                            // 插入失败，手动回滚
                            Console.WriteLine($"插入失败: {result.WriteReturn.Message}");
                            db.RollbackTrans();
                            Console.WriteLine($"事务已回滚，已插入 {totalAffected} 条数据被撤销");
                            Console.WriteLine($"操作结果: 失败");
                            Console.WriteLine();
                            return;
                        }
                        totalAffected++;
                    }

                    // 所有操作成功，提交事务
                    db.SubmitTrans();
                    Console.WriteLine($"批量插入成功，共插入 {totalAffected} 条记录");
                    Console.WriteLine($"事务已提交");
                    Console.WriteLine($"操作结果: 成功");
                }
                catch (Exception ex)
                {
                    // 发生异常，回滚事务
                    db.RollbackTrans();
                    Console.WriteLine($"事务异常: {ex.Message}");
                    Console.WriteLine($"事务已自动回滚");
                    Console.WriteLine($"操作结果: 失败");
                }
            }

            Console.WriteLine();

            // 验证数据
            VerifyUserCount("示例1插入后");
        }

        #endregion

        #region 示例 2: 异常回滚演示

        /// <summary>
        /// 演示异常回滚：故意插入重复用户名触发唯一约束冲突
        /// </summary>
        private static void DemoErrorRollback()
        {
            Console.WriteLine("=== 示例 2: 异常回滚演示 ===");
            Console.WriteLine("场景：插入违反唯一约束的数据，展示事务自动回滚");
            Console.WriteLine();

            // "张三"已在示例1中插入，这里再次插入会触发唯一约束冲突
            var users = new List<TransUser>
            {
                new TransUser { UserName = "赵六", Email = "zhaoliu@example.com", Age = 35, IsActive = 1, CreatedAt = DateTime.Now },
                new TransUser { UserName = "张三", Email = "duplicate@example.com", Age = 99, IsActive = 1, CreatedAt = DateTime.Now } // 重复用户名
            };

            using (var db = new DataContext("Sqlite"))
            {
                try
                {
                    // 开始事务
                    db.BeginTrans();

                    var totalAffected = 0;

                    foreach (var user in users)
                    {
                        var result = db.Add(user, isTrans: false);
                        if (!result.WriteReturn.IsSuccess)
                        {
                            // 插入失败，回滚整个事务
                            Console.WriteLine($"插入用户[{user.UserName}]失败: {result.WriteReturn.Message}");
                            db.RollbackTrans();
                            Console.WriteLine($"回滚原因: 违反数据库约束（用户名唯一）");
                            Console.WriteLine($"事务已回滚，已插入 {totalAffected} 条数据被撤销（赵六的记录也被回滚）");
                            Console.WriteLine($"操作结果: 失败（事务回滚）");
                            Console.WriteLine();
                            return;
                        }
                        totalAffected++;
                        Console.WriteLine($"  已插入: {user.UserName}");
                    }

                    db.SubmitTrans();
                    Console.WriteLine($"事务提交成功，共插入 {totalAffected} 条记录");
                    Console.WriteLine($"操作结果: 成功");
                }
                catch (Exception ex)
                {
                    db.RollbackTrans();
                    Console.WriteLine($"事务异常: {ex.Message}");
                    Console.WriteLine($"事务已自动回滚");
                    Console.WriteLine($"操作结果: 失败");
                }
            }

            Console.WriteLine();

            // 验证数据（赵六不应存在，因为事务回滚了）
            VerifyUserCount("示例2回滚后");
        }

        #endregion

        #region 示例 3: 批量写入事务

        /// <summary>
        /// 演示批量写入：50条数据分批写入，每批独立事务
        /// </summary>
        private static void DemoBatchTransaction()
        {
            Console.WriteLine("=== 示例 3: 批量写入事务 ===");
            Console.WriteLine("场景：大批量数据（50条）分批写入，每批独立事务");
            Console.WriteLine();

            var totalUsers = 50;
            var batchSize = 10; // 每批10条
            var successCount = 0;
            var failCount = 0;
            var batchNumber = 0;

            for (int i = 0; i < totalUsers; i += batchSize)
            {
                batchNumber++;
                var batch = new List<TransUser>();
                for (int j = 0; j < batchSize && (i + j) < totalUsers; j++)
                {
                    var idx = i + j + 100; // 从100开始编号，避免与示例1冲突
                    batch.Add(new TransUser
                    {
                        UserName = $"批量用户{idx}",
                        Email = $"batch{idx}@example.com",
                        Age = 20 + (idx % 40),
                        IsActive = 1,
                        CreatedAt = DateTime.Now
                    });
                }

                // 每批使用独立事务
                var batchResult = InsertBatch(batch);
                if (batchResult.isSuccess)
                {
                    successCount += batch.Count;
                    Console.WriteLine($"  批次 {batchNumber}: 成功写入 {batch.Count} 条");
                }
                else
                {
                    failCount += batch.Count;
                    Console.WriteLine($"  批次 {batchNumber}: 写入失败 - {batchResult.message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"总计: 成功 {successCount} 条, 失败 {failCount} 条, 共 {batchNumber} 个批次");
            Console.WriteLine($"操作结果: {(failCount == 0 ? "全部成功" : "部分失败")}");

            Console.WriteLine();

            // 验证数据
            VerifyUserCount("示例3批量插入后");
        }

        /// <summary>
        /// 插入一批用户（单批独立事务）
        /// </summary>
        private static (bool isSuccess, string message) InsertBatch(List<TransUser> users)
        {
            using (var db = new DataContext("Sqlite"))
            {
                try
                {
                    db.BeginTrans();

                    foreach (var user in users)
                    {
                        var result = db.Add(user, isTrans: false);
                        if (!result.WriteReturn.IsSuccess)
                        {
                            db.RollbackTrans();
                            return (false, result.WriteReturn.Message);
                        }
                    }

                    db.SubmitTrans();
                    return (true, "成功");
                }
                catch (Exception ex)
                {
                    db.RollbackTrans();
                    return (false, ex.Message);
                }
            }
        }

        #endregion

        #region 示例 4: 多表事务

        /// <summary>
        /// 演示多表事务：创建用户 + 记录操作日志，在同一事务中完成
        /// </summary>
        private static void DemoMultiTableTransaction()
        {
            Console.WriteLine("=== 示例 4: 多表事务 ===");
            Console.WriteLine("场景：创建用户 + 记录操作日志，在同一个事务中完成");
            Console.WriteLine();

            // 成功场景：正常创建用户并记录日志
            Console.WriteLine("--- 场景 A: 正常流程（用户 + 日志） ---");
            var createResult = CreateUserWithLog(
                userName: "多表用户A",
                email: "multiA@example.com",
                age: 28,
                actionType: "Create",
                description: "通过多表事务创建用户");

            Console.WriteLine($"操作结果: {(createResult.isSuccess ? "成功" : "失败")}");
            Console.WriteLine($"影响行数: 用户表 +1, 日志表 +1");
            Console.WriteLine();

            // 失败场景：故意制造错误，演示两表同时回滚
            Console.WriteLine("--- 场景 B: 失败流程（两表同时回滚） ---");
            // 再次使用已存在的用户名触发唯一约束冲突
            var failResult = CreateUserWithLog(
                userName: "张三", // 已存在，会触发唯一约束冲突
                email: "fail@example.com",
                age: 50,
                actionType: "Create",
                description: "应被回滚的操作");

            Console.WriteLine($"回滚原因: {failResult.message}");
            Console.WriteLine($"操作结果: 失败（事务回滚，用户表和日志表均无变化）");
            Console.WriteLine();

            // 验证数据
            VerifyUserCount("示例4多表事务后");
            VerifyLogCount();
        }

        /// <summary>
        /// 在同一个事务中创建用户并记录日志
        /// </summary>
        private static (bool isSuccess, string message) CreateUserWithLog(
            string userName, string email, int age, string actionType, string description)
        {
            using (var db = new DataContext("Sqlite"))
            {
                try
                {
                    // 开始事务
                    db.BeginTrans();

                    // 1. 插入用户
                    var user = new TransUser
                    {
                        UserName = userName,
                        Email = email,
                        Age = age,
                        IsActive = 1,
                        CreatedAt = DateTime.Now
                    };
                    var userResult = db.Add(user, isTrans: false);
                    if (!userResult.WriteReturn.IsSuccess)
                    {
                        db.RollbackTrans();
                        return (false, $"创建用户失败: {userResult.WriteReturn.Message}");
                    }

                    // 2. 插入操作日志（关联刚创建的用户）
                    var log = new TransUserLog
                    {
                        UserId = user.Id,
                        ActionType = actionType,
                        Description = description,
                        CreatedAt = DateTime.Now
                    };
                    var logResult = db.Add(log, isTrans: false);
                    if (!logResult.WriteReturn.IsSuccess)
                    {
                        db.RollbackTrans();
                        return (false, $"记录日志失败: {logResult.WriteReturn.Message}");
                    }

                    // 3. 提交事务
                    db.SubmitTrans();

                    Console.WriteLine($"  用户[{userName}]创建成功，日志已记录");
                    return (true, "成功");
                }
                catch (Exception ex)
                {
                    db.RollbackTrans();
                    return (false, $"异常: {ex.Message}");
                }
            }
        }

        #endregion

        #region 数据验证

        /// <summary>
        /// 验证 TransUsers 表中的记录数
        /// </summary>
        private static void VerifyUserCount(string label)
        {
            try
            {
                using (var db = new DataContext("Sqlite"))
                {
                    var sql = "SELECT COUNT(*) AS Total FROM TransUsers";
                    var result = db.ExecuteSqlList(sql);
                    if (result.WriteReturn.IsSuccess && result.DicList.Count > 0)
                    {
                        var count = Convert.ToInt32(result.DicList[0]["Total"]);
                        Console.WriteLine($"  [{label}] TransUsers 表记录数: {count}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询记录数失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证 TransUserLogs 表中的记录数
        /// </summary>
        private static void VerifyLogCount()
        {
            try
            {
                using (var db = new DataContext("Sqlite"))
                {
                    var sql = "SELECT COUNT(*) AS Total FROM TransUserLogs";
                    var result = db.ExecuteSqlList(sql);
                    if (result.WriteReturn.IsSuccess && result.DicList.Count > 0)
                    {
                        var count = Convert.ToInt32(result.DicList[0]["Total"]);
                        Console.WriteLine($"  [示例4后] TransUserLogs 表记录数: {count}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询日志数失败: {ex.Message}");
            }
        }

        #endregion

        #region 实体类定义

        /// <summary>
        /// 测试用户实体
        /// </summary>
        [Table(Name = "TransUsers")]
        private class TransUser
        {
            /// <summary>
            /// 主键（自增）
            /// </summary>
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            /// <summary>
            /// 用户名（唯一）
            /// </summary>
            [Column(Length = 100, IsNull = false)]
            public string UserName { get; set; }

            /// <summary>
            /// 邮箱
            /// </summary>
            [Column(Length = 200, IsNull = false)]
            public string Email { get; set; }

            /// <summary>
            /// 年龄
            /// </summary>
            public int Age { get; set; }

            /// <summary>
            /// 是否激活（1=是, 0=否）
            /// </summary>
            public int IsActive { get; set; }

            /// <summary>
            /// 创建时间
            /// </summary>
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// 用户操作日志实体
        /// </summary>
        [Table(Name = "TransUserLogs")]
        private class TransUserLog
        {
            /// <summary>
            /// 主键（自增）
            /// </summary>
            [Primary]
            [Column(IsIdentity = true)]
            public int Id { get; set; }

            /// <summary>
            /// 关联用户ID
            /// </summary>
            public int UserId { get; set; }

            /// <summary>
            /// 操作类型
            /// </summary>
            [Column(Length = 50, IsNull = false)]
            public string ActionType { get; set; }

            /// <summary>
            /// 操作描述
            /// </summary>
            [Column(Length = 500)]
            public string Description { get; set; }

            /// <summary>
            /// 创建时间
            /// </summary>
            public DateTime CreatedAt { get; set; }
        }

        #endregion
    }
}
