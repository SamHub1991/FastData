# FastData.ModelGenerator（代码生成工具）

多页面 WinForms 代码生成工具，支持从数据库、JSON 和 API 端点生成代码。

## 目标框架

| 框架 | 说明 |
|------|------|
| net6.0-windows | .NET 6 Windows Desktop |
| net8.0-windows | .NET 8 Windows Desktop |
| net10.0-windows | .NET 10 Windows Desktop |

---

## 功能页面

| Tab | 功能 | 说明 |
|-----|------|------|
| **连接管理** | 保存/测试/删除数据库连接 | 连接配置持久化为 db_connections.json |
| **Model 生成** | 从数据库表生成 C# Model | 批量选择、命名空间、实时预览 |
| **XML Map 生成** | 生成 XML SQL 配置文件 | 支持 CRUD 操作和动态条件 |
| **代码生成** | 全功能分层代码生成 | Repository/Service/Controller/Demo |
| **JSON 转 Model** | JSON 自动转换为 C# 类 | 类型推断、嵌套对象、数组 |
| **API 代码生成** | RestSharp 客户端代码生成 | 6 种认证方式、响应 Model |

---

## 快速开始

### 1. 加载数据库表

1. 在 Tab 1「连接管理」添加数据库连接
2. 在 Tab 2「Model 生成」选择 Provider 和输入连接字符串
3. 点击「加载表」获取表列表
4. 使用搜索框过滤表名

### 2. 生成 Model

```bash
选择表 → 设置命名空间 → 点击「生成」
```

生成的 Model：

```csharp
[Table("Users")]
public class User
{
    [Column("Id"), Primary]
    public long Id { get; set; }

    [Column("UserName")]
    public string UserName { get; set; }

    [Column("Email")]
    public string Email { get; set; }
}
```

### 3. 生成分层代码（Tab 4）

勾选需要的文件类型和功能选项：

| 文件选项 | 功能选项 |
|---------|---------|
| Model / XML Map / Repository / Service | 缓存 / 消息队列 / 分页 / 事务 |
| Controller / Demo / 接口 / README | 分表 / 数据同步 / 原生 SQL / Map SQL |

### 4. JSON 转 Model（Tab 5）

粘贴 JSON 或加载 JSON 文件，自动生成 C# 类：

```
输入：{"id": 1, "name": "张三", "age": 25, "tags": ["admin"]}
输出：C# 类含 Id(long), Name(string), Age(long), Tags(List<string>)
```

### 5. API 代码生成（Tab 6）

输入 API 信息生成 RestSharp 客户端：

```
Base URL: https://api.example.com
Endpoint: /api/v1/users
Method: POST
认证: Bearer

输出：完整的 RestClient 类 + 响应 Model
```

---

## Tab 4 代码生成选项

### 文件类型

| 选项 | 生成内容 |
|------|---------|
| Model | POCO 实体类 |
| XML Map | XML SQL 配置文件 |
| Repository | 数据访问层 |
| Service | 业务逻辑层（可集成缓存） |
| Controller | Web API 控制器 |
| Demo | 使用示例代码 |
| 接口 | Repository/Service 接口定义 |
| README | 模块说明文档 |

### 功能特性

| 选项 | 说明 |
|------|------|
| 缓存 | Service 层集成 Redis 缓存 |
| 消息队列 | 消息队列生产/消费代码 |
| 分页 | 分页查询方法 |
| 事务 | 事务支持代码 |
| 分表 | 分表查询支持 |
| 数据同步 | 跨库同步代码 |
| 原生 SQL | 原生 SQL 查询 |
| XML Map SQL | Map SQL 查询 |

### 生成的文件结构

```
Output/CodeGen/
├── Models/Users.cs
├── XmlMaps/Users.xml
├── Repositories/UsersRepository.cs
├── Services/UsersService.cs
├── Controllers/UsersController.cs
├── Demo/UsersDemo.cs
└── Docs/Users_README.md
```

---

## Tab 6 认证方式

| 认证类型 | 请求头格式 |
|---------|-----------|
| None | 无需认证 |
| Bearer | `Authorization: Bearer <token>` |
| JWT | `Authorization: Bearer <jwt>` |
| API Key (Header) | `X-API-Key: <key>` |
| Token (Header) | `Authorization: Token <token>` |
| Basic Auth | `Authorization: Basic <base64>` |

---

## 连接字符串示例

### SQL Server
```
server=localhost;database=MyDb;uid=sa;pwd=123456;
```

### MySQL
```
Server=localhost;Database=mydb;Uid=root;Pwd=123456;
```

### PostgreSQL
```
Host=localhost;Database=mydb;Username=postgres;Password=123456;
```

### SQLite
```
Data Source=C:\Data\mydb.db;Version=3;
```

---

## 类型映射

| 数据库类型 | CLR 类型 |
|-----------|---------|
| bigint / BIGINT | long |
| int / INT | int |
| bit / BOOLEAN | bool |
| datetime / DATETIME | DateTime |
| decimal / NUMERIC | decimal |
| float / REAL | double |
| nvarchar / VARCHAR | string |
| text / TEXT | string |

---

## 构建

```bash
dotnet build FastData.ModelGenerator.WinForms --framework net10.0-windows
```

## 依赖

- FastData（ORM 核心）
- FastData.Tooling（工具库）

## 许可证

MIT License
