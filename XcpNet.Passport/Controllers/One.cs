using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Product.Modules;
using P = Cnaws.Passport.Modules;
using Cnaws.Web.Templates;

namespace XcpNet.Passport.Controllers.Extension
{
    public sealed class One : Common.CommPassportController
    {
        [Authorize(true)]
        public void Index()
        {
            List();
        }

        [Authorize(true)]
        public void List(int state = 0, long index = 1L)
        {
            this["State"] = state;
            this["OrderList"] = M.OneProductOrder.GetPageByUserAndState(DataSource, User.Identity.Id, state, index, 10, 8);
            this["GetImage"] = new FuncHandler((args) =>
              {
                  string imgs = args[0] as string;
                  if (!string.IsNullOrEmpty(imgs))
                      return imgs.Split(M.OneProduct.ImageSplitChar)[0];
                  return string.Empty;
              });
            Render("one.html");
        }
    }
}
