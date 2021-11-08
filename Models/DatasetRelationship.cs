namespace Query.Models
{
    public class DatasetRelationship
    {
        public string parentId { get; set; }
        public string childId { get; set; }
        public string parentLabel { get; set; }
        public string childLabel { get; set; }
        public string parentTableName { get; set; }
        public string childTableName { get; set; }
        public string parentList { get; set; }
        public string childColumn { get; set; }
    }
}
