namespace FastData.Repository
{
    /// <summary>
    /// FastData 统一仓储接口（组合接口）
    /// 建议新代码使用 IReadRepository / IWriteRepository / IMapRepository 分层接口
    /// </summary>
    public interface IFastRepository : IReadRepository, IWriteRepository, IMapRepository
    {
    }
}
