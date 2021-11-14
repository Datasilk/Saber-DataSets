using System.Collections.Generic;
using System.Linq;
using Saber.Vendor;

namespace Saber.Vendors.DataSets
{
    public static class Cache
    {
        private static List<DataSource> _dataSources { get; set; }
        private static List<Query.Models.DataSet> _dataSets { get; set; }

        public static List<DataSource> DataSources
        {
            get {
                if(_dataSources == null)
                {
                    _dataSources = new List<DataSource>();
                    var datasets = Query.DataSets.GetList();
                    var columns = Query.DataSets.GetAllColumns();
                    if (datasets != null)
                    {
                        foreach(var dataset in datasets)
                        {
                            _dataSources.Add(new DataSource()
                            {
                                Key = dataset.datasetId.ToString(),
                                Name = dataset.label,
                                Columns = columns.Where(a => a.DataSetId == dataset.datasetId).Select(a => new DataSource.Column() { Name = a.Name }).ToArray()
                            });
                        }
                    }
                }
                return _dataSources;
            }

            set
            {
                _dataSources = value;
            }
        }

        public static List<Query.Models.DataSet> DataSets
        {
            get
            {
                if (_dataSets == null)
                {
                    _dataSets = Query.DataSets.GetList();
                }
                return _dataSets;
            }

            set
            {
                _dataSets = value;
            }
        }
    }
}
