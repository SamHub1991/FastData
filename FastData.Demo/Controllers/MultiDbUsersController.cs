using FastData.Demo.Models;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 多数据库表名映射示例控制器
    /// 演示同一个实体在不同数据库中使用不同表名
    /// </summary>
    [ApiController]
    [Route("api/MultiDbUsers")]
    public class MultiDbUsersController : ControllerBase
    {
        /// <summary>
        /// 获取所有用户（使用默认数据库）
        /// </summary>
        [HttpGet]
        public IActionResult GetAll()
        {
            var users = FastRead.Query<MultiDbUser>(u => u.Id > 0).ToList();
            return Ok(new
            {
                Database = "Default",
                TableName = "根据配置自动映射",
                Count = users.Count,
                Data = users
            });
        }

        /// <summary>
        /// 获取用户（指定数据库 Key）
        /// </summary>
        [HttpGet("{dbKey}")]
        public IActionResult GetByDbKey(string dbKey)
        {
            using (FastDb.Use(dbKey))
            {
                var users = FastRead.Query<MultiDbUser>(u => u.Id > 0).ToList();
                return Ok(new
                {
                    Database = dbKey,
                    TableName = GetTableNameForDb(dbKey),
                    Count = users.Count,
                    Data = users
                });
            }
        }

        /// <summary>
        /// 获取订单（混合模式示例）
        /// </summary>
        [HttpGet("orders/{dbKey}")]
        public IActionResult GetOrders(string dbKey)
        {
            using (FastDb.Use(dbKey))
            {
                var orders = FastRead.Query<MixedDbOrder>(o => o.Id > 0).ToList();
                return Ok(new
                {
                    Database = dbKey,
                    TableName = GetOrderTableNameForDb(dbKey),
                    Count = orders.Count,
                    Data = orders
                });
            }
        }

        /// <summary>
        /// 添加用户（多数据库表名映射）
        /// </summary>
        [HttpPost]
        public IActionResult Add([FromBody] MultiDbUser user, [FromQuery] string dbKey = null)
        {
            if (!string.IsNullOrEmpty(dbKey))
            {
                using (FastDb.Use(dbKey))
                {
                    var result = FastWrite.Add(user);
                    return Ok(new
                    {
                        Success = result.IsSuccess,
                        Message = result.IsSuccess ? "添加成功" : result.Message,
                        Database = dbKey,
                        TableName = GetTableNameForDb(dbKey)
                    });
                }
            }
            else
            {
                var result = FastWrite.Add(user);
                return Ok(new
                {
                    Success = result.IsSuccess,
                    Message = result.IsSuccess ? "添加成功" : result.Message,
                    Database = "Default",
                    TableName = "根据配置自动映射"
                });
            }
        }

        /// <summary>
        /// 获取表名映射信息
        /// </summary>
        [HttpGet("mapping-info")]
        public IActionResult GetMappingInfo()
        {
            return Ok(new
            {
                MultiDbUser = new
                {
                    SqlServer = "Users",
                    MySql = "user_info",
                    PostgreSQL = "tb_users",
                    SQLite = "users"
                },
                MixedDbOrder = new
                {
                    SqlServer = "Orders",
                    MySql = "order_info",
                    Default = "default_orders"
                },
                Usage = new
                {
                    Description = "使用 FastDb.Use(dbKey) 切换数据库上下文，ORM 会自动映射到对应的表名",
                    Example = @"
// 使用 SqlServer 数据库
using (FastDb.Use(""SqlServer""))
{
    var users = FastRead.Query<MultiDbUser>(u => u.Id > 0).ToList();
    // 实际查询的表名是 ""Users""
}

// 使用 MySql 数据库
using (FastDb.Use(""MySql""))
{
    var users = FastRead.Query<MultiDbUser>(u => u.Id > 0).ToList();
    // 实际查询的表名是 ""user_info""
}"
                }
            });
        }

        private string GetTableNameForDb(string dbKey)
        {
            return dbKey?.ToLower() switch
            {
                "sqlserver" => "Users",
                "mysql" => "user_info",
                "postgresql" => "tb_users",
                "sqlite" => "users",
                _ => "未知"
            };
        }

        private string GetOrderTableNameForDb(string dbKey)
        {
            return dbKey?.ToLower() switch
            {
                "sqlserver" => "Orders",
                "mysql" => "order_info",
                _ => "default_orders"
            };
        }
    }
}
