using System;
using Cnaws.Web;
using Cnaws.Data;
using XcpNet.Website.Modules;
using System.Web;

namespace XcpNet.Website.Controllers.Extension
{
    /// <summary>
    /// 父亲节活动控制器
    /// </summary>
    public class FathersDay : DataController
    {
        /// <summary>
        /// 活动首页
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        public void Index(int pageIndex = 1, int pageSize = 10)
        {
            if (!IsAjax)
                this["NewList"] = FDRegister.GetRegisterList(DataSource, 1, 6, 6, "CreationDate");

            this["RegisterList"] = FDRegister.GetRegisterList(DataSource, pageIndex, pageSize);

            Render("fd_index.html");
        }
        /// <summary>
        /// 活动报名
        /// </summary>
        [Authorize(true)]
        public void VoteRegister()
        {
            if (Request.RequestType.Equals("GET"))
            {
                this["MaxNo"] = FDRegister.GenerateMaxNo(DataSource);
                this["HasRegister"] = FDRegister.HasRegisted(DataSource, User.Identity.Id);
                this["Token"] = HttpUtility.UrlEncode(User.Identity.GetToken());
                Render("fd_register.html");
            }
            else
            {
                FDRegister register = DbTable.Load<FDRegister>(Request.Form);
                register.CreationDate = DateTime.Now;
                register.UserId = User.Identity.Id;

                DataStatus result = register.Insert(DataSource);
                if (result == DataStatus.Success)
                    Index();
            }
        }
        /// <summary>
        /// 投票
        /// </summary>
        [Authorize(true)]
        public void Vote()
        {
            long id = 0L;
            long.TryParse(Request["id"], out id);
            if (id <= 0L) throw new Exception("参数无效");

            FDVote vote = FDVote.GetNewsVote(DataSource, User.Identity.Id);
            int state = 0;
            //首次投票
            if (vote == null)
            {
                vote = new FDVote
                {
                    FromUserId = User.Identity.Id,
                    IsGetCoupons = false,
                    ToId = id,
                    VoteCount = 1,
                    CreationDate = DateTime.Now
                };
                vote.Insert(DataSource);
                FDRegister.UpdateVoteCount(DataSource, id, 1);
                state = 1;
            }
            //当天没有投票过
            else if (vote.CreationDate.Day < DateTime.Now.Day)
            {
                vote.VoteCount += 1;
                vote.IsGetCoupons = true;
                vote.Update(DataSource);
                FDRegister.UpdateVoteCount(DataSource, id, 1);
                state = 2;
            }
            //当天己投票
            else
            {
                state = 3;
            }
            this["Register"] = FDRegister.GetById(DataSource, id);
            this["State"] = state;
            Render("fd_vote.html");
        }

        public void UpLoadImg()
        {
            Render("temp/uploadimg.html");
        }

        [HttpPost]
        public void Search()
        {
            string query = Request.Form["id"];
            int id = 0;
            int.TryParse(query, out id);
            this["SearchList"] = FDRegister.Search(DataSource, id);
            Render("fd_search.html");
        }

    }
}
