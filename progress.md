
## 2026-05-25: 数据库配置和补录功能

### 新增功能

#### 1. 数据库配置管理 Tab 页

**功能**：
- 独立 Tab 页集中管理数据库连接
- 保存常用连接（名称、类型、连接字符串）
- 测试连接可用性
- 快速加载已保存连接
- 删除连接

**UI 控件**：
- 连接名称输入框
- 数据库类型下拉框（SQL Server、MySQL、Oracle、PostgreSQL、SQLite）
- 连接字符串多行文本框
- 测试连接/保存连接/删除连接按钮
- 连接列表表格（名称、类型、连接字符串、最后测试时间）
- 配置日志框

**配置文件**：`db_connections.json`

#### 2. 数据补录 Tab 页

**功能**：
- 手动补录指定表的历史数据
- 支持跨库同步（源库和目标库可不同）
- 按时间范围筛选数据（可选）
- 根据业务主键自动判断更新/插入
- 数据库连接失败自动重试（最多 3 次，间隔 5 秒）

**UI 控件**：
- 源库配置（Provider + 连接字符串）
- 目标库配置（Provider + 连接字符串）
- 表名输入框（支持自动完成）
- 业务主键配置（支持复合主键）
- 加载表列表按钮
- 时间范围配置（启用复选框 + 开始/结束时间）
- 开始补录按钮
- 状态标签
- 补录日志框

**更新/插入逻辑**：
- 主键已存在 → UPDATE
- 主键不存在 → INSERT
- 无主键配置 → INSERT（不去重）

**重试机制**：
- 数据库连接失败自动重试
- 最多重试 3 次
- 重试间隔 5 秒
- 记录和显示每次重试信息

#### 3. 共享日志面板

**功能**：
- 所有 Tab 页操作日志统一显示
- 时间戳精确到毫秒
- 日志级别（INFO/WARN/ERROR）
- 自动滚动到最新消息
- 深色主题（#1E1E1E 背景，#DCDCDC 文字）
- 清空日志按钮
- 导出日志按钮

**日志格式**：
```
[yyyy-MM-dd HH:mm:ss.fff] [LEVEL] Message
```

**导出功能**：
- 导出为文本文件
- 文件名：`sync_log_yyyyMMdd_HHmmss.txt`
- 包含所有历史日志

### 核心实现

#### ReplayService

**位置**：`FastData.SyncTool.WinForms/ReplayService.cs`

**主要方法**：
- `ReplayTableAsync()` - 补录表数据
- `GetTableSchema()` - 获取表结构
- `BuildSelectSql()` - 构建查询 SQL（含时间范围）
- `LoadExistingKeys()` - 加载目标表现有主键
- `InsertRowAsync()` - 插入记录
- `UpdateRowAsync()` - 更新记录

**补录流程**：
1. 获取源表和目标表结构
2. 解析业务主键
3. 构建带时间范围的 SELECT SQL
4. 读取源数据
5. 加载目标表现有主键
6. 遍历数据：存在则 UPDATE，不存在则 INSERT
7. 批量处理（每批 100 条）
8. 记录日志和统计

#### MainForm 增强

**新增字段**：
- 数据库配置控件（dbConnectionNameBox、dbProviderBox 等）
- 补录功能控件（replaySourceProviderBox、replayTableNameBox 等）
- 共享日志控件（sharedLogBox、clearLogButton、exportLogButton）
- SplitContainer 分割上下布局

**新增方法**：
- `BuildDbConfigTab()` - 构建数据库配置页
- `BuildReplayTab()` - 构建补录页
- `TestDatabaseConnection()` - 测试连接
- `SaveDbConnection()` - 保存连接
- `DeleteDbConnection()` - 删除连接
- `ReplayLoadTables()` - 加载表列表
- `ExecuteReplay()` - 执行补录
- `LogInternal()` - 内部日志记录
- `ExportLog()` - 导出日志

**布局变化**：
```
┌─────────────────────────────────┐
│  TabControl (4 个 Tab 页)         │
│  ┌─────────────────────────┐   │
│  │ 数据库配置│同步配置│任务管理│补录│   │
│  └─────────────────────────┘   │
├─────────────────────────────────┤
│  共享日志面板                    │
│  [清空日志] [导出日志]            │
│  ┌─────────────────────────┐   │
│  │  日志内容（滚动）        │   │
│  └─────────────────────────┘   │
└─────────────────────────────────┘
```

### 技术细节

#### .NET Framework 4.5 兼容性

**修复问题**：
1. `FixedPanel.Bottom` 不存在 → 移除 FixedPanel 设置
2. `TextBox.PlaceholderText` 不存在 → 移除占位符
3. `String.Contains(string, comparison)` 不存在 → 改用 `IndexOf() >= 0`
4. `DataTable.AsEnumerable()` 需要额外引用 → 改用 foreach 遍历
5. `System.Text.Json` 不支持 → 改用 `JavaScriptSerializer`
6. `DbProviderFactoryHelper` 不存在 → 改用 `DbProviderFactories.GetFactory()`

**添加引用**：
- `System.Web.Extensions`（JavaScriptSerializer）
- `System.Xml`（DataTable schema）

#### 数据库连接工厂

```csharp
private static DbConnection CreateDbConnection(string provider, string connectionString)
{
    var factory = DbProviderFactories.GetFactory(provider);
    var connection = factory.CreateConnection();
    if (connection == null)
        throw new InvalidOperationException("无法创建数据库连接");
    connection.ConnectionString = connectionString;
    return connection;
}
```

#### 补录重试逻辑

```csharp
var retryCount = 0;
var maxRetries = 3;
var retryDelay = TimeSpan.FromSeconds(5);

while (retryCount < maxRetries)
{
    try
    {
        // 执行补录
        break;
    }
    catch (DbException dbEx)
    {
        retryCount++;
        Log($"数据库错误 (重试 {retryCount}/{maxRetries})：{dbEx.Message}");
        if (retryCount >= maxRetries) throw;
        await Task.Delay(retryDelay);
    }
}
```

### 构建状态

`Build succeeded. 0 Warning(s), 0 Error(s)`

### 文档更新

- `.monkeycode/docs/sync-tool.md` - 新增第 12-14 章
  - 数据库配置管理
  - 数据补录功能
  - 共享日志面板

### 新增文件

- `FastData.SyncTool.WinForms/ReplayService.cs` - 补录服务
- `FastData.SyncTool.WinForms/db_connections.json` - 连接配置（运行时创建）

### 修改文件

- `FastData.SyncTool.WinForms/MainForm.cs` - 新增约 600 行代码
- `FastData.SyncTool.WinForms/FastData.SyncTool.WinForms.csproj` - 添加引用和文件
- `.monkeycode/docs/sync-tool.md` - 新增 3 章（约 400 行）

### 后续工作

- 补录功能真实数据库验证
- 数据库连接池优化（可选）
- 补录进度条显示（可选）
- 批量补录多表（可选）

