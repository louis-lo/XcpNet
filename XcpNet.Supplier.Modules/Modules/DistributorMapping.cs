using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class DistributorMapping : Cnaws.Product.Modules.ProductMapping
    {
        public static new DataStatus DeleteBySerie(DataSource ds, long serieId)
        {
            return (new DistributorMapping() { SerieId = serieId }).Delete(ds, "SerieId");
        }

        public static new IList<DistributorMapping> GetAllByProduct(DataSource ds, long productId)
        {
            return ExecuteReader<DistributorMapping>(ds, P("ProductId", productId));
        }

        public new static IList<DistributorMapping> GetAllByAllProductEx(DataSource ds, long productId)
        {
            return Db<DistributorMapping>.Query(ds)
                .Select()
                .Where(W("ProductId").InSelect<DistributorProduct>("Id").Where(W("Id", productId) | W("ParentId", productId)).Result())
                .ToList<DistributorMapping>();
        }
        public static new IList<DistributorMapping> GetAllBySerie(DataSource ds, long serieId)
        {
            return Db<DistributorMapping>.Query(ds)
                .Select("Value")
                .Where(W("SerieId", serieId))
                .GroupBy("Value")
                .ToList<DistributorMapping>();
        }

        public static new DistributorMapping GetBySerie(DataSource ds, long productId, long serieId)
        {
            return Db<DistributorMapping>.Query(ds)
                .Select()
                .Where(W("ProductId", productId) & W("SerieId", serieId))
                .First<DistributorMapping>();
        }
        public static IList<DistributorMapping> GetAllMappingByProductId(DataSource ds, long productId)
        {
            return Db<DistributorMapping>.Query(ds)
                .Select()
                .Where(W("ProductId", productId))
                .ToList<DistributorMapping>();
        }
        public static IList<DistributorMapping> GetByProductIds(DataSource ds, long[] productIds)
        {
            return Db<DistributorMapping>.Query(ds)
                .Select()
                .Where(W("ProductId", productIds,DbWhereType.In))
                .ToList<DistributorMapping>();
        }

        public static new IList<DistributorMapping> GetAllByAllProduct(DataSource ds, long productId)
        {
            return Db<DistributorMapping>.Query(ds)
                .Select()
                .Where(W("ProductId").InSelect<DistributorProduct>("Id").Where(W("Id", productId) | W("ParentId", productId)).Result())
                .ToList<DistributorMapping>();
        }
        public static IList<DistributorMapping> GetAllByProducts(DataSource ds, object[] productIds)
        {
            return Db<DistributorMapping>.Query(ds)
                .Select()
                .Where(W("ProductId").InSelect<DistributorProduct>("Id").Where(W("Id", productIds,DbWhereType.In) | W("ParentId", productIds, DbWhereType.In)).Result())
                .ToList<DistributorMapping>();
        }

        
        public static new IList<DistributorMapping> GetAllByAllProductAndNotSerie(DataSource ds, long productId, long serieId)
        {
            return Db<DistributorMapping>.Query(ds)
                .Select("Value")
                .Where(W("ProductId").InSelect<DistributorProduct>("Id").Where(W("Id", productId) | W("ParentId", productId)).Result() & W("SerieId", serieId, DbWhereType.NotEqual))
                .GroupBy("Value")
                .ToList<DistributorMapping>();
        }

        public new static bool Exists(DataSource ds, long productId, long serieId, string value)
        {

            DbWhereQueue where = new DbWhereQueue();
            where = (W("SerieId", serieId) & W("Value", value));
            IList<DistributorMapping> Mappings = GetAllByProduct(ds, productId);
            long serieCount = Db<DistributorSerie>.Query(ds).Select().Where(W("ProductId").InSelect<DistributorSerie>("ProductId").Where(W("Id", serieId)).Result()).Count();
            if (Mappings.Count == serieCount || (Mappings.Count == serieCount - 1 && (GetBySerieIdAndProductId(ds, serieId, productId) <= 0)))
            {
                foreach (DistributorMapping mapping in Mappings)
                {
                    if (mapping.SerieId != serieId)
                        where &= (W("ProductId").InSelect<DistributorMapping>("ProductId").Where(W("SerieId", mapping.SerieId) & W("Value", mapping.Value)).GroupBy("ProductId").Result());
                }
                where = (where) & W("ProductId", productId, DbWhereType.NotEqual);
            }
            else
            {
                return false;
            }
            return Db<DistributorMapping>.Query(ds).Select().Where(W("ProductId").InSelect<DistributorMapping>("ProductId").Where(where).GroupBy("ProductId").Result()).Count() > 0;
        }

        public new static long GetBySerieIdAndProductId(DataSource ds, long serieId, long productId)
        {
            return Db<DistributorMapping>.Query(ds).Select().Where(W("SerieId", serieId) & W("ProductId", productId)).Count();
        }
    }
}
