using System.Collections.Generic;
using System.Data.Common;
using FastData.DbTypes;

namespace FastData.Aop
{
    public class MapBeforeContext
    {
        public DataDbType dbType { get; set; }

        public string sql { get; set; }

        public string mapName { get; set; }

        public List<DbParameter> param { get; set; } = new List<DbParameter>();

        public AopType type { get; set; }
    }
}
