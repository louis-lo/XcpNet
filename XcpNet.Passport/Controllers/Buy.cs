using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws;
using Cnaws.Passport.Controllers;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using L = Cnaws.Product.Logistics;
using Cnaws.Pay;
using System.Collections.Generic;
using Cnaws.Pay.Modules;
using System.Threading;
using Cnaws.Area;
using System.Web;
using A = XcpNet.AfterSales.Modules;
using S = XcpNet.Supplier.Modules.Modules;
using Cnaws.Web.Templates;
using Cnaws.Sms;
using Cnaws.Sms.Modules;
using Cnaws.Sms.Providers;
using XcpNet.Services.Sms;
using XcpNet.Common;
using System.Drawing;
using System.IO;

namespace XcpNet.Passport.Controllers
{
    public sealed class Buy : Extension.Recharge
    {
        protected override bool CheckProvider(PayProvider provider)
        {
            return true;
        }

        protected override void OnIndex()
        {
            if (IsPost)
            {
                object code, data;
                try
                {
                    var m = Cnaws.Passport.Modules.MemberInfo.GetById(this.DataSource, this.User.Identity.Id);

                    M.Member member = new M.Member { Id = User.Identity.Id };
                    string Id = string.Join(",", Request.Form["Id"]);
                    string Count = string.Join(",", Request.Form["Count"]);
                    code = CommonBuy.CommSetOrder<P.ProductOrder>(DataSource, member, Id, Count, Location.ProvinceId, Location.CityId, Location.CountyId, out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }
            }
            else
            {
                NotFound();
            }
        }

        [HttpPost]
        [Authorize(true)]
        public void One()
        {
            bool end = false;
            DataSource.Begin();
            try
            {
                bool isEnd = false;
                int productId = int.Parse(Request.Form["ProductId"]);
                long productNum = long.Parse(Request.Form["ProductNum"]);
                int count = int.Parse(Request.Form["Count"]);
                long addrId = long.Parse(Request.Form["AddressId"]);
                string ip = ClientIp;
                string ipaddr = string.Empty;

                try
                {
                    using (IPArea area = new IPArea())
                        ipaddr = area.Search(ip).Country;
                }
                catch (Exception) { }

                M.MemberInfo member = M.MemberInfo.GetByRecharge(DataSource, User.Identity.Id);
                if (member.Money < count)
                    throw new NotSupportedException("余额不足");

                P.OneProduct product = P.OneProduct.GetById(DataSource, productId);
                if (product == null)
                    throw new Exception("商品不存在");

                int max = product.Count + P.OneProductOrder.BeginOrderNumber;

                M.ShippingAddress addr = M.ShippingAddress.GetById(DataSource, addrId, User.Identity.Id);
                if (addr == null)
                    throw new Exception("请选择收货地址");

                P.OneProductOrder value = new P.OneProductOrder()
                {
                    ProductId = productId,
                    ProductNum = productNum,
                    UserId = User.Identity.Id,
                    OrderNum = 0,
                    Address = addr.BuildInfo(),
                    Ip = ip,
                    IpAddress = ipaddr,
                    CreationDate = DateTime.Now
                };

                for (int i = 0; i < count; ++i)
                {
                    if (value.Insert(DataSource) != DataStatus.Success)
                        throw new Exception("创建订单失败");

                    if (value.Id <= 0)
                        throw new Exception("创建订单失败");

                    if (M.MemberInfo.ModifyMoney(DataSource, User.Identity.Id, -1, "1元抢", P.OneProductOrder.PayOneProductType, value.Id.ToString()) != DataStatus.Success)
                        throw new Exception("余额不足");

                    value.OrderNum = P.OneProductOrder.GetCountByNumber(DataSource, productId, productNum, value.Id) + P.OneProductOrder.BeginOrderNumber;
                    if (value.OrderNum >= (max))
                        throw new Exception("份数不足");

                    if (value.Update(DataSource, ColumnMode.Include, "OrderNum") != DataStatus.Success)
                        throw new Exception("更新订单失败");

                    if (value.OrderNum == (max - 1))
                        isEnd = true;
                }

                DataSource.Commit();
                end = true;

                if (isEnd)
                {
                    try
                    {
                        long tmp = 0L;
                        DateTime now = DateTime.Now;
                        IList<P.OneProductOrder> list = P.OneProductOrder.GetTop(DataSource, now, 100);
                        if (list.Count < 100)
                            throw new Exception();
                        foreach (P.OneProductOrder item in list)
                            tmp += int.Parse(item.CreationDate.ToString("HHmmssfff"));
                        int number = (int)(tmp % product.Count) + P.OneProductOrder.BeginOrderNumber;

                        P.OneProductOrder order = P.OneProductOrder.GetByOrder(DataSource, productId, productNum, number);
                        if (order == null)
                            throw new Exception();

                        P.OneProductNumber temp = new P.OneProductNumber()
                        {
                            Id = productNum,
                            State = P.OneProductNumberState.Delivery,
                            LuckNum = number,
                            UserId = order.UserId,
                            Address = order.Address,
                            EndDate = now
                        };
                        temp.Update(DataSource, ColumnMode.Include, "State", "LuckNum", "UserId", "Address", "EndDate");

                        if (product.Approved)
                            P.OneProductNumber.Create(DataSource, product.Id);
                    }
                    catch (Exception) { }
                }

                Redirect(Request.UrlReferrer.ToString());
            }
            catch (ThreadAbortException)
            {
                if (!end)
                    DataSource.Rollback();
            }
            catch (NotSupportedException)
            {
                DataSource.Rollback();
                Redirect(string.Concat(GetUrl("/recharge"), "?target=", HttpUtility.UrlEncode(Request.UrlReferrer.ToString())));
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                Response.Write(string.Concat("<script type=\"text/javascript\">alert('", ex.Message.Replace("\r", string.Empty).Replace("\n", string.Empty), "');window.history.go(-1);</script>"));
            }
        }

        [Authorize(true)]
        public void Perfect(string id, long addressId = 0)
        {
            IList<P.ProductOrder> orders = CommonBuy.GetOrders(DataSource, id, P.OrderState.Perfect, User.Identity.Id);
            if (orders[0] == null || orders.Count <= 0)
                orders = CommonBuy.GetOrdersNoState(DataSource, id, User.Identity.Id);
            if (orders[0] == null || orders.Count <= 0)
            {

                return;
            }

            M.ShippingAddress defaultAdrs = new M.ShippingAddress()
            {
                Province = Location.ProvinceId,
                City = Location.CityId,
                County = Location.CountyId
            };

            if (addressId <= 0)
            {
                M.ShippingAddress tmpAdrs = M.ShippingAddress.GetDefault(DataSource, User.Identity.Id);
                if (tmpAdrs != null)
                {
                    defaultAdrs = tmpAdrs;
                    addressId = defaultAdrs.Id;
                }
            }
            else
            {
                defaultAdrs = M.ShippingAddress.GetById(DataSource, addressId, User.Identity.Id);
                M.ShippingAddress.SetDefault(DataSource, addressId, User.Identity.Id);
            }

            this["DefaultAdrs"] = defaultAdrs;
            this["Member"] = M.MemberInfo.GetByRecharge(DataSource, User.Identity.Id);
            this["OrderList"] = orders;
            this["TotalMoney"] = SumMoney(orders);
            this["AddressList"] = M.ShippingAddress.GetAll(DataSource, User.Identity.Id);
            this["OrderId"] = id;
            City city;
            using (IPArea area = new IPArea())
            {
                IPLocation local = area.Search(ClientIp);
                using (Country country = Country.GetCountry())
                    city = local.GetCity(country);
            }
            this["Location"] = city != null ? city.Id : 441900;
            Render("buy.html");
        }

        [HttpPost]
        [Authorize(true)]
        public void OnPerfect()
        {
            object code, data;
            try
            {
                M.Member member = new M.Member { Id = User.Identity.Id };
                code = CommonBuy.CommSetPerfect<P.ProductOrder>(DataSource, member, Request["Id"], Request["Address"], Request["Message"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
            //DataSource.Begin();
            //try
            //{
            //    M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Request.Form["Address"]), User.Identity.Id);
            //    if (address == null)
            //        throw new Exception("收货地址为空");

            //    string orderId = Request.Form["Id"];
            //    IList<P.ProductOrder> orders = GetOrders(orderId, P.OrderState.Perfect, User.Identity.Id);
            //    if (orders[0] == null || orders.Count <= 0)
            //    {
            //        IList<P.ProductOrder> neworders = GetOrders(orderId, P.OrderState.Payment, User.Identity.Id);
            //        if (neworders[0] == null || neworders.Count <= 0)
            //            throw new Exception("订单不存在");
            //        DataSource.Commit();
            //    }
            //    else
            //    {
            //        foreach (P.ProductOrder order in orders)
            //        {
            //            order.State = P.OrderState.Payment;
            //            order.FreightMoney = P.ProductOrder.GetFreight(DataSource, order.Id, address.Province, address.City, order.TotalMoney);
            //            order.TotalMoney = order.TotalMoney + order.FreightMoney;
            //            order.Address = address.BuildInfo();
            //            order.Message = Request.Form["Message"];
            //            if (order.UpdatePerfectByUser(DataSource) != DataStatus.Success)
            //                throw new Exception("更新订单失败");

            //            IList<P.ProductOrderMapping> ProductOrderMappings = P.ProductOrderMapping.GetAllByOrder(DataSource, order.Id);
            //            if (ProductOrderMappings.Count > 0 && ProductOrderMappings[0].ProductId > 0)
            //            {
            //                foreach (P.ProductOrderMapping pm in ProductOrderMappings)
            //                {
            //                    try { P.ProductCart.Remove(DataSource, pm.ProductId, User.Identity.Id); }
            //                    catch (Exception) { }
            //                }
            //            }
            //        }

            //        DataSource.Commit();
            //    }
            //    Redirect(GetUrl("/buy/payment/", orderId));
            //}
            //catch (ThreadAbortException) { }
            //catch (Exception)
            //{
            //    DataSource.Rollback();
            //    throw;
            //}
        }

        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void Freight()
        {
            try
            {
                string orderId = Request.Form["Order"];
                IList<P.ProductOrder> orders = CommonBuy.GetOrders(DataSource, orderId, P.OrderState.Perfect, User.Identity.Id);
                if (orders[0] == null || orders.Count <= 0)
                    orders = CommonBuy.GetOrdersNoState(DataSource, orderId, User.Identity.Id);
                M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Request.Form["Address"]), User.Identity.Id);
                Money FreightMoney = 0;
                foreach (P.ProductOrder item in orders)
                    FreightMoney += item.GetFreight(DataSource, address.Province, address.City);
                SetResult(true, FreightMoney.ToString("C2"));
            }
            catch (Exception e)
            {
                SetResult(CommUtility.PROGRAM_ERROR);
            }
        }
        private new Money SumMoney(IList<P.ProductOrder> orders)
        {
            Money money = 0;
            foreach (P.ProductOrder order in orders)
                money += order.TotalMoney;
            return money;
        }
        private new Money SumSupplierMoney(IList<S.DistributorOrder> orders)
        {
            Money money = 0;
            foreach (P.ProductOrder order in orders)
                money += order.TotalMoney;
            return money;
        }
        [Authorize(true)]
        public void Payment(string id)
        {
            IList<P.ProductOrder> orders = CommonBuy.GetOrders(DataSource, id, P.OrderState.Payment, User.Identity.Id);
            if (orders != null && orders.Count > 0 && orders[0] != null)
            {
                this["OrderList"] = orders;
                this["OrderType"] = 0;
                this["TotalMoney"] = SumMoney(orders);
            }
            else
            {
                //处理进货订单
                IList<S.DistributorOrder> supplierorder = CommonBuy.GetSupplierOrders(DataSource, id, P.OrderState.Payment, User.Identity.Id);
                if (supplierorder == null || supplierorder.Count <= 0 || supplierorder[0] == null)
                    throw new Exception("订单不存在");
                this["OrderList"] = supplierorder;
                this["OrderType"] = 1;
                this["TotalMoney"] = SumSupplierMoney(supplierorder);
            }
            this["OrderId"] = id;
            this["Money"] = M.MemberInfo.GetByRecharge(DataSource, User.Identity.Id)?.Money;
            this["HasPayPassword"] = !string.IsNullOrEmpty(M.MemberInfo.GetPayPasswordById(DataSource, User.Identity.Id));

            Render("buy_payment.html");
        }

        [Authorize(true)]
        public void Refund(string id, long productId, int step = 1)
        {
            P.ProductOrder order = P.ProductOrder.GetByUser(DataSource, id, User.Identity.Id);
            this["Order"] = order;
            Render(string.Concat("refund", step, ".html"));
        }

        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void Del()
        {
            object code, data;
            try
            {
                M.Member member = new M.Member { Id = User.Identity.Id };
                code = CommonBuy.CommDelOrder<P.ProductOrder>(DataSource, member, Request.Form["Id"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }
        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void Cancel()
        {
            object code, data;
            try
            {
                M.Member member = new M.Member { Id = User.Identity.Id };
                code = CommonBuy.CommCancelOrder<P.ProductOrder>(DataSource, member, Request.Form["Id"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }

        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void Receipt()
        {
            object code, data;
            try
            {
                M.Member member = new M.Member { Id = User.Identity.Id };
                code = CommonBuy.CommSetReceipt<P.ProductOrder>(DataSource, member, Request.Form["Id"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }

        [Authorize(true)]
        public void Status(string id, long productid = 0)
        {
            P.ProductOrder order = P.ProductOrder.GetByUser(DataSource, id, User.Identity.Id);
            IList<P.ProductOrderMapping> orderMapping = P.ProductOrderMapping.GetAllByOrder(DataSource, id);
            P.StoreInfo store = P.StoreInfo.GetStoreInfoByUserId(DataSource, User.Identity.Id);
            P.Supplier supplier = P.Supplier.GetById(DataSource, order.SupplierId);
            PayRecord pay = PayRecord.GetById(DataSource, id, PaymentType.Pay);
            this["Pay"] = pay;
            this["Store"] = store;
            this["Order"] = order;
            this["OrderMapping"] = orderMapping;
            this["shopName"] = store != null ? store.StoreName : supplier.Company;
            this["GetOrderStateName"] = new FuncHandler(args =>
            {
                int state = Convert.ToInt32(args[0]);
                string[] OrderStateList = { "交易关闭", "等待完善", "等待付款", "等待发货", "等待收货", "等待评价", "交易完成", "申请退款", "等待退货发货", "退货已发货", "退款成功" };
                return OrderStateList[state];
            });


            P.ProductLogistics log;
            if (order != null && order.State > P.OrderState.Delivery)
            {
                log = P.ProductLogistics.GetByOrder(DataSource, order.Id);
                if (log != null)
                {
                    try
                    {
                    }
                    catch (Exception) { }
                }
                else
                {
                    log = new P.ProductLogistics();
                }
            }
            else
            {
                log = new P.ProductLogistics();
            }
            this["Logistics"] = log;
            if (!string.IsNullOrEmpty(log.BillNo) && !string.IsNullOrEmpty(log.ProviderKey))
            {
                try
                {
                    if (string.IsNullOrEmpty(log.ProviderDetailed))
                    {
                        string providerDetailed = L.ExpressQuery.QueryReturnJson(log.ProviderKey, log.BillNo);
                        L.ExpressInfo expressInfo = L.ExpressQuery.Query(providerDetailed);
                        this["ExpressInfo"] = expressInfo;
                        if (expressInfo.state == "3")
                        {
                            log.ProviderDetailed = providerDetailed;
                            P.ProductLogistics.UpdateProviderDetailed(DataSource, log);
                        }
                    }
                    else
                    {
                        this["ExpressInfo"] = L.ExpressQuery.Query(log.ProviderDetailed);
                    }
                }
                catch (Exception) { this["ExpressInfo"] = null; }
            }
            else
            {
                this["ExpressInfo"] = null;
            }
            this["GetDay"] = new FuncHandler((args) =>
            {
                if (!string.IsNullOrEmpty(Convert.ToString(args[0])))
                    return Convert.ToDateTime(args[0]).ToString("yyyy-MM-dd");
                else
                    return "";
            });
            this["GetWeek"] = new FuncHandler((args) =>
            {
                if (!string.IsNullOrEmpty(Convert.ToString(args[0])))
                {
                    string week;
                    string dt = Convert.ToDateTime(args[0]).DayOfWeek.ToString();
                    switch (dt)
                    {
                        case "Monday":
                            week = "周一";
                            break;
                        case "Tuesday":
                            week = "周二";
                            break;
                        case "Wednesday":
                            week = "周三";
                            break;
                        case "Thursday":
                            week = "周四";
                            break;
                        case "Friday":
                            week = "周五";
                            break;
                        case "Saturday":
                            week = "周六";
                            break;
                        case "Sunday":
                            week = "周日";
                            break;
                        default:
                            week = "";
                            break;
                    }
                    return week;
                }
                else
                    return "";
            });
            this["GetTime"] = new FuncHandler((args) =>
            {
                if (!string.IsNullOrEmpty(Convert.ToString(args[0])))
                    return Convert.ToDateTime(args[0]).ToString("HH:mm:ss");
                else
                    return "";
            });

            Render("buy_status.html");
        }

        [Authorize(true)]
        public void Logistics(string id)
        {
            P.ProductLogistics log;
            P.ProductOrder order = P.ProductOrder.GetById(DataSource, id);
            if (order != null && order.State > P.OrderState.Delivery)
            {
                log = P.ProductLogistics.GetByOrder(DataSource, order.Id);
                if (log != null)
                {
                    try
                    {
                    }
                    catch (Exception) { }
                }
                else
                {
                    log = new P.ProductLogistics();
                }
            }
            else
            {
                log = new P.ProductLogistics();
            }
            this["Logistics"] = log;
            this["Order"] = order;
            try
            {
                if (string.IsNullOrEmpty(log.ProviderDetailed))
                {
                    string providerDetailed = Cnaws.Product.Logistics.ExpressQuery.QueryReturnJson(log.ProviderKey, log.BillNo);
                    Cnaws.Product.Logistics.ExpressInfo expressInfo = Cnaws.Product.Logistics.ExpressQuery.Query(providerDetailed);
                    this["ExpressInfo"] = expressInfo;
                    if (expressInfo.state == "3")
                    {
                        log.ProviderDetailed = providerDetailed;
                        P.ProductLogistics.UpdateProviderDetailed(DataSource, log);
                    }
                }
                else
                {
                    this["ExpressInfo"] = Cnaws.Product.Logistics.ExpressQuery.Query(log.ProviderDetailed);
                }
            }
            catch (Exception) { this["ExpressInfo"] = null; }
            this["GetDay"] = new FuncHandler((args) =>
            {
                if (!string.IsNullOrEmpty(Convert.ToString(args[0])))
                    return Convert.ToDateTime(args[0]).ToString("yyyy-MM-dd");
                else
                    return "";
            });
            this["GetWeek"] = new FuncHandler((args) =>
            {
                if (!string.IsNullOrEmpty(Convert.ToString(args[0])))
                {
                    string week;
                    string dt = Convert.ToDateTime(args[0]).DayOfWeek.ToString();
                    switch (dt)
                    {
                        case "Monday":
                            week = "周一";
                            break;
                        case "Tuesday":
                            week = "周二";
                            break;
                        case "Wednesday":
                            week = "周三";
                            break;
                        case "Thursday":
                            week = "周四";
                            break;
                        case "Friday":
                            week = "周五";
                            break;
                        case "Saturday":
                            week = "周六";
                            break;
                        case "Sunday":
                            week = "周日";
                            break;
                        default:
                            week = "";
                            break;
                    }
                    return week;
                }
                else
                    return "";
            });
            this["GetTime"] = new FuncHandler((args) =>
            {
                if (!string.IsNullOrEmpty(Convert.ToString(args[0])))
                    return Convert.ToDateTime(args[0]).ToString("HH:mm:ss");
                else
                    return "";
            });
            Render("buy_logistics.html");
        }

        protected override IPayOrder GetPayOrder(string provider)
        {
            DataSource.Begin();
            try
            {
                long UserId = User.Identity.Id;
                if (UserId == 0)
                {
                    if (!string.IsNullOrEmpty(Request["mark"]) && !string.IsNullOrEmpty(Request["token"]))
                    {
                        Guid token;
                        M.Member newmember;
                        if (!Guid.TryParse(Request["token"], out token) || Guid.Empty.Equals(token))
                        {
                            newmember = null;
                        }
                        newmember = M.Member.GetByToken(DataSource, token);
                        if (newmember != null)
                        {
                            string mark = Request["mark"];
                            if (string.Equals(mark, newmember.Mark) || token.Equals(newmember.Token))
                            {
                                UserId = newmember.Id;
                            }
                        }
                    }
                }
                if (UserId == 0)
                    throw new Exception("找不到用户");
                string pwd = Request.Form["PayPassword"];
                if (!string.IsNullOrEmpty(pwd))
                {
                    if (!M.MemberInfo.CheckPayPassword(DataSource, UserId, pwd))
                        throw new Exception("支付密码错误");
                }
                string orderId = Request.Form["Id"];
                IList<P.ProductOrder> orders = CommonBuy.GetOrders(DataSource, orderId, P.OrderState.Payment, UserId);
                PayRecord pr;
                if (orders != null && orders.Count > 0 && orders[0] != null)
                {
                    bool IsInventory = true;
                    foreach (P.ProductOrder order in orders)
                    {
                        foreach (P.ProductOrderMapping orderMapping in order.GetMapping(DataSource))
                        {
                            P.Product product = P.Product.GetSaleProduct(DataSource, orderMapping.ProductId);
                            if (product == null)
                            {
                                throw new Exception("支付失败，存在已经下架的商品！");
                            }
                            if (product.Inventory < orderMapping.Count)
                            {
                                IsInventory = false;
                                break;
                            }
                        }
                        if (!IsInventory)
                        {
                            break;
                        }
                    }
                    if (!IsInventory)
                    {
                        throw new Exception("支付失败，存在库存不足的商品！");
                    }
                    //处理产品订单
                    string openId = null;
                    M.OAuth2Member member = M.OAuth2Member.GetByUserPay(DataSource, provider, UserId);
                    if (member != null)
                        openId = member.UserId;

                    pr = PayRecord.GetByUser(DataSource, orderId, UserId, PaymentType.Pay, PayStatus.Paying);
                    if (pr == null)
                    {
                        pr = P.ProductOrder.GetPayRecord(provider, orderId, UserId, orders[0].Title, SumMoney(orders), openId);
                        if (pr.Insert(DataSource) != DataStatus.Success)
                            throw new Exception("插入支付记录失败");
                    }
                    else
                    {
                        pr.Provider = provider;
                        pr.OpenId = openId;
                        pr.Money = SumMoney(orders);
                        if (pr.Update(DataSource, ColumnMode.Include, "Provider", "OpenId", "Money") != DataStatus.Success)
                            throw new Exception("更新支付记录失败");
                    }
                }
                else
                {
                    //处理进货订单
                    IList<S.DistributorOrder> supplierorder = CommonBuy.GetSupplierOrders(DataSource, orderId, P.OrderState.Payment, UserId);
                    if (supplierorder == null || supplierorder.Count <= 0 || supplierorder[0] == null)
                        throw new Exception("订单不存在");
                    bool IsInventory = true;
                    foreach (S.DistributorOrder order in supplierorder)
                    {
                        foreach (S.DistributorOrderMapping orderMapping in order.GetMapping(DataSource))
                        {
                            S.DistributorProduct product = S.DistributorProduct.GetSaleProduct(DataSource, orderMapping.ProductId);
                            if (product == null)
                            {
                                throw new Exception("支付失败，存在已经下架的商品！");
                            }
                            if (product.Inventory < orderMapping.Count)
                            {
                                IsInventory = false;
                                break;
                            }
                        }
                        if (!IsInventory)
                        {
                            break;
                        }
                    }
                    if (!IsInventory)
                    {
                        throw new Exception("支付失败，存在库存不足的商品！");
                    }
                    string openId = null;
                    M.OAuth2Member member = M.OAuth2Member.GetByUserPay(DataSource, provider, UserId);
                    if (member != null)
                        openId = member.UserId;

                    pr = PayRecord.GetByUser(DataSource, orderId, UserId, PaymentType.Pay, PayStatus.Paying);
                    if (pr == null)
                    {
                        //插入type为5的参数
                        pr = P.ProductOrder.GetPayRecord(provider, orderId, UserId, supplierorder[0].Title, SumSupplierMoney(supplierorder), openId, P.ProductOrder.PayWholesaleType);
                        if (pr.Insert(DataSource) != DataStatus.Success)
                            throw new Exception("插入支付记录失败");
                    }
                    else
                    {
                        pr.Provider = provider;
                        pr.OpenId = openId;
                        pr.Money = SumSupplierMoney(supplierorder);
                        if (pr.Update(DataSource, ColumnMode.Include, "Provider", "OpenId", "Money") != DataStatus.Success)
                            throw new Exception("更新支付记录失败");
                    }
                }
                DataSource.Commit();
                return pr;
            }
            catch (Exception)
            {
                DataSource.Rollback();
                throw;
            }
        }
        protected override bool CheckMoney(IPayOrder order, out PaymentResult result)
        {
            PayRecord pr = PayRecord.GetById(DataSource, order.TradeNo, PaymentType.Pay);
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
        protected override bool OnModifyMoney(PayProvider provider, PaymentType payment, long user, Money money, string trade, string title, int type, string targetId)
        {
            try
            {
                if (payment == PaymentType.Pay)
                {
                    if (provider.IsOnlinePay)
                    {
                        if (M.MemberInfo.ModifyMoney(DataSource, user, money, "充值", type, trade) != DataStatus.Success)
                            throw new Exception();
                    }
                    ///处理产品订单
                    if (type == P.ProductOrder.PayProductType)
                    {
                        bool end = false;
                        DataSource.Begin();                       
                        try
                        {
                            M.MemberInfo member = M.MemberInfo.GetByRecharge(DataSource, user);
                            if (member == null)
                                throw new Exception();

                            IList<P.ProductOrder> orders = CommonBuy.GetOrders(DataSource, targetId, P.OrderState.Payment, user);
                            foreach (P.ProductOrder order in orders)
                            {
                                if (order == null)
                                    throw new Exception();
                                if (user != order.UserId)
                                    throw new Exception();

                                if (provider.IsOnlinePay)
                                {
                                    if (member.Money < order.TotalMoney)
                                        throw new Exception("余额不足");
                                    if (M.MemberInfo.ModifyMoney(DataSource, user, -order.TotalMoney, order.Title, type, targetId) != DataStatus.Success)
                                        throw new Exception("余额不足");
                                    member = M.MemberInfo.GetByRecharge(DataSource, user);
                                }
                                if (member.Money >= 0)
                                {
                                    if (order.UpdateStateByUser(DataSource, P.OrderState.Payment, provider.Key) != DataStatus.Success)
                                        throw new Exception("更新订单失败");
                                }
                                IList<P.ProductOrderMapping> mappings = P.ProductOrderMapping.GetAllByOrder(DataSource, order.Id);
                                foreach (P.ProductOrderMapping mapping in mappings)
                                {
                                    if (P.Product.UpdateInventoryById(DataSource, mapping.ProductId, mapping.Count) != DataStatus.Success)
                                        throw new Exception("更新库存失败");
                                }
                                try
                                {
                                    if (provider.IsOnlinePay || provider.IsCheckMoney)
                                        //插入收益收入记录
                                        A.ProfitRecord.AddProfitByOrder(DataSource, order.Id);
                                }
                                catch (Exception)
                                {

                                }
                            }
                            DataSource.Commit();
                            end = true;
                            try
                            {
                                //发送短信通知
                                if (member.Mobile > 0 && orders.Count > 0)
                                {
                                    List<string> orderIds = new List<string>();
                                    foreach (P.ProductOrder order in orders)
                                        orderIds.Add(order.Id);

                                    SmsMobset.Send(
                                        DataSource,
                                        member.Mobile,
                                        SmsTemplate.MemberPaid,
                                       !string.IsNullOrEmpty(member.RealName) ? member.RealName :
                                       !string.IsNullOrEmpty(member.Name) ? member.Name : User.Identity.Name,
                                        string.Join(",", orderIds));
                                }
                            }
                            catch (Exception) { }
                        }
                        catch (ThreadAbortException)
                        {
                            if (!end)
                                DataSource.Rollback();
                        }
                        catch (Exception)
                        {
                            DataSource.Rollback();
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
                            IList<S.DistributorOrder> orders = CommonBuy.GetSupplierOrders(DataSource, targetId, P.OrderState.Payment, user);
                            foreach (S.DistributorOrder order in orders)
                            {
                                if (order == null)
                                    throw new Exception();
                                if (user != order.UserId)
                                    throw new Exception();

                                M.MemberInfo member = M.MemberInfo.GetByRecharge(DataSource, user);
                                if (member == null)
                                    throw new Exception();
                                if (provider.IsOnlinePay)
                                {
                                    if (member.Money < order.TotalMoney)
                                        throw new Exception("余额不足");
                                    if (M.MemberInfo.ModifyMoney(DataSource, user, -order.TotalMoney, order.Title, type, targetId) != DataStatus.Success)
                                        throw new Exception("余额不足");
                                    member = M.MemberInfo.GetByRecharge(DataSource, user);
                                }
                                if (member.Money >= 0)
                                {
                                    if (order.UpdateStateByUser(DataSource, P.OrderState.Payment, provider.Key) != DataStatus.Success)
                                        throw new Exception("更新订单失败");
                                }
                                IList<S.DistributorOrderMapping> mappings = S.DistributorOrderMapping.GetAllByOrder(DataSource, order.Id);
                                foreach (S.DistributorOrderMapping mapping in mappings)
                                {
                                    if (S.DistributorProduct.UpdateInventoryById(DataSource, mapping.ProductId, mapping.Count) != DataStatus.Success)
                                        throw new Exception("更新库存失败");
                                }
                                try
                                {
                                    try
                                    {
                                        if (provider.IsOnlinePay || provider.IsCheckMoney)
                                            //插入收益收入记录
                                            A.ProfitRecord.AddDistributorProfitByOrder(DataSource, order.Id);
                                    }
                                    catch (Exception)
                                    {

                                    }
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
                        catch (Exception)
                        {
                            DataSource.Rollback();
                        }

                        return true;
                    }
                }
                else
                {
                    if (provider.IsOnlinePay)
                    {
                        if (M.MemberInfo.ModifyMoney(DataSource, user, money, "退款", type, trade) != DataStatus.Success)
                            throw new Exception();
                    }
                }
            }
            catch (Exception) { }

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
                        settlement.ParentId = SaleMember.ParentId;
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
                                if (parentD.Level > 1)
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
                        ///增加省级提成
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
            catch (Exception)
            {
                DataSource.Rollback();
                return DataStatus.Rollback;
            }
        }
        protected override string ReturnUrl
        {
            get
            {
                return GetUrl("/buy/payResult/0");
            }
        }
        protected override void OnError(string message)
        {
            PayResult(0, message,null);
        }

        /// <summary>
        /// 支付结果
        /// </summary>
        /// <param name="state">0失败，1成功</param>
        /// <param name="msg"></param>
        public void PayResult(int state, string msg="", string order="")
        {

            if (!string.IsNullOrEmpty(order) && order != "_")
            {
                if (!string.IsNullOrEmpty(order))
                {
                    PayRecord payrecord = PayRecord.GetById(DataSource, order, PaymentType.Pay);
                    if (payrecord != null)
                    {
                        if (payrecord.Type == 5)
                            this["OrderType"] = 5;
                        else if (payrecord.Type == 1)
                            this["OrderType"] = 1;
                    }
                    else
                        this["OrderType"] = 0;
                }
                else
                {
                    this["OrderType"] = 0;
                }
            }
            else
            {
                this["OrderType"] = 0;
            }
            this["OrderId"] = order;
            this["Result"] = state;
            this["State"] = Convert.ToInt32(state);
            this["Msg"] = msg;
            Render("buy_payresult.html");
        }
        public override void OnRedirect(Controller context, PayProvider pay, PaymentResult result, bool payType)
        {
            if (result.Type == PaymentType.Pay)
            {
                PayResult payresult = (PayResult)result;
                if (payType)
                    context.Redirect(GetUrl("/buy/payResult/1/_/" + payresult.TradeNo));
                else
                    context.Redirect(GetUrl("/buy/payResult/0/_/" + payresult.TradeNo));
            }
            else
            {
                if (payType)
                    context.Redirect(GetUrl("/buy/payResult/1"));
                else
                    context.Redirect(GetUrl("/buy/payResult/0"));
            }
        }

        protected override void OnMakeQR(string url, string orderid = "")
        {
            if (!IsAjax && string.IsNullOrEmpty(Request["token"]))
            {
                string OrderId = orderid;
                this["OrderId"] = OrderId;
                IList<Cnaws.Product.Modules.ProductOrder> orders = CommonBuy.GetOrders(DataSource, OrderId, Cnaws.Product.Modules.OrderState.Payment, User.Identity.Id);
                Money TotalMoney = 0;
                if (orders != null && orders.Count > 0 && orders[0] != null)
                {
                    TotalMoney = CommonBuy.SumMoney(orders);
                }
                else
                {
                    IList<S.DistributorOrder> supplierorder = CommonBuy.GetSupplierOrdersNoState(DataSource, OrderId, User.Identity.Id);
                    if (supplierorder != null && supplierorder.Count > 0 && supplierorder[0] != null)
                    {
                        TotalMoney = CommonBuy.SumSupplierMoney(supplierorder);
                    }
                }

                string path = url;
                Bitmap image = XcpNet.Passport.QRCode.MarkQRCode.Create_ImgCode(path, 10);
                using (MemoryStream ms = new MemoryStream())
                {
                    XcpNet.Passport.QRCode.MarkQRCode.CombinImage(image, Server.MapPath("~/logo.png")).Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    this["QrUrl"] = string.Concat("data:image/png;base64,", Convert.ToBase64String(ms.ToArray()));
                }

                this["Money"] = TotalMoney;
                Render("makeqr.html");
            }
            else
            {
                base.OnMakeQR(url);
            }
        }
        [HttpPost]
        [HttpAjax]
        [Authorize]
        public void GetOrderState(string orderId)
        {
            IList<P.ProductOrder> orders = CommonBuy.GetOrdersNoState(DataSource, orderId, User.Identity.Id);
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
                IList<S.DistributorOrder> supplierorder = CommonBuy.GetSupplierOrdersNoState(DataSource, orderId, User.Identity.Id);
                if (supplierorder != null && supplierorder.Count > 0 && supplierorder[0] != null)
                {
                    foreach (S.DistributorOrder order in supplierorder)
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
                    Cnaws.Pay.Modules.PayRecord payrecord = Cnaws.Pay.Modules.PayRecord.GetByTypeAndStatusAndUserAndId(DataSource, orderId, 0, User.Identity.Id, PayStatus.PaySuccess);
                    if (payrecord == null || payrecord.Status != PayStatus.PaySuccess)
                        state = false;
                }
            }
            SetResult(state);

        }



    }
}
