using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Product.Modules;
using System.Collections.Generic;
using Cnaws.Web.Templates;

namespace XcpNet.Passport.Controllers.Extension
{
    /// <summary>
    /// 买到的宝贝
    /// </summary>
    public class Bought : Common.CommPassportController
    {
        [Authorize(true)]
        public virtual void Index()
        {
            List();
        }

        [Authorize(true)]
        public virtual void List(string state = "_", long index = 1L)
        {
            this["State"] = state;
            this["OrderList"] = M.ProductOrder.GetPageByUserAndState(DataSource, User.Identity.Id, state, index, 10, 8);
            Render("bought.html");
        }

        [HttpAjax]
        [Authorize]
        public virtual void Ajax(string state = null, long index = 1L)
        {
            try
            {
                SetResult(M.ProductOrder.GetAjaxPageByUserAndState(DataSource, User.Identity.Id, state, index, 10, 11));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

    }
}
