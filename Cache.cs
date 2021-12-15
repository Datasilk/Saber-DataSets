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
                    var relationships = Query.DataSets.Relationships.GetAll();
                    if(relationships == null) { relationships = new List<Query.Models.DatasetRelationship>(); }
                    if (datasets != null)
                    {
                        foreach(var dataset in datasets)
                        {
                            var key = dataset.datasetId.ToString();
                            _dataSources.Add(new DataSource()
                            {
                                Key = key,
                                Name = dataset.label,
                                Columns = columns.Where(a => a.DataSetId == dataset.datasetId).Select(a => new DataSource.Column() { Name = a.Name }).ToArray(),
                                Relationships = relationships.Where(a => a.parentId == dataset.datasetId).Select(a => new DataSource.Relationship()
                                {
                                    Key = key,
                                    ChildKey = "dataset-" + a.childId.ToString(),
                                    Child = null,
                                    ChildColumn = a.childColumn,
                                    ListComponent = a.parentList,
                                    ParentTable = a.parentTableName
                                }).ToArray()
                            });
                        }
                        foreach(var datasource in _dataSources)
                        {
                            if(datasource.Relationships.Length > 0)
                            {
                                foreach(var relationship in datasource.Relationships)
                                {
                                    relationship.Child = _dataSources.Where(a => a.Key == relationship.ChildKey.Replace("dataset-", "")).FirstOrDefault();
                                }
                            }
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
