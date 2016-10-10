using System;
using Cnaws.Management;

namespace XcpNet.Admin.Management
{
    public sealed class MenuList : ManagementMenus
    {
        protected override void InitMenus()
        {
            AddMenu(0, 95, "物流管理")
               .AddSubMenu("物流管理", "/logisticslist");
            AddMenu(4, 98, "供应商管理")
                .AddSubMenu("供应商管理", "/supplierex");
            AddMenu(4, 99, "加盟商管理")
                .AddSubMenu("加盟商管理", "/distributorex");
            AddMenu(0, 99, "财务统计")
                .AddSubMenu("用户充值", "/rechargebyadmin")
                .AddSubMenu("财务统计", "/statistics");
            AddMenu(4, 99, "用户管理")
              .AddSubMenu("用户管理", "/memberlist");
            AddMenu(4, 99, "财务充值")
                 .AddSubMenu("用户充值", "/rechargebyadmin")
               .AddSubMenu("提现审核", "/presentaudit");
        }
    }
}
