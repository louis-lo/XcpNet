using Cnaws.Product;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using M = Cnaws.Product.Modules;
using Cnaws.Web.Templates;
using S = Cnaws.Statistic.Modules;
using Cnaws.Data;
using XcpNet.Ad.Modules;

namespace XcpNet.Website.Controllers.Extension
{
    public sealed class Category : Common.CommonController
    {
        public void List(int id, long page = 1, Arguments args = null)
        {
            IList<M.ProductCategory> cates = M.ProductCategory.GetAllParentsById(DataSource, id);

            FilterParameters2 filter = new FilterParameters2();
            filter.Size = 20;
            filter.Load(page, args);

            string requestParam = string.Empty;
            filter.KeyWord = Request["q"];

            if (!string.IsNullOrEmpty(filter.KeyWord))
                requestParam = string.Format("?q={0}&searchType={1}", Request["q"], Request["searchType"]);

            if (id == 0 && !string.IsNullOrEmpty(filter.KeyWord))
            {
                this["CategoryList"] = M.ProductCategory.GetCategoryByApiProductList(DataSource, id, cates.Count, filter, 1);
            }
            else
            {
                this["CategoryList"] = cates;              
            }
            this["CategoryId"] = id;
            this["BrandList"] = M.ProductBrand.GetAllByCategoryAndScreen(DataSource, id);
            this["AttributeList"] = M.ProductAttribute.GetAllByCategoryAndScreen(DataSource, id); ;
            this["AttributeMappingList"] = M.ProductAttributeMapping.GetAllByCategoryId(DataSource, id, cates.Count);
            this["Filter"] = filter;
            this["Category"] = M.ProductCategory.GetById(DataSource, id);
            this["ProductList"] = M.Product.ApiGetPageByCategory(DataSource, id, cates.Count, filter);
            this["PageUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl("/category/list/", id.ToString(), filter.CopyByPage(Convert.ToInt64(ps[0])).ToString()), requestParam);
            });
            this["CateUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl("/category/list/", Convert.ToString(ps[0]), filter.ToString()), requestParam);
            });
            this["BrandUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl("/category/list/", id.ToString(), filter.CopyByBrand(Convert.ToInt32(ps[0])).ToString()), requestParam);
            });
            this["OrderUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl("/category/list/", id.ToString(), filter.CopyByOrderBy(Convert.ToInt32(ps[0])).ToString()), requestParam);
            });
            this["AttrUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl("/category/list/", id.ToString(), filter.CopyByAttr(Convert.ToInt64(ps[0]), ps[1] as string).ToString()), requestParam);
            });
            this["PriceUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl("/category/list/", id.ToString(), filter.CopyByPrice(ps[0] as string, ps[1] as string).ToString()), requestParam);
            });

            Render("cate_sub.html");
        }
        /// <summary>
        /// 大分类页面
        /// </summary>
        /// <param name="categoryId"></param>
        public void CateParent(int categoryId)
        {
            this["AdList"] = Advertisement.GetByLabelAndCategoryId(DataSource, 16, categoryId);
            this["Category"] = M.ProductCategory.GetById(DataSource, categoryId);
            this["Categories"] = M.ProductCategory.GetAll(DataSource, categoryId);

            Render("cate_parent.html");
        }

        public void Child(int id)
        {
            if (IsAjax)
            {
                SetResult(M.ProductCategory.GetAll(DataSource, id));
            }
            else
            {
                NotFound();
            }
        }
        public void BrandList(int id, long page = 1, Arguments args = null)
        {
            IList<M.ProductCategory> cates = M.ProductCategory.GetAllParentsById(DataSource, id);
            IList<M.ProductAttribute> attrs = M.ProductAttribute.GetAllByCategory(DataSource, id);
            FilterParameters filter = new FilterParameters(attrs);
            filter.Load(page, args);

            long index = filter.Page;
            filter.Page = 1;
            SplitPageData<DataJoin<M.Product, S.StatisticData>> BrandList = M.Product.GetBrandPageByArguments(DataSource, id, true, filter, cates.Count, 15,8);
            if (BrandList.PagesCount >= index)
                filter.Page = index;
            else
                filter.Page = BrandList.PagesCount;
            this["BrandProductList"] = M.Product.GetBrandPageByArguments(DataSource, id, true, filter, cates.Count, 4, 8);

            Render("category_brand.html");
        }
    }
}
