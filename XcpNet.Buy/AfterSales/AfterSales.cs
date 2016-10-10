using System;
using System.Collections.Generic;
using Cnaws.Passport.Controllers;
using Cnaws.Data;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using D = XcpNet.Supplier.Modules.Modules;
using Cnaws;
using Cnaws.Web;
using A = XcpNet.AfterSales.Modules;
using Pd = Cnaws.Product.Modules;
using System.Reflection;
using Cnaws.Templates;
namespace XcpNet.Common
{
    public class CommAfterSales
    {
        /// <summary>
        /// 设置售后
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员信息</param>
        /// <param name="orderid">订单号</param>
        /// <param name="productid">产品编号</param>
        /// <param name="servicetype">售后类型</param>
        /// <param name="refundmoney">售后金额</param>
        /// <param name="refundcount">售后数量</param>
        /// <param name="isfreightamount">是否退邮费</param>
        /// <param name="reason">退款原因</param>
        /// <param name="image">凭证图片</param>
        /// <param name="message">用户留言</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object SetAfterSales<T>(DataSource DataSource, M.Member member, string orderid, long productid, string servicetype, Money refundmoney, string refundcount, string isfreightamount, string reason, string image, string message, out object data) where T : class, new()
        {
            object code = null;
            data = null;
            DataSource.Begin();
            try
            {
                T t = new T();
                if (t is D.DistributorOrder)
                {
                    D.DistributorOrder productorder = D.DistributorOrder.GetById(DataSource, orderid);
                    if (productorder != null)
                    {
                        D.DistributorOrderMapping ordermapping = D.DistributorOrderMapping.GetById(DataSource, orderid, productid);
                        if (productid > 0 && ordermapping != null)
                        {
                            if (!ordermapping.IsService)
                            {
                                if (productorder.RefundDate.AddDays(7) <= DateTime.Now && productorder.State == Pd.OrderState.Finished)
                                {
                                    code = CommUtility.ALREADY_OVERDUE;
                                    throw new AggregateException();
                                }
                                else
                                {
                                    A.AfterSalesRecord aftersales = new A.AfterSalesRecord();
                                    aftersales.OrderId = orderid;
                                    aftersales.ProductId = productid;
                                    aftersales.ServiceType = (A.AfterSalesRecord.EServiceType)Enum.Parse(TType<A.AfterSalesRecord.EServiceType>.Type, servicetype, true);
                                    aftersales.Channel = A.AfterSalesRecord.EChannel.WholesaleOrder;
                                    if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.Refund)
                                    {
                                        aftersales.RefundCount = ordermapping.Count;
                                        aftersales.RefundMoney = refundmoney;
                                    }
                                    else if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                    {
                                        aftersales.RefundMoney = refundmoney;
                                        if (!int.TryParse(refundcount, out aftersales.RefundCount))
                                        {
                                            aftersales.RefundCount = ordermapping.Count;
                                        }
                                    }
                                    else
                                    {
                                        aftersales.RefundCount = int.Parse(refundcount);
                                        aftersales.RefundMoney = (ordermapping.TotalMoney / ordermapping.Count) * aftersales.RefundCount;
                                    }
                                    if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                    {
                                        if (aftersales.RefundMoney > (ordermapping.TotalMoney / ordermapping.Count) * aftersales.RefundCount)
                                        {
                                            code = CommUtility.REFUND_EXCEED_MONEY;
                                            throw new AggregateException();
                                        }
                                        else if (aftersales.RefundCount > ordermapping.Count)
                                        {
                                            code = CommUtility.REFUND_EXCEED_COUND;
                                            throw new AggregateException();
                                        }

                                    }
                                    else if (aftersales.RefundMoney > ordermapping.TotalMoney)
                                    {
                                        if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.Refund || aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                        {
                                            code = CommUtility.REFUND_EXCEED_MONEY;
                                            throw new AggregateException();
                                        }
                                        else
                                        {
                                            code = CommUtility.REFUND_EXCEED_COUND;
                                            throw new AggregateException();
                                        }

                                    }
                                    if (aftersales.RefundCount > ordermapping.Count)
                                    {
                                        if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.Refund || aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                        {
                                            code = CommUtility.REFUND_EXCEED_MONEY;
                                            throw new AggregateException();
                                        }
                                        else
                                        {
                                            code = CommUtility.REFUND_EXCEED_COUND;
                                            throw new AggregateException();
                                        }
                                    }
                                    bool IsRetreatFreightAmount = false;
                                    bool.TryParse(isfreightamount, out IsRetreatFreightAmount);
                                    bool IsFreight = A.AfterSalesRecord.IsRetreatFreightAmount(DataSource, orderid);
                                    if (aftersales.ServiceType != A.AfterSalesRecord.EServiceType.ExchangeGoods && IsFreight && IsRetreatFreightAmount)
                                    {
                                        aftersales.FreightAmount = productorder.FreightMoney;
                                    }
                                    else if (IsRetreatFreightAmount && !IsFreight)
                                    {
                                        code = CommUtility.FREIGHT_RETURN_NOT;
                                        throw new AggregateException();
                                    }
                                    aftersales.Reason = reason;
                                    aftersales.Image = image;
                                    aftersales.Message = message;
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
                                            code = CommUtility.SUCCESS;
                                            data = aftersales.Id;
                                            return code;
                                        }
                                        else
                                        {
                                            code = CommUtility.UPDATE_FAIL;
                                            throw new AggregateException();
                                        }
                                    }
                                    else
                                    {
                                        code = CommUtility.INSERT_FAIL;
                                        throw new AggregateException();
                                    }
                                }
                            }
                            else
                            {
                                code = CommUtility.PRODUCT_SERVICED;
                                throw new AggregateException();
                            }
                        }
                        else
                        {
                            code = CommUtility.PRODUCT_ERROR;
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        code = CommUtility.ORDER_EMPTY;
                        throw new AggregateException();
                    }
                }
                else if (t is P.ProductOrder)
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
                                    code = CommUtility.ALREADY_OVERDUE;
                                    throw new AggregateException();
                                }
                                else
                                {
                                    A.AfterSalesRecord aftersales = new A.AfterSalesRecord();
                                    aftersales.OrderId = orderid;
                                    aftersales.ProductId = productid;
                                    aftersales.ServiceType = (A.AfterSalesRecord.EServiceType)Enum.Parse(TType<A.AfterSalesRecord.EServiceType>.Type, servicetype, true);
                                    if (productorder.Channel == 1)
                                        aftersales.Channel = A.AfterSalesRecord.EChannel.GoodsOrder;
                                    if (productorder.Channel == 2)
                                        aftersales.Channel = A.AfterSalesRecord.EChannel.AgricultureOrder;
                                    if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.Refund)
                                    {
                                        aftersales.RefundCount = ordermapping.Count;
                                        aftersales.RefundMoney = refundmoney;
                                    }
                                    else if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                    {
                                        aftersales.RefundMoney = refundmoney;
                                        if (!int.TryParse(refundcount, out aftersales.RefundCount))
                                        {
                                            aftersales.RefundCount = ordermapping.Count;
                                        }
                                    }
                                    else
                                    {
                                        aftersales.RefundCount = int.Parse(refundcount);
                                        aftersales.RefundMoney = (ordermapping.TotalMoney / ordermapping.Count) * aftersales.RefundCount;
                                    }
                                    if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                    {
                                        if (aftersales.RefundMoney > (ordermapping.TotalMoney / ordermapping.Count) * aftersales.RefundCount)
                                        {
                                            code = CommUtility.REFUND_EXCEED_MONEY;
                                            throw new AggregateException();
                                        }
                                        else if (aftersales.RefundCount > ordermapping.Count)
                                        {
                                            code = CommUtility.REFUND_EXCEED_COUND;
                                            throw new AggregateException();
                                        }

                                    }
                                    else if (aftersales.RefundMoney > ordermapping.TotalMoney)
                                    {
                                        if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.Refund || aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                        {
                                            code = CommUtility.REFUND_EXCEED_MONEY;
                                            throw new AggregateException();
                                        }
                                        else
                                        {
                                            code = CommUtility.REFUND_EXCEED_COUND;
                                            throw new AggregateException();
                                        }

                                    }
                                    if (aftersales.RefundCount > ordermapping.Count)
                                    {
                                        if (aftersales.ServiceType == A.AfterSalesRecord.EServiceType.Refund || aftersales.ServiceType == A.AfterSalesRecord.EServiceType.ReturnGoods)
                                        {
                                            code = CommUtility.REFUND_EXCEED_MONEY;
                                            throw new AggregateException();
                                        }
                                        else
                                        {
                                            code = CommUtility.REFUND_EXCEED_COUND;
                                            throw new AggregateException();
                                        }
                                    }
                                    bool IsRetreatFreightAmount = false;
                                    bool.TryParse(isfreightamount, out IsRetreatFreightAmount);
                                    bool IsFreight = A.AfterSalesRecord.IsRetreatFreightAmount(DataSource, orderid);
                                    if (aftersales.ServiceType != A.AfterSalesRecord.EServiceType.ExchangeGoods && IsFreight && IsRetreatFreightAmount)
                                    {
                                        aftersales.FreightAmount = productorder.FreightMoney;
                                    }
                                    else if (IsRetreatFreightAmount && !IsFreight)
                                    {
                                        code = CommUtility.FREIGHT_RETURN_NOT;
                                        throw new AggregateException();
                                    }
                                    aftersales.Reason = reason;
                                    aftersales.Image = image;
                                    aftersales.Message = message;
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
                                            code = CommUtility.SUCCESS;
                                            data = aftersales.Id;
                                            return code;
                                        }
                                        else
                                        {
                                            code = CommUtility.UPDATE_FAIL;
                                            throw new AggregateException();
                                        }
                                    }
                                    else
                                    {
                                        code = CommUtility.INSERT_FAIL;
                                        throw new AggregateException();
                                    }
                                }
                            }
                            else
                            {
                                code = CommUtility.PRODUCT_SERVICED;
                                throw new AggregateException();
                            }
                        }
                        else
                        {
                            code = CommUtility.PRODUCT_ERROR;
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        code = CommUtility.ORDER_EMPTY;
                        throw new AggregateException();
                    }
                }
                else
                {
                    data = null;
                    return CommUtility.PROGRAM_ERROR;
                }
            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                return code;
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }

        /// <summary>
        /// 售后退换货发货
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员信息</param>
        /// <param name="value">物流信息</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object ReturnDeliver(DataSource DataSource, M.Member member, Pd.ProductLogistics value, out object data)
        {
            object code = null;
            data = null;
            DataSource.Begin();
            try
            {
                if (value.Insert(DataSource) == DataStatus.Success)
                {
                    if (A.AfterSalesRecord.ProcessAfterSales(DataSource, value.OrderId) == DataStatus.Success)
                    {
                        DataSource.Commit();
                        code = CommUtility.SUCCESS;
                        return code;
                    }
                    else
                    {
                        code = CommUtility.UPDATE_FAIL;
                        throw new AggregateException();
                    }
                }
                else
                {
                    code = CommUtility.INSERT_FAIL;
                    throw new AggregateException();
                }
            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                return code;
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }

        public static object CancelAfterSales(DataSource DataSource, M.Member member, string orderid, out object data)
        {
            object code = null;
            data = null;
            DataSource.Begin();
            try
            {
                if (A.AfterSalesRecord.CloseAfterSales(DataSource, orderid) != DataStatus.Success)
                {
                    code = CommUtility.UPDATE_FAIL;
                    throw new AggregateException();
                }
                DataSource.Commit();
                code = CommUtility.SUCCESS;
                return code;
            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                return code;
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }

        public static object RemindDelivery(DataSource DataSource, M.Member member, string orderid, out object data)
        {
            object code = null;
            data = null;
            DataSource.Begin();
            try
            {
                Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, orderid);
                if (order != null && order.UserId == member.Id)
                {
                    if (A.ReminderDelivery.SetRemind(DataSource, order.UserId, order.SupplierId, order.Id) == DataStatus.Success)
                    {
                        code = CommUtility.SUCCESS;
                        DataSource.Commit();
                        return code;
                    }
                    else
                    {
                        code = CommUtility.REMINDER_REPEAT;
                        throw new AggregateException();
                    }
                }
                else
                {
                    D.DistributorOrder neworder = D.DistributorOrder.GetById(DataSource, orderid);
                    if (neworder != null && neworder.UserId == member.Id)
                    {
                        if (A.ReminderDelivery.SetRemind(DataSource, neworder.UserId, neworder.SupplierId, neworder.Id) == DataStatus.Success)
                        {
                            code = CommUtility.SUCCESS;
                            DataSource.Commit();
                            return code;
                        }
                        else
                        {
                            code = CommUtility.REMINDER_REPEAT;
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        code = CommUtility.ORDER_EMPTY;
                        throw new AggregateException();
                    }
                }
            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                return code;
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }
    }
}
