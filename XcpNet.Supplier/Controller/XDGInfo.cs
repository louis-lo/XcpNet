using Cnaws.Data;
using Cnaws.Product.Modules;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDG = XcpNet.Supplier.Modules.Modules;
using P = Cnaws.Product.Modules;

namespace XcpNet.Supplier.Controllers
{
    public class XDGInfo: SupplierController
    {
        [Authorize(true)]
        [Distributor(true, 1)]
        public void Index()
        {
            this["XDGInfo"] = XDG.XDGInfo.GetXDGInfoByUserId(DataSource, User.Identity.Id);
            Render("xdg_info.html");
        }

        [Authorize(true)]
        [Distributor(true, 1)]
        public void Submit()
        {
            try
            {
                XDG.XDGInfo model = DbTable.Load<XDG.XDGInfo>(Request.Form);
                model.UserId = User.Identity.Id;
                DataStatus status;
                if (XDG.XDGInfo.GetXDGInfoByUserId(DataSource, User.Identity.Id) != null)
                {
                    status = model.Update(DataSource);
                }
                else
                {
                    status =  model.Insert(DataSource);
                }
                SetResult(status);
            }
            catch (Exception)
            {
                SetResult(DataStatus.Failed);
            }
        }



        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void category(int id=0)
        {
            if (id > 0)
            {
                P.StoreCategory cate = P.StoreCategory.GetById(DataSource, id);
                this["StoreCategoryList"] = cate.GetXDGCategoryTwo(DataSource);
                this["ParentId"] = id;
                this["Name"] = cate.Name;
            }
            else
            {
                this["StoreCategoryList"] = P.StoreCategory.GetXDGCategoryOne(DataSource, User.Identity.Id);
                this["ParentId"] = 0;
                this["Name"] = "";
            }
            Render("store_category.html");
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
