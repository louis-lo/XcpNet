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
    public sealed class LedDistributor : CommDistributor
    {
        protected override void OnInitController()
        {
        }
        //[HttpPost]
        //public new void SetOrder()
        //{
        //    M.Member member;
        //    if (CheckMember(out member))
        //    {

        //        try
        //        {
        //            int count;
        //            D.DistributorProduct p;
        //            D.DistributorOrderMapping pom;
        //            DateTime now = DateTime.Now;
        //            string temp = Request.Form["Id"];
        //            if (string.IsNullOrEmpty(temp))
        //            {
        //                SetResult(ApiUtility.PRODUCT_EMPTY);
        //                throw new AggregateException();
        //            }
        //            string[] ids = temp.Split(',');

        //            temp = Request.Form["Count"];
        //            if (string.IsNullOrEmpty(temp))
        //            {
        //                SetResult(ApiUtility.PRODUCT_SUM_EMPTY);
        //                throw new AggregateException();
        //            }
        //            string[] counts = Request.Form["Count"].Split(',');

        //            if (ids.Length == 0 || ids.Length != counts.Length)
        //            {
        //                SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH);
        //                throw new AggregateException();
        //            }

        //            List<D.DistributorOrderMapping> ps;
        //            KeyValuePair<string, List<D.DistributorOrderMapping>> pair;
        //            Dictionary<long, Money> money = new Dictionary<long, Money>();
        //            Dictionary<long, KeyValuePair<string, List<D.DistributorOrderMapping>>> OrderForSupplier = new Dictionary<long, KeyValuePair<string, List<D.DistributorOrderMapping>>>();
        //            for (int i = 0; i < ids.Length; ++i)
        //            {
        //                count = int.Parse(counts[i]);
        //                if (count <= 0)
        //                {
        //                    SetResult(ApiUtility.PRODUCT_SUM_ERROR);
        //                    throw new AggregateException();
        //                }
        //                p = D.DistributorProduct.GetSaleProduct(DataSource, long.Parse(ids[i]));
        //                if (p == null)
        //                {
        //                    SetResult(ApiUtility.PRODUCT_ERROR, ids[i]);
        //                    throw new AggregateException();
        //                }

        //                if (p.Inventory < count)
        //                {
        //                    SetResult(ApiUtility.PRODUCT_INVENTORY_ENOUGH, ids[i]);
        //                    throw new AggregateException();
        //                }

        //                if (OrderForSupplier.TryGetValue(p.SupplierId, out pair))
        //                {
        //                    pom = new D.DistributorOrderMapping(DataSource, pair.Key, p, count);
        //                    money[p.SupplierId] = money[p.SupplierId] + pom.TotalMoney;
        //                    pair.Value.Add(pom);
        //                }
        //                else
        //                {
        //                    pom = new D.DistributorOrderMapping(DataSource, P.ProductOrder.NewId(now, member.Id, i + 1), p, count);
        //                    money[p.SupplierId] = pom.TotalMoney;
        //                    ps = new List<D.DistributorOrderMapping>();
        //                    ps.Add(pom);
        //                    OrderForSupplier.Add(p.SupplierId, new KeyValuePair<string, List<D.DistributorOrderMapping>>(pom.OrderId, ps));
        //                }
        //            }

        //            string orderId = (OrderForSupplier.Count > 1) ? string.Concat('G', D.DistributorOrder.NewId(now, member.Id)) : null;

        //            long shopId = 0;

        //            P.Distributor distributor = A.MachineCode.GetDistributorByCode(DataSource, member.Mark);
        //            if (distributor == null)
        //            {
        //                SetResult(ApiUtility.DISTRIBUTOR_EMPTY);
        //                throw new AggregateException();
        //            }
        //            shopId = distributor.UserId;

        //            long CurrentSupplie = 0L;
        //            DataSource.Begin();
        //            try
        //            {
        //                foreach (KeyValuePair<long, KeyValuePair<string, List<D.DistributorOrderMapping>>> item in OrderForSupplier)
        //                {
        //                    CurrentSupplie = item.Key;
        //                    D.DistributorOrder order = new D.DistributorOrder()
        //                    {
        //                        Id = item.Value.Key,
        //                        ParentId = orderId,
        //                        SupplierId = item.Key,
        //                        ShopId = shopId,
        //                        UserId = member.Id,
        //                        Title = "购买产品",
        //                        State = P.OrderState.Perfect,
        //                        TotalMoney = money[item.Key],
        //                        FreightMoney = 0,
        //                        Address = null,
        //                        Message = null,
        //                        CreationDate = now
        //                    };

        //                    if (order.Insert(DataSource) != DataStatus.Success)
        //                    {
        //                        SetResult(ApiUtility.ORDER_ADDERROT);
        //                        throw new AggregateException();
        //                    }

        //                    foreach (D.DistributorOrderMapping pm in item.Value.Value)
        //                    {
        //                        if (pm.Insert(DataSource) != DataStatus.Success)
        //                        {
        //                            SetResult(ApiUtility.ORDER_INFO_ADDERROT);
        //                            throw new AggregateException();
        //                        }
        //                        else
        //                        {
        //                            if (SetOrderSettlement(pm) != DataStatus.Success)
        //                            {
        //                                SetResult(ApiUtility.ORDER_SETTLEMENT_ADDERROR);
        //                                throw new AggregateException();
        //                            }
        //                        }
        //                    }

                           

        //                }
        //                DataSource.Commit();
        //            }
        //            catch (AggregateException)
        //            {
        //                DataSource.Rollback();
        //                return;
        //            }
        //            catch (Exception)
        //            {
        //                DataSource.Rollback();
        //                SetResult(false);
        //                return;
        //            }

        //            string NewOrder = orderId ?? OrderForSupplier[CurrentSupplie].Key;

        //            SetResult(true, new { OrderId = NewOrder });
        //        }
        //        catch (AggregateException)
        //        {
        //            DataSource.Rollback();
        //            return;
        //        }
        //        catch (Exception)
        //        {
        //            DataSource.Rollback();
        //            SetResult(false);
        //            return;
        //        }
        //    }
        //}
    }
}
