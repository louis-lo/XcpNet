using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Sms.Modules;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using D = XcpNet.Supplier.Modules.Modules;
using XcpNet.Services.Sms;

namespace XcpNet.Supplier.Controllers
{
    public class DistributorOrder:SupplierController
    {
        [SupplierOrDistributor(true)]
        public void List(string type, long page = 0L)
        {
            //delivery all finished
            type = type.ToLower();
            string title = "", nickName = "", startDate = "", endDate = "";
            if (!string.IsNullOrEmpty(Request["title"])) title = Request["title"];
            if (!string.IsNullOrEmpty(Request["nickName"])) nickName = Request["nickName"];
            if (!string.IsNullOrEmpty(Request["startDate"])) startDate = Request["startDate"];
            if (!string.IsNullOrEmpty(Request["endDate"])) endDate = Request["endDate"];
            var search = new { title = title, nickName = nickName, startDate = startDate, endDate = endDate };
            this["Search"] = search;
            switch (type)
            {
                case "payment":
                    this["OrderList"] = D.DistributorOrder.GetPageBySupplier(DataSource, User.Identity.Id, (int)P.OrderState.Payment, search.title, search.nickName, search.startDate, search.endDate, Math.Max(1, page), 10, 8);
                    break;
                case "delivery":
                    this["OrderList"] = D.DistributorOrder.GetPageBySupplier(DataSource, User.Identity.Id, (int)P.OrderState.Delivery, search.title, search.nickName, search.startDate, search.endDate, Math.Max(1, page), 10, 8);
                    break;
                case "dooutwarehouse"://出库
                    if (IsPost)
                    {
                        DataSource.Begin();                       
                        try
                        {
                            D.DistributorOrder order = new D.DistributorOrder();
                            string OrderId = Request.Form["OrderId"];
                            order = D.DistributorOrder.GetById(DataSource, OrderId);
                            if ((new D.DistributorOrder { Id = OrderId, UserId = order.UserId }).UpdateStateByUser(DataSource, P.OrderState.Delivery) != DataStatus.Success)
                                throw new Exception();
                            DataSource.Commit();
                            SetResult(true);
                        }
                        catch (Exception)
                        {
                            DataSource.Rollback();
                            SetResult(false);
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                    return;
                case "dodelivery":///发货
                    if (IsPost)
                    {
                        DataSource.Begin();                       
                        try
                        {
                            D.DistributorOrder order = new D.DistributorOrder();
                            P.ProductLogistics value = new P.ProductLogistics();
                            value = DbTable.Load<P.ProductLogistics>(Request.Form);
                            if (P.ProductLogistics.GetByOrder(DataSource, value.OrderId) == null)
                            {
                                if (value.Insert(DataSource) != DataStatus.Success)
                                    throw new Exception();
                                order = D.DistributorOrder.GetById(DataSource, value.OrderId);
                                if ((new D.DistributorOrder() { Id = value.OrderId, UserId = order.UserId }).UpdateStateByUser(DataSource, P.OrderState.OutWarehouse) != DataStatus.Success)
                                    throw new Exception();
                            }
                            else
                            {
                                if (value.Update(DataSource) != DataStatus.Success)
                                    throw new Exception();
                            }
                            DataSource.Commit();
                            SetResult(true);
                            try
                            {
                                //发货成功之后发送短信通知
                                M.MemberInfo member = M.MemberInfo.GetById(DataSource, order.UserId);
                                if (Member.Mobile > 0)
                                {
                                    SmsMobset.Send(
                                       DataSource,
                                       Member.Mobile,
                                       SmsTemplate.HasShipped,
                                       !string.IsNullOrEmpty(member.RealName) ? member.RealName : !string.IsNullOrEmpty(member.Name) ? member.Name : string.Empty,
                                       order.Id,
                                       value.ProviderName,
                                       value.BillNo);
                                }
                            }
                            catch (Exception) { }
                        }
                        catch (Exception)
                        {
                            DataSource.Rollback();
                            SetResult(false);
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                    return;
                case "dofreight":
                    if (IsPost)
                    {
                        D.DistributorOrder value = DbTable.Load<D.DistributorOrder>(Request.Form);
                        value.SupplierId = User.Identity.Id;
                        SetResult(value.UpdateFreightBySupplier(DataSource));
                    }
                    else
                    {
                        NotFound();
                    }
                    return;
                case "all":
                    if (page < 1L) page = 1L;
                    this["OrderList"] = D.DistributorOrder.GetPageBySupplier(DataSource, User.Identity.Id, -1, search.title, search.nickName, search.startDate, search.endDate, page, 10, 8);
                    break;
                case "finished":
                    if (page < 1L) page = 1L;
                    this["OrderList"] = D.DistributorOrder.GetPageBySupplier(DataSource, User.Identity.Id, (int)P.OrderState.Finished, search.title, search.nickName, search.startDate, search.endDate, page, 10, 8);
                    break;
                case "getlogistics":
                    string orderid = Request["orderid"];
                    this["Logistics"] = P.ProductLogistics.GetByOrder(DataSource, orderid);
                    type = "all";
                    break;
                default:
                    NotFound();
                    return;
            }
            this["Page"] = page;
            this["State"] = type;
            if (IsSupplier())
                Render(string.Concat("supplier/distributororder_", type, ".html"));
        }
    }
}
