using System;
using Cnaws.Management;

namespace XcpNet.Website.Management
{
    public sealed class MenuList : ManagementMenus
    {
        protected override void InitMenus()
        {
            AddMenu(1, 6, "促销管理")
                .AddSubMenu("促销管理", "/promotions");
        }
    }
}
