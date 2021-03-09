using System;
using System.Collections.Generic;
using System.Linq;
using Saber.Core;
using Saber.Vendor;

namespace Saber.Vendors.DataSets
{
    public class DataSources : IVendorDataSources
    {
        //Register data sources with Saber that can be used with the List component

        public string Vendor { get; set; } = "Data Set";
        public string Prefix { get; set; } = "dataset";

        public List<KeyValuePair<string, string>> List()
        {
            //get list of available Data Sets as data sources
            var datasets = Query.DataSets.GetList();
            return datasets.Select(a => new KeyValuePair<string, string>(a.datasetId.ToString(), a.label)).ToList();
        }

        public DataSource Get(string name)
        {
            //get information about a data set as a data source
            throw new NotImplementedException();
        }

        public List<Dictionary<string, string>> Filter(string key, int start, int length, string lang = "en", Dictionary<string, object> filter = null)
        {
            //get filtered records from a data set
            var search = filter != null && filter.ContainsKey("search") ? filter["search"].ToString() : "";
            var match = filter != null && filter.ContainsKey("match") ? int.Parse(filter["match"].ToString()) : 0;
            var orderby = filter != null && filter.ContainsKey("orderby") ? filter["orderby"].ToString() : "";
            var recordId = filter != null && filter.ContainsKey("recordId") ? int.Parse(filter["recordId"].ToString()) : 0;
            return Query.DataSets.GetRecords(int.Parse(key), start, length, lang, search, (Query.DataSets.SearchType)match, orderby, recordId)?
                .Select(a => a.ToDictionary(k => k.Key, v => v.Value == null ? "" : v.Value.ToString())).ToList();
        }

        public DataSourceFilterForm RenderFilters(string name, IRequest request, Dictionary<string, object> filter = null)
        {
            //render an HTML partial to display a set of filters that can be applied to our data set query results
            var view = new View("/Vendors/DataSets/datasource-filter.html");
            //request.AddScript("/editor/vendors/datasets/datasource-filter.js", "datasets-datasource-filterjs", "S.datasets.datasources.initFilters()");
            if(filter != null && filter.Count > 0)
            {
                //populate filter values (if applicable)
            }
            return new DataSourceFilterForm()
            {
                HTML = view.Render(),
                OnInit = "S.editor.datasets.contentfields.filter.init"
            };
        }
    }
}
