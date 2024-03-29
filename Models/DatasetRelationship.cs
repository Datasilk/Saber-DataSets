﻿namespace Query.Models
{
    public class DatasetRelationship
    {
        public int parentId { get; set; }
        public int childId { get; set; }
        public string parentLabel { get; set; } = "";
        public string childLabel { get; set; } = "";
        public string parentTableName { get; set; } = "";
        public string childTableName { get; set; } = "";
        public string parentList { get; set; } = "";
        public string childColumn { get; set; } = "";
        public string childKey { get; set; } = "";
        public int listType { get; set; } = 1;
    }
}
