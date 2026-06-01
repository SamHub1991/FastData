using FastData.Demo.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// AOP 拦截器控制器
    /// 
    /// 提供 AOP 拦截器功能的测试和信息查询接口。
    /// </summary>
    [ApiController]
    [Route("api/Aop")]
    public class AopController : ControllerBase
    {
        /// <summary>
        /// 测试 AOP 拦截器
        /// </summary>
        /// <returns>测试结果</returns>
        [HttpGet("test")]
        public IActionResult Test()
        {
            var result = new Dictionary<string, object>();

            try
            {
                var users = FastRead.Query<AppUser>(u => u.IsActive)
                    .Take(5)
                    .ToList<AppUser>();

                result["success"] = true;
                result["message"] = "AOP 拦截器测试成功";
                result["data"] = users;
                result["note"] = "AOP 拦截器需要在程序启动时配置，详见 AopExample.cs";
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }

            return Ok(result);
        }

        /// <summary>
        /// 获取 AOP 拦截器信息
        /// </summary>
        /// <returns>AOP 拦截器使用说明</returns>
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            var info = new Dictionary<string, object>
            {
                ["feature"] = "AOP 拦截器",
                ["description"] = "在 SQL 执行前后插入自定义逻辑",
                ["usage"] = new Dictionary<string, string>
                {
                    ["步骤1"] = "实现 IFastAop 接口",
                    ["步骤2"] = "在程序启动时注册: FastDataConfig.SetAop(new MyAop())",
                    ["步骤3"] = "所有 SQL 操作都会自动触发 AOP 拦截器"
                },
                ["examples"] = new[]
                {
                    "SQL 日志记录",
                    "性能监控",
                    "SQL 审计",
                    "错误处理"
                }
            };

            return Ok(info);
        }
    }
}
