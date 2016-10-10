
using System.Collections.Generic;
using Cnaws.Data;
using Pd = Cnaws.Product.Modules;
using S = Cnaws.Statistic.Modules;
using Cnaws.Product;
using System;
using A = Cnaws.Article.Modules;
using System.Web;
using XcpNet.Common;
namespace XcpNet.ApiSecond.Controllers
{
    public class Product2 : CommControllers2
    {
        public static string ClassName = "[type]Product2";
        protected override void OnInitController()
        {
            NotFound();
        }

        /// <summary>
        /// 根据相前参数获取产品
        /// </summary>
        public void GetProductList()
        {
            string mark;
            if (CheckMark(out mark))
            {
                try
                {
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
                        int.TryParse(Request["SupplierType"], out suppliertype);
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
                        int ProvinceId = 0;
                        int CityId = 0;
                        int CountyId = 0;
                        int.TryParse(Request["ProvinceId"], out ProvinceId);
                        int.TryParse(Request["CityId"], out CityId);
                        int.TryParse(Request["CountyId"], out CountyId);
                        filter.Province = ProvinceId;
                        filter.City = CityId;
                        filter.County = CountyId;
                        int categorylevel = 0;
                        IList<dynamic> CategoryList = null;
                        IList<Pd.ProductAttribute> AttributeList = null;
                        IList<Pd.ProductAttributeMapping> AttributeMappingList = null;
                        IList<Pd.ProductBrand> BrandList = null;
                        if (id > 0)
                        {
                            IList<Pd.ProductCategory> cates = Pd.ProductCategory.GetAllParentsById(DataSource, id);
                            categorylevel = cates.Count;
                            AttributeList = Pd.ProductAttribute.GetAllByCategoryAndScreen(DataSource, id);
                            AttributeMappingList = Pd.ProductAttributeMapping.GetAllByCategoryId(DataSource, id, categorylevel);
                            BrandList = Pd.ProductBrand.GetAllByCategoryAndScreen(DataSource, id);
                        }
                        SplitPageData<dynamic> ProductList = Pd.Product.ApiGetPageByCategory(DataSource, id, categorylevel, filter, 8, 1);
                        IList<Pd.ProductSerie> ProductSerieList = null;
                        IList<Pd.ProductMapping> ProductMappingList = null;
                        if (ProductList.Data.Count > 0)
                        {
                            ///实现找产品规格城品惠在列表页不显示，所以暂时不做业务逻辑
                        }
                        if (id == 0 && !string.IsNullOrEmpty(filter.KeyWord))
                        {
                            CategoryList = Pd.ProductCategory.GetCategoryByApiProductList(DataSource, id, categorylevel, filter, 1);
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
            CheckMarkApi(ClassName, "GetProductList", "根据参数获取产品列表")
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
                 .AddArgument("provinceid", typeof(int), "收货省Id")
                 .AddArgument("cityid", typeof(int), "收货市Id")
                 .AddArgument("countyid", typeof(int), "收货区id")
                 .AddResult(true, typeof(SplitPageData<Pd.Product>), "返回结果 Product:产品列表,Category:分类列表,Brand:品牌列表,Attribute:分类属性列表,AttributeMapping:属性值列表,ProductSerie:产品规格列表,ProductMapping:产品规格值列表");
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
                SetResult(Pd.ProductCategory.GetAll(DataSource, id));
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
            string mark;
            if (CheckMark(out mark))
            {
                int id = 0;
                if (int.TryParse(Request["Id"], out id))
                {

                    Pd.Product p = Pd.Product.GetById(DataSource, id);
                    if (p == null)
                    {
                        SetResult(CommUtility.PRODUCT_ERROR);
                        return;
                    }
                    long pid = p.ParentId > 0 ? p.ParentId : p.Id;
                    int ProvinceId = 0;
                    int CityId = 0;
                    int CountyId = 0;
                    int.TryParse(Request["ProvinceId"], out ProvinceId);
                    int.TryParse(Request["CityId"], out CityId);
                    int.TryParse(Request["CountyId"], out CountyId);
                    IList<Pd.ProductMapping> series = Pd.ProductMapping.GetAllByProduct(DataSource, id);
                    IList<dynamic> products = Pd.Product.GetPageByParentId2(DataSource, pid, ProvinceId, CityId, CountyId);
                    List<long> tmp;
                    Dictionary<string, List<long>> temp;
                    IList<Pd.ProductMapping> mappings = Pd.ProductMapping.GetAllByAllProduct(DataSource, pid);

                    IList<DataJoin<Pd.ProductAttribute,Pd.ProductAttributeMapping>> attr = Pd.ProductAttribute.GetAllValuesByProduct(DataSource, pid);

                    Dictionary<string, Dictionary<string, List<long>>> dict = new Dictionary<string, Dictionary<string, List<long>>>();
                    foreach (Pd.ProductMapping item in series)
                    {
                        temp = new Dictionary<string, List<long>>();
                        foreach (Pd.ProductMapping map in mappings)
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
                        supplier = Pd.StoreInfo.GetStoreInfoByUserId(DataSource, p.SupplierId);
                        level = Pd.Supplier.GetById(DataSource, p.SupplierId).Level;
                    }
                    else if (p.ProductType == 2)
                    {
                        supplier = XcpNet.Supplier.Modules.Modules.XDGInfo.GetXDGInfoByUserId(DataSource, p.SupplierId);
                        level = 0;
                    }

                    SetResult(new
                    {
                        Products = products,
                        Series = series,
                        Mappings = dict,
                        AttrValue= attr,
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
            CheckMarkApi(ClassName, "GetProductInfo", "获取产品详情")
                .AddArgument("Id", typeof(int), "产品Id，如果ParentId不为0则为ParentId")
                .AddArgument("provinceid", typeof(int), "收货省Id")
                .AddArgument("cityid", typeof(int), "收货市Id")
                .AddArgument("countyid", typeof(int), "收货区id")
                .AddResult(true, typeof(Pd.Product), "返回结果Supplier_Level:供应商等级");
        }
#endif

        /// <summary>
        /// 获取热门搜索关键词
        /// </summary>
        public void GetHotKeyWord()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int Length, Count;
                if (!int.TryParse(Request["length"], out Length) || Length < 1)
                    Length = 7;
                if (!int.TryParse(Request["count"], out Count) || Count < 1)
                    Count = 10;
                SetResult(S.StatisticTag.GetTop(this.DataSource, Count, Length));
            }
        }
#if (DEBUG)
        public static void GetHotKeyWordHelper()
        {
            CheckMarkApi(ClassName, "GetHotKeyWord", "获取热门搜索关键词")
                 .AddArgument("length", typeof(int), "关键词最大长度,默认为7")
                 .AddArgument("count", typeof(int), "关键词个数,默认为10")
                 .AddResult(true, typeof(IList<S.StatisticTag>), "返回结果");
        }
#endif

        public void GetMonthHotPage()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int page, size;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                if (!int.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                int suppliertype = -1;
                if (!int.TryParse(Request["suppliertype"], out suppliertype) || suppliertype < 0)
                    suppliertype = -1;
                SetResult(Pd.Product.GetMonthHotPageApi(DataSource, page, size, suppliertype,8));
            }
        }

#if (DEBUG)
        public static void GetMonthHotPageHelper()
        {
            CheckMarkApi(ClassName, "GetMonthHotPage", "获取为你推荐产品")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddArgument("size", typeof(int), "显示条数,默认为10")
                 .AddResult(true, typeof(SplitPageData<Pd.Product>), "返回结果");
        }
#endif

        public void GetTopArticle()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int page, size;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                if (!int.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                SetResult(A.Article.GetTop(DataSource, 1, size));
            }
        }
#if (DEBUG)
        public static void GetTopArticleHelper()
        {
            CheckMarkApi(ClassName, "GetTopArticle", "获取网站公告")
                 .AddArgument("size", typeof(int), "显示条数,默认为10")
                 .AddResult(true, typeof(IList<A.Article>), "返回结果");
        }
#endif
    }
}
