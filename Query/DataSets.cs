using System.Collections.Generic;
using System.Linq;

namespace Query
{
    public static class DataSets
    {
        public static int Create(string label, string partialview, string description, List<Models.DataSets.Column> columns)
        {
            var list = new Models.DataSets.Columns()
            {
                Items = columns.ToArray()
            };
            return Sql.ExecuteScalar<int>("DataSet_Create", new { label, partialview, description, columns = Common.Serializer.ToXmlDocument(list).OuterXml });
        }

        public static List<Models.DataSet> GetList(string search = "")
        {
            return Sql.Populate<Models.DataSet>("DataSets_GetList", new { search });
        }

        public enum SearchType
        {
            any = 0,
            startsWith = 1,
            endsWith = 2,
            exactMatch = -1
        }

        public static List<IDictionary<string, object>> GetRecords(int datasetId, int start = 1, int length = 50, string lang = "", string search = "", SearchType searchType = SearchType.any, string orderby = "", int recordId = 0)
        {
            var list = Sql.Populate<dynamic>("DataSet_GetRecords", new { datasetId, start, length, lang, search, searchtype = (int)searchType, recordId, orderby });
            var results = new List<IDictionary<string, object>>();
            foreach(var item in list)
            {
                var row = item as IDictionary<string, object>;
                results.Add(row);
            }
            return results;
        }

        public static void AddRecord(int datasetId, string lang, List<Models.DataSets.Field> fields, int recordId = 0)
        {
            var list = new Models.DataSets.Fields()
            {
                Items = fields.ToArray()
            };
            Sql.ExecuteNonQuery("DataSet_AddRecord", new { datasetId, recordId, lang, fields = Common.Serializer.ToXmlDocument(list).OuterXml });
        }

        public static Models.DataSet GetInfo(int datasetId, bool columns = false)
        {
            var list = Sql.Populate<Models.DataSet>("DataSet_GetInfo", new { datasetId });
            if(list.Count == 1)
            {
                var dataset = list.First();
                if (columns)
                {
                    //get columns for dataset

                }
                return dataset;
            }
            return null;
        }
        
    }
}
