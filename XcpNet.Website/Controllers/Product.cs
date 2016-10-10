using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Product.Modules;
using System.Collections.Generic;
using Cnaws.Web.Templates;
using XcpNet.Common;

namespace XcpNet.Website.Controllers.Extension
{
    public class Product : CommonController
    {
        public void Info(long id)
        {
            M.Product product = M.Product.GetSaleProduct(DataSource, id);
            if (product != null)
            {
                long parent = product.ParentId > 0 ? product.ParentId : product.Id;
                
                this["Product"] = product;
                this["ParentId"] = parent;
                this["Supplier"] = M.Supplier.GetById(DataSource, product.SupplierId);
                this["Series"] = M.ProductSerie.GetAll(DataSource, parent);
                this["Mapping"] = M.ProductMapping.GetAllByProduct(DataSource, product.Id);
                this["Mappings"] = M.ProductMapping.GetAllByAllProduct(DataSource, parent);
                this["Attributes"] = M.ProductAttribute.GetAllValuesByProduct(DataSource, product.Id);
                this["CategoryList"] = M.ProductCategory.GetAllParentsById(DataSource, product.CategoryId);
                this["RecommendList"] = M.Product.GetTopRecommendByCategory(DataSource, 5, product.CategoryId);
                this["StoreInfo"] = M.StoreInfo.GetStoreInfoByUserId(DataSource, product.SupplierId);
                this["ProductCount"] = M.Product.GetProductCountBySupplierId(DataSource, product.SupplierId);
                this["StoreCategoryList"] = M.StoreCategory.GetStoreCategoryListByUserId(DataSource, product.SupplierId);
                this["BrandList"] = M.ProductBrand.GetAllByCategoryAndScreen(DataSource, product.CategoryId);
                Render("product.html");

            }
            else
            {
                NotFound();
            }
        }

        public void View(long id)
        {
            M.Product product = M.Product.GetViewProduct(DataSource, id, User.Identity.Id);
            if (product != null)
            {
                long parent = product.ParentId > 0 ? product.ParentId : product.Id;

                this["Product"] = product;
                this["ParentId"] = parent;
                this["Supplier"] = M.Supplier.GetById(DataSource, product.SupplierId);
                this["Series"] = M.ProductSerie.GetAll(DataSource, parent);
                this["Mapping"] = M.ProductMapping.GetAllByProduct(DataSource, product.Id);
                this["Mappings"] = M.ProductMapping.GetAllByAllProductEx(DataSource, parent);
                this["Attributes"] = M.ProductAttribute.GetAllValuesByProduct(DataSource, product.Id);
                this["CategoryList"] = M.ProductCategory.GetAllParentsById(DataSource, product.CategoryId);
                this["RecommendList"] = M.Product.GetTopRecommendByCategory(DataSource, 5, product.CategoryId);
                this["StoreInfo"] = M.StoreInfo.GetStoreInfoByUserId(DataSource, product.SupplierId);
                this["ProductCount"] = M.Product.GetProductCountBySupplierId(DataSource, product.SupplierId);
                this["StoreCategoryList"] = M.StoreCategory.GetStoreCategoryListByUserId(DataSource, product.SupplierId);
                this["BrandList"] = M.ProductBrand.GetAllByCategoryAndScreen(DataSource, product.CategoryId);
                Render("product.html");
            }
            else
            {
                NotFound();
            }
        }

        public void Show(long id)
        {
            M.Product product = M.Product.GetById(DataSource, id);
            if (product != null)
            {
                long parent = product.ParentId > 0 ? product.ParentId : product.Id;

                this["Product"] = product;
                this["ParentId"] = parent;
                this["Supplier"] = M.Supplier.GetById(DataSource, product.SupplierId);
                this["Series"] = M.ProductSerie.GetAll(DataSource, parent);
                this["Mapping"] = M.ProductMapping.GetAllByProduct(DataSource, product.Id);
                this["Mappings"] = M.ProductMapping.GetAllByAllProductEx(DataSource, parent);
                this["Attributes"] = M.ProductAttribute.GetAllValuesByProduct(DataSource, product.Id);
                this["CategoryList"] = M.ProductCategory.GetAllParentsById(DataSource, product.CategoryId);
                this["RecommendList"] = M.Product.GetTopRecommendByCategory(DataSource, 5, product.CategoryId);
                this["StoreInfo"] = M.StoreInfo.GetStoreInfoByUserId(DataSource, product.SupplierId);
                this["ProductCount"] = M.Product.GetProductCountBySupplierId(DataSource, product.SupplierId);
                this["StoreCategoryList"] = M.StoreCategory.GetStoreCategoryListByUserId(DataSource, product.SupplierId);
                this["BrandList"] = M.ProductBrand.GetAllByCategoryAndScreen(DataSource, product.CategoryId);
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
                SetResult(M.ProductMapping.GetAllByAllProductAndNotSerie(DataSource, productId, serieId));
            }
            else
            {
                NotFound();
            }
        }

        public void shop(long id)
        {
            this["StoreCategoryList"] = M.StoreCategory.GetStoreCategoryListByUserId(DataSource, id);
            this["Supplier"] = M.Supplier.GetById(DataSource, id);
            this["StoreInfo"] = M.StoreInfo.GetStoreInfoByUserId(DataSource, id);
            this["categoryId"] = 0;
            Render("shop.html");
        }
        public void shoplist(long id, int categoryId, int page = 1)
        {
            this["StoreCategoryList"] = M.StoreCategory.GetStoreCategoryListByUserId(DataSource, id);
            this["Supplier"] = M.Supplier.GetById(DataSource, id);
            this["StoreInfo"] = M.StoreInfo.GetStoreInfoByUserId(DataSource, id);
            this["ProductList"] = M.Product.GetProductListByStoreCategoryId(DataSource, categoryId, page, 16, 8, 1);
            this["PageUrl"] = new FuncHandler((object[] ps) =>
            {
                return GetUrl("/product/shoplist/", id.ToString(), "/", categoryId.ToString(), "/", ps[0].ToString());
            });
            this["categoryId"] = categoryId;
            Render("shoplist.html");
        }

        public void shopdetails(long id)
        {
            this["Supplier"] = M.Supplier.GetById(DataSource, id);
            this["StoreInfo"] = M.StoreInfo.GetStoreInfoByUserId(DataSource, id);
            Render("shopdetails.html");
        }
    }
}
