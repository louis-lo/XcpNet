using System;

namespace XcpNet.Admin.Management
{
    public class XProductApproved : ProductApproved
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        public override string Path
        {
            get { return "xproductapproved"; }
        }
        public override int ProductType
        {
            get { return 2; }
        }
    }
}
