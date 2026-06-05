using System;
using FastData.Context;
using System.Collections.Generic;
using System.Linq;

namespace FastData.DevTools
{
    /// <summary>
    /// SQL查询构建器
    /// </summary>
    public class SqlQueryBuilder
    {
        private string _select = "*";
        private string _from;
        private readonly List<string> _joins = new List<string>();
        private readonly List<string> _wheres = new List<string>();
        private readonly List<string> _groupBys = new List<string>();
        private readonly List<string> _orderBys = new List<string>();
        private int? _skip;
        private int? _take;
        private readonly List<object> _parameters = new List<object>();

        public SqlQueryBuilder From(string tableName)
        {
            _from = tableName;
            return this;
        }

        public SqlQueryBuilder Select(string columns)
        {
            _select = columns;
            return this;
        }

        public SqlQueryBuilder Join(string joinTable, string onCondition, string joinType = "INNER")
        {
            _joins.Add(string.Format("{0} JOIN {1} ON {2}", joinType, joinTable, onCondition));
            return this;
        }

        public SqlQueryBuilder LeftJoin(string joinTable, string onCondition)
        {
            return Join(joinTable, onCondition, "LEFT");
        }

        public SqlQueryBuilder RightJoin(string joinTable, string onCondition)
        {
            return Join(joinTable, onCondition, "RIGHT");
        }

        public SqlQueryBuilder Where(string condition, object parameter = null)
        {
            _wheres.Add(condition);
            if (parameter != null)
            {
                _parameters.Add(parameter);
            }
            return this;
        }

        public SqlQueryBuilder WhereIn(string column, IEnumerable<object> values)
        {
            var placeholders = string.Join(", ", values.Select((_, i) => string.Format("@param{0}", _parameters.Count + i)));
            _wheres.Add(string.Format("{0} IN ({1})", column, placeholders));
            _parameters.AddRange(values);
            return this;
        }

        public SqlQueryBuilder WhereBetween(string column, object start, object end)
        {
            _wheres.Add(string.Format("{0} BETWEEN @param{1} AND @param{2}", column, _parameters.Count, _parameters.Count + 1));
            _parameters.Add(start);
            _parameters.Add(end);
            return this;
        }

        public SqlQueryBuilder WhereLike(string column, string pattern)
        {
            _wheres.Add(string.Format("{0} LIKE @param{1}", column, _parameters.Count));
            _parameters.Add(pattern);
            return this;
        }

        public SqlQueryBuilder WhereRaw(string rawSql)
        {
            _wheres.Add(string.Format("({0})", rawSql));
            return this;
        }

        public SqlQueryBuilder GroupBy(string columns)
        {
            _groupBys.Add(columns);
            return this;
        }

        public SqlQueryBuilder OrderBy(string column, bool descending = false)
        {
            _orderBys.Add(string.Format("{0} {1}", column, descending ? "DESC" : "ASC"));
            return this;
        }

        public SqlQueryBuilder OrderByDescending(string column)
        {
            return OrderBy(column, true);
        }

        public SqlQueryBuilder Skip(int count)
        {
            _skip = count;
            return this;
        }

        public SqlQueryBuilder Take(int count)
        {
            _take = count;
            return this;
        }

        public string BuildSql()
        {
            if (string.IsNullOrEmpty(_from))
            {
                throw new InvalidOperationException("必须指定 FROM 子句");
            }

            var sql = new System.Text.StringBuilder();

            // SELECT
            sql.Append(string.Format("SELECT {0}", _select));

            // FROM
            sql.Append(string.Format(" FROM {0}", _from));

            // JOIN
            if (_joins.Any())
            {
                sql.Append(" ");
                sql.Append(string.Join(" ", _joins));
            }

            // WHERE
            if (_wheres.Any())
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", _wheres));
            }

            // GROUP BY
            if (_groupBys.Any())
            {
                sql.Append(" GROUP BY ");
                sql.Append(string.Join(", ", _groupBys));
            }

            // ORDER BY
            if (_orderBys.Any())
            {
                sql.Append(" ORDER BY ");
                sql.Append(string.Join(", ", _orderBys));
            }

            // LIMIT
            if (_take.HasValue)
            {
                if (_skip.HasValue)
                {
                    sql.Append(string.Format(" LIMIT {0}, {1}", _skip.Value, _take.Value));
                }
                else
                {
                    sql.Append(string.Format(" LIMIT {0}", _take.Value));
                }
            }
            else if (_skip.HasValue)
            {
                sql.Append(string.Format(" LIMIT {0}, 18446744073709551615", _skip.Value));
            }

            return sql.ToString();
        }

        public List<object> GetParameters()
        {
            return new List<object>(_parameters);
        }

        public (string sql, List<object> parameters) Build()
        {
            return (BuildSql(), GetParameters());
        }
    }

    /// <summary>
    /// 插入语句构建器
    /// </summary>
    public class InsertQueryBuilder
    {
        private string _tableName;
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        private readonly List<string> _columns = new List<string>();

        public InsertQueryBuilder Into(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        public InsertQueryBuilder Value(string column, object value)
        {
            _columns.Add(column);
            _values.Add(column, value);
            return this;
        }

        public InsertQueryBuilder Values(Dictionary<string, object> values)
        {
            foreach (var kvp in values)
            {
                Value(kvp.Key, kvp.Value);
            }
            return this;
        }

        public string BuildSql()
        {
            if (string.IsNullOrEmpty(_tableName))
            {
                throw new InvalidOperationException("必须指定表名");
            }

            if (!_columns.Any())
            {
                throw new InvalidOperationException("必须指定至少一列");
            }

            var columns = string.Join(", ", _columns);
            var placeholders = string.Join(", ", _columns.Select((c, i) => string.Format("@param{0}", i)));

            return string.Format("INSERT INTO {0} ({1}) VALUES ({2})", _tableName, columns, placeholders);
        }

        public List<object> GetParameters()
        {
            return _columns.Select(c => _values[c]).ToList();
        }

        public (string sql, List<object> parameters) Build()
        {
            return (BuildSql(), GetParameters());
        }
    }

    /// <summary>
    /// 更新语句构建器
    /// </summary>
    public class UpdateQueryBuilder
    {
        private string _tableName;
        private readonly Dictionary<string, object> _setValues = new Dictionary<string, object>();
        private readonly List<string> _wheres = new List<string>();
        private readonly List<object> _parameters = new List<object>();

        public UpdateQueryBuilder Table(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        public UpdateQueryBuilder Set(string column, object value)
        {
            _setValues.Add(column, value);
            return this;
        }

        public UpdateQueryBuilder SetValues(Dictionary<string, object> values)
        {
            foreach (var kvp in values)
            {
                Set(kvp.Key, kvp.Value);
            }
            return this;
        }

        public UpdateQueryBuilder Where(string condition, object parameter = null)
        {
            _wheres.Add(condition);
            if (parameter != null)
            {
                _parameters.Add(parameter);
            }
            return this;
        }

        public string BuildSql()
        {
            if (string.IsNullOrEmpty(_tableName))
            {
                throw new InvalidOperationException("必须指定表名");
            }

            if (!_setValues.Any())
            {
                throw new InvalidOperationException("必须指定至少一个 SET 值");
            }

            var sql = new System.Text.StringBuilder();

            sql.Append(string.Format("UPDATE {0} SET ", _tableName));

            var setClauses = new List<string>();
            var paramIndex = 0;

            foreach (var kvp in _setValues)
            {
                setClauses.Add(string.Format("{0} = @param{1}", kvp.Key, paramIndex));
                _parameters.Add(kvp.Value);
                paramIndex++;
            }

            sql.Append(string.Join(", ", setClauses));

            if (_wheres.Any())
            {
                sql.Append(" WHERE ");
                var whereParams = _wheres.Select((w, i) =>
                {
                    var hasParam = _parameters.Count > paramIndex && _parameters[paramIndex] != null;
                    if (hasParam)
                    {
                        var updated = w.Replace("@" + _parameters[paramIndex], "@param" + paramIndex);
                        paramIndex++;
                        return updated;
                    }
                    return w;
                });
                sql.Append(string.Join(" AND ", _wheres));
            }

            return sql.ToString();
        }

        public List<object> GetParameters()
        {
            return new List<object>(_parameters);
        }

        public (string sql, List<object> parameters) Build()
        {
            return (BuildSql(), GetParameters());
        }
    }

    /// <summary>
    /// 删除语句构建器
    /// </summary>
    public class DeleteQueryBuilder
    {
        private string _tableName;
        private readonly List<string> _wheres = new List<string>();
        private readonly List<object> _parameters = new List<object>();

        public DeleteQueryBuilder From(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        public DeleteQueryBuilder Where(string condition, object parameter = null)
        {
            _wheres.Add(condition);
            if (parameter != null)
            {
                _parameters.Add(parameter);
            }
            return this;
        }

        public string BuildSql()
        {
            if (string.IsNullOrEmpty(_tableName))
            {
                throw new InvalidOperationException("必须指定表名");
            }

            var sql = new System.Text.StringBuilder();
            sql.Append(string.Format("DELETE FROM {0}", _tableName));

            if (_wheres.Any())
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", _wheres));
            }

            return sql.ToString();
        }

        public List<object> GetParameters()
        {
            return new List<object>(_parameters);
        }

        public (string sql, List<object> parameters) Build()
        {
            return (BuildSql(), GetParameters());
        }
    }

    /// <summary>
    /// SQL构建器工厂
    /// </summary>
    public static class SqlBuilder
    {
        public static SqlQueryBuilder Select()
        {
            return new SqlQueryBuilder();
        }

        public static InsertQueryBuilder Insert()
        {
            return new InsertQueryBuilder();
        }

        public static UpdateQueryBuilder Update()
        {
            return new UpdateQueryBuilder();
        }

        public static DeleteQueryBuilder Delete()
        {
            return new DeleteQueryBuilder();
        }
    }
}