using Cnaws;
using Cnaws.Data;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D = XcpNet.Supplier.Modules.Modules;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using A = XcpNet.AfterSales.Modules;
using System.Web;
using Cnaws.Data.Query;
using C = Cnaws.Comment.Modules;
using Cnaws.Json;

namespace XcpNet.Api.Controllers
{
    public class CommDistributor : CommonControllers
    {
        public static string ClassName = "[type]Distributor";
        protected override void OnInitController()
        {
            NotFound();
        }

        public void AddCart()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                P.Distributor distributor;
                if (IsDistributor(out distributor, member.Id))
                {
                    try
                    {
                        D.DistributorCart cart;
                        D.DistributorProduct product;
                        string temp = Request["Id"];
                        string temp2 = Request["Count"];
                        long[] arr = Array.ConvertAll(temp.Split(','), new Converter<string, long>((x) => long.Parse(x)));
                        int[] arr2 = Array.ConvertAll(temp2.Split(','), new Converter<string, int>((x) => int.Parse(x)));
                        DataSource.Begin();
                        try
                        {
                            for (int i = 0; i < Math.Min(arr.Length, arr2.Length); ++i)
                            {
                                product = D.DistributorProduct.GetSaleProduct(DataSource, arr[i]);
                                cart = new D.DistributorCart(DataSource, member.Id, product, arr2[i]);
                                if (product.Inventory >= cart.Count)
                                {
                                    D.DistributorCart productcart = D.DistributorCart.GetProductByUser(DataSource, member.Id, long.Parse(Request["Id"]));
                                    if (productcart == null || productcart.Id <= 0)
                                    {
                                        if (cart.Insert(DataSource) != DataStatus.Success)
                                        {
                                            SetResult(ApiUtility.INSERT_FAIL);
                                            throw new AggregateException();
                                        }
                                    }
                                    else
                                    {
                                        productcart.Count += arr2[i];
                                        if (productcart.Update(DataSource) != DataStatus.Success)
                                        {
                                            SetResult(ApiUtility.UPDATE_FAIL);
                                            throw new AggregateException();
                                        }
                                    }
                                }
                                else
                                {
                                    SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH);
                                    throw new AggregateException();
                                }

                            }
                            DataSource.Commit();
                            SetResult(true);
                        }
                        catch (AggregateException)
                        {
                            DataSource.Rollback();
                            return;
                        }
                        catch (Exception)
                        {
                            DataSource.Rollback();
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        SetResult(false, new { Message = ex.ToString() });
                    }
                }
            }
        }
#if (DEBUG)
        public static void AddCartHelper()
        {
            CheckMemberHelper(ClassName, "AddCart", "添加到加盟商购物车")
                .AddArgument("Id", typeof(long), "产品Id,多个用,隔开")
                .AddArgument("Count", typeof(int), "对应产品Id的数量,多个用,隔开")
                .AddResult(ApiUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "找不到对应的供应商")
                .AddResult(true, typeof(int), "成功返回数据");
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
                        D.DistributorCart cart = D.DistributorCart.GetById(DataSource, long.Parse(ids[i]));
                        if (cart == null)
                        {
                            SetResult(ApiUtility.CART_EMPTY, ids[i]);
                            throw new AggregateException();
                        }
                        D.DistributorProduct product = D.DistributorProduct.GetSaleProduct(DataSource, cart.ProductId);
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
        public void DelCart()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                P.Distributor distributor;
                if (IsDistributor(out distributor, member.Id))
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
                                if ((new D.DistributorCart() { Id = cartid, UserId = member.Id }).Remove(DataSource) != Cnaws.Web.DataStatus.Success)
                                {
                                    throw new Exception();
                                }
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
                else
                {
                    SetResult(ApiUtility.DISTRIBUTOR_NOTIS);
                }
            }
        }
#if (DEBUG)
        public static void DelCartHelper()
        {
            CheckMemberHelper(ClassName, "DelCart", "删除加盟商购物车产品")
                .AddArgument("Id", typeof(long), "产品Id,多个用,隔开")
                .AddArgument("Count", typeof(int), "对应产品Id的数量,多个用,隔开")
                 .AddResult(ApiUtility.DEL_FAIL, "删除数据失败")
                .AddResult(ApiUtility.PRODUCT_EMPTY, "产品为空")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "找不到对应的供应商")
                .AddResult(true, typeof(int), "成功返回数据");
        }
#endif

        public void GetCartList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                IList<DataJoin<D.DistributorCart, D.DistributorProduct>> list = D.DistributorCart.GetPageByUser(DataSource, member.Id);
                SetResult(list);
            }
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
                    D.DistributorProduct p;
                    D.DistributorOrderMapping pom;
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

                    List<D.DistributorOrderMapping> ps;
                    KeyValuePair<string, List<D.DistributorOrderMapping>> pair;
                    Dictionary<long, Money> money = new Dictionary<long, Money>();
                    Dictionary<long, KeyValuePair<string, List<D.DistributorOrderMapping>>> OrderForSupplier = new Dictionary<long, KeyValuePair<string, List<D.DistributorOrderMapping>>>();
                    for (int i = 0; i < ids.Length; ++i)
                    {
                        count = int.Parse(counts[i]);
                        if (count <= 0)
                        {
                            SetResult(ApiUtility.PRODUCT_SUM_ERROR);
                            throw new AggregateException();
                        }
                        p = D.DistributorProduct.GetSaleProduct(DataSource, long.Parse(ids[i]));
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
                            pom = new D.DistributorOrderMapping(DataSource, pair.Key, p, count);
                            money[p.SupplierId] = money[p.SupplierId] + pom.TotalMoney;
                            pair.Value.Add(pom);
                        }
                        else
                        {
                            pom = new D.DistributorOrderMapping(DataSource, P.ProductOrder.NewId(now, member.Id, i + 1), p, count);
                            money[p.SupplierId] = pom.TotalMoney;
                            ps = new List<D.DistributorOrderMapping>();
                            ps.Add(pom);
                            OrderForSupplier.Add(p.SupplierId, new KeyValuePair<string, List<D.DistributorOrderMapping>>(pom.OrderId, ps));
                        }
                    }

                    string orderId = (OrderForSupplier.Count > 1) ? string.Concat('G', D.DistributorOrder.NewId(now, member.Id)) : null;

                    long shopId = 0;
                    P.Distributor distributor = Ad.Modules.MachineCode.GetDistributorByCode(DataSource, member.Mark);
                    if (distributor == null)
                    {
                        SetResult(ApiUtility.DISTRIBUTOR_EMPTY);
                        throw new AggregateException();
                    }
                    shopId = distributor.UserId;

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
                                CreationDate = now
                            };

                            if (order.Insert(DataSource) != DataStatus.Success)
                            {
                                SetResult(ApiUtility.ORDER_ADDERROT);
                                throw new AggregateException();
                            }

                            foreach (D.DistributorOrderMapping pm in item.Value.Value)
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
        public static void SetOrderHelper()
        {
            CheckMemberHelper(ClassName, "SetOrder", "提交加盟商订单获取订单号")
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
        /// 设置订单收货地址根据收货地址编号
        /// </summary>
        [HttpPost]
        public void SetPerfect()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                bool end = false;
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
                        IList<D.DistributorOrder> orders = GetOrders(orderId, P.OrderState.Perfect, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {

                            orders = GetOrdersNoState(orderId, member.Id);
                            if (orders == null || orders.Count <= 0 || orders[0] == null)
                            {
                                SetResult(ApiUtility.ORDER_EMPTY, "订单不存在");
                                throw new AggregateException();
                            }
                        }
                        foreach (D.DistributorOrder order in orders)
                        {
                            if (order.State != P.OrderState.Payment)
                            {
                                order.State = P.OrderState.Payment;
                                order.FreightMoney = order.GetFreight(DataSource,address.Province, address.City);
                                order.TotalMoney = order.TotalMoney + order.FreightMoney;
                                order.Address = address.BuildInfo();
                                order.Message = Request.Form["Message"];
                                if (order.UpdatePerfectByUser(DataSource) != DataStatus.Success)
                                {
                                    SetResult(ApiUtility.ORDER_UPDATE_ERROR, "更新订单失败");
                                    throw new AggregateException();
                                }
                            }
                            IList<D.DistributorOrderMapping> ProductOrderMappings = D.DistributorOrderMapping.GetAllByOrder(DataSource, order.Id);
                            if (ProductOrderMappings.Count > 0 && ProductOrderMappings[0].ProductId > 0)
                            {
                                foreach (D.DistributorOrderMapping pm in ProductOrderMappings)
                                {
                                    try { D.DistributorCart.Remove(DataSource, pm.ProductId, member.Id); }
                                    catch (Exception) { }
                                }
                            }
                        }
                    }
                    end = true;
                    DataSource.Commit();
                    SetResult(true);
                }
                catch (ThreadAbortException) {
                    if (!end)
                        DataSource.Rollback();
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
                SetResult(D.DistributorOrder.GetAjaxPageByUserAndStateApi(DataSource, member.Id, state, page, size, 11));
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
        /// 根据状态获取订单信息列表
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

                        IList<D.DistributorOrder> orders;

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
                        Supplier.Modules.DistributorProductCacheInfo pci;
                        IList<D.DistributorOrderMapping> maps;
                        maps = orders[0].GetMapping(DataSource);
                        temp = new List<dynamic>(maps.Count);
                        foreach (D.DistributorOrderMapping it in maps)
                        {
                            pci = JsonValue.Deserialize<Supplier.Modules.DistributorProductCacheInfo>(it.ProductInfo);
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
            }
        }
#if (DEBUG)
        public static void GetOrdersHelper()
        {
            CheckMemberHelper(ClassName, "GetOrders", "根据用户获取所有用户订单")
                 .AddArgument("state", typeof(P.OrderState), "状态,默认为'_'查询所有,对应交易关闭\t等待完善\t等待付款\t等待发货\t等待收货\t等待评价\t交易完成\t申请退款\t等待退货发货\t退货已发货\t退款成功")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddResult(true, typeof(SplitPageData<D.DistributorOrder>), "返回订单结果");
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
                    string orderid = Request["Id"];
                    IList<D.DistributorOrder> orders = GetOrders(orderid, P.OrderState.Perfect, member.Id);
                    if (orders == null || orders.Count <= 0 || orders[0] == null)
                    {
                        orders = GetOrdersNoState(orderid, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(ApiUtility.ORDER_EMPTY, "订单不存在");
                            throw new AggregateException();
                        }
                    }
                    M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Request["Address"]), member.Id);
                    Money FreightMoney = 0;
                    foreach (D.DistributorOrder item in orders)
                        FreightMoney += item.GetFreight(DataSource,  address.Province, address.City);
                    SetResult(true, FreightMoney);
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
            CheckMemberHelper(ClassName, "GetFreight", "根据加盟商订单号获取运费")
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
                try
                {
                    string id;
                    if (!string.IsNullOrEmpty(Request["Id"]))
                    {
                        id = Request["Id"];
                        IList<D.DistributorOrder> orders = GetOrdersNoState(id, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(ApiUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }
                        List<D.DistributorOrderMapping> ordermapping = new List<D.DistributorOrderMapping>();
                        foreach (D.DistributorOrder order in orders)
                        {
                            if (order != null)
                            {
                                IList<D.DistributorOrderMapping> plist = D.DistributorOrderMapping.GetAllByOrder(DataSource, order.Id);
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
            CheckMemberHelper(ClassName, "GetOrderMapping", "根据订单号获取加盟商订单产品信息列表")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(List<D.DistributorOrderMapping>), "产品列表");
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
            CheckMemberHelper(ClassName, "GetAllShippingAddress", "获取所有收货地址")
                .AddResult(true, typeof(IList<M.ShippingAddress>), "产品列表");
        }
#endif
        private IList<D.DistributorOrder> GetOrdersNoState(string id, long userId)
        {
            if (id[0] == 'G')
                return D.DistributorOrder.GetListByParentid(DataSource, id, userId);
            return new D.DistributorOrder[] { D.DistributorOrder.GetById(DataSource, id) };
        }

        private IList<D.DistributorOrder> GetOrders(string id, P.OrderState state, long userId)
        {
            if (id[0] == 'G')
                return D.DistributorOrder.GetListByState(DataSource, id, state, userId);
            return new D.DistributorOrder[] { D.DistributorOrder.GetByState(DataSource, id, state, userId) };
        }

        public void GetUntreatedSum()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    long paymentsum = D.DistributorOrder.GetMyCountByState(DataSource, P.OrderState.Payment, member.Id);
                    long deliverysum = D.DistributorOrder.GetMyCountByState(DataSource, P.OrderState.Delivery, member.Id);
                    long receiptsum = D.DistributorOrder.GetMyCountByState(DataSource, P.OrderState.Receipt, member.Id);
                    long evaluationsum = Db<C.Comment>.Query(DataSource)
                                 .Select(new DbSelect<C.Comment>("*"), new DbSelect<D.DistributorOrderMapping>("*"), new DbSelect<D.DistributorOrder>("ReceiptDate"))
                                 .RightJoin(new DbColumn<C.Comment>("TargetId"), new DbColumn<D.DistributorOrderMapping>("ProductId"))
                                 .InnerJoin(new DbColumn<D.DistributorOrderMapping>("OrderId"), new DbColumn<D.DistributorOrder>("Id"))
                                 .Where(new DbWhere<D.DistributorOrder>("UserId", member.Id) & new DbWhere<D.DistributorOrderMapping>("Evaluation", false) & new DbWhere<D.DistributorOrder>("State", P.OrderState.Finished))
                                 .Count();//D.DistributorOrder.GetMyCountByState(DataSource, P.OrderState.Evaluation, member.Id);
                    long cartsum = D.DistributorCart.GetCountByUser(DataSource, member.Id);

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
            CheckMemberHelper(ClassName, "GetUntreatedSum", "获取加盟商未处理的数量")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "处理结果");
        }
#endif


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
                    D.DistributorOrder order = D.DistributorOrder.GetByState(DataSource, id, P.OrderState.Payment, member.Id);
                    if (order == null)
                        order = D.DistributorOrder.GetByState(DataSource, id, P.OrderState.Perfect, member.Id);
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
            CheckMemberHelper(ClassName, "CancelOrder", "取消加盟商订单")
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
                    D.DistributorOrder order = D.DistributorOrder.GetByState(DataSource, id, P.OrderState.Invalid, member.Id);
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
            CheckMemberHelper(ClassName, "DelOrder", "删除加盟商订单")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif



        public void GetMoney()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    long parentid = 0;
                    if (!long.TryParse(Request["parentid"], out parentid) || parentid <= 0)
                        parentid = member.Id;
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    //SetResult(P.Distributor.GetMoneyByParent(DataSource, parentid, page, size));
                    var MyMoney = new
                    {
                        Total = A.ProfitRecord.GetAllHoustonMoney(DataSource, parentid),
                        Money = A.ProfitRecord.GetArrivalMoney(DataSource, parentid),
                        FreezeMoney = A.ProfitRecord.GetHoustonFreezeMoney(DataSource, parentid),
                        Data = A.ProfitRecord.GetListByUser(DataSource, parentid, Math.Max(1, page), size, 8)
                    };
                    SetResult(MyMoney);

                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetMoneyHelper()
        {
            CheckMemberHelper(ClassName, "GetMoney", "获取加盟商的收益表")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddArgument("parentid", typeof(string), "加盟商编号,传0为获取当前用户")
                .AddResult(true, typeof(SplitPageData<AfterSales.Modules.ProfitRecord>), "返回结果");
        }
#endif

        public void GetDistributorByParentId()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    long parentid = 0;
                    if (!long.TryParse(Request["parentid"], out parentid) || parentid <= 0)
                        parentid = member.Id;
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    SetResult(P.Distributor.GetPageByParent(DataSource, parentid, page, size));
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetDistributorByParentIdHelper()
        {
            CheckMemberHelper(ClassName, "GetDistributorByParentId", "获取我的加盟店")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddArgument("parentid", typeof(string), "加盟商编号,传0为获取当前用户")
                .AddResult(true, typeof(string), "返回结果");
        }
#endif
        public void GetOrdersByChildDistributor()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    long parentid = 0;
                    if (!long.TryParse(Request["parentid"], out parentid) || parentid <= 0)
                        parentid = member.Id;
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    SetResult(A.ProfitRecord.GetOrderByRecord(DataSource, parentid, page, size));
                }
                catch (Exception ex)
                {
                    SetResult(ApiUtility.PROGRAM_ERROR, new { ErrorMessage = ex.ToString() });
                }
            }
        }
#if (DEBUG)
        public static void GetOrdersByChildDistributorHelper()
        {
            CheckMemberHelper(ClassName, "GetOrdersByChildDistributor", "获取我的加盟商的订单")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddArgument("parentid", typeof(string), "加盟商编号,传0为获取当前用户")
                .AddResult(true, typeof(string), "返回结果，跟获取会员全部订单数据一致");
        }
#endif

        [HttpPost]
        public void CreateAccount()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                DataSource.Begin();
                try
                {
                    M.Member newmember = DbTable.Load<M.Member>(Request.Form);
                    newmember.ParentId = 0;
                    newmember.Approved = true;
                    newmember.CreationDate = DateTime.Now;
                    if (newmember.Insert(DataSource) != DataStatus.Success)
                    {
                        SetResult((int)member.Insert(DataSource));
                        throw new AggregateException();
                    }

                    P.Distributor value = DbTable.Load<P.Distributor>(Request.Form);
                    value.UserId = newmember.Id;
                    value.ParentId = member.Id;
                    value.State = P.DistributorState.NotApproved;
                    value.Images = HttpUtility.UrlDecode(value.Images);
                    value.Province = int.Parse(Request.Form["Provinces"]);
                    value.City = int.Parse(Request.Form["Cities"]);
                    value.County = int.Parse(Request.Form["Counties"]);
                    value.CreationDate = newmember.CreationDate;
                    if (value.Insert(DataSource) != DataStatus.Success)
                    {
                        SetResult(-501);
                        throw new AggregateException();
                    }

                    DataSource.Commit();

                    SetResult(true);
                }
                catch (AggregateException)
                {
                    DataSource.Rollback();
                    return;
                }
                catch (Exception ex)
                {
                    SetResult(false, new { Message = ex.ToString() });
                }
            }
        }
#if (DEBUG)
        public static void CreateAccountHelper()
        {
            CheckMemberHelper(ClassName, "CreateAccount", "开通加盟商账号")
                .AddArgument("Name", typeof(string), "加盟商账号")
                .AddArgument("Password", typeof(string), "加盟商密码")
                .AddArgument("Company", typeof(string), "加盟商公司名称")
                .AddArgument("Images", typeof(string), "加盟商证件号码")
                .AddArgument("Signatories", typeof(string), "签约人")
                .AddArgument("SignatoriesPhone", typeof(long), "签约人联系电话")
                .AddArgument("Contact", typeof(string), "负责人")
                .AddArgument("ContactPhone", typeof(string), "负责人联系电话")
                .AddArgument("Provinces", typeof(long), "省Id")
                .AddArgument("Cities", typeof(string), "市Id")
                .AddArgument("Counties", typeof(string), "区Id")
                .AddArgument("Address", typeof(string), "加盟商地址")
                .AddArgument("PostId", typeof(string), "邮政编码")
                .AddResult(true, typeof(string), "返回结果");
        }
#endif

        public void GetUnComment()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    long count;
                    IList<D.DistributorOrderMapping> list = Db<C.Comment>.Query(DataSource)
                             .Select(new DbSelect<C.Comment>("*"), new DbSelect<D.DistributorOrderMapping>("*"), new DbSelect<D.DistributorOrder>("ReceiptDate"))
                             .RightJoin(new DbColumn<C.Comment>("TargetId"), new DbColumn<D.DistributorOrderMapping>("ProductId"))
                             .InnerJoin(new DbColumn<D.DistributorOrderMapping>("OrderId"), new DbColumn<D.DistributorOrder>("Id"))
                             .Where(new DbWhere<D.DistributorOrder>("UserId", member.Id) & new DbWhere<D.DistributorOrderMapping>("Evaluation", false) & new DbWhere<D.DistributorOrder>("State", P.OrderState.Finished))
                             .OrderBy(new DbOrderBy<D.DistributorOrder>("ReceiptDate", DbOrderByType.Desc))
                             .ToList<D.DistributorOrderMapping>(size, page, out count);
                    SetResult(new SplitPageData<D.DistributorOrderMapping>(page, size, list, count));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }

        }
#if (DEBUG)
        public static void GetUnCommentHelper()
        {
            CheckMemberHelper(ClassName, "GetUnComment", "获取未评论列表")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(SplitPageData<D.DistributorOrderMapping>), "返回结果");
        }
#endif
        [HttpPost]
        public void SetDistributor()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    P.Distributor value = P.Distributor.GetById(DataSource, member.Id);
                    if (value != null)
                    {
                        value = DbTable.Load<P.Distributor>(Request.Form);
                        value.UserId = member.Id;
                        value.Images = HttpUtility.UrlDecode(value.Images);
                        SetResult(value.UpdateWithState(DataSource));
                    }
                    else
                    {
                        SetResult(ApiUtility.DISTRIBUTOR_NOTIS);
                    }
                }
            }
            catch (Exception ex)
            {
                SetResult(ApiUtility.PROGRAM_ERROR, new { Message = ex.ToString() });
            }
        }
#if (DEBUG)
        public static void SetDistributorHelper()
        {
            CheckMemberHelper(ClassName, "SetDistributor", "设置供应商信息")
                .AddArgument("Company", typeof(string), "公司名称")
                .AddArgument("Signatories", typeof(int), "签约人")
                .AddArgument("SignatoriesPhone", typeof(int), "签约人手机")
                .AddArgument("Contact", typeof(int), "负责人")
                .AddArgument("ContactPhone", typeof(int), "负责人联系电话")
                .AddArgument("Province", typeof(int), "省Id")
                .AddArgument("City", typeof(int), "市Id")
                .AddArgument("County", typeof(int), "区Id")
                .AddArgument("Address", typeof(int), "详细地址")
                .AddArgument("PostId", typeof(int), "邮政编码")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "不是供应商")
                .AddResult(true, typeof(DataStatus), "返回结果");
        }
#endif

        public void PayOrder()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                DataSource.Begin();
                try
                {
                    string orderid = Request["OrderId"];
                    D.DistributorOrder order = D.DistributorOrder.GetById(DataSource, orderid);
                    if (order != null && order.State == P.OrderState.Payment)
                    {
                        M.MemberInfo memberinfo = M.MemberInfo.GetById(DataSource, member.Id);
                        if (memberinfo.Money > order.TotalMoney)
                        {

                            IList<D.DistributorOrder> orders = GetOrders(orderid, P.OrderState.Payment, member.Id);
                            if (orders == null || orders.Count <= 0 || orders[0] == null)
                            {
                                orders = GetOrdersNoState(orderid, member.Id);
                                if (orders == null || orders.Count <= 0 || orders[0] == null)
                                {
                                    SetResult(ApiUtility.ORDER_EMPTY, "订单不存在");
                                    throw new AggregateException();
                                }
                            }

                            foreach (D.DistributorOrder neworder in orders)
                            {

                                if ((new D.DistributorMoneyRecord()
                                {
                                    MemberId = User.Identity.Id,
                                    Title = "进货",
                                    Type = D.DistributorOrder.PayType,
                                    TargetId = neworder.Id,
                                    Value = -neworder.TotalMoney,
                                    CreationDate = DateTime.Now
                                }).Insert(DataSource) != DataStatus.Success)
                                {
                                    SetResult(ApiUtility.INSERT_FAIL);
                                    throw new AggregateException("创建账单记录失败");
                                }
                                if (M.MemberInfo.ModifyMoney(DataSource, User.Identity.Id, -neworder.TotalMoney, "进货") != DataStatus.Success)
                                {
                                    SetResult(ApiUtility.MONEY_NOT_ENOUGH);
                                    throw new AggregateException("余额不足");
                                }

                            }
                        }
                    }
                    else
                    {
                        SetResult(ApiUtility.MONEY_NOT_ENOUGH);
                        throw new AggregateException();
                    }
                    DataSource.Commit();
                    SetResult(true);
                }
                catch (AggregateException)
                {
                    DataSource.Rollback();
                    return;
                }
                catch (Exception)
                {
                    DataSource.Rollback();
                    SetResult(ApiUtility.PROGRAM_ERROR);
                    return;
                }
            }
        }
#if (DEBUG)
        //public static void PayOrderHelper()
        //{
        //    CheckMemberHelper(ClassName, "PayOrder", "余额支付进货宝订单")
        //        .AddArgument("OrderId", typeof(string), "订单号")
        //        .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
        //        .AddResult(ApiUtility.ORDER_EMPTY, "订单不存在")
        //        .AddResult(ApiUtility.INSERT_FAIL, "创建账单记录失败")
        //        .AddResult(ApiUtility.MONEY_NOT_ENOUGH, "余额不足")
        //        .AddResult(true, typeof(DataStatus), "返回结果");
        //}
#endif
        public DataStatus SetOrderSettlement(D.DistributorOrderMapping ordermapping)
        {
            DataSource.Begin();
            try
            {
                D.DistributorProduct product = D.DistributorProduct.GetById(DataSource, ordermapping.ProductId);
                D.DistributorOrderSettlement settlement = new D.DistributorOrderSettlement
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
                //增加收益快照GetRoyaltyByOrderMapping
                D.DistributorOrder order = D.DistributorOrder.GetById(DataSource, ordermapping.OrderId);

                long ShopId = 0;
                //增加收益快照GetRoyaltyByOrderMapping

                P.Distributor distributor = P.Distributor.GetById(DataSource, order.UserId);
                if (distributor != null && distributor.UserId != 0)
                {
                    ShopId = order.UserId;
                }
                else
                {
                    M.Member member = M.Member.GetById(DataSource, order.UserId);
                    if (member.ParentId != 0)
                    {
                        distributor = P.Distributor.GetById(DataSource, member.ParentId);
                        ShopId = distributor.UserId;
                    }
                }
                ///销售人员及加盟商算提成
                if (distributor != null && distributor.UserId != 0)
                {
                    if (distributor.Level == 2 || distributor.Level == 3 || distributor.Level == 4)
                    {
                        //settlement.SaleId = order.UserId;
                        /////当前加盟商不是广东省加盟商
                        //if (distributor.Province != 440000 && DateTime.Now < DateTime.Parse("2016-12-31 23:59:59"))
                        //{
                        //    ///总收入5000封顶
                        //    if (A.ProfitRecord.GetTimeHoustonMoneyByChannel(DataSource, order.UserId, A.ProfitRecord.EChannel.WholesaleOrder, DateTime.Parse("2016-1-1 00:00:00"), DateTime.Parse("2016-12-31 23:59:59")) <= 5000)
                        //    {
                        //        ///当月收入1000封顶
                        //        if (A.ProfitRecord.GetTimeHoustonMoneyByChannel(DataSource, order.UserId, A.ProfitRecord.EChannel.WholesaleOrder, DateTime.Parse("2016-1-1 00:00:00"), DateTime.Parse("2016-12-31 23:59:59")) <= 5000)
                        //        {
                        //            DateTime now = DateTime.Now;
                        //            DateTime begin = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                        //            DateTime end = begin.AddMonths(1);
                        //            if (A.ProfitRecord.GetTimeHoustonMoneyByChannel(DataSource, order.UserId, A.ProfitRecord.EChannel.WholesaleOrder, begin, end) <= 1000)
                        //            {
                        //                settlement.SaleRoyaltyRate = int.Parse((distributor.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
                        //            }
                        //        }
                        //    }
                        //}
                        M.Member SaleMember = M.Member.GetById(DataSource, order.UserId);
                        ///增加推荐人提成
                        if (distributor.Level == 2)
                        {
                            settlement.ParentId = SaleMember.ParentId;
                            if (settlement.ParentId != 0)
                            {
                                P.Distributor parentD = P.Distributor.GetById(DataSource, settlement.ParentId);
                                if (SaleMember.CreationDate.AddYears(3) >= DateTime.Now)///创建三年内有收益
                                    settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
                            }
                        }
                        else
                        {
                            settlement.ParentId = distributor.ParentId;
                            if (settlement.ParentId != 0)
                            {
                                P.Distributor parentD = P.Distributor.GetById(DataSource, distributor.ParentId);
                                settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
                                if (parentD.Level > 1)
                                    distributor.ParentId = parentD.ParentId;
                            }
                        }

                        ///增加县级提成
                        settlement.CountyUserId = distributor.ParentId;
                        P.Distributor CountyD = P.Distributor.GetById(DataSource, settlement.CountyUserId);
                        settlement.CountyRoyaltyRate = int.Parse((CountyD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
                        ///增加省级提成
                        settlement.ProvinceUserId = CountyD.ParentId;
                        P.Distributor ProvinceD = P.Distributor.GetById(DataSource, settlement.ProvinceUserId);
                        settlement.ProvinceRoyaltyRate = int.Parse((ProvinceD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());

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
            catch (Exception)
            {
                DataSource.Rollback();
                return DataStatus.Rollback;
            }
        }


        public void GetMyChildMember()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                int size, page;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                if (!int.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                SetResult(M.MemberInfo.GetByParentId(DataSource, member.Id, page, size));
            }
        }
#if (DEBUG)
        public static void GetMyChildMemberHelper()
        {
            CheckMemberHelper(ClassName, "GetMyChildMember", "获取我的下级用户")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(true, typeof(SplitPageData<M.MemberInfo>), "返回结果");
        }
#endif

        public void CheckPassword()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                string password = Request["PassWord"];
                if (!string.IsNullOrEmpty(password))
                {
                    if (password.Equals(member.Password, StringComparison.OrdinalIgnoreCase))
                    {
                        SetResult(true);
                    }
                    else
                    {
                        SetResult(ApiUtility.PASSWORD_EQUALS);
                    }
                }
                else
                {
                    SetResult(ApiUtility.PASSWORD_EQUALS);
                }
            }
        }
#if (DEBUG)
        public static void CheckPasswordHelper()
        {
            CheckMemberHelper(ClassName, "CheckPassword", "供应商再次验证密码")
                .AddArgument("PassWord", typeof(int), "md5(密码)")
                .AddResult(true, typeof(bool), "成功");
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
                    D.DistributorOrder productorder = D.DistributorOrder.GetById(DataSource, Id);
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
                    SetResult((new D.DistributorOrder() { Id = Request.Form["Id"], UserId = member.Id }).UpdateStateByUser(DataSource, P.OrderState.Receipt));
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
    }
}
