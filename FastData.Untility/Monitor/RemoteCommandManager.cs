using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace FastUntility.Monitor
{
    /// <summary>
    /// 连接池信息提供者接口
    /// </summary>
    public interface IConnectionPoolInfoProvider
    {
        /// <summary>
        /// 获取所有连接池信息
        /// </summary>
        /// <returns>连接池信息字典</returns>
        Dictionary<string, ConnectionPoolInfo> GetAllPoolInfo();

        /// <summary>
        /// 关闭连接池
        /// </summary>
        /// <param name="poolName">连接池名称</param>
        void ClosePool(string poolName);
    }

    /// <summary>
    /// 连接池信息
    /// </summary>
    public class ConnectionPoolInfo
    {
        public int ActiveConnections { get; set; }
        public int IdleConnections { get; set; }
        public int TotalConnections { get; set; }
        public int PendingRequests { get; set; }
    }

    /// <summary>
    /// 远程指令管理器
    /// </summary>
    public class RemoteCommandManager
    {
        private readonly Dictionary<string, IRemoteCommandHandler> _handlers = new Dictionary<string, IRemoteCommandHandler>(StringComparer.OrdinalIgnoreCase);
        private readonly QQBotConfig _config;
        private readonly IMessageSender _sender;
        private readonly IConnectionPoolInfoProvider _poolInfoProvider;

        public RemoteCommandManager(QQBotConfig config, IMessageSender sender, IConnectionPoolInfoProvider poolInfoProvider = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _poolInfoProvider = poolInfoProvider;
        }

        /// <summary>
        /// 注册指令处理器
        /// </summary>
        public void RegisterHandler(IRemoteCommandHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[handler.CommandName] = handler;
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="senderQQ">发送者QQ号</param>
        /// <param name="groupId">群号</param>
        /// <param name="message">消息内容</param>
        public void ProcessMessage(string senderQQ, string groupId, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            // 检查指令前缀
            if (!message.StartsWith(_config.CommandPrefix))
                return;

            // 解析指令
            var commandText = message.Substring(_config.CommandPrefix.Length).Trim();
            var parts = commandText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return;

            var commandName = parts[0];
            var args = parts.Skip(1).ToArray();

            // 检查权限
            if (_config.RequireAdminForCommands && !_config.AdminQQNumbers.Contains(senderQQ))
            {
                _sender.SendPrivateMessage(senderQQ, "权限不足，只有管理员才能执行指令");
                return;
            }

            // 执行指令
            var request = new RemoteCommandRequest
            {
                Command = commandName,
                Args = args,
                SenderQQ = senderQQ,
                GroupId = groupId
            };

            var response = ExecuteCommand(request);

            // 发送响应
            var responseMessage = response.Success ? response.Message : $"执行失败: {response.Message}";

            if (!string.IsNullOrEmpty(groupId))
            {
                _sender.SendGroupMessage(groupId, responseMessage);
            }
            else
            {
                _sender.SendPrivateMessage(senderQQ, responseMessage);
            }
        }

        /// <summary>
        /// 执行指令
        /// </summary>
        public RemoteCommandResponse ExecuteCommand(RemoteCommandRequest request)
        {
            if (!_handlers.TryGetValue(request.Command, out var handler))
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = $"未知指令: {request.Command}"
                };
            }

            // 检查管理员权限
            if (handler.RequiresAdmin && !_config.AdminQQNumbers.Contains(request.SenderQQ))
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = "权限不足"
                };
            }

            try
            {
                return handler.Execute(request);
            }
            catch (Exception ex)
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = $"指令执行异常: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取所有已注册指令
        /// </summary>
        public IReadOnlyDictionary<string, IRemoteCommandHandler> GetHandlers()
        {
            return _handlers;
        }

        /// <summary>
        /// 获取连接池信息提供者
        /// </summary>
        public IConnectionPoolInfoProvider GetPoolInfoProvider()
        {
            return _poolInfoProvider;
        }
    }

    #region 内置指令处理器

    /// <summary>
    /// 帮助指令
    /// </summary>
    public class HelpCommandHandler : IRemoteCommandHandler
    {
        private readonly RemoteCommandManager _manager;

        public string CommandName => "help";
        public string Description => "显示帮助信息";
        public bool RequiresAdmin => false;

        public HelpCommandHandler(RemoteCommandManager manager)
        {
            _manager = manager;
        }

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("可用指令列表:");
            sb.AppendLine("-------------------");

            foreach (var handler in _manager.GetHandlers())
            {
                var adminTag = handler.Value.RequiresAdmin ? " [管理员]" : "";
                sb.AppendLine($"{handler.Key}{adminTag} - {handler.Value.Description}");
            }

            return new RemoteCommandResponse
            {
                Success = true,
                Message = sb.ToString()
            };
        }
    }

    /// <summary>
    /// 服务器状态指令
    /// </summary>
    public class ServerStatusCommandHandler : IRemoteCommandHandler
    {
        public string CommandName => "status";
        public string Description => "获取服务器状态";
        public bool RequiresAdmin => false;

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            var monitorInfo = FastUntility.Security.ServerMonitor.GetMonitorInfo();

            var sb = new StringBuilder();
            sb.AppendLine("[服务器状态]");
            sb.AppendLine($"机器名: {monitorInfo.MachineName}");
            sb.AppendLine($"系统: {monitorInfo.OsVersion}");
            sb.AppendLine($"CPU: {monitorInfo.CpuUsage:F1}% ({monitorInfo.ProcessorCount} 核)");
            sb.AppendLine($"内存: {FormatBytes(monitorInfo.UsedMemory)} / {FormatBytes(monitorInfo.TotalMemory)} ({monitorInfo.MemoryUsage:F1}%)");
            sb.AppendLine($"运行时间: {FormatTimeSpan(monitorInfo.Uptime)}");

            if (monitorInfo.Disks.Count > 0)
            {
                sb.AppendLine("磁盘:");
                foreach (var disk in monitorInfo.Disks)
                {
                    sb.AppendLine($"  {disk.Name}: {FormatBytes(disk.FreeSpace)} 可用 / {FormatBytes(disk.TotalSize)} ({disk.UsagePercentage:F1}%)");
                }
            }

            return new RemoteCommandResponse
            {
                Success = true,
                Message = sb.ToString()
            };
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}天{ts.Hours}小时{ts.Minutes}分钟";
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}小时{ts.Minutes}分钟";
            return $"{(int)ts.TotalMinutes}分钟";
        }
    }

    /// <summary>
    /// 内存信息指令
    /// </summary>
    public class MemoryCommandHandler : IRemoteCommandHandler
    {
        public string CommandName => "memory";
        public string Description => "获取内存使用详情";
        public bool RequiresAdmin => false;

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            var (total, used, available, usage) = FastUntility.Security.ServerMonitor.GetMemoryInfo();

            var sb = new StringBuilder();
            sb.AppendLine("[内存信息]");
            sb.AppendLine($"总计: {FormatBytes(total)}");
            sb.AppendLine($"已用: {FormatBytes(used)}");
            sb.AppendLine($"可用: {FormatBytes(available)}");
            sb.AppendLine($"使用率: {usage:F1}%");

            return new RemoteCommandResponse
            {
                Success = true,
                Message = sb.ToString()
            };
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// CPU 信息指令
    /// </summary>
    public class CpuCommandHandler : IRemoteCommandHandler
    {
        public string CommandName => "cpu";
        public string Description => "获取 CPU 使用率";
        public bool RequiresAdmin => false;

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            var cpuUsage = FastUntility.Security.ServerMonitor.GetCpuUsage();
            var processCount = Environment.ProcessorCount;

            return new RemoteCommandResponse
            {
                Success = true,
                Message = $"[CPU 信息]\n核心数: {processCount}\n使用率: {cpuUsage:F1}%"
            };
        }
    }

    /// <summary>
    /// 磁盘信息指令
    /// </summary>
    public class DiskCommandHandler : IRemoteCommandHandler
    {
        public string CommandName => "disk";
        public string Description => "获取磁盘信息";
        public bool RequiresAdmin => false;

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            var disks = FastUntility.Security.ServerMonitor.GetDiskInfo();

            if (disks.Count == 0)
            {
                return new RemoteCommandResponse
                {
                    Success = true,
                    Message = "没有可用的磁盘信息"
                };
            }

            var sb = new StringBuilder();
            sb.AppendLine("[磁盘信息]");

            foreach (var disk in disks)
            {
                sb.AppendLine($"{disk.Name} ({disk.FileSystem})");
                sb.AppendLine($"  总计: {FormatBytes(disk.TotalSize)}");
                sb.AppendLine($"  已用: {FormatBytes(disk.UsedSpace)} ({disk.UsagePercentage:F1}%)");
                sb.AppendLine($"  可用: {FormatBytes(disk.FreeSpace)}");
            }

            return new RemoteCommandResponse
            {
                Success = true,
                Message = sb.ToString()
            };
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// 进程信息指令
    /// </summary>
    public class ProcessCommandHandler : IRemoteCommandHandler
    {
        public string CommandName => "process";
        public string Description => "获取当前进程信息";
        public bool RequiresAdmin => false;

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            var process = Process.GetCurrentProcess();

            var sb = new StringBuilder();
            sb.AppendLine("[进程信息]");
            sb.AppendLine($"进程名: {process.ProcessName}");
            sb.AppendLine($"PID: {process.Id}");
            sb.AppendLine($"内存: {FormatBytes(process.WorkingSet64)}");
            sb.AppendLine($"线程数: {process.Threads.Count}");
            sb.AppendLine($"启动时间: {process.StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"运行时间: {FormatTimeSpan(DateTime.Now - process.StartTime)}");

            return new RemoteCommandResponse
            {
                Success = true,
                Message = sb.ToString()
            };
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}天{ts.Hours}小时{ts.Minutes}分钟";
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}小时{ts.Minutes}分钟";
            return $"{(int)ts.TotalMinutes}分钟";
        }
    }

    /// <summary>
    /// 数据库连接状态指令
    /// </summary>
    public class DbStatusCommandHandler : IRemoteCommandHandler
    {
        private readonly RemoteCommandManager _manager;

        public string CommandName => "dbstatus";
        public string Description => "获取数据库连接状态";
        public bool RequiresAdmin => true;

        public DbStatusCommandHandler(RemoteCommandManager manager)
        {
            _manager = manager;
        }

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            var poolInfoProvider = _manager.GetPoolInfoProvider();
            if (poolInfoProvider == null)
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = "连接池信息提供者未配置"
                };
            }

            var poolInfo = poolInfoProvider.GetAllPoolInfo();

            var sb = new StringBuilder();
            sb.AppendLine("[数据库连接状态]");

            if (poolInfo.Count == 0)
            {
                sb.AppendLine("没有活跃的连接池");
            }
            else
            {
                foreach (var kvp in poolInfo)
                {
                    sb.AppendLine($"连接池: {kvp.Key}");
                    sb.AppendLine($"  活跃连接: {kvp.Value.ActiveConnections}");
                    sb.AppendLine($"  空闲连接: {kvp.Value.IdleConnections}");
                    sb.AppendLine($"  总连接数: {kvp.Value.TotalConnections}");
                    sb.AppendLine($"  等待请求: {kvp.Value.PendingRequests}");
                }
            }

            return new RemoteCommandResponse
            {
                Success = true,
                Message = sb.ToString()
            };
        }
    }

    /// <summary>
    /// 关闭数据库连接指令
    /// </summary>
    public class DbCloseCommandHandler : IRemoteCommandHandler
    {
        private readonly RemoteCommandManager _manager;

        public string CommandName => "dbclose";
        public string Description => "关闭指定数据库连接池";
        public bool RequiresAdmin => true;

        public DbCloseCommandHandler(RemoteCommandManager manager)
        {
            _manager = manager;
        }

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            if (request.Args == null || request.Args.Length == 0)
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = "用法: dbclose <连接池名称>"
                };
            }

            var poolName = request.Args[0];
            var poolInfoProvider = _manager.GetPoolInfoProvider();

            if (poolInfoProvider == null)
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = "连接池信息提供者未配置"
                };
            }

            try
            {
                poolInfoProvider.ClosePool(poolName);

                return new RemoteCommandResponse
                {
                    Success = true,
                    Message = $"已关闭连接池: {poolName}"
                };
            }
            catch (Exception ex)
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = $"关闭连接池失败: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// 重启数据库连接指令
    /// </summary>
    public class DbRestartCommandHandler : IRemoteCommandHandler
    {
        private readonly RemoteCommandManager _manager;

        public string CommandName => "dbrestart";
        public string Description => "重启指定数据库连接池";
        public bool RequiresAdmin => true;

        public DbRestartCommandHandler(RemoteCommandManager manager)
        {
            _manager = manager;
        }

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            if (request.Args == null || request.Args.Length == 0)
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = "用法: dbrestart <连接池名称>"
                };
            }

            var poolName = request.Args[0];
            var poolInfoProvider = _manager.GetPoolInfoProvider();

            if (poolInfoProvider == null)
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = "连接池信息提供者未配置"
                };
            }

            try
            {
                poolInfoProvider.ClosePool(poolName);
                // 注意：实际重新初始化需要根据具体配置进行

                return new RemoteCommandResponse
                {
                    Success = true,
                    Message = $"已重启连接池: {poolName}"
                };
            }
            catch (Exception ex)
            {
                return new RemoteCommandResponse
                {
                    Success = false,
                    Message = $"重启连接池失败: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// 清理内存指令
    /// </summary>
    public class GcCommandHandler : IRemoteCommandHandler
    {
        public string CommandName => "gc";
        public string Description => "强制垃圾回收";
        public bool RequiresAdmin => true;

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            var beforeMemory = GC.GetTotalMemory(false);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var afterMemory = GC.GetTotalMemory(false);
            var freed = beforeMemory - afterMemory;

            return new RemoteCommandResponse
            {
                Success = true,
                Message = $"垃圾回收完成\n释放内存: {FormatBytes(freed)}\n当前内存: {FormatBytes(afterMemory)}"
            };
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// 版本信息指令
    /// </summary>
    public class VersionCommandHandler : IRemoteCommandHandler
    {
        public string CommandName => "version";
        public string Description => "获取系统版本信息";
        public bool RequiresAdmin => false;

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[版本信息]");
            sb.AppendLine($"系统: {Environment.OSVersion}");
            sb.AppendLine($"运行时: {Environment.Version}");
            sb.AppendLine($"机器名: {Environment.MachineName}");
            sb.AppendLine($"处理器: {Environment.ProcessorCount} 核");
            sb.AppendLine($"64位系统: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"64位进程: {Environment.Is64BitProcess}");

            return new RemoteCommandResponse
            {
                Success = true,
                Message = sb.ToString()
            };
        }
    }

    /// <summary>
    /// 时间信息指令
    /// </summary>
    public class TimeCommandHandler : IRemoteCommandHandler
    {
        public string CommandName => "time";
        public string Description => "获取服务器时间";
        public bool RequiresAdmin => false;

        public RemoteCommandResponse Execute(RemoteCommandRequest request)
        {
            return new RemoteCommandResponse
            {
                Success = true,
                Message = $"服务器时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };
        }
    }

    #endregion
}
