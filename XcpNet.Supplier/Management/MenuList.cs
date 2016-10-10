using System;
using Cnaws.Management;

namespace XcpNet.Supplier.Management
{
    public sealed class MenuList : ManagementMenus
    {
        protected override void InitMenus()
        {
            AddMenu(1, 99, "批发管理")
                .AddSubMenu("批发分类", "/distributorcategory")
                .AddSubMenu("批发规格", "/distributorattribute")
                .AddSubMenu("批发产品", "/distributorproduct")
                .AddSubMenu("批发订单", "/distributororder");
        }
    }
}
