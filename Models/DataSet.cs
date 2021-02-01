using System;
using System.Xml.Serialization;

namespace Query.Models
{
    public class DataSet
    {

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
    }

    public enum DataType
    {
        text = 0,
        number = 1,
        number_decimal = 2,
        bit = 3,
        datetime = 4
    }
}
