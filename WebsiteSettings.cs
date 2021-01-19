using System.IO;
using System.Text.Json;
using Saber.Core;
using Saber.Vendor;

namespace Saber.Vendors.DataSets
{
    public class WebsiteSettings : IVendorWebsiteSettings
    {
        public string Name { get; set; } = "Data Sets";
        public string Render(IRequest request)
        {
            if (!request.CheckSecurity("data-sets")) { return ""; }
            var view = new View(App.MapPath("/Vendors/DataSets/websitesettings.html"));
            request.AddScript("/editor/vendors/datasets/websitesettings.js");
            return view.Render();
        }
    }
}
