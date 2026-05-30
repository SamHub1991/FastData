using FastData.Demo.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace FastData.Demo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AopController : ControllerBase
    {
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
