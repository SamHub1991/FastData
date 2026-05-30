using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 数据同步 Demo
    /// 演示 FastData 的数据同步功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DataSyncController : ControllerBase
    {
        /// <summary>
        /// 获取同步状态
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var result = new Dictionary<string, object>
            {
                ["feature"] = "数据同步",
                ["description"] = "多数据库数据同步",
                ["status"] = "就绪",
                ["supportedDatabases"] = new[] { "SqlServer", "MySql", "PostgreSql", "SQLite" }
            };
            
            return Ok(result);
        }

        /// <summary>
        /// 执行数据同步
        /// </summary>
        [HttpPost("sync")]
        public IActionResult SyncData([FromBody] DataSyncRequest request)
        {
            var result = new Dictionary<string, object>();
            
            try
            {
                // 示例：数据同步配置
                // 实际使用需要配置源数据库和目标数据库
                result["success"] = true;
                result["message"] = "数据同步任务已创建";
                result["data"] = new
                {
                    request.SourceDb,
                    request.TargetDb,
                    request.TableName,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                result["success"] = false;
                result["error"] = ex.Message;
            }
            
            return Ok(result);
        }

        /// <summary>
        /// 获取同步历史
        /// </summary>
        [HttpGet("history")]
        public IActionResult GetHistory()
        {
            var result = new Dictionary<string, object>
            {
                ["success"] = true,
                ["data"] = new[]
                {
                    new { Id = 1, SourceDb = "SqlServer", TargetDb = "MySql", Status = "成功", Time = DateTime.Now.AddHours(-2) },
                    new { Id = 2, SourceDb = "PostgreSql", TargetDb = "SQLite", Status = "成功", Time = DateTime.Now.AddHours(-1) }
                }
            };
            
            return Ok(result);
        }

        /// <summary>
        /// 获取数据同步使用说明
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            var info = new Dictionary<string, object>
            {
                ["feature"] = "数据同步",
                ["description"] = "多数据库数据同步",
                ["usage"] = new Dictionary<string, string>
                {
                    ["配置源数据库"] = "在 db.config 中配置源数据库连接",
                    ["配置目标数据库"] = "在 db.config 中配置目标数据库连接",
                    ["执行同步"] = "调用同步 API 或定时任务"
                },
                ["examples"] = new[]
                {
                    "SqlServer 同步到 MySql",
                    "PostgreSql 同步到 SQLite",
                    "定时数据备份"
                }
            };
            
            return Ok(info);
        }
    }

    public class DataSyncRequest
    {
        public string SourceDb { get; set; } = "";
        public string TargetDb { get; set; } = "";
        public string TableName { get; set; } = "";
    }
}
