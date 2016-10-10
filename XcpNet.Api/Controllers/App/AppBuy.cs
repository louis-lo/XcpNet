using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Pay;
using System.Collections.Generic;
using System.Threading;
using A = XcpNet.Ad.Modules;
using Py = Cnaws.Pay.Modules;
using C = Cnaws.Comment.Modules;
using Cnaws.Data.Query;

namespace XcpNet.Api.Controllers
{
    public sealed class AppBuy : CommBuy
    {
        protected override bool CheckProvider(PayProvider provider)
        {
            return true;
        }
        protected override void OnInitController()
        {
            CheckSign(IsPost ? Request.Form : Request.QueryString);
        }
    }
}
