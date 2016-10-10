using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Data;
using Cnaws.Web;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using C = Cnaws.Comment.Modules;
using Cnaws.Data.Query;
namespace XcpNet.Common
{
    public class CommComment
    {

        private const int ProductCommentType = 1;

        /// <summary>
        /// 设置评论
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员登陆信息</param>
        /// <param name="comment">评论表对象</param>
        /// <param name="commentimgs">评论的图片，多个用'|'隔开</param>
        /// <param name="keywords">评论的关键词，多个用'|'隔开</param>
        /// <param name="clientip">登陆Ip</param>
        /// <param name="data">返回信息</param>
        /// <returns>返回代码</returns>
        public static object SetComment(DataSource DataSource, M.Member member, C.Comment comment, string commentimgs, string keywords, string clientip, out object data)
        {
            object code = null; data = null;
            DataSource.Begin();
            try
            {
                comment.TargetType = ProductCommentType;
                comment.UserId = member.Id;
                comment.CreationDate = DateTime.Now;
                comment.Ip = clientip;
                P.Product p = P.Product.GetById(DataSource, comment.TargetId);
                P.ProductOrder productorder = P.ProductOrder.GetById(DataSource, comment.TargetData);
                if (productorder.State != P.OrderState.Finished)
                {
                    code = CommUtility.PRODUCT_ERROR;
                    throw new AggregateException();
                }
                if (p.ParentId != 0)
                    comment.TargetId = p.ParentId;
                if (comment.Insert(DataSource) != DataStatus.Success)
                {
                    code = CommUtility.INSERT_FAIL;
                    throw new AggregateException();
                }
                string image = commentimgs;
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
                            code = CommUtility.INSERT_FAIL;
                            throw new AggregateException();
                        }
                    }
                }
                if (!string.IsNullOrEmpty(keywords))
                {
                    string[] KeyWords = keywords.Split('|');
                    foreach (string keyword in KeyWords)
                    {
                        if ((new C.CommentKeyword() { Id = comment.Id, Keyword = keyword }).Insert(DataSource) != DataStatus.Success)
                        {
                            code = CommUtility.INSERT_FAIL;
                            throw new AggregateException();
                        }
                    }
                }
                if (Db<P.ProductOrderMapping>.Query(DataSource)
                    .Update()
                    .Set("Evaluation", true)
                    .Where(new DbWhere("OrderId", comment.TargetData) & new DbWhere("ProductId", p.Id))
                    .Execute() <= 0)
                {
                    code = CommUtility.UPDATE_FAIL;
                    throw new AggregateException();
                }
                DataSource.Commit();
                return CommUtility.SUCCESS;
            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                return code;
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }
    }
}
