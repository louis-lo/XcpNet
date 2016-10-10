using Cnaws.Data;
using Cnaws.Web;
using Cnaws.Web.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using M = XcpNet.Ad.Modules;

namespace XcpNet.Supplier.Controllers
{
    public class Advertisement : SupplierController
    {
        [Authorize]
        [Distributor]
        public void List(long label = -1, int page = 1)
        {
            this["label"] = label;
            this["AdTypes"] = M.AdType.GetAll(DataSource, (int)M.AdType.EAdType.TownshipStore);
            this["AdList"] = M.Advertisement.GetPageByTypeByUser(DataSource, label, User.Identity.Id, Ad.Modules.AdType.EAdType.TownshipStore, page, 10, 5);
            Render("advertisement.html");
        }

        [HttpAjax]
        public void GetAllType(int type)
        {
            SetResult(M.AdType.GetAll(DataSource, type));
        }

        [HttpPost]
        [HttpAjax]
        public void Del(int id)
        {
            try
            {
                SetResult(M.Advertisement.DeleteByUserId(DataSource, id, User.Identity.Id));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }


        [HttpPost]
        [HttpAjax]
        public void Submit(int id = 0)
        {
            try
            {
                M.Advertisement ad = DbTable.Load<M.Advertisement>(Request.Form);
                ad.UserId = User.Identity.Id;
                if (id != 0)
                {
                    ad.Id = id;
                    SetResult(ad.Update(DataSource));
                }
                else
                {
                    SetResult(ad.Insert(DataSource));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [HttpAjax]
        public void Info(int id)
        {            
            M.Advertisement ad = M.Advertisement.GetById(DataSource, id);
            this["AdInfo"] = ad;
            this["AdTypes"] = M.AdType.GetAll(DataSource, (int)M.AdType.EAdType.TownshipStore);
            Render("advertisement.html");
        }
    }
}





