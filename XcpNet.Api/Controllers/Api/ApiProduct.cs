
using System.Collections.Generic;
using Cnaws.Data;
using Pd = Cnaws.Product.Modules;
using S = Cnaws.Statistic.Modules;
using Cnaws.Product;
using Cnaws.Web;
using System;

namespace XcpNet.Api.Controllers
{
    public class ApiProduct : CommonControllers
    {
        /// <summary>
        /// 根据分类获取产品
        /// </summary>
        public void GetByCategory()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int id = 0;
                if (int.TryParse(Request["Id"], out id))
                {
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;

                    IList<Pd.ProductCategory> cates = Pd.ProductCategory.GetAllParentsById(DataSource, id);
                    SetResult(Pd.Product.GetPageByApi(DataSource, id, cates.Count, page, size, 11));
                }
                else
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetByCategoryHelper()
        {
            CheckMarkHelper("ApiProduct", "GetByCategory", "根据分类获取产品")
                 .AddArgument("Id", typeof(int), "父分类Id,默认为0")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddResult(true, typeof(Pd.Product), "返回结果");
        }
#endif


        /// <summary>
        /// 根据分类获取产品
        /// </summary>
        public void GetBrandByCategory()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int id = 0;
                if (int.TryParse(Request["Id"], out id))
                {
                    int size, page, isbrand = 0;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    if (!int.TryParse(Request["isbrand"], out isbrand) || isbrand > 1)
                        isbrand = 1;
                    IList<Pd.ProductCategory> cates = Pd.ProductCategory.GetAllParentsById(DataSource, id);
                    SetResult(Pd.Product.GetBrandPageByApi(DataSource, id, cates.Count, isbrand, page, size, 11));
                }
                else
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetBrandByCategoryHelper()
        {
            CheckMarkHelper("ApiProduct", "GetBrandByCategory", "根据分类获取品牌产品")
                 .AddArgument("Id", typeof(int), "父分类Id,默认为0")
                 .AddArgument("isbrand", typeof(int), "品牌为1,默认为0")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddResult(true, typeof(Pd.Product), "返回结果");
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
            CheckMarkHelper("ApiProduct", "GetCategory", "根据父分类Id获取分类")
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
                    SetResult(Pd.Product.GetProductAndSupplierById(DataSource, id));
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
            CheckMarkHelper("ApiProduct", "GetProductInfo", "获取产品详情")
                .AddArgument("Id", typeof(int), "产品Id，如果ParentId不为0则为ParentId")
                .AddResult(true, typeof(Pd.Product), "返回结果Supplier_Level:供应商等级");
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
                    Pd.Product p = Pd.Product.GetById(DataSource, id);
                    long pid = p.ParentId > 0 ? p.ParentId : p.Id;
                    IList<Pd.ProductMapping> series = Pd.ProductMapping.GetAllByProduct(DataSource, id);
                    IList<dynamic> products = Pd.Product.GetPageByParentId(DataSource, pid);
                    List<long> tmp;
                    Dictionary<string, List<long>> temp;
                    IList<Pd.ProductMapping> mappings = Pd.ProductMapping.GetAllByAllProduct(DataSource, pid);
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
            CheckMarkHelper("ApiProduct", "GetAllMappings", "获取产品,属性,属性值")
                .AddArgument("Id", typeof(int), "产品属性Id")
                .AddResult(true, typeof(string), "返回结果:Series:为属性列表,Products:产品列表,Mappings:属性值列表");
        }
#endif

        /// <summary>
        /// 根据关键词搜索
        /// </summary>
        public void SearchByKeyWord()
        {
            string mark;
            if (CheckMark(out mark))
            {
                try
                {
                    string keyword = Request["keyword"];
                    int size, page, brand = 0, orderby = 1;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    int.TryParse(Request["brand"], out brand);
                    int.TryParse(Request["orderby"], out orderby);
                    FilterParameters filter = new FilterParameters();
                    Arguments args = new Arguments(new List<string>() { brand.ToString(), orderby.ToString() }, 0);
                    filter.Load(page, args);
                    SetResult(Pd.Product.ApiGetPageBySearch(DataSource, keyword, filter, page, size, 11));
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void SearchByKeyWordHelper()
        {
            CheckMarkHelper("ApiProduct", "SearchByKeyWord", "根据关键词获取产品")
                 .AddArgument("keyword", typeof(int), "关键词,多个用空格格开")
                 .AddArgument("brand", typeof(int), "品牌Id,默认为0")
                 .AddArgument("orderby", typeof(int), "排序,默认为0   1:销量降序,2:销量升序，3:人气降序,4:人气升序,5:价格升序,6:价格降序")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddResult(true, typeof(SplitPageData<DataJoin<Pd.Product, S.StatisticData>>), "返回结果");
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
            CheckMarkHelper("ApiProduct", "GetHotKeyWord", "获取热门搜索关键词")
                 .AddArgument("length", typeof(int), "关键词最大长度,默认为7")
                 .AddArgument("count", typeof(int), "关键词个数,默认为10")
                 .AddResult(true, typeof(IList<S.StatisticTag>), "返回结果");
        }
#endif

    }
}
