﻿using System.Collections.Generic;
using System.Linq;

namespace Query
{
    public static class DataSets
    {
        public static int Create(string label, string partialview, string description, List<Models.DataSets.Column> columns, int? userId = null)
        {
            var list = new Models.DataSets.Columns()
            {
                Items = columns.ToArray()
            };
            return Sql.ExecuteScalar<int>("DataSet_Create", new { userId, label, partialview, description, columns = Common.Serializer.ToXmlDocument(list).OuterXml });
        }

        public static void UpdateColumns(int datasetId, List<Models.DataSets.Column> columns)
        {
            var list = new Models.DataSets.Columns()
            {
                Items = columns.ToArray()
            };
            Sql.ExecuteNonQuery("DataSet_UpdateColumns", new { datasetId, columns = Common.Serializer.ToXmlDocument(list).OuterXml });
        }

        public static List<Models.DataSet> GetList(int? userId = null, bool all = false, string search = "")
        {
            return Sql.Populate<Models.DataSet>("DataSets_GetList", new { userId, all, search });
        }

        public enum SearchType
        {
            any = 0,
            startsWith = 1,
            endsWith = 2,
            exactMatch = -1
        }

        public static List<IDictionary<string, object>> GetRecords(int datasetId, int start = 1, int length = 50, string lang = "", string search = "", SearchType searchType = SearchType.any, string orderby = "", int userId = 0, int recordId = 0)
        {
            var list = Sql.Populate<dynamic>("DataSet_GetRecords", new { datasetId, userId, start, length, lang, search, searchtype = (int)searchType, recordId, orderby });
            var results = new List<IDictionary<string, object>>();
            foreach(var item in list)
            {
                var row = item as IDictionary<string, object>;
                results.Add(row);
            }
            return results;
        }

        public static void AddRecord(int userId, int datasetId, string lang, List<Models.DataSets.Field> fields, int recordId = 0)
        {
            var list = new Models.DataSets.Fields()
            {
                Items = fields.ToArray()
            };
            Sql.ExecuteNonQuery("DataSet_AddRecord", new { userId, datasetId, recordId, lang, fields = Common.Serializer.ToXmlDocument(list).OuterXml });
        }

        public static void UpdateRecord(int datasetId, int recordId, string lang, List<Models.DataSets.Field> fields)
        {
            var list = new Models.DataSets.Fields()
            {
                Items = fields.ToArray()
            };
            Sql.ExecuteNonQuery("DataSet_UpdateRecord", new { datasetId, recordId, lang, fields = Common.Serializer.ToXmlDocument(list).OuterXml });
        }

        public static Models.DataSet GetInfo(int datasetId)
        {
            var list = Sql.Populate<Models.DataSet>("DataSet_GetInfo", new { datasetId });
            if(list.Count == 1)
            {
                return list.First();
            }
            return null;
        }

        public static List<Models.DataSets.Column> GetColumns(int datasetId)
        {
            return Sql.Populate<Models.DataSets.Column>("DataSet_GetColumns", new { datasetId });
        }

        public static void UpdateInfo(int datasetId, int? userId, string label, string description)
        {
            Sql.ExecuteNonQuery("DataSet_UpdateInfo", new { datasetId, userId, label, description });
        }

        public static void Delete(int datasetId)
        {
            Sql.ExecuteNonQuery("DataSet_Delete", new { datasetId });
        }
        
    }
}
