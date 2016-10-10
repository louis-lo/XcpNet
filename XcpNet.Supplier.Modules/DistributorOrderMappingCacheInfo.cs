using Cnaws;
using Cnaws.Data;
using Cnaws.Json;
using System;
using System.Collections.Generic;

namespace XcpNet.Supplier.Modules
{
    [Serializable]
    public sealed class DistributorOrderMappingCacheInfo
    {

        public long ProductId = 0L;
        public string Image = null;
        public DistributorProductCacheInfo ProductInfo = null;
        public Money Price = 0;
        public Money TotalMoney = 0;
        public int Count = 0;
        public bool Evaluation = false;
        public bool IsService = false;
        public string AfterSalesOrderId = null;


        public DistributorOrderMappingCacheInfo()
        {
        }
        public DistributorOrderMappingCacheInfo(Modules.DistributorOrderMapping p)
        {
            ProductInfo=JsonValue.Deserialize<DistributorProductCacheInfo>(p.ProductInfo);
            ProductId = p.ProductId;
            Image = p.GetImage(ProductInfo.Image);
            Price = p.Price;
            TotalMoney = p.TotalMoney;
            Count = p.Count;
            Evaluation = p.Evaluation;
            IsService = p.IsService;
            AfterSalesOrderId = p.AfterSalesOrderId;
        }
    }
}
