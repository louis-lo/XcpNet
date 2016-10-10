using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws.Management;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using M = Cnaws.Product.Modules;
using U = Cnaws.Passport.Modules;

namespace XcpNet.Admin.Management
{
    public class ProductApproved : ManagementController
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

        public virtual string Path
        {
            get { return "productapproved"; }
        }
        public virtual int ProductType
        {
            get { return 1; }
        }

        protected virtual object GetCategories()
        {
            return M.ProductCategory.GetAll(DataSource, -1);
        }
        protected virtual object GetList(int page)
        {
            long count;
            IList<dynamic> list = Db<M.Product>.Query(DataSource)
                .Select(
                    new DbSelectAs<M.Product>("Id"), 
                    new DbSelectAs<U.Member>("Name"), 
                    new DbSelectAs<M.Supplier>("Company"),
                    new DbSelectAs<M.Product>("CategoryId"),
                    new DbSelectAs<M.Product>("Title"),
                    new DbSelectAs<M.Product>("Image"),
                    new DbSelectAs<M.Product>("CostPrice"),
                    new DbSelectAs<M.Product>("CountyPrice"),
                    new DbSelectAs<M.Product>("DotPrice"),
                    new DbSelectAs<M.Product>("Price"),
                    new DbSelectAs<M.Product>("Inventory"),
                    new DbSelectAs<M.Product>("BarCode"),

                    new DbSelect<M.Product>("SortNum"),
                    new DbSelect<M.Product>("Id")
                )
                .LeftJoin(new DbColumn<M.Product>("SupplierId"), new DbColumn<U.Member>("Id"))
                .LeftJoin(new DbColumn<M.Product>("SupplierId"), new DbColumn<M.Supplier>("UserId"))
                .Where(new DbWhere<M.Product>("State", M.ProductState.BeforeSale) & new DbWhere<M.Product>("ProductType", ProductType))
                .OrderBy(new DbOrderBy<M.Product>("SortNum", DbOrderByType.Desc), new DbOrderBy<M.Product>("Id", DbOrderByType.Desc))
                .ToList(10, page, out count);
            foreach (dynamic item in list)
                item.Mappings = M.ProductMapping.GetAllByProduct(DataSource, item.Id);
            return new SplitPageData<dynamic>(page, 10, list, count, 11);
        }
        protected virtual DataStatus OnApproved()
        {
            M.Product value = DbTable.Load<M.Product>(Request.Form);
            value.State = M.ProductState.Sale;
            value.SaleTime = DateTime.Now;
            return value.Update(DataSource, ColumnMode.Include, "CostPrice", "CountyPrice", "DotPrice", "Price", "State", "SaleTime");
        }

        public void Index()
        {
            if (CheckRight())
            {
                if (CheckPost("productapproved", () =>
                {
                    this["AllCategory"] = GetCategories();
                }))
                    NotFound();
            }
        }
        public void List(int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    SetResult(GetList(page));
                }
            }
        }
        public void Approved()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    SetResult(OnApproved(), () =>
                    {
                        WritePostLog("APP");
                    });
                }
            }
        }
    }
}
