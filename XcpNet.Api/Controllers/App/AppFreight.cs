using System;
using Cnaws.Area;
using Cnaws.Product.Modules;

namespace XcpNet.Api.Controllers
{
    public sealed class AppFreight : CommFreight
    {
        protected override void OnInitController()
        {
            CheckSign(IsPost ? Request.Form : Request.QueryString);
        }


    }
}
