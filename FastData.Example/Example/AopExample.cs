using System;
using FastData.Aop;

namespace FastData.Example.Example
{
    /// <summary>
    /// AOP 拦截器使用示例
    /// 场景：SQL 日志、性能监控、异常处理、数据审计
    /// </summary>
    public static class AopExample
    {
        /// <summary>
        /// 运行所有 AOP 示例
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("--- AOP 拦截器使用示例 ---");
            Console.WriteLine();

            DemoBasicAop();
            DemoSqlLogging();
            DemoPerformanceMonitor();
            DemoExceptionHandling();
            DemoDataAudit();
        }

        /// <summary>
        /// 示例 1: 基本 AOP 配置
        /// 场景：注册 AOP 拦截器
        /// </summary>
        private static void DemoBasicAop()
        {
            Console.WriteLine("=== 示例 1: 基本 AOP 配置 ===");
            Console.WriteLine("场景：注册 AOP 拦截器，拦截所有数据库操作");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  // 1. 创建 AOP 拦截器实现
  public class MyAopInterceptor : IFastAop
  {
      public void Before(BeforeContext context)
      {
          Console.WriteLine($""[Before] SQL: {context.Sql}"");
          Console.WriteLine($""[Before] 数据库类型: {context.DbType}"");
          Console.WriteLine($""[Before] 是否读操作: {context.IsRead}"");
      }

      public void After(AfterContext context)
      {
          Console.WriteLine($""[After] SQL: {context.Sql}"");
          Console.WriteLine($""[After] 结果: {context.Result}"");
      }

      public void Exception(ExceptionContext context)
      {
          Console.WriteLine($""[Exception] 错误: {context.Exception.Message}"");
      }

      public void MapBefore(MapBeforeContext context)
      {
          Console.WriteLine($""[MapBefore] Map SQL: {context.MapId}"");
      }

      public void MapAfter(MapAfterContext context)
      {
          Console.WriteLine($""[MapAfter] Map SQL: {context.MapId}"");
      }
  }

  // 2. 在启动时注册 AOP
  FastMap.InstanceMap(aop: new MyAopInterceptor());

  // 3. 之后所有数据库操作都会被拦截
  var users = FastRead.Query<User>(u => u.IsActive).ToList();
  // 控制台会输出 SQL 日志");
            Console.WriteLine();

            Console.WriteLine("说明：");
            Console.WriteLine("  - IFastAop 接口定义了 5 个拦截方法");
            Console.WriteLine("  - Before/After: 拦截 Lambda 操作");
            Console.WriteLine("  - MapBefore/MapAfter: 拦截 Map SQL 操作");
            Console.WriteLine("  - Exception: 拦截异常");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 2: SQL 日志记录
        /// 场景：记录所有执行的 SQL 语句
        /// </summary>
        private static void DemoSqlLogging()
        {
            Console.WriteLine("=== 示例 2: SQL 日志记录 ===");
            Console.WriteLine("场景：记录所有执行的 SQL 语句，便于调试和审计");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  public class SqlLoggingAop : IFastAop
  {
      private readonly ILogger _logger;

      public SqlLoggingAop(ILogger logger)
      {
          _logger = logger;
      }

      public void Before(BeforeContext context)
      {
          _logger.LogInformation($""SQL 执行前: {context.Sql}"");
          _logger.LogInformation($""数据库类型: {context.DbType}"");
          _logger.LogInformation($""操作类型: {(context.IsRead ? ""读"" : ""写"")}"");
          
          if (context.Param != null)
          {
              foreach (var param in context.Param)
              {
                  _logger.LogInformation($""参数: {param.ParameterName} = {param.Value}"");
              }
          }
      }

      public void After(AfterContext context)
      {
          _logger.LogInformation($""SQL 执行后: {context.Sql}"");
          _logger.LogInformation($""执行结果: {context.Result}"");
      }

      public void Exception(ExceptionContext context)
      {
          _logger.LogError(context.Exception, $""SQL 执行异常: {context.Sql}"");
      }

      public void MapBefore(MapBeforeContext context) { }
      public void MapAfter(MapAfterContext context) { }
  }");
            Console.WriteLine();

            Console.WriteLine("最佳实践：");
            Console.WriteLine("  - 生产环境建议关闭详细日志，避免性能影响");
            Console.WriteLine("  - 可以根据日志级别动态控制日志输出");
            Console.WriteLine("  - 敏感数据（密码、密钥）需要脱敏处理");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 3: 性能监控
        /// 场景：监控 SQL 执行时间，发现慢查询
        /// </summary>
        private static void DemoPerformanceMonitor()
        {
            Console.WriteLine("=== 示例 3: 性能监控 ===");
            Console.WriteLine("场景：监控 SQL 执行时间，发现慢查询");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  public class PerformanceMonitorAop : IFastAop
  {
      private readonly ILogger _logger;
      private readonly int _slowQueryThresholdMs = 1000; // 慢查询阈值：1秒

      public PerformanceMonitorAop(ILogger logger)
      {
          _logger = logger;
      }

      public void Before(BeforeContext context)
      {
          context.Items[""StartTime""] = DateTime.Now;
      }

      public void After(AfterContext context)
      {
          if (context.Items.TryGetValue(""StartTime"", out var startTime))
          {
              var elapsed = DateTime.Now - (DateTime)startTime;
              var elapsedMs = elapsed.TotalMilliseconds;

              if (elapsedMs > _slowQueryThresholdMs)
              {
                  _logger.LogWarning($""慢查询警告: {elapsedMs:F2}ms - {context.Sql}"");
              }
              else
              {
                  _logger.LogInformation($""SQL 执行完成: {elapsedMs:F2}ms - {context.Sql}"");
              }
          }
      }

      public void Exception(ExceptionContext context)
      {
          _logger.LogError(context.Exception, $""SQL 执行异常"");
      }

      public void MapBefore(MapBeforeContext context) { }
      public void MapAfter(MapAfterContext context) { }
  }");
            Console.WriteLine();

            Console.WriteLine("性能监控指标：");
            Console.WriteLine("  - SQL 执行时间");
            Console.WriteLine("  - 慢查询统计");
            Console.WriteLine("  - 查询频率统计");
            Console.WriteLine("  - 错误率统计");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 4: 异常处理
        /// 场景：统一处理数据库异常
        /// </summary>
        private static void DemoExceptionHandling()
        {
            Console.WriteLine("=== 示例 4: 异常处理 ===");
            Console.WriteLine("场景：统一处理数据库异常，记录错误日志");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  public class ExceptionHandlingAop : IFastAop
  {
      private readonly ILogger _logger;
      private readonly IEmailService _emailService;

      public ExceptionHandlingAop(ILogger logger, IEmailService emailService)
      {
          _logger = logger;
          _emailService = emailService;
      }

      public void Exception(ExceptionContext context)
      {
          // 1. 记录详细错误日志
          _logger.LogError(context.Exception, 
              $""数据库异常:"" +
              $""\n  SQL: {context.Sql}"" +
              $""\n  数据库类型: {context.DbType}"" +
              $""\n  错误信息: {context.Exception.Message}"");

          // 2. 发送告警邮件（严重错误）
          if (IsCriticalException(context.Exception))
          {
              _emailService.SendAlert($""数据库严重异常: {context.Exception.Message}"");
          }

          // 3. 记录到监控系统
          Metrics.IncrementCounter(""database.errors"", new Dictionary<string, string>
          {
              { ""dbType"", context.DbType },
              { ""errorType"", context.Exception.GetType().Name }
          });
      }

      private bool IsCriticalException(Exception ex)
      {
          // 判断是否为严重异常
          return ex is System.Data.Common.DbException ||
                 ex is TimeoutException ||
                 ex is OutOfMemoryException;
      }

      public void Before(BeforeContext context) { }
      public void After(AfterContext context) { }
      public void MapBefore(MapBeforeContext context) { }
      public void MapAfter(MapAfterContext context) { }
  }");
            Console.WriteLine();

            Console.WriteLine("异常处理策略：");
            Console.WriteLine("  - 记录详细错误日志");
            Console.WriteLine("  - 发送告警通知");
            Console.WriteLine("  - 记录监控指标");
            Console.WriteLine("  - 根据异常类型决定是否重试");
            Console.WriteLine();
        }

        /// <summary>
        /// 示例 5: 数据审计
        /// 场景：记录数据变更历史
        /// </summary>
        private static void DemoDataAudit()
        {
            Console.WriteLine("=== 示例 5: 数据审计 ===");
            Console.WriteLine("场景：记录数据变更历史，便于审计和回溯");
            Console.WriteLine();

            Console.WriteLine("C# 代码：");
            Console.WriteLine(@"  public class DataAuditAop : IFastAop
  {
      private readonly IAuditRepository _auditRepo;
      private readonly IHttpContextAccessor _httpContextAccessor;

      public DataAuditAop(IAuditRepository auditRepo, IHttpContextAccessor httpContextAccessor)
      {
          _auditRepo = auditRepo;
          _httpContextAccessor = httpContextAccessor;
      }

      public void Before(BeforeContext context)
      {
          // 记录操作前的数据快照（仅写操作）
          if (!context.IsRead)
          {
              context.Items[""AuditSnapshot""] = CaptureSnapshot(context);
          }
      }

      public void After(AfterContext context)
      {
          // 记录审计日志（仅写操作）
          if (!context.IsRead && context.Items.ContainsKey(""AuditSnapshot""))
          {
              var auditLog = new AuditLog
              {
                  TableName = ExtractTableName(context.Sql),
                  Operation = ExtractOperation(context.Sql),
                  Sql = context.Sql,
                  BeforeSnapshot = context.Items[""AuditSnapshot""]?.ToString(),
                  AfterSnapshot = context.Result?.ToString(),
                  Operator = GetCurrentOperator(),
                  IpAddress = GetClientIp(),
                  CreateTime = DateTime.Now
              };
              _auditRepo.AddAuditLog(auditLog);
          }
      }

      public void Exception(ExceptionContext context)
      {
          // 记录异常审计日志
          var auditLog = new AuditLog
          {
              Operation = ""ERROR"",
              Sql = context.Sql,
              ErrorMessage = context.Exception.Message,
              Operator = GetCurrentOperator(),
              CreateTime = DateTime.Now
          };
          _auditRepo.AddAuditLog(auditLog);
      }

      private string GetCurrentOperator()
      {
          return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? ""System"";
      }

      private string GetClientIp()
      {
          return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
      }

      public void MapBefore(MapBeforeContext context) { }
      public void MapAfter(MapAfterContext context) { }
  }");
            Console.WriteLine();

            Console.WriteLine("审计日志字段：");
            Console.WriteLine("  - 表名、操作类型、SQL 语句");
            Console.WriteLine("  - 操作前数据快照");
            Console.WriteLine("  - 操作后数据快照");
            Console.WriteLine("  - 操作人、IP 地址、操作时间");
            Console.WriteLine();
        }
    }
}
