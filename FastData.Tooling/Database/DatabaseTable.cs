namespace FastData.Tooling.Database
{
    public class DatabaseTable
    {
        public string Schema { get; set; }

        public string Name { get; set; }

        public string Comment { get; set; }

        public string FullName
        {
            get
            {
                return string.IsNullOrEmpty(Schema) ? Name : Schema + "." + Name;
            }
        }
    }
}
