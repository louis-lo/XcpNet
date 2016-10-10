using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class DistributorAttribute : Cnaws.Product.Modules.ProductAttribute
    {
        private static string[] GetCacheName(int category)
        {
            return new string[] { "DistributorAttribute", "Module", category.ToString() };
        }
        private static void RemoveCache(int category)
        {
            CacheProvider.Current.Set(GetCacheName(category), null);
        }
        protected override void RemoveCacheImpl(int category)
        {
            RemoveCache(category);
        }

        protected override void CheckCategoryId(DataSource ds)
        {
            if (CategoryId == 0)
                CategoryId = ExecuteScalar<DistributorCategory, int>(ds, "Id", P("Id", Id));
        }
        protected override bool HasReferences(DataSource ds)
        {
            if (DistributorAttributeMapping.GetCountByAttributeId(ds, Id) > 0)
                return true;
            return false;
        }

        public static new long GetCountByCategoryId(DataSource ds, int categoryId)
        {
            return ExecuteCount<DistributorAttribute>(ds, P("CategoryId", categoryId));
        }
        public static new DistributorAttribute GetById(DataSource ds, int id)
        {
            return ExecuteSingleRow<DistributorAttribute>(ds, P("Id", id));
        }
        public static new SplitPageData<DistributorAttribute> GetPage(DataSource ds, int categoryId, int index, int size, int show = 8)
        {
            int count;
            IList<DistributorAttribute> list;
            if (categoryId > 0)
                list = ExecuteReader<DistributorAttribute>(ds, Os(Oa("SortNum"), Oa("Id")), index, size, out count, P("CategoryId", categoryId));
            else
                list = ExecuteReader<DistributorAttribute>(ds, Os(Oa("SortNum"), Oa("Id")), index, size, out count);
            return new SplitPageData<DistributorAttribute>(index, size, list, count, show);
        }
        public static new IList<DistributorAttribute> GetAllByCategory(DataSource ds, int categoryId)
        {
            string[] key;
            IList<DistributorAttribute> value;
            List<DistributorAttribute> result = new List<DistributorAttribute>();
            foreach (DistributorCategory cate in DistributorCategory.GetAllParentsById(ds, categoryId))
            {
                key = GetCacheName(cate.Id);
                value = CacheProvider.Current.Get<IList<DistributorAttribute>>(key);
                if (value == null)
                {
                    value = ExecuteReader<DistributorAttribute>(ds, Os(Oa("SortNum"), Oa("Id")), P("CategoryId", cate.Id)& P("Screen", true));
                    CacheProvider.Current.Set(key, value);
                }
                result.AddRange(value);
            }
            return result;
        }

        public new IList<DistributorAttributeMapping> GetAllValuesByCategory(DataSource ds)
        {
            return DistributorAttributeMapping.GetAllByAttribute(ds, Id);
        }
        public static new IList<DataJoin<DistributorAttribute, DistributorAttributeMapping>> GetAllValuesByProduct(DataSource ds, long productId)
        {
            return DataQuery
                .Select<DistributorAttribute>(ds, C<DistributorAttribute>("Name"), C<DistributorAttributeMapping>("Value"))
                .Join<DistributorAttribute>("Id", DataJoinType.Right).On<DistributorAttributeMapping>("AttributeId")
                .Where(P<DistributorAttributeMapping>("ProductId", productId))
                .ToList<DataJoin<DistributorAttribute, DistributorAttributeMapping>>();
        }
    }
}
