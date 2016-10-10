using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;

namespace XcpNet.Passport.Controllers.Extension
{
    public sealed class Logout : Cnaws.Passport.Controllers.Logout
    {
        protected override void OnLogouted(long userId)
        {
            PassportAuthentication.SetCustomCookie(XcpUtility.SupplierCookieName, XcpUtility.GetBytes(new KeyValuePair<bool, bool>(false, false)), Context);
        }
    }
}
