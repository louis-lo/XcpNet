using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cnaws.Data;
using System.Threading.Tasks;
using Cnaws.Web;
using Cnaws.Templates;
using M = Cnaws.Passport.Modules;
using Pd = Cnaws.Product.Modules;
using Supp = XcpNet.Supplier.Modules.Modules;
using Cnaws;
using Cnaws.Data.Query;
using Cnaws.Product;
using Cnaws.Json;

namespace XcpNet.AfterSales.Modules
{
    /// <summary>
    /// 收益记录表
    /// </summary>
    [Serializable]
    public class ProfitRecord : NoIdentityModule
    {
        public enum EProfitState
        {
            /// <summary>
            /// 失效
            /// </summary>
            Invalid = 0,
            /// <summary>
            /// 未到账
            /// </summary>
            NoArrival,
            /// <summary>
            /// 已到账
            /// </summary>
            Arrival,
            ///// <summary>
            ///// 未审核
            ///// </summary>
            //NoAudited,
            ///// <summary>
            ///// 已审核
            ///// </summary>
            //Audited,
        }
        public enum EProfitType
        {
            /// <summary>
            /// 收益
            /// </summary>
            Houston = 0,
            /// <summary>
            /// 提现
            /// </summary>
            OutOfAccount,
            /// <summary>
            /// 退款
            /// </summary>
            Refound
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

        public enum ERole
        {
            /// <summary>
            /// 其它（提现）
            /// </summary>
            Other = 0,
            /// <summary>
            /// 供应商
            /// </summary>
            Supplier = 1,
            /// <summary>
            /// 加盟商
            /// </summary>
            Distributor
        }

        /// <summary>
        /// 订单编号
        /// </summary>
        [DataColumn(true, 36)]
        public string OrderId = null;
        /// <summary>
        /// 订单产品编号
        /// </summary>
        [DataColumn(true)]
        public long ProductId = 0;
        /// <summary>
        /// 用户Id
        /// </summary>
        [DataColumn(true)]
        public long UserId = 0;
        ///// <summary>
        ///// 收益角色
        ///// </summary>
        //[DataColumn(true)]
        //public ERole Role = ERole.Other;
        /// <summary>
        /// 来源用户
        /// </summary>
        public long SourceUserId = 0;
        /// <summary>
        /// 标题 
        /// </summary>
        public string Title = null;
        /// <summary>
        /// 订单产品成交总金额
        /// </summary>
        public Money TotalMoney = 0;
        /// <summary>
        /// 收益金额
        /// </summary>
        public Money ProfitMoney = 0;
        /// <summary>
        /// 状态
        /// </summary>
        public EProfitState ProfitState = EProfitState.Invalid;
        /// <summary>
        /// 类型
        /// </summary>
        public EProfitType ProfitType = EProfitType.Houston;
        /// <summary>
        /// 收益频道
        /// </summary>
        public EChannel Channel = EChannel.GoodsOrder;
        /// <summary>
        /// 创健时间
        /// </summary>
        public DateTime CreateDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);

        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "ProfitMoney");
            DropIndex(ds, "SourceUserId");
            DropIndex(ds, "ProfitState");
            DropIndex(ds, "ProfitType");
        }
        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "ProfitMoney", "ProfitMoney");
            CreateIndex(ds, "SourceUserId", "SourceUserId", "UserId");
            CreateIndex(ds, "ProfitState", "UserId", "ProfitState");
            CreateIndex(ds, "ProfitType", "UserId", "ProfitType", "ProfitState");
        }

        public M.Member GetMemberBySourceUser(DataSource ds)
        {
            return Db<M.Member>.Query(ds).Select().Where(W("Id", SourceUserId)).First<M.Member>();
        }


        /// <summary>
        /// 根据付款订单号增加对应的收益
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="orderid"></param>
        /// <returns></returns>
        public static DataStatus AddProfitByOrder(DataSource ds, string orderid)
        {            
            ds.Begin();
            try
            {
                Pd.ProductOrder productorder = Pd.ProductOrder.GetById(ds, orderid);
                IList<Pd.DifferenceSettlement> ordersetts = Pd.DifferenceSettlement.GetListbyOrderId(ds, orderid);
                foreach (Pd.DifferenceSettlement settlement in ordersetts)
                {
                    Pd.ProductOrderMapping mapping = Pd.ProductOrderMapping.GetById(ds, orderid, settlement.ProductId);
                    ProfitRecord profitrecord = new ProfitRecord();
                    profitrecord.OrderId = orderid;
                    profitrecord.ProductId = settlement.ProductId;
                    profitrecord.ProfitState = EProfitState.NoArrival;
                    profitrecord.ProfitType = EProfitType.Houston;
                    profitrecord.Channel = EChannel.GoodsOrder;//普通商品订单
                    profitrecord.Title = "购买商品";
                    profitrecord.TotalMoney = mapping.TotalMoney;
                    profitrecord.UserId = productorder.SupplierId;///优先加盟商结算
                    profitrecord.SourceUserId = productorder.UserId;
                    profitrecord.CreateDate = DateTime.Now;
                    profitrecord.Channel = EChannel.GoodsOrder;
                    //插入收益记录
                    AddProfitByUser(ds, profitrecord, settlement);
                }
                //给供应商结算邮费
                if (productorder.FreightMoney > 0)///加盟商结算
                {
                    ProfitRecord profitrecord = new ProfitRecord();
                    //profitrecord.Role = ERole.Supplier;
                    profitrecord.OrderId = orderid;
                    profitrecord.ProductId = 0;//邮费的产品Id为0
                    profitrecord.ProfitState = EProfitState.NoArrival;
                    profitrecord.ProfitType = EProfitType.Houston;
                    profitrecord.Channel = EChannel.GoodsOrder;//普通商品订单
                    profitrecord.Title = "邮费";
                    profitrecord.TotalMoney = productorder.FreightMoney;//邮费
                    profitrecord.ProfitMoney = productorder.FreightMoney;//邮费
                    profitrecord.UserId = productorder.SupplierId;
                    profitrecord.SourceUserId = productorder.UserId;
                    profitrecord.CreateDate = DateTime.Now;
                    profitrecord.Channel = EChannel.GoodsOrder;
                    profitrecord.Insert(ds);

                }
                //本地供应商成交后给县级加盟商结算邮费
                if (productorder.FreightMoney > 0)
                {
                    if (ordersetts.Count > 0)
                    {
                        Pd.Supplier supplier = Pd.Supplier.GetById(ds, productorder.SupplierId);
                        if (supplier.SupplierType == 1 && supplier.Subjection != 0 && ordersetts[0].CountyId > 0)
                        {

                            ProfitRecord profitrecord = new ProfitRecord();
                            //profitrecord.Role = ERole.Supplier;
                            profitrecord.OrderId = orderid;
                            profitrecord.ProductId = 0;//邮费的产品Id为0
                            profitrecord.ProfitState = EProfitState.NoArrival;
                            profitrecord.ProfitType = EProfitType.Houston;
                            profitrecord.Channel = EChannel.GoodsOrder;//普通商品订单
                            profitrecord.Title = "邮费";
                            profitrecord.TotalMoney = productorder.FreightMoney;//邮费
                            profitrecord.ProfitMoney = productorder.FreightMoney;//邮费
                            profitrecord.UserId = ordersetts[0].CountyId;
                            profitrecord.SourceUserId = productorder.UserId;
                            profitrecord.CreateDate = DateTime.Now;
                            profitrecord.Channel = EChannel.GoodsOrder;
                            profitrecord.Insert(ds);
                        }
                    }
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
        /// 插入供应商商及各级加盟商收益记录
        /// </summary>
        /// <param name="ds">DataSource</param>
        /// <param name="profitrecord">收益基础数据</param>
        /// <param name="userid">当前收益人</param>
        /// <param name="childlevel">子级加盟商级别</param>
        /// <param name="parentuserid">订单购买者的推荐人</param>
        private static void AddProfitByUser(DataSource ds, ProfitRecord profitrecord, Pd.DifferenceSettlement settlement)
        {
            if (profitrecord.UserId != 0)///供应商结算
            {
                ProfitRecord newrecord = profitrecord;
                    newrecord.ProfitMoney = settlement.CostPrice;///产品出厂价
                newrecord.Insert(ds);
            }
            if (settlement.ProductType == Pd.DifferenceSettlement.EProductType.Routine)///常规订单结算
            {
                ///增加卖货人的收益
                if (settlement.DotId != 0)///销售人结算
                {
                    ProfitRecord newrecord = profitrecord;
                    //newrecord.Role = ERole.Distributor;
                    newrecord.UserId = settlement.DotId;
                    newrecord.ProfitMoney = settlement.DotPrice;
                    newrecord.Insert(ds);
                }
                if (settlement.ParentId != 0)///父级结算
                {
                    ProfitRecord newrecord = profitrecord;
                    //newrecord.Role = ERole.Distributor;
                    newrecord.UserId = settlement.ParentId;
                    newrecord.ProfitMoney = settlement.ParentPrice;
                    newrecord.Insert(ds);
                }
                if (settlement.CountyId != 0)///县级结算
                {
                    ProfitRecord newrecord = profitrecord;
                    //newrecord.Role = ERole.Distributor;
                    newrecord.UserId = settlement.CountyId;
                    newrecord.ProfitMoney = settlement.CountyPrice;
                    newrecord.Insert(ds);
                }
                //if (settlement.ProvinceUserId != 0)///省级结算
                //{
                //    ProfitRecord newrecord = profitrecord;
                //    //newrecord.Role = ERole.Distributor;
                //    newrecord.UserId = settlement.ProvinceUserId;
                //    newrecord.ProfitMoney = profitrecord.TotalMoney * (((Money)settlement.ProvinceRoyaltyRate) / 100);
                //    newrecord.Insert(ds);
                //}
            }


        }


        /// <summary>
        /// 根据付款订单号增加对应的收益
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="orderid"></param>
        /// <returns></returns>
        public static DataStatus AddDistributorProfitByOrder(DataSource ds, string orderid)
        {

            
            ds.Begin();
            try
            {
                Supp.DistributorOrder productorder = Supp.DistributorOrder.GetById(ds, orderid);
                IList<Supp.DistributorDifferenceSettlement> ordersetts = Supp.DistributorDifferenceSettlement.GetListbyOrderId(ds, orderid);
                foreach (Supp.DistributorDifferenceSettlement settlement in ordersetts)
                {
                    Supp.DistributorOrderMapping mapping = Supp.DistributorOrderMapping.GetById(ds, orderid, settlement.ProductId);
                    ProfitRecord profitrecord = new ProfitRecord();
                    profitrecord.OrderId = orderid;
                    profitrecord.ProductId = settlement.ProductId;
                    profitrecord.ProfitState = EProfitState.NoArrival;
                    profitrecord.ProfitType = EProfitType.Houston;
                    profitrecord.Channel = EChannel.WholesaleOrder;//普通商品订单
                    profitrecord.Title = "进货";
                    profitrecord.TotalMoney = mapping.TotalMoney;
                    profitrecord.UserId = productorder.SupplierId;///优先加盟商结算
                    profitrecord.SourceUserId = productorder.UserId;
                    profitrecord.CreateDate = DateTime.Now;
                    profitrecord.Channel = EChannel.WholesaleOrder;
                    //插入收益记录
                    AddDistributorProfitByUser(ds, profitrecord, settlement);
                }
                //给供应商结算邮费
                if (productorder.FreightMoney > 0)///加盟商结算
                {
                    ProfitRecord profitrecord = new ProfitRecord();
                    profitrecord.OrderId = orderid;
                    //profitrecord.Role = ERole.Supplier;
                    profitrecord.ProductId = 0;//邮费的产品Id为0
                    profitrecord.ProfitState = EProfitState.NoArrival;
                    profitrecord.ProfitType = EProfitType.Houston;
                    profitrecord.Channel = EChannel.WholesaleOrder;//普通商品订单
                    profitrecord.Title = "邮费";
                    profitrecord.TotalMoney = productorder.FreightMoney;//邮费
                    profitrecord.ProfitMoney = productorder.FreightMoney;//邮费
                    profitrecord.UserId = productorder.SupplierId;
                    profitrecord.SourceUserId = productorder.UserId;
                    profitrecord.CreateDate = DateTime.Now;
                    profitrecord.Channel = EChannel.WholesaleOrder;
                    profitrecord.Insert(ds);
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
        /// 插入供应商商及各级加盟商收益记录
        /// </summary>
        /// <param name="ds">DataSource</param>
        /// <param name="profitrecord">收益基础数据</param>
        /// <param name="userid">当前收益人</param>
        /// <param name="childlevel">子级加盟商级别</param>
        /// <param name="parentuserid">订单购买者的推荐人</param>
        private static void AddDistributorProfitByUser(DataSource ds, ProfitRecord profitrecord, Supp.DistributorDifferenceSettlement settlement)
        {
            if (profitrecord.UserId != 0)///供应商结算
            {
                ProfitRecord newrecord = profitrecord;
                newrecord.ProfitMoney = settlement.CostPrice;///产品出厂价
                newrecord.Insert(ds);
            }
            if (settlement.ProductType == Pd.DifferenceSettlement.EProductType.Wholesale)///常规订单结算
            {
                /////增加卖货人的收益
                //if (settlement.DotPrice != 0)///销售人结算
                //{
                //    ProfitRecord newrecord = profitrecord;
                //    //newrecord.Role = ERole.Distributor;
                //    newrecord.UserId = settlement.SaleId;
                //    newrecord.ProfitMoney = profitrecord.TotalMoney * (((Money)settlement.SaleRoyaltyRate) / 100);
                //    newrecord.Insert(ds);
                //}
                //if (settlement.ParentId != 0)///父级结算
                //{
                //    ProfitRecord newrecord = profitrecord;
                //    //newrecord.Role = ERole.Distributor;
                //    newrecord.UserId = settlement.ParentId;
                //    newrecord.ProfitMoney = profitrecord.TotalMoney * (((Money)settlement.ParentRoyaltyRate) / 100);
                //    newrecord.Insert(ds);
                //}
                if (settlement.CountyId != 0)///县级结算
                {
                    ProfitRecord newrecord = profitrecord;
                    //newrecord.Role = ERole.Distributor;
                    newrecord.UserId = settlement.CountyId;
                    newrecord.ProfitMoney = settlement.CountyPrice;
                    newrecord.Insert(ds);
                }
                //if (settlement.ProvinceUserId != 0)///县级结算
                //{
                //    ProfitRecord newrecord = profitrecord;
                //    //newrecord.Role = ERole.Distributor;
                //    newrecord.UserId = settlement.ProvinceUserId;
                //    newrecord.ProfitMoney = profitrecord.TotalMoney * (((Money)settlement.ProvinceRoyaltyRate) / 100);
                //    newrecord.Insert(ds);
                //}
            }


        }


        /// <summary>
        /// 根据退款订单号扣除对应加盟商的收益
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="orderid"></param>
        /// <returns></returns>
        public static DataStatus ReduceProfitByOrder(DataSource ds, string orderid)
        {
            
            ds.Begin();
            try
            {
                AfterSalesRecord aftersales = AfterSalesRecord.GetById(ds, orderid);//退款订单
                M.Member shopuser = M.Member.GetById(ds, aftersales.UserId);//买货人信息
                Pd.ProductOrderMapping ordermapping = Pd.ProductOrderMapping.GetById(ds, aftersales.OrderId, aftersales.ProductId);
                Pd.DifferenceSettlement settlement = Pd.DifferenceSettlement.GetbyId(ds, aftersales.OrderId, aftersales.ProductId);

                ProfitRecord profitrecord = new ProfitRecord();
                profitrecord.OrderId = orderid;
                profitrecord.ProfitState = EProfitState.NoArrival;
                profitrecord.ProfitType = EProfitType.Refound;
                profitrecord.Channel = EChannel.GoodsOrder;
                profitrecord.Title = "商品退款";
                profitrecord.ProductId = aftersales.ProductId;
                profitrecord.TotalMoney = aftersales.RefundCount * ordermapping.Price;
                profitrecord.UserId = aftersales.SupplierId;
                profitrecord.SourceUserId = aftersales.UserId;
                profitrecord.CreateDate = DateTime.Now;
                //插入扣除收益记录
                ReduceProfitByUser(ds, profitrecord, settlement, aftersales, ordermapping);
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
        /// 插入扣除的收益记录
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="profitrecord"></param>
        /// <param name="userid"></param>
        /// <param name="childlevel"></param>
        /// <param name="shopuserid"></param>
        private static void ReduceProfitByUser(DataSource ds, ProfitRecord profitrecord, Pd.DifferenceSettlement settlement, AfterSalesRecord aftersales, Pd.ProductOrderMapping ordermapping)
        {
            if (profitrecord.UserId != 0)///供应商结算
            {
                ProfitRecord newrecord = profitrecord;
                ProfitRecord sittRecord = GetByOrderId(ds, aftersales.OrderId, newrecord.UserId);
                if (aftersales.RefundMoney > sittRecord.ProfitMoney)
                    newrecord.ProfitMoney = -sittRecord.ProfitMoney;
                else
                    newrecord.ProfitMoney = -aftersales.RefundMoney;
                newrecord.Insert(ds);

                ///判断是否扣除邮费,邮费大于0
                if (aftersales.FreightAmount > 0)
                {
                    ProfitRecord freightrecord = profitrecord;
                    //freightrecord.Role = ERole.Supplier;
                    freightrecord.Title = "退邮费";
                    freightrecord.ProductId = 0;//邮费的产品Id为0
                    freightrecord.TotalMoney = aftersales.FreightAmount;
                    freightrecord.ProfitMoney = -aftersales.FreightAmount;
                    freightrecord.Insert(ds);
                }
                //本地供应商成交后给扣除县级加盟商结算邮费
                if (aftersales.FreightAmount > 0)
                {
                    Pd.Supplier supplier = Pd.Supplier.GetById(ds, aftersales.SupplierId);
                    if (supplier.SupplierType == 1 && supplier.Subjection != 0 && settlement.CountyId > 0)
                    {

                        ProfitRecord freightrecord = profitrecord;
                        //freightrecord.Role = ERole.Supplier;
                        freightrecord.Title = "退邮费";
                        freightrecord.ProductId = 0;//邮费的产品Id为0
                        freightrecord.UserId = settlement.CountyId;
                        freightrecord.TotalMoney = aftersales.FreightAmount;
                        freightrecord.ProfitMoney = -aftersales.FreightAmount;
                        freightrecord.Insert(ds);
                    }
                }
            }
            if (settlement.ProductType == Pd.DifferenceSettlement.EProductType.Routine)///常规订单结算
            {
                ///增加卖货人的收益
                if (settlement.DotId != 0)///销售人结算
                {
                    ProfitRecord newrecord = profitrecord;
                    //newrecord.Role = ERole.Distributor;
                    newrecord.UserId = settlement.DotId;
                    newrecord.ProfitMoney = -settlement.DotPrice;
                    newrecord.Insert(ds);
                }
                if (settlement.ParentId != 0)///父级结算
                {
                    ProfitRecord newrecord = profitrecord;
                    //newrecord.Role = ERole.Distributor;
                    newrecord.UserId = settlement.ParentId;
                    newrecord.ProfitMoney = -settlement.ParentPrice;
                    newrecord.Insert(ds);
                }
                if (settlement.CountyId != 0)///县级结算
                {
                    ProfitRecord newrecord = profitrecord;
                    //newrecord.Role = ERole.Distributor;
                    newrecord.UserId = settlement.CountyId;
                    newrecord.ProfitMoney = -settlement.CountyPrice;
                    newrecord.Insert(ds);
                }
                //if (settlement.ProvinceUserId != 0)///省级结算
                //{
                //    ProfitRecord newrecord = profitrecord;
                //    //newrecord.Role = ERole.Distributor;
                //    newrecord.UserId = settlement.ProvinceUserId;
                //    newrecord.ProfitMoney = -aftersales.RefundCount * ordermapping.Price * (((Money)settlement.ProvinceRoyaltyRate) / 100);
                //    newrecord.Insert(ds);
                //}
            }
        }


        /// <summary>
        /// 根据退款订单号扣除对应加盟商的收益(进货宝)
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="orderid"></param>
        /// <returns></returns>
        public static DataStatus ReduceDistributorProfitByOrder(DataSource ds, string orderid)
        {
            
            ds.Begin();
            try
            {
                AfterSalesRecord aftersales = AfterSalesRecord.GetById(ds, orderid);//退款订单
                M.Member shopuser = M.Member.GetById(ds, aftersales.UserId);//买货人信息
                Supp.DistributorOrderMapping ordermapping = Supp.DistributorOrderMapping.GetById(ds, aftersales.OrderId, aftersales.ProductId);
                Supp.DistributorDifferenceSettlement settlement = Supp.DistributorDifferenceSettlement.GetbyId(ds, aftersales.OrderId, aftersales.ProductId);
                ProfitRecord profitrecord = new ProfitRecord();
                profitrecord.OrderId = orderid;
                profitrecord.ProfitState = EProfitState.NoArrival;
                profitrecord.ProfitType = EProfitType.Refound;
                profitrecord.Title = "商品退款";
                profitrecord.ProductId = aftersales.ProductId;
                profitrecord.TotalMoney = aftersales.RefundCount * ordermapping.Price;
                profitrecord.UserId = aftersales.SupplierId;
                profitrecord.SourceUserId = aftersales.UserId;
                profitrecord.Channel = EChannel.WholesaleOrder;
                profitrecord.CreateDate = DateTime.Now;
                //插入扣除收益记录
                ReduceDistributorProfitByUser(ds, profitrecord, settlement, aftersales, ordermapping);
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
        /// 插入扣除的收益记录
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="profitrecord"></param>
        /// <param name="userid"></param>
        /// <param name="childlevel"></param>
        /// <param name="shopuserid"></param>
        private static void ReduceDistributorProfitByUser(DataSource ds, ProfitRecord profitrecord, Supp.DistributorDifferenceSettlement settlement, AfterSalesRecord aftersales, Supp.DistributorOrderMapping ordermapping)
        {
            if (profitrecord.UserId != 0)///供应商结算
            {
                ProfitRecord newrecord = profitrecord;
                ProfitRecord sittRecord = GetByOrderId(ds,aftersales.OrderId, newrecord.UserId);
                if (aftersales.RefundMoney > sittRecord.ProfitMoney)
                    newrecord.ProfitMoney = -sittRecord.ProfitMoney;
                else
                    newrecord.ProfitMoney = -aftersales.RefundMoney;
                newrecord.Insert(ds);

                ///判断是否扣除邮费,邮费大于0
                if (aftersales.FreightAmount > 0)
                {
                    ProfitRecord freightrecord = profitrecord;
                    //freightrecord.Role = ERole.Supplier;
                    freightrecord.Title = "退邮费";
                    freightrecord.ProductId = 0;//邮费的产品Id为0
                    freightrecord.TotalMoney = aftersales.FreightAmount;
                    freightrecord.ProfitMoney = -aftersales.FreightAmount;
                    freightrecord.Insert(ds);
                }
            }
            if (settlement.ProductType == Pd.DifferenceSettlement.EProductType.Routine)///常规订单结算
            {
                /////增加卖货人的收益
                //if (settlement.DotId != 0)///销售人结算
                //{
                //    ProfitRecord newrecord = profitrecord;
                //    //newrecord.Role = ERole.Distributor;
                //    newrecord.UserId = settlement.DotId;
                //    newrecord.ProfitMoney = -settlement.DotPrice;
                //    newrecord.Insert(ds);
                //}
                //if (settlement.ParentId != 0)///父级结算
                //{
                //    ProfitRecord newrecord = profitrecord;
                //    //newrecord.Role = ERole.Distributor;
                //    newrecord.UserId = settlement.ParentId;
                //    newrecord.ProfitMoney = -settlement.ParentPrice;
                //    newrecord.Insert(ds);
                //}
                if (settlement.CountyId != 0)///县级结算
                {
                    ProfitRecord newrecord = profitrecord;
                    newrecord.UserId = settlement.CountyId;
                    newrecord.ProfitMoney = -settlement.CountyPrice;
                    newrecord.Insert(ds);
                }
                //if (settlement.ProvinceUserId != 0)///省级结算
                //{
                //    ProfitRecord newrecord = profitrecord;
                //    //newrecord.Role = ERole.Distributor;
                //    newrecord.UserId = settlement.ProvinceUserId;
                //    newrecord.ProfitMoney = -aftersales.RefundCount * ordermapping.Price * (((Money)settlement.ProvinceRoyaltyRate) / 100);
                //    newrecord.Insert(ds);
                //}
            }
        }


        /// <summary>
        /// 查看所有进账的收益
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static Money GetAllMoney(DataSource ds, long userid)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            Money ProfitMoney = Db<ProfitRecord>.Query(ds).Select(S_SUM("ProfitMoney"))
                .Where((W("ProfitType", EProfitType.Houston) | W("ProfitType", EProfitType.Refound)) & (W("ProfitState", EProfitState.Invalid, DbWhereType.NotEqual)) & W("UserId", userid))
                .First<ProfitRecord>().ProfitMoney;
            return ProfitMoney;
        }
        /// <summary>
        /// 查看所有已到账进账的收益
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static Money GetAllHoustonMoney(DataSource ds, long userid)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            Money ProfitMoney = Db<ProfitRecord>.Query(ds).Select(S_SUM("ProfitMoney"))
                .Where((W("ProfitType", EProfitType.Houston) | W("ProfitType", EProfitType.Refound)) & (W("ProfitState", EProfitState.Arrival)) & W("UserId", userid))
                .First<ProfitRecord>().ProfitMoney;
            return ProfitMoney;
        }


        /// <summary>
        /// 查看所有已到账提现的收益
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static Money GetAllOutMoney(DataSource ds, long userid)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            Money ProfitMoney = Db<ProfitRecord>.Query(ds).Select(S_SUM("ProfitMoney"))
                .Where(W("ProfitType", EProfitType.OutOfAccount) & W("ProfitState", EProfitState.Arrival) & W("UserId", userid))
                .First<ProfitRecord>().ProfitMoney;
            return ProfitMoney;
        }



        /// <summary>
        /// 查看所有可提现金额
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static Money GetArrivalMoney(DataSource ds, long userid)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            Money ProfitMoney = Db<ProfitRecord>.Query(ds).Select(S_SUM("ProfitMoney"))
                .Where(((W("ProfitType", EProfitType.Houston) & W("ProfitState", EProfitState.Arrival)) |
                (W("ProfitType", EProfitType.OutOfAccount) & W("ProfitState", EProfitState.Invalid, DbWhereType.NotEqual)) |
                (W("ProfitType", EProfitType.Refound) & W("ProfitState", EProfitState.Arrival))) & W("UserId", userid))
                .First<ProfitRecord>().ProfitMoney;

            return ProfitMoney;
        }
        /// <summary>
        /// 获取提现冻结资金
        /// </summary>
        /// <returns></returns>
        public static Money GetFreezeMoney(DataSource ds, long userid)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            Money ProfitMoney = Db<ProfitRecord>.Query(ds).Select(S_SUM("ProfitMoney"))
                .Where(W("ProfitType", EProfitType.OutOfAccount) & W("ProfitState", EProfitState.NoArrival) & W("UserId", userid))
                .First<ProfitRecord>().ProfitMoney;

            return ProfitMoney;
        }

        /// <summary>
        /// 获取提现冻结资金
        /// </summary>
        /// <returns></returns>
        public static Money GetTransferInMoney(DataSource ds, long userid)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            Money ProfitMoney = Db<ProfitRecord>.Query(ds).Select(S_SUM("ProfitMoney"))
                .Where(W("ProfitType", EProfitType.OutOfAccount) & W("ProfitState", EProfitState.NoArrival) & W("UserId", userid))
                .First<ProfitRecord>().ProfitMoney;
            return ProfitMoney;
        }

        /// <summary>
        /// 获取冻结资金
        /// </summary>
        /// <returns></returns>
        public static Money GetHoustonFreezeMoney(DataSource ds, long userid)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            Money ProfitMoney = Db<ProfitRecord>.Query(ds).Select(S_SUM("ProfitMoney"))
                .Where((W("ProfitType", EProfitType.Houston) | W("ProfitType", EProfitType.Refound)) & W("ProfitState", EProfitState.NoArrival) & W("UserId", userid))
                .First<ProfitRecord>().ProfitMoney;

            return ProfitMoney;
        }

        /// <summary>
        /// 进货宝确认收货后修改收益为已到账
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="orderid"></param>
        public static DataStatus UpdateHoustonStateByDistributor(DataSource ds, string orderid)
        {
            try
            {
                Db<ProfitRecord>.Query(ds).Update()
                    .Set("ProfitState", EProfitState.Arrival)
                    .Where(W("OrderId", orderid))
                    .Execute();
                return DataStatus.Success;
            }
            catch (Exception)
            {
                return DataStatus.Failed;
            }
        }
        /// <summary>
        /// 根据T+7修改收益资金到账状态
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        private static void UpdateHoustonStateByT(DataSource ds, long userid)
        {
            Db<ProfitRecord>.Query(ds).Update()
                .Set("ProfitState", EProfitState.Arrival)
                .Where(W("OrderId")
                .InSelect<Pd.ProductOrder>(("Id"))
                .Where(W("State", Pd.OrderState.Finished) & W("RefundDate", DateTime.Now.AddDays(-7), DbWhereType.LessThan) & W("Id")
                .InSelect<ProfitRecord>(S("OrderId"))
                .Where(W("UserId", userid) & W("ProfitType", EProfitType.Houston) & W("Channel", EChannel.GoodsOrder) & W("ProfitState", EProfitState.NoArrival))
                .Result()
                ).Result())
                .Execute();
        }
        /// <summary>
        /// 根据T+7修改退款资金到账状态
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        private static void UpdateRefoundStateByT(DataSource ds, long userid)
        {
            Db<ProfitRecord>.Query(ds).Update()
                .Set("ProfitState", EProfitState.Arrival)
                .Where(W("ProfitType", EProfitType.Refound) & W("OrderId")
                .InSelect<AfterSalesRecord>(S("Id")).Where(W("ServerState", AfterSalesRecord.EServiceState.Complete) & W("OrderId")
                .InSelect<ProfitRecord>(S("OrderId"))
                .Where(W("UserId", userid) & W("ProfitType", EProfitType.Houston) & W("ProfitState", EProfitState.NoArrival))
                .Result()
                ).Result()
                )
                .Execute();
        }

        /// <summary>
        /// 根据用户获取收益列表
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="show"></param>
        /// <returns></returns>
        public static SplitPageData<ProfitRecord> GetListByUser(DataSource ds, long userid, long index, int size, int show = 8)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            IList<ProfitRecord> list;
            long count;
            list = Db<ProfitRecord>.Query(ds).Select()
                .Where((W("ProfitType", EProfitType.Refound) | W("ProfitType", EProfitType.Houston)) & (W("ProfitState", EProfitState.Arrival) | W("ProfitState", EProfitState.NoArrival)) & W("UserId", userid))
                .OrderBy(D("CreateDate"))
                .ToList<ProfitRecord>(size, index, out count);

            return new SplitPageData<ProfitRecord>(index, size, list, count, show);
        }
        /// <summary>
        /// 根据用户获取收益列表
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="show"></param>
        /// <param name="orderid"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static SplitPageData<ProfitRecord> GetListByUser(DataSource ds, long userid, string orderid, string startDate, string endDate,int type, long index, int size, int show = 8)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            IList<ProfitRecord> list;
            long count;
            DbWhereQueue dw = new DbWhereQueue();
            if (type == -1)
                dw = (W("ProfitType", EProfitType.Refound) | W("ProfitType", EProfitType.Houston)) & (W("ProfitState", EProfitState.Arrival) | W("ProfitState", EProfitState.NoArrival)) & W("UserId", userid);
            else if (type == 1)
                dw = (W("ProfitType", EProfitType.Refound) | W("ProfitType", EProfitType.Houston)) & W("ProfitState", EProfitState.Arrival) & W("UserId", userid);
            else if (type == 2)
                dw = (W("ProfitType", EProfitType.Refound) | W("ProfitType", EProfitType.Houston)) & W("ProfitState", EProfitState.NoArrival) & W("UserId", userid);
            else if (type == 3)
                dw = (W("ProfitType", EProfitType.OutOfAccount) ) & W("ProfitState", EProfitState.NoArrival) & W("UserId", userid);
            else if (type == 4)
                dw = (W("ProfitType", EProfitType.OutOfAccount)) & W("ProfitState", EProfitState.Arrival) & W("UserId", userid);
            else if (type == 5)
                dw = (W("ProfitType", EProfitType.Refound)) & W("ProfitState", EProfitState.NoArrival) & W("UserId", userid);
            else if (type == 6)
                dw = (W("ProfitType", EProfitType.Refound)) & W("ProfitState", EProfitState.Arrival) & W("UserId", userid);
            else
                dw = (W("ProfitType", EProfitType.Refound) | W("ProfitType", EProfitType.Houston)) & (W("ProfitState", EProfitState.Arrival) | W("ProfitState", EProfitState.NoArrival)) & W("UserId", userid);
            if (!string.IsNullOrEmpty(startDate))
            {
                dw &= W("CreateDate", DateTime.Parse(startDate), DbWhereType.GreaterThanOrEqual);
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                dw &= W("CreateDate", DateTime.Parse(endDate), DbWhereType.LessThanOrEqual);
            }
            if (!string.IsNullOrEmpty(orderid))
            {
                dw &= W("OrderId", orderid);
            }
            list = Db<ProfitRecord>.Query(ds).Select()
                .Where(dw)
                .OrderBy(D("CreateDate"))
                .ToList<ProfitRecord>(size, index, out count);

            return new SplitPageData<ProfitRecord>(index, size, list, count, show);
        }  

        /// <summary>
        /// 根据用户获取收益列表
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="show"></param>
        /// <param name="orderid"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static ProfitRecord GetListCountByUser(DataSource ds, long userid, string orderid, string startDate, string endDate,int type)
        {
            ProfitRecord profitrecord;
            long count;
            DbWhereQueue dw = new DbWhereQueue();
            if (type == -1)
                dw = (W("ProfitType", EProfitType.Refound) | W("ProfitType", EProfitType.Houston)) & (W("ProfitState", EProfitState.Arrival) | W("ProfitState", EProfitState.NoArrival)) & W("UserId", userid);
            else if (type == 1)
                dw = (W("ProfitType", EProfitType.Refound) | W("ProfitType", EProfitType.Houston)) & W("ProfitState", EProfitState.NoArrival) & W("UserId", userid);
            else if (type == 2)
                dw = (W("ProfitType", EProfitType.Refound) | W("ProfitType", EProfitType.Houston)) & W("ProfitState", EProfitState.Arrival) & W("UserId", userid);
            else if (type == 3)
                dw = (W("ProfitType", EProfitType.OutOfAccount)) & W("ProfitState", EProfitState.NoArrival) & W("UserId", userid);
            else if (type == 4)
                dw = (W("ProfitType", EProfitType.OutOfAccount)) & W("ProfitState", EProfitState.Arrival) & W("UserId", userid);
            else if (type == 5)
                dw = (W("ProfitType", EProfitType.Refound)) & W("ProfitState", EProfitState.NoArrival) & W("UserId", userid);
            else if (type == 6)
                dw = (W("ProfitType", EProfitType.Refound)) & W("ProfitState", EProfitState.Arrival) & W("UserId", userid);
            else
                dw = (W("ProfitType", EProfitType.Refound) | W("ProfitType", EProfitType.Houston)) & (W("ProfitState", EProfitState.Arrival) | W("ProfitState", EProfitState.NoArrival)) & W("UserId", userid);
            if (!string.IsNullOrEmpty(startDate))
            {
                dw &= W("CreateDate", DateTime.Parse(startDate), DbWhereType.GreaterThanOrEqual);
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                dw &= W("CreateDate", DateTime.Parse(endDate), DbWhereType.LessThanOrEqual);
            }
            if (!string.IsNullOrEmpty(orderid))
            {
                dw &= W("OrderId", orderid);
            }
            profitrecord = Db<ProfitRecord>.Query(ds).Select(S_SUM("TotalMoney"), S_SUM("ProfitMoney"))
                .Where(dw)
                .First<ProfitRecord>();

            return profitrecord;
        }

        /// <summary>
        /// 根据来源用户获取当前用户收益列表
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="show"></param>
        /// <returns></returns>
        public static SplitPageData<ProfitRecord> GetListBySourceUser(DataSource ds, long userid, long sourceid, int index, int size, int show = 8)
        {
            UpdateHoustonStateByT(ds, userid);
            UpdateRefoundStateByT(ds, userid);
            IList<ProfitRecord> list;
            long count;
            list = Db<ProfitRecord>.Query(ds).Select()
                .Where(W("ProfitType", EProfitType.Houston) & W("ProfitState", EProfitState.Arrival) & W("UserId", userid) & W("SourceUserId", sourceid))
                .OrderBy(D("CreateDate"))
                .ToList<ProfitRecord>(size, index, out count);

            return new SplitPageData<ProfitRecord>(index, size, list, count, show);
        }

        public static DataStatus UpdataState(DataSource ds, EProfitState state, string orderid)
        {
            if (Db<ProfitRecord>.Query(ds).Update()
                .Set("ProfitState", state)
                .Where(W("OrderId", orderid))
                .Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }

        /// <summary>
        /// 根据用户获取收益订单
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="show"></param>
        /// <returns></returns>
        [Obsolete]
        public static SplitPageData<dynamic> GetOrderByRecord(DataSource ds, long userid, int index, int size, int show = 8)
        {
            long count;

            //IList<Pd.ProductOrder> list = Db<ProfitRecord>.Query(ds)
            //    .Select(S<ProfitRecord>(), S<Pd.ProductOrder>())
            //    .InnerJoin(O<ProfitRecord>("OrderId"), O<Pd.ProductOrder>("Id"))
            //    .Where(W<ProfitRecord>("UserId", userid))
            //    .OrderBy(D<ProfitRecord>("CreateDate"))
            //    .ToList<Pd.ProductOrder>(size, index, out count);

            IList<Pd.ProductOrder> list = Db<Pd.ProductOrder>.Query(ds)
                .Select()
                .Where(W("Id")
                .InSelect<ProfitRecord>(S("OrderId")).Where(W("UserId", userid)).GroupBy("OrderId").Result())
                .OrderBy(D("PaymentDate"))
                .ToList<Pd.ProductOrder>(size, index, out count);

            List<dynamic> temp;
            ProductCacheInfo pci;
            IList<Pd.ProductOrderMapping> maps;
            List<dynamic> result = new List<dynamic>(list.Count);
            foreach (Pd.ProductOrder item in list)
            {
                maps = item.GetMapping(ds);
                temp = new List<dynamic>(maps.Count);
                foreach (Pd.ProductOrderMapping it in maps)
                {
                    pci = JsonValue.Deserialize<ProductCacheInfo>(it.ProductInfo);
                    temp.Add(new
                    {
                        ProductId = it.ProductId,
                        Image = it.GetImage(pci.Image),
                        ProductInfo = pci,
                        Price = it.Price,
                        TotalMoney = it.TotalMoney,
                        Count = it.Count,
                        Evaluation = it.Evaluation,
                        IsService = it.IsService,
                        AfterSalesOrderId = it.AfterSalesOrderId
                    });
                }
                result.Add(new
                {
                    Id = item.Id,
                    State = (int)item.State,
                    StateInfo = item.GetStateInfo(),
                    CreationDate = item.CreationDate,
                    Address = item.Address,
                    Message = item.Message,
                    Mappings = temp,
                    FreightMoney = item.FreightMoney,
                    TotalMoney = item.TotalMoney
                });
            }
            return new SplitPageData<dynamic>(index, size, result, count, show);
        }


        public static long GetOrderNumByRecord(DataSource ds, long userId)
        {
            return Db<Pd.ProductOrder>.Query(ds)
                .Select()
                .Where(W("Id")
                .InSelect<ProfitRecord>(S("OrderId")).Where(W("UserId", userId)).GroupBy("OrderId").Result())
                .Count();
        }


        /// <summary>
        /// 根据用户获取收益订单
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="show"></param>
        /// <returns></returns>
        public static SplitPageData<dynamic> GetOrderByRecord2(DataSource ds, long userid, int index, int size, int show = 8)
        {
            long count;

            //IList<Pd.ProductOrder> list = Db<ProfitRecord>.Query(ds)
            //    .Select(S<ProfitRecord>(), S<Pd.ProductOrder>())
            //    .InnerJoin(O<ProfitRecord>("OrderId"), O<Pd.ProductOrder>("Id"))
            //    .Where(W<ProfitRecord>("UserId", userid))
            //    .OrderBy(D<ProfitRecord>("CreateDate"))
            //    .ToList<Pd.ProductOrder>(size, index, out count);

            IList<Pd.ProductOrder> list = Db<Pd.ProductOrder>.Query(ds)
                .Select()
                .Where(W("Id")
                .InSelect<ProfitRecord>(S("OrderId")).Where(W("UserId", userid)).GroupBy("OrderId").Result())
                .OrderBy(D("PaymentDate"))
                .ToList<Pd.ProductOrder>(size, index, out count);


            ProductCacheInfo pci;
            IList<Pd.ProductOrderMapping> maps;
            List<dynamic> result = new List<dynamic>();
            foreach (Pd.ProductOrder item in list)
            {
                maps = item.GetMapping(ds);
                List<OrderMappingCacheInfo> temp = new List<OrderMappingCacheInfo>(maps.Count);
                foreach (Pd.ProductOrderMapping it in maps)
                {
                    temp.Add(new OrderMappingCacheInfo(it));
                }
                result.Add(new
                {
                    Order = item,
                    Products = temp,
                });
            }
            return new SplitPageData<dynamic>(index, size, result, count, show);
        }
        /// <summary>
        /// 插入提现记录
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userid">用户</param>
        /// <param name="money">提现金额</param>
        /// <param name="title">标题</param>
        /// <param name="orderid">提现订单号</param>
        /// <returns></returns>
        public static DataStatus WithDrawByMoney(DataSource ds, long userid, Money money, string title, string orderid, EProfitState state)
        {
            ProfitRecord precord = new ProfitRecord
            {
                CreateDate = DateTime.Now,
                OrderId = orderid,
                ProfitMoney = -money,
                ProfitState = state,
                ProfitType = EProfitType.OutOfAccount,
                Title = title,
                TotalMoney = money,
                UserId = userid,
                SourceUserId = userid
            };
            return precord.Insert(ds);
        }
        public static ProfitRecord GetByOrderId(DataSource ds, string orderid, long userid)
        {
            return Db<ProfitRecord>.Query(ds)
                .Select().Where(W("OrderId", orderid) & W("UserId", userid))
                .First<ProfitRecord>();
        }

        public static Money GetAllHoustonMoneyByChannel(DataSource ds, long userid, EChannel channel)
        {
            if (channel == EChannel.GoodsOrder)
                UpdateHoustonStateByT(ds, userid);
            else if (channel == EChannel.WholesaleOrder)
                UpdateHoustonDistributorStateByT(ds, userid);

            Money ProfitMoney = Db<ProfitRecord>.Query(ds).Select(S_SUM("ProfitMoney"))
                .Where(W("ProfitType", EProfitType.Houston) & W("ProfitState", EProfitState.Arrival) & W("Channel", channel) & W("UserId", userid))
                .First<ProfitRecord>().ProfitMoney;
            return ProfitMoney;
        }
        public static Money GetTimeHoustonMoneyByChannel(DataSource ds, long userid, EChannel channel, DateTime begintime, DateTime endtime)
        {
            if (channel == EChannel.GoodsOrder)
                UpdateHoustonStateByT(ds, userid);
            else if (channel == EChannel.WholesaleOrder)
                UpdateHoustonDistributorStateByT(ds, userid);

            Money ProfitMoney = Db<ProfitRecord>.Query(ds).Select(S_SUM("ProfitMoney"))
                .Where(W("ProfitType", EProfitType.Houston) & W("ProfitState", EProfitState.Arrival) & W("Channel", channel) & W("UserId", userid) & W("CreateDate", begintime, DbWhereType.GreaterThanOrEqual) & W("CreateDate", endtime, DbWhereType.LessThan))
                .First<ProfitRecord>().ProfitMoney;
            return ProfitMoney;
        }
        private static void UpdateHoustonDistributorStateByT(DataSource ds, long userid)
        {
            Db<ProfitRecord>.Query(ds).Update()
                .Set("ProfitState", EProfitState.Arrival)
                .Where(W("OrderId")
                .InSelect<Supp.DistributorOrder>(("Id"))
                .Where(W("State", Pd.OrderState.Finished) & W("RefundDate", DateTime.Now.AddDays(7), DbWhereType.LessThan) & W("Id")
                .InSelect<ProfitRecord>(S("OrderId"))
                .Where(W("UserId", userid) & W("ProfitType", EProfitType.Houston) & W("Channel", EChannel.WholesaleOrder) & W("ProfitState", EProfitState.NoArrival))
                .Result()
                ).Result())
                .Execute();
        }

        public static DataStatus UpdataStateForAfter(DataSource ds,string orderid,long productid)
        {
            if (Db<ProfitRecord>.Query(ds).Update().Set("ProfitState", EProfitState.Arrival)
                .Where(W("OrderId", orderid) & W("ProductId", productid) & W("ProfitType", EProfitType.Houston) & W("ProfitState", EProfitState.NoArrival))
                .Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }
    }
}
