using System;
using System.Data.Common;
using FastData.Context;

namespace FastData.ConnectionPool
{
    /// <summary>
    /// 连接池扩展方法
    /// </summary>
    public static class ConnectionPoolExtensions
    {
        /// <summary>
        /// 创建带连接池的 DataContext
        /// </summary>
        public static DataContext CreatePooledDataContext(string dbKey, ConnectionPoolConfig config = null)
        {
            return new DataContext(dbKey, poolConfig: config);
        }

        /// <summary>
        /// 获取连接池指标
        /// </summary>
        public static ConnectionPoolMetrics GetPoolMetrics(string dbKey)
        {
            var pool = ConnectionPoolFactory.Instance.GetPool(dbKey);
            return pool?.GetMetrics();
        }

        /// <summary>
        /// 获取所有连接池指标
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, ConnectionPoolMetrics> GetAllPoolMetrics()
        {
            return ConnectionPoolFactory.Instance.GetAllMetrics();
        }
    }
}
