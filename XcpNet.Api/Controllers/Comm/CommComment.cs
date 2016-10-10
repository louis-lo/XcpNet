using System;
using Cnaws.Data;
using C = Cnaws.Comment.Modules;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using Cnaws.Web;
using System.Web;
using System.Collections.Generic;
using XcpNet.Api.Modules;
using Cnaws.Data.Query;

namespace XcpNet.Api.Controllers
{
    public class CommComment : CommonControllers
    {
        public static string ClassName = "[type]Comment";
        protected override void OnInitController()
        {
            NotFound();
        }
        /// <summary>
        /// 获取产品评论的列表
        /// </summary>
        public void GetList()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int type = 1, state = 0, star1 = 0, star2 = 5, size = 10;
                long id, page = 1;
                long.TryParse(Request["Id"], out id);
                if (!int.TryParse(Request["type"], out type) || type < 1)
                    type = 1;
                int.TryParse(Request["state"], out state);
                int.TryParse(Request["star1"], out star1);
                int.TryParse(Request["star2"], out star2);
                if (!long.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                SplitPageData<C.Comment> data;
                switch (state)
                {
                    case 1:
                        data = C.Comment.GetPageByTypeAndIdAndStar(DataSource, type, id, star1, star2, Math.Max(page, 1), size, 8);
                        break;
                    case 2:
                        data = C.Comment.GetPageByTypeAndIdAndImage(DataSource, type, id, Math.Max(page, 1), size, 8);
                        break;
                    default:
                        data = C.Comment.GetPageByTypeAndId(DataSource, type, id, Math.Max(page, 1), size, 8);
                        break;
                }
                List<dynamic> list = new List<dynamic>();
                dynamic dd = null;
                if (data.Data.Count > 0)
                {
                    foreach (C.Comment co in data.Data)
                    {

                    }
                }



                SetResult(data);
            }
        }
#if (DEBUG)
        public static void GetListHelper()
        {
            CheckMarkHelper(ClassName, "GetList", "获取产品评论列表")
                 .AddArgument("Id", typeof(int), "产品Id,如果ParentId不为0则为ParentId")
                .AddResult(true, typeof(long), "评论总数");
        }
#endif

        public void GetUnComment()
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
                    long count;
                    IList<P.ProductOrderMapping> list = Db<P.ProductOrderMapping>.Query(DataSource)
                            .Select(new DbSelect<P.ProductOrderMapping>("*"), new DbSelect<P.ProductOrder>("ReceiptDate"))
                            .InnerJoin(new DbColumn<P.ProductOrderMapping>("OrderId"), new DbColumn<P.ProductOrder>("Id"))
                            .Where(new DbWhere<P.ProductOrder>("UserId", member.Id) & new DbWhere<P.ProductOrderMapping>("Evaluation", false) & new DbWhere<P.ProductOrder>("State", P.OrderState.Finished))
                            .OrderBy(new DbOrderBy<P.ProductOrder>("ReceiptDate",DbOrderByType.Desc))
                            .ToList<P.ProductOrderMapping>(size, page, out count);
                    SetResult(new SplitPageData<P.ProductOrderMapping>(page, size, list, count));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }

        }
#if (DEBUG)
        public static void GetUnCommentHelper()
        {
            CheckMemberHelper(ClassName, "GetUnComment", "获取未评论列表")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(SplitPageData<P.ProductOrderMapping>), "返回结果");
        }
#endif

        private const int ProductCommentType = 1;

        public void SetComment()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                DataSource.Begin();
                try
                {
                    C.Comment comment = DbTable.Load<C.Comment>(Request.Form);
                    comment.TargetType = ProductCommentType;
                    comment.UserId = member.Id;
                    comment.CreationDate = DateTime.Now;
                    comment.Ip = ClientIp;

                    P.Product p = P.Product.GetById(DataSource, comment.TargetId);
                    P.ProductOrder productorder = P.ProductOrder.GetById(DataSource, comment.TargetData);
                    if (productorder.State != P.OrderState.Finished)
                        throw new Exception();

                    if (p.ParentId != 0)
                        comment.TargetId = p.ParentId;
                    if (comment.Insert(DataSource) != DataStatus.Success)
                    {
                        SetResult(ApiUtility.INSERT_FAIL);
                        throw new AggregateException();
                    }

                    string image = Request["CommentImgs"];
                    if (!string.IsNullOrEmpty(image))
                    {
                        string[] Images = System.Web.HttpUtility.UrlDecode(image).Split('|');
                        foreach (string img in Images)
                        {
                            var cImg = new C.CommentImage()
                            {
                                Id = comment.Id,
                                Image = img
                            };

                            if (cImg.Insert(DataSource) != DataStatus.Success)
                            {
                                SetResult(ApiUtility.INSERT_FAIL);
                                throw new AggregateException();
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(Request.Form["KeyWords"]))
                    {
                        string[] KeyWords = Request.Form["KeyWords"].Split('|');
                        foreach (string keyword in KeyWords)
                        {
                            if ((new C.CommentKeyword() { Id = comment.Id, Keyword = keyword }).Insert(DataSource) != DataStatus.Success)
                            {
                                SetResult(ApiUtility.INSERT_FAIL);
                                throw new AggregateException();
                            }
                        }
                    }

                    if (Db<P.ProductOrderMapping>.Query(DataSource)
                        .Update()
                        .Set("Evaluation", true)
                        .Where(new DbWhere("OrderId", comment.TargetData) & new DbWhere("ProductId", p.Id))
                        .Execute() != 1)
                    {
                        SetResult(ApiUtility.UPDATE_FAIL);
                        throw new AggregateException();
                    }

                    DataSource.Commit();
                    SetResult(true);
                }
                catch (AggregateException)
                {
                    DataSource.Rollback();
                    return;
                }
                catch (Exception)
                {
                    DataSource.Rollback();
                    SetResult(false);
                }
            }
        }

#if (DEBUG)
        public static void SetCommentHelper()
        {
            CheckMemberHelper(ClassName, "SetComment", "设置产品的评论")
                .AddArgument("Star", typeof(int), "星级")
                .AddArgument("Content", typeof(int), "内容")
                .AddArgument("CommentImgs", typeof(int), "评论图片多张用|隔开")
                .AddArgument("KeyWords", typeof(int), "评论关键词，多个用|隔开")
                .AddArgument("TargetId", typeof(int), "产品编号")
                .AddArgument("TargetData", typeof(int), "订单号")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(ApiUtility.UPDATE_FAIL, "修改数据失败")
                .AddResult(true, typeof(DataSource), "返回结果");
        }
#endif
    }
}
