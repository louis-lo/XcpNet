using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Pay;
using System.Collections.Generic;
using System.Threading;
using Py = Cnaws.Pay.Modules;
using Cnaws.Data.Query;
using D = XcpNet.Supplier.Modules.Modules;
using Af = XcpNet.AfterSales.Modules;
using XcpNet.Common;
using System.Linq;
using A = XcpNet.Ad.Modules;

namespace XcpNet.ApiSecond.Controllers
{
    public class Buy2 : CommControllers2
    {
        protected override bool CheckProvider(PayProvider provider)
        {
            return true;
        }
        public static string ClassName = "[type]Buy2";
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
                object code=CommUtility.SUCCESS, data=null;
                bool Success = true;
                if (Array.IndexOf(Request.Form.AllKeys, "MemberId") > 0)
                {
                    try
                    {
                        long Memberid = 0;
                        long.TryParse(Request["MemberId"], out Memberid);
                        A.MachineCode machinecode = A.MachineCode.GetByCode(DataSource, member.Mark);
                        if (machinecode == null || machinecode.MemberId == 0)
                            throw new AggregateException();
                        if (Memberid != machinecode.MemberId)
                            throw new AggregateException();
                    }
                    catch (AggregateException)
                    {
                        Success = false;
                        data = null;
                        code = Common.CommUtility.DISTRIBUTOR_EMPTY;
                    }
                    catch(Exception)
                    {
                        Success = false;
                        data = null;
                        code = Common.CommUtility.DISTRIBUTOR_EMPTY;
                    }
                }
                if (Success)
                {
                    int ProvinceId = 0;
                    int CityId = 0;
                    int CountyId = 0;
                    int.TryParse(Request["ProvinceId"], out ProvinceId);
                    int.TryParse(Request["CityId"], out CityId);
                    int.TryParse(Request["CountyId"], out CountyId);
                    code = CommonBuy.CommSetOrder<P.ProductOrder>(DataSource, member, Request.Form["Ids"], Request.Form["Counts"], ProvinceId, CityId, CountyId, out data);
                }
                new CommUtility(this).CommSetResult(code, data);
            }
        }

#if (DEBUG)
        public static void SetOrderHelper()
        {
            CheckMemberApi(ClassName, "SetOrder", "提交订单获取订单号")
                .AddArgument("Ids", typeof(string), "产品编号(多个编号用','隔开)")
                .AddArgument("Counts", typeof(string), "各产品的数量(多个编号用','隔开与Id对应)")
                .AddArgument("provinceid", typeof(int), "收货省Id")
                .AddArgument("cityid", typeof(int), "收货市Id")
                .AddArgument("countyid", typeof(int), "收货区id")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(CommUtility.PRODUCT_EMPTY, "购买的商品为空")
                .AddResult(CommUtility.PRODUCT_SUM_EMPTY, "购买商品的数量为空")
                .AddResult(CommUtility.PRODUCT_SUM_ERROR, "购买的商品以及数量错误")
                .AddResult(CommUtility.PRODUCT_ERROR, "包含错误商品")
                .AddResult(CommUtility.PRODUCT_INVENTORY_ENOUGH, "商品库存不足")
                .AddResult(CommUtility.ORDER_INFO_ADDERROT, "创建订单详情失败")
                .AddResult(CommUtility.ORDER_ADDERROT, "创建订单失败")
                .AddResult(CommUtility.ADDRESS_EMPTY, "收货地址为空")
                .AddResult(CommUtility.ORDER_EMPTY, "订单不存在")
                .AddResult(CommUtility.ORDER_UPDATE_ERROR, "更新订单失败")
                .AddResult(CommUtility.FREIGHT_ADDERROR, "添加快递费失败，返回错误信息")
                .AddResult(true, typeof(string), "订单号");
        }
#endif

        /// <summary>
        /// 合并订单返回总订单号
        /// </summary>
        [HttpPost]
        public void MergeOrder()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                object code, data;
                code = CommonBuy.CommMergeOrder<P.ProductOrder>(DataSource, member, Request.Form["Ids"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }
#if (DEBUG)
        public static void MergeOrderHelper()
        {
            CheckMemberApi(ClassName, "MergeOrder", "合并订单返回总订单号")
                .AddArgument("Ids", typeof(string), "订单编号(多个订单编号用','隔开)")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(CommUtility.PARAMETER_NOFOND, "参数未找到")
                .AddResult(CommUtility.ORDER_UPDATE_ERROR, "更新订单失败")
                .AddResult(CommUtility.ORDER_EMPTY, "找不到该订单号,或订单的状态错误")
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
                SetResult(P.ProductOrder.GetAjaxPageByUserAndStateApi2(DataSource, member.Id, state, page, size, 11));
            }
        }
#if (DEBUG)
        public static void GetAllOrdersHelper()
        {
            CheckMemberApi(ClassName, "GetAllOrders", "根据用户获取所有用户订单")
                 .AddArgument("state", typeof(P.OrderState), "状态,默认为'_'查询所有,对应交易关闭\t等待完善\t等待付款\t等待发货\t出库中\t等待收货\t等待评价\t交易完成\t申请退款\t等待退货发货\t退货已发货\t退款成功")
                 .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                 .AddResult(true, typeof(SplitPageData<P.ProductOrder>), "返回订单结果");
        }
#endif

        public void GetOrdersById()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    string id = Request["Id"];
                    if (!string.IsNullOrEmpty(id))
                    {
                        IList<P.ProductOrder> orders;
                        orders = CommonBuy.GetOrdersNoState(DataSource, id, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(CommUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }
                        Cnaws.Product.ProductCacheInfo pci;
                        IList<P.ProductOrderMapping> maps;
                        List<dynamic> Result = new List<dynamic>();
                        foreach (P.ProductOrder order in orders)
                        {
                            maps = order.GetMapping(DataSource);
                            if (maps.Count > 0)
                            {
                                List<Cnaws.Product.OrderMappingCacheInfo> temp = new List<Cnaws.Product.OrderMappingCacheInfo>(maps.Count);
                                foreach (P.ProductOrderMapping it in maps)
                                {
                                    temp.Add(new Cnaws.Product.OrderMappingCacheInfo(it));
                                }
                                P.Product product = P.Product.GetById(DataSource, maps[0].ProductId);
                                dynamic supplier = null;
                                if (product.ProductType == 1)
                                {
                                    P.StoreInfo info = P.StoreInfo.GetStoreInfoByUserId(DataSource, order.SupplierId);
                                    supplier = info;
                                }
                                else if (product.ProductType == 2)
                                {
                                    D.XDGInfo info = D.XDGInfo.GetXDGInfoByUserId(DataSource, order.SupplierId);
                                    supplier = info;
                                }
                                Result.Add(new { Order = order, Products = temp, Supplier = supplier });
                            }
                        }
                        SetResult(Result);
                    }
                    else
                    {
                        SetResult(CommUtility.PARAMETER_ERROR);
                        throw new AggregateException();
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
        public static void GetOrdersByIdHelper()
        {
            CheckMemberApi(ClassName, "GetOrdersById", "根据总订单号获取订单列表信息--提交订单列表")
                 .AddArgument("Id", typeof(string), "订单编号")
                 .AddResult(true, typeof(string), "返回订单结果");
        }
#endif


        /// <summary>
        /// 根据总订单号获取订单信息列表
        /// </summary>
        public void GetOrderInfo()
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
                            orders = CommonBuy.GetOrdersNoState(DataSource, id, member.Id);
                        else
                            orders = CommonBuy.GetOrders(DataSource, id, (P.OrderState)state, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(CommUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }
                        IList<P.ProductOrderMapping> maps;
                        maps = orders[0].GetMapping(DataSource);
                        List<Cnaws.Product.OrderMappingCacheInfo> temp = new List<Cnaws.Product.OrderMappingCacheInfo>(maps.Count);
                        foreach (P.ProductOrderMapping it in maps)
                        {
                            temp.Add(new Cnaws.Product.OrderMappingCacheInfo(it));
                        }
                        string StoreName = "乡城品店铺";
                        P.StoreInfo StoreInfo = P.StoreInfo.GetStoreInfoByUserId(DataSource, orders[0].SupplierId);
                        if (StoreInfo != null)
                            StoreName = StoreInfo.StoreName;
                        SetResult(new
                        {
                            Order = orders[0],
                            StoreName = StoreName,
                            Products = temp
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
        public static void GetOrderInfoHelper()
        {
            CheckMemberApi(ClassName, "GetOrderInfo", "根据订单号获取订单信息")
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
                object code, data;
                code = CommonBuy.CommSetPerfect<P.ProductOrder>(DataSource, member, Request.Form["Id"], Request.Form["Address"], Request.Form["Message"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }

#if (DEBUG)
        public static void SetPerfectHelper()
        {
            CheckMemberApi(ClassName, "SetPerfect", "设置订单收货地址根据收货地址编号")
                .AddArgument("Id", typeof(string), "订单编号")
                 .AddArgument("Address", typeof(long), "收货地址Id")
                 .AddArgument("Message", typeof(string), "给商家的留言,订单号和留言内容用'_'对应，多个用'@'分隔，格式为：订单号_留言内空@订单号2_留言内空2,最后都以@结尾")
                 .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                 .AddResult(CommUtility.ADDRESS_EMPTY, "收货地址为空")
                 .AddResult(CommUtility.ORDER_EMPTY, "订单不存在")
                 .AddResult(CommUtility.ORDER_UPDATE_ERROR, "更新订单失败")
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
                    IList<P.ProductOrder> orders = CommonBuy.GetOrders(DataSource, Request["Id"], P.OrderState.Perfect, member.Id);
                    if (orders.Count <= 0 || orders[0] == null)
                    {
                        SetResult(CommUtility.ORDER_EMPTY);
                        throw new AggregateException();
                    }
                    M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Request["Address"]), member.Id);
                    if (address == null || address.Id <= 0)
                    {
                        SetResult(CommUtility.ADDRESS_EMPTY);
                        throw new AggregateException();
                    }
                    Money FreightMoney = 0;
                    List<dynamic> list = new List<dynamic>();
                    List<long> productid = new List<long>();
                    foreach (P.ProductOrder item in orders)
                    {
                        Money CussFreightMoney = item.GetFreight(DataSource, address.Province, address.City);
                        list.Add(new { OrderId = item.Id, FreightMoney = CussFreightMoney });
                        FreightMoney += CussFreightMoney;

                        IList<P.ProductOrderMapping> ProductOrderMappings = P.ProductOrderMapping.GetAllByOrder(DataSource, item.Id);
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
                                        productid.Add(pm.ProductId);
                                    }
                                }
                            }
                        }

                    }
                    SetResult(true, new { TotalFreightMoney = FreightMoney, OrderFreight = list, NoProduct = productid });
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
            CheckMemberApi(ClassName, "GetFreight", "根据订单号获取运费")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddArgument("Address", typeof(long), "收货地址Id")
                .AddResult(CommUtility.ADDRESS_EMPTY, "找不到地址")
                .AddResult(true, typeof(Money), "TotalFreightMoney:物流总费用,OrderFreight:订单对应物流费用,NoProduct:无货的产品Id");
        }
#endif


        /// <summary>
        /// 取消订单
        /// </summary>
        [HttpPost]
        public void CancelOrder()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                object code, data;
                code = CommonBuy.CommCancelOrder<P.ProductOrder>(DataSource, member, Request.Form["Id"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }
#if (DEBUG)
        public static void CancelOrderHelper()
        {
            CheckMemberApi(ClassName, "CancelOrder", "取消订单")
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
                object code, data;
                code = CommonBuy.CommDelOrder<P.ProductOrder>(DataSource, member, Request.Form["Id"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }
#if (DEBUG)
        public static void DelOrderHelper()
        {
            CheckMemberApi(ClassName, "DelOrder", "删除订单")
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
                object code, data;
                code = CommonBuy.CommSetReceipt<P.ProductOrder>(DataSource, member, Request.Form["Id"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }
#if (DEBUG)
        public static void SetReceiptHelper()
        {
            CheckMemberApi(ClassName, "SetReceipt", "确定收货")
                 .AddArgument("Id", typeof(string), "订单编号")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif

        public void AddToCart()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                object code, data;
                int ProvinceId = 0;
                int CityId = 0;
                int CountyId = 0;
                int.TryParse(Request["ProvinceId"], out ProvinceId);
                int.TryParse(Request["CityId"], out CityId);
                int.TryParse(Request["CountyId"], out CountyId);
                code = CommonBuy.CommAddToCart<P.ProductCart>(DataSource, member, Request.Form["Ids"], Request.Form["Counts"], ProvinceId, CityId, CountyId, out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }
#if (DEBUG)
        public static void AddToCartHelper()
        {
            CheckMemberApi(ClassName, "AddToCart", "加入购物车")
                .AddArgument("Ids", typeof(string), "商品编号,多个用','隔开")
                .AddArgument("Counts", typeof(string), "数量,多个用','隔开")
                .AddArgument("provinceid", typeof(int), "收货省Id")
                .AddArgument("cityid", typeof(int), "收货市Id")
                .AddArgument("countyid", typeof(int), "收货区id")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(CommUtility.PRODUCT_EMPTY, "找不到商品")
                .AddResult(CommUtility.PRODUCT_INVENTORY_ENOUGH, "库存不足")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif


        public void GetCartList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                int ProvinceId = 0;
                int CityId = 0;
                int CountyId = 0;
                int.TryParse(Request["ProvinceId"], out ProvinceId);
                int.TryParse(Request["CityId"], out CityId);
                int.TryParse(Request["CountyId"], out CountyId);
                IList<dynamic> list = P.ProductCart.GetPageByUser(DataSource, member.Id, ProvinceId, CityId, CountyId);
                SetResult(list);
            }
        }
#if (DEBUG)
        public static void GetCartListHelper()
        {
            CheckMemberApi(ClassName, "GetCartList", "获取我的购物车")
                .AddResult(true, typeof(IList<DataJoin<P.ProductCart, P.Product>>), "处理结果");
        }
#endif
        public void GetNewCartList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                int ProvinceId = 0;
                int CityId = 0;
                int CountyId = 0;
                int.TryParse(Request["ProvinceId"], out ProvinceId);
                int.TryParse(Request["CityId"], out CityId);
                int.TryParse(Request["CountyId"], out CountyId);
                List<dynamic> list = new List<dynamic>();
                long[] SupplierIds = P.ProductCart.GetBySupplierId(DataSource, member.Id);
                long[] DistributorIds = P.ProductCart.GetByDistributorId(DataSource,member.Id);
                if (SupplierIds.Length > 0 || DistributorIds.Length > 0)
                {
                    IList<dynamic> CartList = P.ProductCart.GetPageByUser(DataSource, member.Id, ProvinceId, CityId, CountyId);
                    if (SupplierIds.Length > 0)
                    {
                        IList<dynamic> SupplierList = P.Supplier.GetDynamicAndStorInfoByIds(DataSource, SupplierIds);
                        foreach (dynamic supplier in SupplierList)
                        {
                            IList<dynamic> newList = CartList.Where(x => x.ProductCart_SupplierId == supplier.Supplier_UserId).ToList();
                            if (newList.Count > 0)
                                list.Add(new { Supplier = supplier, CartList = newList });
                        }
                    }
                    if (DistributorIds.Length > 0)
                    {
                        IList<dynamic> DistributorList = D.XDGInfo.GetAndStorInfoByIds(DataSource, DistributorIds);
                        foreach (dynamic distributor in DistributorList)
                        {
                            List<dynamic> newList = CartList.Where(x => x.ProductCart_SupplierId == distributor.Supplier_UserId).ToList();
                            if (newList.Count > 0)
                                list.Add(new { Supplier = distributor, CartList = newList });
                        }
                    }
                }
                SetResult(list);
            }
        }
#if (DEBUG)
        public static void GetNewCartListHelper()
        {
            CheckMemberApi(ClassName, "GetNewCartList", "(新)获取我的购物车")
                .AddResult(true, typeof(IList<DataJoin<P.Supplier, P.StoreInfo>>), "处理结果");
        }
#endif

        public void RefreshCart()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                object code, data;
                code = CommonBuy.CommRefreshCart<P.ProductCart>(DataSource, member, Request.Form["Ids"], Request.Form["Counts"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }
#if (DEBUG)
        public static void RefreshCartHelper()
        {
            CheckMemberApi(ClassName, "RefreshCart", "刷新购物车")
                .AddArgument("Ids", typeof(string), "购物车对应编号,多个用逗号隔开")
                .AddArgument("Counts", typeof(string), "商品数量,多个用逗号隔开")
                .AddResult(CommUtility.PRODUCT_EMPTY, "购买商品为空返回商品Id")
                .AddResult(CommUtility.UPDATE_FAIL, "修改购物车失败返回商品Id")
                .AddResult(CommUtility.INSERT_FAIL, "插入购物车返回商品Id")
                .AddResult(CommUtility.PRODUCT_INVENTORY_ENOUGH, "商品库存不足返回商品Id")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "处理结果,失败返回对应结果以及出错产品ID");
        }
#endif

        public void DelForCart()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                object code, data;
                code = CommonBuy.CommDelForCart<P.ProductCart>(DataSource, member, Request.Form["Ids"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }
#if (DEBUG)
        public static void DelForCartHelper()
        {
            CheckMemberApi(ClassName, "DelForCart", "从购物车中删除")
                .AddArgument("Ids", typeof(string), "商品编号,多个用逗号隔开")
                .AddResult(CommUtility.PRODUCT_EMPTY, "产品为空")
                .AddResult(CommUtility.DEL_FAIL, "删除失败")
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
            CheckMemberApi(ClassName, "GetUntreatedSum", "获取未处理的数量")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
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
            CheckMemberApi(ClassName, "Recharge", "充值")
                .AddArgument("provider", typeof(string), "Alipay,AlipayApp,Wxpay,AlipayDirect,AlipayGateway,AlipayMobile")
                .AddArgument("Money", typeof(double), "金额")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
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
                    SetResult(CommUtility.LOGISTICS_EMPTY);
                }
            }
        }
#if (DEBUG)
        public static void GetLogisticsHelper()
        {
            CheckMemberApi(ClassName, "GetLogistics", "根据订单号获取物流信息")
                .AddArgument("OrderId", typeof(string), "订单编号")
                .AddResult(true, typeof(Cnaws.Product.Logistics.ExpressInfo), "物流信息");
        }
#endif

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
                        SetResult(CommUtility.PASSWORD_EQUALS);
                        throw new AggregateException();
                    }
                }
                string orderId = Request.Form["Id"];
                IList<P.ProductOrder> orders = CommonBuy.GetOrders(DataSource, orderId, P.OrderState.Payment, UserId);
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
                        pr = P.ProductOrder.GetPayRecord(provider, orderId, UserId, orders[0].Title, CommonBuy.SumMoney(orders), openId);
                        if (pr.Insert(DataSource) != DataStatus.Success)
                        {
                            SetResult(CommUtility.INSERT_FAIL, pr);
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        pr.Provider = provider;
                        pr.OpenId = openId;
                        pr.Money = CommonBuy.SumMoney(orders);
                        if (pr.Update(DataSource, ColumnMode.Include, "Provider", "OpenId", "Money") != DataStatus.Success)
                        {
                            SetResult(CommUtility.UPDATE_FAIL);
                            throw new AggregateException();
                        }
                    }
                }
                else
                {
                    //处理进货订单
                    IList<D.DistributorOrder> supplierorder = CommonBuy.GetSupplierOrders(DataSource, orderId, P.OrderState.Payment, UserId);
                    if (supplierorder == null || supplierorder.Count <= 0 || supplierorder[0] == null)
                    {
                        SetResult(CommUtility.ORDER_EMPTY);
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
                        pr = P.ProductOrder.GetPayRecord(provider, orderId, UserId, supplierorder[0].Title, CommonBuy.SumSupplierMoney(supplierorder), openId, P.ProductOrder.PayWholesaleType);
                        if (pr.Insert(DataSource) != DataStatus.Success)
                        {
                            SetResult(CommUtility.INSERT_FAIL);
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        pr.Provider = provider;
                        pr.OpenId = openId;
                        pr.Money = CommonBuy.SumSupplierMoney(supplierorder);
                        if (pr.Update(DataSource, ColumnMode.Include, "Provider", "OpenId", "Money") != DataStatus.Success)
                        {
                            SetResult(CommUtility.UPDATE_FAIL);
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

        [HttpPost]
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
                                SetResult(CommUtility.ORDER_EMPTY);
                                throw new AggregateException();
                            }
                            if (pay.IsOnlinePay)
                            {
                                OnSubmit(pay.Submit(this, pay.PackData(order), SubmitText, ReturnUrl));
                            }
                            else
                            {
                                PaymentResult result;
                                bool value;
                                if (pay.IsCheckMoney)
                                {
                                    value = CheckMoney(order, out result);
                                }
                                else
                                {
                                    result = new PayResult()
                                    {
                                        TradeNo = order.TradeNo,
                                        PayTradeNo = order.TradeNo,
                                        Status = "Success",
                                        TotalFee = order.TotalFee
                                    };
                                    value = true;
                                }
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
            CheckMemberApi(ClassName, "Submit/{支付方式}", "根据订单和支付方式支付,balance:余额支付")
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
                            SetResult(CommUtility.ORDER_EMPTY);
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
                                        SetResult(CommUtility.MONEY_NOT_ENOUGH);
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
                                        SetResult(CommUtility.RECHARGE_FAIL);
                                        throw new AggregateException();
                                    }
                                }
                            }
                            else
                            {
                                SetResult(CommUtility.UPDATE_FAIL);
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
                    SetResult(CommUtility.ORDER_EMPTY);
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
                SetResult(CommUtility.PAYMENT_ERROR);
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
                            SetResult(CommUtility.RECHARGE_FAIL);
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
                            IList<P.ProductOrder> orders = CommonBuy.GetOrders(DataSource, targetId, P.OrderState.Payment, user);
                            foreach (P.ProductOrder order in orders)
                            {
                                if (order == null)
                                {
                                    SetResult(CommUtility.ORDER_EMPTY);
                                    throw new AggregateException();
                                }
                                if (user != order.UserId)
                                {
                                    SetResult(CommUtility.MEMBER_NOTFOUND);
                                    throw new AggregateException();
                                }

                                M.MemberInfo member = M.MemberInfo.GetByRecharge(DataSource, user);
                                if (member == null)
                                {
                                    SetResult(CommUtility.MEMBER_NOTFOUND);
                                    throw new AggregateException();
                                }
                                if (provider.IsOnlinePay)
                                {
                                    if (member.Money < order.TotalMoney)
                                    {
                                        SetResult(CommUtility.MONEY_NOT_ENOUGH);
                                        throw new AggregateException();
                                    }
                                    if (M.MemberInfo.ModifyMoney(DataSource, user, -order.TotalMoney, order.Title, type, targetId) != DataStatus.Success)
                                    {
                                        SetResult(CommUtility.MONEY_NOT_ENOUGH);
                                        throw new AggregateException();
                                    }
                                    member = M.MemberInfo.GetByRecharge(DataSource, user);
                                }
                                if (member.Money >= 0)
                                {
                                    if (order.UpdateStateByUser(DataSource, P.OrderState.Payment, provider.Key) != DataStatus.Success)
                                    {
                                        SetResult(CommUtility.UPDATE_FAIL);
                                        throw new AggregateException();
                                    }
                                }
                                IList<P.ProductOrderMapping> mappings = P.ProductOrderMapping.GetAllByOrder(DataSource, order.Id);
                                foreach (P.ProductOrderMapping mapping in mappings)
                                {
                                    if (P.Product.UpdateInventoryById(DataSource, mapping.ProductId, mapping.Count) != DataStatus.Success)
                                    {
                                        SetResult(CommUtility.UPDATE_FAIL);
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
                            IList<D.DistributorOrder> orders = CommonBuy.GetSupplierOrders(DataSource, targetId, P.OrderState.Payment, user);
                            foreach (D.DistributorOrder order in orders)
                            {
                                if (order == null)
                                {
                                    SetResult(CommUtility.ORDER_EMPTY);
                                    throw new AggregateException();
                                }
                                if (user != order.UserId)
                                {
                                    SetResult(CommUtility.MEMBER_NOTFOUND);
                                    throw new AggregateException();
                                }

                                M.MemberInfo member = M.MemberInfo.GetByRecharge(DataSource, user);
                                if (member == null)
                                {
                                    SetResult(CommUtility.MEMBER_NOTFOUND);
                                    throw new AggregateException();
                                }
                                if (provider.IsOnlinePay)
                                {
                                    if (member.Money < order.TotalMoney)
                                    {
                                        SetResult(CommUtility.MONEY_NOT_ENOUGH);
                                        throw new AggregateException();
                                    }
                                    if (M.MemberInfo.ModifyMoney(DataSource, user, -order.TotalMoney, order.Title, type, targetId) != DataStatus.Success)
                                    {
                                        SetResult(CommUtility.MONEY_NOT_ENOUGH);
                                        throw new AggregateException();
                                    }
                                    member = M.MemberInfo.GetByRecharge(DataSource, user);
                                }
                                if (member.Money >= 0)
                                {
                                    if (order.UpdateStateByUser(DataSource, P.OrderState.Payment, provider.Key) != DataStatus.Success)
                                    {
                                        SetResult(CommUtility.UPDATE_FAIL);
                                        throw new AggregateException();
                                    }
                                }
                                IList<D.DistributorOrderMapping> mappings = D.DistributorOrderMapping.GetAllByOrder(DataSource, order.Id);
                                foreach (D.DistributorOrderMapping mapping in mappings)
                                {
                                    if (D.DistributorProduct.UpdateInventoryById(DataSource, mapping.ProductId, mapping.Count) != DataStatus.Success)
                                    {
                                        SetResult(CommUtility.UPDATE_FAIL);
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
                            SetResult(CommUtility.UPDATE_FAIL);
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

        public void GetOrderState()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    string orderId = "";
                    if (!string.IsNullOrEmpty(Request["OrderId"]))
                        orderId = Request["OrderId"];
                    else
                    {
                        SetResult(false);
                        return;
                    }

                    IList<P.ProductOrder> orders = CommonBuy.GetOrdersNoState(DataSource, orderId, member.Id);
                    bool state = true;
                    if (orders != null && orders.Count > 0 && orders[0] != null)
                    {
                        foreach (P.ProductOrder order in orders)
                        {
                            if ((int)order.State < (int)P.OrderState.Delivery)
                            {
                                state = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        IList<D.DistributorOrder> supplierorder = CommonBuy.GetSupplierOrdersNoState(DataSource, orderId, member.Id);
                        if (supplierorder != null && supplierorder.Count > 0 && supplierorder[0] != null)
                        {
                            foreach (D.DistributorOrder order in supplierorder)
                            {
                                if ((int)order.State < (int)P.OrderState.Delivery)
                                {
                                    state = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Cnaws.Pay.Modules.PayRecord payrecord = Cnaws.Pay.Modules.PayRecord.GetByTypeAndStatusAndUserAndId(DataSource, orderId, 0, member.Id, PayStatus.PaySuccess);
                            if (payrecord == null || payrecord.Status != PayStatus.PaySuccess)
                                state = false;
                        }
                    }
                    SetResult(state);
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
            
        }
#if (DEBUG)
        public static void GetOrderStateHelper()
        {
            CheckMemberApi(ClassName, "GetOrderState", "获取订单支付状态")
                .AddArgument("Id", typeof(string), "订单编号")
                .AddArgument("PayPassword", typeof(string), "支付密码(md5)")
                .AddResult(true, typeof(string), "成功返回-200");
        }
#endif

    }
}
