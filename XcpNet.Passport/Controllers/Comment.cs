using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Comment.Modules;
using P = Cnaws.Product.Modules;
using U = Cnaws.Passport.Modules;
using Cnaws.Web.Templates;
using Cnaws.Data.Query;
using System.Collections.Generic;

namespace XcpNet.Passport.Controllers.Extension
{
    public sealed class Comment : Common.CommPassportController
    {
        private const int ProductCommentType = 1;

        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void Submit()
        {
            DataSource.Begin();
            try
            {
                M.Comment comment = DbTable.Load<M.Comment>(Request.Form);
                comment.TargetType = ProductCommentType;
                comment.UserId = User.Identity.Id;
                comment.CreationDate = DateTime.Now;
                comment.Ip = ClientIp;
                if (string.IsNullOrEmpty(comment.Content))
                    comment.Content = "没有填写评论！";
                P.Product p = P.Product.GetById(DataSource, comment.TargetId);
                P.ProductOrder productorder = P.ProductOrder.GetById(DataSource, comment.TargetData);
                if (productorder.State != P.OrderState.Finished)
                    throw new Exception("订单未完成收货！");

                if (p.ParentId != 0)
                    comment.TargetId = p.ParentId;
                if (comment.Insert(DataSource) != DataStatus.Success)
                    throw new Exception();

                string image = Request["CommentImgs"];
                if (!string.IsNullOrEmpty(image))
                {
                    string[] Images = System.Web.HttpUtility.UrlDecode(image).Split('|');
                    if (Images.Length > 0)
                    {
                        foreach (string img in Images)
                        {
                            var cImg = new M.CommentImage()
                            {
                                Id = comment.Id,
                                Image = img
                            };

                            if (cImg.Insert(DataSource) != DataStatus.Success)
                            {
                                throw new Exception("图片保存失败!");
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(Request.Form["KeyWords"]))
                {
                    string[] KeyWords = Request.Form["KeyWords"].Split('|');
                    if (KeyWords.Length > 0)
                    {
                        foreach (string keyword in KeyWords)
                        {
                            if ((new M.CommentKeyword() { Id = comment.Id, Keyword = keyword }).Insert(DataSource) != DataStatus.Success)
                            {
                                throw new Exception("关键词保存失败！");
                            }
                        }
                    }
                }

                if (Db<P.ProductOrderMapping>.Query(DataSource)
                    .Update()
                    .Set("Evaluation", true)
                    .Where(new DbWhere("OrderId", comment.TargetData) & new DbWhere("ProductId", p.Id))
                    .Execute() != 1)
                {
                    throw new Exception("修改评论状态失败！");
                }

                DataSource.Commit();
                SetResult(true);
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                SetResult(false, ex.Message);
            }
        }

        [HttpGet]
        [HttpAjax]
        public void Keywords(int type, long id)
        {
            SetResult(true, M.Comment.GetAllKeywords(DataSource, type, id));
        }
        //立即评价      
        [HttpGet]
        public void List(int type, long id, int state = 0, int star1 = 0, int star2 = 0, long page = 1)
        {
            long count = 0;
            page = Math.Max(1, page);
            IList<DataJoin<M.Comment, P.ProductOrderMapping>> list = new List<DataJoin<M.Comment, P.ProductOrderMapping>>();
            switch (state)
            {
                case 0:
                    {
                        list = Db<P.ProductOrderMapping>.Query(DataSource)
                           .Select(new DbSelect<P.ProductOrderMapping>("*"), new DbSelect<P.ProductOrder>("ReceiptDate"), new DbSelect<M.Comment>())
                           .InnerJoin(new DbColumn<P.ProductOrderMapping>("OrderId"), new DbColumn<P.ProductOrder>("Id"))
                           .LeftJoin(new DbColumn<P.ProductOrderMapping>("OrderId"), new DbColumn<M.Comment>("TargetData"))
                           .And(new DbColumn<P.ProductOrderMapping>("ProductId"), new DbColumn<M.Comment>("TargetData"))
                           .Where(new DbWhere<P.ProductOrder>("UserId", User.Identity.Id) & new DbWhere<P.ProductOrderMapping>("Evaluation", false) & new DbWhere<P.ProductOrder>("State", P.OrderState.Finished))
                           .OrderBy(new DbOrderBy<P.ProductOrder>("ReceiptDate", DbOrderByType.Desc))
                           .ToList<DataJoin<M.Comment, P.ProductOrderMapping>>(10, page, out count);
                    }
                    break;
                case 1:
                    {
                        list = Db<M.Comment>.Query(DataSource)
                            .Select(new DbSelect<M.Comment>(), new DbSelect<P.ProductOrderMapping>("*"))
                            .InnerJoin(new DbColumn<M.Comment>("TargetId"), new DbColumn<P.ProductOrderMapping>("ProductId"))
                            .Where(new DbWhere<M.Comment>("TargetType", ProductCommentType) & new DbWhere<M.Comment>("UserId", User.Identity.Id) & new DbWhere<P.ProductOrderMapping>("Evaluation", true))
                            .OrderBy(new DbOrderBy<M.Comment>("CreationDate", DbOrderByType.Desc))
                            .ToList<DataJoin<M.Comment, P.ProductOrderMapping>>(10, page, out count);
                    }
                    break;
            }
            this["State"] = state;
            this["CommentList"] = new SplitPageData<DataJoin<M.Comment, P.ProductOrderMapping>>(page, 10, list, count, 8);

            this["SplitImage"] = new FuncHandler((args) =>
            {
                if (args != null && args.Length > 0)
                {
                    return Convert.ToString(args[0]).Split('|')[0];
                }
                return string.Empty;
            });
            this["GetPageUrl"] = new FuncHandler((args) =>
            {
                return GetUrl("/comment/list/", type.ToString(), "/", id.ToString(), "/", state.ToString(), "/", star1.ToString(), "/", star2.ToString(), "/", Convert.ToInt64(args[0]).ToString());
            });
            Render("comment.html");
        }
        ////立即评价2.0      
        //[HttpGet]
        //public void ListNew(int state = 0, long orderId = 0)
        //{
        //    long count = 0;
        //    IList<DataJoin<M.Comment, P.ProductOrderMapping>> list = new List<DataJoin<M.Comment, P.ProductOrderMapping>>();
        //    switch (state)
        //    {
        //        case 0:
        //            {
        //                list = Db<P.ProductOrderMapping>.Query(DataSource)
        //                   .Select(new DbSelect<P.ProductOrderMapping>("*"), new DbSelect<P.ProductOrder>("ReceiptDate"), new DbSelect<M.Comment>())
        //                   .InnerJoin(new DbColumn<P.ProductOrderMapping>("OrderId"), new DbColumn<P.ProductOrder>("Id"))
        //                   .LeftJoin(new DbColumn<P.ProductOrderMapping>("OrderId"), new DbColumn<M.Comment>("TargetData"))
        //                   .And(new DbColumn<P.ProductOrderMapping>("ProductId"), new DbColumn<M.Comment>("TargetData"))
        //                   .Where(new DbWhere<P.ProductOrder>("UserId", User.Identity.Id) & new DbWhere<P.ProductOrder>("Id", orderId) & new DbWhere<P.ProductOrderMapping>("Evaluation", false) & new DbWhere<P.ProductOrder>("State", P.OrderState.Finished))
        //                   .OrderBy(new DbOrderBy<P.ProductOrder>("ReceiptDate", DbOrderByType.Desc))
        //                   .ToList<DataJoin<M.Comment, P.ProductOrderMapping>>();
        //            }
        //            break;
        //        case 1:
        //            {
        //                list = Db<M.Comment>.Query(DataSource)
        //                    .Select(new DbSelect<M.Comment>(), new DbSelect<P.ProductOrderMapping>("*"))
        //                    .InnerJoin(new DbColumn<M.Comment>("TargetId"), new DbColumn<P.ProductOrderMapping>("ProductId"))
        //                    .Where(new DbWhere<M.Comment>("TargetType", ProductCommentType) & new DbWhere<M.Comment>("UserId", User.Identity.Id) & new DbWhere<P.ProductOrderMapping>("Evaluation", true) & new DbWhere<P.ProductOrderMapping>("OrderId", orderId))
        //                    .OrderBy(new DbOrderBy<M.Comment>("CreationDate", DbOrderByType.Desc))
        //                    .ToList<DataJoin<M.Comment, P.ProductOrderMapping>>();
        //            }
        //            break;
        //    }
        //    this["State"] = state;
        //    this["CommentList"] = list;

        //    this["SplitImage"] = new FuncHandler((args) =>
        //    {
        //        if (args != null && args.Length > 0)
        //        {
        //            return Convert.ToString(args[0]).Split('|')[0];
        //        }
        //        return string.Empty;
        //    });
        //    Render("comment.html");
        //}
        [HttpGet]
        [HttpAjax]
        public void Count(int type, long id, int state, int star1, int star2)
        {
            try
            {
                long count = 0;
                switch (state)
                {
                    case 0:
                        {
                            count = Db<P.ProductOrderMapping>.Query(DataSource)
                           .Select(new DbSelect<P.ProductOrderMapping>("*"), new DbSelect<P.ProductOrder>("ReceiptDate"), new DbSelect<M.Comment>())
                           .InnerJoin(new DbColumn<P.ProductOrderMapping>("OrderId"), new DbColumn<P.ProductOrder>("Id"))
                           .LeftJoin(new DbColumn<P.ProductOrderMapping>("OrderId"), new DbColumn<M.Comment>("TargetData"))
                           .And(new DbColumn<P.ProductOrderMapping>("ProductId"), new DbColumn<M.Comment>("TargetData"))
                           .Where(new DbWhere<P.ProductOrder>("UserId", User.Identity.Id) & new DbWhere<P.ProductOrderMapping>("Evaluation", false) & new DbWhere<P.ProductOrder>("State", P.OrderState.Finished))
                           .Count();
                        }
                        break;
                    case 1:
                        {
                            count = Db<M.Comment>.Query(DataSource)
                                .Select(new DbSelect<M.Comment>("*"), new DbSelect<P.ProductOrderMapping>("*"))
                                .InnerJoin(new DbColumn<M.Comment>("TargetId"), new DbColumn<P.ProductOrderMapping>("ProductId"))
                                .Where(new DbWhere<M.Comment>("TargetType", ProductCommentType) & new DbWhere<M.Comment>("UserId", User.Identity.Id) & new DbWhere<P.ProductOrderMapping>("Evaluation", true))
                                .Count();
                        }
                        break;
                }
                SetResult(true, count);
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">产品ID</param>
        /// <param name="orderid">订单ID</param>
        /// <param name="state">0未评价1已评价</param>
        public void Set(long id, string orderid,int state= 0)
        {
            
            if (IsWap)
            {
                P.ProductOrderMapping pom = P.ProductOrderMapping.GetById(DataSource, orderid, id);
                if (pom.Evaluation)
                {
                    NotFound();
                    return;
                }
                U.MemberInfo member = U.MemberInfo.GetBySecurity(DataSource, User.Identity.Id);
                this["Member"] = member;
                this["Product"] = pom;
                this["SplitImage"] = new FuncHandler((args) =>
                {
                    if (args != null && args.Length > 0)
                    {
                        return Convert.ToString(args[0]).Split('|')[0];
                    }
                    return string.Empty;
                });
                Render("setcomment.html");
            }
            else
            {
                if (state == 0)
                {
                    P.ProductOrderMapping pom = P.ProductOrderMapping.GetById(DataSource, orderid, id);

                    this["Product"] = pom;
                    this["SplitImage"] = new FuncHandler((args) =>
                    {
                        if (args != null && args.Length > 0)
                        {
                            return Convert.ToString(args[0]).Split('|')[0];
                        }
                        return string.Empty;
                    });
                    this["State"] = state;
                    Render("comment.html");
                }
                else if(state==1)
                {
                    P.ProductOrderMapping pom = P.ProductOrderMapping.GetById(DataSource, orderid, id);
                    M.Comment comment = M.Comment.GetByTargetIdAndTargetDate(DataSource, id, orderid);
                    IList<M.CommentImage> commentiage;
                    if (comment != null)
                    {
                        commentiage = M.CommentImage.GetAllById(DataSource, comment.Id);
                    }
                    else {
                        commentiage =null;
                    }
                    //IList<M.CommentImage> commentiage = M.CommentImage.GetAllById(DataSource, comment.Id);
                    this["Product"] = pom;
                    this["Comment"] = comment;
                    this["CommentImage"] = commentiage;
                    this["SplitImage"] = new FuncHandler((args) =>
                    {
                        if (args != null && args.Length > 0)
                        {
                            return Convert.ToString(args[0]).Split('|')[0];
                        }
                        return string.Empty;
                    });
                    this["State"] = state;
                    Render("comment.html");
                }
            }
        }

        /// <summary>
        /// 查看评价
        /// </summary>
        /// <param name="id">产品ID</param>
        /// <param name="orderid">订单ID</param>
        public void ViewComment(long id,string orderid)
        {
            U.MemberInfo member = U.MemberInfo.GetBySecurity(DataSource, User.Identity.Id);
            M.Comment comment = Db<M.Comment>.Query(DataSource)
                .Select()
                .Where(new DbWhere<M.Comment>("TargetId", id) & new DbWhere<M.Comment>("TargetData", orderid))
                .First<M.Comment>();
            this["Comment"] = comment;
            this["Member"] = member;
            Render("viewcomment.html");
        }
    }
}