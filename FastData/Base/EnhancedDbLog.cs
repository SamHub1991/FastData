using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using FastData.DbTypes;
using Microsoft.Extensions.Logging;

namespace FastData.Base
{
    /// <summary>
    /// 增强的数据库日志记录器
    /// 支持 Microsoft.Extensions.Logging 抽象，输出参数化 SQL
    /// </summary>
    internal static class EnhancedDbLog
    {
        private static ILoggerFactory _loggerFactory;
        private static ILogger _sqlLogger;
        private static ILogger _errorLogger;
        private static bool _isInitialized;

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        /// <param name="loggerFactory">日志工厂</param>
        public static void Initialize(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _sqlLogger = loggerFactory.CreateLogger("FastData.Sql");
            _errorLogger = loggerFactory.CreateLogger("FastData.Error");
            _isInitialized = true;
        }

        /// <summary>
        /// 记录参数化 SQL（带实际参数值）
        /// </summary>
        /// <param name="sql">SQL 语句</param>
        /// <param name="parameters">参数列表</param>
        /// <param name="dbType">数据库类型</param>
        /// <param name="elapsedMs">执行时间（毫秒）</param>
        /// <param name="isSlowQueryThreshold">慢查询阈值（毫秒）</param>
        public static void LogParameterizedSql(string sql, IEnumerable<DbParameter> parameters, 
            DataDbType dbType, double elapsedMs, int isSlowQueryThreshold = 1000)
        {
            if (!_isInitialized)
                return;

            var formattedSql = FormatSqlWithParameters(sql, parameters);
            var logLevel = elapsedMs > isSlowQueryThreshold ? LogLevel.Warning : LogLevel.Information;
            
            var message = new StringBuilder()
                .AppendLine(string.Format("[{0}] {1}", dbType, formattedSql))
                .AppendLine(string.Format("Execution Time: {0:F2}ms", elapsedMs))
                .ToString();

            _sqlLogger.Log(logLevel, message);

            if (elapsedMs > isSlowQueryThreshold)
            {
                _sqlLogger.LogWarning("慢查询警告：执行时间 {ElapsedMs}ms > 阈值 {Threshold}ms", 
                    elapsedMs, isSlowQueryThreshold);
            }
        }

        /// <summary>
        /// 格式化 SQL，将参数占位符替换为实际值
        /// </summary>
        private static string FormatSqlWithParameters(string sql, IEnumerable<DbParameter> parameters)
        {
            if (parameters == null || !parameters.Any())
                return sql;

            var formattedSql = sql;
            foreach (var param in parameters)
            {
                var paramName = param.ParameterName;
                var paramValue = param.Value == null || param.Value == DBNull.Value 
                    ? "NULL" 
                    : FormatParameterValue(param.Value, param.DbType);
                
                // 处理 @param 和 :param 两种格式
                formattedSql = formattedSql.Replace(paramName, paramValue);
                formattedSql = formattedSql.Replace(paramName.TrimStart('@', ':'), paramValue);
            }

            return formattedSql;
        }

        /// <summary>
        /// 格式化参数值
        /// </summary>
        private static string FormatParameterValue(object value, System.Data.DbType? dbType = null)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            return value switch
            {
                string s => string.Format("'{0}'", s.Replace("'", "''")),  // SQL 注入防护
                DateTime dt => string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", dt),
                DateTimeOffset dto => string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", dto),
                bool b => b ? "1" : "0",
                byte[] bytes => string.Format("0x{0}", BitConverter.ToString(bytes).Replace("-", "")),
                _ => value.ToString()
            };
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void LogException(Exception ex, string method, string sql, 
            IEnumerable<DbParameter> parameters = null)
        {
            if (!_isInitialized)
                return;

            var message = new StringBuilder()
                .AppendLine(string.Format("方法：{0}", method))
                .AppendLine(string.Format("错误：{0} - {1}", ex.GetType().Name, ex.Message))
                .AppendLine(string.Format("堆栈：{0}", ex.StackTrace));

            if (!string.IsNullOrEmpty(sql))
            {
                message.AppendLine(string.Format("SQL: {0}", sql));
                if (parameters != null && parameters.Any())
                {
                    message.AppendLine(string.Format("参数：{0}", FormatSqlWithParameters(sql, parameters)));
                }
            }

            _errorLogger.LogCritical(ex, message.ToString());
        }
    }
}
