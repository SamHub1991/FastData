using System.Collections.Generic;

namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 表主键配置
    /// </summary>
    public class TablePrimaryKeyConfig
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 主键字段列表（支持复合主键）
        /// </summary>
        public IList<string> PrimaryKeyColumns { get; set; }

        /// <summary>
        /// 是否使用自增主键
        /// </summary>
        public bool IsAutoIncrement { get; set; }

        /// <summary>
        /// 增量字段（用于时间戳增量同步）
        /// </summary>
        public string IncrementalColumn { get; set; }
    }

    /// <summary>
    /// 主键配置服务
    /// </summary>
    public class PrimaryKeyConfigService
    {
        private readonly IDictionary<string, TablePrimaryKeyConfig> configs = new Dictionary<string, TablePrimaryKeyConfig>();

        /// <summary>
        /// 添加表主键配置
        /// </summary>
        public void AddTableConfig(TablePrimaryKeyConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.TableName))
                return;

            configs[config.TableName] = config;
        }

        /// <summary>
        /// 获取表的主键配置
        /// </summary>
        public TablePrimaryKeyConfig GetTableConfig(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return null;

            TablePrimaryKeyConfig config;
            return configs.TryGetValue(tableName, out config) ? config : null;
        }

        /// <summary>
        /// 构建主键 WHERE 条件
        /// </summary>
        public string BuildPrimaryKeyWhereClause(TablePrimaryKeyConfig config, IList<string> parameterNames)
        {
            if (config == null || config.PrimaryKeyColumns == null || config.PrimaryKeyColumns.Count == 0)
                return "1=1";

            var conditions = new List<string>();
            for (var i = 0; i < config.PrimaryKeyColumns.Count; i++)
            {
                var column = config.PrimaryKeyColumns[i];
                var paramName = parameterNames != null && i < parameterNames.Count 
                    ? parameterNames[i] 
                    : "@pk" + i;
                conditions.Add(column + " = " + paramName);
            }

            return string.Join(" AND ", conditions);
        }

        /// <summary>
        /// 构建主键比较条件（用于增量同步）
        /// </summary>
        public string BuildIncrementalWhereClause(TablePrimaryKeyConfig config, int lastIndex)
        {
            if (config == null || config.PrimaryKeyColumns == null || config.PrimaryKeyColumns.Count == 0)
                return "1=1";

            if (config.IsAutoIncrement && config.PrimaryKeyColumns.Count == 1)
            {
                return config.PrimaryKeyColumns[0] + " > @lastValue";
            }

            var conditions = new List<string>();
            for (var i = 0; i < config.PrimaryKeyColumns.Count; i++)
            {
                var column = config.PrimaryKeyColumns[i];
                if (i <= lastIndex)
                    conditions.Add(column + " > @lastValue" + i);
                else
                    conditions.Add(column + " IS NOT NULL");
            }

            return string.Join(" OR ", conditions);
        }

        /// <summary>
        /// 导出配置为 SQL（创建配置表并插入数据）
        /// </summary>
        public string ExportToSql()
        {
            var sql = new System.Text.StringBuilder();
            sql.AppendLine("-- 表主键配置表");
            sql.AppendLine("IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='fd_table_pk_config' AND xtype='U')");
            sql.AppendLine("CREATE TABLE fd_table_pk_config (");
            sql.AppendLine("    table_name NVARCHAR(128) PRIMARY KEY,");
            sql.AppendLine("    pk_columns NVARCHAR(512),");
            sql.AppendLine("    is_auto_increment BIT DEFAULT 0,");
            sql.AppendLine("    incremental_column NVARCHAR(128)");
            sql.AppendLine(");");
            sql.AppendLine();

            foreach (var config in configs.Values)
            {
                var pkColumns = config.PrimaryKeyColumns != null 
                    ? string.Join(",", config.PrimaryKeyColumns) 
                    : "";
                sql.AppendLine(string.Format(
                    "INSERT INTO fd_table_pk_config (table_name, pk_columns, is_auto_increment, incremental_column) VALUES ('{0}', '{1}', {2}, '{3}');",
                    config.TableName,
                    pkColumns,
                    config.IsAutoIncrement ? "1" : "0",
                    config.IncrementalColumn ?? ""));
            }

            return sql.ToString();
        }
    }
}
