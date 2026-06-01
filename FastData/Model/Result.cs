using System;

namespace FastData
{
    /// <summary>
    /// FastData 错误码
    /// </summary>
    public enum FastDataErrorCode
    {
        ConfigNotFound = 1001,        // 配置未找到
        ConnectionFailed = 1002,      // 连接失败
        QueryTimeout = 1003,          // 查询超时
        ValidationFailed = 1004,      // 验证失败
        TransactionFailed = 1005,     // 事务失败
        EntityNotFound = 1006,        // 实体未找到
        ConstraintViolation = 1007,   // 约束违反
        InternalError = 1008,         // 内部错误
    }

    /// <summary>
    /// FastData 结果包装类
    /// </summary>
    public class Result
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public FastDataErrorCode? Code { get; set; }

        public static Result Ok() => new Result { Success = true, Message = "Success" };
        public static Result Fail(string message, FastDataErrorCode? code = null) => new Result { Success = false, Message = message, Code = code };
    }

    /// <summary>
    /// FastData 结果包装类 (泛型)
    /// </summary>
    public class Result<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Message { get; set; }
        public FastDataErrorCode? Code { get; set; }

        public static Result<T> Ok(T data) => new Result<T> { Success = true, Data = data, Message = "Success" };
        public static Result<T> Fail(string message, FastDataErrorCode? code = null) => new Result<T> { Success = false, Message = message, Code = code };
    }

    /// <summary>
    /// FastData 专用异常
    /// </summary>
    public class FastDataException : Exception
    {
        public FastDataErrorCode Code { get; }
        public string Suggestion { get; }

        public FastDataException(string message) : base(message)
        {
            Code = FastDataErrorCode.InternalError;
            Suggestion = "Please check logs for more details";
        }

        public FastDataException(string message, FastDataErrorCode code) : base(message)
        {
            Code = code;
            Suggestion = GetSuggestion(code);
        }

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
