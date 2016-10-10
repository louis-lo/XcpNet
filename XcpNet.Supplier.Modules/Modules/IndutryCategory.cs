using System;
using System.Collections.Generic;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    /// <summary>
    /// 行业分类
    /// </summary>
    [Serializable]
    public class IndutryCategory : IdentityModule
    {
        [DataColumn(16)]
        public string Name = null;
        [DataColumn(256)]
        public string Image = null;
        public int ParentId = 0;
        /// <summary>
        /// 显示品牌Logo还是文章
        /// </summary>
        public bool ShowLogo = false;
        public int SortNum = 0;

        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "ParentId");
        }
        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "ParentId", "ParentId");
        }

        private static string[] GetCacheName(int id)
        {
            return new string[] { "IndutryCategory", "Module", id.ToString() };
        }
        private static void RemoveCache(int id, int parentId)
        {
            CacheProvider.Current.Set(GetCacheName(-1), null);
            if (parentId > -1)
                CacheProvider.Current.Set(GetCacheName(parentId), null);
            CacheProvider.Current.Set(GetCacheName(id), null);
        }
        protected virtual void RemoveCacheImpl(int id, int parentId)
        {
            RemoveCache(id, parentId);
        }
        private void RemoveCache()
        {
            RemoveCacheImpl(Id, ParentId);
        }

        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (string.IsNullOrEmpty(Name))
                return DataStatus.Failed;
            return DataStatus.Success;
        }
        protected override DataStatus OnInsertAfter(DataSource ds)
        {
            RemoveCache();
            return DataStatus.Success;
        }
        protected virtual void CheckParentId(DataSource ds)
        {
            if (ParentId == 0)
                ParentId = ExecuteScalar<IndutryCategory, int>(ds, "ParentId", P("Id", Id));
        }


        protected override DataStatus OnUpdateBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            columns = Exclude(columns, mode, "ParentId");
            if (string.IsNullOrEmpty(Name))
                return DataStatus.Failed;
            CheckParentId(ds);
            return DataStatus.Success;
        }
        protected override DataStatus OnUpdateAfter(DataSource ds)
        {
            RemoveCache();
            return DataStatus.Success;
        }
        protected override DataStatus OnDeleteBefor(DataSource ds, ref DataColumn[] columns)
        {
            CheckParentId(ds);
            return DataStatus.Success;
        }
        protected override DataStatus OnDeleteAfter(DataSource ds)
        {
            RemoveCache();
            return DataStatus.Success;
        }

        public static IndutryCategory GetById(DataSource ds, int id)
        {
            return ExecuteSingleRow<IndutryCategory>(ds, P("Id", id));
        }
        public static IList<IndutryCategory> GetAllParentsById(DataSource ds, int id)
        {
            IndutryCategory pc;
            List<IndutryCategory> list = new List<IndutryCategory>();
            while (id > 0)
            {
                pc = GetById(ds, id);
                if (pc != null)
                {
                    list.Insert(0, pc);
                    id = pc.ParentId;
                }
                else
                {
                    break;
                }
            }
            return list;
        }
        public static int GetParentId(DataSource ds, int id)
        {
            return ExecuteScalar<IndutryCategory, int>(ds, "ParentId", P("Id", id));
        }
        public static SplitPageData<IndutryCategory> GetPage(DataSource ds, int parentId, int index, int size, int show = 8)
        {
            int count;
            IList<IndutryCategory> list = ExecuteReader<IndutryCategory>(ds, Os(Oa("SortNum"), Oa("Id")), index, size, out count, P("ParentId", parentId));
            return new SplitPageData<IndutryCategory>(index, size, list, count, show);
        }
        public static IList<IndutryCategory> GetAll(DataSource ds, int parentId)
        {
            string[] key = GetCacheName(parentId);
            IList<IndutryCategory> result = CacheProvider.Current.Get<IList<IndutryCategory>>(key);
            if (result == null)
            {
                if (parentId == -1)
                    result = ExecuteReader<IndutryCategory>(ds);
                else
                    result = ExecuteReader<IndutryCategory>(ds, Os(Oa("SortNum"), Oa("Id")), P("ParentId", parentId));
                CacheProvider.Current.Set(key, result);
            }
            return result;
        }
        public static IList<IndutryCategory> GetTop(DataSource ds, int parentId, int count)
        {
            return Db<IndutryCategory>.Query(ds)
                .Select()
                .Where(W("ParentId", parentId))
                .OrderBy(A("SortNum"), A("Id"))
                .ToList<IndutryCategory>(count);
        }
        public static long GetCountByParent(DataSource ds, int parentId)
        {
            return ExecuteCount<IndutryCategory>(ds, P("ParentId", parentId));
        }
        public static int GetDefaultChild(DataSource ds, int id)
        {
            return DataQuery
                .Select<IndutryCategory>(ds, "Id")
                .Where(P("ParentId", id))
                .OrderBy(Oa("SortNum"), Oa("Id"))
                .Single<int>();
        }
    }
}
