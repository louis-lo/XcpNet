using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Data;
using Cnaws.Web;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    /// <summary>
    /// 进货方案产品映射表
    /// </summary>
    [Serializable]
    public class ProgrammeProductMapping : NoIdentityModule
    {
        /// <summary>
        /// 方案Id
        /// </summary>
        [DataColumn(true)]
        public long ProgrammeId = 0;
        /// <summary>
        /// 产品Id
        /// </summary>
        [DataColumn(true)]
        public long ProductId = 0;
        /// <summary>
        /// 方案中产品数量
        /// </summary>
        public int Count = 0;

        private static string[] GetCacheName(long id)
        {
            return new string[] { "Programme", "Module", id.ToString() };
        }
        private static string[] ApiGetCacheName(long id)
        {
            return new string[] { "ApiProgramme", "Module", id.ToString() };
        }
        private static void RemoveCache(long id)
        {
            CacheProvider.Current.Set(ApiGetCacheName(id), null);
            CacheProvider.Current.Set(GetCacheName(id), null);
        }
        protected void RemoveCacheImpl(long id)
        {
            RemoveCache(id);
        }
        private void RemoveCache()
        {
            RemoveCacheImpl(ProgrammeId);
        }
        protected override DataStatus OnInsertAfter(DataSource ds)
        {
            RemoveCache();
            return DataStatus.Success;
        }
        protected override DataStatus OnDeleteAfter(DataSource ds)
        {
            RemoveCache();
            return DataStatus.Success;
        }
        protected override DataStatus OnUpdateAfter(DataSource ds)
        {
            RemoveCache();
            return DataStatus.Success;
        }
        /// <summary>
        /// 根据方案Id获取方案内产品信息
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="programmeid"></param>
        /// <returns></returns>
        public static IList<DataJoin<ProgrammeProductMapping, DistributorProduct>> GetAllById(DataSource ds, long programmeid)
        {
             string[] key = GetCacheName(programmeid);
            IList<DataJoin<ProgrammeProductMapping, DistributorProduct>> result = CacheProvider.Current.Get<IList<DataJoin<ProgrammeProductMapping, DistributorProduct>>>(key);
            if (result == null)
            {
                result = Db<ProgrammeProductMapping>.Query(ds)
                 .Select(S<ProgrammeProductMapping>("*"), S<DistributorProduct>("*"))
                 .InnerJoin(O<ProgrammeProductMapping>("ProductId"), O<DistributorProduct>("Id"))
                 .Where(W<ProgrammeProductMapping>("ProgrammeId", programmeid))
                 .ToList<DataJoin<ProgrammeProductMapping, DistributorProduct>>();
                CacheProvider.Current.Set(key, result);
            }
            return result;
        }
        /// <summary>
        /// 根据方案Id获取方案内产品信息
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="programmeid"></param>
        /// <returns></returns>
        public static SplitPageData<DataJoin<ProgrammeProductMapping, DistributorProduct>> GetAllPageById(DataSource ds, long programmeid,int state, int index,int size,int show=11)
        {
            long count;
            DbWhereQueue where = W<ProgrammeProductMapping>("ProgrammeId", programmeid);
            if (state == 1)
                where &= (W<DistributorProduct>("State", Cnaws.Product.Modules.ProductState.Sale)| W<DistributorProduct>("State", Cnaws.Product.Modules.ProductState.BeforeSaved));
            else if (state == 0)
                where &= (W<DistributorProduct>("State", Cnaws.Product.Modules.ProductState.BeforeSale) | W<DistributorProduct>("State", Cnaws.Product.Modules.ProductState.Saved)| W<DistributorProduct>("State", Cnaws.Product.Modules.ProductState.Deleted));
            IList<DataJoin<ProgrammeProductMapping, DistributorProduct>> result;
            result = Db<ProgrammeProductMapping>.Query(ds)
             .Select(S<ProgrammeProductMapping>("*"), S<DistributorProduct>("*"))
             .InnerJoin(O<ProgrammeProductMapping>("ProductId"), O<DistributorProduct>("Id"))
             .Where(where)
             .OrderBy(D("SortNum"))
             .ToList<DataJoin<ProgrammeProductMapping, DistributorProduct>>(size,index,out count);

            return new SplitPageData<DataJoin<ProgrammeProductMapping, DistributorProduct>>(index, size, result, count, show);
        }
        /// <summary>
        /// 根据方案Id获取方案内产品信息
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="programmeid"></param>
        /// <returns></returns>
        public static IList<dynamic> ApiGetAllById(DataSource ds, long programmeid)
        {
            string[] key = ApiGetCacheName(programmeid);
            IList<dynamic> result = CacheProvider.Current.Get<IList<dynamic>>(key);
            if (result == null)
            {
                result = Db<ProgrammeProductMapping>.Query(ds)
                 .Select(S<ProgrammeProductMapping>("*"), S<DistributorProduct>("*"))
                 .InnerJoin(O<ProgrammeProductMapping>("ProductId"), O<DistributorProduct>("Id"))
                 .Where(W<ProgrammeProductMapping>("ProgrammeId", programmeid))
                 .ToList();
                CacheProvider.Current.Set(key, result);
            }
            return result;
        }

        public static DataStatus UpdataCount(DataSource ds, long programmeId,long productId,int count)
        {
            if (Db<ProgrammeProductMapping>.Query(ds).Update().Set("Count", count)
               .Where(W("ProgrammeId", programmeId) & W("ProductId", productId)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }
        public static DataStatus Del(DataSource ds, long programmeId, long productId)
        {
            if (Db<ProgrammeProductMapping>.Query(ds).Delete()
               .Where(W("ProgrammeId", programmeId) & W("ProductId", productId)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }

        public static DataStatus Add(DataSource ds, long programmeId, long productId,int count)
        {
           return new ProgrammeProductMapping() { ProgrammeId = programmeId, ProductId = productId, Count = count }.Insert(ds);
        }

        public static bool ExistsParent(DataSource ds,long programmeid,long productid)
        {
            return Db<ProgrammeProductMapping>.Query(ds).Select().Where(W("ProgrammeId", programmeid) & W("ProductId")
                .InSelect<DistributorProduct>("Id").Where(W("Id", productid) | W("ParentId", productid)).Result())
                .First<ProgrammeProductMapping>() != null;
        }
        public static bool Exists(DataSource ds, long programmeid, long productid)
        {
            return Db<ProgrammeProductMapping>.Query(ds).Select().Where(W("ProgrammeId", programmeid) 
                & W("ProductId",productid))
                .First<ProgrammeProductMapping>() != null;
        }

    }
}
