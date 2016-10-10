using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Data.Query;
using Cnaws.Web;
using Cnaws.Data;
using Pd = Cnaws.Product.Modules;
using Cnaws;
using Cnaws.Templates;
using U = Cnaws.Passport.Modules;

namespace XcpNet.Supplier.Modules.Modules
{
    /// <summary>
    /// 进货方案表
    /// </summary>
    public class DistributorProgramme : LongIdentityModule
    {
        /// <summary>
        /// 方案类型枚举
        /// </summary>
        public enum EProgrammeType
        {
            /// <summary>
            /// 新店方案
            /// </summary>
            NewStore = 0,
            /// <summary>
            /// 改造店方案
            /// </summary>
            ReformStore,
            /// <summary>
            /// 综合方案
            /// </summary>
            PublicProgramme,
            /// <summary>
            /// 所有，用于查询
            /// </summary>
            All = -1
        }
        /// <summary>
        /// 方案名称
        /// </summary>
        [DataColumn(64)]
        public string Title = string.Empty;
        /// <summary>
        /// 方案所属供应商Id
        /// </summary>
        public long DistributorId = 0;
        /// <summary>
        /// 方案所属用户（0为系统方案）
        /// </summary>
        public long UserId = 0;
        /// <summary>
        /// 行业分类Id
        /// </summary>
        public int CategoryId = 0;
        /// <summary>
        /// 方案类型
        /// </summary>
        public EProgrammeType Type = EProgrammeType.NewStore;
        /// <summary>
        /// 方案状态
        /// </summary>
        public Pd.ProductState State = Pd.ProductState.Saved;
        /// <summary>
        /// 方案展示图
        /// </summary>
        [DataColumn(128)]
        public string Image = string.Empty;
        public int Count = 0;
        /// <summary>
        /// 省Id
        /// </summary>
        public int Province = 0;
        /// <summary>
        /// 市Id
        /// </summary>
        public int City = 0;
        /// <summary>
        /// 区Id
        /// </summary>
        public int County = 0;
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);


        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "Title");
            DropIndex(ds, "UserId");
            DropIndex(ds, "CategoryId");
            DropIndex(ds, "Type");
            DropIndex(ds, "UserIdAndTitle");
            DropIndex(ds, "CategoryIdAndTitle");
            DropIndex(ds, "CategoryIdAndType");
            DropIndex(ds, "CategoryAndTitleAndType");
            DropIndex(ds, "TitleAndType");

        }
        public string GetTypeString()
        {
            switch (Type)
            {
                case EProgrammeType.NewStore: return "新店方案";
                case EProgrammeType.PublicProgramme: return "综合方案";
                case EProgrammeType.ReformStore: return "改造店方案";
            }
            return "错误的类型";
        }
        public string GetStateString()
        {
            switch (State)
            {
                case Pd.ProductState.Sale: return "上架中";
                case Pd.ProductState.Saved: return "未上架";
            }
            return "错误的状态";
        }
        public string GetCategoryString(DataSource ds)
        {
            return IndutryCategory.GetById(ds, CategoryId).Name;
        }
        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "Title", "Title");
            CreateIndex(ds, "UserId", "UserId");
            CreateIndex(ds, "CategoryId", "CategoryId");
            CreateIndex(ds, "Type", "Type");
            CreateIndex(ds, "UserIdAndTitle", "UserId", "Title");
            CreateIndex(ds, "CategoryIdAndTitle", "CategoryId", "Title");
            CreateIndex(ds, "CategoryIdAndType", "CategoryId", "Type");
            CreateIndex(ds, "CategoryAndTitleAndType", "CategoryId", "Title", "Type");
            CreateIndex(ds, "TitleAndType", "Title", "Type");
        }

        /// <summary>
        /// 前端获取进货方案列表
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <param name="categoryid"></param>
        /// <param name="title"></param>
        /// <param name="type"></param>
        /// <param name="province"></param>
        /// <param name="city"></param>
        /// <param name="county"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="show"></param>
        /// <returns></returns>
        public static SplitPageData<DistributorProgramme> GetList(DataSource ds, long userid, int categoryid, string title, EProgrammeType type, int province, int city, int county, int index, int size, int show = 8)
        {
            DbWhereQueue where = (W("State", Pd.ProductState.Sale) | W("State", Pd.ProductState.BeforeSaved));
            if (!string.IsNullOrEmpty(title))
                where &= W("Title", title, DbWhereType.Like);
            if (categoryid > 0)
            {
                where &= (W("CategoryId", categoryid));
            }
            ///根据区域查询
            where &= (W("Province", province) | W("Province", 0));
            where &= (W("City", city) | W("City", 0));
            where &= (W("County", county) | W("County", 0));
            if (type != EProgrammeType.All)
                where &= W("Type", type);
            long count;
            IList<DistributorProgramme> list;
            list = Db<DistributorProgramme>.Query(ds)
                .Select()
                .Where(where)
                .OrderBy(D("CreateDate"), A("Type"))
                .ToList<DistributorProgramme>(size, index, out count);
            return new SplitPageData<DistributorProgramme>(index, size, list, count, show);
        }
        public static SplitPageData<dynamic> GetPageEx(DataSource ds, int categoryid, int index, int size, int show = 8)
        {
            DbWhereQueue where = new DbWhereQueue();
            if (categoryid > 0)
                where &= (W("CategoryId", categoryid));
            long count;
            IList<dynamic> list;
            list = Db<DistributorProgramme>.Query(ds)
                .Select()
                .Where(where)
                .OrderBy(D("Id"))
                .ToList(size, index, out count);
            foreach (var item in list)
            {
                item.Distributor = item.DistributorId > 0 ? U.Member.GetById(ds, item.DistributorId).Name : string.Empty;
                item.User = item.UserId > 0 ? U.Member.GetById(ds, item.UserId).Name : string.Empty;
            }
            return new SplitPageData<dynamic>(index, size, list, count, show);
        }

        public static SplitPageData<DistributorProgramme> GetListbyDistributor(DataSource ds, long userid, int categoryid, string title, EProgrammeType type, int province, int city, int county, int index, int size, int show = 8)
        {
            DbWhereQueue where = (W("State", Pd.ProductState.Sale) | W("State", Pd.ProductState.BeforeSaved));
            if (!string.IsNullOrEmpty(title))
                where &= W("Title", title, DbWhereType.Like);
            where &= W("DistributorId", userid);
            if (categoryid > 0)
            {
                where &= (W("CategoryId", categoryid));
            }
            ///根据区域查询
            where &= (W("Province", province) | W("Province", 0));
            where &= (W("City", city) | W("City", 0));
            where &= (W("County", county) | W("County", 0));
            if (type != EProgrammeType.All)
                where &= W("Type", type);
            long count;
            IList<DistributorProgramme> list;
            list = Db<DistributorProgramme>.Query(ds)
                .Select()
                .Where(where)
                .OrderBy(D("CreateDate"), A("Type"))
                .ToList<DistributorProgramme>(size, index, out count);
            return new SplitPageData<DistributorProgramme>(index, size, list, count, show);
        }

        public static DistributorProgramme GetById(DataSource ds, long id)
        {
            return Db<DistributorProgramme>.Query(ds).Select().Where(W("Id", id)).First<DistributorProgramme>();
        }
        public static DistributorProgramme GetById(DataSource ds, long id, long UserId)
        {
            return Db<DistributorProgramme>.Query(ds).Select().Where(W("Id", id) & W("DistributorId", UserId)).First<DistributorProgramme>();
        }

        public static DataStatus DelByDistributor(DataSource ds, long id, long distributorid)
        {
            if (Db<DistributorProgramme>.Query(ds).Delete().Where(W("Id", id) & W("DistributorId", distributorid)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }
        public static DataStatus DelByUser(DataSource ds, long id, long userid)
        {
            if (Db<DistributorProgramme>.Query(ds).Delete().Where(W("Id", id) & W("UserId", userid)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }
        public static DataStatus DelById(DataSource ds, long id)
        {
            if (Db<DistributorProgramme>.Query(ds).Delete().Where(W("Id", id)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }

        public static DataStatus UpdataCount(DataSource ds, long programmeId, int count)
        {
            return new DistributorProgramme() { Id = programmeId }.Update(ds, ColumnMode.Include, MODC("Count", count));
        }
    }
}
