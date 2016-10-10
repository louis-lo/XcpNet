using System;
using Cnaws.Management;

namespace XcpNet.Management
{
    public sealed class MenuList : ManagementMenus
    {
        protected override void InitMenus()
        {
            AddMenu(4, 99, "供应商管理")
                .AddSubMenu("添加供应商", "/supplierex");
        }
    }
}
