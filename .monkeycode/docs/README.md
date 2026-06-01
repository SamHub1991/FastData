# FastData 文档

本目录包含 FastData ORM 的所有文档。

## 快速开始

- [快速入门指南](./QUICK_START.md) - 5 分钟快速上手 FastData ORM
- [CHANGELOG](../../CHANGELOG.md) - 版本变更记录

## 功能文档

- [未来改进规划](./FUTURE_IMPROVEMENTS.md) - FastData ORM 的未来发展方向和改进计划

## 工具文档

- [DevTools 工具集](../../FastData/DevTools/README.md) - 22 个专业开发工具的详细文档

## 报告文档

- [最终完成报告 v2.0](./FINAL_COMPLETION_REPORT.md) - 项目最终完成报告 v1.4.0，包含所有改进和功能的完整记录

## 相关文档

- [项目主文档](../../README.md) - FastData ORM 项目主页
- [代码生成器文档](../../FastData.ModelGenerator.WinForms/README.md) - ModelCodeGenerator 使用手册
- [数据同步工具文档](../../FastData.SyncTool.WinForms/README.md) - DataSyncTool 使用手册

## 记忆文档

- [用户指令记忆](../MEMORY.md) - 用户行为指令和项目知识（运维/构建/排错/协作/环境）

## 项目结构

```
FastData/
├── FastData/                     # 核心 ORM
│   ├── DevTools/                 # 开发工具集
│   │   └── README.md              # DevTools 工具文档
│   ├── README.md                 # ORM 主文档
│   ├── CODE_STYLE.md             # 代码风格规范
│   └── MODERN_ORM_FEATURES.md    # 现代 ORM 特性（变更跟踪、迁移）
├── FastData.Untility/            # 工具库
│   └── README.md
├── FastData.Tests/               # 单元测试
│   └── README.md
├── FastData.Demo/                # Web API 示例
│   └── README.md
├── FastData.Example/             # 控制台示例
│   └── README.md
├── FastData.ModelGenerator.WinForms/  # 代码生成工具
│   └── README.md
├── FastData.SyncTool.WinForms/       # 数据同步工具
│   └── README.md
├── .monkeycode/                  # MonkeyCode 相关
│   ├── docs/                     # 所有文档（本目录）
│   │   ├── README.md             # 文档索引（本文件）
│   │   ├── QUICK_START.md        # 快速入门
│   │   ├── FUTURE_IMPROVEMENTS.md # 未来规划
│   │   └── FINAL_COMPLETION_REPORT.md # 最终完成报告
│   ├── specs/                    # 需求/设计/任务规格
│   │   └── tasklist.md           # 任务清单
│   └── MEMORY.md                 # 用户指令记忆
├── README.md                     # 项目主文档
└── CHANGELOG.md                  # 版本变更记录
```