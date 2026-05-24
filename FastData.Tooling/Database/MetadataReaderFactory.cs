using System;

namespace FastData.Tooling.Database
{
    public static class MetadataReaderFactory
    {
        public static IDatabaseMetadataReader Create(DatabaseConnectionOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            if (string.IsNullOrEmpty(options.Provider))
                throw new ArgumentException("Provider不能为空", "options");

            return new ProviderMetadataReader(options);
        }
    }
}
