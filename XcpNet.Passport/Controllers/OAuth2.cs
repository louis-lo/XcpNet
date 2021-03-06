﻿using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Product.Modules;
using System.Collections.Generic;

namespace XcpNet.Passport.Controllers.Extension
{
    public sealed class OAuth2 : Cnaws.Passport.Controllers.OAuth2
    {
        protected override void OnLogined(long userId)
        {
            bool isSupplier = M.Supplier.IsSupplier(DataSource, userId);
            bool isDistributor = M.Distributor.IsDistributor(DataSource, userId);
            PassportAuthentication.SetCustomCookie(XcpUtility.SupplierCookieName, XcpUtility.GetBytes(new KeyValuePair<bool, bool>(isSupplier, isDistributor)), Context);
        }
    }
}
