using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Web;
using Saber.Core;
using Saber.Vendor;

namespace Saber.Vendors.DataSets
{
    public class DataSets : Service, IVendorService
    {
        public string GetList(string search)
        {
            try
            {
                return JsonResponse("");
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

        public string LoadColumns(string partial)
        {
            if (!CheckSecurity("create-datasets")) { return AccessDenied(); }
            //generate a form based on all the mustache variables & mustache components in the partial view.
            //add extra forms for each List component in order to create sub-data sets.
            var html = new StringBuilder();
            var view = new View("/Content/" + partial);
            var viewColumn = new View("/Vendors/DataSets/column-field.html");
            for(var x = 0; x < view.Elements.Count; x++)
            {
                var elem = view.Elements[x];
                if(elem.Name == "") { continue; }
                if(Common.Vendors.HtmlComponentKeys.Any(a => elem.Name.IndexOf(a) == 0)) { continue; }
                if(elem.Htm.Substring(0, 1) == "/") { continue; }
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
            }

            foreach(var elem in view.Elements.Where(a => !a.isBlock && a.Name.StartsWith("list")))
            {
                //TODO: get all views from list components and include them in the form
            }

            var viewColumns = new View("/Vendors/DataSets/columns.html");
            viewColumns["summary"] = "Dataset columns were generated based on the partial view you selected. Please make any data type changes to your columns before continuing.";
            viewColumns["save-button"] = "Create Dataset";
            viewColumns["columns"] = html.ToString();

            return viewColumns.Render();
        }

        public string Create(string name, string description, List<Query.Models.DataSets.Column> columns)
        {
            if (!CheckSecurity("create-datasets")) { return AccessDenied(); }
            if(columns == null || columns.Count <= 0 || columns[0].Name == null || columns[0].Name == "") { return Error("No columns were defined"); }
            var id = Query.DataSets.Create(name, description, columns);
            return id > 0 ? id.ToString() : Error("An error occurred when trying to create a new data set");
        }

        public string Details(int datasetId)
        {
            if (!CheckSecurity("view-datasets")) { return AccessDenied(); }
            var data = Query.DataSets.GetRecords(datasetId);
            var view = new View("/Vendors/DataSets/dataset.html");
            var header = new StringBuilder();
            var rows = new StringBuilder();
            if(data.Count > 0)
            {
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(data.First()))
                {
                    header.Append("<td>" + property.Name);
                }
                view["table-head"] = header.ToString();
                foreach (var item in data)
                {
                    rows.Append("<tr>");
                    foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(item))
                    {
                        rows.Append("<td>" + property.GetValue(item) + "</td>");
                    }
                    rows.Append("</tr>");
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
    }
}
