using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws;

namespace XcpNet.Supplier.Modules.Modules
{
    public sealed class DistributorAreaMapping : Cnaws.Product.Modules.ProductAreaMapping
    {
        public new static DistributorAreaMapping GetById(DataSource ds, long id, int province, int city, int county)
        {
            return Db<DistributorAreaMapping>.Query(ds).Select().Where(W("ProductId", id) & W("Province", province) & W("City", city) & W("County", county)).First<DistributorAreaMapping>();
        }

        public new static DataStatus ModPrice(DataSource ds, long id, int province, int city, int county, string ModField, Money price)
        {
            if (Db<DistributorAreaMapping>.Query(ds).Update().Set(ModField, price).Where(W("ProductId", id) & W("Province", province) & W("City", city) & W("County", county)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }

        public new static DataStatus ModSaled(DataSource ds, long id, int province, int city, int county, bool saled)
        {
            if (Db<DistributorAreaMapping>.Query(ds).Update().Set("Saled", saled).Where(W("ProductId", id) & W("Province", province) & W("City", city) & W("County", county)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }
    }
}
