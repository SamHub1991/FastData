using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using FastData.DbTypes;
using FastData.Base;

namespace FastData.Model
{
    /// <summary>
    /// 改进的查询条件构建器，提供类型安全和验证
    /// </summary>
    public static class QueryConditionBuilder
    {
        /// <summary>
        /// 构建强类型的 WHERE 条件
        /// </summary>
        internal static void AddWhere<T>(
            List<VisitModel> predicateList,
            Expression<Func<T, bool>> predicate,
            ConfigModel config)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate), "WHERE 条件不能为空");

            if (predicateList == null)
                throw new ArgumentNullException(nameof(predicateList), "Predicate 列表不能为空");

            try
            {
                var visitModel = VisitExpression.LambdaWhere<T>(predicate, config);
                if (visitModel.IsSuccess)
                {
                    predicateList.Add(visitModel);
                }
                else
                {
                    throw new InvalidOperationException("WHERE 条件解析失败");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("构建 WHERE 条件时出错: {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public static void ValidateConfig(ConfigModel config, string operation)
        {
            if (config == null)
            {
                throw new InvalidOperationException(
                    string.Format("配置为空 - 操作: {0}\n请确保 db.config 文件存在并正确配置", operation));
            }

            if (string.IsNullOrEmpty(config.Key))
            {
                throw new InvalidOperationException(
                    string.Format("配置 Key 为空 - 操作: {0}\n请在 db.config 中定义数据库 Key", operation));
            }
        }

        /// <summary>
        /// 检查数据库类型是否支持
        /// </summary>
        public static bool IsSupportedDbType(DataDbType dbType)
        {
            return dbType == DataDbType.SqlServer ||
                   dbType == DataDbType.MySql ||
                   dbType == DataDbType.PostgreSql ||
                   dbType == DataDbType.Oracle ||
                   dbType == DataDbType.DB2;
        }

        /// <summary>
        /// 获取数据库类型的友好名称
        /// </summary>
        public static string GetFriendlyDbTypeName(DataDbType dbType)
        {
            return dbType switch
            {
                DataDbType.SqlServer => "SQL Server",
                DataDbType.MySql => "MySQL",
                DataDbType.PostgreSql => "PostgreSQL",
                DataDbType.Oracle => "Oracle",
                DataDbType.DB2 => "DB2",
                _ => dbType.ToString()
            };
        }

        /// <summary>
        /// 验证分页参数是否有效
        /// </summary>
        public static void ValidatePagination(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "页码必须大于 0");

            if (pageSize < 1 || pageSize > 1000)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "每页大小必须在 1 到 1000 之间");

            if (pageNumber * pageSize > 1000000)
                throw new InvalidOperationException("查询结果超过 1,000,000 条，请使用分页限制结果集");
        }
    }

    /// <summary>
    /// 查询参数验证器
    /// </summary>
    public static class QueryValidator
    {
        /// <summary>
        /// 验证字符串参数
        /// </summary>
        public static void ValidateString(string value, string paramName, int maxLength = 1000)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException(string.Format("{0} 不能为空", paramName), nameof(value));

            if (value.Length > maxLength)
                throw new ArgumentException(string.Format("{0} 长度不能超过 {1} 字符", paramName, maxLength), nameof(value));
        }

        /// <summary>
        /// 验证整数参数
        /// </summary>
        public static void ValidateInt(int value, string paramName, int min = int.MinValue, int max = int.MaxValue)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(nameof(value), 
                    string.Format("{0} 必须在 {1} 到 {2} 之间", paramName, min, max));
        }

        /// <summary>
        /// 验证 GUID 参数
        /// </summary>
        public static void ValidateGuid(Guid? value, string paramName)
        {
            if (value == null || value == Guid.Empty)
                throw new ArgumentException(string.Format("{0} 必须提供有效的 GUID", paramName), nameof(value));
        }

        /// <summary>
        /// 验证日期范围
        /// </summary>
        public static void ValidateDateRange(DateTime? startDate, DateTime? endDate, string paramName)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                throw new ArgumentException(string.Format("{0} 开始时间不能晚于结束时间", paramName));
        }
    }
}
