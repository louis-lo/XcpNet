using Cnaws.Data;
using Cnaws.Data.Query;
using System;
using System.Collections.Generic;
using M = XcpNet.Supplier.Modules.Modules;
using P = Cnaws.Product.Modules;
using U = Cnaws.Passport.Modules;
using Cnaws.Web;

namespace XcpNet.Admin.Management
{
    public sealed class DistributorApproved : ProductApproved
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        public override string Path
        {
            get { return "distributorapproved"; }
        }
        public override int ProductType
        {
            get { return 0; }
        }

        protected override object GetCategories()
        {
            return M.DistributorCategory.GetAll(DataSource, -1);
        }
        protected override object GetList(int page)
        {
            long count;
            IList<dynamic> list = Db<M.DistributorProduct>.Query(DataSource)
                .Select(
                    new DbSelectAs<M.DistributorProduct>("Id"),
                    new DbSelectAs<U.Member>("Name"),
                    new DbSelectAs<P.Supplier>("Company"),
                    new DbSelectAs<M.DistributorProduct>("CategoryId"),
                    new DbSelectAs<M.DistributorProduct>("Title"),
                    new DbSelectAs<M.DistributorProduct>("Image"),
                    new DbSelectAs<M.DistributorProduct>("CostPrice"),
                    new DbSelectAs<M.DistributorProduct>("CountyPrice"),
                    new DbSelectAs<M.DistributorProduct>("DotPrice"),
                    new DbSelectAs<M.DistributorProduct>("Price"),
                    new DbSelectAs<M.DistributorProduct>("Inventory"),
                    new DbSelectAs<M.DistributorProduct>("BarCode"),

                    new DbSelect<M.DistributorProduct>("SortNum"),
                    new DbSelect<M.DistributorProduct>("Id")
                )
                .LeftJoin(new DbColumn<M.DistributorProduct>("SupplierId"), new DbColumn<U.Member>("Id"))
                .LeftJoin(new DbColumn<M.DistributorProduct>("SupplierId"), new DbColumn<P.Supplier>("UserId"))
                .Where(new DbWhere<M.DistributorProduct>("State", P.ProductState.BeforeSale))
                .OrderBy(new DbOrderBy<M.DistributorProduct>("SortNum", DbOrderByType.Desc), new DbOrderBy<M.DistributorProduct>("Id", DbOrderByType.Desc))
                .ToList(10, page, out count);
            foreach (dynamic item in list)
                item.Mappings = M.DistributorMapping.GetAllByProduct(DataSource, item.Id);
            return new SplitPageData<dynamic>(page, 10, list, count, 11);
        }
        protected override DataStatus OnApproved()
        {
            M.DistributorProduct value = DbTable.Load<M.DistributorProduct>(Request.Form);
            value.State = P.ProductState.Sale;
            value.SaleTime = DateTime.Now;
            return value.Update(DataSource, ColumnMode.Include, "CostPrice", "CountyPrice", "DotPrice", "Price", "State", "SaleTime");
        }
    }
}
