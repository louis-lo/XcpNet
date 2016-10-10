using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws;
using Cnaws.Data;
using Cnaws.Management;
using Pd=Cnaws.Product.Modules;

namespace XcpNet.Admin.Management
{
    public sealed class LogisticsList : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }
        protected override string Namespace
        {
            get { return "XcpNet.Admin"; }
        }
        public void Index()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (CheckPost("logisticslist"))
                        NotFound();
                }
            }
        }


        public void List(string keyword="_",int page = 1)
        {
            if (CheckAjax())
            {
                SetResult(Pd.LogisticsCompany.GetList(DataSource, keyword, page, 10, 8));
            }
        }

        public void Add()
        {
            if (CheckAjax())
            {
                if (IsPost)
                {
                    Pd.LogisticsCompany logisticscompany = new Pd.LogisticsCompany();
                    logisticscompany.Name = Request["Name"];
                    logisticscompany.NameCode = Request["NameCode"];
                    logisticscompany.IsEnable = Types.GetBooleanFromString(Request.Form["IsEnable"]);
                    SetResult(logisticscompany.Insert(DataSource));
                }
            }
        }

        public void Mod()
        {
            if (CheckAjax())
            {
                if (IsPost)
                {
                    Pd.LogisticsCompany logisticscompany = new Pd.LogisticsCompany();
                    logisticscompany.Id = int.Parse(Request["Id"]);
                    logisticscompany.IsEnable = Types.GetBooleanFromString(Request.Form["IsEnable"]);
                    SetResult(logisticscompany.Update(DataSource, ColumnMode.Include, "IsEnable"));
                }
            }
        }
        public void Del()
        {
            if (CheckAjax())
            {
                if (IsPost)
                {
                    SetResult(new Pd.LogisticsCompany() { Id= int.Parse(Request["Id"]) }.Delete(DataSource));
                }
            }
        }
    }
}
