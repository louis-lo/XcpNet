using System;
using Cnaws;
using Cnaws.Data;
using Cnaws.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Data.Query;

namespace XcpNet.Ad.Modules
{
    public class Advertisement : IdentityModule
    {

        /// <summary>
        /// 省、直辖市
        /// </summary>
        public int Province = 0;
        /// <summary>
        /// 城市
        /// </summary>
        public int City = 0;
        /// <summary>
        /// 县、区
        /// </summary>
        public int County = 0;
        /// <summary>
        /// 标题
        /// </summary>
        [DataColumn(128)]
        public string Title = null;
        /// <summary>
        /// 用户Id
        /// </summary>
        public long UserId = 0;
        /// <summary>
        /// 分类Id
        /// </summary>
        public long CategoryId = 0;
        /// <summary>
        /// 分类Id对应AdType表的Id
        /// </summary>
        public int LabelId = 0;
        /// <summary>
        /// 类型：1.Banner  2.轮播广告  3.促销广告
        /// </summary>
        public int Type = 0;
        /// <summary>
        /// 链接地址
        /// </summary>
        [DataColumn(128)]
        public string Url = null;
        /// <summary>
        /// 宽度
        /// </summary>
        public int Width = 0;
        /// <summary>
        /// 高度
        /// </summary>
        public int Height = 0;
        /// <summary>
        /// 图片地址
        /// </summary>
        [DataColumn(128)]
        public string ImgUrl = null;
        /// <summary>
        /// 广告内容
        /// </summary>
        [DataColumn(2000)]
        public string Content = null;
        /// <summary>
        /// 排序
        /// </summary>
        public int Sort = 0;
        /// <summary>
        /// 是否启用
        /// </summary>  
        public bool IsEnable = true;

        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (string.IsNullOrEmpty(Title))
                return DataStatus.Failed;
            return DataStatus.Success;
        }


        protected override DataStatus OnUpdateBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (string.IsNullOrEmpty(Title))
                return DataStatus.Failed;
            return DataStatus.Success;
        }

        public static IList<Advertisement> GetAll(DataSource ds)
        {
            return ExecuteReader<Advertisement>(ds, P("IsEnable", true));
        }

        public static Advertisement GetById(DataSource ds, int id)
        {
            return ExecuteSingleRow<Advertisement>(ds, P("Id", id));
        }

        public static IList<Advertisement> GetByLabel(DataSource ds, int label, int type, int province, int city, int county)
        {
            return Db<Advertisement>.Query(ds)
                .Select()
                .Where((W("Province", 0) | W("Province", province) | W("City", city) | W("County", county)) & W("LabelId", label)& W("IsEnable", true))
                .OrderBy(D("Sort"), D("Id"))
                .ToList<Advertisement>();
        }

        public static IList<Advertisement> GetByLabel(DataSource ds, int label)
        {
            return Db<Advertisement>.Query(ds)
                .Select()
                .Where(W("LabelId", label))
                .OrderBy(D("Sort"), D("Id"))
                .ToList<Advertisement>();
        }
        public static IList<Advertisement> GetByLabelAndCategoryId(DataSource ds, int label, int category)
        {
            return Db<Advertisement>.Query(ds)
                .Select()
                .Where(W("LabelId", label) & W("CategoryId", category))
                .OrderBy(D("Sort"), D("Id"))
                .ToList<Advertisement>();
        }

        public static DataStatus Delete(DataSource ds, int id)
        {
            return (new Advertisement() { Id = id }).Delete(ds);
        }
        public static DataStatus DeleteByUserId(DataSource ds, int id,long userid)
        {
            return (new Advertisement() { Id = id,UserId=userid}).Delete(ds, "Id","UserId");
        }

        public static SplitPageData<DataJoin<Advertisement, AdType>> GetPageByType(DataSource ds, long label, int index, int size, int show = 8)
        {
            long count;
            IList<DataJoin<Advertisement, AdType>> list;
            if (label == -1)
            {
                list = Db<Advertisement>.Query(ds).Select(S<Advertisement>(), S<AdType>("Name"), S_AS<AdType>("Type", "TypeName")).InnerJoin(O<Advertisement>("LabelId"), O<AdType>("Id"))
                    .OrderBy(D<Advertisement>("Sort"),D<Advertisement>("Id")).ToList<DataJoin<Advertisement, AdType>>(size, index, out count);
            }
            else
            {
                list = Db<Advertisement>.Query(ds).Select(S<Advertisement>(), S<AdType>("Name"), S_AS<AdType>("Type", "TypeName")).InnerJoin(O<Advertisement>("LabelId"), O<AdType>("Id"))
                    .Where(W<AdType>("Type", label))
                    .OrderBy(D<Advertisement>("Sort"),D<Advertisement>("Id")).ToList<DataJoin<Advertisement, AdType>>(size, index, out count);
            }
            return new SplitPageData<DataJoin<Advertisement, AdType>>(index, size, list, count, show);
        }

        public static SplitPageData<DataJoin<Advertisement, AdType>> GetPageByTypeByUser(DataSource ds, long label,long userid,AdType.EAdType type, int index, int size, int show = 8)
        {
            long count;
            IList<DataJoin<Advertisement, AdType>> list;
            if (label == -1)
            {
                list = Db<Advertisement>.Query(ds).Select(S<Advertisement>(), S<AdType>("Name"), S_AS<AdType>("Type", "TypeName")).InnerJoin(O<Advertisement>("LabelId"), O<AdType>("Id"))
                    .Where(W<Advertisement>("UserId", userid)& W<AdType>("Type", type))
                    .OrderBy(D<Advertisement>("Sort"),D<Advertisement>("Id")).ToList<DataJoin<Advertisement, AdType>>(size, index, out count);
            }
            else
            {
                list = Db<Advertisement>.Query(ds).Select(S<Advertisement>(), S<AdType>("Name"), S_AS<AdType>("Type", "TypeName")).InnerJoin(O<Advertisement>("LabelId"), O<AdType>("Id"))
                    .Where(W<AdType>("Type", type) &W<Advertisement>("LabelId", label)& W<Advertisement>("UserId", userid))
                    .OrderBy(D<Advertisement>("Sort"),D<Advertisement>("Id")).ToList<DataJoin<Advertisement, AdType>>(size, index, out count);
            }
            return new SplitPageData<DataJoin<Advertisement, AdType>>(index, size, list, count, show);
        }


        /// <summary>
        /// 根据店铺Id/乡道馆Id和标签调用店铺和乡道馆等广告
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="labelid"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static IList<Advertisement> GetByLabelAndUser(DataSource ds, long labelid, long userid)
        {
            return Db<Advertisement>.Query(ds)
                .Select()
                .Where(W("LabelId", labelid) & W("UserId", userid))
                .OrderBy(D("Sort"),D("Id"))
                .ToList<Advertisement>();
        }
        /// <summary>
        /// 根据店铺Id/乡道馆Id调用店铺和乡道馆所有广告
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="labelid"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static IList<DataJoin<Advertisement, AdType>> GetAllByUser(DataSource ds, long userid)
        {
            return Db<Advertisement>.Query(ds)
                .Select(S<Advertisement>(), S<AdType>("Name"), S_AS<AdType>("Type", "TypeName"))
                .InnerJoin(O<Advertisement>("LabelId"), O<AdType>("Id"))
                .Where(W<Advertisement>("UserId", userid))
                .OrderBy(D<Advertisement>("Sort"),D<Advertisement>("Id"))
                .ToList<DataJoin<Advertisement, AdType>>();
        }
        /// <summary>
        /// 根据产品分类Id调用分类广告
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="labelid"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static IList<Advertisement> GetByLabelAndCategory(DataSource ds, long labelid, long categoryid)
        {
            return Db<Advertisement>.Query(ds)
                .Select()
                .Where(W("LabelId", labelid) & W("CategoryId", categoryid))
                .OrderBy(D("Sort"),D("Id"))
                .ToList<Advertisement>();
        }
    }
}
