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
    /// 父亲节活动报名表
    /// </summary>
    [Serializable]
    public class FDRegister : LongIdentityModule
    {
        /// <summary>
        /// 用户Id，member表对应
        /// </summary>
        public long UserId = 0L;
        /// <summary>
        /// 头像
        /// </summary>
        [DataColumn(256)]
        public string HeadImgUrl;
        /// <summary>
        /// 名称
        /// </summary>
        [DataColumn(50)]
        public string Name;
        /// <summary>
        /// 手机号码
        /// </summary>
        public long Mobile = 0L;
        /// <summary>
        /// 被投票次数
        /// </summary>
        public long VoteCount = 0L;
        /// <summary>
        /// 最想对爸爸说的一句话
        /// </summary>
        [DataColumn(512)]
        public string Content = null;
        /// <summary>
        /// 报名时间
        /// </summary>
        public DateTime CreationDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);

        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "UserIdIndex");
        }
        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "UserIdIndex", "UserId");
        }

        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            return (UserId > 0L &&
                Mobile > 0L &&
                !string.IsNullOrEmpty(Name) &&
                !string.IsNullOrEmpty(Content)) ?
                DataStatus.Success : DataStatus.Failed;
        }

        public static FDRegister GetById(DataSource ds, long id)
        {
            return Db<FDRegister>.Query(ds).Select().Where(W("Id", id)).First<FDRegister>();
        }

        public static bool HasRegisted(DataSource ds, long userId)
        {
            FDRegister item = Db<FDRegister>.Query(ds).Select().Where(W("UserId", userId)).First<FDRegister>();
            return item == null;
        }

        public static string GenerateMaxNo(DataSource ds)
        {
            long register_maxId = Db<FDRegister>.Query(ds).Select(S_MAX("Id")).Single<long>();
            register_maxId = register_maxId == 0L ? 1L : register_maxId + 1;
            return register_maxId.ToString("d5");
        }

        public static SplitPageData<FDRegister> GetRegisterList(DataSource ds, long pageIndex, int pageSize, int show = 10, string orderbyColumn = "VoteCount")
        {
            long count = 0;
            IList<FDRegister> list = DataQuery
                .Select<FDRegister>(ds)
                .OrderBy(Od(orderbyColumn))
                .Limit(pageSize, pageIndex)
                .ToList<FDRegister>(out count);

            return new SplitPageData<FDRegister>(pageIndex, pageSize, list, count, show);
        }

        public static void UpdateVoteCount(DataSource ds, long id, int count)
        {
            FDRegister item = Db<FDRegister>.Query(ds).Select().Where(W("Id",id)).First<FDRegister>();
            item.VoteCount += count;
            item.Update(ds, ColumnMode.Include, new DataColumn("VoteCount"));
        }

        public static IList<FDRegister> Search(DataSource ds, int id)
        {
            return Db<FDRegister>.Query(ds).Select().Where(W<FDRegister>("Id", id)).ToList<FDRegister>();
        }
    }
}
