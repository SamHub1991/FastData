using System;
using FastUntility.Base;
using FastData.DbTypes;
#if !NETFRAMEWORK
using Microsoft.Extensions.Logging;
#endif

namespace FastData.Base
{
    /// <summary>
    /// 统一的数据库日志记录器
    /// 
    /// 日志策略：
    /// - NETFRAMEWORK：使用 BaseLog.SaveLog 直接写文件
    /// - !NETFRAMEWORK：若已通过 ConfigureLogging 初始化则走 ILogger，否则降级到 BaseLog.SaveLog
    /// </summary>
    internal static class DbLog
    {
#if !NETFRAMEWORK
        private static ILoggerFactory _loggerFactory;
        private static ILogger _sqlLogger;
        private static ILogger _errorLogger;
        private static bool _isInitialized;

        /// <summary>
        /// 初始化 Microsoft.Extensions.Logging 集成
        /// 调用后将优先使用 ILogger 输出日志
        /// </summary>
        /// <param name="loggerFactory">日志工厂</param>
        public static void InitializeLogger(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _sqlLogger = loggerFactory.CreateLogger("FastData.Sql");
            _errorLogger = loggerFactory.CreateLogger("FastData.Error");
            _isInitialized = true;
        }

        /// <summary>
        /// 判断是否使用 ILogger 输出
        /// </summary>
        private static bool UseILogger => _isInitialized && _sqlLogger != null;
#endif

        #region 数据库出错日志（泛型版本）

        /// <summary>
        /// 数据库出错日志（泛型版本，输出类型信息）
        /// </summary>
        public static void LogException<T>(bool isOutError, DataDbType dbType, Exception ex, string currentMethod, string sql)
        {
            if (!isOutError) return;

#if !NETFRAMEWORK
            if (UseILogger)
            {
                var message = string.Format("方法：{0}, 对象：{1}, {2}出错详情：{3}",
                    currentMethod, typeof(T).Name,
                    string.IsNullOrEmpty(sql) ? "" : string.Format("SQL：{0}, ", sql),
                    ex.ToString());
                _errorLogger.LogCritical(ex, message);
                return;
            }
#endif
            var content = string.Format("方法：{0},对象：{1},{3}出错详情：{2}",
                currentMethod, typeof(T).Name, ex.ToString(),
                string.IsNullOrEmpty(sql) ? "" : string.Format("SQL：{0},", sql));
            BaseLog.SaveLog(content, string.Format("{0}_Error", dbType));
        }

        #endregion

        #region 数据库出错日志（非泛型版本）

        /// <summary>
        /// 数据库出错日志（非泛型版本）
        /// </summary>
        public static void LogException(bool isOutError, DataDbType dbType, Exception ex, string currentMethod, string sql)
        {
            if (!isOutError) return;

#if !NETFRAMEWORK
            if (UseILogger)
            {
                var message = string.Format("方法：{0}, {1}出错详情：{2}",
                    currentMethod,
                    string.IsNullOrEmpty(sql) ? "" : string.Format("SQL：{0}, ", sql),
                    ex.ToString());
                _errorLogger.LogCritical(ex, message);
                return;
            }
#endif
            var content = string.Format("方法：{0},{2}出错详情：{1}",
                currentMethod, ex.ToString(),
                string.IsNullOrEmpty(sql) ? "" : string.Format("SQL：{0},", sql));
            BaseLog.SaveLog(content, string.Format("{0}_Error", dbType));
        }

        #endregion

        #region 数据库 SQL 日志

        /// <summary>
        /// 数据库 SQL 日志
        /// </summary>
        /// <param name="isOutSql">是否输出 SQL</param>
        /// <param name="sql">SQL 语句</param>
        /// <param name="dbType">数据库类型</param>
        /// <param name="elapsedMs">执行耗时（毫秒）</param>
        /// <param name="type">日志类型（可选，如 "CodeFirst"）</param>
        public static void LogSql(bool isOutSql, string sql, DataDbType dbType, double elapsedMs, string type = null)
        {
            if (!FastDb.EnableSqlLog && !isOutSql) return;

#if !NETFRAMEWORK
            if (UseILogger)
            {
                var logLevel = elapsedMs > FastDb.SlowQueryThresholdMs ? LogLevel.Warning : LogLevel.Information;
                var suffix = type != null ? string.Format("[{0}]", type) : "";
                _sqlLogger.Log(logLevel, "{Sql} [{ElapsedMs:F2}ms]{Suffix}", sql, elapsedMs, suffix);
                if (elapsedMs > FastDb.SlowQueryThresholdMs)
                {
                    _sqlLogger.LogWarning("慢查询警告：耗时 {ElapsedMs:F2}ms > 阈值 {Threshold}ms",
                        elapsedMs, FastDb.SlowQueryThresholdMs);
                }
                return;
            }
#endif
            var tag = type != null
                ? string.Format("{1}_{0}_Sql", dbType, type)
                : string.Format("{0}_Sql", dbType);
            BaseLog.SaveLog(string.Format("{0}[{1}毫秒]", sql, elapsedMs), tag);
        }

        #endregion

        #region CodeFirst SQL 日志

        /// <summary>
        /// CodeFirst 建表 SQL 日志（无耗时参数的重载）
        /// </summary>
        public static void LogSql(bool isOutSql, string sql, DataDbType dbType)
        {
            LogSql(isOutSql, sql, dbType, 0, "CodeFirst");
        }

        #endregion
    }
}