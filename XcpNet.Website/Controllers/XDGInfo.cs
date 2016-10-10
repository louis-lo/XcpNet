using Cnaws.Comment.Modules;
using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws.Passport.Modules;
using Cnaws.Product;
using Cnaws.Product.Modules;
using Cnaws.Web;
using Cnaws.Web.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDG = XcpNet.Supplier.Modules.Modules;

namespace XcpNet.Website.Controllers
{
    public class XdgInfo : DataController
    {
        #region 乡道馆首页
        public void XdgHomePage(int pageIndex = 1)
        {
            this["XDGInfoList"] = XDG.XDGInfo.GetXDGInfoList(DataSource, pageIndex, 6);
            this["PageUrl"] = new FuncHandler((object[] ps) =>
            {
                return GetUrl("/xdginfo/xdghomepage/", ps[0].ToString());
            });
            this["UserId"] = "";
            Render("XDGHomePage.html");
        }
        #endregion

        #region 个人乡道馆首页
        public void XdgDetailPage(int userId)
        {
            XDG.XDGInfo XDGInfo = XDG.XDGInfo.GetXDGInfoByUserId(DataSource, userId);
            this["XDGInfo"] = XDGInfo;
            if (IsWap)
            {
                this["ReMenProduct"] = XDGInfo.GetProduct(DataSource, 2);
                this["TuanGouProduct"] = Product.GetProduct(DataSource, userId, 3, DateTime.Now);
            }
            else
            {
                this["ReMenProduct"] = XDGInfo.GetProduct(DataSource, 6);
                this["TuanGouProduct"] = Product.GetProduct(DataSource, userId, 8, DateTime.Now);
                this["Ad"] = XcpNet.Ad.Modules.AdMember.GetAdMember(DataSource, User.Identity.Id, 2, 1);
            }
            this["XDGCategoryList"] = StoreCategory.GetXDGCategoryOne(DataSource, userId);
            this["UserId"] = userId;
            Render("XDGDetailPage.html");
        }
        #endregion

        #region 乡道馆产品列表
        public void XdgProductList(int categoryId, int pageIndex = 1)
        {
            StoreCategory XDGCategory = StoreCategory.GetXDGCategoryByCategoryId(DataSource, categoryId);
            this["XDGCategory"] = XDGCategory;
            this["XDGCategoryList"] = StoreCategory.GetXDGCategoryOne(DataSource, XDGCategory.UserId);
            
            this["ProductList"] = Product.GetProductListByXDGCategoryId(DataSource, categoryId, StoreCategory.GetAllParentsById(DataSource, categoryId).Count, pageIndex, 18);
            this["PageUrl"] = new FuncHandler((object[] ps) =>
            {
                return GetUrl("/xdginfo/xdgproductlist/", categoryId.ToString(), "/", ps[0].ToString());
            });
            XDG.XDGInfo XDGInfo = XDG.XDGInfo.GetXDGInfoByUserId(DataSource, XDGCategory.UserId);
            this["XDGInfo"] = XDGInfo;
            this["UserId"] = XDGCategory.UserId;
            Render("XDGProductList.html");
        }
        #endregion

        #region 乡道馆产品
        public void info(long productId)
        {
            Product product = Product.GetSaleProduct(DataSource, productId);
            if (product != null)
            {
                //产品信息
                this["Product"] = product;
                //产品参数
                this["Attributes"] = ProductAttribute.GetAllValuesByProduct(DataSource, productId);
                //产品规格
                long parent = product.ParentId > 0 ? product.ParentId : product.Id;
                this["Series"] = ProductSerie.GetAll(DataSource, parent);
                this["Mapping"] = ProductMapping.GetAllByProduct(DataSource, product.Id);
                this["Mappings"] = ProductMapping.GetAllByAllProduct(DataSource, parent);

                if (!IsWap)
                {
                    this["UserId"] = product.SupplierId;
                    long allCommentCount = GetCommentCount(productId, 0);
                    long commentImgCount = GetCommentImgCount(productId);
                    long goodCommentCount = GetCommentCount(productId, 2);
                    long mediumCommentCount = GetCommentCount(productId, 3);
                    long differenceCommentCount = GetCommentCount(productId, 4);
                    this["allCommentCount"] = allCommentCount;
                    this["commentImgCount"] = commentImgCount;
                    this["goodCommentCount"] = goodCommentCount;
                    this["mediumCommentCount"] = mediumCommentCount;
                    this["differenceCommentCount"] = differenceCommentCount;
                    if (allCommentCount == 0)
                    {
                        this["goodCommentProportion"] = 100;
                        this["mediumCommentProportion"] = 0;
                        this["differenceCommentProportion"] = 0;
                    }
                    else
                    {
                        this["goodCommentProportion"] = Math.Round((double)goodCommentCount / allCommentCount, 2) * 100;
                        this["mediumCommentProportion"] = Math.Round((double)mediumCommentCount / allCommentCount, 2) * 100;
                        this["differenceCommentProportion"] = Math.Round((double)differenceCommentCount / allCommentCount, 2) * 100;
                    }
                    this["TuanGouProduct"] = Product.GetProduct(DataSource, User.Identity.Id, 8, DateTime.Now);
                    this["ReMenProduct"] = Product.GetProduct(DataSource, product.SupplierId, 6);
                }
                else
                {
                    this["Comment"] = Db<Comment>.Query(DataSource).
                        Select(new DbSelect<Comment>("*"), new DbSelect<Comment>("CreationDate"), new DbSelect<MemberInfo>("NickName"), new DbSelect<MemberInfo>("Image")).
                        LeftJoin(new DbColumn<Comment>("UserId"), new DbColumn<MemberInfo>("Id")).
                        Where(new DbWhereQueue("TargetId", productId) & (new DbWhereQueue("Star", 5) | new DbWhereQueue("Star", 4))).
                        OrderBy(new DbOrderBy<Comment>("CreationDate", DbOrderByType.Desc)).First<DataJoin<Comment, MemberInfo>>();
                    this["allCommentCount"] = GetCommentCount(productId, 0);
                }
                Render("XDGProduct.html");
            }
            else
            {
                NotFound();
            }
        }
        #endregion

        #region WAP端评论页面
        public void ShowComment(long productId)
        {
            long allCommentCount = GetCommentCount(productId, 0);
            long commentImgCount = GetCommentImgCount(productId);
            long goodCommentCount = GetCommentCount(productId, 2);
            long mediumCommentCount = GetCommentCount(productId, 3);
            long differenceCommentCount = GetCommentCount(productId, 4);
            this["allCommentCount"] = allCommentCount;
            this["commentImgCount"] = commentImgCount;
            this["goodCommentCount"] = goodCommentCount;
            this["mediumCommentCount"] = mediumCommentCount;
            this["differenceCommentCount"] = differenceCommentCount;
            this["ProductId"] = productId;
            Render("XDGComment.html");
        } 
        #endregion

        #region 评论
        public void comment(long productId, int commentType, int pageIndex = 1)
        {
            if (commentType == 1)
            {
                if(IsWap)
                {
                    DbWhereQueue where = new DbWhereQueue("TargetId", productId);
                    if (commentType == 2)
                    {
                        where &= (new DbWhereQueue("Star", 5) | new DbWhereQueue("Star", 4));
                    }
                    else if (commentType == 3)
                    {
                        where &= (new DbWhereQueue("Star", 3) | new DbWhereQueue("Star", 2));
                    }
                    else if (commentType == 4)
                    {
                        where &= new DbWhereQueue("Star", 1);
                    }
                    long count;
                    IList<DataJoin<Comment, MemberInfo>> data = Db<Comment>.Query(DataSource).
                        Select(new DbSelect<Comment>("*"), new DbSelect<Comment>("CreationDate"), new DbSelect<MemberInfo>("NickName"), new DbSelect<MemberInfo>("Image")).
                        LeftJoin(new DbColumn<Comment>("UserId"), new DbColumn<MemberInfo>("Id")).
                        InnerJoin(new DbColumn<Comment>("Id"), new DbColumn<CommentImage>("Id")).
                        Where(where).OrderBy(new DbOrderBy<Comment>("CreationDate", DbOrderByType.Desc)).
                        ToList<DataJoin<Comment, MemberInfo>>(20, pageIndex, out count);
                    this["Comment"] = new SplitPageData<DataJoin<Comment, MemberInfo>>(pageIndex, 20, data, count);
                }
                else
                {
                    this["Comment"] = GetCommentImg(productId, pageIndex);
                }
            }
            else
            {
                this["Comment"] = GetComment(productId, commentType, pageIndex);
            }
            this["GetDateSpan"] = new FuncHandler((object[] param) =>
            {
                return (Convert.ToDateTime(param[0]) - Convert.ToDateTime(param[1])).Days;
            });
            this["GetCommentImg"] = new FuncHandler((object[] param) =>
            {
                return CommentImage.GetAllById(DataSource, Convert.ToInt64(param[0]));
            });
            this["PageUrl"] = new FuncHandler((object[] param) =>
            {
                return "javascript:GetComment(" + productId + ", " + commentType + ", " + param[0].ToString() + ")";
            });
            this["commentType"] = commentType;
            Render("XDGProductComment.html");
        }
        #endregion

        #region 获取商品评论
        /// <summary>
        /// 获取商品评论
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="commentType">0=全部评论、1=晒图、2=好评、3=中评、4=差评</param>
        /// <param name="pageIndex"></param>
        private SplitPageData<dynamic> GetComment(long productId, int commentType, int pageIndex = 1)
        {
            DbWhereQueue where = new DbWhereQueue("TargetId", productId);
            if (commentType == 2)
            {
                where &= (new DbWhereQueue("Star", 5) | new DbWhereQueue("Star", 4));
            }
            else if (commentType == 3)
            {
                where &= (new DbWhereQueue("Star", 3) | new DbWhereQueue("Star", 2));
            }
            else if (commentType == 4)
            {
                where &= new DbWhereQueue("Star", 1);
            }
            long count;
            dynamic data = Db<Comment>.Query(DataSource).
                Select(new DbSelect<Comment>("*"), new DbSelect<Comment>("CreationDate"), new DbSelect<MemberInfo>("NickName"), new DbSelect<MemberInfo>("Image"), new DbSelect<ProductOrder>("ReceiptDate")).
                LeftJoin(new DbColumn<Comment>("UserId"), new DbColumn<MemberInfo>("Id")).
                LeftJoin(new DbColumn<Comment>("TargetData"), new DbColumn<ProductOrder>("Id")).
                Where(where).OrderBy(new DbOrderBy<Comment>("CreationDate", DbOrderByType.Desc)).
                ToList(20, pageIndex, out count);
            return new SplitPageData<dynamic>(pageIndex, 20, data, count);
        }
        #endregion

        #region 获取商品晒图
        /// <summary>
        /// 获取商品晒图
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        private SplitPageData<CommentImage> GetCommentImg(long productId, int pageIndex = 1)
        {
            long count;
            IList<CommentImage> data = Db<Comment>.Query(DataSource).
                Select(new DbSelect<CommentImage>("*"), new DbSelect<Comment>("CreationDate")).
                InnerJoin(new DbColumn<Comment>("Id"), new DbColumn<CommentImage>("Id")).
                Where(new DbWhereQueue("TargetId", productId)).OrderBy(new DbOrderBy<Comment>("CreationDate", DbOrderByType.Desc)).
                ToList<CommentImage>(20, pageIndex, out count);
            return new SplitPageData<CommentImage>(pageIndex, 20, data, count);
        }
        #endregion

        #region 获取商品评论总数
        private long GetCommentCount(long productId, int commentType)
        {
            DbWhereQueue where = new DbWhereQueue("TargetId", productId);
            if (commentType == 2)
            {
                where &= (new DbWhereQueue("Star", 5) | new DbWhereQueue("Star", 4));
            }
            else if (commentType == 3)
            {
                where &= (new DbWhereQueue("Star", 3) | new DbWhereQueue("Star", 2));
            }
            else if (commentType == 4)
            {
                where &= new DbWhereQueue("Star", 1);
            }
            return Db<Comment>.Query(DataSource).
                Select().
                Where(where).
                ToList().Count;
        }
        #endregion

        #region 获取商品晒图总数
        private long GetCommentImgCount(long productId)
        {
            return Db<Comment>.Query(DataSource).
                Select().
                InnerJoin(new DbColumn<Comment>("Id"), new DbColumn<CommentImage>("Id")).
                Where(new DbWhereQueue("TargetId", productId)).
                ToList().Count;
        }
        #endregion

        public void GetFreight(string provice, string city, long productId)
        {
            Response.Write(Product.GetFreight(DataSource, provice, city, productId));
        }
    }
}
