using Cnaws.Data;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using P = Cnaws.Product.Modules;

namespace XcpNet.Supplier.Controllers
{
    public class StoreProductCategory: SupplierController
    {
        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void Index()
        {
            this["StoreCategoryList"] = P.StoreCategory.GetXDGCategoryOne(DataSource, User.Identity.Id);
            this["ParentId"] = 0;
            this["Name"] = ""; 
            Render("store_product_category.html");
        }
        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void Index2(int id)
        {
            P.StoreCategory cate= P.StoreCategory.GetById(DataSource, id);
            this["StoreCategoryList"] = cate.GetXDGCategoryTwo(DataSource);
            this["ParentId"] = id;
            this["Name"] = cate.Name;
            Render("store_product_category.html");
        }
        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void Add()
        {
            try
            {
                P.StoreCategory model = DbTable.Load<P.StoreCategory>(Request.Form);
                if (model != null)
                {
                    model.UserId = User.Identity.Id;
                    SetResult(model.Insert(DataSource));
                }
                else
                {
                    SetResult(DataStatus.Failed);
                }
            }
            catch (Exception)
            {
                SetResult(DataStatus.Failed);
            }
        }
        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void Delete(int id)
        {
            try
            {
                SetResult(P.StoreCategory.Delete(DataSource, User.Identity.Id, id));
            }
            catch (Exception)
            {
                SetResult(DataStatus.Failed);
            }
        }
        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void Update()
        {
            try
            {
                P.StoreCategory model = DbTable.Load<P.StoreCategory>(Request.Form);
                if (model != null)
                {
                    model.UserId = User.Identity.Id;
                    SetResult(P.StoreCategory.Update(DataSource, model));
                }
                else
                {
                    SetResult(DataStatus.Failed);
                }
            }
            catch (Exception)
            {
                SetResult(DataStatus.Failed);
            }
        }
    }
}
