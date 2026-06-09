using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using FastData.Infrastructure;
using FastData.Model;

namespace FastData.Base
{
    /// <summary>
    /// 不可变条件值对象
    /// <para>
    /// 借鉴 Dos.ORM 的 WhereClip 思想：一个 <see cref="Condition"/> 对象即一段
    /// "字段 + 操作符 + 值"的 WHERE 条件片段，自身不参与拼接逻辑，
    /// 由 <see cref="ConditionBuilder"/> 负责组合多个 Condition 输出最终 SQL。
    /// </para>
    /// <para>
    /// 关键特性：
    /// <list type="bullet">
    /// <item>所有值均通过 <see cref="DbParameter"/> 绑定，天然防 SQL 注入</item>
    /// <item>通过 <see cref="Render"/> 集中渲染 SQL 片段，新增操作符仅需扩展一处</item>
    /// <item>使用字段占位符（如 <c>@p0</c>），最终参数化由 ADO.NET 完成</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class Condition
    {
        /// <summary>字段名（不带表别名）</summary>
        public string Field { get; }

        /// <summary>条件操作符</summary>
        public ConditionOperator Operator { get; }

        /// <summary>参数值（IN/Between 时为集合；IsNull/IsNotNull 时为 null）</summary>
        public object Value { get; }

        /// <summary>逻辑连接符（AND/OR），决定与前一个条件的连接方式</summary>
        public ConditionLogic Logic { get; }

        /// <summary>
        /// 构造一个不可变条件片段
        /// </summary>
        /// <param name="field">字段名（不含表别名）</param>
        /// <param name="op">条件操作符</param>
        /// <param name="value">参数值；IN/Between 接受集合，其他接受标量</param>
        /// <param name="logic">与前一个条件的逻辑关系，默认 AND</param>
        public Condition(string field, ConditionOperator op, object value, ConditionLogic logic = ConditionLogic.And)
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("字段名不能为空", nameof(field));

            Field = field;
            Operator = op;
            Value = value;
            Logic = logic;
        }

        /// <summary>
        /// 将当前条件渲染为 SQL 片段，并填充对应的参数列表
        /// </summary>
        /// <param name="config">数据库配置（用于确定参数前缀、方言等）</param>
        /// <param name="parameters">输出参数列表</param>
        /// <param name="paramIndex">参数索引起点（避免不同条件间参数名冲突）</param>
        /// <returns>渲染完成的 SQL 片段（不含 AND/OR 前缀）</returns>
        public string Render(ConfigModel config, List<DbParameter> parameters, ref int paramIndex)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var flag = config.Flag ?? "@";
            var factory = DbProviderFactories.GetFactory(config.ProviderName);
            var sb = new StringBuilder();
            var quotedField = QuoteIdentifier(Field);

            switch (Operator)
            {
                case ConditionOperator.Equal:
                    AppendScalar(sb, quotedField, "=", Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.NotEqual:
                    AppendScalar(sb, quotedField, "<>", Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.GreaterThan:
                    AppendScalar(sb, quotedField, ">", Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.GreaterThanOrEqual:
                    AppendScalar(sb, quotedField, ">=", Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.LessThan:
                    AppendScalar(sb, quotedField, "<", Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.LessThanOrEqual:
                    AppendScalar(sb, quotedField, "<=", Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.Like:
                    AppendScalar(sb, quotedField, "LIKE", Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.NotLike:
                    AppendScalar(sb, quotedField, "NOT LIKE", Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.Contains:
                    AppendLikeWrapped(sb, quotedField, Value, "%", "%", flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.StartsWith:
                    AppendLikeWrapped(sb, quotedField, Value, "", "%", flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.EndsWith:
                    AppendLikeWrapped(sb, quotedField, Value, "%", "", flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.In:
                case ConditionOperator.NotIn:
                    AppendInList(sb, quotedField, Operator == ConditionOperator.In, Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.Between:
                case ConditionOperator.NotBetween:
                    AppendBetween(sb, quotedField, Operator == ConditionOperator.Between, Value, flag, parameters, ref paramIndex, factory);
                    break;

                case ConditionOperator.IsNull:
                    sb.Append(quotedField).Append(" IS NULL");
                    break;

                case ConditionOperator.IsNotNull:
                    sb.Append(quotedField).Append(" IS NOT NULL");
                    break;

                default:
                    throw new NotSupportedException(string.Format("不支持的条件操作符: {0}", Operator));
            }

            return sb.ToString();
        }

        #region 私有辅助方法

        /// <summary>
        /// 追加标量比较：field <op> @pN
        /// </summary>
        private static void AppendScalar(StringBuilder sb, string field, string op, object value,
            string flag, List<DbParameter> parameters, ref int index, DbProviderFactory factory)
        {
            var paramName = string.Format("{0}p{1}", flag, index++);
            sb.Append(field).Append(' ').Append(op).Append(' ').Append(paramName);
            parameters.Add(CreateParameter(factory, paramName, value));
        }

        /// <summary>
        /// 追加 LIKE 包装：在值两端附加通配符后参数化
        /// </summary>
        private static void AppendLikeWrapped(StringBuilder sb, string field, object value,
            string prefix, string suffix, string flag, List<DbParameter> parameters,
            ref int index, DbProviderFactory factory)
        {
            var raw = value?.ToString() ?? string.Empty;
            var wrapped = string.Concat(prefix, raw, suffix);
            var paramName = string.Format("{0}p{1}", flag, index++);
            sb.Append(field).Append(" LIKE ").Append(paramName);
            parameters.Add(CreateParameter(factory, paramName, wrapped));
        }

        /// <summary>
        /// 追加 IN 列表：field IN (@p0,@p1,...) - 全部参数化
        /// </summary>
        private static void AppendInList(StringBuilder sb, string field, bool isIn, object value,
            string flag, List<DbParameter> parameters, ref int index, DbProviderFactory factory)
        {
            if (!(value is IEnumerable enumerable) || value is string)
                throw new ArgumentException("IN/NOT IN 条件需要 IEnumerable 类型的值", nameof(value));

            var placeholders = new List<string>();
            foreach (var item in enumerable)
            {
                var paramName = string.Format("{0}p{1}", flag, index++);
                placeholders.Add(paramName);
                parameters.Add(CreateParameter(factory, paramName, item ?? DBNull.Value));
            }

            if (placeholders.Count == 0)
            {
                // 空集合：IN 永远不匹配，NOT IN 永远匹配
                sb.Append(isIn ? "1=0" : "1=1");
                return;
            }

            sb.Append(field).Append(isIn ? " IN (" : " NOT IN (")
              .Append(string.Join(",", placeholders))
              .Append(')');
        }

        /// <summary>
        /// 追加 BETWEEN 区间：field BETWEEN @pStart AND @pEnd
        /// <para>
        /// 接受任何 <see cref="IEnumerable"/>，取前两个元素分别作为 start / end。
        /// 这样的设计在 net452 上不依赖 <c>ITuple</c>，对调用方也足够灵活。
        /// </para>
        /// </summary>
        private static void AppendBetween(StringBuilder sb, string field, bool isBetween, object value,
            string flag, List<DbParameter> parameters, ref int index, DbProviderFactory factory)
        {
            if (!(value is IEnumerable enumerable) || value is string)
                throw new ArgumentException("BETWEEN 条件需要包含 2 个元素的集合", nameof(value));

            object startVal = null;
            object endVal = null;
            var count = 0;
            foreach (var item in enumerable)
            {
                if (count == 0) startVal = item;
                else if (count == 1) { endVal = item; }
                count++;
                if (count >= 2) break;
            }

            if (count < 2)
                throw new ArgumentException("BETWEEN 条件需要包含至少 2 个元素", nameof(value));

            var startName = string.Format("{0}p{1}", flag, index++);
            var endName = string.Format("{0}p{1}", flag, index++);
            sb.Append(field).Append(isBetween ? " BETWEEN " : " NOT BETWEEN ")
              .Append(startName).Append(" AND ").Append(endName);

            parameters.Add(CreateParameter(factory, startName, startVal ?? DBNull.Value));
            parameters.Add(CreateParameter(factory, endName, endVal ?? DBNull.Value));
        }

        /// <summary>
        /// 创建数据库参数
        /// </summary>
        private static DbParameter CreateParameter(DbProviderFactory factory, string name, object value)
        {
            var p = factory.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            return p;
        }

        /// <summary>
        /// 字段名引用：当前为简单字段名（不带特殊字符），原样返回；
        /// 保留扩展点：未来如果需要支持带特殊字符的字段名可在此处加强。
        /// </summary>
        private static string QuoteIdentifier(string field)
        {
            return field;
        }

        #endregion
    }
}
