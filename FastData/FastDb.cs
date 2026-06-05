using System;
using System.Reflection;
#if !NETFRAMEWORK
using Microsoft.Extensions.Logging;
#endif
#if NETFRAMEWORK
using System.Runtime.Remoting.Messaging;
using System.Threading;
#else
using System.Threading;
#endif
using FastData.Base;

namespace FastData
{
    /// <summary>
    /// FastData 全局配置与数据库上下文管理
    /// 
    /// 职责：
    /// 1. 全局 SQL 日志开关控制
    /// 2. 数据库 Key 作用域切换（支持嵌套）
    /// 3. 当前数据库 Key 管理
    /// 4. 调用程序集名称缓存（避免重复 Assembly.GetCallingAssembly() 栈回溯开销）
    /// 
    /// 使用示例：
    /// <code>
    /// // 1. 全局启用 SQL 日志
    /// FastDb.EnableSqlLog = true;
    /// 
    /// // 2. 切换数据库（使用 using 限定作用域）
    /// using (FastDb.Use("db2"))
    /// {
    ///     // 此作用域内所有操作使用 db2
    ///     var users = FastRead.Query&lt;User&gt;(u =&gt; true).ToList();
    /// }
    /// // 离开作用域后自动恢复原数据库
    /// 
    /// // 3. 获取当前数据库 Key
    /// var currentKey = FastDb.CurrentKey;
    /// </code>
    /// 
    /// 相关类：
    /// - FastRead: 读取操作（静态方法）
    /// - FastWrite: 写入操作（静态方法）
    /// - FastMap: XML 映射操作（静态方法）
    /// - FastDataClient: 统一门面（推荐，绑定 Key 的实例方法）
    /// </summary>
    public static class FastDb
    {
        private const string ScopeKey = "FastData.CurrentDbKey";

#if !NETFRAMEWORK
        private static readonly AsyncLocal<string> _currentKey = new AsyncLocal<string>();
#endif

        private static readonly Lazy<string> _cachedProjectName =
            new Lazy<string>(() => Assembly.GetCallingAssembly().GetName().Name, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 调用程序集名称缓存
        /// 首次访问时通过 Assembly.GetCallingAssembly() 获取并缓存，避免每次 API 调用的栈回溯开销
        /// 多程序集场景下可通过调用 ResetProjectName() 重置
        /// </summary>
        internal static string CachedProjectName => _cachedProjectName.Value;

        /// <summary>
        /// 获取调用程序集名称（优先使用缓存，首次自动获取）
        /// </summary>
        internal static string GetProjectName()
        {
            return CachedProjectName;
        }

        /// <summary>
        /// 重置缓存的程序集名称（多程序集场景使用）
        /// </summary>
        internal static void ResetProjectName()
        {
            typeof(FastDb).GetField("_cachedProjectName", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, new Lazy<string>(() => Assembly.GetCallingAssembly().GetName().Name, LazyThreadSafetyMode.ExecutionAndPublication));
        }

/// <summary>
        /// 全局 SQL 日志开关（默认关闭）
        /// 设置为 true 时，所有 SQL 查询都会被记录
        /// </summary>
        public static bool EnableSqlLog { get; set; } = false;

        /// <summary>
        /// 慢查询阈值（毫秒），超过此阈值的查询会被记录为警告
        /// </summary>
        public static int SlowQueryThresholdMs { get; set; } = 1000;

        /// <summary>
        /// 配置 FastData 使用 Microsoft.Extensions.Logging
        /// </summary>
        /// <param name="loggerFactory">日志工厂</param>
#if !NETFRAMEWORK
        public static void ConfigureLogging(ILoggerFactory loggerFactory)
        {
            EnhancedDbLog.Initialize(loggerFactory);
        }
#endif

        /// <summary>
        /// 当前数据库Key
        /// </summary>
        public static string CurrentKey
        {
            get
            {
#if NETFRAMEWORK
                return CallContext.LogicalGetData(ScopeKey) as string;
#else
                return _currentKey.Value;
#endif
            }
        }

        /// <summary>
        /// 在当前执行上下文内切换数据库
        /// </summary>
        public static IDisposable Use(string key)
        {
            return new FastDbScope(key);
        }

        private sealed class FastDbScope : IDisposable
        {
            private readonly string oldKey;

            public FastDbScope(string key)
            {
                oldKey = CurrentKey;
#if NETFRAMEWORK
                CallContext.LogicalSetData(ScopeKey, key);
#else
                _currentKey.Value = key;
#endif
            }

            public void Dispose()
            {
#if NETFRAMEWORK
                if (string.IsNullOrEmpty(oldKey))
                    CallContext.FreeNamedDataSlot(ScopeKey);
                else
                    CallContext.LogicalSetData(ScopeKey, oldKey);
#else
                _currentKey.Value = oldKey;
#endif
            }
        }
    }
}
