# FastData.SyncTool.WinForms 功能测试指南

## 概述

`FastData.SyncTool.WinForms` 是独立的数据同步工具，用于在四个数据库（SQL Server、MySQL、PostgreSQL、SQLite）之间进行数据同步。

## 项目结构

```
FastData.SyncTool.WinForms/
├── MainForm.cs                 # 主界面（重构后）
├── MainFormRefactored.cs       # 重构版本界面
├── Program.cs                  # 程序入口
├── Services/
│   ├── SyncService.cs          # 同步服务（批处理、进度事件）
│   ├── ReplayService.cs        # 重放服务（失败重试）
│   └── ShardingTaskService.cs  # 分表任务服务
├── Components/
│   ├── SyncConfigPanel.cs      # 同步配置面板
│   ├── ShardingSyncControl.cs  # 分表同步控件
│   ├── ShardingImportControl.cs # 分表导入控件
│   └── ShardingDataControl.cs  # 分表数据管理控件
└── IoC/
    └── ServiceProvider.cs      # 依赖注入容器
```

## 核心功能

### 1. SQL Server ↔ MySQL 同步

**测试场景**：
- SQL Server (主库) → MySQL (从库) 全量同步
- MySQL → SQL Server 增量同步
- 字段映射配置
- 主键配置

**配置项**：
```json
{
  "TaskId": "ss_to_mysql",
  "SourceProvider": "SqlServer",
  "SourceConnStr": "server=localhost;database=FastDataDemo;uid=sa;pwd=FastData@Test123",
  "TargetProvider": "MySql",
  "TargetConnStr": "server=127.0.0.1;database=FastDataDemo;uid=root;pwd=FastData@Test123",
  "SourceTable": "AppUser",
  "TargetTable": "AppUser",
  "SyncColumns": "Id,UserName,Email,Phone,Age,Department,Salary,IsActive,CreateTime,UpdateTime",
  "KeyColumns": "Id",
  "BatchSize": 1000,
  "EnableRetry": true,
  "MaxRetryCount": 3
}
```

### 2. SQL Server ↔ PostgreSQL 同步

**测试场景**：
- SQL Server → PostgreSQL 全量同步
- 数据类型转换（datetime → timestamp, bit → boolean）
- 自增 ID 处理

### 3. SQL Server ↔ SQLite 同步

**测试场景**：
- SQL Server → SQLite 文件数据库同步
- 大批量数据分页处理

### 4. 分表同步

**支持的同步策略**：
- **按时间分表**：按年/月/日自动创建表
- **按 hash 分表**：按主键 hash 分布数据
- **按列表分表**：按字段值列表分布
- **按频率分表**：按访问热度分表

### 5. 高级功能

| 功能 | 描述 | 测试方法 |
|------|------|---------|
| 增量同步 | 基于时间戳/变更日志 | 修改源数据 → 增量同步 → 验证目标 |
| 失败重试 | 自动重试失败的记录 | 模拟网络中断 → 查看重试日志 |
| 断点续传 | 从上次失败位置继续 | 中断同步 → 重新启动 → 验证进度 |
| 字段映射 | 源目标字段名不一致 | 配置映射 → 同步 → 验证数据 |
| 数据清洗 | 同步前转换数据 | 配置转换规则 → 同步 → 验证格式 |
| 并发控制 | 多线程并行同步 | 调整线程数 → 测性能 |
| 事务支持 | 批量写入事务 | 模拟错误 → 验证回滚 |

## 编译说明

### Windows 环境
```powershell
cd FastData.SyncTool.WinForms
dotnet build -c Release
```

### Linux 环境（交叉编译）
```bash
dotnet build -c Release -r win-x64
```

**输出**：
```
FastData.SyncTool.WinForms/bin/Release/net10.0-windows/FastData.SyncTool.WinForms.exe
```

## 测试用例

### 测试 1：全量同步（39K 用户）

**步骤**：
1. 打开 SyncTool.exe
2. 选择"同步配置"标签页
3. 添加源连接（SQL Server）
4. 添加目标连接（MySQL/PostgreSQL）
5. 选择表：AppUser
6. 配置字段映射（自动）
7. 配置主键：Id
8. 点击"开始同步"
9. 观察进度条和日志
10. 验证目标库数据

**预期结果**：
- ✓ 39,465 条记录全部同步
- ✓ 无失败记录
- ✓ 数据一致性验证通过

### 测试 2：增量同步

**步骤**：
1. 完成全量同步
2. 在 SQL Server 新增 100 条记录
3. 修改 50 条现有记录
4. 删除 10 条记录
5. 执行增量同步
6. 验证 MySQL/PostgreSQL

**预期结果**：
- ✓ 新增 100 条
- ✓ 更新 50 条
- ✓ 删除 10 条（如配置）

### 测试 3：字段映射测试

**测试字段**：
| 源字段 (SQL Server) | 目标字段 (MySQL) | 类型 |
|---------------------|------------------|------|
| IsActive (bit) | IsActive (tinyint) | bit → bool |
| CreateTime (datetime) | CreateTime (datetime) | datetime → datetime |
| Salary (decimal) | Salary (decimal(18,2)) | decimal → decimal |

**预期结果**：
- ✓ 数据类型正确转换
- ✓ 值保持一致

### 测试 4：失败重试

**步骤**：
1. 开始同步
2. 模拟网络中断（关闭目标数据库）
3. 观察失败日志
4. 恢复数据库
5. 点击"重试失败记录"
6. 验证同步成功

**预期结果**：
- ✓ 失败记录正确记录
- ✓ 重试后成功同步

### 测试 5：批量性能测试

**批量大小配置**：
```
BatchSize = 100, 500, 1000, 5000
```

**性能指标**：
| BatchSize | 期望时间 | 实际时间 |
|-----------|---------|---------|
| 100       | ~60s    | TBD |
| 500       | ~20s    | TBD |
| 1000      | ~15s    | TBD |
| 5000      | ~12s    | TBD |

## 数据库连接配置

```xml
<!-- FastData.Demo/db.config 也适用于 SyncTool -->
<DataConfig Default="SqlServer">
  <Connections>
    <SqlServer>server=localhost;database=FastDataDemo;uid=sa;pwd=FastData@Test123;TrustServerCertificate=true</SqlServer>
    <MySql>server=127.0.0.1;database=FastDataDemo;uid=root;pwd=FastData@Test123</MySql>
    <PostgreSql>server=127.0.0.1;database=fastdatademo;uid=postgres;pwd=postgres</PostgreSql>
    <Sqlite>Data Source=fastdata_demo.db</Sqlite>
  </Connections>
</DataConfig>
```

## 日志文件位置

```
%BASE%/logs/
├── sync_2026-05-29.log      # 同步操作日志
├── sync_error_2026-05-29.log # 错误日志
└── retry_2026-05-29.log      # 重试日志
```

## 常见问题

### Q1: 同步时提示"连接失败"
**A**: 检查数据库容器是否运行，连接字符串是否正确。

### Q2: SQL Server → MySQL 同步失败
**A**: 可能是数据类型不兼容，检查字段映射配置。

### Q3: 同步速度慢
**A**: 增加 `BatchSize`（1000→5000），启用多线程。

### Q4: .net45 FrameworkPathOverride
**A**: SyncTool 基于 net10.0-windows，不需要 net45 特殊配置。

---

*文档更新时间：2026-05-29*
*FastData 版本：1.0.0*
*SyncTool 版本：1.0.0*
