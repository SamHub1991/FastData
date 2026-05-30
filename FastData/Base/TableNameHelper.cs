using System;
using System.Collections.Generic;
using System.Reflection;

namespace FastData.Base
{
    internal static class TableNameHelper
    {
        public static string GetTableName<T>(string dbKey = null)
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<Property.TableAttribute>();
            if (attr == null)
                return type.Name;

            // 优先使用多数据库表名映射
            if (!string.IsNullOrEmpty(attr.DbTableNames))
            {
                // 如果未传入 dbKey，尝试使用当前上下文的数据库 key
                var effectiveKey = dbKey ?? FastDb.CurrentKey;
                if (!string.IsNullOrEmpty(effectiveKey))
                {
                    var tableName = GetTableNameFromMapping(attr.DbTableNames, effectiveKey);
                    if (!string.IsNullOrEmpty(tableName))
                        return tableName;
                }
            }

            // 其次使用 Name 属性
            if (!string.IsNullOrEmpty(attr.Name))
                return attr.Name;

            // 最后使用类名
            return type.Name;
        }

        /// <summary>
        /// 从多数据库表名映射中获取表名
        /// 格式: "数据库Key.表名,数据库Key.表名"
        /// </summary>
        /// <param name="dbTableNames">数据库表名映射字符串</param>
        /// <param name="dbKey">数据库键</param>
        /// <returns>表名</returns>
        private static string GetTableNameFromMapping(string dbTableNames, string dbKey)
        {
            if (string.IsNullOrEmpty(dbTableNames) || string.IsNullOrEmpty(dbKey))
                return null;

            var pairs = dbTableNames.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Trim().Split('.');
                if (parts.Length == 2 && string.Equals(parts[0].Trim(), dbKey, StringComparison.OrdinalIgnoreCase))
                {
                    return parts[1].Trim();
                }
            }

            return null;
        }
    }
}