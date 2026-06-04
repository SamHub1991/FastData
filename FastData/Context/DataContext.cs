using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using FastUntility.Page;
using FastUntility.Base;
using FastData.Base;
using FastData.Model;
using FastData.DbTypes;
using FastData.Config;
using System.Data;
using FastData.Property;
using FastData.Aop;
using FastData.ConnectionPool;
using Microsoft.Data.Sqlite;

namespace FastData.Context
{
    /// <summary>
    /// 数据库上下文（轻量级 DbContext）
    ///
    /// 职责：
    /// 1. 管理数据库连接（支持连接池）
    /// 2. 管理 DbCommand 对象
    /// 3. 管理事务（BeginTransaction / Commit / Rollback）
    /// 4. AOP 拦截（Before / After / Error）
    /// 5. 资源释放（实现 IDisposable，遵循 RAII 模式）
    ///
    /// 注意事项：
    /// - 使用完毕后必须调用 Dispose() 释放资源
    /// - 推荐使用 using 语句确保资源被正确释放
    /// - 支持连接池：_usePool=true 时使用 PooledConnection
    /// - 兼容旧 API：暴露小写别名（config/cmd/conn）以兼容历史代码
    /// </summary>
    public partial class DataContext : IDisposable
    {
        private bool _disposed;
        private ConfigModel _config;
        private DbConnection _connection;
        private DbCommand _command;
        private DbTransaction _transaction;
        private PooledConnection _pooledConnection;
        private bool _usePool;

        public ConfigModel Config => _config;
        public ConfigModel config => _config;
        public DbCommand cmd => _command;
        public DbConnection conn => _connection;

        public void DisposeCommand(DbCommand command)
        {
            if (command == null) return;

            if (command.Parameters != null && _config != null && _config.DbType == DataDbType.Oracle)
            {
                foreach (var param in command.Parameters.Cast<DbParameter>())
                {
                    (param as IDisposable)?.Dispose();
                }
            }

            command.Parameters.Clear();
            command.Dispose();
        }

        /// <summary>
        /// Aop Before
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <param name="config"></param>
        private void AopBefore(List<string> tableName, string sql, List<DbParameter> param, ConfigModel config, bool isRead, AopType type)
        {
            if (FastMap.fastAop != null)
            {
                var context = new BeforeContext
                {
                    tableName = tableName,
                    sql = sql,
                    param = param,
                    dbType = config.DbType,
                    isRead = isRead,
                    isWrite = !isRead
                };

                FastMap.fastAop.Before(context);
            }
        }

        private void AopAfter(List<string> tableName, string sql, List<DbParameter> param, ConfigModel config, bool isRead, AopType type, object result)
        {
            if (FastMap.fastAop != null)
            {
                var context = new AfterContext
                {
                    tableName = tableName,
                    sql = sql,
                    param = param,
                    dbType = config.DbType,
                    isRead = isRead,
                    isWrite = !isRead,
                    result = result
                };

                FastMap.fastAop.After(context);
            }
        }

        private void AopException(Exception ex, string name, ConfigModel config, AopType type)
        {
            if (FastMap.fastAop != null)
            {
                var context = new ExceptionContext
                {
                    dbType = config.DbType,
                    ex = ex,
                    name = name,
                    type = type
                };
                FastMap.fastAop.Exception(context);
            }
        }

        ~DataContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                DisposeCommand(_command);

                if (_usePool && _pooledConnection != null)
                {
                    _pooledConnection.Dispose();
                    _pooledConnection = null;
                }
                else
                {
                    if (_connection != null)
                    {
                        try { _connection.Close(); } catch { }
                        _connection.Dispose();
                    }
                }
            }

            _command = null;
            _connection = null;
            _transaction = null;
            _pooledConnection = null;
            _config = null;
            _disposed = true;
        }

        public void EnsureConnectionOpen()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DataContext));

            if (_connection == null)
                throw new InvalidOperationException("Connection is null. DataContext was not initialized properly.");

            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        #region 初始化
        /// <summary>
        /// 初始化（延迟打开连接）
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="projectName">项目名称</param>
        /// <param name="poolConfig">连接池配置</param>
        public DataContext(string key = null, string projectName = null, ConnectionPoolConfig poolConfig = null)
        {
            try
            {
                _config = DataConfig.GetConfig(key, projectName);
                if (_config == null)
                    throw new InvalidOperationException($"Config is null for key={key}, project={projectName}");
                if (string.IsNullOrEmpty(_config.ProviderName))
                    throw new InvalidOperationException($"ProviderName is null for key={key}, config.Key={_config.Key}");
                
                var connStr = _config.ConnStr;
                if (_config.IsEncrypt && !string.IsNullOrEmpty(connStr))
                {
                    try
                    {
                        connStr = BaseSymmetric.Decrypto(connStr);
                    }
                    catch
                    {
                        DbLog.LogSql(true, "连接字符串解密失败，使用原始连接字符串", _config.DbType, 0);
                    }
                }

                if (poolConfig == null)
                    poolConfig = DataConfig.GetConnectionPoolConfigPublic();

                // SQLite 连接非常轻量（本质是文件句柄），无需使用连接池
                // 直接创建连接即可，避免连接池带来的序列化开销和语义问题
                if (_config.ProviderName == Provider.MicrosoftDataSqlite)
                {
                    _connection = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
                    _command = _connection.CreateCommand();
                }
                else
                {
                    var factory = DbProviderFactories.GetFactory(_config.ProviderName);
                    if (factory == null)
                        throw new InvalidOperationException($"DbProviderFactory not found for provider: {_config.ProviderName}");

                    if (poolConfig != null)
                    {
                        _usePool = true;
                        var pool = ConnectionPoolFactory.Instance.GetOrCreatePool(
                            key ?? "default",
                            () =>
                            {
                                var c = factory.CreateConnection();
                                c.ConnectionString = connStr;
                                return c;
                            },
                            poolConfig);

                        _pooledConnection = pool.GetConnection();
                        _connection = _pooledConnection.Connection;
                        _command = _connection.CreateCommand();
                    }
                    else
                    {
                        _connection = factory.CreateConnection();
                        _connection.ConnectionString = connStr;
                        _command = _connection.CreateCommand();
                    }
                }
            }
            catch (Exception ex)
            {
                AopException(ex, "DataContext :" + key, _config ?? new ConfigModel(), AopType.DataContext);

                if (_config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(_config, ex, "DataContext", "");
                else
                    DbLog.LogException(true, _config?.DbType ?? DataDbType.SqlServer, ex, "DataContext", "");
                
                throw;
            }
        }

        public void BeginTransaction()
        {
            EnsureConnectionOpen();

            if (_command == null)
                _command = _connection.CreateCommand();

            if (_command.Connection == null)
                _command.Connection = _connection;

            _transaction = _connection.BeginTransaction();
            if (_transaction != null)
                _command.Transaction = _transaction;
        }

        public void CommitTransaction()
        {
            _transaction?.Commit();
        }

        public void RollbackTransaction()
        {
            _transaction?.Rollback();
        }

        public void BeginTrans() => BeginTransaction();
        public void SubmitTrans() => CommitTransaction();
        public void RollbackTrans() => RollbackTransaction();
        #endregion
    }
}
