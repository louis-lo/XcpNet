using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws.Json;
using Cnaws;
using Cnaws.Product.Modules;
using Cnaws.Product;
using Cnaws.Statistic.Modules;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class DistributorProduct : Cnaws.Product.Modules.Product
    {
        /// <summary>
        /// 零售单位
        /// </summary>
        [DataColumn(8)]
        public string RetailUnit = null;

        public string GetStateString()
        {
            switch (State)
            {
                case ProductState.BeforeSale:
                    return "准备上架";
                case ProductState.BeforeSaved:
                    return "准备下架";
                case ProductState.Deleted:
                    return "已删除";
                case ProductState.Sale:
                    return "已上架";
                case ProductState.Saved:
                    return "未上架";
                default:
                    return "错误的状态";
            }
        }

        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (DistributorCategory.GetCountByParent(ds, CategoryId) > 0)
                return DataStatus.Exist;
            if (string.IsNullOrEmpty(Title))
                return DataStatus.Failed;
            if (string.IsNullOrEmpty(Content))
                return DataStatus.Failed;
            return DataStatus.Success;
        }
        protected override DataStatus OnUpdateBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            columns = Exclude(columns, mode, "SupplierId", "ParentId");
            if (Include(columns, mode, "CategoryId") && DistributorCategory.GetById(ds, CategoryId) == null)
                return DataStatus.Failed;
            if (Include(columns, mode, "Title") && string.IsNullOrEmpty(Title))
                return DataStatus.Failed;
            if (Include(columns, mode, "Content") && string.IsNullOrEmpty(Content))
                return DataStatus.Failed;
            return DataStatus.Success;
        }
        public new string GetAttributes(DataSource ds)
        {
            return JsonValue.Serialize(DistributorMapping.GetAllByProduct(ds, Id));
        }

        public Money GetSalePrice(bool first)
        {
            if (first)
                return Price;
            return WholesalePrice;
        }
        public new static DataStatus ModfiyByParentId(DataSource ds, DistributorProduct product)
        {
            long parentId = product.ParentId > 0 ? product.ParentId : product.Id;
            if (Db<DistributorProduct>.Query(ds).Update()
                .Set("Image", product.Image)
                .Set("State", product.State)
                .Set("Title", product.Title)
                .Set("Content", product.Content)
                .Set("Keywords", product.Keywords)
                .Set("Description", product.Description)
                .Set("BarCode", product.BarCode)
                .Set("Unit", product.Unit)
                .Set("RetailUnit", product.RetailUnit)
                .Set("Inventory", product.Inventory)
                .Set("InventoryAlert", product.InventoryAlert)
                .Set("Province", product.Province)
                .Set("City", product.City)
                .Set("County", product.County)
                .Set("FreightType", product.FreightType)
                .Set("FreightMoney", product.FreightMoney)
                .Set("FreightTemplate", product.FreightTemplate)
                .Set("HasReceipt", product.HasReceipt)
                .Set("StoreCategoryId", product.StoreCategoryId)
                .Set("Settlement", product.Settlement)
                .Set("RoyaltyRate", product.RoyaltyRate)
                .Set("ProductSupplier", product.ProductSupplier)
                .Set("Weight", product.Weight)
                .Set("Norms", product.Norms)                
                .Set("Volume", product.Volume)
                .Where((W("ParentId", parentId) | W("Id", parentId)) & W("State", ProductState.Deleted, DbWhereType.NotEqual))
                .Execute() > 0
                )
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }
        public new static IList<DistributorProduct> GetTopRecommendByCategory(DataSource ds, int count, int categoryId = 0, int productType = 1)
        {
            DbWhereQueue where = GetStateWhereQueue();
            if (categoryId > 0)
                where &= W("CategoryId", categoryId) & W("ParentId", 0);
            where &= W("Recommend", true);
            where &= W("ProductType", productType);
            return Db<DistributorProduct>.Query(ds)
                .Select()
                .Where(where)
                .OrderBy(D("SortNum"), D("SaleTime"), D("Id"))
                .ToList<DistributorProduct>(count);
        }

        public new static long[] GetAllIdsByParentId(DataSource ds, long parentId)
        {
            return Db<DistributorProduct>.Query(ds).Select(S("Id")).Where(W("ParentId", parentId) | W("Id", parentId)).ToArray<long>();
        }
        /// <summary>
        /// 市场价
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="province"></param>
        /// <param name="city"></param>
        /// <param name="county"></param>
        /// <returns></returns>
        public Money GetPrice(DataSource ds, int province, int city, int county)
        {
            DistributorAreaMapping areamapping = Db<DistributorAreaMapping>.Query(ds).Select().Where(W("ProductId", Id) & W("Province", province) & W("City", city) & W("County", county)).First<DistributorAreaMapping>();
            if (areamapping != null && areamapping.Price > 0)
                return areamapping.Price;
            return Price;
        }
        /// <summary>
        /// 批发价
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="province"></param>
        /// <param name="city"></param>
        /// <param name="county"></param>
        /// <returns></returns>
        public Money GetSalePrice(DataSource ds, int province, int city, int county)
        {
            DistributorAreaMapping areamapping = Db<DistributorAreaMapping>.Query(ds).Select().Where(W("ProductId", Id) & W("Province", province) & W("City", city) & W("County", county)).First<DistributorAreaMapping>();
            if (areamapping != null && areamapping.DotPrice > 0)
                return areamapping.DotPrice;
            return WholesalePrice;
        }
        public Money GetTotalMoney(bool first, int count)
        {
            if (IsPublish())
                return GetSalePrice(first) * count;
            return 0;
        }

        public new static DataStatus ModfiyImageByIds(DataSource ds, long[] ids, string image)
        {
            try
            {
                if (Db<DistributorProduct>.Query(ds).Update().Set("Image", image).Where(W("Id", ids, DbWhereType.In)).Execute() > 0)
                    return DataStatus.Success;
                else
                    return DataStatus.Failed;
            }
            catch (Exception)
            {
                return DataStatus.Failed;
            }
        }
        public new bool GetSaleArea(DataSource ds, int province, int city, int county)
        {
            return Db<DistributorSalesArea>.Query(ds)
                .Select()
                .Where(W("ProductId", Id) & (W("Province", province) | W("Province", 0)) & (W("City", city) | W("City", 0)) & (W("County", county) | W("County", 0)))
                .First<DistributorSalesArea>() != null;
        }
        public new IList<DistributorSerie> GetSerie(DataSource ds)
        {
            return DistributorSerie.GetAll(ds, Id);
        }
        public IList<DistributorMapping> GetMappingByProduct(DataSource ds)
        {
            return DistributorMapping.GetAllMappingByProductId(ds, Id);
        }
        public new DistributorMapping GetMapping(DataSource ds, long serieId)
        {
            return DistributorMapping.GetBySerie(ds, Id, serieId);
        }
        public new IList<DistributorProduct> GetChildren(DataSource ds)
        {
            return Db<DistributorProduct>.Query(ds)
                .Select()
                .Where(W("ParentId", Id) & W("State", ProductState.Deleted, DbWhereType.NotEqual))
                .ToList<DistributorProduct>();
        }
        public new static DistributorProduct GetBySupplier(DataSource ds, long id, long supplierId)
        {
            return Db<DistributorProduct>.Query(ds)
                .Select()
                .Where(W("SupplierId", supplierId) & W("Id", id))
                .First<DistributorProduct>();
        }

        public static long GetCountByState(DataSource ds, bool sale, long userId)
        {
            if (sale)
            {
                return Db<DistributorProduct>.Query(ds)
                    .Select()
                    .Where((W("State", ProductState.Sale) | W("State", ProductState.BeforeSaved)) & W("SupplierId", userId) & W("ParentId", 0))
                    .Count();
            }
            return Db<DistributorProduct>.Query(ds)
                .Select()
                .Where((W("State", ProductState.Saved) | W("State", ProductState.BeforeSale)) & W("SupplierId", userId) & W("ParentId", 0))
                .Count();
        }
        public static new DataStatus CreateCopy(DataSource ds, long parentId, out long productId)
        {
            ds.Begin();
            try
            {
                DistributorProduct value = GetById(ds, parentId);
                if (value == null)
                    throw new Exception();
                value.ParentId = parentId;
                if (value.Insert(ds) != DataStatus.Success)
                    throw new Exception();
                productId = value.Id;
                IList<DistributorAttributeMapping> maps = DistributorAttributeMapping.GetListByProduct(ds, parentId);
                foreach (DistributorAttributeMapping map in maps)
                {
                    map.ProductId = value.Id;
                    if (map.Insert(ds) != DataStatus.Success)
                        throw new Exception();
                }
                IList<DistributorSalesArea> salesareas = DistributorSalesArea.GetById(ds, parentId);
                foreach (DistributorSalesArea area in salesareas)
                {
                    area.ProductId = value.Id;
                    if (area.Insert(ds) != DataStatus.Success)
                        throw new Exception();
                }

                ds.Commit();
                return DataStatus.Success;
            }
            catch (Exception)
            {
                ds.Rollback();
                productId = 0;
                return DataStatus.Failed;
            }
        }
        public static new DataStatus DeleteChild(DataSource ds, long id)
        {
            return (new DistributorProduct() { Id = id }).Delete(ds);
        }
        public static new DistributorProduct GetById(DataSource ds, long id)
        {
            return Db<DistributorProduct>.Query(ds)
                .Select()
                .Where(W("Id", id))
                .First<DistributorProduct>();
        }
        public static new DistributorProduct GetSaleProduct(DataSource ds, long id)
        {
            return Db<DistributorProduct>.Query(ds)
                .Select()
                .Where(GetStateWhereQueue() & W("Id", id))
                .First<DistributorProduct>();
        }
        /// <summary>
        /// 根据省市区和Id获取销售的产品
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="id"></param>
        /// <param name="province"></param>
        /// <param name="city"></param>
        /// <param name="county"></param>
        /// <returns></returns>
        public static new DistributorProduct GetSaleProduct(DataSource ds, long id, int province, int city, int county)
        {
            return Db<DistributorProduct>.Query(ds)
                .Select(S<DistributorProduct>())
                .InnerJoin(O<DistributorProduct>("Id"), O<DistributorSalesArea>("ProductId"))
                .Where(GetStateWhereQueue<DistributorProduct>()
                & W<DistributorProduct>("Id", id) & (W<DistributorSalesArea>("Province", province) | W<DistributorSalesArea>("Province", 0))
                & (W<DistributorSalesArea>("City", city) | W<DistributorSalesArea>("City", 0))
                & (W<DistributorSalesArea>("County", county) | W<DistributorSalesArea>("County", 0))
                )
                .First<DistributorProduct>();
        }
        public static DistributorProduct GetViewProduct(DataSource ds, long id, long supplierId)
        {
            return Db<DistributorProduct>.Query(ds)
                .Select()
                .Where((W("SupplierId", supplierId) | W("SupplierId").InSelect<Cnaws.Product.Modules.Supplier>(S("UserId")).Where(W("Subjection", supplierId)).Result()) & W("Id", id))
                .First<DistributorProduct>();
        }
        public static new long GetCountByCategoryId(DataSource ds, int categoryId)
        {
            return ExecuteCount<DistributorProduct>(ds, P("CategoryId", categoryId));
        }
        public static new long GetCountByBrandId(DataSource ds, int brandId)
        {
            return ExecuteCount<DistributorProduct>(ds, P("BrandId", brandId));
        }

        /// <summary>
        /// 根据省市县查找加盟商产品列表
        /// </summary>
        /// <param name="pro">省Id</param>
        /// <param name="city">市Id</param>
        /// <param name="county">县Id</param>
        /// <param name="index">当前页</param>
        /// <param name="size">每页条数</param>
        /// <returns></returns>
        public static SplitPageData<DistributorProduct> GetListByLocal(DataSource ds, int pro, int city, int county, long index, int size, int show = 8)
        {
            long count;
            IList<DistributorProduct> list;
            list = Db<DistributorProduct>.Query(ds)
                .Select("Id", "Title", "Image", "CategoryId", "DiscountState", "State", "Recommend", "CategoryBest", "WholesaleCount", "WholesalePrice")
                .Where(W("Province", pro) & W("City", city) & W("County", county) & W("ParentId", 0))
                .OrderBy(D("Id"))
                .ToList<DistributorProduct>(size, index, out count);

            return new SplitPageData<DistributorProduct>(index, size, list, count, show);
        }

        /// <summary>
        /// 根据产品Id获取Mapping列表(包含父产品与子产品)
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public IList<DistributorMapping> GetAllByAllProduct(DataSource ds)
        {
            return DistributorMapping.GetAllByAllProduct(ds, this.Id);
        }

        public DataStatus UpdateInventory(DataSource ds, int value)
        {
            return Update(ds, ColumnMode.Include, Cs(MODC("Inventory", value)), P("State", Cnaws.Product.Modules.ProductState.Sale) & P("Id", Id));
        }
        

        public static SplitPageData<DistributorProduct> GetPage(DataSource ds, int categoryId, long index, int size, int show = 8)
        {
            long count;
            IList<DistributorProduct> list;
            if (categoryId > 0)
                list = Db<DistributorProduct>.Query(ds)
                .Select("Id", "Title", "Image", "CategoryId", "DiscountState", "State", "Recommend", "CategoryBest", "WholesaleCount", "WholesalePrice")
                .Where(W("State", Cnaws.Product.Modules.ProductState.Deleted, DbWhereType.NotEqual) & W("CategoryId", categoryId) & W("ParentId", 0))
                .OrderBy(D("Id"))
                .ToList<DistributorProduct>(size, index, out count);
            else if (categoryId == 0)
                list = Db<DistributorProduct>.Query(ds)
                .Select("Id", "Title", "Image", "CategoryId", "DiscountState", "State", "Recommend", "CategoryBest", "WholesaleCount", "WholesalePrice")
                .Where(W("State", Cnaws.Product.Modules.ProductState.Deleted, DbWhereType.NotEqual) & W("ParentId", 0))
                .OrderBy(D("Id"))
                .ToList<DistributorProduct>(size, index, out count);
            else if (categoryId == -1)
                list = Db<DistributorProduct>.Query(ds)
                .Select("Id", "Title", "Image", "CategoryId", "DiscountState", "State", "Recommend", "CategoryBest", "WholesaleCount", "WholesalePrice")
                .Where(W("State", Cnaws.Product.Modules.ProductState.Saved) & W("ParentId", 0))
                .OrderBy(D("Id"))
                .ToList<DistributorProduct>(size, index, out count);
            else if (categoryId == -2)
                list = Db<DistributorProduct>.Query(ds)
                .Select("Id", "Title", "Image", "CategoryId", "DiscountState", "State", "Recommend", "CategoryBest", "WholesaleCount", "WholesalePrice")
                .Where(W("State", Cnaws.Product.Modules.ProductState.Sale) & W("ParentId", 0))
                .OrderBy(D("Id"))
                .ToList<DistributorProduct>(size, index, out count);
            else
                throw new Exception();
            return new SplitPageData<DistributorProduct>(index, size, list, count, show);
        }

        public new static DataStatus UpdateStoreRecommend(DataSource ds, long id, bool storeRecommend, long userId)
        {
            return Db<DistributorProduct>.Query(ds).Update().Set("StoreRecommend", storeRecommend).Where(W("Id", id) & W("SupplierId", userId)).Execute() > 0 ? DataStatus.Success : DataStatus.Failed;
        }

        public static SplitPageData<DataJoin<DistributorProduct, DistributorCategory>> GetPageByWholesale(DataSource ds, string q, int categoryId, string price, long index, int size, int show = 8)
        {
            long count;
            DbWhereQueue where = GetStateWhereQueue<DistributorProduct>() & W<DistributorProduct>("ParentId", 0) & W<DistributorProduct>("Wholesale", true) & W<DistributorProduct>("ParentId", 0);
            if (!string.IsNullOrEmpty(price))
            {
                string[] arr = price.Split('-');
                try
                {
                    if (!string.IsNullOrEmpty(arr[0]) && !string.IsNullOrEmpty(arr[1]))
                    {
                        where = W<DistributorProduct>("WholesalePrice", Money.Parse(arr[0]), DbWhereType.GreaterThanOrEqual) & W<DistributorProduct>("WholesalePrice", int.Parse(arr[1]), DbWhereType.LessThanOrEqual) & where;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(arr[0]))
                            where = W<DistributorProduct>("WholesalePrice", Money.Parse(arr[0]), DbWhereType.GreaterThanOrEqual) & where;
                        else
                            where = W<DistributorProduct>("WholesalePrice", Money.Parse(arr[1]), DbWhereType.LessThanOrEqual) & where;
                    }
                }
                catch (Exception) { }
            }
            if (!string.IsNullOrEmpty(q))
                where = W<DistributorProduct>("Title", q, DbWhereType.Like) & where;
            if (categoryId > 0)
                where &= W<DistributorProduct>("CategoryId", categoryId);
            IList<DataJoin<DistributorProduct, DistributorCategory>> list = Db<DistributorProduct>.Query(ds)
                .Select(S<DistributorProduct>(), S<DistributorCategory>())
                .InnerJoin(O<DistributorProduct>("CategoryId"), O<DistributorCategory>("Id"))
                .Where(where)
                .OrderBy(D<DistributorProduct>("SaleTime"), D<DistributorProduct>("Id"))
                .ToList<DataJoin<DistributorProduct, DistributorCategory>>(size, index, out count);
            return new SplitPageData<DataJoin<DistributorProduct, DistributorCategory>>(index, size, list, count, show);
        }
        public static IList<dynamic> GetPageByParentId2NoState(DataSource ds, long parentid, int province, int city, int county)
        {
            DbWhereQueue where = (W<DistributorProduct>("ParentId", parentid) | W<DistributorProduct>("Id", parentid));
            DbWhereQueue areawhere = new DbWhereQueue();
            areawhere &= W<DistributorAreaMapping>("Province", province);
            areawhere &= W<DistributorAreaMapping>("City", city);
            areawhere &= W<DistributorAreaMapping>("County", county);

            IList<dynamic> list = Db<DistributorProduct>.Query(ds)
                .Select(S<DistributorProduct>(), S<DistributorAreaMapping>("CostPrice"), S<DistributorAreaMapping>("CountyPrice"), S<DistributorAreaMapping>("DotPrice"), S<DistributorAreaMapping>("Price"), S<DistributorAreaMapping>("Saled"))
                .LeftJoin(O<DistributorProduct>("Id"), O<DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                .Where(where)
                .OrderBy(D<DistributorProduct>("SaleTime"), D<DistributorProduct>("Id"))
                .ToList();
            return list;
        }
        public static SplitPageData<DataJoin<DistributorProduct, DistributorCategory>> ApiGetPageByWholesale(DataSource ds, string q, int categoryId, string price, int provinceid, int cityid, int countyid, long index, int size, int show = 8)
        {
            long count;
            DbWhereQueue where = GetStateWhereQueue<DistributorProduct>() & W<DistributorProduct>("Wholesale", true) & W<DistributorProduct>("ParentId", 0);
            if (!string.IsNullOrEmpty(price))
            {
                string[] arr = price.Split('-');
                try
                {
                    if (!string.IsNullOrEmpty(arr[0]) && !string.IsNullOrEmpty(arr[1]))
                    {
                        where = W<DistributorProduct>("WholesalePrice", Money.Parse(arr[0]), DbWhereType.GreaterThanOrEqual) & W<DistributorProduct>("WholesalePrice", int.Parse(arr[1]), DbWhereType.LessThanOrEqual) & where;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(arr[0]))
                            where = W<DistributorProduct>("WholesalePrice", Money.Parse(arr[0]), DbWhereType.GreaterThanOrEqual) & where;
                        else
                            where = W<DistributorProduct>("WholesalePrice", Money.Parse(arr[1]), DbWhereType.LessThanOrEqual) & where;
                    }
                }
                catch (Exception) { }
            }

            where &= (W<DistributorAreaMapping>("Province", provinceid) | W<DistributorAreaMapping>("Province", 0));
            where &= (W<DistributorAreaMapping>("City", cityid) | W<DistributorAreaMapping>("City", 0));
            where &= (W<DistributorAreaMapping>("County", countyid) | W<DistributorAreaMapping>("County", 0));

            if (!string.IsNullOrEmpty(q))
                where = W<DistributorProduct>("Title", q, DbWhereType.Like) & where;
            if (categoryId > 0)
                where &= W<DistributorProduct>("CategoryId", categoryId);
            IList<DataJoin<DistributorProduct, DistributorCategory>> list = Db<DistributorProduct>.Query(ds)
                .Select(S<DistributorProduct>(), S<DistributorCategory>())
                .InnerJoin(O<DistributorProduct>("CategoryId"), O<DistributorCategory>("Id"))
                .InnerJoin(O<DistributorProduct>("Id"), O<DistributorAreaMapping>("ProductId"))
                .Where(where)
                .OrderBy(D<DistributorProduct>("SaleTime"), D<DistributorProduct>("Id"))
                .ToList<DataJoin<DistributorProduct, DistributorCategory>>(size, index, out count);
            return new SplitPageData<DataJoin<DistributorProduct, DistributorCategory>>(index, size, list, count, show);
        }
        [Obsolete]
        public new static IList<dynamic> GetPageByParentId(DataSource ds, long parentid)
        {
            IList<dynamic> list = Db<DistributorProduct>.Query(ds)
                .Select(S("*"))
                .Where(GetStateWhereQueue() & (W("ParentId", parentid) | W("Id", parentid)))
                .OrderBy(D("SaleTime"), D("Id"))
                .ToList();
            return list;
        }
        public static IList<dynamic> GetPageByParentId2(DataSource ds, long parentid, int province, int city, int county)
        {
            DbWhereQueue where = GetStateWhereQueue<DistributorProduct>() & (W<DistributorProduct>("ParentId", parentid) | W<DistributorProduct>("Id", parentid));
            DbWhereQueue areawhere = new DbWhereQueue();
            areawhere &= W<DistributorAreaMapping>("Province", province);
            areawhere &= W<DistributorAreaMapping>("City", city);
            areawhere &= W<DistributorAreaMapping>("County", county);

            IList<dynamic> list = Db<DistributorProduct>.Query(ds)
                .Select(S<DistributorProduct>("*"), S<DistributorAreaMapping>("CostPrice"), S<DistributorAreaMapping>("CountyPrice"), S<DistributorAreaMapping>("DotPrice"), S<DistributorAreaMapping>("Price"))
                .LeftJoin(O<DistributorProduct>("Id"), O<DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                .Where(where)
                .OrderBy(D<DistributorProduct>("SaleTime"), D<DistributorProduct>("Id"))
                .ToList();
            return list;
        }
        public new static DataStatus UpdateInventoryById(DataSource ds, long id, int inventory)
        {
            return (new DistributorProduct()).Update(ds, ColumnMode.Include, Cs(MODC("Inventory", -inventory)), WN("Inventory", inventory, "OldInventory", ">=") & (WN("State", ProductState.Sale, "State1") | WN("State", ProductState.BeforeSaved, "State2")) & P("Id", id));
        }

        public static IList<DistributorProduct> GetByParentId(DataSource ds, long parentId)
        {
            return Db<DistributorProduct>.Query(ds).Select().Where((W("Id", parentId) | W("ParentId", parentId)) & GetStateWhereQueue()).ToList<DistributorProduct>();
        }
        public new static long GetProductCountBySupplierId(DataSource ds, long supplierId)
        {
            return Db<DistributorProduct>.Query(ds).Select().Where(GetParentWhereQueue() & GetStateWhereQueue() & W("SupplierId", supplierId)).Count();
        }
        public new static DataStatus RemoveToRecycleBin(DataSource ds, long id)
        {
            ds.Begin();
            try
            {
                if (Db<DistributorProduct>.Query(ds).Update().Set("State", ProductState.Deleted).Where(W("Id", id)).Execute() == 1)
                {
                    Db<DistributorMapping>.Query(ds).Delete().Where(W("ProductId", id)).Execute();
                    ds.Commit();
                    return DataStatus.Success;
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Exist;
            }
        }

        /// <summary>
        /// 商城推荐
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="count"></param>
        /// <param name="province"></param>
        /// <param name="city"></param>
        /// <param name="county"></param>
        /// <param name="productType"></param>
        /// <returns></returns>
        public static new IList<DataJoin<DistributorProduct, DistributorAreaMapping>> GetTopRecommendByArea(DataSource ds, int count, int province, int city, int county, int productType = 1)
        {
            DbWhereQueue where = new DbWhereQueue();
            where = GetStateWhereQueue<DistributorProduct>() & W<DistributorProduct>("Recommend", true) & W<DistributorProduct>("ParentId", 0) & W<DistributorProduct>("ProductType", productType);
            where &= (W<DistributorSalesArea>("Province", province) | W<DistributorSalesArea>("Province", 0));
            where &= (W<DistributorSalesArea>("City", city) | W<DistributorSalesArea>("City", 0));
            where &= (W<DistributorSalesArea>("County", county) | W<DistributorSalesArea>("County", 0));
            DbWhereQueue areawhere = new DbWhereQueue();
            areawhere = GetAreaMappingWhereQueue(province, city, county);
            IList<DataJoin<DistributorProduct, DistributorAreaMapping>> items = Db<DistributorProduct>.Query(ds)
                .Select(S<DistributorProduct>("Id"), S<DistributorProduct>("SortNum"), S<DistributorProduct>("SaleTime"), S<DistributorProduct>("Title"), S<DistributorProduct>("Image"), S<DistributorProduct>("DiscountState"), S<DistributorProduct>("DiscountBeginTime"), S<DistributorProduct>("DiscountEndTime"), S<DistributorProduct>("Wholesale"), S<DistributorProduct>("WholesalePrice"), S<DistributorProduct>("WholesaleCount"), S<DistributorProduct>("WholesaleDiscount"), S<DistributorProduct>("CountyPrice"), S<DistributorProduct>("DotPrice"), S<DistributorProduct>("DiscountPrice"), S<DistributorProduct>("Price"), S<DistributorProduct>("MarketPrice"), S<DistributorProduct>("ProductType"), S<DistributorProduct>("SupplierType"),
                 S<DistributorProduct>("Unit"), S<DistributorProduct>("RetailUnit"),
                S<DistributorAreaMapping>("CostPrice"), S<DistributorAreaMapping>("CountyPrice"), S<DistributorAreaMapping>("DotPrice"), S<DistributorAreaMapping>("Price"))
                .LeftJoin(O<DistributorProduct>("Id"), O<DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                .InnerJoin(O<DistributorProduct>("Id"), O<DistributorSalesArea>("ProductId"))
                .Where(where)
                .OrderBy(D<DistributorProduct>("SortNum"), D<DistributorProduct>("SaleTime"), D<DistributorProduct>("Id"))
                .ToList<DataJoin<DistributorProduct, DistributorAreaMapping>>(count);

            return items;
        }
        /// <summary>
        /// 分类热销商品
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="count"></param>
        /// <param name="categoryId"></param>
        /// <param name="province"></param>
        /// <param name="city"></param>
        /// <param name="county"></param>
        /// <param name="productType"></param>
        /// <returns></returns>
        public static new IList<DataJoin<DistributorProduct, DistributorAreaMapping>> GetTopBestByCategoryByArea(DataSource ds, int count, int categoryId, int province, int city, int county, int productType = 1)
        {
            DbWhereQueue where = new DbWhereQueue();
            where = GetStateWhereQueue<DistributorProduct>() & W<DistributorProduct>("CategoryBest", categoryId) & W<DistributorProduct>("ParentId", 0) & W<DistributorProduct>("ProductType", productType);
            where &= (W<DistributorSalesArea>("Province", province) | W<DistributorSalesArea>("Province", 0));
            where &= (W<DistributorSalesArea>("City", city) | W<DistributorSalesArea>("City", 0));
            where &= (W<DistributorSalesArea>("County", county) | W<DistributorSalesArea>("County", 0));
            DbWhereQueue areawhere = new DbWhereQueue();
            areawhere = GetAreaMappingWhereQueue(province, city, county);
            IList<DataJoin<DistributorProduct, DistributorAreaMapping>> items = Db<DistributorProduct>.Query(ds)
                 .Select(S<DistributorProduct>("Id"), S<DistributorProduct>("SortNum"), S<DistributorProduct>("SaleTime"), S<DistributorProduct>("Title"), S<DistributorProduct>("Image"), S<DistributorProduct>("DiscountState"), S<DistributorProduct>("DiscountBeginTime"), S<DistributorProduct>("DiscountEndTime"), S<DistributorProduct>("Wholesale"), S<DistributorProduct>("WholesalePrice"), S<DistributorProduct>("WholesaleCount"), S<DistributorProduct>("WholesaleDiscount"), S<DistributorProduct>("CountyPrice"), S<DistributorProduct>("DotPrice"), S<DistributorProduct>("DiscountPrice"), S<DistributorProduct>("Price"), S<DistributorProduct>("MarketPrice"), S<DistributorProduct>("ProductType"), S<DistributorProduct>("SupplierType"), S<DistributorProduct>("Unit"), S<DistributorProduct>("RetailUnit"), S<DistributorAreaMapping>("CostPrice"), S<DistributorAreaMapping>("CountyPrice"), S<DistributorAreaMapping>("DotPrice"), S<DistributorAreaMapping>("Price"))
                 .LeftJoin(O<DistributorProduct>("Id"), O<DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                 .InnerJoin(O<DistributorProduct>("Id"), O<DistributorSalesArea>("ProductId"))
                 .Where(where)
                 .OrderBy(D<DistributorProduct>("SortNum"), D<DistributorProduct>("SaleTime"), D<DistributorProduct>("Id"))
                 .ToList<DataJoin<DistributorProduct, DistributorAreaMapping>>(count);

            return items;
        }
        /// <summary>
        /// 返回产品区域查询条件
        /// </summary>
        /// <param name="province"></param>
        /// <param name="city"></param>
        /// <param name="county"></param>
        /// <returns></returns>
        protected static new DbWhereQueue GetAreaMappingWhereQueue(int province, int city, int county)
        {
            DbWhereQueue where = W<DistributorAreaMapping>("Province", province);
            where &= W<DistributorAreaMapping>("City", city);
            where &= W<DistributorAreaMapping>("County", county);
            where &= W<DistributorAreaMapping>("Saled", true);
            return where;
        }
        #region Api专用
        public new static SplitPageData<dynamic> ApiGetPageByCategory(DataSource ds, int categoryId, int categorylevel, FilterParameters2 parameters, int show = 8, int productType = 1)
        {
            long count;
            DbWhereQueue where = null;

            //关键词
            string[] qs;
            string q = parameters.KeyWord;

            if (string.IsNullOrEmpty(q))
            {
                qs = new string[] { string.Empty };
                where = W<DistributorProduct>("Title", "", DbWhereType.Like);
            }
            else
            {
                qs = q.Split(' ');
                foreach (string s in qs)
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        if (where == null)
                            where = W<DistributorProduct>("Title", s, DbWhereType.Like);
                        else
                            where |= W<DistributorProduct>("Title", s, DbWhereType.Like);
                    }
                }
            }
            where &= GetParentWhereQueue<DistributorProduct>() & GetStateWhereQueue<DistributorProduct>();
            //分类
            if (categoryId > 0)
            {
                if (categorylevel == 3)
                    where &= W<DistributorProduct>("CategoryId", categoryId);
                else if (categorylevel == 2)
                    where &= (W<DistributorProduct>("CategoryId", categoryId) | W<DistributorProduct>("CategoryId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId", categoryId)).Result());
                else if (categorylevel == 1)
                    where &= (W<DistributorProduct>("CategoryId", categoryId) | W<DistributorProduct>("CategoryId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId", categoryId)).Result() | W<DistributorProduct>("CategoryId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId", categoryId)).Result()).Result());
            }
            //供应类型
            if (parameters.SupplierType != -1)
            {
                where &= W<DistributorProduct>("SupplierType", parameters.SupplierType);
            }
            //品牌
            if (parameters.Brand > 0)
                    where &= W<DistributorProduct>("BrandId", parameters.Brand);

            //}
            //产品属性
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
                                    where &= (W<DistributorProduct>("Id").InSelect<DistributorAttributeMapping>(S("ProductId")).Where(W("AttributeId", long.Parse(Attr_Value[0].ToString())) & W("Value", Attr_Value[1].ToString())).Result());
                                }
                            }
                        }
                    }
                }
            }
            //店铺
            if (parameters.StoreId > 0)
            {
                where &= W<DistributorProduct>("SupplierId", parameters.StoreId);
                //店铺分类
                if (parameters.StoreCategoryId > 0)
                {
                    if (Db<StoreCategory>.Query(ds).Select(S("ParentId")).Where(W("Id", parameters.StoreCategoryId)).First<StoreCategory>().ParentId > 0)
                    {
                        where &= W<DistributorProduct>("StoreCategoryId", parameters.StoreCategoryId);
                    }
                    else
                    {
                        where &= (W<DistributorProduct>("StoreCategoryId", parameters.StoreCategoryId) | W<DistributorProduct>("StoreCategoryId").InSelect<StoreCategory>(S("Id")).Where(W("ParentId", parameters.StoreCategoryId)).Result());
                    }

                }
            }
            //价格
            if (!string.IsNullOrEmpty(parameters.Price))
            {
                if (parameters.Price.IndexOf('-') != -1)
                {
                    Money left = 0, right = 0;
                    string[] price = parameters.Price.Split('-');
                    if (price.Length > 1)
                    {
                        Money.TryParse(price[0], out left);
                        Money.TryParse(price[1], out right);
                        if (left != 0 && right != 0)
                        {
                            where &= (W<DistributorProduct>("Price", left, DbWhereType.GreaterThanOrEqual) & W<DistributorProduct>("Price", right, DbWhereType.LessThanOrEqual));
                        }
                        else
                        {
                            if (left != 0)
                                where &= W<DistributorProduct>("Price", left, DbWhereType.GreaterThanOrEqual);
                            else
                                where &= W<DistributorProduct>("Price", right, DbWhereType.LessThanOrEqual);
                        }
                    }
                }
            }
            //类型
            where &= W<DistributorProduct>("ProductType", productType);

            //区域
            where &= (W<DistributorSalesArea>("Province", parameters.Province) | W<DistributorSalesArea>("Province", 0));
            where &= (W<DistributorSalesArea>("City", parameters.City) | W<DistributorSalesArea>("City", 0));
            where &= (W<DistributorSalesArea>("County", parameters.County) | W<DistributorSalesArea>("County", 0));
            DbWhereQueue areawhere = new DbWhereQueue();
            areawhere &= W<DistributorAreaMapping>("Province", parameters.Province);
            areawhere &= W<DistributorAreaMapping>("City", parameters.City);
            areawhere &= W<DistributorAreaMapping>("County", parameters.County);

            DbWhereQueue subwhere = new DbWhereQueue();
            List<DbOrderBy> orders = new List<DbOrderBy>(2);
            orders.Add(D<DistributorProduct>("SortNum"));
            orders.Add(D<DistributorProduct>("SaleTime"));
            orders.Add(D<DistributorProduct>("Id"));
            ProductOrderBy ob = (ProductOrderBy)parameters.OrderBy;
            switch (ob)
            {
                case ProductOrderBy.SaleDesc:
                    subwhere &= W<StatisticData>("Type", SaleType);
                    orders.Insert(0, D<StatisticData>("Count"));
                    break;
                case ProductOrderBy.SaleAsc:
                    subwhere &= W<StatisticData>("Type", SaleType);
                    orders.Insert(0, A<StatisticData>("Count"));
                    break;
                case ProductOrderBy.ViewDesc:
                    subwhere &= W<StatisticData>("Type", ViewType);
                    orders.Insert(0, D<StatisticData>("Count"));
                    break;
                case ProductOrderBy.ViewAsc:
                    subwhere &= W<StatisticData>("Type", ViewType);
                    orders.Insert(0, A<StatisticData>("Count"));
                    break;
                case ProductOrderBy.PriceAsc:
                    orders.Insert(0, A<DistributorProduct>("Price"));
                    break;
                case ProductOrderBy.PriceDesc:
                    orders.Insert(0, D<DistributorProduct>("Price"));
                    break;
            }

            IList<dynamic> list = Db<DistributorProduct>.Query(ds)
                .Select(S<DistributorProduct>("Id"), S<DistributorProduct>("SortNum"), S<DistributorProduct>("SaleTime"), S<DistributorProduct>("Title"), S<DistributorProduct>("Image"), S<DistributorProduct>("DiscountState"), S<DistributorProduct>("DiscountBeginTime"), S<DistributorProduct>("DiscountEndTime"), S<DistributorProduct>("Wholesale"), S<DistributorProduct>("WholesalePrice"), S<DistributorProduct>("WholesaleCount"), S<DistributorProduct>("WholesaleDiscount"), S<DistributorProduct>("DiscountPrice"), S<DistributorProduct>("Price"), S<DistributorProduct>("CountyPrice"), S<DistributorProduct>("DotPrice"), S<DistributorProduct>("RetailUnit"), S<DistributorProduct>("Unit"), S<DistributorProduct>("Norms"), S<DistributorProduct>("ProductType"), S<DistributorProduct>("SupplierType"), S<StatisticData>("Count"), S<StatisticData>("Type"), S<DistributorAreaMapping>("CostPrice"), S<DistributorAreaMapping>("CountyPrice"), S<DistributorAreaMapping>("DotPrice"), S<DistributorAreaMapping>("Price"))
                .LeftJoin(O<DistributorProduct>("Id"), O<StatisticData>("TargetId")).Select().Where(subwhere).Result()
                 .LeftJoin(O<DistributorProduct>("Id"), O<DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                .InnerJoin(O<DistributorProduct>("Id"), O<DistributorSalesArea>("ProductId"))
                .Where(where)
                .OrderBy(orders.ToArray())
                .ToList(parameters.Size, parameters.Page, out count);
            if (list != null && list.Count > 0)
                StatisticTag.Add(ds, qs);
            return new SplitPageData<dynamic>(parameters.Page, parameters.Size, list, count, show);
        }
        public new static SplitPageData<DistributorProduct> GetBySupplierStateOrState(DataSource ds, long supplierId, string keyword, ProductState state1, ProductState state2, long index, int size, int show = 8, int productType = 1)
        {
            long count;
            DbWhereQueue where = null;

            if (!string.IsNullOrEmpty(keyword))
            {
                foreach (string s in keyword.Split(' '))
                {
                    if (where == null)
                        where = W("Title", s, DbWhereType.Like);
                    else
                        where |= W("Title", s, DbWhereType.Like);
                }
            }
            else
            {
                where = W("Title", keyword, DbWhereType.Like);
            }
            where = (where) & (W("State", state1) | W("State", state2)) & W("SupplierId", supplierId) & W("ParentId", 0) & W("ProductType", productType);

            IList<DistributorProduct> list = Db<DistributorProduct>.Query(ds)
                .Select()
                .Where(where)
                .OrderBy(D("CreationDate"))
                .ToList<DistributorProduct>(size, index, out count);
            return new SplitPageData<DistributorProduct>(index, size, list, count, show);
        }
        public new static IList<dynamic> ApiGetPageByRecommend(DataSource ds, int show, int province, int city, int county, int suppliertype, int producttype = 1)
        {
            DbWhereQueue where = null;

            where = GetParentWhereQueue<DistributorProduct>() & GetStateWhereQueue<DistributorProduct>();
            //分类            
            where &= W<DistributorProduct>("ProductType", producttype);
            ///供应类型
            if (suppliertype != -1)
                where &= W<DistributorProduct>("SupplierType", suppliertype);
            //推荐
            where &= W<DistributorProduct>("Recommend", true);
            //区域
            where &= (W<DistributorSalesArea>("Province", province) | W<DistributorSalesArea>("Province", 0));
            where &= (W<DistributorSalesArea>("City", city) | W<DistributorSalesArea>("City", 0));
            where &= (W<DistributorSalesArea>("County", county) | W<DistributorSalesArea>("County", 0));
            DbWhereQueue areawhere = new DbWhereQueue();
            areawhere &= W<DistributorAreaMapping>("Province", province);
            areawhere &= W<DistributorAreaMapping>("City", city);
            areawhere &= W<DistributorAreaMapping>("County", county);

            DbWhereQueue subwhere = new DbWhereQueue();
            List<DbOrderBy> orders = new List<DbOrderBy>(2);
            orders.Add(D<DistributorProduct>("SortNum"));
            orders.Add(D<DistributorProduct>("SaleTime"));
            orders.Add(D<DistributorProduct>("Id"));

            IList<dynamic> list = Db<DistributorProduct>.Query(ds)
                .Select(S<DistributorProduct>("Id"), S<DistributorProduct>("SortNum"), S<DistributorProduct>("SaleTime"), S<DistributorProduct>("Title"), S<DistributorProduct>("Image"), S<DistributorProduct>("DiscountState"), S<DistributorProduct>("DiscountBeginTime"), S<DistributorProduct>("DiscountEndTime"), S<DistributorProduct>("Wholesale"), S<DistributorProduct>("WholesalePrice"), S<DistributorProduct>("CountyPrice"), S<DistributorProduct>("DotPrice"), S<DistributorProduct>("WholesaleCount"), S<DistributorProduct>("WholesaleDiscount"), S<DistributorProduct>("DiscountPrice"), S<DistributorProduct>("Price"), S<DistributorProduct>("RetailUnit"), S<DistributorProduct>("Unit"), S<DistributorProduct>("Norms"), S<DistributorProduct>("ProductType"), S<DistributorProduct>("SupplierType"), S<DistributorAreaMapping>("CostPrice"), S<DistributorAreaMapping>("CountyPrice"), S<DistributorAreaMapping>("DotPrice"), S<DistributorAreaMapping>("Price"))
                 .LeftJoin(O<DistributorProduct>("Id"), O<DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                .InnerJoin(O<DistributorProduct>("Id"), O<DistributorSalesArea>("ProductId"))
                .Where(where)
                .OrderBy(orders.ToArray())
                .ToList(show);
            return list;
        }

        public new static DataStatus UpdateStoreSortNum(DataSource ds, long id, int storeSortNum, long userId)
        {
            return Db<DistributorProduct>.Query(ds).Update().Set("StoreSortNum", storeSortNum).Where(W("Id", id) & W("SupplierId", userId)).Execute() > 0 ? DataStatus.Success : DataStatus.Failed;
        }
        public new static IList<dynamic> ApiGetRecommendByCategory(DataSource ds, int show, int category, int province, int city, int county,int suppliertype, int producttype = 1)
        {
            DbWhereQueue where = null;

            where = GetParentWhereQueue<DistributorProduct>() & GetStateWhereQueue<DistributorProduct>();
            //分类            
            where &= W<DistributorProduct>("ProductType", producttype);
            ///供应类型
            if (suppliertype != -1)
                where &= W<DistributorProduct>("SupplierType", suppliertype);
            //推荐
            where &= W<DistributorProduct>("CategoryBest", category);
            //区域
           
            where &= (W<DistributorSalesArea>("Province", province) | W<DistributorSalesArea>("Province", 0));
            where &= (W<DistributorSalesArea>("City", city) | W<DistributorSalesArea>("City", 0));
            where &= (W<DistributorSalesArea>("County", county) | W<DistributorSalesArea>("County", 0));
            DbWhereQueue areawhere = new DbWhereQueue();
            areawhere &= W<DistributorAreaMapping>("Province", province);
            areawhere &= W<DistributorAreaMapping>("City", city);
            areawhere &= W<DistributorAreaMapping>("County", county);

            DbWhereQueue subwhere = new DbWhereQueue();
            List<DbOrderBy> orders = new List<DbOrderBy>(2);
            orders.Add(D<DistributorProduct>("SortNum"));
            orders.Add(D<DistributorProduct>("SaleTime"));
            orders.Add(D<DistributorProduct>("Id"));

            IList<dynamic> list = Db<DistributorProduct>.Query(ds)
                .Select(S<DistributorProduct>("Id"), S<DistributorProduct>("SortNum"), S<DistributorProduct>("SaleTime"), S<DistributorProduct>("Title"), S<DistributorProduct>("Image"), S<DistributorProduct>("DiscountState"), S<DistributorProduct>("Norms"), S<DistributorProduct>("DiscountBeginTime"), S<DistributorProduct>("DiscountEndTime"), S<DistributorProduct>("Wholesale"), S<DistributorProduct>("WholesalePrice"), S<DistributorProduct>("WholesaleCount"), S<DistributorProduct>("WholesaleDiscount"), S<DistributorProduct>("DiscountPrice"), S<DistributorProduct>("Price"), S<DistributorProduct>("CountyPrice"), S<DistributorProduct>("DotPrice"), S<DistributorProduct>("RetailUnit"), S<DistributorProduct>("Unit"), S<DistributorProduct>("ProductType"), S<DistributorProduct>("SupplierType"), S<DistributorAreaMapping>("CostPrice"), S<DistributorAreaMapping>("CountyPrice"), S<DistributorAreaMapping>("DotPrice"), S<DistributorAreaMapping>("Price"))
                 .LeftJoin(O<DistributorProduct>("Id"), O<DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                .InnerJoin(O<DistributorProduct>("Id"), O<DistributorSalesArea>("ProductId"))
                .Where(where)
                .OrderBy(orders.ToArray())
                .ToList(show);
            return list;
        }

        #endregion Api专用
    }
}
