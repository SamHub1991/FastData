using System;
using System.Linq;
using FastData;
using FastData.Model;
using FastData.Property;

namespace FastData.Example.Example
{
    /// <summary>
    /// 基本 CRUD 操作示例 - 可运行版本
    /// 使用 SQLite 数据库演示完整的增删改查流程
    /// </summary>
    public class BasicCrudExample
    {
        /// <summary>
        /// 数据库连接配置键
        /// </summary>
        private const string DbKey = "CrudExample";

        /// <summary>
        /// SQLite 数据库文件名
        /// </summary>
        private const string DbFileName = "crud_example.db";

        /// <summary>
        /// 演示基本 CRUD 操作
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  基本 CRUD 操作示例（SQLite）");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // 配置 SQLite 数据库连接（通过环境变量覆盖，避免配置文件缓存问题）
                var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);
                var connStr = $"Data Source={dbPath}";
                Environment.SetEnvironmentVariable($"FASTDATA_CONN_{DbKey.ToUpper()}", connStr);

                // 验证配置加载
                var config = FastMap.DbConfig(DbKey);
                Console.WriteLine($"数据库类型: {config.DbType}");
                Console.WriteLine($"提供程序: {config.ProviderName}");
                Console.WriteLine($"数据库路径: {dbPath}");
                Console.WriteLine();

                // 1. 使用 SQL 创建测试表（CodeFirst 生成的 SQL 为 SQL Server 语法，不兼容 SQLite）
                CreateTable();

                // 2. 插入数据
                InsertData();

                // 3. 查询数据
                SelectData();

                // 4. 更新数据
                UpdateData();

                // 5. 删除数据
                DeleteData();

                // 6. Upsert 操作（存在则更新，不存在则插入）
                UpsertData();

                // 7. 最终查询验证
                Console.WriteLine("【7】最终数据验证");
                Console.WriteLine("----------------------------------------");
                var finalUsers = FastRead.Query<CrudUser>(u => u.Id >= 0).ToList();
                Console.WriteLine($"当前表中共有 {finalUsers.Count} 条记录：");
                foreach (var user in finalUsers)
                {
                    Console.WriteLine($"  Id={user.Id}, UserName={user.UserName}, Email={user.Email}, Age={user.Age}");
                }
                Console.WriteLine();

                // 8. 清理测试数据
                CleanupData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // 清除此示例使用的环境变量
                Environment.SetEnvironmentVariable($"FASTDATA_CONN_{DbKey.ToUpper()}", null);
            }

            Console.WriteLine("========================================");
            Console.WriteLine("  示例执行完成");
            Console.WriteLine("========================================");
        }

        /// <summary>
        /// 创建测试表
        /// 注意：FastWrite.CodeFirst 生成的 SQL 使用 SQL Server 语法（IDENTITY、BIT 等），
        /// 不兼容 SQLite，因此使用原生 SQL 创建表
        /// </summary>
        private static void CreateTable()
        {
            Console.WriteLine("【1】创建测试表");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 先删除已存在的表（支持重复运行）
                var dropResult = FastWrite.ExecuteSql("DROP TABLE IF EXISTS CrudUsers", null, key: DbKey);
                Console.WriteLine($"  清理旧表: {(dropResult.IsSuccess ? "成功" : "成功（表不存在）")}");

                // 使用 SQLite 语法创建表
                // 注意：UserName 添加 UNIQUE 约束，以支持 Upsert 的 ON CONFLICT 语法
                var createSql = @"
                    CREATE TABLE CrudUsers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserName VARCHAR(50) NOT NULL UNIQUE,
                        Email VARCHAR(100),
                        Age INT,
                        IsActive BIT DEFAULT 1,
                        CreatedTime DATETIME
                    )";
                var result = FastWrite.ExecuteSql(createSql, null, key: DbKey);
                Console.WriteLine($"  创建表: {(result.IsSuccess ? "成功" : "失败")}");
                if (!result.IsSuccess)
                {
                    Console.WriteLine($"  错误: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  创建表异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 插入两条测试数据
        /// </summary>
        private static void InsertData()
        {
            Console.WriteLine("【2】INSERT - 插入数据");
            Console.WriteLine("----------------------------------------");
            try
            {
                var user1 = new CrudUser
                {
                    UserName = "张三",
                    Email = "zhangsan@example.com",
                    Age = 28,
                    IsActive = true,
                    CreatedTime = DateTime.Now
                };
                var result1 = FastWrite.Add(user1, key: DbKey);
                Console.WriteLine($"  插入用户 '张三': {(result1.IsSuccess ? "成功" : "失败")}");
                if (!result1.IsSuccess)
                {
                    Console.WriteLine($"  错误: {result1.Message}");
                }

                var user2 = new CrudUser
                {
                    UserName = "李四",
                    Email = "lisi@example.com",
                    Age = 35,
                    IsActive = true,
                    CreatedTime = DateTime.Now
                };
                var result2 = FastWrite.Add(user2, key: DbKey);
                Console.WriteLine($"  插入用户 '李四': {(result2.IsSuccess ? "成功" : "失败")}");
                if (!result2.IsSuccess)
                {
                    Console.WriteLine($"  错误: {result2.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  插入数据异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 查询数据并打印结果
        /// </summary>
        private static void SelectData()
        {
            Console.WriteLine("【3】SELECT - 查询数据");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 查询全部数据（使用始终为 true 的条件模拟全表查询）
                var allUsers = FastRead.Query<CrudUser>(u => u.Id >= 0).ToList();
                Console.WriteLine($"  查询全部记录: 共 {allUsers.Count} 条");
                foreach (var user in allUsers)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Email={user.Email}, Age={user.Age}");
                }

                // 按条件查询单条
                Console.WriteLine();
                var zhangsan = FastRead.Query<CrudUser>(u => u.UserName == "张三").ToItem();
                if (zhangsan != null)
                {
                    Console.WriteLine($"  按条件查询 '张三': 找到");
                    Console.WriteLine($"    Id={zhangsan.Id}, Email={zhangsan.Email}, Age={zhangsan.Age}");
                }
                else
                {
                    Console.WriteLine("  按条件查询 '张三': 未找到");
                }

                // 查询记录数
                var count = FastRead.Query<CrudUser>(u => u.Id >= 0).ToCount();
                Console.WriteLine($"  记录总数: {count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询数据异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 更新数据并打印影响结果
        /// </summary>
        private static void UpdateData()
        {
            Console.WriteLine("【4】UPDATE - 更新数据");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 先查询要更新的用户
                var user = FastRead.Query<CrudUser>(u => u.UserName == "张三").ToItem();
                if (user == null)
                {
                    Console.WriteLine("  未找到用户 '张三'，跳过更新");
                    return;
                }

                Console.WriteLine($"  更新前: Id={user.Id}, Email={user.Email}, Age={user.Age}");

                // 更新指定字段：修改邮箱和年龄
                // FastWrite.Update 按主键更新（CrudUser.Id 标记了 [Primary]）
                user.Email = "zhangsan_new@example.com";
                user.Age = 29;
                var result = FastWrite.Update(user, key: DbKey);

                Console.WriteLine($"  更新结果: {(result.IsSuccess ? "成功" : "失败")}");
                if (!result.IsSuccess)
                {
                    Console.WriteLine($"  错误: {result.Message}");
                }

                // 验证更新结果
                var updated = FastRead.Query<CrudUser>(u => u.Id == user.Id).ToItem();
                if (updated != null)
                {
                    Console.WriteLine($"  更新后: Id={updated.Id}, Email={updated.Email}, Age={updated.Age}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  更新数据异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        private static void DeleteData()
        {
            Console.WriteLine("【5】DELETE - 删除数据");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 先查询要删除的用户
                var user = FastRead.Query<CrudUser>(u => u.UserName == "李四").ToItem();
                if (user == null)
                {
                    Console.WriteLine("  未找到用户 '李四'，跳过删除");
                    return;
                }

                Console.WriteLine($"  删除前: Id={user.Id}, UserName={user.UserName}");

                // 按条件删除
                var result = FastWrite.Delete<CrudUser>(u => u.Id == user.Id, key: DbKey);

                Console.WriteLine($"  删除结果: {(result.IsSuccess ? "成功" : "失败")}");
                if (!result.IsSuccess)
                {
                    Console.WriteLine($"  错误: {result.Message}");
                }

                // 验证删除结果
                var deleted = FastRead.Query<CrudUser>(u => u.Id == user.Id).ToItem();
                Console.WriteLine($"  验证删除: {(deleted == null ? "已删除" : "仍然存在")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  删除数据异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Upsert 操作：存在则更新，不存在则插入
        /// SQLite 使用 INSERT ... ON CONFLICT 语法实现
        /// </summary>
        private static void UpsertData()
        {
            Console.WriteLine("【6】UPSERT - 存在则更新，不存在则插入");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 场景1：插入已存在的用户名（应触发更新）
                Console.WriteLine("  场景1: Upsert 已存在的用户 '张三'");
                var upsertSql1 = @"
                    INSERT INTO CrudUsers (UserName, Email, Age, IsActive, CreatedTime)
                    VALUES ('张三', 'zhangsan_upsert@example.com', 30, 1, datetime('now'))
                    ON CONFLICT(UserName) DO UPDATE SET
                        Email = excluded.Email,
                        Age = excluded.Age,
                        IsActive = excluded.IsActive";
                var result1 = FastWrite.ExecuteSql(upsertSql1, null, key: DbKey);
                Console.WriteLine($"    结果: {(result1.IsSuccess ? "成功（更新）" : "失败")}");
                if (!result1.IsSuccess)
                {
                    Console.WriteLine($"    错误: {result1.Message}");
                }

                // 场景2：插入不存在的用户名（应触发插入）
                Console.WriteLine("  场景2: Upsert 不存在的用户 '王五'");
                var upsertSql2 = @"
                    INSERT INTO CrudUsers (UserName, Email, Age, IsActive, CreatedTime)
                    VALUES ('王五', 'wangwu@example.com', 22, 1, datetime('now'))
                    ON CONFLICT(UserName) DO UPDATE SET
                        Email = excluded.Email,
                        Age = excluded.Age,
                        IsActive = excluded.IsActive";
                var result2 = FastWrite.ExecuteSql(upsertSql2, null, key: DbKey);
                Console.WriteLine($"    结果: {(result2.IsSuccess ? "成功（插入）" : "失败")}");
                if (!result2.IsSuccess)
                {
                    Console.WriteLine($"    错误: {result2.Message}");
                }

                // 验证 Upsert 结果
                Console.WriteLine("  Upsert 后数据：");
                var users = FastRead.Query<CrudUser>(u => u.Id >= 0).ToList();
                foreach (var u in users)
                {
                    Console.WriteLine($"    Id={u.Id}, UserName={u.UserName}, Email={u.Email}, Age={u.Age}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Upsert 异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        private static void CleanupData()
        {
            Console.WriteLine("【8】清理测试数据");
            Console.WriteLine("----------------------------------------");
            try
            {
                var result = FastWrite.ExecuteSql("DROP TABLE IF EXISTS CrudUsers", null, key: DbKey);
                Console.WriteLine($"  删除表: {(result.IsSuccess ? "成功" : "失败")}");

                // 删除测试数据库文件
                var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);
                if (System.IO.File.Exists(dbPath))
                {
                    System.IO.File.Delete(dbPath);
                    Console.WriteLine($"  删除数据库文件: 成功 ({DbFileName})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  清理异常: {ex.Message}");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// CRUD 示例用户实体
    /// </summary>
    [Table(Name = "CrudUsers")]
    public class CrudUser
    {
        /// <summary>
        /// 主键 ID
        /// </summary>
        [Primary]
        public long Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Column(Length = 50, IsNull = false)]
        public string UserName { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [Column(Length = 100)]
        public string Email { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [Column(Comments = "年龄")]
        public int Age { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        [Column(Comments = "是否激活")]
        public bool IsActive { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column(Comments = "创建时间")]
        public DateTime CreatedTime { get; set; }
    }
}
