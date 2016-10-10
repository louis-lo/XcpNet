
using System.Collections.Generic;
using Cnaws.Data;
using Pd = Cnaws.Product.Modules;
using S = Cnaws.Statistic.Modules;
using Cnaws.Product;
using Cnaws.Web;
using System;

namespace XcpNet.Api.Controllers
{
    public sealed class LedProduct : CommProduct
    {
        protected override void OnInitController()
        {
        }

        public new void GetByCategory()
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
                    SetResult(Pd.Product.GetPageByApi(DataSource, id, cates.Count, page, size, 8));
                }
                else
                {
                    SetResult(false);
                }
            }
        }

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
                    SetResult(Pd.Product.GetBrandPageByApi(DataSource, id, cates.Count, isbrand, page, size, 8));
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
            CheckMarkHelper(ClassName, "GetBrandByCategory", "购物机根据分类获取产品")
                 .AddArgument("Id", typeof(int), "分类Id")
                 .AddArgument("isbrand", typeof(int), "是否品牌")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddResult(true, typeof(SplitPageData<DataJoin<Pd.Product, S.StatisticData>>), "返回结果");
        }
#endif

        public void GetNewBrandByCategory()
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
                    SetResult(Pd.Product.GetNewBrandPageByApi(DataSource, id, cates.Count, isbrand, page, size, 8));
                }
                else
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetNewBrandByCategoryHelper()
        {
            CheckMarkHelper(ClassName, "GetNewBrandByCategory", "最新根据分类获取产品")
                 .AddArgument("Id", typeof(int), "分类Id")
                 .AddArgument("isbrand", typeof(int), "是否品牌")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddResult(true, typeof(SplitPageData<DataJoin<Pd.Product, S.StatisticData>>), "返回结果");
        }
#endif
    }
}
