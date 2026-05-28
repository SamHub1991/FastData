using FastData.Config;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 配置信息 API（只读，密码脱敏）
    /// </summary>
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        /// <summary>
        /// 获取当前活跃环境名称
        /// </summary>
        [HttpGet("environment")]
        public ActionResult<object> GetEnvironment()
        {
            var active = FastDataConfig.GetActiveEnvironment();
            var envOverride = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FASTDATA_ACTIVE"));
            return Ok(new
            {
                active = active,
                envVarOverride = envOverride
            });
        }

        /// <summary>
        /// 获取所有数据库连接配置（密码脱敏）
        /// </summary>
        [HttpGet("connections")]
        public ActionResult<object> GetConnections()
        {
            var list = FastDataConfig.GetConnectionSummaries();
            return Ok(list);
        }

        /// <summary>
        /// 获取指定数据库连接配置（密码脱敏）
        /// </summary>
        [HttpGet("connections/{key}")]
        public ActionResult<object> GetConnection(string key)
        {
            var config = FastDataConfig.GetConnectionSummary(key);
            if (config == null)
                return NotFound(new { error = $"Connection key '{key}' not found" });

            return Ok(config);
        }

        /// <summary>
        /// 获取 Redis 配置（密码脱敏）
        /// </summary>
        [HttpGet("redis")]
        public ActionResult<object> GetRedis()
        {
            var redis = FastDataConfig.GetRedisSummary();
            return Ok(redis);
        }

        /// <summary>
        /// 获取完整配置概览
        /// </summary>
        [HttpGet("summary")]
        public ActionResult<object> GetSummary()
        {
            var active = FastDataConfig.GetActiveEnvironment();
            var connections = FastDataConfig.GetConnectionSummaries();
            var redis = FastDataConfig.GetRedisSummary();

            return Ok(new
            {
                environment = new { active },
                database = new
                {
                    count = connections.Count,
                    connections
                },
                redis
            });
        }
    }
}
