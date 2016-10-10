using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;

namespace XcpNet.Website.Management
{
    public sealed class Promotions : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Website"; }
        }


    }
}
