using System;
#if NETFRAMEWORK
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace FastData
{
    /// <summary>
    /// 数据库上下文切换
    /// </summary>
    public static class FastDb
    {
        private const string ScopeKey = "FastData.CurrentDbKey";

#if !NETFRAMEWORK
        private static readonly AsyncLocal<string> _currentKey = new AsyncLocal<string>();
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
