using System.Collections.Generic;
using System.Data.Common;
using FastData.Model;

namespace FastData.Base
{
    /// <summary>
    /// 标签：2015.9.6，魏中针
    /// 说明：Parameter转sql
    /// </summary>
    internal static class ParameterToSql
    {
        #region object 转sql
         /// <summary>
        /// 标签：2015.9.6，魏中针
        /// 说明：DbParameter转sql
        /// </summary>
        /// <param name="param">数据库参数列表</param>
        /// <param name="Sql">SQL语句</param>
        /// <param name="config">配置模型</param>
        /// <returns>带参数的SQL字符串</returns>
        public static string ObjectParamToSql(List<DbParameter> param, string Sql, ConfigModel config)
        {
            if (param == null)
                return Sql;
            Sql = string.Format("sql:{0},param:", Sql);
          
            param.ForEach(a => {
                if (a != null)
                    Sql = string.Format("{0}{1}={2},", Sql, a.ParameterName, a.Value);
            });

            return Sql;
        }
        #endregion
    }
}
