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
using A = XcpNet.Ad.Modules;
using System.Web;
using Cnaws.Data.Query;
using C = Cnaws.Comment.Modules;

namespace XcpNet.Api.Controllers
{
    public class ApiDistributor : CommonControllers
    {
        public void GetProductList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                P.Distributor distributor;
                if (IsDistributor(out distributor, member.Id))
                {
                    Dictionary<string, dynamic> productList = new Dictionary<string, dynamic>();
                    string keyword = Request["KeyWord"];
                    int categoryid = int.Parse(Request["CategoryId"]);
                    int page = int.Parse(Request["Page"]);
                    string price = Request["Price"];
                    D.DistributorCategory cate = new Supplier.Modules.Modules.DistributorCategory();
                    if (categoryid > 0)
                        cate = D.DistributorCategory.GetById(DataSource, categoryid);
                    else
                        cate.Id = 0;
                    SplitPageData<DataJoin<D.DistributorProduct, D.DistributorCategory>> Splitdata = D.DistributorProduct.ApiGetPageByWholesale(DataSource, "_".Equals(keyword) ? null : keyword, cate.Id, "_".Equals(price) ? null : price, Math.Max(1, page), 20, 11);
                    List<object> list = new List<object>();
                    foreach (DataJoin<D.DistributorProduct, D.DistributorCategory> product in Splitdata.Data)
                    {
                        dynamic value = new { Product = product, Mapping = product.A.GetMappingByProduct(DataSource) };
                        //productList.Add(product.A.Id.ToString(), value);
                        list.Add(value);
                    }
                    SetResult(list);
                }
                else
                {
                    SetResult(ApiUtility.DISTRIBUTOR_NOTIS);
                }
            }
        }
#if (DEBUG)
        public static void GetProductListHelper()
        {
            CheckMemberHelper("ApiDistributor", "GetProductList", "获取产品列表")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddArgument("CategoryId", typeof(int), "分类Id")
                 .AddArgument("Price", typeof(string), "价格区间")
                .AddArgument("KeyWord", typeof(string), "关键词")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "找不到对应的供应商")
                .AddResult(true, typeof(string), "成功返回数据,动态对象");
        }
#endif
        public void GetCartList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                P.Distributor distributor;
                if (IsDistributor(out distributor, member.Id))
                {
                    SetResult(D.DistributorCart.GetPageByUser(DataSource, member.Id));
                }
                else
                {
                    SetResult(ApiUtility.DISTRIBUTOR_NOTIS);
                }
            }

        }
#if (DEBUG)
        public static void GetCartListHelper()
        {
            CheckMemberHelper("ApiDistributor", "GetCartList", "获取加盟商购物车")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "找不到对应的供应商")
                .AddResult(true, typeof(IList<DataJoin<D.DistributorCart, D.DistributorProduct>>), "成功返回数据");
        }
#endif

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
                        string temp = Request.Form["Id"];
                        string temp2 = Request.Form["Count"];
                        long[] arr = Array.ConvertAll(temp.Split(','), new Converter<string, long>((x) => long.Parse(x)));
                        int[] arr2 = Array.ConvertAll(temp2.Split(','), new Converter<string, int>((x) => int.Parse(x)));
                        DataSource.Begin();
                        try
                        {
                            for (int i = 0; i < Math.Min(arr.Length, arr2.Length); ++i)
                            {
                                product = D.DistributorProduct.GetSaleProduct(DataSource, arr[i]);
                                cart = new D.DistributorCart(DataSource, member.Id, product, arr2[i]);
                                if (product.Inventory > cart.Count)
                                {
                                    D.DistributorCart productcart = D.DistributorCart.GetProductByUser(DataSource, member.Id, long.Parse(Request.Form["Id"]));
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
                                        productcart.Count += int.Parse(Request.Form["Count"]);
                                        SetResult(productcart.Update(DataSource));
                                    }
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
                    catch (Exception)
                    {
                        SetResult(false);
                    }
                }
            }
        }
#if (DEBUG)
        public static void AddCartHelper()
        {
            CheckMemberHelper("ApiDistributor", "AddCart", "添加到加盟商购物车")
                .AddArgument("Id", typeof(long), "产品Id,多个用,隔开")
                .AddArgument("Count", typeof(int), "对应产品Id的数量,多个用,隔开")
                .AddResult(ApiUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "找不到对应的供应商")
                .AddResult(true, typeof(int), "成功返回数据");
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
            CheckMemberHelper("ApiDistributor", "DelCart", "删除加盟商购物车产品")
                .AddArgument("Id", typeof(long), "产品Id,多个用,隔开")
                .AddArgument("Count", typeof(int), "对应产品Id的数量,多个用,隔开")
                 .AddResult(ApiUtility.DEL_FAIL, "删除数据失败")
                .AddResult(ApiUtility.PRODUCT_EMPTY, "产品为空")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "找不到对应的供应商")
                .AddResult(true, typeof(int), "成功返回数据");
        }
#endif

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
                        p = D.DistributorProduct.GetSaleProduct(DataSource, long.Parse(ids[i]));
                        if (p == null)
                        {
                            string a = null;
                            SetResult(ApiUtility.PRODUCT_ERROR, new { OrderId = a, OrderMoney = a, FreightMoney = a, ProductCart = D.DistributorCart.GetProductByUser(DataSource, member.Id, long.Parse(ids[i])) });
                            throw new AggregateException();
                        }

                        if (p.Inventory < count)
                        {
                            string a = null;
                            SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH, new { OrderId = a, OrderMoney = a, FreightMoney = a, ProductCart = D.DistributorCart.GetProductByUser(DataSource, member.Id, long.Parse(ids[i])) });
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
                            pom = new D.DistributorOrderMapping(DataSource, D.DistributorOrder.NewId(now, member.Id, i + 1), p, count);
                            money[p.SupplierId] = pom.TotalMoney;
                            ps = new List<D.DistributorOrderMapping>();
                            ps.Add(pom);
                            OrderForSupplier.Add(p.SupplierId, new KeyValuePair<string, List<D.DistributorOrderMapping>>(pom.OrderId, ps));
                        }
                    }

                    string orderId = (OrderForSupplier.Count > 1) ? string.Concat('G', D.DistributorOrder.NewId(now, member.Id)) : null;

                    ///设置收货地址

                    P.Distributor distributor = A.MachineCode.GetDistributorByCode(DataSource, member.Mark);
                    if (distributor == null)
                    {
                        SetResult(ApiUtility.DISTRIBUTOR_EMPTY);
                        throw new AggregateException();
                    }
                    long shopId = 0;

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

                            foreach (D.DistributorOrderMapping pm in item.Value.Value)
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
                    catch (Exception ex)
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

                        IList<D.DistributorOrder> orders = GetOrders(NewOrder, P.OrderState.Perfect, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(ApiUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }

                        foreach (D.DistributorOrder order in orders)
                        {
                            order.State = P.OrderState.Payment;
                            order.FreightMoney = D.DistributorOrder.GetFreight(DataSource, order.Id, address.Province, address.City, order.TotalMoney);
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
                    catch (Exception ex)
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
            CheckMemberHelper("ApiDistributor", "SetOrder", "提交加盟商订单获取订单号")
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
            if (CheckSign(Request.QueryString))
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
                    SetResult(D.DistributorOrder.GetAjaxPageByUserAndStateApi(DataSource, member.Id, state, page, size));
                }
            }
        }
#if (DEBUG)
        public static void GetAllOrdersHelper()
        {
            CheckMemberHelper("ApiDistributor", "GetAllOrders", "根据用户获取所有用户订单")
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
            CheckMemberHelper("ApiDistributor", "GetOrders", "根据加盟商总订单号获取订单信息列表")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(IList<D.DistributorOrder>), "订单号列表");
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
                        IList<D.DistributorOrder> orders = GetOrders(orderId, P.OrderState.Perfect, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(ApiUtility.ORDER_EMPTY, "订单不存在");
                            throw new AggregateException();
                        }
                        foreach (D.DistributorOrder order in orders)
                        {
                            order.State = P.OrderState.Payment;
                            order.FreightMoney = D.DistributorOrder.GetFreight(DataSource, order.Id, address.Province, address.City, order.TotalMoney);
                            order.TotalMoney = order.TotalMoney + order.FreightMoney;
                            order.Address = address.BuildInfo();
                            order.Message = Request.Form["Message"];

                            if (order.UpdatePerfectByUser(DataSource) != DataStatus.Success)
                            {
                                SetResult(ApiUtility.ORDER_UPDATE_ERROR, "更新订单失败");
                                throw new AggregateException();
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
            CheckMemberHelper("ApiDistributor", "SetPerfect", "设置加盟商订单收货地址根据收货地址编号")
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
                    IList<D.DistributorOrder> orders = GetOrders(Request["Id"], P.OrderState.Perfect, member.Id);
                    M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Request["Address"]), member.Id);
                    Money FreightMoney = 0;
                    foreach (D.DistributorOrder item in orders)
                        FreightMoney += D.DistributorOrder.GetFreight(DataSource, item.Id, address.Province, address.City, item.TotalMoney);
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
            CheckMemberHelper("ApiDistributor", "GetFreight", "根据加盟商订单号获取运费")
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
                    IList<D.DistributorOrder> orders = GetOrdersNoState(id, member.Id);
                    List<D.DistributorOrderMapping> mappings = new List<D.DistributorOrderMapping>();
                    foreach (D.DistributorOrder order in orders)
                    {
                        IList<D.DistributorOrderMapping> mapping = order.GetMapping(DataSource);
                        mappings.AddRange(mapping);
                    }
                    SetResult(mappings);
                }
            }
        }
#if (DEBUG)
        public static void GetOrderMappingHelper()
        {
            CheckMemberHelper("ApiDistributor", "GetOrderMapping", "根据订单号获取加盟商订单产品信息列表")
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
            CheckMemberHelper("ApiDistributor", "GetAllShippingAddress", "获取所有收货地址")
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
            CheckMemberHelper("ApiDistributor", "GetUntreatedSum", "获取加盟商未处理的数量")
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
            CheckMemberHelper("ApiDistributor", "CancelOrder", "取消加盟商订单")
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
            CheckMemberHelper("ApiDistributor", "DelOrder", "删除加盟商订单")
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
                    SetResult(P.Distributor.GetMoneyByParent(DataSource, parentid, page, size));
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
            CheckMemberHelper("ApiDistributor", "GetMoney", "获取加盟商的收益表")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddArgument("parentid", typeof(string), "加盟商编号,传0为获取当前用户")
                .AddResult(true, typeof(string), "返回结果");
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
            CheckMemberHelper("ApiDistributor", "GetDistributorByParentId", "获取我的加盟店")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddArgument("parentid", typeof(string), "加盟商编号,传0为获取当前用户")
                .AddResult(true, typeof(string), "返回结果");
        }
#endif

        [HttpPost]
        public void CreateAccount()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    DataSource.Begin();
                    try
                    {
                        M.Member newmember = DbTable.Load<M.Member>(Request.Form);
                        newmember.ParentId = 0;
                        newmember.Approved = true;
                        newmember.CreationDate = DateTime.Now;
                        if (member.Insert(DataSource) != DataStatus.Success)
                            throw new Exception();

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
                            throw new Exception();

                        DataSource.Commit();

                        SetResult(true);
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        throw;
                    }
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void CreateAccountHelper()
        {
            CheckMemberHelper("ApiDistributor", "GetMoney", "开通加盟商账号")
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
                             .Where(new DbWhere<D.DistributorOrder>("UserId", member.Id) & new DbWhere<D.DistributorOrderMapping>("Evaluation", false) & new DbWhere<P.ProductOrder>("State", P.OrderState.Finished))
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
            CheckMemberHelper("ApiDistributor", "GetUnComment", "获取未评论列表")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(SplitPageData<D.DistributorOrderMapping>), "返回结果");
        }
#endif
    }
}
