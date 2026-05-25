# FastData 数据同步工具使用指南

更新时间：2026-05-25

---

## 概述

FastData 数据同步工具是一个基于 WinForms 的可视化数据同步管理工具，支持多表批量同步、字段级选择、定时同步、任务管理和数据恢复。

### 核心特性

- **多数据库支持**：SQL Server、MySQL、Oracle、PostgreSQL、DB2
- **中间库模式**：源库 → 中间库 → 目标库，确保数据一致性
- **智能同步策略**：静态数据按主键增量，动态数据按时间范围增量
- **字段级控制**：支持选择部分字段同步，主键强制包含
- **任务管理**：完整 CRUD 操作，支持批量启用/禁用/删除
- **定时同步**：可配置 5-3600 秒同步间隔
- **失败恢复**：自动记录失败数据，支持从中间库恢复
- **导入导出**：支持任务配置的 JSON 导入导出

---

## 快速开始

### 1. 启动工具

```bash
cd FastData.SyncTool.WinForms/bin/Debug
FastData.SyncTool.WinForms.exe
```

### 2. 配置数据库连接

在**同步配置**Tab 页填写：

| 字段 | 说明 | 示例 |
|------|------|------|
| 源库 Provider | 源数据库类型 | `System.Data.SqlClient` |
| 源库连接字符串 | 源数据库连接 | `server=.;database=SourceDb;uid=sa;pwd=123` |
| 目标库 Provider | 目标数据库类型 | `MySql.Data.MySqlClient` |
| 目标库连接字符串 | 目标数据库连接 | `server=127.0.0.1;database=TargetDb;uid=root;pwd=123` |
| 中间库 Provider | 中间库类型 | `System.Data.SqlClient` |
| 中间库连接字符串 | 中间库连接 | `server=.;database=SyncBuffer;uid=sa;pwd=123` |
| 任务 ID | 任务唯一标识 | `daily_sync_20260525` |

### 3. 添加同步表

1. 点击**加载表列表**从源库加载所有表
2. 或点击**添加表**手动选择表
3. 在表配置列表中配置每表参数

### 4. 配置表参数

选中表后在下方配置：

| 参数 | 说明 | 静态数据 | 动态数据 |
|------|------|----------|----------|
| 主键字段 | 表的主键列名 | `Id` | `Id` |
| 时间字段 | 用于增量判断的时间列 | （留空） | `UpdateTime` |
| 启用时间范围 | 是否按时间范围同步 | ☐ | ☑ |
| 时间范围 (天) | 每次同步最近 N 天 | - | `3` |
| 同步字段 | 点击"选择字段"按钮配置 | 全选 | 部分字段 |
| 启用 | 是否同步此表 | ☑ | ☑ |

### 5. 保存任务

点击**保存任务配置**，配置将保存到 `sync_tasks.json`。

### 6. 执行同步

- **立即同步**：点击按钮立即执行一次同步
- **定时同步**：勾选"启用定时同步"，设置间隔秒数

---

## 高级功能

### 字段级选择

1. 在表配置列表中点击某行的"选择"按钮
2. 在字段选择对话框中：
   - ☑ **全选**：勾选所有字段
   - ☐ **反选**：反转当前选择
   - ☑ **主键字段**：自动勾选且不可取消
3. 点击确定保存选择

**注意**：主键字段必须在同步字段中，否则会提示错误。

### 任务管理

切换到**任务管理**Tab 页：

| 按钮 | 功能 | 说明 |
|------|------|------|
| 新建任务 | 创建空任务 | 输入任务名称后在同步配置页添加表 |
| 编辑任务 | 加载任务配置 | 将任务加载到同步配置页进行修改 |
| 加载任务 | 加载并切换 | 加载任务配置并切换到同步配置页 |
| 刷新列表 | 重新加载 | 从配置文件重新加载任务列表 |
| 批量启用 | 启用选中任务 | 勾选复选框后批量启用 |
| 批量禁用 | 禁用选中任务 | 勾选复选框后批量禁用 |
| 批量删除 | 删除选中任务 | 勾选复选框后批量删除 |
| 导出配置 | 导出为 JSON | 导出单个任务配置到文件 |
| 导入配置 | 从 JSON 导入 | 从文件导入任务配置 |

### 同步策略说明

#### 静态数据（无时间字段）

- 首次同步：全量同步所有数据
- 后续同步：按主键最大值增量同步
- 适用场景：配置表、字典表、极少变更的数据

#### 动态数据（有时间字段）

- 首次同步：全量同步所有数据
- 后续同步：按时间字段同步最近 N 天数据
- 适用场景：订单表、日志表、频繁变更的数据

### 定时同步配置

1. 勾选"启用定时同步"
2. 设置间隔秒数（5-3600 秒）
3. 点击"立即同步"启动定时器

定时器会在后台周期性执行同步，状态栏显示"同步中..."时表示正在执行。

---

## 任务配置 JSON 格式

### 导出示例

```json
{
  "TaskId": "daily_sync_20260525",
  "TaskName": "每日同步任务",
  "SourceConnection": "server=.;database=SourceDb;uid=sa;pwd=123",
  "TargetConnection": "server=127.0.0.1;database=TargetDb;uid=root;pwd=123",
  "IntermediateConnection": "server=.;database=SyncBuffer;uid=sa;pwd=123",
  "Tables": [
    {
      "TableName": "Users",
      "PrimaryKeyColumns": "Id",
      "TimeColumn": "UpdateTime",
      "EnableTimeRange": true,
      "RangeDays": 3,
      "DataType": "Dynamic",
      "SyncColumns": "Id,Name,Email,UpdateTime"
    },
    {
      "TableName": "Products",
      "PrimaryKeyColumns": "Id",
      "TimeColumn": "",
      "EnableTimeRange": false,
      "RangeDays": 3,
      "DataType": "Static",
      "SyncColumns": "Id,ProductName,Price"
    }
  ]
}
```

### 导入步骤

1. 点击**导入配置**按钮
2. 选择之前导出的 JSON 文件
3. 验证导入成功提示
4. 在任务列表中查看导入的任务

---

## 同步流程

```
1. 读取配置
   ↓
2. 连接源库、中间库、目标库
   ↓
3. 对每个启用的表执行：
   ├─ 判断数据类型（静态/动态）
   ├─ 构建源库查询 SQL
   ├─ 读取数据到中间库
   ├─ 从中间库写入目标库
   ├─ 更新最后同步时间
   └─ 记录同步状态
   ↓
4. 清理中间库历史数据（可选）
   ↓
5. 保存任务配置
```

### 中间库表结构

中间库表自动创建，结构包含：

| 列名 | 类型 | 说明 |
|------|------|------|
| 源表所有列 | - | 与源表结构一致 |
| `__sync_status` | int | 0=待同步，1=已同步，2=失败 |
| `__sync_time` | datetime | 同步时间 |
| `__sync_error` | ntext | 错误信息（失败时） |

---

## 故障排查

### 连接失败

**现象**：点击"加载表列表"或"立即同步"时报连接错误

**排查步骤**：
1. 检查连接字符串格式是否正确
2. 确认数据库服务正在运行
3. 验证网络连通性
4. 检查防火墙设置

### 同步失败

**现象**：状态显示"部分失败"或"失败"

**排查步骤**：
1. 查看日志区域的错误信息
2. 检查目标库表结构是否匹配
3. 验证主键配置是否正确
4. 确认中间库有足够空间

### 主键校验失败

**现象**：保存任务时提示"主键字段不在同步字段列表中"

**解决方法**：
1. 点击"选择字段"按钮
2. 确保所有主键字段都被勾选
3. 保存字段选择后重新保存任务

### 定时同步不执行

**现象**：勾选定时同步后没有周期性执行

**排查步骤**：
1. 确认间隔设置在 5-3600 秒范围内
2. 检查是否点击过"立即同步"启动定时器
3. 查看日志是否有定时器启动信息

---

## 最佳实践

### 1. 任务命名规范

建议使用有意义的任务名称：
- `daily_sync_users` - 每日用户表同步
- `hourly_sync_orders` - 每小时订单同步
- `realtime_sync_logs` - 实时日志同步

### 2. 时间范围配置

- 高频同步（< 1 分钟）：1 天
- 中频同步（1-10 分钟）：3-7 天
- 低频同步（> 10 分钟）：7-30 天

### 3. 字段选择策略

优先同步必要字段，减少网络传输：
- ✅ 主键字段（必须）
- ✅ 业务关键字段
- ❌ 大文本字段（如非必需）
- ❌ 计算字段

### 4. 中间库管理

定期清理中间库，避免数据积压：
- 启用"同步后清理中间库"选项
- 或定期手动执行：`TRUNCATE TABLE [中间库表]`

### 5. 备份策略

导出任务配置到安全位置：
- 版本控制（Git）
- 定期备份
- 多环境同步（开发/测试/生产）

---

## API 扩展

### 编程方式调用

数据同步工具的核心逻辑在 `FastData.Tooling.Sync` 命名空间：

```csharp
using FastData.Tooling.Sync;

var options = new DataSyncOptions
{
    SourceProvider = "System.Data.SqlClient",
    SourceConnectionString = "source_conn",
    TargetProvider = "MySql.Data.MySqlClient",
    TargetConnectionString = "target_conn",
    TaskId = "my_task",
    SourceTable = "Users",
    TargetTable = "Users",
    PrimaryKeyColumns = "Id",
    TimeColumn = "UpdateTime",
    EnableTimeRange = true,
    RangeDays = 3,
    BatchSize = 1000,
    RetryCount = 3
};

var result = new DataSyncService().SyncTable(options);
Console.WriteLine($"读取 {result.ReadCount} 条，写入 {result.WriteCount} 条");
```

---

## 常见问题

### Q: 中间库的作用是什么？

A: 中间库作为缓冲区，确保同步过程的数据一致性。源库数据先写入中间库，成功后再写入目标库。如果目标库写入失败，可以从中间库恢复，避免数据丢失。

### Q: 如何跳过某些表的同步？

A: 在表配置列表中取消勾选"启用"列，或直接在任务管理 Tab 页禁用该任务。

### Q: 可以在不同数据库类型之间同步吗？

A: 可以。工具支持跨数据库类型同步（如 SQL Server → MySQL）。注意数据类型映射和字段兼容性。

### Q: 同步失败后如何恢复？

A: 勾选"失败记录续传"选项，下次同步会自动从中间库读取失败记录并重试。

### Q: 如何查看详细的同步日志？

A: 工具底部的日志区域会显示每次同步的详细信息，包括读取条数、写入条数、失败条数和错误信息。

---

## 相关文件

- `FastData.SyncTool.WinForms/` - 工具主项目
- `FastData.Tooling.Sync/` - 同步核心库
- `sync_tasks.json` - 任务配置文件（运行时生成）

---

**最后更新**：2026-05-25
**版本**：v1.0
