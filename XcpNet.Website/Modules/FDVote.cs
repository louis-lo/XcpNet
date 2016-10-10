using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Templates;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws;

namespace XcpNet.Website.Modules
{
    /// <summary>
    /// 父亲节活动用户投票记录表
    /// </summary>
    [Serializable]
    public class FDVote : NoIdentityModule
    {
        /// <summary>
        /// 投票用户Id，member表对应
        /// </summary>
        public long FromUserId = 0L;
        /// <summary>
        /// 被投人Id，FDRegister表对应
        /// </summary>
        public long ToId = 0L;
        /// <summary>
        /// 是否己经获取优惠券
        /// </summary>
        public bool IsGetCoupons;
        /// <summary>
        /// 被投票次数
        /// </summary>
        public int VoteCount = 0;
        /// <summary>
        /// 投票时间
        /// </summary>
        public DateTime CreationDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);

        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "UserIdIndex");
        }
        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "UserIdIndex", "FromUserId", "ToId");
        }

        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            return (
                FromUserId > 0L &&
                ToId > 0L) ?
                DataStatus.Success : DataStatus.Failed;
        }

        public static FDVote GetNewsVote(DataSource ds, long fromUserId)
        {
           return Db<FDVote>.Query(ds)
                .Select()
                .Where(W("FromUserId", fromUserId))
                .OrderBy(new DbOrderBy("CreationDate",DbOrderByType.Desc))
                .First<FDVote>();
        }
    }
}
