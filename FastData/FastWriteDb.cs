using FastData.Context;
using FastData.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FastData
{
    /// <summary>
    /// 绑定数据库Key的写入入口
    /// </summary>
    public sealed class FastWriteDb
    {
        private readonly string key;

        internal FastWriteDb(string key)
        {
            this.key = key;
        }

        public WriteReturn AddList<T>(List<T> list, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            return FastWrite.AddList(list, key, IsTrans, isLog);
        }

        public Task<WriteReturn> AddListAsy<T>(List<T> list, bool IsTrans = false, bool isLog = true) where T : class, new()
        {
            return FastWrite.AddListAsy(list, key, IsTrans, isLog);
        }

        public WriteReturn Add<T>(T model, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Add(model, db, key, isOutSql);
        }

        public Task<WriteReturn> AddAsy<T>(T model, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.AddAsy(model, db, key, isOutSql);
        }

        public WriteReturn Delete<T>(Expression<Func<T, bool>> predicate, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Delete(predicate, db, key, isOutSql);
        }

        public WriteReturn Delete<T>(T model, DataContext db = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Delete(model, db, key, isTrans, isOutSql);
        }

        public Task<WriteReturn> DeleteAsy<T>(Expression<Func<T, bool>> predicate, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.DeleteAsy(predicate, db, key, isOutSql);
        }

        public Task<WriteReturn> UpdateAsy<T>(T model, DataContext db = null, bool isTrans = false, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, db, key, isTrans, isOutSql);
        }

        public WriteReturn Update<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Update(model, predicate, field, db, key, isOutSql);
        }

        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, predicate, field, db, key, isOutSql);
        }

        public WriteReturn Update<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.Update(model, field, db, key, isOutSql);
        }

        public Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateAsy(model, field, db, key, isOutSql);
        }

        public WriteReturn UpdateList<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateList(list, field, db, key, isOutSql);
        }

        public Task<WriteReturn> UpdateListAsy<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, bool isOutSql = false) where T : class, new()
        {
            return FastWrite.UpdateListAsy(list, field, db, key, isOutSql);
        }

        public WriteReturn ExecuteSql(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastWrite.ExecuteSql(sql, param, db, key, isOutSql);
        }

        public Task<WriteReturn> ExecuteSqlAsy(string sql, DbParameter[] param, DataContext db = null, bool isOutSql = false)
        {
            return FastWrite.ExecuteSqlAsy(sql, param, db, key, isOutSql);
        }
    }
}
