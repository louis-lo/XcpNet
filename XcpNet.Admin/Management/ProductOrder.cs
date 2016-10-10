using Cnaws;
using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws.ExtensionMethods;
using Cnaws.Management;
using Cnaws.Pay;
using Cnaws.Web;
using Cnaws.Web.Templates;
using System;
using System.Collections.Generic;
using M = Cnaws.Product.Modules;
using U = Cnaws.Passport.Modules;

namespace XcpNet.Admin.Management
{
    public class ProductOrder : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        private Dictionary<int, string> _menus;

        public ProductOrder()
        {
            _menus = new Dictionary<int, string>();
            _menus.Add(-1, "所有订单");
            _menus.Add(0, "已关闭订单");
            _menus.Add(1, "待完善订单");
            _menus.Add(2, "待付款订单");
            _menus.Add(3, "待发货订单");
            _menus.Add(4, "待收货订单");
            _menus.Add(5, "已出库订单");
            _menus.Add(6, "已完成订单");
        }

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Admin"; }
        }

        public virtual string Path
        {
            get { return "productorder"; }
        }
        public virtual int Channel
        {
            get { return 1; }
        }

        protected virtual object GetList(int state, int type, int page, string orderid)
        {
            long count;

            if ("_".Equals(orderid))
                orderid = string.Empty;

            DbWhereQueue where = null;
            List<DbOrderBy> order = new List<DbOrderBy>();
            switch (state)
            {
                case -1:
                    order.Add(new DbOrderBy<M.ProductOrder>("Id", DbOrderByType.Desc));
                    break;
                case (int)M.OrderState.Delivery:
                case (int)M.OrderState.OutWarehouse:
                    where &= new DbWhere<M.ProductOrder>("State", state);
                    order.Add(new DbOrderBy<M.ProductOrder>("PaymentDate", DbOrderByType.Desc));
                    break;
                case (int)M.OrderState.Receipt:
                    where &= new DbWhere<M.ProductOrder>("State", state);
                    order.Add(new DbOrderBy<M.ProductOrder>("DeliveryDate", DbOrderByType.Desc));
                    break;
                case (int)M.OrderState.Finished:
                    where &= new DbWhere<M.ProductOrder>("State", state);
                    order.Add(new DbOrderBy<M.ProductOrder>("ReceiptDate", DbOrderByType.Desc));
                    break;
                case (int)M.OrderState.Invalid:
                case (int)M.OrderState.Perfect:
                case (int)M.OrderState.Payment:
                    where &= new DbWhere<M.ProductOrder>("State", state);
                    order.Add(new DbOrderBy<M.ProductOrder>("CreationDate", DbOrderByType.Desc));
                    break;
                default:
                    throw new Exception();
            }

            if (!string.IsNullOrEmpty(orderid))
            {
                long mobile;
                switch (type)
                {
                    case 0:
                        where &= new DbWhere<M.ProductOrder>("Id", orderid);
                        break;
                    case 1:
                        if (long.TryParse(orderid, out mobile))
                            where &= (new DbWhere<M.ProductOrder>("SupplierId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Mobile", long.Parse(orderid))).Result());
                        else if (Utility.EmailRegularExpression.IsMatch(orderid))
                            where &= (new DbWhere<M.ProductOrder>("SupplierId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Email", orderid)).Result());
                        else
                            where &= (new DbWhere<M.ProductOrder>("SupplierId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Name", orderid)).Result());
                        break;
                    case 2:
                        if (long.TryParse(orderid, out mobile))
                            where &= (new DbWhere<M.ProductOrder>("UserId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Mobile", long.Parse(orderid))).Result());
                        else if (Utility.EmailRegularExpression.IsMatch(orderid))
                            where &= (new DbWhere<M.ProductOrder>("UserId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Email", orderid)).Result());
                        else
                            where &= (new DbWhere<M.ProductOrder>("UserId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Name", orderid)).Result());
                        break;
                    case 3:
                        if (orderid.IndexOf('|') != -1)
                        {
                            string[] time = orderid.Split('|');
                            DateTime begindt = new DateTime(), endtime = new DateTime();
                            if (DateTime.TryParse(time[0], out begindt) && DateTime.TryParse(time[1], out endtime))
                            {
                                where &= (new DbWhere<M.ProductOrder>("CreationDate", begindt, DbWhereType.GreaterThan) & new DbWhere<M.ProductOrder>("CreationDate", endtime, DbWhereType.LessThan));
                            }
                        }
                        break;
                    case 4:
                        where &= new DbWhere<M.ProductOrder>("Address", orderid, DbWhereType.Like);
                        break;
                    case 5:
                        where &= new DbWhere<M.ProductOrder>("TotalMoney", Money.Parse(orderid));
                        break;
                    case 6:
                        where &= new DbWhere<M.Supplier>("Company", orderid, DbWhereType.Like);
                        break;
                    case 7:
                        where &= new DbWhere<M.Distributor>("Company", orderid, DbWhereType.Like);
                        break;
                }
            }

            where &= new DbWhere<M.ProductOrder>("Channel", Channel);//增加频道

            IList<dynamic> list = Db<M.ProductOrder>.Query(DataSource)
                .Select(
                    new DbSelectAs<M.ProductOrder>("Id"),
                    new DbSelectAs<U.Member>("Name"),
                    new DbSelectAs<M.Supplier>("Company"),
                    new DbSelectAs<M.Supplier>("Contact"),
                    new DbSelectAs<M.Supplier>("ContactPhone"),
                    new DbSelectAs<M.Distributor>("Company", "Distributor"),
                    new DbSelectAs<M.ProductOrder>("TotalMoney"),
                    new DbSelectAs<M.ProductOrder>("CreationDate"),
                    new DbSelectAs<M.ProductOrder>("PaymentDate"),
                    new DbSelectAs<M.ProductOrder>("State"),

                    new DbSelect<M.ProductOrder>("Id"),
                    new DbSelect<M.ProductOrder>("DeliveryDate"),
                    new DbSelect<M.ProductOrder>("ReceiptDate"),
                    new DbSelect<M.ProductOrder>("PaymentDate"),
                    new DbSelect<M.ProductOrder>("CreationDate")
                )
                .LeftJoin(new DbColumn<M.ProductOrder>("SupplierId"), new DbColumn<U.Member>("Id"))
                .LeftJoin(new DbColumn<M.ProductOrder>("SupplierId"), new DbColumn<M.Supplier>("UserId"))
                .LeftJoin(new DbColumn<M.ProductOrder>("ShopId"), new DbColumn<M.Distributor>("UserId"))
                .Where(where)
                .OrderBy(order.ToArray())
                .ToList(10, page, out count);

            return new SplitPageData<dynamic>(page, 10, list, count, 11);
        }
        protected virtual M.ProductOrder GetOrder(string orderId)
        {
            return M.ProductOrder.GetById(DataSource, orderId);
        }

        public void Index(int state = -1, int type = -1)
        {
            if (CheckRight())
            {
                if (CheckPost("productorder", () =>
                {
                    this["State"] = state;
                    this["Type"] = type;
                    this["Menus"] = _menus;
                    this["GetMenuName"] = new FuncHandler((args) =>
                    {
                        try { return _menus[(int)args[0]]; }
                        catch (Exception) { }
                        return _menus[0];
                    });
                }))
                    NotFound();
            }
        }
        public void List(int state = -1, int type = -1, int page = 1, string orderid = "")
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    SetResult(GetList(state, type, page, orderid));
                }
            }
        }

        public void Info(string orderId)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (CheckPost("productorder", () =>
                     {
                         M.ProductOrder order = GetOrder(orderId);
                         this["Order"] = order;
                         this["PayMent"] = Cnaws.Pay.Modules.PayRecord.GetById(DataSource, orderId, PaymentType.Pay);

                         this["Distributor"] = M.Distributor.GetById(DataSource, order.ShopId);

                         M.ProductLogistics log = null;
                         if (order != null && order.State > M.OrderState.Delivery)
                             log = M.ProductLogistics.GetByOrder(DataSource, order.Id);
                         if (log == null)
                             log = new M.ProductLogistics();
                         this["Logistics"] = log;

                         if (!string.IsNullOrEmpty(log.BillNo))
                         {
                             if (string.IsNullOrEmpty(log.ProviderDetailed))
                             {
                                 string providerDetailed = Cnaws.Product.Logistics.ExpressQuery.QueryReturnJson(log.ProviderKey, log.BillNo);
                                 Cnaws.Product.Logistics.ExpressInfo expressInfo = Cnaws.Product.Logistics.ExpressQuery.Query(providerDetailed);
                                 this["ExpressInfo"] = expressInfo;
                                 if (expressInfo.state == "3")
                                 {
                                     log.ProviderDetailed = providerDetailed;
                                     M.ProductLogistics.UpdateProviderDetailed(DataSource, log);
                                 }
                             }
                             else
                             {
                                 this["ExpressInfo"] = Cnaws.Product.Logistics.ExpressQuery.Query(log.ProviderDetailed);
                             }
                         }
                     }))
                    {
                        NotFound();
                    }
                }
            }
        }

        ///// <summary>
        ///// 导出到excel
        ///// </summary>
        ///// <param name="state"></param>
        ///// <param name="type"></param>
        ///// <param name="orderid"></param>
        //public void Export(int state, int type, string orderid)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine(string.Concat("订单号", "\t", "供应商", "\t", "联系人", "\t", "联系电话", "\t", "总金额", "\t", "创建时间", "\t", "支付时间", "\t", "状态", "\t", "留言"));
        //    try
        //    {
        //        IList<DataJoin<M.ProductOrder, M.Supplier>> list = GetQueryList(state, type, orderid).ToList<DataJoin<M.ProductOrder, M.Supplier>>();
        //        if (list.Count == 0)
        //            throw new Exception("导出数据有误");

        //        foreach (DataJoin<M.ProductOrder, M.Supplier> order in list)
        //            sb.AppendLine(string.Concat(
        //                 "'", order.A.Id.ToString(), "\t",
        //                order.B.Company, "\t",
        //                order.B.Contact, "\t",
        //                order.B.ContactPhone, "\t",
        //                order.A.TotalMoney.ToString("c2"), "\t",
        //                order.A.CreationDate.ToString("yyyy-MM-dd HH:mm:ss"), "\t",
        //                order.A.PaymentDate.ToString("yyyy-MM-dd HH:mm:ss"), "\t",
        //                M.ProductOrder.GetStateText(order.A.State), "\t",
        //                order.A.Message
        //                ));
        //        sb.Append(Environment.NewLine);
        //        Response.Charset = "UTF-8";
        //        Response.ContentEncoding = Encoding.UTF8;
        //        Response.ContentType = "application/ms-excel";
        //        Response.AppendHeader("Content-Disposition", "attachment;filename=" + System.Web.HttpUtility.UrlEncode("城品惠订单") + ".xls");
        //        Response.Write(sb);
        //    }
        //    catch (Exception ex)
        //    {
        //        Response.Write(ex.Message);
        //    }
        //}
    }
}
