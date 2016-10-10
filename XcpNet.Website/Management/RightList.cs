using System;
using Cnaws.Management;

namespace XcpNet.Website.Management
{
    public sealed class RightList : ManagementRights
    {
        protected override void InitRights()
        {
            AddRight("促销管理", "management.promotions");
        }
    }
}
