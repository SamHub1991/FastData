using FastData.Tooling.Database;
using System.Collections.Generic;
using System.Text;

namespace FastData.Tooling.CodeGeneration
{
    /// <summary>
    /// Model 代码生成器
    /// 
    /// 根据数据库表结构生成 C# 实体类代码。
    /// 
    /// 使用示例：
    /// <code>
    /// var generator = new ModelCodeGenerator();
    /// var code = generator.Generate("MyApp.Models", table, columns);
    /// </code>
    /// </summary>
    public class ModelCodeGenerator
    {
        /// <summary>
        /// 生成 Model 代码
        /// </summary>
        /// <param name="namespaceName">命名空间</param>
        /// <param name="table">表信息</param>
        /// <param name="columns">列信息列表</param>
        /// <param name="dbTableNames">多数据库表名映射（可选）</param>
        /// <returns>C# 代码字符串</returns>
        public string Generate(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns, string dbTableNames = null)
        {
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            builder.AppendLine("using FastData.Property;");
            builder.AppendLine();
            builder.AppendLine("namespace " + namespaceName);
            builder.AppendLine("{");
            
            // 生成 Table 属性
            var tableName = Escape(table.Name);
            var tableAttr = $"[Table(Name = \"{tableName}\")";
            
            if (!string.IsNullOrEmpty(table.Comment))
            {
                tableAttr += $", Comments = \"{Escape(table.Comment)}\"";
            }
            
            if (!string.IsNullOrEmpty(dbTableNames))
            {
                tableAttr += $", DbTableNames = \"{Escape(dbTableNames)}\"";
            }
            
            tableAttr += ")]";
            builder.AppendLine("    " + tableAttr);
            
            builder.AppendLine("    public class " + ToPascal(table.Name));
            builder.AppendLine("    {");

            foreach (var column in columns)
            {
                var isPrimaryKey = column.IsPrimaryKey;
                var comments = Escape(column.Comment ?? column.Name);
                var dataType = Escape(column.DbType ?? "string");
                var isNull = column.IsNullable ? "true" : "false";
                var isKey = isPrimaryKey ? "true" : "false";

                // 主键使用 [Primary] 特性
                if (isPrimaryKey)
                {
                    builder.AppendLine("        [Primary]");
                }

                // 列属性
                builder.AppendLine($"        [Column(Comments = \"{comments}\", DataType = \"{dataType}\", IsNull = {isNull}, IsKey = {isKey})]");
                builder.AppendLine("        public " + GetClrType(column) + " " + ToPascal(column.Name) + " { get; set; }");
                builder.AppendLine();
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>
        /// 生成 Model 代码（不包含多数据库表名映射）
        /// </summary>
        /// <param name="namespaceName">命名空间</param>
        /// <param name="table">表信息</param>
        /// <param name="columns">列信息列表</param>
        /// <returns>C# 代码字符串</returns>
        public string Generate(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            return Generate(namespaceName, table, columns, null);
        }

        /// <summary>
        /// 获取 CLR 类型名称
        /// </summary>
        /// <param name="column">列信息</param>
        /// <returns>CLR 类型名称</returns>
        public string GetClrType(DatabaseColumn column)
        {
            var dbType = (column.DbType ?? string.Empty).ToLower();
            var nullable = column.IsNullable && dbType != "string";
            string type;

            if (dbType.Contains("bigint"))
                type = "long";
            else if (dbType.Contains("int") || dbType.Contains("number") && column.Scale == 0 && column.Precision <= 10)
                type = "int";
            else if (dbType.Contains("decimal") || dbType.Contains("numeric") || dbType.Contains("money") || dbType.Contains("number"))
                type = "decimal";
            else if (dbType.Contains("float") || dbType.Contains("double"))
                type = "double";
            else if (dbType.Contains("bit") || dbType.Contains("bool"))
                type = "bool";
            else if (dbType.Contains("date") || dbType.Contains("time"))
                type = "DateTime";
            else if (dbType.Contains("uniqueidentifier") || dbType.Contains("uuid"))
                type = "Guid";
            else if (dbType.Contains("binary") || dbType.Contains("blob") || dbType.Contains("image"))
                type = "byte[]";
            else
                type = "string";

            return nullable && type != "byte[]" ? type + "?" : type;
        }

        /// <summary>
        /// 转换为 Pascal 命名
        /// </summary>
        /// <param name="value">原始字符串</param>
        /// <returns>Pascal 命名字符串</returns>
        private static string ToPascal(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Model";

            // 处理下划线分隔的命名
            var parts = value.Split('_');
            var result = new StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    result.Append(char.ToUpper(part[0]));
                    if (part.Length > 1)
                        result.Append(part.Substring(1).ToLower());
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// 转义字符串中的特殊字符
        /// </summary>
        /// <param name="value">原始字符串</param>
        /// <returns>转义后的字符串</returns>
        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
