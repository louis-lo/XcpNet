using System;
using Cnaws.Web;
using Cnaws.Data;
using M = XcpNet.Supplier.Modules.Modules;
using P = Cnaws.Passport.Modules;
using System.Collections.Generic;
using Cnaws;
using System.Web;
using Cnaws.Web.Templates;
using A = XcpNet.AfterSales.Modules;
using Pd = Cnaws.Product.Modules;
using XcpNet.Common;
using L = Cnaws.Product.Logistics;
namespace XcpNet.Supplier.Controllers.Extension
{
    public sealed class Shop : SupplierController
    {

        [Distributor(true)]
        public void Index()
        {
            List();
        }
        [Distributor(true)]
        public void List(int categoryId = 0, long page = 1L, string price = "_")
        {
            string q = Request.QueryString["q"];
            if (string.IsNullOrEmpty(price))
                price = "_";
            if (string.IsNullOrEmpty(q))
                q = "_";
            M.DistributorCategory cate = M.DistributorCategory.GetById(DataSource, categoryId);
            if (cate == null)
                cate = new M.DistributorCategory();
            this["Price"] = "_".Equals(price) ? string.Empty : price;
            this["Title"] = "_".Equals(q) ? string.Empty : q;
            this["Category"] = cate;
            this["ProductList"] = M.DistributorProduct.GetPageByWholesale(DataSource, "_".Equals(q) ? null : q, cate.Id, "_".Equals(price) ? null : price, /*isFirst,*/ Math.Max(1, page), 20, 8);
            this["GetPageUrl"] = new FuncHandler((args) =>
              {
                  return string.Concat(GetUrl("/shop/list/", categoryId.ToString(), "/", Convert.ToInt64(args[0]).ToString(), "/", price), "?q=", HttpUtility.UrlEncode(q));
              });
            Render("shop.html");
        }

        [Distributor(true)]
        public void Product(long id)
        {
            M.DistributorProduct product = M.DistributorProduct.GetSaleProduct(DataSource, id);
            if (product != null)
            {
                long parent = product.ParentId > 0 ? product.ParentId : product.Id;

                this["Product"] = product;
                this["ParentId"] = parent;
                this["Supplier"] = Cnaws.Product.Modules.Supplier.GetById(DataSource, product.SupplierId);
                this["Series"] = M.DistributorSerie.GetAll(DataSource, parent);
                this["Mapping"] = M.DistributorMapping.GetAllByProduct(DataSource, product.Id);
                this["Mappings"] = M.DistributorMapping.GetAllByAllProduct(DataSource, parent);
                this["Attributes"] = M.DistributorAttribute.GetAllValuesByProduct(DataSource, product.Id);
                this["CategoryList"] = M.DistributorCategory.GetAllParentsById(DataSource, product.CategoryId);
                Render("product_info.html");
            }
            else
            {
                NotFound();
            }
        }

        [Distributor(true)]
        public void Cart()
        {
            IList<DataJoin<M.DistributorCart, M.DistributorProduct>> list = M.DistributorCart.GetPageByUser(DataSource, User.Identity.Id);
            Money total = 0;
            foreach (DataJoin<M.DistributorCart, M.DistributorProduct> item in list)
                total += item.B.GetTotalMoney(item.A.Count);
            this["CartList"] = list;
            this["TotalMoney"] = total;
            this["Address"] = P.ShippingAddress.GetAll(DataSource, User.Identity.Id);
            this["HasPayPassword"] = !string.IsNullOrEmpty(P.MemberInfo.GetPayPasswordById(DataSource, User.Identity.Id));
            Render("cart.html");
        }

        [Distributor(true)]
        public void Order(string type = "_", long page = 1L)
        {
            if (string.IsNullOrEmpty(type))
                type = "_";
            this["State"] = type;
            long userId = User.Identity.Id;
            this["PaymentCount"] = M.DistributorOrder.GetCountByState(DataSource, Pd.OrderState.Payment, userId);
            this["DeliveryCount"] = M.DistributorOrder.GetCountByState(DataSource, Pd.OrderState.Delivery, userId);
            this["ReceiptCount"] = M.DistributorOrder.GetCountByState(DataSource, Pd.OrderState.Receipt, userId);
            this["FinishedCount"] = M.DistributorOrder.GetCountByState(DataSource, Pd.OrderState.Finished, userId);
            this["OutWarehouseCount"] = M.DistributorOrder.GetCountByState(DataSource, Pd.OrderState.OutWarehouse, userId);
            if (Request["IsSearch"] == "true")
            {
                string bgDate = Request["bgDate"],
                      endDate = Request["endDate"],
                     txtQuery = Request["txtQuery"],
                   orderState = Request["orderState"];
                this["OrderList"] = M.DistributorOrder.SearchOrder(DataSource, userId, orderState, Math.Max(1, page), 10, 11, txtQuery, bgDate, endDate);

            }
            else
            {
                this["OrderList"] = M.DistributorOrder.GetPageByUserAndState(DataSource, User.Identity.Id, type, Math.Max(1, page), 10, 8);
            }

            Render("order.html");
        }
        [Distributor(true)]
        public void MyOrder(long userid, long page = 1L)
        {
            if (userid <= 0)
                userid = User.Identity.Id;
            this["UserId"] = userid;
            this["OrderList"] = M.DistributorOrder.GetPageByUser(DataSource, userid, Math.Max(1, page), 5, 8);
            this["GetImage"] = new FuncHandler((args) =>
            {
                string imgs = args[0] as string;
                if (!string.IsNullOrEmpty(imgs))
                    return imgs.Split('|')[0];
                return string.Empty;
            });
            Render("myorder.html");
        }
        /// <summary>
        /// 立即支付
        /// </summary>
        /// <param name="id"></param>
        [Distributor(true)]
        public void Payment(string id)
        {
            M.DistributorOrder order = M.DistributorOrder.GetById(DataSource, id);
            this["Order"] = order;
            long count= M.DistributorOrder.GetCount(DataSource, id);
            this["Count"] = count;
            Render("buy_payment.html");
        }

        [Distributor(true)]
        public void Status(string id)
        {
            Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, id);
            M.DistributorOrder neworder = new M.DistributorOrder(); ;
            if (order == null)
                neworder = M.DistributorOrder.GetById(DataSource, id);
            XcpNet.AfterSales.Modules.ReminderDelivery reminderDelivery = XcpNet.AfterSales.Modules.ReminderDelivery.GetReminderDeliveryByOrderIdAndSupplierId(DataSource, id, User.Identity.Id);
            if (order != null)
            {
                this["OrderType"] = "0";
                this["Order"] = order;
            }
            else
            {
                this["OrderType"] = "1";
                this["Order"] = neworder;
            }
            this["ReminderDelivery"] = reminderDelivery;
            Pd.ProductLogistics log;
            if (order != null && order.State > Pd.OrderState.Delivery)
            {
                log = Pd.ProductLogistics.GetByOrder(DataSource, order.Id);
                if (log != null)
                {
                    try
                    {
                    }
                    catch (Exception) { }
                }
                else
                {
                    log = new Pd.ProductLogistics();
                }
            }
            else
            {
                log = new Pd.ProductLogistics();
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
                            Pd.ProductLogistics.UpdateProviderDetailed(DataSource, log);
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
            Render("shop_status.html");
        }
        [SupplierOrDistributor(true)]
        public void SupplierStatus(string id)
        {
            Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, id);
            M.DistributorOrder neworder = new M.DistributorOrder(); ;
            if (order==null)
                neworder = M.DistributorOrder.GetById(DataSource, id);
            XcpNet.AfterSales.Modules.ReminderDelivery reminderDelivery = XcpNet.AfterSales.Modules.ReminderDelivery.GetReminderDeliveryByOrderIdAndSupplierId(DataSource, id, User.Identity.Id);
            if (order != null)
            {
                this["Order"] = order;
                this["OrderType"] = "0";
            }
            else
            {
                this["Order"] = neworder;
                this["OrderType"] = "1";
            }
            this["ReminderDelivery"] = reminderDelivery;
            Pd.ProductLogistics log;
            if (order != null && order.State > Pd.OrderState.Delivery)
            {
                log = Pd.ProductLogistics.GetByOrder(DataSource, order.Id);
                if (log != null)
                {
                    try
                    {
                    }
                    catch (Exception) { }
                }
                else
                {
                    log = new Pd.ProductLogistics();
                }
            }
            else
            {
                log = new Pd.ProductLogistics();
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
                            Pd.ProductLogistics.UpdateProviderDetailed(DataSource, log);
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
            Render("supplier/shop_status.html");
        }

        [SupplierOrDistributor(true)]
        public void XdgStatus(string id)
        {
            Pd.ProductOrder order = Pd.ProductOrder.GetById(DataSource, id);
            XcpNet.AfterSales.Modules.ReminderDelivery reminderDelivery = XcpNet.AfterSales.Modules.ReminderDelivery.GetReminderDeliveryByOrderIdAndSupplierId(DataSource, id, User.Identity.Id);
            this["Order"] = order;
            this["ReminderDelivery"] = reminderDelivery;
            Render("shop_status.html");
        }

        [HttpPost]
        [HttpAjax]
        [Distributor]
        public void Buy()
        {
            object code, data;
            try
            {
                P.Member member = new P.Member { Id = User.Identity.Id };
                Pd.Distributor distributor = Pd.Distributor.GetById(DataSource, member.Id);
                code = CommonBuy.CommSetOrder<M.DistributorOrder>(DataSource, member, Request.Form["Id"], Request.Form["Count"], distributor.Province, distributor.City, distributor.County, out data);
                if ((int)code == -200)
                {
                    code = CommonBuy.CommSetPerfect<M.DistributorOrder>(DataSource, member, data.ToString(), Request.Form["Address"], Request.Form["Message"], out data);
                }
                CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }

        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void AddCart()
        {

            object code, data;
            try
            {
                P.Member member = new P.Member { Id = User.Identity.Id };
                Pd.Distributor distributor = Pd.Distributor.GetById(DataSource, member.Id);
                code = CommonBuy.CommAddToCart<M.DistributorCart>(DataSource, member, Request.Form["Id"], Request.Form["Count"], distributor.Province, distributor.City, distributor.County, out data);
                CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }
        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void DelCart()
        {
            object code, data;
            try
            {
                P.Member member = new P.Member { Id = User.Identity.Id };
                code = CommonBuy.CommDelForCart<M.DistributorCart>(DataSource, member, Request.Form["Id"], out data);
                CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
            //try
            //{
            //    SetResult(M.DistributorCart.Remove(DataSource, long.Parse(Request.Form["id"]), User.Identity.Id));
            //}
            //catch (Exception)
            //{
            //    SetResult(false);
            //}
        }

        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void DelCartAll()
        {
            object code, data;
            try
            {
                P.Member member = new P.Member { Id = User.Identity.Id };
                code = CommonBuy.CommDelForCart<M.DistributorCart>(DataSource, member, Request.Form["Id"], out data);
                CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
            //try
            //{
            //    string idstr = Request.Form["id"];
            //    if (string.IsNullOrEmpty(idstr))
            //        throw new Exception();

            //    string[] idsStr = idstr.Split(',');
            //    SetResult(M.DistributorCart.Remove(DataSource, idsStr));
            //}
            //catch (Exception)
            //{
            //    SetResult(false);
            //}
        }

        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void Del()
        {
            object code, data;
            try
            {
                P.Member member = new P.Member { Id = User.Identity.Id };
                code = CommonBuy.CommDelOrder<M.DistributorOrder>(DataSource, member, Request.Form["Id"], out data);
                CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
            //try
            //{
            //    string id = Request.Form["Id"];
            //    M.DistributorOrder order = M.DistributorOrder.GetByState(DataSource, id, Pd.OrderState.Invalid, User.Identity.Id);
            //    if (order != null)
            //        SetResult(order.Delete(DataSource));
            //}
            //catch (Exception)
            //{
            //    SetResult(false);
            //}
        }
        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void Cancel()
        {
            object code, data;
            try
            {
                P.Member member = new P.Member { Id = User.Identity.Id };
                code = CommonBuy.CommCancelOrder<M.DistributorOrder>(DataSource, member, Request.Form["Id"], out data);
                CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }

        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void Receipt()
        {
            object code, data;
            try
            {
                P.Member member = new P.Member { Id = User.Identity.Id };
                code = CommonBuy.CommSetReceipt<M.DistributorOrder>(DataSource, member, Request.Form["Id"], out data);
                CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
            //try
            //{
            //    SetResult((new M.DistributorOrder() { Id = Request.Form["Id"], UserId = User.Identity.Id }).UpdateStateByUser(DataSource, Pd.OrderState.Receipt));
            //}
            //catch (Exception)
            //{
            //    SetResult(false);
            //}

        }

        protected override void Unauthorized(bool redirect = false)
        {
            if (IsHtml())
            {
                if (User != null && User.Identity != null && User.Identity.IsAuthenticated && !User.Identity.IsAdmin)
                    Response.Write(string.Concat("<script type=\"text/javascript\">window.location.href='http://www.xcpnet.com/channel/joind.html';</script>"));
                else
                    Response.Write(string.Concat("<script type=\"text/javascript\">window.location.href='", GetPassportUrl("/logout"), "?target=", HttpUtility.UrlEncode(string.Concat(GetPassportUrl("/login"), "?target=", Request.Url.ToString())), "';</script>"));
            }
            else
            {
                base.Unauthorized(redirect);
            }
        }
        public DataStatus SetOrderSettlement(M.DistributorOrderMapping ordermapping)
        {
            DataSource.Begin();
            try
            {
                M.DistributorProduct product = M.DistributorProduct.GetById(DataSource, ordermapping.ProductId);
                M.DistributorOrderSettlement settlement = new M.DistributorOrderSettlement
                {
                    OrderId = ordermapping.OrderId,
                    ProductId = ordermapping.ProductId,
                    CostPrice = product.CostPrice,
                    Settlement = product.Settlement,
                    RoyaltyRate = product.RoyaltyRate
                };
                if (product.Wholesale && product.WholesalePrice > 0)
                {
                    settlement.ProductType = Pd.EProductType.Wholesale;
                }
                else if (product.DiscountState == Pd.DiscountState.Activated && product.DiscountBeginTime < DateTime.Now && product.DiscountEndTime > DateTime.Now)
                {
                    settlement.ProductType = Pd.EProductType.GroupBuy;
                }
                else
                {
                    settlement.ProductType = Pd.EProductType.Routine;
                }
                //增加收益快照GetRoyaltyByOrderMapping
                M.DistributorOrder order = M.DistributorOrder.GetById(DataSource, ordermapping.OrderId);
                long ShopId = 0;
                //增加收益快照GetRoyaltyByOrderMapping

                Pd.Distributor distributor = Pd.Distributor.GetById(DataSource, order.UserId);
                if (distributor != null && distributor.UserId != 0)
                {
                    ShopId = order.UserId;
                }
                else
                {
                    P.Member member = P.Member.GetById(DataSource, order.UserId);
                    if (member.ParentId != 0)
                    {
                        distributor = Pd.Distributor.GetById(DataSource, member.ParentId);
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
                        P.Member SaleMember = P.Member.GetById(DataSource, order.UserId);
                        ///增加推荐人提成
                        if (distributor.Level == 2)
                        {
                            settlement.ParentId = SaleMember.ParentId;
                            if (settlement.ParentId != 0)
                            {
                                Pd.Distributor parentD = Pd.Distributor.GetById(DataSource, settlement.ParentId);
                                if (SaleMember.CreationDate.AddYears(3)>= DateTime.Now)///创建三年内有收益
                                    settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
                            }
                        }
                        else
                        {
                            settlement.ParentId = distributor.ParentId;
                            if (settlement.ParentId != 0)
                            {
                                Pd.Distributor parentD = Pd.Distributor.GetById(DataSource, distributor.ParentId);
                                settlement.ParentRoyaltyRate = int.Parse((parentD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
                                if (parentD.Level > 1)
                                    distributor.ParentId = parentD.ParentId;
                            }
                        }

                        ///增加县级提成
                        settlement.CountyUserId = distributor.ParentId;
                        Pd.Distributor CountyD = Pd.Distributor.GetById(DataSource, settlement.CountyUserId);
                        settlement.CountyRoyaltyRate = int.Parse((CountyD.GetRoyaltyByDistributorOrderMapping(order.UserId) * 100).ToString());
                        ///增加省级提成
                        settlement.ProvinceUserId = CountyD.ParentId;
                        Pd.Distributor ProvinceD = Pd.Distributor.GetById(DataSource, settlement.ProvinceUserId);
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

        /// <summary>
        /// 公众返回
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        private void CommSetResult(object code, object data)
        {
            if (data == null)
            {
                if (code.GetType() == typeof(int))
                    SetResult((int)code);
                else if (code.GetType() == typeof(DataStatus))
                    SetResult((DataStatus)code);
                else
                    SetResult(code);
            }
            else
                SetResult((int)code, data);
        }
    }
}
