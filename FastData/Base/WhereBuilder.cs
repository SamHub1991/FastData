using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using FastData.Model;

namespace FastData.Base
{
    /// <summary>
    /// WHERE 条件组合帮助类
    /// </summary>
    internal static class WhereBuilder
    {
        /// <summary>
        /// 构建完整的 WHERE 子句（包含链式条件）
        /// </summary>
        /// <param name="query">查询对象</param>
        /// <param name="param">参数列表（输出）</param>
        /// <returns>完整的 WHERE 子句</returns>
        public static string BuildWhereClause(DataQuery query, ref List<DbParameter> param)
        {
            if (query.Predicate.Count == 0 || string.IsNullOrEmpty(query.Predicate[0].Where))
            {
                // 没有初始条件，但可能有链式条件
                if (query.ChainedConditions.Count == 0)
                    return "";

                // 只有链式条件
                var sb = new StringBuilder();
                for (int i = 0; i < query.ChainedConditions.Count; i++)
                {
                    var condition = query.ChainedConditions[i];
                    if (i == 0)
                    {
                        sb.Append(condition.Where);
                    }
                    else
                    {
                        sb.AppendFormat(" {0} {1}", condition.Operator, condition.Where);
                    }

                    if (condition.Param.Count > 0)
                        param.AddRange(condition.Param);
                }

                return sb.ToString();
            }

            // 有初始条件
            var result = new StringBuilder(query.Predicate[0].Where);

            // 添加初始条件的参数
            if (query.Predicate[0].Param.Count > 0)
                param.AddRange(query.Predicate[0].Param);

            // 追加链式条件
            foreach (var condition in query.ChainedConditions)
            {
                result.AppendFormat(" {0} {1}", condition.Operator, condition.Where);

                if (condition.Param.Count > 0)
                    param.AddRange(condition.Param);
            }

            return result.ToString();
        }

        /// <summary>
        /// 检查是否有任何 WHERE 条件
        /// </summary>
        public static bool HasWhereClause(DataQuery query)
        {
            if (query.Predicate.Count > 0 && !string.IsNullOrEmpty(query.Predicate[0].Where))
                return true;

            return query.ChainedConditions.Count > 0;
        }
    }
}
