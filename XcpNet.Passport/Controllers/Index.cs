using System;
using Cnaws.Web;
using M = Cnaws.Product.Modules;

namespace XcpNet.Passport.Controllers.Extension
{
    public sealed class Index : Common.CommPassportController
    {
        public void index()
        {
            Redirect(GetUrl("ucenter"));
        }

        [Authorize(true)]
        public void Order(string state = "_", long index = 0L)
        {
            if (index > 0)
                this["OrderList"] = M.ProductOrder.GetPageByUserAndState(DataSource, User.Identity.Id, state, index, 5, 8);
            this["State"] = state;
            Render("ucenter.html");
        }
    }
#if (DEBUG)
    public sealed class Test : DataController
    {

        public void index(string filename)
        {
            Render(string.Concat("test_", filename, ".html"));
        }
    }
#endif


}