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
    public sealed class LedBuy : CommBuy
    {
        protected override void OnInitController()
        {
        }

        [HttpPost]
        public new void SetOrder()
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
                    P.Distributor distributor = A.MachineCode.GetDistributorByCode(DataSource, member.Mark);
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

        public void ToPayByOrder(string orderId)
        {
            M.Member member;
            if (CheckMember(out member))
            {
                Cnaws.Web.PassportAuthentication.SetAuthCookie(true, false, member);
                //string PostUrl=GetPassportUrl("/buy/submit/alipayqr");
                string PostUrl = "http://wappass.xcpnet.com/buy/submit/alipayqr.html";
                //string PostUrl = "http://localhost:1879/buy/submit/alipayqr.html";
                Response.Clear();
                Response.Write("<html><head>");
                Response.Write(string.Format("</head><body onload=\"document.{0}.submit()\">", "payform"));
                Response.Write(string.Format("<form name=\"{0}\" method=\"{1}\" action=\"{2}\" >", "payform", "POST", PostUrl));
                Response.Write(string.Format("<input name=\"{0}\" type=\"hidden\" value=\"{1}\">", "Id", orderId));
                Response.Write("</form>");
                Response.Write("</body></html>");
                Response.End();
            }
        }
#if (DEBUG)
        public static void ToPayByOrderHelper()
        {
            CheckMemberHelper(ClassName, "ToPayByOrder/{订单号}", "支付地址,直接访问打开")
                .AddResult(true, typeof(string), "直接跳转支付页面");
        }
#endif
    }
}
