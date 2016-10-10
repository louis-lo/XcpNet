using System;
using Cnaws.Web;
using Cnaws.Data;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class DistributorMoneyRecord : Cnaws.Passport.Modules.MoneyRecord
    {
        public const int RechargeType = 0;
    }
}
