using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using FastData.Model;

namespace FastData.Base
{
    /// <summary>
    /// 条件操作符枚举
    /// <para>
    /// 借鉴 Dos.ORM 的设计思路：将所有可用的 WHERE 条件操作符集中定义为枚举，
    /// 配合 <see cref="Condition"/> 值对象与 <see cref="ConditionBuilder"/> 构建器，
    /// 实现"动态拼接"且"全部使用参数化查询"的安全 WHERE 子句构造能力。
    /// </para>
    /// <para>
    /// 新增条件类型时仅需扩展此枚举 + 在 <see cref="Condition.Render"/> 中注册
    /// 渲染逻辑即可，对外 API 与调用方代码无需改动（开闭原则）。
    /// </para>
    /// </summary>
    public enum ConditionOperator
    {
        /// <summary>等于（=）</summary>
        Equal,
        /// <summary>不等于（&lt;&gt;）</summary>
        NotEqual,
        /// <summary>大于（&gt;）</summary>
        GreaterThan,
        /// <summary>大于等于（&gt;=）</summary>
        GreaterThanOrEqual,
        /// <summary>小于（&lt;）</summary>
        LessThan,
        /// <summary>小于等于（&lt;=）</summary>
        LessThanOrEqual,
        /// <summary>LIKE 模糊匹配（值由调用方负责追加 %）</summary>
        Like,
        /// <summary>NOT LIKE 模糊不匹配</summary>
        NotLike,
        /// <summary>包含子串（LIKE '%value%'，自动加 %）</summary>
        Contains,
        /// <summary>开头匹配（LIKE 'value%'，自动加 %）</summary>
        StartsWith,
        /// <summary>结尾匹配（LIKE '%value'，自动加 %）</summary>
        EndsWith,
        /// <summary>IN 列表（值为 IEnumerable）</summary>
        In,
        /// <summary>NOT IN 列表</summary>
        NotIn,
        /// <summary>BETWEEN 区间（值需为二元组：start,end）</summary>
        Between,
        /// <summary>NOT BETWEEN 区间</summary>
        NotBetween,
        /// <summary>IS NULL（值忽略）</summary>
        IsNull,
        /// <summary>IS NOT NULL（值忽略）</summary>
        IsNotNull
    }

    /// <summary>
    /// 条件逻辑操作符
    /// </summary>
    public enum ConditionLogic
    {
        /// <summary>AND</summary>
        And,
        /// <summary>OR</summary>
        Or
    }
}
