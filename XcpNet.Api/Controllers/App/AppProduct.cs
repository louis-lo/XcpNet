using System.Collections.Generic;
using Cnaws.Data;
using Pd = Cnaws.Product.Modules;
using S = Cnaws.Statistic.Modules;
using Cnaws.Product;
using Cnaws.Web;
using System;
using A = Cnaws.Article.Modules;

namespace XcpNet.Api.Controllers
{
    public sealed class AppProduct : CommProduct
    {
        protected override void OnInitController()
        {
            CheckSign(IsPost ? Request.Form : Request.QueryString);
        }
    }
}
