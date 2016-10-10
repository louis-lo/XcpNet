using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws.Statistic.Modules;
using Cnaws.Product;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class DistributorCategory : Cnaws.Product.Modules.ProductCategory
    {
        private static string[] GetCacheName(int id)
        {
            return new string[] { "DistributorCategory", "Module", id.ToString() };
        }
        private static void RemoveCache(int id, int parentId)
        {
            CacheProvider.Current.Set(GetCacheName(-1), null);
            if (parentId > -1)
                CacheProvider.Current.Set(GetCacheName(parentId), null);
            CacheProvider.Current.Set(GetCacheName(id), null);
        }
        protected override void RemoveCacheImpl(int id, int parentId)
        {
            RemoveCache(id, parentId);
        }

        protected override void CheckParentId(DataSource ds)
        {
            if (ParentId == 0)
                ParentId = ExecuteScalar<DistributorCategory, int>(ds, "ParentId", P("Id", Id));
        }
        protected override bool HasReferences(DataSource ds)
        {
            if (ExecuteCount<DistributorCategory>(ds, P("ParentId", Id)) > 0)
                return true;
            if (DistributorProduct.GetCountByCategoryId(ds, Id) > 0)
                return true;
            if (DistributorAttribute.GetCountByCategoryId(ds, Id) > 0)
                return true;
            return false;
        }

        public static new DistributorCategory GetById(DataSource ds, int id)
        {
            return ExecuteSingleRow<DistributorCategory>(ds, P("Id", id));
        }
        public static new IList<DistributorCategory> GetAllParentsById(DataSource ds, int id)
        {
            DistributorCategory pc;
            List<DistributorCategory> list = new List<DistributorCategory>();
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
        public static new int GetParentId(DataSource ds, int id)
        {
            return ExecuteScalar<DistributorCategory, int>(ds, "ParentId", P("Id", id));
        }
        public static new SplitPageData<DistributorCategory> GetPage(DataSource ds, int parentId, int index, int size, int show = 8)
        {
            int count;
            IList<DistributorCategory> list = ExecuteReader<DistributorCategory>(ds, Os(Od("SortNum"), Od("Id")), index, size, out count, P("ParentId", parentId));
            return new SplitPageData<DistributorCategory>(index, size, list, count, show);
        }
        public static new IList<DistributorCategory> GetAll(DataSource ds, int parentId)
        {
            string[] key = GetCacheName(parentId);
            IList<DistributorCategory> result = CacheProvider.Current.Get<IList<DistributorCategory>>(key);
            if (result == null)
            {
                if (parentId == -1)
                    result = ExecuteReader<DistributorCategory>(ds, Os(Od("SortNum"), Od("Id")));
                else
                    result = ExecuteReader<DistributorCategory>(ds, Os(Od("SortNum"), Od("Id")), P("ParentId", parentId));
                CacheProvider.Current.Set(key, result);
            }
            return result;
        }
        public static new IList<DistributorCategory> GetTop(DataSource ds, int parentId, int count)
        {
            return Db<DistributorCategory>.Query(ds)
                .Select()
                .Where(W("ParentId", parentId))
                .OrderBy(D("SortNum"), D("Id"))
                .ToList<DistributorCategory>(count);
        }
        public static new long GetCountByParent(DataSource ds, int parentId)
        {
            return ExecuteCount<DistributorCategory>(ds, P("ParentId", parentId));
        }
        public static new int GetDefaultChild(DataSource ds, int id)
        {
            return DataQuery
                .Select<DistributorCategory>(ds, "Id")
                .Where(P("ParentId", id))
                .OrderBy(Oa("SortNum"), Oa("Id"))
                .Single<int>();
        }

        public new static IList<dynamic> GetCategoryByApiProductList(DataSource ds, int categoryId, int categorylevel, FilterParameters2 parameters, int productType = 1)
        {
            DbWhereQueue where = null;
            ///关键词
            if (!string.IsNullOrEmpty(parameters.KeyWord))
            {
                foreach (string s in parameters.KeyWord.Split(' '))
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        if (where == null)
                            where = W("Title", s, DbWhereType.Like);
                        else
                            where |= W("Title", s, DbWhereType.Like);
                    }
                }
            }
            else
            {
                where = W("Title", parameters.KeyWord, DbWhereType.Like);
            }
            where &= (W("State", Cnaws.Product.Modules.ProductState.Sale) | W("State", Cnaws.Product.Modules.ProductState.BeforeSaved)) & W("ParentId", 0);
            //分类
            if (categoryId > 0)
            {
                if (categorylevel == 3)
                    where &= W("CategoryId", categoryId);
                else if (categorylevel == 2)
                    where &= (W("CategoryId", categoryId) | W("CategoryId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId", categoryId)).Result());
                else if (categorylevel == 1)
                    where &= (W("CategoryId", categoryId) | W("CategoryId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId", categoryId)).Result() | W("CategoryId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId", categoryId)).Result()).Result());
            }

            ///产品属性
            if (!string.IsNullOrEmpty(parameters.Attribute))
            {
                if (parameters.Attribute.IndexOf('@') != -1)
                {
                    string[] Attributes = parameters.Attribute.Split('@');
                    foreach (string Attr_Item in Attributes)
                    {
                        if (!string.IsNullOrEmpty(Attr_Item))
                        {
                            if (Attr_Item.IndexOf('_') != -1)
                            {
                                string[] Attr_Value = Attr_Item.Split('_');
                                if (!string.IsNullOrEmpty(Attr_Value[0]) && !string.IsNullOrEmpty(Attr_Value[1]))
                                {
                                    where &= (W("Id").InSelect<DistributorAttributeMapping>(S("ProductId")).Where(W("AttributeId", long.Parse(Attr_Value[0].ToString())) & W("Value", Attr_Value[1].ToString())).Result());
                                }
                            }
                        }
                    }
                }
            }
            //供应类型
            if (parameters.SupplierType != -1)
            {
                where &= W("SupplierType", parameters.SupplierType);
            }
            //品牌
            if (parameters.Brand > 0)
                where &= W("BrandId", parameters.Brand);
            //if (parameters.StoreId > 0)
            //{
            //    where &= W("SupplierId", parameters.StoreId);
            //    if (parameters.StoreId > 0)
            //    {
            //        where &= W("SupplierId", parameters.StoreId);
            //        if (parameters.StoreCategoryId > 0)
            //        {
            //            if (Db<StoreCategory>.Query(ds).Select(S("ParentId")).Where(W("Id", parameters.StoreCategoryId)).First<StoreCategory>().ParentId > 0)
            //            {
            //                where &= W("StoreCategoryId", parameters.StoreCategoryId);
            //            }
            //            else
            //            {
            //                where &= (W("StoreCategoryId", parameters.StoreCategoryId) | W("StoreCategoryId").InSelect<StoreCategory>(S("Id")).Where(W("ParentId", parameters.StoreCategoryId)).Result());
            //            }

            //        }
            //    }
            //}
            where &= W("ProductType", productType);

            IList<dynamic> list = Db<DistributorCategory>.Query(ds).Select(S<DistributorCategory>("*"))
                .Where(W("Id").InSelect<DistributorProduct>(S("CategoryId")).Where(where).Result())
                .ToList();
            return list;
        }

    }
}
