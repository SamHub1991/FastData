﻿using System.Collections.Generic;
using System.Data.Common;

namespace FastData.Aop
{
    public class MapBeforeContext
    {
        public string dbType { get; set; }

        public string sql { get; set; }

        public string mapName { get; set; }

        public List<DbParameter> param { get; set; } = new List<DbParameter>();

        public AopType type { get; set; }
    }
}
