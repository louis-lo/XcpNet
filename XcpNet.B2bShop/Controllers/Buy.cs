using System;
using System.Collections.Generic;
using System.Threading;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws;
using Cnaws.Pay;
using Cnaws.Area;
using Cnaws.Web.Templates;
using XcpNet.Common;
using XcpNet.Common.Address;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using A = XcpNet.AfterSales.Modules;
using S = XcpNet.Supplier.Modules.Modules;
using Cnaws.Pay.Modules;
using XcpNet.Services.Sms;
using Cnaws.Sms.Modules;

namespace XcpNet.B2bShop.Controllers.Extension
{
    public sealed class Buy : Passport.Controllers.Extension.Recharge
    {
        protected override bool CheckProvider(PayProvider provider)
        {
            return true;
        }
        [Authorize(true)]
        protected override void OnIndex()
        {
            if (IsPost)
            {
                object code, data;
                try
                {
                    var m = M.MemberInfo.GetById(DataSource, User.Identity.Id);
                    P.Distributor distributor = P.Distributor.GetById(DataSource, User.Identity.Id);
                    M.Member member = new M.Member { Id = User.Identity.Id };
                    string Id = string.Join(",", Request.Form["Id"]);
                    string Count = string.Join(",", Request.Form["Count"]);
                    code = CommonBuy.CommSetOrder<S.DistributorOrder>(DataSource, member, Id, Count, Location.ProvinceId, Location.CityId, Location.CountyId, out data);
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

        [Authorize(true)]
        public void Perfect(string id, long addressId = 0)
        {
            IList<S.DistributorOrder> orders = GetOrders(id, P.OrderState.Perfect, User.Identity.Id);
            if (orders[0] == null || orders.Count <= 0)
                orders = GetOrders(id, P.OrderState.Payment, User.Identity.Id);
            if (orders[0] == null || orders.Count <= 0)
            {
                NotFound();
                return;
            }
            P.Distributor distributor = P.Distributor.GetById(DataSource, User.Identity.Id);
            M.ShippingAddress defaultAdrs = new M.ShippingAddress()
            {
                Province = distributor.Province,
                City = distributor.City,
                County = distributor.County,
                Address = distributor.Address,
                Mobile = distributor.Mobile,
                Consignee = distributor.Consignee,
                IsDefault = true,
                UserId = distributor.UserId,
                Id = 1
            };

            this["DefaultAdrs"] = defaultAdrs;
            this["Member"] = M.MemberInfo.GetByRecharge(DataSource, User.Identity.Id);
            this["OrderList"] = orders;
            this["TotalMoney"] = SumMoney(orders);
            this["AddressList"] = new List<M.ShippingAddress>() { defaultAdrs };
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
                code = CommonBuy.CommSetPerfect<S.DistributorOrder>(DataSource, member, Request["Id"], Request["Address"], Request["Message"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }

        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        public void Freight()
        {
            try
            {
                string orderId = Request.Form["Order"];
                IList<S.DistributorOrder> orders = GetOrders(orderId, P.OrderState.Perfect, User.Identity.Id);
                if (orders[0] == null || orders.Count <= 0)
                    orders = GetOrders(orderId, P.OrderState.Payment, User.Identity.Id);
                M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, long.Parse(Request.Form["Address"]), User.Identity.Id);
                Money FreightMoney = 0;
                foreach (S.DistributorOrder item in orders)
                    FreightMoney += item.GetFreight(DataSource,address.Province, address.City);
                SetResult(true, FreightMoney.ToString("C2"));
            }
            catch (Exception)
            {
                SetResult(CommUtility.PROGRAM_ERROR);
            }
        }

        private IList<S.DistributorOrder> GetOrders(string id, P.OrderState state, long userId)
        {
            if (id[0] == 'G')
                return S.DistributorOrder.GetListByState(DataSource, id, state, userId);
            return new S.DistributorOrder[] { S.DistributorOrder.GetByState(DataSource, id, state, userId) };
        }
        private IList<S.DistributorOrder> GetSupplierOrders(string id, P.OrderState state, long userId)
        {
            if (id[0] == 'G')
                return S.DistributorOrder.GetListByState(DataSource, id, state, userId);
            return new S.DistributorOrder[] { S.DistributorOrder.GetByState(DataSource, id, state, userId) };
        }
        private new Money SumMoney(IList<S.DistributorOrder> orders)
        {
            Money money = 0;
            foreach (S.DistributorOrder order in orders)
                money += order.TotalMoney;
            return money;
        }
        private new Money SumSupplierMoney(IList<S.DistributorOrder> orders)
        {
            Money money = 0;
            foreach (S.DistributorOrder order in orders)
                money += order.TotalMoney;
            return money;
        }
        [Authorize(true)]
        public void Payment(string id)
        {
            IList<S.DistributorOrder> orders = GetOrders(id, P.OrderState.Payment, User.Identity.Id);
            if (orders != null && orders.Count > 0 && orders[0] != null)
            {
                this["OrderList"] = orders;
                this["TotalMoney"] = SumMoney(orders);
            }
            else
            {
                //处理进货订单
                IList<S.DistributorOrder> supplierorder = GetSupplierOrders(id, P.OrderState.Payment, User.Identity.Id);
                if (supplierorder == null || supplierorder.Count <= 0 || supplierorder[0] == null)
                    throw new Exception("订单不存在");
                this["OrderList"] = supplierorder;
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
            S.DistributorOrder order = S.DistributorOrder.GetByUser(DataSource, id, User.Identity.Id);
            this["Order"] = order;
            Render(string.Concat("refund", step, ".html"));
        }

        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        public void Del()
        {
            object code, data;
            try
            {
                M.Member member = new M.Member { Id = User.Identity.Id };
                code = CommonBuy.CommDelOrder<S.DistributorOrder>(DataSource, member, Request.Form["Id"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }
        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        public void Cancel()
        {
            object code, data;
            try
            {
                M.Member member = new M.Member { Id = User.Identity.Id };
                code = CommonBuy.CommCancelOrder<S.DistributorOrder>(DataSource, member, Request.Form["Id"], out data);
                new CommUtility(this).CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }

        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        public void Receipt()
        {
            object code, data;
            try
            {
                M.Member member = new M.Member { Id = User.Identity.Id };
                code = CommonBuy.CommSetReceipt<S.DistributorOrder>(DataSource, member, Request.Form["Id"], out data);
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
            S.DistributorOrder order = S.DistributorOrder.GetByUser(DataSource, id, User.Identity.Id);
            IList<S.DistributorOrderMapping> orderMapping = S.DistributorOrderMapping.GetAllByOrder(DataSource, id);
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
            Render("buy_status.html");
        }

        [Authorize(true)]
        public void Logistics(string id)
        {
            P.ProductLogistics log;
            S.DistributorOrder order = S.DistributorOrder.GetById(DataSource, id);
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
            }
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
                PayRecord pr;

                //处理进货订单
                IList<S.DistributorOrder> supplierorder = GetSupplierOrders(orderId, P.OrderState.Payment, UserId);
                if (supplierorder == null || supplierorder.Count <= 0 || supplierorder[0] == null)
                    throw new Exception("订单不存在");
                bool IsInventory = true;
                foreach (S.DistributorOrder order in supplierorder)
                {
                    foreach (S.DistributorOrderMapping orderMapping in order.GetMapping(DataSource))
                    {
                        S.DistributorProduct product = S.DistributorProduct.GetById(DataSource, orderMapping.ProductId);
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
                    pr = S.DistributorOrder.GetPayRecord(provider, orderId, UserId, supplierorder[0].Title, SumSupplierMoney(supplierorder), openId, P.ProductOrder.PayWholesaleType);
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
            int type = S.DistributorOrder.PayProductType;
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
                    ///处理进货订单
                    if (type == P.ProductOrder.PayWholesaleType)
                    {
                        bool end = false;
                        DataSource.Begin();
                        try
                        {
                            IList<S.DistributorOrder> orders = GetSupplierOrders(targetId, P.OrderState.Payment, user);
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
                                //if (member.ParentId > 0)
                                //{
                                //    try
                                //    {
                                //        //插入加盟商收益记录
                                //        //A.ProfitRecord.AddDistributorProfitByOrder(DataSource, order.Id, member);
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
        public DataStatus SetOrderSettlement(S.DistributorOrderMapping ordermapping)
        {
            DataSource.Begin();
            try
            {
                S.DistributorProduct product = S.DistributorProduct.GetById(DataSource, ordermapping.ProductId);
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
                S.DistributorOrder order = S.DistributorOrder.GetById(DataSource, ordermapping.OrderId);
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
                return GetUrl("/buy/payResult/1");
            }
        }
        protected override void OnError(string message)
        {
            PayResult(0, message);
        }
        /// <summary>
        /// 支付结果
        /// </summary>
        /// <param name="state">0失败，1成功</param>
        /// <param name="msg"></param>
        public void PayResult(int state, string msg)
        {
            this["State"] = state;
            this["Msg"] = msg;
            Render("buy_payresult.html");
        }

        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        public void SetAddress()
        {
            try
            {
                P.Distributor value = P.Distributor.GetById(DataSource, User.Identity.Id);
                if (value != null)
                {
                    value = DbTable.Load<P.Distributor>(Request.Form);
                    value.UserId = User.Identity.Id;
                    SetResult(value.UpdateAddressWithState(DataSource));
                }
                else
                {
                    SetResult(CommUtility.DISTRIBUTOR_NOTIS);
                }
            }
            catch (Exception ex)
            {
                SetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.ToString() });
            }
        }
    }
}
