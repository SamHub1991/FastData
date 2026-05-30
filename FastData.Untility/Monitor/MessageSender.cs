using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                Console.WriteLine($"发送私聊消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送群消息
        /// </summary>
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
                Console.WriteLine($"发送群消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送请求到 QQ 机器人 API
        /// </summary>
        private void SendRequest(string action, object payload)
        {
            var url = $"{_config.ApiUrl.TrimEnd('/')}/{action}";
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 添加认证头
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            if (!string.IsNullOrEmpty(_config.BotToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_config.BotToken}");
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
    }

    /// <summary>
    /// 控制台消息发送器（用于测试）
    /// </summary>
    public class ConsoleMessageSender : IMessageSender
    {
        public void SendPrivateMessage(string qqNumber, string message)
        {
            Console.WriteLine($"[QQ私聊] 发送到 {qqNumber}: {message}");
        }

        public void SendGroupMessage(string groupId, string message)
        {
            Console.WriteLine($"[QQ群聊] 发送到群 {groupId}: {message}");
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
            sb.AppendLine($"[{GetLevelEmoji(exception.Level)}] 系统异常通知");
            sb.AppendLine($"时间: {exception.Timestamp:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"来源: {exception.Source ?? "未知"}");
            sb.AppendLine($"消息: {exception.Message}");

            if (_notifyConfig.SendStackTrace && !string.IsNullOrEmpty(exception.StackTrace))
            {
                var stackTrace = exception.StackTrace;
                if (stackTrace.Length > _notifyConfig.MaxStackTraceLength)
                    stackTrace = stackTrace.Substring(0, _notifyConfig.MaxStackTraceLength) + "...";

                sb.AppendLine($"堆栈: {stackTrace}");
            }

            if (exception.AdditionalData.Count > 0)
            {
                sb.AppendLine("附加信息:");
                foreach (var kvp in exception.AdditionalData)
                {
                    sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
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
