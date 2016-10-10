using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Passport.Modules;
using Cnaws.Data.Query;
using Cnaws;

namespace XcpNet.Admin.Management
{
    public sealed class Statistics : ManagementController
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
                    if (CheckPost("statistics", () =>
                     {
                         DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, 0);
                         this["Sum0"] = Db<M.MoneyRecord>.Query(DataSource)
                             .Select(new DbSelectSum("Value"))
                             .Where(new DbWhere("CreationDate", now, DbWhereType.GreaterThanOrEqual) & new DbWhere("Value", 0, DbWhereType.LessThan) & new DbWhere("Type", 0))
                             .Single<Money>().Abs();
                         this["Sum1"] = Db<M.MoneyRecord>.Query(DataSource)
                             .Select(new DbSelectSum("Value"))
                             .Where(new DbWhere("CreationDate", now, DbWhereType.GreaterThanOrEqual) & new DbWhere("Value", 0, DbWhereType.LessThan) & new DbWhere("Type", 1))
                             .Single<Money>().Abs();
                         this["SupplierSum2"] = Db<M.MoneyRecord>.Query(DataSource)
                             .Select(new DbSelectSum("Value"))
                             .Where(new DbWhere("CreationDate", now, DbWhereType.GreaterThanOrEqual) & new DbWhere("Value", 0, DbWhereType.LessThan) & new DbWhere("Type", 5))
                             .Single<Money>().Abs();
                         this["Sum3"] = Db<M.MoneyRecord>.Query(DataSource)
                             .Select(new DbSelectSum("Value"))
                             .Where(new DbWhere("CreationDate", now, DbWhereType.GreaterThanOrEqual) & new DbWhere("Value", 0, DbWhereType.LessThan) & (new DbWhere("Type", 5) | new DbWhere("Type", 1)))
                             .Single<Money>().Abs();

                         this["TSum0"] = Db<M.MoneyRecord>.Query(DataSource)
                             .Select(new DbSelectSum("Value"))
                             .Where(new DbWhere("Value", 0, DbWhereType.LessThan) & new DbWhere("Type", 0))
                             .Single<Money>().Abs();
                         this["TSum1"] = Db<M.MoneyRecord>.Query(DataSource)
                             .Select(new DbSelectSum("Value"))
                             .Where(new DbWhere("Value", 0, DbWhereType.LessThan) & new DbWhere("Type", 1))
                             .Single<Money>().Abs();

                         this["Sum2"] = Db<P.Member>.Query(DataSource)
                             .Select()
                             .Where(new DbWhere("CreationDate", now, DbWhereType.GreaterThanOrEqual))
                             .Count();

                         this["TSum2"] = Db<P.Member>.Query(DataSource)
                             .Select()
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
