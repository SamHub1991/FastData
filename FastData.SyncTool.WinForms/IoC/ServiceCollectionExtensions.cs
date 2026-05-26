using FastData.Tooling.Sync;

namespace FastData.SyncTool.WinForms.IoC
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 SyncTool 所需的所有服务
        /// </summary>
        public static void RegisterSyncToolServices(this ServiceContainer container)
        {
            // 注册数据同步服务（每次请求创建新实例，因为可能包含状态）
            container.Register<DataSyncService, DataSyncService>();

            // 注册主键配置服务（单例，无状态）
            container.RegisterInstance<PrimaryKeyConfigService>(new PrimaryKeyConfigService());

            // 注册同步配置管理器（单例，管理应用程序配置）
            container.RegisterInstance<SyncConfigManager>(new SyncConfigManager());

            // ReplayService 需要运行时参数，不在此处注册，使用时手动创建
        }
    }
}
