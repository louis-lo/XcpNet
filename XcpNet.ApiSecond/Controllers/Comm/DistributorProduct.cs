using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Pay;
using System.Collections.Generic;
using System.Threading;
using A = XcpNet.Ad.Modules;
using Py = Cnaws.Pay.Modules;
using C = Cnaws.Comment.Modules;
using Cnaws.Data.Query;
using D = XcpNet.Supplier.Modules.Modules;
using Pd = Cnaws.Product.Modules;
using System.Web;
using System.Text;
using Cnaws.Json;
using Af = XcpNet.AfterSales.Modules;
using XcpNet.Common;
using Cnaws.Product;
using System.Linq;

namespace XcpNet.ApiSecond.Controllers
{
    public class DistributorProduct2 : CommControllers2
    {
        public static string ClassName = "[type]DistributorProduct2";
        protected override void OnInitController()
        {
            NotFound();
        }


        public void GetRecommend()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                Pd.Distributor distributor = Pd.Distributor.GetById(DataSource, member.Id);
                int show = 10;
                if (!int.TryParse(Request["show"], out show) || show < 1)
                    show = 10;
                int suppliertype = -1;
                if (!int.TryParse(Request["suppliertype"], out suppliertype) || suppliertype < 0)
                    suppliertype = -1;
                IList<dynamic> ProductList = D.DistributorProduct.ApiGetPageByRecommend(DataSource, show, distributor.Province, distributor.City, distributor.County,suppliertype);
                SetResult(new
                {
                    Product = ProductList
                });
            }
        }
#if (DEBUG)
        public static void GetRecommendHelper()
        {
            CheckMemberApi(ClassName, "GetRecommend", "获取首页推荐产品列表")
                 .AddArgument("show", typeof(int), "显示个数,默认10个")
                 .AddResult(true, typeof(IList<D.DistributorProduct>), "返回结果 Product:产品列表");
        }
#endif

        public void GetCategoryRecommend()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                Pd.Distributor distributor = Pd.Distributor.GetById(DataSource, member.Id);
                int show = 10;
                if (!int.TryParse(Request["show"], out show) || show < 1)
                    show = 10;
                int suppliertype = -1;
                if (!int.TryParse(Request["suppliertype"], out suppliertype) || suppliertype < 0)
                    suppliertype = -1;
                List<dynamic> list = new List<dynamic>();
                IList<D.DistributorCategory> categorylist = D.DistributorCategory.GetAll(DataSource, 0);
                foreach(D.DistributorCategory category in categorylist)
                {
                    IList<dynamic> ProductList = D.DistributorProduct.ApiGetRecommendByCategory(DataSource, show,category.Id, distributor.Province, distributor.City, distributor.County, suppliertype);
                    IList<A.Advertisement> ad = A.Advertisement.GetByLabelAndCategory(DataSource, 15, category.Id);
                    list.Add(new { Category = category, Product = ProductList, Advertisement = ad });
                }
                SetResult(list);
            }
        }
#if (DEBUG)
        public static void GetCategoryRecommendHelper()
        {
            CheckMemberApi(ClassName, "GetCategoryRecommend", "获取首页分类推荐产品列表,包含广告")
                 .AddArgument("show", typeof(int), "显示个数,默认10个")
                 .AddResult(true, typeof(IList<D.DistributorProduct>), "返回结果Category:分类信息, Product:产品列表,Advertisement:广告图列表");
        }
#endif


        /// <summary>
        /// 根据相前参数获取产品
        /// </summary>
        public void GetProductList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    Pd.Distributor distributor = Pd.Distributor.GetById(DataSource, member.Id);
                    ///分类
                    int id = 0;
                    if (int.TryParse(Request["Id"], out id))
                    {
                        FilterParameters2 filter = new FilterParameters2();
                        int size = 10, page = 1, brand = 0, orderby = 1;
                        long storeid = 0;
                        ///显示条数
                        if (!int.TryParse(Request["size"], out size) || size < 1)
                            size = 10;
                        filter.Size = size;
                        ///页码
                        if (!int.TryParse(Request["page"], out page) || page < 1)
                            page = 1;
                        filter.Page = page;
                        ///排序
                        int.TryParse(Request["orderby"], out orderby);
                        filter.OrderBy = orderby;
                        ///关键词
                        if (!string.IsNullOrEmpty(Request["keyword"]))
                        {
                            filter.KeyWord = HttpUtility.UrlDecode(Request["keyword"]);
                        }
                        ///产品属性
                        if (!string.IsNullOrEmpty(Request["attribute"]))
                        {
                            filter.Attribute = HttpUtility.UrlDecode(Request["attribute"]);
                        }
                        ///供应类型
                        int suppliertype = -1;
                        int.TryParse(Request["suppliertype"], out suppliertype);
                        filter.SupplierType = suppliertype;
                        ///品牌
                        int.TryParse(Request["brand"], out brand);
                        filter.Brand = brand;
                        ///店铺Id
                        long.TryParse(Request["storeid"], out storeid);
                        filter.StoreId = storeid;
                        int storecategoryid = 0;
                        ///店铺Id
                        int.TryParse(Request["storecategoryid"], out storecategoryid);
                        filter.StoreCategoryId = storecategoryid;
                        //价格
                        if (!string.IsNullOrEmpty(Request["price"]))
                        {
                            filter.Price = Request["price"];
                        }
                        ///区域
                        filter.Province = distributor.Province;
                        filter.City = distributor.City;
                        filter.County = distributor.County;
                        int categorylevel = 0;
                        IList<dynamic> CategoryList = null;
                        IList<D.DistributorAttribute> AttributeList = null;
                        IList<D.DistributorAttributeMapping> AttributeMappingList = null;
                        IList<D.DistributorBrand> BrandList = null;
                        if (id > 0)
                        {
                            IList<D.DistributorCategory> cates = D.DistributorCategory.GetAllParentsById(DataSource, id);
                            categorylevel = cates.Count;
                            AttributeList = D.DistributorAttribute.GetAllByCategory(DataSource, id);
                            AttributeMappingList = D.DistributorAttributeMapping.GetAllByCategoryId(DataSource, id, categorylevel);
                            BrandList = D.DistributorBrand.GetAllByCategoryAndScreen(DataSource, id);
                        }
                        SplitPageData<dynamic> ProductList = D.DistributorProduct.ApiGetPageByCategory(DataSource, id, categorylevel, filter, 8, 1);
                        IList<D.DistributorSerie> ProductSerieList = null;
                        IList<D.DistributorMapping> ProductMappingList = null;
                        //Dictionary<long, IList<D.DistributorSerie>> SerieList = new Dictionary<long, IList<D.DistributorSerie>>();
                        if (ProductList.Data.Count > 0)
                        {
                            //IEnumerable<object> listID = ProductList.Select(x => x.DistributorProduct_Id).ToList<object>();
                            //ProductSerieList = D.DistributorSerie.GetAllByProducts(DataSource, listID.ToArray());

                            //foreach(long pid in ProductSerieList.Select(x => x.ProductId).ToList().ToArray())
                            //{
                            //    SerieList.Add(pid, ProductSerieList.Where(x => x.ProductId == pid).ToList());
                            //}
                            //ProductMappingList = D.DistributorMapping.GetAllByProducts(DataSource, listID.ToArray());
                            ///实现找产品规格城品惠在列表页不显示，所以暂时不做业务逻辑
                        }
                        if (id == 0 && !string.IsNullOrEmpty(filter.KeyWord))
                        {
                            CategoryList = D.DistributorCategory.GetCategoryByApiProductList(DataSource, id, categorylevel, filter, 1);
                        }

                        SetResult(new { Product = ProductList, Category = CategoryList, Brand = BrandList, Attribute = AttributeList, AttributeMapping = AttributeMappingList, ProductSerie = ProductSerieList, ProductMapping = ProductMappingList });
                    }
                    else
                    {
                        SetResult(false);
                    }
                }
                catch (Exception ex)
                {
                    SetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void GetProductListHelper()
        {
            CheckMemberApi(ClassName, "GetProductList", "根据参数获取产品列表")
                 .AddArgument("id", typeof(int), "父分类Id,默认为0")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddArgument("orderby", typeof(int), "排序,默认为0 1:销量降序,2:销量升序，3:人气降序,4:人气升序,5:价格升序,6:价格降序")
                 .AddArgument("keyword", typeof(string), "搜索关键词,多个用空格隔开")
                 .AddArgument("attribute", typeof(int), "产品属性,属性名和属性值用'_'对应，多个用'@'分隔，格式为:属性Id_属性值@属性2Id_属性2值,最后都以@结尾")
                 .AddArgument("isbrand", typeof(int), "是否是品牌,默认值0为不限,1为非品牌,2为品牌")
                 .AddArgument("brand", typeof(int), "品牌Id,默认为0")
                 .AddArgument("price", typeof(int), "价格区间,用'-'隔开,后面为0时表示不设上限")
                 .AddArgument("storeid", typeof(int), "店铺Id,默认为0")
                 .AddArgument("storecategoryid", typeof(int), "店铺分类Id,默认为0")
                 .AddResult(true, typeof(SplitPageData<D.DistributorProduct>), "返回结果 Product:产品列表,Category:分类列表,Brand:品牌列表,Attribute:分类属性列表,AttributeMapping:属性值列表,ProductSerie:产品规格列表,ProductMapping:产品规格值列表");
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
            CheckMarkApi(ClassName, "GetCategory", "根据父分类Id获取分类")
                .AddArgument("Id", typeof(int), "父分类Id,默认为0")
                .AddResult(true, typeof(IList<Pd.ProductCategory>), "返回结果");
        }
#endif
        /// <summary>
        /// 获取产品详情
        /// </summary>
        public void GetProductInfo()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                int id = 0;
                if (int.TryParse(Request["Id"], out id))
                {
                    Pd.Distributor distributor=Pd.Distributor.GetById(DataSource, member.Id);
                    D.DistributorProduct p = D.DistributorProduct.GetById(DataSource, id);
                    if (p == null)
                    {
                        SetResult(false);
                        return;
                    }
                    long pid = p.ParentId > 0 ? p.ParentId : p.Id;
                    IList<D.DistributorMapping> series = D.DistributorMapping.GetAllByProduct(DataSource, id);
                    IList<dynamic> products = D.DistributorProduct.GetPageByParentId2(DataSource, pid, distributor.Province, distributor.City, distributor.County);
                    List<long> tmp;
                    Dictionary<string, List<long>> temp;
                    IList<D.DistributorMapping> mappings = D.DistributorMapping.GetAllByAllProduct(DataSource, pid);
                    IList<DataJoin<D.DistributorAttribute, D.DistributorAttributeMapping>> attr = D.DistributorAttribute.GetAllValuesByProduct(DataSource, pid);
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
                    object supplier = null;
                    int level = 0;
                    if (p.ProductType == 1)
                    {
                        if (p.SupplierId > 0)
                        {
                            supplier = Pd.StoreInfo.GetStoreInfoByUserId(DataSource, p.SupplierId);
                            level = Pd.Supplier.GetById(DataSource, p.SupplierId).Level;
                        }
                    }
                    SetResult(new
                    {
                        Products = products,
                        Series = series,
                        Mappings = dict,
                        AttrValue=attr,
                        Supplier = supplier,
                        Level = level
                    });
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
            CheckMemberApi(ClassName, "GetProductInfo", "获取产品详情")
                .AddArgument("Id", typeof(int), "产品Id，如果ParentId不为0则为ParentId")
                .AddResult(true, typeof(Pd.Product), "返回结果Supplier_Level:供应商等级");
        }
#endif

        /// <summary>
        /// 根据父分类Id获取行业分类列表
        /// </summary>
        public void GetIndustryList()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int id = 0;
                int.TryParse(Request["Id"], out id);
                SetResult(D.IndutryCategory.GetAll(DataSource, id));
            }
        }
#if (DEBUG)
        public static void GetIndustryListHelper()
        {
            CheckMarkApi(ClassName, "GetIndustryList", "根据父分类Id获取行业分类列表")
                .AddArgument("Id", typeof(int), "父分类Id,默认为0,-1为查询所有")
                .AddResult(true, typeof(IList<Pd.ProductCategory>), "返回结果");
        }
#endif

        /// <summary>
        /// 获取进货方案列表
        /// </summary>
        public void GetProgrammes()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                Pd.Distributor distributor = Pd.Distributor.GetById(DataSource, member.Id);
                int size = 10, page = 1;
                ///显示条数
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                ///页码
                if (!int.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                int category = 0;
                int.TryParse(Request["category"], out category);
                string title = Request["keyword"];
                D.DistributorProgramme.EProgrammeType type = D.DistributorProgramme.EProgrammeType.All;
                Enum.TryParse(Request["type"], out type);
                SplitPageData<D.DistributorProgramme> programme = D.DistributorProgramme.GetList(DataSource, 0, category, title, type, distributor.Province, distributor.City, distributor.County, page, size, 8);
                SetResult(programme);
            }
        }
#if (DEBUG)
        public static void GetProgrammesHelper()
        {
            CheckMemberApi(ClassName, "GetProgrammes", "获取进货方案列表")
                .AddArgument("keyword", typeof(string), "方案标题关键词")
                .AddArgument("category", typeof(int), "行业分类Id,0为所有行业")
                .AddArgument("type", typeof(string), "All(-1):所有,NewStore(0):新店方案,ReformStore(1):改造店方案,PublicProgramme(2):综合方案")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(true, typeof(D.DistributorProgramme), "返回结果进货方案列表");
        }
#endif

        /// <summary>
        /// 获取进货宝里面的产品列表
        /// </summary>
        public void GetProgrammesMapping()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                long id = 0;
                long.TryParse(Request["id"], out id);
                if (id > 0)
                {
                    SetResult(D.ProgrammeProductMapping.ApiGetAllById(DataSource, id));
                }
                else
                {
                    SetResult(CommUtility.PARAMETER_ERROR);
                }
            }
        }
#if (DEBUG)
        public static void GetProgrammesMappingHelper()
        {
            CheckMemberApi(ClassName, "GetProgrammesMapping", "获取进货宝里面的产品列表")
                .AddArgument("id", typeof(long), "方案Id")
                .AddResult(true, typeof(D.DistributorProgramme), "返回结果进货方案列表");
        }
#endif
    }
}
