using FastData.Demo.Models;
using FastUntility.Page;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 分页查询 API 示例
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaginationController : ControllerBase
    {
        /// <summary>
        /// 分页查询用户（简化API）
        /// GET /api/pagination/users?page=1&amp;pageSize=10
        /// </summary>
        [HttpGet("users")]
        public ActionResult<PaginationResult<User>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = FastRead.Query<User>(u => u.Id > 0)
                    .OrderBy<User>(u => u.Id)
                    .ToPagination<User>(page, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 分页查询用户（使用 PaginationRequest）
        /// POST /api/pagination/users/search
        /// Body: { "page": 1, "pageSize": 10 }
        /// </summary>
        [HttpPost("users/search")]
        public ActionResult<PaginationResult<User>> SearchUsers([FromBody] PaginationRequest request)
        {
            try
            {
                var result = FastRead.Query<User>(u => u.IsActive)
                    .OrderBy<User>(u => u.CreateTime)
                    .ToPagination<User>(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 分页查询用户（带条件）
        /// GET /api/pagination/users/department/{dept}?page=1&amp;pageSize=10
        /// </summary>
        [HttpGet("users/department/{department}")]
        public ActionResult<PaginationResult<User>> GetByDepartment(
            string department,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = FastRead.Query<User>(u => u.Department == department)
                    .OrderBy<User>(u => u.Id)
                    .ToPagination<User>(page, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 分页查询用户（异步版本）
        /// GET /api/pagination/users/async?page=1&amp;pageSize=10
        /// </summary>
        [HttpGet("users/async")]
        public async Task<ActionResult<PaginationResult<User>>> GetUsersAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await FastRead.Query<User>(u => u.Id > 0)
                    .OrderBy<User>(u => u.Id)
                    .ToPaginationAsync<User>(page, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 分页查询（返回字典格式）
        /// GET /api/pagination/users/dictionary?page=1&amp;pageSize=10
        /// </summary>
        [HttpGet("users/dictionary")]
        public ActionResult<PaginationResult> GetUsersDictionary([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = FastRead.Query<User>(u => u.Id > 0)
                    .OrderBy<User>(u => u.Id)
                    .ToPagination(page, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
