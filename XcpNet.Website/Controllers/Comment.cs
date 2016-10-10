using System;
using Cnaws.Web;
using Cnaws.Data;
using M = XcpNet.Website.Modules;
using Cnaws.Web.Templates;

namespace XcpNet.Website.Controllers.Extension
{
    public sealed class Comment : Cnaws.Comment.Controllers.Comment
    {
        [HttpGet]
        public override void List(int type, long id, int state = 0, int star1 = 0, int star2 = 0, long page = 1)
        {
            SplitPageData<M.CommentInfo> data;
            switch (state)
            {
                case 1:
                    data = M.CommentInfo.GetPageByStar(DataSource, type, id, star1, star2, Math.Max(page, 1), 20, 8);
                    break;
                case 2:
                    data = M.CommentInfo.GetPageByImage(DataSource, type, id, Math.Max(page, 1), 20, 8);
                    break;
                default:
                    data = M.CommentInfo.GetPage(DataSource, type, id, Math.Max(page, 1), 20, 8);
                    break;
            }
            this["CommentList"] = data;
            this["GetPageUrl"] = new FuncHandler((args) =>
            {
                return GetUrl("/comment/list/", type.ToString(), "/", id.ToString(), "/", state.ToString(), "/", star1.ToString(), "/", star2.ToString(), "/", Convert.ToInt64(args[0]).ToString());
            });
            this["State"] = state;
            Render("comment.html");
        }

        [HttpGet]
        [HttpAjax]
        public void Statistics(int type, long id)
        {
            long star1 = M.CommentInfo.GetCountByTypeAndIdAndStar(DataSource, type, id, 0, 2);
            long star2 = M.CommentInfo.GetCountByTypeAndIdAndStar(DataSource, type, id, 2, 4);
            long star3 = M.CommentInfo.GetCountByTypeAndIdAndStar(DataSource, type, id, 4, 6);
            long count = star1 + star2 + star3;
            if (count == 0) count = 1L;
            SetResult(new
            {
                star1 = star1,
                star2 = star2,
                star3 = star3,
                star4 = M.CommentInfo.GetCountByTypeAndIdAndImage(DataSource, type, id),
                rate1 = (int)((double)star1 / (double)count * 100.0),
                rate2 = (int)((double)star2 / (double)count * 100.0),
                rate3 = (int)((double)star3 / (double)count * 100.0)
            });
        }
    }
}
