# FastData 项目进度总结

更新时间：2026-05-24

## 2026 年 5 月需求实施总结

### 已完成（代码实现与构建验证）

#### 1. 架构优化
- [x] 梳理现有多数据库重复代码。
- [x] 抽取数据库适配器接口 (`IDatabaseAdapter`)。
- [x] 抽取 SQL 方言接口 (`ISqlDialect`)。
- [x] 抽取元数据读取接口 (`IDatabaseMetadataReader`)。
- [x] 将工具项目与 ORM 核心项目分离。

#### 2. 多数据库配置简化
- [x] 新增统一 `Connections` 配置结构。
- [x] 保留旧配置格式兼容读取。
- [x] 实现默认数据库配置。
- [x] 实现 `FastRead.Use(key)` 和 `FastWrite.Use(key)`。
- [x] 实现 `FastDb.Use(key)` 作用域切换。
- [x] 实现 `IFastRepositoryFactory` 指定库 Repository。
- [x] 补充配置错误提示和可用 Key 提示。

#### 3. Model 生成工具
- [x] 新建 `FastData.Tooling` 公共工具项目。
- [x] 新建 `FastData.ModelGenerator.WinForms` 项目。
- [x] 实现数据库连接测试。
- [x] 实现表加载、多选和代码预览。
- [x] 实现默认命名空间配置。
- [x] 实现表搜索过滤。
- [x] 实现字段预览。
- [x] 实现单表命名空间覆盖。
- [x] 实现 Model 代码预览、编辑和生成。
- [x] 验证生成工具项目可编译。

#### 4. 数据同步工具
- [x] 新建 `FastData.SyncTool.WinForms` 项目。
- [x] 实现源库和目标库配置。
- [x] 设计中间库表结构。
- [x] 实现 SQL Server、MySQL、Oracle 中间库脚本生成。
- [x] 实现中间库 SQL 导出。
- [x] 实现基础全量同步。
- [x] 实现增量同步基础入口（增量字段 + 增量起点）。
- [x] 实现同步重试和错误计数。
- [x] 实现失败记录保存与恢复。
- [x] 实现中间库历史数据清理。
- [x] 实现自动创建中间库表。
- [x] 实现任务状态和错误日志界面。

#### 5. 中文文档
- [x] 编写快速开始文档 (`.monkeycode/docs/usage.md`)。
- [x] 编写多数据库配置和优雅切换文档。
- [x] 编写 Model 生成工具文档。
- [x] 编写数据同步工具文档。
- [x] 编写 XML SQL Map、Repository、AOP 和 FAQ 文档。

#### 6. 构建与代码质量
- [x] 构建通过，`0 Warning(s)`、`0 Error(s)`。
- [x] 修复 `BaseCodeDom` 过期 API。
- [x] 屏蔽既有 XML 文档注释警告，保留真实编译警告。
- [x] 新增数据库适配器与 SQL 方言抽象。

---

### 待环境验证（需真实数据库）

以下验证项代码已实现，但需要在具备真实数据库连接的环境中执行验证：

#### ORM 核心验证
- [ ] 验证原有 ORM API 兼容（`FastRead.Query`、`FastWrite.Add` 等）。
- [ ] 验证默认库和指定库切换写法。
- [ ] 验证多数据库同时使用场景。

#### 数据同步验证
- [ ] 验证源库到目标库端到端同步。
- [ ] 验证失败重试机制。
- [ ] 验证任务恢复（从中间库恢复失败记录）。
- [ ] 验证中间库清理。
- [ ] 验证增量同步。

#### Model 生成工具验证
- [ ] 使用真实数据库连接测试连接。
- [ ] 加载真实数据表。
- [ ] 生成 Model 并编译验证。

---

## 构建命令

```bash
# .NET Framework 4.5 构建（Linux 环境）
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" \
/root/.dotnet/dotnet build FastData.sln /p:RegisterForComInterop=false
```

---

## 验证清单（供真实数据库环境使用）

### ORM 核心验证清单

1. **默认库查询**
   ```csharp
   var users = FastRead.Query<User>(a => a.IsEnabled == true);
   ```
   - 预期：使用 `DefaultDb` 连接查询成功。

2. **指定库查询**
   ```csharp
   var reports = FastRead.Use("ReportDb").Query<Report>(a => a.Year == 2026);
   ```
   - 预期：使用 `ReportDb` 连接查询成功。

3. **作用域切换**
   ```csharp
   using (FastDb.Use("ArchiveDb"))
   {
       var archived = FastRead.Query<User>(a => a.IsArchived == true);
       FastWrite.Add(new User { UserName = "archive-user" });
   }
   ```
   - 预期：作用域内所有操作都使用 `ArchiveDb`。

4. **Repository 工厂**
   ```csharp
   var factory = new FastRepositoryFactory();
   var defaultRepo = factory.Default();
   var reportRepo = factory.Use("ReportDb");
   ```
   - 预期：两个 Repository 分别使用不同数据库。

---

### 数据同步验证清单

1. **全量同步**
   - 配置源库和目标库连接。
   - 配置源表和目标表。
   - 执行同步，验证读取和写入数量。

2. **增量同步**
   - 配置增量字段（如 `UpdateTime`）。
   - 配置增量起点（如 `2026-01-01`）。
   - 验证只同步增量数据。

3. **失败重试**
   - 配置失败重试次数（如 3 次）。
   - 模拟写入失败，验证重试机制。
   - 验证失败计数和日志。

4. **任务恢复**
   - 配置中间库连接。
   - 执行失败后，勾选"恢复失败记录"。
   - 验证失败记录重新写入成功。

5. **中间库清理**
   - 勾选"清理中间库成功记录"。
   - 验证 `fd_sync_record` 和 `fd_sync_batch` 中成功记录被清理。

---

### Model 生成工具验证清单

1. **连接测试**
   - 选择 Provider。
   - 输入连接字符串。
   - 点击"测试连接"，验证成功。

2. **加载数据表**
   - 点击"加载表"。
   - 验证数据表列表正确显示。

3. **字段预览**
   - 选择数据表。
   - 验证字段名、类型、是否可空、主键信息正确。

4. **生成 Model**
   - 选择多个表。
   - 设置命名空间。
   - 生成 `.cs` 文件。
   - 编译验证生成的 Model 可被项目引用。

---

## 已知注意事项

- `dotnet-install.sh` 为本地未跟踪文件，已排除在提交之外。
- Linux 环境构建需设置 `FrameworkPathOverride`。
- 使用 `/p:RegisterForComInterop=false` 绕过 COM 注册限制。

---

## 最近提交

- `b83e03a` chore: fix deprecated API and update progress
- `85845b2` refactor: add database adapter abstractions
- `52e99c9` feat: add sync recovery workflow
- `8b4216a` feat: enhance tooling workflows
- `638d865` feat: add model generator and sync tools
