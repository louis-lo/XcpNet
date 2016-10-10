using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws.Templates;
using Cnaws.Json;
using P = Cnaws.Product.Modules;
using Cnaws;
using System.Linq;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    //[DataTable("ProductOrder")]
    public sealed class DistributorOrder : P.ProductOrder
    {
        public const int PayType = 1;

        public new IList<DistributorOrderMapping> GetMapping(DataSource ds)
        {
            return DistributorOrderMapping.GetAllByOrder(ds, Id);
        }

        public static new DistributorOrder GetById(DataSource ds, string id)
        {
            return Db<DistributorOrder>.Query(ds)
                .Select()
                .Where(W("Id", id))
                .First<DistributorOrder>();
        }
        public static long  GetCount(DataSource ds, string id)
        {
            DbWhereQueue dw = W("OrderId", id) ;           
            return Db<DistributorOrderMapping>.Query(ds)
                    .Select()
                    .Where(dw)
                    .Count();
        }
        public new static long GetCountByState(DataSource ds, Cnaws.Product.Modules.OrderState state, long userId)
        {
            return Db<DistributorOrderMapping>.Query(ds)
                    .Select()
                    .Where(W("OrderId").InSelect<DistributorOrder>("Id").Where(W("State", state) & W("UserId", userId)).Result())
                    .Count();
        }
        public static new DistributorOrder GetByState(DataSource ds, string id, Cnaws.Product.Modules.OrderState state, long userId)
        {
            return DataQuery
                .Select<DistributorOrder>(ds)
                .Where(P("State", state) & P("UserId", userId) & P("Id", id))
                .First<DistributorOrder>();
        }

        public static new DistributorOrder GetByUser(DataSource ds, string id, long userId, int channel = 2)
        {
            return DataQuery
                .Select<DistributorOrder>(ds)
                .Where(P("UserId", userId) & P("Id", id) /*& P("Channel", channel)*/)
                .First<DistributorOrder>();
        }

        public static new dynamic GetAllProductInfo(DataSource ds, long userId, int channel = 2)
        {
            long all = DataQuery.Select<DistributorOrder>(ds)
                .Where(P("UserId", userId) /*& P("Channel", channel)*/)
                .Count();
            long payment = DataQuery.Select<DistributorOrder>(ds)
                .Where((WN("State", Cnaws.Product.Modules.OrderState.Payment, "Payment") | WN("State", Cnaws.Product.Modules.OrderState.Perfect, "Perfect")) & P("UserId", userId) & P("Channel", channel))
                .Count();
            long dlivery = DataQuery.Select<DistributorOrder>(ds)
                .Where(P("State", Cnaws.Product.Modules.OrderState.Delivery) & P("UserId", userId) /*& P("Channel", channel)*/)
                .Count();
            long receipt = DataQuery.Select<DistributorOrder>(ds)
                .Where(P("State", Cnaws.Product.Modules.OrderState.Receipt) & P("UserId", userId) /*& P("Channel", channel)*/)
                .Count();
            //long evaluation = DataQuery.Select<DistributorOrder>(ds)
            //    .Where(P("State", Cnaws.Product.Modules.OrderState.Evaluation) & P("UserId", userId))
            //    .Count();
            return new
            {
                All = all,
                Payment = payment,
                Delivery = dlivery,
                Receipt = receipt,
                //Evaluation = evaluation
            };
        }

        public static new SplitPageData<DataJoin<DistributorOrder, P.Supplier>> GetPage(DataSource ds, int state, string orderid, long index, int size, int show, int channel = 2)
        {
            long count;
            IList<DataJoin<DistributorOrder, P.Supplier>> list;
            DbWhereQueue where = null;
            DbOrderBy[] order = new DbOrderBy[1];
            switch (state)
            {
                case -1:
                    where = new DbWhereQueue();
                    order[0] = D<DistributorOrder>("Id");
                    break;
                case (int)Cnaws.Product.Modules.OrderState.Delivery:
                    where = W<DistributorOrder>("State", state);
                    order[0] = D<DistributorOrder>("PaymentDate");
                    break;
                case (int)Cnaws.Product.Modules.OrderState.Receipt:
                    where = W<DistributorOrder>("State", state);
                    order[0] = D<DistributorOrder>("DeliveryDate");
                    break;
                //case (int)Cnaws.Product.Modules.OrderState.Evaluation:
                case (int)Cnaws.Product.Modules.OrderState.Finished:
                    where = W<DistributorOrder>("State", state);
                    order[0] = D<DistributorOrder>("ReceiptDate");
                    break;
                case (int)Cnaws.Product.Modules.OrderState.Invalid:
                case (int)Cnaws.Product.Modules.OrderState.Perfect:
                case (int)Cnaws.Product.Modules.OrderState.Payment:
                    where = W<DistributorOrder>("State", state);
                    order[0] = D<DistributorOrder>("CreationDate");
                    break;
                default:
                    throw new Exception();
            }
            if (state != -1 && !string.IsNullOrEmpty(orderid))
            {
                where &= W<DistributorOrder>("Id", orderid);
            }
            else if (!string.IsNullOrEmpty(orderid))
            {
                where = W<DistributorOrder>("Id", orderid);
            }
            //where &= W<DistributorOrder>("Channel", channel);
            list = Db<DistributorOrder>.Query(ds)
                .Select(S<DistributorOrder>(), S<P.Supplier>())
                .LeftJoin(O<DistributorOrder>("SupplierId"), O<P.Supplier>("UserId"))
                .Where(where)
                .OrderBy(order)
                .ToList<DataJoin<DistributorOrder, P.Supplier>>(size, index, out count);
            return new SplitPageData<DataJoin<DistributorOrder, P.Supplier>>(index, size, list, count, show);
        }

        public static new SplitPageData<DistributorOrder> GetPageByUser(DataSource ds, long userId, long index, int size, int show, int channel = 2)
        {
            long count;
            IList<DistributorOrder> list = DataQuery
                .Select<DistributorOrder>(ds)
                .Where(P("UserId", userId) /*& P("Channel", channel)*/)
                .OrderBy(Od("CreationDate"))
                .Limit(size, index)
                .ToList<DistributorOrder>(out count);
            return new SplitPageData<DistributorOrder>(index, size, list, count, show);
        }
        public new Money GetTotalMoney(DataSource ds)
        {
            return Db<DistributorOrderMapping>.Query(ds).Select(S_SUM("TotalMoney")).Where(W("OrderId", Id)).First<DistributorOrderMapping>().TotalMoney;
        }
        public static new SplitPageData<DistributorOrder> GetPageByUserAndState(DataSource ds, long userId, string state, long index, int size, int show)
        {
            long count;
            DataWhereQueue dw = P("UserId", userId);
            if (!string.IsNullOrEmpty(state) && !"_".Equals(state))
            {
                Cnaws.Product.Modules.OrderState os = (Cnaws.Product.Modules.OrderState)Enum.Parse(TType<Cnaws.Product.Modules.OrderState>.Type, state, true);
                if (os == Cnaws.Product.Modules.OrderState.Payment)
                    dw &= (WN("State", Cnaws.Product.Modules.OrderState.Payment, "Payment"));
                else
                    dw &= P("State", os);
            }
            else
            {
                dw &= WF("State", Cnaws.Product.Modules.OrderState.Perfect, "{0} <> {1}");
            }
            IList<DistributorOrder> list = DataQuery
                .Select<DistributorOrder>(ds)
                .Where(dw)
                .OrderBy(Od("CreationDate"))
                .Limit(size, index)
                .ToList<DistributorOrder>(out count);
            return new SplitPageData<DistributorOrder>(index, size, list, count, show);
        }

        public static new SplitPageData<dynamic> GetAjaxPageByUserAndState(DataSource ds, long userId, string state, long index, int size, int show, int channel = 2)
        {
            long count;
            DataWhereQueue dw = P("UserId", userId) /*& P("Channel", channel)*/;
            if (!string.IsNullOrEmpty(state) && !"_".Equals(state))
            {
                Cnaws.Product.Modules.OrderState os = (Cnaws.Product.Modules.OrderState)Enum.Parse(TType<Cnaws.Product.Modules.OrderState>.Type, state, true);
                if (os == Cnaws.Product.Modules.OrderState.Payment)
                    dw &= (WN("State", Cnaws.Product.Modules.OrderState.Payment, "Payment") | WN("State", Cnaws.Product.Modules.OrderState.Perfect, "Perfect"));
                else
                    dw &= P("State", os);
            }
            IList<DistributorOrder> list = DataQuery
                .Select<DistributorOrder>(ds)
                .Where(dw)
                .OrderBy(Od("CreationDate"))
                .Limit(size, index)
                .ToList<DistributorOrder>(out count);
            List<dynamic> temp;
            DistributorProductCacheInfo pci;
            IList<DistributorOrderMapping> maps;
            List<dynamic> result = new List<dynamic>(list.Count);
            foreach (DistributorOrder item in list)
            {
                maps = item.GetMapping(ds);
                temp = new List<dynamic>(maps.Count);
                foreach (DistributorOrderMapping it in maps)
                {
                    pci = JsonValue.Deserialize<DistributorProductCacheInfo>(it.ProductInfo);
                    temp.Add(new
                    {
                        Image = it.GetImage(pci.Image),
                        ProductInfo = pci,
                        Price = it.Price.ToString("C2"),
                        Count = it.Count
                    });
                }
                result.Add(new
                {
                    Id = item.Id,
                    State = (int)item.State,
                    StateInfo = item.GetStateInfo(),
                    Mappings = temp,
                    FreightMoney = item.FreightMoney.ToString("C2"),
                    TotalMoney = item.TotalMoney.ToString("C2")
                });
            }
            return new SplitPageData<dynamic>(index, size, result, count, show);
        }
        public static new SplitPageData<DistributorOrder> GetPageBySupplier(DataSource ds, long userId, int state, string title, string nickName, string startDate, string endDate, long index, int size, int show, int channel = 2)
        {
            long count;
            DbWhereQueue where;
            if (state == (int)Cnaws.Product.Modules.OrderState.Delivery)
                where = W("State", Cnaws.Product.Modules.OrderState.Delivery) | W("State", Cnaws.Product.Modules.OrderState.OutWarehouse);
            else if (state > -1)
                where = W("State", state);
            else
                where = (W("State", Cnaws.Product.Modules.OrderState.Payment) | W("State", Cnaws.Product.Modules.OrderState.Receipt)/* | W("State", Cnaws.Product.Modules.OrderState.Evaluation)*/);
            where &= W("SupplierId", userId) /*& W("Channel", channel)*/;

            if (!string.IsNullOrEmpty(title))
            {
                where &= (W("Id", title, DbWhereType.Like) | W("Id").InSelect<DistributorOrderMapping>(S("OrderId")).Where(W("ProductTitle", title, DbWhereType.Like)).Result());
            }

            if (!string.IsNullOrEmpty((string)nickName))
            {
                where &= W("Address", nickName, DbWhereType.Like);
            }
            string DateType = "CreationDate";
            if (state == (int)Cnaws.Product.Modules.OrderState.Delivery || state == (int)Cnaws.Product.Modules.OrderState.OutWarehouse)
                DateType = "PaymentDate";
            else if (state == (int)Cnaws.Product.Modules.OrderState.Finished)
                DateType = "ReceiptDate";
            else if (state == (int)Cnaws.Product.Modules.OrderState.Receipt)
                DateType = "DeliveryDate";

            DateTime begindt = new DateTime(), endtime = new DateTime();
            if (DateTime.TryParse((string)startDate, out begindt) && DateTime.TryParse((string)endDate, out endtime))
            {
                where &= (W(DateType, begindt, DbWhereType.GreaterThan) & W(DateType, endtime, DbWhereType.LessThan));
            }
            else if (DateTime.TryParse((string)startDate, out begindt))
            {
                where &= W(DateType, begindt, DbWhereType.GreaterThan);
            }
            else if (DateTime.TryParse((string)endDate, out endtime))
            {
                where &= W(DateType, endtime, DbWhereType.LessThan);
            }

            IList<DistributorOrder> list = Db<DistributorOrder>.Query(ds)
                .Select()
                .Where(where)
                .OrderBy(D("Id"))
                .ToList<DistributorOrder>(size, index, out count);
            return new SplitPageData<DistributorOrder>(index, size, list, count, show);
        }
        public static new SplitPageData<DistributorOrder> GetPageByDistributor(DataSource ds, long userId, int state, string title, string nickName, string startDate, string endDate, long index, int size, int show, int channel = 2)
        {
            long count;
            DbWhereQueue where;
            if (state > -1)
                where = W("State", state);
            else
                where = (W("State", Cnaws.Product.Modules.OrderState.Payment) | W("State", Cnaws.Product.Modules.OrderState.Receipt)/* | W("State", Cnaws.Product.Modules.OrderState.Evaluation)*/);
            where &= (W("SupplierId").InSelect<Cnaws.Product.Modules.Supplier>(S("UserId")).Where(W("Subjection", userId)).Result());

            if (!string.IsNullOrEmpty(title))
            {
                where &= (W("Id", title, DbWhereType.Like) | W("Id").InSelect<DistributorOrderMapping>(S("OrderId")).Where(W("ProductTitle", title, DbWhereType.Like)).Result());
            }

            if (!string.IsNullOrEmpty((string)nickName))
            {
                where &= W("AddressId", nickName, DbWhereType.Like);
            }
            string DateType = "CreationDate";
            if (state == (int)Cnaws.Product.Modules.OrderState.Delivery || state == (int)Cnaws.Product.Modules.OrderState.OutWarehouse)
                DateType = "PaymentDate";
            else if (state == (int)Cnaws.Product.Modules.OrderState.Finished)
                DateType = "ReceiptDate";
            else if (state == (int)Cnaws.Product.Modules.OrderState.Receipt)
                DateType = "DeliveryDate";

            DateTime begindt = new DateTime(), endtime = new DateTime();
            if (DateTime.TryParse((string)startDate, out begindt) && DateTime.TryParse((string)endDate, out endtime))
            {
                where &= (W(DateType, begindt, DbWhereType.GreaterThan) & W(DateType, endtime, DbWhereType.LessThan));
            }
            else if (DateTime.TryParse((string)startDate, out begindt))
            {
                where &= W(DateType, begindt, DbWhereType.GreaterThan);
            }
            else if (DateTime.TryParse((string)endDate, out endtime))
            {
                where &= W(DateType, endtime, DbWhereType.LessThan);
            }

            IList<DistributorOrder> list = Db<DistributorOrder>.Query(ds)
                .Select()
                .Where(where)
                .OrderBy(D("Id"))
                .ToList<DistributorOrder>(size, index, out count);
            return new SplitPageData<DistributorOrder>(index, size, list, count, show);
        }
        //public static bool IsFirst(DataSource ds, long userId)
        //{
        //    return Db<DistributorOrder>.Query(ds)
        //        .Select()
        //        .Where(W("State", Cnaws.Product.Modules.OrderState.Payment, DbWhereType.GreaterThan) & W("UserId", userId))
        //        .Count() == 0;
        //}

        /// <summary>
        /// 查询运费
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="orderId"></param>
        /// <param name="provice"></param>
        /// <param name="city"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        public new Money GetFreight(DataSource ds, int provice, int city)
        {
            Money money = 0;
            bool IsActivityFree = false;
            Cnaws.Product.Modules.Supplier supplier = Cnaws.Product.Modules.Supplier.GetById(ds, SupplierId);
            if (supplier.IsActivityFree)
            {
                if (TotalMoney > supplier.ActivityCondition)
                {
                    money = supplier.ActivityFree;
                    IsActivityFree = true;
                }
            }
            if (!IsActivityFree)
            {
                IList<DistributorOrderMapping> distributorordermappings = DistributorOrderMapping.GetAllByOrder(ds, Id);
                IList<DistributorProduct> ps = Db<DistributorProduct>.Query(ds)
                    .Select()
                    .Where(W("Id", distributorordermappings.Select(x => x.ProductId).ToArray(), DbWhereType.In))
                    .ToList<DistributorProduct>();

                long[] template = ps.Where(w => w.FreightType == Cnaws.Product.Modules.FreightType.Template).GroupBy(x => x.FreightTemplate, a => a.FreightTemplate).Select(s => s.Key).ToArray();

                int Number = distributorordermappings.Select(s => s.Count).Sum();
                int Volume = 0, Weight = 0;
                foreach (DistributorOrderMapping mapping in distributorordermappings)
                {
                    DistributorProduct product = ps.Where(x => x.Id == mapping.ProductId).First();
                    Volume += (product.Volume * mapping.Count);
                    Weight += (product.Weight * mapping.Count);
                }
                if (template.Length > 0)
                {
                    for (int i = 0; i < template.Length; i++)
                    {
                        if (i == 0)
                            money = Cnaws.Product.Modules.FreightTemplate.GetFreight(ds, template[i], provice, city, 0, TotalMoney, Number, Volume, Weight);
                        else
                            money = new Money(Math.Max(money, Cnaws.Product.Modules.FreightTemplate.GetFreight(ds, template[i], provice, city, 0, TotalMoney, Number, Volume, Weight)));
                    }
                }
                else
                {
                    for (int i = 0; i < ps.Count(); i++)
                    {
                        if (i == 0)
                            money = ps[i].FreightMoney;
                        else
                            money = new Money(Math.Max(money, ps[i].FreightMoney));
                    }
                }
            }
            return money;
        }
        public static new IList<DistributorOrder> GetListByParentid(DataSource ds, string parentid, long userId)
        {
            return DataQuery
                .Select<DistributorOrder>(ds)
                .Where(P("UserId", userId) & P("ParentId", parentid))
                .ToList<DistributorOrder>();
        }
        public static new IList<DistributorOrder> GetListByState(DataSource ds, string parentid, P.OrderState state, long userId)
        {
            return DataQuery
                .Select<DistributorOrder>(ds)
                .Where(P("State", state) & P("UserId", userId) & P("ParentId", parentid))
                .ToList<DistributorOrder>();
        }
        public long GetCount(DataSource ds)
        {
            DbWhereQueue dw = W("OrderId", Id);
            return Db<DistributorOrderMapping>.Query(ds)
                    .Select()
                    .Where(dw)
                    .Count();
        }
        public static new long GetMyCountByState(DataSource ds, P.OrderState state, long userId, int channel = 2)
        {
            DbWhereQueue dw = W("UserId", userId) /*& W("Channel", channel)*/;
            if (state == Cnaws.Product.Modules.OrderState.Payment)
                dw &= W("State", Cnaws.Product.Modules.OrderState.Payment);
            else
                dw &= W("State", state);
            return Db<DistributorOrder>.Query(ds)
                    .Select()
                    .Where(dw)
                    .Count();
        }


        public static DataStatus ModOrder(DataSource ds, DistributorOrder order)
        {
            return order.Update(ds);
        }
        public static DataStatus DelOrder(DataSource ds, string orderid)
        {
            if (Db<DistributorOrder>.Query(ds).Delete().Where(W("Id", orderid)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }

        public new DataStatus UpdateFreightBySupplier(DataSource ds)
        {
            //修改邮费并修改订单号
            ds.Begin();
            try
            {
                if (Update(ds, ColumnMode.Include, Cs(MODC("FreightMoney", -FreightMoney), MODC("TotalMoney", -FreightMoney)), WN("FreightMoney", FreightMoney, "Value", ">=") & P("State", Cnaws.Product.Modules.OrderState.Payment) & P("Id", Id)) == DataStatus.Success)
                {
                    DistributorOrder order = GetById(ds, Id);
                    string NewOrderId = NewId(DateTime.Now, order.UserId);
                    if (Db<DistributorOrder>.Query(ds).Update().Set("Id", NewOrderId).Where(W("Id", Id)).Execute() > 0)
                    {
                        if (Db<DistributorOrderMapping>.Query(ds).Update().Set("OrderId", NewOrderId).Where(W("OrderId", Id)).Execute() > 0)
                        {
                            ds.Commit();
                            return DataStatus.Success;
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Rollback;
            }

        }


        /// <summary>
        /// 自动确认收货
        /// </summary>
        /// <param name="ds"></param>
        internal static void AutomaticReceipt(DataSource ds)
        {
            Db<DistributorOrder>.Query(ds).Update()
                .Set("State", Cnaws.Product.Modules.OrderState.Finished)
                .Set("ReceiptDate", DateTime.Now)
                .Set("RefundDate", DateTime.Now)
                .Where(W("State", Cnaws.Product.Modules.OrderState.Receipt) & W("DeliveryDate", DateTime.Now.AddDays(-15), DbWhereType.LessThan))
                .Execute();
        }

        #region Api专用方法
        [Obsolete]
        public static new SplitPageData<dynamic> GetAjaxPageByUserAndStateApi(DataSource ds, long userId, string state, long index, int size, int show, int channel = 2)
        {
            AutomaticReceipt(ds);
            long count;
            DbWhereQueue dw = W("UserId", userId) /*& W("Channel", channel)*/;
            if (!string.IsNullOrEmpty(state) && !"_".Equals(state))
            {
                P.OrderState os = (P.OrderState)Enum.Parse(TType<P.OrderState>.Type, state, true);
                if (os == Cnaws.Product.Modules.OrderState.Payment)
                    dw &= W("State", Cnaws.Product.Modules.OrderState.Payment);
                else
                    dw &= W("State", os);
            }
            dw &= W("State", Cnaws.Product.Modules.OrderState.Perfect, DbWhereType.NotEqual);
            IList<DistributorOrder> list = Db<DistributorOrder>.Query(ds)
                .Select()
                .Where(dw)
                .OrderBy(D("CreationDate"))
                .ToList<DistributorOrder>(size, index, out count);
            List<dynamic> temp;
            DistributorProductCacheInfo pci;
            IList<DistributorOrderMapping> maps;
            List<dynamic> result = new List<dynamic>(list.Count);
            foreach (DistributorOrder item in list)
            {
                maps = item.GetMapping(ds);
                temp = new List<dynamic>(maps.Count);
                foreach (DistributorOrderMapping it in maps)
                {
                    pci = JsonValue.Deserialize<DistributorProductCacheInfo>(it.ProductInfo);
                    temp.Add(new
                    {
                        ProductId = it.ProductId,
                        Image = it.GetImage(pci.Image),
                        ProductInfo = pci,
                        Price = it.Price,
                        Count = it.Count,
                        IsService = it.IsService,
                        AfterSalesOrderId = it.AfterSalesOrderId
                    });
                }
                result.Add(new
                {
                    Id = item.Id,
                    State = (int)item.State,
                    StateInfo = item.GetStateInfo(),
                    CreationDate = item.CreationDate,
                    Address = item.Address,
                    Message = item.Message,
                    Mappings = temp,
                    FreightMoney = item.FreightMoney,
                    TotalMoney = item.TotalMoney
                });
            }
            return new SplitPageData<dynamic>(index, size, result, count, show);
        }
        /// <summary>
        /// 2.0版本根据订单获取订单列表(包含产品)
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userId"></param>
        /// <param name="state"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="show"></param>
        /// <returns></returns>
        public static SplitPageData<dynamic> GetAjaxPageByUserAndStateApi2(DataSource ds, long userId, string state, int index, int size, int show)
        {
            AutomaticReceipt(ds);
            long count;
            DbWhereQueue dw = W("UserId", userId);
            if (!string.IsNullOrEmpty(state) && !"_".Equals(state))
            {
                P.OrderState os = (P.OrderState)Enum.Parse(TType<P.OrderState>.Type, state, true);
                if (os == Cnaws.Product.Modules.OrderState.Payment)
                    dw &= W("State", Cnaws.Product.Modules.OrderState.Payment);
                else
                    dw &= W("State", os);
            }
            dw &= W("State", Cnaws.Product.Modules.OrderState.Perfect, DbWhereType.NotEqual);
            IList<DistributorOrder> list = Db<DistributorOrder>.Query(ds)
                .Select()
                .Where(dw)
                .OrderBy(D("CreationDate"))
                .ToList<DistributorOrder>(size, index, out count);
            DistributorProductCacheInfo pci;
            IList<DistributorOrderMapping> maps;
            List<dynamic> result = new List<dynamic>();
            foreach (DistributorOrder item in list)
            {
                maps = item.GetMapping(ds);
                List<DistributorOrderMappingCacheInfo> temp = new List<DistributorOrderMappingCacheInfo>(maps.Count);
                foreach (DistributorOrderMapping it in maps)
                {
                    temp.Add(new DistributorOrderMappingCacheInfo(it));
                }
                result.Add(new
                {
                    Order = item,
                    Products = temp,
                });
            }
            return new SplitPageData<dynamic>(index, size, result, count, show);
        }

        #endregion Api专用方法

        /// <summary>
        /// 根据订单号，商品名称，下单时间以及状态搜索
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userId"></param>
        /// <param name="state"></param>
        /// <param name="index"></param>
        /// <param name="size"></param>
        /// <param name="show"></param>
        /// <param name="queryText"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static new SplitPageData<DistributorOrder> SearchOrder(
            DataSource ds,
            long userId,
            string state,
            long index,
            int size,
            int show,
            string queryText,
            string startDate,
            string endDate,
            int channel = 2)
        {
            long count;
            DbWhereQueue dw = W("UserId", userId) /*& W("Channel", channel)*/;
            if (!string.IsNullOrEmpty(state) && !"_".Equals(state))
            {
                P.OrderState os = (P.OrderState)Enum.Parse(TType<P.OrderState>.Type, state, true);
                dw &= (W("State", (int)os));
            }
            else
            {
                dw &= (W("State", Cnaws.Product.Modules.OrderState.Perfect, DbWhereType.NotEqual));
            }
            if (!string.IsNullOrEmpty(queryText))
            {
                long orderId = 0;
                long.TryParse(queryText, out orderId);
                if (orderId > 0)
                {
                    dw &= W("Id", orderId, DbWhereType.LikeBegin);
                }
                else
                {
                    dw &= W("Id")
                        .InSelect<DistributorOrderMapping>(S("OrderId")).Where(
                        W("ProductId").InSelect<DistributorProduct>(S("Id")).Where(W("Title", queryText, DbWhereType.Like)).Result()
                        ).Result();
                }
            }

            if (!string.IsNullOrEmpty(startDate))
            {
                dw &= W("CreationDate", startDate, DbWhereType.GreaterThanOrEqual);
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                dw &= W("CreationDate", endDate, DbWhereType.LessThanOrEqual);
            }

            IList<DistributorOrder> list = Db<DistributorOrder>.Query(ds)
                .Select()
                .Where(dw)
                .OrderBy(new DbOrderBy("CreationDate", DbOrderByType.Desc))
                .ToList<DistributorOrder>(size, index, out count);
            return new SplitPageData<DistributorOrder>(index, size, list, count, show);
        }
        /// <summary>
        /// 修改订单的父订单号
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="orderid"></param>
        /// <param name="neworderid"></param>
        /// <returns></returns>
        public static DataStatus UpdateParentId(DataSource ds,string orderid,string neworderid)
        {
            if (Db<DistributorOrder>.Query(ds).Update().Set("ParentId", neworderid).Where(W("Id", orderid)).Execute() > 0)
                return DataStatus.Success;
            else
                return DataStatus.Failed;
        }
    }
}
