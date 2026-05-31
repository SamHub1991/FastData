using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FastData;
using FastData.Model;
using FastData.Example.Model;
using FastRedis;
using FastUntility.Page;

namespace FastData.Example.Example
{
    /// <summary>
    /// 用户管理业务示例
    /// 演示完整的用户管理业务流程：注册、登录、查询、更新、删除等
    /// </summary>
    public static class UserManagementExample
    {
        /// <summary>
        /// 运行所有用户管理示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  用户管理业务示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // 1. 用户注册
                Console.WriteLine("【1】用户注册");
                Console.WriteLine("----------------------------------------");
                var registerResult = UserRegistration("zhangsan", "zhangsan@example.com", "13800138000", "Password123!");
                Console.WriteLine($"注册结果: {(registerResult.IsSuccess ? "成功" : "失败")}");
                if (!registerResult.IsSuccess)
                    Console.WriteLine($"失败原因: {registerResult.Message}");
                Console.WriteLine();

                // 2. 用户登录
                Console.WriteLine("【2】用户登录");
                Console.WriteLine("----------------------------------------");
                var loginResult = UserLogin("zhangsan", "Password123!");
                if (loginResult != null)
                {
                    Console.WriteLine($"登录成功: {loginResult.UserName}");
                    Console.WriteLine($"邮箱: {loginResult.Email}");
                    Console.WriteLine($"最后登录时间: {loginResult.LastLoginTime}");
                }
                else
                {
                    Console.WriteLine("登录失败: 用户名或密码错误");
                }
                Console.WriteLine();

                // 3. 获取用户资料（带缓存）
                Console.WriteLine("【3】获取用户资料（带缓存）");
                Console.WriteLine("----------------------------------------");
                var profile = GetUserProfile(1);
                if (profile != null)
                {
                    Console.WriteLine($"用户ID: {profile.Id}");
                    Console.WriteLine($"用户名: {profile.UserName}");
                    Console.WriteLine($"邮箱: {profile.Email}");
                    Console.WriteLine($"年龄: {profile.Age}");
                    Console.WriteLine($"状态: {(profile.IsActive ? "正常" : "禁用")}");
                }
                Console.WriteLine();

                // 4. 更新用户资料
                Console.WriteLine("【4】更新用户资料（部分更新）");
                Console.WriteLine("----------------------------------------");
                var updateResult = UpdateUserProfile(1, email: "newemail@example.com", phone: "13900139000");
                Console.WriteLine($"更新结果: {(updateResult.IsSuccess ? "成功" : "失败")}");
                Console.WriteLine();

                // 5. 搜索用户（分页）
                Console.WriteLine("【5】搜索用户（分页查询）");
                Console.WriteLine("----------------------------------------");
                var searchResult = SearchUsers(
                    userName: "zhang",
                    email: null,
                    isActive: true,
                    minAge: 18,
                    maxAge: 60,
                    pageIndex: 1,
                    pageSize: 10
                );
                Console.WriteLine($"总记录数: {searchResult.pModel.TotalRecord}");
                Console.WriteLine($"总页数: {searchResult.pModel.TotalPage}");
                Console.WriteLine($"当前页: {searchResult.pModel.PageId}");
                foreach (var user in searchResult.list)
                {
                    Console.WriteLine($"  - {user.UserName} ({user.Email})");
                }
                Console.WriteLine();

                // 6. 批量禁用用户
                Console.WriteLine("【6】批量禁用用户");
                Console.WriteLine("----------------------------------------");
                var userIdsToDisable = new List<int> { 2, 3, 4, 5 };
                var batchResult = BatchDisableUsers(userIdsToDisable);
                Console.WriteLine($"批量禁用结果: {(batchResult.IsSuccess ? "成功" : "失败")}");
                Console.WriteLine($"批量禁用完成");
                Console.WriteLine();

                // 7. 软删除用户
                Console.WriteLine("【7】软删除用户");
                Console.WriteLine("----------------------------------------");
                var deleteResult = SoftDeleteUser(10);
                Console.WriteLine($"软删除结果: {(deleteResult.IsSuccess ? "成功" : "失败")}");
                Console.WriteLine();

                Console.WriteLine("========================================");
                Console.WriteLine("  所有示例执行完成");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 用户注册
        /// 功能：验证输入、检查用户名唯一性、创建用户、记录操作日志
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="email">邮箱</param>
        /// <param name="phone">手机号</param>
        /// <param name="password">密码</param>
        /// <returns>操作结果</returns>
        public static WriteReturn UserRegistration(string userName, string email, string phone, string password)
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(userName))
                return new WriteReturn { IsSuccess = false, Message = "用户名不能为空" };

            if (string.IsNullOrWhiteSpace(email))
                return new WriteReturn { IsSuccess = false, Message = "邮箱不能为空" };

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return new WriteReturn { IsSuccess = false, Message = "密码长度不能少于6位" };

            try
            {
                // 检查用户名是否已存在
                var existingUser = FastRead.Query<User>(u => u.UserName == userName).ToItem();
                if (existingUser != null)
                {
                    Console.WriteLine($"用户名 '{userName}' 已存在");
                    return new WriteReturn { IsSuccess = false, Message = "用户名已存在" };
                }

                // 检查邮箱是否已存在
                var existingEmail = FastRead.Query<User>(u => u.Email == email).ToItem();
                if (existingEmail != null)
                {
                    Console.WriteLine($"邮箱 '{email}' 已被注册");
                    return new WriteReturn { IsSuccess = false, Message = "邮箱已被注册" };
                }

                // 创建新用户
                var newUser = new User
                {
                    UserName = userName,
                    Email = email,
                    Phone = phone,
                    PasswordHash = HashPassword(password),
                    Role = "User",
                    IsActive = true,
                    IsDeleted = false,
                    CreateTime = DateTime.Now
                };

                // 写入数据库
                var result = FastWrite.Add(newUser);
                if (result.IsSuccess)
                {
                    Console.WriteLine($"用户 '{userName}' 注册成功，ID: {newUser.Id}");

                    // 记录操作日志
                    LogOperation("admin", "Register", $"新用户注册: {userName}", "UserManagement");
                }
                else
                {
                    Console.WriteLine($"注册失败: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注册异常: {ex.Message}");
                return new WriteReturn { IsSuccess = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 用户登录（带缓存）
        /// 功能：验证用户名密码、更新最后登录时间、返回用户信息（不含密码）
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>用户信息（不含密码），登录失败返回 null</returns>
        public static User UserLogin(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("用户名和密码不能为空");
                return null;
            }

            try
            {
                // 从数据库查询用户
                var user = FastRead.Query<User>(u => u.UserName == userName && !u.IsDeleted).ToItem();
                if (user == null)
                {
                    Console.WriteLine($"用户 '{userName}' 不存在");
                    return null;
                }

                // 验证密码
                if (user.PasswordHash != HashPassword(password))
                {
                    Console.WriteLine("密码错误");
                    return null;
                }

                // 检查用户状态
                if (!user.IsActive)
                {
                    Console.WriteLine("账号已被禁用，请联系管理员");
                    return null;
                }

                // 更新最后登录时间
                var updateUser = new User { Id = user.Id, LastLoginTime = DateTime.Now };
                var updateResult = FastWrite.Update(updateUser, u => u.Id == user.Id);

                if (updateResult.IsSuccess)
                {
                    Console.WriteLine($"用户 '{userName}' 登录成功");
                    user.LastLoginTime = DateTime.Now;

                    // 记录登录日志
                    LogOperation(userName, "Login", "用户登录成功", "UserManagement");

                    // 缓存用户信息（5分钟）
                    var cacheKey = $"user:profile:{user.Id}";
                    RedisInfo.Set(cacheKey, user, 300);
                }

                // 返回用户信息（清除密码哈希）
                user.PasswordHash = null;
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"登录异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取用户资料（带缓存）
        /// 功能：先查缓存，缓存未命中查询数据库，设置5分钟缓存
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户信息</returns>
        public static User GetUserProfile(int userId)
        {
            try
            {
                var cacheKey = $"user:profile:{userId}";

                // 1. 先查缓存
                var cachedUser = RedisInfo.Get<User>(cacheKey);
                if (cachedUser != null)
                {
                    Console.WriteLine($"从缓存获取用户资料: {cachedUser.UserName}");
                    cachedUser.PasswordHash = null; // 清除密码
                    return cachedUser;
                }

                // 2. 缓存未命中，查询数据库
                Console.WriteLine($"缓存未命中，从数据库查询用户: {userId}");
                var user = FastRead.Query<User>(u => u.Id == userId && !u.IsDeleted).ToItem();

                if (user != null)
                {
                    // 3. 写入缓存，5分钟过期
                    RedisInfo.Set(cacheKey, user, 300);
                    Console.WriteLine($"用户资料已缓存: {user.UserName}");

                    // 返回时清除密码
                    user.PasswordHash = null;
                }
                else
                {
                    Console.WriteLine($"用户 {userId} 不存在");
                }

                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取用户资料异常: {ex.Message}");
                // 降级到直接查询数据库
                var user = FastRead.Query<User>(u => u.Id == userId && !u.IsDeleted).ToItem();
                if (user != null) user.PasswordHash = null;
                return user;
            }
        }

        /// <summary>
        /// 更新用户资料（部分更新）
        /// 功能：只更新传入的字段，清除用户缓存，记录操作日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="userName">用户名（可选）</param>
        /// <param name="email">邮箱（可选）</param>
        /// <param name="phone">手机号（可选）</param>
        /// <param name="age">年龄（可选）</param>
        /// <returns>操作结果</returns>
        public static WriteReturn UpdateUserProfile(int userId, string userName = null, string email = null, 
            string phone = null, int? age = null)
        {
            try
            {
                // 检查用户是否存在
                var user = FastRead.Query<User>(u => u.Id == userId).ToItem();
                if (user == null)
                    return new WriteReturn { IsSuccess = false, Message = "用户不存在" };

                // 构建动态更新
                var updateUser = new User { Id = userId, UpdateTime = DateTime.Now };

                // 只更新传入的字段
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    // 检查新用户名是否已存在
                    var exists = FastRead.Query<User>(u => u.UserName == userName && u.Id != userId).ToItem();
                    if (exists != null)
                        return new WriteReturn { IsSuccess = false, Message = "用户名已存在" };
                    updateUser.UserName = userName;
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    // 检查新邮箱是否已存在
                    var exists = FastRead.Query<User>(u => u.Email == email && u.Id != userId).ToItem();
                    if (exists != null)
                        return new WriteReturn { IsSuccess = false, Message = "邮箱已被使用" };
                    updateUser.Email = email;
                }

                if (!string.IsNullOrWhiteSpace(phone))
                    updateUser.Phone = phone;

                if (age.HasValue)
                    updateUser.Age = age.Value;

                // 执行更新
                var result = FastWrite.Update(updateUser, u => u.Id == userId);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"用户 {userId} 资料更新成功");

                    // 清除用户缓存
                    var cacheKey = $"user:profile:{userId}";
                    RedisInfo.Remove(cacheKey);
                    Console.WriteLine($"已清除用户缓存: {cacheKey}");

                    // 记录操作日志
                    var changes = new List<string>();
                    if (!string.IsNullOrWhiteSpace(userName)) changes.Add($"用户名={userName}");
                    if (!string.IsNullOrWhiteSpace(email)) changes.Add($"邮箱={email}");
                    if (!string.IsNullOrWhiteSpace(phone)) changes.Add($"手机={phone}");
                    if (age.HasValue) changes.Add($"年龄={age}");

                    LogOperation("admin", "Update", $"更新用户 {userId}: {string.Join(", ", changes)}", "UserManagement");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新用户资料异常: {ex.Message}");
                return new WriteReturn { IsSuccess = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 搜索用户（分页查询）
        /// 功能：支持按用户名、邮箱模糊搜索，按状态、年龄范围筛选
        /// </summary>
        /// <param name="userName">用户名（模糊匹配）</param>
        /// <param name="email">邮箱（模糊匹配）</param>
        /// <param name="isActive">是否激活</param>
        /// <param name="minAge">最小年龄</param>
        /// <param name="maxAge">最大年龄</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页条数</param>
        /// <returns>分页结果</returns>
        public static PageResult<User> SearchUsers(string userName = null, string email = null, 
            bool? isActive = null, int? minAge = null, int? maxAge = null, 
            int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                // 构建动态查询条件
                var query = FastRead.Query<User>(u => !u.IsDeleted);

                // 用户名模糊搜索
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    query = query.Like(u => u.UserName, $"%{userName}%");
                }

                // 邮箱模糊搜索
                if (!string.IsNullOrWhiteSpace(email))
                {
                    query = query.Like(u => u.Email, $"%{email}%");
                }

                // 状态筛选
                if (isActive.HasValue)
                {
                    query = query.And(u => u.IsActive == isActive.Value);
                }

                // 年龄范围筛选
                if (minAge.HasValue)
                {
                    query = query.And(u => u.Age >= minAge.Value);
                }
                if (maxAge.HasValue)
                {
                    query = query.And(u => u.Age <= maxAge.Value);
                }

                // 按创建时间倒序排列，执行分页查询
                var pageModel = new PageModel { PageId = pageIndex, PageSize = pageSize };
                var result = query
                    .OrderByDescending(u => u.CreateTime)
                    .ToPage<User>(pageModel);

                Console.WriteLine($"搜索完成，共 {result.pModel.TotalRecord} 条记录");

                // 清除密码哈希
                if (result.list != null)
                {
                    foreach (var user in result.list)
                    {
                        user.PasswordHash = null;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索用户异常: {ex.Message}");
                return new PageResult<User>
                {
                    pModel = new PageModel { TotalRecord = 0, PageId = pageIndex, PageSize = pageSize },
                    list = new List<User>()
                };
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// 功能：批量更新用户状态为禁用，清除相关缓存
        /// </summary>
        /// <param name="userIds">要禁用的用户ID列表</param>
        /// <returns>操作结果</returns>
        public static WriteReturn BatchDisableUsers(List<int> userIds)
        {
            if (userIds == null || userIds.Count == 0)
                return new WriteReturn { IsSuccess = false, Message = "用户ID列表不能为空" };

            try
            {
                Console.WriteLine($"准备禁用 {userIds.Count} 个用户...");

                // 批量更新用户状态
                var batchUpdate = new User { IsActive = false, UpdateTime = DateTime.Now };
                var result = FastWrite.Update(batchUpdate, u => userIds.Contains(u.Id));

                if (result.IsSuccess)
                {
                    Console.WriteLine($"成功禁用用户");

                    // 清除所有相关用户的缓存
                    foreach (var userId in userIds)
                    {
                        var cacheKey = $"user:profile:{userId}";
                        RedisInfo.Remove(cacheKey);
                    }
                    Console.WriteLine($"已清除 {userIds.Count} 个用户的缓存");

                    // 记录操作日志
                    LogOperation("admin", "BatchDisable", 
                        $"批量禁用用户: {string.Join(", ", userIds)}", "UserManagement");
                }
                else
                {
                    Console.WriteLine($"批量禁用失败: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"批量禁用异常: {ex.Message}");
                return new WriteReturn { IsSuccess = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 软删除用户
        /// 功能：设置 IsDeleted 标记而非物理删除，清除缓存
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>操作结果</returns>
        public static WriteReturn SoftDeleteUser(int userId)
        {
            try
            {
                // 检查用户是否存在
                var user = FastRead.Query<User>(u => u.Id == userId).ToItem();
                if (user == null)
                    return new WriteReturn { IsSuccess = false, Message = "用户不存在" };

                if (user.IsDeleted)
                    return new WriteReturn { IsSuccess = false, Message = "用户已被删除" };

                // 执行软删除
                var deleteUpdate = new User { Id = userId, IsDeleted = true, IsActive = false, UpdateTime = DateTime.Now };
                var result = FastWrite.Update(deleteUpdate, u => u.Id == userId);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"用户 {userId} ({user.UserName}) 已软删除");

                    // 清除用户缓存
                    var cacheKey = $"user:profile:{userId}";
                    RedisInfo.Remove(cacheKey);
                    Console.WriteLine($"已清除用户缓存: {cacheKey}");

                    // 记录操作日志
                    LogOperation("admin", "SoftDelete", 
                        $"软删除用户: {userId} ({user.UserName})", "UserManagement");
                }
                else
                {
                    Console.WriteLine($"软删除失败: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"软删除异常: {ex.Message}");
                return new WriteReturn { IsSuccess = false, Message = ex.Message };
            }
        }

        #region 辅助方法

        /// <summary>
        /// 密码哈希（演示用简单哈希，生产环境建议使用 BCrypt）
        /// </summary>
        /// <param name="password">原始密码</param>
        /// <returns>哈希后的密码</returns>
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "FastDataSalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        /// <param name="operatorName">操作人</param>
        /// <param name="operationType">操作类型</param>
        /// <param name="description">操作描述</param>
        /// <param name="module">所属模块</param>
        private static void LogOperation(string operatorName, string operationType, string description, string module)
        {
            try
            {
                var log = new OperationLog
                {
                    OperatorName = operatorName,
                    OperationType = operationType,
                    Description = description,
                    Module = module,
                    CreateTime = DateTime.Now
                };

                var result = FastWrite.Add(log);
                if (result.IsSuccess)
                {
                    Console.WriteLine($"[日志] {operationType}: {description}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"记录日志失败: {ex.Message}");
            }
        }

        #endregion
    }
}
