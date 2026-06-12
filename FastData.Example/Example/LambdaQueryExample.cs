using System;
using System.Collections.Generic;
using FastData;
using FastData.Model;
using FastData.Property;

namespace FastData.Example.Example
{
    /// <summary>
    /// Lambda 查询示例 - 可运行版本
    /// 使用 SQLite 数据库演示 FastRead.Query&lt;T&gt; 的链式 API
    /// </summary>
    public class LambdaQueryExample
    {
        /// <summary>
        /// 数据库连接配置键
        /// </summary>
        private const string DbKey = "LambdaQueryExample";

        /// <summary>
        /// SQLite 数据库文件名
        /// </summary>
        private const string DbFileName = "lambda_query.db";

        /// <summary>
        /// 演示 Lambda 查询功能
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Lambda 查询示例（SQLite）");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // 配置 SQLite 数据库连接（通过环境变量覆盖）
                var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);
                var connStr = $"Data Source={dbPath}";
                Environment.SetEnvironmentVariable($"FASTDATA_CONN_{DbKey.ToUpper()}", connStr);

                Console.WriteLine($"数据库路径: {dbPath}");
                Console.WriteLine();

                // 1. 创建测试表
                CreateTable();

                // 2. 插入 10 条测试数据
                InsertTestData();

                // 3. 基础条件查询：查询所有 IsActive=true 的用户
                QueryBasicCondition();

                // 4. DataQuery 链式调用：Active 且 Age>18 且 Role=Admin 的用户
                QueryChainConditions();

                // 5. 链式 Like 条件：UserName 包含特定字符的用户
                QueryLikeConditions();

                // 6. 链式 In/Between 条件：在特定部门且年龄在特定范围的用户
                QueryInBetweenConditions();

                // 7. 分页查询：第1页，每页3条
                QueryPagination();

                // 8. 动态条件构建：根据传入参数动态构建查询条件
                QueryDynamicConditions();

                // 9. 清理测试数据和表
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
        /// 注意：FastWrite.CodeFirst 生成的 SQL 使用 SQL Server 语法，不兼容 SQLite，因此使用原生 SQL
        /// </summary>
        private static void CreateTable()
        {
            Console.WriteLine("【1】创建测试表 LambdaQueryUsers");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 先删除已存在的表（支持重复运行）
                var dropResult = FastWrite.ExecuteSql("DROP TABLE IF EXISTS LambdaQueryUsers", null, key: DbKey);
                Console.WriteLine($"  清理旧表: {(dropResult.IsSuccess ? "成功" : "成功（表不存在）")}");

                // 使用 SQLite 语法创建表
                var createSql = @"
                    CREATE TABLE LambdaQueryUsers (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserName VARCHAR(50) NOT NULL,
                        Email VARCHAR(100),
                        Age INT,
                        Department VARCHAR(50),
                        Role VARCHAR(20),
                        IsActive BIT DEFAULT 1,
                        Salary DECIMAL(10,2),
                        CreateTime DATETIME
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
        /// 插入 10 条测试数据（不同部门、年龄、角色）
        /// </summary>
        private static void InsertTestData()
        {
            Console.WriteLine("【2】插入 10 条测试数据");
            Console.WriteLine("----------------------------------------");
            try
            {
                var users = new List<LambdaQueryUser>
                {
                    new LambdaQueryUser { UserName = "张三", Email = "zhangsan@it.com", Age = 28, Department = "IT", Role = "Admin", IsActive = true, Salary = 15000, CreateTime = DateTime.Now },
                    new LambdaQueryUser { UserName = "李四", Email = "lisi@hr.com", Age = 35, Department = "HR", Role = "Manager", IsActive = true, Salary = 18000, CreateTime = DateTime.Now },
                    new LambdaQueryUser { UserName = "王五", Email = "wangwu@it.com", Age = 22, Department = "IT", Role = "Developer", IsActive = true, Salary = 12000, CreateTime = DateTime.Now },
                    new LambdaQueryUser { UserName = "赵六", Email = "zhaoliu@finance.com", Age = 40, Department = "Finance", Role = "Manager", IsActive = true, Salary = 20000, CreateTime = DateTime.Now },
                    new LambdaQueryUser { UserName = "钱七", Email = "qianqi@it.com", Age = 17, Department = "IT", Role = "Intern", IsActive = true, Salary = 5000, CreateTime = DateTime.Now },
                    new LambdaQueryUser { UserName = "孙八", Email = "sunba@hr.com", Age = 30, Department = "HR", Role = "Admin", IsActive = false, Salary = 14000, CreateTime = DateTime.Now },
                    new LambdaQueryUser { UserName = "周九", Email = "zhoujiu@finance.com", Age = 45, Department = "Finance", Role = "Director", IsActive = true, Salary = 25000, CreateTime = DateTime.Now },
                    new LambdaQueryUser { UserName = "吴十", Email = "wushi@it.com", Age = 26, Department = "IT", Role = "Developer", IsActive = true, Salary = 13000, CreateTime = DateTime.Now },
                    new LambdaQueryUser { UserName = "郑十一", Email = "zheng11@hr.com", Age = 33, Department = "HR", Role = "Developer", IsActive = false, Salary = 11000, CreateTime = DateTime.Now },
                    new LambdaQueryUser { UserName = "王小明", Email = "wangxm@it.com", Age = 29, Department = "IT", Role = "Admin", IsActive = true, Salary = 16000, CreateTime = DateTime.Now }
                };

                foreach (var user in users)
                {
                    var result = Db.Use(DbKey).Add(user);
                    Console.WriteLine($"  插入用户 '{user.UserName}': {(result.IsSuccess ? "成功" : "失败")}");
                }

                var count = Db.Use(DbKey).Count<LambdaQueryUser>();
                Console.WriteLine($"\n  当前表中共有 {count} 条记录");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  插入数据异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 基础条件查询：查询所有 IsActive=true 的用户
        /// </summary>
        private static void QueryBasicCondition()
        {
            Console.WriteLine("【3】基础条件查询 - 查询所有激活用户（IsActive=true）");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 使用推荐的短入口执行基础条件查询
                var users = Db.Use(DbKey).List<LambdaQueryUser>(u => u.IsActive);
                Console.WriteLine($"  查询条件: IsActive = true");
                Console.WriteLine($"  结果数量: {users.Count} 条");
                foreach (var user in users)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Department={user.Department}, Role={user.Role}, Salary={user.Salary}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// DataQuery 链式调用：Active 且 Age>18 且 Role=Admin 的用户
        /// </summary>
        private static void QueryChainConditions()
        {
            Console.WriteLine("【4】DataQuery 链式调用 - 激活且年龄>18且角色=Admin 的用户");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 链式调用：初始条件 + And + And（使用 DataQuery<T> 实例方法）
                var users = FastRead.Query<LambdaQueryUser>(u => u.IsActive, key: DbKey)
                    .And(u => u.Age > 18)
                    .And(u => u.Role == "Admin")
                    .OrderBy(u => u.Id)
                    .ToList();

                Console.WriteLine($"  查询条件: IsActive=true AND Age>18 AND Role='Admin'");
                Console.WriteLine($"  结果数量: {users.Count} 条");
                foreach (var user in users)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Age={user.Age}, Department={user.Department}, Role={user.Role}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 链式 Like 条件：UserName 包含特定字符的用户
        /// </summary>
        private static void QueryLikeConditions()
        {
            Console.WriteLine("【5】链式 Like 条件 - UserName 包含'张'或以'王'开头的用户");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 使用 Like 链式条件
                var users = FastRead.Query<LambdaQueryUser>(u => u.IsActive, key: DbKey)
                    .Like(u => u.UserName, "张%")
                    .OrderBy(u => u.Id)
                    .ToList();

                // 演示 Contains 用法（LIKE '%value%'）
                var containsUsers = FastRead.Query<LambdaQueryUser>(u => u.IsActive, key: DbKey)
                    .Like(u => u.Email, "%it%")
                    .ToList();

                Console.WriteLine($"  Like 查询（UserName LIKE '张%'）: {users.Count} 条");
                foreach (var user in users)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Email={user.Email}");
                }

                Console.WriteLine($"\n  Contains 查询（Email 包含 'it'）: {containsUsers.Count} 条");
                foreach (var user in containsUsers)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Email={user.Email}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 链式 In/Between 条件：在特定部门且年龄在特定范围的用户
        /// </summary>
        private static void QueryInBetweenConditions()
        {
            Console.WriteLine("【6】链式 In/Between 条件 - 特定部门且年龄在特定范围的用户");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 使用 In 和 Between 链式条件
                var departments = new List<object> { "IT", "HR" };
                var users = FastRead.Query<LambdaQueryUser>(u => u.IsActive, key: DbKey)
                    .In(u => u.Department, departments)
                    .Between(u => u.Age, 25, 40)
                    .OrderBy(u => u.Age)
                    .ToList();

                Console.WriteLine($"  查询条件: IsActive=true AND Department IN ('IT','HR') AND Age BETWEEN 25 AND 40");
                Console.WriteLine($"  结果数量: {users.Count} 条");
                foreach (var user in users)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Age={user.Age}, Department={user.Department}, Role={user.Role}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 分页查询：第1页，每页3条
        /// </summary>
        private static void QueryPagination()
        {
            Console.WriteLine("【7】分页查询 - 第1页，每页3条");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 使用 ToPagination 简化 API
                var result = FastRead.Query<LambdaQueryUser>(u => u.IsActive, key: DbKey)
                    .OrderBy(u => u.Id)
                    .ToPagination<LambdaQueryUser>(page: 1, pageSize: 3);

                Console.WriteLine($"  查询条件: IsActive=true ORDER BY Id ASC");
                Console.WriteLine($"  总记录数: {result.Total}");
                Console.WriteLine($"  总页数: {result.TotalPages}");
                Console.WriteLine($"  当前页: {result.Page}");
                Console.WriteLine($"  每页条数: {result.PageSize}");
                Console.WriteLine($"  本页数据: {result.Data.Count} 条");
                foreach (var user in result.Data)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Age={user.Age}, Department={user.Department}");
                }

                // 演示第 2 页
                Console.WriteLine();
                var result2 = FastRead.Query<LambdaQueryUser>(u => u.IsActive, key: DbKey)
                    .OrderBy(u => u.Id)
                    .ToPagination<LambdaQueryUser>(page: 2, pageSize: 3);

                Console.WriteLine($"  第 2 页数据: {result2.Data.Count} 条");
                foreach (var user in result2.Data)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Age={user.Age}, Department={user.Department}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 动态条件构建：根据传入参数动态构建查询条件
        /// </summary>
        private static void QueryDynamicConditions()
        {
            Console.WriteLine("【8】动态条件构建 - 根据参数动态构建查询");
            Console.WriteLine("----------------------------------------");
            try
            {
                // 场景 1：有部门和最小年龄参数
                Console.WriteLine("  场景1: 筛选 IT 部门且年龄 >= 25 的激活用户");
                var result1 = QueryWithParameters(key: DbKey, department: "IT", minAge: 25, keyword: null);
                Console.WriteLine($"  结果数量: {result1.Count} 条");
                foreach (var user in result1)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Age={user.Age}, Department={user.Department}");
                }

                // 场景 2：有关键词搜索
                Console.WriteLine();
                Console.WriteLine("  场景2: 搜索用户名包含'王'的激活用户");
                var result2 = QueryWithParameters(key: DbKey, department: null, minAge: 0, keyword: "王");
                Console.WriteLine($"  结果数量: {result2.Count} 条");
                foreach (var user in result2)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Age={user.Age}, Department={user.Department}");
                }

                // 场景 3：组合多个参数
                Console.WriteLine();
                Console.WriteLine("  场景3: 筛选 HR 部门、年龄 >= 30、用户名包含'李'的激活用户");
                var result3 = QueryWithParameters(key: DbKey, department: "HR", minAge: 30, keyword: "李");
                Console.WriteLine($"  结果数量: {result3.Count} 条");
                foreach (var user in result3)
                {
                    Console.WriteLine($"    Id={user.Id}, UserName={user.UserName}, Age={user.Age}, Department={user.Department}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  查询异常: {ex.Message}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 根据参数动态构建查询条件
        /// </summary>
        /// <param name="key">数据库连接键</param>
        /// <param name="department">部门筛选（为空则不筛选）</param>
        /// <param name="minAge">最小年龄（<=0 则不筛选）</param>
        /// <param name="keyword">用户名关键词（为空则不筛选）</param>
        /// <returns>符合条件的用户列表</returns>
        private static List<LambdaQueryUser> QueryWithParameters(string key, string department, int minAge, string keyword)
        {
            // 使用 Where<T> 条件构建器，可以分开写条件
            var where = new Where<LambdaQueryUser>();

            // 基础条件：始终筛选激活用户
            where.Add(u => u.IsActive);

            // 动态添加部门条件
            if (!string.IsNullOrEmpty(department))
            {
                where.And(u => u.Department == department);
            }

            // 动态添加年龄条件
            if (minAge > 0)
            {
                where.And(u => u.Age >= minAge);
            }

            // 动态添加关键词搜索条件
            if (!string.IsNullOrEmpty(keyword))
            {
                where.Like(u => u.UserName, $"%{keyword}%");
            }

            // 执行查询
            return FastRead.Query<LambdaQueryUser>(u => true, key: key)
                .Where(where)
                .OrderBy(u => u.Id)
                .ToList();
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        private static void CleanupData()
        {
            Console.WriteLine("【9】清理测试数据");
            Console.WriteLine("----------------------------------------");
            try
            {
                var result = Db.Use(DbKey).Exec("DROP TABLE IF EXISTS LambdaQueryUsers");
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
    /// Lambda 查询示例用户实体
    /// </summary>
    [Table(Name = "LambdaQueryUsers")]
    public class LambdaQueryUser
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
        public int Age { get; set; }

        /// <summary>
        /// 部门
        /// </summary>
        [Column(Length = 50)]
        public string Department { get; set; }

        /// <summary>
        /// 角色
        /// </summary>
        [Column(Length = 20)]
        public string Role { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 薪资
        /// </summary>
        public decimal Salary { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
