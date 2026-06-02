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

            sb.AppendLine($"using System;");
            sb.AppendLine($"using System.ComponentModel.DataAnnotations;");
            sb.AppendLine($"using FastData.ChangeTracking;");
            sb.AppendLine();
            sb.AppendLine($"namespace {@namespace}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {tableName} 实体类（自动生成）");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [TableName(\"{tableName}\")]");
            sb.AppendLine($"    public class {ToPascalCase(tableName)}");
            sb.AppendLine($"    {{");

            foreach (var prop in properties)
            {
                var propType = GetCSharpType(prop.DataType);
                var propName = ToPascalCase(prop.ColumnName);
                var isNullable = prop.IsNullable && propType != "string";

                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// {prop.ColumnName}");
                sb.AppendLine($"        /// </summary>");

                if (prop.ColumnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"        [Key]");
                    sb.AppendLine($"        public {propType}{(isNullable ? "?" : "")} {propName} {{ get; set; }}");
                }
                else if (prop.ColumnName.Contains("Name", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"        [Required]");
                    sb.AppendLine($"        [StringLength({prop.MaxLength ?? 50})]");
                    sb.AppendLine($"        public {propType}{(isNullable ? "?" : "")} {propName} {{ get; set; }}");
                }
                else if (prop.ColumnName.Contains("Email", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"        [Required]");
                    sb.AppendLine($"        [EmailAddress]");
                    sb.AppendLine($"        [StringLength({prop.MaxLength ?? 100})]");
                    sb.AppendLine($"        public {propType}{(isNullable ? "?" : "")} {propName} {{ get; set; }}");
                }
                else
                {
                    sb.AppendLine($"        public {propType}{(isNullable ? "?" : "")} {propName} {{ get; set; }}");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成仓储类代码
        /// </summary>
        public static string GenerateRepositoryCode(string entityName, string @namespace = "FastData.Repository")
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"using System;");
            sb.AppendLine($"using System.Collections.Generic;");
            sb.AppendLine($"using System.Linq;");
            sb.AppendLine($"using System.Linq.Expressions;");
            sb.AppendLine($"using FastData;");
            sb.AppendLine();

            sb.AppendLine($"namespace {@namespace}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {entityName} 仓储类");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {entityName}Repository");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 查询所有 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public List<{entityName}> GetAll()");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return FastRead.Query<{entityName}>().ToList();");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 根据 ID 查询 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public {entityName} GetById(int id)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return FastRead.Query<{entityName}>(e => e.Id == id).ToItem();");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 添加 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public WriteReturn Add({entityName} entity)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return FastWrite.Add(entity);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 更新 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public WriteReturn Update({entityName} entity)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return FastWrite.Update(entity);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 删除 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public WriteReturn Delete(int id)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return FastWrite.Delete<{entityName}>(e => e.Id == id);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 分页查询 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public (List<{entityName}> Items, int Total) GetPaged(int page, int pageSize)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            var query = FastRead.Query<{entityName}>();");
            sb.AppendLine($"            var pageResult = query.ToPage(new FastUntility.Page.PageModel {{ PageId = page, PageSize = pageSize }});");
            sb.AppendLine($"            return (pageResult.list, pageResult.pModel.TotalRecord);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 条件查询 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public List<{entityName}> Find(Expression<Func<{entityName}, bool>> predicate)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return FastRead.Query<{entityName}>(predicate).ToList();");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成服务类代码
        /// </summary>
        public static string GenerateServiceCode(string entityName, string @namespace = "FastData.Service")
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"using System;");
            sb.AppendLine($"using System.Collections.Generic;");
            sb.AppendLine($"using FastData.Repository;");
            sb.AppendLine();

            sb.AppendLine($"namespace {@namespace}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {entityName} 服务类");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {entityName}Service");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        private readonly {entityName}Repository _repository;");
            sb.AppendLine();
            sb.AppendLine($"        public {entityName}Service()");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            _repository = new {entityName}Repository();");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 获取所有 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public List<{entityName}> GetAll()");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return _repository.GetAll();");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 根据 ID 获取 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public {entityName} GetById(int id)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return _repository.GetById(id);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 添加 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public WriteReturn Add({entityName} entity)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return _repository.Add(entity);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 更新 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public WriteReturn Update({entityName} entity)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return _repository.Update(entity);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 删除 {entityName}");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public WriteReturn Delete(int id)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return _repository.Delete(id);");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

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