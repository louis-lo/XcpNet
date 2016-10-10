using System;

namespace XcpNet.Admin.Management
{
    public class XProductOrder : ProductOrder
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        public override string Path
        {
            get { return "xproductorder"; }
        }
        public override int Channel
        {
            get { return 2; }
        }
    }
}
