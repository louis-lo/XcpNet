using System;
using Cnaws.Web;
using Cnaws.Data;
using M = XcpNet.Supplier.Modules.Modules;
using P = Cnaws.Product.Modules;
using System.Collections.Generic;
using Cnaws.Web.Templates;
using XcpNet.Common;

namespace XcpNet.B2bShop.Controllers.Extension
{
    public class Product : CommoSupplierController
    {
        [Authorize(true)]
        [Distributor(true)]
        public void Info(long id)
        {
            M.DistributorProduct product = M.DistributorProduct.GetSaleProduct(DataSource, id);
            if (product != null)
            {
                long parent = product.ParentId > 0 ? product.ParentId : product.Id;
                
                this["Product"] = product;
                this["ParentId"] = parent;
                this["Supplier"] = P.Distributor.GetById(DataSource, product.SupplierId);
                this["Series"] = M.DistributorSerie.GetAll(DataSource, parent);
                this["Mapping"] = M.DistributorMapping.GetAllByProduct(DataSource, product.Id);
                this["Mappings"] = M.DistributorMapping.GetAllByAllProduct(DataSource, parent);
                this["Attributes"] = M.DistributorAttribute.GetAllValuesByProduct(DataSource, product.Id);
                this["CategoryList"] = M.DistributorCategory.GetAllParentsById(DataSource, product.CategoryId);
                this["RecommendList"] = M.DistributorProduct.GetTopRecommendByCategory(DataSource, 5, product.CategoryId);
                this["StoreInfo"] = P.StoreInfo.GetStoreInfoByUserId(DataSource, product.SupplierId);
                this["ProductCount"] = M.DistributorProduct.GetProductCountBySupplierId(DataSource, product.SupplierId);
                this["StoreCategoryList"] = P.StoreCategory.GetStoreCategoryListByUserId(DataSource, product.SupplierId);
                Render("product.html");
            }
            else
            {
                NotFound();
            }
        }
        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void View(long id)
        {
            M.DistributorProduct product = M.DistributorProduct.GetViewProduct(DataSource, id, User.Identity.Id);
            if (product != null)
            {
                long parent = product.ParentId > 0 ? product.ParentId : product.Id;

                this["Product"] = product;
                this["ParentId"] = parent;
                this["Supplier"] = P.Distributor.GetById(DataSource, product.SupplierId);
                this["Series"] = M.DistributorSerie.GetAll(DataSource, parent);
                this["Mapping"] = M.DistributorMapping.GetAllByProduct(DataSource, product.Id);
                this["Mappings"] = M.DistributorMapping.GetAllByAllProductEx(DataSource, parent);
                this["Attributes"] = M.DistributorAttribute.GetAllValuesByProduct(DataSource, product.Id);
                this["CategoryList"] = M.DistributorCategory.GetAllParentsById(DataSource, product.CategoryId);
                this["RecommendList"] = M.DistributorProduct.GetTopRecommendByCategory(DataSource, 5, product.CategoryId);
                this["StoreInfo"] = P.StoreInfo.GetStoreInfoByUserId(DataSource, product.SupplierId);
                this["ProductCount"] = M.DistributorProduct.GetProductCountBySupplierId(DataSource, product.SupplierId);
                this["StoreCategoryList"] = P.StoreCategory.GetStoreCategoryListByUserId(DataSource, product.SupplierId);
                Render("product.html");
            }
            else
            {
                NotFound();
            }
        }

        public void Show(long id)
        {
            M.DistributorProduct product = M.DistributorProduct.GetById(DataSource, id);
            if (product != null)
            {
                long parent = product.ParentId > 0 ? product.ParentId : product.Id;

                this["Product"] = product;
                this["ParentId"] = parent;
                this["Supplier"] = P.Distributor.GetById(DataSource, product.SupplierId);
                this["Series"] = M.DistributorSerie.GetAll(DataSource, parent);
                this["Mapping"] = M.DistributorMapping.GetAllByProduct(DataSource, product.Id);
                this["Mappings"] = M.DistributorMapping.GetAllByAllProductEx(DataSource, parent);
                this["Attributes"] = M.DistributorAttribute.GetAllValuesByProduct(DataSource, product.Id);
                this["CategoryList"] = M.DistributorCategory.GetAllParentsById(DataSource, product.CategoryId);
                this["RecommendList"] = M.DistributorProduct.GetTopRecommendByCategory(DataSource, 5, product.CategoryId);
                this["StoreInfo"] = P.StoreInfo.GetStoreInfoByUserId(DataSource, product.SupplierId);
                this["ProductCount"] = M.DistributorProduct.GetProductCountBySupplierId(DataSource, product.SupplierId);
                this["StoreCategoryList"] = P.StoreCategory.GetStoreCategoryListByUserId(DataSource, product.SupplierId);
                Render("product.html");
            }
            else
            {
                NotFound();
            }
        }

        public void Mapping(long productId, long serieId)
        {
            if (IsAjax)
            {
                SetResult(M.DistributorMapping.GetAllByAllProductAndNotSerie(DataSource, productId, serieId));
            }
            else
            {
                NotFound();
            }
        }
        [Authorize(true)]
        public void shop(long id)
        {
            this["StoreCategoryList"] = P.StoreCategory.GetStoreCategoryListByUserId(DataSource, id);
            this["Supplier"] = P.Supplier.GetById(DataSource, id);
            this["StoreInfo"] = P.StoreInfo.GetStoreInfoByUserId(DataSource, id);
            this["categoryId"] = 0;
            Render("shop.html");
        }
        [Authorize(true)]
        public void shoplist(long id, int categoryId, int page = 1)
        {
            this["StoreCategoryList"] = P.StoreCategory.GetStoreCategoryListByUserId(DataSource, id);
            this["Supplier"] = P.Supplier.GetById(DataSource, id);
            this["StoreInfo"] = P.StoreInfo.GetStoreInfoByUserId(DataSource, id);
            this["ProductList"] = M.DistributorProduct.GetProductListByStoreCategoryId(DataSource, categoryId, page, 16, 8, 1);
            this["PageUrl"] = new FuncHandler((object[] ps) =>
            {
                return GetUrl("/product/shoplist/", id.ToString(), "/", categoryId.ToString(), "/", ps[0].ToString());
            });
            this["categoryId"] = categoryId;
            Render("shoplist.html");
        }
        [Authorize(true)]
        public void shopdetails(long id)
        {
            this["Supplier"] = P.Supplier.GetById(DataSource, id);
            this["StoreInfo"] = P.StoreInfo.GetStoreInfoByUserId(DataSource, id);
            Render("shopdetails.html");
        }
    }
}
