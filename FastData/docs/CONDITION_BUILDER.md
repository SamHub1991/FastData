# 条件动态拼接（Condition / ConditionBuilder）

## 概述

`FastData.Base.Condition` 与 `FastData.Base.ConditionBuilder` 共同组成了 ORM 中"动态 WHERE 条件"的核心组件。
它们的设计参考了 Dos.ORM 的 `WhereClip` / `WhereClipBuilder` 思路：通过 **对象化** 的方式构建 WHERE 条件，
将"字段 + 操作符 + 值"封装成不可变片段，再由构建器统一渲染为带参数的 SQL 子句。

**与旧版字符串拼接相比，本机制的核心收益**：

| 维度 | 旧实现 | 新机制 |
|------|--------|--------|
| SQL 注入防护 | 依赖调用方手工拼参数 | **所有值强制走 `DbParameter`**，无注入入口 |
| 代码冗余 | `FastRead` / `DataQuery<T>` / `Where<T>` 各自实现 | **统一收敛到 `Condition.Render`** |
| 扩展性 | 每加一种条件要改 3~5 处 | **新增 `ConditionOperator` 枚举值 + 一处 switch 分支** |
| 维护性 | 字符串拼接散落各处，难审计 | **集中渲染 + 集中参数化** |
| 测试性 | 难以纯函数单测 | **`ConditionBuilder.Build` 是无副作用的纯函数** |

---

## 核心类型

| 类型 | 命名空间 | 职责 |
|------|----------|------|
| [`ConditionOperator`](../../FastData/Base/Condition/ConditionOperator.cs) | `FastData.Base` | 枚举所有支持的条件操作符（17 种） |
| [`ConditionLogic`](../../FastData/Base/Condition/ConditionOperator.cs) | `FastData.Base` | 条件间逻辑连接符（And / Or） |
| [`Condition`](../../FastData/Base/Condition/Condition.cs) | `FastData.Base` | 不可变条件片段（字段 + 操作符 + 值 + 逻辑） |
| [`ConditionBuilder`](../../FastData/Base/Condition/ConditionBuilder.cs) | `FastData.Base` | 链式条件构建器（Fluent API） |
| [`ConditionExpression`](../../FastData/Base/Condition/ConditionExpression.cs) | `FastData.Base` | `Expression<Func<T, object>>` → 字段名 工具 |

---

## 支持的条件操作符

| 枚举值 | 渲染结果 | 适用值类型 | 备注 |
|--------|----------|------------|------|
| `Equal` | `field = @pN` | 任意标量 | 等于 |
| `NotEqual` | `field <> @pN` | 任意标量 | 不等于 |
| `GreaterThan` | `field > @pN` | 可比较类型 | 大于 |
| `GreaterThanOrEqual` | `field >= @pN` | 可比较类型 | 大于等于 |
| `LessThan` | `field < @pN` | 可比较类型 | 小于 |
| `LessThanOrEqual` | `field <= @pN` | 可比较类型 | 小于等于 |
| `Like` | `field LIKE @pN` | string | 模糊匹配，**值需自行包含 %** |
| `NotLike` | `field NOT LIKE @pN` | string | 模糊不匹配 |
| `Contains` | `field LIKE @pN` | string | **自动在两端追加 %** |
| `StartsWith` | `field LIKE @pN` | string | **自动在尾部追加 %** |
| `EndsWith` | `field LIKE @pN` | string | **自动在头部追加 %** |
| `In` | `field IN (@p0,@p1,...)` | `IEnumerable` | 空集合 → `1=0` |
| `NotIn` | `field NOT IN (@p0,...)` | `IEnumerable` | 空集合 → `1=1` |
| `Between` | `field BETWEEN @p0 AND @p1` | 含 2 元素集合 | 闭区间 |
| `NotBetween` | `field NOT BETWEEN @p0 AND @p1` | 含 2 元素集合 | |
| `IsNull` | `field IS NULL` | — | 值忽略 |
| `IsNotNull` | `field IS NOT NULL` | — | 值忽略 |

---

## 快速上手

### 1. 独立使用 `ConditionBuilder`

```csharp
using FastData.Base;
using FastData.Config;

// 取配置（详见 README）
var config = FastDataConfig.GetConfig("DefaultDb");

// 链式构建：Age=18 AND Name 含"张" OR Status IN (1,2,3) AND CreateTime 在区间内
var builder = new ConditionBuilder(config)
    .Equal<User>(u => u.Age, 18)
    .And()
    .Contains<User>(u => u.Name, "张")
    .Or()
    .In<User>(u => u.Status, new object[] { 1, 2, 3 })
    .And()
    .Between<User>(u => u.CreateTime, DateTime.Today.AddDays(-7), DateTime.Today);

// 渲染为 SQL 子句 + 参数列表
var whereClause = builder.Build(out var parameters);
// whereClause ≈ "Age = @p0 AND Name LIKE @p1 OR Status IN (@p2,@p3,@p4) AND CreateTime BETWEEN @p5 AND @p6"
// parameters : 7 个 DbParameter，按出现顺序填入
```

### 2. 与 `DataQuery` 链式 API 集成

`ConditionBuilder` 既可独立使用，也可将构造好的条件 **附加** 到 `DataQuery.ChainedConditions`，
与 `Query<User>().Where(...).Or(...).Like(...)` 等现有 API 协同工作：

```csharp
var sub = new ConditionBuilder(config)
    .GreaterThan<User>(u => u.Age, 18)
    .Or()
    .IsNotNull<User>(u => u.Email);

var query = FastRead.Query<User>(config, u => u.IsActive)
                    .Where<User>(w => w.Dept, "IT")   // 来自 DataQuery 链式 API
                    .And();                            // 显式 AND 连接

// 把 builder 条件挂到 query 上
sub.AppendTo(query.ChainedConditions, ConditionLogic.And);
var users = query.ToList();
```

### 3. 直接构造 `Condition` 对象

当条件来源是数据（配置/规则）而非代码时，可直接 new：

```csharp
var c1 = new Condition("Age", ConditionOperator.GreaterThanOrEqual, 18);
var c2 = new Condition("Status", ConditionOperator.In, new object[] { 1, 2, 3 });
var builder = new ConditionBuilder(config);
builder.Build(out var ps);  // 空
```

> 提示：独立 `Condition` 不参与拼接，请通过 `ConditionBuilder.Add(...)` 或将 `Condition`
> 加入 `ChainedCondition.Conditions` 列表来使用。

---

## 与现有 `DataQuery` 链式 API 的对应关系

| 旧 API（`FastRead` / `DataQuery<T>`） | 新机制对应 | 说明 |
|--------------------------------------|-----------|------|
| `Where<T>(f, v)` | `Equal<T>(f, v)` | 内部已切换为 `Condition(Equal)` |
| `Like<T>(f, v)` | `Like<T>(f, v)` / `Contains<T>(f, v)` | 二者行为已统一参数化 |
| `In<T>(f, list)` | `In<T>(f, list)` | 空集合语义对齐：`IN` → 永远不匹配 |
| `Between<T>(f, a, b)` | `Between<T>(f, a, b)` | 起止值都走参数化 |
| `IsNull<T>(f)` / `IsNotNull<T>(f)` | `IsNull<T>(f)` / `IsNotNull<T>(f)` | 直接渲染关键字 |
| 字符串拼接自定义条件 | 见下方「新增条件类型」 | 旧字段 `Where` + `Param` 仍保留作兜底 |

---

## SQL 注入防护机制

`ConditionBuilder` 之所以能彻底防 SQL 注入，关键在于 `Condition.Render` 内部
**对每一个值都使用 `DbProviderFactory.CreateParameter()` 创建参数对象**，
最终通过 ADO.NET 完成绑定，原始值不会进入 SQL 文本。

```csharp
var malicious = "'; DROP TABLE Users; --";
var builder = new ConditionBuilder(config).Contains<User>(u => u.Name, malicious);

var sql = builder.Build(out var parameters);
// sql  : "Name LIKE @p0"                ← 仅有占位符，无恶意语句
// ps[0]: { Name="@p0", Value="%'; DROP TABLE Users; --%" }  ← 整段作为值
```

防御策略一览：

1. **零字符串拼接**：所有值都通过 `CreateParameter` 走参数通道。
2. **白名单操作符**：`switch` 之外的枚举值会抛 `NotSupportedException`。
3. **空集合语义明确**：`IN` 空集合 → `1=0`（永远不匹配），`NOT IN` 空集合 → `1=1`（永远匹配），
   避免生成 `field IN ()` 这种非法 SQL。
4. **BETWEEN 元数校验**：值必须能枚举出至少 2 个元素，否则抛 `ArgumentException`。
5. **字段名白名单**：字段名来源于 `Expression<Func<T, object>>` 编译期绑定，**不接受外部字符串注入**。

---

## 扩展新条件类型

新增条件类型只需 **3 步**，对调用方代码 **零侵入**（开闭原则）：

### 第 1 步：扩展枚举

```csharp
// Base/Condition/ConditionOperator.cs
public enum ConditionOperator
{
    // ... 现有 17 项
    RegexMatch,   // ← 新增：正则匹配
}
```

### 第 2 步：在 `Condition.Render` 中注册渲染分支

```csharp
// Base/Condition/Condition.cs Render 方法内
case ConditionOperator.RegexMatch:
    AppendScalar(sb, quotedField, "REGEXP", Value, flag, parameters, ref paramIndex, factory);
    break;
```

### 第 3 步（可选）：在 `ConditionBuilder` 暴露 Fluent 方法

```csharp
// Base/Condition/ConditionBuilder.cs
public ConditionBuilder RegexMatch<T>(Expression<Func<T, object>> field, string pattern)
    => Add(ConditionOperator.RegexMatch, field, pattern);
```

完成后 `ConditionBuilderTests` 中追加对应测试即可，**所有现有调用方代码无需改动**。

---

## 注意事项

### 1. 字段名大小写

`Condition.Render` 当前对字段名不做引号包装（`QuoteIdentifier` 直接返回原值），
因此 **字段名大小写需要与数据库实际列名一致**。如未来需要支持带特殊字符的列名，
请扩展 `QuoteIdentifier`。

### 2. 表达式类型必须是 `Func<T, object>`

`ConditionExpression.GetMemberName` 接受 `Expression<Func<T, object>>`，
目的是兼容 `u => u.Id`（值类型会被装箱）和 `u => u.Name`（引用类型直接传）。
**不支持** `u => u.Id + 1` 这类算术表达式，会抛 `ArgumentException`。

### 3. `IN` / `BETWEEN` 接受任意 `IEnumerable`

- `string` 也会实现 `IEnumerable<char>`，因此渲染时 **显式排除 string**，
  传入 string 会抛 `ArgumentException`。
- `BETWEEN` 取枚举的前 2 个元素，剩余元素被忽略。推荐显式传 `new[] { start, end }`
  或 `(start, end)` 这种二元 ValueTuple（已用 `IEnumerable` 兼容方式处理）。

### 4. 空集合语义

| 操作符 | 传入空集合 | 渲染结果 | 语义 |
|--------|-----------|----------|------|
| `In` | `Array.Empty<int>()` | `1=0` | 永远不匹配 |
| `NotIn` | `Array.Empty<int>()` | `1=1` | 永远匹配 |
| `Between` | 元素 < 2 | **抛异常** | 主动失败，不静默生成错误 SQL |

### 5. `AppendTo` 复用建议

`ConditionBuilder.AppendTo(target, outerLogic)` 会把内部条件 **拷贝** 到 `target`，
之后修改 `builder` 不会影响已附加的部分。建议在 **builder 配置完毕后再附加**，
避免链式顺序错误。

### 6. 与旧版 `Where` 字符串的兼容

`ChainedCondition` 同时保留 `Where` + `Param`（旧）和 `Conditions`（新）。
`WhereBuilder` 渲染时 **优先使用新机制**，仅当 `Conditions` 为空才回退到旧字符串。
**新代码统一使用新机制**；老代码无需任何改动即可继续工作。

### 7. 线程安全

- `Condition` 是 **不可变** 的，可在多线程间共享。
- `ConditionBuilder` 内部维护 `_conditions` 列表，**非线程安全**；每个线程请构造独立实例。

### 8. 参数名冲突

`paramIndex` 由 `WhereBuilder` / `ConditionBuilder` 统一从 0 开始计数，
保证同一查询内不同 `Condition` 生成的占位符（`@p0`、`@p1`...）互不冲突。
**不要** 在自己的 `Param` 列表中混入 `@pN` 形式的参数名。

---

## 单元测试

完整测试覆盖位于 [`FastData.Tests/ConditionBuilderTests.cs`](../../FastData.Tests/ConditionBuilderTests.cs)，
共 50+ 用例，覆盖：

- 17 种 `ConditionOperator` 的渲染结果
- AND / OR 混合拼接
- 空集合边界（`In` / `NotIn` / `Between`）
- 多条件组合下的占位符命名
- **典型 SQL 注入载荷**的拦截（`'`, `'; DROP TABLE`, `UNION SELECT` 等）
- `DataQuery` 链式 API 与 `ConditionBuilder` 的集成
- `WhereBuilder` 渲染（含新旧机制回退）

运行测试：

```bash
dotnet test FastData.Tests --filter "FullyQualifiedName~ConditionBuilder"
```

---

## 迁移指南（从旧字符串拼接 → 新机制）

### Before（旧）

```csharp
// 直接在调用方拼接：极易出错，且无注入防护
var sql = "Name LIKE '%" + userInput + "%' AND Age > " + minAge;
```

### After（新）

```csharp
var sql = new ConditionBuilder(config)
    .Contains<User>(u => u.Name, userInput)   // 自动 % 包裹 + 参数化
    .And()
    .GreaterThan<User>(u => u.Age, minAge)    // 标量比较 + 参数化
    .Build(out var parameters);
```

> 永远不要再手动拼接带用户输入的 SQL 字符串。**所有用户输入值** 都必须走 `Condition` /
> `ConditionBuilder` 体系或显式 `DbParameter` 绑定。
