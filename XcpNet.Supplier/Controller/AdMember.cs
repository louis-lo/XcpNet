using Cnaws.Data;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using P = XcpNet.Ad.Modules;

namespace XcpNet.Supplier.Controllers
{
    public class AdMember : SupplierController
    {
        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void Index()
        {
            int type = IsDistributor() ? 1: 2;
            this["Ad"] = P.AdMember.GetAdMember(DataSource, User.Identity.Id, 2, type);
            this["type"] = type;
            Render("xdg_ad.html");
        }
        [Authorize(true)]
        [SupplierOrDistributor(true)]
        public void Add()
        {
            try
            {
                P.AdMember model = DbTable.Load<P.AdMember>(Request.Form);
                if (model != null)
                {
                    model.UserId = User.Identity.Id;
                    model.Type = IsDistributor() ? 1 : 2;
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
                SetResult(P.AdMember.Delete(DataSource, User.Identity.Id, id));
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
                P.AdMember model = DbTable.Load<P.AdMember>(Request.Form);
                if (model != null)
                {
                    model.UserId = User.Identity.Id;
                    SetResult(P.AdMember.Update(DataSource, model));
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
