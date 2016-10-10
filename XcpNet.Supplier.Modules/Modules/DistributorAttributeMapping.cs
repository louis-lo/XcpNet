using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class DistributorAttributeMapping : Cnaws.Product.Modules.ProductAttributeMapping
    {
        public static new long GetCountByAttributeId(DataSource ds, long attributeId)
        {
            return ExecuteCount<DistributorAttributeMapping>(ds, P("AttributeId", attributeId));
        }

        public static new IList<DistributorAttributeMapping> GetAllByAttribute(DataSource ds, long attrId)
        {
            return DataQuery.Select<DistributorAttributeMapping>(ds, "Value")
                .Where(P("AttributeId", attrId))
                .GroupBy("Value")
                .ToList<DistributorAttributeMapping>();
        }

        public static new IList<DistributorAttributeMapping> GetListByProduct(DataSource ds, long productId)
        {
            return Db<DistributorAttributeMapping>.Query(ds)
                .Select()
                .Where(W("ProductId", productId))
                .ToList<DistributorAttributeMapping>();
        }

        public static new Dictionary<long, string> GetAllByProduct(DataSource ds, long productId)
        {
            IList<DistributorAttributeMapping> list = DataQuery.Select<DistributorAttributeMapping>(ds)
                .Where(P("ProductId", productId))
                .ToList<DistributorAttributeMapping>();
            Dictionary<long, string> dict = new Dictionary<long, string>(list.Count);
            foreach (DistributorAttributeMapping item in list)
                dict.Add(item.AttributeId, item.Value);
            return dict;
        }

        public new static IList<DistributorAttributeMapping> GetAllByCategoryId(DataSource ds, int categoryId, int categorylevel)
        {
            DbWhereQueue where = W("Id", 0, DbWhereType.GreaterThan);
            if (categoryId > 0)
            {
                if (categorylevel == 3)
                    where &= W("CategoryId", categoryId);
                else if (categorylevel == 2)
                    where &= (W("CategoryId", categoryId) | W("CategoryId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId", categoryId)).Result());
                else if (categorylevel == 1)
                    where &= (W("CategoryId", categoryId) | W("CategoryId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId", categoryId)).Result() | W("CategoryId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId").InSelect<DistributorCategory>(S("Id")).Where(W("ParentId", categoryId)).Result()).Result());
            }
            return Db<DistributorAttributeMapping>.Query(ds).Select(S("AttributeId"), S("Value"))
                .Where(W("AttributeId").InSelect<DistributorAttribute>(S("Id")).Where(where).Result()
                )
                .GroupBy(G("AttributeId"), G("Value"))
                .ToList<DistributorAttributeMapping>();
        }
    }
}
