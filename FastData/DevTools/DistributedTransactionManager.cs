using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FastData.Config;
using FastData.Model;

namespace FastData.DevTools
{
    /// <summary>
    /// 分布式事务管理器
    /// </summary>
    public static class DistributedTransactionManager
    {
        private static readonly Dictionary<string, TransactionContext> _activeTransactions = new Dictionary<string, TransactionContext>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 开始分布式事务
        /// </summary>
        public static DistributedTransaction BeginTransaction(params string[] dbKeys)
        {
            if (dbKeys == null || dbKeys.Length == 0)
            {
                throw new ArgumentException("至少需要一个数据库连接");
            }

            var transactionId = Guid.NewGuid().ToString();
            var contexts = new List<TransactionContext>();

            foreach (var dbKey in dbKeys)
            {
                var config = DataConfig.GetConfig(dbKey);
                if (config == null)
                {
                    RollbackTransaction(transactionId);
                    throw new ArgumentException($"找不到数据库配置: {dbKey}");
                }

                var context = new TransactionContext
                {
                    DbKey = dbKey,
                    Config = config
                };

                var db = new DataContext(dbKey);
                context.Connection = db.conn;
                context.Transaction = db.conn.BeginTransaction();
                contexts.Add(context);
            }

            var distributedTx = new DistributedTransaction
            {
                TransactionId = transactionId,
                Contexts = contexts,
                Status = TransactionStatus.Active
            };

            lock (_lock)
            {
                _activeTransactions[transactionId] = distributedTx;
            }

            return distributedTx;
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public static Result CommitTransaction(string transactionId)
        {
            DistributedTransaction distributedTx;

            lock (_lock)
            {
                if (!_activeTransactions.TryGetValue(transactionId, out distributedTx))
                {
                    return Result.Error("事务不存在");
                }
            }

            try
            {
                // 两阶段提交协议
                // 阶段 1: 准备阶段
                foreach (var context in distributedTx.Contexts)
                {
                    try
                    {
                        if (context.Connection != null && context.Transaction != null)
                        {
                            context.Transaction.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        // 准备阶段失败，执行回滚
                        RollbackTransaction(transactionId);
                        return Result.Error($"事务提交失败: {ex.Message}");
                    }
                }

                distributedTx.Status = TransactionStatus.Committed;

                lock (_lock)
                {
                    _activeTransactions.Remove(transactionId);
                }

                Cleanup(distributedTx);

                return Result.Success();
            }
            catch (Exception ex)
            {
                RollbackTransaction(transactionId);
                return Result.Error($"事务提交异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public static Result RollbackTransaction(string transactionId)
        {
            DistributedTransaction distributedTx;

            lock (_lock)
            {
                if (!_activeTransactions.TryGetValue(transactionId, out distributedTx))
                {
                    return Result.Error("事务不存在");
                }
            }

            try
            {
                foreach (var context in distributedTx.Contexts)
                {
                    try
                    {
                        if (context.Connection != null && context.Transaction != null)
                        {
                            context.Transaction.Rollback();
                        }
                    }
                    catch
                    {
                        // 忽略回滚错误
                    }
                }

                distributedTx.Status = TransactionStatus.RolledBack;

                lock (_lock)
                {
                    _activeTransactions.Remove(transactionId);
                }

                Cleanup(distributedTx);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Error($"事务回滚异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取事务上下文
        /// </summary>
        public static DistributedTransaction GetTransaction(string transactionId)
        {
            lock (_lock)
            {
                return _activeTransactions.TryGetValue(transactionId, out var tx) ? tx : null;
            }
        }

        /// <summary>
        /// 检查事务是否活动
        /// </summary>
        public static bool IsActive(string transactionId)
        {
            lock (_lock)
            {
                return _activeTransactions.ContainsKey(transactionId);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private static void Cleanup(DistributedTransaction distributedTx)
        {
            foreach (var context in distributedTx.Contexts)
            {
                try
                {
                    context.Transaction?.Dispose();
                    context.Connection?.Close();
                    context.Connection?.Dispose();
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }

        /// <summary>
        /// 获取所有活动事务
        /// </summary>
        public static List<DistributedTransaction> GetActiveTransactions()
        {
            lock (_lock)
            {
                return _activeTransactions.Values.ToList();
            }
        }

        /// <summary>
        /// 清理所有活动事务
        /// </summary>
        public static void CleanupAll()
        {
            List<string> transactionIds;

            lock (_lock)
            {
                transactionIds = _activeTransactions.Keys.ToList();
            }

            foreach (var transactionId in transactionIds)
            {
                RollbackTransaction(transactionId);
            }
        }
    }

    /// <summary>
    /// 分布式事务
    /// </summary>
    public class DistributedTransaction
    {
        public string TransactionId { get; set; }
        public List<TransactionContext> Contexts { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 提交事务
        /// </summary>
        public Result Commit()
        {
            return DistributedTransactionManager.CommitTransaction(TransactionId);
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public Result Rollback()
        {
            return DistributedTransactionManager.RollbackTransaction(TransactionId);
        }

        /// <summary>
        /// 获取指定数据库的命令
        /// </summary>
        public IDbCommand GetCommand(string dbKey)
        {
            var context = Contexts.FirstOrDefault(c => c.DbKey == dbKey);
            if (context == null)
            {
                throw new ArgumentException($"数据库连接不在事务中: {dbKey}");
            }

            var command = context.Connection.CreateCommand();
            command.Transaction = context.Transaction;
            return command;
        }

        /// <summary>
        /// 在事务中执行操作
        /// </summary>
        public Result ExecuteInTransaction(string dbKey, Action<IDbCommand> action)
        {
            var command = GetCommand(dbKey);
            try
            {
                action(command);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Error(ex.Message);
            }
        }

        /// <summary>
        /// 在事务中执行查询
        /// </summary>
        public List<T> QueryInTransaction<T>(string dbKey, Func<IDbCommand, List<T>> queryFunc)
        {
            var command = GetCommand(dbKey);
            return queryFunc(command);
        }
    }

    /// <summary>
    /// 事务上下文
    /// </summary>
    public class TransactionContext
    {
        public string DbKey { get; set; }
        public DataConfig Config { get; set; }
        public IDbConnection Connection { get; set; }
        public IDbTransaction Transaction { get; set; }
    }

    /// <summary>
    /// 事务状态
    /// </summary>
    public enum TransactionStatus
    {
        Active,
        Committed,
        RolledBack,
        Failed
    }

    /// <summary>
    /// 事务作用域
    /// </summary>
    public class TransactionScope : IDisposable
    {
        private readonly DistributedTransaction _transaction;
        private readonly bool _autoCommit;
        private bool _committed;
        private bool _disposed;

        public TransactionScope(params string[] dbKeys)
            : this(dbKeys, true)
        {
        }

        public TransactionScope(string[] dbKeys, bool autoCommit)
        {
            _autoCommit = autoCommit;
            _transaction = DistributedTransactionManager.BeginTransaction(dbKeys);
        }

        public DistributedTransaction Transaction => _transaction;

        public Result Commit()
        {
            if (_committed)
            {
                return Result.Error("事务已提交");
            }

            var result = _transaction.Commit();
            _committed = true;
            return result;
        }

        public void Complete()
        {
            if (!_committed && _autoCommit)
            {
                Commit();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            if (!_committed)
            {
                _transaction.Rollback();
            }
        }
    }

    /// <summary>
    /// 分布式事务辅助类
    /// </summary>
    public static class DistributedTransactionHelper
    {
        /// <summary>
        /// 在单个事务中执行多个数据库操作
        /// </summary>
        public static Result ExecuteInSingleTransaction(string[] dbKeys, Action<DistributedTransaction> action)
        {
            var transaction = DistributedTransactionManager.BeginTransaction(dbKeys);

            try
            {
                action(transaction);

                if (transaction.Status == TransactionStatus.Active)
                {
                    return transaction.Commit();
                }

                return Result.Error("事务状态异常");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Result.Error(ex.Message);
            }
        }

        /// <summary>
        /// 使用事务作用域执行操作
        /// </summary>
        public static Result ExecuteInScope(string[] dbKeys, Action<DistributedTransaction> action)
        {
            using (var scope = new TransactionScope(dbKeys))
            {
                try
                {
                    action(scope.Transaction);
                    scope.Complete();
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    return Result.Error(ex.Message);
                }
            }
        }

        /// <summary>
        /// 异步执行分布式事务
        /// </summary>
        public static async Task<Result> ExecuteAsync(string[] dbKeys, Func<DistributedTransaction, Task<Result>> action)
        {
            return await Task.Run(() =>
            {
                var transaction = DistributedTransactionManager.BeginTransaction(dbKeys);

                try
                {
                    var result = action(transaction).Result;

                    if (result.IsSuccess && transaction.Status == TransactionStatus.Active)
                    {
                        return transaction.Commit();
                    }

                    transaction.Rollback();
                    return result;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Result.Error(ex.Message);
                }
            });
        }
    }
}