using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;

namespace Query
{
    public static class DataSets
    {
        public static int Create(string label, string partialview, string description, List<Models.DataSets.Column> columns, int? userId = null)
        {
            Saber.Vendors.DataSets.Cache.DataSets = null;
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

        public static List<Models.DataSet> GetList(int? userId = null, bool all = false, bool noadmin = false, string search = "")
        {
            return Sql.Populate<Models.DataSet>("DataSets_GetList", new { userId, all, noadmin, search });
        }

        public enum SearchType
        {
            any = 0,
            startsWith = 1,
            endsWith = 2,
            exactMatch = -1
        }

        public static List<IDictionary<string, object>> GetRecords(int datasetId, int start = 1, int length = 50, string lang = "", int userId = 0, List<Saber.Vendor.DataSource.FilterGroup> filters = null, List<Saber.Vendor.DataSource.OrderBy> sort = null)
        {
            var datasource = Saber.Vendors.DataSets.Cache.DataSources.Where(a => a.Key == datasetId.ToString()).FirstOrDefault();
            if(datasource == null) { return null; }
            var dataset = Saber.Vendors.DataSets.Cache.DataSets.Where(a => a.datasetId == datasetId).FirstOrDefault();
            if(dataset == null) { return null; }

            var sql = new StringBuilder(@"
                SELECT u.name AS username, u.email AS useremail, d.*
                FROM DataSet_" + dataset.tableName + @" d
		        LEFT JOIN Users u ON u.userId=d.userId
                WHERE " + (userId > 0 ? "d.userId=" + userId + " AND" : "") + " d.lang='" + lang + "' \n");

            if(filters != null && filters.Count > 0)
            {
                for(var x = 0; x < filters.Count; x++)
                {
                    //generate root filter group sql
                    var group = filters[x];
                    sql.Append(" AND " + GetFilterGroupSql(group, datasource.Columns));
                }
            }

            var conn = new SqlConnection(Sql.ConnectionString);
            var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
            var reader = server.ConnectionContext.ExecuteReader(sql.ToString());
            var results = new List<IDictionary<string, object>>();
            var columns = reader.GetColumnSchema();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                foreach(var column in columns)
                {
                    row.Add(column.ColumnName, reader[column.ColumnName]);
                }
                results.Add(row);
            }
            return results;
        }

        private static string GetFilterGroupSql(Saber.Vendor.DataSource.FilterGroup group, Saber.Vendor.DataSource.Column[] columns)
        {
            var sql = new StringBuilder("( \n");
            if(group.Elements.Count > 0)
            {
                for (var x = 0; x < group.Elements.Count; x++)
                {
                    var element = group.Elements[x];
                    var column = columns.Where(a => a.Name == element.Column).FirstOrDefault();
                    if(column == null) { continue; }
                    switch (column.DataType)
                    {
                        case Saber.Vendor.DataSource.DataType.Text:
                            switch (element.Match)
                            {
                                case Saber.Vendor.DataSource.FilterMatchType.StartsWith:
                                    sql.Append(column.Name + " LIKE '" + element.Value.Replace("'", "''") + "%'\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.EndsWith:
                                    sql.Append(column.Name + " LIKE '%" + element.Value.Replace("'", "''") + "'\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.Contains:
                                    sql.Append(column.Name + " LIKE '%" + element.Value.Replace("'", "''") + "%'\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.Equals:
                                    sql.Append(column.Name + " = '" + element.Value.Replace("'", "''") + "'\n");
                                    break;
                            }
                            break;
                        case Saber.Vendor.DataSource.DataType.Number:
                            switch (element.Match)
                            {
                                case Saber.Vendor.DataSource.FilterMatchType.Equals:
                                    sql.Append(column.Name + " = " + element.Value + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.GreaterThan:
                                    sql.Append(column.Name + " > " + element.Value + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.GreaterEqualTo:
                                    sql.Append(column.Name + " >= " + element.Value + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.LessThan:
                                    sql.Append(column.Name + " < " + element.Value + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.LessThanEqualTo:
                                    sql.Append(column.Name + " <= " + element.Value + "\n");
                                    break;
                            }
                            break;
                        case Saber.Vendor.DataSource.DataType.Boolean:
                            sql.Append(column.Name + " = " + element.Value + "\n");
                            break;
                        case Saber.Vendor.DataSource.DataType.DateTime:
                            var datetime = DateTime.Parse(element.Value);
                            var dateparts = "DATETIMEFROMPARTS(" + datetime.Year + ", " + datetime.Month + ", " + datetime.Day + "," +
                                datetime.Hour + ", " + datetime.Minute + ", " + datetime.Second + ", " + datetime.Millisecond + ")";
                            switch (element.Match)
                            {
                                case Saber.Vendor.DataSource.FilterMatchType.Equals:
                                    sql.Append(column.Name + " = " + dateparts + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.GreaterThan:
                                    sql.Append(column.Name + " > " + dateparts + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.GreaterEqualTo:
                                    sql.Append(column.Name + " >= " + dateparts + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.LessThan:
                                    sql.Append(column.Name + " < " + dateparts + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.LessThanEqualTo:
                                    sql.Append(column.Name + " <= " + dateparts + "\n");
                                    break;
                            }
                            break;
                    }
                    if(x < group.Elements.Count - 1)
                    {
                        sql.Append(group.Match == Saber.Vendor.DataSource.GroupMatchType.All ? " AND " : " OR ");
                    }
                }
            }
            foreach(var sub in group.Groups)
            {
                sql.Append(group.Match == Saber.Vendor.DataSource.GroupMatchType.All ? " AND " : " OR ");
                sql.Append(GetFilterGroupSql(sub, columns));
            }
            sql.Append(")\n");
            return sql.ToString();
        }

        public static void AddRecord(int userId, int datasetId, string lang, List<Models.DataSets.Field> fields, int recordId = 0)
        {
            var list = new Models.DataSets.Fields()
            {
                Items = fields.ToArray()
            };
            Sql.ExecuteNonQuery("DataSet_AddRecord", new { userId, datasetId, recordId, lang, fields = Common.Serializer.ToXmlDocument(list).OuterXml });
        }

        public static void UpdateRecord(int userId, int datasetId, int recordId, string lang, List<Models.DataSets.Field> fields)
        {
            var list = new Models.DataSets.Fields()
            {
                Items = fields.ToArray()
            };
            Sql.ExecuteNonQuery("DataSet_UpdateRecord", new { userId, datasetId, recordId, lang, fields = Common.Serializer.ToXmlDocument(list).OuterXml });
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

        public static List<Models.DataSets.ColumnName> GetAllColumns()
        {
            return Sql.Populate<Models.DataSets.ColumnName>("DataSet_GetAllColumns");
        }

        public static void UpdateInfo(int datasetId, int? userId, string label, string description)
        {
            Saber.Vendors.DataSets.Cache.DataSets = null;
            Sql.ExecuteNonQuery("DataSet_UpdateInfo", new { datasetId, userId, label, description });
        }

        public static void Delete(int datasetId)
        {
            Saber.Vendors.DataSets.Cache.DataSets = null;
            Sql.ExecuteNonQuery("DataSet_Delete", new { datasetId });
        }

        #region "Relationships"
        public static class Relationships
        {
            public static List<Models.DatasetRelationship> GetList(int parentId)
            {
                return Sql.Populate<Models.DatasetRelationship>("Datasets_Relationships_GetList", new { parentId });
            }
        }
        #endregion

    }
}
