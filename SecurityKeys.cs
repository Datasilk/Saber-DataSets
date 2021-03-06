﻿using Saber.Core;
using Saber.Vendor;

namespace Saber.Vendors.DataSets
{
    public class SecurityKeys : IVendorKeys
    {
        public string Vendor { get; set; } = "Data Sets";
        public SecurityKey[] Keys { get; set; } = new SecurityKey[]
        {
            new SecurityKey(){Value = "create-datasets", Label = "Create Data Sets", Description = "Able to create new data sets for the website"},
            new SecurityKey(){Value = "private-datasets", Label = "Manage Private Data Sets", Description = "Able to create, update, & delete data sets that can only be used & managed by the user who created them"},
            new SecurityKey(){Value = "edit-datasets", Label = "Edit Data Sets", Description = "Able to edit public data set information and update columns based on associated partial view"},
            new SecurityKey(){Value = "delete-datasets", Label = "Delete Data Sets", Description = "Able to delete existing public data sets"},
            new SecurityKey(){Value = "view-datasets", Label = "View Data Sets", Description = "Able to view a list of data sets and view their data"},
            new SecurityKey(){Value = "add-dataset-data", Label = "Add/Import Data", Description = "Able to add or import new records to data sets"},
            new SecurityKey(){Value = "edit-dataset-data", Label = "Edit Data", Description = "Able to edit existing data set records"}
        };
    }
}
