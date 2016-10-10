using System;
using Cnaws.Management;

namespace XcpNet.Supplier.Management
{
    public sealed class RightList : ManagementRights
    {
        protected override void InitRights()
        {
            AddRight("进货宝-商品分类管理", "management.distributorcategory");
            AddRight("进货宝-商品规格管理", "management.distributorattribute");
            AddRight("进货宝-商品品牌管理", "management.distributorbrand");
            AddRight("进货宝-商品管理", "management.distributorproduct");
            AddRight("进货宝-行业分类管理", "management.indutrycategory");
            AddRight("进货宝-进货方案管理", "management.distributorprogramme");
        }
    }
}
