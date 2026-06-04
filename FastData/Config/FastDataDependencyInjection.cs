using System;
using Microsoft.Extensions.DependencyInjection;
using FastData.Context;
using FastData.Repository;

namespace FastData.Config
{
    /// <summary>
    /// FastData 依赖注入扩展
    ///
    /// 职责：为 Microsoft.Extensions.DependencyInjection 提供 FastData 集成。
    ///
    /// 使用示例：
    /// <code>
    /// // Program.cs / Startup.cs
    /// services.AddFastData(builder =&gt;
    /// {
    ///     builder.SoftDelete.IsEnabled = true;
    ///     builder.Audit.IsEnabled = true;
    /// });
    ///
    /// // 注入并使用
    /// public class UserService
    /// {
    ///     private readonly IFastRepository _repository;
    ///     public UserService(IFastRepository repository) { _repository = repository; }
    /// }
    /// </code>
    ///
    /// 主要扩展方法：
    /// 1. AddFastData：注册默认配置（使用 db.config 中的 Default Key）
    /// 2. AddFastDataWithKey：注册指定 Key 的数据库
    /// 3. AddFastDataContext：注册 DataContext（事务/连接管理）
    ///
    /// 生命周期：
    /// - IFastRepositoryFactory：Singleton
    /// - IFastRepository/IReadRepository/IWriteRepository/IMapRepository：Scoped
    /// - DataContext：Scoped
    /// </summary>
    public static class FastDataDependencyInjection
    {
        public static IServiceCollection AddFastData(this IServiceCollection services, Action<FastDataOptionsBuilder> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configure != null)
            {
                var builder = new FastDataOptionsBuilder();
                configure(builder);
                builder.Apply();
            }
            
            services.AddSingleton<IFastRepositoryFactory, FastRepositoryFactory>();
            
            services.AddScoped<IFastRepository>(provider => 
                provider.GetRequiredService<IFastRepositoryFactory>().Default());

            services.AddScoped<IReadRepository>(provider => 
                provider.GetRequiredService<IFastRepositoryFactory>().Default());

            services.AddScoped<IWriteRepository>(provider => 
                provider.GetRequiredService<IFastRepositoryFactory>().Default());

            services.AddScoped<IMapRepository>(provider => 
                provider.GetRequiredService<IFastRepositoryFactory>().Default());

            return services;
        }

        public static IServiceCollection AddFastDataWithKey(this IServiceCollection services, string connectionKey, Action<FastDataOptionsBuilder> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrEmpty(connectionKey))
                throw new ArgumentNullException(nameof(connectionKey));

            if (configure != null)
            {
                var builder = new FastDataOptionsBuilder();
                configure(builder);
                builder.Apply();
            }

            services.AddSingleton<IFastRepositoryFactory, FastRepositoryFactory>();

            services.AddScoped<IFastRepository>(provider => 
                provider.GetRequiredService<IFastRepositoryFactory>().Use(connectionKey));

            services.AddScoped<IReadRepository>(provider => 
                provider.GetRequiredService<IFastRepositoryFactory>().Use(connectionKey));

            services.AddScoped<IWriteRepository>(provider => 
                provider.GetRequiredService<IFastRepositoryFactory>().Use(connectionKey));

            services.AddScoped<IMapRepository>(provider => 
                provider.GetRequiredService<IFastRepositoryFactory>().Use(connectionKey));

            return services;
        }

        public static IServiceCollection AddFastDataContext(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddScoped<DataContext>();

            return services;
        }

        public static IServiceCollection AddFastDataContext(this IServiceCollection services, string connectionKey)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrEmpty(connectionKey))
                throw new ArgumentNullException(nameof(connectionKey));

            services.AddScoped(provider => new DataContext(connectionKey));

            return services;
        }
    }

    public class FastDataOptionsBuilder
    {
        public SoftDeleteOptions SoftDelete { get; set; } = new SoftDeleteOptions();
        public AuditOptions Audit { get; set; } = new AuditOptions();
        public MultiTenantOptions MultiTenant { get; set; } = new MultiTenantOptions();
        public ChangeTrackingOptions ChangeTracking { get; set; } = new ChangeTrackingOptions();
        public LazyLoadingOptions LazyLoading { get; set; } = new LazyLoadingOptions();
        public bool Enabled { get; set; } = true;

        public void Apply()
        {
            FastDataOptions.SoftDelete = SoftDelete;
            FastDataOptions.Audit = Audit;
            FastDataOptions.MultiTenant = MultiTenant;
            FastDataOptions.ChangeTracking = ChangeTracking;
            FastDataOptions.LazyLoading = LazyLoading;
            FastDataOptions.Enabled = Enabled;
        }
    }
}