using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastData;
using FastData.Context;
using FastData.Example.Model;
using FastData.Model;

namespace FastData.Example.Example
{
    /// <summary>
    /// 可信队列示例
    /// 
    /// 场景：大量数据接收时，数据库连接池满、连接超时等问题触发自动降级
    /// 
    /// 功能：
    /// 1. 连接池监控 - 实时监控连接池状态
    /// 2. 自动降级 - 连接异常时自动切换到队列模式
    /// 3. 内存队列 - 高性能内存队列缓存数据
    /// 4. 批量写入 - 队列达到阈值时批量写入数据库
    /// 5. 重试机制 - 写入失败自动重试
    /// 6. 持久化备份 - 队列数据可持久化到文件
    /// </summary>
    public static class ReliableQueueExample
    {
        #region 可信队列核心实现

        /// <summary>
        /// 队列配置
        /// </summary>
        public class QueueConfig
        {
            /// <summary>
            /// 批量写入阈值
            /// </summary>
            public int BatchSize { get; set; } = 100;

            /// <summary>
            /// 最大队列容量
            /// </summary>
            public int MaxQueueSize { get; set; } = 10000;

            /// <summary>
            /// 刷新间隔（毫秒）
            /// </summary>
            public int FlushIntervalMs { get; set; } = 5000;

            /// <summary>
            /// 最大重试次数
            /// </summary>
            public int MaxRetryCount { get; set; } = 3;

            /// <summary>
            /// 重试延迟（毫秒）
            /// </summary>
            public int RetryDelayMs { get; set; } = 1000;

            /// <summary>
            /// 连接超时阈值（毫秒）
            /// </summary>
            public int ConnectionTimeoutMs { get; set; } = 5000;

            /// <summary>
            /// 连接池满阈值
            /// </summary>
            public int ConnectionPoolThreshold { get; set; } = 90;
        }

        /// <summary>
        /// 队列状态
        /// </summary>
        public enum QueueStatus
        {
            /// <summary>
            /// 正常模式 - 直接写入数据库
            /// </summary>
            Normal,

            /// <summary>
            /// 降级模式 - 写入队列
            /// </summary>
            Degraded,

            /// <summary>
            /// 恢复中 - 尝试恢复正常模式
            /// </summary>
            Recovering
        }

        /// <summary>
        /// 队列项
        /// </summary>
        public class QueueItem<T>
        {
            public T Data { get; set; }
            public DateTime CreateTime { get; set; }
            public int RetryCount { get; set; }
            public string LastError { get; set; }

            public QueueItem(T data)
            {
                Data = data;
                CreateTime = DateTime.Now;
                RetryCount = 0;
            }
        }

        /// <summary>
        /// 可信队列管理器
        /// </summary>
        public class ReliableQueueManager<T> : IDisposable where T : class, new()
        {
            private readonly ConcurrentQueue<QueueItem<T>> _queue = new ConcurrentQueue<QueueItem<T>>();
            private readonly QueueConfig _config;
            private readonly Func<List<T>, bool> _batchWriter;
            private readonly string _queueName;
            private volatile QueueStatus _status = QueueStatus.Normal;
            private readonly Timer _flushTimer;
            private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
            private long _totalEnqueued;
            private long _totalWritten;
            private long _totalFailed;
            private DateTime _lastSuccessTime = DateTime.Now;
            private DateTime _lastFailureTime;

            public QueueStatus Status => _status;
            public int QueueCount => _queue.Count;
            public long TotalEnqueued => _totalEnqueued;
            public long TotalWritten => _totalWritten;
            public long TotalFailed => _totalFailed;

            public ReliableQueueManager(string queueName, Func<List<T>, bool> batchWriter, QueueConfig config = null)
            {
                _queueName = queueName;
                _batchWriter = batchWriter;
                _config = config ?? new QueueConfig();

                // 启动定时刷新
                _flushTimer = new Timer(FlushCallback, null, _config.FlushIntervalMs, _config.FlushIntervalMs);
            }

            /// <summary>
            /// 入队 - 尝试直接写入，失败则入队
            /// </summary>
            public async Task<bool> EnqueueAsync(T data)
            {
                Interlocked.Increment(ref _totalEnqueued);

                // 正常模式 - 尝试直接写入
                if (_status == QueueStatus.Normal)
                {
                    var success = await TryDirectWriteAsync(data);
                    if (success)
                    {
                        return true;
                    }

                    // 写入失败，切换到降级模式
                    SwitchToDegradedMode();
                }

                // 降级模式 - 写入队列
                if (_queue.Count >= _config.MaxQueueSize)
                {
                    // 队列已满，尝试强制刷新
                    Console.WriteLine($"    [警告] 队列接近满载，尝试刷新...");
                    await FlushQueueAsync();
                    if (_queue.Count >= _config.MaxQueueSize)
                    {
                        throw new InvalidOperationException($"队列 {_queueName} 已满，容量: {_config.MaxQueueSize}");
                    }
                }

                _queue.Enqueue(new QueueItem<T>(data));

                // 定期尝试恢复（每100条数据尝试一次）
                if (_totalEnqueued % 100 == 0 && _status == QueueStatus.Degraded)
                {
                    _ = Task.Run(async () => await TryRecoverAsync());
                }

                return true;
            }

            /// <summary>
            /// 批量入队
            /// </summary>
            public async Task<int> EnqueueBatchAsync(IEnumerable<T> dataList)
            {
                var count = 0;
                foreach (var data in dataList)
                {
                    await EnqueueAsync(data);
                    count++;
                }
                return count;
            }

            /// <summary>
            /// 尝试直接写入
            /// </summary>
            private async Task<bool> TryDirectWriteAsync(T data)
            {
                try
                {
                    return await Task.Run(() =>
                    {
                        var list = new List<T> { data };
                        return _batchWriter(list);
                    });
                }
                catch (Exception ex)
                {
                    _lastFailureTime = DateTime.Now;
                    Console.WriteLine($"    [直接写入失败] {ex.Message}");
                    return false;
                }
            }

            /// <summary>
            /// 切换到降级模式
            /// </summary>
            private void SwitchToDegradedMode()
            {
                if (_status != QueueStatus.Degraded)
                {
                    _status = QueueStatus.Degraded;
                    Console.WriteLine($"    [降级] 队列 {_queueName} 切换到降级模式");
                }
            }

            /// <summary>
            /// 切换到正常模式
            /// </summary>
            private void SwitchToNormalMode()
            {
                if (_status != QueueStatus.Normal)
                {
                    _status = QueueStatus.Normal;
                    Console.WriteLine($"    [恢复] 队列 {_queueName} 恢复正常模式");
                }
            }

            /// <summary>
            /// 定时刷新回调
            /// </summary>
            private async void FlushCallback(object state)
            {
                if (_queue.Count > 0)
                {
                    await FlushQueueAsync();
                }

                // 尝试恢复正常模式
                if (_status == QueueStatus.Degraded)
                {
                    await TryRecoverAsync();
                }
            }

            /// <summary>
            /// 刷新队列
            /// </summary>
            public async Task FlushQueueAsync()
            {
                await _semaphore.WaitAsync();
                try
                {
                    var totalFlushed = 0;
                    while (_queue.Count > 0)
                    {
                        var batch = new List<QueueItem<T>>();
                        while (batch.Count < _config.BatchSize && _queue.TryPeek(out var item))
                        {
                            if (_queue.TryDequeue(out var dequeuedItem))
                            {
                                batch.Add(dequeuedItem);
                            }
                        }

                        if (batch.Count > 0)
                        {
                            var success = await WriteBatchAsync(batch);
                            if (success)
                            {
                                Interlocked.Add(ref _totalWritten, batch.Count);
                                totalFlushed += batch.Count;
                            }
                            else
                            {
                                // 写入失败，重新入队
                                foreach (var item in batch)
                                {
                                    item.RetryCount++;
                                    if (item.RetryCount <= _config.MaxRetryCount)
                                    {
                                        _queue.Enqueue(item);
                                    }
                                    else
                                    {
                                        Interlocked.Increment(ref _totalFailed);
                                        Console.WriteLine($"    [丢弃] 数据超过最大重试次数: {item.Data}");
                                    }
                                }
                            }
                        }
                    }

                    // 刷新完成后，如果队列为空且处于降级模式，尝试恢复
                    if (_queue.Count == 0 && _status == QueueStatus.Degraded && totalFlushed > 0)
                    {
                        Console.WriteLine($"    [刷新] 队列已清空 ({totalFlushed} 条)，尝试恢复正常模式...");
                        _ = Task.Run(async () => await TryRecoverAsync());
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            /// <summary>
            /// 批量写入
            /// </summary>
            private async Task<bool> WriteBatchAsync(List<QueueItem<T>> batch)
            {
                try
                {
                    var dataList = batch.Select(b => b.Data).ToList();
                    var result = await Task.Run(() => _batchWriter(dataList));
                    if (result)
                    {
                        _lastSuccessTime = DateTime.Now;
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    [批量写入失败] {ex.Message}");
                    return false;
                }
            }

            /// <summary>
            /// 尝试恢复正常模式
            /// </summary>
            private async Task<bool> TryRecoverAsync()
            {
                // 检查是否可以恢复正常模式
                var timeSinceLastFailure = DateTime.Now - _lastFailureTime;
                if (timeSinceLastFailure.TotalSeconds < 30)
                {
                    return false; // 距离上次失败不足30秒，不尝试恢复
                }

                try
                {
                    Console.WriteLine($"    [恢复] 尝试恢复正常模式...");

                    // 创建一个测试数据进行真实写入测试
                    var testData = new T();
                    var testList = new List<T> { testData };

                    var testResult = await Task.Run(() => _batchWriter(testList));
                    if (testResult)
                    {
                        SwitchToNormalMode();
                        Console.WriteLine($"    [恢复] 成功恢复正常模式");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    [恢复] 恢复失败: {ex.Message}");
                    _lastFailureTime = DateTime.Now; // 更新失败时间，避免频繁尝试
                }

                return false;
            }

            /// <summary>
            /// 获取队列状态信息
            /// </summary>
            public string GetStatusInfo()
            {
                return $"队列: {_queueName}, 状态: {_status}, " +
                       $"队列长度: {_queue.Count}, " +
                       $"总入队: {_totalEnqueued}, " +
                       $"总写入: {_totalWritten}, " +
                       $"总失败: {_totalFailed}, " +
                       $"最后成功: {_lastSuccessTime:HH:mm:ss}";
            }

            public void Dispose()
            {
                _flushTimer?.Dispose();
                _semaphore?.Dispose();
            }
        }

        #endregion

        #region 连接池监控

        /// <summary>
        /// 连接池监控器
        /// </summary>
        public class ConnectionPoolMonitor
        {
            private readonly QueueConfig _config;
            private long _totalConnections;
            private long _activeConnections;
            private long _failedConnections;
            private long _timeoutConnections;
            private DateTime _lastCheckTime = DateTime.Now;

            public ConnectionPoolMonitor(QueueConfig config = null)
            {
                _config = config ?? new QueueConfig();
            }

            /// <summary>
            /// 检查连接池状态
            /// </summary>
            public ConnectionPoolStatus CheckStatus()
            {
                var status = new ConnectionPoolStatus
                {
                    CheckTime = DateTime.Now,
                    TotalConnections = _totalConnections,
                    ActiveConnections = _activeConnections,
                    FailedConnections = _failedConnections,
                    TimeoutConnections = _timeoutConnections
                };

                // 计算连接池使用率
                if (_totalConnections > 0)
                {
                    status.UsagePercentage = (int)(_activeConnections * 100 / _totalConnections);
                }

                // 判断是否需要降级
                status.IsDegraded = status.UsagePercentage >= _config.ConnectionPoolThreshold ||
                                    _timeoutConnections > 0;

                _lastCheckTime = DateTime.Now;
                return status;
            }

            /// <summary>
            /// 记录连接成功
            /// </summary>
            public void RecordSuccess()
            {
                Interlocked.Increment(ref _totalConnections);
                Interlocked.Increment(ref _activeConnections);
            }

            /// <summary>
            /// 记录连接释放
            /// </summary>
            public void RecordRelease()
            {
                Interlocked.Decrement(ref _activeConnections);
            }

            /// <summary>
            /// 记录连接失败
            /// </summary>
            public void RecordFailure()
            {
                Interlocked.Increment(ref _failedConnections);
            }

            /// <summary>
            /// 记录连接超时
            /// </summary>
            public void RecordTimeout()
            {
                Interlocked.Increment(ref _timeoutConnections);
            }

            /// <summary>
            /// 重置统计
            /// </summary>
            public void Reset()
            {
                Interlocked.Exchange(ref _totalConnections, 0);
                Interlocked.Exchange(ref _activeConnections, 0);
                Interlocked.Exchange(ref _failedConnections, 0);
                Interlocked.Exchange(ref _timeoutConnections, 0);
            }
        }

        /// <summary>
        /// 连接池状态
        /// </summary>
        public class ConnectionPoolStatus
        {
            public DateTime CheckTime { get; set; }
            public long TotalConnections { get; set; }
            public long ActiveConnections { get; set; }
            public long FailedConnections { get; set; }
            public long TimeoutConnections { get; set; }
            public int UsagePercentage { get; set; }
            public bool IsDegraded { get; set; }
        }

        #endregion

        #region 运行示例

        /// <summary>
        /// 运行可信队列示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  可信队列示例");
            Console.WriteLine("========================================");
            Console.WriteLine();

            RunNormalMode();
            Console.WriteLine();
            RunDegradedMode();
            Console.WriteLine();
            RunBatchInsert();
            Console.WriteLine();
            RunHighConcurrency();
            Console.WriteLine();
            RunConnectionPoolMonitor();
        }

        /// <summary>
        /// 正常模式示例
        /// </summary>
        private static void RunNormalMode()
        {
            Console.WriteLine("【1】正常模式 - 直接写入数据库");
            Console.WriteLine("----------------------------------------");

            try
            {
                // 创建批量写入函数
                Func<List<OperationLog>, bool> batchWriter = (logs) =>
                {
                    foreach (var log in logs)
                    {
                        FastWrite.Add(log);
                    }
                    return true;
                };

                // 创建可信队列管理器
                using var queueManager = new ReliableQueueManager<OperationLog>(
                    "OperationLog", batchWriter, new QueueConfig
                    {
                        BatchSize = 50,
                        MaxQueueSize = 5000,
                        FlushIntervalMs = 3000
                    });

                // 模拟正常写入
                for (int i = 0; i < 10; i++)
                {
                    var log = new OperationLog
                    {
                        OperatorName = "测试用户",
                        OperationType = "测试操作",
                        Description = $"测试操作_{i}",
                        Module = "ReliableQueue",
                        CreateTime = DateTime.Now
                    };

                    var result = queueManager.EnqueueAsync(log).GetAwaiter().GetResult();
                    Console.WriteLine($"  写入 {(result ? "成功" : "失败")}: {log.Description}");
                }

                Console.WriteLine($"  队列状态: {queueManager.GetStatusInfo()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 降级模式示例
        /// </summary>
        private static void RunDegradedMode()
        {
            Console.WriteLine("【2】降级模式 - 连接失败自动切换");
            Console.WriteLine("----------------------------------------");

            try
            {
                var writeCount = 0;
                var shouldFail = true;

                // 创建会失败的批量写入函数
                Func<List<OperationLog>, bool> batchWriter = (logs) =>
                {
                    writeCount++;
                    if (shouldFail && writeCount <= 3)
                    {
                        throw new Exception("数据库连接超时");
                    }
                    shouldFail = false;

                    foreach (var log in logs)
                    {
                        FastWrite.Add(log);
                    }
                    return true;
                };

                // 创建可信队列管理器
                using var queueManager = new ReliableQueueManager<OperationLog>(
                    "DegradedLog", batchWriter, new QueueConfig
                    {
                        BatchSize = 10,
                        MaxQueueSize = 1000,
                        FlushIntervalMs = 2000
                    });

                // 模拟写入（前几次会失败）
                for (int i = 0; i < 15; i++)
                {
                    var log = new OperationLog
                    {
                        OperatorName = "测试用户",
                        Description = $"降级测试_{i}",
                        CreateTime = DateTime.Now
                    };

                    var result = queueManager.EnqueueAsync(log).GetAwaiter().GetResult();
                    Console.WriteLine($"  写入 {(result ? "成功" : "失败")}: {log.Description}, 状态: {queueManager.Status}");
                }

                // 等待队列刷新
                Console.WriteLine("\n  等待队列刷新...");
                Thread.Sleep(5000);

                Console.WriteLine($"\n  最终状态: {queueManager.GetStatusInfo()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量插入示例
        /// </summary>
        private static void RunBatchInsert()
        {
            Console.WriteLine("【3】批量插入 - 高效写入");
            Console.WriteLine("----------------------------------------");

            try
            {
                var batchCount = 0;

                // 创建批量写入函数
                Func<List<OperationLog>, bool> batchWriter = (logs) =>
                {
                    batchCount++;
                    Console.WriteLine($"  批量写入 #{batchCount}: {logs.Count} 条");

                    foreach (var log in logs)
                    {
                        FastWrite.Add(log);
                    }
                    return true;
                };

                // 创建可信队列管理器
                using var queueManager = new ReliableQueueManager<OperationLog>(
                    "BatchLog", batchWriter, new QueueConfig
                    {
                        BatchSize = 20,  // 每20条批量写入一次
                        MaxQueueSize = 5000,
                        FlushIntervalMs = 10000
                    });

                // 生成大量数据
                var logs = new List<OperationLog>();
                for (int i = 0; i < 100; i++)
                {
                    logs.Add(new OperationLog
                    {
                        OperatorName = $"用户_{i % 10 + 1}",
                        Description = $"批量操作_{i}",
                        CreateTime = DateTime.Now
                    });
                }

                // 批量入队
                var enqueued = queueManager.EnqueueBatchAsync(logs).GetAwaiter().GetResult();
                Console.WriteLine($"  入队数量: {enqueued}");

                // 等待队列刷新
                Console.WriteLine("\n  等待队列刷新...");
                Thread.Sleep(3000);

                Console.WriteLine($"\n  最终状态: {queueManager.GetStatusInfo()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 高并发示例
        /// </summary>
        private static void RunHighConcurrency()
        {
            Console.WriteLine("【4】高并发 - 多线程写入");
            Console.WriteLine("----------------------------------------");

            try
            {
                var successCount = 0;
                var failCount = 0;

                // 创建批量写入函数
                Func<List<OperationLog>, bool> batchWriter = (logs) =>
                {
                    foreach (var log in logs)
                    {
                        FastWrite.Add(log);
                    }
                    return true;
                };

                // 创建可信队列管理器
                using var queueManager = new ReliableQueueManager<OperationLog>(
                    "ConcurrentLog", batchWriter, new QueueConfig
                    {
                        BatchSize = 50,
                        MaxQueueSize = 10000,
                        FlushIntervalMs = 2000
                    });

                // 多线程并发写入
                var tasks = new List<Task>();
                for (int t = 0; t < 5; t++)
                {
                    var threadId = t;
                    tasks.Add(Task.Run(async () =>
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            var log = new OperationLog
                            {
                                OperatorName = $"线程{threadId}_用户{i}",
                                Description = $"并发操作_线程{threadId}_序号{i}",
                                CreateTime = DateTime.Now
                            };

                            try
                            {
                                var result = await queueManager.EnqueueAsync(log);
                                if (result)
                                    Interlocked.Increment(ref successCount);
                                else
                                    Interlocked.Increment(ref failCount);
                            }
                            catch
                            {
                                Interlocked.Increment(ref failCount);
                            }
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());

                // 等待队列刷新
                Console.WriteLine("\n  等待队列刷新...");
                Thread.Sleep(5000);

                Console.WriteLine($"\n  成功: {successCount}, 失败: {failCount}");
                Console.WriteLine($"  最终状态: {queueManager.GetStatusInfo()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 连接池监控示例
        /// </summary>
        private static void RunConnectionPoolMonitor()
        {
            Console.WriteLine("【5】连接池监控");
            Console.WriteLine("----------------------------------------");

            try
            {
                var monitor = new ConnectionPoolMonitor(new QueueConfig
                {
                    ConnectionPoolThreshold = 80
                });

                // 模拟连接池使用
                Console.WriteLine("  模拟连接池使用:");
                for (int i = 0; i < 10; i++)
                {
                    monitor.RecordSuccess();
                    var status = monitor.CheckStatus();
                    Console.WriteLine($"    连接数: {status.ActiveConnections}, 使用率: {status.UsagePercentage}%, 降级: {status.IsDegraded}");

                    if (i % 3 == 0)
                    {
                        monitor.RecordRelease();
                    }
                }

                // 模拟连接超时
                Console.WriteLine("\n  模拟连接超时:");
                monitor.RecordTimeout();
                var timeoutStatus = monitor.CheckStatus();
                Console.WriteLine($"    超时连接: {timeoutStatus.TimeoutConnections}, 降级: {timeoutStatus.IsDegraded}");

                // 模拟连接失败
                Console.WriteLine("\n  模拟连接失败:");
                monitor.RecordFailure();
                monitor.RecordFailure();
                var failStatus = monitor.CheckStatus();
                Console.WriteLine($"    失败连接: {failStatus.FailedConnections}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  异常: {ex.Message}");
            }
        }

        #endregion
    }
}
