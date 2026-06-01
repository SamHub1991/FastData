using System;

namespace FastData.Config
{
    /// <summary>
    /// FastData 全局配置
    /// </summary>
    public static class FastDataOptions
    {
        /// <summary>
        /// 软删除配置
        /// </summary>
        public static SoftDeleteOptions SoftDelete { get; set; } = new SoftDeleteOptions();

        /// <summary>
        /// 审计字段配置
        /// </summary>
        public static AuditOptions Audit { get; set; } = new AuditOptions();
        /// <summary>
        /// 多租户配置        /// </summary>
        public static MultiTenantOptions MultiTenant { get; set; } = new MultiTenantOptions();
        /// <summary>
        /// 变更跟踪配置        /// </summary>
        public static ChangeTrackingOptions ChangeTracking { get; set; } = new ChangeTrackingOptions();
        /// <summary>
        /// 懒加载配置        /// </summary>
        public static LazyLoadingOptions LazyLoading { get; set; } = new LazyLoadingOptions();

        /// <summary>
        /// 是否启用配置
        /// </summary>
        public static bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// 软删除配置
    /// </summary>
    public class SoftDeleteOptions
    {
        /// <summary>
        /// 是否启用软删除
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 软删除字段名（默认 IsDeleted）
        /// </summary>
        public string PropertyName { get; set; } = "IsDeleted";

        /// <summary>
        /// 软删除值（默认 true）
        /// </summary>
        public object DeletedValue { get; set; } = true;

        /// <summary>
        /// 未删除值（默认 false）
        /// </summary>
        public object NotDeletedValue { get; set; } = false;
    }

    /// <summary>
    /// 审计字段配置
    /// </summary>
    public class AuditOptions
    {
        /// <summary>
        /// 是否启用审计字段自动填充
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 创建时间字段名
        /// </summary>
        public string CreatedTimeProperty { get; set; } = "CreateTime";

        /// <summary>
        /// 更新时间字段名
        /// </summary>
        public string UpdatedTimeProperty { get; set; } = "UpdateTime";

        /// <summary>
        /// 创建人字段名
        /// </summary>
        public string CreatedByProperty { get; set; } = "CreateBy";

        /// <summary>
        /// 更新人字段名
        /// </summary>
        public string UpdatedByProperty { get; set; } = "UpdateBy";

        /// <summary>
        /// 获取当前用户委托
        /// </summary>
        public Func<string> GetCurrentUser { get; set; } = () => "System";
    }
}
    /// <summary>
    /// 多租户配置
    /// </summary>
    public class MultiTenantOptions
    {
        /// <summary>
        /// 是否启用多租户
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 租户字段名（默认 TenantId）
        /// </summary>
        public string TenantProperty { get; set; } = "TenantId";

        /// <summary>
        /// 获取当前租户 ID 委托
        /// </summary>
        public Func<string> CurrentTenant { get; set; } = () => "default";
    }

    /// <summary>
    /// 变更跟踪配置
    /// </summary>
    public class ChangeTrackingOptions
    {
        /// <summary>
        /// 是否启用变更跟踪
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 跟踪缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;
    }

    /// <summary>
    /// 懒加载配置
    /// </summary>
    public class LazyLoadingOptions
    {
        /// <summary>
        /// 是否启用懒加载
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 懒加载超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
