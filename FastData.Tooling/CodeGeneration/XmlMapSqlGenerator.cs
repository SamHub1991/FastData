using FastData.Tooling.Database;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FastData.Tooling.CodeGeneration
{
    public class XmlMapSqlGenerator
    {
        public string Generate(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            var builder = new StringBuilder();
            var className = ToPascal(table.Name);
            var tableName = table.FullName;
            var primaryKey = columns.FirstOrDefault(c => c.IsPrimaryKey);
            var nonPkColumns = columns.Where(c => !c.IsPrimaryKey).ToList();

            builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            builder.AppendLine("<sqlMap>");

            // Select All
            GenerateSelectAll(builder, className, tableName, columns);

            // Select By Primary Key
            if (primaryKey != null)
                GenerateSelectByPrimaryKey(builder, className, tableName, primaryKey, columns);

            // Select With Dynamic Conditions
            GenerateSelectWithDynamic(builder, className, tableName, columns);

            // Insert
            GenerateInsert(builder, className, tableName, columns);

            // Update
            if (primaryKey != null)
                GenerateUpdate(builder, className, tableName, primaryKey, nonPkColumns);

            // Delete
            if (primaryKey != null)
                GenerateDelete(builder, className, tableName, primaryKey);

            builder.AppendLine("</sqlMap>");
            return builder.ToString();
        }

        private void GenerateSelectAll(StringBuilder builder, string className, string tableName, IList<DatabaseColumn> columns)
        {
            builder.AppendLine("  <!-- 查询所有 -->");
            builder.AppendLine("  <select id=\"" + className + ".GetAll\">");
            builder.AppendLine("    select");

            var columnList = columns.Select(c => "      a.[" + c.Name + "]").ToList();
            builder.AppendLine(string.Join(",\n", columnList));

            builder.AppendLine("    from " + tableName + " a");
            builder.AppendLine("  </select>");
            builder.AppendLine();
        }

        private void GenerateSelectByPrimaryKey(StringBuilder builder, string className, string tableName, DatabaseColumn primaryKey, IList<DatabaseColumn> columns)
        {
            builder.AppendLine("  <!-- 根据主键查询 -->");
            builder.AppendLine("  <select id=\"" + className + ".GetById\">");
            builder.AppendLine("    select");

            var columnList = columns.Select(c => "      a.[" + c.Name + "]").ToList();
            builder.AppendLine(string.Join(",\n", columnList));

            builder.AppendLine("    from " + tableName + " a");
            builder.AppendLine("    where a.[" + primaryKey.Name + "] = ?" + primaryKey.Name);
            builder.AppendLine("  </select>");
            builder.AppendLine();
        }

        private void GenerateSelectWithDynamic(StringBuilder builder, string className, string tableName, IList<DatabaseColumn> columns)
        {
            builder.AppendLine("  <!-- 动态条件查询 -->");
            builder.AppendLine("  <select id=\"" + className + ".GetList\" log=\"true\">");
            builder.AppendLine("    select");

            var columnList = columns.Select(c => "      a.[" + c.Name + "]").ToList();
            builder.AppendLine(string.Join(",\n", columnList));

            builder.AppendLine("    from " + tableName + " a");
            builder.AppendLine("    <dynamic prepend=\" where 1=1\">");

            foreach (var column in columns)
            {
                var propName = column.Name;
                builder.AppendLine("      <isPropertyAvailable prepend=\" and \" property=\"" + propName + "\">a.[" + propName + "] = ?" + propName + "</isPropertyAvailable>");
            }

            builder.AppendLine("    </dynamic>");
            builder.AppendLine("  </select>");
            builder.AppendLine();
        }

        private void GenerateInsert(StringBuilder builder, string className, string tableName, IList<DatabaseColumn> columns)
        {
            builder.AppendLine("  <!-- 新增 -->");
            builder.AppendLine("  <insert id=\"" + className + ".Add\">");
            builder.AppendLine("    insert into " + tableName + " (");

            var columnNames = columns.Select(c => "      [" + c.Name + "]").ToList();
            builder.AppendLine(string.Join(",\n", columnNames));

            builder.AppendLine("    ) values (");

            var paramNames = columns.Select(c => "      ?" + c.Name).ToList();
            builder.AppendLine(string.Join(",\n", paramNames));

            builder.AppendLine("    )");
            builder.AppendLine("  </insert>");
            builder.AppendLine();
        }

        private void GenerateUpdate(StringBuilder builder, string className, string tableName, DatabaseColumn primaryKey, IList<DatabaseColumn> nonPkColumns)
        {
            builder.AppendLine("  <!-- 更新 -->");
            builder.AppendLine("  <update id=\"" + className + ".Update\" log=\"true\">");
            builder.AppendLine("    update " + tableName + " set");

            var setClauses = nonPkColumns.Select(c => "      [" + c.Name + "] = ?" + c.Name).ToList();
            builder.AppendLine(string.Join(",\n", setClauses));

            builder.AppendLine("    where [" + primaryKey.Name + "] = ?" + primaryKey.Name);
            builder.AppendLine("  </update>");
            builder.AppendLine();
        }

        private void GenerateDelete(StringBuilder builder, string className, string tableName, DatabaseColumn primaryKey)
        {
            builder.AppendLine("  <!-- 删除 -->");
            builder.AppendLine("  <delete id=\"" + className + ".Delete\">");
            builder.AppendLine("    delete from " + tableName);
            builder.AppendLine("    where [" + primaryKey.Name + "] = ?" + primaryKey.Name);
            builder.AppendLine("  </delete>");
            builder.AppendLine();
        }

        private static string ToPascal(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Model";

            var builder = new StringBuilder();
            var upper = true;
            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch))
                {
                    upper = true;
                    continue;
                }

                builder.Append(upper ? char.ToUpperInvariant(ch) : ch);
                upper = false;
            }

            if (builder.Length == 0 || char.IsDigit(builder[0]))
                builder.Insert(0, "Model");

            return builder.ToString();
        }
    }
}
