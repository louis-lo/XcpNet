using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    public class DistributorBrandMapping : Cnaws.Product.Modules.ProductBrandMapping
    {


        public new static IList<DistributorCategory> GetCategoryListByBrandId(DataSource ds, int brandid)
        {
            return Db<DistributorBrandMapping>.Query(ds)
                .Select(S<DistributorCategory>())
                .InnerJoin(O<DistributorBrandMapping>("CategoryId"), O<DistributorCategory>("Id"))
                .Where(W<DistributorBrandMapping>("BrandId", brandid))
                .OrderBy(D<DistributorCategory>("SortNum"))
                .ToList<DistributorCategory>();
        }

        public new static IList<DistributorBrandMapping> GetByBrandId(DataSource ds, int brandId)
        {
            return Db<DistributorBrandMapping>.Query(ds)
                .Select()
                .Where(W("BrandId", brandId))
                .ToList<DistributorBrandMapping>();
        }

        public new static IList<DistributorBrand> GetBrandListByCategoryIdAndScreen(DataSource ds, int categoryid)
        {
            return Db<DistributorBrandMapping>.Query(ds)
                .Select(S<DistributorBrand>())
                .InnerJoin(O<DistributorBrandMapping>("BrandId"), O<DistributorBrand>("Id"))
                .Where(W<DistributorBrandMapping>("CategoryId", categoryid) & W<DistributorBrand>("Screen", true))
                .OrderBy(D<DistributorBrand>("SortNum"))
                .ToList<DistributorBrand>();
        }

        public new static IList<DistributorBrand> GetBrandListByCategoryId(DataSource ds, int categoryid)
        {
            return Db<DistributorBrandMapping>.Query(ds)
                .Select(S<DistributorBrand>())
                .InnerJoin(O<DistributorBrandMapping>("BrandId"), O<DistributorBrand>("Id"))
                .Where(W<DistributorBrandMapping>("CategoryId", categoryid))
                .OrderBy(D<DistributorBrand>("SortNum"))
                .ToList<DistributorBrand>();
        }

        public new static DataStatus Add(DataSource ds, DistributorBrandMapping brandmapping)
        {
            if (Db<DistributorBrandMapping>.Query(ds)
                .Select().Where(W("BrandId", brandmapping.BrandId) & W("CategoryId", brandmapping.CategoryId))
                .Count() > 0)
                return DataStatus.Success;
            else
            {
                return brandmapping.Insert(ds);
            }
        }
        public new static DataStatus Del(DataSource ds, List<int> ids, int brandid)
        {
            if (ids.Count > 0)
                Db<DistributorBrandMapping>.Query(ds).Delete().Where(W("BrandId", brandid) & W("CategoryId", ids.ToArray(), DbWhereType.In)).Execute();
            return DataStatus.Success;
        }
        public new static DataStatus DelByBrandId(DataSource ds, int brandid)
        {
            Db<DistributorBrandMapping>.Query(ds).Delete().Where(W("BrandId", brandid)).Execute();
            return DataStatus.Success;
        }
    }
}
