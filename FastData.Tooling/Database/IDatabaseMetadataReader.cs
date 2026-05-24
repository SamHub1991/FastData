using System.Collections.Generic;

namespace FastData.Tooling.Database
{
    public interface IDatabaseMetadataReader
    {
        bool TestConnection();

        IList<DatabaseTable> GetTables();

        IList<DatabaseColumn> GetColumns(string tableName);
    }
}
