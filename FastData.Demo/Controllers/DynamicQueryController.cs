using System;
using System.Collections.Generic;
using System.Linq;
using FastData;
using FastData.Demo.Models;
using FastData.Model;
using FastUntility.Base;
using FastUntility.Page;
using Microsoft.AspNetCore.Mvc;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 动态查询控制器
    /// 覆盖 ORM 功能：Where条件构建器/Any/All/First/Single/Contains/In/Between
    /// </summary>
    [ApiController]
    [Route("api/DynamicQuery")]
    public class DynamicQueryController : ControllerBase
    {
        /// <summary>
        /// 动态条件查询（Where 条件构建器）
        /// </summary>
        [HttpGet("search")]
        public IActionResult Search(
            [FromQuery] string name = null,
            [FromQuery] string email = null,
            [FromQuery] int? minAge = null,
            [FromQuery] int? maxAge = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string department = null,
            [FromQuery] decimal? minSalary = null,
            [FromQuery] decimal? maxSalary = null)
        {
            var where = new Where<AppUser>();

            // 姓名模糊搜索
            if (!string.IsNullOrEmpty(name))
                where.And(u => u.UserName.Contains(name));

            // 邮箱精确匹配
            if (!string.IsNullOrEmpty(email))
                where.And(u => u.Email == email);

            // 年龄范围
            if (minAge.HasValue)
                where.And(u => u.Age >= minAge.Value);
            if (maxAge.HasValue)
                where.And(u => u.Age <= maxAge.Value);

            // 状态过滤
            if (isActive.HasValue)
                where.And(u => u.IsActive == isActive.Value);

            // 部门过滤
            if (!string.IsNullOrEmpty(department))
                where.And(u => u.Department == department);

            // 薪资范围
            if (minSalary.HasValue)
                where.And(u => u.Salary >= minSalary.Value);
            if (maxSalary.HasValue)
                where.And(u => u.Salary <= maxSalary.Value);

            var users = FastRead.Query<AppUser>(u => u.Id > 0)
                .Where(where)
                .OrderBy<AppUser>(u => u.Id)
                .ToList<AppUser>();

            return Ok(ApiResponse<List<AppUser>>.Ok(users));
        }

        /// <summary>
        /// OR 条件查询
        /// </summary>
        [HttpGet("or-query")]
        public IActionResult OrQuery([FromQuery] string keyword = null)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                var allUsers = FastRead.Query<AppUser>(u => u.Id > 0).ToList<AppUser>();
                return Ok(ApiResponse<List<AppUser>>.Ok(allUsers));
            }

            var where = new Where<AppUser>();
            where.Or(u => u.UserName.Contains(keyword));
            where.Or(u => u.Email.Contains(keyword));
            where.Or(u => u.Department.Contains(keyword));

            var users = FastRead.Query<AppUser>(u => u.Id > 0)
                .Where(where)
                .ToList<AppUser>();

            return Ok(ApiResponse<List<AppUser>>.Ok(users));
        }

        /// <summary>
        /// Any 存在性判断
        /// </summary>
        [HttpGet("any")]
        public IActionResult Any([FromQuery] string department = "技术部")
        {
            // 判断是否存在该部门的用户
            var exists = FastRead.Query<AppUser>(u => u.Department == department && u.IsActive)
                .ToCount() > 0;

            return Ok(ApiResponse<bool>.Ok(exists));
        }

        /// <summary>
        /// All 全量判断
        /// </summary>
        [HttpGet("all")]
        public IActionResult All([FromQuery] decimal minSalary = 3000)
        {
            // 判断所有用户薪资是否都大于指定值
            var allAbove = FastRead.Query<AppUser>(u => u.IsActive)
                .ToList().All(u => u.Salary >= minSalary);

            return Ok(ApiResponse<bool>.Ok(allAbove));
        }

        /// <summary>
        /// First 查询
        /// </summary>
        [HttpGet("first")]
        public IActionResult First([FromQuery] string department = null)
        {
            var first = FastRead.Query<AppUser>(u => u.IsActive)
                .OrderBy<AppUser>(u => u.Id)
                .ToList<AppUser>()
                .FirstOrDefault();

            var deptFirst = !string.IsNullOrEmpty(department)
                ? FastRead.Query<AppUser>(u => u.Department == department && u.IsActive)
                    .OrderBy<AppUser>(u => u.Id)
                    .ToList<AppUser>()
                    .FirstOrDefault()
                : null;

            return Ok(ApiResponse<object>.Ok(new
            {
                FirstActive = first,
                DepartmentFirst = deptFirst
            }));
        }

        [HttpGet("single")]
        public IActionResult Single([FromQuery] int id = 1)
        {
            var user = FastRead.Query<AppUser>(u => u.Id == id)
                .ToList<AppUser>()
                .FirstOrDefault();

            return Ok(ApiResponse<AppUser>.Ok(user));
        }

        /// <summary>
        /// In 查询
        /// </summary>
        [HttpGet("in")]
        public IActionResult InQuery([FromQuery] string ids = "1,2,3")
        {
            var idList = ids.Split(',').Select(id => id.ToInt(0)).Where(id => id > 0).ToList();

            // 使用 Where 构建器模拟 In 查询
            var where = new Where<AppUser>();
            where.And(u => idList.Contains(u.Id));

            var users = FastRead.Query<AppUser>(u => u.Id > 0)
                .Where(where)
                .ToList<AppUser>();

            return Ok(ApiResponse<List<AppUser>>.Ok(users));
        }

        /// <summary>
        /// Between 范围查询
        /// </summary>
        [HttpGet("between")]
        public IActionResult Between([FromQuery] decimal minSalary = 5000, [FromQuery] decimal maxSalary = 10000)
        {
            var where = new Where<AppUser>();
            where.And(u => u.Salary >= minSalary && u.Salary <= maxSalary);

            var users = FastRead.Query<AppUser>(u => u.Id > 0)
                .Where(where)
                .OrderBy<AppUser>(u => u.Salary)
                .ToList<AppUser>();

            return Ok(ApiResponse<List<AppUser>>.Ok(users));
        }

        /// <summary>
        /// Like 模糊查询
        /// </summary>
        [HttpGet("like")]
        public IActionResult Like([FromQuery] string keyword = "张")
        {
            var where = new Where<AppUser>();
            where.And(u => u.UserName.Contains(keyword));

            var users = FastRead.Query<AppUser>(u => u.Id > 0)
                .Where(where)
                .ToList<AppUser>();

            return Ok(ApiResponse<List<AppUser>>.Ok(users));
        }

        /// <summary>
        /// 复杂动态查询（多条件组合）
        /// </summary>
        [HttpGet("complex")]
        public IActionResult ComplexQuery(
            [FromQuery] string department = null,
            [FromQuery] int? minAge = null,
            [FromQuery] int? maxAge = null,
            [FromQuery] decimal? minSalary = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string sortBy = "Id",
            [FromQuery] bool asc = true,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            var where = new Where<AppUser>();

            if (!string.IsNullOrEmpty(department))
                where.And(u => u.Department == department);
            if (minAge.HasValue)
                where.And(u => u.Age >= minAge.Value);
            if (maxAge.HasValue)
                where.And(u => u.Age <= maxAge.Value);
            if (minSalary.HasValue)
                where.And(u => u.Salary >= minSalary.Value);
            if (isActive.HasValue)
                where.And(u => u.IsActive == isActive.Value);

            var query = FastRead.Query<AppUser>(u => u.Id > 0).Where(where);

            // 动态排序
            query = sortBy.ToLower() switch
            {
                "name" => asc ? query.OrderBy(u => u.UserName) : query.OrderByDescending(u => u.UserName),
                "age" => asc ? query.OrderBy(u => u.Age) : query.OrderByDescending(u => u.Age),
                "salary" => asc ? query.OrderBy(u => u.Salary) : query.OrderByDescending(u => u.Salary),
                "createdate" => asc ? query.OrderBy(u => u.CreateTime) : query.OrderByDescending(u => u.CreateTime),
                _ => asc ? query.OrderBy(u => u.Id) : query.OrderByDescending(u => u.Id)
            };

            var result = query.ToPage<AppUser>(new PageModel { PageId = page, PageSize = size });

            return Ok(ApiResponse<object>.Ok(new
            {
                Data = result.list,
                Total = result.pModel.TotalRecord,
                Page = result.pModel.PageId,
                PageSize = result.pModel.PageSize
            }));
        }

        /// <summary>
        /// 统计查询
        /// </summary>
        [HttpGet("count")]
        public IActionResult Count([FromQuery] string department = null)
        {
            var total = FastRead.Query<AppUser>(u => u.Id > 0).ToCount();
            var active = FastRead.Query<AppUser>(u => u.IsActive).ToCount();
            var deptCount = !string.IsNullOrEmpty(department)
                ? FastRead.Query<AppUser>(u => u.Department == department).ToCount()
                : 0;

            return Ok(ApiResponse<object>.Ok(new
            {
                Total = total,
                Active = active,
                DepartmentCount = deptCount
            }));
        }
    }
}
