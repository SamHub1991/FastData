using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
#if NET452
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif
using System.Threading.Tasks;
using FastUntility.Base;

namespace FastUntility.Monitor
{
    /// <summary>
    /// QQ 机器人消息发送器
    /// </summary>
    public class QQBotMessageSender : IMessageSender
    {
        private readonly QQBotConfig _config;
        private readonly HttpClient _httpClient;

        public QQBotMessageSender(QQBotConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="qqNumber">QQ号</param>
        /// <param name="message">消息内容</param>
        public void SendPrivateMessage(string qqNumber, string message)
        {
            if (!_config.IsEnabled)
                return;

            try
            {
                var payload = new
                {
                    user_id = qqNumber,
                    message = message
                };

                SendRequest("send_private_msg", payload);
            }
            catch (Exception ex)
            {
                BaseLog.SaveLog(string.Format("发送私聊消息失败: {0}", ex.Message), "MessageSender_Error");
            }
        }

        /// <summary>
        /// 发送群消息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="message">消息内容</param>
        public void SendGroupMessage(string groupId, string message)
        {
            if (!_config.IsEnabled)
                return;

            try
            {
                var payload = new
                {
                    group_id = groupId,
                    message = message
                };

                SendRequest("send_group_msg", payload);
            }
            catch (Exception ex)
            {
                BaseLog.SaveLog(string.Format("发送群消息失败: {0}", ex.Message), "MessageSender_Error");
            }
        }

        /// <summary>
        /// 发送请求到 QQ 机器人 API
        /// </summary>
        /// <param name="action">API动作</param>
        /// <param name="payload">请求负载</param>
        private void SendRequest(string action, object payload)
        {
            var url = string.Format("{0}/{1}", _config.ApiUrl.TrimEnd('/'), action);
            var json = JsonSerialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 添加认证头
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            if (!string.IsNullOrEmpty(_config.BotToken))
            {
                request.Headers.Add("Authorization", string.Format("Bearer {0}", _config.BotToken));
            }

            var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        /// <summary>
        /// 跨框架 JSON 序列化：net452 用 Newtonsoft，其他用 System.Text.Json
        /// </summary>
        private static string JsonSerialize(object value)
        {
#if NET452
            return JsonConvert.SerializeObject(value);
#else
            return JsonSerializer.Serialize(value);
#endif
        }
    }

    /// <summary>
    /// 控制台消息发送器（用于测试）
    /// </summary>
    public class ConsoleMessageSender : IMessageSender
    {
        public void SendPrivateMessage(string qqNumber, string message)
        {
            Console.WriteLine(string.Format("[QQ私聊] 发送到 {0}: {1}", qqNumber, message));
        }

        public void SendGroupMessage(string groupId, string message)
        {
            Console.WriteLine(string.Format("[QQ群聊] 发送到群 {0}: {1}", groupId, message));
        }
    }

    /// <summary>
    /// 异常通知器
    /// </summary>
    public class ExceptionNotifier
    {
        private readonly IMessageSender _sender;
        private readonly QQBotConfig _botConfig;
        private readonly ExceptionNotifyConfig _notifyConfig;
        private readonly Dictionary<string, DateTime> _lastNotifyTime = new Dictionary<string, DateTime>();

        public ExceptionNotifier(IMessageSender sender, QQBotConfig botConfig, ExceptionNotifyConfig notifyConfig)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _botConfig = botConfig ?? throw new ArgumentNullException(nameof(botConfig));
            _notifyConfig = notifyConfig ?? throw new ArgumentNullException(nameof(notifyConfig));
        }

        /// <summary>
        /// 发送异常通知
        /// </summary>
        public void Notify(ExceptionInfo exception)
        {
            if (!_notifyConfig.IsEnabled)
                return;

            if (exception.Level < _notifyConfig.MinLevel)
                return;

            // 检查通知间隔
            if (!CanNotify(exception))
                return;

            var message = FormatExceptionMessage(exception);

            // 发送到所有通知群
            foreach (var group in _botConfig.NotifyGroups)
            {
                _sender.SendGroupMessage(group, message);
            }

            // 发送给所有管理员
            foreach (var admin in _botConfig.AdminQQNumbers)
            {
                _sender.SendPrivateMessage(admin, message);
            }

            // 发送给所有通知个人用户
            foreach (var user in _botConfig.NotifyUsers)
            {
                _sender.SendPrivateMessage(user, message);
            }

            // 更新最后通知时间
            _lastNotifyTime[exception.Id] = DateTime.UtcNow;
        }

        /// <summary>
        /// 检查是否可以发送通知
        /// </summary>
        private bool CanNotify(ExceptionInfo exception)
        {
            if (!_lastNotifyTime.ContainsKey(exception.Id))
                return true;

            var lastTime = _lastNotifyTime[exception.Id];
            var interval = DateTime.UtcNow - lastTime;
            return interval.TotalSeconds >= _notifyConfig.MinNotifyIntervalSeconds;
        }

        /// <summary>
        /// 格式化异常消息
        /// </summary>
        private string FormatExceptionMessage(ExceptionInfo exception)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("[{0}] 系统异常通知", GetLevelEmoji(exception.Level)));
            sb.AppendLine(string.Format("时间: {0:yyyy-MM-dd HH:mm:ss}", exception.Timestamp));
            sb.AppendLine(string.Format("来源: {0}", exception.Source ?? "未知"));
            sb.AppendLine(string.Format("消息: {0}", exception.Message));

            if (_notifyConfig.SendStackTrace && !string.IsNullOrEmpty(exception.StackTrace))
            {
                var stackTrace = exception.StackTrace;
                if (stackTrace.Length > _notifyConfig.MaxStackTraceLength)
                    stackTrace = stackTrace.Substring(0, _notifyConfig.MaxStackTraceLength) + "...";

                sb.AppendLine(string.Format("堆栈: {0}", stackTrace));
            }

            if (exception.AdditionalData.Count > 0)
            {
                sb.AppendLine("附加信息:");
                foreach (var kvp in exception.AdditionalData)
                {
                    sb.AppendLine(string.Format("  {0}: {1}", kvp.Key, kvp.Value));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取级别对应的 Emoji
        /// </summary>
        private string GetLevelEmoji(ExceptionLevel level)
        {
            return level switch
            {
                ExceptionLevel.Debug => "DEBUG",
                ExceptionLevel.Info => "INFO",
                ExceptionLevel.Warning => "WARNING",
                ExceptionLevel.Error => "ERROR",
                ExceptionLevel.Critical => "CRITICAL",
                _ => "UNKNOWN"
            };
        }
    }
}
