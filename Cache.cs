using System.Collections.Generic;
using System.Linq;
using Saber.Vendor;

namespace Saber.Vendors.DataSets
{
    public static class Cache
    {
        private static Dictionary<int, DataSource> _dataSources { get; set; }
        private static Dictionary<int, Query.Models.DataSet> _dataSets { get; set; }

        public static Dictionary<int, DataSource> DataSources
        {
            get {
                if(_dataSources == null)
                {
                    _dataSources = new Dictionary<int, DataSource>();
                    var datasets = Query.DataSets.GetList();
                    var columns = Query.DataSets.GetAllColumns();
                    var relationships = Query.DataSets.Relationships.GetAll();
                    if(relationships == null) { relationships = new List<Query.Models.DatasetRelationship>(); }
                    if (datasets != null)
                    {
                        foreach(var dataset in datasets)
                        {
                            var key = dataset.datasetId.ToString();
                            _dataSources.Add(dataset.datasetId, new DataSource()
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
                                    ParentTable = a.parentTableName,
                                    Type = (DataSource.RelationshipType)a.listType
                                }).ToArray()
                            });
                        }
                        foreach(var datasource in _dataSources)
                        {
                            if(datasource.Value.Relationships.Length > 0)
                            {
                                foreach(var relationship in datasource.Value.Relationships)
                                {
                                    relationship.Child = _dataSources[int.Parse(relationship.ChildKey.Replace("dataset-", ""))];
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

        public static Dictionary<int, Query.Models.DataSet> DataSets
        {
            get
            {
                if (_dataSets == null)
                {
                    var list = Query.DataSets.GetList();
                    _dataSets = new Dictionary<int, Query.Models.DataSet>();
                    foreach (var item in list)
                    {
                        _dataSets.Add(item.datasetId, item);
                    }
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
