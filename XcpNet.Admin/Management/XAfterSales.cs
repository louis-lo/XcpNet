using System;
using Cnaws.Management;
using P = Cnaws.Product.Modules;
using A = XcpNet.AfterSales.Modules;
using Cnaws.Web.Templates;

namespace XcpNet.Admin.Management
{
    public class XAfterSales : AfterSales
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        public override string Path
        {
            get { return "xaftersales"; }
        }
        public override A.AfterSalesRecord.EChannel Channel
        {
            get { return A.AfterSalesRecord.EChannel.AgricultureOrder; }
        }
    }
}
