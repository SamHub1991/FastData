
namespace FastData.Aop
{
    /// <summary>
    /// AOP 操作类型枚举
    /// 
    /// 用于标识当前拦截的 SQL 操作类型。
    /// </summary>
    public enum AopType
    {
        /// <summary>
        /// 添加单条记录
        /// </summary>
        Add = 1,

        /// <summary>
        /// 批量添加记录
        /// </summary>
        AddList = 2,

        /// <summary>
        /// Lambda 表达式更新
        /// </summary>
        Update_Lambda = 3,

        /// <summary>
        /// 主键更新
        /// </summary>
        Update_PrimaryKey = 4,

        /// <summary>
        /// 批量更新
        /// </summary>
        UpdateList = 5,

        /// <summary>
        /// Lambda 表达式删除
        /// </summary>
        Delete_Lambda = 6,

        /// <summary>
        /// 主键删除
        /// </summary>
        Delete_PrimaryKey = 7,

        /// <summary>
        /// 执行原生 SQL（返回布尔值）
        /// </summary>
        Execute_Sql_Bool = 8,

        /// <summary>
        /// 执行原生 SQL（返回实体）
        /// </summary>
        Execute_Sql_Model = 9,

        /// <summary>
        /// 执行原生 SQL（返回字典）
        /// </summary>
        Execute_Sql_Dic = 10,

        /// <summary>
        /// Map SQL 查询（返回实体列表）
        /// </summary>
        Map_List_Model = 11,

        /// <summary>
        /// Map SQL 查询（返回字典列表）
        /// </summary>
        Map_List_Dic = 12,

        /// <summary>
        /// Map SQL 分页查询（返回字典）
        /// </summary>
        Map_Page_Dic = 13,

        /// <summary>
        /// Map SQL 分页查询（返回实体）
        /// </summary>
        Map_Page_Model = 14,

        /// <summary>
        /// Map SQL 写入操作
        /// </summary>
        Map_Write = 15,

        /// <summary>
        /// Lambda 查询（返回实体列表）
        /// </summary>
        Query_List_Lambda = 16,

        /// <summary>
        /// Lambda 查询（返回字典列表）
        /// </summary>
        Query_Dic_Lambda = 17,

        /// <summary>
        /// Lambda 查询（返回 DataTable）
        /// </summary>
        Query_DataTable_Lambda = 18,

        /// <summary>
        /// Lambda 查询（返回 JSON）
        /// </summary>
        Query_Json_Lambda = 19,

        /// <summary>
        /// Lambda 查询（返回计数）
        /// </summary>
        Query_Count_Lambda = 20,

        /// <summary>
        /// Lambda 分页查询（返回字典）
        /// </summary>
        Query_Page_Lambda_Dic = 21,

        /// <summary>
        /// Lambda 分页查询（返回实体）
        /// </summary>
        Query_Page_Lambda_Model = 22,

        /// <summary>
        /// SQL 分页查询（返回字典）
        /// </summary>
        Query_Page_Sql_Dic = 23,

        /// <summary>
        /// SQL 分页查询（返回实体）
        /// </summary>
        Query_Page_Sql_Model = 24,

        /// <summary>
        /// DataContext 操作
        /// </summary>
        DataContext = 25,

        /// <summary>
        /// 解析 XML 配置
        /// </summary>
        ParsingXml = 26,

        /// <summary>
        /// CodeFirst 建表
        /// </summary>
        CodeFirst = 27
    }
}
