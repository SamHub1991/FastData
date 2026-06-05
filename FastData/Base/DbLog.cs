using System;
using FastUntility.Base;
using FastData.DbTypes;

namespace FastData.Base
{
    internal static class DbLog
    {
        #region 数据库出错日志
        /// <summary>
        /// 数据库出错日志
        /// </summary>
        /// <param name="IsOutError">是否输出错误</param>
        /// <param name="dbType">数据库类型</param>
        /// <param name="ex">异常对象</param>
        /// <param name="CurrentMethod">当前方法名</param>
        /// <param name="sql">SQL语句</param>
        public static void LogException<T>(bool IsOutError, DataDbType dbType, Exception ex, string CurrentMethod, string sql)
        {
            if (IsOutError)
            {
                var content = string.Format("方法：{0},对象：{1},{3}出错详情：{2}"
                                              , CurrentMethod
                                              , typeof(T).Name
                                              , ex.ToString()
                                              , sql == "" ? "" : string.Format("SQL：{0},", sql));

                BaseLog.SaveLog(content, string.Format("{0}_Error", dbType));
            }
        }
        #endregion 

        #region 数据库出错日志
        /// <summary>
        /// 数据库出错日志
        /// </summary>
        /// <param name="IsOutError">是否输出错误</param>
        /// <param name="dbType">数据库类型</param>
        /// <param name="ex">异常对象</param>
        /// <param name="CurrentMethod">当前方法名</param>
        /// <param name="sql">SQL语句</param>
        public static void LogException(bool IsOutError, DataDbType dbType, Exception ex, string CurrentMethod, string sql)
        {
            if (IsOutError)
            {
                var content = string.Format("方法：{0},{2}出错详情：{1}"
                                              , CurrentMethod
                                              , ex.ToString()
                                              , sql == "" ? "" : string.Format("SQL：{0},", sql));

                BaseLog.SaveLog(content, string.Format("{0}_Error", dbType));
            }
        }
        #endregion 

        #region 数据库sql日志
        /// <summary>
        /// 数据库sql日志
        /// </summary>
        /// <param name="IsOutSql">是否输出SQL</param>
        /// <param name="sql">SQL语句</param>
        /// <param name="dbType">数据库类型</param>
        /// <param name="time">执行时间</param>
        /// <param name="type">日志类型</param>
        public static void LogSql(bool IsOutSql, string sql, DataDbType dbType, double time, string type = null)
        {
            // Check global SQL log setting first, then per-database setting
            if (FastDb.EnableSqlLog || IsOutSql)
            {
                if (type != null)
                    BaseLog.SaveLog(string.Format("{0}[{1}毫秒]", sql, time), string.Format("{1}_{0}_Sql", dbType, type));
                else
                    BaseLog.SaveLog(string.Format("{0}[{1}毫秒]", sql, time), string.Format("{0}_Sql", dbType));
            }
        }
        #endregion

        #region 数据库sql code first日志
        /// <summary>
        /// 数据库sql code first日志
        /// </summary>
        /// <param name="IsOutSql">是否输出 SQL 日志</param>
        /// <param name="sql">执行的 SQL 语句</param>
        /// <param name="dbType">数据库类型</param>
        public static void LogSql(bool IsOutSql, string sql, DataDbType dbType)
        {
            // Check global SQL log setting first, then per-database setting
            if (FastDb.EnableSqlLog || IsOutSql)
            {
                BaseLog.SaveLog(string.Format("{0}", sql), string.Format("{0}_CodeFirst_Sql", dbType));
            }
        }
        #endregion 
    }
}
