using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FastData.Base;

namespace FastData.Model
{
    /// <summary>
    /// 条件构建器 - 支持分开写条件
    /// <para>
    /// 设计思路参考 Dos.ORM 的 WhereClipBuilder：在内存中累积条件列表，
    /// 所有条件使用 <see cref="Condition"/> 不可变值对象 + 参数化渲染，
    /// 彻底解决字符串拼接带来的 SQL 注入问题。
    /// </para>
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class Where<T> where T : class, new()
    {
        private readonly List<ChainedCondition> _conditions = new List<ChainedCondition>();
        private ConfigModel _config;

        /// <summary>
        /// 获取所有条件
        /// </summary>
        internal List<ChainedCondition> Conditions => _conditions;

        /// <summary>
        /// 设置配置（由 DataQuery 调用）
        /// </summary>
        internal void SetConfig(ConfigModel config)
        {
            _config = config;
        }

        /// <summary>
        /// 添加 AND 条件
        /// </summary>
        public Where<T> Add(Expression<Func<T, bool>> predicate)
        {
            return Add("AND", predicate);
        }

        /// <summary>
        /// 添加 AND 条件（别名）
        /// </summary>
        public Where<T> And(Expression<Func<T, bool>> predicate)
        {
            return Add("AND", predicate);
        }

        /// <summary>
        /// 添加 OR 条件
        /// </summary>
        public Where<T> Or(Expression<Func<T, bool>> predicate)
        {
            return Add("OR", predicate);
        }

        /// <summary>
        /// 添加 LIKE 条件（参数化）
        /// </summary>
        public Where<T> Like(Expression<Func<T, object>> field, string value)
        {
            return AppendCondition(ConditionOperator.Like, field, value);
        }

        /// <summary>
        /// 添加 NOT LIKE 条件
        /// </summary>
        public Where<T> NotLike(Expression<Func<T, object>> field, string value)
        {
            return AppendCondition(ConditionOperator.NotLike, field, value);
        }

        /// <summary>
        /// 添加 Contains 条件（LIKE '%value%'）
        /// </summary>
        public Where<T> Contains(Expression<Func<T, object>> field, string value)
        {
            return AppendCondition(ConditionOperator.Contains, field, value);
        }

        /// <summary>
        /// 添加 StartsWith 条件（LIKE 'value%'）
        /// </summary>
        public Where<T> StartsWith(Expression<Func<T, object>> field, string value)
        {
            return AppendCondition(ConditionOperator.StartsWith, field, value);
        }

        /// <summary>
        /// 添加 EndsWith 条件（LIKE '%value'）
        /// </summary>
        public Where<T> EndsWith(Expression<Func<T, object>> field, string value)
        {
            return AppendCondition(ConditionOperator.EndsWith, field, value);
        }

        /// <summary>
        /// 添加 IN 条件
        /// </summary>
        public Where<T> In(Expression<Func<T, object>> field, IEnumerable<object> values)
        {
            if (field == null || values == null) return this;
            var list = new List<object>();
            foreach (var v in values) list.Add(v);
            return AppendCondition(ConditionOperator.In, field, list);
        }

        /// <summary>
        /// 添加 NOT IN 条件
        /// </summary>
        public Where<T> NotIn(Expression<Func<T, object>> field, IEnumerable<object> values)
        {
            if (field == null || values == null) return this;
            var list = new List<object>();
            foreach (var v in values) list.Add(v);
            return AppendCondition(ConditionOperator.NotIn, field, list);
        }

        /// <summary>
        /// 添加 BETWEEN 条件
        /// </summary>
        /// <param name="field">字段表达式</param>
        /// <param name="start">起始值</param>
        /// <param name="end">结束值</param>
        /// <returns>Where构建器</returns>
        public Where<T> Between(Expression<Func<T, object>> field, object start, object end)
        {
            if (field == null) return this;
            var fieldName = ConditionExpression.GetMemberName(field);
            _conditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Empty,
                Param = new List<System.Data.Common.DbParameter>(),
                Conditions = new List<Condition>
                {
                    new Condition(fieldName, ConditionOperator.Between, (start, end), ConditionLogic.And)
                }
            });
            return this;
        }

        /// <summary>添加 NOT BETWEEN 条件</summary>
        public Where<T> NotBetween(Expression<Func<T, object>> field, object start, object end)
        {
            if (field == null) return this;
            var fieldName = ConditionExpression.GetMemberName(field);
            _conditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Empty,
                Param = new List<System.Data.Common.DbParameter>(),
                Conditions = new List<Condition>
                {
                    new Condition(fieldName, ConditionOperator.NotBetween, (start, end), ConditionLogic.And)
                }
            });
            return this;
        }

        /// <summary>添加 IS NULL 条件</summary>
        public Where<T> IsNull(Expression<Func<T, object>> field)
        {
            return AppendCondition(ConditionOperator.IsNull, field, null);
        }

        /// <summary>添加 IS NOT NULL 条件</summary>
        public Where<T> IsNotNull(Expression<Func<T, object>> field)
        {
            return AppendCondition(ConditionOperator.IsNotNull, field, null);
        }

        /// <summary>添加等于条件</summary>
        public Where<T> Equal(Expression<Func<T, object>> field, object value)
        {
            return AppendCondition(ConditionOperator.Equal, field, value);
        }

        /// <summary>添加不等于条件</summary>
        public Where<T> NotEqual(Expression<Func<T, object>> field, object value)
        {
            return AppendCondition(ConditionOperator.NotEqual, field, value);
        }

        /// <summary>添加大于条件</summary>
        public Where<T> GreaterThan(Expression<Func<T, object>> field, object value)
        {
            return AppendCondition(ConditionOperator.GreaterThan, field, value);
        }

        /// <summary>添加大于等于条件</summary>
        public Where<T> GreaterThanOrEqual(Expression<Func<T, object>> field, object value)
        {
            return AppendCondition(ConditionOperator.GreaterThanOrEqual, field, value);
        }

        /// <summary>添加小于条件</summary>
        public Where<T> LessThan(Expression<Func<T, object>> field, object value)
        {
            return AppendCondition(ConditionOperator.LessThan, field, value);
        }

        /// <summary>添加小于等于条件</summary>
        public Where<T> LessThanOrEqual(Expression<Func<T, object>> field, object value)
        {
            return AppendCondition(ConditionOperator.LessThanOrEqual, field, value);
        }

        /// <summary>
        /// 内部统一添加条件（参数化）
        /// </summary>
        private Where<T> AppendCondition(ConditionOperator op, Expression<Func<T, object>> field, object value)
        {
            if (field == null) return this;
            var fieldName = ConditionExpression.GetMemberName(field);
            _conditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Empty,
                Param = new List<System.Data.Common.DbParameter>(),
                Conditions = new List<Condition>
                {
                    new Condition(fieldName, op, value, ConditionLogic.And)
                }
            });
            return this;
        }

        /// <summary>
        /// 内部添加条件
        /// </summary>
        /// <param name="op">操作符</param>
        /// <param name="predicate">条件表达式</param>
        /// <returns>Where构建器</returns>
        private Where<T> Add(string op, Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                return this;

            var visitModel = VisitExpression.LambdaWhere<T>(predicate, _config);
            if (visitModel.IsSuccess)
            {
                _conditions.Add(new ChainedCondition
                {
                    Operator = op,
                    Where = visitModel.Where,
                    Param = visitModel.Param
                });
            }

            return this;
        }
    }
}
