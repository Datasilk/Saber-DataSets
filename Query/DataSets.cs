using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Saber.Core;

namespace Query
{
    public static class DataSets
    {
        
        public enum SearchType
        {
            any = 0,
            startsWith = 1,
            endsWith = 2,
            exactMatch = -1
        }

        #region "Filter"
        public static List<IDictionary<string, object>> GetRecords(IRequest request, int datasetId, int start = 1, int length = 50, string lang = "en", int userId = 0, List<Saber.Vendor.DataSource.FilterGroup> filters = null, List<Saber.Vendor.DataSource.OrderBy> sort = null, bool userEmail = false)
        {
            var datasource = Saber.Vendors.DataSets.Cache.DataSources[datasetId];
            var info = Vendors.DataSources.FirstOrDefault(a => a.Key == "dataset-" + datasource.Key);
            var dataset = Saber.Vendors.DataSets.Cache.DataSets[datasetId];

            var sql = new StringBuilder(@"SELECT u.name AS username, " + 
                (userEmail ? "u.email AS useremail" : "'' AS useremail") + @", d.*
FROM [DataSet_" + dataset.tableName + @"] d
LEFT JOIN Users u ON u.userId=d.userId
WHERE " + (userId > 0 && dataset.userdata ? "d.userId=" + userId + " AND" : "") + " d.lang='" + lang + "' \n");

            if(filters != null && filters.Count > 0)
            {
                for(var x = 0; x < filters.Count; x++)
                {
                    //generate root filter group sql
                    var group = filters[x];
                    var groupSql = GetFilterGroupSql(group, info, datasource, dataset, request);
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
                using (var conn = new SqlConnection(Sql.ConnectionString))
                {
                    var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
                    using (var reader = server.ConnectionContext.ExecuteReader(sql.ToString()))
                    {
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
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n\n" + sql.ToString() + "\n\n", ex);
            }
            return results;
        }

        public static Dictionary<string, List<IDictionary<string, object>>> GetRecordsInRelationships(IRequest request, int datasetId, string lang = "en", int userId = 0, Dictionary<string, Saber.Vendor.DataSource.PositionSettings> positions = null, Dictionary<string, List<Saber.Vendor.DataSource.FilterGroup>> filters = null, Dictionary<string, List<Saber.Vendor.DataSource.OrderBy>> sort = null, string[] childKeys = null, bool userEmail = false)
        {
            var datasource = Saber.Vendors.DataSets.Cache.DataSources[datasetId];
            if (datasource == null) { return null; }
            var info = Vendors.DataSources.FirstOrDefault(a => a.Key == "dataset-" + datasource.Key);
            if(info == null) { return null; }
            var dataset = Saber.Vendors.DataSets.Cache.DataSets[datasetId];
            if (dataset == null) { return null; }
            var datasets = new List<Models.DataSet>();
            var rndId = (new Random()).Next(999, 999999);
            var tmpTable = "#datasets_records_in_relationships_" + rndId;
            var sqlSelect = "";
            var sql = new StringBuilder("WHERE " + (userId > 0 && dataset.userdata && dataset.userdata ?
                "d.userId=" + userId + " AND" : "") + " d.lang='" + lang + "' \n");
            var relationshipIds = new Dictionary<string, List<string>>();
            var selectLists = datasource.Relationships.Where(a => a.Type == Saber.Vendor.DataSource.RelationshipType.SingleSelection || a.Type == Saber.Vendor.DataSource.RelationshipType.MultiSelection);
            if (selectLists.Count() > 0)
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////
                //get first result set so that we can collect a list of IDs to get for our relationship tables
                try
                {
                    //only select columns related to single/multi-select lists
                    sqlSelect = @"SELECT " + (string.Join(", ", selectLists.Select(a => "d.[" + a.ListComponent + "]"))) + @"
FROM [DataSet_" + dataset.tableName + @"] d
LEFT JOIN Users u ON u.userId=d.userId
";
                    using (var conn = new SqlConnection(Sql.ConnectionString))
                    {
                        var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
                        using (var reader = server.ConnectionContext.ExecuteReader(sqlSelect + sql.ToString()))
                        {
                            try
                            {
                                var columns = reader.GetColumnSchema();

                                //get all rows for result
                                while (reader.Read())
                                {
                                    var row = new Dictionary<string, object>();
                                    foreach (var column in columns)
                                    {
                                        row.Add(column.ColumnName, reader[column.ColumnName]);
                                    }

                                    //find all single-selection & multi-selection relationship lists and collect IDs
                                    foreach (var list in selectLists)
                                    {
                                        var col = row.ContainsKey(list.ListComponent) ? (string)row[list.ListComponent] : "";
                                        if (!string.IsNullOrEmpty(col))
                                        {
                                            var parts = col.Split("|!|");
                                            var part = parts.Where(a => a.StartsWith("selected=")).FirstOrDefault()?.Split("selected=")[1] ?? "";
                                            if (!string.IsNullOrEmpty(part))
                                            {
                                                if (!relationshipIds.ContainsKey(list.Child.Key))
                                                {
                                                    relationshipIds.Add(list.Child.Key, new List<string>(part.Split(",", StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries)));
                                                }
                                                else
                                                {
                                                    var add = part.Split(",", StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries).Where(a => !relationshipIds[list.Child.Key].Contains(a));
                                                    if (add.Count() > 0)
                                                    {
                                                        relationshipIds[list.Child.Key].AddRange(add);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message, ex);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message + "\n\n" + sqlSelect + sql.ToString() + "\n\n", ex);
                }
            }

            //generate query for parent data set /////////////////////////////////////////////////////////////////////
            sqlSelect = "SELECT u.name AS username, " +
                (userEmail ? "u.email AS useremail" : "'' AS useremail") + @", d.*
INTO " + tmpTable + @"
FROM [DataSet_" + dataset.tableName + @"] d
LEFT JOIN Users u ON u.userId=d.userId
";

            var key = "dataset-" + datasetId.ToString();
            var keyId = datasetId.ToString();
            if (filters != null && filters.ContainsKey(key) && filters[key].Count > 0)
            {
                for (var x = 0; x < filters[key].Count; x++)
                {
                    //generate root filter group sql
                    var group = filters[key][x];
                    var groupSql = GetFilterGroupSql(group, info, datasource, dataset, request);
                    if (groupSql != "")
                    {
                        sql.Append(" AND " + groupSql);
                    }
                }
            }
            if (sort != null && sort.ContainsKey(key) && sort[key].Count > 0)
            {
                sql.Append("\nORDER BY ");
                for (var x = 0; x < sort[key].Count; x++)
                {
                    //generate order by sql
                    var orderby = sort[key][x];
                    sql.Append("d.[" + orderby.Column + "]" +
                        (orderby.Direction == Saber.Vendor.DataSource.OrderByDirection.Ascending ? " ASC" : " DESC") +
                        (x < sort[key].Count - 1 ? ", \n" : "\n"));
                }
            }
            else
            {
                sql.Append("\nORDER BY d.id");
            }
            var parentPos = positions != null && positions.ContainsKey(key) ? positions[key] :
                new Saber.Vendor.DataSource.PositionSettings() { Start = 1, Length = 10 };
            if (parentPos.Start > 0)
            {
                sql.Append("\nOFFSET " + (parentPos.Start - 1) + " ROWS FETCH NEXT " + parentPos.Length + " ROWS ONLY");
            }
            sql.Append("\n\n\n");
            datasets.Add(dataset);

            sql.Append("SELECT * FROM " + tmpTable + "\n\n\n");

            foreach (var child in datasource.Relationships.Where(a => a.Key == keyId))
            {
                //generate queries for child data sets ///////////////////////////////////////////////////////////////
                if (childKeys != null && !childKeys.Contains("dataset-" + child.Child.Key)) { continue; }
                var childId = int.Parse(child.Child.Key);
                dataset = Saber.Vendors.DataSets.Cache.DataSets[childId];
                if(dataset != null)
                {
                    var childsource = Saber.Vendors.DataSets.Cache.DataSources[childId];
                    sql.Append(@"SELECT u.name AS username, '' AS useremail, d.*
FROM [DataSet_" + dataset.tableName + @"] d
LEFT JOIN Users u ON u.userId=d.userId 
WHERE " + (userId > 0 && dataset.userdata ? "d.userId=" + userId + " AND" : "") + " d.lang='" + lang + "'"); 

                    key = "dataset-" + childId.ToString();
                    if(child.Type == Saber.Vendor.DataSource.RelationshipType.RelatedList)
                    {
                        sql.Append("AND d.[" + child.ChildColumn + @"] IN (SELECT id FROM " + tmpTable + ")");
                    }
                    else if((child.Type == Saber.Vendor.DataSource.RelationshipType.SingleSelection || 
                        child.Type == Saber.Vendor.DataSource.RelationshipType.MultiSelection) &&
                        relationshipIds.ContainsKey(child.Child.Key))
                    {
                        //Get specific records based on collected IDs from single/multi-select lists
                        sql.Append("AND d.Id IN (" + string.Join(",", relationshipIds[child.Child.Key]) + ")");
                    }
                    if (child.Type != Saber.Vendor.DataSource.RelationshipType.SingleSelection)
                    {
                        if (filters != null && filters.ContainsKey(key) && filters[key].Count > 0)
                        {
                            for (var x = 0; x < filters[key].Count; x++)
                            {
                                //generate root filter group sql
                                var group = filters[key][x];
                                var groupSql = GetFilterGroupSql(group, info, datasource, dataset, request);
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
                using (var conn = new SqlConnection(Sql.ConnectionString))
                {
                    var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
                    using (var reader = server.ConnectionContext.ExecuteReader(sqlSelect + sql.ToString()))
                    {
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
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n\n" + sqlSelect + sql.ToString() + "\n\n", ex);
            }
            return results;
        }

        public static int GetRecordCount(IRequest request, int datasetId, string lang = "en", int userId = 0, List<Saber.Vendor.DataSource.FilterGroup> filters = null)
        {
            var datasource = Saber.Vendors.DataSets.Cache.DataSources[datasetId];
            if (datasource == null) { return 0; }
            var info = Vendors.DataSources.FirstOrDefault(a => a.Key == "dataset-" + datasource.Key);
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
                    var groupSql = GetFilterGroupSql(group, info, datasource, dataset, request);
                    if (groupSql != "")
                    {
                        sql.Append(" AND " + groupSql);
                    }
                }
            }

            try
            {
                int result = 0;
                using (var conn = new SqlConnection(Sql.ConnectionString))
                {
                    var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
                    using (var reader = server.ConnectionContext.ExecuteReader(sql.ToString()))
                    {
                        reader.Read();
                        result = (int)reader[0];
                    }
                }
                return result;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message + "\n\n" + sql.ToString() + "\n\n", ex);
            }
        }

        public static Dictionary<string, int> GetRecordCountInRelationships(IRequest request, int datasetId, string lang = "en", int userId = 0, Dictionary<string, List<Saber.Vendor.DataSource.FilterGroup>> filters = null, string[] childKeys = null)
        {
            var datasource = Saber.Vendors.DataSets.Cache.DataSources[datasetId];
            if (datasource == null) { return null; }
            var info = Vendors.DataSources.FirstOrDefault(a => a.Key == "dataset-" + datasource.Key);
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
                    var groupSql = GetFilterGroupSql(group, info, datasource, dataset, request);
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
                using (var conn = new SqlConnection(Sql.ConnectionString))
                {
                    var server = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(conn));
                    using (var reader = server.ConnectionContext.ExecuteReader(sql.ToString()))
                    {
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
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "\n\n" + sql.ToString() + "\n\n", ex);
            }
            return results;
        }

        private static string GetFilterGroupSql(Saber.Vendor.DataSource.FilterGroup group, DataSourceInfo info, Saber.Vendor.DataSource datasource, Models.DataSet dataset, IRequest request)
        {
            var sql = new StringBuilder();
            if (group.Elements != null && group.Elements.Count > 0)
            {
                var userId = request.User.UserId;
                var columns = datasource.Columns;
                for (var x = 0; x < group.Elements.Count; x++)
                {
                    var element = group.Elements[x];
                    if (element.Value == "#single-selection" || element.Value == "#multi-selection") { continue; }
                    var col = element.Column;
                    var colparts = col.Split(".");
                    if(colparts.Length == 2) { col = colparts[1]; }

                    //get column info
                    var column = columns.Where(a => a.Name == col).FirstOrDefault();
                    if (col == "id")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "id",
                            DataType = Saber.Vendor.DataSource.DataType.Number
                        };
                    }
                    else if (col.ToLower() == "userid")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "userId",
                            DataType = Saber.Vendor.DataSource.DataType.Number
                        };
                        switch (element.Value.ToLower())
                        {
                            case "{{userid}}":
                                //replace value with current user ID
                                column.Id = "{{userId}}";
                                break;
                        }
                    }
                    else if (col == "lang")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "lang",
                            DataType = Saber.Vendor.DataSource.DataType.Text
                        };
                    }
                    else if (col == "datecreated")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "datecreated",
                            DataType = Saber.Vendor.DataSource.DataType.DateTime
                        };
                    }
                    else if (col == "datemodified")
                    {
                        column = new Saber.Vendor.DataSource.Column()
                        {
                            Name = "datemodified",
                            DataType = Saber.Vendor.DataSource.DataType.DateTime
                        };
                    }
                    if(column == null) { continue; }

                    if (colparts.Length > 1)
                    {
                        ////////////////////////////////////////////////////////////////////////////////////////
                        /// filter columns from child table
                        var sub = datasource.Relationships.FirstOrDefault(a => a.Child.Key == colparts[0]);
                        if(sub == null) { continue; }
                        var colname = "c.[" + column.Name + "]";
                        if(sub.Type == Saber.Vendor.DataSource.RelationshipType.RelatedList)
                        {
                            sql.Append("EXISTS(SELECT * FROM " + "[DataSet_" + sub.Child.Name + "] c WHERE " +
                                "c.[" + sub.ChildColumn + "] = d.[Id] AND " + 
                                GetFilterGroupColumnSql(request, column, colname, element) + ")");
                        }else if(sub.Type == Saber.Vendor.DataSource.RelationshipType.SingleSelection ||
                            sub.Type == Saber.Vendor.DataSource.RelationshipType.MultiSelection)
                        {

                            sql.Append("EXISTS(SELECT * FROM " + "[DataSet_" + sub.Child.Name + "] c WHERE " +
                                "EXISTS(" + 
                                    "SELECT * FROM STRING_SPLIT(dbo.SUBSTRING_INDEX(d.[" + sub.ListComponent + "], 'selected=', -1), ',') s " +
                                    "WHERE s.value = CAST(c.[Id] AS varchar(16))" +
                                ") AND " +
                                GetFilterGroupColumnSql(request, column, colname, element) + ")");
                        }
                    }
                    else
                    {
                        ////////////////////////////////////////////////////////////////////////////////////////
                        /// filter columns from parent table
                        if (column.Id == "{{userId}}")
                        {
                            sql.Append("d.[userId] = " + userId + "\n");
                            continue; 
                        }
                        var colname = "d.[" + column.Name + "]";
                        //get SQL string for column
                        sql.Append(GetFilterGroupColumnSql(request, column, colname, element));
                    }
                    if (x < group.Elements.Count - 1)
                    {
                        sql.Append(group.Match == Saber.Vendor.DataSource.GroupMatchType.All ? " AND " : " OR ");
                    }
                }
            }
            if (group.Groups != null)
            {
                foreach (var sub in group.Groups)
                {
                    sql.Append(group.Match == Saber.Vendor.DataSource.GroupMatchType.All ? " AND " : " OR ");
                    sql.Append(GetFilterGroupSql(sub, info, datasource, dataset, request));
                }
            }
            if (sql.Length > 0)
            {
                return "( \n" + sql.ToString() + ")\n";
            }
            return "";
        }

        private static string GetFilterGroupColumnSql(IRequest request, Saber.Vendor.DataSource.Column column, string columnName, Saber.Vendor.DataSource.FilterElement element)
        {
            var sql = new StringBuilder();
            switch (column.DataType)
            {
                case Saber.Vendor.DataSource.DataType.Text:
                    switch (element.Match)
                    {
                        case Saber.Vendor.DataSource.FilterMatchType.StartsWith:
                            sql.Append(columnName + " LIKE '" + element.Value.Replace("'", "''") + "%'\n");
                            break;
                        case Saber.Vendor.DataSource.FilterMatchType.EndsWith:
                            sql.Append(columnName + " LIKE '%" + element.Value.Replace("'", "''") + "'\n");
                            break;
                        case Saber.Vendor.DataSource.FilterMatchType.Contains:
                            sql.Append(columnName + " LIKE '%" + element.Value.Replace("'", "''") + "%'\n");
                            break;
                        case Saber.Vendor.DataSource.FilterMatchType.Equals:
                            sql.Append(columnName + " = '" + element.Value.Replace("'", "''") + "'\n");
                            break;
                    }
                    break;
                case Saber.Vendor.DataSource.DataType.Number:
                    if(column.Id == "{{userId}}")
                    {
                        sql.Append(columnName + " = " + request.User.UserId + "\n");
                    }
                    else
                    {
                        switch (element.Match)
                        {
                            case Saber.Vendor.DataSource.FilterMatchType.Equals:
                                sql.Append(columnName + " = " + element.Value + "\n");
                                break;
                            case Saber.Vendor.DataSource.FilterMatchType.GreaterThan:
                                sql.Append(columnName + " > " + element.Value + "\n");
                                break;
                            case Saber.Vendor.DataSource.FilterMatchType.GreaterEqualTo:
                                sql.Append(columnName + " >= " + element.Value + "\n");
                                break;
                            case Saber.Vendor.DataSource.FilterMatchType.LessThan:
                                sql.Append(columnName + " < " + element.Value + "\n");
                                break;
                            case Saber.Vendor.DataSource.FilterMatchType.LessThanEqualTo:
                                sql.Append(columnName + " <= " + element.Value + "\n");
                                break;
                        }
                    }
                    break;
                case Saber.Vendor.DataSource.DataType.Boolean:
                    var boolval = element.Value == "1" || element.Value.ToLower() == "true";
                    sql.Append(columnName + " = " + boolval + "\n");
                    break;
                case Saber.Vendor.DataSource.DataType.DateTime:
                    var datetime = DateTime.Parse(element.Value);
                    var dateparts = "DATETIMEFROMPARTS(" + datetime.Year + ", " + datetime.Month + ", " + datetime.Day + "," +
                        datetime.Hour + ", " + datetime.Minute + ", " + datetime.Second + ", " + datetime.Millisecond + ")";
                    switch (element.Match)
                    {
                        case Saber.Vendor.DataSource.FilterMatchType.Equals:
                            sql.Append(columnName + " = " + dateparts + "\n");
                            break;
                        case Saber.Vendor.DataSource.FilterMatchType.GreaterThan:
                            sql.Append(columnName + " > " + dateparts + "\n");
                            break;
                        case Saber.Vendor.DataSource.FilterMatchType.GreaterEqualTo:
                            sql.Append(columnName + " >= " + dateparts + "\n");
                            break;
                        case Saber.Vendor.DataSource.FilterMatchType.LessThan:
                            sql.Append(columnName + " < " + dateparts + "\n");
                            break;
                        case Saber.Vendor.DataSource.FilterMatchType.LessThanEqualTo:
                            sql.Append(columnName + " <= " + dateparts + "\n");
                            break;
                    }
                    break;
            }
            return sql.ToString();
        }

        #endregion

        #region "DataSets"
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

        public static Models.DataSet GetInfo(int datasetId)
        {
            var list = Sql.Populate<Models.DataSet>("DataSet_GetInfo", new { datasetId });
            if (list.Count == 1)
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

        #endregion

        #region "Records"
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
            Sql.ExecuteNonQuery("DataSet_DeleteRecord", new { datasetId, recordId });
        }

        #endregion

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
