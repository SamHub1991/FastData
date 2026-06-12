using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace FastData.ChangeTracking
{
    /// <summary>
    /// 表名帮助类
    /// </summary>
    internal static class TableNameHelper
    {
        /// <summary>
        /// 获取实体对应的表名
        /// </summary>
        public static string GetTableName(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var tableAttribute = type.GetCustomAttributes(typeof(TableNameAttribute), false)
                .FirstOrDefault() as TableNameAttribute;

            if (tableAttribute != null)
                return tableAttribute.Name;

            return ToSnakeCase(type.Name);
        }

        /// <summary>
        /// 将 PascalCase 转换为 snake_case
        /// </summary>
        private static string ToSnakeCase(string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }
    }

    /// <summary>
    /// 表名特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableNameAttribute : Attribute
    {
        /// <summary>
        /// Gets the mapped table name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a table-name mapping attribute.
        /// </summary>
        /// <param name="name">Mapped table name.</param>
        public TableNameAttribute(string name)
        {
            Name = name;
        }
    }
}
