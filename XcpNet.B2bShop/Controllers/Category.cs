using System;
using System.Collections.Generic;
using Cnaws.Web;
using Cnaws.Product;
using Cnaws.Web.Templates;
using M = XcpNet.Supplier.Modules.Modules;

namespace XcpNet.B2bShop.Controllers.Extension
{
    public sealed class Category : Common.CommoSupplierController
    {
        [Authorize(true)]
        [Distributor(true)]
        public void List(int id, long page = 1, Arguments args = null)
        {
            IList<M.DistributorCategory> cates = M.DistributorCategory.GetAllParentsById(DataSource, id);
            FilterParameters2 filter = new FilterParameters2();
            filter.Size = 20;
            filter.Load(page, args);
            filter.Province = Distributor.Province;
            filter.City = Distributor.City;
            filter.County = Distributor.County;
            string requestParam = string.Empty;
            filter.KeyWord = Request["q"];

            if (!string.IsNullOrEmpty(filter.KeyWord))
                requestParam = $"?q={filter.KeyWord}";

            if (id == 0 && !string.IsNullOrEmpty(filter.KeyWord))
            {
                this["CategoryList"] = M.DistributorCategory.GetCategoryByApiProductList(DataSource, id, cates.Count, filter, 1);
            }
            else
            {
                if (cates.Count > 0)
                    this["TopCategory"] = cates[0];
                this["CategoryList"] = cates;
            }
            this["Filter"] = filter;
            this["ProductList"] = M.DistributorProduct.ApiGetPageByCategory(DataSource, id, cates.Count, filter);
            this["PageUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl("/category/list/", id.ToString(), filter.CopyByPage(Convert.ToInt64(ps[0])).ToString()), requestParam);
            });
            this["BrandUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl("/category/list/", id.ToString(), filter.CopyByStore(Convert.ToInt32(ps[0])).ToString()), requestParam);
            });
            this["OrderUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl("/category/list/", id.ToString(), filter.CopyByOrderBy(Convert.ToInt32(ps[0])).ToString()), requestParam);
            });

            Render("category.html");
        }
    }
}
