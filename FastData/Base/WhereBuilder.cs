using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using FastData.Model;

namespace FastData.Base
{
    /// <summary>
    /// WHERE 条件组合帮助类
    /// <para>
    /// 渲染策略：
    /// <list type="bullet">
    /// <item>如果链式条件携带 <see cref="Condition"/> 列表，则按新机制渲染（推荐，全部参数化）</item>
    /// <item>否则回退到旧版 <c>Where</c> + <c>Param</c> 字符串拼接（保持向后兼容）</item>
    /// </list>
    /// </para>
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
            // 没有初始条件 + 没有链式条件
            var hasInitial = query.Predicate.Count > 0 && !string.IsNullOrEmpty(query.Predicate[0].Where);
            if (!hasInitial && query.ChainedConditions.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            var paramIndex = param.Count;

            // 1. 渲染初始条件（来自 Query 入口的 Lambda 表达式）
            if (hasInitial)
            {
                sb.Append(query.Predicate[0].Where);
                if (query.Predicate[0].Param.Count > 0)
                {
                    // 初始条件来自 VisitExpression，它内部已经处理好参数名
                    param.AddRange(query.Predicate[0].Param);
                    paramIndex = param.Count;
                }
            }

            // 2. 渲染链式条件
            for (var i = 0; i < query.ChainedConditions.Count; i++)
            {
                var condition = query.ChainedConditions[i];
                var rendered = RenderChainedCondition(query, condition, param, ref paramIndex);

                if (string.IsNullOrEmpty(rendered)) continue;

                if (sb.Length == 0)
                {
                    // 还没有初始条件，但当前是第一个有效链式条件
                    if (condition.Operator == "OR") sb.Append("OR ");
                    sb.Append(rendered);
                }
                else
                {
                    sb.Append(' ').Append(condition.Operator).Append(' ').Append(rendered);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 渲染单个链式条件：优先使用新 Condition 列表，否则回退到 Where 字符串
        /// </summary>
        private static string RenderChainedCondition(DataQuery query, ChainedCondition condition, List<DbParameter> param, ref int paramIndex)
        {
            if (condition.Conditions != null && condition.Conditions.Count > 0)
            {
                // 新机制：按 Condition 列表渲染
                var sb = new StringBuilder();
                for (var i = 0; i < condition.Conditions.Count; i++)
                {
                    var c = condition.Conditions[i];
                    if (i == 0)
                    {
                        if (c.Logic == ConditionLogic.Or) sb.Append("OR ");
                    }
                    else
                    {
                        sb.Append(' ').Append(c.Logic == ConditionLogic.And ? "AND " : "OR ");
                    }

                    sb.Append(c.Render(query.Config, param, ref paramIndex));
                }
                return sb.ToString();
            }

            // 旧机制：直接使用预渲染的 Where 字符串
            if (condition.Param != null && condition.Param.Count > 0)
                param.AddRange(condition.Param);

            return condition.Where ?? string.Empty;
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
