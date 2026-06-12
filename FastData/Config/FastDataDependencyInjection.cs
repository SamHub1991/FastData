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
        /// <summary>
        /// Registers FastData repositories using the default configured connection key.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configure">Optional FastData options callback.</param>
        /// <returns>The service collection.</returns>
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

        /// <summary>
        /// Registers FastData repositories bound to a specific connection key.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="connectionKey">FastData connection key.</param>
        /// <param name="configure">Optional FastData options callback.</param>
        /// <returns>The service collection.</returns>
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

        /// <summary>
        /// Registers DataContext using the default configured connection key.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddFastDataContext(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddScoped<DataContext>();

            return services;
        }

        /// <summary>
        /// Registers DataContext bound to a specific connection key.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="connectionKey">FastData connection key.</param>
        /// <returns>The service collection.</returns>
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

    /// <summary>
    /// Builds FastData global options for dependency injection registration.
    /// </summary>
    public class FastDataOptionsBuilder
    {
        /// <summary>Gets or sets soft-delete options.</summary>
        public SoftDeleteOptions SoftDelete { get; set; } = new SoftDeleteOptions();
        /// <summary>Gets or sets audit options.</summary>
        public AuditOptions Audit { get; set; } = new AuditOptions();
        /// <summary>Gets or sets multi-tenant options.</summary>
        public MultiTenantOptions MultiTenant { get; set; } = new MultiTenantOptions();
        /// <summary>Gets or sets change-tracking options.</summary>
        public ChangeTrackingOptions ChangeTracking { get; set; } = new ChangeTrackingOptions();
        /// <summary>Gets or sets lazy-loading options.</summary>
        public LazyLoadingOptions LazyLoading { get; set; } = new LazyLoadingOptions();
        /// <summary>Gets or sets whether FastData options are enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Applies the configured options to FastDataOptions.
        /// </summary>
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
