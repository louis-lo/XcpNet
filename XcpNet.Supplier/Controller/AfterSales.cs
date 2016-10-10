using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws;
using Cnaws.Web;
using A = XcpNet.AfterSales.Modules;
using Pd = Cnaws.Product.Modules;
using S = XcpNet.Supplier.Modules.Modules;
using Cnaws.Data.Query;
using Cnaws.Json;
using Cnaws.Area;
using Cnaws.Web.Templates;

namespace XcpNet.Supplier.Controllers
{
    public class AfterSales : SupplierController
    {

        /// <summary>
        /// 售后管理
        /// </summary>
        /// <param name="page"></param>
        public void DistributorList(long page = 1L)
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
            this["OrderList"] = A.AfterSalesRecord.GetAfterSalesListBySupplier(DataSource, User.Identity.Id, Math.Max(1, page), 10, title, nickName, startCreateDate, endCreateDate, serverState, 11, A.AfterSalesRecord.EChannel.WholesaleOrder);
            if (IsSupplier())
            {
                Render("supplier/distributoraftersales.html");
            }
            else
            {
                Render("distributoraftersaless.html");
            }

        }
        /// <summary>
        /// 售后管理
        /// </summary>
        /// <param name="page"></param>
        public void List(long page = 1L)
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
            this["OrderList"] = A.AfterSalesRecord.GetAfterSalesListBySupplier(DataSource, User.Identity.Id, Math.Max(1, page), 10, title, nickName, startCreateDate, endCreateDate, serverState, 11,A.AfterSalesRecord.EChannel.GoodsOrder);
            if (IsSupplier())
            {
                Render("supplier/aftersales.html");
            }
            else
            {
                Render("aftersales.html");
            }

        }
        /// <summary>
        /// 处理售后
        /// </summary>
        /// <param name="id"></param>
        public void AfterSalesDetailed(string id)
        {

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
            this["GetImages"] = new FuncHandler((args) =>
            {
                string s = Convert.ToString(args[0]);
                if (!string.IsNullOrEmpty(s))
                {
                    return s.Split(Pd.Product.ImageSplitChar);
                }
                return new string[] { };
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
            var afterSalesDetailed = A.AfterSalesRecord.GetAndProductAndMemberInfoById(DataSource, id);
            this["AfterSalesDetailed"] = afterSalesDetailed;
            this["Address"]= Pd.ProductOrder.GetById(DataSource, afterSalesDetailed.AfterSalesRecord_OrderId);
            this["GetCitys"] = new FuncHandler((args) =>
            {
                int s = Convert.ToInt32(args[0]);
                Cnaws.Area.Country country = Country.GetCountry();
                return country.GetCity(s).Name;
            });
            this["ReturnAddressList"] = S.ReturnAddress.GetAll(DataSource, User.Identity.Id);
            Pd.ProductLogistics log = Pd.ProductLogistics.GetByOrder(DataSource, afterSalesDetailed.AfterSalesRecord.Id);
            this["productLogistics"] = log;
            if (log != null)
            {
                if (string.IsNullOrEmpty(log.ProviderDetailed))
                {
                    string providerDetailed = Cnaws.Product.Logistics.ExpressQuery.QueryReturnJson(log.ProviderKey, log.BillNo);
                    if (!string.IsNullOrEmpty(providerDetailed))
                    {
                        Cnaws.Product.Logistics.ExpressInfo expressInfo = Cnaws.Product.Logistics.ExpressQuery.Query(providerDetailed);
                        this["ExpressInfo"] = expressInfo;
                        if (expressInfo.state == "3")
                        {
                            log.ProviderDetailed = providerDetailed;
                            Pd.ProductLogistics.UpdateProviderDetailed(DataSource, log);
                        }
                    }
                }
                else
                {
                    this["ExpressInfo"] = Cnaws.Product.Logistics.ExpressQuery.Query(log.ProviderDetailed);
                }
            }
            if (afterSalesDetailed.AfterSalesRecord.ServerState == 3 && afterSalesDetailed.AfterSalesRecord.ServiceType == 2 && !string.IsNullOrEmpty(afterSalesDetailed.AfterSalesRecord.NewOrderId))
            {
                this["NewProductOrder"] = Pd.ProductOrder.GetById(DataSource, afterSalesDetailed.AfterSalesRecord.NewOrderId);
            }
            if (IsSupplier())
            {
                Render("supplier/afterSalesDetailed.html");
            }
            else
            {
                Render("afterSalesDetailed.html");
            }

        }

        public void Detailed(string id)
        {

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
            this["GetImages"] = new FuncHandler((args) =>
            {
                string s = Convert.ToString(args[0]);
                if (!string.IsNullOrEmpty(s))
                {
                    return s.Split(Pd.Product.ImageSplitChar);
                }
                return new string[] { };
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
            var afterSalesDetailed = A.AfterSalesRecord.GetAndProductAndMemberInfoById(DataSource, id);
            this["AfterSalesDetailed"] = afterSalesDetailed;

            this["GetCitys"] = new FuncHandler((args) =>
            {
                int s = Convert.ToInt32(args[0]);
                Cnaws.Area.Country country = Country.GetCountry();
                return country.GetCity(s).Name;
            });
            this["ReturnAddressList"] = S.ReturnAddress.GetAll(DataSource, User.Identity.Id);
            Pd.ProductLogistics log = Pd.ProductLogistics.GetByOrder(DataSource, afterSalesDetailed.AfterSalesRecord.Id);
            this["productLogistics"] = log;
            if (log != null)
            {
                if (string.IsNullOrEmpty(log.ProviderDetailed))
                {
                    string providerDetailed = Cnaws.Product.Logistics.ExpressQuery.QueryReturnJson(log.ProviderKey, log.BillNo);
                    if (!string.IsNullOrEmpty(providerDetailed))
                    {
                        Cnaws.Product.Logistics.ExpressInfo expressInfo = Cnaws.Product.Logistics.ExpressQuery.Query(providerDetailed);
                        this["ExpressInfo"] = expressInfo;
                        if (expressInfo.state == "3")
                        {
                            log.ProviderDetailed = providerDetailed;
                            Pd.ProductLogistics.UpdateProviderDetailed(DataSource, log);
                        }
                    }
                }
                else
                {
                    this["ExpressInfo"] = Cnaws.Product.Logistics.ExpressQuery.Query(log.ProviderDetailed);
                }
            }
            if (afterSalesDetailed.AfterSalesRecord.ServerState == 3 && afterSalesDetailed.AfterSalesRecord.ServiceType == 2 && !string.IsNullOrEmpty(afterSalesDetailed.AfterSalesRecord.NewOrderId))
            {
                this["NewProductOrder"] = Pd.ProductOrder.GetById(DataSource, afterSalesDetailed.AfterSalesRecord.NewOrderId);
            }
            if (IsSupplier())
            {
                Render("supplier/afterSalesDetailed_Record.html");
            }
            else
            {
                Render("afterSalesDetailed_Record.html");
            }

        }
        /// <summary>
        /// 审核售后
        /// </summary>
        public void Examine()
        {
            string OrderId = Request["OrderId"];//售后订单
            int ExamineType = int.Parse(Request["ExamineType"]);
            if (ExamineType == 0)
            {
                ///驳回售后申请
                SetResult(A.AfterSalesRecord.RejectAfterSales(DataSource, OrderId, ""));
            }
            else if (ExamineType == 1)
            {
                ///审核成功并
                A.AfterSalesRecord aftersalesrecord = A.AfterSalesRecord.GetById(DataSource, OrderId);
                string AddressStr = "";
                if (aftersalesrecord.ServiceType != A.AfterSalesRecord.EServiceType.Refund)
                {
                    int Address = int.Parse(Request["Address"]);
                    S.ReturnAddress returnaddress = S.ReturnAddress.GetById(DataSource, Address, User.Identity.Id);
                    AddressStr = returnaddress.BuildInfo();
                    SetResult(A.AfterSalesRecord.UpdateStateForAddress(DataSource, OrderId, AddressStr));
                }
                else
                {
                    SetResult(A.AfterSalesRecord.CompleteAfterSales(DataSource, OrderId));
                }


            }
        }
        /// <summary>
        /// 拒绝售后
        /// </summary>
        public void FailSubmit()
        {
            string id = Request.Form["id"];
            string failMessage = Request.Form["failMessage"];
            SetResult(A.AfterSalesRecord.RejectAfterSales(DataSource, id, failMessage));
        }
        /// <summary>
        /// 退款
        /// </summary>
        public void Refund()
        {
            string id = Request.Form["id"];
            SetResult(A.AfterSalesRecord.Refund(DataSource, id));
        }
        public void UpdateStateForAddress()
        {
            string id = Request.Form["id"];
            int addressId;
            int.TryParse(Request.Form["address"], out addressId);
            S.ReturnAddress returnAddress = S.ReturnAddress.GetById(DataSource, addressId, User.Identity.Id);
            string address = returnAddress.BuildInfo();
            SetResult(A.AfterSalesRecord.UpdateStateForAddress(DataSource, id, address));
        }
        /// <summary>
        /// 确认收货
        /// </summary>
        public void Receipt()
        {
            string OrderId = Request["OrderId"];//售后订单
            SetResult(A.AfterSalesRecord.CompleteAfterSales(DataSource, OrderId));
        }
        /// <summary>
        /// 换货
        /// </summary>
        public void ExchangeGoods()
        {
            string id = Request.Form["id"];
            SetResult(A.AfterSalesRecord.CompleteAfterSales(DataSource, id));
        }
        [SupplierOrDistributor(true)]
        public void Status(string id)
        {
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
            this["GetImages"] = new FuncHandler((args) =>
            {
                string s = Convert.ToString(args[0]);
                if (!string.IsNullOrEmpty(s))
                {
                    return s.Split(Pd.Product.ImageSplitChar);
                }
                return new string[] { };
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
            var afterSalesDetailed = A.AfterSalesRecord.GetAndProductAndMemberInfoById(DataSource, id);
            this["AfterSalesDetailed"] = afterSalesDetailed;
            this["Address"] = Pd.ProductOrder.GetById(DataSource, afterSalesDetailed.AfterSalesRecord_OrderId);
            this["GetCitys"] = new FuncHandler((args) =>
            {
                int s = Convert.ToInt32(args[0]);
                Cnaws.Area.Country country = Country.GetCountry();
                return country.GetCity(s).Name;
            });
            this["ReturnAddressList"] = S.ReturnAddress.GetAll(DataSource, User.Identity.Id);
            Pd.ProductLogistics log = Pd.ProductLogistics.GetByOrder(DataSource, afterSalesDetailed.AfterSalesRecord.Id);
            this["productLogistics"] = log;
            if (log != null)
            {
                if (string.IsNullOrEmpty(log.ProviderDetailed))
                {
                    string providerDetailed = Cnaws.Product.Logistics.ExpressQuery.QueryReturnJson(log.ProviderKey, log.BillNo);
                    if (!string.IsNullOrEmpty(providerDetailed))
                    {
                        Cnaws.Product.Logistics.ExpressInfo expressInfo = Cnaws.Product.Logistics.ExpressQuery.Query(providerDetailed);
                        this["ExpressInfo"] = expressInfo;
                        if (expressInfo.state == "3")
                        {
                            log.ProviderDetailed = providerDetailed;
                            Pd.ProductLogistics.UpdateProviderDetailed(DataSource, log);
                        }
                    }
                }
                else
                {
                    this["ExpressInfo"] = Cnaws.Product.Logistics.ExpressQuery.Query(log.ProviderDetailed);
                }
            }
            if (afterSalesDetailed.AfterSalesRecord.ServerState == 3 && afterSalesDetailed.AfterSalesRecord.ServiceType == 2 && !string.IsNullOrEmpty(afterSalesDetailed.AfterSalesRecord.NewOrderId))
            {
                this["NewProductOrder"] = Pd.ProductOrder.GetById(DataSource, afterSalesDetailed.AfterSalesRecord.NewOrderId);
            }
            if (IsSupplier())
            {
                Render("supplier/afterSalesDetailed.html");
            }
            else
            {
                Render("afterSalesDetailed.html");
            }
        }
    }
}
