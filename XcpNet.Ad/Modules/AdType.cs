using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace XcpNet.Ad.Modules
{
    [Serializable]
    public class AdType : LongIdentityModule
    {
        [Serializable]
        public enum EAdType
        {
            /// <summary>
            /// 不限
            /// </summary>
            All = -1,
            /// <summary>
            /// 网站广告位
            /// </summary>
            WebSite = 0,
            /// <summary>
            /// 供应商店铺广告
            /// </summary>
            SupplierStore,
            /// <summary>
            /// 乡道管店铺广告位
            /// </summary>
            TownshipStore,
            /// <summary>
            /// 产品分类广告位
            /// </summary>
            Category,
            /// <summary>
            /// 会员中心
            /// </summary>
            Passport,
            /// <summary>
            /// 加盟商供应商中心
            /// </summary>
            Supplier,
            /// <summary>
            /// 购物机广告
            /// </summary>
            Machine,
            /// <summary>
            /// APP广告
            /// </summary>
            App,
            /// <summary>
            /// 进货分类广告
            /// </summary>
            DsitributorCategory

        }
        /// <summary>
        /// 广告类型名称
        /// </summary>
        [DataColumn(128)]
        public string Name = string.Empty;
        /// <summary>
        /// 广告类型
        /// </summary>
        public EAdType Type = EAdType.WebSite;
        /// <summary>
        /// 当前类型最大数量
        /// </summary>
        public int MaxSum = 0;
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnable = true;

        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "Type", "Type");
            CreateIndex(ds, "MaxSum", "Id", "MaxSum");
        }

        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "MaxSum");
            DropIndex(ds, "Type");
        }
        private static string[] GetCacheName(long id)
        {
            return new string[] { "AdType", "Module", id.ToString() };
        }
        private static void RemoveCache(long id, int typeId)
        {
            CacheProvider.Current.Set(GetCacheName(-1), null);
            if (typeId > -1)
                CacheProvider.Current.Set(GetCacheName(typeId), null);
            CacheProvider.Current.Set(GetCacheName(id), null);
        }
        protected virtual void RemoveCacheImpl(long id, int typeId)
        {
            RemoveCache(id, typeId);
        }
        private void RemoveCache()
        {
            RemoveCacheImpl(Id, (int)Type);
        }
        protected override DataStatus OnInsertAfter(DataSource ds)
        {
            RemoveCache();
            return DataStatus.Success;
        }
        protected override DataStatus OnUpdateAfter(DataSource ds)
        {
            RemoveCache();
            return DataStatus.Success;
        }
        protected override DataStatus OnDeleteAfter(DataSource ds)
        {
            RemoveCache();
            return DataStatus.Success;
        }
        public static AdType GetById(DataSource ds, long id)
        {
            return ExecuteSingleRow<AdType>(ds, P("Id", id));
        }
        public static SplitPageData<AdType> GetPage(DataSource ds, EAdType adtype, int index, int size, int show = 8)
        {
            int count;
            IList<AdType> list = null;
            if (adtype != EAdType.All) list = ExecuteReader<AdType>(ds, Os(Oa("Id")), index, size, out count, P("Type", adtype));
            else list = ExecuteReader<AdType>(ds, Os(Oa("Id")), index, size, out count);
            return new SplitPageData<AdType>(index, size, list, count, show);
        }

        public static IList<AdType> GetAll(DataSource ds, int type)
        {
            string[] key = GetCacheName(type);
            IList<AdType> result = CacheProvider.Current.Get<IList<AdType>>(key);
            if (result == null)
            {
                if (type == -1)
                    result = ExecuteReader<AdType>(ds);
                else
                    result = Db<AdType>.Query(ds).Select().Where(W("Type", (EAdType)type)).ToList<AdType>();
                CacheProvider.Current.Set(key, result);
            }
            return result;
        }

    }
}
