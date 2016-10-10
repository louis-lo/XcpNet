using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Data.Query;
using Cnaws;

namespace XcpNet.Admin.Management
{
    public sealed class ProductStatistics : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Admin"; }
        }

        public void Index()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (CheckPost("productstatistics", () =>
                     {
                         DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, 0);
                         this["Sum4"] = Db<P.Product>.Query(DataSource)
                             .Select()
                             .Where(new DbWhere("ParentId",0)&new DbWhere("CreationDate", now, DbWhereType.GreaterThanOrEqual)& new DbWhere("CreationDate", now.AddDays(1), DbWhereType.LessThan))
                             .Count();

                         this["Sum3"] = Db<P.Product>.Query(DataSource)
                             .Select()
                             .Where(new DbWhere("ParentId", 0))
                             .Count();
                         this["Sum2"] = Db<P.Product>.Query(DataSource)
                            .Select()
                            .Where(new DbWhere("ParentId", 0) & new DbWhere<P.Product>("State", P.ProductState.Saved))
                            .Count();
                         this["Sum1"] = Db<P.Product>.Query(DataSource)
                            .Select()
                            .Where(new DbWhere("ParentId", 0) & new DbWhere<P.Product>("State", P.ProductState.Sale))
                            .Count();
                     }))
                    {
                        NotFound();
                    }
                }
            }
        }
    }
}
