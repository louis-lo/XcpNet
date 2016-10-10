using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws.Product.Modules;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XcpNet.Supplier.Modules.Modules
{
    public class XDGInfo: StoreInfo
    {
        #region 通过用户Id查询乡道馆信息
        /// <summary>
        /// 通过用户Id查询乡道馆信息
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static XDGInfo GetXDGInfoByUserId(DataSource ds, long userId)
        {
            return Db<XDGInfo>.Query(ds).Select().Where(W("UserId", userId)).First<XDGInfo>();
        }
        #endregion

        #region 查询乡道馆信息集合
        /// <summary>
        /// 查询乡道馆信息集合
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static SplitPageData<XDGInfo> GetXDGInfoList(DataSource ds, int pageIndex, int pageSize)
        {
            long count;
            IList<XDGInfo> XDGInfoList = Db<XDGInfo>.Query(ds).Select().OrderBy(A("Id")).ToList<XDGInfo>(pageSize, pageIndex, out count);
            return new SplitPageData<XDGInfo>(pageIndex, pageSize, XDGInfoList, count);
        }

        

        #endregion

        #region 通过乡道馆信息Id查询乡道馆首页推荐商品
        /// <summary>
        /// 通过乡道馆信息Id查询乡道馆首页推荐商品
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public IList<Product> GetProduct(DataSource ds, int pageSize)
        {
            return Product.GetProduct(ds, this.UserId, pageSize);
        }
        #endregion

        #region 修改乡道馆信息
        /// <summary>
        /// 修改乡道馆信息
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public DataStatus Update(DataSource ds)
        {
            return Db<XDGInfo>.Query(ds).Update()
                        .Set("StoreName", this.StoreName)
                        .Set("StoreLogo", this.StoreLogo)
                        .Set("StoreSlogan", this.StoreSlogan)
                        .Set("StoreNotice", this.StoreNotice)
                        .Set("StoreExplain", this.StoreExplain)
                        .Set("StoreBusinessLicense", this.StoreBusinessLicense).
                        Where(W("UserId", this.UserId)).
                        Execute() > 0 ? DataStatus.Success : DataStatus.Failed;
        }
        #endregion

        #region 删除乡道馆信息
        /// <summary>
        /// 删除乡道馆信息
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public DataStatus Delete(DataSource ds)
        {
            return Db<XDGInfo>.Query(ds).Delete().
                        Where(W("UserId", this.UserId)).
                        Execute() > 0 ? DataStatus.Success : DataStatus.Failed;
        }
        #endregion
        /// <summary>
        /// 仅适用于接口
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userIds"></param>
        /// <returns></returns>
        public static IList<dynamic> GetAndStorInfoByIds(DataSource ds, long[] userIds)
        {
            //S_AS<Distributor>("IsActivityFree", "Supplier_IsActivityFree"),
            //    S_AS<Distributor>("ActivityCondition"),
            //    S_AS<Distributor>("ActivityFree"),
            //    S_AS<Distributor>("MinOrderPrice"),
            return Db<Distributor>.Query(ds)
                .Select(S_AS<Distributor>("UserId", "Supplier_UserId"), S_AS<Distributor>("Company", "Supplier_Company"),

                S_AS<XDGInfo>("StoreName","StoreInfo_StoreName"), S_AS<XDGInfo>("StoreLogo", "StoreInfo_StoreLogo"), S_AS<XDGInfo>("StoreSlogan", "StoreInfo_StoreSlogan"), S_AS<XDGInfo>("StoreNotice", "StoreInfo_StoreNotice"), S_AS<XDGInfo>("StoreExplain", "StoreInfo_StoreExplain"))
                .LeftJoin(O<Distributor>("UserId"), O<XDGInfo>("UserId"))
                .Where(W<Distributor>("UserId", userIds, DbWhereType.In) & W<Distributor>("State", -1, DbWhereType.NotEqual))
                .ToList();
        }
    }
}
