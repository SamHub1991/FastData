using System;
using System.Collections.Generic;
using System.Text;
#if NETFRAMEWORK
using System.Web.Script.Serialization;
#else
using System.Text.Json;
#endif

namespace FastData.Tooling.CodeGeneration
{
    /// <summary>
    /// JSON 转 Model 生成器
    /// </summary>
    public class JsonToModelConverter
    {
        /// <summary>
        /// 将 JSON 转换为 C# Model
        /// </summary>
        public string Convert(string json, string className, string namespaceName = "FastData.Generated.Models")
        {
            if (string.IsNullOrWhiteSpace(json))
                return "// 请输入有效的 JSON";

            try
            {
                var builder = new StringBuilder();
                builder.AppendLine("using System;");
                builder.AppendLine("using System.Text.Json.Serialization;");
                builder.AppendLine();
                builder.AppendLine("namespace " + namespaceName);
                builder.AppendLine("{");

                // 解析 JSON 并生成类
                var root = ParseJson(json);
                builder.AppendLine(GenerateClass(className, root));
                
                builder.AppendLine("}");
                return builder.ToString();
            }
            catch (Exception ex)
            {
                return "// JSON 解析失败：" + ex.Message;
            }
        }

        private JsonValue ParseJson(string json)
        {
            json = json.Trim();
            if (json.StartsWith("{") && json.EndsWith("}"))
                return ParseObject(json);
            if (json.StartsWith("[") && json.EndsWith("]"))
                return ParseArray(json);
            return ParsePrimitive(json);
        }

        private JsonValue ParseObject(string json)
        {
            var result = new JsonValue { Type = JsonType.Object };
            result.Properties = new Dictionary<string, JsonValue>();
            
            if (json == "{}") return result;
            
            var content = json.Substring(1, json.Length - 2).Trim();
            var depth = 0;
            var start = 0;
            var inString = false;
            var escapeNext = false;
            
            for (int i = 0; i < content.Length; i++)
            {
                var ch = content[i];
                
                if (escapeNext) { escapeNext = false; continue; }
                if (ch == '\\') { escapeNext = true; continue; }
                if (ch == '"') { inString = !inString; continue; }
                if (inString) continue;
                
                if (ch == '{' || ch == '[') depth++;
                else if (ch == '}' || ch == ']') depth--;
                else if (ch == ',' && depth == 0)
                {
                    AddProperty(content.Substring(start, i - start).Trim(), result);
                    start = i + 1;
                }
            }
            
            if (start < content.Length)
                AddProperty(content.Substring(start).Trim(), result);
            
            return result;
        }

        private void AddProperty(string prop, JsonValue parent)
        {
            var colonIndex = prop.IndexOf(':');
            if (colonIndex < 0) return;
            
            var key = prop.Substring(0, colonIndex).Trim().Trim('"');
            var value = prop.Substring(colonIndex + 1).Trim();
            
            parent.Properties[key] = ParseJson(value);
        }

        private JsonValue ParseArray(string json)
        {
            var result = new JsonValue { Type = JsonType.Array };
            result.Items = new List<JsonValue>();
            
            if (json == "[]") return result;
            
            var content = json.Substring(1, json.Length - 2).Trim();
            var depth = 0;
            var start = 0;
            var inString = false;
            var escapeNext = false;
            
            for (int i = 0; i < content.Length; i++)
            {
                var ch = content[i];
                
                if (escapeNext) { escapeNext = false; continue; }
                if (ch == '\\') { escapeNext = true; continue; }
                if (ch == '"') { inString = !inString; continue; }
                if (inString) continue;
                
                if (ch == '{' || ch == '[') depth++;
                else if (ch == '}' || ch == ']') depth--;
                else if (ch == ',' && depth == 0)
                {
                    result.Items.Add(ParseJson(content.Substring(start, i - start).Trim()));
                    start = i + 1;
                }
            }
            
            if (start < content.Length)
                result.Items.Add(ParseJson(content.Substring(start).Trim()));
            
            return result;
        }

        private JsonValue ParsePrimitive(string value)
        {
            value = value.Trim();
            
            if (value == "null")
                return new JsonValue { Type = JsonType.Null };
            if (value == "true" || value == "false")
                return new JsonValue { Type = JsonType.Boolean };
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return new JsonValue { Type = JsonType.String };
            if (value.Contains(".") || value.Contains("e") || value.Contains("E"))
                return new JsonValue { Type = JsonType.Double };
            
            return new JsonValue { Type = JsonType.Integer };
        }

        private string GenerateClass(string className, JsonValue jsonValue)
        {
            var builder = new StringBuilder();
            
            if (jsonValue.Type != JsonType.Object)
                return builder.ToString();
            
            builder.AppendLine("    public class " + ToPascal(className));
            builder.AppendLine("    {");
            
            foreach (var prop in jsonValue.Properties)
            {
                var propName = ToPascal(prop.Key);
                var propType = GetClrType(prop.Value);
                
                builder.AppendLine("        [JsonPropertyName(\"" + Escape(prop.Key) + "\")]");
                builder.AppendLine($"        public {propType} {propName} {{ get; set; }}");
                builder.AppendLine();
            }
            
            builder.AppendLine("    }");
            return builder.ToString();
        }

        private string GetClrType(JsonValue jsonValue)
        {
            switch (jsonValue.Type)
            {
                case JsonType.String:
                    return "string";
                case JsonType.Integer:
                    return "long";
                case JsonType.Double:
                    return "double";
                case JsonType.Boolean:
                    return "bool";
                case JsonType.Null:
                    return "string"; // 默认可空字符串
                case JsonType.Object:
                    return "object";
                case JsonType.Array:
                    if (jsonValue.Items != null && jsonValue.Items.Count > 0)
                    {
                        var itemType = GetClrType(jsonValue.Items[0]);
                        return $"List<{itemType}>";
                    }
                    return "List<object>";
                default:
                    return "string";
            }
        }

        private static string ToPascal(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Property";
            var builder = new StringBuilder();
            var upper = true;
            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch)) { upper = true; continue; }
                builder.Append(upper ? char.ToUpperInvariant(ch) : (builder.Length == 0 ? char.ToLowerInvariant(ch) : ch));
                upper = false;
            }
            return builder.Length == 0 || char.IsDigit(builder[0]) ? "Property" : builder.ToString();
        }

        private static string Escape(string value)
        {
            return value?.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t") ?? "";
        }
    }

    public enum JsonType
    {
        String,
        Integer,
        Double,
        Boolean,
        Null,
        Object,
        Array
    }

    public class JsonValue
    {
        public JsonType Type { get; set; }
        public Dictionary<string, JsonValue> Properties { get; set; }
        public List<JsonValue> Items { get; set; }
    }
}
