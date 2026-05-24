namespace FastData.Repository
{
    /// <summary>
    /// FastData repository factory.
    /// </summary>
    public class FastRepositoryFactory : IFastRepositoryFactory
    {
        /// <summary>
        /// Creates a repository for the default database.
        /// </summary>
        public IFastRepository Default()
        {
            return new FastRepository();
        }

        /// <summary>
        /// Creates a repository bound to the specified database key.
        /// </summary>
        public IFastRepository Use(string key)
        {
            return new FastRepository().SetKey(key);
        }
    }
}
