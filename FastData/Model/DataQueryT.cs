using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using FastData.Base;
using FastUntility.Page;

namespace FastData.Model
{
    #region 泛型查询
    /// <summary>
    /// 泛型查询类 - 支持链式调用且只需指定一次类型
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public sealed class DataQuery<T> : DataQuery where T : class, new()
    {
        /// <summary>
        /// 链式 Where 条件（AND）
        /// </summary>
        public DataQuery<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                return this;

            var visitModel = VisitExpression.LambdaWhere<T>(predicate, Config);
            if (visitModel.IsSuccess)
            {
                ChainedConditions.Add(new ChainedCondition
                {
                    Operator = "AND",
                    Where = visitModel.Where,
                    Param = visitModel.Param
                });
            }

            return this;
        }

        /// <summary>
        /// 使用 Where&lt;T&gt; 条件构建器
        /// </summary>
        public DataQuery<T> Where(Where<T> where)
        {
            if (where == null)
                return this;

            where.SetConfig(Config);
            ChainedConditions.AddRange(where.Conditions);

            return this;
        }

        /// <summary>
        /// 链式 And 条件（Where 的别名）
        /// </summary>
        public DataQuery<T> And(Expression<Func<T, bool>> predicate)
        {
            return Where(predicate);
        }

        /// <summary>
        /// 链式 Or 条件
        /// </summary>
        public DataQuery<T> Or(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                return this;

            var visitModel = VisitExpression.LambdaWhere<T>(predicate, Config);
            if (visitModel.IsSuccess)
            {
                ChainedConditions.Add(new ChainedCondition
                {
                    Operator = "OR",
                    Where = visitModel.Where,
                    Param = visitModel.Param
                });
            }

            return this;
        }

        /// <summary>
        /// 链式 Like 条件
        /// </summary>
        public DataQuery<T> Like(Expression<Func<T, object>> field, string value)
        {
            if (field == null || string.IsNullOrEmpty(value))
                return this;

            var fieldName = GetMemberName(field);
            ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Format("{0} like '{1}'", fieldName, value)
            });

            return this;
        }

        /// <summary>
        /// 链式 Contains 条件（LIKE '%value%'）
        /// </summary>
        public DataQuery<T> Contains(Expression<Func<T, object>> field, string value)
        {
            return Like(field, string.Format("%{0}%", value));
        }

        /// <summary>
        /// 链式 StartsWith 条件（LIKE 'value%'）
        /// </summary>
        public DataQuery<T> StartsWith(Expression<Func<T, object>> field, string value)
        {
            return Like(field, string.Format("{0}%", value));
        }

        /// <summary>
        /// 链式 EndsWith 条件（LIKE '%value'）
        /// </summary>
        public DataQuery<T> EndsWith(Expression<Func<T, object>> field, string value)
        {
            return Like(field, string.Format("%{0}", value));
        }

        /// <summary>
        /// 链式 In 条件
        /// </summary>
        public DataQuery<T> In(Expression<Func<T, object>> field, IEnumerable<object> values)
        {
            if (field == null || values == null)
                return this;

            var fieldName = GetMemberName(field);
            var valueList = values.Select(v => string.Format("'{0}'", v)).ToArray();
            ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Format("{0} in ({1})", fieldName, string.Join(",", valueList))
            });

            return this;
        }

        /// <summary>
        /// 链式 Between 条件
        /// </summary>
        public DataQuery<T> Between(Expression<Func<T, object>> field, object start, object end)
        {
            if (field == null)
                return this;

            var fieldName = GetMemberName(field);
            ChainedConditions.Add(new ChainedCondition
            {
                Operator = "AND",
                Where = string.Format("{0} between '{1}' and '{2}'", fieldName, start, end)
            });

            return this;
        }

        /// <summary>
        /// 排序
        /// </summary>
        public new DataQuery<T> OrderBy(Expression<Func<T, object>> field)
        {
            if (field == null)
                return this;

            var fieldName = GetMemberName(field);
            base.OrderBy.Add(fieldName);

            return this;
        }

        /// <summary>
        /// 降序排序
        /// </summary>
        public DataQuery<T> OrderByDescending(Expression<Func<T, object>> field)
        {
            if (field == null)
                return this;

            var fieldName = GetMemberName(field);
            base.OrderBy.Add(string.Format("{0} desc", fieldName));

            return this;
        }

        /// <summary>
        /// 分组
        /// </summary>
        public new DataQuery<T> GroupBy(Expression<Func<T, object>> field)
        {
            if (field == null)
                return this;

            var fieldName = GetMemberName(field);
            base.GroupBy.Add(fieldName);

            return this;
        }

        /// <summary>
        /// 投影查询（支持匿名类型）
        /// </summary>
        public ProjectedQuery<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            return new ProjectedQuery<T, TResult>(this, selector);
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        public List<T> ToList()
        {
            return FastRead.ToList<T>(this);
        }

        /// <summary>
        /// 查询单条
        /// </summary>
        public T ToItem()
        {
            return FastRead.ToItem<T>(this);
        }

        /// <summary>
        /// 查询条数
        /// </summary>
        public int ToCount()
        {
            return FastRead.ToCount(this);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        public PaginationResult<T> ToPagination(int page, int pageSize)
        {
            return FastRead.ToPagination<T>(this, page, pageSize);
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
    #endregion
}
