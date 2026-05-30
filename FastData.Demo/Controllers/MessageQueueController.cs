using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace FastData.Demo.Controllers
{
    /// <summary>
    /// 消息队列 Demo
    /// 演示 FastData 的消息队列功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MessageQueueController : ControllerBase
    {
        /// <summary>
        /// 获取消息队列状态
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var result = new Dictionary<string, object>
            {
                ["feature"] = "消息队列",
                ["description"] = "RTU 削峰、多方推送",
                ["status"] = "运行中",
                ["note"] = "消息队列功能需要在程序启动时配置"
            };
            
            return Ok(result);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        [HttpPost("send")]
        public IActionResult SendMessage([FromBody] MessageRequest request)
        {
            var result = new Dictionary<string, object>();
            
            try
            {
                // 示例：发送消息到队列
                // 实际使用需要配置消息队列
                result["success"] = true;
                result["message"] = "消息已发送";
                result["data"] = new
                {
                    request.Topic,
                    request.Content,
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
        /// 获取消息队列使用说明
        /// </summary>
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            var info = new Dictionary<string, object>
            {
                ["feature"] = "消息队列",
                ["description"] = "RTU 削峰、多方推送",
                ["usage"] = new Dictionary<string, string>
                {
                    ["RTU 削峰"] = "将大量写入请求放入队列，异步处理",
                    ["多方推送"] = "将消息推送到多个消费者"
                },
                ["examples"] = new[]
                {
                    "用户注册后发送欢迎邮件",
                    "订单创建后通知库存系统",
                    "数据变更后同步到缓存"
                }
            };
            
            return Ok(info);
        }
    }

    public class MessageRequest
    {
        public string Topic { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
