using System;
using Cnaws.Area;
using System.Collections.Generic;
using PM = Cnaws.Passport.Modules;
using A = XcpNet.AfterSales.Modules;
using Cnaws.Web;
using M = XcpNet.Supplier.Modules.Modules;
using Pd = Cnaws.Product.Modules;
namespace XcpNet.Supplier.Controllers.Extension
{
    public sealed class Index : SupplierController
    {
        [SupplierOrDistributor(true)]
        public void index(string type = "_")
        {
            PM.MemberInfo memberinfo = PM.MemberInfo.GetById(DataSource, User.Identity.Id);
            this["Member"] = memberinfo;
            using (IPArea area = new IPArea())
            {
                try
                {
                    this["Location"] = area.Search(User.Identity.LastIp).ToString();
                }
                catch (Exception)
                {
                    this["Location"] = "未知区域";
                }
            }
            this["Profit"] = new
            {
                ArrivalMoney = A.ProfitRecord.GetArrivalMoney(DataSource, memberinfo.Id),
                FreezeMoney = Cnaws.Passport.Modules.MemberDrawOrder.GetInTreatmentMoney(DataSource, memberinfo.Id),
                HoustonFreeze = A.ProfitRecord.GetHoustonFreezeMoney(DataSource, memberinfo.Id),
                AllHoustonMoney = A.ProfitRecord.GetAllHoustonMoney(DataSource, memberinfo.Id),
                PendingAudit = Cnaws.Passport.Modules.MemberDrawOrder.GetPendingAuditMoney(DataSource, memberinfo.Id),
            };
            if (IsDistributor())
            {
                Pd.Distributor dis = Pd.Distributor.GetById(DataSource, User.Identity.Id);
                if (dis.State == Pd.DistributorState.NotApproved)
                {
                    Refresh(GetUrl("/joinus/wait"));
                }
                else
                {
                    this["OrderList"] = M.DistributorOrder.GetPageByUserAndState(DataSource, User.Identity.Id, type, 1, 3, 3);
                    long userId = User.Identity.Id;
                    this["PaymentCount"] = M.DistributorOrder.GetCountByState(this.DataSource, Pd.OrderState.Payment, userId);
                    this["DeliveryCount"] = M.DistributorOrder.GetCountByState(this.DataSource, Pd.OrderState.Delivery, userId);
                    this["OutWarehouseCount"] = M.DistributorOrder.GetCountByState(this.DataSource, Pd.OrderState.OutWarehouse, userId);
                    this["ReceiptCount"] = M.DistributorOrder.GetCountByState(this.DataSource, Pd.OrderState.Receipt, userId);
                    this["FinishedCount"] = M.DistributorOrder.GetCountByState(this.DataSource, Pd.OrderState.Finished, userId);
                    Render("index.html");
                }
            }
            else
            {
                if (IsSupplier())
                {
                    if (IsWap)
                    {
                        Render("errors/code/notauthorized.html");
                    }
                    else
                        Render("supplier/index.html");
                }                
            }
        }
        [AdminAuthorize]
        public void Admin(long user)
        {
            MemberId = user;
            using (IPArea area = new IPArea())
            {
                try
                {
                    this["Location"] = area.Search(Member.LastIp).ToString();
                }
                catch (Exception)
                {
                    this["Location"] = "未知区域";
                }
            }
            Render("index.html");
        }

        public void Management()
        {
            Dictionary<string, bool> dict = new Dictionary<string, bool>();
            dict.Add("IsSupplier", IsSupplier());
            dict.Add("IsDistributor", IsDistributor());
            SetJavascript("Management", dict);
        }
    }
}
