using System;
using System.Runtime.Remoting.Messaging;

namespace FastData
{
    /// <summary>
    /// 数据库上下文切换
    /// </summary>
    public static class FastDb
    {
        private const string ScopeKey = "FastData.CurrentDbKey";

        /// <summary>
        /// 当前数据库Key
        /// </summary>
        public static string CurrentKey
        {
            get
            {
                return CallContext.LogicalGetData(ScopeKey) as string;
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
                CallContext.LogicalSetData(ScopeKey, key);
            }

            public void Dispose()
            {
                if (string.IsNullOrEmpty(oldKey))
                    CallContext.FreeNamedDataSlot(ScopeKey);
                else
                    CallContext.LogicalSetData(ScopeKey, oldKey);
            }
        }
    }
}
