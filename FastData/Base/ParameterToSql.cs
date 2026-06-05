using System.Collections.Generic;
using System.Data.Common;
using FastData.Model;

namespace FastData.Base
{
    /// <summary>
    /// DbParameter 转 SQL 字符串工具类
    /// </summary>
    internal static class ParameterToSql
    {
        /// <summary>
        /// 将 DbParameter 集合转换为可读的 SQL 调试字符串
        /// 格式: sql:原始SQL,param:参数名1=值1,参数名2=值2,
        /// </summary>
        /// <param name="parameters">数据库参数列表</param>
        /// <param name="sql">原始 SQL 语句</param>
        /// <param name="config">数据库配置模型</param>
        /// <returns>包含参数值的调试用 SQL 字符串</returns>
        public static string ObjectParamToSql(List<DbParameter> parameters, string sql, ConfigModel config)
        {
            if (parameters == null || parameters.Count == 0)
                return sql;

            var result = string.Concat("sql:", sql, ",param:");

            foreach (var parameter in parameters)
            {
                if (parameter != null)
                    result = string.Concat(result, parameter.ParameterName, "=", parameter.Value, ",");
            }

            return result;
        }
    }
}
