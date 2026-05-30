using System;
using System.Collections.Generic;
using System.Text;

namespace FastData.Tooling.CodeGeneration
{
    /// <summary>
    /// API 调用代码生成器 - 使用 RestSharp，支持 GET/POST、认证、自动生成 Model
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
                result.RequestCode = GenerateRequest(baseUrl, endpoint, method, contentType, requestBody, className, jsonResponse);

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
            b.AppendLine("using System.Threading.Tasks;");
            b.AppendLine("using RestSharp;");
            b.AppendLine("using RestSharp.Authenticators;");
            b.AppendLine();

            b.AppendLine("namespace FastData.Generated.ApiClients");
            b.AppendLine("{");
            b.AppendLine("    /// <summary>");
            b.AppendLine("    /// API 客户端 - " + url);
            b.AppendLine("    /// </summary>");
            b.AppendLine("    public class " + pascalName + "Client");
            b.AppendLine("    {");
            b.AppendLine("        private readonly RestClient _restClient;");
            b.AppendLine("        private readonly string _endpoint = \"" + endpoint + "\";");

            if (!string.IsNullOrEmpty(Config.AuthToken) || Config.AuthType != "None")
            {
                b.AppendLine("        private readonly string _authToken;");
            }

            b.AppendLine();
            b.AppendLine("        public " + pascalName + "Client(");
            b.AppendLine("            string baseUrl = \"" + url + "\"");
            if (!string.IsNullOrEmpty(Config.AuthToken) || Config.AuthType != "None")
            {
                b.AppendLine("            , string authToken = null");
            }
            b.AppendLine("            )");
            b.AppendLine("        {");
            b.AppendLine("            var options = new RestClientOptions(baseUrl);");
            
            if (Config.AuthType == "Bearer" || Config.AuthType == "JWT")
            {
                b.AppendLine("            options.Authenticator = new JwtAuthenticator(authToken ?? " + FormatAuthValue() + ");");
            }
            else if (Config.AuthType == "ApiKeyHeader")
            {
                b.AppendLine("            options.MaxTimeout = 30000;");
            }

            b.AppendLine("            _restClient = new RestClient(options);");
            
            if (!string.IsNullOrEmpty(Config.AuthToken) || Config.AuthType != "None")
            {
                b.AppendLine("            _authToken = authToken ?? " + FormatAuthValue() + ";");
            }

            if (Config.AuthType == "ApiKeyHeader" || Config.AuthType == "CustomHeaderToken" || Config.AuthType == "BasicAuth")
            {
                b.AppendLine("            SetupAuthHeaders();");
            }

            b.AppendLine("        }");
            b.AppendLine();

            if (Config.AuthType == "ApiKeyHeader" || Config.AuthType == "CustomHeaderToken" || Config.AuthType == "BasicAuth")
            {
                b.AppendLine("        private void SetupAuthHeaders()");
                b.AppendLine("        {");
                b.AppendLine(GetAuthSetupCode());
                b.AppendLine("        }");
                b.AppendLine();
            }

            string returnType = "string";
            if (!string.IsNullOrWhiteSpace(jsonResponse))
                returnType = pascalName + "Response";

            b.AppendLine("        /// <summary>");
            b.AppendLine("        /// " + endpoint + " - " + method);
            b.AppendLine("        /// </summary>");
            b.AppendLine($"        public async Task<{returnType}> {method.ToLower()}Async(");
            
            if (method == "POST" || method == "PUT")
            {
                b.AppendLine("            object request");
            }
            else
            {
                b.AppendLine("            // 参数");
            }
            
            b.AppendLine("        )");
            b.AppendLine("        {");
            b.AppendLine("            var request = new RestRequest(_endpoint, Method." + method + ");");
            b.AppendLine();
            
            if (method == "POST" || method == "PUT")
            {
                b.AppendLine("            request.AddJsonBody(request);");
            }

            b.AppendLine();
            b.AppendLine("            var response = await _restClient.ExecuteAsync(request);");
            b.AppendLine();
            b.AppendLine("            if (!response.IsSuccessful)");
            b.AppendLine("            {");
            b.AppendLine("                throw new Exception($\"API 调用失败：{response.ErrorMessage}\");");
            b.AppendLine("            }");
            b.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(jsonResponse))
            {
                b.AppendLine("            return System.Text.Json.JsonSerializer.Deserialize<" + returnType + ">(response.Content);");
            }
            else
            {
                b.AppendLine("            return response.Content;");
            }

            b.AppendLine("        }");
            b.AppendLine("    }");
            b.AppendLine("}");

            return b.ToString();
        }

        private string GetAuthSetupCode()
        {
            if (Config.AuthType == "ApiKeyHeader")
                return "            _restClient.AddDefaultHeader(\"X-API-Key\", _authToken);";
            
            if (Config.AuthType == "CustomHeaderToken")
                return "            _restClient.AddDefaultHeader(\"Authorization\", \"Token \" + _authToken);";
            
            if (Config.AuthType == "BasicAuth")
                return "            var credentials = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(_authToken));\n            _restClient.AddDefaultHeader(\"Authorization\", \"Basic \" + credentials);";
            
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

        private string GenerateService(string baseUrl, string endpoint, string className)
        {
            var b = new StringBuilder();
            b.AppendLine("using System;");
            b.AppendLine("using System.Threading.Tasks;");
            b.AppendLine("using RestSharp;");
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
            b.AppendLine("        private readonly RestClient _restClient;");
            b.AppendLine("        private readonly string _endpoint = \"" + endpoint + "\";");
            b.AppendLine();
            b.AppendLine("        public " + ToPascal(className) + "ApiService(string baseUrl = \"" + baseUrl + "\")");
            b.AppendLine("        {");
            b.AppendLine("            _restClient = new RestClient(baseUrl);");
            b.AppendLine("        }");
            b.AppendLine();
            b.AppendLine("        public async Task<dynamic> CallApiAsync(object parameters = null)");
            b.AppendLine("        {");
            b.AppendLine("            Console.WriteLine($\"Calling API: {_endpoint}\");");
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
        public string AuthType { get; set; } = "None";
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
