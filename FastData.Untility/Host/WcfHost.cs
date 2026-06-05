using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Messaging;
using FastUntility.Base;

namespace FastUntility.Host
{
    /// <summary>
    /// WCF 宿主类
    /// 提供创建和管理 WCF 服务宿主的功能
    /// </summary>
    public static class WcfHost
    {
        /// <summary>
        /// 创建 WCF 服务宿主
        /// </summary>
        /// <param name="baseUrl">基础 URL 地址</param>
        /// <param name="serviceType">服务实现类型，例如 typeof(MyService)</param>
        /// <param name="contractType">服务契约接口类型，例如 typeof(IMyService)</param>
        /// <param name="binding">绑定配置，由 WcfBinding 类提供</param>
        /// <param name="netUrl">网络访问地址（tcp/msg 模式需要）</param>
        /// <param name="queueName">消息队列名称（仅 msg 模式使用）</param>
        /// <returns>ServiceHost 实例，创建失败时返回 null</returns>
        public static ServiceHost CreateHost(
            string baseUrl,
            Type serviceType,
            Type contractType,
            Binding binding,
            string netUrl = "",
            string queueName = "")
        {
            if (baseUrl == null)
                throw new ArgumentNullException("baseUrl");

            if (serviceType == null)
                throw new ArgumentNullException("serviceType");

            if (contractType == null)
                throw new ArgumentNullException("contractType");

            if (binding == null)
                throw new ArgumentNullException("binding");

            try
            {
                var host = new ServiceHost(serviceType, new Uri(baseUrl));

                // 根据绑定类型决定端点地址
                var endpointAddress = binding.Name == "msg" || binding.Name == "tcp"
                    ? new Uri(netUrl)
                    : new Uri(baseUrl);

                var endpoint = host.AddServiceEndpoint(contractType, binding, endpointAddress);

                // WebHttpBinding 需要添加 WebHttpBehavior 以启用 REST 风格访问
                if (binding.Name == "http")
                {
                    var webBehavior = new WebHttpBehavior
                    {
                        HelpEnabled = true
                    };
                    endpoint.Behaviors.Add(webBehavior);
                }

                // MSMQ 模式需要创建消息队列
                if (binding.Name == "msg")
                {
                    var queuePath = string.Format(@".\private$\{0}", queueName);
                    if (!MessageQueue.Exists(queuePath))
                    {
                        MessageQueue.Create(queuePath, true);
                    }
                }

                host.Open();

                return host;
            }
            catch (Exception ex)
            {
                BaseLog.SaveLog(ex.ToString(), "WcfHost_Error.txt");
                return null;
            }
        }
    }
}
