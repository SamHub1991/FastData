using System;

namespace FastData
{
    /// <summary>
    /// FastData 错误码
    /// </summary>
    public enum FastDataErrorCode
    {
        /// <summary>Database configuration was not found.</summary>
        ConfigNotFound = 1001,        // 配置未找到
        /// <summary>Database connection failed.</summary>
        ConnectionFailed = 1002,      // 连接失败
        /// <summary>Query execution exceeded the configured timeout.</summary>
        QueryTimeout = 1003,          // 查询超时
        /// <summary>Input validation failed.</summary>
        ValidationFailed = 1004,      // 验证失败
        /// <summary>Transaction execution failed.</summary>
        TransactionFailed = 1005,     // 事务失败
        /// <summary>The requested entity was not found.</summary>
        EntityNotFound = 1006,        // 实体未找到
        /// <summary>A database constraint was violated.</summary>
        ConstraintViolation = 1007,   // 约束违反
        /// <summary>An internal FastData error occurred.</summary>
        InternalError = 1008,         // 内部错误
    }

    /// <summary>
    /// FastData 结果包装类
    /// </summary>
    public class Result
    {
        /// <summary>Gets or sets whether the operation succeeded.</summary>
        public bool Success { get; set; }
        /// <summary>Gets or sets the operation message.</summary>
        public string Message { get; set; }
        /// <summary>Gets or sets the optional FastData error code.</summary>
        public FastDataErrorCode? Code { get; set; }

        /// <summary>Creates a successful result.</summary>
        public static Result Ok() => new Result { Success = true, Message = "Success" };
        /// <summary>Creates a failed result.</summary>
        /// <param name="message">Failure message.</param>
        /// <param name="code">Optional error code.</param>
        public static Result Fail(string message, FastDataErrorCode? code = null) => new Result { Success = false, Message = message, Code = code };
    }

    /// <summary>
    /// FastData 结果包装类 (泛型)
    /// </summary>
    public class Result<T>
    {
        /// <summary>Gets or sets whether the operation succeeded.</summary>
        public bool Success { get; set; }
        /// <summary>Gets or sets the result data.</summary>
        public T Data { get; set; }
        /// <summary>Gets or sets the operation message.</summary>
        public string Message { get; set; }
        /// <summary>Gets or sets the optional FastData error code.</summary>
        public FastDataErrorCode? Code { get; set; }

        /// <summary>Creates a successful result with data.</summary>
        /// <param name="data">Result data.</param>
        public static Result<T> Ok(T data) => new Result<T> { Success = true, Data = data, Message = "Success" };
        /// <summary>Creates a failed result.</summary>
        /// <param name="message">Failure message.</param>
        /// <param name="code">Optional error code.</param>
        public static Result<T> Fail(string message, FastDataErrorCode? code = null) => new Result<T> { Success = false, Message = message, Code = code };
    }

    /// <summary>
    /// FastData 专用异常
    /// </summary>
    public class FastDataException : Exception
    {
        /// <summary>Gets the FastData error code.</summary>
        public FastDataErrorCode Code { get; }
        /// <summary>Gets the suggested remediation text.</summary>
        public string Suggestion { get; }

        /// <summary>Initializes a new FastData exception with the default internal error code.</summary>
        /// <param name="message">Exception message.</param>
        public FastDataException(string message) : base(message)
        {
            Code = FastDataErrorCode.InternalError;
            Suggestion = "Please check logs for more details";
        }

        /// <summary>Initializes a new FastData exception with an error code.</summary>
        /// <param name="message">Exception message.</param>
        /// <param name="code">FastData error code.</param>
        public FastDataException(string message, FastDataErrorCode code) : base(message)
        {
            Code = code;
            Suggestion = GetSuggestion(code);
        }

        /// <summary>Initializes a new FastData exception with an error code and suggestion.</summary>
        /// <param name="message">Exception message.</param>
        /// <param name="code">FastData error code.</param>
        /// <param name="suggestion">Suggested remediation text.</param>
        public FastDataException(string message, FastDataErrorCode code, string suggestion) : base(message)
        {
            Code = code;
            Suggestion = suggestion;
        }

        private static string GetSuggestion(FastDataErrorCode code)
        {
            return code switch
            {
                FastDataErrorCode.ConfigNotFound => "Check if db.config file exists and is properly formatted",
                FastDataErrorCode.ConnectionFailed => "Verify database connection string and network connectivity",
                FastDataErrorCode.QueryTimeout => "Consider optimizing query or increasing CommandTimeout",
                FastDataErrorCode.ValidationFailed => "Check input data meets validation requirements",
                FastDataErrorCode.TransactionFailed => "Review transaction logic and lock conflicts",
                FastDataErrorCode.EntityNotFound => "Verify entity exists before operation",
                FastDataErrorCode.ConstraintViolation => "Check foreign key and unique constraints",
                _ => "Please check logs for more details"
            };
        }
    }
}
