using System;
using System.Collections.Generic;
using System.Xml;
using FastUntility.Base;

namespace FastUntility.Monitor
{
    /// <summary>
    /// QQ 机器人配置
    /// </summary>
    public class QQBotConfig
    {
        /// <summary>
        /// 机器人 ID
        /// </summary>
        public string BotId { get; set; }

        /// <summary>
        /// 机器人 Token
        /// </summary>
        public string BotToken { get; set; }

        /// <summary>
        /// API 地址（如 http://127.0.0.1:5700）
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// 管理员 QQ 号列表
        /// </summary>
        public List<string> AdminQQNumbers { get; set; } = new List<string>();

        /// <summary>
        /// 通知群号列表
        /// </summary>
        public List<string> NotifyGroups { get; set; } = new List<string>();

        /// <summary>
        /// 通知个人 QQ 号列表
        /// </summary>
        public List<string> NotifyUsers { get; set; } = new List<string>();

        /// <summary>
        /// 指令前缀
        /// </summary>
        public string CommandPrefix { get; set; } = "#";

        /// <summary>
        /// 是否需要管理员权限才能执行指令
        /// </summary>
        public bool RequireAdminForCommands { get; set; } = true;
    }

    /// <summary>
    /// 异常通知配置
    /// </summary>
    public class ExceptionNotifyConfig
    {
        /// <summary>
        /// 是否启用异常通知
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 最小通知间隔（秒），防止重复通知
        /// </summary>
        public int MinNotifyIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 是否发送堆栈信息
        /// </summary>
        public bool SendStackTrace { get; set; } = true;

        /// <summary>
        /// 最大堆栈长度
        /// </summary>
        public int MaxStackTraceLength { get; set; } = 500;

        /// <summary>
        /// 只通知指定级别以上的异常
        /// </summary>
        public ExceptionLevel MinLevel { get; set; } = ExceptionLevel.Error;
    }

    /// <summary>
    /// 异常级别
    /// </summary>
    public enum ExceptionLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 异常信息
    /// </summary>
    public class ExceptionInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public ExceptionLevel Level { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 远程指令请求
    /// </summary>
    public class RemoteCommandRequest
    {
        public string Command { get; set; }
        public string[] Args { get; set; }
        public string SenderQQ { get; set; }
        public string GroupId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 远程指令响应
    /// </summary>
    public class RemoteCommandResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    /// <summary>
    /// 远程指令处理器接口
    /// </summary>
    public interface IRemoteCommandHandler
    {
        string CommandName { get; }
        string Description { get; }
        bool RequiresAdmin { get; }
        RemoteCommandResponse Execute(RemoteCommandRequest request);
    }

    /// <summary>
    /// IM 消息发送接口
    /// </summary>
    public interface IMessageSender
    {
        /// <summary>
        /// 发送私聊消息
        /// </summary>
        /// <param name="qqNumber">QQ 号</param>
        /// <param name="message">消息内容</param>
        void SendPrivateMessage(string qqNumber, string message);

        /// <summary>
        /// 发送群消息
        /// </summary>
        /// <param name="groupId">群号</param>
        /// <param name="message">消息内容</param>
        void SendGroupMessage(string groupId, string message);
    }

    /// <summary>
    /// IM 平台配置解析器
    /// </summary>
    public static class IMPlatformConfigParser
    {
        /// <summary>
        /// 从 db.config 解析 IM 平台配置
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        /// <returns>配置元组</returns>
        public static (QQBotConfig BotConfig, ExceptionNotifyConfig NotifyConfig) ParseFromConfig(string configPath = "db.config")
        {
            var botConfig = new QQBotConfig();
            var notifyConfig = new ExceptionNotifyConfig();

            if (!System.IO.File.Exists(configPath))
                return (botConfig, notifyConfig);

            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.Load(configPath);

                // 解析 QQBot 配置
                var qqBotNode = doc.SelectSingleNode("//IMPlatform/QQBot");
                if (qqBotNode != null)
                {
                    botConfig.IsEnabled = GetBoolAttribute(qqBotNode, "IsEnabled", false);
                    botConfig.BotId = GetStringAttribute(qqBotNode, "BotId", "");
                    botConfig.BotToken = GetStringAttribute(qqBotNode, "BotToken", "");
                    botConfig.ApiUrl = GetStringAttribute(qqBotNode, "ApiUrl", "http://127.0.0.1:5700");
                    botConfig.CommandPrefix = GetStringAttribute(qqBotNode, "CommandPrefix", "#");
                    botConfig.RequireAdminForCommands = GetBoolAttribute(qqBotNode, "RequireAdminForCommands", true);

                    // 解析管理员 QQ 号列表
                    var adminNode = qqBotNode.SelectSingleNode("AdminQQNumbers");
                    if (adminNode != null)
                    {
                        foreach (System.Xml.XmlNode addNode in adminNode.SelectNodes("Add"))
                        {
                            var value = GetStringAttribute(addNode, "Value", "");
                            if (!string.IsNullOrEmpty(value))
                                botConfig.AdminQQNumbers.Add(value);
                        }
                    }

                    // 解析通知群号列表
                    var groupsNode = qqBotNode.SelectSingleNode("NotifyGroups");
                    if (groupsNode != null)
                    {
                        foreach (System.Xml.XmlNode addNode in groupsNode.SelectNodes("Add"))
                        {
                            var value = GetStringAttribute(addNode, "Value", "");
                            if (!string.IsNullOrEmpty(value))
                                botConfig.NotifyGroups.Add(value);
                        }
                    }

                    // 解析通知个人 QQ 号列表
                    var usersNode = qqBotNode.SelectSingleNode("NotifyUsers");
                    if (usersNode != null)
                    {
                        foreach (System.Xml.XmlNode addNode in usersNode.SelectNodes("Add"))
                        {
                            var value = GetStringAttribute(addNode, "Value", "");
                            if (!string.IsNullOrEmpty(value))
                                botConfig.NotifyUsers.Add(value);
                        }
                    }
                }

                // 解析异常通知配置
                var notifyNode = doc.SelectSingleNode("//IMPlatform/ExceptionNotify");
                if (notifyNode != null)
                {
                    notifyConfig.IsEnabled = GetBoolAttribute(notifyNode, "IsEnabled", true);
                    notifyConfig.MinNotifyIntervalSeconds = GetIntAttribute(notifyNode, "MinNotifyIntervalSeconds", 60);
                    notifyConfig.SendStackTrace = GetBoolAttribute(notifyNode, "SendStackTrace", true);
                    notifyConfig.MaxStackTraceLength = GetIntAttribute(notifyNode, "MaxStackTraceLength", 500);
                    notifyConfig.MinLevel = GetEnumAttribute(notifyNode, "MinLevel", ExceptionLevel.Error);
                }
            }
            catch (Exception ex)
            {
                BaseLog.SaveLog($"解析 IM 平台配置失败: {ex.Message}", "MonitorConfig_Error");
            }

            return (botConfig, notifyConfig);
        }

        private static string GetStringAttribute(System.Xml.XmlNode node, string name, string defaultValue)
        {
            var attr = node.Attributes[name];
            return attr != null ? attr.Value : defaultValue;
        }

        private static bool GetBoolAttribute(System.Xml.XmlNode node, string name, bool defaultValue)
        {
            var attr = node.Attributes[name];
            if (attr != null && bool.TryParse(attr.Value, out var result))
                return result;
            return defaultValue;
        }

        private static int GetIntAttribute(System.Xml.XmlNode node, string name, int defaultValue)
        {
            var attr = node.Attributes[name];
            if (attr != null && int.TryParse(attr.Value, out var result))
                return result;
            return defaultValue;
        }

        private static T GetEnumAttribute<T>(System.Xml.XmlNode node, string name, T defaultValue) where T : struct
        {
            var attr = node.Attributes[name];
            if (attr != null && Enum.TryParse<T>(attr.Value, true, out var result))
                return result;
            return defaultValue;
        }
    }
}
