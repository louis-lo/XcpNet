using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcpNet.Common;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
namespace XcpNet.Website.Controllers
{
    public class Buy : CommonController
    {
        [HttpPost]
        [HttpAjax]
        [Authorize(true)]
        public void Index()
        {
            if (IsPost)
            {
                object code, data;
                try
                {
                    string Id = string.Join(",", Request.Form["Id"]);
                    string Count = string.Join(",", Request.Form["Count"]);
                    M.Member member = M.Member.GetById(DataSource, User.Identity.Id);
                    code = CommonBuy.CommSetOrder<P.ProductOrder>(DataSource, member, Id, Count, Location.ProvinceId, Location.CityId, Location.CountyId, out data);
                    CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });

                }
            }
            else
            {
                NotFound();
            }
        }

        private void CommSetResult(object code, object data)
        {
            if (data == null)
            {
                if (code.GetType() == typeof(int))
                    SetResult((int)code);
                else if (code.GetType() == typeof(DataStatus))
                    SetResult((DataStatus)code);
                else
                    SetResult(code);
            }
            else
                SetResult((int)code, data);
        }
    }
}
