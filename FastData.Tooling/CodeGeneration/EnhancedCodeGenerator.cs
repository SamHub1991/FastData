using FastData.Tooling.Database;
using System.Collections.Generic;
using System.Text;

namespace FastData.Tooling.CodeGeneration
{
    /// <summary>
    /// 增强的代码生成器 - 生成完整的 Model + Repository + Service
    /// </summary>
    public class EnhancedCodeGenerator
    {
        private readonly ModelCodeGenerator _modelGenerator = new ModelCodeGenerator();

        /// <summary>
        /// 生成选项
        /// </summary>
        public GeneratorOptions Options { get; set; } = new GeneratorOptions();

        /// <summary>
        /// 生成完整的代码文件
        /// </summary>
        public CodeGenerationResult GenerateComplete(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            var result = new CodeGenerationResult();

            if (Options.GenerateModel)
            {
                result.ModelCode = _modelGenerator.Generate(namespaceName, table, columns);
            }

            if (Options.GenerateXmlMap)
            {
                result.XmlMapCode = new XmlMapSqlGenerator().Generate(namespaceName, table, columns);
            }

            if (Options.GenerateRepository)
            {
                result.RepositoryCode = GenerateRepository(namespaceName, table, columns);
            }

            if (Options.GenerateService)
            {
                result.ServiceCode = GenerateService(namespaceName, table);
            }

            if (Options.GenerateController)
            {
                result.ControllerCode = GenerateController(namespaceName, table);
            }

            return result;
        }

        /// <summary>
        /// 生成 Model 类
        /// </summary>
        public string GenerateModel(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            return _modelGenerator.Generate(namespaceName, table, columns);
        }

        /// <summary>
        /// 生成 XML Map 文件
        /// </summary>
        public string GenerateXmlMap(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            return new XmlMapSqlGenerator().Generate(namespaceName, table, columns);
        }

        /// <summary>
        /// 生成 Repository 类
        /// </summary>
        public string GenerateRepository(string namespaceName, DatabaseTable table, IList<DatabaseColumn> columns)
        {
            var modelName = ToPascal(table.Name);
            var builder = new StringBuilder();

            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using System.Linq;");
            builder.AppendLine("using System.Threading.Tasks;");
            builder.AppendLine("using FastData;");
            builder.AppendLine("using FastData.Context;");
            builder.AppendLine("using FastUntility.Base;");
            builder.AppendLine();

            var repoNamespace = namespaceName + ".Repositories";
            builder.AppendLine("namespace " + repoNamespace);
            builder.AppendLine("{");

            if (Options.GenerateCache)
            {
                builder.AppendLine("    /// <summary>");
                builder.AppendLine("    /// " + modelName + " 仓储接口");
                builder.AppendLine("    /// </summary>");
                builder.AppendLine("    public interface I" + modelName + "Repository");
                builder.AppendLine("    {");
                builder.AppendLine("        Task<List<" + modelName + ">> GetAllAsync();");
                builder.AppendLine("        Task<" + modelName + "> GetByIdAsync(dynamic id);");
                builder.AppendLine("        Task<List<" + modelName + ">> QueryAsync(Func<" + modelName + ", bool> predicate);");
                builder.AppendLine("        Task<" + modelName + "> AddAsync(" + modelName + " entity);");
                builder.AppendLine("        Task<bool> UpdateAsync(" + modelName + " entity);");
                builder.AppendLine("        Task<bool> DeleteAsync(dynamic id);");
                builder.AppendLine("        Task<int> CountAsync();");
                builder.AppendLine("    }");
                builder.AppendLine();
            }

            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// " + modelName + " 仓储实现");
            builder.AppendLine("    /// </summary>");
            if (Options.GenerateCache)
            {
                builder.AppendLine("    public class " + modelName + "Repository : I" + modelName + "Repository");
            }
            else
            {
                builder.AppendLine("    public class " + modelName + "Repository");
            }
            builder.AppendLine("    {");
            builder.AppendLine("        private readonly string _cacheKey = \"" + modelName + "\";");
            builder.AppendLine("        private readonly string _dbKey;");
            builder.AppendLine();

            builder.AppendLine("        public " + modelName + "Repository(string dbKey = null)");
            builder.AppendLine("        {");
            builder.AppendLine("            _dbKey = dbKey ?? \"default\";");
            builder.AppendLine("        }");
            builder.AppendLine();

            // GetAllAsync
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 获取所有数据");
            builder.AppendLine("        /// </summary>");
            if (Options.GenerateCache)
            {
                builder.AppendLine("        public async Task<List<" + modelName + ">> GetAllAsync()");
                builder.AppendLine("        {");
                builder.AppendLine("            var cache = CacheHelper.Get<List<" + modelName + ">>(_cacheKey);");
                builder.AppendLine("            if (cache != null) return cache;");
                builder.AppendLine();
                builder.AppendLine("            using var db = new DataContext(_dbKey);");
                builder.AppendLine("            var result = await FastRead.Use(_dbKey).ToListAsync<" + modelName + ">();");
                builder.AppendLine("            CacheHelper.Set(_cacheKey, result, 30);");
                builder.AppendLine("            return result;");
                builder.AppendLine("        }");
            }
            else
            {
                builder.AppendLine("        public async Task<List<" + modelName + ">> GetAllAsync()");
                builder.AppendLine("        {");
                builder.AppendLine("            using var db = new DataContext(_dbKey);");
                builder.AppendLine("            return await FastRead.Use(_dbKey).ToListAsync<" + modelName + ">();");
                builder.AppendLine("        }");
            }
            builder.AppendLine();

            // GetByIdAsync
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 根据ID获取");
            builder.AppendLine("        /// </summary>");
            var keyColumn = FindPrimaryKey(columns);
            var keyType = keyColumn != null ? _modelGenerator.GetClrType(keyColumn) : "int";
            if (Options.GenerateCache)
            {
                builder.AppendLine("        public async Task<" + modelName + "> GetByIdAsync(" + keyType + " id)");
                builder.AppendLine("        {");
                builder.AppendLine("            var cacheKey = $\"{_cacheKey}_{id}\";");
                builder.AppendLine("            var cache = CacheHelper.Get<" + modelName + ">(cacheKey);");
                builder.AppendLine("            if (cache != null) return cache;");
                builder.AppendLine();
                builder.AppendLine("            using var db = new DataContext(_dbKey);");
                builder.AppendLine("            var result = await FastRead.Use(_dbKey).ToItemAsync<" + modelName + ">(q => q.Where(o => o." + (keyColumn != null ? ToPascal(keyColumn.Name) : "Id") + " == id));");
                builder.AppendLine("            if (result != null) CacheHelper.Set(cacheKey, result, 60);");
                builder.AppendLine("            return result;");
                builder.AppendLine("        }");
            }
            else
            {
                builder.AppendLine("        public async Task<" + modelName + "> GetByIdAsync(" + keyType + " id)");
                builder.AppendLine("        {");
                builder.AppendLine("            using var db = new DataContext(_dbKey);");
                builder.AppendLine("            return await FastRead.Use(_dbKey).ToItemAsync<" + modelName + ">(q => q.Where(o => o." + (keyColumn != null ? ToPascal(keyColumn.Name) : "Id") + " == id));");
                builder.AppendLine("        }");
            }
            builder.AppendLine();

            // AddAsync
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 添加");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        public async Task<" + modelName + "> AddAsync(" + modelName + " entity)");
            builder.AppendLine("        {");
            builder.AppendLine("            using var db = new DataContext(_dbKey);");
            builder.AppendLine("            var result = await FastWrite.Use(_dbKey).AddAsync(entity);");
            if (Options.GenerateCache)
            {
                builder.AppendLine("            if (result.IsSuccess) CacheHelper.Remove(_cacheKey);");
            }
            if (Options.GenerateQueue)
            {
                builder.AppendLine("            // 写入消息队列");
                builder.AppendLine("            await FastWrite.Use(_dbKey).Queue()");
                builder.AppendLine("                .Add(entity)");
                builder.AppendLine("                .WithMetadata(new Dictionary<string, object> { { \"Operation\", \"Insert\" }, { \"Table\", \"" + modelName + "\" } })");
                builder.AppendLine("                .ExecuteAsync();");
            }
            builder.AppendLine("            return entity;");
            builder.AppendLine("        }");
            builder.AppendLine();

            // UpdateAsync
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 更新");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        public async Task<bool> UpdateAsync(" + modelName + " entity)");
            builder.AppendLine("        {");
            builder.AppendLine("            using var db = new DataContext(_dbKey);");
            builder.AppendLine("            var result = await FastWrite.Use(_dbKey).UpdateAsync(entity);");
            if (Options.GenerateCache)
            {
                builder.AppendLine("            if (result.IsSuccess)");
                builder.AppendLine("            {");
                builder.AppendLine("                CacheHelper.Remove(_cacheKey);");
                builder.AppendLine("                CacheHelper.Remove($\"{_cacheKey}_{entity." + (keyColumn != null ? ToPascal(keyColumn.Name) : "Id") + "}\");");
                builder.AppendLine("            }");
            }
            if (Options.GenerateQueue)
            {
                builder.AppendLine("            // 写入消息队列");
                builder.AppendLine("            await FastWrite.Use(_dbKey).Queue()");
                builder.AppendLine("                .Add(entity)");
                builder.AppendLine("                .WithMetadata(new Dictionary<string, object> { { \"Operation\", \"Update\" }, { \"Table\", \"" + modelName + "\" } })");
                builder.AppendLine("                .ExecuteAsync();");
            }
            builder.AppendLine("            return result.IsSuccess;");
            builder.AppendLine("        }");
            builder.AppendLine();

            // DeleteAsync
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 删除");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        public async Task<bool> DeleteAsync(" + keyType + " id)");
            builder.AppendLine("        {");
            builder.AppendLine("            using var db = new DataContext(_dbKey);");
            builder.AppendLine("            var result = await FastWrite.Use(_dbKey).DeleteAsync<" + modelName + ">(q => q.Where(o => o." + (keyColumn != null ? ToPascal(keyColumn.Name) : "Id") + " == id));");
            if (Options.GenerateCache)
            {
                builder.AppendLine("            if (result.IsSuccess)");
                builder.AppendLine("            {");
                builder.AppendLine("                CacheHelper.Remove(_cacheKey);");
                builder.AppendLine("                CacheHelper.Remove($\"{_cacheKey}_{id}\");");
                builder.AppendLine("            }");
            }
            if (Options.GenerateQueue)
            {
                builder.AppendLine("            // 写入消息队列");
                builder.AppendLine("            await FastWrite.Use(_dbKey).Queue()");
                builder.AppendLine("                .Add(new " + modelName + " { " + (keyColumn != null ? ToPascal(keyColumn.Name) : "Id") + " = id })");
                builder.AppendLine("                .WithMetadata(new Dictionary<string, object> { { \"Operation\", \"Delete\" }, { \"Table\", \"" + modelName + "\" } })");
                builder.AppendLine("                .ExecuteAsync();");
            }
            builder.AppendLine("            return result.IsSuccess;");
            builder.AppendLine("        }");
            builder.AppendLine();

            // CountAsync
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 获取总数");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        public async Task<int> CountAsync()");
            builder.AppendLine("        {");
            builder.AppendLine("            using var db = new DataContext(_dbKey);");
            builder.AppendLine("            return await FastRead.Use(_dbKey).ToCountAsync<" + modelName + ">();");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        /// <summary>
        /// 生成 Service 类
        /// </summary>
        public string GenerateService(string namespaceName, DatabaseTable table)
        {
            var modelName = ToPascal(table.Name);
            var builder = new StringBuilder();

            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using System.Threading.Tasks;");
            builder.AppendLine("using " + namespaceName + ".Repositories;");
            builder.AppendLine();

            var serviceNamespace = namespaceName + ".Services";
            builder.AppendLine("namespace " + serviceNamespace);
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// " + modelName + " 服务接口");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public interface I" + modelName + "Service");
            builder.AppendLine("    {");
            builder.AppendLine("        Task<List<" + modelName + ">> GetAllAsync();");
            builder.AppendLine("        Task<" + modelName + "> GetByIdAsync(dynamic id);");
            builder.AppendLine("        Task<" + modelName + "> CreateAsync(" + modelName + " entity);");
            builder.AppendLine("        Task<bool> UpdateAsync(" + modelName + " entity);");
            builder.AppendLine("        Task<bool> DeleteAsync(dynamic id);");
            builder.AppendLine("    }");
            builder.AppendLine();

            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// " + modelName + " 服务实现");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public class " + modelName + "Service : I" + modelName + "Service");
            builder.AppendLine("    {");
            builder.AppendLine("        private readonly I" + modelName + "Repository _repository;");
            builder.AppendLine();

            builder.AppendLine("        public " + modelName + "Service(I" + modelName + "Repository repository)");
            builder.AppendLine("        {");
            builder.AppendLine("            _repository = repository;");
            builder.AppendLine("        }");
            builder.AppendLine();

            builder.AppendLine("        public Task<List<" + modelName + ">> GetAllAsync() => _repository.GetAllAsync();");
            builder.AppendLine("        public Task<" + modelName + "> GetByIdAsync(dynamic id) => _repository.GetByIdAsync(id);");
            builder.AppendLine("        public async Task<" + modelName + "> CreateAsync(" + modelName + " entity)");
            builder.AppendLine("        {");
            builder.AppendLine("            entity.CreateTime = DateTime.Now;");
            builder.AppendLine("            return await _repository.AddAsync(entity);");
            builder.AppendLine("        }");
            builder.AppendLine("        public Task<bool> UpdateAsync(" + modelName + " entity) => _repository.UpdateAsync(entity);");
            builder.AppendLine("        public Task<bool> DeleteAsync(dynamic id) => _repository.DeleteAsync(id);");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        /// <summary>
        /// 生成 Controller 类
        /// </summary>
        public string GenerateController(string namespaceName, DatabaseTable table)
        {
            var modelName = ToPascal(table.Name);
            var controllerName = modelName + "Controller";
            var builder = new StringBuilder();

            builder.AppendLine("using Microsoft.AspNetCore.Mvc;");
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using System.Threading.Tasks;");
            builder.AppendLine("using " + namespaceName + ".Models;");
            builder.AppendLine("using " + namespaceName + ".Services;");
            builder.AppendLine();

            var controllerNamespace = namespaceName + ".Controllers";
            builder.AppendLine("namespace " + controllerNamespace);
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// " + modelName + " API 控制器");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    [ApiController]");
            builder.AppendLine("    [Route(\"api/[controller]\")]");
            builder.AppendLine("    public class " + controllerName + " : ControllerBase");
            builder.AppendLine("    {");
            builder.AppendLine("        private readonly I" + modelName + "Service _service;");
            builder.AppendLine();

            builder.AppendLine("        public " + controllerName + "(I" + modelName + "Service service)");
            builder.AppendLine("        {");
            builder.AppendLine("            _service = service;");
            builder.AppendLine("        }");
            builder.AppendLine();

            // GET all
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 获取所有" + modelName);
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        [HttpGet]");
            builder.AppendLine("        public async Task<ActionResult<List<" + modelName + ">>> GetAll()");
            builder.AppendLine("        {");
            builder.AppendLine("            var result = await _service.GetAllAsync();");
            builder.AppendLine("            return Ok(result);");
            builder.AppendLine("        }");
            builder.AppendLine();

            // GET by id
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 根据ID获取" + modelName);
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        [HttpGet(\"{id}\")]");
            builder.AppendLine("        public async Task<ActionResult<" + modelName + ">> GetById(dynamic id)");
            builder.AppendLine("        {");
            builder.AppendLine("            var result = await _service.GetByIdAsync(id);");
            builder.AppendLine("            if (result == null) return NotFound();");
            builder.AppendLine("            return Ok(result);");
            builder.AppendLine("        }");
            builder.AppendLine();

            // POST
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 创建" + modelName);
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        [HttpPost]");
            builder.AppendLine("        public async Task<ActionResult<" + modelName + ">> Create([FromBody] " + modelName + " entity)");
            builder.AppendLine("        {");
            builder.AppendLine("            var result = await _service.CreateAsync(entity);");
            builder.AppendLine("            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);");
            builder.AppendLine("        }");
            builder.AppendLine();

            // PUT
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 更新" + modelName);
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        [HttpPut(\"{id}\")]");
            builder.AppendLine("        public async Task<ActionResult> Update(dynamic id, [FromBody] " + modelName + " entity)");
            builder.AppendLine("        {");
            builder.AppendLine("            var result = await _service.UpdateAsync(entity);");
            builder.AppendLine("            if (!result) return NotFound();");
            builder.AppendLine("            return NoContent();");
            builder.AppendLine("        }");
            builder.AppendLine();

            // DELETE
            builder.AppendLine("        /// <summary>");
            builder.AppendLine("        /// 删除" + modelName);
            builder.AppendLine("        /// </summary>");
            builder.AppendLine("        [HttpDelete(\"{id}\")]");
            builder.AppendLine("        public async Task<ActionResult> Delete(dynamic id)");
            builder.AppendLine("        {");
            builder.AppendLine("            var result = await _service.DeleteAsync(id);");
            builder.AppendLine("            if (!result) return NotFound();");
            builder.AppendLine("            return NoContent();");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static string ToPascal(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Model";
            var builder = new StringBuilder();
            var upper = true;
            foreach (var ch in value)
            {
                if (!char.IsLetterOrDigit(ch)) { upper = true; continue; }
                builder.Append(upper ? char.ToUpperInvariant(ch) : ch);
                upper = false;
            }
            return builder.Length == 0 || char.IsDigit(builder[0]) ? "Model" : builder.ToString();
        }

        private static DatabaseColumn FindPrimaryKey(IList<DatabaseColumn> columns)
        {
            foreach (var col in columns)
                if (col.IsPrimaryKey) return col;
            return null;
        }
    }

    /// <summary>
    /// 代码生成选项
    /// </summary>
    public class GeneratorOptions
    {
        public bool GenerateModel { get; set; } = true;
        public bool GenerateXmlMap { get; set; } = true;
        public bool GenerateRepository { get; set; } = true;
        public bool GenerateService { get; set; } = true;
        public bool GenerateController { get; set; } = true;
        public bool GenerateCache { get; set; } = true;
        public bool GenerateQueue { get; set; } = false;
    }

    /// <summary>
    /// 代码生成结果
    /// </summary>
    public class CodeGenerationResult
    {
        public string ModelCode { get; set; }
        public string XmlMapCode { get; set; }
        public string RepositoryCode { get; set; }
        public string ServiceCode { get; set; }
        public string ControllerCode { get; set; }
    }
}
