using FastData.Tooling.Database;
using System.Collections.Generic;
using System.Text;

namespace FastData.Tooling.CodeGeneration
{
    /// <summary>
    /// 增强的代码生成器 - 支持 Example 项目全部功能
    /// </summary>
    public class EnhancedCodeGenerator
    {
        private readonly ModelCodeGenerator _modelGenerator = new ModelCodeGenerator();

        public GeneratorOptions Options { get; set; } = new GeneratorOptions();

        public CodeGenerationResult GenerateComplete(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            var result = new CodeGenerationResult();
            var pascalName = ToPascal(table.Name);

            if (Options.GenerateModel)
                result.ModelCode = _modelGenerator.Generate(namespaceName, table, columns);

            if (Options.GenerateXmlMap)
                result.XmlMapCode = new XmlMapSqlGenerator().Generate(namespaceName, table, columns);

            if (Options.GenerateRepository)
                result.RepositoryCode = GenerateRepository(namespaceName, table, columns);

            if (Options.GenerateService)
                result.ServiceCode = GenerateService(namespaceName, table);

            if (Options.GenerateController)
                result.ControllerCode = GenerateController(namespaceName, table);

            if (Options.GenerateDemo)
                result.DemoCode = GenerateDemo(namespaceName, table, columns);

            if (Options.GenerateReadme)
                result.ReadmeCode = GenerateReadme(namespaceName, table);

            return result;
        }

        public string GenerateModel(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
            => _modelGenerator.Generate(namespaceName, table, columns);

        public string GenerateXmlMap(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
            => new XmlMapSqlGenerator().Generate(namespaceName, table, columns);

        public string GenerateRepository(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            var modelName = ToPascal(table.Name);
            var keyProp = FindPrimaryKey(columns);
            var keyType = keyProp != null ? _modelGenerator.GetClrType(keyProp) : "int";
            var keyName = keyProp != null ? ToPascal(keyProp.Name) : "Id";

            var b = new StringBuilder();
            b.AppendLine("using System;");
            b.AppendLine("using System.Collections.Generic;");
            b.AppendLine("using System.Threading.Tasks;");
            b.AppendLine("using FastData;");
            b.AppendLine("using FastData.Context;");
            if (Options.GenerateCache)
                b.AppendLine("using FastUntility.Base;");
            b.AppendLine();

            b.AppendLine("namespace " + namespaceName + ".Repositories");
            b.AppendLine("{");

            if (Options.GenerateWithInterface)
            {
                b.AppendLine("    public interface I" + modelName + "Repository");
                b.AppendLine("    {");
                b.AppendLine("        Task<List<" + modelName + ">> GetAllAsync();");
                b.AppendLine("        Task<" + modelName + "> GetByIdAsync(" + keyType + " id);");
                b.AppendLine("        Task<List<" + modelName + ">> QueryAsync(System.Linq.Expressions.Expression<System.Func<" + modelName + ", bool>> predicate);");
                b.AppendLine("        Task<" + modelName + "> AddAsync(" + modelName + " entity);");
                b.AppendLine("        Task<bool> UpdateAsync(" + modelName + " entity);");
                b.AppendLine("        Task<bool> DeleteAsync(" + keyType + " id);");
                b.AppendLine("        Task<int> CountAsync();");
                if (Options.GenerateWithPagination)
                    b.AppendLine("        Task<FastUntility.Page.PageResult<" + modelName + ">> GetPageAsync(int pageIndex, int pageSize);");
                if (Options.GenerateWithTransaction)
                    b.AppendLine("        System.Data.Common.DbTransaction BeginTransaction();");
                if (Options.GenerateWithSharding)
                    b.AppendLine("        void ConfigureSharding(FastData.Sharding.ShardingConfig config);");
                b.AppendLine("    }");
                b.AppendLine();
            }

            b.AppendLine("    public class " + modelName + "Repository" + (Options.GenerateWithInterface ? " : I" + modelName + "Repository" : ""));
            b.AppendLine("    {");
            b.AppendLine("        private readonly string _dbKey;");
            b.AppendLine("        private readonly string _cachePrefix = \"" + modelName + "\";");
            b.AppendLine();
            b.AppendLine("        public " + modelName + "Repository(string dbKey = null)");
            b.AppendLine("        {");
            b.AppendLine("            _dbKey = dbKey ?? \"default\";");
            b.AppendLine("        }");
            b.AppendLine();

            // GetAllAsync
            b.AppendLine("        public async Task<List<" + modelName + ">> GetAllAsync()");
            b.AppendLine("        {");
            if (Options.GenerateCache)
                b.AppendLine("            var cache = CacheHelper.Get<List<" + modelName + ">>(_cachePrefix);");
            if (Options.GenerateCache)
                b.AppendLine("            if (cache != null) return cache;");
            b.AppendLine("            using var db = new DataContext(_dbKey);");
            b.AppendLine("            var result = FastRead.Use(_dbKey).Query<" + modelName + ">(q => q." + keyName + " > 0).ToList<" + modelName + ">();");
            if (Options.GenerateCache)
            {
                b.AppendLine("            CacheHelper.Set(_cachePrefix, result, 30);");
                b.AppendLine("            return result;");
            }
            b.AppendLine("            return result;");
            b.AppendLine("        }");
            b.AppendLine();

            // GetByIdAsync
            b.AppendLine("        public async Task<" + modelName + "> GetByIdAsync(" + keyType + " id)");
            b.AppendLine("        {");
            if (Options.GenerateCache)
            {
                b.AppendLine("            var cacheKey = _cachePrefix + \"_\" + id;");
                b.AppendLine("            var cache = CacheHelper.Get<" + modelName + ">(cacheKey);");
                b.AppendLine("            if (cache != null) return cache;");
            }
            b.AppendLine("            using var db = new DataContext(_dbKey);");
            b.AppendLine("            var result = FastRead.Use(_dbKey).Query<" + modelName + ">(q => q." + keyName + " == id).ToItem<" + modelName + ">();");
            if (Options.GenerateCache)
            {
                b.AppendLine("            if (result != null) CacheHelper.Set(cacheKey, result, 60);");
                b.AppendLine("            return result;");
            }
            b.AppendLine("            return result;");
            b.AppendLine("        }");
            b.AppendLine();

            // QueryAsync
            b.AppendLine("        public async Task<List<" + modelName + ">> QueryAsync(System.Linq.Expressions.Expression<System.Func<" + modelName + ", bool>> predicate)");
            b.AppendLine("        {");
            b.AppendLine("            using var db = new DataContext(_dbKey);");
            b.AppendLine("            return FastRead.Use(_dbKey).Query(predicate).ToList<" + modelName + ">();");
            b.AppendLine("        }");
            b.AppendLine();

            // AddAsync
            b.AppendLine("        public async Task<" + modelName + "> AddAsync(" + modelName + " entity)");
            b.AppendLine("        {");
            b.AppendLine("            using var db = new DataContext(_dbKey);");
            b.AppendLine("            var result = FastWrite.Use(_dbKey).Add(entity).Execute();");
            if (Options.GenerateCache)
                b.AppendLine("            if (result.IsSuccess) CacheHelper.Remove(_cachePrefix);");
            if (Options.GenerateQueue)
            {
                b.AppendLine("            FastWrite.Use(_dbKey).Queue()");
                b.AppendLine("                .Add(entity)");
                b.AppendLine("                .WithMetadata(new System.Collections.Generic.Dictionary<string, object> { { \"Operation\", \"Insert\" } })");
                b.AppendLine("                .Execute();");
            }
            b.AppendLine("            return entity;");
            b.AppendLine("        }");
            b.AppendLine();

            // UpdateAsync
            b.AppendLine("        public async Task<bool> UpdateAsync(" + modelName + " entity)");
            b.AppendLine("        {");
            b.AppendLine("            using var db = new DataContext(_dbKey);");
            b.AppendLine("            var result = FastWrite.Use(_dbKey).Update(entity).Execute();");
            if (Options.GenerateCache)
            {
                b.AppendLine("            if (result.IsSuccess) {");
                b.AppendLine("                CacheHelper.Remove(_cachePrefix);");
                b.AppendLine("                CacheHelper.Remove(_cachePrefix + \"_\" + entity." + keyName + ");");
                b.AppendLine("            }");
            }
            b.AppendLine("            return result.IsSuccess;");
            b.AppendLine("        }");
            b.AppendLine();

            // DeleteAsync
            b.AppendLine("        public async Task<bool> DeleteAsync(" + keyType + " id)");
            b.AppendLine("        {");
            b.AppendLine("            using var db = new DataContext(_dbKey);");
            b.AppendLine("            var result = FastWrite.Use(_dbKey).Delete<" + modelName + ">(q => q." + keyName + " == id).Execute();");
            if (Options.GenerateCache)
            {
                b.AppendLine("            if (result.IsSuccess) {");
                b.AppendLine("                CacheHelper.Remove(_cachePrefix);");
                b.AppendLine("                CacheHelper.Remove(_cachePrefix + \"_\" + id);");
                b.AppendLine("            }");
            }
            b.AppendLine("            return result.IsSuccess;");
            b.AppendLine("        }");
            b.AppendLine();

            // CountAsync
            b.AppendLine("        public async Task<int> CountAsync()");
            b.AppendLine("        {");
            b.AppendLine("            using var db = new DataContext(_dbKey);");
            b.AppendLine("            return FastRead.Use(_dbKey).Query<" + modelName + ">(q => q." + keyName + " > 0).ToCount<" + modelName + ">();");
            b.AppendLine("        }");
            b.AppendLine();

            // GetPageAsync with pagination
            if (Options.GenerateWithPagination)
            {
                b.AppendLine("        public async Task<FastUntility.Page.PageResult<" + modelName + ">> GetPageAsync(int pageIndex, int pageSize)");
                b.AppendLine("        {");
                b.AppendLine("            using var db = new DataContext(_dbKey);");
                b.AppendLine("            var query = FastRead.Use(_dbKey).Query<" + modelName + ">(q => q." + keyName + " > 0);");
                b.AppendLine("            return query.ToPage<" + modelName + ">(new FastUntility.Page.PageModel { PageSize = pageSize, PageIndex = pageIndex });");
                b.AppendLine("        }");
                b.AppendLine();
            }

            // ExecuteRawSql
            if (Options.GenerateRawSql)
            {
                b.AppendLine("        public async Task<List<" + modelName + ">> ExecuteSqlAsync(string sql, System.Data.Common.DbParameter[] parameters = null)");
                b.AppendLine("        {");
                b.AppendLine("            using var db = new DataContext(_dbKey);");
                b.AppendLine("            return FastRead.Use(_dbKey).ExecuteSql<" + modelName + ">(sql, parameters);");
                b.AppendLine("        }");
                b.AppendLine();

                b.AppendLine("        public async Task<int> ExecuteNonQueryAsync(string sql, System.Data.Common.DbParameter[] parameters = null)");
                b.AppendLine("        {");
                b.AppendLine("            using var db = new DataContext(_dbKey);");
                b.AppendLine("            var result = FastWrite.Use(_dbKey).ExecuteSql(sql, parameters);");
                b.AppendLine("            return result.IsSuccess ? 1 : 0;");
                b.AppendLine("        }");
                b.AppendLine();
            }

            // MapSql execution
            if (Options.GenerateMapSql)
            {
                b.AppendLine("        public async Task<List<" + modelName + ">> ExecuteMapSqlAsync(string mapId, object param = null)");
                b.AppendLine("        {");
                b.AppendLine("            using var db = new DataContext(_dbKey);");
                b.AppendLine("            return FastRead.Use(_dbKey).Query<" + modelName + ">(q => q." + keyName + " > 0).Map(mapId).ToList<" + modelName + ">();");
                b.AppendLine("        }");
                b.AppendLine();
            }

            // Transaction
            if (Options.GenerateWithTransaction)
            {
                b.AppendLine("        public System.Data.Common.DbTransaction BeginTransaction()");
                b.AppendLine("        {");
                b.AppendLine("            var db = new DataContext(_dbKey);");
                b.AppendLine("            db.BeginTrans();");
                b.AppendLine("            return null; // 使用 DataContext 管理事务");
                b.AppendLine("        }");
                b.AppendLine();

                b.AppendLine("        public async Task<bool> ExecuteInTransactionAsync(System.Func<Task<bool>> operation)");
                b.AppendLine("        {");
                b.AppendLine("            using var db = new DataContext(_dbKey);");
                b.AppendLine("            try");
                b.AppendLine("            {");
                b.AppendLine("                db.BeginTrans();");
                b.AppendLine("                var result = await operation();");
                b.AppendLine("                if (result) db.SubmitTrans();");
                b.AppendLine("                else db.RollbackTrans();");
                b.AppendLine("                return result;");
                b.AppendLine("            }");
                b.AppendLine("            catch");
                b.AppendLine("            {");
                b.AppendLine("                db.RollbackTrans();");
                b.AppendLine("                throw;");
                b.AppendLine("            }");
                b.AppendLine("        }");
                b.AppendLine();
            }

            // Sharding config
            if (Options.GenerateWithSharding)
            {
                b.AppendLine("        public void ConfigureSharding(FastData.Sharding.ShardingConfig config)");
                b.AppendLine("        {");
                b.AppendLine("            FastData.Sharding.ShardingManager.Configure<" + modelName + ">(config);");
                b.AppendLine("        }");
                b.AppendLine();

                b.AppendLine("        public List<string> GetShardTables(FastData.Sharding.QueryShardingConfig queryConfig)");
                b.AppendLine("        {");
                b.AppendLine("            return FastData.Sharding.ShardingManager.GetTableNames<" + modelName + ">(queryConfig);");
                b.AppendLine("        }");
                b.AppendLine();
            }

            // Data sync
            if (Options.GenerateWithSync)
            {
                b.AppendLine("        public async Task<int> SyncToDatabaseAsync(string targetKey, System.Func<" + modelName + ", bool> filter = null)");
                b.AppendLine("        {");
                b.AppendLine("            var list = await GetAllAsync();");
                b.AppendLine("            if (filter != null)");
                b.AppendLine("            {");
                b.AppendLine("                var filtered = new List<" + modelName + ">();");
                b.AppendLine("                foreach (var item in list)");
                b.AppendLine("                    if (filter(item)) filtered.Add(item);");
                b.AppendLine("                list = filtered;");
                b.AppendLine("            }");
                b.AppendLine("            int count = 0;");
                b.AppendLine("            foreach (var item in list)");
                b.AppendLine("            {");
                b.AppendLine("                var result = FastWrite.Use(targetKey).Add(item).Execute();");
                b.AppendLine("                if (result.IsSuccess) count++;");
                b.AppendLine("            }");
                b.AppendLine("            return count;");
                b.AppendLine("        }");
                b.AppendLine();
            }

            b.AppendLine("    }");
            b.AppendLine("}");
            return b.ToString();
        }

        public string GenerateService(string namespaceName, DatabaseTable table)
        {
            var modelName = ToPascal(table.Name);
            var b = new StringBuilder();
            b.AppendLine("using System;");
            b.AppendLine("using System.Collections.Generic;");
            b.AppendLine("using System.Threading.Tasks;");
            b.AppendLine("using " + namespaceName + ".Repositories;");
            b.AppendLine();
            b.AppendLine("namespace " + namespaceName + ".Services");
            b.AppendLine("{");

            if (Options.GenerateWithInterface)
            {
                b.AppendLine("    public interface I" + modelName + "Service");
                b.AppendLine("    {");
                b.AppendLine("        Task<List<" + modelName + ">> GetAllAsync();");
                b.AppendLine("        Task<" + modelName + "> GetByIdAsync(dynamic id);");
                b.AppendLine("        Task<" + modelName + "> CreateAsync(" + modelName + " entity);");
                b.AppendLine("        Task<bool> UpdateAsync(" + modelName + " entity);");
                b.AppendLine("        Task<bool> DeleteAsync(dynamic id);");
                b.AppendLine("        Task<int> CountAsync();");
                if (Options.GenerateWithPagination)
                    b.AppendLine("        Task<FastUntility.Page.PageResult<" + modelName + ">> GetPageAsync(int pageIndex, int pageSize);");
                b.AppendLine("    }");
                b.AppendLine();
            }

            b.AppendLine("    public class " + modelName + "Service" + (Options.GenerateWithInterface ? " : I" + modelName + "Service" : ""));
            b.AppendLine("    {");
            b.AppendLine("        private readonly I" + modelName + "Repository _repository;");
            b.AppendLine("        public " + modelName + "Service(I" + modelName + "Repository repository)");
            b.AppendLine("        {");
            b.AppendLine("            _repository = repository;");
            b.AppendLine("        }");
            b.AppendLine("        public Task<List<" + modelName + ">> GetAllAsync() => _repository.GetAllAsync();");
            b.AppendLine("        public Task<" + modelName + "> GetByIdAsync(dynamic id) => _repository.GetByIdAsync(id);");
            b.AppendLine("        public async Task<" + modelName + "> CreateAsync(" + modelName + " entity)");
            b.AppendLine("        {");
            b.AppendLine("            return await _repository.AddAsync(entity);");
            b.AppendLine("        }");
            b.AppendLine("        public Task<bool> UpdateAsync(" + modelName + " entity) => _repository.UpdateAsync(entity);");
            b.AppendLine("        public Task<bool> DeleteAsync(dynamic id) => _repository.DeleteAsync(id);");
            b.AppendLine("        public Task<int> CountAsync() => _repository.CountAsync();");
            if (Options.GenerateWithPagination)
                b.AppendLine("        public Task<FastUntility.Page.PageResult<" + modelName + ">> GetPageAsync(int pageIndex, int pageSize) => _repository.GetPageAsync(pageIndex, pageSize);");
            b.AppendLine("    }");
            b.AppendLine("}");
            return b.ToString();
        }

        public string GenerateController(string namespaceName, DatabaseTable table)
        {
            var modelName = ToPascal(table.Name);
            var b = new StringBuilder();
            b.AppendLine("using Microsoft.AspNetCore.Mvc;");
            b.AppendLine("using System;");
            b.AppendLine("using System.Collections.Generic;");
            b.AppendLine("using System.Threading.Tasks;");
            b.AppendLine("using " + namespaceName + ".Models;");
            b.AppendLine("using " + namespaceName + ".Services;");
            b.AppendLine();
            b.AppendLine("namespace " + namespaceName + ".Controllers");
            b.AppendLine("{");
            b.AppendLine("    [ApiController]");
            b.AppendLine("    [Route(\"api/[controller]\")]");
            b.AppendLine("    public class " + modelName + "Controller : ControllerBase");
            b.AppendLine("    {");
            b.AppendLine("        private readonly I" + modelName + "Service _service;");
            b.AppendLine("        public " + modelName + "Controller(I" + modelName + "Service service) => _service = service;");
            b.AppendLine();
            b.AppendLine("        [HttpGet]");
            b.AppendLine("        public async Task<ActionResult<List<" + modelName + ">>> GetAll()");
            b.AppendLine("        {");
            b.AppendLine("            return Ok(await _service.GetAllAsync());");
            b.AppendLine("        }");
            b.AppendLine();
            b.AppendLine("        [HttpGet(\"{id}\")]");
            b.AppendLine("        public async Task<ActionResult<" + modelName + ">> GetById(int id)");
            b.AppendLine("        {");
            b.AppendLine("            var result = await _service.GetByIdAsync(id);");
            b.AppendLine("            if (result == null) return NotFound();");
            b.AppendLine("            return Ok(result);");
            b.AppendLine("        }");
            b.AppendLine();
            b.AppendLine("        [HttpPost]");
            b.AppendLine("        public async Task<ActionResult<" + modelName + ">> Create([FromBody] " + modelName + " entity)");
            b.AppendLine("        {");
            b.AppendLine("            var result = await _service.CreateAsync(entity);");
            b.AppendLine("            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);");
            b.AppendLine("        }");
            b.AppendLine();
            b.AppendLine("        [HttpPut(\"{id}\")]");
            b.AppendLine("        public async Task<ActionResult> Update(int id, [FromBody] " + modelName + " entity)");
            b.AppendLine("        {");
            b.AppendLine("            await _service.UpdateAsync(entity);");
            b.AppendLine("            return NoContent();");
            b.AppendLine("        }");
            b.AppendLine();
            b.AppendLine("        [HttpDelete(\"{id}\")]");
            b.AppendLine("        public async Task<ActionResult> Delete(int id)");
            b.AppendLine("        {");
            b.AppendLine("            await _service.DeleteAsync(id);");
            b.AppendLine("            return NoContent();");
            b.AppendLine("        }");
            b.AppendLine("    }");
            b.AppendLine("}");
            return b.ToString();
        }

        public string GenerateDemo(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            var modelName = ToPascal(table.Name);
            var keyProp = FindPrimaryKey(columns);
            var keyName = keyProp != null ? ToPascal(keyProp.Name) : "Id";
            var b = new StringBuilder();

            b.AppendLine("using System;");
            b.AppendLine("using System.Collections.Generic;");
            b.AppendLine("using System.Linq;");
            b.AppendLine("using " + namespaceName + ".Models;");
            b.AppendLine("using " + namespaceName + ".Repositories;");
            b.AppendLine();

            b.AppendLine("namespace " + namespaceName + ".Demo");
            b.AppendLine("{");
            b.AppendLine("    public static class " + modelName + "Demo");
            b.AppendLine("    {");
            b.AppendLine("        public static void RunAll()");
            b.AppendLine("        {");
            b.AppendLine("            var repo = new " + modelName + "Repository();");
            b.AppendLine("            Console.WriteLine(\"=== " + modelName + " Demo ===\");");
            b.AppendLine();

            // Insert demo
            b.AppendLine("            // 1. Insert");
            b.AppendLine("            var entity = new " + modelName + "();");
            if (columns.Count > 1)
                b.AppendLine("            entity." + ToPascal(columns[1].Name) + " = \"test_value\";");
            b.AppendLine("            var inserted = repo.AddAsync(entity).Result;");
            b.AppendLine("            Console.WriteLine($\"Inserted: " + keyName + "={inserted." + keyName + "}\");");
            b.AppendLine();

            // Query demo
            b.AppendLine("            // 2. Query by id");
            b.AppendLine("            var found = repo.GetByIdAsync(1).Result;");
            b.AppendLine("            Console.WriteLine($\"Found: {found != null}\");");
            b.AppendLine();

            // List demo
            b.AppendLine("            // 3. Get all");
            b.AppendLine("            var all = repo.GetAllAsync().Result;");
            b.AppendLine("            Console.WriteLine($\"Count: {all.Count}\");");
            b.AppendLine();

            // Update demo
            b.AppendLine("            // 4. Update");
            b.AppendLine("            if (found != null)");
            b.AppendLine("            {");
            if (columns.Count > 1)
                b.AppendLine("                found." + ToPascal(columns[1].Name) + " = \"updated\";");
            b.AppendLine("                var updated = repo.UpdateAsync(found).Result;");
            b.AppendLine("                Console.WriteLine($\"Updated: {updated}\");");
            b.AppendLine("            }");
            b.AppendLine();

            // Delete demo
            b.AppendLine("            // 5. Delete");
            b.AppendLine("            if (inserted != null)");
            b.AppendLine("            {");
            b.AppendLine("                var deleted = repo.DeleteAsync(inserted." + keyName + ").Result;");
            b.AppendLine("                Console.WriteLine($\"Deleted: {deleted}\");");
            b.AppendLine("            }");
            b.AppendLine();

            // Pagination demo
            if (Options.GenerateWithPagination)
            {
                b.AppendLine("            // 6. Pagination");
                b.AppendLine("            var page = repo.GetPageAsync(1, 10).Result;");
                b.AppendLine("            Console.WriteLine($\"Page: {page.Data.Count}/{page.TotalCount}\");");
                b.AppendLine();
            }

            // Transaction demo
            if (Options.GenerateWithTransaction)
            {
                b.AppendLine("            // 7. Transaction");
                b.AppendLine("            var txSuccess = repo.ExecuteInTransactionAsync(async () =>");
                b.AppendLine("            {");
                b.AppendLine("                var txEntity = new " + modelName + "();");
                b.AppendLine("                await repo.AddAsync(txEntity);");
                b.AppendLine("                return true;");
                b.AppendLine("            }).Result;");
                b.AppendLine("            Console.WriteLine($\"Transaction: {txSuccess}\");");
                b.AppendLine();
            }

            // Raw SQL demo
            if (Options.GenerateRawSql)
            {
                b.AppendLine("            // 8. Raw SQL");
                b.AppendLine("            var sqlList = repo.ExecuteSqlAsync(\"SELECT * FROM " + table.Name + "\").Result;");
                b.AppendLine("            Console.WriteLine($\"Raw SQL count: {sqlList.Count}\");");
                b.AppendLine();
            }

            b.AppendLine("        }");
            b.AppendLine("    }");
            b.AppendLine("}");
            return b.ToString();
        }

        public string GenerateReadme(string namespaceName, DatabaseTable table)
        {
            var modelName = ToPascal(table.Name);
            var b = new StringBuilder();

            b.AppendLine("# " + modelName + " - 代码生成说明");
            b.AppendLine();
            b.AppendLine("## 生成的文件");
            b.AppendLine();
            b.AppendLine("| 文件 | 说明 |");
            b.AppendLine("|------|------|");
            if (Options.GenerateModel)
                b.AppendLine("| Models/" + table.Name + ".cs | Model 实体类 |");
            if (Options.GenerateXmlMap)
                b.AppendLine("| XmlMaps/" + table.Name + ".xml | XML Map SQL 映射 |");
            if (Options.GenerateRepository)
                b.AppendLine("| Repositories/" + modelName + "Repository.cs | 数据仓储（CRUD/缓存/队列/分页/分表/事务/同步） |");
            if (Options.GenerateService)
                b.AppendLine("| Services/" + modelName + "Service.cs | 业务服务 |");
            if (Options.GenerateController)
                b.AppendLine("| Controllers/" + modelName + "Controller.cs | Web API 控制器 |");
            if (Options.GenerateDemo)
                b.AppendLine("| Demo/" + modelName + "Demo.cs | 功能演示代码 |");
            b.AppendLine();
            b.AppendLine("## 功能特性");
            b.AppendLine();
            if (Options.GenerateCache) b.AppendLine("- 缓存支持 (Redis/Memory)");
            if (Options.GenerateQueue) b.AppendLine("- 消息队列 (Write-Behind)");
            if (Options.GenerateWithPagination) b.AppendLine("- 分页查询");
            if (Options.GenerateWithTransaction) b.AppendLine("- 事务支持");
            if (Options.GenerateWithSharding) b.AppendLine("- 分表策略 (时间/哈希/列表/频率)");
            if (Options.GenerateWithSync) b.AppendLine("- 数据同步");
            if (Options.GenerateRawSql) b.AppendLine("- 原生 SQL 执行");
            if (Options.GenerateMapSql) b.AppendLine("- XML Map SQL 执行");
            b.AppendLine();
            b.AppendLine("## 使用方法");
            b.AppendLine();
            b.AppendLine("1. 在 db.config 中配置数据库连接");
            b.AppendLine("2. 注册依赖注入（如使用 ASP.NET Core）");
            b.AppendLine("3. 参考 Demo 代码调用 API");
            return b.ToString();
        }

        private static string ToPascal(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Model";
            var b = new StringBuilder();
            var upper = true;
            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch)) { upper = true; continue; }
                b.Append(upper ? char.ToUpperInvariant(ch) : ch);
                upper = false;
            }
            return b.Length == 0 || char.IsDigit(b[0]) ? "Model" : b.ToString();
        }

        private static DatabaseColumn FindPrimaryKey(IList<DatabaseColumn> columns)
        {
            foreach (var col in columns)
                if (col.IsPrimaryKey) return col;
            return null;
        }
    }

    public class GeneratorOptions
    {
        public bool GenerateModel { get; set; } = true;
        public bool GenerateXmlMap { get; set; } = true;
        public bool GenerateRepository { get; set; } = true;
        public bool GenerateService { get; set; } = true;
        public bool GenerateController { get; set; } = true;
        public bool GenerateDemo { get; set; } = false;
        public bool GenerateReadme { get; set; } = false;
        public bool GenerateCache { get; set; } = true;
        public bool GenerateQueue { get; set; } = false;
        public bool GenerateWithInterface { get; set; } = true;
        public bool GenerateWithPagination { get; set; } = false;
        public bool GenerateWithTransaction { get; set; } = false;
        public bool GenerateWithSharding { get; set; } = false;
        public bool GenerateWithSync { get; set; } = false;
        public bool GenerateRawSql { get; set; } = false;
        public bool GenerateMapSql { get; set; } = false;
    }

    public class CodeGenerationResult
    {
        public string ModelCode { get; set; }
        public string XmlMapCode { get; set; }
        public string RepositoryCode { get; set; }
        public string ServiceCode { get; set; }
        public string ControllerCode { get; set; }
        public string DemoCode { get; set; }
        public string ReadmeCode { get; set; }
    }
}
