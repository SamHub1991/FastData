using System;
using FastData.DbTypes;

namespace FastData.Aop
{
    public class ExceptionContext
    {
        public DataDbType dbType { get; set; }

        public AopType type { get; set; }

        public string name { get; set; }

        public Exception ex { get; set; }
    }
}
