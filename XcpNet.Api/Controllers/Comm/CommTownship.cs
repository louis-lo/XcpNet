using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Passport;
using XDG = XcpNet.Supplier.Modules.Modules;
using Cnaws.Data;
using Pd = Cnaws.Product.Modules;
using Cnaws.Product;
using Cnaws.Web;

namespace XcpNet.Api.Controllers
{
    public class CommTownship : CommonControllers
    {
        public static string ClassName = "[type]Township";
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
            CheckMarkHelper(ClassName, "GetTownshipList", "获取乡道馆，首页馆列表")
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
            CheckMarkHelper(ClassName, "GetCategory", "根据父分类Id获取乡道馆分类")
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
                int id = 0;
                if (int.TryParse(Request["Id"], out id))
                {
                    int size, page, brand = 0, orderby = 1;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    int.TryParse(Request["brand"], out brand);
                    int.TryParse(Request["orderby"], out orderby);
                    string keyword = "";
                    if (!string.IsNullOrEmpty(Request["keyword"]))
                    {
                        keyword = Request["keyword"];
                    }
                    FilterParameters filter = new FilterParameters();
                    Arguments args = new Arguments(new List<string>() { brand.ToString(), orderby.ToString() }, 0);
                    filter.Load(page, args);
                    IList<Pd.StoreCategory> cates = Pd.StoreCategory.GetAllParentsById(DataSource, id);
                    SetResult(Pd.Product.ApiGetPageByStoreCategory(DataSource, id, cates.Count, keyword, filter, size, 8, 2));
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
            CheckMarkHelper(ClassName, "GetByCategory", "根据分类获取乡道馆产品")
                 .AddArgument("Id", typeof(int), "父分类Id,默认为0")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddArgument("keyword", typeof(int), "搜索关键词，多个用空格格开！")
                 .AddResult(true, typeof(SplitPageData<Pd.Product>), "返回结果");
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
                    SetResult(Pd.Product.GetProductApi(DataSource, 1, id, size));
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
            CheckMarkHelper(ClassName, "GetHotProduct", "根据乡道馆Id获取热门乡道馆产品")
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

                    SetResult(Pd.Product.GetProductApi(DataSource, id, 1, size, DateTime.Now));
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
            CheckMarkHelper(ClassName, "GetGroupProduct", "根据乡道馆Id获取团购乡道馆产品")
                 .AddArgument("xdgId", typeof(int), "当前乡道馆Id")
                 .AddArgument("size", typeof(int), "展示个数,默认为2")
                 .AddResult(true, typeof(SplitPageData<Pd.Product>), "返回结果");
        }
#endif
    }
}
