using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class DistributorSerie : Cnaws.Product.Modules.ProductSerie
    {
        protected override DataStatus DeleteMapping(DataSource ds)
        {
            return DistributorMapping.DeleteBySerie(ds, Id);
        }


        public static new DataStatus Delete(DataSource ds, long id)
        {
            ds.Begin();
            try
            {
                Db<DistributorMapping>.Query(ds).Delete().Where(W("SerieId", id)).Execute();
                if (Db<DistributorSerie>.Query(ds).Delete().Where(W("Id", id)).Execute() != 1)
                    throw new Exception();
                ds.Commit();
                return DataStatus.Success;
            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Rollback;
            }
            //return (new DistributorSerie() { Id = id }).Delete(ds);
        }

        public static new IList<DistributorSerie> GetAll(DataSource ds, long productId)
        {
            return Db<DistributorSerie>.Query(ds)
                    .Select()
                    .Where(W("ProductId", productId))
                    .ToList<DistributorSerie>();
        }
        public static IList<DistributorSerie> GetAllByProducts(DataSource ds, object[] productIds)
        {
            return Db<DistributorSerie>.Query(ds)
                    .Select()
                    .Where(W("ProductId", productIds, DbWhereType.In))
                    .ToList<DistributorSerie>();
        }

        public new IList<DistributorMapping> GetMappings(DataSource ds)
        {
            return DistributorMapping.GetAllBySerie(ds, Id);
        }
        public new static DistributorSerie GetByProductAndName(DataSource ds, long productId, string name)
        {
            return Db<DistributorSerie>.Query(ds)
                    .Select()
                    .Where(W("ProductId", productId) & W("Name", name))
                    .First<DistributorSerie>();
        }

        /// <summary>
        /// 是否存在相同属性
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="productId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public new static bool Exists(DataSource ds, long productId, string name)
        {
            return Db<DistributorSerie>.Query(ds).Select().Where(W("ProductId", productId) & W("Name", name)).Count() > 0;
        }
    }
}
