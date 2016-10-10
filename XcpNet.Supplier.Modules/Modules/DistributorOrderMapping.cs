using System;
using System.Collections.Generic;
using Cnaws;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Json;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class DistributorOrderMapping : Cnaws.Product.Modules.ProductOrderMapping
    {
        public DistributorOrderMapping()
        {

        }
        public new static DistributorOrderMapping GetById(DataSource ds, string orderId, long productId)
        {
            return Db<DistributorOrderMapping>.Query(ds)
                .Select()
                .Where(W("OrderId", orderId) & W("ProductId", productId))
                .First<DistributorOrderMapping>();
        }
        public static string BuildInfo(DataSource ds, DistributorProduct p)
        {
            return JsonValue.Serialize(new DistributorProductCacheInfo(ds, p));
        }
        public new DistributorProductCacheInfo GetProductInfo()
        {
            return JsonValue.Deserialize<DistributorProductCacheInfo>(ProductInfo);
        }
        public DistributorOrderMapping(DataSource ds, string orderId, DistributorProduct p, int count)
        {
            OrderId = orderId;
            ProductId = p.Id;
            Price = p.WholesalePrice;
            Count = count;
            TotalMoney = Price * Count;
            ProductInfo = BuildInfo(ds, p);
        }
        public static DataStatus ModByArea(DataSource ds, DistributorOrderMapping pom)
        {
            if (Db<DistributorOrderMapping>.Query(ds).Update()
                .Set("Province", pom.Province)
                .Set("City", pom.City)
                .Set("County", pom.County)
                .Set("Price", pom.Price)
                .Where(W("OrderId", pom.OrderId) & W("ProductId", pom.ProductId)).Execute() > 0
                )
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }
        public DistributorOrderMapping(DataSource ds, string orderId, DistributorProduct p, int count,int province,int city,int county)
        {
            OrderId = orderId;
            Province = province;
            City = city;
            County = county;
            ProductId = p.Id;
            Price = p.GetSalePrice(ds,province,city,county);
            Count = count;
            TotalMoney = Price * Count;
            ProductInfo = BuildInfo(ds, p);
        }

        public static new IList<DistributorOrderMapping> GetAllByOrder(DataSource ds, string orderId)
        {
            return DataQuery
                .Select<DistributorOrderMapping>(ds)
                .Where(P("OrderId", orderId))
                .ToList<DistributorOrderMapping>();
        }

        public new Money GetSalePrice(DataSource ds, int province, int city, int county)
        {
            DistributorProduct product = DistributorProduct.GetById(ds, ProductId);
            return product.GetSalePrice(ds, province, city, county);
        }

        public new bool GetSaleArea(DataSource ds, int province, int city, int county)
        {
            DistributorProduct product = DistributorProduct.GetById(ds, ProductId);
            return product.GetSaleArea(ds, province, city, county);
        }
    }
}
