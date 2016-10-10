using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;

namespace XcpNet.Website.Modules
{
    public enum SiteType
    {
        Www = 1,
        Wap = 2,
    }

    [Serializable]
    public sealed class PromotionChannel : IdentityModule
    {
        public int ParentId = 0;
        [DataColumn(16)]
        public string Name = null;
        [DataColumn(256)]
        public string Image = null;
        public SiteType Type = SiteType.Www;
        [DataColumn(16)]
        public string BackColor = null;
        [DataColumn(256)]
        public string BackImage = null;

        public static PromotionChannel GetById(DataSource ds, int id)
        {
            return Db<PromotionChannel>.Query(ds)
                .Select()
                .Where(W("Id", id))
                .First<PromotionChannel>();
        }
        public static IList<PromotionChannel> GetAll(DataSource ds, int parent, int type)
        {
            return Db<PromotionChannel>.Query(ds)
                .Select()
                .Where(W("ParentId", parent) & W("Type", type))
                .OrderBy(A("Id"))
                .ToList<PromotionChannel>();
        }
    }
}
