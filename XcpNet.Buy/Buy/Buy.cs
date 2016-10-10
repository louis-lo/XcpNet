using System;
using System.Collections.Generic;
using Cnaws.Passport.Controllers;
using Cnaws.Data;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using D = XcpNet.Supplier.Modules.Modules;
using Cnaws;
using Cnaws.Web;
using A = XcpNet.Ad.Modules;
using System.Reflection;
namespace XcpNet.Common
{
    public class CommonBuy
    {
        /// <summary>
        /// 设置订单
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员</param>
        /// <param name="Id">产品编号多个用,隔开</param>
        /// <param name="Count">产品数量多个用,隔开</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object CommSetOrder<T>(DataSource DataSource, M.Member member, string Id, string Count, int province, int city, int county, out object data) where T : class, new()
        {
            object code = null, newdata = null;
            Id = Id.Trim(',');
            Count = Count.Trim(',');
            try
            {
                T t = new T();
                int count;
                if (t is D.DistributorOrder)
                {
                    D.DistributorProduct p;
                    D.DistributorOrderMapping pom;

                    DateTime now = DateTime.Now;
                    string temp = Id;
                    if (string.IsNullOrEmpty(temp))
                    {
                        code = CommUtility.PRODUCT_EMPTY;
                        throw new AggregateException();
                    }
                    string[] ids = temp.Split(',');

                    temp = Count;
                    if (string.IsNullOrEmpty(temp))
                    {
                        code = CommUtility.PRODUCT_SUM_EMPTY;
                        throw new AggregateException();
                    }
                    string[] counts = Count.Split(',');

                    if (ids.Length == 0 || ids.Length != counts.Length)
                    {
                        code = CommUtility.PRODUCT_SUM_ERROR;
                        throw new AggregateException();
                    }

                    List<D.DistributorOrderMapping> ps;
                    KeyValuePair<string, List<D.DistributorOrderMapping>> pair;
                    Dictionary<long, Money> money = new Dictionary<long, Money>();
                    Dictionary<long, KeyValuePair<string, List<D.DistributorOrderMapping>>> OrderForSupplier = new Dictionary<long, KeyValuePair<string, List<D.DistributorOrderMapping>>>();
                    for (int i = 0; i < ids.Length; ++i)
                    {

                        count = int.Parse(counts[i]);
                        if (count <= 0)
                        {
                            code = CommUtility.PRODUCT_SUM_ERROR;
                            throw new AggregateException();
                        }
                        ///判断产品是否该地区和销售和是否在销售
                        p = D.DistributorProduct.GetSaleProduct(DataSource, long.Parse(ids[i]), province, city, county);
                        if (p == null)
                        {
                            newdata = ids[i];
                            code = CommUtility.PRODUCT_ERROR;
                            throw new AggregateException();
                        }

                        if (p.Inventory < count)
                        {
                            newdata = ids[i];
                            code = CommUtility.PRODUCT_INVENTORY_ENOUGH;
                            throw new AggregateException();
                        }

                        if (OrderForSupplier.TryGetValue(p.SupplierId, out pair))
                        {
                            ///根据地区获取订单映射表（价格根据地区获取）
                            pom = new D.DistributorOrderMapping(DataSource, pair.Key, p, count, province, city, county);
                            money[p.SupplierId] = money[p.SupplierId] + pom.TotalMoney;
                            pair.Value.Add(pom);
                        }
                        else
                        {
                            ///根据地区获取订单映射表（价格根据地区获取）
                            pom = new D.DistributorOrderMapping(DataSource, P.ProductOrder.NewId(now, member.Id, i + 1), p, count, province, city, county);
                            money[p.SupplierId] = pom.TotalMoney;
                            ps = new List<D.DistributorOrderMapping>();
                            ps.Add(pom);
                            OrderForSupplier.Add(p.SupplierId, new KeyValuePair<string, List<D.DistributorOrderMapping>>(pom.OrderId, ps));
                        }
                    }

                    string orderId = (OrderForSupplier.Count > 1) ? string.Concat('G', P.ProductOrder.NewId(now, member.Id)) : null;

                    long shopId = 0;

                    P.Distributor distributor = A.MachineCode.GetDistributorByCode(DataSource, member.Mark);
                    if (distributor != null)
                    {
                        shopId = distributor.UserId;
                    }
                    else
                    {
                        shopId = member.Id;
                    }

                    long CurrentSupplie = 0L;
                    DataSource.Begin();
                    try
                    {
                        foreach (KeyValuePair<long, KeyValuePair<string, List<D.DistributorOrderMapping>>> item in OrderForSupplier)
                        {
                            CurrentSupplie = item.Key;
                            D.DistributorOrder order = new D.DistributorOrder()
                            {
                                Id = item.Value.Key,
                                ParentId = orderId,
                                SupplierId = item.Key,
                                ShopId = shopId,
                                UserId = member.Id,
                                Title = "购买产品",
                                State = P.OrderState.Perfect,
                                TotalMoney = money[item.Key],
                                FreightMoney = 0,
                                Address = null,
                                Message = null,
                                Province = province,
                                City = city,
                                County = county,
                                CreationDate = now
                            };

                            P.Supplier supplier = P.Supplier.GetById(DataSource, order.SupplierId);
                            if (order.TotalMoney < supplier.MinOrderPrice)
                            {
                                code = CommUtility.PRICE_NOTENOUGH;
                                throw new AggregateException();
                            }

                            if (order.Insert(DataSource) != DataStatus.Success)
                            {
                                code = CommUtility.ORDER_ADDERROT;
                                throw new AggregateException();
                            }

                            foreach (D.DistributorOrderMapping pm in item.Value.Value)
                            {
                                if (pm.Insert(DataSource) != DataStatus.Success)
                                {
                                    code = CommUtility.ORDER_INFO_ADDERROT;
                                    throw new AggregateException();
                                }
                                else
                                {
                                    if (SetOrderSupplierSettlement(DataSource, pm) != DataStatus.Success)
                                    {
                                        code = CommUtility.ORDER_SETTLEMENT_ADDERROR;
                                        throw new AggregateException();
                                    }
                                }
                            }
                        }
                        DataSource.Commit();

                    }
                    catch (AggregateException)
                    {
                        DataSource.Rollback();
                        data = newdata;
                        return code;
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        data = null;
                        return CommUtility.PROGRAM_ERROR;
                    }
                    string NewOrder = orderId ?? OrderForSupplier[CurrentSupplie].Key;

                    data = new { OrderId = NewOrder };
                    return CommUtility.SUCCESS;

                }
                else if (t is P.ProductOrder)
                {
                    P.Product p;
                    P.ProductOrderMapping pom;

                    DateTime now = DateTime.Now;
                    string temp = Id;
                    if (string.IsNullOrEmpty(temp))
                    {
                        code = CommUtility.PRODUCT_EMPTY;
                        throw new AggregateException();
                    }
                    string[] ids = temp.Split(',');

                    temp = Count;
                    if (string.IsNullOrEmpty(temp))
                    {
                        code = CommUtility.PRODUCT_SUM_EMPTY;
                        throw new AggregateException();
                    }
                    string[] counts = Count.Split(',');

                    if (ids.Length == 0 || ids.Length != counts.Length)
                    {
                        code = CommUtility.PRODUCT_SUM_ERROR;
                        throw new AggregateException();
                    }

                    List<P.ProductOrderMapping> ps;
                    KeyValuePair<string, List<P.ProductOrderMapping>> pair;
                    Dictionary<long, Money> money = new Dictionary<long, Money>();
                    Dictionary<long, int> channel = new Dictionary<long, int>();
                    Dictionary<long, KeyValuePair<string, List<P.ProductOrderMapping>>> OrderForSupplier = new Dictionary<long, KeyValuePair<string, List<P.ProductOrderMapping>>>();
                    for (int i = 0; i < ids.Length; ++i)
                    {

                        count = int.Parse(counts[i]);
                        if (count <= 0)
                        {
                            code = CommUtility.PRODUCT_SUM_ERROR;
                            throw new AggregateException();
                        }
                        ///判断产品是否该地区和销售和是否在销售
                        p = P.Product.GetSaleProduct(DataSource, long.Parse(ids[i]), province, city, county);
                        if (p == null)
                        {
                            newdata = ids[i];
                            code = CommUtility.PRODUCT_ERROR;
                            throw new AggregateException();
                        }

                        if (p.Inventory < count)
                        {
                            newdata = ids[i];
                            code = CommUtility.PRODUCT_INVENTORY_ENOUGH;
                            throw new AggregateException();
                        }
                        
                        if (OrderForSupplier.TryGetValue(p.SupplierId, out pair))
                        {
                            channel[p.SupplierId] = p.ProductType;
                            ///根据地区获取订单映射表（价格根据地区获取）
                            pom = new P.ProductOrderMapping(DataSource, pair.Key, p, count, province, city, county);
                            money[p.SupplierId] = money[p.SupplierId] + pom.TotalMoney;                           
                            pair.Value.Add(pom);
                        }
                        else
                        {
                            channel[p.SupplierId] = p.ProductType;
                            ///根据地区获取订单映射表（价格根据地区获取）
                            pom = new P.ProductOrderMapping(DataSource, P.ProductOrder.NewId(now, member.Id, i + 1), p, count, province, city, county);
                            money[p.SupplierId] = pom.TotalMoney;
                            ps = new List<P.ProductOrderMapping>();
                            ps.Add(pom);
                            OrderForSupplier.Add(p.SupplierId, new KeyValuePair<string, List<P.ProductOrderMapping>>(pom.OrderId, ps));
                        }
                    }

                    string orderId = (OrderForSupplier.Count > 1) ? string.Concat('G', P.ProductOrder.NewId(now, member.Id)) : null;

                    long shopId = 0;
                    P.Distributor distributor = A.MachineCode.GetDistributorByCode(DataSource, member.Mark);
                    if (distributor != null)
                    {
                        shopId = distributor.UserId;
                    }

                    long CurrentSupplie = 0L;
                    DataSource.Begin();
                    try
                    {
                        foreach (KeyValuePair<long, KeyValuePair<string, List<P.ProductOrderMapping>>> item in OrderForSupplier)
                        {
                            CurrentSupplie = item.Key;
                            P.ProductOrder order = new P.ProductOrder()
                            {
                                Id = item.Value.Key,
                                ParentId = orderId,
                                SupplierId = item.Key,
                                ShopId = shopId,
                                UserId = member.Id,
                                Title = "购买产品",
                                State = P.OrderState.Perfect,
                                TotalMoney = money[item.Key],
                                FreightMoney = 0,
                                Address = null,
                                Message = null,
                                Province = province,
                                City = city,
                                County = county,
                                CreationDate = now,
                                Channel=channel[item.Key]
                            };

                            if (order.Insert(DataSource) != DataStatus.Success)
                            {
                                code = CommUtility.ORDER_ADDERROT;
                                throw new AggregateException();
                            }

                            foreach (P.ProductOrderMapping pm in item.Value.Value)
                            {
                                if (pm.Insert(DataSource) != DataStatus.Success)
                                {
                                    code = CommUtility.ORDER_INFO_ADDERROT;
                                    throw new AggregateException();
                                }
                                else
                                {
                                    if (SetOrderSettlement(DataSource, pm) != DataStatus.Success)
                                    {
                                        code = CommUtility.ORDER_SETTLEMENT_ADDERROR;
                                        throw new AggregateException();
                                    }
                                }
                            }
                        }
                        DataSource.Commit();


                    }
                    catch (AggregateException)
                    {
                        DataSource.Rollback();
                        data = newdata;
                        return code;
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        data = null;
                        return CommUtility.PROGRAM_ERROR;
                    }
                    string NewOrder = orderId ?? OrderForSupplier[CurrentSupplie].Key;
                    data = new { OrderId = NewOrder };
                    return CommUtility.SUCCESS;

                }
                else
                {
                    data = null;
                    return CommUtility.PROGRAM_ERROR;
                }
            }
            catch (AggregateException)
            {
                data = newdata;
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }
        /// <summary>
        /// 合并支付
        /// </summary>
        /// <typeparam name="T">订单类型</typeparam>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员信息</param>
        /// <param name="ids">订单号,以","隔开的数组</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object CommMergeOrder<T>(DataSource DataSource, M.Member member, string ids, out object data) where T : class, new()
        {
            object code = null;
            data = null;
            try
            {
                if (ids.IndexOf(",") != -1)
                {
                    T t = new T();
                    string NewOrderId = string.Concat('G', P.ProductOrder.NewId(DateTime.Now, member.Id));
                    if (t is D.DistributorOrder)
                    {
                        string[] orderids = ids.Split(',');
                        foreach (string orderid in orderids)
                        {
                            if (!string.IsNullOrEmpty(orderid))
                            {
                                D.DistributorOrder order = D.DistributorOrder.GetById(DataSource, orderid);
                                if (order != null && order.State == P.OrderState.Payment)
                                {
                                    ///修改订单的父订单
                                    if (D.DistributorOrder.UpdateParentId(DataSource, orderid, NewOrderId) != DataStatus.Success)
                                    {
                                        data = new { OrderId = orderid };
                                        code = CommUtility.ORDER_UPDATE_ERROR;
                                        throw new AggregateException();
                                    }
                                }
                                else
                                {
                                    data = new { OrderId = orderid };
                                    code = CommUtility.ORDER_EMPTY;
                                    throw new AggregateException();
                                }
                            }
                        }
                        data = new { OrderId = NewOrderId };
                        return CommUtility.SUCCESS;
                    }
                    else if (t is P.ProductOrder)
                    {
                        string[] orderids = ids.Split(',');
                        foreach (string orderid in orderids)
                        {
                            if (!string.IsNullOrEmpty(orderid))
                            {
                                P.ProductOrder order = P.ProductOrder.GetById(DataSource, orderid);
                                if (order != null && order.State == P.OrderState.Payment)
                                {
                                    ///修改订单的父订单
                                    if (P.ProductOrder.UpdateParentId(DataSource, orderid, NewOrderId) != DataStatus.Success)
                                    {
                                        data = new { OrderId = orderid };
                                        code = CommUtility.ORDER_UPDATE_ERROR;
                                        throw new AggregateException();
                                    }
                                }
                                else
                                {
                                    data = new { OrderId = orderid };
                                    code = CommUtility.ORDER_EMPTY;
                                    throw new AggregateException();
                                }
                            }
                        }
                        data = new { OrderId = NewOrderId };
                        return CommUtility.SUCCESS;
                    }
                    else
                    {
                        data = null;
                        return CommUtility.PROGRAM_ERROR;
                    }
                }
                else
                {
                    data = null;
                    return CommUtility.PARAMETER_NOFOND;
                }
            }
            catch (AggregateException)
            {
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }
        /// <summary>
        /// 设置订单的交易快照
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="ordermapping">产品映射表</param>
        /// <returns></returns>
        public static DataStatus SetOrderSettlement(DataSource DataSource, P.ProductOrderMapping ordermapping)
        {
            DataSource.Begin();
            try
            {
                P.ProductOrder order = P.ProductOrder.GetById(DataSource, ordermapping.OrderId);
                P.Product product = P.Product.GetById(DataSource, ordermapping.ProductId);
                P.ProductAreaMapping areaMapping = P.ProductAreaMapping.GetById(DataSource, product.Id, order.Province, order.City, order.County);

                ///增加供应商结算快照
                P.DifferenceSettlement settlement = new P.DifferenceSettlement();
                settlement.OrderId = ordermapping.OrderId;
                settlement.ProductId = ordermapping.ProductId;
                if (areaMapping == null)
                {
                    settlement.CostPrice = product.CostPrice * ordermapping.Count;
                    settlement.DotPrice = (product.Price - product.DotPrice) * ordermapping.Count;
                    if (order.Channel == 1)
                    {
                        P.Supplier supplier = P.Supplier.GetById(DataSource, order.SupplierId);
                        if (supplier.SupplierType == 1 && supplier.Subjection != 0)///县级自己的供应商把钱结算给县级
                            settlement.CountyPrice = product.CostPrice + ((product.DotPrice - product.CountyPrice) * ordermapping.Count);
                        else
                            settlement.CountyPrice = (product.DotPrice - product.CountyPrice) * ordermapping.Count;
                    }
                    else if (order.Channel == 2)
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
                        P.Supplier supplier = P.Supplier.GetById(DataSource, order.SupplierId);
                        if (supplier.SupplierType == 1 && supplier.Subjection != 0)///县级自己的供应商把钱结算给县级
                            settlement.CountyPrice = product.CostPrice + ((areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count);
                        else
                            settlement.CountyPrice = (areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count;
                    }
                    else if (order.Channel == 2)
                    {
                        settlement.CountyPrice = (areaMapping.DotPrice - areaMapping.CountyPrice) * ordermapping.Count;
                    }
                }
                if (product.Wholesale && product.WholesalePrice > 0)
                {
                    settlement.ProductType = P.DifferenceSettlement.EProductType.Wholesale;
                }
                else if (product.DiscountState == P.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
                {
                    settlement.ProductType = P.DifferenceSettlement.EProductType.GroupBuy;
                }
                else
                {
                    settlement.ProductType = P.DifferenceSettlement.EProductType.Routine;
                }

                //增加收益快照GetRoyaltyByOrderMapping
                P.Distributor CountyDist = null;
                if (order.ShopId <= 0)
                {
                    P.Distributor distributor = P.Distributor.GetByAreaAndLevel(DataSource, order.Province, order.City, order.County, 1);
                    if (distributor != null)
                    {
                        order.ShopId = distributor.UserId;
                        CountyDist = distributor;
                    }
                }
                if (order.ShopId > 0)
                {
                    if (CountyDist == null)
                        CountyDist = P.Distributor.GetById(DataSource, order.ShopId);
                    if (CountyDist != null)
                    {
                        ///给销售网点增加收益
                        if (CountyDist.Level > 1)
                        {
                            settlement.DotId = CountyDist.UserId;
                            CountyDist = P.Distributor.GetById(DataSource, CountyDist.ParentId);
                            if (CountyDist != null)
                            {
                                ///给镇级增加收益              
                                if (CountyDist.Level > 1)
                                {
                                    settlement.ParentId = CountyDist.UserId;
                                    settlement.ParentPrice = (ordermapping.Price * (Money)0.01) * ordermapping.Count;
                                    settlement.CountyPrice = (settlement.CountyPrice - settlement.ParentPrice) * ordermapping.Count;
                                    CountyDist = P.Distributor.GetById(DataSource, CountyDist.ParentId);
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
                if (settlement.Insert(DataSource) == DataStatus.Success)
                {
                    DataSource.Commit();
                    return DataStatus.Success;
                }
                else
                {
                    DataSource.Rollback();
                    return DataStatus.Rollback;
                }
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
        public static DataStatus SetOrderSupplierSettlement(DataSource DataSource, D.DistributorOrderMapping ordermapping)
        {
            DataSource.Begin();
            try
            {
                D.DistributorOrder order = D.DistributorOrder.GetById(DataSource, ordermapping.OrderId);
                D.DistributorProduct product = D.DistributorProduct.GetById(DataSource, ordermapping.ProductId);
                D.DistributorAreaMapping areaMapping = D.DistributorAreaMapping.GetById(DataSource, product.Id, order.Province, order.City, order.County);

                ///增加供应商结算快照
                D.DistributorDifferenceSettlement settlement = new D.DistributorDifferenceSettlement();
                settlement.OrderId = ordermapping.OrderId;
                settlement.ProductId = ordermapping.ProductId;
                P.Supplier supplier = P.Supplier.GetById(DataSource, order.SupplierId);
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
                    settlement.ProductType = P.DifferenceSettlement.EProductType.Wholesale;
                }
                else if (product.DiscountState == P.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
                {
                    settlement.ProductType = P.DifferenceSettlement.EProductType.GroupBuy;
                }
                else
                {
                    settlement.ProductType = P.DifferenceSettlement.EProductType.Routine;
                }

                //增加收益快照GetRoyaltyByOrderMapping
                P.Distributor CountyDist = null;
                if (order.ShopId <= 0)
                {
                    P.Distributor distributor = P.Distributor.GetByAreaAndLevel(DataSource, order.Province, order.City, order.County, 1);
                    if (distributor != null)
                    {
                        order.ShopId = distributor.UserId;
                        CountyDist = distributor;
                    }
                }
                if (order.ShopId > 0)
                {
                    if (CountyDist == null)
                        CountyDist = P.Distributor.GetById(DataSource, order.ShopId);
                    if (CountyDist != null)
                    {
                        ///给销售网点增加收益
                        if (CountyDist.Level > 1)
                        {
                            CountyDist = P.Distributor.GetById(DataSource, CountyDist.ParentId);
                            if (CountyDist != null)
                            {
                                if (CountyDist.Level > 1)
                                {
                                    CountyDist = P.Distributor.GetById(DataSource, CountyDist.ParentId);
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
                if (settlement.Insert(DataSource) == DataStatus.Success)
                {
                    DataSource.Commit();
                    return DataStatus.Success;
                }
                else
                {
                    DataSource.Rollback();
                    return DataStatus.Rollback;
                }
            }
            catch (Exception)
            {
                DataSource.Rollback();
                return DataStatus.Rollback;
            }


            #region 原来的
            //DataSource.Begin();
            //try
            //{
            //    D.DistributorProduct product = D.DistributorProduct.GetById(DataSource, ordermapping.ProductId);
            //    D.DistributorOrderSettlement settlement = new D.DistributorOrderSettlement
            //    {
            //        OrderId = ordermapping.OrderId,
            //        ProductId = ordermapping.ProductId,
            //        CostPrice = product.CostPrice,
            //        Settlement = product.Settlement,
            //        RoyaltyRate = product.RoyaltyRate
            //    };
            //    if (product.Wholesale && product.WholesalePrice > 0)
            //    {
            //        settlement.ProductType = P.EProductType.Wholesale;
            //    }
            //    else if (product.DiscountState == P.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
            //    {
            //        settlement.ProductType = P.EProductType.GroupBuy;
            //    }
            //    else
            //    {
            //        settlement.ProductType = P.EProductType.Routine;
            //    }
            //    //增加收益快照GetRoyaltyByOrderMapping
            //    D.DistributorOrder order = D.DistributorOrder.GetById(DataSource, ordermapping.OrderId);

            //    long ShopId = 0;
            //    //增加收益快照GetRoyaltyByOrderMapping

            //    P.Distributor distributor = P.Distributor.GetById(DataSource, order.UserId);
            //    if (distributor != null && distributor.UserId != 0)
            //    {
            //        ShopId = order.UserId;
            //    }
            //    else
            //    {
            //        M.Member member = M.Member.GetById(DataSource, order.UserId);
            //        if (member.ParentId != 0)
            //        {
            //            distributor = P.Distributor.GetById(DataSource, member.ParentId);
            //            ShopId = distributor.UserId;
            //        }
            //    }
            //    ///销售人员及加盟商算提成
            //    if (distributor != null && distributor.UserId != 0)
            //    {
            //        if (distributor.Level == 2 || distributor.Level == 3 || distributor.Level == 4)
            //        {
            //            M.Member SaleMember = M.Member.GetById(DataSource, order.UserId);
            //            ///增加推荐人提成
            //            if (distributor.Level == 2)
            //            {
            //                settlement.ParentId = SaleMember.ParentId;
            //                if (settlement.ParentId != 0)
            //                {
            //                    P.Distributor parentD = P.Distributor.GetById(DataSource, settlement.ParentId);
            //                    if (SaleMember.CreationDate.AddYears(3) >= DateTime.Now)///创建三年内有收益
            //                        settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
            //                }
            //            }
            //            else
            //            {
            //                settlement.ParentId = distributor.ParentId;
            //                if (settlement.ParentId != 0)
            //                {
            //                    P.Distributor parentD = P.Distributor.GetById(DataSource, distributor.ParentId);
            //                    settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
            //                    if (parentD.Level > 1)
            //                        distributor.ParentId = parentD.ParentId;
            //                }
            //            }

            //            ///增加县级提成
            //            settlement.CountyUserId = distributor.ParentId;
            //            P.Distributor CountyD = P.Distributor.GetById(DataSource, settlement.CountyUserId);
            //            settlement.CountyRoyaltyRate = int.Parse((CountyD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
            //            ///增加省级提成
            //            settlement.ProvinceUserId = CountyD.ParentId;
            //            P.Distributor ProvinceD = P.Distributor.GetById(DataSource, settlement.ProvinceUserId);
            //            settlement.ProvinceRoyaltyRate = int.Parse((ProvinceD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());

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
            //    else
            //    {
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
            //}
            //catch (Exception)
            //{
            //    DataSource.Rollback();
            //    return DataStatus.Rollback;
            //}
            #endregion 原来的
        }

        /// <summary>
        /// 设置订单
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">用户</param>
        /// <param name="Id">订单号</param>
        /// <param name="Address">地址Id</param>
        /// <param name="Message">留言</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object CommSetPerfect<T>(DataSource DataSource, M.Member member, string Id, string Address, string Message, out object data) where T : class, new()
        {
            object code = null, newdata = null;
            DataSource.Begin();
            try
            {
                string id;
                if (!string.IsNullOrEmpty(Id))
                {
                    T t = new T();

                    Dictionary<string, string> Messages = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(Message))
                    {
                        if (Message.IndexOf('@') != -1)
                        {
                            string[] Attributes = Message.Split('@');
                            foreach (string Attr_Item in Attributes)
                            {
                                if (!string.IsNullOrEmpty(Attr_Item))
                                {
                                    if (Attr_Item.IndexOf('_') != -1)
                                    {
                                        string[] Attr_Value = Attr_Item.Split('_');
                                        if (!string.IsNullOrEmpty(Attr_Value[0]) && !string.IsNullOrEmpty(Attr_Value[1]))
                                        {
                                            Messages.Add(Attr_Value[0], Attr_Value[1]);
                                        }
                                    }
                                }
                            }
                        }
                    }


                    if (t is D.DistributorOrder)
                    {
                        id = Id;
                        //M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Address), member.Id);
                        //供应商收货地址
                        P.Distributor distributor = P.Distributor.GetById(DataSource, member.Id);
                        M.ShippingAddress address = new M.ShippingAddress()
                        {
                            Id = 0,
                            Address = distributor.Address,
                            Province = distributor.Province,
                            City = distributor.City,
                            County = distributor.County,
                            UserId = distributor.UserId,
                            Consignee = distributor.Consignee,
                            Mobile = distributor.Mobile,
                            PostId = distributor.PostId
                        };
                        if (address == null || address.Province == 0 || address.City == 0 || address.County == 0 || string.IsNullOrEmpty(address.Address) || address.Mobile == 0)
                        {
                            newdata = "收货地址为空";
                            code = CommUtility.ADDRESS_EMPTY;
                            throw new AggregateException();
                        }

                        string orderId = Id;
                        IList<D.DistributorOrder> orders = GetSupplierOrders(DataSource, orderId, P.OrderState.Perfect, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {

                            orders = GetSupplierOrdersNoState(DataSource, orderId, member.Id);
                            if (orders == null || orders.Count <= 0 || orders[0] == null)
                            {
                                newdata = "订单不存在";
                                code = CommUtility.ORDER_EMPTY;
                                throw new AggregateException();
                            }
                        }
                        foreach (D.DistributorOrder order in orders)
                        {
                            IList<D.DistributorOrderMapping> ProductOrderMappings = D.DistributorOrderMapping.GetAllByOrder(DataSource, order.Id);
                            if (ProductOrderMappings.Count > 0 && ProductOrderMappings[0].ProductId > 0)
                            {
                                foreach (D.DistributorOrderMapping pm in ProductOrderMappings)
                                {
                                    if (pm.Province != address.Province || pm.City != address.City || pm.County != address.County)
                                    {
                                        D.DistributorProduct p = D.DistributorProduct.GetSaleProduct(DataSource, pm.ProductId, address.Province, address.City, address.County);
                                        if (p == null || p.Inventory <= 0)
                                        {
                                            ///该地区无该产品销售
                                            newdata = pm.ProductId;
                                            code = CommUtility.PRODUCT_ERROR;
                                            throw new AggregateException();
                                        }
                                        D.DistributorOrderMapping pom = new D.DistributorOrderMapping(DataSource, pm.OrderId, p, pm.Count, address.Province, address.City, address.County);
                                        if (D.DistributorOrderMapping.ModByArea(DataSource, pom) != DataStatus.Success)
                                        {
                                            newdata = pm.ProductId;
                                            code = CommUtility.UPDATE_FAIL;
                                            throw new AggregateException();
                                        }
                                    }
                                    try { D.DistributorCart.Remove(DataSource, pm.ProductId, member.Id); }
                                    catch (Exception) { }
                                }
                            }
                            if (order.State != P.OrderState.Payment)
                            {
                                order.State = P.OrderState.Payment;

                                if (order.Province == address.Province && order.City == address.City && order.County == address.County)
                                {
                                    order.FreightMoney = order.GetFreight(DataSource,address.Province, address.City);
                                    order.TotalMoney = order.TotalMoney + order.FreightMoney;
                                }
                                else
                                {
                                    Money TotalMoney = order.GetTotalMoney(DataSource);
                                    order.TotalMoney = TotalMoney;
                                    order.FreightMoney = order.GetFreight(DataSource,address.Province, address.City);
                                    order.TotalMoney = order.TotalMoney + order.FreightMoney;
                                    order.Province = address.Province;
                                    order.City = address.City;
                                    order.County = address.County;
                                }
                                order.Address = address.BuildInfo();
                                if (Messages.ContainsKey(order.Id))
                                    order.Message = Messages[order.Id];
                                if (order.UpdatePerfectByUser(DataSource) != DataStatus.Success)
                                {
                                    newdata = "更新订单失败";
                                    code = CommUtility.ORDER_UPDATE_ERROR;
                                    throw new AggregateException();
                                }
                            }

                        }
                    }
                    else if (t is P.ProductOrder)
                    {
                        id = Id;
                        M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Address), member.Id);
                        if (address == null)
                        {
                            newdata = "收货地址为空";
                            code = CommUtility.ADDRESS_EMPTY;
                            throw new AggregateException();
                        }

                        string orderId = Id;
                        IList<P.ProductOrder> orders = GetOrders(DataSource, orderId, P.OrderState.Perfect, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {

                            orders = GetOrdersNoState(DataSource, orderId, member.Id);
                            if (orders == null || orders.Count <= 0 || orders[0] == null)
                            {
                                newdata = "订单不存在";
                                code = CommUtility.ORDER_EMPTY;
                                throw new AggregateException();
                            }
                        }


                        foreach (P.ProductOrder order in orders)
                        {
                            IList<P.ProductOrderMapping> ProductOrderMappings = P.ProductOrderMapping.GetAllByOrder(DataSource, order.Id);
                            if (ProductOrderMappings.Count > 0 && ProductOrderMappings[0].ProductId > 0)
                            {
                                foreach (P.ProductOrderMapping pm in ProductOrderMappings)
                                {
                                    if (pm.Province != address.Province || pm.City != address.City || pm.County != address.County)
                                    {
                                        P.Product p = P.Product.GetSaleProduct(DataSource, pm.ProductId, address.Province, address.City, address.County);
                                        if (p == null || p.Inventory <= 0)
                                        {
                                            ///该地区无该产品销售
                                            newdata = pm.ProductId;
                                            code = CommUtility.PRODUCT_ERROR;
                                            throw new AggregateException();
                                        }
                                        P.ProductOrderMapping pom = new P.ProductOrderMapping(DataSource, pm.OrderId, p, pm.Count, address.Province, address.City, address.County);
                                        if (P.ProductOrderMapping.ModByArea(DataSource, pom) != DataStatus.Success)
                                        {
                                            newdata = pm.ProductId;
                                            code = CommUtility.UPDATE_FAIL;
                                            throw new AggregateException();
                                        }

                                    }
                                    try { P.ProductCart.Remove(DataSource, pm.ProductId, member.Id); }
                                    catch (Exception) { }
                                }
                            }
                            if (order.State != P.OrderState.Payment)
                            {

                                order.State = P.OrderState.Payment;
                                if (order.Province == address.Province && order.City == address.City && order.County == address.County)
                                {
                                    order.FreightMoney = order.GetFreight(DataSource,  address.Province, address.City);
                                    order.TotalMoney = order.TotalMoney + order.FreightMoney;
                                }
                                else
                                {
                                    Money TotalMoney = order.GetTotalMoney(DataSource);
                                    order.TotalMoney = TotalMoney;
                                    order.FreightMoney = order.GetFreight(DataSource, address.Province, address.City);
                                    order.TotalMoney = order.TotalMoney + order.FreightMoney;
                                    order.Province = address.Province;
                                    order.City = address.City;
                                    order.County = address.County;
                                }
                                order.Address = address.BuildInfo();
                                if (Messages.ContainsKey(order.Id))
                                    order.Message = Messages[order.Id];
                                if (order.UpdatePerfectByUser(DataSource) != DataStatus.Success)
                                {
                                    newdata = "更新订单失败";
                                    code = CommUtility.ORDER_UPDATE_ERROR;
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                data = null;
                                code= CommUtility.ORDER_EMPTY;
                                throw new AggregateException();
                            }
                        }
                    }
                    DataSource.Commit();
                    data = new { OrderId = Id };
                    return CommUtility.SUCCESS;
                }
                else
                {
                    data = null;
                    code= CommUtility.PROGRAM_ERROR;
                    throw new AggregateException();
                }

            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                data = newdata;
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
        /// 添加到购物车
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员</param>
        /// <param name="Id">产品编号</param>
        /// <param name="Count">产品数量</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object CommAddToCart<T>(DataSource DataSource, M.Member member, string Ids, string Counts, int province, int city, int county, out object data) where T : class, new()
        {
            object code = null;
            data = null;
            DataSource.Begin();
            Ids = Ids.Trim(',');
            Counts = Counts.Trim(',');
            try
            {
                T t = new T();
                string temp = Ids;
                if (string.IsNullOrEmpty(temp))
                {
                    code = CommUtility.PRODUCT_SUM_EMPTY;
                    throw new AggregateException();
                }
                string[] ids = temp.Split(',');

                temp = Counts;
                if (string.IsNullOrEmpty(temp))
                {
                    code = CommUtility.PRODUCT_SUM_EMPTY;
                    throw new AggregateException();
                }
                string[] counts = temp.Split(',');
                if (ids.Length == 0 || ids.Length != counts.Length)
                {
                    code = CommUtility.PRODUCT_SUM_ERROR;
                    throw new AggregateException();
                }

                for (int i = 0; i < ids.Length; i++)
                {
                    if (t is D.DistributorCart)
                    {
                        D.DistributorProduct product = D.DistributorProduct.GetSaleProduct(DataSource, long.Parse(ids[i]));
                        if (product == null)
                        {
                            code = CommUtility.PRODUCT_EMPTY;
                            throw new AggregateException();
                        }
                        D.DistributorCart cart = new D.DistributorCart(DataSource, member.Id, product, int.Parse(counts[i]), province, city, county);
                        if (cart.Count <= product.Inventory)
                        {
                            D.DistributorCart productcart = D.DistributorCart.GetProductByUser(DataSource, member.Id, long.Parse(ids[i]));
                            if (productcart == null || productcart.Id <= 0)
                            {
                                if (cart.Add(DataSource) != DataStatus.Success)
                                {
                                    code = CommUtility.UPDATE_FAIL;
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                if (productcart.Count + cart.Count <= product.Inventory)
                                {
                                    productcart.Count += int.Parse(counts[i]);
                                    if (productcart.Update(DataSource) != DataStatus.Success)
                                    {
                                        code = CommUtility.UPDATE_FAIL;
                                        throw new AggregateException();
                                    }
                                }
                                else
                                {
                                    code = CommUtility.PRODUCT_INVENTORY_ENOUGH;
                                    throw new AggregateException();
                                }
                            }
                        }
                        else
                        {
                            code = CommUtility.PRODUCT_INVENTORY_ENOUGH;
                            throw new AggregateException();
                        }
                    }
                    else if (t is P.ProductCart)
                    {

                        P.Product product = P.Product.GetSaleProduct(DataSource, long.Parse(ids[i]));
                        if (product == null)
                        {
                            code = CommUtility.PRODUCT_EMPTY;
                            throw new AggregateException();
                        }
                        P.ProductCart cart = new P.ProductCart(DataSource, member.Id, product, int.Parse(counts[i]), province, city, county);
                        if (cart.Count <= product.Inventory)
                        {
                            P.ProductCart productcart = P.ProductCart.GetProductByUser(DataSource, member.Id, long.Parse(ids[i]));
                            if (productcart == null || productcart.Id <= 0)
                            {
                                if (cart.Add(DataSource) != DataStatus.Success)
                                {
                                    code = CommUtility.UPDATE_FAIL;
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                if (productcart.Count + cart.Count <= product.Inventory)
                                {
                                    productcart.Count += int.Parse(counts[i]);
                                    if (productcart.Update(DataSource) != DataStatus.Success)
                                    {
                                        code = CommUtility.UPDATE_FAIL;
                                        throw new AggregateException();
                                    }
                                }
                                else
                                {
                                    code = CommUtility.PRODUCT_INVENTORY_ENOUGH;
                                    throw new AggregateException();
                                }
                            }

                        }
                        else
                        {
                            code = CommUtility.PRODUCT_INVENTORY_ENOUGH;
                            throw new AggregateException();
                        }

                    }
                    else
                    {
                        code = CommUtility.PROGRAM_ERROR;
                        throw new AggregateException();
                    }
                }
                code = CommUtility.SUCCESS;
                DataSource.Commit();
                return code;

            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                DataSource.Rollback();
                return CommUtility.PROGRAM_ERROR;
            }
        }

        /// <summary>
        /// 刷新购物车数据
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员</param>
        /// <param name="Id">产品编号</param>
        /// <param name="Count">产品数量</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object CommRefreshCart<T>(DataSource DataSource, M.Member member, string Ids, string Counts, out object data) where T : class, new()
        {
            try
            {
                T t = new T();
                if (t is D.DistributorCart)
                {
                    string temp = Ids;
                    if (string.IsNullOrEmpty(temp))
                    {
                        data = null;
                        return CommUtility.PRODUCT_SUM_EMPTY;
                    }
                    string[] ids = temp.Split(',');

                    temp = Counts;
                    if (string.IsNullOrEmpty(temp))
                    {
                        data = null;
                        return CommUtility.PRODUCT_SUM_EMPTY;
                    }
                    string[] counts = temp.Split(',');
                    if (ids.Length == 0 || ids.Length != counts.Length)
                    {
                        data = null;
                        return CommUtility.PRODUCT_SUM_ERROR;
                    }

                    for (int i = 0; i < ids.Length; i++)
                    {
                        D.DistributorCart cart = D.DistributorCart.GetById(DataSource, long.Parse(ids[i]));
                        if (cart == null)
                        {
                            data = ids[i];
                            return CommUtility.CART_EMPTY;
                        }
                        D.DistributorProduct product = D.DistributorProduct.GetSaleProduct(DataSource, cart.ProductId);
                        if (product == null)
                        {
                            data = ids[i];
                            return CommUtility.PRODUCT_EMPTY;
                        }
                        cart.Count = int.Parse(counts[i]);
                        if (cart.Count < product.Inventory)
                        {
                            if (cart.Update(DataSource) != DataStatus.Success)
                            {
                                data = ids[i];
                                return CommUtility.UPDATE_FAIL;
                            }
                        }
                        else
                        {
                            data = ids[i];
                            return CommUtility.PRODUCT_INVENTORY_ENOUGH;
                        }
                    }
                    data = null;
                    return CommUtility.SUCCESS;
                }
                else if (t is P.ProductCart)
                {
                    string temp = Ids;
                    if (string.IsNullOrEmpty(temp))
                    {
                        data = null;
                        return CommUtility.PRODUCT_SUM_EMPTY;
                    }
                    string[] ids = temp.Split(',');

                    temp = Counts;
                    if (string.IsNullOrEmpty(temp))
                    {
                        data = null;
                        return CommUtility.PRODUCT_SUM_EMPTY;
                    }
                    string[] counts = temp.Split(',');
                    if (ids.Length == 0 || ids.Length != counts.Length)
                    {
                        data = null;
                        return CommUtility.PRODUCT_SUM_ERROR;
                    }

                    for (int i = 0; i < ids.Length; i++)
                    {
                        P.ProductCart cart = P.ProductCart.GetById(DataSource, long.Parse(ids[i]));
                        if (cart == null)
                        {
                            data = ids[i];
                            return CommUtility.CART_EMPTY;
                        }
                        P.Product product = P.Product.GetSaleProduct(DataSource, cart.ProductId);
                        if (product == null)
                        {
                            data = ids[i];
                            return CommUtility.PRODUCT_EMPTY;
                        }
                        cart.Count = int.Parse(counts[i]);
                        if (cart.Count < product.Inventory)
                        {
                            if (cart.Update(DataSource) != DataStatus.Success)
                            {
                                data = ids[i];
                                return CommUtility.UPDATE_FAIL;
                            }
                        }
                        else
                        {
                            data = ids[i];
                            return CommUtility.PRODUCT_INVENTORY_ENOUGH;
                        }
                    }
                    data = null;
                    return CommUtility.SUCCESS;
                }
                else
                {
                    data = null;
                    return CommUtility.PROGRAM_ERROR;
                }
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }

        /// <summary>
        /// 从购物车中删除
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员</param>
        /// <param name="Id">产品编号</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object CommDelForCart<T>(DataSource DataSource, M.Member member, string Id, out object data) where T : class, new()
        {
            try
            {
                string temp = Id;
                string[] ids = temp.Split(',');
                if (ids.Length > 0)
                {
                    DataSource.Begin();
                    try
                    {
                        for (int i = 0; i < ids.Length; ++i)
                        {
                            T t = new T();
                            MethodInfo info = typeof(T).GetMethod("RemoveCart");
                            if ((DataStatus)info.Invoke(info, new object[] { DataSource, long.Parse(ids[i]), member.Id }) != DataStatus.Success)
                                throw new Exception();
                        }
                        DataSource.Commit();
                        data = null;
                        return CommUtility.SUCCESS;
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        data = null;
                        return CommUtility.DEL_FAIL;
                    }
                }
                else
                {
                    data = null;
                    return CommUtility.PRODUCT_EMPTY;
                }
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }

        /// <summary>
        /// 取消订单
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员</param>
        /// <param name="Id">订单编号</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object CommCancelOrder<T>(DataSource DataSource, M.Member member, string Id, out object data) where T : class, new()
        {
            object code = DataStatus.Success; data = null;
            try
            {
                string id = Id;
                T t = new T();
                MethodInfo info = typeof(T).GetMethod("GetByState");
                T order = null;
                if (info != null)
                {
                    order = (T)info.Invoke(info, new object[] { DataSource, id, P.OrderState.Payment, member.Id });
                    if (order == null)
                        order = (T)info.Invoke(info, new object[] { DataSource, id, P.OrderState.Perfect, member.Id });
                    if (t is D.DistributorOrder)
                    {
                        if (order == null)
                            order = (T)info.Invoke(info, new object[] { DataSource, id, P.OrderState.Delivery, member.Id });
                    }
                }
                if (order != null)
                {
                    if (t is D.DistributorOrder)
                    {
                        D.DistributorOrder neworder = order as D.DistributorOrder;
                        if (neworder.State == P.OrderState.Delivery)
                        {
                            if (neworder.Payment != "cashondelivery")
                            {
                                data = null;
                                code = CommUtility.ORDER_EMPTY;
                                throw new AggregateException();
                            }
                        }
                    }
                    FieldInfo fi = typeof(T).GetField("State");
                    fi.SetValue(order, P.OrderState.Invalid);
                    MethodInfo info1 = typeof(T).GetMethod("ModOrder");
                    data = null;
                    return info1.Invoke(info1, new object[] { DataSource, order });
                }
                else
                {
                    data = null;
                    return CommUtility.ORDER_EMPTY;
                }
            }
            catch(AggregateException)
            {
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }

        /// <summary>
        /// 删除订单
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员</param>
        /// <param name="Id">订单编号</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object CommDelOrder<T>(DataSource DataSource, M.Member member, string Id, out object data) where T : class, new()
        {
            //DelOrder
            try
            {
                string id = Id;
                MethodInfo info = typeof(T).GetMethod("GetByState");
                T order = null;
                if (info != null)
                {
                    order = (T)info.Invoke(info, new object[] { DataSource, id, P.OrderState.Invalid, member.Id });
                }
                if (order != null)
                {
                    MethodInfo info1 = typeof(T).GetMethod("DelOrder");
                    data = null;
                    return info1.Invoke(info1, new object[] { DataSource, Id });
                }
                else
                {
                    data = null;
                    return CommUtility.ORDER_EMPTY;
                }
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }

        /// <summary>
        /// 确认收货
        /// </summary>
        /// <param name="DataSource">数据源</param>
        /// <param name="member">会员</param>
        /// <param name="Id">订单编号</param>
        /// <param name="data">返回数据</param>
        /// <returns></returns>
        public static object CommSetReceipt<T>(DataSource DataSource, M.Member member, string Id, out object data) where T : class, new()
        {
            DataSource.Begin();
            object code = CommUtility.PROGRAM_ERROR;
            data = null;
            try
            {

                string OrderId = Id;
                MethodInfo info = typeof(T).GetMethod("GetById");
                T productorder = null;
                if (info != null)
                {
                    productorder = (T)info.Invoke(info, new object[] { DataSource, OrderId });
                }
                if (productorder == null)
                {
                    data = null;
                    code= CommUtility.ORDER_EMPTY;
                    throw new AggregateException();
                }
                FieldInfo fi = typeof(T).GetField("State");
                if ((P.OrderState)fi.GetValue(productorder) != P.OrderState.Receipt)
                {
                    data = null;
                    code= CommUtility.ORDER_UPDATE_ERROR;
                    throw new AggregateException();
                }
                MethodInfo info1 = typeof(T).GetMethod("UpdateStateByUser");
                if (info1 != null)
                {
                    T t = new T();
                    data = null;
                    if (t is D.DistributorOrder)
                    {
                        if ((DataStatus)info1.Invoke(productorder, new object[] { DataSource, P.OrderState.Receipt, "" }) != DataStatus.Success)
                        {
                            data = null;
                            code = CommUtility.UPDATE_FAIL;
                            throw new AggregateException();
                        }
                        else
                        {
                            if(AfterSales.Modules.ProfitRecord.UpdateHoustonStateByDistributor(DataSource, Id)!=DataStatus.Success)
                            {
                                data = null;
                                code = CommUtility.UPDATE_FAIL;
                                throw new AggregateException();
                            }
                        }
                    }
                    else
                    {
                        if ((DataStatus)info1.Invoke(productorder, new object[] { DataSource, P.OrderState.Receipt, "" }) != DataStatus.Success)
                        {
                            data = null;
                            code = CommUtility.UPDATE_FAIL;
                            throw new AggregateException();
                        }
                    }
                    data = null;
                    DataSource.Commit();
                    return  DataStatus.Success ;

                }
                else
                {
                    data = null;
                    code= CommUtility.PROGRAM_ERROR;
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
                data = new { Message = ex.Message };
                DataSource.Rollback();
                return CommUtility.PROGRAM_ERROR;
            }
        }

        /// <summary>
        /// 根据用户和订单获取订单集合
        /// </summary>
        /// <param name="DataSource"></param>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static IList<P.ProductOrder> GetOrdersNoState(DataSource DataSource, string id, long userId)
        {
            if (id[0] == 'G')
                return P.ProductOrder.GetListByParentid(DataSource, id, userId);
            return new P.ProductOrder[] { P.ProductOrder.GetById(DataSource, id) };
        }
        /// <summary>
        /// 根据用户和订单获取进货宝订单集合
        /// </summary>
        /// <param name="DataSource"></param>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static IList<D.DistributorOrder> GetSupplierOrdersNoState(DataSource DataSource, string id, long userId)
        {
            if (id[0] == 'G')
                return D.DistributorOrder.GetListByParentid(DataSource, id, userId);
            return new D.DistributorOrder[] { D.DistributorOrder.GetById(DataSource, id) };
        }
        /// <summary>
        /// 根据用户和订单获取进货宝订单集合
        /// </summary>
        /// <param name="DataSource"></param>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static IList<D.DistributorOrder> GetSupplierOrders(DataSource DataSource, string id, P.OrderState state, long userId)
        {
            if (id[0] == 'G')
                return D.DistributorOrder.GetListByState(DataSource, id, state, userId);
            return new D.DistributorOrder[] { D.DistributorOrder.GetByState(DataSource, id, state, userId) };
        }
        /// <summary>
        /// 根据用户和订单以及状态获取订单集合
        /// </summary>
        /// <param name="DataSource"></param>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static IList<P.ProductOrder> GetOrders(DataSource DataSource, string id, P.OrderState state, long userId)
        {
            if (id[0] == 'G')
                return P.ProductOrder.GetListByState(DataSource, id, state, userId);
            return new P.ProductOrder[] { P.ProductOrder.GetByState(DataSource, id, state, userId) };
        }
        /// <summary>
        /// 获取订单总金额
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public static Money SumMoney(IList<P.ProductOrder> orders)
        {
            Money money = 0;
            foreach (P.ProductOrder order in orders)
                money += order.TotalMoney;
            return money;
        }
        /// <summary>
        /// 获取进货宝订单总金额
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public static Money SumSupplierMoney(IList<D.DistributorOrder> orders)
        {
            Money money = 0;
            foreach (P.ProductOrder order in orders)
                money += order.TotalMoney;
            return money;
        }

    }
}
