using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    /// <summary>
    /// 课程
    /// </summary>
    [Serializable]
    public sealed class Courses : IdentityModule
    {
        [DataColumn(32)]
        public string Name = null;

        private static string[] GetCacheName()
        {
            return new string[] { "Courses", "Module" };
        }
        private static void RemoveCache()
        {
            CacheProvider.Current.Set(GetCacheName(), null);
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
        protected override DataStatus OnUpdateBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (string.IsNullOrEmpty(Name))
                return DataStatus.Failed;
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

        public static IList<Courses> GetAll(DataSource ds)
        {
            string[] key = GetCacheName();
            IList<Courses> result = CacheProvider.Current.Get<IList<Courses>>(key);
            if (result == null)
            {
                result = Db<Courses>.Query(ds)
                    .Select()
                    .ToList<Courses>();
                CacheProvider.Current.Set(key, result);
            }
            return result;
        }
    }
}
