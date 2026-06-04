# FastData.SyncTool（数据同步工具）

跨数据库数据同步工具，支持 SQL Server、MySQL、PostgreSQL、SQLite 之间的数据同步。

## 目标框架

| 框架 | 说明 |
|------|------|
| net10.0-windows | .NET 10 Windows Desktop |

---

## 功能概览

| 功能 | 说明 |
|------|------|
| **多数据库同步** | SQL Server / MySQL / PostgreSQL / SQLite 之间互相同步 |
| **中间库模式** | 源库 → 中间库 → 目标库，支持断点续传 |
| **增量同步** | 按时间字段增量同步 |
| **全量同步** | 按主键全量同步 |
| **失败重试** | 自动重试 + 失败记录恢复 |
| **定时调度** | 可配置间隔（5-3600 秒） |
| **批量处理** | 可配置批次大小 |
| **去重模式** | 插入前自动检查业务主键 |
| **分表同步** | 支持 5 种分表策略同步 |

---

## 项目结构

```
FastData.SyncTool.WinForms/
├── MainForm.cs                     # 主窗口
├── Program.cs                      # 程序入口
├── Components/                     # 模块化组件
│   ├── DbConfigControl.cs          # 数据库配置
│   ├── SyncConfigControl.cs        # 同步配置
│   ├── TaskManagerControl.cs       # 任务管理
│   ├── ReplayControl.cs            # 数据补录
│   ├── ShardingSyncControl.cs      # 分表同步
│   ├── ShardingTaskControl.cs      # 分表任务
│   ├── ShardingImportControl.cs    # 分表导入
│   └── ShardingDataControl.cs      # 分表数据操作
├── Services/                       # 服务层
│   ├── SyncService.cs              # 同步服务
│   ├── LogService.cs               # 日志服务
│   ├── ReplayService.cs            # 数据补录服务
│   └── ShardingTaskService.cs      # 分表任务服务
└── IoC/                            # 依赖注入
    └── ServiceProvider.cs          # DI 容器
```

---

## 同步配置示例

### SQL Server → MySQL 同步

```json
{
  "TaskId": "ss_to_mysql",
  "SourceProvider": "SqlServer",
  "SourceConnStr": "server=localhost;database=FastDataDemo;uid=sa;pwd=xxx",
  "TargetProvider": "MySql",
  "TargetConnStr": "server=127.0.0.1;database=FastDataDemo;uid=root;pwd=xxx",
  "SourceTable": "AppUser",
  "TargetTable": "AppUser",
  "SyncColumns": "Id,UserName,Email,Phone,Age,IsActive,CreateTime",
  "KeyColumns": "Id",
  "BatchSize": 1000,
  "AlwaysDeduplicate": true,
  "EnableRetry": true,
  "MaxRetryCount": 3
}
```

### SQL Server → PostgreSQL 同步

```json
{
  "TaskId": "ss_to_pg",
  "SourceProvider": "SqlServer",
  "SourceConnStr": "server=localhost;database=FastDataDemo;uid=sa;pwd=xxx",
  "TargetProvider": "Npgsql",
  "TargetConnStr": "Host=localhost;Database=fastdatademo;Username=postgres;Password=xxx",
  "SourceTable": "AppUser",
  "TargetTable": "AppUser",
  "SyncColumns": "Id,UserName,Email,IsActive,CreateTime",
  "KeyColumns": "Id",
  "BatchSize": 1000
}
```

---

## 同步策略

| 策略 | 触发条件 | 行为 |
|------|---------|------|
| 全量同步 | 无时间字段 | 按主键逐行同步 |
| 增量同步 | 有时间字段 + RangeDays | 按时间范围同步 |
| 去重插入 | AlwaysDeduplicate=true | INSERT 前检查存在性 |
| UPSERT | AlwaysDeduplicate=false | 存在则 UPDATE，不存在 INSERT |
| 直接插入 | 无主键 | 直接 INSERT |

---

## 全局配置

```csharp
var config = new TableSyncConfig
{
    EnableGlobalConfig = true,      // 启用全局统一配置
    GlobalRangeDays = 7,            // 全局同步范围（天）
    AlwaysDeduplicate = true,       // 始终根据业务主键去重
    EnableMessageQueue = true,      // 启用消息队列
    MessageQueueType = MessageQueueType.ReliableQueue,
    MessageQueueTopic = "sync:users",
    ConsumerConcurrency = 8         // 消费者并发数
};
```

---

## 构建

```bash
dotnet build FastData.SyncTool.WinForms --framework net10.0-windows
```

## 依赖

- FastData（ORM 核心）
- FastData.Tooling（工具库）
- FastData.Shared（共享库）

## 许可证

MIT License
