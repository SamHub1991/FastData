# 项目需求2026年5月 - 实施任务清单

## 1. 架构优化

- [x] 梳理现有多数据库重复代码。
- [x] 抽取数据库适配器接口。
- [x] 抽取 SQL 方言接口。
- [x] 抽取元数据读取接口。
- [x] 将工具项目与 ORM 核心项目分离。

## 2. 多数据库配置简化

- [x] 新增统一 `Connections` 配置结构。
- [x] 保留旧配置格式兼容读取。
- [x] 实现默认数据库配置。
- [x] 实现 `FastRead.Use(key)` 和 `FastWrite.Use(key)`。
- [x] 实现 `FastDb.Use(key)` 作用域切换。
- [x] 实现 `IFastRepositoryFactory` 指定库 Repository。
- [x] 补充配置错误提示和可用 Key 提示。

## 3. Model 生成工具

- [x] 新建 `FastData.Tooling` 公共工具项目。
- [x] 新建 `FastData.ModelGenerator.WinForms` 项目。
- [x] 实现数据库连接测试。
- [x] 实现表加载、多选和代码预览。
- [x] 实现默认命名空间配置。
- [x] 实现 Model 代码预览、编辑和生成。
- [x] 验证生成工具项目可编译。

## 4. 数据同步工具

- [x] 新建 `FastData.SyncTool.WinForms` 项目。
- [x] 实现源库和目标库配置。
- [x] 设计中间库表结构。
- [x] 实现 SQL Server 中间库脚本生成。
- [x] 实现 MySQL 和 Oracle 中间库脚本生成。
- [x] 实现中间库 SQL 导出。
- [x] 实现基础全量同步。
- [x] 实现增量同步基础入口。
- [x] 实现同步重试和错误计数。
- [x] 实现任务恢复基础入口。
- [x] 实现中间库历史数据清理基础入口。
- [x] 实现任务状态和错误日志界面。

## 5. 中文文档

- [x] 编写快速开始文档。
- [x] 编写多数据库配置和优雅切换文档。
- [x] 编写 Model 生成工具文档。
- [x] 编写数据同步工具文档。
- [x] 编写 XML SQL Map、Repository、AOP 和 FAQ 文档。

## 6. 验证

- [ ] 验证原有 ORM API 兼容。
- [ ] 验证默认库和指定库切换写法。
- [x] 验证 Model 生成工具项目可编译。
- [x] 验证中间库 SQL 导出。
- [ ] 验证源库到目标库端到端同步。
- [ ] 验证失败重试、任务恢复和中间库清理。

## 7. 代码质量优化

### 7.1 异常修复（高优先级）

- [x] 修复 ModelGenerator UI row 冲突。
- [x] 修复 SyncTool BuildReplayTab row 越界（RowCount 12→13）。
- [x] 修复 FastRepository 90% 重复代码（使用 AsyncHelper 提取通用方法）。
- [x] 修复 Task.Run 反模式（集中到 AsyncHelper，添加注释说明）。
- [ ] 修复 BuildLayout() 过长方法（暂缓：需深度重构，与现有代码结构耦合度高）。

### 7.2 可测试性改进

- [ ] 为 SyncTool MainForm 引入依赖注入。
- [ ] 为 DataSyncService 添加接口抽象。
- [ ] 替换 DateTime.Now 为可测试抽象。

### 7.3 性能优化

- [ ] 实现 SqlBulkCopy 批量插入。
- [ ] 优化失败记录序列化（XML→JSON）。
- [ ] 优化大表主键加载（流式处理）。

### 7.4 代码可读性

- [ ] 拆分 MainForm 为 Tab UserControl。
- [ ] 提取数据库类型映射为 Dictionary。
- [ ] 规范命名（消除魔法字符串）。
