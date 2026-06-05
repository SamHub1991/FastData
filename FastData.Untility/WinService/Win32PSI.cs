using System;
using System.Management;
using System.Web;

namespace FastUntility.WinService
{
    /// <summary>
    /// 硬件信息采集类
    /// 用于读取 CPU、网卡、操作系统等硬件信息
    /// </summary>
    public static class Win32PSI
    {
        /// <summary>
        /// 获取 CPU 序列号
        /// </summary>
        /// <returns>CPU 序列号</returns>
        public static string GetCpuInfo()
        {
            try
            {
                using (var sysInfo = new ManagementClass(ConfigWin32PSI.Cpu))
                using (var moc = sysInfo.GetInstances())
                {
                    foreach (ManagementObject mo in moc)
                    {
                        var processorId = mo.Properties["ProcessorId"];
                        if (processorId != null && processorId.Value != null)
                        {
                            return processorId.Value.ToString();
                        }
                    }
                }
            }
            catch
            {
                // 忽略获取异常
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取网卡 MAC 地址
        /// </summary>
        /// <returns>第一个已启用 IP 的网卡 MAC 地址</returns>
        public static string GetMacAddress()
        {
            try
            {
                using (var mc = new ManagementClass(ConfigWin32PSI.NetworkAdapterConfiguration))
                using (var moc = mc.GetInstances())
                {
                    foreach (ManagementObject mo in moc)
                    {
                        var ipEnabled = mo["IPEnabled"];
                        if (ipEnabled != null && (bool)ipEnabled)
                        {
                            var macAddress = mo["MacAddress"];
                            if (macAddress != null)
                            {
                                return macAddress.ToString();
                            }
                        }
                    }
                }
            }
            catch
            {
                // 忽略获取异常
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取当前操作系统的登录用户名
        /// </summary>
        /// <returns>当前登录用户名</returns>
        public static string GetSysUserName()
        {
            return Environment.UserName;
        }

        /// <summary>
        /// 获取操作系统类型
        /// </summary>
        /// <returns>系统类型描述</returns>
        public static string GetSystemType()
        {
            try
            {
                using (var mc = new ManagementClass(ConfigWin32.ComputerSystem))
                using (var moc = mc.GetInstances())
                {
                    foreach (ManagementObject mo in moc)
                    {
                        var systemType = mo["SystemType"];
                        if (systemType != null)
                        {
                            return systemType.ToString();
                        }
                    }
                }
            }
            catch
            {
                // 忽略获取异常
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取计算机名称
        /// </summary>
        /// <returns>计算机名称</returns>
        public static string GetComputerName()
        {
            return Environment.MachineName;
        }

        /// <summary>
        /// 获取物理内存大小（字节）
        /// </summary>
        /// <returns>物理内存总量</returns>
        public static string GetPhysicalMemory()
        {
            try
            {
                using (var mc = new ManagementClass(ConfigWin32.ComputerSystem))
                using (var moc = mc.GetInstances())
                {
                    foreach (ManagementObject mo in moc)
                    {
                        var totalMemory = mo["TotalPhysicalMemory"];
                        if (totalMemory != null)
                        {
                            return totalMemory.ToString();
                        }
                    }
                }
            }
            catch
            {
                // 忽略获取异常
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取客户端 IP 地址（支持 HttpContextBase）
        /// 优先获取代理转发的真实 IP
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <returns>客户端 IP 地址</returns>
        public static string GetClientIPAsync(HttpContextBase context)
        {
            if (context == null || context.Request == null)
                return "127.0.0.1";

            try
            {
                var userIP = context.Request.ServerVariables["HTTP_VIA"];

                if (userIP == null)
                {
                    // 无代理，直接获取客户端地址
                    userIP = context.Request.UserHostAddress;
                }
                else
                {
                    // 有代理，获取转发后的真实 IP
                    userIP = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                }

                // IPv6 本地回环地址转换为 IPv4
                if (userIP == "::1")
                {
                    userIP = "127.0.0.1";
                }

                return string.IsNullOrEmpty(userIP) ? "127.0.0.1" : userIP;
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// 获取客户端 IP 地址
        /// 优先获取代理转发的真实 IP
        /// </summary>
        /// <returns>客户端 IP 地址</returns>
        public static string GetClientIP()
        {
            try
            {
                var current = HttpContext.Current;
                if (current == null || current.Request == null)
                    return "127.0.0.1";

                var viaHeader = current.Request.ServerVariables["HTTP_VIA"];

                string userIP;
                if (viaHeader == null)
                {
                    // 无代理，直接获取客户端地址
                    userIP = current.Request.UserHostAddress;
                }
                else
                {
                    // 有代理，获取转发后的真实 IP
                    userIP = current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                }

                // IPv6 本地回环地址转换为 IPv4
                if (userIP == "::1")
                {
                    userIP = "127.0.0.1";
                }

                return string.IsNullOrEmpty(userIP) ? "127.0.0.1" : userIP;
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}
