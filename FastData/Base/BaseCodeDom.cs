using System;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using FastUntility.Base;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace FastData.Base
{
    /// <summary>
    /// 动态解析条件
    /// <para>
    /// 将 C# 布尔表达式编译为动态程序集，通过反射调用 OutPut 方法获取执行结果。
    /// 编译失败时记录错误日志并返回 false。
    /// </para>
    /// </summary>
    internal static class BaseCodeDom
    {
        /// <summary>
        /// 动态编译 C# 表达式代码并获取布尔结果
        /// </summary>
        /// <param name="code">要编译的 C# 布尔表达式代码</param>
        /// <param name="references">需要引用的程序集名称（不含扩展名），为 null 时不额外引用</param>
        /// <returns>表达式执行结果；编译失败时返回 false 并记录错误日志</returns>
        public static bool GetResult(string code, string references = null)
        {
            //动态编译
            var compiler = new CSharpCodeProvider();
            var param = new CompilerParameters();

            param.ReferencedAssemblies.Add("System.dll");
            param.ReferencedAssemblies.Add("System.Core.dll");
            param.ReferencedAssemblies.Add("mscorlib.dll");

            var assembly = AppDomain.CurrentDomain.GetAssemblies().ToList().Find(a => a.FullName.Split(',')[0] == references);
            if (assembly == null)
                assembly = Assembly.Load(references);
                        
            param.ReferencedAssemblies.Add(assembly.Location);

            param.GenerateExecutable = false;
            param.GenerateInMemory = true;                        
            var result = compiler.CompileAssemblyFromSource(param, GetCode(code,references));

            if (result.Errors.HasErrors)
            {
                var error = new StringBuilder();
                error.AppendFormat("code:{0},error info:", GetCode(code));
                               
                foreach (CompilerError info in result.Errors)
                {
                    error.Append(info.ErrorText);
                }

                BaseLog.SaveLog(error.ToString(), "DynamicCompiler");

                return false;
            }
            else
            {
                //反射
                assembly = result.CompiledAssembly;
                var instance = assembly.CreateInstance("DynamicCode.Condition");
                var method = instance.GetType().GetMethod("OutPut");
                return (bool)method.Invoke(instance, null);
            }
        }

        /// <summary>
        /// 生成动态编译的源代码模板
        /// </summary>
        /// <param name="code">C# 布尔表达式代码</param>
        /// <param name="references">需要引用的命名空间，为 null 时跳过</param>
        /// <returns>完整的 C# 源代码字符串</returns>
        private static string GetCode(string code, string references = null)
        {
            var sb = new StringBuilder();
            sb.Append("using System;");
            sb.Append("using System.Collections.Generic;");
            sb.Append("using System.Linq;");
            sb.Append("using System.Web;");

            if (!string.IsNullOrEmpty(references))
                sb.AppendFormat("using {0};", references);

            sb.Append(Environment.NewLine);
            sb.Append("namespace DynamicCode");
            sb.Append(Environment.NewLine);
            sb.Append("{");
            sb.Append(Environment.NewLine);
            sb.Append("    public class Condition");
            sb.Append(Environment.NewLine);
            sb.Append("    {");
            sb.Append(Environment.NewLine);
            sb.Append("        public bool OutPut()");
            sb.Append(Environment.NewLine);
            sb.Append("        {");
            sb.Append(Environment.NewLine);
            sb.AppendFormat("             return {0};", code);
            sb.Append(Environment.NewLine);
            sb.Append("        }");
            sb.Append(Environment.NewLine);
            sb.Append("    }");
            sb.Append(Environment.NewLine);
            sb.Append("}");

            return sb.ToString();
        }
    }
}
