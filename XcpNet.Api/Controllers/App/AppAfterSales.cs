using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Web;
using M = XcpNet.Ad.Modules;
using Pd = Cnaws.Product.Modules;

namespace XcpNet.Api.Controllers
{
    public sealed class AppAfterSales : CommAfterSales
    {
        protected override void OnInitController()
        {
            CheckSign(IsPost ? Request.Form : Request.QueryString);
        }
    }
}
