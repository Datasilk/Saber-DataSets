using System;
using System.Xml.Serialization;

namespace Query.Models
{
    public class DataSet
    {
        public int datasetId { get; set; }
        public int? userId { get; set; }
        public string label { get; set; }
        public string tableName { get; set; }
        public string partialview { get; set; }
        public string description { get; set; }
        public DateTime datecreated { get; set; }
        public bool userdata { get; set; }
        public bool deleted { get; set; }
    }
}

namespace Query.Models.DataSets
{
    [Serializable]
    [XmlRoot("columns")]
    public class Columns
    {
        [XmlElement("column")]
        public Column[] Items { get; set; }
    }
    public class Column
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("datatype")]
        public string DataType { get; set; }
        [XmlAttribute("maxlength")]
        public string MaxLength { get; set; }
        [XmlAttribute("default")]
        public string DefaultValue { get; set; }
        [XmlAttribute("dataset")]
        public string Dataset { get; set; }
        [XmlAttribute("columnname")]
        public string ColumnName { get; set; }
        [XmlAttribute("listtype")]
        public string ListType { get; set; }
    }

    public class ColumnName
    {
        public string Name { get; set; }
        public int DataSetId { get; set; }
    }

    public enum DataType
    {
        text = 0,
        number = 1,
        number_decimal = 2,
        bit = 3,
        datetime = 4
    }

    [Serializable]
    [XmlRoot("fields")]
    public class Fields
    {
        [XmlElement("field")]
        public Field[] Items { get; set; }
    }

    public class Field
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("value")]
        public string Value { get; set; }

        [XmlIgnore]
        public DataType DataType { get; set; }
    }
}
