using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws.Passport.Modules;
using Cnaws.Product.Modules;
using Cnaws.Comment.Modules;

namespace XcpNet.Website.Modules
{
    [Serializable]
    [DataTable("Comment")]
    public sealed class CommentInfo : Comment
    {
        public string UserName = null;
        public string UserEmail = null;
        public long UserMobile = 0L;
        public string UserImage = null;
        public string ProductInfo = null;


        public override bool CanInstall
        {
            get { return false; }
        }

        public string GetUserName()
        {
            Guid guid;
            string name = UserName;
            if (Guid.TryParse(UserName, out guid))
            {
                if (UserMobile > 0)
                {
                    name = UserMobile.ToString();
                }
                else {
                    if (!string.IsNullOrEmpty(UserEmail))
                        name = UserEmail;
                }
            }
            return string.Concat(name[0], "***", name[name.Length - 1]);
        }


        public static SplitPageData<CommentInfo> GetPage(DataSource ds, int targetType, long targetId, long index, int size, int show = 8)
        {
            long count;
            IList<CommentInfo> list = Db<CommentInfo>.Query(ds)
                .Select(
                    S<Comment>(),
                    S_AS<MemberInfo, CommentInfo>("Name", "UserName"),
                    S_AS<MemberInfo, CommentInfo>("Email", "UserEmail"),
                    S_AS<MemberInfo, CommentInfo>("Mobile", "UserMobile"),
                    S_AS<MemberInfo, CommentInfo>("Image", "UserImage")
                    /*, S_AS<ProductOrderMapping>("ProductInfo", "ProductInfo")*/)
                .InnerJoin(O<CommentInfo>("UserId"), O<MemberInfo>("Id"))
                //.InnerJoin(O<CommentInfo>("TargetId"), O<ProductOrderMapping>("ProductId")).And(O<CommentInfo>("TargetData"), O<ProductOrderMapping>("OrderId"))
                .Where(W<CommentInfo>("ParentId", 0) & W<CommentInfo>("TargetType", targetType) & W<CommentInfo>("TargetId", targetId))
                .OrderBy(D<CommentInfo>("CreationDate"))
                .ToList<CommentInfo>(size, index, out count);
            return new SplitPageData<CommentInfo>(index, size, list, count, show);
        }
        public static SplitPageData<CommentInfo> GetPageByStar(DataSource ds, int targetType, long targetId, int star1, int star2, long index, int size, int show = 8)
        {
            long count;
            IList<CommentInfo> list = Db<CommentInfo>.Query(ds)
                .Select(
                    S<Comment>(),
                    S_AS<MemberInfo, CommentInfo>("Name", "UserName"),
                    S_AS<MemberInfo, CommentInfo>("Email", "UserEmail"),
                    S_AS<MemberInfo, CommentInfo>("Mobile", "UserMobile"),
                    S_AS<MemberInfo, CommentInfo>("Image", "UserImage")
                    /*, S_AS<ProductOrderMapping>("ProductInfo", "ProductInfo")*/)
                .InnerJoin(O<CommentInfo>("UserId"), O<MemberInfo>("Id"))
                //.InnerJoin(O<CommentInfo>("TargetId"), O<ProductOrderMapping>("ProductId")).And(O<CommentInfo>("TargetData"), O<ProductOrderMapping>("OrderId"))
                .Where(W<CommentInfo>("Star", star1, DbWhereType.GreaterThanOrEqual) & W<CommentInfo>("Star", star2, DbWhereType.LessThan) & W<CommentInfo>("ParentId", 0) & W<CommentInfo>("TargetType", targetType) & W<CommentInfo>("TargetId", targetId))
                .OrderBy(D<CommentInfo>("CreationDate"))
                .ToList<CommentInfo>(size, index, out count);
            return new SplitPageData<CommentInfo>(index, size, list, count, show);
        }
        public static SplitPageData<CommentInfo> GetPageByImage(DataSource ds, int targetType, long targetId, long index, int size, int show = 8)
        {
            long count;
            IList<CommentInfo> list = Db<CommentInfo>.Query(ds)
                .Select(
                    S<Comment>(),
                    S_AS<MemberInfo, CommentInfo>("Name", "UserName"),
                    S_AS<MemberInfo, CommentInfo>("Email", "UserEmail"),
                    S_AS<MemberInfo, CommentInfo>("Mobile", "UserMobile"),
                    S_AS<MemberInfo, CommentInfo>("Image", "UserImage")
                    /*, S_AS<ProductOrderMapping>("ProductInfo", "ProductInfo")*/)
                .InnerJoin(O<CommentInfo>("UserId"), O<MemberInfo>("Id"))
                //.InnerJoin(O<CommentInfo>("TargetId"), O<ProductOrderMapping>("ProductId")).And(O<CommentInfo>("TargetData"), O<ProductOrderMapping>("OrderId"))
                .RightJoin(O<CommentInfo>("Id"), O<CommentImage>("Id"))
                .Where(W<CommentInfo>("ParentId", 0) & W<CommentInfo>("TargetType", targetType) & W<CommentInfo>("TargetId", targetId))
                .OrderBy(D<CommentInfo>("CreationDate"))
                .ToList<CommentInfo>(size, index, out count);
            return new SplitPageData<CommentInfo>(index, size, list, count, show);
        }
    }
}
