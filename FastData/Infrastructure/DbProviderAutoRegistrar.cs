using System.Data.Common;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

namespace FastData.Infrastructure;

/// <summary>
/// 数据库提供程序自动注册器
/// </summary>
public static class DbProviderAutoRegistrar
{
    private static bool _isRegistered = false;
    private static readonly object _lockObj = new();

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
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var (assemblyKeyword, factoryTypeName, invariantName) in KnownProviders)
                {
                    if (TryRegisterProvider(assemblies, assemblyKeyword, factoryTypeName, invariantName))
                        registeredCount++;
                }

                _isRegistered = true;

                if (Environment.GetEnvironmentVariable("FASTDATA_DEBUG") == "true")
                    Console.WriteLine($"[FastData] Auto-registered {registeredCount} database provider(s)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FastData] Auto-registration warning: {ex.Message}");
            }
        }
    }

    private static bool TryRegisterProvider(IEnumerable<Assembly> assemblies, string assemblyKeyword, string factoryTypeName, string invariantName)
    {
        try
        {
            foreach (var assembly in assemblies)
            {
                if (!assembly.GetName().Name?.Contains(assemblyKeyword, StringComparison.OrdinalIgnoreCase) == true)
                    continue;

                var factoryType = assembly.GetType(factoryTypeName);
                if (factoryType == null) continue;

                var instanceProperty = factoryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty == null) continue;

                var factoryInstance = instanceProperty.GetValue(null) as DbProviderFactory;
                if (factoryInstance == null) continue;

                try
                {
                    var existingFactory = DbProviderFactories.GetFactory(invariantName);
                    if (existingFactory != null) return true;
                }
                catch { }

                DbProviderFactories.RegisterFactory(invariantName, factoryInstance);
                return true;
            }

            foreach (var asmKeyword in new[] { assemblyKeyword, "Microsoft.Data.SqlClient", "Npgsql", "MySql.Data" })
            {
                try
                {
                    var loadedAssembly = Assembly.Load(asmKeyword);
                    var factoryType = loadedAssembly.GetType(factoryTypeName);
                    if (factoryType == null) continue;

                    var instanceProperty = factoryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var factoryInstance = instanceProperty?.GetValue(null) as DbProviderFactory;
                    if (factoryInstance != null)
                    {
                        DbProviderFactories.RegisterFactory(invariantName, factoryInstance);
                        return true;
                    }
                }
                catch { }
            }
        }
        catch { }

        return false;
    }
}
