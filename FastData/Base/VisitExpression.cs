using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Data.Common;
using System.Text;
using FastUntility.Base;
using FastData.DbTypes;
using FastData.Infrastructure;
using FastData.Model;
using System.Data;
using System.Linq;

namespace FastData.Base
{
    /// <summary>
    /// Lambda 表达式解析器
    /// 将 LINQ 表达式树解析为 SQL WHERE 条件语句和参数集合
    /// </summary>
    public static class VisitExpression
    {
        /// <summary>
        /// 已编译表达式委托的缓存，避免重复调用 Expression.Compile() 产生 IL 开销
        /// 键为表达式字符串表示，值为编译后的委托
        /// </summary>
        private static readonly ConcurrentDictionary<string, Delegate> _compiledCache =
            new ConcurrentDictionary<string, Delegate>();

        /// <summary>
        /// 缓存编译并执行表达式，避免同一表达式重复编译
        /// </summary>
        /// <param name="expression">待求值的表达式</param>
        /// <returns>表达式的运行时求值结果</returns>
        private static object EvaluateExpressionCached(Expression expression)
        {
            if (!CanCacheEvaluation(expression))
                return Expression.Lambda(expression).Compile().DynamicInvoke();

            var cacheKey = expression.ToString();
            var compiled = _compiledCache.GetOrAdd(cacheKey,
                _ => Expression.Lambda(expression).Compile());
            return compiled.DynamicInvoke();
        }

        private static bool CanCacheEvaluation(Expression expression)
        {
            if (expression == null)
                return true;

            if (expression is ConstantExpression constant)
                return IsStableConstant(constant.Value);

            if (expression is MemberExpression member)
                return CanCacheEvaluation(member.Expression);

            if (expression is MethodCallExpression methodCall)
            {
                if (!CanCacheEvaluation(methodCall.Object))
                    return false;

                foreach (var argument in methodCall.Arguments)
                {
                    if (!CanCacheEvaluation(argument))
                        return false;
                }

                return true;
            }

            if (expression is UnaryExpression unary)
                return CanCacheEvaluation(unary.Operand);

            if (expression is BinaryExpression binary)
                return CanCacheEvaluation(binary.Left) && CanCacheEvaluation(binary.Right);

            if (expression is NewArrayExpression array)
            {
                foreach (var item in array.Expressions)
                {
                    if (!CanCacheEvaluation(item))
                        return false;
                }

                return true;
            }

            return true;
        }

        private static bool IsStableConstant(object value)
        {
            if (value == null)
                return true;

            var type = value.GetType();
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(Guid);
        }

        #region 解析 Lambda 表达式为 SQL WHERE 条件
        /// <summary>
        /// 将 Lambda 表达式解析为 SQL WHERE 条件
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="expression">Lambda 表达式（如 u => u.Age > 18）</param>
        /// <param name="config">数据库配置（用于确定数据库方言）</param>
        /// <returns>VisitModel（包含 IsSuccess、解析后的 SQL、参数列表）</returns>
        public static VisitModel LambdaWhere<T>(Expression<Func<T, bool>> expression, ConfigModel config)
        {
            var result = new VisitModel();

            if (expression == null)
                return result;

            var leftList = new List<string>();
            var rightList = new List<string>();
            var typeList = new List<Type>();
            var sqlBuilder = new StringBuilder();
            int parameterIndex = 0;

            try
            {
                var whereClause = ParseExpression(config, expression.Body, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex);
                whereClause = TrimTrailingOperators(whereClause);

                result.Where = whereClause;

                // 构建参数列表
                var dbFactory = DbProviderAutoRegistrar.GetFactory(config.ProviderName);
                for (int i = 0; i < leftList.Count; i++)
                {
                    var parameter = dbFactory.CreateParameter();
                    parameter.ParameterName = leftList[i] + i.ToString();
                    parameter.Value = ConvertToTypedValue(rightList[i], typeList, i, config);
                    result.Param.Add(parameter);
                }

                result.IsSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                LogException(config, ex, "LambdaWhere<T>");
                result.IsSuccess = false;
                return result;
            }
        }
        #endregion

        #region 解析双表 Lambda 表达式
        /// <summary>
        /// 将双表 Lambda 表达式解析为 SQL WHERE 条件
        /// </summary>
        /// <typeparam name="T1">第一个表类型</typeparam>
        /// <typeparam name="T2">第二个表类型</typeparam>
        /// <param name="expression">条件表达式</param>
        /// <param name="config">数据库配置</param>
        /// <param name="isPaging">是否为分页查询</param>
        /// <returns>解析后的 VisitModel</returns>
        public static VisitModel LambdaWhere<T1, T2>(Expression<Func<T1, T2, bool>> expression, ConfigModel config, bool isPaging = false)
        {
            var result = new VisitModel();

            if (expression == null)
                return result;

            var leftList = new List<string>();
            var rightList = new List<string>();
            var typeList = new List<Type>();
            var sqlBuilder = new StringBuilder();
            int parameterIndex = 0;

            try
            {
                var whereClause = ParseExpression(config, expression.Body, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex);

                whereClause = TrimTrailingOperators(whereClause);
                result.Where = whereClause;

                // 构建参数列表
                var dbFactory = DbProviderAutoRegistrar.GetFactory(config.ProviderName);
                for (int i = 0; i < leftList.Count; i++)
                {
                    var parameter = dbFactory.CreateParameter();
                    parameter.ParameterName = leftList[i] + i.ToString();
                    parameter.Value = ConvertToTypedValue(rightList[i], typeList, i, config);
                    result.Param.Add(parameter);
                }

                result.IsSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                LogException(config, ex, "LambdaWhere<T1, T2>");
                result.IsSuccess = false;
                return result;
            }
        }
        #endregion

        #region 递归解析表达式
        /// <summary>
        /// 递归解析表达式为 SQL 片段
        /// </summary>
        /// <param name="config">数据库配置</param>
        /// <param name="expression">待解析的表达式</param>
        /// <param name="leftList">左侧字段列表（输出）</param>
        /// <param name="rightList">右侧值列表（输出）</param>
        /// <param name="typeList">类型列表（输出）</param>
        /// <param name="sqlBuilder">SQL 拼接构建器（输出）</param>
        /// <param name="parameterIndex">参数索引（输出）</param>
        /// <param name="isRightOperand">是否为比较运算符的右操作数</param>
        /// <returns>SQL 片段字符串</returns>
        private static string ParseExpression(ConfigModel config, Expression expression, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex, bool isRightOperand = false)
        {
            if (expression is BinaryExpression binary)
                return ParseBinaryExpression(config, binary.Left, binary.Right, binary.NodeType, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex, isRightOperand);

            if (expression is MemberExpression memberExp)
                return ParseMemberExpression(config, memberExp, typeList);

            if (expression is MethodCallExpression methodCall)
                return ParseMethodCallExpression(config, methodCall, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex, isRightOperand);

            if (expression is NewArrayExpression arrayExp)
                return ParseArrayExpression(config, arrayExp, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex, isRightOperand);

            if (expression is ConstantExpression constant)
                return ParseConstantExpression(constant, typeList);

            if (expression is UnaryExpression unary)
                return ParseExpression(config, unary.Operand, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex, isRightOperand);

            return TryEvaluateExpression(expression, typeList);
        }
        #endregion

        #region 解析二元表达式
        /// <summary>
        /// 解析二元运算表达式（比较、逻辑运算）
        /// </summary>
        private static string ParseBinaryExpression(ConfigModel config, Expression left, Expression right, ExpressionType operatorType, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex, bool isRightOperand)
        {
            // 特殊处理：布尔属性 == true/false 简化为直接的值
            if (operatorType == ExpressionType.Equal && left is MemberExpression leftMember && leftMember.Type == typeof(bool) && right is ConstantExpression rightConst && rightConst.Value is bool)
            {
                var memberName = leftMember.Member.Name;
                var boolValue = (bool)rightConst.Value;
                var boolSql = config.DbType == DataDbType.PostgreSql
                    ? string.Format("{0}={1}", memberName, boolValue ? "true" : "false")
                    : string.Format("{0}={1}", memberName, boolValue ? "1" : "0");
                sqlBuilder.Append(boolSql);
                return sqlBuilder.ToString();
            }

            var leftSql = ParseExpression(config, left, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex, isRightOperand: false);
            var operatorSymbol = GetOperatorSymbol(operatorType);
            var needsParameter = "=,>,<,>=,<=,<>".Contains(operatorSymbol);

            if (!needsParameter)
            {
                sqlBuilder.Append(string.Format(" {0} ", operatorSymbol));
            }

            var rightSql = ParseExpression(config, right, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex, needsParameter);

            // 处理 null 比较
            if (rightSql != null && string.Equals(rightSql, "NULL", StringComparison.OrdinalIgnoreCase) || (config.DbType == DataDbType.Oracle && string.IsNullOrEmpty(rightSql)))
            {
                var nullSql = operatorSymbol == "=" ? "IS NULL" : "IS NOT NULL";
                if (left is MemberExpression memberLeft)
                {
                    var paramName = GetParameterName(memberLeft.Expression as ParameterExpression);
                    sqlBuilder.Append(string.Format("{0}.{1} {2} ", paramName, leftSql, nullSql));
                }
            }
            else if (needsParameter)
            {
                // 构建带参数的比较表达式
                if (left is MemberExpression leftField && right is MemberExpression rightField && rightField.Expression is ParameterExpression)
                {
                    var leftParam = GetParameterName(leftField.Expression as ParameterExpression);
                    var rightParam = GetParameterName(rightField.Expression as ParameterExpression);
                    sqlBuilder.Append(string.Format("{0}.{1}{2}{3}.{4} ", leftParam, leftSql, operatorSymbol, rightParam, rightSql));
                }
                else if (left is MemberExpression valueLeft)
                {
                    var paramName = GetParameterName(valueLeft.Expression as ParameterExpression);
                    sqlBuilder.Append(string.Format("{0}.{1}{2}{3}{1}{4} ", paramName, leftSql, operatorSymbol, config.Flag, parameterIndex));
                    rightList.Add(rightSql);
                    leftList.Add(leftSql);
                    parameterIndex++;
                }
            }

            return sqlBuilder.ToString();
        }
        #endregion

        #region 解析属性表达式
        /// <summary>
        /// 解析属性/字段访问表达式
        /// </summary>
        private static string ParseMemberExpression(ConfigModel config, MemberExpression memberExp, List<Type> typeList)
        {
            // 情况1：直接属性访问（如 e.Name）
            if (memberExp.Expression is ParameterExpression)
            {
                var memberName = memberExp.Member.Name;

                // 布尔属性作为条件时直接返回值
                if (memberExp.Type == typeof(bool))
                {
                    return config.DbType == DataDbType.PostgreSql
                        ? string.Format("{0}=true", memberName)
                        : string.Format("{0}=1", memberName);
                }

                return memberName;
            }

            // 情况2：嵌套属性访问（如 e.Salary.HasValue）
            if (memberExp.Expression is MemberExpression innerMember)
            {
                // HasValue 转换为 IS NOT NULL
                if (memberExp.Member.Name == "HasValue" && innerMember.Expression is ParameterExpression)
                {
                    return string.Format("{0} IS NOT NULL", innerMember.Member.Name);
                }
            }

            // 其他情况：尝试直接求值
            return TryEvaluateExpression(memberExp, typeList);
        }
        #endregion

        #region 解析方法调用表达式
        /// <summary>
        /// 解析方法调用表达式（Contains、StartsWith、EndsWith 等）
        /// </summary>
        private static string ParseMethodCallExpression(ConfigModel config, MethodCallExpression methodCall, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex, bool isRightOperand)
        {
            if (isRightOperand)
            {
                var result = EvaluateExpressionCached(methodCall);
                typeList.Add(result.GetType());
                return result.ToString();
            }

            var methodName = methodCall.Method.Name.ToLowerInvariant();
            var aliasPrefix = GetTableAlias(methodCall.Object);

            try
            {
                // 确保 Object 是 MemberExpression 再访问其成员
                if (methodCall.Object is MemberExpression memberExp)
                {
                    var memberName = memberExp.Member.Name;

                    if (methodName == "contains")
                        return ParseContainsMethod(config, memberName, aliasPrefix, methodCall, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex);

                    if (methodName == "endswith")
                        return ParseEndsWithMethod(config, memberName, aliasPrefix, methodCall, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex);

                    if (methodName == "startswith")
                        return ParseStartsWithMethod(config, memberName, aliasPrefix, methodCall, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex);

                    if (methodName == "substring")
                        return ParseSubstringMethod(config, memberName, aliasPrefix, methodCall, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex);

                    if (methodName == "toupper")
                        return ParseToUpperMethod(config, memberName, aliasPrefix, methodCall, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex);

                    if (methodName == "tolower")
                        return ParseToLowerMethod(config, memberName, aliasPrefix, methodCall, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                // 使用 config.DbType 而非硬编码 SqlServer
                LogException(config, ex, "ParseMethodCallExpression");
                return string.Empty;
            }
        }
        #endregion

        #region Contains 方法解析
        /// <summary>
        /// 解析 string.Contains() 为 SQL LIKE '%value%'
        /// </summary>
        private static string ParseContainsMethod(ConfigModel config, string memberName, string aliasPrefix, MethodCallExpression methodCall, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex)
        {
            var searchValue = EvaluateExpressionCached(methodCall.Arguments[0]).ToString();
            sqlBuilder.Append(string.Format(" {0}{1} LIKE {2}{1}{3}", aliasPrefix, memberName, config.Flag, parameterIndex));
            leftList.Add(memberName);
            rightList.Add(string.Format("%{0}%", searchValue));
            typeList.Add(typeof(string));
            parameterIndex++;
            return sqlBuilder.ToString();
        }
        #endregion

        #region EndsWith 方法解析
        /// <summary>
        /// 解析 string.EndsWith() 为 SQL LIKE '%value'
        /// </summary>
        private static string ParseEndsWithMethod(ConfigModel config, string memberName, string aliasPrefix, MethodCallExpression methodCall, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex)
        {
            var searchValue = EvaluateExpressionCached(methodCall.Arguments[0]).ToString();
            sqlBuilder.Append(string.Format(" {0}{1} LIKE {2}{1}{3}", aliasPrefix, memberName, config.Flag, parameterIndex));
            leftList.Add(memberName);
            rightList.Add(string.Format("%{0}", searchValue));
            typeList.Add(typeof(string));
            parameterIndex++;
            return sqlBuilder.ToString();
        }
        #endregion

        #region StartsWith 方法解析
        /// <summary>
        /// 解析 string.StartsWith() 为 SQL LIKE 'value%'
        /// </summary>
        private static string ParseStartsWithMethod(ConfigModel config, string memberName, string aliasPrefix, MethodCallExpression methodCall, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex)
        {
            var searchValue = EvaluateExpressionCached(methodCall.Arguments[0]).ToString();
            sqlBuilder.Append(string.Format(" {0}{1} LIKE {2}{1}{3}", aliasPrefix, memberName, config.Flag, parameterIndex));
            leftList.Add(memberName);
            rightList.Add(string.Format("{0}%", searchValue));
            typeList.Add(typeof(string));
            parameterIndex++;
            return sqlBuilder.ToString();
        }
        #endregion

        #region Substring 方法解析
        /// <summary>
        /// 解析 string.Substring() 为 SQL SUBSTRING/SUBSTR
        /// </summary>
        private static string ParseSubstringMethod(ConfigModel config, string memberName, string aliasPrefix, MethodCallExpression methodCall, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex)
        {
            var startIndex = EvaluateExpressionCached(methodCall.Arguments[0]).ToString();
            var length = methodCall.Arguments.Count > 1
                ? EvaluateExpressionCached(methodCall.Arguments[1]).ToString()
                : string.Empty;

            var funcName = config.DbType == DataDbType.SqlServer ? "SUBSTRING" : "SUBSTR";
            sqlBuilder.Append(string.Format(" {0}({1}{2},{3},{4}) = {5}{2}{6}", funcName, aliasPrefix, memberName, startIndex, length, config.Flag, parameterIndex));
            leftList.Add(memberName);
            parameterIndex++;
            return sqlBuilder.ToString();
        }
        #endregion

        #region ToUpper 方法解析
        /// <summary>
        /// 解析 string.ToUpper() 为 SQL UPPER()
        /// </summary>
        private static string ParseToUpperMethod(ConfigModel config, string memberName, string aliasPrefix, MethodCallExpression methodCall, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex)
        {
            sqlBuilder.Append(string.Format(" UPPER({0}{1}) = {2}{1}{3}", aliasPrefix, memberName, config.Flag, parameterIndex));
            leftList.Add(memberName);
            parameterIndex++;
            return sqlBuilder.ToString();
        }
        #endregion

        #region ToLower 方法解析
        /// <summary>
        /// 解析 string.ToLower() 为 SQL LOWER()
        /// </summary>
        private static string ParseToLowerMethod(ConfigModel config, string memberName, string aliasPrefix, MethodCallExpression methodCall, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex)
        {
            sqlBuilder.Append(string.Format(" LOWER({0}{1}) = {2}{1}{3}", aliasPrefix, memberName, config.Flag, parameterIndex));
            leftList.Add(memberName);
            parameterIndex++;
            return sqlBuilder.ToString();
        }
        #endregion

        #region 解析数组表达式
        /// <summary>
        /// 解析数组初始化表达式
        /// </summary>
        private static string ParseArrayExpression(ConfigModel config, NewArrayExpression arrayExp, ref List<string> leftList, ref List<string> rightList, ref List<Type> typeList, ref StringBuilder sqlBuilder, ref int parameterIndex, bool isRightOperand)
        {
            var sbArray = new StringBuilder();
            foreach (var expr in arrayExp.Expressions)
            {
                sbArray.Append(",").Append(ParseExpression(config, expr, ref leftList, ref rightList, ref typeList, ref sqlBuilder, ref parameterIndex, isRightOperand));
            }

            return sbArray.Length == 0 ? string.Empty : sbArray.Remove(0, 1).ToString();
        }
        #endregion

        #region 解析常量表达式
        /// <summary>
        /// 解析常量表达式
        /// </summary>
        private static string ParseConstantExpression(ConstantExpression constant, List<Type> typeList)
        {
            if (constant.Value == null)
            {
                typeList.Add(typeof(string));
                return "NULL";
            }

            if (constant.Value is bool)
            {
                typeList.Add(typeof(bool));
                return (bool)constant.Value ? "1=1" : "1=0";
            }

            typeList.Add(constant.Value.GetType());
            return constant.Value.ToString();
        }
        #endregion

        #region 尝试求值表达式
        /// <summary>
        /// 尝试编译并执行表达式获取值
        /// </summary>
        private static string TryEvaluateExpression(Expression expression, List<Type> typeList)
        {
            try
            {
                var value = EvaluateExpressionCached(expression);
                if (value == null)
                {
                    typeList.Add(typeof(string));
                    return "NULL";
                }

                typeList.Add(value.GetType());
                return value.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
        #endregion

        #region 获取运算符符号
        /// <summary>
        /// 将 ExpressionType 转换为 SQL 运算符
        /// </summary>
        private static string GetOperatorSymbol(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return "+";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return "-";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return "*";
                default:
                    return string.Empty;
            }
        }
        #endregion

        #region 获取参数前缀
        /// <summary>
        /// 从表达式获取表别名前缀（如 "u."）
        /// </summary>
        private static string GetTableAlias(Expression expression)
        {
            if (expression == null)
                return string.Empty;

            if (expression is MemberExpression member)
            {
                var param = member.Expression as ParameterExpression;
                return param != null ? param.Name + "." : string.Empty;
            }

            if (expression is UnaryExpression unary)
            {
                var innerMember = unary.Operand as MemberExpression;
                if (innerMember != null && innerMember.Expression is ParameterExpression param)
                    return param.Name + ".";
            }

            return string.Empty;
        }
        #endregion

        #region 获取参数名称
        /// <summary>
        /// 从 ParameterExpression 获取参数名
        /// </summary>
        private static string GetParameterName(ParameterExpression parameter)
        {
            return parameter != null ? parameter.Name : string.Empty;
        }
        #endregion

        #region 转换类型化值
        /// <summary>
        /// 将字符串值转换为对应数据库参数的类型化值
        /// </summary>
        private static object ConvertToTypedValue(string stringValue, List<Type> typeList, int index, ConfigModel config)
        {
            if (index >= typeList.Count)
                return stringValue;

            var targetType = Nullable.GetUnderlyingType(typeList[index]) ?? typeList[index];

            if (targetType == typeof(DateTime))
            {
                if (config.DbType == DataDbType.Oracle)
                    return DateTime.Parse(stringValue).Date;
                return DateTime.Parse(stringValue);
            }

            if (targetType == typeof(int))
                return int.Parse(stringValue);
            if (targetType == typeof(long))
                return long.Parse(stringValue);
            if (targetType == typeof(decimal))
                return decimal.Parse(stringValue);
            if (targetType == typeof(double))
                return double.Parse(stringValue);
            if (targetType == typeof(bool))
                return bool.Parse(stringValue);

            return stringValue;
        }
        #endregion

        #region 裁剪尾部运算符
        /// <summary>
        /// 移除 SQL 末尾多余的 AND/OR 运算符
        /// </summary>
        private static string TrimTrailingOperators(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return string.Empty;

            var trimmed = sql.Trim();

            while (trimmed.EndsWith(" AND", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 4).Trim();
            }

            return trimmed;
        }
        #endregion

        #region 记录异常
        /// <summary>
        /// 统一异常日志记录
        /// </summary>
        private static void LogException(ConfigModel config, Exception exception, string methodName)
        {
            try
            {
                if (config.SqlErrorType != null && config.SqlErrorType.ToLowerInvariant() == SqlErrorType.Db)
                    DbLogTable.LogException(config, exception, methodName, string.Empty);
                else
                    DbLog.LogException(config.IsOutError, config.DbType, exception, methodName, string.Empty);
            }
            catch
            {
                // 避免日志记录失败影响主流程
            }
        }
        #endregion
    }
}
