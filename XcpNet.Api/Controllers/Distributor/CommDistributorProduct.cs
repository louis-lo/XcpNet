using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D = XcpNet.Supplier.Modules.Modules;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using A = XcpNet.Ad.Modules;
using System.Web;
using Cnaws.Data.Query;
using C = Cnaws.Comment.Modules;
using Cnaws.Data;

namespace XcpNet.Api.Controllers
{

    public class CommDistributorProduct : CommonControllers
    {
        public static string ClassName = "[type]DistributorProduct";
        protected override void OnInitController()
        {
            NotFound();
        }

        public void GetProductList()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    P.Distributor distributor;
                    if (IsDistributor(out distributor, member.Id))
                    {
                        Dictionary<string, dynamic> productList = new Dictionary<string, dynamic>();
                        string keyword = Request["KeyWord"];
                        int categoryid = int.Parse(Request["CategoryId"]);
                        string price = Request["Price"];
                        int ProvinceId = 0;
                        int CityId = 0;
                        int CountyId = 0;
                        int.TryParse(Request["ProvinceId"], out ProvinceId);
                        int.TryParse(Request["CityId"], out CityId);
                        int.TryParse(Request["CountyId"], out CountyId);
                        D.DistributorCategory cate = new D.DistributorCategory();
                        if (categoryid > 0)
                        {
                            cate = D.DistributorCategory.GetById(DataSource, categoryid);
                            if (cate == null || cate.Id <= 0)
                            {
                                cate.Id = 0;
                            }
                        }
                        else
                            cate.Id = 0;
                        SplitPageData<DataJoin<D.DistributorProduct, D.DistributorCategory>> Splitdata = 
                            D.DistributorProduct.ApiGetPageByWholesale(DataSource, "_".Equals(keyword) ? null : keyword, cate.Id, "_".Equals(price) ? null : price, ProvinceId, CityId, CountyId, Math.Max(1, page), size, 8);
                        List<dynamic> list = new List<dynamic>();
                        long[] productids = Splitdata.Data.Select(x => x.A.Id).ToArray();
                        IList<D.DistributorMapping> distributormapping = null;
                        if (productids.Length > 0)
                            distributormapping = D.DistributorMapping.GetByProductIds(DataSource, productids);
                        foreach (DataJoin<D.DistributorProduct, D.DistributorCategory> product in Splitdata.Data)
                        {
                            dynamic value = new { Product = product, Mapping = distributormapping.Where(x => x.ProductId == product.A.Id).ToList() };
                            //productList.Add(product.A.Id.ToString(), value);
                            list.Add(value);
                        }
                        SplitPageData<dynamic> date = new SplitPageData<dynamic>(Math.Max(1, page), size, list, Splitdata.TotalCount, 8);
                        SetResult(date);
                    }
                    else
                    {
                        SetResult(ApiUtility.DISTRIBUTOR_NOTIS);
                    }
                }
            }
            catch (Exception ex)
            {
                SetResult(ApiUtility.PROGRAM_ERROR, new { Message = ex.ToString() });
            }
        }
#if (DEBUG)
        public static void GetProductListHelper()
        {
            CheckMemberHelper(ClassName, "GetProductList", "获取进货宝供应产品列表")
                 .AddArgument("provinceid", typeof(int), "加盟商省Id")
                 .AddArgument("cityid", typeof(int), "加盟商市Id")
                 .AddArgument("countyid", typeof(int), "加盟商区id")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddArgument("CategoryId", typeof(int), "分类Id")
                 .AddArgument("Price", typeof(string), "价格区间")
                .AddArgument("KeyWord", typeof(string), "关键词")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "找不到对应的供应商")
                .AddResult(true, typeof(string), "成功返回数据,动态对象");
        }
#endif
        public void GetCartList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                P.Distributor distributor;
                if (IsDistributor(out distributor, member.Id))
                {
                    SetResult(D.DistributorCart.GetPageByUser(DataSource, member.Id));
                }
                else
                {
                    SetResult(ApiUtility.DISTRIBUTOR_NOTIS);
                }
            }

        }
#if (DEBUG)
        public static void GetCartListHelper()
        {
            CheckMemberHelper(ClassName, "GetCartList", "获取加盟商购物车")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "找不到对应的供应商")
                .AddResult(true, typeof(IList<DataJoin<D.DistributorCart, D.DistributorProduct>>), "成功返回数据");
        }
#endif
        /// <summary>
        /// 根据父分类Id获取分类
        /// </summary>
        public void GetCategory()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int id = 0;
                int.TryParse(Request["Id"], out id);
                SetResult(D.DistributorCategory.GetAll(DataSource, id));
            }
        }
#if (DEBUG)
        public static void GetCategoryHelper()
        {
            CheckMarkHelper(ClassName, "GetCategory", "根据父分类Id获取进货宝分类")
                .AddArgument("Id", typeof(int), "父分类Id,默认为0")
                .AddResult(true, typeof(IList<D.DistributorCategory>), "返回结果");
        }
#endif

        /// <summary>
        /// 获取产品详情
        /// </summary>
        public void GetProductInfo()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int id = 0;
                if (int.TryParse(Request["Id"], out id))
                {
                    SetResult(D.DistributorProduct.GetProductAndSupplierById(DataSource, id));
                }
                else
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetProductInfoHelper()
        {
            CheckMarkHelper(ClassName, "GetProductInfo", "获取产品详情")
                .AddArgument("Id", typeof(int), "产品Id，如果ParentId不为0则为ParentId")
                .AddResult(true, typeof(D.DistributorProduct), "返回结果Supplier_Level:供应商等级");
        }
#endif

        public void GetAllMappings()
        {
            string mark;
            if (CheckMark(out mark))
            {
                long id = 0;
                if (long.TryParse(Request["id"], out id))
                {
                    D.DistributorProduct p = D.DistributorProduct.GetById(DataSource, id);
                    long pid = p.ParentId > 0 ? p.ParentId : p.Id;
                    IList<D.DistributorMapping> series = D.DistributorMapping.GetAllByProduct(DataSource, id);
                    IList<dynamic> products = D.DistributorProduct.GetPageByParentId(DataSource, pid);
                    List<long> tmp;
                    Dictionary<string, List<long>> temp;
                    IList<D.DistributorMapping> mappings = D.DistributorMapping.GetAllByAllProduct(DataSource, pid);
                    Dictionary<string, Dictionary<string, List<long>>> dict = new Dictionary<string, Dictionary<string, List<long>>>();
                    foreach (D.DistributorMapping item in series)
                    {
                        temp = new Dictionary<string, List<long>>();
                        foreach (D.DistributorMapping map in mappings)
                        {
                            if (map.SerieId == item.SerieId)
                            {
                                if (!temp.TryGetValue(map.Value, out tmp))
                                {
                                    tmp = new List<long>();
                                    temp.Add(map.Value, tmp);
                                }
                                tmp.Add(map.ProductId);
                            }
                        }
                        dict[item.SerieId.ToString()] = temp;
                    }

                    SetResult(new
                    {
                        Series = series,
                        Products = products,
                        Mappings = dict
                    });
                }
            }
        }
#if (DEBUG)
        public static void GetAllMappingsHelper()
        {
            CheckMarkHelper(ClassName, "GetAllMappings", "获取产品,属性,属性值")
                .AddArgument("Id", typeof(int), "产品属性Id")
                .AddResult(true, typeof(string), "返回结果:Series:为属性列表,Products:产品列表,Mappings:属性值列表");
        }
#endif
    }
}
