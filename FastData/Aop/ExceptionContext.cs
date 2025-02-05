﻿using System;

namespace FastData.Aop
{
    public class ExceptionContext
    {
        public string dbType { get; set; }

        public AopType type { get; set; }

        public string name { get; set; }

        public Exception ex { get; set; }
    }
}
