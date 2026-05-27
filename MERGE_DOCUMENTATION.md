# FastData.Tooling 重构与代码质量改进合并说明

## 合并概要

本次合并包含代码重构、质量修复和新功能添加，旨在提升代码可维护性、修复测试问题并增强日志记录能力。

## 主要变更

### 1. 代码重构 - 按功能拆分文件夹

**变更说明**：将 `FastData.Tooling/Sync/` 目录下的文件按功能拆分为子目录，提升项目结构清晰度。

**新目录结构**：
```
FastData.Tooling/Sync/
├── Logging/
│   └── Logger.cs                          # 新增：日志工具类
├── Models/
│   ├── DataSyncOptions.cs
│   ├── DataSyncResult.cs
│   ├── SyncDataType.cs
│   ├── SyncTaskConfig.cs
│   └── TableSyncConfig.cs
├── Services/
│   ├── DataSyncService.cs                 # 修复：业务主键配置集成
│   ├── PrimaryKeyConfigService.cs
│   └── SyncConfigManager.cs
└── Utils/
    ├── DataRowSerializer.cs               # 修复：JSON 序列化/反序列化问题
    ├── IntermediateSchemaBuilder.cs
    └── TimeRangeCalculator.cs
```

**影响范围**：
- 项目名称空间保持不变（`FastData.Tooling.Sync`）
- 所有现有引用无需修改
- 测试代码无需修改

### 2. 业务主键配置缺口修复

**问题描述**：
- `PrimaryKeyConfigService` 配置表从未在 `DataSyncService` 中被调用
- 业务主键配置无法生效，复合主键配置无法使用
- 去重逻辑只能依赖数据库表结构主键

**修复方案**：
修改以下方法，添加 `primaryKeyColumns` 参数支持：

1. `DataSyncService.TryInsertRowWithDedup()` - 新增参数并传递
2. `DataSyncService.UpsertRow()` - 新增参数并传递
3. `DataSyncService.CheckRowExists()` - 新增参数并传递给 `GetPrimaryKeyColumns`
4. `DataSyncService.UpdateRow()` - 新增参数并传递给 `GetPrimaryKeyColumns`
5. `DataSyncService.GetPrimaryKeyColumns()` - 优先使用配置的业务主键

**使用方法**：
```csharp
var options = new DataSyncOptions
{
    PrimaryKeyColumns = "email",  // 单字段业务主键
    // 或
    PrimaryKeyColumns = "order_id,line_no",  // 复合业务主键
    AlwaysDeduplicate = true
};
```

### 3. DataRowSerializer 测试修复

**修复的测试失败**（5 个）：
1. `Serialize_RowWithData_ReturnsValidJson` - 断言格式调整
2. `Deserialize_ValidJson_ReturnsDataTableWithSameSchema` - 修复列解析
3. `Deserialize_ValidJson_RestoresRowData` - 修复行数据恢复
4. `SerializeBatch_MultipleRows_ReturnsJsonArray` - 断言格式调整
5. `RoundTrip_SerializeThenDeserialize_PreservesData` - 修复序列化往返

**技术细节**：
- `JavaScriptSerializer` 反序列化嵌套对象返回 `IDictionary<string, object>` 而非 `Dictionary<string, object>`
- `IEnumerable` 替代 `object[]` 用于列数组解析
- 测试断言调整为匹配 JavaScriptSerializer 的实际输出格式（无空格）

**测试结果**：69/69 测试通过

### 4. 新增 Logger 日志工具

**功能特性**：
- 支持 4 个日志级别：Debug, Info, Warn, Error
- 自动按日期分割日志文件
- 支持按任务 ID 分割日志文件
- 线程安全的文件写入
- 异常日志支持

**使用示例**：
```csharp
// 初始化
Logger.Initialize("./logs", LogLevel.Info);

// 按任务设置日志文件
Logger.SetLogFile("user_sync_001");

// 记录日志
Logger.Debug("调试信息");
Logger.Info("同步开始");
Logger.Warn("警告信息");
Logger.Error("错误信息", exception);
```

### 5. 测试文件

**新增测试脚本**：
- `test_sync_integration.py` - MySQL → MySQL 中间库集成测试
- `test_mysql_to_pg_sync.py` - MySQL → PG 端到端同步测试（需要 PG 环境配置）

## 构建验证

```bash
# 构建主项目
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
FrameworkPathOverride="/root/.nuget/packages/microsoft.netframework.referenceassemblies.net45/1.0.3/build/.NETFramework/v4.5" \
/root/.dotnet/dotnet build FastData.Tooling/FastData.Tooling.csproj /p:RegisterForComInterop=false

# 构建并运行测试
/root/.dotnet/dotnet build FastData.Tests/FastData.Tests.csproj /p:RegisterForComInterop=false
mono /workspace/FastData.Tests/bin/Debug/FastData.Tests.exe
```

**验证结果**：
- 构建：0 Warning, 0 Error
- 测试：69/69 通过

## 回滚方案

如需回滚，执行：
```bash
git revert <commit-hash>
```

## 后续工作

1. **PostgreSQL 环境配置** - 修复 PG 数据库连接，完成 MySQL→PG 端到端测试
2. **Logger 集成** - 在 DataSyncService 中集成 Logger，记录同步过程日志
3. **性能优化** - 大批量数据同步性能测试与优化
4. **文档完善** - 补充 API 文档和使用示例

## 相关文件

### 新增文件
- `FastData.Tooling/Sync/Logging/Logger.cs`
- `SYNC_PROGRESS.md`
- `test_sync_integration.py`
- `test_mysql_to_pg_sync.py`

### 修改文件
- `FastData.Tooling/FastData.Tooling.csproj`
- `FastData.Tooling/Sync/DataSyncService.cs` (移动到 Services/ 目录)
- `FastData.Tooling/Sync/DataRowSerializer.cs` (移动到 Utils/ 目录)
- `FastData.Tests/Sync/DataRowSerializerTests.cs`

### 重命名/移动文件
- `FastData.Tooling/Sync/*.cs` → `FastData.Tooling/Sync/Models/*.cs`
- `FastData.Tooling/Sync/*.cs` → `FastData.Tooling/Sync/Services/*.cs`
- `FastData.Tooling/Sync/*.cs` → `FastData.Tooling/Sync/Utils/*.cs`

## 风险评估

- **低风险**：名称空间未变更，现有代码无需修改
- **中风险**：DataSyncService 参数变更，但使用默认值兼容旧调用
- **测试覆盖**：69 个单元测试全部通过

---

**合并日期**: 2026-05-27  
**测试状态**: ✓ 通过 (69/69)  
**构建状态**: ✓ 成功 (0 Warning, 0 Error)
