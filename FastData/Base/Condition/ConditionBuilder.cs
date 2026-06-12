using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using FastData.Model;

namespace FastData.Base
{
    /// <summary>
    /// 条件 Fluent Builder
    /// <para>
    /// 借鉴 Dos.ORM 的 WhereClipBuilder 思路：维护一个 <see cref="Condition"/> 列表，
    /// 提供链式 API（Equal/And/Or/Like/In/Between/IsNull 等）按需添加条件，
    /// 最终通过 <see cref="Build(out List{DbParameter})"/> 统一渲染为带参数的 SQL 子句。
    /// </para>
    /// <para>
    /// 优势：
    /// <list type="number">
    /// <item>所有值走 <see cref="DbParameter"/>，彻底杜绝 SQL 注入</item>
    /// <item>操作符集中枚举，新增条件类型不影响调用代码</item>
    /// <item>同时支持 AND/OR 逻辑切换与嵌套分组</item>
    /// <item>与现有 <see cref="DataQuery.ChainedConditions"/> 兼容</item>
    /// </list>
    /// </para>
    /// <para>
    /// 使用示例：
    /// <code>
    /// var builder = new ConditionBuilder(config)
    ///     .Equal(u =&gt; u.Age, 18)
    ///     .And()
    ///     .Contains(u =&gt; u.Name, "张")
    ///     .Or()
    ///     .In(u =&gt; u.Status, new object[] { 1, 2, 3 })
    ///     .And()
    ///     .Between(u =&gt; u.CreateTime, start, end);
    ///
    /// var whereClause = builder.Build(out var parameters);
    /// </code>
    /// </para>
    /// </summary>
    public sealed class ConditionBuilder
    {
        private readonly ConfigModel _config;
        private readonly List<Condition> _conditions = new List<Condition>();
        private ConditionLogic _pendingLogic = ConditionLogic.And;

        /// <summary>
        /// 构造条件构建器
        /// </summary>
        /// <param name="config">数据库配置（用于确定参数前缀、Provider 等）</param>
        public ConditionBuilder(ConfigModel config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 当前已添加的条件数量
        /// </summary>
        public int Count => _conditions.Count;

        /// <summary>
        /// 将下一次条件以 AND 方式连接（默认）
        /// </summary>
        public ConditionBuilder And()
        {
            _pendingLogic = ConditionLogic.And;
            return this;
        }

        /// <summary>
        /// 将下一次条件以 OR 方式连接
        /// </summary>
        public ConditionBuilder Or()
        {
            _pendingLogic = ConditionLogic.Or;
            return this;
        }

        #region 标量比较

        /// <summary>添加等于条件</summary>
        public ConditionBuilder Equal<T>(Expression<Func<T, object>> field, object value)
            => Add(ConditionOperator.Equal, field, value);

        /// <summary>添加不等于条件</summary>
        public ConditionBuilder NotEqual<T>(Expression<Func<T, object>> field, object value)
            => Add(ConditionOperator.NotEqual, field, value);

        /// <summary>添加大于条件</summary>
        public ConditionBuilder GreaterThan<T>(Expression<Func<T, object>> field, object value)
            => Add(ConditionOperator.GreaterThan, field, value);

        /// <summary>添加大于等于条件</summary>
        public ConditionBuilder GreaterThanOrEqual<T>(Expression<Func<T, object>> field, object value)
            => Add(ConditionOperator.GreaterThanOrEqual, field, value);

        /// <summary>添加小于条件</summary>
        public ConditionBuilder LessThan<T>(Expression<Func<T, object>> field, object value)
            => Add(ConditionOperator.LessThan, field, value);

        /// <summary>添加小于等于条件</summary>
        public ConditionBuilder LessThanOrEqual<T>(Expression<Func<T, object>> field, object value)
            => Add(ConditionOperator.LessThanOrEqual, field, value);

        #endregion

        #region 模糊匹配

        /// <summary>添加 LIKE 条件（值需自行包含 % 通配符）</summary>
        public ConditionBuilder Like<T>(Expression<Func<T, object>> field, string value)
            => Add(ConditionOperator.Like, field, value);

        /// <summary>添加 NOT LIKE 条件</summary>
        public ConditionBuilder NotLike<T>(Expression<Func<T, object>> field, string value)
            => Add(ConditionOperator.NotLike, field, value);

        /// <summary>添加包含子串条件（LIKE '%value%'）</summary>
        public ConditionBuilder Contains<T>(Expression<Func<T, object>> field, string value)
            => Add(ConditionOperator.Contains, field, value);

        /// <summary>添加开头匹配条件（LIKE 'value%'）</summary>
        public ConditionBuilder StartsWith<T>(Expression<Func<T, object>> field, string value)
            => Add(ConditionOperator.StartsWith, field, value);

        /// <summary>添加结尾匹配条件（LIKE '%value'）</summary>
        public ConditionBuilder EndsWith<T>(Expression<Func<T, object>> field, string value)
            => Add(ConditionOperator.EndsWith, field, value);

        #endregion

        #region 集合匹配

        /// <summary>
        /// 添加 IN 条件
        /// </summary>
        /// <param name="field">字段表达式</param>
        /// <param name="values">值集合（可空集合会被视作"无匹配"或"全部匹配"语义）</param>
        public ConditionBuilder In<T>(Expression<Func<T, object>> field, IEnumerable values)
        {
            ValidateValueArrayNotNull(field, values);
            return Add(ConditionOperator.In, field, NormalizeToArray(values));
        }

        /// <summary>添加 NOT IN 条件</summary>
        public ConditionBuilder NotIn<T>(Expression<Func<T, object>> field, IEnumerable values)
        {
            ValidateValueArrayNotNull(field, values);
            return Add(ConditionOperator.NotIn, field, NormalizeToArray(values));
        }

        #endregion

        #region 区间匹配

        /// <summary>
        /// 添加 BETWEEN 条件（闭区间）
        /// </summary>
        public ConditionBuilder Between<T>(Expression<Func<T, object>> field, object start, object end)
        {
            var fieldName = ConditionExpression.GetMemberName(field);
            // 将 (start, end) 包装为 List，确保 AppendBetween 能通过 IEnumerable 路径解析两个边界值
            _conditions.Add(new Condition(fieldName, ConditionOperator.Between, new List<object> { start, end }, _pendingLogic));
            _pendingLogic = ConditionLogic.And;
            return this;
        }

        /// <summary>添加 NOT BETWEEN 条件</summary>
        public ConditionBuilder NotBetween<T>(Expression<Func<T, object>> field, object start, object end)
        {
            var fieldName = ConditionExpression.GetMemberName(field);
            _conditions.Add(new Condition(fieldName, ConditionOperator.NotBetween, new List<object> { start, end }, _pendingLogic));
            _pendingLogic = ConditionLogic.And;
            return this;
        }

        #endregion

        #region 空值判断

        /// <summary>添加 IS NULL 条件</summary>
        public ConditionBuilder IsNull<T>(Expression<Func<T, object>> field)
            => Add(ConditionOperator.IsNull, field, null);

        /// <summary>添加 IS NOT NULL 条件</summary>
        public ConditionBuilder IsNotNull<T>(Expression<Func<T, object>> field)
            => Add(ConditionOperator.IsNotNull, field, null);

        #endregion

        #region 内部添加

        /// <summary>
        /// 通用添加入口：处理字段名解析、逻辑状态重置
        /// </summary>
        private ConditionBuilder Add<T>(ConditionOperator op, Expression<Func<T, object>> field, object value)
        {
            var fieldName = ConditionExpression.GetMemberName(field);
            _conditions.Add(new Condition(fieldName, op, value, _pendingLogic));
            _pendingLogic = ConditionLogic.And;
            return this;
        }

        private static void ValidateValueArrayNotNull<T>(Expression<Func<T, object>> field, IEnumerable values)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (values == null) throw new ArgumentNullException(nameof(values), "IN 条件值集合不能为空");
        }

        private static object NormalizeToArray(IEnumerable values)
        {
            var list = new List<object>();
            foreach (var item in values)
                list.Add(item);
            return list;
        }

        #endregion

        /// <summary>
        /// 渲染为最终的 SQL 子句和参数列表
        /// </summary>
        /// <param name="parameters">输出的数据库参数集合（已绑定顺序）</param>
        /// <returns>渲染完成的 WHERE 子句；无任何条件时返回空字符串</returns>
        public string Build(out List<DbParameter> parameters)
        {
            parameters = new List<DbParameter>();
            if (_conditions.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            var paramIndex = 0;
            for (var i = 0; i < _conditions.Count; i++)
            {
                var c = _conditions[i];
                if (i == 0)
                {
                    // 第一个条件：仅当显式为 OR 时输出 OR 前缀（实际几乎不会发生）
                    if (c.Logic == ConditionLogic.Or) sb.Append("OR ");
                }
                else
                {
                    sb.Append(' ').Append(c.Logic == ConditionLogic.And ? "AND " : "OR ");
                }

                sb.Append(c.Render(_config, parameters, ref paramIndex));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 渲染并附加到指定的参数集合（避免新建 List，复用调用方容器）
        /// </summary>
        /// <param name="parameters">要追加到的参数列表</param>
        /// <returns>渲染完成的 WHERE 子句</returns>
        public string Build(List<DbParameter> parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            if (_conditions.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            var paramIndex = 0;
            for (var i = 0; i < _conditions.Count; i++)
            {
                var c = _conditions[i];
                if (i == 0)
                {
                    if (c.Logic == ConditionLogic.Or) sb.Append("OR ");
                }
                else
                {
                    sb.Append(' ').Append(c.Logic == ConditionLogic.And ? "AND " : "OR ");
                }

                sb.Append(c.Render(_config, parameters, ref paramIndex));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将当前构建器内的所有条件快照复制到外部链式条件列表。
        /// 用于将 <see cref="ConditionBuilder"/> 的结果挂载到 <see cref="FastData.Model.DataQuery.ChainedConditions"/>。
        /// </summary>
        /// <param name="target">外部条件列表（接收方）</param>
        /// <param name="outerLogic">与前一个外部条件的逻辑关系，默认 AND</param>
        public void AppendTo(List<FastData.Model.ChainedCondition> target, ConditionLogic outerLogic = ConditionLogic.And)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (_conditions.Count == 0) return;

            var chained = new FastData.Model.ChainedCondition
            {
                Operator = outerLogic == ConditionLogic.And ? "AND" : "OR",
                Where = string.Empty,
                Param = new List<System.Data.Common.DbParameter>(),
                Conditions = new List<Condition>(_conditions)
            };
            target.Add(chained);
        }
    }
}
