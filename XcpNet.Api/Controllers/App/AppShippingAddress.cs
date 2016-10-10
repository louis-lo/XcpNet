using System;
using Cnaws.Data;
using C = Cnaws.Comment.Modules;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using Cnaws.Web;
using System.Web;
using System.Collections.Generic;
using XcpNet.Api.Modules;
using Cnaws.Data.Query;

namespace XcpNet.Api.Controllers
{
    public sealed class AppShippingAddress : CommShippingAddress
    {
        protected override void OnInitController()
        {
            CheckSign(IsPost ? Request.Form : Request.QueryString);
        }
    }
}
