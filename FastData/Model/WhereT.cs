using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FastData.Base;

namespace FastData.Model
{
    /// <summary>
    /// 条件构建器 - 支持分开写条件
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
        /// 添加 LIKE 条件
        /// </summary>
        public Where<T> Like(Expression<Func<T, object>> field, string value)
        {
            if (field == null || string.IsNullOrEmpty(value))
                return this;

            var fieldName = GetMemberName(field);
            _conditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Format("{0} like '{1}'", fieldName, value)
            });

            return this;
        }

        /// <summary>
        /// 添加 Contains 条件（LIKE '%value%'）
        /// </summary>
        public Where<T> Contains(Expression<Func<T, object>> field, string value)
        {
            return Like(field, string.Format("%{0}%", value));
        }

        /// <summary>
        /// 添加 StartsWith 条件（LIKE 'value%'）
        /// </summary>
        public Where<T> StartsWith(Expression<Func<T, object>> field, string value)
        {
            return Like(field, string.Format("{0}%", value));
        }

        /// <summary>
        /// 添加 EndsWith 条件（LIKE '%value'）
        /// </summary>
        public Where<T> EndsWith(Expression<Func<T, object>> field, string value)
        {
            return Like(field, string.Format("%{0}", value));
        }

        /// <summary>
        /// 添加 IN 条件
        /// </summary>
        public Where<T> In(Expression<Func<T, object>> field, IEnumerable<object> values)
        {
            if (field == null || values == null)
                return this;

            var fieldName = GetMemberName(field);
            var valueList = values.Select(v => string.Format("'{0}'", v)).ToArray();
            _conditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Format("{0} in ({1})", fieldName, string.Join(",", valueList))
            });

            return this;
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
            if (field == null)
                return this;

            var fieldName = GetMemberName(field);
            _conditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Format("{0} between '{1}' and '{2}'", fieldName, start, end)
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

        /// <summary>
        /// 从表达式中获取成员名称
        /// </summary>
        private static string GetMemberName(Expression<Func<T, object>> expression)
        {
            if (expression.Body is MemberExpression member)
                return member.Member.Name;

            if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
                return unaryMember.Member.Name;

            throw new ArgumentException("表达式必须是成员访问表达式");
        }
    }
}
