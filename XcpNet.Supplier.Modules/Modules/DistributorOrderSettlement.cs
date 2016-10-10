using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Data;
using Cnaws.Web;
using Cnaws.Product.Modules;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    public enum EProductType
    {
        /// <summary>
        /// 常规
        /// </summary>
        Routine = 1,
        /// <summary>
        /// 团购
        /// </summary>
        GroupBuy,
        /// <summary>
        /// 抢购
        /// </summary>
        PanicBuying,
        /// <summary>
        /// 批发
        /// </summary>
        Wholesale,
        /// <summary>
        /// 进货
        /// </summary>
        Purchase

    }
    public class DistributorOrderSettlement : ProductOrderSettlement
    {
        public new static IList<DistributorOrderSettlement> GetListbyOrderId(DataSource ds, string orderid)
        {
            return Db<DistributorOrderSettlement>.Query(ds).Select().Where(W("OrderId", orderid)).ToList<DistributorOrderSettlement>();
        }

        public new static DistributorOrderSettlement GetbyId(DataSource ds, string orderid, long productid)
        {
            return Db<DistributorOrderSettlement>.Query(ds).Select().Where(W("OrderId", orderid) & W("ProductId", productid)).First<DistributorOrderSettlement>();
        }
    }
}
