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

        public string GetPermissions()
        {
            return (CheckSecurity("create-datasets") ? "1" : "0") + "," +
                (CheckSecurity("edit-datasets") ? "1" : "0") + "," +
                (CheckSecurity("delete-datasets") ? "1" : "0") + "," +
                (CheckSecurity("view-datasets") ? "1" : "0") + "," +
                (CheckSecurity("add-dataset-data") ? "1" : "0");
        }

        public string GetCreateForm()
        {
            if (!CheckSecurity("create-datasets")) { return AccessDenied(); }
            return Cache.LoadFile("/Vendors/DataSets/create.html");
        }

        public string Create(string name, string partial)
        {
            if (!CheckSecurity("create-datasets")) { return AccessDenied(); }
            return Success();
        }
    }
}
