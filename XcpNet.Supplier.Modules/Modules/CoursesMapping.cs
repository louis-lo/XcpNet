using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class CoursesMapping : NoIdentityModule
    {
        [DataColumn(true)]
        public long UserId = 0L;
        public int CoursesId = 0;

        public static bool Exists(DataSource ds, long userId, int coureseId)
        {
            return Db<CoursesMapping>.Query(ds)
                .Select()
                .Where(W("CoursesId", coureseId) & W("UserId", userId))
                .Count() > 0;
        }
    }
}
