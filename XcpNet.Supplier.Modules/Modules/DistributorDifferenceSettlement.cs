using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Data;
using Cnaws.Web;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    public class DistributorDifferenceSettlement : Cnaws.Product.Modules.DifferenceSettlement
    {

        public new static IList<DistributorDifferenceSettlement> GetListbyOrderId(DataSource ds, string orderid)
        {
            return Db<DistributorDifferenceSettlement>.Query(ds).Select().Where(W("OrderId", orderid)).ToList<DistributorDifferenceSettlement>();
        }

        public new static DistributorDifferenceSettlement GetbyId(DataSource ds, string orderid, long productid)
        {
            return Db<DistributorDifferenceSettlement>.Query(ds).Select().Where(W("OrderId", orderid) & W("ProductId", productid)).First<DistributorDifferenceSettlement>();
        }
    }
}
