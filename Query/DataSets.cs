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
        public static int Create(string label, string partialview, string description, List<Models.DataSets.Column> columns, int? userId = null, bool userdata = false)
        {
            Saber.Vendors.DataSets.Cache.DataSets = null;
            var list = new Models.DataSets.Columns()
            {
                Items = columns.ToArray()
            };
            return Sql.ExecuteScalar<int>("DataSet_Create", new { userId, userdata, label, partialview, description, columns = Common.Serializer.ToXmlDocument(list).OuterXml });
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

        #region "Filter"
        public static List<IDictionary<string, object>> GetRecords(int datasetId, int start = 1, int length = 50, string lang = "en", int userId = 0, List<Saber.Vendor.DataSource.FilterGroup> filters = null, List<Saber.Vendor.DataSource.OrderBy> sort = null)
        {
            var datasource = Saber.Vendors.DataSets.Cache.DataSources[datasetId];
            var dataset = Saber.Vendors.DataSets.Cache.DataSets[datasetId];

            var sql = new StringBuilder(@"
SELECT u.name AS username, u.email AS useremail, d.*
FROM [DataSet_" + dataset.tableName + @"] d
LEFT JOIN Users u ON u.userId=d.userId
WHERE " + (userId > 0 && dataset.userdata ? "d.userId=" + userId + " AND" : "") + " d.lang='" + lang + "' \n");

            if(filters != null && filters.Count > 0)
            {
                for(var x = 0; x < filters.Count; x++)
                {
                    //generate root filter group sql
                    var group = filters[x];
                    var groupSql = GetFilterGroupSql(group, datasource.Columns);
                    if (groupSql != "")
                    {
                        sql.Append(" AND " + groupSql);
                    }
                }
            }
            if(sort != null && sort.Count > 0)
            {
                sql.Append(" ORDER BY ");
                for (var x = 0; x < sort.Count; x++)
                {
                    //generate order by sql
                    var orderby = sort[x];
                    sql.Append("d." + orderby.Column + 
                        (orderby.Direction == Saber.Vendor.DataSource.OrderByDirection.Ascending ? " ASC" : " DESC") + 
                        (x < sort.Count - 1 ? ", " : ""));
                }
            }
            else
            {
                sql.Append(" ORDER BY d.id");
            }
            if(start > 0)
            {
                sql.Append(" OFFSET " + (start - 1) + " ROWS FETCH NEXT " + length + " ROWS ONLY");
            }

            var results = new List<IDictionary<string, object>>();
            try
            {
                var conn = new SqlConnection(Sql.ConnectionString);
                var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
                var reader = server.ConnectionContext.ExecuteReader(sql.ToString());
                var columns = reader.GetColumnSchema();
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    foreach (var column in columns)
                    {
                        row.Add(column.ColumnName, reader[column.ColumnName]);
                    }
                    results.Add(row);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n\n" + sql.ToString() + "\n\n", ex);
            }
            return results;
        }

        public static Dictionary<string, List<IDictionary<string, object>>> GetRecordsInRelationships(int datasetId, string lang = "en", int userId = 0, Dictionary<string, Saber.Vendor.DataSource.PositionSettings> positions = null, Dictionary<string, List<Saber.Vendor.DataSource.FilterGroup>> filters = null, Dictionary<string, List<Saber.Vendor.DataSource.OrderBy>> sort = null, string[] childKeys = null)
        {
            var datasource = Saber.Vendors.DataSets.Cache.DataSources[datasetId];
            if (datasource == null) { return null; }
            var dataset = Saber.Vendors.DataSets.Cache.DataSets[datasetId];
            if (dataset == null) { return null; }
            var datasets = new List<Models.DataSet>();
            var rndId = (new Random()).Next(999, 999999);
            var tmpTable = "#datasets_records_in_relationships_" + rndId;

            //generate query for parent data set ///////////////////////////////////////////////////////////////////
            var sql = new StringBuilder(@"
SELECT u.name AS username, u.email AS useremail, d.*
INTO " + tmpTable + @"
FROM [DataSet_" + dataset.tableName + @"] d
LEFT JOIN Users u ON u.userId=d.userId
WHERE " + (userId > 0 && dataset.userdata && dataset.userdata ? "d.userId=" + userId + " AND" : "") + " d.lang='" + lang + "' \n");

            var key = "dataset-" + datasetId.ToString();
            var keyId = datasetId.ToString();
            if (filters != null && filters.ContainsKey(key) && filters[key].Count > 0)
            {
                for (var x = 0; x < filters[key].Count; x++)
                {
                    //generate root filter group sql
                    var group = filters[key][x];
                    var groupSql = GetFilterGroupSql(group, datasource.Columns);
                    if (groupSql != "")
                    {
                        sql.Append(" AND " + groupSql);
                    }
                }
            }
            if (sort != null && sort.ContainsKey(key) && sort[key].Count > 0)
            {
                sql.Append("\n ORDER BY "); 
                for (var x = 0; x < sort[key].Count; x++)
                {
                    //generate order by sql
                    var orderby = sort[key][x];
                    sql.Append("d." + orderby.Column +
                        (orderby.Direction == Saber.Vendor.DataSource.OrderByDirection.Ascending ? " ASC" : " DESC") +
                        (x < sort[key].Count - 1 ? ", \n" : "\n"));
                }
            }
            else
            {
                sql.Append("\n ORDER BY d.id");
            }
            var parentPos = positions != null && positions.ContainsKey(key) ? positions[key] :
                new Saber.Vendor.DataSource.PositionSettings() { Start = 1, Length = 10 };
            if (parentPos.Start > 0)
            {
                sql.Append("\n OFFSET " + (parentPos.Start - 1) + " ROWS FETCH NEXT " + parentPos.Length + " ROWS ONLY");
            }
            sql.Append("\n\n\n SELECT * FROM " + tmpTable);
            datasets.Add(dataset);

            foreach (var child in datasource.Relationships.Where(a => a.Key == keyId))
            {
                //generate queries for child data sets ///////////////////////////////////////////////////////////////
                if (childKeys != null && !childKeys.Contains("dataset-" + child.Child.Key)) { continue; }
                var childId = int.Parse(child.Child.Key);
                dataset = Saber.Vendors.DataSets.Cache.DataSets[childId];
                if(dataset != null)
                {
                    var childsource = Saber.Vendors.DataSets.Cache.DataSources[childId];
                    sql.Append(@"

SELECT u.name AS username, u.email AS useremail, d.*
FROM [DataSet_" + dataset.tableName + @"] d
LEFT JOIN Users u ON u.userId=d.userId
WHERE " + (userId > 0 && dataset.userdata ? "d.userId=" + userId + " AND" : "") + " d.lang='" + lang + @"'"); 

                    //make sure no-relationship doesn't exist in the filters
                    key = "dataset-" + childId.ToString();
                    if(child.Type != Saber.Vendor.DataSource.RelationshipType.SingleSelection && child.Type != Saber.Vendor.DataSource.RelationshipType.FilteredList)
                    {
                        sql.Append(@"AND d.[" + child.ChildColumn + @"] IN (SELECT id FROM " + tmpTable + ")");
                    }
                    else if(child.Type == Saber.Vendor.DataSource.RelationshipType.SingleSelection)
                    {
                        sql.Append(@"AND EXISTS(SELECT * FROM " + tmpTable + " WHERE [" + child.ChildColumn + "] " +
                            "LIKE '%selected=' + CAST(d.id AS varchar(16)))");
                    }
                    else
                    {
                        //for multi-select, just return all records in the table (for now, until I find a fix for this issue)
                    }
                    if(child.Type != Saber.Vendor.DataSource.RelationshipType.SingleSelection)
                    {
                        if (filters != null && filters.ContainsKey(key) && filters[key].Count > 0)
                        {
                            for (var x = 0; x < filters[key].Count; x++)
                            {
                                //generate root filter group sql
                                var group = filters[key][x];
                                var groupSql = GetFilterGroupSql(group, childsource.Columns);
                                if (!string.IsNullOrEmpty(groupSql))
                                {
                                    sql.Append(" AND " + groupSql);
                                }
                            }
                        }
                        if (sort != null && sort.ContainsKey(key) && sort[key].Count > 0)
                        {
                            sql.Append("\n ORDER BY ");
                            for (var x = 0; x < sort[key].Count; x++)
                            {
                                //generate order by sql
                                var orderby = sort[key][x];
                                sql.Append("d." + orderby.Column +
                                    (orderby.Direction == Saber.Vendor.DataSource.OrderByDirection.Ascending ? " ASC" : " DESC") +
                                    (x < sort[key].Count - 1 ? ", \n" : "\n"));
                            }
                        }
                        else
                        {
                            sql.Append("\n ORDER BY d.id");
                        }
                        var pos = positions != null && positions.ContainsKey(key) ? positions[key] :
                            new Saber.Vendor.DataSource.PositionSettings() { Start = 1, Length = 1000 };
                        if (pos.Start > 0)
                        {
                            sql.Append("\n OFFSET " + (pos.Start - 1) + " ROWS FETCH NEXT " + (parentPos.Length * pos.Length) + " ROWS ONLY");
                        }
                    }
                    
                    sql.Append("\n\n\n");
                    datasets.Add(dataset);
                }
            }
            sql.Append("\nDROP TABLE " + tmpTable + "\n");

            //execute query ///////////////////////////////////////////////////////////////////////////////////////////////////
            var results = new Dictionary<string, List<IDictionary<string, object>>>();
            try
            {
                var conn = new SqlConnection(Sql.ConnectionString);
                var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
                var reader = server.ConnectionContext.ExecuteReader(sql.ToString());
                var i = 0;
                //read query results
                do
                {
                    try
                    {
                        dataset = datasets[i];
                        var columns = reader.GetColumnSchema();
                        var result = new List<IDictionary<string, object>>();
                        //get all rows for result
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            foreach (var column in columns)
                            {
                                row.Add(column.ColumnName, reader[column.ColumnName]);
                            }
                            result.Add(row);
                        }
                        results.Add(dataset.datasetId.ToString(), result);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message, ex);
                    }

                    i++;
                } while (reader.NextResult());
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n\n" + sql.ToString() + "\n\n", ex);
            }
            return results;
        }

        public static int GetRecordCount(int datasetId, string lang = "en", int userId = 0, List<Saber.Vendor.DataSource.FilterGroup> filters = null)
        {
            var datasource = Saber.Vendors.DataSets.Cache.DataSources[datasetId];
            if (datasource == null) { return 0; }
            var dataset = Saber.Vendors.DataSets.Cache.DataSets[datasetId];
            if (dataset == null) { return 0; }

            var sql = new StringBuilder(@"
SELECT COUNT(*)
FROM [DataSet_" + dataset.tableName + @"] d
LEFT JOIN Users u ON u.userId=d.userId
WHERE " + (userId > 0 && dataset.userdata ? "d.userId=" + userId + " AND" : "") + " d.lang='" + lang + "' \n");

            if (filters != null && filters.Count > 0)
            {
                for (var x = 0; x < filters.Count; x++)
                {
                    //generate root filter group sql
                    var group = filters[x];
                    var groupSql = GetFilterGroupSql(group, datasource.Columns);
                    if (groupSql != "")
                    {
                        sql.Append(" AND " + groupSql);
                    }
                }
            }

            try
            {
                var conn = new SqlConnection(Sql.ConnectionString);
                var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
                var reader = server.ConnectionContext.ExecuteReader(sql.ToString());
                reader.Read();
                return (int)reader[0];
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message + "\n\n" + sql.ToString() + "\n\n", ex);
            }
            return 0;
        }

        public static Dictionary<string, int> GetRecordCountInRelationships(int datasetId, string lang = "en", int userId = 0, Dictionary<string, List<Saber.Vendor.DataSource.FilterGroup>> filters = null, string[] childKeys = null)
        {
            var datasource = Saber.Vendors.DataSets.Cache.DataSources[datasetId];
            if (datasource == null) { return null; }
            var dataset = Saber.Vendors.DataSets.Cache.DataSets[datasetId];
            if (dataset == null) { return null; }
            var datasets = new List<Models.DataSet>();
            var rndId = (new Random()).Next(999, 999999);
            var tmpTable = "#datasets_records_in_relationships_" + rndId;

            //generate query for parent data set ///////////////////////////////////////////////////////////////////
            var sql = new StringBuilder(@"
SELECT COUNT(*)
FROM [DataSet_" + dataset.tableName + @"] d
LEFT JOIN Users u ON u.userId=d.userId
WHERE " + (userId > 0 && dataset.userdata ? "d.userId=" + userId + " AND" : "") + " d.lang='" + lang + "' \n");

            var key = "dataset-" + datasetId.ToString();
            var keyId = datasetId.ToString();
            if (filters != null && filters.ContainsKey(key) && filters[key].Count > 0)
            {
                for (var x = 0; x < filters[key].Count; x++)
                {
                    //generate root filter group sql
                    var group = filters[key][x];
                    var groupSql = GetFilterGroupSql(group, datasource.Columns);
                    if (groupSql != "")
                    {
                        sql.Append(" AND " + groupSql);
                    }
                }
            }

            //execute query ///////////////////////////////////////////////////////////////////////////////////////////////////
            var results = new Dictionary<string, int>();
            try
            {
                var conn = new SqlConnection(Sql.ConnectionString);
                var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
                var reader = server.ConnectionContext.ExecuteReader(sql.ToString());
                var i = 0;
                //read query results
                do
                {
                    try
                    {
                        dataset = datasets[i];
                        //get all rows for result
                        reader.Read();
                        results.Add(dataset.datasetId.ToString(), (int)reader[0]);
                    }
                    catch (Exception)
                    {
                        results.Add(dataset.datasetId.ToString(), 0);
                    }

                    i++;
                } while (reader.NextResult());
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n\n" + sql.ToString() + "\n\n", ex);
            }
            return results;
        }

        #endregion

        private static string GetFilterGroupSql(Saber.Vendor.DataSource.FilterGroup group, Saber.Vendor.DataSource.Column[] columns, string childColumn = "")
        {
            var sql = new StringBuilder();
            if(group.Elements != null && group.Elements.Count > 0)
            {
                for (var x = 0; x < group.Elements.Count; x++)
                {
                    var element = group.Elements[x];
                    var column = columns.Where(a => a.Name == element.Column).FirstOrDefault();
                    if(element.Value == "#single-selection" || element.Value == "#multi-selection") { continue; }
                    if (element.Column == "id")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "id",
                            DataType = Saber.Vendor.DataSource.DataType.Number
                        };
                    }
                    else if (element.Column.ToLower() == "userid")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "userId",
                            DataType = Saber.Vendor.DataSource.DataType.Number
                        };
                    }
                    else if (element.Column == "lang")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "lang",
                            DataType = Saber.Vendor.DataSource.DataType.Text
                        };
                    }
                    else if (element.Column == "datecreated")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "datecreated",
                            DataType = Saber.Vendor.DataSource.DataType.DateTime
                        };
                    }
                    else if (element.Column == "datemodified")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "datemodified",
                            DataType = Saber.Vendor.DataSource.DataType.DateTime
                        };
                    }
                    if (column == null) { continue; }
                    var colname = "[" + column.Name + "]";
                    switch (column.DataType)
                    {
                        case Saber.Vendor.DataSource.DataType.Text:
                            switch (element.Match)
                            {
                                case Saber.Vendor.DataSource.FilterMatchType.StartsWith:
                                    sql.Append(colname + " LIKE '" + element.Value.Replace("'", "''") + "%'\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.EndsWith:
                                    sql.Append(colname + " LIKE '%" + element.Value.Replace("'", "''") + "'\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.Contains:
                                    sql.Append(colname + " LIKE '%" + element.Value.Replace("'", "''") + "%'\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.Equals:
                                    sql.Append(colname + " = '" + element.Value.Replace("'", "''") + "'\n");
                                    break;
                            }
                            break;
                        case Saber.Vendor.DataSource.DataType.Number:
                            switch (element.Match)
                            {
                                case Saber.Vendor.DataSource.FilterMatchType.Equals:
                                    sql.Append(colname + " = " + element.Value + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.GreaterThan:
                                    sql.Append(colname + " > " + element.Value + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.GreaterEqualTo:
                                    sql.Append(colname + " >= " + element.Value + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.LessThan:
                                    sql.Append(colname + " < " + element.Value + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.LessThanEqualTo:
                                    sql.Append(colname + " <= " + element.Value + "\n");
                                    break;
                            }
                            break;
                        case Saber.Vendor.DataSource.DataType.Boolean:
                            sql.Append(colname + " = " + element.Value + "\n");
                            break;
                        case Saber.Vendor.DataSource.DataType.DateTime:
                            var datetime = DateTime.Parse(element.Value);
                            var dateparts = "DATETIMEFROMPARTS(" + datetime.Year + ", " + datetime.Month + ", " + datetime.Day + "," +
                                datetime.Hour + ", " + datetime.Minute + ", " + datetime.Second + ", " + datetime.Millisecond + ")";
                            switch (element.Match)
                            {
                                case Saber.Vendor.DataSource.FilterMatchType.Equals:
                                    sql.Append(colname + " = " + dateparts + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.GreaterThan:
                                    sql.Append(colname + " > " + dateparts + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.GreaterEqualTo:
                                    sql.Append(colname + " >= " + dateparts + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.LessThan:
                                    sql.Append(colname + " < " + dateparts + "\n");
                                    break;
                                case Saber.Vendor.DataSource.FilterMatchType.LessThanEqualTo:
                                    sql.Append(colname + " <= " + dateparts + "\n");
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
            if(group.Groups != null)
            {
                foreach (var sub in group.Groups)
                {
                    sql.Append(group.Match == Saber.Vendor.DataSource.GroupMatchType.All ? " AND " : " OR ");
                    sql.Append(GetFilterGroupSql(sub, columns));
                }
            }
            if (sql.Length > 0)
            {
                sql.Append(")\n");
                return "( \n" + sql.ToString();
            }
            return "";
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

        public static void DeleteRecord(int datasetId, int recordId)
        {
            Sql.ExecuteNonQuery("DataSet_DeleteRecord", new { datasetId, recordId});
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

        public static void UpdateInfo(int datasetId, int? userId, bool userdata, string label, string description)
        {
            Saber.Vendors.DataSets.Cache.DataSets = null;
            Sql.ExecuteNonQuery("DataSet_UpdateInfo", new { datasetId, userId, userdata, label, description });
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

            public static List<Models.DatasetRelationship> GetAll()
            {
                return Sql.Populate<Models.DatasetRelationship>("Datasets_Relationships_GetAll");
            }
        }
        #endregion

    }
}
