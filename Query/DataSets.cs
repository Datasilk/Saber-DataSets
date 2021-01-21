using System.Collections.Generic;
using System.Linq;

namespace Query
{
    public static class DataSets
    {
        public static int Create(string label, string description, List<Models.DataSets.Column> columns)
        {
            var list = new Models.DataSets.Columns()
            {
                Items = columns.ToArray()
            };
            return Sql.ExecuteScalar<int>("DataSet_Create", new { label, description, columns = Common.Serializer.ToXmlDocument(list).OuterXml });
        }

        public static List<Models.DataSet> GetList()
        {
            return Sql.Populate<Models.DataSet>("DataSets_GetList");
        }

        public static List<dynamic> GetRecords(int datasetId)
        {
            return Sql.Populate<dynamic>("DataSet_GetRecords", new { datasetId });
        }
        
    }
}
