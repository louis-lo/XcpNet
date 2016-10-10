using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Pay;
using System.Collections.Generic;
using D = XcpNet.Supplier.Modules.Modules;
using XcpNet.Common;
using System.Linq;

namespace XcpNet.ApiSecond.Controllers
{
    public class DistributorBuy2 : CommControllers2
    {
        protected override bool CheckProvider(PayProvider provider)
        {
            return true;
        }
        public static string ClassName = "[type]DistributorBuy2";
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
                object code, data;
                P.Distributor distributor = P.Distributor.GetById(DataSource, member.Id);
                if (distributor != null)
                {
                    code = CommonBuy.CommSetOrder<D.DistributorOrder>(DataSource, member, Request.Form["Ids"], Request.Form["Counts"], distributor.Province, distributor.City, distributor.County, out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                else
                    SetResult(CommUtility.MEMBER_NOTFOUND);
            }
        }

#if (DEBUG)
        public static void SetOrderHelper()
        {
            CheckMemberApi(ClassName, "SetOrder", "提交订单获取订单号")
                .AddArgument("Ids", typeof(string), "产品编号(多个编号用','隔开)")
                .AddArgument("Counts", typeof(string), "各产品的数量(多个编号用','隔开与Id对应)")
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
                code = CommonBuy.CommMergeOrder<D.DistributorOrder>(DataSource, member, Request.Form["Ids"], out data);
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
        /// 设置订单收货地址根据收货地址编号
        /// </summary>
        [HttpPost]
        public void SetPerfect()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                object code, data;
                string Address = Request.Form["Address"];
                if (string.IsNullOrEmpty(Request.Form["Address"]))
                    Address = "0";
                code = CommonBuy.CommSetPerfect<D.DistributorOrder>(DataSource, member, Request.Form["Id"], Request.Form["Address"], Request.Form["Message"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }

#if (DEBUG)
        public static void SetPerfectHelper()
        {
            CheckMemberApi(ClassName, "SetPerfect", "设置订单收货地址根据收货地址编号")
                .AddArgument("Id", typeof(string), "订单编号")
                 .AddArgument("Address", typeof(long), "收货地址Id,加盟商传0")
                 .AddArgument("Message", typeof(string), "给商家的留言,订单号和留言内容用'_'对应，多个用'@'分隔，格式为：订单号_留言内空@订单号2_留言内空2,最后都以@结尾")
                 .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                 .AddResult(CommUtility.ADDRESS_EMPTY, "收货地址为空")
                 .AddResult(CommUtility.ORDER_EMPTY, "订单不存在")
                 .AddResult(CommUtility.ORDER_UPDATE_ERROR, "更新订单失败")
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
                SetResult(D.DistributorOrder.GetAjaxPageByUserAndStateApi2(DataSource, member.Id, state, page, size, 11));
            }
        }
#if (DEBUG)
        public static void GetAllOrdersHelper()
        {
            CheckMemberApi(ClassName, "GetAllOrders", "根据用户获取所有用户订单")
                 .AddArgument("state", typeof(P.OrderState), "状态,默认为'_'查询所有,对应交易关闭\t等待完善\t等待付款\t等待发货\t等待收货\t等待评价\t交易完成\t申请退款\t等待退货发货\t退货已发货\t退款成功")
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
                        IList<D.DistributorOrder> orders;
                        orders = CommonBuy.GetSupplierOrdersNoState(DataSource, id, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(CommUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }
                        IList<D.DistributorOrderMapping> maps;
                        List<dynamic> Result = new List<dynamic>();
                        foreach (D.DistributorOrder order in orders)
                        {
                            maps = order.GetMapping(DataSource);
                            if (maps.Count > 0)
                            {
                                List<XcpNet.Supplier.Modules.DistributorOrderMappingCacheInfo> temp = new List<XcpNet.Supplier.Modules.DistributorOrderMappingCacheInfo>(maps.Count);
                                foreach (D.DistributorOrderMapping it in maps)
                                {
                                    temp.Add(new Supplier.Modules.DistributorOrderMappingCacheInfo(it));
                                }
                                dynamic supplier = null;
                                if (order.SupplierId > 0)
                                {
                                    P.StoreInfo info = P.StoreInfo.GetStoreInfoByUserId(DataSource, order.SupplierId);
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
                        IList<D.DistributorOrder> orders;
                        if (state == -99)
                            orders = CommonBuy.GetSupplierOrdersNoState(DataSource, id, member.Id);
                        else
                            orders = CommonBuy.GetSupplierOrders(DataSource, id, (P.OrderState)state, member.Id);
                        if (orders == null || orders.Count <= 0 || orders[0] == null)
                        {
                            SetResult(CommUtility.ORDER_EMPTY);
                            throw new AggregateException();
                        }
                        IList<D.DistributorOrderMapping> maps;
                        maps = orders[0].GetMapping(DataSource);
                        List<Supplier.Modules.DistributorOrderMappingCacheInfo> temp = new List<Supplier.Modules.DistributorOrderMappingCacheInfo>(maps.Count);
                        foreach (D.DistributorOrderMapping it in maps)
                        {
                            temp.Add(new Supplier.Modules.DistributorOrderMappingCacheInfo(it));
                        }
                        string StoreName = P.StoreInfo.GetStoreInfoByUserId(DataSource, orders[0].SupplierId).StoreName;
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
                .AddResult(true, typeof(IList<D.DistributorOrder>), "Orders:DistributorOrder,Mappings:DistributorOrderMapping");
        }
#endif

        public void AddToCart()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                P.Distributor distributor = P.Distributor.GetById(DataSource, member.Id);
                object code, data;
                code = CommonBuy.CommAddToCart<D.DistributorCart>(DataSource, member, Request.Form["Ids"], Request.Form["Counts"], distributor.Province, distributor.City, distributor.County, out data);
                new CommUtility(this).CommSetResult(code, data);
            }
        }
#if (DEBUG)
        public static void AddToCartHelper()
        {
            CheckMemberApi(ClassName, "AddToCart", "加入购物车")
                .AddArgument("Ids", typeof(string), "商品编号,多个用','隔开")
                .AddArgument("Counts", typeof(string), "数量,多个用','隔开")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(CommUtility.PRODUCT_EMPTY, "找不到商品")
                .AddResult(CommUtility.PRODUCT_INVENTORY_ENOUGH, "库存不足")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif
        public void RefreshCart()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                object code, data;
                code = CommonBuy.CommRefreshCart<D.DistributorCart>(DataSource, member, Request.Form["Ids"], Request.Form["Counts"], out data);
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
                code = CommonBuy.CommDelForCart<D.DistributorCart>(DataSource, member, Request.Form["Ids"], out data);
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

        public void GetCartList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                P.Distributor distributor = P.Distributor.GetById(DataSource, member.Id);
                IList<dynamic> list = D.DistributorCart.GetPageByUser(DataSource, member.Id, distributor.Province, distributor.City, distributor.County);
                SetResult(list);
            }
        }
#if (DEBUG)
        public static void GetCartListHelper()
        {
            CheckMemberApi(ClassName, "GetCartList", "获取我的购物车")
                .AddResult(true, typeof(IList<DataJoin<D.DistributorCart, D.DistributorProduct>>), "处理结果");
        }
#endif
        public void GetNewCartList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                P.Distributor distributor = P.Distributor.GetById(DataSource, member.Id);

                long[] SupplierIds = D.DistributorCart.GetBySupplierId(DataSource, member.Id);
                List<dynamic> list = new List<dynamic>();
                if (SupplierIds.Length > 0)
                {
                    IList<dynamic> SupplierList = P.Supplier.GetDynamicAndStorInfoByIds(DataSource, SupplierIds);
                    IList<dynamic> CartList = D.DistributorCart.GetPageByUser(DataSource, member.Id, distributor.Province, distributor.City, distributor.County);

                    foreach (dynamic supplier in SupplierList)
                    {
                        IList<dynamic> newList = CartList.Where(x => x.DistributorCart_SupplierId == supplier.Supplier_UserId).ToList();
                        if (newList.Count > 0)
                            list.Add(new { Supplier = supplier, CartList = newList });
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
                code = CommonBuy.CommCancelOrder<D.DistributorOrder>(DataSource, member, Request.Form["Id"], out data);
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
                code = CommonBuy.CommDelOrder<D.DistributorOrder>(DataSource, member, Request.Form["Id"], out data);
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
                code = CommonBuy.CommSetReceipt<D.DistributorOrder>(DataSource, member, Request.Form["Id"], out data);
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

    }
}
