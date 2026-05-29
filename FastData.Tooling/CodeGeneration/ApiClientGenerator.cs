using System;
using System.Collections.Generic;
using System.Text;

namespace FastData.Tooling.CodeGeneration
{
    /// <summary>
    /// API 调用代码生成器 - 支持 GET/POST、认证、自动生成 Model
    /// </summary>
    public class ApiClientGenerator
    {
        public ApiClientConfig Config { get; set; } = new ApiClientConfig();

        /// <summary>
        /// 生成 API 客户端代码和 Model
        /// </summary>
        public ApiClientResult Generate(string baseUrl, string endpoint, string method, string contentType, string requestBody, string jsonResponse, string className)
        {
            var result = new ApiClientResult();

            if (Config.GenerateRequest)
                result.RequestCode = GenerateRequest(url: baseUrl, endpoint: endpoint, method: method, contentType: contentType, requestBody: requestBody, className: className, jsonResponse: jsonResponse);

            if (Config.GenerateResponse && !string.IsNullOrWhiteSpace(jsonResponse))
                result.ResponseCode = new JsonToModelConverter().Convert(jsonResponse, className + "Response", Config.Namespace);

            if (Config.GenerateService)
                result.ServiceCode = GenerateService(baseUrl, endpoint, className);

            return result;
        }

        private string GenerateRequest(string url, string endpoint, string method, string contentType, string requestBody, string className, string jsonResponse)
        {
            var pascalName = ToPascal(className);
            var b = new StringBuilder();

            b.AppendLine("using System;");
            b.AppendLine("using System.Net.Http;");
            b.AppendLine("using System.Text;");
            b.AppendLine("using System.Text.Json;");
            b.AppendLine("using System.Threading.Tasks;");
            b.AppendLine();

            b.AppendLine("namespace FastData.Generated.ApiClients");
            b.AppendLine("{");
            b.AppendLine("    /// <summary>");
            b.AppendLine("    /// API 客户端 - " + url);
            b.AppendLine("    /// </summary>");
            b.AppendLine("    public class " + pascalName + "Client");
            b.AppendLine("    {");
            b.AppendLine("        private readonly HttpClient _httpClient;");
            
            if (!string.IsNullOrEmpty(Config.AuthToken) || Config.AuthType != "None")
            {
                b.AppendLine("        private readonly string _authToken;");
            }

            b.AppendLine();
            b.AppendLine("        public " + pascalName + "Client(HttpClient httpClient");
            if (!string.IsNullOrEmpty(Config.AuthToken) || Config.AuthType != "None")
            {
                b.AppendLine("            , string authToken = null");
            }
            b.AppendLine("            )");
            b.AppendLine("        {");
            b.AppendLine("            _httpClient = httpClient;");
            b.AppendLine("            _httpClient.BaseAddress = new Uri(\"" + url + "\");");
            
            if (!string.IsNullOrEmpty(Config.AuthToken) || Config.AuthType != "None")
            {
                b.AppendLine("            _authToken = authToken ?? " + FormatAuthValue() + ";");
            }

            // Auth setup
            b.AppendLine();
            b.AppendLine("            // 设置认证头");
            b.AppendLine("            SetupAuthHeaders();");
            
            b.AppendLine("        }");
            b.AppendLine();

            b.AppendLine("        private void SetupAuthHeaders()");
            b.AppendLine("        {");
            b.AppendLine(GetAuthSetupCode());
            b.AppendLine("        }");
            b.AppendLine();

            // Main method for the API call
            b.AppendLine("        /// <summary>");
            b.AppendLine("        /// " + endpoint + " - " + method);
            b.AppendLine("        /// </summary>");
            
            string returnType = "string";
            if (!string.IsNullOrWhiteSpace(jsonResponse))
                returnType = pascalName + "Response";
            
            b.AppendLine($"        public async Task<{returnType}> {method.ToLower()}Async(");
            
            if (method == "POST" && !string.IsNullOrEmpty(requestBody))
                b.AppendLine("            object request");
            else
                b.AppendLine("            // 参数");
            
            b.AppendLine("        )");
            b.AppendLine("        {");
            b.AppendLine("            var requestUri = \"" + endpoint + "\";");
            b.AppendLine();
            
            b.AppendLine("            HttpResponseMessage response;");
            if (method == "POST")
            {
                b.AppendLine("            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, \"" + contentType + "\");");
                b.AppendLine("            response = await _httpClient.PostAsync(requestUri, content);");
            }
            else if (method == "GET")
            {
                b.AppendLine("            response = await _httpClient.GetAsync(requestUri);");
            }
            else if (method == "PUT")
            {
                b.AppendLine("            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, \"" + contentType + "\");");
                b.AppendLine("            response = await _httpClient.PutAsync(requestUri, content);");
            }
            else if (method == "DELETE")
            {
                b.AppendLine("            response = await _httpClient.DeleteAsync(requestUri);");
            }

            b.AppendLine();
            b.AppendLine("            response.EnsureSuccessStatusCode();");
            b.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(jsonResponse))
            {
                b.AppendLine("            var jsonString = await response.Content.ReadAsStringAsync();");
                b.AppendLine("            return JsonSerializer.Deserialize<" + returnType + ">(jsonString);");
            }
            else
            {
                b.AppendLine("            return await response.Content.ReadAsStringAsync();");
            }

            b.AppendLine("        }");
            b.AppendLine("    }");
            b.AppendLine("}");

            return b.ToString();
        }

        private string GetAuthSetupCode()
        {
            if (Config.AuthType == "Bearer")
                return "            if (!string.IsNullOrEmpty(_authToken))\n            {\n                _httpClient.DefaultRequestHeaders.Authorization = \n                    new System.Net.Http.Headers.AuthenticationHeaderValue(\"Bearer\", _authToken);\n            }\n            else if (!string.IsNullOrEmpty(" + FormatAuthValue() + ".ToString()))\n            {\n                _httpClient.DefaultRequestHeaders.Authorization = \n                    new System.Net.Http.Headers.AuthenticationHeaderValue(\"Bearer\", \"Bearer\");\n            }";
            
            if (Config.AuthType == "JWT")
                return "            if (!string.IsNullOrEmpty(_authToken))\n            {\n                _httpClient.DefaultRequestHeaders.Authorization = \n                    new System.Net.Http.Headers.AuthenticationHeaderValue(\"Bearer\", _authToken);\n            }";
            
            if (Config.AuthType == "ApiKeyHeader")
                return "            if (!string.IsNullOrEmpty(_authToken))\n                _httpClient.DefaultRequestHeaders.Add(\"X-API-Key\", _authToken);";
            
            if (Config.AuthType == "CustomHeaderToken")
                return "            if (!string.IsNullOrEmpty(_authToken))\n                _httpClient.DefaultRequestHeaders.Add(\"Authorization\", \"Token \" + _authToken);\n            else if (!string.IsNullOrEmpty(" + FormatAuthValue() + ".ToString()))\n                _httpClient.DefaultRequestHeaders.Add(\"Authorization\", \"Token " + FormatAuthValueValue() + "\");";
            
            if (Config.AuthType == "BasicAuth")
                return "            if (!string.IsNullOrEmpty(_authToken))\n            {\n                var credentials = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_authToken));\n                _httpClient.DefaultRequestHeaders.Authorization = \n                    new System.Net.Http.Headers.AuthenticationHeaderValue(\"Basic\", credentials);\n            }";
            
            return "            // 无需认证";
        }

        private string FormatAuthValue()
        {
            return Config.AuthType switch
            {
                "Bearer" => "\"your-bearer-token\"",
                "JWT" => "\"your-jwt-token\"",
                "ApiKeyHeader" => "\"your-api-key\"",
                "CustomHeaderToken" => "\"your-token\"",
                "BasicAuth" => "\"username:password\"",
                _ => "null"
            };
        }

        private string FormatAuthValueValue()
        {
            return Config.AuthType switch
            {
                "Bearer" => "your-bearer-token",
                "JWT" => "your-jwt-token",
                "ApiKeyHeader" => "your-api-key",
                "CustomHeaderToken" => "your-token",
                "BasicAuth" => "username:password",
                _ => "token"
            };
        }

        private string GenerateService(string baseUrl, string endpoint, string className)
        {
            var b = new StringBuilder();
            b.AppendLine("using System;");
            b.AppendLine("using System.Net.Http;");
            b.AppendLine("using System.Threading.Tasks;");
            b.AppendLine();
            b.AppendLine("namespace FastData.Generated.Services");
            b.AppendLine("{");
            b.AppendLine("    public interface I" + ToPascal(className) + "ApiService");
            b.AppendLine("    {");
            b.AppendLine("        Task<dynamic> CallApiAsync(object parameters = null);");
            b.AppendLine("    }");
            b.AppendLine();
            b.AppendLine("    public class " + ToPascal(className) + "ApiService : I" + ToPascal(className) + "ApiService");
            b.AppendLine("    {");
            b.AppendLine("        private readonly HttpClient _httpClient;");
            b.AppendLine("        private readonly string _baseUrl = \"" + baseUrl + "\";");
            b.AppendLine("        private readonly string _endpoint = \"" + endpoint + "\";");
            b.AppendLine();
            b.AppendLine("        public " + ToPascal(className) + "ApiService(HttpClient httpClient)");
            b.AppendLine("        {");
            b.AppendLine("            _httpClient = httpClient;");
            b.AppendLine("        }");
            b.AppendLine();
            b.AppendLine("        public async Task<dynamic> CallApiAsync(object parameters = null)");
            b.AppendLine("        {");
            b.AppendLine("            Console.WriteLine($\"Calling API: {_baseUrl}/{_endpoint}\");");
            b.AppendLine("            // TODO: Implement actual API call");
            b.AppendLine("            return await Task.FromResult<dynamic>(null);");
            b.AppendLine("        }");
            b.AppendLine("    }");
            b.AppendLine("}");
            return b.ToString();
        }

        private static string ToPascal(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Api";
            var builder = new StringBuilder();
            var upper = true;
            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch)) { upper = true; continue; }
                builder.Append(upper ? char.ToUpperInvariant(ch) : ch);
                upper = false;
            }
            return builder.Length == 0 ? "Api" : builder.ToString();
        }
    }

    public class ApiClientConfig
    {
        public string AuthType { get; set; } = "None"; // None, Bearer, JWT, ApiKeyHeader, CustomHeaderToken, BasicAuth
        public string AuthToken { get; set; } = "";
        public string Namespace { get; set; } = "FastData.Generated";
        public bool GenerateRequest { get; set; } = true;
        public bool GenerateResponse { get; set; } = true;
        public bool GenerateService { get; set; } = true;
    }

    public class ApiClientResult
    {
        public string RequestCode { get; set; }
        public string ResponseCode { get; set; }
        public string ServiceCode { get; set; }
    }
}
