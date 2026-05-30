using System;
using FastData.Example.Model;

namespace FastData.Example.Example
{
    /// <summary>
    /// Repository 模式使用示例
    /// 场景：分层架构、依赖注入、单元测试
    /// </summary>
    public static class RepositoryExample
    {
        /// <summary>
        /// 运行所有 Repository 示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("--- Repository 模式使用示例 ---");
            Console.WriteLine();

            DemoBasicRepository();
            DemoDependencyInjection();
            DemoMultiDatabaseRepository();
            DemoUnitTesting();
        }

        /// <summary>
        /// 示例 1: 基本 Repository 使用
        /// 场景：使用 FastRepository 进行 CRUD 操作
        /// </summary>
        private static void DemoBasicRepository()
        {
            Console.WriteLine("=== 示例 1: 基本 Repository 使用 ===");
            Console.WriteLine("场景：使用 FastRepository 进行 CRUD 操作");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 1. 创建 Repository 实例
  var repo = new FastRepository();

  // 2. 使用 Map SQL 查询
  var users = repo.Query<User>(""User.GetAll"", null);
  var user = repo.Query<User>(""User.GetById"", new[] { CreateParameter(""Id"", 1) });

  // 3. 使用 Lambda 查询
  var activeUsers = repo.Query<User>(u => u.IsActive)
      .OrderBy(u => u.Id)
      .ToList();

  // 4. 分页查询
  var pageResult = repo.QueryPage<User>(
      new PageModel { PageIndex = 1, PageSize = 10 },
      ""User.GetPaged"",
      null);

  // 5. 写入操作
  var newUser = new User { UserName = ""test"", Email = ""test@example.com"" };
  var addResult = repo.Add(newUser);

  // 6. 更新操作
  repo.Update<User>(u => u.Id == 1)
      .Set(u => u.UserName, ""newname"")
      .Execute();

  // 7. 删除操作
  repo.Delete<User>(u => u.Id == 1);");
            Console.WriteLine();

            Console.WriteLine("说明：");
            Console.WriteLine("  - FastRepository 封装了 FastRead/FastWrite");
            Console.WriteLine("  - 支持 Map SQL 和 Lambda 查询");
            Console.WriteLine("  - 支持分页、批量操作");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 2: 依赖注入
        /// 场景：在 ASP.NET Core 中使用依赖注入
        /// </summary>
        private static void DemoDependencyInjection()
        {
            Console.WriteLine("=== 示例 2: 依赖注入 ===");
            Console.WriteLine("场景：在 ASP.NET Core 中使用依赖注入");
            Console.WriteLine();

            Console.WriteLine("步骤 1: 注册服务（Program.cs）");
            Console.WriteLine(@"  // 注册 FastRepository
  builder.Services.AddScoped<IFastRepository, FastRepository>();

  // 或使用工厂模式
  builder.Services.AddScoped<IFastRepositoryFactory, FastRepositoryFactory>();");
            Console.WriteLine();

            Console.WriteLine("步骤 2: 创建 UserRepository");
            Console.WriteLine(@"  public class UserRepository
  {
      private readonly IFastRepository _repo;

      public UserRepository(IFastRepository repo)
      {
          _repo = repo;
      }

      public List<User> GetActiveUsers()
      {
          return _repo.Query<User>(u => u.IsActive)
              .OrderBy(u => u.Id)
              .ToList();
      }

      public User GetById(int id)
      {
          return _repo.Query<User>(u => u.Id == id)
              .FirstOrDefault();
      }

      public WriteReturn Add(User user)
      {
          return _repo.Add(user);
      }

      public WriteReturn Update(User user)
      {
          return _repo.Update<User>(u => u.Id == user.Id)
              .Set(u => u.UserName, user.UserName)
              .Set(u => u.Email, user.Email)
              .Execute();
      }

      public WriteReturn Delete(int id)
      {
          return _repo.Delete<User>(u => u.Id == id);
      }
  }");
            Console.WriteLine();

            Console.WriteLine("步骤 3: 在 Controller 中使用");
            Console.WriteLine(@"  [ApiController]
  [Route(""api/[controller]"")]
  public class UsersController : ControllerBase
  {
      private readonly UserRepository _userRepo;

      public UsersController(UserRepository userRepo)
      {
          _userRepo = userRepo;
      }

      [HttpGet]
      public ActionResult<List<User>> Get()
      {
          var users = _userRepo.GetActiveUsers();
          return Ok(users);
      }

      [HttpGet(""{id}"")]
      public ActionResult<User> Get(int id)
      {
          var user = _userRepo.GetById(id);
          if (user == null) return NotFound();
          return Ok(user);
      }
  }");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 3: 多数据库 Repository
        /// 场景：不同数据库使用不同的 Repository
        /// </summary>
        private static void DemoMultiDatabaseRepository()
        {
            Console.WriteLine("=== 示例 3: 多数据库 Repository ===");
            Console.WriteLine("场景：不同数据库使用不同的 Repository");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 1. 创建特定数据库的 Repository
  public class SqlServerUserRepository
  {
      private readonly IFastRepository _repo;

      public SqlServerUserRepository()
      {
          _repo = new FastRepository();
          _repo.SetKey(""SqlServer""); // 指定数据库
      }

      public List<User> GetAll()
      {
          return _repo.Query<User>(u => u.Id > 0).ToList();
      }
  }

  public class MySqlUserRepository
  {
      private readonly IFastRepository _repo;

      public MySqlUserRepository()
      {
          _repo = new FastRepository();
          _repo.SetKey(""MySql""); // 指定数据库
      }

      public List<User> GetAll()
      {
          return _repo.Query<User>(u => u.Id > 0).ToList();
      }
  }

  // 2. 使用工厂创建 Repository
  public class RepositoryFactory
  {
      public UserRepository Create(string dbKey)
      {
          var repo = new FastRepository();
          repo.SetKey(dbKey);
          return new UserRepository(repo);
      }
  }

  // 3. 使用示例
  var factory = new RepositoryFactory();
  var sqlServerRepo = factory.Create(""SqlServer"");
  var mySqlRepo = factory.Create(""MySql"");

  var sqlServerUsers = sqlServerRepo.GetAll();
  var mySqlUsers = mySqlRepo.GetAll();");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 4: 单元测试
        /// 场景：为 Repository 编写单元测试
        /// </summary>
        private static void DemoUnitTesting()
        {
            Console.WriteLine("=== 示例 4: 单元测试 ===");
            Console.WriteLine("场景：为 Repository 编写单元测试");
            Console.WriteLine();

            Console.WriteLine("测试代码：");
            Console.WriteLine(@"  public class UserRepositoryTests
  {
      [Fact]
      public void GetActiveUsers_ReturnsActiveUsers()
      {
          // Arrange
          var repo = new FastRepository();
          repo.SetKey(""TestDb"");

          // Act
          var users = repo.Query<User>(u => u.IsActive).ToList();

          // Assert
          Assert.NotNull(users);
          Assert.All(users, u => Assert.True(u.IsActive));
      }

      [Fact]
      public void Add_ValidUser_ReturnsSuccess()
      {
          // Arrange
          var repo = new FastRepository();
          repo.SetKey(""TestDb"");
          var user = new User
          {
              UserName = ""testuser"",
              Email = ""test@example.com"",
              IsActive = true,
              CreateTime = DateTime.Now
          };

          // Act
          var result = repo.Add(user);

          // Assert
          Assert.True(result.IsSuccess);
      }

      [Fact]
      public void Delete_ExistingUser_ReturnsSuccess()
      {
          // Arrange
          var repo = new FastRepository();
          repo.SetKey(""TestDb"");

          // Act
          var result = repo.Delete<User>(u => u.UserName == ""testuser"");

          // Assert
          Assert.True(result.IsSuccess);
      }
  }");
            Console.WriteLine();

            Console.WriteLine("测试最佳实践：");
            Console.WriteLine("  - 使用独立的测试数据库");
            Console.WriteLine("  - 每个测试前清理数据");
            Console.WriteLine("  - 测试成功和失败场景");
            Console.WriteLine("  - 使用 Mock 对象隔离依赖");
            Console.WriteLine();
        }
    }
}
