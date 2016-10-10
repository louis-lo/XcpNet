using System;
using System.Collections.Generic;
using System.Linq;
using Cnaws.Data;
using C = Cnaws.Comment.Modules;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using Cnaws.Web;
using Cnaws.Data.Query;
using XcpNet.Common;
using Cnaws.Product;

namespace XcpNet.ApiSecond.Controllers
{
    public class Comment2 : CommControllers2
    {
        public static string ClassName = "[type]Comment2";
        protected override void OnInitController()
        {
            NotFound();
        }

        public void GetList()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int type = 1, state = 0, star1 = 0, star2 = 5, size = 10;
                long id, page = 1;
                long.TryParse(Request["id"], out id);
                if (!int.TryParse(Request["type"], out type) || type < 1)
                    type = 1;
                int.TryParse(Request["state"], out state);
                string star = Request["star"];
                if (state == 1)
                {
                    if (string.IsNullOrEmpty(star) && star.IndexOf("_") == -1)
                    {
                        SetResult(CommUtility.PARAMETER_ERROR);
                        return;
                    }
                    else
                    {
                        string[] stars = star.Split('_');
                        int.TryParse(stars[0], out star1);
                        int.TryParse(stars[1], out star2);
                    }
                }
                if (!long.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;

                SplitPageData<C.Comment> data = C.Comment.ApiGetPagebyId(DataSource, type, id, state, star1, star2, Math.Max(page, 1), size, 8);
                long[] commentIds = data.Data.Select(x => x.Id).ToArray();
                long[] userIds = data.Data.Select(x => x.UserId).ToArray();
                IList<C.CommentImage> Imgdata = C.CommentImage.GetAllByIds(DataSource,commentIds);
                IList<C.CommentKeyword> KeyWorddata = C.CommentKeyword.GetAllByIds(DataSource, commentIds);
                IList<M.MemberInfo> Memberdata = M.MemberInfo.GetByIds(DataSource, userIds);
                List<dynamic> list = new List<dynamic>();
                if (data.Data.Count > 0)
                {
                    foreach (C.Comment co in data.Data)
                    {
                        list.Add(new { Comment = co, UserInfo = Memberdata.Where(x => x.Id == co.UserId).ToList(), Images = Imgdata.Where(x => x.Id == co.Id).ToList(), KeyWords = KeyWorddata.Where(x => x.Id == co.Id).ToList() });
                    }
                }
                SetResult(list);
            }
        }
#if (DEBUG)
        public static void GetListHelper()
        {
            CheckMarkApi(ClassName, "GetList", "获取产品评论列表")
                .AddArgument("id", typeof(int), "产品Id,如果ParentId不为0则为ParentId")
                .AddArgument("type", typeof(int), "评论类型,1为产品评论(默认为1)")
                .AddArgument("state", typeof(int), "查询类型,0所查询所有,1为根据评分区间查询,2为查询有图片的(默认为0)")
                .AddArgument("star", typeof(string), "当state为1时,star为评分区间用_隔开(必填,默认0_5)")
                .AddResult(true, typeof(string), "评论列表,Comment:评论,UserInfo:用户信息,Images:评论图片,KeyWords:评论标签");
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
                            .OrderBy(new DbOrderBy<P.ProductOrder>("ReceiptDate", DbOrderByType.Desc))
                            .ToList<P.ProductOrderMapping>(size, page, out count);
                    List<dynamic> result = new List<dynamic>();                  
                    foreach(P.ProductOrder item in P.ProductOrder.GetByIds(DataSource, list.Select(p => p.OrderId).ToArray()))
                    {
                        IList<P.ProductOrderMapping> newlist = list.Where(p => p.OrderId == item.Id).ToList();
                        List<OrderMappingCacheInfo> mappinglist = new List<OrderMappingCacheInfo>(newlist.Count);
                        foreach (P.ProductOrderMapping mapping in newlist)
                        {
                            mappinglist.Add(new OrderMappingCacheInfo(mapping));
                        }
                        result.Add(new
                        {
                            Order = item,
                            Products = mappinglist,
                        });
                    }
                    SetResult(new SplitPageData<dynamic>(page, size, result, count));
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
            CheckMemberApi(ClassName, "GetUnComment", "获取未评论订单列表")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(SplitPageData<P.ProductOrder>), "返回订单结果");
        }
#endif

        private const int ProductCommentType = 1;

        public void SetComment()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                object code, data;
                C.Comment comment = DbTable.Load<C.Comment>(Request.Form);
                code = CommComment.SetComment(DataSource, member, comment, Request["CommentImgs"], Request["KeyWords"], ClientIp, out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }

#if (DEBUG)
        public static void SetCommentHelper()
        {
            CheckMemberApi(ClassName, "SetComment", "设置产品的评论")
                .AddArgument("Star", typeof(int), "星级")
                .AddArgument("Content", typeof(int), "内容")
                .AddArgument("CommentImgs", typeof(int), "评论图片多张用|隔开")
                .AddArgument("KeyWords", typeof(int), "评论关键词，多个用|隔开")
                .AddArgument("TargetId", typeof(int), "产品编号")
                .AddArgument("TargetData", typeof(int), "订单号")
                .AddResult(CommUtility.PRODUCT_ERROR, "该产品找不到或已经评论过了")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(CommUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(CommUtility.UPDATE_FAIL, "修改数据失败")
                .AddResult(true, typeof(DataSource), "返回结果");
        }
#endif

        public void GetCommentByOrderAndProduct()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                C.Comment comment = C.Comment.GetByTargetIdAndTargetDate(DataSource, long.Parse(Request["TargetId"]), Request["TargetData"]);
                IList<C.CommentImage> commentImage = C.CommentImage.GetAllById(DataSource, comment.Id);
                IList<C.CommentKeyword> commentKeyWord = C.CommentKeyword.GetAllById(DataSource, comment.Id);
                M.MemberInfo MemberInfo = M.MemberInfo.GetById(DataSource, comment.UserId, ColumnMode.Exclude, "Name","NickName","Image","Sex", "Mobile");
                SetResult(new { Comment = comment, UserInfo = MemberInfo, Images = commentImage, KeyWords = commentKeyWord });
            }
        }
#if (DEBUG)
        public static void GetCommentByOrderAndProductHelper()
        {
            CheckMemberApi(ClassName, "GetCommentByOrderAndProduct", "获取对应订单产品的评论")
                .AddArgument("TargetId", typeof(int), "产品编号")
                .AddArgument("TargetData", typeof(int), "订单号")
                .AddResult(true, typeof(DataSource), "返回结果");
        }
#endif
    }
}
