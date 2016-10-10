using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pd = Cnaws.Product.Modules;
using A = XcpNet.AfterSales.Modules;
using Cnaws;
using Cnaws.Templates;
using Cnaws.Data;
using Cnaws.Web.Templates;
using XcpNet.Common;
namespace XcpNet.Passport.Controllers
{
    public class Service :CommPassportController
    {
        /// <summary>
        /// 售后管理
        /// </summary>
        /// <param name="page"></param>
        [Authorize(true)]
        public void ServiceList(long page = 1L)
        {
            string title = Request.Form["title"];
            string nickName = Request.Form["nickName"];
            string startCreateDate = Request.Form["startCreateDate"];
            string endCreateDate = Request.Form["endCreateDate"];
            string serverState = Request.Form["serverState"];
            this["title"] = title;
            this["nickName"] = nickName;
            this["startCreateDate"] = startCreateDate;
            this["endCreateDate"] = endCreateDate;
            this["serverState"] = serverState;
            this["GetImage"] = new FuncHandler((args) =>
            {
                string s = Convert.ToString(args[0]);
                if (!string.IsNullOrEmpty(s))
                {
                    string[] arr = s.Split(Pd.Product.ImageSplitChar);
                    if (arr.Length > 0)
                        return arr[0];
                }
                return string.Empty;
            });
            this["GetStateInfo"] = new FuncHandler((args) =>
            {
                string[] ServerState = { "申请售后", "等待邮寄", "处理中", "完成售后", "申请失败", "取消" };
                int s = Convert.ToInt32(args[0]);
                return ServerState[s];
            });
            this["GetTypeInfo"] = new FuncHandler((args) =>
            {
                string[] ServiceType = { "", "退货", "换货", "退款" };
                int s = Convert.ToInt32(args[0]);
                return ServiceType[s];
            });
            this["OrderList"] = A.AfterSalesRecord.GetAfterSalesListByMember(DataSource, User.Identity.Id, Math.Max(1, page), 10, title, nickName, startCreateDate, endCreateDate, serverState, 8);
            Render("servicelist.html");
        }
        [Authorize(true)]
        public void AddService(string orderId, long productId)
        {
            Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, orderId);
            Pd.Supplier supplier = Pd.Supplier.GetById(DataSource,order.SupplierId);
            Pd.StoreInfo storeInfo = Pd.StoreInfo.GetStoreInfoByUserId(DataSource, order.SupplierId);
            this["Product"] = Pd.ProductOrderMapping.GetById(DataSource, orderId, productId);
            this["PrOrder"] = order;
            this["Order"] = order;
            this["IsRetreatFreightAmount"] = A.AfterSalesRecord.IsRetreatFreightAmount(DataSource, orderId);
            this["ShopName"] = storeInfo != null ? storeInfo.StoreName : supplier.Company;
            this["TimeNow"] = DateTime.Now.ToString("");
            this["SubStr"] = new FuncHandler((args)=> 
            {
                if (args[0] != null)
                {
                    string s = Convert.ToString(args[0]);
                    if (s.Length>11)
                    {
                        s = Convert.ToString(args[0]).Substring(0, 11);
                    }
                    return s;
                }
                else
                    return "";
            });
            Render("addservice.html");
        }
        [Authorize(true)]
        public void ShowInfo(string orderId)
        {
            A.AfterSalesRecord aftersalesrecord = A.AfterSalesRecord.GetById(DataSource, orderId);
            Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, aftersalesrecord.OrderId);
            Pd.Supplier supplier = Pd.Supplier.GetById(DataSource, order.SupplierId);
            Pd.StoreInfo storeInfo = Pd.StoreInfo.GetStoreInfoByUserId(DataSource, order.SupplierId);
            this["GetStateInfo"] = new FuncHandler((args) =>
            {
                string[] ServerState = { "申请售后", "等待邮寄", "处理中", "完成售后", "申请失败", "取消" , "驳回" };
                int s = Convert.ToInt32(args[0]);
                return ServerState[s];
            });
            this["SubStr"] = new FuncHandler((args) =>
            {
                if (args[0] != null)
                {
                    string s = Convert.ToString(args[0]);
                    if (s.Length > 11)
                    {
                        s = Convert.ToString(args[0]).Substring(0, 11);
                    }
                    return s;
                }
                else
                    return "";
            });
            this["PrOrder"] = order;
            this["ShopName"] = storeInfo != null ? storeInfo.StoreName : supplier.Company;
            if (aftersalesrecord != null)
            {
                if (aftersalesrecord.ServerState == A.AfterSalesRecord.EServiceState.Process && aftersalesrecord.ServiceType != A.AfterSalesRecord.EServiceType.Refund)
                    this["Logistics"] = Pd.ProductLogistics.GetByOrder(DataSource, aftersalesrecord.Id);
                this["Product"] = Pd.ProductOrderMapping.GetById(DataSource, aftersalesrecord.OrderId, aftersalesrecord.ProductId);
                this["Order"] = aftersalesrecord;
                Render("serviceinfo.html");
            }
            else
            {
                NotFound();
            }
        }
        [Authorize(true)]
        public void DoDelivery(string orderid)
        {
            if (IsPost)
            {
                DataSource.Begin();
                try
                {
                    Pd.ProductLogistics value = DbTable.Load<Pd.ProductLogistics>(Request.Form);
                    value.OrderId = orderid;
                    if (Pd.ProductLogistics.GetByOrder(DataSource, value.OrderId) == null)
                    {
                        if (value.Insert(DataSource) != DataStatus.Success)
                            throw new Exception();
                        Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, value.OrderId);
                        if (A.AfterSalesRecord.ProcessAfterSales(DataSource, value.OrderId) != DataStatus.Success)
                            throw new Exception();
                        DataSource.Commit();
                        SetResult(true);
                    }
                    else
                    {
                        if (value.Update(DataSource) != DataStatus.Success)
                            throw new Exception();
                        DataSource.Commit();
                        SetResult(true);
                    }
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
        }
        [Authorize(true)]
        public void Submit(string orderid, long productid)
        {
            DataSource.Begin();
            try
            {
                Pd.ProductOrder productorder = Pd.ProductOrder.GetById(DataSource, orderid);
                if (productorder != null)
                {
                    Pd.ProductOrderMapping ordermapping = Pd.ProductOrderMapping.GetById(DataSource, orderid, productid);
                    if (productid > 0 && ordermapping != null)
                    {
                        if (!ordermapping.IsService)
                        {
                            if (productorder.RefundDate.AddDays(7) <= DateTime.Now && productorder.State == Pd.OrderState.Finished)
                            {
                                SetResult(-1054);
                                throw new AggregateException();
                            }
                            else
                            {
                                A.AfterSalesRecord aftersales = new A.AfterSalesRecord();
                                aftersales.OrderId = orderid;
                                aftersales.ProductId = productid;
                                aftersales.ServiceType = (A.AfterSalesRecord.EServiceType)Enum.Parse(TType<A.AfterSalesRecord.EServiceType>.Type, Request["ServiceType"], true);
                                if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.Refund)
                                {
                                    aftersales.RefundCount = ordermapping.Count;
                                    aftersales.RefundMoney = Money.Parse(Request["RefundMoney"]);
                                }
                                else if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                {
                                    aftersales.RefundMoney = Money.Parse(Request["RefundMoney"]);
                                    aftersales.RefundCount = int.Parse(Request["RefundCount"]);
                                }
                                else
                                {
                                    aftersales.RefundCount = int.Parse(Request["RefundCount"]);
                                    aftersales.RefundMoney = (ordermapping.TotalMoney / ordermapping.Count) * aftersales.RefundCount;
                                }
                                if (aftersales.RefundMoney > ordermapping.TotalMoney)
                                {
                                    SetResult(-1046);
                                    throw new AggregateException();
                                }
                                if (aftersales.RefundCount > ordermapping.Count)
                                {
                                    SetResult(-1047);
                                    throw new AggregateException();
                                }
                                aftersales.Reason = Request["Reason"];
                                aftersales.Image = Request["Image"];
                                aftersales.Message = Request["Message"];
                                aftersales.CreateDate = DateTime.Now;
                                aftersales.DealMoney = ordermapping.TotalMoney;
                                aftersales.Id = Pd.ProductOrder.NewId(DateTime.Now, User.Identity.Id);
                                aftersales.UserId = User.Identity.Id;
                                aftersales.SupplierId = productorder.SupplierId;
                                aftersales.ServerState = A.AfterSalesRecord.EServiceState.ApplySerice;
                                if (aftersales.ServiceType != A.AfterSalesRecord.EServiceType.ExchangeGoods && A.AfterSalesRecord.IsRetreatFreightAmount(DataSource, orderid) && !string.IsNullOrEmpty(Request.Form["IsRetreatFreightAmount"]) && Request.Form["IsRetreatFreightAmount"] == "on")
                                {
                                    aftersales.FreightAmount = productorder.FreightMoney;
                                }
                                if (aftersales.Insert(DataSource) == Cnaws.Web.DataStatus.Success)
                                {
                                    ordermapping.IsService = true;
                                    ordermapping.AfterSalesOrderId = aftersales.Id;
                                    if (ordermapping.Update(DataSource) == Cnaws.Web.DataStatus.Success)
                                    {
                                        DataSource.Commit();
                                        SetResult(true);
                                    }
                                    else
                                    {
                                        SetResult(-1019);
                                        throw new AggregateException();
                                    }
                                }
                                else
                                {
                                    SetResult(-1018);
                                    throw new AggregateException();
                                }
                            }
                        }
                        else
                        {
                            SetResult(-1043);
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        SetResult(-1026);
                        throw new AggregateException();
                    }
                }
                else
                {
                    SetResult(-1030);
                    throw new AggregateException();
                }

            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                return;
            }
            catch (Exception)
            {
                DataSource.Rollback();
                SetResult(-500);
            }
        }

        public void Count()
        {
            SetResult(A.AfterSalesRecord.Count(DataSource, User.Identity.Id));
        }
    }
}
