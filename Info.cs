using Saber.Vendor;
namespace Saber.Vendors.DataSets
{
    public class Info : IVendorInfo
    {
        public string Key { get; set; } = "DataSets";
        public string Name { get; set; } = "Data Sets";
        public string Description { get; set; } = "Create data sets that act like database tables by converting a partial view's mustache code into table columns.";
        public string Icon { get; set; }
        public Version Version { get; set; } = "1.0.0.0";
    }
}
