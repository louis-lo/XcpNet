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
using C = Cnaws.Comment.Modules;
using Cnaws.Data.Query;
using D = XcpNet.Supplier.Modules.Modules;
using System.Web;
using System.Text;
using Cnaws.Json;
using Af = XcpNet.AfterSales.Modules;

namespace XcpNet.Api.Controllers
{
    public class CommBuy : CommonControllers
    {
        protected override bool CheckProvider(PayProvider provider)
        {
            return true;
        }
        public static string ClassName = "[type]Buy";
        protected override void OnInitController()
        {
            NotFound();
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
                        SetResult(ApiUtility.PRODUCT_SUM_ERROR);
                        throw new AggregateException();
                    }

                    List<P.ProductOrderMapping> ps;
                    KeyValuePair<string, List<P.ProductOrderMapping>> pair;
                    Dictionary<long, Money> money = new Dictionary<long, Money>();
                    Dictionary<long, KeyValuePair<string, List<P.ProductOrderMapping>>> OrderForSupplier = new Dictionary<long, KeyValuePair<string, List<P.ProductOrderMapping>>>();
                    for (int i = 0; i < ids.Length; ++i)
                    {

                        count = int.Parse(counts[i]);
                        if (count <= 0)
                        {
                            SetResult(ApiUtility.PRODUCT_SUM_ERROR);
                            throw new AggregateException();
                        }
                        p = P.Product.GetSaleProduct(DataSource, long.Parse(ids[i]));
                        if (p == null)
                        {
                            SetResult(ApiUtility.PRODUCT_ERROR, ids[i]);
                            throw new AggregateException();
                        }

                        if (p.Inventory < count)
                        {
                            SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH, ids[i]);
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

                    long shopId = 0;

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
                            P.Product product = P.Product.GetById(DataSource, item.Value.Value[0].ProductId);
                            if (product != null)
                            {
                                order.Channel = product.ProductType;
                            }
                            if (order.Insert(DataSource) != DataStatus.Success)
                            {
                                SetResult(ApiUtility.ORDER_ADDERROT);
                                throw new AggregateException();
                            }

                            foreach (P.ProductOrderMapping pm in item.Value.Value)
                            {
                                if (pm.Insert(DataSource) != DataStatus.Success)
                                {
                                    SetResult(ApiUtility.ORDER_INFO_ADDERROT);
                                    throw new AggregateException();
                                }
                                else
                                {
                                    if (SetOrderSettlement(pm) != DataStatus.Success)
                                    {
                                        SetResult(ApiUtility.ORDER_SETTLEMENT_ADDERROR);
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
                        return;
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        SetResult(false);
                        return;
                    }

                    string NewOrder = orderId ?? OrderForSupplier[CurrentSupplie].Key;

                    SetResult(true, new { OrderId = NewOrder });
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception)
                {
                    SetResult(false);
                    return;
                }
            }
        }

#if (DEBUG)
        public static void SetOrderHelper()
        {
            CheckMemberHelper(ClassName, "SetOrder", "提交订单获取订单号")
                .AddArgument("Id", typeof(string), "产品编号(多个编号用','隔开)")
                .AddArgument("Count", typeof(string), "各产品的数量(多个编号用','隔开与Id对应)")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.PRODUCT_EMPTY, "购买的商品为空")
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
                SetResult(P.ProductOrder.GetAjaxPageByUserAndStateApi(DataSource, member.Id, state, page, size, 11));
            }
        }
#if (DEBUG)
        public static void GetAllOrdersHelper()
        {
            CheckMemberHelper(ClassName, "GetAllOrders", "根据用户获取所有用户订单")
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
                try
                {
                    string id;
                    int state = -99;
                    if (!string.IsNullOrEmpty(Request["Id"]))
                    {
                        id = Request["Id"];
                        if (!int.TryParse(Request["state"], out state))
                        {
                            state = -99;
                        }

                        IList<P.ProductOrder> orders;

                        if (state == -99)
                            orders = GetOrdersNoState(id, member.Id);
                        else
                            orders = GetOrders(id, (P.OrderState)state, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(ApiUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }
                        List<dynamic> temp;
                        Cnaws.Product.ProductCacheInfo pci;
                        IList<P.ProductOrderMapping> maps;
                        maps = orders[0].GetMapping(DataSource);
                        temp = new List<dynamic>(maps.Count);
                        foreach (P.ProductOrderMapping it in maps)
                        {
                            pci = JsonValue.Deserialize<Cnaws.Product.ProductCacheInfo>(it.ProductInfo);
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

                        SetResult(new
                        {
                            Id = orders[0].Id,
                            State = (int)orders[0].State,
                            StateInfo = orders[0].GetStateInfo(),
                            CreationDate = orders[0].CreationDate,
                            Address = orders[0].Address,
                            Message = orders[0].Message,
                            Mappings = temp,
                            FreightMoney = orders[0].FreightMoney,
                            TotalMoney = orders[0].TotalMoney
                        });
                    }
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    SetResult(false, new { Mesage = ex.ToString() });
                }
            }
        }
#if (DEBUG)
        public static void GetOrdersHelper()
        {
            CheckMemberHelper(ClassName, "GetOrders", "根据总订单号获取订单信息列表")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(IList<P.ProductOrder>), "Orders:ProductOrder,Mappings:ProductOrderMapping");
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

                            orders = GetOrdersNoState(orderId, member.Id);
                            if (orders == null || orders.Count <= 0 || orders[0] == null)
                            {
                                SetResult(ApiUtility.ORDER_EMPTY, "订单不存在");
                                throw new AggregateException();
                            }
                        }
                        foreach (P.ProductOrder order in orders)
                        {
                            if (order.State != P.OrderState.Payment)
                            {
                                order.State = P.OrderState.Payment;
                                order.FreightMoney = order.GetFreight(DataSource, address.Province, address.City);
                                order.TotalMoney = order.TotalMoney + order.FreightMoney;
                                order.Address = address.BuildInfo();
                                order.Message = Request.Form["Message"];
                                if (order.UpdatePerfectByUser(DataSource) != DataStatus.Success)
                                {
                                    SetResult(ApiUtility.ORDER_UPDATE_ERROR, "更新订单失败");
                                    throw new AggregateException();
                                }
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
                catch (ThreadAbortException) {
                    DataSource.Rollback();
                    return;
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
            }
        }

#if (DEBUG)
        public static void SetPerfectHelper()
        {
            CheckMemberHelper(ClassName, "SetPerfect", "设置订单收货地址根据收货地址编号")
                .AddArgument("Id", typeof(string), "订单编号")
                 .AddArgument("Address", typeof(long), "收货地址Id")
                 .AddArgument("Message", typeof(string), "给商家的留言")
                 .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
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
                    if (orders.Count <= 0 || orders[0] == null)
                    {
                        SetResult(ApiUtility.ORDER_EMPTY);
                        throw new AggregateException();
                    }
                    M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Request["Address"]), member.Id);
                    if (address == null || address.Id <= 0)
                    {
                        SetResult(ApiUtility.ADDRESS_EMPTY);
                        throw new AggregateException();
                    }
                    Money FreightMoney = 0;
                    foreach (P.ProductOrder item in orders)
                        FreightMoney +=item.GetFreight(DataSource,address.Province, address.City);
                    SetResult(true, FreightMoney);
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    SetResult(false, new { Message = ex.ToString() });
                }
            }
        }
#if (DEBUG)
        public static void GetFreightHelper()
        {
            CheckMemberHelper(ClassName, "GetFreight", "根据订单号获取运费")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddArgument("Address", typeof(long), "收货地址Id")
                .AddResult(ApiUtility.ADDRESS_EMPTY, "找不到地址")
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
                try
                {
                    string id;
                    if (!string.IsNullOrEmpty(Request["Id"]))
                    {
                        id = Request["Id"];
                        IList<P.ProductOrder> orders = GetOrdersNoState(id, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(ApiUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }
                        List<P.ProductOrderMapping> ordermapping = new List<P.ProductOrderMapping>();
                        foreach (P.ProductOrder order in orders)
                        {
                            if (order != null)
                            {
                                IList<P.ProductOrderMapping> plist = P.ProductOrderMapping.GetAllByOrder(DataSource, order.Id);
                                if (plist.Count > 0)
                                    ordermapping.AddRange(plist);
                            }
                            else
                            {
                                SetResult(ApiUtility.ORDER_EMPTY);
                                throw new AggregateException();
                            }
                        }
                        SetResult(ordermapping);
                    }
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception)
                {
                    SetResult(false);
                    return;
                }
            }
        }
#if (DEBUG)
        public static void GetOrderMappingHelper()
        {
            CheckMemberHelper(ClassName, "GetOrderMapping", "根据订单号获取订单产品信息列表")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(List<P.ProductOrderMapping>), "产品列表");
        }
#endif


        private IList<P.ProductOrder> GetOrdersNoState(string id, long userId)
        {
            if (id[0] == 'G')
                return P.ProductOrder.GetListByParentid(DataSource, id, userId);
            return new P.ProductOrder[] { P.ProductOrder.GetById(DataSource, id) };
        }
        private IList<D.DistributorOrder> GetSupplierOrdersNoState(string id, long userId)
        {
            if (id[0] == 'G')
                return D.DistributorOrder.GetListByParentid(DataSource, id, userId);
            return new D.DistributorOrder[] { D.DistributorOrder.GetById(DataSource, id) };
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
        /// 取消订单
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
                        SetResult(ApiUtility.ORDER_EMPTY);
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
            CheckMemberHelper(ClassName, "CancelOrder", "取消订单")
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
            CheckMemberHelper(ClassName, "DelOrder", "删除订单")
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

                    string Id = Request.Form["Id"];
                    P.ProductOrder productorder = P.ProductOrder.GetById(DataSource, Id);
                    if (productorder == null)
                    {
                        SetResult(ApiUtility.ORDER_EMPTY);
                        throw new AggregateException();
                    }
                    if (productorder.State != P.OrderState.Receipt)
                    {
                        SetResult(ApiUtility.ORDER_UPDATE_ERROR);
                        throw new AggregateException();
                    }
                    SetResult((new P.ProductOrder() { Id = Request.Form["Id"], UserId = member.Id }).UpdateStateByUser(DataSource, P.OrderState.Receipt));
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
        public static void SetReceiptHelper()
        {
            CheckMemberHelper(ClassName, "SetReceipt", "确定收货")
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
                        SetResult(ApiUtility.PRODUCT_EMPTY);
                    }
                    P.ProductCart cart = new P.ProductCart(DataSource, member.Id, product, int.Parse(Request.Form["Count"]));
                    if (cart.Count <= product.Inventory)
                    {
                        P.ProductCart productcart = P.ProductCart.GetProductByUser(DataSource, member.Id, long.Parse(Request.Form["Id"]));
                        if (productcart == null || productcart.Id <= 0)
                            SetResult(cart.Add(DataSource));
                        else
                        {
                            if (productcart.Count + cart.Count <= product.Inventory)
                            {
                                productcart.Count += int.Parse(Request.Form["Count"]);
                                SetResult(productcart.Update(DataSource));
                            }
                            else
                                SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH);
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
            CheckMemberHelper(ClassName, "AddToCar", "加入购物车")
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
            CheckMemberHelper(ClassName, "GetCarList", "获取我的购物车")
                .AddResult(true, typeof(IList<DataJoin<P.ProductCart, P.Product>>), "处理结果");
        }
#endif

        public void RefreshCart()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    string temp = Request.Form["Ids"];
                    if (string.IsNullOrEmpty(temp))
                    {
                        SetResult(ApiUtility.PRODUCT_SUM_EMPTY);
                        throw new AggregateException();
                    }
                    string[] ids = temp.Split(',');

                    temp = Request.Form["Counts"];
                    if (string.IsNullOrEmpty(temp))
                    {
                        SetResult(ApiUtility.PRODUCT_SUM_EMPTY);
                        throw new AggregateException();
                    }
                    string[] counts = temp.Split(',');
                    if (ids.Length == 0 || ids.Length != counts.Length)
                    {
                        SetResult(ApiUtility.PRODUCT_SUM_ERROR);
                        throw new AggregateException();
                    }

                    for (int i = 0; i < ids.Length; i++)
                    {
                        P.ProductCart cart = P.ProductCart.GetById(DataSource, long.Parse(ids[i]));
                        if (cart == null)
                        {
                            SetResult(ApiUtility.CART_EMPTY, ids[i]);
                            throw new AggregateException();
                        }
                        P.Product product = P.Product.GetSaleProduct(DataSource, cart.ProductId);
                        if (product == null)
                        {
                            SetResult(ApiUtility.PRODUCT_EMPTY, ids[i]);
                            throw new AggregateException();
                        }
                        cart.Count = int.Parse(counts[i]);
                        if (cart.Count < product.Inventory)
                        {
                            if (cart.Update(DataSource) != DataStatus.Success)
                            {
                                SetResult(ApiUtility.UPDATE_FAIL, ids[i]);
                                throw new AggregateException();
                            }
                        }
                        else
                        {
                            SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH, ids[i]);
                            throw new AggregateException();
                        }
                    }
                    SetResult(true);
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception)
                {
                    SetResult(ApiUtility.PROGRAM_ERROR);
                }
            }
        }
#if (DEBUG)
        public static void RefreshCartHelper()
        {
            CheckMemberHelper(ClassName, "RefreshCart", "刷新购物车")
                .AddArgument("Ids", typeof(string), "购物车对应编号,多个用逗号隔开")
                .AddArgument("Counts", typeof(string), "商品数量,多个用逗号隔开")
                .AddResult(ApiUtility.PRODUCT_EMPTY, "购买商品为空返回商品Id")
                .AddResult(ApiUtility.UPDATE_FAIL, "修改购物车失败返回商品Id")
                .AddResult(ApiUtility.INSERT_FAIL, "插入购物车返回商品Id")
                .AddResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH, "商品库存不足返回商品Id")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "处理结果,失败返回对应结果以及出错产品ID");
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
                                if ((new P.ProductCart() { Id = long.Parse(ids[i]), UserId = member.Id }).Remove(DataSource) != DataStatus.Success)
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
            CheckMemberHelper(ClassName, "DelForCar", "从购物车中删除")
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
                            .Count();/*P.ProductOrder.GetMyCountByState(DataSource, P.OrderState.Evaluation, member.Id)*/
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
            CheckMemberHelper(ClassName, "GetUntreatedSum", "获取未处理的数量")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "处理结果,PaymentSum:待付款数,DeliverySum:待发货数,ReceiptSum:待收货数,EvaluationSum:待评论数,CartSum:购物车数");
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
            CheckMemberHelper(ClassName, "Recharge", "充值")
                .AddArgument("provider", typeof(string), "Alipay,AlipayApp,Wxpay,AlipayDirect,AlipayGateway,AlipayMobile")
                .AddArgument("Money", typeof(double), "金额")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "获取充值订单号");
        }
#endif

        public void GetLogistics()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                string orderid = Request["OrderId"];
                P.ProductLogistics logistics;
                Cnaws.Product.Logistics.ExpressInfo expressinfo = Cnaws.Product.Logistics.ExpressQuery.Query(DataSource, orderid, out logistics);
                if (expressinfo != null && expressinfo.data != null)
                {
                    SetResult(new { Logistics = logistics, Message = expressinfo.data, State = expressinfo.state });
                }
                else
                {
                    SetResult(ApiUtility.LOGISTICS_EMPTY);
                }

            }
        }
#if (DEBUG)
        public static void GetLogisticsHelper()
        {
            CheckMemberHelper(ClassName, "GetLogistics", "根据订单号获取物流信息")
                .AddArgument("OrderId", typeof(string), "订单编号")
                .AddResult(true, typeof(Cnaws.Product.Logistics.ExpressInfo), "物流信息");
        }
#endif

        public void GetOrderPayMent()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                string orderid = Request["Id"];
                IList<P.ProductOrder> order = GetOrdersNoState(orderid, member.Id);
                if (order != null && order.Count > 0 && order[0] != null)
                {
                    SetResult(order[0]);
                }
                else
                {
                    IList<D.DistributorOrder> distributororder = GetSupplierOrdersNoState(orderid, member.Id);
                    if (distributororder != null && distributororder.Count > 0 && distributororder[0] != null)
                    {
                        SetResult(distributororder[0]);
                    }
                    else
                    {
                        SetResult(ApiUtility.ORDER_EMPTY);
                    }
                }
            }
        }
#if (DEBUG)
        public static void GetOrderPayMentHelper()
        {
            CheckMemberHelper(ClassName, "GetOrderPayMent", "根据订单获取订单信息和状态")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(Cnaws.Product.Logistics.ExpressInfo), "物流信息");
        }
#endif
        private IList<D.DistributorOrder> GetSupplierOrders(string id, P.OrderState state, long userId)
        {
            if (id[0] == 'G')
                return D.DistributorOrder.GetListByState(DataSource, id, state, userId);
            return new D.DistributorOrder[] { D.DistributorOrder.GetByState(DataSource, id, state, userId) };
        }

        private Money SumSupplierMoney(IList<D.DistributorOrder> orders)
        {
            Money money = 0;
            foreach (P.ProductOrder order in orders)
                money += order.TotalMoney;
            return money;
        }

        protected override IPayOrder GetPayOrder(string provider)
        {
            DataSource.Begin();
            try
            {
                long UserId = User.Identity.Id;
                string pwd = Request.Form["PayPassword"];
                if (!string.IsNullOrEmpty(pwd))
                {
                    if (!M.MemberInfo.ApiCheckPayPassword(DataSource, UserId, pwd))
                    {
                        SetResult(ApiUtility.PASSWORD_EQUALS);
                        throw new AggregateException();
                    }
                }
                string orderId = Request.Form["Id"];
                IList<P.ProductOrder> orders = GetOrders(orderId, P.OrderState.Payment, UserId);
                Py.PayRecord pr;
                if (orders != null && orders.Count > 0 && orders[0] != null)
                {
                    //处理产品订单
                    string openId = null;
                    M.OAuth2Member member = M.OAuth2Member.GetByUserPay(DataSource, provider, UserId);
                    if (member != null)
                        openId = member.UserId;

                    pr = Py.PayRecord.GetByUser(DataSource, orderId, UserId, PaymentType.Pay, PayStatus.Paying);
                    if (pr == null)
                    {
                        pr = P.ProductOrder.GetPayRecord(provider, orderId, UserId, orders[0].Title, SumMoney(orders), openId);
                        if (pr.Insert(DataSource) != DataStatus.Success)
                        {
                            SetResult(ApiUtility.INSERT_FAIL, pr);
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        pr.Provider = provider;
                        pr.OpenId = openId;
                        pr.Money = SumMoney(orders);
                        if (pr.Update(DataSource, ColumnMode.Include, "Provider", "OpenId", "Money") != DataStatus.Success)
                        {
                            SetResult(ApiUtility.UPDATE_FAIL);
                            throw new AggregateException();
                        }
                    }
                }
                else
                {
                    //处理进货订单
                    IList<D.DistributorOrder> supplierorder = GetSupplierOrders(orderId, P.OrderState.Payment, UserId);
                    if (supplierorder == null || supplierorder.Count <= 0 || supplierorder[0] == null)
                    {
                        SetResult(ApiUtility.ORDER_EMPTY);
                        throw new AggregateException();
                    }
                    string openId = null;
                    M.OAuth2Member member = M.OAuth2Member.GetByUserPay(DataSource, provider, UserId);
                    if (member != null)
                        openId = member.UserId;

                    pr = Py.PayRecord.GetByUser(DataSource, orderId, UserId, PaymentType.Pay, PayStatus.Paying);
                    if (pr == null)
                    {
                        //插入type为5的参数
                        pr = P.ProductOrder.GetPayRecord(provider, orderId, UserId, supplierorder[0].Title, SumSupplierMoney(supplierorder), openId, P.ProductOrder.PayWholesaleType);
                        if (pr.Insert(DataSource) != DataStatus.Success)
                        {
                            SetResult(ApiUtility.INSERT_FAIL);
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        pr.Provider = provider;
                        pr.OpenId = openId;
                        pr.Money = SumSupplierMoney(supplierorder);
                        if (pr.Update(DataSource, ColumnMode.Include, "Provider", "OpenId", "Money") != DataStatus.Success)
                        {
                            SetResult(ApiUtility.UPDATE_FAIL);
                            throw new AggregateException();
                        }

                    }
                }
                DataSource.Commit();
                return pr;
            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                throw new AggregateException();
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                throw ex;
            }
        }

        public new void Submit(string provider)
        {
            M.Member newmember;
            if (CheckMember(out newmember))
            {
                Cnaws.Web.PassportAuthentication.SetAuthCookie(true, false, newmember);
                PayProvider pay = LoadProvider(provider);
                if (pay != null)
                {
                    if (pay.IsNeedSubmit)
                    {
                        DataSource.Begin();
                        try
                        {
                            IPayOrder order = GetPayOrder(pay.Key);
                            if (order == null)
                            {
                                SetResult(ApiUtility.ORDER_EMPTY);
                                throw new AggregateException();
                            }
                            if (pay.IsOnlinePay)
                            {
                                OnSubmit(pay.Submit(this, pay.PackData(order), SubmitText, ReturnUrl));
                            }
                            else
                            {
                                PaymentResult result;
                                bool value = CheckMoney(order, out result);
                                OnCallback(pay, value, result);
                                SetResult(true);
                                //try { Response.Redirect(ReturnUrl, true); }
                                //catch (Exception) { }
                            }
                            DataSource.Commit();
                        }
                        catch (AggregateException)
                        {
                            DataSource.Rollback();
                            return;
                        }
                        catch (Exception ex)
                        {
                            DataSource.Rollback();
                            SetResult(false, new { Message = ex.ToString() });
                            return;
                        }
                    }
                    else
                    {
                        SetResult(false, new { Message = "需要单独表单提交" });
                        return;
                    }
                }
                else
                {
                    SetResult(false, new { Message = "支付方式未开启" });
                    return;
                }
            }
        }
#if (DEBUG)
        public static void SubmitHelper()
        {
            CheckMemberHelper(ClassName, "Submit/{支付方式}", "根据订单和支付方式支付")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddArgument("PayPassword", typeof(string), "支付密码(md5)")
                .AddResult(true, typeof(string), "成功返回-200");
        }
#endif
        protected override bool CheckMoney(IPayOrder order, out PaymentResult result)
        {
            Py.PayRecord pr = Py.PayRecord.GetById(DataSource, order.TradeNo, PaymentType.Pay);
            int type = P.ProductOrder.PayProductType;
            if (pr != null)
                type = pr.Type;
            DataStatus status = M.MemberInfo.ModifyMoney(DataSource, User.Identity.Id, -order.TotalFee, order.Subject, type, order.TradeNo);
            result = new PayResult()
            {
                TradeNo = order.TradeNo,
                PayTradeNo = order.TradeNo,
                Status = status.ToString(),
                TotalFee = order.TotalFee
            };
            return status == DataStatus.Success;
        }

        protected override void OnCallback(PayProvider provider, bool success, PaymentResult result)
        {
            #region Pay
            if (result.Type == PaymentType.Pay)
            {
                PayResult presult = (PayResult)result;
                if (!string.IsNullOrEmpty(presult.TradeNo))
                {
                    DataSource.Begin();
                    try
                    {
                        Py.PayRecord pr = Py.PayRecord.GetById(DataSource, presult.TradeNo, result.Type);
                        if (pr == null)
                        {
                            SetResult(ApiUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }
                        if (pr.Status >= PayStatus.Paying && pr.Status < PayStatus.PaySuccess)
                        {
                            PayStatus status = pr.Status;
                            pr.PayId = presult.PayTradeNo;
                            if (provider.IsOnlinePay)
                                pr.Money = presult.TotalFee;
                            if (success)
                            {
                                if (provider.IsNeedNotify)
                                    pr.Status = PayStatus.PayNotifying;
                                else
                                    pr.Status = PayStatus.PaySuccess;
                            }
                            else
                            {
                                if (provider.IsOnlinePay && !provider.IsNeedNotify)
                                {
                                    pr.Status = PayStatus.PayFailed;
                                }
                                else
                                {
                                    if (!provider.IsOnlinePay)
                                    {
                                        SetResult(ApiUtility.MONEY_NOT_ENOUGH);
                                        throw new AggregateException();
                                    }
                                }
                            }
                            if (pr.UpdateStatus(DataSource, status) == DataStatus.Success)
                            {
                                if (success && pr.Status == PayStatus.PaySuccess)
                                {
                                    if (!OnModifyMoney(provider, pr.PayType, pr.UserId, presult.TotalFee, pr.Id, pr.Title, pr.Type, pr.TargetId))
                                    {
                                        SetResult(ApiUtility.RECHARGE_FAIL);
                                        throw new AggregateException();
                                    }
                                }
                            }
                            else
                            {
                                SetResult(ApiUtility.UPDATE_FAIL);
                                throw new AggregateException();
                            }
                        }
                        DataSource.Commit();
                    }
                    catch (AggregateException)
                    {
                        DataSource.Rollback();
                        throw new AggregateException();
                    }
                    catch (Exception ex)
                    {
                        DataSource.Rollback();
                        throw ex;
                    }
                }
                else
                {
                    SetResult(ApiUtility.ORDER_EMPTY);
                    throw new AggregateException();
                }
            }
            #endregion

            #region Refund
            else if (result.Type == PaymentType.Refund)
            {

            }
            #endregion

            else
            {
                SetResult(ApiUtility.PAYMENT_ERROR);
                throw new AggregateException();
            }
        }

        protected override bool OnModifyMoney(PayProvider provider, PaymentType payment, long user, Money money, string trade, string title, int type, string targetId)
        {
            try
            {
                if (payment == PaymentType.Pay)
                {
                    if (provider.IsOnlinePay)
                    {
                        if (M.MemberInfo.ModifyMoney(DataSource, user, money, "充值", type, trade) != DataStatus.Success)
                        {
                            SetResult(ApiUtility.RECHARGE_FAIL);
                            throw new AggregateException();
                        }
                    }
                    ///处理产品订单
                    if (type == P.ProductOrder.PayProductType)
                    {
                        bool end = false;
                        DataSource.Begin();
                        try
                        {
                            IList<P.ProductOrder> orders = GetOrders(targetId, P.OrderState.Payment, user);
                            foreach (P.ProductOrder order in orders)
                            {
                                if (order == null)
                                {
                                    SetResult(ApiUtility.ORDER_EMPTY);
                                    throw new AggregateException();
                                }
                                if (user != order.UserId)
                                {
                                    SetResult(ApiUtility.MEMBER_NOTFOUND);
                                    throw new AggregateException();
                                }

                                M.MemberInfo member = M.MemberInfo.GetByRecharge(DataSource, user);
                                if (member == null)
                                {
                                    SetResult(ApiUtility.MEMBER_NOTFOUND);
                                    throw new AggregateException();
                                }
                                if (provider.IsOnlinePay)
                                {
                                    if (member.Money < order.TotalMoney)
                                    {
                                        SetResult(ApiUtility.MONEY_NOT_ENOUGH);
                                        throw new AggregateException();
                                    }
                                    if (M.MemberInfo.ModifyMoney(DataSource, user, -order.TotalMoney, order.Title, type, targetId) != DataStatus.Success)
                                    {
                                        SetResult(ApiUtility.MONEY_NOT_ENOUGH);
                                        throw new AggregateException();
                                    }
                                    member = M.MemberInfo.GetByRecharge(DataSource, user);
                                }
                                if (member.Money >= 0)
                                {
                                    if (order.UpdateStateByUser(DataSource, P.OrderState.Payment, provider.Key) != DataStatus.Success)
                                    {
                                        SetResult(ApiUtility.UPDATE_FAIL);
                                        throw new AggregateException();
                                    }
                                }
                                IList<P.ProductOrderMapping> mappings = P.ProductOrderMapping.GetAllByOrder(DataSource, order.Id);
                                foreach (P.ProductOrderMapping mapping in mappings)
                                {
                                    if (P.Product.UpdateInventoryById(DataSource, mapping.ProductId, mapping.Count) != DataStatus.Success)
                                    {
                                        SetResult(ApiUtility.UPDATE_FAIL);
                                        throw new AggregateException();
                                    }
                                }
                                try
                                {
                                    //插入收益和收入记录
                                    if (provider.IsOnlinePay || provider.IsCheckMoney)
                                        Af.ProfitRecord.AddProfitByOrder(DataSource, order.Id);
                                }
                                catch (Exception ex)
                                {

                                }
                                //if (member.ParentId > 0)
                                //{
                                //    try
                                //    {
                                //        M.Member m = M.Member.GetById(DataSource, user);
                                //        //插入加盟商收益记录
                                //        Af.ProfitRecord.AddDistributorProfitByOrder(DataSource, order.Id, m);
                                //    }
                                //    catch (Exception)
                                //    {

                                //    }
                                //}
                            }
                            DataSource.Commit();
                            end = true;
                        }
                        catch (ThreadAbortException)
                        {
                            if (!end)
                                DataSource.Rollback();
                        }
                        catch (AggregateException)
                        {
                            DataSource.Rollback();
                            throw new AggregateException();
                        }
                        catch (Exception ex)
                        {
                            DataSource.Rollback();
                            throw ex;
                        }

                        return true;
                    }
                    ///处理进货订单
                    else if (type == P.ProductOrder.PayWholesaleType)
                    {
                        bool end = false;
                        DataSource.Begin();
                        try
                        {
                            IList<D.DistributorOrder> orders = GetSupplierOrders(targetId, P.OrderState.Payment, user);
                            foreach (D.DistributorOrder order in orders)
                            {
                                if (order == null)
                                {
                                    SetResult(ApiUtility.ORDER_EMPTY);
                                    throw new AggregateException();
                                }
                                if (user != order.UserId)
                                {
                                    SetResult(ApiUtility.MEMBER_NOTFOUND);
                                    throw new AggregateException();
                                }

                                M.MemberInfo member = M.MemberInfo.GetByRecharge(DataSource, user);
                                if (member == null)
                                {
                                    SetResult(ApiUtility.MEMBER_NOTFOUND);
                                    throw new AggregateException();
                                }
                                if (provider.IsOnlinePay)
                                {
                                    if (member.Money < order.TotalMoney)
                                    {
                                        SetResult(ApiUtility.MONEY_NOT_ENOUGH);
                                        throw new AggregateException();
                                    }
                                    if (M.MemberInfo.ModifyMoney(DataSource, user, -order.TotalMoney, order.Title, type, targetId) != DataStatus.Success)
                                    {
                                        SetResult(ApiUtility.MONEY_NOT_ENOUGH);
                                        throw new AggregateException();
                                    }
                                    member = M.MemberInfo.GetByRecharge(DataSource, user);
                                }
                                if (member.Money >= 0)
                                {
                                    if (order.UpdateStateByUser(DataSource, P.OrderState.Payment, provider.Key) != DataStatus.Success)
                                    {
                                        SetResult(ApiUtility.UPDATE_FAIL);
                                        throw new AggregateException();
                                    }
                                }
                                IList<D.DistributorOrderMapping> mappings = D.DistributorOrderMapping.GetAllByOrder(DataSource, order.Id);
                                foreach (D.DistributorOrderMapping mapping in mappings)
                                {
                                    if (D.DistributorProduct.UpdateInventoryById(DataSource, mapping.ProductId, mapping.Count) != DataStatus.Success)
                                    {
                                        SetResult(ApiUtility.UPDATE_FAIL);
                                        throw new AggregateException();
                                    }
                                }
                                try
                                {
                                    if (provider.IsOnlinePay || provider.IsCheckMoney)
                                        Af.ProfitRecord.AddDistributorProfitByOrder(DataSource, order.Id);
                                    //插入进货供应商收益记录
                                    //A.ProfitRecord.AddSupplierProfitByOrder(DataSource, order.Id);
                                }
                                catch (Exception)
                                {

                                }

                            }
                            DataSource.Commit();
                            end = true;
                        }
                        catch (ThreadAbortException)
                        {
                            if (!end)
                                DataSource.Rollback();
                        }
                        catch (AggregateException)
                        {
                            DataSource.Rollback();
                            throw new AggregateException();
                        }
                        catch (Exception ex)
                        {
                            DataSource.Rollback();
                            throw ex;
                        }

                        return true;
                    }
                }
                else
                {
                    if (provider.IsOnlinePay)
                    {
                        if (M.MemberInfo.ModifyMoney(DataSource, user, money, "退款", type, trade) != DataStatus.Success)
                        {
                            SetResult(ApiUtility.UPDATE_FAIL);
                            throw new AggregateException();
                        }
                    }
                }
            }
            catch (AggregateException)
            {
                throw new AggregateException();
            }
            catch (Exception ex) { throw ex; }

            return false;
        }
        /// <summary>
        /// 创建收益快照
        /// </summary>
        /// <param name="ordermapping"></param>
        /// <returns></returns>
        public DataStatus SetOrderSettlement(P.ProductOrderMapping ordermapping)
        {
            DataSource.Begin();
            try
            {
                P.Product product = P.Product.GetById(DataSource, ordermapping.ProductId);
                ///增加供应商结算快照
                P.ProductOrderSettlement settlement = new P.ProductOrderSettlement
                {
                    OrderId = ordermapping.OrderId,
                    ProductId = ordermapping.ProductId,
                    CostPrice = product.CostPrice,
                    Settlement = product.Settlement,
                    RoyaltyRate = product.RoyaltyRate
                };
                if (product.Wholesale && product.WholesalePrice > 0)
                {
                    settlement.ProductType = P.EProductType.Wholesale;
                }
                else if (product.DiscountState == P.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
                {
                    settlement.ProductType = P.EProductType.GroupBuy;
                }
                else
                {
                    settlement.ProductType = P.EProductType.Routine;
                }
                long ShopId = 0;
                //增加收益快照GetRoyaltyByOrderMapping
                P.ProductOrder order = P.ProductOrder.GetById(DataSource, ordermapping.OrderId);

                P.Distributor distributor = P.Distributor.GetById(DataSource, order.UserId);
                if (distributor != null && distributor.UserId != 0)
                {
                    order.ShopId = order.UserId;
                }
                else
                {
                    M.Member member = M.Member.GetById(DataSource, order.UserId);
                    if (member.ParentId != 0)
                    {
                        distributor = P.Distributor.GetById(DataSource, member.ParentId);
                        order.ShopId = distributor.UserId;
                    }
                }
                ///销售人员及加盟商算提成
                if (distributor != null && distributor.UserId != 0)
                {
                    if (distributor.Level == 2 || distributor.Level == 3 || distributor.Level == 4)
                    {
                        settlement.SaleId = order.ShopId;
                        settlement.SaleRoyaltyRate = int.Parse((distributor.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
                        settlement.SaleId = order.ShopId;
                        M.Member SaleMember = M.Member.GetById(DataSource, order.ShopId);
                        ///增加推荐人提成
                        if (distributor.Level == 2)
                        {
                            settlement.ParentId = SaleMember.ParentId;
                            if (settlement.ParentId != 0)
                            {
                                P.Distributor parentD = P.Distributor.GetById(DataSource, settlement.ParentId);
                                if (SaleMember.CreationDate.AddYears(3) >= DateTime.Now)///创建三年内有收益
                                    settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
                            }
                        }
                        else
                        {
                            settlement.ParentId = distributor.ParentId;
                            if (settlement.ParentId != 0)
                            {
                                P.Distributor parentD = P.Distributor.GetById(DataSource, distributor.ParentId);
                                settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
                                if (P.Distributor.GetById(DataSource, parentD.ParentId).Level > 1)
                                    distributor.ParentId = parentD.ParentId;
                            }
                        }
                        ///增加县级提成
                        settlement.CountyUserId = distributor.ParentId;
                        P.Distributor CountyD = P.Distributor.GetById(DataSource, settlement.CountyUserId);
                        settlement.CountyRoyaltyRate = int.Parse((CountyD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
                        ///增加省级提成
                        settlement.ProvinceUserId = CountyD.ParentId;
                        P.Distributor ProvinceD = P.Distributor.GetById(DataSource, settlement.ProvinceUserId);
                        settlement.ProvinceRoyaltyRate = int.Parse((ProvinceD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());

                    }
                    else if (distributor.Level == 1)
                    {
                        settlement.CountyRoyaltyRate = int.Parse((distributor.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
                        settlement.CountyUserId = order.ShopId;
                        ///增加省级提成
                        settlement.ProvinceUserId = distributor.ParentId;
                        P.Distributor ProvinceD = P.Distributor.GetById(DataSource, settlement.ProvinceUserId);
                        settlement.ProvinceRoyaltyRate = int.Parse((ProvinceD.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
                    }
                    else if (distributor.Level == 0)
                    {
                        settlement.ProvinceRoyaltyRate = int.Parse((distributor.GetRoyaltyByOrderMapping(order.ShopId) * 100).ToString());
                        settlement.ProvinceUserId = order.ShopId;
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
                else
                {
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

            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                return DataStatus.Rollback;
            }
        }

    }
}
