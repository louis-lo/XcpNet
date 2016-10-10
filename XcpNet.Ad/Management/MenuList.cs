using System;
using Cnaws.Management;

namespace XcpNet.Ad.Management
{
    public sealed class MenuList:ManagementMenus
    {
        protected override void InitMenus()
        {
            AddMenu(0, 95, "广告管理")
                .AddSubMenu("广告类型", "/adtype")
                .AddSubMenu("广告管理", "/advertisement");
        }
    }
}
