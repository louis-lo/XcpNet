using System;
using A = XcpNet.AfterSales.Modules;
using P = XcpNet.Supplier.Modules.Modules;

namespace XcpNet.Admin.Management
{
    public class DistributorAfterSales : AfterSales
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        public override string Path
        {
            get { return "distributoraftersales"; }
        }
        public override A.AfterSalesRecord.EChannel Channel
        {
            get { return A.AfterSalesRecord.EChannel.WholesaleOrder; }
        }

        protected override object GetOrder(string orderId)
        {
            return A.AfterSalesRecord.GetAndDistributorProductById(DataSource, orderId);
        }
        protected override object GetOrderMapping(object[] args)
        {
            return P.DistributorOrderMapping.GetById(DataSource, Convert.ToString(args[0]), Convert.ToInt64(args[1]));
        }
    }
}
