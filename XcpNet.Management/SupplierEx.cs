using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;

namespace XcpNet.Management
{
    public sealed class SupplierEx : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Management"; }
        }

        public void Index()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (CheckPost("supplierex"))
                        NotFound();
                }
            }
        }
    }
}
