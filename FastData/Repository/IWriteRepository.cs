using FastData.Context;
using FastData.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FastData.Repository
{
    /// <summary>
    /// 写入仓储接口 - 增删改相关操作
    /// </summary>
    public interface IWriteRepository
    {
        WriteReturn Write(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Task<WriteReturn> WriteAsy(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Lazy<WriteReturn> WriteLazy(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Task<Lazy<WriteReturn>> WriteLazyAsy(string name, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        WriteReturn AddList<T>(List<T> list, string key = null, bool IsTrans = false, bool isLog = true) where T : class, new();

        Task<WriteReturn> AddListAsy<T>(List<T> list, string key = null, bool IsTrans = false, bool isLog = true) where T : class, new();

        WriteReturn Add<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Task<WriteReturn> AddAsy<T>(T model, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        WriteReturn Delete<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Task<WriteReturn> DeleteAsy<T>(Expression<Func<T, bool>> predicate, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        WriteReturn Delete<T>(T model, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new();

        Task<WriteReturn> UpdateAsy<T>(T model, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new();

        WriteReturn Update<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        WriteReturn Update<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new();

        Task<WriteReturn> UpdateAsy<T>(T model, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isTrans = false, bool isOutSql = false) where T : class, new();

        WriteReturn UpdateList<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        Task<WriteReturn> UpdateListAsy<T>(List<T> list, Expression<Func<T, object>> field = null, DataContext db = null, string key = null, bool isOutSql = false) where T : class, new();

        WriteReturn ExecuteSql(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);

        Task<WriteReturn> ExecuteSqlAsy(string sql, DbParameter[] param, DataContext db = null, string key = null, bool isOutSql = false);
    }
}
