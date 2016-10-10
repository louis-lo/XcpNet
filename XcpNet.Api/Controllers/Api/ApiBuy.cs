using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Pay;
using System.Collections.Generic;
using System.Threading;
using A = XcpNet.Ad.Modules;
using Py = Cnaws.Pay.Modules;
using Cnaws.Data.Query;
using C = Cnaws.Comment.Modules;

namespace XcpNet.Api.Controllers
{
    public class ApiBuy : CommonControllers
    {
        protected override bool CheckProvider(PayProvider provider)
        {
            return true;
        }



        /// <summary>
        /// 提交订单获取订单号
        /// </summary>
        [HttpPost]
        public void SetOrder()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                PassportAuthentication.SetAuthCookie(true, false, member, Context);
                User.Identity.GetToken();

                try
                {
                    int count;
                    P.Product p;
                    P.ProductOrderMapping pom;
                    DateTime now = DateTime.Now;
                    string temp = Request.Form["Id"];
                    if (string.IsNullOrEmpty(temp))
                    {
                        SetResult(ApiUtility.PRODUCT_EMPTY);
                        throw new AggregateException();
                    }
                    string[] ids = temp.Split(',');

                    temp = Request.Form["Count"];
                    if (string.IsNullOrEmpty(temp))
                    {
                        SetResult(ApiUtility.PRODUCT_SUM_EMPTY);
                        throw new AggregateException();
                    }
                    string[] counts = Request.Form["Count"].Split(',');

                    if (ids.Length == 0 || ids.Length != counts.Length)
                    {
                        SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH);
                        throw new AggregateException();
                    }

                    List<P.ProductOrderMapping> ps;
                    KeyValuePair<string, List<P.ProductOrderMapping>> pair;
                    Dictionary<long, Money> money = new Dictionary<long, Money>();
                    Dictionary<long, KeyValuePair<string, List<P.ProductOrderMapping>>> OrderForSupplier = new Dictionary<long, KeyValuePair<string, List<P.ProductOrderMapping>>>();
                    for (int i = 0; i < ids.Length; ++i)
                    {

                        count = int.Parse(counts[i]);
                        p = P.Product.GetSaleProduct(DataSource, long.Parse(ids[i]));
                        if (p == null)
                        {
                            string a = null;
                            SetResult(ApiUtility.PRODUCT_ERROR, new { OrderId = a, OrderMoney = a, FreightMoney = a, ProductCart = P.ProductCart.GetProductByUser(DataSource, member.Id, long.Parse(ids[i])) });
                            throw new AggregateException();
                        }

                        if (p.Inventory < count)
                        {
                            string a = null;
                            SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH, new { OrderId = a, OrderMoney = a, FreightMoney = a, ProductCart = P.ProductCart.GetProductByUser(DataSource, member.Id, long.Parse(ids[i])) });
                            throw new AggregateException();
                        }

                        if (OrderForSupplier.TryGetValue(p.SupplierId, out pair))
                        {
                            pom = new P.ProductOrderMapping(DataSource, pair.Key, p, count);
                            money[p.SupplierId] = money[p.SupplierId] + pom.TotalMoney;
                            pair.Value.Add(pom);
                        }
                        else
                        {
                            pom = new P.ProductOrderMapping(DataSource, P.ProductOrder.NewId(now, member.Id, i + 1), p, count);
                            money[p.SupplierId] = pom.TotalMoney;
                            ps = new List<P.ProductOrderMapping>();
                            ps.Add(pom);
                            OrderForSupplier.Add(p.SupplierId, new KeyValuePair<string, List<P.ProductOrderMapping>>(pom.OrderId, ps));
                        }
                    }

                    string orderId = (OrderForSupplier.Count > 1) ? string.Concat('G', P.ProductOrder.NewId(now, member.Id)) : null;
                    P.Distributor distributor = A.MachineCode.GetDistributorByCode(DataSource, member.Mark);
                    if (distributor == null)
                    {
                        SetResult(ApiUtility.DISTRIBUTOR_EMPTY);
                        throw new AggregateException();
                    }
                    long shopId = distributor.UserId;

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
                                CreationDate = now
                            };

                            foreach (P.ProductOrderMapping pm in item.Value.Value)
                            {
                                if (pm.Insert(DataSource) != DataStatus.Success)
                                {
                                    SetResult(ApiUtility.ORDER_INFO_ADDERROT);
                                    throw new AggregateException();
                                }
                            }

                            if (order.Insert(DataSource) != DataStatus.Success)
                            {
                                SetResult(ApiUtility.ORDER_ADDERROT);
                                throw new AggregateException();
                            }
                        }
                        DataSource.Commit();
                    }
                    catch (AggregateException)
                    {
                        DataSource.Rollback();
                        return;
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        SetResult(false);
                        return;
                    }

                    string NewOrder = orderId ?? OrderForSupplier[CurrentSupplie].Key;

                    IList<M.ShippingAddress> shippingaddresss = M.ShippingAddress.GetAll(DataSource, member.Id);
                    if (shippingaddresss == null || shippingaddresss.Count == 0)
                    {
                        M.ShippingAddress shippingaddress = new M.ShippingAddress()
                        {
                            Consignee = distributor.Company,
                            Province = distributor.Province,
                            City = distributor.City,
                            County = distributor.County,
                            Address = distributor.Address,
                            Mobile = member.Mobile,
                            PostId = distributor.PostId,
                            IsDefault = true,
                            UserId = member.Id
                        };
                        shippingaddress.Insert(DataSource);
                        shippingaddresss = new List<M.ShippingAddress>() { shippingaddress };
                    }


                    long Address = 0;
                    foreach (M.ShippingAddress shippingaddress in shippingaddresss)
                    {
                        if (shippingaddress.IsDefault)
                            Address = shippingaddress.Id;
                    }
                    if (Address == 0 && shippingaddresss.Count > 0)
                        Address = shippingaddresss[0].Id;


                    Money Total_Money = 0;
                    Money Freight = 0;
                    DataSource.Begin();
                    try
                    {
                        M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, Address, member.Id);
                        if (address == null)
                        {
                            SetResult(ApiUtility.ADDRESS_EMPTY);
                            throw new AggregateException();
                        }

                        IList<P.ProductOrder> orders = GetOrders(NewOrder, P.OrderState.Perfect, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(ApiUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }

                        foreach (P.ProductOrder order in orders)
                        {
                            order.State = P.OrderState.Payment;
                            order.FreightMoney = P.ProductOrder.GetFreight(DataSource, order.Id, address.Province, address.City, order.TotalMoney);
                            Freight += order.FreightMoney;
                            order.TotalMoney = order.TotalMoney + order.FreightMoney;
                            Total_Money += order.TotalMoney;
                            try
                            {
                                order.Address = address.BuildInfo();
                            }
                            catch (Exception)
                            {
                                SetResult(ApiUtility.ADDRESS_EMPTY);
                                throw new AggregateException();
                            }
                            order.Message = "";//Request.Form["Message"];
                            if (order.UpdatePerfectByUser(DataSource) != DataStatus.Success)
                            {
                                SetResult(ApiUtility.ORDER_UPDATE_ERROR);
                                throw new AggregateException();
                            }
                        }
                        DataSource.Commit();
                    }
                    catch (ThreadAbortException) { }
                    catch (AggregateException)
                    {
                        DataSource.Rollback();
                        return;
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        SetResult(false);
                        return;
                    }

                    SetResult(true, new { OrderId = NewOrder, OrderMoney = Total_Money, FreightMoney = Freight, ProductCart = "" });
                }
                catch (AggregateException)
                {
                    DataSource.Rollback();
                    return;
                }
                catch (Exception ex)
                {
                    DataSource.Rollback();
                    SetResult(false);
                    return;
                }
            }
        }

#if (DEBUG)
        public static void SetOrderHelper()
        {
            CheckMemberHelper("ApiBuy", "SetOrder", "提交订单获取订单号")
                .AddArgument("Id", typeof(string), "产品编号(多个编号用','隔开)")
                .AddArgument("Count", typeof(string), "各产品的数量(多个编号用','隔开与Id对应)")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.PRODUCT_EMPTY, "购买的商品为空")
                .AddResult(ApiUtility.ADDRESS_EMPTY, "可能供应商或当前账号收货地址为空")
                .AddResult(ApiUtility.PRODUCT_SUM_EMPTY, "购买商品的数量为空")
                .AddResult(ApiUtility.PRODUCT_SUM_ERROR, "购买的商品以及数量错误")
                .AddResult(ApiUtility.PRODUCT_ERROR, "包含错误商品")
                .AddResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH, "商品库存不足")
                .AddResult(ApiUtility.ORDER_INFO_ADDERROT, "创建订单详情失败")
                .AddResult(ApiUtility.ORDER_ADDERROT, "创建订单失败")
                .AddResult(ApiUtility.ADDRESS_EMPTY, "收货地址为空")
                .AddResult(ApiUtility.ORDER_EMPTY, "订单不存在")
                .AddResult(ApiUtility.ORDER_UPDATE_ERROR, "更新订单失败")
                .AddResult(ApiUtility.FREIGHT_ADDERROR, "添加快递费失败，返回错误信息")
                .AddResult(ApiUtility.DISTRIBUTOR_EMPTY, "根据对应的Mark找不到供应商")
                .AddResult(true, typeof(string), "订单号");
        }
#endif
        /// <summary>
        /// 根据用户获取所有用户订单
        /// </summary>
        public void GetAllOrders()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                string state = "_";
                if (!string.IsNullOrEmpty(Request["state"]))
                    state = Request["state"];
                int size, page;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                if (!int.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                SetResult(P.ProductOrder.GetAjaxPageByUserAndStateApi(DataSource, member.Id, state, page, size));
            }
        }
#if (DEBUG)
        public static void GetAllOrdersHelper()
        {
            CheckMemberHelper("ApiBuy", "GetAllOrders", "根据用户获取所有用户订单")
                 .AddArgument("state", typeof(P.OrderState), "状态,默认为'_'查询所有,对应交易关闭\t等待完善\t等待付款\t等待发货\t等待收货\t等待评价\t交易完成\t申请退款\t等待退货发货\t退货已发货\t退款成功")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddResult(true, typeof(SplitPageData<P.ProductOrder>), "返回订单结果");
        }
#endif

        /// <summary>
        /// 根据总订单号获取订单信息列表
        /// </summary>
        public void GetOrders()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                string id;
                if (!string.IsNullOrEmpty(Request["Id"]))
                {
                    id = Request["Id"];
                    SetResult(GetOrdersNoState(id, member.Id));
                }
            }
        }
#if (DEBUG)
        public static void GetOrdersHelper()
        {
            CheckMemberHelper("ApiBuy", "GetOrders", "根据总订单号获取订单信息列表")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(IList<P.ProductOrder>), "订单号列表");
        }
#endif
        /// <summary>
        /// 设置订单收货地址根据收货地址编号
        /// </summary>
        [HttpPost]
        public void SetPerfect()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                DataSource.Begin();
                try
                {
                    string id;
                    if (!string.IsNullOrEmpty(Request["Id"]))
                    {
                        id = Request["Id"];
                        M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Request.Form["Address"]), member.Id);
                        if (address == null)
                        {
                            SetResult(ApiUtility.ADDRESS_EMPTY, "收货地址为空");
                            throw new AggregateException();
                        }

                        string orderId = Request.Form["Id"];
                        IList<P.ProductOrder> orders = GetOrders(orderId, P.OrderState.Perfect, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(ApiUtility.ORDER_EMPTY, "订单不存在");
                            throw new AggregateException();
                        }
                        foreach (P.ProductOrder order in orders)
                        {
                            order.State = P.OrderState.Payment;
                            order.FreightMoney = P.ProductOrder.GetFreight(DataSource, order.Id, address.Province, address.City, order.TotalMoney);
                            order.TotalMoney = order.TotalMoney + order.FreightMoney;
                            order.Address = address.BuildInfo();
                            order.Message = Request.Form["Message"];

                            if (order.UpdatePerfectByUser(DataSource) != DataStatus.Success)
                            {
                                SetResult(ApiUtility.ORDER_UPDATE_ERROR, "更新订单失败");
                                throw new AggregateException();
                            }

                            IList<P.ProductOrderMapping> ProductOrderMappings = P.ProductOrderMapping.GetAllByOrder(DataSource, order.Id);
                            if (ProductOrderMappings.Count > 0 && ProductOrderMappings[0].ProductId > 0)
                            {
                                foreach (P.ProductOrderMapping pm in ProductOrderMappings)
                                {
                                    try { P.ProductCart.Remove(DataSource, pm.ProductId, member.Id); }
                                    catch (Exception) { }
                                }
                            }
                        }
                    }
                    DataSource.Commit();
                    SetResult(true);
                }
                catch (ThreadAbortException) { }
                catch (AggregateException)
                {
                    DataSource.Rollback();
                    return;

                }
                catch (Exception)
                {
                    DataSource.Rollback();
                    SetResult(false);
                    return;
                }
            }
        }

#if (DEBUG)
        public static void SetPerfectHelper()
        {
            CheckMemberHelper("ApiBuy", "SetPerfect", "设置订单收货地址根据收货地址编号")
                .AddArgument("Id", typeof(string), "订单编号")
                 .AddArgument("Address", typeof(long), "收货地址Id")
                 .AddArgument("Message", typeof(string), "给商家的留言")
                 .AddResult(ApiUtility.ADDRESS_EMPTY, "收货地址为空")
                 .AddResult(ApiUtility.ORDER_EMPTY, "订单不存在")
                 .AddResult(ApiUtility.ORDER_UPDATE_ERROR, "更新订单失败")
                 .AddResult(true, typeof(bool), "是否成功");
        }
#endif

        /// <summary>
        /// 根据订单号获取运费
        /// </summary>
        public void GetFreight()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    IList<P.ProductOrder> orders = GetOrders(Request["Id"], P.OrderState.Perfect, member.Id);
                    M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Request["Address"]), member.Id);
                    Money FreightMoney = 0;
                    foreach (P.ProductOrder item in orders)
                        FreightMoney += P.ProductOrder.GetFreight(DataSource, item.Id, address.Province, address.City, item.TotalMoney);
                    SetResult(true, FreightMoney);
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetFreightHelper()
        {
            CheckMemberHelper("ApiBuy", "GetFreight", "根据订单号获取运费")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddArgument("Address", typeof(long), "收货地址Id")
                .AddResult(true, typeof(Money), "物流费用");
        }
#endif
        /// <summary>
        /// 根据订单号获取订单产品信息列表
        /// </summary>
        public void GetOrderMapping()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                string id;
                if (!string.IsNullOrEmpty(Request["Id"]))
                {
                    id = Request["Id"];
                    IList<P.ProductOrder> orders = GetOrdersNoState(id, member.Id);
                    List<P.ProductOrderMapping> mappings = new List<P.ProductOrderMapping>();
                    foreach (P.ProductOrder order in orders)
                    {
                        IList<P.ProductOrderMapping> mapping = order.GetMapping(DataSource);
                        mappings.AddRange(mapping);
                    }
                    SetResult(mappings);
                }
            }
        }
#if (DEBUG)
        public static void GetOrderMappingHelper()
        {
            CheckMemberHelper("ApiBuy", "GetOrderMapping", "根据订单号获取订单产品信息列表")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(List<P.ProductOrderMapping>), "产品列表");
        }
#endif
        /// <summary>
        /// 获取所有收货地址
        /// </summary>
        public void GetAllShippingAddress()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                SetResult(M.ShippingAddress.GetAll(DataSource, member.Id));
            }
        }
#if (DEBUG)
        public static void GetAllShippingAddressHelper()
        {
            CheckMemberHelper("ApiBuy", "GetAllShippingAddress", "获取所有收货地址")
                .AddResult(true, typeof(IList<M.ShippingAddress>), "产品列表");
        }
#endif
        [HttpPost]
        public void SetShippingAddress()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    M.ShippingAddress address = DbTable.Load<M.ShippingAddress>(Request.Form);
                    address.UserId = member.Id;
                    SetResult(address.Update(DataSource));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void SetShippingAddressHelper()
        {
            CheckMemberHelper("ApiBuy", "SetShippingAddress", "编辑收货地址")
                .AddArgument("Id", typeof(long), "编号")
                .AddArgument("Consignee", typeof(string), "收货人姓名")
                .AddArgument("Mobile", typeof(long), "联系手机号码")
                .AddArgument("PostId", typeof(int), "邮政编码")
                .AddArgument("Province", typeof(int), "省ID")
                .AddArgument("City", typeof(int), "市ID")
                .AddArgument("County", typeof(int), "区ID")
                .AddArgument("Address", typeof(string), "具体地址")
                .AddArgument("IsDefault", typeof(bool), "是否默认收货地址")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif
        [HttpPost]
        public void AddShippingAddress()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    M.ShippingAddress address = DbTable.Load<M.ShippingAddress>(Request.Form);
                    address.UserId = member.Id;
                    SetResult(address.Insert(DataSource));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void AddShippingAddressHelper()
        {
            CheckMemberHelper("ApiBuy", "AddShippingAddress", "添加收货地址")
                .AddArgument("Consignee", typeof(string), "收货人姓名")
                .AddArgument("Mobile", typeof(long), "联系手机号码")
                .AddArgument("PostId", typeof(int), "邮政编码")
                .AddArgument("Province", typeof(int), "省ID")
                .AddArgument("City", typeof(int), "市ID")
                .AddArgument("County", typeof(int), "区ID")
                .AddArgument("Address", typeof(string), "具体地址")
                .AddArgument("IsDefault", typeof(bool), "是否默认收货地址")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif
        [HttpPost]
        public void DelShippingAddress()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    SetResult(new M.ShippingAddress() { Id = long.Parse(Request["Id"]), UserId = member.Id }.Delete(DataSource));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void DelShippingAddressHelper()
        {
            CheckMemberHelper("ApiBuy", "DelShippingAddress", "删除收货地址")
                .AddArgument("Id", typeof(string), "收货地址Id")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif
        private IList<P.ProductOrder> GetOrdersNoState(string id, long userId)
        {
            if (id[0] == 'G')
                return P.ProductOrder.GetListByParentid(DataSource, id, userId);
            return new P.ProductOrder[] { P.ProductOrder.GetById(DataSource, id) };
        }

        private IList<P.ProductOrder> GetOrders(string id, P.OrderState state, long userId)
        {
            if (id[0] == 'G')
                return P.ProductOrder.GetListByState(DataSource, id, state, userId);
            return new P.ProductOrder[] { P.ProductOrder.GetByState(DataSource, id, state, userId) };
        }
        private Money SumMoney(IList<P.ProductOrder> orders)
        {
            Money money = 0;
            foreach (P.ProductOrder order in orders)
                money += order.TotalMoney;
            return money;
        }

        /// <summary>
        /// 关闭订单
        /// </summary>
        [HttpPost]
        public void CancelOrder()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    string id = Request.Form["Id"];
                    P.ProductOrder order = P.ProductOrder.GetByState(DataSource, id, P.OrderState.Payment, member.Id);
                    if (order == null)
                        order = P.ProductOrder.GetByState(DataSource, id, P.OrderState.Perfect, member.Id);
                    if (order != null)
                    {
                        order.State = P.OrderState.Invalid;
                        SetResult(order.Update(DataSource));
                    }
                    else
                    {
                        SetResult(ApiUtility.ORDER_EMPTY);
                    }
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void CancelOrderHelper()
        {
            CheckMemberHelper("ApiBuy", "CancelOrder", "取消订单")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif

        /// <summary>
        /// 删除订单
        /// </summary>
        [HttpPost]
        public void DelOrder()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    string id = Request.Form["Id"];
                    P.ProductOrder order = P.ProductOrder.GetByState(DataSource, id, P.OrderState.Invalid, member.Id);
                    if (order != null)
                        SetResult(order.Delete(DataSource));
                    else
                        SetResult(ApiUtility.ORDER_EMPTY);
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void DelOrderHelper()
        {
            CheckMemberHelper("ApiBuy", "DelOrder", "删除订单")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif
        /// <summary>
        /// 确定收货
        /// </summary>
        [HttpPost]
        public void SetReceipt()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    SetResult((new P.ProductOrder() { Id = Request.Form["Id"], UserId = member.Id }).UpdateStateByUser(DataSource, P.OrderState.Receipt));
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void SetReceiptHelper()
        {
            CheckMemberHelper("ApiBuy", "SetReceipt", "确定收货")
                 .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif



        public void AddToCar()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {

                    P.Product product = P.Product.GetSaleProduct(DataSource, long.Parse(Request.Form["Id"]));
                    if (product == null)
                    {
                        SetResult(ApiUtility.PRODUCT_EMPTY, "找不到商品");
                    }
                    P.ProductCart cart = new P.ProductCart(DataSource, member.Id, product, int.Parse(Request.Form["Count"]));
                    if (cart.Count < product.Inventory)
                    {
                        P.ProductCart productcart = P.ProductCart.GetProductByUser(DataSource, member.Id, long.Parse(Request.Form["Id"]));
                        if (productcart == null || productcart.Id <= 0)
                            SetResult(cart.Add(DataSource));
                        else
                        {
                            productcart.Count += int.Parse(Request.Form["Count"]);
                            SetResult(productcart.Update(DataSource));
                        }
                    }
                    else
                        SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH);
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void AddToCarHelper()
        {
            CheckMemberHelper("ApiBuy", "AddToCar", "加入购物车")
                .AddArgument("Id", typeof(string), "商品编号")
                .AddArgument("Count", typeof(string), "数量")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.PRODUCT_EMPTY, "找不到商品")
                .AddResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH, "库存不足")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif


        public void GetCarList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                IList<DataJoin<P.ProductCart, P.Product>> list = P.ProductCart.GetPageByUser(DataSource, member.Id);
                SetResult(list);
            }
        }
#if (DEBUG)
        public static void GetCarListHelper()
        {
            CheckMemberHelper("ApiBuy", "GetCarList", "获取我的购物车")
                .AddResult(true, typeof(IList<DataJoin<P.ProductCart, P.Product>>), "处理结果");
        }
#endif

        public void DelForCar()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    string temp = Request.Form["Id"];
                    string[] ids = temp.Split(',');
                    if (ids.Length > 0)
                    {
                        DataSource.Begin();
                        try
                        {
                            for (int i = 0; i < ids.Length; ++i)
                            {
                                long cartid = long.Parse(ids[i]);
                                if ((new P.ProductCart() { Id = cartid, UserId = member.Id }).Remove(DataSource) != DataStatus.Success)
                                    throw new Exception();
                            }
                            DataSource.Commit();
                            SetResult(true);
                        }
                        catch (Exception)
                        {
                            DataSource.Rollback();
                            SetResult(ApiUtility.DEL_FAIL);
                        }
                    }
                    else
                    {
                        SetResult(ApiUtility.PRODUCT_EMPTY);
                    }
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void DelForCarHelper()
        {
            CheckMemberHelper("ApiBuy", "DelForCar", "从购物车中删除")
                .AddArgument("Id", typeof(string), "商品编号,多个用逗号隔开")
                .AddResult(ApiUtility.PRODUCT_EMPTY, "产品为空")
                .AddResult(ApiUtility.DEL_FAIL, "删除失败")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif
        public void GetUntreatedSum()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    long paymentsum = P.ProductOrder.GetMyCountByState(DataSource, P.OrderState.Payment, member.Id);
                    long deliverysum = P.ProductOrder.GetMyCountByState(DataSource, P.OrderState.Delivery, member.Id);
                    long receiptsum = P.ProductOrder.GetMyCountByState(DataSource, P.OrderState.Receipt, member.Id);
                    long evaluationsum = Db<P.ProductOrderMapping>.Query(DataSource)
                            .Select(new DbSelect<P.ProductOrderMapping>("*"), new DbSelect<P.ProductOrder>("ReceiptDate"))
                            .InnerJoin(new DbColumn<P.ProductOrderMapping>("OrderId"), new DbColumn<P.ProductOrder>("Id"))
                            .Where(new DbWhere<P.ProductOrder>("UserId", member.Id) & new DbWhere<P.ProductOrderMapping>("Evaluation", false) & new DbWhere<P.ProductOrder>("State", P.OrderState.Finished))
                            .Count();

                    /* P.ProductOrder.GetMyCountByState(DataSource, P.OrderState.Evaluation, member.Id)*/
                    ;
                    long cartsum = P.ProductCart.GetCountByUser(DataSource, member.Id);

                    SetResult(new
                    {
                        PaymentSum = paymentsum,
                        DeliverySum = deliverysum,
                        ReceiptSum = receiptsum,
                        EvaluationSum = evaluationsum,
                        CartSum = cartsum
                    });
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void GetUntreatedSumHelper()
        {
            CheckMemberHelper("ApiBuy", "GetUntreatedSum", "获取未处理的数量")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "处理结果");
        }
#endif

        public void Recharge()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    string provider = Request["provider"];
                    SetResult(Py.PayRecord.Create(DataSource, member.Id, null, "充值", provider, Money.Parse(Request.Form["Money"]), 0, string.Empty).Id);
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

#if (DEBUG)
        public static void RechargeHelper()
        {
            CheckMemberHelper("ApiBuy", "Recharge", "充值")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "获取充值订单号");
        }
#endif


    }
}
