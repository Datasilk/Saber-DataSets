using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Saber.Core;
using Saber.Vendor;

namespace Saber.Vendors.DataSets
{
    public class DataSets : Service, IVendorService
    {
        public string GetList(string search)
        {
            try
            {
                return JsonResponse("");
            }
            catch (Exception)
            {
                return Error("Could not retrieve list of Data Sets" + (!string.IsNullOrEmpty(search) ? " using search \"" + search + "\"." : ""));
            }
        }

        public string GetCreateForm()
        {
            if (!CheckSecurity()) { return AccessDenied(); }
            return Cache.LoadFile("/Vendors/DataSets/create.html");
        }
    }
}
