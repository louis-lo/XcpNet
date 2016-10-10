using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws.Product.Modules;

namespace XcpNet.Website.Modules
{
    [Serializable]
    public sealed class PromotionProduct : NoIdentityModule
    {
        [DataColumn(true)]
        public int ChannelId = 0;
        [DataColumn(true)]
        public long ProductId = 0L;
        [DataColumn(256)]
        public string ProductImage = null;
        public int SortNum = 0;

        public static IList<PromotionProduct> GetAll(DataSource ds, int channel)
        {
            return Db<PromotionProduct>.Query(ds)
                .Select()
                .Where(W("ProductImage", string.Empty, DbWhereType.NotEqual) & W("ChannelId", channel))
                .OrderBy(D("SortNum"))
                .ToList<PromotionProduct>();
        }
        public static IList<Product> GetAllEx(DataSource ds, int channel)
        {
            return Db<Product>.Query(ds)
                .Select()
                .InnerJoin(O<Product>("Id"), O<PromotionProduct>("ProductId"))
                .Where(W<PromotionProduct>("ProductImage", string.Empty) & W<PromotionProduct>("ChannelId", channel))
                .OrderBy(D<PromotionProduct>("SortNum"))
                .ToList<Product>();
        }
    }
}
