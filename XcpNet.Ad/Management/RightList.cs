using System;
using Cnaws.Management;

namespace XcpNet.Ad.Management
{
    public sealed class RightList : ManagementRights
    {
        protected override void InitRights()
        {
            AddRight("广告位管理", "management.adtype");
            AddRight("广告管理", "management.advertisement");
        }
    }
}
