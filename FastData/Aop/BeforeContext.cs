using System.Collections.Generic;
using System.Data.Common;
using FastData.DbTypes;

namespace FastData.Aop
{
    public class BeforeContext
    {
        public DataDbType dbType { get; set; }

        public List<string> tableName { get; set; } = new List<string>();

        public string sql { get; set; }

        public List<DbParameter> param { get; set; } = new List<DbParameter>();

        public bool isRead { get; set; } = false;

        public bool isWrite { get; set; } = false;

        public AopType type { get; set; }
    }
}
