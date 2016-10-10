using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XcpNet.Ad.Modules
{
    public class AdMember : IdentityModule
    {
        public long UserId = 0;
        /// <summary>
        /// 标题
        /// </summary>
        [DataColumn(128)]
        public string Title = null;
        /// <summary>
        /// 链接地址
        /// </summary>
        [DataColumn(128)]
        public string Url = null;
        /// <summary>
        /// 图片地址
        /// </summary>
        [DataColumn(128)]
        public string ImgUrl = null;
        /// <summary>
        /// 是否启用
        /// </summary>  
        public bool IsEnable = true;
        /// <summary>
        /// 类型：1：乡道馆 2：店铺
        /// </summary>
        public int Type = 0;

        public static IList<AdMember> GetAdMember(DataSource ds, long userId, int pageSize, int type = 1)
        {
            return Db<AdMember>.Query(ds).Select().
                Where(W("UserId", userId) & W("IsEnable", true) & W("Type", type)).
                ToList<AdMember>(pageSize);
        }

        public static DataStatus Delete(DataSource ds, long userId, int id)
        {
            int result = Db<AdMember>.Query(ds).Delete()
                .Where(W("UserId", userId) & W("Id", id)).Execute();
            return result > 0 ? DataStatus.Success : DataStatus.Failed;
        }

        public static DataStatus Update(DataSource ds, AdMember model)
        {
            int result = Db<AdMember>.Query(ds).Update()
                .Set("Title", model.Title)
                .Set("ImgUrl", model.ImgUrl)
                .Set("Url", model.Url)
                .Where(W("UserId", model.UserId) & W("Id", model.Id)).Execute();
            return result > 0 ? DataStatus.Success : DataStatus.Failed;
        }
    }
}
