using System;
using System.Collections.Generic;
using M = XcpNet.Ad.Modules;
using Pd = Cnaws.Product.Modules;

namespace XcpNet.Api.Controllers
{
    public sealed class AppExtract : CommExtract
    {
        protected override void OnInitController()
        {
            CheckSign(IsPost ? Request.Form : Request.QueryString);
        }
    }
}
