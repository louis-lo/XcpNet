using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using A = XcpNet.AfterSales.Modules;

namespace XcpNet.Admin.Management
{
    class ReminderDelivery : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        protected override string Namespace
        {
            get
            {
                return "XcpNet.Admin"; ;
            }
        }

        protected override Version Version
        {
            get
            {
                return VERSION;
            }
        }

        public void Index()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (CheckPost("reminderdelivery"))
                        NotFound();
                }
            }
        }

        public void List(string q = "_", int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    long count;
                    IList<A.ReminderDelivery> list = Db<A.ReminderDelivery>.Query(DataSource)
                        .Select(new DbSelect<A.ReminderDelivery>())
                        .OrderBy(new DbOrderBy<A.ReminderDelivery>("RemindTime", DbOrderByType.Desc))
                        .ToList<A.ReminderDelivery>(10, page, out count);
                    SetResult(new SplitPageData<A.ReminderDelivery>(page, 10, list, count, 11));
                }

            }
        }
    }
}
