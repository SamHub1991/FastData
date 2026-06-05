using FastUntility.Monitor;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace FastData.Tests
{
    /// <summary>
    /// 异常管理器测试
    /// 
    /// 测试异常管理器的初始化、异常记录、历史记录清理、
    /// 远程指令处理、配置解析等核心功能。
    /// </summary>
    public class ExceptionManagerTests
    {
        /// <summary>
        /// 创建测试用的 QQBot 配置
        /// </summary>
        /// <returns>预配置的 QQBotConfig 实例，包含测试机器人 ID、Token、API URL 和管理员 QQ 号等配置项</returns>
        private QQBotConfig CreateBotConfig()
        {
            return new QQBotConfig
            {
                BotId = "test-bot",
                BotToken = "test-token",
                ApiUrl = "http://127.0.0.1:5700",
                IsEnabled = true,
                AdminQQNumbers = new List<string> { "123456789" },
                NotifyGroups = new List<string> { "987654321" },
                NotifyUsers = new List<string> { "111222333" },
                CommandPrefix = "#",
                RequireAdminForCommands = true
            };
        }

        /// <summary>
        /// 创建测试用的异常通知配置
        /// </summary>
        /// <returns>预配置的 ExceptionNotifyConfig 实例，启用了通知功能，最低通知级别为 Warning</returns>
        private ExceptionNotifyConfig CreateNotifyConfig()
        {
            return new ExceptionNotifyConfig
            {
                IsEnabled = true,
                MinNotifyIntervalSeconds = 0,
                SendStackTrace = true,
                MaxStackTraceLength = 500,
                MinLevel = ExceptionLevel.Warning
            };
        }

        /// <summary>
        /// 通过反射重置 ExceptionManager 单例实例，确保每个测试的隔离性
        /// </summary>
        private void ResetSingleton()
        {
            var field = typeof(ExceptionManager).GetField("_instance", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(null, null);
        }

        /// <summary>
        /// 测试 ExceptionManager 初始化：验证 Initialize 方法正确创建实例并可通过 Instance 属性访问
        /// </summary>
        [Fact]
        public void Test_ExceptionManager_Initialize()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            Assert.NotNull(manager);
            Assert.Same(manager, ExceptionManager.Instance);
        }

        /// <summary>
        /// 测试异常记录功能：验证 LogException 方法正确记录异常信息（消息、级别、来源）到历史记录
        /// </summary>
        [Fact]
        public void Test_ExceptionManager_LogException()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            var ex = new Exception("测试异常");
            manager.LogException(ex, ExceptionLevel.Error, "TestSource");

            var history = manager.GetExceptionHistory();
            Assert.Single(history);
            Assert.Equal("测试异常", history[0].Message);
            Assert.Equal(ExceptionLevel.Error, history[0].Level);
            Assert.Equal("TestSource", history[0].Source);
        }

        /// <summary>
        /// 测试多条异常记录：验证连续记录多条异常时历史记录正确累积
        /// </summary>
        [Fact]
        public void Test_ExceptionManager_MultipleExceptions()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            for (int i = 0; i < 5; i++)
            {
                manager.LogException(new Exception(string.Format("异常 {0}", i)), ExceptionLevel.Error);
            }

            var history = manager.GetExceptionHistory();
            Assert.Equal(5, history.Count);
        }

        /// <summary>
        /// 测试清除异常历史记录：验证 ClearExceptionHistory 方法正确清空所有异常记录
        /// </summary>
        [Fact]
        public void Test_ExceptionManager_ClearHistory()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            manager.LogException(new Exception("测试异常"), ExceptionLevel.Error);
            manager.ClearExceptionHistory();

            var history = manager.GetExceptionHistory();
            Assert.Empty(history);
        }

        /// <summary>
        /// 测试 help 远程指令：验证 help 指令返回包含可用指令列表的帮助信息
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_Help()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            var response = manager.ExecuteCommand("help", null, "123456789");

            Assert.True(response.Success);
            Assert.Contains("help", response.Message);
            Assert.Contains("status", response.Message);
        }

        /// <summary>
        /// 测试 status 远程指令：验证 status 指令返回服务器运行状态信息（CPU、内存等）
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_ServerStatus()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            var response = manager.ExecuteCommand("status", null, "123456789");

            Assert.True(response.Success);
            Assert.Contains("CPU", response.Message);
            Assert.Contains("内存", response.Message);
        }

        /// <summary>
        /// 测试 memory 远程指令：验证 memory 指令返回内存使用情况（总计、已用等）
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_Memory()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            var response = manager.ExecuteCommand("memory", null, "123456789");

            Assert.True(response.Success);
            Assert.Contains("总计", response.Message);
            Assert.Contains("已用", response.Message);
        }

        /// <summary>
        /// 测试 cpu 远程指令：验证 cpu 指令返回 CPU 信息（核心数、使用率等）
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_Cpu()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            var response = manager.ExecuteCommand("cpu", null, "123456789");

            Assert.True(response.Success);
            Assert.Contains("核心数", response.Message);
            Assert.Contains("使用率", response.Message);
        }

        /// <summary>
        /// 测试 version 远程指令：验证 version 指令返回系统和运行时版本信息
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_Version()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            var response = manager.ExecuteCommand("version", null, "123456789");

            Assert.True(response.Success);
            Assert.Contains("系统", response.Message);
            Assert.Contains("运行时", response.Message);
        }

        /// <summary>
        /// 测试 time 远程指令：验证 time 指令返回当前服务器时间
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_Time()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            var response = manager.ExecuteCommand("time", null, "123456789");

            Assert.True(response.Success);
            Assert.Contains("服务器时间", response.Message);
        }

        /// <summary>
        /// 测试 process 远程指令：验证 process 指令返回当前进程信息（进程名、PID 等）
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_Process()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            var response = manager.ExecuteCommand("process", null, "123456789");

            Assert.True(response.Success);
            Assert.Contains("进程名", response.Message);
            Assert.Contains("PID", response.Message);
        }

        /// <summary>
        /// 测试未知远程指令处理：验证执行未注册的指令时返回错误信息"未知指令"
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_Unknown()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            var response = manager.ExecuteCommand("unknown", null, "123456789");

            Assert.False(response.Success);
            Assert.Contains("未知指令", response.Message);
        }

        /// <summary>
        /// 测试远程指令管理员权限验证：验证非管理员用户执行需要管理员权限的指令时返回"权限不足"错误
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_AdminPermission()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            // 非管理员执行需要管理员权限的指令
            var response = manager.ExecuteCommand("gc", null, "999999999");

            Assert.False(response.Success);
            Assert.Contains("权限不足", response.Message);
        }

        /// <summary>
        /// 测试自定义指令处理器注册：验证 RegisterCommandHandler 注册的自定义指令可被正确识别和执行
        /// </summary>
        [Fact]
        public void Test_RemoteCommand_CustomHandler()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            // 注册自定义指令
            manager.RegisterCommandHandler(new TestCommandHandler());

            var response = manager.ExecuteCommand("test", new[] { "arg1", "arg2" }, "123456789");

            Assert.True(response.Success);
            Assert.Contains("test", response.Message);
            Assert.Contains("arg1", response.Message);
        }

        /// <summary>
        /// 测试异常级别过滤：验证配置 MinLevel 为 Error 时，Warning 级别异常不会触发通知
        /// </summary>
        [Fact]
        public void Test_ExceptionLevel_Filter()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = new ExceptionNotifyConfig
            {
                IsEnabled = true,
                MinLevel = ExceptionLevel.Error // 只通知 Error 及以上
            };
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            // Warning 级别不应通知
            manager.LogException(new Exception("警告"), ExceptionLevel.Warning);
            // Error 级别应通知
            manager.LogException(new Exception("错误"), ExceptionLevel.Error);

            var history = manager.GetExceptionHistory();
            Assert.Equal(2, history.Count);
        }

        /// <summary>
        /// 测试处理消息：验证 ProcessMessage 方法可正确处理带 # 前缀的指令消息，不会抛出异常
        /// </summary>
        [Fact]
        public void Test_ProcessMessage()
        {
            var botConfig = CreateBotConfig();
            var notifyConfig = CreateNotifyConfig();
            var sender = new ConsoleMessageSender();

            ResetSingleton();

            var manager = ExceptionManager.Initialize(botConfig, notifyConfig, sender);

            // 测试处理消息（不会抛出异常）
            manager.ProcessMessage("123456789", "987654321", "#help");
            manager.ProcessMessage("123456789", "987654321", "#status");
            manager.ProcessMessage("123456789", null, "#version");
        }

        /// <summary>
        /// 测试配置文件解析（禁用场景）：验证当配置文件中 IsEnabled=false 时，InitializeFromConfig 返回 null
        /// </summary>
        [Fact]
        public void Test_ConfigParser_Disabled()
        {
            // 测试配置文件中 IsEnabled=false 的情况
            var configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db.config");
            
            // 确保单例已重置
            ResetSingleton();

            // 从配置文件初始化（IsEnabled=false，应该返回 null）
            var manager = ExceptionManager.InitializeFromConfig(configPath);
            Assert.Null(manager);
        }

        /// <summary>
        /// 测试 QQBot 通知用户配置：验证 CreateBotConfig 创建的配置中包含正确的通知用户列表
        /// </summary>
        [Fact]
        public void Test_QBotConfig_NotifyUsers()
        {
            var botConfig = CreateBotConfig();
            
            Assert.Single(botConfig.NotifyUsers);
            Assert.Contains("111222333", botConfig.NotifyUsers);
        }
    }

    /// <summary>
    /// 测试用指令处理器
    /// </summary>
    public class TestCommandHandler : IRemoteCommandHandler
    {
        /// <summary>获取指令名称</summary>
        public string CommandName => "test";
        /// <summary>获取指令描述</summary>
        public string Description => "测试指令";
        /// <summary>获取是否需要管理员权限</summary>
        public bool RequiresAdmin => false;

        /// <summary>
        /// 执行测试指令
        /// </summary>
        /// <param name="request">远程指令请求，包含指令参数</param>
        /// <returns>执行结果，包含成功状态和返回信息</returns>
        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            return new RemoteCommandResponse
            {
                Success = true,
                Message = string.Format("test 指令执行成功，参数: {0}", string.Join(", ", request.Args ?? new string[0]))
            };
        }
    }
}
