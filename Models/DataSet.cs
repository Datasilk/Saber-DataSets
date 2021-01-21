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
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("datatype")]
        public string DataType { get; set; }
    }

    public enum DataType
    {
        text = 0,
        number = 1,
        number_float = 2,
        bit = 3,
        datetime = 4
    }

    public class Row
    {
        public string Name { get; set; }
        public DataType DataType { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }
        public int? ToNumber()
        {
            int.TryParse(Value, out var number);
            return number;
        }
        public decimal? ToFloat()
        {
            decimal.TryParse(Value, out var number);
            return number;
        }
        
        public DateTime? ToDateTime()
        {
            DateTime.TryParse(Value, out var date);
            return date;
        }
    }
}
