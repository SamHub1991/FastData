# 数据同步工具增强功能任务清单

更新时间：2026-05-24

---

## 一、已有的定时同步功能

### 1.1 已实现功能 ✅

- [x] **启用/禁用定时同步开关**
  - 控件：`enableTimerBox` (CheckBox)
  - 位置：界面第 18 行
  - 功能：勾选后启动定时同步，取消勾选停止定时同步

- [x] **同步间隔配置（秒）**
  - 控件：`intervalBox` (NumericUpDown)
  - 范围：5-3600 秒
  - 默认值：30 秒
  - 位置：界面第 14 行

- [x] **启动/停止按钮**
  - 控件：`syncButton` (Button)
  - 文本：动态切换"执行同步"/"停止同步"
  - 功能：
    - 定时模式：启动/停止定时器
    - 手动模式：执行单次同步

- [x] **运行状态指示器**
  - 控件：`syncStatusLabel` (Label)
  - 状态颜色：
    - 绿色：定时同步运行中
    - 橙色：同步进行中
    - 灰色：定时同步已暂停
    - 黑色：就绪状态

---

## 二、待实现功能

### 2.1 首次全量/后续三天范围同步

#### 需求描述
- **首次同步**：同步表中所有历史数据
- **后续同步**：只同步最近三天的数据（从上次同步时间点到现在）
- **自动记录**：每次同步完成后自动记录当前时间作为下次同步的起点

#### 实现方案

**方案 A：自动模式（推荐）**
```
配置项：
- [ ] 启用智能范围（勾选后自动判断首次/后续）
- [ ] 同步范围天数：3 天（可配置）

逻辑：
1. 读取上次同步时间（从配置文件或中间库）
2. 如果无历史记录 → 首次全量同步
3. 如果有历史记录 → 同步范围 = [上次同步时间 - 3 天，当前时间]
4. 同步完成后更新最后同步时间
```

**方案 B：手动模式**
```
配置项：
- [x] 首次全量同步（单选）
- [ ] 增量同步（单选）
  - 起始时间：[日期选择器]
  - 结束时间：[日期选择器]
```

#### 需要新增的 UI 控件
- `CheckBox smartRangeBox` - 启用智能范围
- `NumericUpDown rangeDaysBox` - 同步范围天数（默认 3 天）
- `Label lastSyncTimeLabel` - 显示上次同步时间
- `DateTimePicker startTimePicker` - 起始时间（手动模式）
- `DateTimePicker endTimePicker` - 结束时间（手动模式）

#### 需要新增的配置存储
```csharp
// SyncTaskConfig.cs
public class SyncTaskConfig
{
    public string TaskId { get; set; }
    public string SourceTable { get; set; }
    public DateTime? LastSyncTime { get; set; }  // 上次同步时间
    public bool IsFirstSync { get; set; }        // 是否首次同步
    public int RangeDays { get; set; } = 3;      // 同步范围天数
}
```

---

### 2.2 指定表列表同步

#### 需求描述
- 当前只能同步单个表（`sourceTableBox`）
- 需要支持一次配置多个表，批量同步
- 每个表可以有独立的主键配置和增量策略

#### 实现方案

**方案 A：多行表配置（推荐）**
```
界面改造：
- 将"源表"单行文本框改为多行列表
- 每行包含：表名 | 主键字段 | 增量字段 | 状态
- 支持添加、删除、上移、下移操作

示例：
┌─────────────┬──────────────┬──────────────┬────────┐
│ 表名        │ 主键字段     │ 增量字段     │ 状态   │
├─────────────┼──────────────┼──────────────┼────────┤
│ Users       │ Id           │ UpdateTime   │ 待同步 │
│ Orders      │ OrderId      │ OrderDate    │ 待同步 │
│ Products    │ ProductId    │ NULL         │ 待同步 │
└─────────────┴──────────────┴──────────────┴────────┘
[添加表] [删除表] [上移] [下移] [全选] [反选]
```

**方案 B：表选择器（备选）**
```
界面改造：
- 添加"选择表"按钮，弹出表选择对话框
- 对话框中显示所有可用表（支持搜索、多选）
- 选中的表显示在列表中

示例对话框：
┌────────────────────────────────────┐
│ 选择要同步的表                     │
├────────────────────────────────────┤
│ [搜索框]                           │
│ ☐ Users                            │
│ ☑ Orders                           │
│ ☑ Products                         │
│ ☐ Logs                             │
├────────────────────────────────────┤
│          [确定] [取消]             │
└────────────────────────────────────┘
```

#### 需要新增的 UI 控件
- `DataGridView tableListGrid` - 表配置列表
- `Button addTableButton` - 添加表
- `Button removeTableButton` - 删除表
- `Button moveUpButton` - 上移
- `Button moveDownButton` - 下移
- `Button selectTableButton` - 选择表（弹出对话框）

#### 需要同步逻辑改造
```csharp
// DataSyncOptions.cs 新增
public IList<TableSyncConfig> Tables { get; set; }  // 多表配置
public bool IsBatchSync { get; set; }               // 是否批量同步

// TableSyncConfig.cs 新增
public class TableSyncConfig
{
    public string TableName { get; set; }
    public string PrimaryKeyColumns { get; set; }
    public string IncrementalColumn { get; set; }
    public bool IsEnabled { get; set; }
}
```

---

### 2.3 时间段范围选择功能

#### 需求描述
- 支持手动指定同步的时间范围
- 适用于补历史数据、重新同步特定时间段等场景
- 与智能范围模式互斥（二选一）

#### 实现方案

**模式选择**
```
○ 智能范围模式（自动判断首次/后续，同步最近 N 天）
○ 手动范围模式（指定起止时间）
○ 全量同步模式（不限时间范围）
```

**手动范围配置**
```
起始时间：[2026-01-01 00:00:00] 📅
结束时间：[2026-01-31 23:59:59] 📅

[快速选择]
- 最近 1 天
- 最近 3 天
- 最近 7 天
- 最近 30 天
- 本月
- 上月
```

#### 需要新增的 UI 控件
- `RadioButton smartRangeRadio` - 智能范围模式
- `RadioButton manualRangeRadio` - 手动范围模式
- `RadioButton fullSyncRadio` - 全量同步模式
- `DateTimePicker startTimePicker` - 起始时间
- `DateTimePicker endTimePicker` - 结束时间
- `Button quickSelectButton` - 快速选择（下拉菜单）

#### SQL 生成逻辑
```csharp
private string BuildTimeRangeSql(DataSyncOptions options)
{
    if (options.SyncMode == SyncMode.Full)
        return "SELECT * FROM " + options.SourceTable;

    if (options.SyncMode == SyncMode.Smart)
    {
        var lastSyncTime = GetLastSyncTime(options.TaskId);
        var startTime = lastSyncTime ?? DateTime.MinValue;
        var endTime = DateTime.Now;
        return BuildTimeRangeQuery(options, startTime, endTime);
    }

    if (options.SyncMode == SyncMode.Manual)
    {
        return BuildTimeRangeQuery(options, options.StartTime, options.EndTime);
    }
}

private string BuildTimeRangeQuery(DataSyncOptions options, DateTime start, DateTime end)
{
    var timeColumn = options.IncrementalColumn ?? GetDefaultTimeColumn(options.SourceTable);
    // 参数化查询防止 SQL 注入
    return string.Format(
        "SELECT * FROM {0} WHERE {1} >= @startTime AND {1} <= @endTime",
        options.SourceTable, timeColumn);
}
```

---

## 三、任务优先级

### 高优先级（P0）
- [ ] **首次全量/后续三天范围同步逻辑**
  - 新增智能范围配置 UI
  - 实现 LastSyncTime 存储和读取
  - 修改 BuildSourceSql 支持时间范围
  - 同步完成后自动更新 LastSyncTime

- [ ] **指定表列表同步功能**
  - 改造单表 UI 为多表列表
  - 新增 TableSyncConfig 类
  - 修改 SyncTable 支持批量同步
  - 实现同步进度和结果汇总

### 中优先级（P1）
- [ ] **时间段范围选择功能**
  - 新增模式选择 RadioBox
  - 新增 DateTimePicker 控件
  - 实现快速选择按钮
  - 手动范围 SQL 生成逻辑

### 低优先级（P2）
- [ ] 同步任务配置文件（JSON/XML）
- [ ] 同步任务导入导出
- [ ] 同步历史记录查看
- [ ] 同步性能统计（速率、耗时）

---

## 四、界面布局建议

### 改造后的界面结构
```
┌────────────────────────────────────────────────────┐
│ 数据库配置（源库、目标库、中间库）                 │
├────────────────────────────────────────────────────┤
│ 同步模式配置                                       │
│ ○ 智能范围  ○ 手动范围  ○ 全量同步                │
│ 起始时间：[日期选择器]  结束时间：[日期选择器]    │
│ 同步范围：[3] 天    上次同步：2026-05-24 10:30    │
├────────────────────────────────────────────────────┤
│ 表配置列表                                         │
│ ┌─────────────────────────────────────────────┐   │
│ │ 表名  │ 主键  │ 增量字段 │ ☑ 启用 │ 状态  │   │
│ ├─────────────────────────────────────────────┤   │
│ │ User  │ Id    │ UpdateTime│ ☑    │ 待同步│   │
│ └─────────────────────────────────────────────┘   │
│ [添加表] [删除] [上移] [下移] [从数据库加载]      │
├────────────────────────────────────────────────────┤
│ 高级选项                                           │
│ ☑ 定时同步  间隔：[30] 秒                         │
│ ☐ 自动创建中间库  ☐ 恢复失败记录  ☐ 清理成功记录│
│ 批量大小：[500]  重试次数：[1]                    │
├────────────────────────────────────────────────────┤
│ [导出 SQL] [开始同步] [停止] [主键配置]            │
│ 状态：定时同步中 (每 30 秒)  [绿色]                │
├────────────────────────────────────────────────────┤
│ 运行日志                                           │
│ ┌─────────────────────────────────────────────┐   │
│ │ 2026-05-24 10:30:00 同步完成，读取100条... │   │
│ │ 2026-05-24 10:30:30 同步完成，读取 5 条...  │   │
│ └─────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────┘
```

---

## 五、技术要点

### 5.1 配置文件存储
```csharp
// 建议使用 JSON 配置文件
public class SyncConfigManager
{
    private readonly string configPath = "sync_config.json";
    
    public void SaveTaskConfig(SyncTaskConfig config)
    {
        var configs = LoadAllConfigs();
        configs[config.TaskId] = config;
        File.WriteAllText(configPath, JsonConvert.SerializeObject(configs));
    }
    
    public SyncTaskConfig GetTaskConfig(string taskId)
    {
        var configs = LoadAllConfigs();
        return configs.TryGetValue(taskId, out var config) ? config : null;
    }
}
```

### 5.2 时间范围计算
```csharp
public class TimeRangeCalculator
{
    public static DateTime GetSyncStartTime(DateTime? lastSyncTime, int rangeDays)
    {
        if (!lastSyncTime.HasValue)
            return DateTime.MinValue;  // 首次同步，全量
        
        // 后续同步：从 (上次同步时间 - rangeDays) 开始
        return lastSyncTime.Value.AddDays(-rangeDays);
    }
    
    public static DateTime GetSyncEndTime()
    {
        return DateTime.Now;
    }
}
```

### 5.3 批量同步调度
```csharp
public class BatchSyncExecutor
{
    private readonly IList<TableSyncConfig> tables;
    private int currentTableIndex = 0;
    
    public async Task ExecuteAllTablesAsync()
    {
        foreach (var table in tables.Where(t => t.IsEnabled))
        {
            UpdateStatus($"正在同步：{table.TableName}");
            var result = await SyncTableAsync(table);
            LogResult(table.TableName, result);
            currentTableIndex++;
        }
        UpdateStatus("全部同步完成");
    }
}
```

---

## 六、验收标准

### 6.1 智能范围同步
- [ ] 首次同步执行全量查询
- [ ] 第二次同步只同步最近 3 天数据
- [ ] 每次同步后自动记录最后同步时间
- [ ] 界面显示上次同步时间
- [ ] 可配置同步范围天数

### 6.2 多表批量同步
- [ ] 可添加多个表到同步列表
- [ ] 每个表可独立配置主键和增量字段
- [ ] 可选择性启用/禁用某个表
- [ ] 批量同步时显示当前进度
- [ ] 汇总所有表的同步结果

### 6.3 时间范围选择
- [ ] 三种模式切换正常（智能/手动/全量）
- [ ] 手动模式可精确选择起止时间
- [ ] 快速选择按钮提供常用时间范围
- [ ] 时间范围正确应用到 SQL 查询

---

## 七、相关文件

- `FastData.SyncTool.WinForms/MainForm.cs` - 主界面改造
- `FastData.SyncTool.WinForms/TableSelectForm.cs` - 表选择对话框（新建）
- `FastData.Tooling/Sync/DataSyncOptions.cs` - 新增多表和范围配置
- `FastData.Tooling/Sync/SyncTaskConfig.cs` - 任务配置类（新建）
- `FastData.Tooling/Sync/SyncConfigManager.cs` - 配置管理器（新建）
- `FastData.Tooling/Sync/TimeRangeCalculator.cs` - 时间范围计算（新建）
- `FastData.Tooling/Sync/BatchSyncExecutor.cs` - 批量同步执行器（新建）
