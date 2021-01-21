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

        public string GetCreateColumnsForm(string partial)
        {
            if (!CheckSecurity("create-datasets")) { return AccessDenied(); }
            //generate a form based on all the mustache variables & mustache components in the partial view.
            //add extra forms for each List component in order to create sub-data sets.
            return Success();
        }

        public string Create(string label, string description, List<Query.Models.DataSets.Column> columns)
        {
            if (!CheckSecurity("create-datasets")) { return AccessDenied(); }
            var id = Query.DataSets.Create(label, description, columns);
            return id > 0 ? id.ToString() : Error("An error occurred when trying to create a new data set");
        }

        public string Details(int datasetId)
        {
            if (!CheckSecurity("view-datasets")) { return AccessDenied(); }
            var data = Query.DataSets.GetRecords(datasetId);
            var records = new Dictionary<string, string>();
            var view = new View("/Vendors/DataSets/dataset.html");
            var header = new StringBuilder();
            var rows = new StringBuilder();
            if(data.Count > 0)
            {
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(data.First()))
                {
                    header.Append("<td>" + property.Name);
                }
                foreach (var item in data)
                {
                    rows.Append("<tr>");
                    foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(item))
                    {
                        rows.Append("<td>" + property.GetValue(item) + "</td>");
                    }
                    rows.Append("</tr>");
                }
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
