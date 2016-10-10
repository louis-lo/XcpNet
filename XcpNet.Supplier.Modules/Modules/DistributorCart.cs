using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class DistributorCart : Cnaws.Product.Modules.ProductCart
    {
        public DistributorCart()
            : base()
        {
        }
        public DistributorCart(DataSource ds, long userId, DistributorProduct value, int count)
            : base(ds, userId, value, count)
        {
        }
        public DistributorCart(DataSource ds, long userId, DistributorProduct value, int count,int province,int city,int county)
        {
            UserId = userId;
            ProductId = value.Id;
            Title = value.Title;
            try { Image = value.GetImages()[0]; }
            catch (Exception) { }
            Attributes = value.GetAttributes(ds);
            Price = value.GetPrice(ds, province, city, county);
            SalePrice = value.GetSalePrice(ds,province,city,county);
            Count = count;
            SupplierId = value.SupplierId;
            CreationDate = DateTime.Now;
        }

        private void Load(DataSource ds, DistributorProduct p)
        {
            Title = p.Title;
            try { Image = p.GetImages()[0]; }
            catch (Exception) { }
            Attributes = p.GetAttributes(ds);
            Price = p.WholesalePrice;
            SupplierId = p.SupplierId;
            SalePrice = p.Price;
        }

        private static dynamic LoadDynamic(DataSource ds, dynamic p)
        {
            p.DistributorCart_Title = p.DistributorProduct_Title;
            try { p.DistributorCart_Image = p.DistributorProduct_Image.Split('|')[0]; }
            catch (Exception) { }
            p.DistributorCart_Attributes = DistributorMapping.GetAttributes(ds, p.DistributorProduct_Id);

            if (!(p.DistributorAreaMapping_Price is DBNull) && p.DistributorAreaMapping_Price > 0)
                p.DistributorCart_Price = p.DistributorAreaMapping_Price;
            else
                p.DistributorCart_Price = p.DistributorProduct_Price;
            DateTime now = DateTime.Now;
            if (p.DistributorProduct_DiscountState == (int)Cnaws.Product.Modules.DiscountState.Activated && (now >= p.DistributorProduct_DiscountBeginTime && now < p.DistributorProduct_DiscountEndTime))
            {
                p.DistributorCart_SalePrice = p.DistributorProduct_DiscountPrice;
            }
            else if (p.DistributorProduct_Wholesale)
                p.DistributorCart_SalePrice = p.DistributorProduct_WholesalePrice;
            else if (!(p.DistributorAreaMapping_Price is DBNull) && p.DistributorAreaMapping_Price > 0)
                p.DistributorCart_SalePrice = p.DistributorAreaMapping_Price;
            else
                p.DistributorCart_SalePrice = p.DistributorProduct_Price;
            return p;
        }

        public override DataStatus Add(DataSource ds)
        {
            if (ExecuteCount<DistributorCart>(ds, P("ProductId", ProductId) & P("UserId", UserId)) > 0)
                return DataStatus.ExistOther;
            return Insert(ds);
        }

        public static new DataStatus Remove(DataSource ds, long productId, long userId)
        {
            return (new DistributorCart() { ProductId = productId, UserId = userId }).Delete(ds, "ProductId", "UserId");
        }

        /// <summary>
        /// 根据多个主键批量删除
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="Ids"></param>
        /// <returns></returns>
        public static DataStatus Remove(DataSource ds, string[] Ids)
        {
            DataStatus statu = DataStatus.Failed;
            for (int i = 0; i < Ids.Length; ++i)
            {
                statu = new DistributorCart() { Id = long.Parse(Ids[i]) }.Delete(ds, "Id");
                if (statu != DataStatus.Success)
                    break;
            }
            return statu;
        }

        public static new long GetCountByUser(DataSource ds, long userId)
        {
            IList<DataJoin<DistributorCart, DistributorProduct>> list = ExecuteReader<DistributorCart, DistributorProduct>(ds, Cs(C<DistributorCart>("Id"), C<DistributorProduct>("Id"), C<DistributorProduct>("State")), Os(Od<DistributorCart>("CreationDate")), "ProductId", "Id", DataJoinType.Inner, P<DistributorCart>("UserId", userId));
            long Total = 0;
            foreach (DataJoin<DistributorCart, DistributorProduct> item in list)
            {
                if (item.B.IsPublish())
                    Total++;
            }
            return Total;
        }
        public static DataStatus RemoveCart(DataSource ds, long id, long userid)
        {
            return (new DistributorCart() { Id = id, UserId = userid }).Remove(ds);
        }

        public static new IList<DataJoin<DistributorCart, DistributorProduct>> GetPageByUser(DataSource ds, long userId)
        {
            IList<DataJoin<DistributorCart, DistributorProduct>> list = ExecuteReader<DistributorCart, DistributorProduct>(ds, Cs(C<DistributorCart>("*"), C<DistributorProduct>("*")), Os(Od<DistributorCart>("CreationDate")), "ProductId", "Id", DataJoinType.Inner, P<DistributorCart>("UserId", userId));
            foreach (DataJoin<DistributorCart, DistributorProduct> item in list)
            {
                if (item.B.IsPublish())
                    item.A.Load(ds, item.B);
            }
            return list;
        }

        public static new IList<DataJoin<DistributorCart, DistributorProduct>> GetBySupplierId(DataSource ds, long userId,long supplierId)
        {
            IList<DataJoin<DistributorCart, DistributorProduct>> list = ExecuteReader<DistributorCart, DistributorProduct>(ds, Cs(C<DistributorCart>("*"), C<DistributorProduct>("*")), Os(Od<DistributorCart>("CreationDate")), "ProductId", "Id", DataJoinType.Inner, P<DistributorCart>("UserId", userId)& P<DistributorCart>("SupplierId", supplierId));
            foreach (DataJoin<DistributorCart, DistributorProduct> item in list)
            {
                if (item.B.IsPublish())
                    item.A.Load(ds, item.B);
            }
            return list;
        }

        public static new long[] GetBySupplierId(DataSource ds, long userId)
        {
            return Db<DistributorCart>.Query(ds).Select(S("SupplierId")).Where(W("UserId", userId)).GroupBy(G("SupplierId")).ToArray<long>();
        }


        /// <summary>
        /// 根据用户以及省市区获取购物车列表
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userId"></param>
        /// <param name="province"></param>
        /// <param name="city"></param>
        /// <param name="county"></param>
        /// <returns></returns>
        public static IList<dynamic> GetPageByUser(DataSource ds, long userId, int province, int city, int county)
        {
            IList<dynamic> list;
            DbWhereQueue where = W<DistributorCart>("UserId", userId) & (W<DistributorProduct>("State", Cnaws.Product.Modules.ProductState.Sale) | W<DistributorProduct>("State", Cnaws.Product.Modules.ProductState.BeforeSaved));
            where &= (W<DistributorSalesArea>("Province", province) | W<DistributorSalesArea>("Province", 0));
            where &= (W<DistributorSalesArea>("City", city) | W<DistributorSalesArea>("City", 0));
            where &= (W<DistributorSalesArea>("County", county) | W<DistributorSalesArea>("County", 0));
            DbWhereQueue areawhere = new DbWhereQueue();
            areawhere &= W<DistributorAreaMapping>("Province", province);
            areawhere &= W<DistributorAreaMapping>("City", city);
            areawhere &= W<DistributorAreaMapping>("County", county);
            list = Db<DistributorCart>.Query(ds).Select(S<DistributorCart>(), S<DistributorProduct>("Id"), S<DistributorProduct>("Inventory"), S<DistributorProduct>("SortNum"), S<DistributorProduct>("SaleTime"), S<DistributorProduct>("Title"), S<DistributorProduct>("Image"), S<DistributorProduct>("DiscountState"), S<DistributorProduct>("DiscountBeginTime"), S<DistributorProduct>("DiscountEndTime"), S<DistributorProduct>("DiscountPrice"), S<DistributorProduct>("Price"), S<DistributorProduct>("Wholesale"), S<DistributorProduct>("WholesalePrice"), S<DistributorProduct>("WholesaleCount"), S<DistributorProduct>("WholesaleDiscount"), S<DistributorProduct>("ProductType"), S<DistributorProduct>("State"), S<DistributorAreaMapping>())
                .InnerJoin(O<DistributorCart>("ProductId"), O<DistributorProduct>("Id"))
                 .LeftJoin(O<DistributorCart>("ProductId"), O<DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                 .InnerJoin(O<DistributorCart>("ProductId"), O<DistributorSalesArea>("ProductId"))
                 .Where(where)
                .OrderBy(D<DistributorCart>("CreationDate"))
                .ToList();
            List<dynamic> newlist = new List<dynamic>();
            foreach (dynamic item in list)
            {
                newlist.Add(LoadDynamic(ds, item));
            }
            return newlist;
        }
        public new static DistributorCart GetById(DataSource ds, long id)
        {
            return ExecuteSingleRow<DistributorCart>(ds, P("Id", id));
        }
        public static new DistributorCart GetProductByUser(DataSource ds, long userid, long productid)
        {
            return Db<DistributorCart>.Query(ds).Select().Where(W("UserId", userid) & W("ProductId", productid)).First<DistributorCart>();
        }
    }
}
