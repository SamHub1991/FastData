using System;
using FastData.Context;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FastData.DevTools
{
    /// <summary>
    /// 代码生成器 - 自动生成实体类和代码
    /// </summary>
    public static class CodeGenerator
    {
        /// <summary>
        /// 从数据库表生成实体类代码
        /// </summary>
        public static string GenerateEntityFromTable(string tableName, string @namespace = "FastData.Model")
        {
            var properties = GetTableColumns(tableName);
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using FastData.ChangeTracking;");
            sb.AppendLine();
            sb.AppendLine(string.Format("namespace {0}", @namespace));
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine(string.Format("    /// {0} 实体类（自动生成）", tableName));
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(string.Format("    [TableName(\"{0}\")]", tableName));
            sb.AppendLine(string.Format("    public class {0}", ToPascalCase(tableName)));
            sb.AppendLine("    {");

            foreach (var prop in properties)
            {
                var propType = GetCSharpType(prop.DataType);
                var propName = ToPascalCase(prop.ColumnName);
                var isNullable = prop.IsNullable && propType != "string";

                sb.AppendLine("        /// <summary>");
                sb.AppendLine(string.Format("        /// {0}", prop.ColumnName));
                sb.AppendLine("        /// </summary>");

                if (prop.ColumnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("        [Key]");
                    sb.AppendLine(string.Format("        public {0}{1} {2} {{ get; set; }}", propType, (isNullable ? "?" : ""), propName));
                }
                else if (prop.ColumnName.Contains("Name", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("        [Required]");
                    sb.AppendLine(string.Format("        [StringLength({0})]", prop.MaxLength ?? 50));
                    sb.AppendLine(string.Format("        public {0}{1} {2} {{ get; set; }}", propType, (isNullable ? "?" : ""), propName));
                }
                else if (prop.ColumnName.Contains("Email", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("        [Required]");
                    sb.AppendLine("        [EmailAddress]");
                    sb.AppendLine(string.Format("        [StringLength({0})]", prop.MaxLength ?? 100));
                    sb.AppendLine(string.Format("        public {0}{1} {2} {{ get; set; }}", propType, (isNullable ? "?" : ""), propName));
                }
                else
                {
                    sb.AppendLine(string.Format("        public {0}{1} {2} {{ get; set; }}", propType, (isNullable ? "?" : ""), propName));
                }
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成仓储类代码
        /// </summary>
        public static string GenerateRepositoryCode(string entityName, string @namespace = "FastData.Repository")
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Linq.Expressions;");
            sb.AppendLine("using FastData;");
            sb.AppendLine();

            sb.AppendLine(string.Format("namespace {0}", @namespace));
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine(string.Format("    /// {0} 仓储类", entityName));
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(string.Format("    public class {0}Repository", entityName));
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 查询所有 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public List<{0}> GetAll()", entityName));
            sb.AppendLine("        {");
            sb.AppendLine(string.Format("            return FastRead.Query<{0}>().ToList();", entityName));
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 根据 ID 查询 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public {0} GetById(int id)", entityName));
            sb.AppendLine("        {");
            sb.AppendLine(string.Format("            return FastRead.Query<{0}>(e => e.Id == id).ToItem();", entityName));
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 添加 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public WriteReturn Add({0} entity)", entityName));
            sb.AppendLine("        {");
            sb.AppendLine("            return FastWrite.Add(entity);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 更新 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public WriteReturn Update({0} entity)", entityName));
            sb.AppendLine("        {");
            sb.AppendLine("            return FastWrite.Update(entity);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 删除 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public WriteReturn Delete(int id)");
            sb.AppendLine("        {");
            sb.AppendLine(string.Format("            return FastWrite.Delete<{0}>(e => e.Id == id);", entityName));
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 分页查询 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public (List<{0}> Items, int Total) GetPaged(int page, int pageSize)", entityName));
            sb.AppendLine("        {");
            sb.AppendLine(string.Format("            var query = FastRead.Query<{0}>();", entityName));
            sb.AppendLine("            var pageResult = query.ToPage(new FastUntility.Page.PageModel { PageId = page, PageSize = pageSize });");
            sb.AppendLine("            return (pageResult.list, pageResult.pModel.TotalRecord);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 条件查询 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public List<{0}> Find(Expression<Func<{0}, bool>> predicate)", entityName));
            sb.AppendLine("        {");
            sb.AppendLine(string.Format("            return FastRead.Query<{0}>(predicate).ToList();", entityName));
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成服务类代码
        /// </summary>
        public static string GenerateServiceCode(string entityName, string @namespace = "FastData.Service")
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using FastData.Repository;");
            sb.AppendLine();

            sb.AppendLine(string.Format("namespace {0}", @namespace));
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine(string.Format("    /// {0} 服务类", entityName));
            sb.AppendLine("    /// </summary>");
            sb.AppendLine(string.Format("    public class {0}Service", entityName));
            sb.AppendLine("    {");
            sb.AppendLine(string.Format("        private readonly {0}Repository _repository;", entityName));
            sb.AppendLine();
            sb.AppendLine(string.Format("        public {0}Service()", entityName));
            sb.AppendLine("        {");
            sb.AppendLine(string.Format("            _repository = new {0}Repository();", entityName));
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 获取所有 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public List<{0}> GetAll()", entityName));
            sb.AppendLine("        {");
            sb.AppendLine("            return _repository.GetAll();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 根据 ID 获取 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public {0} GetById(int id)", entityName));
            sb.AppendLine("        {");
            sb.AppendLine("            return _repository.GetById(id);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 添加 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public WriteReturn Add({0} entity)", entityName));
            sb.AppendLine("        {");
            sb.AppendLine("            return _repository.Add(entity);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 更新 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(string.Format("        public WriteReturn Update({0} entity)", entityName));
            sb.AppendLine("        {");
            sb.AppendLine("            return _repository.Update(entity);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(string.Format("        /// 删除 {0}", entityName));
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public WriteReturn Delete(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            return _repository.Delete(id);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 转换为 PascalCase
        /// </summary>
        private static string ToPascalCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var words = text.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = string.Concat(words.Select(word => 
                char.ToUpper(word[0]) + word.Substring(1).ToLower()));

            return result;
        }

        /// <summary>
        /// 获取 C# 类型
        /// </summary>
        private static string GetCSharpType(string dbType)
        {
            return dbType.ToLower() switch
            {
                "int" => "int",
                "bigint" => "long",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "decimal" => "decimal",
                "money" => "decimal",
                "float" => "float",
                "double" => "double",
                "datetime" => "DateTime",
                "date" => "DateTime",
                "time" => "TimeSpan",
                "varchar" => "string",
                "nvarchar" => "string",
                "char" => "string",
                "nchar" => "string",
                "text" => "string",
                "ntext" => "string",
                "uniqueidentifier" => "Guid",
                _ => "object"
            };
        }

        /// <summary>
        /// 获取表列信息（示例实现）
        /// </summary>
        private static List<ColumnMetadata> GetTableColumns(string tableName)
        {
            // 这里应该从数据库查询实际的列信息
            // 这是示例实现
            return new List<ColumnMetadata>
            {
                new ColumnMetadata { ColumnName = "Id", DataType = "int", IsNullable = false },
                new ColumnMetadata { ColumnName = "Name", DataType = "nvarchar", IsNullable = false, MaxLength = 50 },
                new ColumnMetadata { ColumnName = "Email", DataType = "nvarchar", IsNullable = false, MaxLength = 100 },
                new ColumnMetadata { ColumnName = "Age", DataType = "int", IsNullable = true },
                new ColumnMetadata { ColumnName = "IsActive", DataType = "bit", IsNullable = false },
                new ColumnMetadata { ColumnName = "CreateTime", DataType = "datetime", IsNullable = false }
            };
        }

        /// <summary>
        /// 列元数据
        /// </summary>
        private class ColumnMetadata
        {
            public string ColumnName { get; set; }
            public string DataType { get; set; }
            public bool IsNullable { get; set; }
            public int? MaxLength { get; set; }
        }
    }
}
