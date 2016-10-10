using System;
using Cnaws.Management;
using P = Cnaws.Product.Modules;
using A = XcpNet.AfterSales.Modules;
using Cnaws.Web.Templates;

namespace XcpNet.Admin.Management
{
    public class AfterSales : ManagementController
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
        
        public virtual string Path
        {
            get { return "aftersales"; }
        }
        public virtual A.AfterSalesRecord.EChannel Channel
        {
            get { return A.AfterSalesRecord.EChannel.GoodsOrder; }
        }

        protected virtual object GetOrder(string orderId)
        {
            return A.AfterSalesRecord.GetAndProductById(DataSource, orderId);
        }
        protected virtual object GetOrderMapping(object[] args)
        {
            return P.ProductOrderMapping.GetById(DataSource, Convert.ToString(args[0]), Convert.ToInt64(args[1]));
        }
        
        public void Index(int type = 0, int state = -99)
        {
            if (CheckRight())
            {
                if (CheckPost("aftersales", () =>
                 {
                     this["Type"] = type;
                     this["State"] = state;
                     this["TypeName"] = A.AfterSalesRecord.GetTypeInfo(type);
                     this["StateName"] = A.AfterSalesRecord.GetStateInfo(state);
                 }))
                    NotFound();
            }
        }
        public void List(int type, int state, int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    SetResult(A.AfterSalesRecord.GetListByPage(DataSource, type, state, page, 10, 11, Channel));
                }
            }
        }
        public void Info(string orderId)
        {
            if (CheckAjax())
            {

                this["Order"] = GetOrder(orderId);
                this["GetMapping"] = new FuncHandler((args) =>
                {
                    if (args != null && args.Length > 0)
                        return GetOrderMapping(args);
                    return null;
                });
                this["GetStateString"] = new FuncHandler((args) =>
                {
                    try
                    {
                        object arg = args[0];
                        A.AfterSalesRecord.EServiceState state;
                        if (arg is string)
                            state = (A.AfterSalesRecord.EServiceState)Enum.Parse(typeof(A.AfterSalesRecord.EServiceState), (string)arg);
                        else
                            state = (A.AfterSalesRecord.EServiceState)(int)arg;
                        return A.AfterSalesRecord.GetStateInfo((int)state);
                    }
                    catch (Exception) { }
                    return string.Empty;
                });
                RenderTemplate("aftersales.html");
            }
        }
    }
}
