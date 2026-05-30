namespace FastData.Tooling.Database
{
    public class DatabaseColumn
    {
        public string Name { get; set; }

        public string DbType { get; set; }

        public int Length { get; set; }

        public int Precision { get; set; }

        public int Scale { get; set; }

        public bool IsNullable { get; set; }

        public bool IsPrimaryKey { get; set; }

        public string Comment { get; set; }
    }
}
