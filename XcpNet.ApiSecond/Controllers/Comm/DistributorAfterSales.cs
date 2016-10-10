using System;
using System.Collections.Generic;
using M = Cnaws.Passport.Modules;
using Pd = Cnaws.Product.Modules;
using A = XcpNet.AfterSales.Modules;
using D = XcpNet.Supplier.Modules.Modules;
using Cnaws.Data;
using Cnaws.Templates;
using Cnaws;
using Cnaws.Web;
using Cnaws.Data.Query;
using XcpNet.Common;

namespace XcpNet.ApiSecond.Controllers
{
    public class DistributorAfterSales2 : CommControllers2
    {
        public static string ClassName = "[type]DistributorAfterSales2";
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
                SetResult(A.AfterSalesRecord.GetInDistributorProductListByUser(DataSource, member.Id, page, size, 11,A.AfterSalesRecord.EChannel.WholesaleOrder));

            }
        }
#if (DEBUG)
        public static void GetAfterSalesListHelper()
        {
            CheckMemberApi(ClassName, "GetAfterSalesList", "获取当前用户售后列表")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(true, typeof(SplitPageData<DataJoin<A.AfterSalesRecord, D.DistributorOrderMapping>>), "返回结果,当前用户售后列表");
        }
#endif

        public void SetAfterSales()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    object code, data;
                    string message = Request["message"];
                    string image = Request["image"];
                    string order = Request.Form["orderid"];
                    long productid = long.Parse(Request.Form["productid"]);
                    string servicetype = Request.Form["servicetype"];
                    Money refundmoney = Money.Parse(Request.Form["refundmoney"]);
                    string refundcount = Request.Form["refundcount"];
                    string isfreightamount = Request.Form["isfreightamount"];
                    string reason=Request["reason"];
                    code = CommAfterSales.SetAfterSales<D.DistributorOrder>(DataSource, member, Request.Form["orderid"], long.Parse(Request.Form["productid"]), Request.Form["servicetype"], Money.Parse(Request.Form["refundmoney"]), Request.Form["refundcount"], Request.Form["isfreightamount"], Request["reason"], image, message, out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    SetResult(false, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void SetAfterSalesHelper()
        {
            CheckMemberApi(ClassName, "SetAfterSales", "申请售后服务")
                .AddArgument("orderid", typeof(string), "订单编号")
                .AddArgument("productid", typeof(long), "订单中产品的编号")
                .AddArgument("servicetype", typeof(string), "售后类型 退货:ReturnGoods,换货:ExchangeGoods,退款:Refund")
                .AddArgument("isfreightamount", typeof(bool), "是否包邮,true,false")
                .AddArgument("refundmoney", typeof(double), "类型为退货款时退款金额")
                .AddArgument("refundcount", typeof(int), "类型为换货时退还数量")
                .AddArgument("reason", typeof(string), "售后原因")
                .AddArgument("image", typeof(string), "图片凭证，多张用|隔开")
                .AddArgument("message", typeof(string), "售后说明")
                .AddResult(CommUtility.FREIGHT_RETURN_NOT, "该售后订单不能退邮费")
                .AddResult(CommUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(CommUtility.PRODUCT_SERVICED, "该产品已经提交过售后,请勿重复提交")
                .AddResult(CommUtility.PRODUCT_ERROR, "找不到订单中有该商品存在")
                .AddResult(CommUtility.ORDER_EMPTY, "找不到该订单")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(CommUtility.REFUND_EXCEED_COUND, "退款金额超过最可退款数额")
                .AddResult(CommUtility.REFUND_EXCEED_COUND, "退款金额超过最可退换货数量")
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
                    D.DistributorOrder productorder = D.DistributorOrder.GetById(DataSource, orderid);
                    if (productorder != null)
                    {
                        long productid = long.Parse(Request["ProductId"]);
                        D.DistributorOrderMapping ordermapping = D.DistributorOrderMapping.GetById(DataSource, orderid, productid);
                        if (ordermapping != null)
                        {
                            if (productorder.RefundDate.AddDays(7) <= DateTime.Now && productorder.State == Pd.OrderState.Finished)
                            {
                                SetResult(CommUtility.ALREADY_OVERDUE);
                                throw new AggregateException();
                            }
                            else
                            {
                                SetResult(true, new { IsFreightAmount = A.AfterSalesRecord.IsRetreatDistributorFreightAmount(DataSource, orderid) });
                            }
                        }
                        else
                        {
                            SetResult(CommUtility.PRODUCT_ERROR);
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        SetResult(CommUtility.ORDER_EMPTY);
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
            CheckMemberApi(ClassName, "IsApply", "查看是否能申请售后")
                .AddArgument("OrderId", typeof(string), "订单编号")
                .AddArgument("ProductId", typeof(long), "订单中产品的编号")
                .AddResult(CommUtility.ALREADY_OVERDUE, "已经逾期")
                .AddResult(CommUtility.PRODUCT_ERROR, "找不到订单中有该商品存在")
                .AddResult(CommUtility.ORDER_EMPTY, "找不到该订单")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "返回结果IsFreightAmount：是否可申请邮费退款bool");
        }
#endif
        public void ReturnDeliver()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    object code, data;
                    Pd.ProductLogistics value = DbTable.Load<Pd.ProductLogistics>(Request.Form);
                    code = CommAfterSales.ReturnDeliver(DataSource, member, value, out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    SetResult(false, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void ReturnDeliverHelper()
        {
            CheckMemberApi(ClassName, "ReturnDeliver", "退换货回寄发货")
                .AddArgument("OrderId", typeof(string), "订单编号")
                .AddArgument("ProviderKey", typeof(long), "物流公司Key")
                .AddArgument("ProviderName", typeof(string), "快递名称")
                .AddArgument("BillNo", typeof(int), "物流订单号")
                .AddResult(CommUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(CommUtility.UPDATE_FAIL, "修改数据失败")
                .AddResult(CommUtility.PROGRAM_ERROR, "PROGRAM_ERROR")
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
            CheckMemberApi(ClassName, "GetLogisticsCompany", "获取支持的物流公司")
                .AddResult(true, typeof(IList<Pd.LogisticsCompany>), "返回结果");
        }
#endif

        public void CancelAfterSales()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    object code, data;
                    code = CommAfterSales.CancelAfterSales(DataSource, member, Request.Form["OrderId"], out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    SetResult(false, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void CancelAfterSalesHelper()
        {
            CheckMemberApi(ClassName, "CancelAfterSales", "取消售后")
                .AddArgument("OrderId", typeof(string), "退换货订单号")
                .AddResult(true, typeof(IList<Pd.LogisticsCompany>), "返回结果");
        }
#endif
        public void GetAfterSalesByOrderId()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                SetResult(A.AfterSalesRecord.GetAndDistributorProductById(DataSource, Request["OrderId"]));
            }
        }
#if (DEBUG)
        public static void GetAfterSalesByOrderIdHelper()
        {
            CheckMemberApi(ClassName, "GetAfterSalesByOrderId", "根据订单获取售后信息")
                .AddArgument("OrderId", typeof(string), "退换货订单号")
                .AddResult(true, typeof(DataJoin<A.AfterSalesRecord, D.DistributorOrderMapping>), "返回结果");
        }
#endif

        public void RemindDelivery()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    object code, data;
                    code = CommAfterSales.RemindDelivery(DataSource, member, Request.Form["OrderId"], out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    SetResult(false, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void RemindDeliveryHelper()
        {
            CheckMemberApi(ClassName, "RemindDelivery", "提醒发货")
                .AddArgument("OrderId", typeof(string), "订单号")
                .AddResult(CommUtility.REMINDER_REPEAT, "今日已成功提醒过商家，请等待商家发货")
                .AddResult(CommUtility.ORDER_EMPTY, "订单不存在")
                .AddResult(true, typeof(int), "返回成功");
        }
#endif

    }
}
