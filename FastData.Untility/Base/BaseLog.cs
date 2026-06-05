namespace FastUntility.Base
{
    /// <summary>
    /// 日志操作类
    /// 封装 LogManager 的同步和异步写日志功能
    /// 
    /// 日志功能特性：
    /// 1. 按日期轮转：每天自动创建新的日志目录（格式：yyyy-MM-dd）
    /// 2. 按大小轮转：当日志文件超过指定大小时，自动创建新文件（后缀 _1, _2, ...）
    /// 3. 自动清理：保留最近 7 天的日志，自动删除过期日志
    /// 
    /// 配置说明：
    /// - LogManager.MaxFileSizeBytes：单个日志文件最大大小（默认 10MB）
    /// - LogManager.RetentionDays：日志保留天数（默认 7 天）
    /// - LogManager.LogRootDirectory：自定义日志根目录（默认为 App_Data/log）
    /// </summary>
    public static class BaseLog
    {
        /// <summary>
        /// 同步写日志
        /// </summary>
        /// <param name="logContent">日志内容</param>
        /// <param name="fileName">日志文件名（不含扩展名）</param>
        public static void SaveLog(string logContent, string fileName)
        {
            if (string.IsNullOrEmpty(logContent) || string.IsNullOrEmpty(fileName))
                return;

            LogManager.SaveLog(logContent, fileName);
        }

        /// <summary>
        /// 异步写日志
        /// </summary>
        /// <param name="logContent">日志内容</param>
        /// <param name="fileName">日志文件名（不含扩展名）</param>
        public static void SaveLogAsync(string logContent, string fileName)
        {
            if (string.IsNullOrEmpty(logContent) || string.IsNullOrEmpty(fileName))
                return;

            LogManager.SaveLogAsync(logContent, fileName);
        }
    }
}
