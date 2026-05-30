using FastData.Tooling.Database;
using System.Collections.Generic;
using System.Text;

namespace FastData.Tooling.CodeGeneration
{
    public class ModelCodeGenerator
    {
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

        public string Generate(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            return Generate(namespaceName, table, columns, null);
        }

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

        private static string ToPascal(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Model";

            var builder = new StringBuilder();
            var upper = true;
            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch))
                {
                    upper = true;
                    continue;
                }

                builder.Append(upper ? char.ToUpperInvariant(ch) : ch);
                upper = false;
            }

            if (builder.Length == 0 || char.IsDigit(builder[0]))
                builder.Insert(0, "Model");

            return builder.ToString();
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
