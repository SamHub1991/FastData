using FastUntility.Monitor;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace FastData.Tests
{
    public class ExceptionManagerTests
    {
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

        private void ResetSingleton()
        {
            var field = typeof(ExceptionManager).GetField("_instance", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(null, null);
        }

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
                manager.LogException(new Exception($"异常 {i}"), ExceptionLevel.Error);
            }

            var history = manager.GetExceptionHistory();
            Assert.Equal(5, history.Count);
        }

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
        public string CommandName => "test";
        public string Description => "测试指令";
        public bool RequiresAdmin => false;

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            return new RemoteCommandResponse
            {
                Success = true,
                Message = $"test 指令执行成功，参数: {string.Join(", ", request.Args ?? new string[0])}"
            };
        }
    }
}
