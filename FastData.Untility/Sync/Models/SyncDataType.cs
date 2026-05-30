namespace FastData.Tooling.Sync
{
    /// <summary>
    /// 数据类型
    /// </summary>
    public enum SyncDataType
    {
        /// <summary>
        /// 静态数据（无时间字段，按主键增量或全量）
        /// </summary>
        Static,

        /// <summary>
        /// 动态数据（有时间字段，按时间范围增量）
        /// </summary>
        Dynamic
    }
}
