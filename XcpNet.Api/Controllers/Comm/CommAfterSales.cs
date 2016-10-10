using System;
using System.Collections.Generic;
using M = Cnaws.Passport.Modules;
using Pd = Cnaws.Product.Modules;
using A = XcpNet.AfterSales.Modules;
using Cnaws.Data;
using Cnaws.Templates;
using Cnaws;
using Cnaws.Web;
using Cnaws.Data.Query;

namespace XcpNet.Api.Controllers
{
    public class CommAfterSales : CommonControllers
    {
        public static string ClassName = "[type]AfterSales";
        protected override void OnInitController()
        {
            NotFound();
        }

        public void GetAfterSalesList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                int size, page;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                if (!int.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                SetResult(A.AfterSalesRecord.GetInProductListByUser(DataSource, member.Id, page, size,11));

            }
        }
#if (DEBUG)
        public static void GetAfterSalesListHelper()
        {
            CheckMemberHelper(ClassName, "GetAfterSalesList", "获取当前用户售后列表")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(true, typeof(SplitPageData<DataJoin<A.AfterSalesRecord, Pd.ProductOrderMapping>>), "返回结果,当前用户售后列表");
        }
#endif

        public void SetAfterSales()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                DataSource.Begin();
                try
                {
                    string orderid = Request["OrderId"];
                    Pd.ProductOrder productorder = Pd.ProductOrder.GetById(DataSource, orderid);
                    if (productorder != null)
                    {
                        long productid = long.Parse(Request["ProductId"]);
                        Pd.ProductOrderMapping ordermapping = Pd.ProductOrderMapping.GetById(DataSource, orderid, productid);
                        if (productid > 0 && ordermapping != null)
                        {
                            if (!ordermapping.IsService)
                            {
                                if (productorder.RefundDate.AddDays(7) <= DateTime.Now && productorder.State == Pd.OrderState.Finished)
                                {
                                    SetResult(ApiUtility.ALREADY_OVERDUE);
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
                                        if (!int.TryParse(Request["RefundCount"], out aftersales.RefundCount))
                                        {
                                            aftersales.RefundCount = ordermapping.Count;
                                        }
                                    }
                                    else
                                    {
                                        aftersales.RefundCount = int.Parse(Request["RefundCount"]);
                                        aftersales.RefundMoney = (ordermapping.TotalMoney / ordermapping.Count) * aftersales.RefundCount;
                                    }
                                    if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                    {
                                        if (aftersales.RefundMoney > (ordermapping.TotalMoney / ordermapping.Count) * aftersales.RefundCount)
                                        {
                                            SetResult(ApiUtility.REFUND_EXCEED_MONEY);
                                            throw new AggregateException();
                                        }
                                        else if (aftersales.RefundCount > ordermapping.Count)
                                        {
                                            SetResult(ApiUtility.REFUND_EXCEED_COUND);
                                            throw new AggregateException();
                                        }

                                    }
                                    else if (aftersales.RefundMoney > ordermapping.TotalMoney)
                                    {
                                        if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.Refund || aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                        {
                                            SetResult(ApiUtility.REFUND_EXCEED_MONEY);
                                            throw new AggregateException();
                                        }
                                        else
                                        {
                                            SetResult(ApiUtility.REFUND_EXCEED_COUND);
                                            throw new AggregateException();
                                        }

                                    }
                                    if (aftersales.RefundCount > ordermapping.Count)
                                    {
                                        if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.Refund || aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                        {
                                            SetResult(ApiUtility.REFUND_EXCEED_MONEY);
                                            throw new AggregateException();
                                        }
                                        else
                                        {
                                            SetResult(ApiUtility.REFUND_EXCEED_COUND);
                                            throw new AggregateException();
                                        }
                                    }
                                    bool IsRetreatFreightAmount = false;
                                    bool.TryParse(Request["IsFreightAmount"], out IsRetreatFreightAmount);
                                    bool IsFreight = A.AfterSalesRecord.IsRetreatFreightAmount(DataSource, orderid);
                                    if (aftersales.ServiceType != A.AfterSalesRecord.EServiceType.ExchangeGoods && IsFreight && IsRetreatFreightAmount)
                                    {
                                        aftersales.FreightAmount = productorder.FreightMoney;
                                    }
                                    else if (IsRetreatFreightAmount && !IsFreight)
                                    {
                                        SetResult(ApiUtility.FREIGHT_RETURN_NOT);
                                        throw new AggregateException();
                                    }
                                    aftersales.Reason = Request["Reason"];
                                    aftersales.Image = Request["Image"];
                                    aftersales.Message = Request["Message"];
                                    aftersales.CreateDate = DateTime.Now;
                                    aftersales.DealMoney = ordermapping.TotalMoney;
                                    aftersales.Id = Pd.ProductOrder.NewId(DateTime.Now, member.Id);
                                    aftersales.UserId = member.Id;
                                    aftersales.SupplierId = productorder.SupplierId;
                                    aftersales.ServerState = A.AfterSalesRecord.EServiceState.ApplySerice;
                                    if (aftersales.Insert(DataSource) == Cnaws.Web.DataStatus.Success)
                                    {
                                        ordermapping.IsService = true;
                                        ordermapping.AfterSalesOrderId = aftersales.Id;
                                        if (ordermapping.Update(DataSource) == Cnaws.Web.DataStatus.Success)
                                        {
                                            DataSource.Commit();
                                            SetResult(true, aftersales.Id);
                                        }
                                        else
                                        {
                                            SetResult(ApiUtility.UPDATE_FAIL);
                                            throw new AggregateException();
                                        }
                                    }
                                    else
                                    {
                                        SetResult(ApiUtility.INSERT_FAIL);
                                        throw new AggregateException();
                                    }

                                }
                            }
                            else
                            {
                                SetResult(ApiUtility.PRODUCT_SERVICED);
                                throw new AggregateException();
                            }
                        }
                        else
                        {
                            SetResult(ApiUtility.PRODUCT_ERROR);
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        SetResult(ApiUtility.ORDER_EMPTY);
                        throw new AggregateException();
                    }

                }
                catch (AggregateException)
                {
                    DataSource.Rollback();
                    return;
                }
                catch (Exception ex)
                {
                    DataSource.Rollback();
                    SetResult(ApiUtility.PROGRAM_ERROR, ex.ToString());
                }
            }
        }
#if (DEBUG)
        public static void SetAfterSalesHelper()
        {
            CheckMemberHelper(ClassName, "SetAfterSales", "申请售后服务")
                .AddArgument("OrderId", typeof(string), "订单编号")
                .AddArgument("ProductId", typeof(long), "订单中产品的编号")
                .AddArgument("ServiceType", typeof(string), "售后类型 退货:ReturnGoods,换货:ExchangeGoods,退款:Refund")
                .AddArgument("IsFreightAmount", typeof(bool), "是否包邮,true,false")
                .AddArgument("RefundMoney", typeof(double), "类型为退货款时退款金额")
                .AddArgument("RefundCount", typeof(int), "类型为换货时退还数量")
                .AddArgument("Reason", typeof(string), "售后原因")
                .AddArgument("Image", typeof(string), "图片凭证，多张用|隔开")
                .AddArgument("Message", typeof(string), "售后说明")
                .AddResult(ApiUtility.FREIGHT_RETURN_NOT, "该售后订单不能退邮费")
                .AddResult(ApiUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(ApiUtility.PRODUCT_SERVICED, "该产品已经提交过售后,请勿重复提交")
                .AddResult(ApiUtility.PRODUCT_ERROR, "找不到订单中有该商品存在")
                .AddResult(ApiUtility.ORDER_EMPTY, "找不到该订单")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.REFUND_EXCEED_COUND, "退款金额超过最可退款数额")
                .AddResult(ApiUtility.REFUND_EXCEED_COUND, "退款金额超过最可退换货数量")
                .AddResult(true, typeof(string), "返回结果,是否成功");
        }
#endif


        public void IsApply()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    string orderid = Request["OrderId"];
                    Pd.ProductOrder productorder = Pd.ProductOrder.GetById(DataSource, orderid);
                    if (productorder != null)
                    {
                        long productid = long.Parse(Request["ProductId"]);
                        Pd.ProductOrderMapping ordermapping = Pd.ProductOrderMapping.GetById(DataSource, orderid, productid);
                        if (ordermapping != null)
                        {
                            if (productorder.RefundDate.AddDays(7) <= DateTime.Now && productorder.State == Pd.OrderState.Finished)
                            {
                                SetResult(ApiUtility.ALREADY_OVERDUE);
                                throw new AggregateException();
                            }
                            else
                            {
                                SetResult(true, new { IsFreightAmount = A.AfterSalesRecord.IsRetreatFreightAmount(DataSource, orderid) });
                            }
                        }
                        else
                        {
                            SetResult(ApiUtility.PRODUCT_ERROR);
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        SetResult(ApiUtility.ORDER_EMPTY);
                        throw new AggregateException();
                    }
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void IsApplyHelper()
        {
            CheckMemberHelper(ClassName, "IsApply", "查看是否能申请售后")
                .AddArgument("OrderId", typeof(string), "订单编号")
                .AddArgument("ProductId", typeof(long), "订单中产品的编号")
                .AddResult(ApiUtility.ALREADY_OVERDUE, "已经逾期")
                .AddResult(ApiUtility.PRODUCT_ERROR, "找不到订单中有该商品存在")
                .AddResult(ApiUtility.ORDER_EMPTY, "找不到该订单")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "返回结果IsFreightAmount：是否可申请邮费退款bool");
        }
#endif
        public void ReturnDeliver()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                DataSource.Begin();
                try
                {
                    Pd.ProductLogistics value = DbTable.Load<Pd.ProductLogistics>(Request.Form);
                    if (value.Insert(DataSource) == DataStatus.Success)
                    {
                        if (A.AfterSalesRecord.ProcessAfterSales(DataSource, value.OrderId) == DataStatus.Success)
                        {
                            DataSource.Commit();
                            SetResult(true);
                        }
                        else
                            throw new Exception();
                    }
                    else
                    {
                        DataSource.Rollback();
                        SetResult(ApiUtility.INSERT_FAIL);
                    }
                }
                catch (Exception)
                {
                    DataSource.Rollback();
                    SetResult(ApiUtility.UPDATE_FAIL);
                }
            }
        }
#if (DEBUG)
        public static void ReturnDeliverHelper()
        {
            CheckMemberHelper(ClassName, "ReturnDeliver", "退换货回寄发货")
                .AddArgument("OrderId", typeof(string), "订单编号")
                .AddArgument("ProviderKey", typeof(long), "物流公司Key")
                .AddArgument("ProviderName", typeof(string), "快递名称")
                .AddArgument("BillNo", typeof(int), "物流订单号")
                .AddResult(ApiUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(ApiUtility.UPDATE_FAIL, "修改数据失败")
                .AddResult(ApiUtility.PROGRAM_ERROR, "PROGRAM_ERROR")
                .AddResult(true, typeof(string), "返回结果,是否成功");
        }
#endif

        public void GetLogisticsCompany()
        {
            string mark;
            if (CheckMark(out mark))
            {
                SetResult(Pd.LogisticsCompany.GetAll(DataSource));
            }
        }
#if (DEBUG)
        public static void GetLogisticsCompanyHelper()
        {
            CheckMarkHelper(ClassName, "GetLogisticsCompany", "获取支持的物流公司")
                .AddResult(true, typeof(IList<Pd.LogisticsCompany>), "返回结果");
        }
#endif

        public void CancelAfterSales()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                DataSource.Begin();
                try
                {
                    if (A.AfterSalesRecord.CloseAfterSales(DataSource, Request["OrderId"]) != DataStatus.Success)
                        throw new Exception();
                    DataSource.Commit();
                    SetResult(true);
                }
                catch (Exception)
                {
                    DataSource.Rollback();
                    SetResult(ApiUtility.UPDATE_FAIL);
                }
            }
        }
#if (DEBUG)
        public static void CancelAfterSalesHelper()
        {
            CheckMarkHelper(ClassName, "CancelAfterSales", "取消售后")
                .AddArgument("OrderId", typeof(string), "退换货订单号")
                .AddResult(true, typeof(IList<Pd.LogisticsCompany>), "返回结果");
        }
#endif
        public void GetAfterSalesByOrderId()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                SetResult(A.AfterSalesRecord.GetAndProductById(DataSource, Request["OrderId"]));
            }
        }
#if (DEBUG)
        public static void GetAfterSalesByOrderIdHelper()
        {
            CheckMemberHelper(ClassName, "GetAfterSalesByOrderId", "根据订单获取售后信息")
                .AddArgument("OrderId", typeof(string), "退换货订单号")
                .AddResult(true, typeof(DataJoin<A.AfterSalesRecord, Pd.ProductOrderMapping>), "返回结果");
        }
#endif

        public void RemindDelivery()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, Request["OrderId"]);
                if (order != null && order.UserId == member.Id)
                {
                    if (A.ReminderDelivery.SetRemind(DataSource, order.UserId, order.SupplierId, order.Id) == DataStatus.Success)
                        SetResult(true);
                    else
                        SetResult(ApiUtility.REMINDER_REPEAT);
                }
                else
                {
                    SetResult(ApiUtility.ORDER_EMPTY);
                }
            }
        }
#if (DEBUG)
        public static void RemindDeliveryHelper()
        {
            CheckMemberHelper(ClassName, "RemindDelivery", "提醒发货")
                .AddArgument("OrderId", typeof(string), "订单号")
                .AddResult(ApiUtility.REMINDER_REPEAT, "今日已成功提醒过商家，请等待商家发货")
                .AddResult(ApiUtility.ORDER_EMPTY, "订单不存在")
                .AddResult(true, typeof(int), "返回成功");
        }
#endif

    }
}
