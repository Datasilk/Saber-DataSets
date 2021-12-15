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
        public string Description { get; set; } = "Create database tables, columns of various data types, and rows of data from within Saber's Editor, then use your tables as data sources.";

        public void Init()
        {}

        public List<KeyValuePair<string, string>> List()
        {
            //get list of available Data Sets as data sources
            var datasets = Query.DataSets.GetList(null, true, true);
            return datasets.Select(a => new KeyValuePair<string, string>(a.datasetId.ToString(), a.label)).ToList();
        }

        public DataSource Get(string key)
        {
            //get information about a data set as a data source
            return Cache.DataSources.Where(a => a.Key == key).FirstOrDefault();
        }

        public void Create(IRequest request, string key, Dictionary<string, string> columns)
        {
            var datasetId = int.Parse(key);
            var lang = columns.ContainsKey("lang") ? columns["lang"] : request.User.Language;
            Query.DataSets.AddRecord(request.User.UserId, datasetId, lang, columns.Where(a => !DataSets.ExcludedFields.Contains(a.Key))
                .Select(a => new Query.Models.DataSets.Field() { Name = a.Key, Value = a.Value }).ToList());
        }

        public void Update(IRequest request, string key, string id, Dictionary<string, string> columns)
        {
            var datasetId = int.Parse(key);
            var lang = columns.ContainsKey("lang") ? columns["lang"] : request.User.Language;
            Query.DataSets.UpdateRecord(request.User.UserId, datasetId, int.Parse(id), lang, columns.Where(a => !DataSets.ExcludedFields.Contains(a.Key))
                .Select(a => new Query.Models.DataSets.Field() { Name = a.Key, Value = a.Value }).ToList());
        }

        public List<Dictionary<string, string>> Filter(IRequest request, string key, int start, int length, string lang = "en", List<DataSource.FilterGroup> filters = null, List<DataSource.OrderBy> orderBy = null)
        {
            //get filtered records from a data set
            var datasetId = int.Parse(key);
            return Query.DataSets.GetRecords(datasetId, start, length, lang, request.User.UserId, filters, orderBy)?
                .Select(a => a.ToDictionary(k => k.Key, v => v.Value == null ? "" : v.Value.ToString())).ToList();
        }

        public Dictionary<string, List<Dictionary<string, string>>> Filter(IRequest request, string key, string lang = "en", Dictionary<string, DataSource.PositionSettings> positions = null,  Dictionary<string, List<DataSource.FilterGroup>> filters = null, Dictionary<string, List<DataSource.OrderBy>> orderBy = null, string[] childKeys = null)
        {
            var datasetId = int.Parse(key);
            return Query.DataSets.GetRecordsInRelationships(datasetId, lang, request.User.UserId, positions, filters, orderBy, childKeys)?
                .ToDictionary(a => a.Key, a =>
                {
                    return new List<Dictionary<string, string>>(a.Value.Select(b =>
                    {
                        var c = new Dictionary<string, string>();
                        foreach(var k in b)
                        {
                            c.Add(k.Key, k.Value.ToString());
                        }
                        return c;
                    }).ToList());
                });
        }
    }
}
