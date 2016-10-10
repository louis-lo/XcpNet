using System;
using System.Collections.Generic;
using Cnaws.Data;
using Cnaws.Web;
using Cnaws.Web.Templates;
using XcpNet.B2bShop.Controllers.Extension;
using XcpNet.Common;
using XcpNet.Supplier.Modules.Modules;

namespace XcpNet.B2bShop.Controllers
{
    public class Programme : CommoSupplierController
    {
        [Authorize(true)]
        [Distributor(true)]
        public void Index(int pageIndex = 1, int indutryCategoryId = 0, int ptype = -1)
        {
            this["IndutryCategoryId"] = indutryCategoryId;
            this["Ptype"] = ptype;
            this["IndutryCategoryList"] = IndutryCategory.GetAll(DataSource, 0);
            DistributorProgramme.EProgrammeType epType = (DistributorProgramme.EProgrammeType)ptype;
            this["ProgrammeList"] = DistributorProgramme.GetList(DataSource, Distributor.UserId, indutryCategoryId, string.Empty, epType, Location.ProvinceId, Location.CityId, Location.CountyId, pageIndex, 5);
            this["ProUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl($"/programme/Index/1/{indutryCategoryId}/{ps[0]}"));
            });
            this["DetailUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl($"/programme/detail/{ps[0]}/{indutryCategoryId}/{ptype}"));
            });
            this["PageUrl"] = new FuncHandler((object[] ps) =>
            {
                return string.Concat(GetUrl($"/programme/Index/{ps[0]}/{indutryCategoryId}/{ptype}"));
            });
            Render("progra_index.html");
        }

        [Authorize(true)]
        [Distributor(true)]
        public void Detail(int progId, int indutryCategoryId, int ptype)
        {
            this["PrograName"] = DistributorProgramme.GetById(DataSource, progId, Distributor.UserId)?.Title ?? "全部方案";
            this["IndutryCategoryName"] = IndutryCategory.GetById(DataSource, indutryCategoryId)?.Name ?? "全部行业";

            this["ProductList"] = ProgrammeProductMapping.GetAllById(DataSource, progId);

            Render("progra_detail.html");
        }

        [Authorize(true)]
        [Distributor(true)]
        [HttpAjax]
        [HttpGet]
        public void GetProductIdsAndCounts(int progId)
        {
            Cart cart = new Cart();
            IList<DataJoin<ProgrammeProductMapping, DistributorProduct>> products = ProgrammeProductMapping.GetAllById(DataSource, progId);
            List<long> ids = new List<long>();
            List<int> counts = new List<int>();
            foreach (DataJoin<ProgrammeProductMapping, DistributorProduct> product in products)
            {
                ids.Add(product.B.Id);
                counts.Add(product.B.WholesaleCount);
            }

            SetResult(true, new { id = string.Join(",", ids), count = string.Join(",", counts) });
        }
    }
}
