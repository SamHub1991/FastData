using System;

namespace FastData.Property
{
    /// <summary>
    /// 缓存配置属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CacheAttribute : Attribute
    {
        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool IsEnable { get; set; } = false;

        /// <summary>
        /// 缓存过期时间（秒）
        /// </summary>
        public int ExpireTime { get; set; } = 300;

        /// <summary>
        /// 缓存键模板（支持占位符 {PropertyName}）
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 缓存条件
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// 缓存类型（Local/Redis）
        /// </summary>
        public string CacheType { get; set; } = "Redis";

        /// <summary>
        /// 缓存属性构造函数
        /// </summary>
        public CacheAttribute()
        {
        }

        /// <summary>
        /// 缓存属性构造函数
        /// </summary>
        /// <param name="isEnable">是否启用缓存</param>
        /// <param name="expireTime">过期时间（秒）</param>
        public CacheAttribute(bool isEnable, int expireTime = 300)
        {
            IsEnable = isEnable;
            ExpireTime = expireTime;
        }
    }
}
