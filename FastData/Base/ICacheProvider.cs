namespace FastData.Base
{
    /// <summary>
    /// 缓存提供者接口
    /// 定义统一的缓存操作契约，支持策略模式扩展不同缓存实现
    /// </summary>
    internal interface ICacheProvider
    {
        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="hours">缓存时间（小时）</param>
        void Set(string key, string value, int hours);

        /// <summary>
        /// 设置泛型缓存
        /// </summary>
        /// <typeparam name="T">缓存实体类型（需有无参构造函数）</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存实体对象</param>
        /// <param name="hours">缓存时间（小时）</param>
        void Set<T>(string key, T value, int hours) where T : class, new();

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>缓存值</returns>
        string Get(string key);

        /// <summary>
        /// 获取泛型缓存
        /// </summary>
        /// <typeparam name="T">缓存实体类型（需有无参构造函数）</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存实体对象；缓存不存在时返回 default(T)</returns>
        T Get<T>(string key) where T : class, new();

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>缓存存在返回 true，否则返回 false</returns>
        bool Exists(string key);

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        void Remove(string key);
    }
}
