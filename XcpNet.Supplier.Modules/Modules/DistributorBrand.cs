using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    /// <summary>
    /// 品牌
    /// </summary>
    [Serializable]
    public sealed class DistributorBrand : Cnaws.Product.Modules.ProductBrand
    {

        private static string[] GetCacheName(int category)
        {
            return new string[] { "DistributorBrand", "Module", category.ToString() };
        }
        private static string[] GetRecommendCacheName(int category)
        {
            return new string[] { "DistributorBrand", "Module", category.ToString(), "1" };
        }
        private static string[] GetParentRecommendCacheName(int category)
        {
            return new string[] { "DistributorBrand", "Module", category.ToString(), "2" };
        }
        private static string[] GetFirstCharCacheName(int category)
        {
            return new string[] { "DistributorBrand", "Module", category.ToString(), "3" };
        }
        private static void RemoveParentRecommendCache(DataSource ds, int category)
        {
            CacheProvider.Current.Set(GetParentRecommendCacheName(category), null);
            int parent = DistributorCategory.GetParentId(ds, category);
            if (parent > 0)
                RemoveParentRecommendCache(ds, category);
        }
        private static void RemoveCache(DataSource ds, int category)
        {
            CacheProvider.Current.Set(GetCacheName(category), null);
            CacheProvider.Current.Set(GetRecommendCacheName(category), null);
            RemoveParentRecommendCache(ds, category);
            CacheProvider.Current.Set(GetFirstCharCacheName(category), null);
        }
        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (string.IsNullOrEmpty(Name))
                return DataStatus.Failed;
            if (string.IsNullOrEmpty(FirstChar))
                return DataStatus.Failed;
            //if (ProductCategory.GetCountByParent(ds, CategoryId) > 0)
            //    return DataStatus.Exist;
            FirstChar = FirstChar.ToUpper();
            return DataStatus.Success;
        }
        protected override DataStatus OnInsertAfter(DataSource ds)
        {
            RemoveCache(ds, CategoryId);
            return DataStatus.Success;
        }
        private void CheckCategoryId(DataSource ds)
        {
            if (CategoryId == 0)
                CategoryId = ExecuteScalar<DistributorCategory, int>(ds, "CategoryId", P("Id", Id));
        }
        protected override DataStatus OnUpdateBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (string.IsNullOrEmpty(Name))
                return DataStatus.Failed;
            if (string.IsNullOrEmpty(FirstChar))
                return DataStatus.Failed;
            FirstChar = FirstChar.ToUpper();
            //CheckCategoryId(ds);
            //if (ProductCategory.GetCountByParent(ds, CategoryId) > 0)
            //    return DataStatus.Exist;
            return DataStatus.Success;
        }
        protected override DataStatus OnUpdateAfter(DataSource ds)
        {
            RemoveCache(ds, CategoryId);
            return DataStatus.Success;
        }
        protected override DataStatus OnDeleteBefor(DataSource ds, ref DataColumn[] columns)
        {
            if (DistributorProduct.GetCountByBrandId(ds, Id) > 0)
                return DataStatus.Exist;
            //CheckCategoryId(ds);
            return DataStatus.Success;
        }
        protected override DataStatus OnDeleteAfter(DataSource ds)
        {
            RemoveCache(ds, CategoryId);
            return DataStatus.Success;
        }

        public new static long GetCountByCategoryId(DataSource ds, int categoryId)
        {
            return ExecuteCount<DistributorBrand>(ds, P("CategoryId", categoryId));
        }
        public new static DistributorBrand GetById(DataSource ds, int id)
        {
            return ExecuteSingleRow<DistributorBrand>(ds, P("Id", id));
        }
        public new static SplitPageData<DistributorBrand> GetPage(DataSource ds, bool approved, int index, int size, int show = 8)
        {
            int count;
            IList<DistributorBrand> list = ExecuteReader<DistributorBrand>(ds, Os(Oa("SortNum"), Oa("Id")), index, size, out count, P("Approved", approved));
            return new SplitPageData<DistributorBrand>(index, size, list, count, show);
        }
        public new static SplitPageData<dynamic> GetPageEx(DataSource ds, int categoryId, string sort, string order, int index, int size, int show = 8)
        {
            long count;
            IList<dynamic> list;
            if (categoryId > 0)
            {
                if ("desc".Equals(order, StringComparison.OrdinalIgnoreCase))
                {
                    if ("approved".Equals(sort, StringComparison.OrdinalIgnoreCase))
                        list = Db<DistributorBrand>.Query(ds)
                            .Select(S<DistributorBrand>())
                            .InnerJoin(O<DistributorBrand>("Id"), O<DistributorBrandMapping>("BrandId"))
                            .Where(W<DistributorBrandMapping>("CategoryId", categoryId))
                            .OrderBy(D<DistributorBrand>("Approved"), A<DistributorBrand>("SortNum"), A<DistributorBrand>("Id"))
                            .ToList(size, index, out count);
                    //list = ExecuteReader<DistributorBrand>(ds, Os(Od("Approved"), Oa("SortNum"), Oa("Id")), index, size, out count, P("CategoryId", categoryId));
                    else
                        list = Db<DistributorBrand>.Query(ds)
                            .Select(S<DistributorBrand>())
                            .InnerJoin(O<DistributorBrand>("Id"), O<DistributorBrandMapping>("BrandId"))
                            .Where(W<DistributorBrandMapping>("CategoryId", categoryId))
                            .OrderBy(D<DistributorBrand>("SortNum"), A<DistributorBrand>("Id"))
                            .ToList(size, index, out count);
                    //list = ExecuteReader<DistributorBrand>(ds, Os(Od("SortNum"), Oa("Id")), index, size, out count, P("CategoryId", categoryId));
                }
                else
                {
                    if ("approved".Equals(sort, StringComparison.OrdinalIgnoreCase))
                        list = Db<DistributorBrand>.Query(ds)
                            .Select(S<DistributorBrand>())
                            .InnerJoin(O<DistributorBrand>("Id"), O<DistributorBrandMapping>("BrandId"))
                            .Where(W<DistributorBrandMapping>("CategoryId", categoryId))
                            .OrderBy(A<DistributorBrand>("Approved"), A<DistributorBrand>("SortNum"), A<DistributorBrand>("Id"))
                            .ToList(size, index, out count);
                    //list = ExecuteReader<DistributorBrand>(ds, Os(Oa("Approved"), Oa("SortNum"), Oa("Id")), index, size, out count, P("CategoryId", categoryId));
                    else
                        list = Db<DistributorBrand>.Query(ds)
                            .Select(S<DistributorBrand>())
                            .InnerJoin(O<DistributorBrand>("Id"), O<DistributorBrandMapping>("BrandId"))
                            .Where(W<DistributorBrandMapping>("CategoryId", categoryId))
                            .OrderBy(A<DistributorBrand>("SortNum"), A<DistributorBrand>("Id"))
                            .ToList(size, index, out count);
                    //list = ExecuteReader<DistributorBrand>(ds, Os(Oa("SortNum"), Oa("Id")), index, size, out count, P("CategoryId", categoryId));
                }
            }
            else
            {
                if ("desc".Equals(order, StringComparison.OrdinalIgnoreCase))
                {
                    if ("approved".Equals(sort, StringComparison.OrdinalIgnoreCase))
                        list = Db<DistributorBrand>.Query(ds)
                            .Select()
                            .OrderBy(D("Approved"), A("SortNum"), A("Id"))
                            .ToList(size, index, out count);
                    //list = ExecuteReader<DistributorBrand>(ds, Os(Od("Approved"), Oa("SortNum"), Oa("Id")), index, size, out count);
                    else
                        list = Db<DistributorBrand>.Query(ds)
                            .Select()
                            .OrderBy(D("SortNum"), A("Id"))
                            .ToList(size, index, out count);
                    //list = ExecuteReader<DistributorBrand>(ds, Os(Od("SortNum"), Oa("Id")), index, size, out count);
                }
                else
                {
                    if ("approved".Equals(sort, StringComparison.OrdinalIgnoreCase))
                        list = Db<DistributorBrand>.Query(ds)
                            .Select()
                            .OrderBy(A("Approved"), A("SortNum"), A("Id"))
                            .ToList(size, index, out count);
                    //list = ExecuteReader<DistributorBrand>(ds, Os(Oa("Approved"), Oa("SortNum"), Oa("Id")), index, size, out count);
                    else
                        list = Db<DistributorBrand>.Query(ds)
                            .Select()
                            .OrderBy(A("SortNum"), A("Id"))
                            .ToList(size, index, out count);
                    //list = ExecuteReader<DistributorBrand>(ds, Os(Oa("SortNum"), Oa("Id")), index, size, out count);
                }
            }
            foreach (var item in list)
                item.Categorys = DistributorBrandMapping.GetByBrandId(ds, item.Id);
            return new SplitPageData<dynamic>(index, size, list, count, show);
        }
        public new static IList<DistributorBrand> GetAllByCategory(DataSource ds, int categoryId)
        {
            string[] key = GetCacheName(categoryId);
            IList<DistributorBrand> result = null; //CacheProvider.Current.Get<IList<ProductBrand>>(key);
            if (result == null)
            {
                //result = ExecuteReader<ProductBrand>(ds, Os(Oa("SortNum"), Oa("Id")), P("Approved", true) & P("CategoryId", categoryId));
                result = DistributorBrandMapping.GetBrandListByCategoryId(ds, categoryId);
                //CacheProvider.Current.Set(key, result);
            }
            return result;

        }
        public new static IList<DistributorBrand> GetAllByCategoryAndScreen(DataSource ds, int categoryId)
        {
            string[] key = GetCacheName(categoryId);
            IList<DistributorBrand> result = null; //CacheProvider.Current.Get<IList<ProductBrand>>(key);
            if (result == null)
            {
                //result = ExecuteReader<ProductBrand>(ds, Os(Oa("SortNum"), Oa("Id")), P("Approved", true) & P("CategoryId", categoryId));
                result = DistributorBrandMapping.GetBrandListByCategoryIdAndScreen(ds, categoryId);
                //CacheProvider.Current.Set(key, result);
            }
            return result;

        }
        public new static IList<DistributorBrand> GetAllRecommendByCategory(DataSource ds, int categoryId)
        {
            string[] key = GetRecommendCacheName(categoryId);
            IList<DistributorBrand> result = CacheProvider.Current.Get<IList<DistributorBrand>>(key);
            if (result == null)
            {
                result = ExecuteReader<DistributorBrand>(ds, Os(Oa("SortNum"), Oa("Id")), P("Approved", true) & P("Recommend", true) & P("CategoryId", categoryId));
                CacheProvider.Current.Set(key, result);
            }
            return result;
        }
        private static void FillRecommend(DataSource ds, int categoryId, List<DistributorBrand> list)
        {
            list.AddRange(ExecuteReader<DistributorBrand>(ds, Os(Oa("SortNum"), Oa("Id")), P("Approved", true) & P("Recommend", true) & P("CategoryId", categoryId)));
            foreach (DistributorCategory item in DistributorCategory.GetAll(ds, categoryId))
                FillRecommend(ds, item.Id, list);
        }
        public new static IList<DistributorBrand> GetAllRecommendByParentCategory(DataSource ds, int categoryId)
        {
            string[] key = GetParentRecommendCacheName(categoryId);
            IList<DistributorBrand> result = CacheProvider.Current.Get<IList<DistributorBrand>>(key);
            if (result == null)
            {
                result = new List<DistributorBrand>();
                FillRecommend(ds, categoryId, (List<DistributorBrand>)result);
                CacheProvider.Current.Set(key, result);
            }
            return result;
        }
        public new static SortedDictionary<string, List<DistributorBrand>> GetAllFirstCharByCategory(DataSource ds, int categoryId)
        {
            string[] key = GetFirstCharCacheName(categoryId);
            SortedDictionary<string, List<DistributorBrand>> dict = CacheProvider.Current.Get<SortedDictionary<string, List<DistributorBrand>>>(key);
            if (dict == null)
            {
                List<DistributorBrand> temp;
                IList<DistributorBrand> list = GetAllByCategory(ds, categoryId);
                dict = new SortedDictionary<string, List<DistributorBrand>>(StringComparer.InvariantCulture);
                foreach (DistributorBrand pb in list)
                {
                    if (dict.TryGetValue(pb.FirstChar, out temp))
                    {
                        temp.Add(pb);
                    }
                    else
                    {
                        temp = new List<DistributorBrand>();
                        temp.Add(pb);
                        dict.Add(pb.FirstChar, temp);
                    }
                }
                CacheProvider.Current.Set(key, dict);
            }
            return dict;
        }
    }
}
