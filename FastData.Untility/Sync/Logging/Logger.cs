using System;
using System.IO;

namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    /// <summary>
    /// 同步工具日志记录器
    /// </summary>
    public static class Logger
    {
        private static string _logDirectory;
        private static string _logFile;
        private static LogLevel _minLevel = LogLevel.Info;
        private static readonly object LockObj = new object();

        /// <summary>
        /// 初始化日志记录器
        /// </summary>
        /// <param name="logDirectory">日志目录</param>
        /// <param name="minLevel">最低日志级别</param>
        public static void Initialize(string logDirectory, LogLevel minLevel = LogLevel.Info)
        {
            _logDirectory = logDirectory;
            _minLevel = minLevel;

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            var fileName = string.Format("sync_{0}.log", DateTime.Now.ToString("yyyyMMdd"));
            _logFile = Path.Combine(_logDirectory, fileName);
        }

        /// <summary>
        /// 设置日志文件（按任务 ID）
        /// </summary>
        /// <param name="taskId">任务 ID</param>
        public static void SetLogFile(string taskId)
        {
            if (string.IsNullOrWhiteSpace(_logDirectory))
                Initialize("./logs");

            var fileName = string.Format("sync_{0}_{1}.log", DateTime.Now.ToString("yyyyMMdd"), taskId);
            _logFile = Path.Combine(_logDirectory, fileName);
        }

        /// <summary>
        /// 记录 Debug 级别日志
        /// </summary>
        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// 记录 Info 级别日志
        /// </summary>
        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// 记录 Warn 级别日志
        /// </summary>
        public static void Warn(string message)
        {
            Log(LogLevel.Warn, message);
        }

        /// <summary>
        /// 记录 Error 级别日志
        /// </summary>
        public static void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        /// <summary>
        /// 记录 Error 级别日志（带异常）
        /// </summary>
        public static void Error(string message, Exception ex)
        {
            Log(LogLevel.Error, string.Format("{0} {1}", message, ex.ToString()));
        }

        private static void Log(LogLevel level, string message)
        {
            if (level < _minLevel)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper().PadRight(5);
            var logLine = string.Format("[{0}] [{1}] {2}", timestamp, levelStr, message);

            lock (LockObj)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_logFile))
                    {
                        _logDirectory = "./logs";
                        if (!Directory.Exists(_logDirectory))
                            Directory.CreateDirectory(_logDirectory);
                        _logFile = Path.Combine(_logDirectory, string.Format("sync_{0}.log", DateTime.Now.ToString("yyyyMMdd")));
                    }

                    File.AppendAllText(_logFile, logLine + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[LOGGER ERROR] Failed to write log: " + ex.Message);
                }
            }

            if (_minLevel <= LogLevel.Debug)
            {
                Console.WriteLine(logLine);
            }
        }
    }
}
