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
using Cnaws.Data.Query;
using C = Cnaws.Comment.Modules;
using XcpNet.Common;
namespace XcpNet.ApiSecond.Controllers
{
    public sealed class LedDistributorAfterSales2 : DistributorAfterSales2
    {
        protected override void OnInitController()
        {

        }

        protected override void Dispose(bool disposing)
        {

            base.Dispose(disposing);
        }
    }
}
