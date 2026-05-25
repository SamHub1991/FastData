# FastData Model 生成工具使用指南

更新时间：2026-05-25

---

## 概述

FastData Model 生成工具是一个基于 WinForms 的可视化代码生成工具，支持从数据库表结构自动生成 C# Model 类代码。

### 核心特性

- **多数据库支持**：SQL Server、MySQL、Oracle
- **批量生成**：支持一次选择多张表批量生成
- **命名空间配置**：支持默认命名空间和单表覆盖
- **代码预览**：生成前可预览和编辑代码
- **智能类型映射**：数据库类型自动映射到 C# 类型
- **属性标注**：自动生成 FastData ORM 特性标注

---

## 快速开始

### 1. 启动工具

```bash
cd FastData.ModelGenerator.WinForms/bin/Debug
FastData.ModelGenerator.WinForms.exe
```

### 2. 配置数据库连接

| 字段 | 说明 | 示例 |
|------|------|------|
| 数据库类型 | 选择数据库类型 | `SQL Server` |
| 连接字符串 | 数据库连接信息 | `server=.;database=DemoDb;uid=sa;pwd=123` |
| 命名空间 | 生成代码的命名空间 | `FastData.Model` |

### 3. 测试连接

点击**测试连接**按钮验证连接信息是否正确。

### 4. 选择数据表

1. 在表列表中勾选要生成的表
2. 支持按住 `Ctrl` 键多选
3. 支持按住 `Shift` 键连选
4. 使用搜索框快速筛选表名

### 5. 预览代码

选中单个表后，在右侧预览区域查看生成的代码。

### 6. 编辑代码（可选）

双击预览区域可编辑生成的代码，支持：
- 修改类名
- 添加注释
- 调整属性顺序
- 添加自定义属性

### 7. 生成 Model 文件

点击**生成 Model**按钮，选择输出目录，代码文件将保存到指定位置。

---

## 界面说明

### 左侧面板

| 控件 | 功能 |
|------|------|
| 数据库类型下拉框 | 选择数据库类型 |
| 连接字符串输入框 | 填写数据库连接 |
| 测试连接按钮 | 验证连接是否可用 |
| 命名空间输入框 | 设置默认命名空间 |
| 搜索框 | 筛选表名（支持模糊匹配） |
| 加载表按钮 | 从数据库加载表列表 |
| 表列表 | 勾选要生成的表 |

### 右侧面板

| 控件 | 功能 |
|------|------|
| 代码预览区 | 显示当前选中表的生成代码 |
| 编辑按钮 | 打开代码编辑器 |
| 生成 Model 按钮 | 批量生成所有选中表的代码 |
| 清空按钮 | 清空预览区域 |

---

## 高级功能

### 单表命名空间覆盖

为特定表指定不同的命名空间：

1. 选中目标表
2. 在命名空间输入框填写新命名空间
3. 该表生成的代码将使用新命名空间
4. 其他表继续使用默认命名空间

**示例**：
- 默认命名空间：`FastData.Model`
- 表 `sys_users` 命名空间：`FastData.Model.System`
- 生成结果：
  - `Users.cs` → `namespace FastData.Model`
  - `SysUsers.cs` → `namespace FastData.Model.System`

### 代码编辑

双击预览区域打开代码编辑器，支持：

- 修改类名（会自动同步文件名）
- 添加 XML 注释
- 调整属性顺序
- 添加 [Obsolete]、[Description] 等特性
- 实现接口（如 `IAuditable`）

**注意**：编辑后的代码仅在本次生成有效，重新预览会恢复自动生成结果。

### 表搜索过滤

在搜索框输入关键词快速筛选表：

- 输入 `user`：筛选包含 "user" 的表
- 输入 `^sys_`：筛选以 "sys_" 开头的表（正则）
- 输入 `log$`：筛选以 "log" 结尾的表（正则）

---

## 代码生成规则

### 类名映射

| 数据库表名 | C# 类名 | 文件名 |
|-----------|---------|--------|
| `users` | `Users` | `Users.cs` |
| `sys_users` | `SysUsers` | `SysUsers.cs` |
| `order_details` | `OrderDetails` | `OrderDetails.cs` |
| `t_user_info` | `TUserInfo` | `TUserInfo.cs` |

规则：
1. 下划线分割的每个单词首字母大写
2. 移除下划线
3. 帕斯卡命名法（PascalCase）

### 属性名映射

| 数据库列名 | C# 属性名 |
|-----------|-----------|
| `user_id` | `UserId` |
| `create_time` | `CreateTime` |
| `is_enabled` | `IsEnabled` |
| `email` | `Email` |

规则：与类名映射规则相同。

### 类型映射

#### SQL Server

| SQL 类型 | C# 类型 | 可空 |
|---------|---------|------|
| `int`, `integer` | `int` | `int?` |
| `bigint` | `long` | `long?` |
| `smallint` | `short` | `short?` |
| `tinyint` | `byte` | `byte?` |
| `bit` | `bool` | `bool?` |
| `varchar`, `nvarchar`, `char`, `nchar` | `string` | - |
| `text`, `ntext` | `string` | - |
| `datetime`, `datetime2` | `DateTime` | `DateTime?` |
| `date` | `DateTime` | `DateTime?` |
| `time` | `TimeSpan` | `TimeSpan?` |
| `decimal`, `numeric` | `decimal` | `decimal?` |
| `float`, `real` | `double` | `double?` |
| `uniqueidentifier` | `Guid` | `Guid?` |
| `varbinary`, `image` | `byte[]` | - |

#### MySQL

| MySQL 类型 | C# 类型 | 可空 |
|-----------|---------|------|
| `int`, `integer` | `int` | `int?` |
| `bigint` | `long` | `long?` |
| `smallint` | `short` | `short?` |
| `tinyint(1)` | `bool` | `bool?` |
| `tinyint` | `sbyte` | `sbyte?` |
| `varchar`, `char`, `text` | `string` | - |
| `datetime`, `timestamp` | `DateTime` | `DateTime?` |
| `date` | `DateTime` | `DateTime?` |
| `time` | `TimeSpan` | `TimeSpan?` |
| `decimal` | `decimal` | `decimal?` |
| `double`, `float` | `double` | `double?` |
| `blob`, `binary` | `byte[]` | - |

#### Oracle

| Oracle 类型 | C# 类型 | 可空 |
|------------|---------|------|
| `NUMBER(p,0)` p≤9 | `int` | `int?` |
| `NUMBER(p,0)` p≤18 | `long` | `long?` |
| `NUMBER` | `decimal` | `decimal?` |
| `VARCHAR2`, `CHAR`, `CLOB` | `string` | - |
| `DATE` | `DateTime` | `DateTime?` |
| `TIMESTAMP` | `DateTime` | `DateTime?` |
| `BLOB`, `RAW`, `LONG RAW` | `byte[]` | - |
| `CHAR(1)` | `bool` | `bool?` |

### 特性标注

生成的代码自动包含 FastData ORM 特性：

```csharp
using System;
using FastData.Model;

namespace FastData.Model
{
    /// <summary>
    /// 用户表
    /// </summary>
    [Table("users")]
    public class Users
    {
        /// <summary>
        /// 用户 ID
        /// </summary>
        [Column("user_id")]
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Column("user_name")]
        public string UserName { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [Column("email")]
        public string Email { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("create_time")]
        public DateTime CreateTime { get; set; }
    }
}
```

#### 特性说明

| 特性 | 说明 |
|------|------|
| `[Table("表名")]` | 指定数据库表名 |
| `[Column("列名")]` | 指定数据库列名 |
| `[Key]` | 标记主键列 |
| `[Identity]` | 标记自增列（如果适用） |

---

## 输出文件结构

生成的 Model 文件直接保存到指定目录：

```
output/
├── Users.cs
├── Orders.cs
├── OrderDetails.cs
├── Products.cs
└── SysUsers.cs
```

每个文件包含：
- 使用声明（`using`）
- 命名空间声明
- 类定义（带 `[Table]` 特性）
- 属性定义（带 `[Column]` 特性）
- XML 注释

---

## 最佳实践

### 1. 命名空间规划

按业务模块划分命名空间：

```
FastData.Model              # 基础模型
FastData.Model.System       # 系统模块
FastData.Model.Business     # 业务模块
FastData.Model.Report       # 报表模块
```

### 2. 表前缀处理

有前缀的表建议使用命名空间：

- `sys_users` → `FastData.Model.System.SysUsers`
- `biz_orders` → `FastData.Model.Business.Orders`
- `rpt_sales` → `FastData.Model.Report.Sales`

### 3. 代码审查

生成后建议检查：
- 主键是否正确识别
- 自增列是否标记 `[Identity]`
- 可空类型是否正确
- 特殊类型映射（如 `bool`、`byte[]`）

### 4. 版本控制

将生成的 Model 文件纳入版本控制：

```bash
git add src/Models/*.cs
git commit -m "feat: add database models generated by FastData tool"
```

### 5. 批量生成顺序

1. 先加载所有表
2. 按模块分组选择
3. 为每组设置命名空间
4. 分批生成到不同目录

---

## 故障排查

### 连接失败

**现象**：点击"测试连接"报错

**排查步骤**：
1. 检查连接字符串格式
2. 确认数据库服务运行正常
3. 验证网络连通性
4. 检查防火墙设置
5. 确认数据库用户权限

### 表列表为空

**现象**：点击"加载表"后列表为空

**排查步骤**：
1. 确认连接成功
2. 检查当前用户是否有读表权限
3. 确认数据库中有用户表（非系统表）
4. 尝试搜索框筛选（可能表名特殊）

### 类型映射错误

**现象**：生成的 C# 类型不正确

**解决方法**：
1. 手动编辑代码修正类型
2. 报告问题以便改进类型映射规则
3. 对于自定义类型，在生成后手动调整

### 生成文件乱码

**现象**：生成的文件打开后乱码

**解决方法**：
1. 使用 UTF-8 编码打开文件
2. 工具默认使用 UTF-8 编码
3. 检查文本编辑器编码设置

---

## API 扩展

### 编程方式生成

Model 生成核心逻辑在 `FastData.Tooling.CodeGeneration` 命名空间：

```csharp
using FastData.Tooling.CodeGeneration;

var metadata = new DatabaseMetadata();
var columns = metadata.GetColumns("users", connectionString);

var generator = new ModelGenerator();
var code = generator.Generate(
    tableName: "users",
    className: "Users",
    namespaceName: "FastData.Model",
    columns: columns
);

File.WriteAllText("Users.cs", code);
```

---

## 支持的开发环境

### IDE 兼容性

- ✅ Visual Studio 2015+
- ✅ Visual Studio Code
- ✅ JetBrains Rider
- ✅ MonoDevelop

### 框架兼容性

- ✅ .NET Framework 4.5+
- ✅ .NET Core 2.0+
- ✅ .NET 5.0+
- ✅ .NET 6.0+

### 编辑器兼容性

生成的代码支持所有 C# 编辑器，代码风格符合：
- C# 命名规范
- FastData ORM 要求
- Visual Studio 默认格式化

---

## 示例输出

### 输入表结构

```sql
CREATE TABLE Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    user_name NVARCHAR(50) NOT NULL,
    email NVARCHAR(100),
    is_enabled BIT DEFAULT 1,
    create_time DATETIME DEFAULT GETDATE()
);
```

### 生成代码

```csharp
using System;
using FastData.Model;

namespace FastData.Model
{
    /// <summary>
    /// 用户表
    /// </summary>
    [Table("users")]
    public class Users
    {
        /// <summary>
        /// 用户 ID
        /// </summary>
        [Column("user_id")]
        [Key]
        [Identity]
        public int UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Column("user_name")]
        public string UserName { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [Column("email")]
        public string Email { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Column("is_enabled")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("create_time")]
        public DateTime CreateTime { get; set; }
    }
}
```

---

## 常见问题

### Q: 支持视图（View）生成吗？

A: 当前版本仅支持表（Table）生成。视图生成已在计划中，后续版本会支持。

### Q: 可以自定义类名映射规则吗？

A: 当前版本使用固定映射规则。自定义规则需要手动编辑代码。

### Q: 生成的代码可以直接使用吗？

A: 可以。生成的代码符合 FastData ORM 规范，无需修改即可用于查询和写入。

### Q: 支持部分字段生成吗？

A: 当前版本生成所有字段。如不需要某些字段，可在生成后手动删除。

### Q: 如何更新已生成的 Model？

A: 重新运行工具，选择相同的表，生成到相同的目录覆盖原文件。

---

## 相关文件

- `FastData.ModelGenerator.WinForms/` - 工具主项目
- `FastData.Tooling/CodeGeneration/` - 代码生成核心库
- `FastData/Model/` - ORM 特性定义

---

**最后更新**：2026-05-25
**版本**：v1.0
