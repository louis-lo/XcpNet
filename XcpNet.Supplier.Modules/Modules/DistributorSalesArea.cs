using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Data.Query;
using System.Collections.Generic;

namespace XcpNet.Supplier.Modules.Modules
{
    public sealed class DistributorSalesArea : Cnaws.Product.Modules.ProductSalesArea
    {
        public new static IList<DistributorSalesArea> GetById(DataSource ds, long productId)
        {
            return Db<DistributorSalesArea>.Query(ds).Select().Where(W("ProductId", productId)).ToList<DistributorSalesArea>();
        }
        public new static DataStatus DelById(DataSource ds, long productId)
        {
            if (Db<DistributorSalesArea>.Query(ds).Delete().Where(W("ProductId", productId)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }
    }
}
