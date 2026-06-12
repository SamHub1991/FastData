#if !NETFRAMEWORK
using System;
using System.Linq;
using System.Reflection;
using FastData.Model;
using Newtonsoft.Json;

namespace FastData.Queue
{
    internal static class QueueWriteInvoker
    {
        internal static WriteReturn Execute(WriteOperation operation, string key)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var entityType = ResolveEntityType(operation.EntityType);
            var model = JsonConvert.DeserializeObject(operation.Data, entityType);

            switch (operation.OperationType)
            {
                case WriteOperationType.Add:
                    return InvokeFastWrite("Add", entityType, model, key);
                case WriteOperationType.Update:
                    return InvokeFastWrite("Update", entityType, model, key);
                case WriteOperationType.Delete:
                    return InvokeFastWrite("Delete", entityType, model, key);
                default:
                    throw new NotSupportedException($"不支持的操作类型: {operation.OperationType}");
            }
        }

        private static WriteReturn InvokeFastWrite(string methodName, Type entityType, object model, string key)
        {
            var method = typeof(FastWrite).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == methodName && m.IsGenericMethodDefinition)
                .First(m =>
                {
                    var parameters = m.GetParameters();
                    return parameters.Length >= 1
                        && parameters[0].ParameterType.IsGenericParameter
                        && parameters.Any(p => p.Name == "key");
                });
            var genericMethod = method.MakeGenericMethod(entityType);
            var args = method.GetParameters()
                .Select(p => p.Name == "model" ? model : p.Name == "key" ? key : p.DefaultValue)
                .ToArray();
            return (WriteReturn)genericMethod.Invoke(null, args);
        }

        private static Type ResolveEntityType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new InvalidOperationException("实体类型不能为空");

            var type = Type.GetType(typeName, throwOnError: false, ignoreCase: true);
            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName, throwOnError: false, ignoreCase: true);
                if (type != null)
                    return type;
            }

            throw new InvalidOperationException($"无法解析实体类型: {typeName}");
        }
    }
}
#endif
