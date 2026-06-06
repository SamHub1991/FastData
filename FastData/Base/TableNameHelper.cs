using System;
using System.Collections.Generic;
using System.Reflection;
using FastData.Config;
using FastData.DbTypes;
using FastData.Model;

namespace FastData.Base
{
    internal static class TableNameHelper
    {
        public static string GetTableName<T>(string dbKey = null)
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<Property.TableAttribute>();
            if (attr == null)
                return QuoteTableName(type.Name, dbKey);

            if (!string.IsNullOrEmpty(attr.DbTableNames))
            {
                var effectiveKey = dbKey ?? FastDb.CurrentKey;
                if (!string.IsNullOrEmpty(effectiveKey))
                {
                    var tableName = GetTableNameFromMapping(attr.DbTableNames, effectiveKey);
                    if (!string.IsNullOrEmpty(tableName))
                        return QuoteTableName(tableName, effectiveKey);
                }
            }

            if (!string.IsNullOrEmpty(attr.Name))
                return QuoteTableName(attr.Name, dbKey);

            return QuoteTableName(type.Name, dbKey);
        }

        public static string GetTableName<T>(ConfigModel config)
        {
            return GetTableName<T>(config?.Key);
        }

        private static string QuoteTableName(string tableName, DataDbType dbType)
        {
            if (string.IsNullOrEmpty(tableName))
                return tableName;

            switch (dbType)
            {
                case DataDbType.PostgreSql:
                    return tableName.ToLowerInvariant();

                case DataDbType.MySql:
                    return $"`{tableName}`";

                default:
                    return tableName;
            }
        }

        private static string QuoteTableName(string tableName, string dbKey)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(dbKey))
                return tableName;

            try
            {
                var dbConfig = FastDataConfig.GetConfig(dbKey);
                if (dbConfig != null)
                    return QuoteTableName(tableName, dbConfig.DbType);
            }
            catch { }

            return tableName;
        }

        private static string GetTableNameFromMapping(string dbTableNames, string dbKey)
        {
            if (string.IsNullOrEmpty(dbTableNames) || string.IsNullOrEmpty(dbKey))
                return null;

            var pairs = dbTableNames.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Trim().Split('.');
                if (parts.Length == 2 && string.Equals(parts[0].Trim(), dbKey, StringComparison.OrdinalIgnoreCase))
                    return parts[1].Trim();
            }

            return null;
        }
    }
}