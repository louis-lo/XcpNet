using System;
using System.Collections.Generic;
using XDG = XcpNet.Supplier.Modules.Modules;
using Cnaws.Data;
using Pd = Cnaws.Product.Modules;
using Cnaws.Product;
using System.Web;
using XcpNet.Common;

namespace XcpNet.ApiSecond.Controllers
{
    public class Township2 : CommControllers2
    {
        public static string ClassName = "[type]Township2";
        protected override void OnInitController()
        {
            NotFound();
        }
        public void GetTownshipList()
        {
            try
            {
                string mark;
                if (CheckMark(out mark))
                {
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    SetResult(XDG.XDGInfo.GetXDGInfoList(DataSource, page, size));
                }
            }
            catch (Exception)
            { }
        }
#if (DEBUG)
        public static void GetTownshipListHelper()
        {
            CheckMarkApi(ClassName, "GetTownshipList", "获取乡道馆，首页馆列表")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddResult(true, typeof(SplitPageData<XDG.XDGInfo>), "返回结果");
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
                int xdgid = 0;
                int.TryParse(Request["xdgId"], out xdgid);
                SetResult(Pd.StoreCategory.GetAll(DataSource, xdgid, id));
            }
        }
#if (DEBUG)
        public static void GetCategoryHelper()
        {
            CheckMarkApi(ClassName, "GetCategory", "根据父分类Id获取乡道馆分类")
                .AddArgument("Id", typeof(int), "父分类Id,默认为0")
                .AddArgument("xdgId", typeof(int), "乡道馆Id")
                .AddResult(true, typeof(IList<Pd.StoreCategory>), "返回结果");
        }
#endif

        /// <summary>
        /// 根据分类获取产品
        /// </summary>
        public void GetByCategory()
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
                            IList<Pd.StoreCategory> cates = Pd.StoreCategory.GetAllParentsById(DataSource, id);
                            categorylevel = cates.Count;
                            AttributeList = Pd.ProductAttribute.GetAllByCategoryAndScreen(DataSource, id);
                            AttributeMappingList = Pd.ProductAttributeMapping.GetAllByCategoryId(DataSource, id, categorylevel);
                            BrandList = Pd.ProductBrand.GetAllByCategoryAndScreen(DataSource, id);
                        }
                        SplitPageData<dynamic> ProductList = Pd.Product.ApiGetPageByCategory(DataSource, id, categorylevel, filter, 8, 2);
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
        public static void GetByCategoryHelper()
        {
           CheckMarkApi(ClassName, "GetByCategory", "根据分类获取产品")
                 .AddArgument("Id", typeof(int), "父分类Id,默认为0")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddArgument("provinceid", typeof(int), "加盟商省Id")
                 .AddArgument("cityid", typeof(int), "加盟商市Id")
                 .AddArgument("countyid", typeof(int), "加盟商区id")
                 .AddArgument("orderby", typeof(int), "排序,默认为0 1:销量降序,2:销量升序，3:人气降序,4:人气升序,5:价格升序,6:价格降序")
                 .AddArgument("keyword", typeof(string), "搜索关键词,多个用空格隔开")
                 .AddArgument("attribute", typeof(int), "产品属性,属性名和属性值用'_'对应，多个用'@'分隔，格式为:属性Id_属性值@属性2Id_属性2值,最后都以@结尾")
                 .AddArgument("isbrand", typeof(int), "是否是品牌,默认值0为不限,1为非品牌,2为品牌")
                 .AddArgument("brand", typeof(int), "品牌Id,默认为0")
                 .AddArgument("price", typeof(int), "价格区间,用'-'隔开,后面为0时表示不设上限")
                 .AddArgument("storeid", typeof(int), "店铺Id,默认为0")
                 .AddArgument("storecategoryid", typeof(int), "店铺分类Id,默认为0")
                 .AddResult(true, typeof(SplitPageData<Pd.Product>), "返回结果 Product:产品列表,Category:分类列表,Brand:品牌列表,Attribute:分类属性列表,AttributeMapping:属性值列表,ProductSerie:产品规格列表,ProductMapping:产品规格值列表");
        }
#endif

        public void GetHotProduct()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int id = 0;
                if (int.TryParse(Request["xdgId"], out id))
                {
                    int size;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 6;
                    int ProvinceId = 0;
                    int CityId = 0;
                    int CountyId = 0;
                    int.TryParse(Request["ProvinceId"], out ProvinceId);
                    int.TryParse(Request["CityId"], out CityId);
                    int.TryParse(Request["CountyId"], out CountyId);
                    SetResult(Pd.Product.GetProductApiByArea(DataSource, ProvinceId , CityId, CountyId,1, id, size));
                }
                else
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetHotProductHelper()
        {
            CheckMarkApi(ClassName, "GetHotProduct", "根据乡道馆Id获取热门乡道馆产品")
                 .AddArgument("xdgId", typeof(int), "当前乡道馆Id")
                 .AddArgument("size", typeof(int), "展示个数,默认为6")
                 .AddResult(true, typeof(SplitPageData<Pd.Product>), "返回结果");
        }
#endif

        public void GetGroupProduct()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int id = 0;
                if (int.TryParse(Request["xdgId"], out id))
                {
                    int size;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 2;
                    int ProvinceId = 0;
                    int CityId = 0;
                    int CountyId = 0;
                    int.TryParse(Request["ProvinceId"], out ProvinceId);
                    int.TryParse(Request["CityId"], out CityId);
                    int.TryParse(Request["CountyId"], out CountyId);

                    SetResult(Pd.Product.GetProductApiByArea(DataSource, ProvinceId, CityId, CountyId, id, 1, size, DateTime.Now));
                }
                else
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetGroupProductHelper()
        {
            CheckMarkApi(ClassName, "GetGroupProduct", "根据乡道馆Id获取团购乡道馆产品")
                 .AddArgument("xdgId", typeof(int), "当前乡道馆Id")
                 .AddArgument("size", typeof(int), "展示个数,默认为2")
                 .AddResult(true, typeof(SplitPageData<Pd.Product>), "返回结果");
        }
#endif
    }
}
