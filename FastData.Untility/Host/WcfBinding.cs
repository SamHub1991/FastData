using System;
using System.ServiceModel;

namespace FastUntility.Host
{
    /// <summary>
    /// WCF 绑定配置类
    /// 提供各类 WCF 绑定方式的便捷配置方法
    /// </summary>
    public static class WcfBinding
    {
        /// <summary>
        /// 创建 WebHttpBinding 配置（REST HTTP 模式）
        /// 适用于通过 HTTP GET/POST/PUT/DELETE 访问的 REST 服务
        /// </summary>
        /// <returns>配置好的 WebHttpBinding 实例</returns>
        public static WebHttpBinding CreateHttpBinding()
        {
            var binding = new WebHttpBinding
            {
                TransferMode = TransferMode.Streamed,
                Security = { Mode = WebHttpSecurityMode.None },
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                MaxReceivedMessageSize = long.MaxValue,
                Name = "http"
            };

            return binding;
        }

        /// <summary>
        /// 创建 BasicHttpBinding 配置（基础 SOAP 模式）
        /// 最简单的绑定类型，通常用于兼容传统 Web Services
        /// </summary>
        /// <returns>配置好的 BasicHttpBinding 实例</returns>
        public static BasicHttpBinding CreateBasicBinding()
        {
            var binding = new BasicHttpBinding
            {
                TransferMode = TransferMode.Streamed,
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                Name = "basic"
            };

            return binding;
        }

        /// <summary>
        /// 创建 WSHttpBinding 配置（增强 SOAP 模式）
        /// 比 BasicHttpBinding 更安全，适用于非双工服务通讯
        /// </summary>
        /// <returns>配置好的 WSHttpBinding 实例</returns>
        public static WSHttpBinding CreateWsBinding()
        {
            var binding = new WSHttpBinding
            {
                Security = { Mode = SecurityMode.None },
                Name = "ws"
            };

            return binding;
        }

        /// <summary>
        /// 创建 WSDualHttpBinding 配置（双工 HTTP 模式）
        /// 支持双向通道通讯，允许服务端回调客户端
        /// </summary>
        /// <returns>配置好的 WSDualHttpBinding 实例</returns>
        public static WSDualHttpBinding CreateWsDualBinding()
        {
            var binding = new WSDualHttpBinding
            {
                Security = { Mode = WSDualHttpSecurityMode.None },
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                Name = "wsdual"
            };

            return binding;
        }

        /// <summary>
        /// 创建 WSFederationHttpBinding 配置（联合认证模式）
        /// 支持 WS-Federation 安全通讯协议
        /// </summary>
        /// <returns>配置好的 WSFederationHttpBinding 实例</returns>
        public static WSFederationHttpBinding CreateWsFederationBinding()
        {
            var binding = new WSFederationHttpBinding
            {
                Security = { Mode = WSFederationHttpSecurityMode.None },
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                Name = "wsf"
            };

            return binding;
        }

        /// <summary>
        /// 创建 NetTcpBinding 配置（TCP 流模式）
        /// 效率最高，安全的跨机器通讯方式
        /// </summary>
        /// <returns>配置好的 NetTcpBinding 实例</returns>
        public static NetTcpBinding CreateTcpBinding()
        {
            var binding = new NetTcpBinding
            {
                TransferMode = TransferMode.Streamed,
                Security = { Mode = SecurityMode.None },
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                MaxConnections = int.MaxValue,
                Name = "tcp"
            };

            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;

            return binding;
        }

        /// <summary>
        /// 创建 NetNamedPipeBinding 配置（命名管道模式）
        /// 安全、可靠、高效的单机进程间通讯方式
        /// </summary>
        /// <returns>配置好的 NetNamedPipeBinding 实例</returns>
        public static NetNamedPipeBinding CreateNetNameBinding()
        {
            var binding = new NetNamedPipeBinding
            {
                TransferMode = TransferMode.Streamed,
                Security = { Mode = NetNamedPipeSecurityMode.None },
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                Name = "net"
            };

            return binding;
        }

        /// <summary>
        /// 创建 NetMsmqBinding 配置（消息队列模式）
        /// 使用消息队列在不同机器间进行异步通讯
        /// </summary>
        /// <returns>配置好的 NetMsmqBinding 实例</returns>
        public static NetMsmqBinding CreateMsgBinding()
        {
            var binding = new NetMsmqBinding
            {
                Security = { Mode = NetMsmqSecurityMode.None },
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                QueueTransferProtocol = QueueTransferProtocol.Srmp,
                Name = "msg",
                ExactlyOnce = false,
                Durable = true
            };

            return binding;
        }
    }
}
