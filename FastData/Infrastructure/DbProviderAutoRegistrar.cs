using System.Data.Common;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
#if NETFRAMEWORK
using FastData.Base;
#endif

namespace FastData.Infrastructure;

/// <summary>
/// 数据库提供程序自动注册器
/// </summary>
public static class DbProviderAutoRegistrar
{
    private static bool _isRegistered = false;
    private static readonly object _lockObj = new();

    /// <summary>
    /// 对字符串执行不区分大小写的包含检查（兼容 .NET 4.5.2）
    /// </summary>
    private static bool ContainsIgnoreCase(string source, string value)
    {
        if (source == null) return false;
#if NETFRAMEWORK
        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
#else
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
#endif
    }

    private static readonly List<(string AssemblyKeyword, string FactoryTypeName, string InvariantName)> KnownProviders = new()
    {
        ("Microsoft.Data.SqlClient", "Microsoft.Data.SqlClient.SqlClientFactory", "Microsoft.Data.SqlClient"),
        ("MySql.Data", "MySql.Data.MySqlClient.MySqlClientFactory", "MySql.Data.MySqlClient"),
        ("Pomelo", "MySql.Data.MySqlClient.MySqlClientFactory", "MySql.Data.MySqlClient"),
        ("Npgsql", "Npgsql.NpgsqlFactory", "Npgsql"),
        ("Microsoft.Data.Sqlite", "Microsoft.Data.Sqlite.SqliteFactory", "Microsoft.Data.Sqlite"),
        ("System.Data.SQLite", "System.Data.SQLite.SQLiteFactory", "System.Data.SQLite"),
        ("Oracle.ManagedDataAccess", "Oracle.ManagedDataAccess.Client.OracleClientFactory", "Oracle.ManagedDataAccess.Client"),
    };

    public static void Register()
    {
        if (_isRegistered) return;

        lock (_lockObj)
        {
            if (_isRegistered) return;

            try
            {
                var registeredCount = 0;

                // 第一遍：扫描已加载的程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var (assemblyKeyword, factoryTypeName, invariantName) in KnownProviders)
                {
                    if (TryRegisterFromAssemblies(assemblies, assemblyKeyword, factoryTypeName, invariantName))
                        registeredCount++;
                }

                // 第二遍：对未注册的提供程序，显式加载程序集
                foreach (var (assemblyKeyword, factoryTypeName, invariantName) in KnownProviders)
                {
                    if (TryRegisterByAssemblyLoad(assemblyKeyword, factoryTypeName, invariantName))
                        registeredCount++;
                }

                _isRegistered = true;

                if (Environment.GetEnvironmentVariable("FASTDATA_DEBUG") == "true")
                    Console.WriteLine(string.Format("[FastData] Auto-registered {0} database provider(s)", registeredCount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("[FastData] Auto-registration warning: {0}", ex.Message));
            }
        }
    }

    private static bool TryRegisterFromAssemblies(IEnumerable<Assembly> assemblies, string assemblyKeyword, string factoryTypeName, string invariantName)
    {
        try
        {
            foreach (var assembly in assemblies)
            {
                if (!ContainsIgnoreCase(assembly.GetName().Name, assemblyKeyword))
                    continue;

                return TryRegisterFromAssembly(assembly, factoryTypeName, invariantName);
            }
        }
        catch { }

        return false;
    }

    private static bool TryRegisterByAssemblyLoad(string assemblyKeyword, string factoryTypeName, string invariantName)
    {
        try
        {
            // 先检查是否已注册
            try
            {
                var existing = DbProviderFactories.GetFactory(invariantName);
                if (existing != null) return true;
            }
            catch { }

            // 尝试按关键词加载程序集
            Assembly assembly = null;
            try { assembly = Assembly.Load(assemblyKeyword); }
            catch { }

            // 如果关键词加载失败，尝试按完整程序集名加载
            if (assembly == null)
            {
                var fallbackNames = GetFallbackAssemblyNames(assemblyKeyword);
                foreach (var name in fallbackNames)
                {
                    try { assembly = Assembly.Load(name); if (assembly != null) break; }
                    catch { }
                }
            }

            if (assembly == null) return false;

            return TryRegisterFromAssembly(assembly, factoryTypeName, invariantName);
        }
        catch { }

        return false;
    }

    private static bool TryRegisterFromAssembly(Assembly assembly, string factoryTypeName, string invariantName)
    {
        try
        {
            var factoryType = assembly.GetType(factoryTypeName);
            if (factoryType == null) return false;

            var instanceProperty = factoryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProperty == null) return false;

            var factoryInstance = instanceProperty.GetValue(null) as DbProviderFactory;
            if (factoryInstance == null) return false;

            // 检查是否已注册
            try
            {
                var existingFactory = DbProviderFactories.GetFactory(invariantName);
                if (existingFactory != null) return true;
            }
            catch { }

#if !NETFRAMEWORK
            // .NET Core/5+ 支持运行时注册
            DbProviderFactories.RegisterFactory(invariantName, factoryInstance);
#else
            // .NET Framework 需在 app.config 中配置 provider，
            // 或通过反射注册到私有字段
            RegisterFactoryNetFramework(invariantName, factoryInstance);
#endif
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string[] GetFallbackAssemblyNames(string keyword)
    {
        return keyword switch
        {
            "Microsoft.Data.SqlClient" => new[] { "Microsoft.Data.SqlClient" },
            "MySql.Data" => new[] { "MySql.Data", "MySqlConnector" },
            "Pomelo" => new[] { "MySqlConnector", "MySql.Data" },
            "Npgsql" => new[] { "Npgsql" },
            "Microsoft.Data.Sqlite" => new[] { "Microsoft.Data.Sqlite" },
            "System.Data.SQLite" => new[] { "System.Data.SQLite", "System.Data.SQLite.Core" },
            "Oracle.ManagedDataAccess" => new[] { "Oracle.ManagedDataAccess", "Oracle.ManagedDataAccess.Core" },
            _ => new[] { keyword }
        };
    }

#if NETFRAMEWORK
    /// <summary>
    /// .NET Framework 下通过反射注册 DbProviderFactory
    /// </summary>
    private static void RegisterFactoryNetFramework(string invariantName, DbProviderFactory factory)
    {
        try
        {
            var factoriesField = typeof(DbProviderFactories)
                .GetField("_configTable", BindingFlags.Static | BindingFlags.NonPublic);
            if (factoriesField == null)
            {
                // 部分实现使用不同字段名
                factoriesField = typeof(DbProviderFactories)
                    .GetField("_registeredProviders", BindingFlags.Static | BindingFlags.NonPublic);
            }
            if (factoriesField != null)
            {
                var table = factoriesField.GetValue(null) as System.Collections.Hashtable;
                if (table != null)
                {
                    table[invariantName] = factory;
                }
            }
        }
        catch { }
    }
#endif
}
