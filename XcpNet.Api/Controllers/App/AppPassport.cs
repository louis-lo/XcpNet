using Cnaws.Data;
using Cnaws.ExtensionMethods;
using Cnaws.Web;
using Cnaws.Web.Configuration;
using System;
using M = Cnaws.Passport.Modules;
using S = Cnaws.Sms.Modules;
using V = Cnaws.Verification.Modules;
using A = XcpNet.Ad.Modules;
using P = Cnaws.Product.Modules;
using System.Collections.Specialized;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web;
using System.Text.RegularExpressions;

namespace XcpNet.Api.Controllers
{
    public sealed class AppPassport : CommPassport
    {
        protected override void OnInitController()
        {
            CheckSign(IsPost ? Request.Form : Request.QueryString);
        }
    }
}
