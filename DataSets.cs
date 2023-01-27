using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Saber.Core;
using Saber.Vendor;

namespace Saber.Vendors.DataSets
{
    public class DataSets : Core.Service, IVendorService
    {
        public static string[] ExcludedFields = new string[]{"id", "lang", "datecreated", "datemodified"};

        #region "Data Sets"
        public string GetList(bool owned = true, bool all = true, string search = "")
        {
            if (IsPublicApiRequest || !CheckSecurity("view-datasets")) { return AccessDenied(); }
            try
            {
                var datasets = Query.DataSets.GetList(owned ? User.UserId : null, all, false, search);
                return JsonResponse(datasets.Select(a => new { a.datasetId, a.tableName, a.label, a.partialview, a.description }));
            }
            catch (Exception)
            {
                return Error("Could not retrieve list of Data Sets" + (!string.IsNullOrEmpty(search) ? " using search \"" + search + "\"." : ""));
            }
        }

        public string GetPermissions()
        {
            if (IsPublicApiRequest) { return AccessDenied(); }
            return (CheckSecurity("create-datasets") ? "1" : "0") + "," +
                (CheckSecurity("edit-datasets") ? "1" : "0") + "," +
                (CheckSecurity("delete-datasets") ? "1" : "0") + "," +
                (CheckSecurity("view-datasets") ? "1" : "0") + "," +
                (CheckSecurity("add-dataset-data") ? "1" : "0");
        }

        public string GetCreateForm()
        {
            if (IsPublicApiRequest || !CheckSecurity("create-datasets")) { return AccessDenied(); }
            return Saber.Cache.LoadFile("/Vendors/DataSets/Views/create.html");
        }

        public string LoadColumns(string partial)
        {
            if (IsPublicApiRequest || !CheckSecurity("create-datasets")) { return AccessDenied(); }
            //generate a form based on all the mustache variables & mustache components in the partial view.
            //add extra forms for each List component in order to create sub-data sets.
            string html;
            try
            {
                html = RenderColumns(partial, new[] { "id", "lang", "userId", "datecreated", "datemodified" });
            }catch(Exception ex)
            {
                return Error(ex.Message);
            }

            var viewColumns = new View("/Vendors/DataSets/Views/columns.html");
            viewColumns["summary"] = "Dataset columns were generated based on the partial view you selected. Please make any data type changes to your columns before continuing.";
            viewColumns["save-button"] = "Create Dataset";
            viewColumns["columns"] = html;

            return viewColumns.Render();
        }

        public string LoadNewColumns(int datasetId)
        {
            //used to display new columns for an existing dataset from the associated partial view
            if (IsPublicApiRequest || !CheckSecurity("edit-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not have access to this dataset"); }
            string html;
            try
            {
                //get existing columns
                var columns = Query.DataSets.GetColumns(datasetId).Select(a => a.Name).ToList();
                columns.Add("id");
                columns.Add("lang");
                columns.Add("userid");
                columns.Add("datecreated");
                columns.Add("datemodified");
                html = RenderColumns(dataset.partialview, columns.ToArray());
            }
            catch (Exception ex)
            {
                //return success since there are no new columns to add to the data set
                return Success();
            }

            //show popup modal to allow the user to choose data types for all new columns being added to the data set
            var viewColumns = new View("/Vendors/DataSets/Views/columns.html");
            viewColumns["summary"] = "New columns were found based on the partial view associated with this data set. Please make any data type changes to your new columns before continuing.";
            viewColumns["save-button"] = "Update Dataset";
            viewColumns["columns"] = html;

            return viewColumns.Render();
        }

        private string RenderColumns(string partial, string[] excludeColumns = null)
        {
            var html = new StringBuilder();
            var view = new View("/Content/" + partial);
            var viewColumn = new View("/Vendors/DataSets/Views/column-field.html");
            var cols = 0;
            var fieldElementInfo = new List<Models.ContentFieldElementInfo>();
            var optionsHtml = new StringBuilder("<option value=\"\" selected=\"selected\">[Select A Data Set]</option>");
            foreach (var dataset in Query.DataSets.GetList(null, true, true))
            {
                optionsHtml.Append("<option value=\"" + dataset.datasetId + "\">" + dataset.label + "</option>");
            }
            var optionsDataSources = optionsHtml.ToString();


            for (var x = 0; x < view.Elements.Count; x++)
            {
                var elem = view.Elements[x];
                fieldElementInfo.Add(new Models.ContentFieldElementInfo()
                {
                    Type = ContentFields.FieldType.text
                });
                var elemInfo = fieldElementInfo[^1];
                //check if we should skip element
                if (elem.Name == "" || elem.Name.Substring(0, 1) == "/" ||
                    (excludeColumns != null && excludeColumns.Any(a => elem.Name.IndexOf(a) == 0)) ||
                    Core.Vendors.HtmlComponentKeys.Any(a => elem.Name.IndexOf(a) == 0 && elem.Name.IndexOf("list-") != 0) ||
                    fieldElementInfo.Any(a => a.Name == elem.Name)) { continue; }
                
                //render column
                viewColumn.Clear();
                viewColumn["id"] = elem.Name.ToLower();
                viewColumn["name"] = elem.Name;
                viewColumn["options-datasources"] = optionsDataSources;
                elemInfo.Name = elem.Name;

                if (elem.isBlock)
                {
                    viewColumn.Show("datatype-bit");
                    viewColumn.Show("default-bit");
                }
                else
                {
                    var datatype = ContentFields.GetFieldType(view, x, fieldElementInfo);
                    elemInfo.Type = datatype;
                    if (datatype == ContentFields.FieldType.image)
                    {
                        viewColumn.Show("datatype-image");
                    }
                    else if(datatype == ContentFields.FieldType.list)
                    {
                        viewColumn.Show("datatype-list");
                        viewColumn.Show("default-list");
                    }
                    else
                    {
                        viewColumn.Show("datatype");
                        viewColumn.Show("maxlength");
                        viewColumn.Show("default-text");
                    }
                }
                html.Append(viewColumn.Render());
                cols++;
            }

            foreach (var elem in view.Elements.Where(a => !a.isBlock && a.Name.StartsWith("list")))
            {
                //TODO: get all views from list components and include them in the form
            }

            if (cols == 0)
            {
                throw new Exception("No mustache variables could be found within the selected partial view.");
            }
            return html.ToString();
        }

        public string Create(string name, string partial, string description, List<Query.Models.DataSets.Column> columns, bool isprivate = false)
        {
            if (IsPublicApiRequest || !CheckSecurity("create-datasets")) { return AccessDenied(); }
            if(columns == null || columns.Count <= 0 || columns[0].Name == null || columns[0].Name == "") { return Error("No columns were defined"); }
            try
            {
                var id = Query.DataSets.Create(name, partial, description, columns, isprivate == true ? User.UserId : null);
                Cache.DataSources = null;
                return id > 0 ? id.ToString() : Error("An error occurred when trying to create a new data set");
            }
            catch(Exception ex)
            {
                return Error(ex.Message);
            }
        }

        public string UpdateColumns(int datasetId, List<Query.Models.DataSets.Column> columns)
        {
            if (IsPublicApiRequest || !CheckSecurity("edit-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not have access to this dataset"); }
            if (columns == null || columns.Count <= 0 || columns[0].Name == null || columns[0].Name == "") { return Error("No columns were defined"); }
            try
            {
                Query.DataSets.UpdateColumns(datasetId, columns);
                Cache.DataSources = null;
                return Success();
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        public string Details(int datasetId, string lang, int start = 1, int length = 50, List<DataSource.FilterGroup> filters = null, List<DataSource.OrderBy> sort = null)
        {
            if (IsPublicApiRequest || !CheckSecurity("view-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not have access to this dataset"); }
            var data = Query.DataSets.GetRecords(datasetId, start, length, lang, User.UserId, filters, sort);
            var view = new View("/Vendors/DataSets/Views/dataset.html");
            var viewMenu = new View("/Vendors/DataSets/Views/record-menu.html");
            var header = new StringBuilder();
            var rows = new StringBuilder();
            var partial = new View("/Content/" + dataset.partialview);
            var lists = partial.Elements.Where(a => a.Name == "list" || a.Name.IndexOf("list-") == 0).Select(a => a.Name.Replace("-", "_")).ToList();

            //load relationship data sets list
            var relationships = Query.DataSets.Relationships.GetList(datasetId);
            if(relationships != null && relationships.Count > 0)
            {
                var html = new StringBuilder();
                var viewRelationship = new View("/Vendors/DataSets/Views/relationship.html");
                foreach(var rel in relationships)
                {
                    viewRelationship.Clear();
                    viewRelationship["id"] = rel.childId.ToString();
                    viewRelationship["name"] = rel.childLabel;
                    html.Append(viewRelationship.Render());
                }
                view.Show("has-relationships");
                view["relationships"] = html.ToString();
            }

            //load list of records
            if(data.Count > 0)
            {
                //generate table header
                var i = 0;
                foreach (var item in data.First())
                {
                    //load dataset column names in header row
                    i++;
                    if( (i <= 4 && i != 3) || (i == 5 && !User.IsAdmin)) 
                    {
                        //skip username, useremail, lang, & userId columns
                        continue; 
                    }else if((i == 5 && User.IsAdmin))
                    {
                        header.Append("<td class=\"owner\"><b>Owner</b></td>");
                    }
                    else if (lists.Any(a => a == item.Key))
                    {
                        //do not show any list data
                    }
                    else
                    {
                        header.Append("<td>" + item.Key + "</td>");
                    }
                }
                view["table-head"] = header.ToString();

                //generate table rows
                foreach (var item in data)
                {
                    //load column values for each dataset record
                    viewMenu.Clear();
                    var recordId = (int)item["Id"];
                    viewMenu["recordId"] = recordId.ToString();
                    rows.Append("<tr data-id=\"" + recordId + "\">");
                    i = 0;
                    var username = item.ContainsKey("username") && item["username"] != null ? item["username"].ToString() : "";
                    var useremail = item.ContainsKey("useremail") && item["useremail"] != null ? item["useremail"].ToString() : "";
                    foreach (var col in item)
                    {
                        i++;
                        if (i <= 4 && i != 3) {
                            //skip username, recordId, & lang columns
                            continue; 
                        } 
                        if(i == 5 && User.IsAdmin == true)
                        {
                            //display user information to Admin
                            rows.Append("<td class=\"no-details\"><a href=\"javascript:\" onclick=\"S.editor.datasets.viewOwner(event, " + col.Value + ",'" + useremail + "')\">" + username + "</a></td>");
                        }
                        else if (lists.Any(a => a == col.Key))
                        {
                            //do not show any list data
                        }
                        else
                        {
                            //display field value
                            rows.Append("<td>" + ConvertFieldToString(col.Value) + "</td>");
                        }
                    }
                    rows.Append(viewMenu.Render() + "</tr>");
                }
                view["rows"] = rows.ToString();
                view.Show("has-rows");
            }
            else
            {
                view.Show("empty");
            }
            return view.Render();
        }

        public string GetUpdateInfoForm(int datasetId)
        {
            if (IsPublicApiRequest || !CheckSecurity("edit-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not have access to this dataset"); }
            var view = new View("/Vendors/DataSets/Views/update.html");
            view["name"] = dataset.label;
            view["description"] = dataset.description;
            if (dataset.userId.HasValue) { view.Show("isprivate"); }
            return view.Render();
        }

        public string UpdateInfo(int datasetId, string name, string description, bool isprivate)
        {
            if (IsPublicApiRequest || !CheckSecurity("edit-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not have access to this dataset"); }
            Query.DataSets.UpdateInfo(datasetId, isprivate == true ? User.UserId : null, name, description);
            Cache.DataSources = null;
            return Success();
        }

        public string Delete(int datasetId)
        {
            if (IsPublicApiRequest || !CheckSecurity("delete-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not have access to this dataset"); }
            try
            {
                Query.DataSets.Delete(datasetId);
                Cache.DataSources = null;
                return Success();
            }
            catch (Exception)
            {
                return Error("Could not delete Data Set");
            }
        }

        public string ListComponents(int datasetId)
        {
            if (IsPublicApiRequest || !CheckSecurity("view-datasets")) { return AccessDenied(); }
            try
            {
                var dataset = Query.DataSets.GetInfo(datasetId);
                var view = new View("/Content/" + dataset.partialview);
                var lists = new List<string>();
                foreach(var elem in view.Elements)
                {
                    if(elem.Name == "list" || elem.Name.IndexOf("list-") == 0)
                    {
                        lists.Add(elem.Name);
                    }
                }
                return JsonResponse(lists);
            }
            catch (Exception)
            {
                return Error("Could not retrieve list components for data set ID " + datasetId);
            }
        }
        #endregion

        #region "Records"
        public string LoadNewRecordForm(int datasetId)
        {
            if (IsPublicApiRequest || !CheckSecurity("view-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not own this dataset"); }
            var details = Query.DataSets.GetInfo(datasetId);
            var view = new View("/partials/" + details.partialview);
            return ContentFields.RenderForm(this, details.label, view, User.Language, ".popup.new-record-for-" + datasetId, new Dictionary<string, string>(), ExcludedFields);
        }

        public string CreateRecord(int datasetId, string lang, Dictionary<string, string> fields, int recordId = 0)
        {
            if (IsPublicApiRequest || !CheckSecurity("add-dataset-data")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not own this dataset"); }
            if (fields.Count == 0)
            {
                return Error("No fields were included when trying to create a new record");
            }
            Query.DataSets.AddRecord(User.UserId, datasetId, lang, fields.Select(a => new Query.Models.DataSets.Field() { Name = a.Key.Replace("-", "_"), Value = a.Value }).ToList(), recordId);
            return Success();
        }

        public string GetRecord(int datasetId, int recordId, string lang)
        {
            if (IsPublicApiRequest || !CheckSecurity("view-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not own this dataset"); }
            var record = Query.DataSets.GetRecords(datasetId, 1, 1, lang, User.UserId, new List<DataSource.FilterGroup>()
            {
                new DataSource.FilterGroup()
                {
                    Match = DataSource.GroupMatchType.All,
                    Elements = new List<DataSource.FilterElement>()
                    {
                        new DataSource.FilterElement()
                        {
                            Column = "id",
                            Value = recordId.ToString(),
                            Match = DataSource.FilterMatchType.Equals
                        }
                    }
                }
            }).FirstOrDefault();
            var fields = new Dictionary<string, string>();
            if(record == null && lang != "en")
            {
                //try to get record in English if one doesn't exist in the selected language
                record = Query.DataSets.GetRecords(datasetId, 1, 1, "en", User.UserId).FirstOrDefault();
            }
            if(record != null)
            {
                foreach (var item in record)
                {
                    //convert field values to strings based on data type
                    fields.Add(item.Key, ConvertFieldToString(item.Value));
                }
            }
            return JsonResponse(fields);
        }

        public string UpdateRecord(int datasetId, int recordId, string lang, Dictionary<string, string> fields)
        {
            if (IsPublicApiRequest || !CheckSecurity("add-dataset-data")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not own this dataset"); }
            if (fields.Count == 0)
            {
                return Error("No fields were included when trying to update an existing record");
            }
            Query.DataSets.UpdateRecord(User.UserId, datasetId, recordId, lang, fields.Select(a => new Query.Models.DataSets.Field() { Name = a.Key.Replace("-", "_"), Value = a.Value }).ToList());
            return Success();
        }

        public string DeleteRecord(int datasetId, int recordId)
        {
            if (IsPublicApiRequest || !CheckSecurity("add-dataset-data")) { return AccessDenied(); }
            if (!IsOwner(datasetId, out var dataset)) { return AccessDenied("You do not own this dataset"); }
            try
            {
                Query.DataSets.DeleteRecord(datasetId, recordId);
            }
            catch (Exception ex)
            {
                return Error("Could not delete record " + recordId + " from data set " + datasetId);
            }
            return Success();
        }
        #endregion

        #region "Content Fields"
        public string RenderContentFields(string path, string language, string container, bool showlang = false, Dictionary<string, string> data = null, List<string> exclude = null)
        {
            if (IsPublicApiRequest || !CheckSecurity("edit-content")) { return AccessDenied(); }
            var paths = PageInfo.GetRelativePath(path);
            var fields = data != null && data.Keys.Count > 0 ? data : ContentFields.GetPageContent(path, language);
            var view = new View(string.Join("/", paths) + (path.Contains(".html") ? "" : ".html"));
            var parentId = Parameters.ContainsKey("parentId") ? int.Parse(Parameters["parentId"]) : 0;
            var datasetId = Parameters.ContainsKey("datasetId") ? int.Parse(Parameters["datasetId"]) : 0;
            var recordId = Parameters.ContainsKey("recordId") ? int.Parse(Parameters["recordId"]) : 0;
            var relationships = Query.DataSets.Relationships.GetList(datasetId);
            var parents = Query.DataSets.Relationships.GetList(parentId);
            var columns = Query.DataSets.GetColumns(datasetId);
            var lists = view.Elements.Where(a => a.Name == "list" || a.Name.IndexOf("list-") == 0);
            var fieldTypes = new Dictionary<string, ContentFields.FieldType>();
            var hideElements = new List<string>();
            foreach(var elem in view.Elements)
            {
                var name = elem.Name.Replace("-", "_");
                var i = relationships.FindIndex(a => a.childColumn == name);
                if (i >= 0)
                {
                    hideElements.Add(elem.Name);
                }
                i = parents.FindIndex(a => a.childColumn == name);
                if (i >= 0)
                {
                    parents[i].childKey = elem.Name;
                    hideElements.Add(elem.Name);
                }
            }
            foreach(var list in lists)
            {
                var columnName = list.Name.Replace("-", "_");
                var field = "";
                var parts = new List<string>();
                if (fields.ContainsKey(columnName))
                {
                    field = fields[columnName];
                    parts = field.Split("|!|").ToList();
                }
                var relationship = relationships.Where(a => a.parentList == list.Name).FirstOrDefault();
                if(relationship != null && field.IndexOf("data-src=") < 0)
                {
                    parts.Add("data-src=dataset-" + relationship.childId);
                }
                if (!parts.Contains("locked"))
                {
                    parts.Add("locked");
                }
                if (!parts.Contains("add") && data != null && data.Keys.Count > 0)
                {
                    parts.Add("add");
                    parts.Add("filter=[]");//include filter groups array
                    parts.Add("sort=[]");//include sort array
                }
                else if(data == null || data.Keys.Count == 0)
                {
                    hideElements.Add(list.Name);
                }
                fields[list.Name] = string.Join("|!|", parts.Where(a => !string.IsNullOrEmpty(a)));

                //add arguments to mustache code
                list.Vars.Add("renderapi", "DataSets/RenderContentFields?parentId=" + datasetId + "&datasetId=" + relationship.childId + "&recordId=" + recordId);
            }

            //hide all unusable lists
            foreach(var elem in hideElements)
            {
                var i = view.Elements.FindIndex(a => a.Name == elem);
                view.Elements[i] = new ViewElement()
                {
                    Name = "",
                    Html = "",
                    Var = "",
                    Vars = new Dictionary<string, string>()
                };
            }

            //find datatypes for fields
            foreach (var elem in view.Elements.Where(a => !string.IsNullOrEmpty(a.Name) && a.isBlock == false).Select(a => a.Name).Distinct())
            {
                var name = elem.Replace("-", "_");
                var datatype = columns.Where(a => a.Name == name).FirstOrDefault();
                if (datatype != null)
                {
                    switch (datatype.DataType.ToLower())
                    {
                        case "int":
                        case "bigint":
                        case "smallint":
                        case "tinyint":
                        case "float":
                        case "decimal":
                            fieldTypes.Add(elem, ContentFields.FieldType.number);
                            break;
                        case "bit":
                            fieldTypes.Add(elem, ContentFields.FieldType.block);
                            break;
                        case "datatime2":
                            fieldTypes.Add(elem, ContentFields.FieldType.datetime);
                            break;
                    }
                }
            }

            var result = Common.Platform.ContentFields.RenderForm(this, "", view, language, container, fields, exclude?.ToArray(), fieldTypes);

            if (string.IsNullOrWhiteSpace(result))
            {
                var nofields = new View("/Views/ContentFields/nofields.html");
                nofields["filename"] = paths[paths.Length - 1];
                return Response(nofields.Render());
            }


            //render ID field as a read-only field
            var fieldReadonly = new View("/Views/ContentFields/readonly.html");
            fieldReadonly["title"] = "ID";
            fieldReadonly["content"] = fields["Id"];
            result = fieldReadonly.Render() + result;

            if (showlang == true)
            {
                var viewlang = new View("/Views/ContentFields/showlang.html");
                viewlang["language"] = App.Languages.Where(a => a.Key == language).First().Value;
                result = viewlang.Render() + result;
            }

            //add hidden fields to result for all child columns found in relationships
            var html = new StringBuilder();
            if(parentId > 0)
            {
                foreach (var item in parents)
                {
                    html.Append("<input type=\"hidden\" id=\"" + Common.Platform.ContentFields.GetFieldId(item.childKey) + "\" class=\"input-field\" value=\"" + recordId + "\"/>\n");
                }
            }

            return Response(result + html.ToString());
        }
        #endregion

        #region "Helpers"

        private bool IsOwner(int datasetId, out Query.Models.DataSet dataset)
        {
            dataset = Query.DataSets.GetInfo(datasetId);
            if (User.IsAdmin == true || (dataset.userId.HasValue && dataset.userId == User.UserId))
            {
                return true;
            }
            return false;
        }

        private string ConvertFieldToString(object item)
        {
            if(item == null) { return ""; }
            var value = "";
            var type = item.GetType();
            if (type.Name == "String" || type == typeof(Int32) || type == typeof(decimal) || type == typeof(float))
            {
                value = item.ToString();
            }
            else if (type.FullName.Contains("DateTime"))
            {
                if (item != null)
                {
                    value = ((DateTime)item).ToString("MM/dd/yyyy hh:mm:ss tt");
                }
            }
            else if (type.Name == "Boolean")
            {
                value = (bool)item == true ? "True" : "False";
            }
            return value;
        }

        #endregion
    }
}
