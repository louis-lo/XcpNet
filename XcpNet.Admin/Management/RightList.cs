using System;
using Cnaws.Management;

namespace XcpNet.Admin.Management
{
    public sealed class RightList : ManagementRights
    {
        protected override void InitRights()
        {
            //AddRight("供应商管理", "management.supplierex");
            //AddRight("加盟商管理", "management.distributorex");
            //AddRight("用户充值", "management.rechargebyadmin");

            AddRight("城品惠-商品管理", "management.s_product");

            AddRight("城品惠-订单管理", "management.productorder");
            AddRight("乡道馆-订单管理", "management.xproductorder");
            AddRight("进货宝-订单管理", "management.distributororder");
            
            AddRight("城品惠-售后管理", "management.aftersales");
            AddRight("乡道馆-售后管理", "management.xaftersales");
            AddRight("进货宝-售后管理", "management.distributoraftersales");
           
            AddRight("城品惠-上架审核", "management.productapproved");
            AddRight("乡道馆-上架审核", "management.xproductapproved");
            AddRight("进货宝-上架审核", "management.distributorapproved");

            AddRight("用户管理", "management.memberlist");
            AddRight("财务流水", "management.moneyrecord");

            AddRight("加盟商管理", "management.distributor");

            //AddRight("财务统计", "management.statistics");
            //AddRight("物流管理", "management.logisticslist");
            //AddRight("提现审核", "management.presentaudit");
        }
    }
}
