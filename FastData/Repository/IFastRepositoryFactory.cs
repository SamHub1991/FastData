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
        IFastRepository Use(string key);
    }
}
