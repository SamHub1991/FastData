using FastData;
using FastData.Base;
using FastData.Demo.Models;
using FastData.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// PostgreSQL 用户 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/pg-users")]
    public class PgUsersController : ControllerBase
    {
        private const string DbKey = "PostgreSql";

        /// <summary>
        /// 获取所有用户
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AppUser>>> GetAll()
        {
            try
            {
                var users = await Task.Factory.StartNew(() =>
                    FastRead.Query<AppUser>(u => u.Id > 0, key: DbKey).ToList());
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        /// <summary>
        /// 根据 ID 获取用户
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppUser>> GetById(int id)
        {
            try
            {
                var user = await Task.Factory.StartNew(() =>
                    FastRead.Query<AppUser>(u => u.Id == id, key: DbKey).ToItem());
                if (user == null || user.Id == 0)
                    return NotFound();
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] AppUser user)
        {
            try
            {
                user.CreateTime = DateTime.Now;
                user.IsActive = true;
                var result = await Task.Factory.StartNew(() =>
                    FastWrite.Add(user, key: DbKey));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        /// <summary>
        /// 根据部门获取用户
        /// </summary>
        [HttpGet("department/{department}")]
        public async Task<ActionResult<List<AppUser>>> GetByDepartment(string department)
        {
            try
            {
                var users = await Task.Factory.StartNew(() =>
                    FastRead.Query<AppUser>(u => u.Department == department, key: DbKey).ToList());
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] AppUser user)
        {
            try
            {
                user.Id = id;
                user.UpdateTime = DateTime.Now;
                var result = await Task.Factory.StartNew(() =>
                    FastWrite.Update(user, key: DbKey));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var result = await Task.Factory.StartNew(() =>
                    FastWrite.Delete<AppUser>(u => u.Id == id, key: DbKey));
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }
    }
}
