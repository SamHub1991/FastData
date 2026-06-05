using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
#if !NET452
using System.Runtime.InteropServices;
#endif
using FastUntility.Base;

namespace FastUntility.Security
{
    /// <summary>
    /// 服务器监控信息
    /// </summary>
    public class ServerMonitorInfo
    {
        public string MachineName { get; set; }
        public string OsVersion { get; set; }
        public string ProcessorName { get; set; }
        public int ProcessorCount { get; set; }
        public double CpuUsage { get; set; }
        public long TotalMemory { get; set; }
        public long UsedMemory { get; set; }
        public long AvailableMemory { get; set; }
        public double MemoryUsage { get; set; }
        public List<DiskInfo> Disks { get; set; } = new List<DiskInfo>();
        public List<NetworkInfo> Networks { get; set; } = new List<NetworkInfo>();
        public TimeSpan Uptime { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 磁盘信息
    /// </summary>
    public class DiskInfo
    {
        public string Name { get; set; }
        public string FileSystem { get; set; }
        public long TotalSize { get; set; }
        public long UsedSpace { get; set; }
        public long FreeSpace { get; set; }
        public double UsagePercentage { get; set; }
    }

    /// <summary>
    /// 网络信息
    /// </summary>
    public class NetworkInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public long Speed { get; set; }
        public bool IsUp { get; set; }
    }

    /// <summary>
    /// 服务器监控工具
    /// </summary>
    public static class ServerMonitor
    {
        /// <summary>
        /// 获取服务器监控信息
        /// </summary>
        public static ServerMonitorInfo GetMonitorInfo()
        {
            var info = new ServerMonitorInfo
            {
                MachineName = Environment.MachineName,
                OsVersion = FrameworkCompat.OSDescription(),
                ProcessorCount = Environment.ProcessorCount,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                info.CpuUsage = GetCpuUsage();
                var memoryInfo = GetMemoryInfo();
                info.TotalMemory = memoryInfo.Total;
                info.UsedMemory = memoryInfo.Used;
                info.AvailableMemory = memoryInfo.Available;
                info.MemoryUsage = memoryInfo.UsagePercentage;
                info.Disks = GetDiskInfo();
                info.Networks = GetNetworkInfo();
                info.Uptime = TimeSpan.FromMilliseconds(FrameworkCompat.TickCount64());
            }
            catch (Exception ex)
            {
                BaseLog.SaveLog(string.Format("获取服务器监控信息失败: {0}", ex.Message), "ServerMonitor_Error");
            }

            return info;
        }

        /// <summary>
        /// 获取 CPU 使用率
        /// </summary>
        public static double GetCpuUsage()
        {
            try
            {
                if (FrameworkCompat.IsLinux())
                {
                    return GetCpuUsageLinux();
                }
                else if (FrameworkCompat.IsWindows())
                {
                    return GetCpuUsageWindows();
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 获取 Windows CPU 使用率（通过进程信息估算）
        /// </summary>
        private static double GetCpuUsageWindows()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var tickCount = FrameworkCompat.TickCount64();
                var cpuUsage = process.TotalProcessorTime.TotalMilliseconds / tickCount * 100;
                return Math.Min(100, cpuUsage / Environment.ProcessorCount);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 获取 Linux CPU 使用率
        /// </summary>
        private static double GetCpuUsageLinux()
        {
            try
            {
                var lines = File.ReadAllLines("/proc/stat");
                foreach (var line in lines)
                {
                    if (line.StartsWith("cpu "))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                        {
                            var user = long.Parse(parts[1]);
                            var nice = long.Parse(parts[2]);
                            var system = long.Parse(parts[3]);
                            var idle = long.Parse(parts[4]);
                            var total = user + nice + system + idle;
                            return (double)(total - idle) / total * 100;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BaseLog.SaveLog(string.Format("获取CPU使用率失败: {0}", ex.Message), "ServerMonitor_Error");
            }
            return 0;
        }

        /// <summary>
        /// 获取内存信息
        /// </summary>
        public static (long Total, long Used, long Available, double UsagePercentage) GetMemoryInfo()
        {
            try
            {
                if (FrameworkCompat.IsWindows())
                {
                    return GetMemoryInfoWindows();
                }
                else if (FrameworkCompat.IsLinux())
                {
                    return GetMemoryInfoLinux();
                }
            }
            catch (Exception ex)
            {
                BaseLog.SaveLog(string.Format("获取内存信息失败: {0}", ex.Message), "ServerMonitor_Error");
            }
            return (0, 0, 0, 0);
        }

        /// <summary>
        /// 获取 Windows 内存信息
        /// </summary>
        private static (long Total, long Used, long Available, double UsagePercentage) GetMemoryInfoWindows()
        {
            var process = Process.GetCurrentProcess();
            var used = process.WorkingSet64;
            var total = Environment.SystemPageSize * (long)Environment.ProcessorCount * 1024 * 1024;
            var available = total - used;
            var usage = (double)used / total * 100;
            return (total, used, available, usage);
        }

        /// <summary>
        /// 获取 Linux 内存信息
        /// </summary>
        private static (long Total, long Used, long Available, double UsagePercentage) GetMemoryInfoLinux()
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            long total = 0, available = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    total = long.Parse(parts[1]) * 1024; // KB to bytes
                }
                else if (line.StartsWith("MemAvailable:"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    available = long.Parse(parts[1]) * 1024; // KB to bytes
                }
            }

            var used = total - available;
            var usage = total > 0 ? (double)used / total * 100 : 0;
            return (total, used, available, usage);
        }

        /// <summary>
        /// 获取磁盘信息
        /// </summary>
        public static List<DiskInfo> GetDiskInfo()
        {
            var disks = new List<DiskInfo>();

            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        var disk = new DiskInfo
                        {
                            Name = drive.Name,
                            FileSystem = drive.DriveFormat,
                            TotalSize = drive.TotalSize,
                            FreeSpace = drive.AvailableFreeSpace,
                            UsedSpace = drive.TotalSize - drive.AvailableFreeSpace,
                            UsagePercentage = (double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize * 100
                        };
                        disks.Add(disk);
                    }
                }
            }
            catch (Exception ex)
            {
                BaseLog.SaveLog(string.Format("获取磁盘信息失败: {0}", ex.Message), "ServerMonitor_Error");
            }

            return disks;
        }

        /// <summary>
        /// 获取网络信息
        /// </summary>
        public static List<NetworkInfo> GetNetworkInfo()
        {
            var networks = new List<NetworkInfo>();

            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var stats = nic.GetIPv4Statistics();
                    var network = new NetworkInfo
                    {
                        Name = nic.Name,
                        Description = nic.Description,
                        BytesSent = stats.BytesSent,
                        BytesReceived = stats.BytesReceived,
                        Speed = nic.Speed,
                        IsUp = nic.OperationalStatus == OperationalStatus.Up
                    };
                    networks.Add(network);
                }
            }
            catch (Exception ex)
            {
                BaseLog.SaveLog(string.Format("获取网络信息失败: {0}", ex.Message), "ServerMonitor_Error");
            }

            return networks;
        }

        /// <summary>
        /// 获取系统运行时间
        /// </summary>
        public static TimeSpan GetUptime()
        {
            return TimeSpan.FromMilliseconds(FrameworkCompat.TickCount64());
        }
    }
}
