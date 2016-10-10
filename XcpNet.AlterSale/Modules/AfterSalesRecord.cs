using Cnaws.Data;
using Cnaws.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Web;
using Cnaws;
using Cnaws.Data.Query;
using Pd = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using Di = XcpNet.Supplier.Modules.Modules;

namespace XcpNet.AfterSales.Modules
{
    /// <summary>
    /// 售后服务记录--调用大量UpdateAfter
    /// </summary>
    [Serializable]
    public class AfterSalesRecord : NoIdentityModule
    {
        public enum EServiceState
        {
            /// <summary>
            /// 申请售后
            /// </summary>
            ApplySerice = 0,
            /// <summary>
            /// 等待邮寄
            /// </summary>
            WaitingMail,
            /// <summary>
            /// 处理中
            /// </summary>
            Process,
            /// <summary>
            /// 完成售后
            /// </summary>
            Complete,
            /// <summary>
            /// 申请失败
            /// </summary>
            Fail,
            /// <summary>
            /// 取消
            /// </summary>
            Cancel,
            /// <summary>
            /// 驳回
            /// </summary>
            Reject,
            /// <summary>
            /// 等待退款 只在退款、退货售后中存在
            /// </summary>
            WaitingRefund
        }

        public enum EChannel
        {
            /// <summary>
            /// 产品订单表（城品惠）
            /// </summary>
            GoodsOrder = 0,
            /// <summary>
            /// 批发订单表（进货宝）
            /// </summary>
            WholesaleOrder,
            /// <summary>
            /// 农产品订单表(乡道馆)
            /// </summary>
            AgricultureOrder

        }
        public string GetStateInfo()
        {
            switch (ServerState)
            {
                case EServiceState.ApplySerice: return "待审核";
                case EServiceState.WaitingMail: return "待邮寄";
                case EServiceState.Process: return "处理中";
                case EServiceState.Complete: return "已完成";
                case EServiceState.Fail: return "申请失败";
                case EServiceState.Cancel: return "已取消";
            }
            return "错误的状态";
        }
        public static string GetStateInfo(int value)
        {
            switch (value)
            {
                case (int)EServiceState.ApplySerice: return "待审核";
                case (int)EServiceState.WaitingMail: return "待邮寄";
                case (int)EServiceState.Process: return "处理中";
                case (int)EServiceState.Complete: return "已完成";
                case (int)EServiceState.Fail: return "申请失败";
                case (int)EServiceState.Cancel: return "已取消";
                case (int)EServiceState.Reject: return "已驳回";
            }
            return "全部状态";
        }

        public string GetTypeInfo()
        {
            switch (ServiceType)
            {
                case EServiceType.ReturnGoods: return "退货";
                case EServiceType.ExchangeGoods: return "换货";
                case EServiceType.Refund: return "退款";
            }
            return "错误的类型";
        }
        public static string GetTypeInfo(int value)
        {
            switch (value)
            {
                case (int)EServiceType.ReturnGoods: return "退货";
                case (int)EServiceType.ExchangeGoods: return "换货";
                case (int)EServiceType.Refund: return "退款";
            }
            return "全部类型";
        }

        public enum EServiceType
        {
            /// <summary>
            /// 退货
            /// </summary>
            ReturnGoods = 1,
            /// <summary>
            /// 换货
            /// </summary>
            ExchangeGoods,
            /// <summary>
            /// 退款
            /// </summary>
            Refund
        }
        /// <summary>
        /// 售后订单号
        /// </summary>
        [DataColumn(true, 36)]
        public string Id = null;
        /// <summary>
        /// 产品订单号
        /// </summary>
        [DataColumn(36)]
        public string OrderId = null;
        /// <summary>
        /// 换货新的订单号
        /// </summary>
        public string NewOrderId = null;
        /// <summary>
        /// 产品编号
        /// </summary>
        public long ProductId = 0L;
        /// <summary>
        /// 供应商Id
        /// </summary>
        public long SupplierId = 0L;
        /// <summary>
        /// 会员Id
        /// </summary>
        public long UserId = 0L;
        /// <summary>
        /// 成交金额
        /// </summary>
        public Money DealMoney = 0;
        /// <summary>
        /// 退款金额
        /// </summary>
        public Money RefundMoney = 0;
        /// <summary>
        /// 换货时换货个数
        /// </summary>
        public int RefundCount = 0;
        /// <summary>
        /// 退货原因
        /// </summary>
        [DataColumn(128)]
        public string Reason = null;
        /// <summary>
        /// 退货说明
        /// </summary>
        [DataColumn(512)]
        public string Message = null;
        /// <summary>
        /// 凭证
        /// </summary>
        [DataColumn(512)]
        public string Image = null;
        /// <summary>
        /// 回寄地址
        /// </summary>
        [DataColumn(512)]
        public string Address = null;
        /// <summary>
        /// 退货状态
        /// </summary>
        public EServiceState ServerState = EServiceState.ApplySerice;
        /// <summary>
        /// 售后类型
        /// </summary>
        public EServiceType ServiceType = EServiceType.ReturnGoods;
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);
        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTime ExamineDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);
        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime Completiontime = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);
        /// <summary>
        /// 驳回的原因
        /// </summary>
        [DataColumn(512)]
        public string FailMessage = null;
        /// <summary>
        /// 物流费用
        /// </summary>
        public Money FreightAmount = 0;
        ///// <summary>
        ///// 售后频道
        ///// </summary>
        public EChannel Channel = EChannel.GoodsOrder;


        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "UserId");
            DropIndex(ds, "OrderId");
            DropIndex(ds, "ProductId");
            DropIndex(ds, "SupplierId");
            DropIndex(ds, "UserIdProductId");
        }

        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "UserId", "UserId");
            CreateIndex(ds, "OrderId", "OrderId");
            CreateIndex(ds, "SupplierId", "SupplierId");
            CreateIndex(ds, "ProductId", "ProductId");
            CreateIndex(ds, "UserIdProductId", "UserId", "ProductId");
        }

        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (Channel == EChannel.WholesaleOrder)
            {
                Di.DistributorOrder order = Di.DistributorOrder.GetById(ds, OrderId);
                if (order.Payment == "cashondelivery")
                    return DataStatus.Failed;
            }
            else
            {
                Pd.ProductOrder order = Pd.ProductOrder.GetById(ds, OrderId);
                if (order.Payment == "cashondelivery")
                    return DataStatus.Failed;
            }
            return DataStatus.Success;
        }

        protected override DataStatus OnInsertAfter(DataSource ds)
        {
            if (Channel == EChannel.GoodsOrder || Channel == EChannel.AgricultureOrder)
            {
                if (ProfitRecord.ReduceProfitByOrder(ds, Id) != DataStatus.Success)
                {
                    return DataStatus.Exist;
                }
            }
            else if (Channel == EChannel.WholesaleOrder)
            {
                if (ProfitRecord.ReduceDistributorProfitByOrder(ds, Id) != DataStatus.Success)
                {
                    return DataStatus.Exist;
                }
            }
            return DataStatus.Success;
        }

        protected override DataStatus OnUpdateAfter(DataSource ds)
        {
            if (ServerState == EServiceState.Cancel || ServerState == EServiceState.Fail)
            {
                return ProfitRecord.UpdataState(ds, ProfitRecord.EProfitState.Invalid, Id);
            }
            else if (ServerState == EServiceState.Complete)
            {
                //修改退款记录状态   现在改为根据订单状态修改
                AfterSalesRecord record = GetById(ds, Id);
                ProfitRecord profit = ProfitRecord.GetByOrderId(ds, record.OrderId, record.SupplierId);
                if (profit != null)
                {
                    if (profit.ProfitState != ProfitRecord.EProfitState.Arrival)
                    {
                        if (ProfitRecord.UpdataState(ds, ProfitRecord.EProfitState.Arrival, Id) != DataStatus.Success)
                        {
                            return DataStatus.Failed;
                        }
                    }
                }
                record = GetById(ds, Id);

                if (Channel == EChannel.GoodsOrder || Channel == EChannel.AgricultureOrder)
                {
                    Pd.ProductOrder productorder = Pd.ProductOrder.GetById(ds, record.OrderId);

                    IList<Pd.ProductOrderMapping> ordermappings = productorder.GetMapping(ds);
                    ///只有一个产品  直接修改订单状态
                    if (ordermappings.Count == 1)
                    {
                        if (productorder.State == Pd.OrderState.Delivery)
                        {
                            if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Delivery) != DataStatus.Success)
                            {
                                return DataStatus.Failed;
                            }
                            else
                            {
                                if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Receipt) != DataStatus.Success)
                                {
                                    return DataStatus.Failed;
                                }
                            }
                        }
                        else if (productorder.State == Pd.OrderState.Receipt)
                        {
                            if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Receipt) != DataStatus.Success)
                            {
                                return DataStatus.Failed;
                            }
                        }

                    }
                    else
                    {
                        bool lastAfterSales = true;
                        foreach (Pd.ProductOrderMapping ordermapping in ordermappings)
                        {
                            ///判断是不是所有产品都申请过售后
                            if (ordermapping.IsService == false)
                            {
                                lastAfterSales = false;
                                break;
                            }
                        }
                        if (lastAfterSales)
                        {
                            IList<AfterSalesRecord> records = GetAllByOrderId(ds, productorder.Id);
                            foreach (AfterSalesRecord lastrecord in records)
                            {
                                ///判断是不是除了当前售后订单 别的订单都已完成售后
                                if (lastrecord.Id != Id && lastrecord.ServerState != EServiceState.Complete)
                                {
                                    lastAfterSales = false;
                                    break;
                                }
                            }
                        }
                        if (lastAfterSales)
                        {
                            if (productorder.State == Pd.OrderState.Delivery)
                            {
                                if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Delivery) != DataStatus.Success)
                                {
                                    return DataStatus.Failed;
                                }
                                else
                                {
                                    if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Receipt) != DataStatus.Success)
                                    {
                                        return DataStatus.Failed;
                                    }
                                }
                            }
                            else if (productorder.State == Pd.OrderState.Receipt)
                            {
                                if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Receipt) != DataStatus.Success)
                                {
                                    return DataStatus.Failed;
                                }
                            }
                        }
                    }
                }
                else if (Channel == EChannel.WholesaleOrder)
                {
                    Di.DistributorOrder productorder = Di.DistributorOrder.GetById(ds, record.OrderId);

                    IList<Di.DistributorOrderMapping> ordermappings = productorder.GetMapping(ds);
                    ///只有一个产品  直接修改订单状态
                    if (ordermappings.Count == 1)
                    {
                        if (productorder.State == Pd.OrderState.Delivery)
                        {
                            if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Delivery) != DataStatus.Success)
                            {
                                return DataStatus.Failed;
                            }
                            else
                            {
                                if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Receipt) != DataStatus.Success)
                                {
                                    return DataStatus.Failed;
                                }
                            }
                        }
                        else if (productorder.State == Pd.OrderState.Receipt)
                        {
                            if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Receipt) != DataStatus.Success)
                            {
                                return DataStatus.Failed;
                            }
                        }

                    }
                    else
                    {
                        bool lastAfterSales = true;
                        foreach (Di.DistributorOrderMapping ordermapping in ordermappings)
                        {
                            ///判断是不是所有产品都申请过售后
                            if (ordermapping.IsService == false)
                            {
                                lastAfterSales = false;
                                break;
                            }
                        }
                        if (lastAfterSales)
                        {
                            IList<AfterSalesRecord> records = GetAllByOrderId(ds, productorder.Id);
                            foreach (AfterSalesRecord lastrecord in records)
                            {
                                ///判断是不是除了当前售后订单 别的订单都已完成售后
                                if (lastrecord.Id != Id && lastrecord.ServerState != EServiceState.Complete)
                                {
                                    lastAfterSales = false;
                                    break;
                                }
                            }
                        }
                        if (lastAfterSales)
                        {
                            if (productorder.State == Pd.OrderState.Delivery)
                            {
                                if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Delivery) != DataStatus.Success)
                                {
                                    return DataStatus.Failed;
                                }
                                else
                                {
                                    if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Receipt) != DataStatus.Success)
                                    {
                                        return DataStatus.Failed;
                                    }
                                }
                            }
                            else if (productorder.State == Pd.OrderState.Receipt)
                            {
                                if (productorder.UpdateStateByUserNoState(ds, Pd.OrderState.Receipt) != DataStatus.Success)
                                {
                                    return DataStatus.Failed;
                                }
                            }
                        }

                    }
                }
                if (FreightAmount > 0)
                {
                    if (ProfitRecord.UpdataStateForAfter(ds, OrderId, 0) != DataStatus.Success)
                    {
                        return DataStatus.Exist;
                    }
                }
                //修改原收益为已到账
                if (ProfitRecord.UpdataStateForAfter(ds, OrderId, ProductId) != DataStatus.Success)
                {
                    return DataStatus.Exist;
                }
                ///给用户充钱
                if (record.ServiceType == EServiceType.Refund || record.ServiceType == EServiceType.ReturnGoods)
                {
                    if (M.MemberInfo.ModifyMoney(ds, record.UserId, record.RefundMoney, "退款订单:" + record.Id, 4, record.OrderId) == DataStatus.Success)
                    {
                        if (record.FreightAmount > 0)
                        {
                            if (M.MemberInfo.ModifyMoney(ds, record.UserId, record.FreightAmount, "退还运费:" + record.Id, 4, record.OrderId) == DataStatus.Success)
                                return DataStatus.Success;
                            else
                                return DataStatus.Failed;
                        }
                        else
                        {
                            return DataStatus.Success;
                        }
                    }
                    else
                        return DataStatus.Failed;
                }
                return DataStatus.Success;


            }
            return DataStatus.Success;
        }

        public static AfterSalesRecord GetById(DataSource ds, string id)
        {
            return Db<AfterSalesRecord>.Query(ds)
                .Select()
                .Where(W("Id", id))
                .First<AfterSalesRecord>();
        }
        public static AfterSalesRecord GetByOrderId(DataSource ds, string id, long userid)
        {
            return Db<AfterSalesRecord>.Query(ds)
                .Select()
                .Where(W("OrderId", id) & W("UserId", userid))
                .First<AfterSalesRecord>();
        }
        public static dynamic GetAndProductById(DataSource ds, string id)
        {
            return Db<AfterSalesRecord>.Query(ds)
               .Select(S<AfterSalesRecord>(), S<Pd.ProductOrderMapping>(), S<Pd.ProductLogistics>())
               .InnerJoin(O<AfterSalesRecord>("OrderId"), O<Pd.ProductOrderMapping>("OrderId"))
               .And(O<AfterSalesRecord>("ProductId"), O<Pd.ProductOrderMapping>("ProductId"))
               .LeftJoin(O<AfterSalesRecord>("Id"), O<Pd.ProductLogistics>("OrderId"))
               .Where(W<AfterSalesRecord>("Id", id))
               .OrderBy(D<AfterSalesRecord>("CreateDate"))
               .First();
        }
        public static dynamic GetAndDistributorProductById(DataSource ds, string id)
        {
            return Db<AfterSalesRecord>.Query(ds)
               .Select(S<AfterSalesRecord>(), S<Di.DistributorOrderMapping>(), S<Pd.ProductLogistics>())
               .InnerJoin(O<AfterSalesRecord>("OrderId"), O<Di.DistributorOrderMapping>("OrderId"))
               .And(O<AfterSalesRecord>("ProductId"), O<Di.DistributorOrderMapping>("ProductId"))
               .LeftJoin(O<AfterSalesRecord>("Id"), O<Pd.ProductLogistics>("OrderId"))
               .Where(W<AfterSalesRecord>("Id", id))
               .OrderBy(D<AfterSalesRecord>("CreateDate"))
               .First();
        }
        public static dynamic GetAndProductAndMemberInfoById(DataSource ds, string id)
        {
            dynamic list = null;
            list = Db<AfterSalesRecord>.Query(ds)
                   .Select(S<AfterSalesRecord>(), S<Pd.ProductOrderMapping>(), S<M.MemberInfo>("NickName"))
                   .InnerJoin(O<AfterSalesRecord>("OrderId"), O<Pd.ProductOrderMapping>("OrderId"))
                   .And(O<AfterSalesRecord>("ProductId"), O<Pd.ProductOrderMapping>("ProductId"))
                   .InnerJoin(O<AfterSalesRecord>("UserId"), O<M.MemberInfo>("Id"))
                   .Where(W<AfterSalesRecord>("Id", id))
                   .First();
            if (list != null)
            {
                return list;
            }
            else
            {
                list = Db<AfterSalesRecord>.Query(ds)
               .Select(S<AfterSalesRecord>(), S<XcpNet.Supplier.Modules.Modules.DistributorOrderMapping>(), S<M.MemberInfo>("NickName"))
               .InnerJoin(O<AfterSalesRecord>("OrderId"), O<XcpNet.Supplier.Modules.Modules.DistributorOrderMapping>("OrderId"))
               .And(O<AfterSalesRecord>("ProductId"), O<XcpNet.Supplier.Modules.Modules.DistributorOrderMapping>("ProductId"))
               .InnerJoin(O<AfterSalesRecord>("UserId"), O<M.MemberInfo>("Id"))
               .Where(W<AfterSalesRecord>("Id", id))
               .First();
            }
            return list;
        }


        public static SplitPageData<AfterSalesRecord> GetListByUser(DataSource ds, long userid, long index, int size, int show = 8)
        {
            long count;
            IList<AfterSalesRecord> list;
            list = Db<AfterSalesRecord>.Query(ds).Select()
                .Where(W("UserId", userid))
                .ToList<AfterSalesRecord>(size, index, out count);
            return new SplitPageData<AfterSalesRecord>(index, size, list, count, show);
        }

        public static SplitPageData<AfterSalesRecord> GetAllListByPage(DataSource ds, int type, int state, long index, int size, int show = 8)
        {
            DbWhereQueue where = new DbWhereQueue();
            where = W("Id", "", DbWhereType.NotEqual);
            if (type != 0)
                where &= W("ServiceType", type);
            if (state != -99)
                where &= W("ServerState", state);
            long count;
            IList<AfterSalesRecord> list;
            list = Db<AfterSalesRecord>.Query(ds).Select()
                .Where(where)
                .OrderBy(D("CreateDate"))
                .ToList<AfterSalesRecord>(size, index, out count);
            return new SplitPageData<AfterSalesRecord>(index, size, list, count, show);
        }
        public static SplitPageData<dynamic> GetListByPage(DataSource ds, int type, int state, long index, int size, int show, EChannel channel = EChannel.GoodsOrder)
        {
            DbWhereQueue where = new DbWhereQueue();
            where = W<AfterSalesRecord>("Id", "", DbWhereType.NotEqual);
            if (type != 0)
                where &= W<AfterSalesRecord>("ServiceType", type);
            if (state != -99)
                where &= W<AfterSalesRecord>("ServerState", state);
            where &= W<AfterSalesRecord>("Channel", channel);
            long count;
            IList<dynamic> list;
            list = Db<AfterSalesRecord>.Query(ds).
                Select(S<AfterSalesRecord>(), S<M.Member>())
                .InnerJoin(O<AfterSalesRecord>("UserId"), O<M.Member>("Id"))
                .Where(where)
                .OrderBy(D<AfterSalesRecord>("CreateDate"))
                .ToList(size, index, out count);
            return new SplitPageData<dynamic>(index, size, list, count, show);
        }

        public static SplitPageData<dynamic> GetInProductListByUser(DataSource ds, long userid, long index, int size, int show, EChannel channel = EChannel.GoodsOrder)
        {
            long count;
            IList<dynamic> list;
            list = Db<AfterSalesRecord>.Query(ds)
                .Select(S<AfterSalesRecord>(), S<Pd.ProductOrderMapping>(), S<Pd.ProductLogistics>())
                .InnerJoin(O<AfterSalesRecord>("OrderId"), O<Pd.ProductOrderMapping>("OrderId"))
                .And(O<AfterSalesRecord>("ProductId"), O<Pd.ProductOrderMapping>("ProductId"))
                .LeftJoin(O<AfterSalesRecord>("Id"), O<Pd.ProductLogistics>("OrderId"))
                .Where(W<AfterSalesRecord>("UserId", userid) & W<AfterSalesRecord>("Channel", channel))
                .OrderBy(D<AfterSalesRecord>("CreateDate"))
                .ToList(size, index, out count);

            return new SplitPageData<dynamic>(index, size, list, count, show);
        }
        public static SplitPageData<dynamic> GetInDistributorProductListByUser(DataSource ds, long userid, long index, int size, int show, EChannel channel = EChannel.GoodsOrder)
        {
            long count;
            IList<dynamic> list;
            list = Db<AfterSalesRecord>.Query(ds)
                .Select(S<AfterSalesRecord>(), S<Di.DistributorOrderMapping>(), S<Pd.ProductLogistics>())
                .InnerJoin(O<AfterSalesRecord>("OrderId"), O<Di.DistributorOrderMapping>("OrderId"))
                .And(O<AfterSalesRecord>("ProductId"), O<Di.DistributorOrderMapping>("ProductId"))
                .LeftJoin(O<AfterSalesRecord>("Id"), O<Pd.ProductLogistics>("OrderId"))
                .Where(W<AfterSalesRecord>("UserId", userid) & W<AfterSalesRecord>("Channel", channel))
                .OrderBy(D<AfterSalesRecord>("CreateDate"))
                .ToList(size, index, out count);

            return new SplitPageData<dynamic>(index, size, list, count, show);
        }

        public static SplitPageData<DataJoin<AfterSalesRecord, Pd.ProductOrderMapping>> GetInProductListBySupplier(DataSource ds, long userid, long index, int size, int show, EChannel channel = EChannel.GoodsOrder)
        {
            long count;
            IList<DataJoin<AfterSalesRecord, Pd.ProductOrderMapping>> list;
            list = Db<AfterSalesRecord>.Query(ds)
                .Select(S<AfterSalesRecord>(), S<Pd.ProductOrderMapping>())
                .InnerJoin(O<AfterSalesRecord>("OrderId"), O<Pd.ProductOrderMapping>("OrderId"))
                .And(O<AfterSalesRecord>("ProductId"), O<Pd.ProductOrderMapping>("ProductId"))
                .Where(W<AfterSalesRecord>("SupplierId", userid) & W<AfterSalesRecord>("Channel", channel))
                .OrderBy(D<AfterSalesRecord>("CreateDate"))
                .ToList<DataJoin<AfterSalesRecord, Pd.ProductOrderMapping>>(size, index, out count);

            return new SplitPageData<DataJoin<AfterSalesRecord, Pd.ProductOrderMapping>>(index, size, list, count, show);
        }

        public static SplitPageData<dynamic> GetAfterSalesListBySupplier(DataSource ds, long userid, long index, int size, string title, string nickName, string startCreateDate, string endCreateDate, string serverState, int show, EChannel channel = EChannel.GoodsOrder)
        {
            #region Where
            DbWhereQueue where = W<AfterSalesRecord>("SupplierId", userid);
            if (!string.IsNullOrEmpty(title))
            {
                where &= W<Pd.ProductOrderMapping>("ProductTitle", title, DbWhereType.Like);
            }
            if (!string.IsNullOrEmpty(nickName))
            {
                where &= W<M.MemberInfo>("NickName", nickName);
            }
            if (!string.IsNullOrEmpty(startCreateDate))
            {
                where &= W<AfterSalesRecord>("CreateDate", startCreateDate, DbWhereType.GreaterThanOrEqual);
            }
            if (!string.IsNullOrEmpty(endCreateDate))
            {
                where &= W<AfterSalesRecord>("CreateDate", endCreateDate, DbWhereType.LessThanOrEqual);
            }
            if (channel == EChannel.GoodsOrder)
                where &= (W<AfterSalesRecord>("Channel", channel) | W<AfterSalesRecord>("Channel", EChannel.AgricultureOrder));
            else if (channel == EChannel.WholesaleOrder)
                where &= W<AfterSalesRecord>("Channel", channel);
            if (!string.IsNullOrEmpty(serverState) && serverState != "-1")
            {
                EServiceState eServiceState;
                bool result = Enum.TryParse<EServiceState>(serverState, out eServiceState);
                if (result)
                {
                    where &= W<AfterSalesRecord>("ServerState", eServiceState);
                }
            }
            #endregion
            long count;
            IList<dynamic> list;
            if (channel == EChannel.GoodsOrder)
            {
                list = Db<AfterSalesRecord>.Query(ds)
                    .Select(S<AfterSalesRecord>(), S<Pd.ProductOrderMapping>(), S<M.MemberInfo>("NickName"))
                    .InnerJoin(O<AfterSalesRecord>("OrderId"), O<Pd.ProductOrderMapping>("OrderId"))
                    .And(O<AfterSalesRecord>("ProductId"), O<Pd.ProductOrderMapping>("ProductId"))
                    .InnerJoin(O<AfterSalesRecord>("UserId"), O<M.MemberInfo>("Id"))
                    .Where(where)
                    .OrderBy(D<AfterSalesRecord>("CreateDate"))
                    .ToList(size, index, out count);
            }
            else
            {
                list = Db<AfterSalesRecord>.Query(ds)
               .Select(S<AfterSalesRecord>(), S<XcpNet.Supplier.Modules.Modules.DistributorOrderMapping>(), S<M.MemberInfo>("NickName"))
               .InnerJoin(O<AfterSalesRecord>("OrderId"), O<XcpNet.Supplier.Modules.Modules.DistributorOrderMapping>("OrderId"))
               .And(O<AfterSalesRecord>("ProductId"), O<XcpNet.Supplier.Modules.Modules.DistributorOrderMapping>("ProductId"))
               .InnerJoin(O<AfterSalesRecord>("UserId"), O<M.MemberInfo>("Id"))
               .Where(where)
               .OrderBy(D<AfterSalesRecord>("CreateDate"))
               .ToList(size, index, out count);
            }
            return new SplitPageData<dynamic>(index, size, list, count, show);
        }
        public static SplitPageData<dynamic> GetAfterSalesListByMember(DataSource ds, long userid, long index, int size, string title, string nickName, string startCreateDate, string endCreateDate, string serverState, int show, EChannel channel = EChannel.GoodsOrder)
        {
            #region Where
            DbWhereQueue where = W<AfterSalesRecord>("UserId", userid);
            if (!string.IsNullOrEmpty(title))
            {
                where &= W<Pd.ProductOrderMapping>("ProductTitle", title, DbWhereType.Like);
            }
            if (!string.IsNullOrEmpty(nickName))
            {
                where &= W<M.MemberInfo>("NickName", nickName);
            }
            if (!string.IsNullOrEmpty(startCreateDate))
            {
                where &= W<AfterSalesRecord>("CreateDate", startCreateDate, DbWhereType.GreaterThanOrEqual);
            }
            if (!string.IsNullOrEmpty(endCreateDate))
            {
                where &= W<AfterSalesRecord>("CreateDate", endCreateDate, DbWhereType.LessThanOrEqual);
            }
            where &= W<AfterSalesRecord>("Channel", channel);
            if (!string.IsNullOrEmpty(serverState) && serverState != "-1")
            {
                EServiceState eServiceState;
                bool result = Enum.TryParse<EServiceState>(serverState, out eServiceState);
                if (result)
                {
                    where &= W<AfterSalesRecord>("ServerState", eServiceState);
                }
            }
            #endregion
            long count;
            IList<dynamic> list;
            list = Db<AfterSalesRecord>.Query(ds)
                .Select(S<AfterSalesRecord>(), S<Pd.ProductOrderMapping>(), S<M.MemberInfo>("NickName"))
                .InnerJoin(O<AfterSalesRecord>("OrderId"), O<Pd.ProductOrderMapping>("OrderId"))
                .And(O<AfterSalesRecord>("ProductId"), O<Pd.ProductOrderMapping>("ProductId"))
                .InnerJoin(O<AfterSalesRecord>("UserId"), O<M.MemberInfo>("Id"))
                .Where(where)
                .OrderBy(D<AfterSalesRecord>("CreateDate"))
                .ToList(size, index, out count);

            return new SplitPageData<dynamic>(index, size, list, count, show);
        }

        /// <summary>
        /// 申请通过并填写回寄地址
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="id"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static DataStatus UpdateStateForAddress(DataSource ds, string id, string address)
        {
            ds.Begin();
            try
            {
                int execution = Db<AfterSalesRecord>.Query(ds).Update()
                    .Set("Address", address).Set("ServerState", EServiceState.WaitingMail).Set("ExamineDate", DateTime.Now)
                    .Where(W("Id", id) & W("ServerState", EServiceState.ApplySerice))
                    .Execute();
                if (execution <= 0)
                {
                    ds.Rollback();
                    return DataStatus.Failed;
                }
                AfterSalesRecord after = GetById(ds, id);
                after.ServerState = EServiceState.WaitingMail;
                DataStatus result = after.OnUpdateAfter(ds);
                if (result != DataStatus.Success)
                {
                    ds.Rollback();
                    return DataStatus.Failed;

                }
                ds.Commit();
                return DataStatus.Success;

            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Rollback;
            }
        }

        /// <summary>
        /// 驳回申请
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DataStatus RejectAfterSales(DataSource ds, string id, string failMessage)
        {
            ds.Begin();
            try
            {
                int execution = Db<AfterSalesRecord>.Query(ds).Update()
                    .Set("ServerState", EServiceState.Fail).Set("FailMessage", failMessage).Set("ExamineDate", DateTime.Now)
                    .Where(W("Id", id) & W("ServerState", EServiceState.ApplySerice))
                    .Execute();
                AfterSalesRecord after = GetById(ds, id);
                after.ServerState = EServiceState.Fail;
                DataStatus result = after.OnUpdateAfter(ds);
                if (result != DataStatus.Success)
                {
                    ds.Rollback();
                    return DataStatus.Failed;
                }
                ///修改订单结算时间
                if (Db<Pd.ProductOrder>.Query(ds).Update().Set("RefundDate", DateTime.Now)
                    .Where(W("Id", after.OrderId)).Execute() <= 0)
                {
                    throw new Exception();
                }
                ///修改申请售后状态
                if (Db<Pd.ProductOrderMapping>.Query(ds).Update().Set("IsService", false).Set("AfterSalesOrderId", "")
                .Where(W("ProductId", after.ProductId) & W("OrderId", after.OrderId)).Execute() <= 0)
                {
                    throw new Exception();
                }
                if (execution <= 0)
                {
                    throw new Exception();
                }
                ds.Commit();
                return DataStatus.Success;

            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Rollback;
            }
        }

        /// <summary>
        /// 取消售后
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DataStatus CloseAfterSales(DataSource ds, string id)
        {
            ds.Begin();
            try
            {
                int execution = Db<AfterSalesRecord>.Query(ds).Update()
                    .Set("ServerState", EServiceState.Cancel)
                    .Where(W("Id", id) & W("ServerState", EServiceState.ApplySerice))
                    .Execute();
                AfterSalesRecord after = GetById(ds, id);
                after.ServerState = EServiceState.Cancel;
                DataStatus result = after.OnUpdateAfter(ds);
                if (result != DataStatus.Success)
                {
                    throw new Exception();
                }
                ///修改申请售后状态
                if (Db<Pd.ProductOrderMapping>.Query(ds).Update().Set("IsService", false).Set("AfterSalesOrderId", "")
                    .Where(W("ProductId", after.ProductId) & W("OrderId", after.OrderId)).Execute() <= 0)
                {
                    throw new Exception();
                }
                if (execution <= 0)
                {
                    throw new Exception();
                }
                ds.Commit();
                return DataStatus.Success;
            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Rollback;
            }
        }

        /// <summary>
        /// 确认完成
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DataStatus CompleteAfterSales(DataSource ds, string id)
        {

            ds.Begin();
            try
            {
                AfterSalesRecord Record = GetById(ds, id);
                int execution = 0;
                DataStatus result = 0;
                //如果为换货订单完成后   新建正常订单
                if (Record.ServiceType == EServiceType.ExchangeGoods)
                {
                    string NewOrderId = "";
                    if (Record.Channel == EChannel.AgricultureOrder || Record.Channel == EChannel.GoodsOrder)//城品慧换货
                    {
                        Pd.ProductOrder oldorder = Pd.ProductOrder.GetById(ds, Record.OrderId);
                        Pd.ProductOrderMapping oldordermapping = Pd.ProductOrderMapping.GetById(ds, Record.OrderId, Record.ProductId);
                        Pd.ProductOrder order = new Pd.ProductOrder()
                        {
                            Id = Pd.ProductOrder.NewId(DateTime.Now, Record.UserId),
                            State = Pd.OrderState.Delivery,
                            CreationDate = DateTime.Now,
                            ParentId = "",
                            PaymentDate = DateTime.Now,
                            ReceiptDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type),
                            TotalMoney = oldordermapping.TotalMoney + oldorder.FreightMoney,
                            Address = oldorder.Address,
                            AddressId = oldorder.AddressId,
                            DeliveryDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type),
                            FreightMoney = oldorder.FreightMoney,
                            Message = oldorder.Message,
                            ShopId = oldorder.ShopId,
                            SupplierId = oldorder.SupplierId,
                            Title = oldorder.Title,
                            UserId = Record.UserId,
                            Channel = oldorder.Channel
                        };
                        if (order.Insert(ds) != DataStatus.Success)
                        {
                            throw new Exception();
                        }

                        Pd.ProductOrderMapping ordermapping = oldordermapping;
                        ordermapping.TotalMoney = Record.RefundMoney;
                        ordermapping.Count = Record.RefundCount;
                        ordermapping.Evaluation = false;
                        ordermapping.IsService = false;
                        ordermapping.OrderId = order.Id;
                        if (ordermapping.Insert(ds) != DataStatus.Success)
                        {
                            throw new Exception();
                        }
                        if (SetOrderSettlement(ds, ordermapping) != DataStatus.Success)
                        {
                            throw new Exception();
                        }
                        NewOrderId = order.Id;
                    }
                    else if (Record.Channel == EChannel.WholesaleOrder)//进货宝换货
                    {
                        Di.DistributorOrder oldorder = Di.DistributorOrder.GetById(ds, Record.OrderId);
                        Di.DistributorOrderMapping oldordermapping = Di.DistributorOrderMapping.GetById(ds, Record.OrderId, Record.ProductId);
                        Di.DistributorOrder order = new Di.DistributorOrder()
                        {
                            Id = Pd.ProductOrder.NewId(DateTime.Now, Record.UserId),
                            State = Pd.OrderState.Delivery,
                            CreationDate = DateTime.Now,
                            ParentId = "",
                            PaymentDate = DateTime.Now,
                            ReceiptDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type),
                            TotalMoney = oldordermapping.TotalMoney + oldorder.FreightMoney,
                            Address = oldorder.Address,
                            AddressId = oldorder.AddressId,
                            DeliveryDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type),
                            FreightMoney = oldorder.FreightMoney,
                            Message = oldorder.Message,
                            ShopId = oldorder.ShopId,
                            SupplierId = oldorder.SupplierId,
                            Title = oldorder.Title,
                            UserId = Record.UserId,
                            
                        };
                        if (order.Insert(ds) != DataStatus.Success)
                        {
                            throw new Exception();
                        }

                        Di.DistributorOrderMapping ordermapping = oldordermapping;
                        ordermapping.TotalMoney = Record.RefundMoney;
                        ordermapping.Count = Record.RefundCount;
                        ordermapping.Evaluation = false;
                        ordermapping.IsService = false;
                        ordermapping.OrderId = order.Id;
                        if (ordermapping.Insert(ds) != DataStatus.Success)
                        {
                            throw new Exception();
                        }
                        if (SetOrderSupplierSettlement(ds, ordermapping) != DataStatus.Success)
                        {
                            throw new Exception();
                        }
                        NewOrderId = order.Id;
                    }
                    execution = Db<AfterSalesRecord>.Query(ds).Update()
                            .Set("ServerState", EServiceState.Complete)
                            .Set("NewOrderId", NewOrderId)
                            .Where(W("Id", id) & W("ServerState", EServiceState.Process))
                            .Execute();
                    if (execution > 0)
                    {
                        AfterSalesRecord after = GetById(ds, id);
                        after.ServerState = EServiceState.Complete;
                        result = after.OnUpdateAfter(ds);
                        if (result != DataStatus.Success)
                        {
                            throw new Exception();
                        }
                    }
                }
                else
                {

                    if (Record.ServiceType == EServiceType.Refund)
                    {
                        execution = Db<AfterSalesRecord>.Query(ds).Update()
                            .Set("ServerState", EServiceState.Complete)
                            .Where(W("Id", id) & W("ServerState", EServiceState.Complete))
                            .Execute();
                        if (execution > 0)
                        {
                            AfterSalesRecord after = GetById(ds, id);
                            after.ServerState = EServiceState.Complete;
                            result = after.OnUpdateAfter(ds);
                            if (result != DataStatus.Success)
                            {
                                throw new Exception();
                            }
                        }
                    }
                    else
                    {
                        execution = Db<AfterSalesRecord>.Query(ds).Update()
                            .Set("ServerState", EServiceState.Complete)
                            .Where(W("Id", id) & W("ServerState", EServiceState.Process))
                            .Execute();
                        if (execution > 0)
                        {
                            AfterSalesRecord after = GetById(ds, id);
                            after.ServerState = EServiceState.Complete;
                            result = after.OnUpdateAfter(ds);
                            if (result != DataStatus.Success)
                            {
                                throw new Exception();
                            }
                        }
                    }

                }
                if (execution > 0)
                {
                    UpdateInventory(ds, id, result);
                    ds.Commit();
                    return DataStatus.Success;
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Rollback;
            }
        }

        private static void UpdateInventory(DataSource ds, string id, DataStatus result)
        {
            if (result == DataStatus.Success)
            {
                AfterSalesRecord record = GetById(ds, id);
                IList<AfterSalesRecord> items = GetByOrderId(ds, record.OrderId);
                foreach (AfterSalesRecord item in items)
                {
                    Pd.Product.UpdateInventoryById(ds, item.ProductId, -item.RefundCount);//负负得正
                }
            }
        }

        /// <summary>
        /// 客户发货
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DataStatus ProcessAfterSales(DataSource ds, string id)
        {
            ds.Begin();
            try
            {
                int execution = Db<AfterSalesRecord>.Query(ds).Update()
                    .Set("ServerState", EServiceState.Process)
                    .Where(W("Id", id) & W("ServerState", EServiceState.WaitingMail))
                    .Execute();
                if (execution <= 0)
                {
                    throw new Exception();
                }
                AfterSalesRecord after = GetById(ds, id);
                after.ServerState = EServiceState.Process;
                DataStatus result = after.OnUpdateAfter(ds);
                if (result != DataStatus.Success)
                {
                    throw new Exception();
                }
                ds.Commit();
                return DataStatus.Success;
            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Rollback;
            }
        }
        /// <summary>
        /// 退款
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DataStatus Refund(DataSource ds, string id)
        {
            ds.Begin();
            try
            {
                int execution = Db<AfterSalesRecord>.Query(ds).Update()
                    .Set("ServerState", EServiceState.Complete)
                    .Where(W("Id", id))
                    .Execute();


                if (execution <= 0)
                {
                    throw new Exception();
                }
                AfterSalesRecord after = GetById(ds, id);
                after.ServerState = EServiceState.Complete;
                DataStatus result = after.OnUpdateAfter(ds);
                if (result != DataStatus.Success)
                {
                    throw new Exception();
                }
                UpdateInventory(ds, id, result);
                ds.Commit();
                return DataStatus.Success;
            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Rollback;
            }
        }

        public static IList<AfterSalesRecord> GetAllByOrderId(DataSource ds, string orderId)
        {
            return Db<AfterSalesRecord>.Query(ds).Select().Where(W("OrderId", orderId)).ToList<AfterSalesRecord>();
        }
        /// <summary>
        /// 根据订单号获取有效的售后订单
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static IList<AfterSalesRecord> GetByOrderId(DataSource ds, string orderId)
        {
            return Db<AfterSalesRecord>.Query(ds).Select().Where(W("OrderId", orderId) & W("ServerState", EServiceState.Fail, DbWhereType.NotEqual) & W("ServerState", EServiceState.Cancel, DbWhereType.NotEqual)).ToList<AfterSalesRecord>();
        }

        public static long Count(DataSource ds, long userId)
        {
            return Db<AfterSalesRecord>.Query(ds).Select().Where(W("UserId", userId)).Count();
        }

        public static bool IsRetreatFreightAmount(DataSource ds, string orderId)
        {
            Pd.ProductOrder order = Db<Pd.ProductOrder>.Query(ds).Select().Where(W("Id", orderId)).First<Pd.ProductOrder>();
            if (order != null && order.State == Pd.OrderState.Delivery)
            {
                int count = order.GetMapping(ds).Where(o => o.IsService == false).Count();
                if (count == 1)
                {
                    int money = GetByOrderId(ds, orderId).Where(a => a.FreightAmount > 0).Count();
                    if (money > 0)
                        return false;
                    else
                        return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool IsRetreatDistributorFreightAmount(DataSource ds, string orderId)
        {
            Di.DistributorOrder order = Db<Di.DistributorOrder>.Query(ds).Select().Where(W("Id", orderId)).First<Di.DistributorOrder>();
            if (order != null && order.State == Pd.OrderState.Delivery)
            {
                int count = order.GetMapping(ds).Where(o => o.IsService == false).Count();
                if (count == 1)
                {
                    int money = GetByOrderId(ds, orderId).Where(a => a.FreightAmount > 0).Count();
                    if (money > 0)
                        return false;
                    else
                        return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 创建收益快照
        /// </summary>
        /// <param name="ordermapping"></param>
        /// <returns></returns>
        //public static DataStatus SetOrderSettlement(DataSource ds, Pd.ProductOrderMapping ordermapping)
        //{
        //    ds.Begin();
        //    try
        //    {
        //        Pd.Product product = Pd.Product.GetById(ds, ordermapping.ProductId);
        //        Pd.ProductOrderSettlement settlement = new Pd.ProductOrderSettlement
        //        {
        //            OrderId = ordermapping.OrderId,
        //            ProductId = ordermapping.ProductId,
        //            CostPrice = product.CostPrice,
        //            Settlement = product.Settlement,
        //            RoyaltyRate = product.RoyaltyRate
        //        };
        //        if (product.Wholesale && product.WholesalePrice > 0)
        //        {
        //            settlement.ProductType = Pd.EProductType.Wholesale;
        //        }
        //        else if (product.DiscountState == Pd.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
        //        {
        //            settlement.ProductType = Pd.EProductType.GroupBuy;
        //        }
        //        else
        //        {
        //            settlement.ProductType = Pd.EProductType.Routine;
        //        }
        //        long ShopId = 0;
        //        //增加收益快照GetRoyaltyByOrderMapping
        //        Pd.ProductOrder order = Pd.ProductOrder.GetById(ds, ordermapping.OrderId);
        //        Pd.Distributor distributor = Pd.Distributor.GetById(ds, order.UserId);
        //        if (distributor != null && distributor.UserId != 0)
        //        {
        //            order.ShopId = order.UserId;
        //        }
        //        else
        //        {
        //            M.Member member = M.Member.GetById(ds, order.UserId);
        //            if (member.ParentId != 0)
        //            {
        //                distributor = Pd.Distributor.GetById(ds, member.ParentId);
        //                order.ShopId = distributor.UserId;
        //            }
        //        }
        //        ///销售人员及加盟商算提成
        //        if (distributor != null && distributor.UserId != 0)
        //        {
        //            if (distributor.Level == 2 || distributor.Level == 3 || distributor.Level == 4)
        //            {
        //                settlement.SaleId = order.ShopId;
        //                settlement.SaleRoyaltyRate = int.Parse((distributor.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
        //                settlement.SaleId = order.ShopId;
        //                M.Member SaleMember = M.Member.GetById(ds, order.ShopId);
        //                ///增加推荐人提成
        //                if (distributor.Level == 2)
        //                {
        //                    settlement.ParentId = SaleMember.ParentId;
        //                    if (settlement.ParentId != 0)
        //                    {
        //                        Pd.Distributor parentD = Pd.Distributor.GetById(ds, settlement.ParentId);
        //                        if (SaleMember.CreationDate.AddYears(3) >= DateTime.Now)///创建三年内有收益
        //                            settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
        //                    }
        //                }
        //                else
        //                {
        //                    settlement.ParentId = distributor.ParentId;
        //                    if (settlement.ParentId != 0)
        //                    {
        //                        Pd.Distributor parentD = Pd.Distributor.GetById(ds, distributor.ParentId);
        //                            settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
        //                        if (parentD.Level > 1)
        //                            distributor.ParentId = parentD.ParentId;
        //                    }                            
        //                }
        //                ///增加县级提成
        //                settlement.CountyUserId = distributor.ParentId;
        //                Pd.Distributor CountyD = Pd.Distributor.GetById(ds, settlement.CountyUserId);
        //                if(CountyD.Level==1)
        //                settlement.CountyRoyaltyRate = int.Parse((CountyD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
        //                ///增加省级提成
        //                settlement.ProvinceUserId = CountyD.ParentId;
        //                Pd.Distributor ProvinceD = Pd.Distributor.GetById(ds, settlement.ProvinceUserId);
        //                settlement.ProvinceRoyaltyRate = int.Parse((ProvinceD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());

        //            }
        //            else if (distributor.Level == 1)
        //            {
        //                settlement.CountyRoyaltyRate = int.Parse((distributor.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
        //                settlement.CountyUserId = order.ShopId;
        //                ///增加省级提成
        //                settlement.ProvinceUserId = distributor.ParentId;
        //                Pd.Distributor ProvinceD = Pd.Distributor.GetById(ds, settlement.ProvinceUserId);
        //                settlement.ProvinceRoyaltyRate = int.Parse((ProvinceD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
        //            }
        //            else if (distributor.Level == 0)
        //            {
        //                settlement.ProvinceRoyaltyRate = int.Parse((distributor.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
        //                settlement.ProvinceUserId = order.ShopId;
        //                ///增加省级提成
        //                settlement.ProvinceUserId = distributor.ParentId;
        //                Pd.Distributor ProvinceD = Pd.Distributor.GetById(ds, settlement.ProvinceUserId);
        //                settlement.ProvinceRoyaltyRate = int.Parse((ProvinceD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
        //            }
        //            if (settlement.Insert(ds) == DataStatus.Success)
        //            {
        //                ds.Commit();
        //                return DataStatus.Success;
        //            }
        //            else
        //            {
        //                ds.Rollback();
        //                return DataStatus.Rollback;
        //            }
        //        }
        //        else
        //        {
        //            if (settlement.Insert(ds) == DataStatus.Success)
        //            {
        //                ds.Commit();
        //                return DataStatus.Success;
        //            }
        //            else
        //            {
        //                ds.Rollback();
        //                return DataStatus.Rollback;
        //            }
        //        }

        //    }
        //    catch (Exception)
        //    {
        //        ds.Rollback();
        //        return DataStatus.Rollback;
        //    }
        //}

        /// <summary>
        /// 设置订单的交易快照
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="ordermapping">产品映射表</param>
        /// <returns></returns>
        public static DataStatus SetOrderSettlement(DataSource DataSource, Pd.ProductOrderMapping ordermapping)
        {
            DataSource.Begin();
            try
            {
                Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, ordermapping.OrderId);
                Pd.Product product = Pd.Product.GetById(DataSource, ordermapping.ProductId);
                Pd.ProductAreaMapping areaMapping = Pd.ProductAreaMapping.GetById(DataSource, product.Id, order.Province, order.City, order.County);

                ///增加供应商结算快照
                Pd.DifferenceSettlement settlement = new Pd.DifferenceSettlement();
                settlement.OrderId = ordermapping.OrderId;
                settlement.ProductId = ordermapping.ProductId;
                if (areaMapping == null)
                {
                    settlement.CostPrice = product.CostPrice * ordermapping.Count;
                    settlement.DotPrice = (product.Price - product.DotPrice) * ordermapping.Count;
                    if (order.Channel == 1)
                    {
                        Pd.Supplier supplier = Pd.Supplier.GetById(DataSource, order.SupplierId);
                        if (supplier.SupplierType == 1 && supplier.Subjection != 0)///县级自己的供应商把钱结算给县级
                            settlement.CountyPrice = product.CostPrice + ((product.DotPrice - product.CountyPrice) * ordermapping.Count);
                        else
                            settlement.CountyPrice = (product.DotPrice - product.CountyPrice) * ordermapping.Count;
                    }
                    else if (order.Channel == 1)
                    {
                        settlement.CountyPrice = (product.DotPrice - product.CountyPrice) * ordermapping.Count;
                    }
                }
                else
                {
                    settlement.CostPrice = areaMapping.CostPrice * ordermapping.Count;
                    settlement.DotPrice = (areaMapping.Price - areaMapping.DotPrice) * ordermapping.Count;
                    if (order.Channel == 1)
                    {
                        Pd.Supplier supplier = Pd.Supplier.GetById(DataSource, order.SupplierId);
                        if (supplier.SupplierType == 1 && supplier.Subjection != 0)///县级自己的供应商把钱结算给县级
                            settlement.CountyPrice = product.CostPrice + ((areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count);
                        else
                            settlement.CountyPrice = (areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count;
                    }
                    else if (order.Channel == 1)
                    {
                        settlement.CountyPrice = (areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count;
                    }
                }
                if (product.Wholesale && product.WholesalePrice > 0)
                {
                    settlement.ProductType = Pd.DifferenceSettlement.EProductType.Wholesale;
                }
                else if (product.DiscountState == Pd.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
                {
                    settlement.ProductType = Pd.DifferenceSettlement.EProductType.GroupBuy;
                }
                else
                {
                    settlement.ProductType = Pd.DifferenceSettlement.EProductType.Routine;
                }

                //增加收益快照GetRoyaltyByOrderMapping
                Pd.Distributor CountyDist = null;
                if (order.ShopId <= 0)
                {
                    Pd.Distributor distributor = Pd.Distributor.GetByAreaAndLevel(DataSource, order.Province, order.City, order.County, 1);
                    if (distributor != null)
                    {
                        order.ShopId = distributor.UserId;
                        CountyDist = distributor;
                    }
                }
                if (order.ShopId > 0)
                {
                    if (CountyDist == null)
                        CountyDist = Pd.Distributor.GetById(DataSource, order.ShopId);
                    if (CountyDist != null)
                    {
                        ///给销售网点增加收益
                        if (CountyDist.Level > 1)
                        {
                            settlement.DotId = CountyDist.UserId;
                            CountyDist = Pd.Distributor.GetById(DataSource, CountyDist.ParentId);
                            if (CountyDist != null)
                            {
                                ///给镇级增加收益              
                                if (CountyDist.Level > 1)
                                {
                                    settlement.ParentId = CountyDist.UserId;
                                    settlement.ParentPrice = (ordermapping.Price * (Money)0.01) * ordermapping.Count;
                                    settlement.CountyPrice = (settlement.CountyPrice - settlement.ParentPrice) * ordermapping.Count;
                                    CountyDist = Pd.Distributor.GetById(DataSource, CountyDist.ParentId);
                                }
                                ///给县级增加收益
                                if (CountyDist.Level == 1)
                                {
                                    ///如果销售人跟供应商是同一个人或则供应商跟县级收益人为同一个人那么这个收益加起来结算给县级供应商（适用于乡道管）
                                    if (order.ShopId != order.SupplierId && order.SupplierId != CountyDist.UserId)
                                        settlement.CountyId = CountyDist.UserId;
                                    else if (order.Channel == 2)
                                        settlement.CostPrice += settlement.CountyPrice;//成本价加上县级的收益金额

                                }
                            }
                        }
                        else if (CountyDist.Level == 1)
                        {
                            ///如果销售人跟供应商是同一个人或则供应商跟县级收益人为同一个人那么这个收益加起来结算给县级供应商（适用于乡道管）
                            if (order.ShopId != order.SupplierId && order.SupplierId != CountyDist.UserId)
                                settlement.CountyId = CountyDist.UserId;
                            else if (order.Channel == 2)
                                settlement.CostPrice += settlement.CountyPrice;//成本价加上县级的收益金额
                        }
                    }
                }
                if (settlement.Insert(DataSource) != DataStatus.Success)
                {
                    throw new Exception();
                }
                DataSource.Commit();
                return DataStatus.Success;
            }
            catch (Exception)
            {
                DataSource.Rollback();
                return DataStatus.Rollback;
            }

        }


        /// <summary>
        /// 设置进货宝订单的交易快照
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="ordermapping">产品映射表</param>
        /// <returns></returns>
        public static DataStatus SetOrderSupplierSettlement(DataSource DataSource, Di.DistributorOrderMapping ordermapping)
        {
            DataSource.Begin();
            try
            {
                Di.DistributorOrder order = Di.DistributorOrder.GetById(DataSource, ordermapping.OrderId);
                Di.DistributorProduct product = Di.DistributorProduct.GetById(DataSource, ordermapping.ProductId);
                Di.DistributorAreaMapping areaMapping = Di.DistributorAreaMapping.GetById(DataSource, product.Id, order.Province, order.City, order.County);

                ///增加供应商结算快照
                Di.DistributorDifferenceSettlement settlement = new Di.DistributorDifferenceSettlement();
                settlement.OrderId = ordermapping.OrderId;
                settlement.ProductId = ordermapping.ProductId;
                Pd.Supplier supplier = Pd.Supplier.GetById(DataSource, order.SupplierId);
                if (areaMapping == null)
                {
                    settlement.CostPrice = product.CostPrice * ordermapping.Count;
                    settlement.DotPrice = 0;
                    ///县级自己的供应商把钱结算给县级             
                    if (supplier.SupplierType == 1 && supplier.Subjection != 0)
                        settlement.CountyPrice = product.CostPrice + ((product.DotPrice - product.CountyPrice) * ordermapping.Count);
                    else
                        settlement.CountyPrice = (product.DotPrice - product.CountyPrice) * ordermapping.Count;
                }
                else
                {
                    settlement.CostPrice = areaMapping.CostPrice * ordermapping.Count;
                    settlement.DotPrice = 0;
                    ///县级自己的供应商把钱结算给县级
                    if (supplier.SupplierType == 1 && supplier.Subjection != 0)
                        settlement.CountyPrice = product.CostPrice + (areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count;
                    else
                        settlement.CountyPrice = (areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count;
                }
                if (product.Wholesale && product.WholesalePrice > 0)
                {
                    settlement.ProductType = Pd.DifferenceSettlement.EProductType.Wholesale;
                }
                else if (product.DiscountState == Pd.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
                {
                    settlement.ProductType = Pd.DifferenceSettlement.EProductType.GroupBuy;
                }
                else
                {
                    settlement.ProductType = Pd.DifferenceSettlement.EProductType.Routine;
                }

                //增加收益快照GetRoyaltyByOrderMapping
                Pd.Distributor CountyDist = null;
                if (order.ShopId <= 0)
                {
                    Pd.Distributor distributor = Pd.Distributor.GetByAreaAndLevel(DataSource, order.Province, order.City, order.County, 1);
                    if (distributor != null)
                    {
                        order.ShopId = distributor.UserId;
                        CountyDist = distributor;
                    }
                }
                if (order.ShopId > 0)
                {
                    if (CountyDist == null)
                        CountyDist = Pd.Distributor.GetById(DataSource, order.ShopId);
                    if (CountyDist != null)
                    {
                        ///给销售网点增加收益
                        if (CountyDist.Level > 1)
                        {
                            CountyDist = Pd.Distributor.GetById(DataSource, CountyDist.ParentId);
                            if (CountyDist != null)
                            {
                                if (CountyDist.Level > 1)
                                {
                                    CountyDist = Pd.Distributor.GetById(DataSource, CountyDist.ParentId);
                                }
                                ///给县级增加收益
                                if (CountyDist.Level == 1)
                                {
                                    settlement.CountyId = CountyDist.UserId;
                                }
                            }
                        }
                        else if (CountyDist.Level == 1)
                        {
                            settlement.CountyId = CountyDist.UserId;
                        }
                    }
                }
                if (settlement.Insert(DataSource) != DataStatus.Success)
                {
                    throw new Exception();
                }
                DataSource.Commit();
                return DataStatus.Success;
            }
            catch (Exception)
            {
                DataSource.Rollback();
                return DataStatus.Rollback;
            }

        }


        ///// <summary>
        ///// 设置订单的交易快照
        ///// </summary>
        ///// <param name="DataSource">数据源</param>
        ///// <param name="ordermapping">产品映射表</param>
        ///// <returns></returns>
        //public static DataStatus SetOrderSettlement(DataSource DataSource, Pd.ProductOrderMapping ordermapping)
        //{
        //    DataSource.Begin();
        //    try
        //    {
        //        Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, ordermapping.OrderId);
        //        Pd.Product product = Pd.Product.GetById(DataSource, ordermapping.ProductId);
        //        Pd.ProductAreaMapping areaMapping = Pd.ProductAreaMapping.GetById(DataSource, product.Id, order.Province, order.City, order.County);

        //        ///增加供应商结算快照
        //        Pd.DifferenceSettlement settlement = new Pd.DifferenceSettlement();
        //        settlement.OrderId = ordermapping.OrderId;
        //        settlement.ProductId = ordermapping.ProductId;
        //        if (areaMapping == null)
        //        {
        //            settlement.CostPrice = product.CostPrice * ordermapping.Count;
        //            settlement.DotPrice = (product.Price - product.DotPrice) * ordermapping.Count;
        //            settlement.CountyPrice = (product.DotPrice - product.CountyPrice) * ordermapping.Count;
        //        }
        //        else
        //        {
        //            settlement.CostPrice = areaMapping.CostPrice * ordermapping.Count;
        //            settlement.DotPrice = (areaMapping.Price - areaMapping.DotPrice) * ordermapping.Count;
        //            settlement.CountyPrice = (areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count;
        //        }
        //        if (product.Wholesale && product.WholesalePrice > 0)
        //        {
        //            settlement.ProductType = Pd.DifferenceSettlement.EProductType.Wholesale;
        //        }
        //        else if (product.DiscountState == Pd.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
        //        {
        //            settlement.ProductType = Pd.DifferenceSettlement.EProductType.GroupBuy;
        //        }
        //        else
        //        {
        //            settlement.ProductType = Pd.DifferenceSettlement.EProductType.Routine;
        //        }

        //        //增加收益快照GetRoyaltyByOrderMapping
        //        Pd.Distributor CountyDist = null;
        //        if (order.ShopId <= 0)
        //        {
        //            Pd.Distributor distributor = Pd.Distributor.GetByAreaAndLevel(DataSource, order.Province, order.City, order.County, 1);
        //            if (distributor != null)
        //            {
        //                order.ShopId = distributor.UserId;
        //                CountyDist = distributor;
        //            }
        //        }
        //        if (order.ShopId > 0)
        //        {
        //            if (CountyDist == null)
        //                CountyDist = Pd.Distributor.GetById(DataSource, order.ShopId);
        //            if (CountyDist != null)
        //            {
        //                ///给销售网点增加收益
        //                if (CountyDist.Level > 1)
        //                {
        //                    settlement.DotId = CountyDist.UserId;
        //                    CountyDist = Pd.Distributor.GetById(DataSource, CountyDist.ParentId);
        //                    if (CountyDist != null)
        //                    {
        //                        ///给镇级增加收益              
        //                        if (CountyDist.Level > 1)
        //                        {
        //                            settlement.ParentId = CountyDist.UserId;
        //                            settlement.ParentPrice = (ordermapping.Price * (Money)0.01) * ordermapping.Count;
        //                            settlement.CountyPrice = (settlement.CountyPrice - settlement.ParentPrice) * ordermapping.Count;
        //                            CountyDist = Pd.Distributor.GetById(DataSource, CountyDist.ParentId);
        //                        }
        //                        ///给县级增加收益
        //                        if (CountyDist.Level == 1)
        //                        {
        //                            settlement.CountyId = CountyDist.UserId;
        //                        }
        //                    }
        //                }
        //                else if (CountyDist.Level == 1)
        //                {
        //                    settlement.CountyId = CountyDist.UserId;
        //                }
        //            }
        //        }
        //        if (settlement.Insert(DataSource) == DataStatus.Success)
        //        {
        //            DataSource.Commit();
        //            return DataStatus.Success;
        //        }
        //        else
        //        {
        //            DataSource.Rollback();
        //            return DataStatus.Rollback;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        DataSource.Rollback();
        //        return DataStatus.Rollback;
        //    }

        //    #region 原来的

        //    #endregion 原来的
        //}


        ///// <summary>
        ///// 设置进货宝订单的交易快照
        ///// </summary>
        ///// <param name="DataSource">数据源</param>
        ///// <param name="ordermapping">产品映射表</param>
        ///// <returns></returns>
        //public static DataStatus SetOrderSupplierSettlement(DataSource DataSource, Di.DistributorOrderMapping ordermapping)
        //{
        //    DataSource.Begin();
        //    try
        //    {
        //        Di.DistributorOrder order = Di.DistributorOrder.GetById(DataSource, ordermapping.OrderId);
        //        Di.DistributorProduct product = Di.DistributorProduct.GetById(DataSource, ordermapping.ProductId);
        //        Di.DistributorAreaMapping areaMapping = Di.DistributorAreaMapping.GetById(DataSource, product.Id, order.Province, order.City, order.County);

        //        ///增加供应商结算快照
        //        Di.DistributorDifferenceSettlement settlement = new Di.DistributorDifferenceSettlement();
        //        settlement.OrderId = ordermapping.OrderId;
        //        settlement.ProductId = ordermapping.ProductId;
        //        if (areaMapping == null)
        //        {
        //            settlement.CostPrice = product.CostPrice * ordermapping.Count;
        //            settlement.DotPrice = 0;
        //            settlement.CountyPrice = (product.DotPrice - product.CountyPrice) * ordermapping.Count;
        //        }
        //        else
        //        {
        //            settlement.CostPrice = areaMapping.CostPrice * ordermapping.Count;
        //            settlement.DotPrice = 0;
        //            settlement.CountyPrice = (areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count;
        //        }
        //        if (product.Wholesale && product.WholesalePrice > 0)
        //        {
        //            settlement.ProductType = Pd.DifferenceSettlement.EProductType.Wholesale;
        //        }
        //        else if (product.DiscountState == Pd.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
        //        {
        //            settlement.ProductType = Pd.DifferenceSettlement.EProductType.GroupBuy;
        //        }
        //        else
        //        {
        //            settlement.ProductType = Pd.DifferenceSettlement.EProductType.Routine;
        //        }

        //        //增加收益快照GetRoyaltyByOrderMapping
        //        Pd.Distributor CountyDist = null;
        //        if (order.ShopId <= 0)
        //        {
        //            Pd.Distributor distributor = Pd.Distributor.GetByAreaAndLevel(DataSource, order.Province, order.City, order.County, 1);
        //            if (distributor != null)
        //            {
        //                order.ShopId = distributor.UserId;
        //                CountyDist = distributor;
        //            }
        //        }
        //        if (order.ShopId > 0)
        //        {
        //            if (CountyDist == null)
        //                CountyDist = Pd.Distributor.GetById(DataSource, order.ShopId);
        //            if (CountyDist != null)
        //            {
        //                ///给销售网点增加收益
        //                if (CountyDist.Level > 1)
        //                {
        //                    CountyDist = Pd.Distributor.GetById(DataSource, CountyDist.ParentId);
        //                    if (CountyDist != null)
        //                    {
        //                        if (CountyDist.Level > 1)
        //                        {
        //                            CountyDist = Pd.Distributor.GetById(DataSource, CountyDist.ParentId);
        //                        }
        //                        ///给县级增加收益
        //                        if (CountyDist.Level == 1)
        //                        {
        //                            settlement.CountyId = CountyDist.UserId;
        //                        }
        //                    }
        //                }
        //                else if (CountyDist.Level == 1)
        //                {
        //                    settlement.CountyId = CountyDist.UserId;
        //                }
        //            }
        //        }
        //        if (settlement.Insert(DataSource) == DataStatus.Success)
        //        {
        //            DataSource.Commit();
        //            return DataStatus.Success;
        //        }
        //        else
        //        {
        //            DataSource.Rollback();
        //            return DataStatus.Rollback;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        DataSource.Rollback();
        //        return DataStatus.Rollback;
        //    }


        //    #region 原来的

        //    #endregion 原来的
        //}


    }
}
