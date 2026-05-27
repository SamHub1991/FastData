using System;
using FastData.Example.Model;
using FastUntility.Page;

namespace FastData.Example.Example
{
    /// <summary>
    /// 分页查询示例
    /// </summary>
    public class PaginationExample
    {
        /// <summary>
        /// 演示分页查询 API
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("=== Pagination Example ===");
            Console.WriteLine();

            Console.WriteLine("1. 基本分页查询（简化API）");
            Console.WriteLine("   传入 page 和 pageSize，返回 total、totalPages、data");
            Console.WriteLine();
            Console.WriteLine("   var result = FastRead.Query<User>(u => u.IsActive)");
            Console.WriteLine("       .OrderBy(u => u.Id)");
            Console.WriteLine("       .ToPagination<User>(page: 1, pageSize: 10);");
            Console.WriteLine();
            Console.WriteLine("   // 返回结果：");
            Console.WriteLine("   // {");
            Console.WriteLine("   //   \"total\": 100,");
            Console.WriteLine("   //   \"totalPages\": 10,");
            Console.WriteLine("   //   \"page\": 1,");
            Console.WriteLine("   //   \"pageSize\": 10,");
            Console.WriteLine("   //   \"hasPrevious\": false,");
            Console.WriteLine("   //   \"hasNext\": true,");
            Console.WriteLine("   //   \"data\": [...]");
            Console.WriteLine("   // }");
            Console.WriteLine();

            Console.WriteLine("2. 使用 PaginationRequest 对象");
            Console.WriteLine("   适合 Web API 接收前端参数");
            Console.WriteLine();
            Console.WriteLine("   [HttpPost]");
            Console.WriteLine("   public ActionResult<PaginationResult<User>> Search(");
            Console.WriteLine("       [FromBody] PaginationRequest request)");
            Console.WriteLine("   {");
            Console.WriteLine("       var result = FastRead.Query<User>(u => u.IsActive)");
            Console.WriteLine("           .OrderBy(u => u.CreateTime)");
            Console.WriteLine("           .ToPagination<User>(request);");
            Console.WriteLine("       return Ok(result);");
            Console.WriteLine("   }");
            Console.WriteLine();

            Console.WriteLine("3. 异步分页查询");
            Console.WriteLine("   var result = await FastRead.Query<User>(u => u.Id > 0)");
            Console.WriteLine("       .OrderBy(u => u.Id)");
            Console.WriteLine("       .ToPaginationAsync<User>(page: 1, pageSize: 20);");
            Console.WriteLine();

            Console.WriteLine("4. 带条件的分页查询");
            Console.WriteLine("   var result = FastRead.Query<User>(u => u.Department == \"IT\")");
            Console.WriteLine("       .OrderBy(u => u.Id)");
            Console.WriteLine("       .ToPagination<User>(page: 1, pageSize: 10);");
            Console.WriteLine();

            Console.WriteLine("5. 返回字典格式（适合动态字段）");
            Console.WriteLine("   var result = FastRead.Query<User>(u => u.Id > 0)");
            Console.WriteLine("       .OrderBy(u => u.Id)");
            Console.WriteLine("       .ToPagination(page: 1, pageSize: 10);");
            Console.WriteLine("   // 返回 PaginationResult（非泛型），Data 为 List<Dictionary<string, object>>");
            Console.WriteLine();

            Console.WriteLine("6. Web API 控制器完整示例");
            Console.WriteLine();
            Console.WriteLine("   [ApiController]");
            Console.WriteLine("   [Route(\"api/[controller]\")]");
            Console.WriteLine("   public class UsersController : ControllerBase");
            Console.WriteLine("   {");
            Console.WriteLine("       [HttpGet]");
            Console.WriteLine("       public ActionResult<PaginationResult<User>> Get(");
            Console.WriteLine("           [FromQuery] int page = 1,");
            Console.WriteLine("           [FromQuery] int pageSize = 10)");
            Console.WriteLine("       {");
            Console.WriteLine("           var result = FastRead.Query<User>(u => u.IsActive)");
            Console.WriteLine("               .OrderBy(u => u.Id)");
            Console.WriteLine("               .ToPagination<User>(page, pageSize);");
            Console.WriteLine("           return Ok(result);");
            Console.WriteLine("       }");
            Console.WriteLine("   }");
            Console.WriteLine();

            Console.WriteLine("PaginationResult 返回字段说明：");
            Console.WriteLine("  - Total      : 总记录数");
            Console.WriteLine("  - TotalPages : 总页数（自动计算）");
            Console.WriteLine("  - Page       : 当前页码");
            Console.WriteLine("  - PageSize   : 每页条数");
            Console.WriteLine("  - HasPrevious: 是否有上一页");
            Console.WriteLine("  - HasNext    : 是否有下一页");
            Console.WriteLine("  - Data       : 数据列表");
            Console.WriteLine();

            Console.WriteLine("提示：运行实际示例需要配置数据库连接，请参考 README.md");
            Console.WriteLine();
        }
    }
}
