namespace SQLiteDiff
{
    internal class TableData
    {
        public TableData(string tableName, string primaryKey, bool hasDifferences = false)
        {
            TableName = tableName;
            PrimaryKey = primaryKey;
            HasDifferences = hasDifferences;
        }

        public string TableName { get; set; }
        public string PrimaryKey { get; set; }
        public bool HasDifferences { get; set; }
    }
}
