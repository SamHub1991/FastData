using System;
using System.Linq.Expressions;

namespace FastData.Base
{
    /// <summary>
    /// 条件表达式辅助工具
    /// <para>
    /// 集中处理"从 Expression&lt;Func&lt;T, object&gt;&gt; 中提取字段名"的逻辑，
    /// 避免在多个调用方重复实现，统一行为。
    /// </para>
    /// </summary>
    public static class ConditionExpression
    {
        /// <summary>
        /// 从形如 <c>x =&gt; x.UserName</c> 的表达式中提取字段名
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="expression">成员访问表达式</param>
        /// <returns>字段名称</returns>
        /// <exception cref="ArgumentException">当表达式不是成员访问时抛出</exception>
        public static string GetMemberName<T>(Expression<Func<T, object>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            if (expression.Body is MemberExpression member)
                return member.Member.Name;

            if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
                return unaryMember.Member.Name;

            throw new ArgumentException("表达式必须是成员访问表达式，例如：x => x.UserName");
        }
    }
}
