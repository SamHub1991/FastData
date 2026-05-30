namespace FastData.Repository
{
    /// <summary>
    /// Creates repositories for default or named database connections.
    /// </summary>
    public interface IFastRepositoryFactory
    {
        /// <summary>
        /// Creates a repository for the default database.
        /// </summary>
        IFastRepository Default();

        /// <summary>
        /// Creates a repository bound to the specified database key.
        /// </summary>
        /// <param name="key">数据库 key</param>
        /// <returns>仓储实例</returns>
        IFastRepository Use(string key);
    }
}
