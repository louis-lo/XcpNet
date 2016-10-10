using System;
using Cnaws.Management;

namespace XcpNet.Management
{
    public sealed class RightList : ManagementRights
    {
        protected override void InitRights()
        {
            AddRight("供应商管理", "management.supplierex");
        }
    }
}
