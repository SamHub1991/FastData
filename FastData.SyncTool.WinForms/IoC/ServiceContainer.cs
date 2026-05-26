using System;
using System.Collections.Generic;

namespace FastData.SyncTool.WinForms.IoC
{
    /// <summary>
    /// 简单的依赖注入容器
    /// </summary>
    public class ServiceContainer
    {
        private readonly Dictionary<System.Type, System.Func<object>> _factories = new Dictionary<System.Type, System.Func<object>>();

        /// <summary>
        /// 注册服务（每次请求创建新实例）
        /// </summary>
        public void Register<TService, TImplementation>()
            where TImplementation : TService, new()
        {
            _factories[typeof(TService)] = () => new TImplementation();
        }

        /// <summary>
        /// 注册单例服务
        /// </summary>
        private TInstance RegisterSingletonInternal<TInstance>(TInstance instance)
        {
            _factories[typeof(TInstance)] = () => instance;
            return instance;
        }

        /// <summary>
        /// 注册单例服务
        /// </summary>
        public void RegisterSingleton<TService, TImplementation>()
            where TImplementation : TService, new()
        {
            var instance = new TImplementation();
            RegisterSingletonInternal((TService)instance);
        }

        /// <summary>
        /// 注册单例实例
        /// </summary>
        public void RegisterInstance<TService>(TService instance)
        {
            _factories[typeof(TService)] = () => instance;
        }

        /// <summary>
        /// 注册工厂方法
        /// </summary>
        public void RegisterFactory<TService>(Func<TService> factory)
        {
            _factories[typeof(TService)] = () => factory();
        }

        /// <summary>
        /// 解析服务
        /// </summary>
        public TService Resolve<TService>()
        {
            if (_factories.TryGetValue(typeof(TService), out var factory))
            {
                return (TService)factory();
            }

            throw new InvalidOperationException($"Service '{typeof(TService).Name}' is not registered.");
        }

        /// <summary>
        /// 尝试解析服务
        /// </summary>
        public bool TryResolve<TService>(out TService service)
        {
            if (_factories.TryGetValue(typeof(TService), out var factory))
            {
                service = (TService)factory();
                return true;
            }

            service = default(TService);
            return false;
        }
    }
}
