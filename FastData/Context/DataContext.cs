using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using FastUntility.Page;
using FastUntility.Base;
using FastData.Base;
using FastData.Model;
using FastData.Type;
using FastData.Config;
using System.Linq.Expressions;
using System.Data;
using FastData.Property;
using FastData.Aop;
using FastData.Core.Base;

namespace FastData.Context
{
    public partial class DataContext : IDisposable
    {
        //变量
        public ConfigModel config;
        private DbConnection conn;
        private DbCommand cmd;
        private DbTransaction trans;

        private void Dispose(DbCommand cmd)
        {
            if (cmd == null) return;
            if (cmd.Parameters != null && config.DbType == DataDbType.Oracle)
                foreach (var param in cmd.Parameters)
                {
                    param.GetType().GetMethods().ToList().ForEach(m =>
                    {
                        if (m.Name == "Dispose")
                            m.Invoke(param, null);
                    });
                }
            cmd.Parameters.Clear();
        }

        /// <summary>
        /// Aop Before
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <param name="config"></param>
        private void AopBefore(List<string> tableName, string sql, List<DbParameter> param, ConfigModel config, bool isRead, AopType type)
        {
            if (FastMap.fastAop != null)
            {
                var context = new BeforeContext();

                if (tableName != null)
                    context.tableName = tableName;

                context.sql = sql;

                if (param != null)
                    context.param = param;

                context.dbType = config.DbType;
                context.isRead = isRead;
                context.isWrite = !isRead;

                FastMap.fastAop.Before(context);
            }
        }

        /// <summary>
        /// Aop After
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <param name="config"></param>
        private void AopAfter(List<string> tableName, string sql, List<DbParameter> param, ConfigModel config, bool isRead, AopType type, object result)
        {
            if (FastMap.fastAop != null)
            {
                var context = new AfterContext();

                if (tableName != null)
                    context.tableName = tableName;

                context.sql = sql;

                if (param != null)
                    context.param = param;

                context.dbType = config.DbType;
                context.isRead = isRead;
                context.isWrite = !isRead;
                context.result = result;

                FastMap.fastAop.After(context);
            }
        }

        /// <summary>
        /// aop Exception
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="name"></param>
        private void AopException(Exception ex, string name, ConfigModel config, AopType type)
        {
            if (FastMap.fastAop != null)
            {
                var context = new ExceptionContext();
                context.dbType = config.DbType;
                context.ex = ex;
                context.name = name;
                context.type = type;
                FastMap.fastAop.Exception(context);
            }
        }


        #region 回收资源
        /// <summary>
        /// 回收资源
        /// </summary>
        public void Dispose()
        {
            Dispose(cmd);
            try { if (conn != null) conn.Close(); } catch { }
            if (cmd != null) cmd.Dispose();
            if (conn != null) conn.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化（延迟打开连接）
        /// </summary>
        public DataContext(string key = null, string projectName = null)
        {
            try
            {
                this.config = DataConfig.GetConfig(key, projectName);
                if (this.config == null)
                    throw new Exception($"Config is null for key={key}, project={projectName}");
                if (string.IsNullOrEmpty(this.config.ProviderName))
                    throw new Exception($"ProviderName is null for key={key}, config.Key={this.config.Key}");
                
                var factory = DbProviderFactories.GetFactory(this.config.ProviderName);
                if (factory == null)
                    throw new Exception($"DbProviderFactory not found for provider: {this.config.ProviderName}");
                
                conn = factory.CreateConnection();
                
                // 支持连接字符串加密
                var connStr = this.config.ConnStr;
                if (this.config.IsEncrypt && !string.IsNullOrEmpty(connStr))
                {
                    try
                    {
                        connStr = BaseSymmetric.Decrypto(connStr);
                    }
                    catch
                    {
                        // 解密失败则使用原始值
                    }
                }
                
                conn.ConnectionString = connStr;
                // 延迟打开连接，提高资源利用率
                cmd = conn.CreateCommand();
            }
            catch (Exception ex)
            {
                AopException(ex, "DataContext :" + key,config,AopType.DataContext);

                if (config?.SqlErrorType?.ToLower() == SqlErrorType.Db)
                    DbLogTable.LogException(config, ex, "DataContext", "");
                else
                    DbLog.LogException(true, config?.DbType ?? "Unknown", ex, "DataContext", "");
            }
        }
        #endregion

        #region 开始事务
        public void BeginTrans()
        {
            if (conn.State == ConnectionState.Closed)
                conn.Open();
            trans = conn.BeginTransaction();
            cmd.Transaction = trans;
        }
        #endregion

        #region 提交事务
        public void SubmitTrans()
        {
            this.trans.Commit();
        }
        #endregion

        #region 回滚事务
        public void RollbackTrans()
        {
            this.trans.Rollback();
        }
        #endregion
    }
}
