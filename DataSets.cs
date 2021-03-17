using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Saber.Core;
using Saber.Vendor;

namespace Saber.Vendors.DataSets
{
    public class DataSets : Service, IVendorService
    {
        #region "Data Sets"
        public string GetList(bool owned = true, bool all = true, string search = "")
        {
            if (!CheckSecurity("view-datasets")) { return AccessDenied(); }
            try
            {
                var datasets = Query.DataSets.GetList(owned ? User.UserId : null, all, search);
                return JsonResponse(datasets.Select(a => new { a.datasetId, a.label, a.partialview, a.description }));
            }
            catch (Exception)
            {
                return Error("Could not retrieve list of Data Sets" + (!string.IsNullOrEmpty(search) ? " using search \"" + search + "\"." : ""));
            }
        }

        public string GetPermissions()
        {
            return (CheckSecurity("create-datasets") ? "1" : "0") + "," +
                (CheckSecurity("edit-datasets") ? "1" : "0") + "," +
                (CheckSecurity("delete-datasets") ? "1" : "0") + "," +
                (CheckSecurity("view-datasets") ? "1" : "0") + "," +
                (CheckSecurity("add-dataset-data") ? "1" : "0");
        }

        public string GetCreateForm()
        {
            if (!CheckSecurity("create-datasets")) { return AccessDenied(); }
            return Cache.LoadFile("/Vendors/DataSets/create.html");
        }

        public string GetUpdateInfoForm()
        {
            if (!CheckSecurity("edit-datasets")) { return AccessDenied(); }
            return Cache.LoadFile("/Vendors/DataSets/update-info.html");
        }

        public string LoadColumns(string partial)
        {
            if (!CheckSecurity("create-datasets")) { return AccessDenied(); }
            //generate a form based on all the mustache variables & mustache components in the partial view.
            //add extra forms for each List component in order to create sub-data sets.
            var html = new StringBuilder();
            var view = new View("/Content/" + partial);
            var viewColumn = new View("/Vendors/DataSets/column-field.html");
            var cols = 0;
            for(var x = 0; x < view.Elements.Count; x++)
            {
                var elem = view.Elements[x];
                if(elem.Name == "") { continue; }
                if(Core.Vendors.HtmlComponentKeys.Any(a => elem.Name.IndexOf(a) == 0)) { continue; }
                if(elem.Name.Substring(0, 1) == "/") { continue; }
                viewColumn.Clear();
                viewColumn["id"] = elem.Name.ToLower();
                viewColumn["name"] = elem.Name;
                if (elem.isBlock)
                {
                    viewColumn.Show("datatype-bit");
                    viewColumn["id"] = elem.Name.ToLower();
                    viewColumn["name"] = elem.Name;
                    viewColumn.Show("default-bit");
                }
                else
                {
                    var datatype = ContentFields.GetFieldType(view, x);
                    if(datatype == ContentFields.FieldType.image)
                    {
                        viewColumn.Show("datatype-image");
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

            foreach(var elem in view.Elements.Where(a => !a.isBlock && a.Name.StartsWith("list")))
            {
                //TODO: get all views from list components and include them in the form
            }

            if(cols == 0)
            {
                return Error("No mustache variables could be found within the selected partial view.");
            }

            var viewColumns = new View("/Vendors/DataSets/columns.html");
            viewColumns["summary"] = "Dataset columns were generated based on the partial view you selected. Please make any data type changes to your columns before continuing.";
            viewColumns["save-button"] = "Create Dataset";
            viewColumns["columns"] = html.ToString();

            return viewColumns.Render();
        }

        public string Create(string name, string partial, string description, List<Query.Models.DataSets.Column> columns, bool isprivate = false)
        {
            if (!CheckSecurity("create-datasets")) { return AccessDenied(); }
            if(columns == null || columns.Count <= 0 || columns[0].Name == null || columns[0].Name == "") { return Error("No columns were defined"); }
            try
            {
                var id = Query.DataSets.Create(name, partial, description, columns, isprivate == true ? User.UserId : null);
                return id > 0 ? id.ToString() : Error("An error occurred when trying to create a new data set");
            }
            catch(Exception ex)
            {
                return Error(ex.Message);
            }
        }

        public string UpdateInfo(int datasetId, string name, string description)
        {
            if (!CheckSecurity("edit-datasets")) { return AccessDenied(); }
            if(!IsOwner(datasetId)){ return AccessDenied("You do not own this dataset"); }
            Query.DataSets.UpdateInfo(datasetId, name, description);
            return Success();
        }

        public string Details(int datasetId, string lang, string search, int start = 1, int length = 50, int searchType = 0)
        {
            if (!CheckSecurity("view-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId)) { return AccessDenied("You do not own this dataset"); }
            var orderby = "";
            var data = Query.DataSets.GetRecords(datasetId, start, length, lang, search, (Query.DataSets.SearchType)searchType, orderby);
            var view = new View("/Vendors/DataSets/dataset.html");
            var viewMenu = new View("/Vendors/DataSets/record-menu.html");
            var header = new StringBuilder();
            var rows = new StringBuilder();
            if(data.Count > 0)
            {
                var i = 0;
                foreach (var item in data.First())
                {
                    //load dataset column names in header row
                    i++;
                    if( i <= 4 || (i == 5 && !User.IsAdmin)) 
                    {
                        //skip username, useremail, ID, lang, & userId columns
                        continue; 
                    }else if((i == 5 && User.IsAdmin))
                    {
                        header.Append("<td><b>Owner</b></td>");
                    }
                    else
                    {
                        header.Append("<td>" + item.Key + "</td>");
                    }
                }
                view["table-head"] = header.ToString();
                foreach (var item in data)
                {
                    //load column values for each dataset record
                    var recordId = ConvertFieldToString(item["Id"]);
                    viewMenu["recordId"] = recordId;
                    rows.Append("<tr data-id=\"" + recordId + "\">");
                    i = 0;
                    viewMenu.Clear();
                    var username = item["username"].ToString();
                    var useremail = item["useremail"].ToString();
                    foreach (var col in item)
                    {
                        i++;
                        if (i <= 4) {
                            //skip username, recordId, & lang columns
                            continue; 
                        } 
                        if(i == 5 && User.IsAdmin == true)
                        {
                            //display user information to Admin
                            rows.Append("<td class=\"no-details\"><a href=\"javascript:\" onclick=\"S.editor.datasets.viewOwner(event, " + col.Value + ",'" + useremail + "')\">" + username + "</a></td>");
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

        public string Delete(int datasetId)
        {
            if (!CheckSecurity("delete-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId)) { return AccessDenied("You do not own this dataset"); }
            try
            {
                Query.DataSets.Delete(datasetId);
                return Success();
            }
            catch (Exception)
            {
                return Error("Could not delete Data Set");
            }
        }
        #endregion

        #region "Records"
        public string LoadNewRecordForm(int datasetId)
        {
            if (!CheckSecurity("view-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId)) { return AccessDenied("You do not own this dataset"); }
            var details = Query.DataSets.GetInfo(datasetId);
            var view = new View("/partials/" + details.partialview);
            return ContentFields.RenderForm(this, details.label, view, User.Language, ".popup.new-record-for-" + datasetId, new Dictionary<string, string>());
        }

        public string CreateRecord(int datasetId, string lang, Dictionary<string, string> fields, int recordId = 0)
        {
            if (!CheckSecurity("add-dataset-data")) { return AccessDenied(); }
            if (!IsOwner(datasetId)) { return AccessDenied("You do not own this dataset"); }
            if (fields.Count == 0)
            {
                return Error("No fields were included when trying to create a new record");
            }
            Query.DataSets.AddRecord(User.UserId, datasetId, lang, fields.Select(a => new Query.Models.DataSets.Field() { Name = a.Key, Value = a.Value }).ToList(), recordId);
            return Success();
        }

        public string GetRecord(int datasetId, int recordId, string lang)
        {
            if (!CheckSecurity("view-datasets")) { return AccessDenied(); }
            if (!IsOwner(datasetId)) { return AccessDenied("You do not own this dataset"); }
            var record = Query.DataSets.GetRecords(datasetId, 1, 1, lang, "", Query.DataSets.SearchType.any, "", 0, recordId).FirstOrDefault();
            var fields = new Dictionary<string, string>();
            if(record == null && lang != "en")
            {
                //try to get record in English if one doesn't exist in the selected language
                record = Query.DataSets.GetRecords(datasetId, 1, 1, "en", "", Query.DataSets.SearchType.any, "", 0, recordId).FirstOrDefault();
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
            if (!CheckSecurity("add-dataset-data")) { return AccessDenied(); }
            if (!IsOwner(datasetId)) { return AccessDenied("You do not own this dataset"); }
            if (fields.Count == 0)
            {
                return Error("No fields were included when trying to update an existing record");
            }
            Query.DataSets.UpdateRecord(datasetId, recordId, lang, fields.Select(a => new Query.Models.DataSets.Field() { Name = a.Key, Value = a.Value }).ToList());
            return Success();
        }
        #endregion

        #region "Helpers"

        private bool IsOwner(int datasetId)
        {
            var dataset = Query.DataSets.GetInfo(datasetId);
            if (User.IsAdmin == true || (dataset.userId.HasValue && dataset.userId == User.UserId))
            {
                return true;
            }
            return false;
        }

        private string ConvertFieldToString(object item)
        {
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
