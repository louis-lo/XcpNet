using System;
using Cnaws.Web;
using M = Cnaws.Passport.Modules;

namespace XcpNet.Passport.Controllers.Extension
{
    public class BillList : Common.CommPassportController
    {
        [Authorize(true)]
        public void Index()
        {
            List();
        }

        [Authorize(true)]
        public void List(long index = 1)
        {
            this["RecordList"] = M.MoneyRecord.GetPageByMember(DataSource, User.Identity.Id, index, 20, 8);
            Render("billlist.html");
        }
    }
}
