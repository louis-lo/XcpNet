using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using M = XcpNet.Supplier.Modules.Modules;
using System.Collections.Generic;
using Pd = Cnaws.Product.Modules;
using Cnaws.Data.Query;
using Me = Cnaws.Passport.Modules;
using Cnaws.ExtensionMethods;
using Cnaws;
using Cnaws.Pay;
using System.IO;
using System.Text;

namespace XcpNet.Supplier.Management
{
    public sealed class DistributorOrder : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Supplier"; }
        }

        //public void AllCategory()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            IList<M.DistributorCategory> list = M.DistributorCategory.GetAll(DataSource, -1);
        //            if (IsPost)
        //                SetResult(list);
        //            else
        //                SetJavascript("allCategory", list);
        //        }
        //    }
        //}

        public void Index(int state = -1, int type = -1)
        {
            if (CheckRight())
            {
                if (CheckPost("distributororder", () =>
                {
                    this["State"] = state;
                    this["Type"] = type;
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
                    long count;
                    IList<DataJoin<M.DistributorOrder, Pd.Supplier>> list = GetQueryList(state, type, orderid)
                        .ToList<DataJoin<M.DistributorOrder, Pd.Supplier>>(10, page, out count);
                    SetResult(new SplitPageData<DataJoin<M.DistributorOrder, Pd.Supplier>>(page, 10, list, count, 11));
                    //SetResult(M.DistributorOrder.GetPage(DataSource, state, orderid, page, 10, 11));
                }
            }
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="state"></param>
        /// <param name="type"></param>
        /// <param name="orderid"></param>
        /// <returns></returns>
        private DbOrderByQuery<DbSelectWhereQuery<DbJoinQuery<DbSelectQuery<M.DistributorOrder>, M.DistributorOrder, Pd.Supplier>>> GetQueryList(int state, int type, string orderid)
        {
            if (orderid == "_")
                orderid = "";

            DbOrderByQuery<DbSelectWhereQuery<DbJoinQuery<DbSelectQuery<M.DistributorOrder>, M.DistributorOrder, Pd.Supplier>>> query;
            DbWhereQueue where = null;
            DbOrderBy[] order = new DbOrderBy[1];
            switch (state)
            {
                case -1:
                    order[0] = new DbOrderBy<M.DistributorOrder>("Id", DbOrderByType.Desc);
                    break;
                case (int)Pd.OrderState.Delivery:
                    where = new DbWhere<M.DistributorOrder>("State", state);
                    order[0] = new DbOrderBy<M.DistributorOrder>("PaymentDate", DbOrderByType.Desc);
                    break;
                case (int)Pd.OrderState.Receipt:
                    where = new DbWhere<M.DistributorOrder>("State", state);
                    order[0] = new DbOrderBy<M.DistributorOrder>("DeliveryDate", DbOrderByType.Desc);
                    break;
                //case (int)OrderState.Evaluation:
                case (int)Pd.OrderState.Finished:
                    where = new DbWhere<M.DistributorOrder>("State", state);
                    order[0] = new DbOrderBy<M.DistributorOrder>("ReceiptDate", DbOrderByType.Desc);
                    break;
                case (int)Pd.OrderState.Invalid:
                case (int)Pd.OrderState.Perfect:
                case (int)Pd.OrderState.Payment:
                    where = new DbWhere<M.DistributorOrder>("State", state);
                    order[0] = new DbOrderBy<M.DistributorOrder>("CreationDate", DbOrderByType.Desc);
                    break;
                default:
                    throw new Exception();
            }
            if (state != -1 && !string.IsNullOrEmpty(orderid))
            {
                if (type == 0)
                {
                    where &= new DbWhere<M.DistributorOrder>("Id", orderid);
                }
                else if (type == 1)
                {
                    long mobile;
                    if (long.TryParse(orderid, out mobile))
                        where &= (new DbWhere<M.DistributorOrder>("SupplierId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Mobile", long.Parse(orderid))).Result());
                    else if (Utility.EmailRegularExpression.IsMatch(orderid))
                        where &= (new DbWhere<M.DistributorOrder>("SupplierId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Email", orderid)).Result());
                    else
                        where &= (new DbWhere<M.DistributorOrder>("SupplierId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Name", orderid)).Result());
                }
                else if (type == 2)
                {
                    long mobile;
                    if (long.TryParse(orderid, out mobile))
                        where &= (new DbWhere<M.DistributorOrder>("UserId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Mobile", long.Parse(orderid))).Result());
                    else if (Utility.EmailRegularExpression.IsMatch(orderid))
                        where &= (new DbWhere<M.DistributorOrder>("UserId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Email", orderid)).Result());
                    else
                        where &= (new DbWhere<M.DistributorOrder>("UserId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Name", orderid)).Result());
                }
                else if (type == 3)
                {
                    if (orderid.IndexOf('|') != -1)
                    {
                        string[] time = orderid.Split('|');
                        DateTime begindt = new DateTime(), endtime = new DateTime();
                        if (DateTime.TryParse(time[0], out begindt) && DateTime.TryParse(time[1], out endtime))
                        {
                            where &= (new DbWhere<M.DistributorOrder>("CreationDate", begindt, DbWhereType.GreaterThan) & new DbWhere<M.DistributorOrder>("CreationDate", endtime, DbWhereType.LessThan));
                        }
                    }
                }
                else if (type == 4)
                {
                    where &= new DbWhere<M.DistributorOrder>("Address", orderid, DbWhereType.Like);
                }
                else if (type == 5)
                {
                    where &= new DbWhere<M.DistributorOrder>("Address", orderid, DbWhereType.Like);
                }
                else if (type == 6)
                {
                    where &= new DbWhere<M.DistributorOrder>("TotalMoney", Money.Parse(orderid));
                }
            }
            else if (!string.IsNullOrEmpty(orderid))
            {
                if (type == 0)
                {
                    where &= new DbWhere<M.DistributorOrder>("Id", orderid);
                }
                else if (type == 1)
                {
                    long mobile;
                    if (long.TryParse(orderid, out mobile))
                        where &= (new DbWhere<M.DistributorOrder>("SupplierId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Mobile", long.Parse(orderid))).Result());
                    else if (Utility.EmailRegularExpression.IsMatch(orderid))
                        where &= (new DbWhere<M.DistributorOrder>("SupplierId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Email", orderid)).Result());
                    else
                        where &= (new DbWhere<M.DistributorOrder>("SupplierId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Name", orderid)).Result());
                }
                else if (type == 2)
                {
                    long mobile;
                    if (long.TryParse(orderid, out mobile))
                        where &= (new DbWhere<M.DistributorOrder>("UserId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Mobile", long.Parse(orderid))).Result());
                    else if (Utility.EmailRegularExpression.IsMatch(orderid))
                        where &= (new DbWhere<M.DistributorOrder>("UserId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Email", orderid)).Result());
                    else
                        where &= (new DbWhere<M.DistributorOrder>("UserId").InSelect<Me.Member>(new DbSelect("Id")).Where(new DbWhere("Name", orderid)).Result());
                }
                else if (type == 3)
                {
                    if (orderid.IndexOf('|') != -1)
                    {
                        string[] time = orderid.Split('|');
                        DateTime begindt = new DateTime(), endtime = new DateTime();
                        if (DateTime.TryParse(time[0], out begindt) && DateTime.TryParse(time[1], out endtime))
                        {
                            where &= (new DbWhere<M.DistributorOrder>("CreationDate", begindt, DbWhereType.GreaterThan) & new DbWhere<M.DistributorOrder>("CreationDate", endtime, DbWhereType.LessThan));
                        }
                    }
                }
                else if (type == 4)
                {
                    where &= new DbWhere<M.DistributorOrder>("Address", orderid, DbWhereType.Like);
                }
                else if (type == 5)
                {
                    where &= new DbWhere<M.DistributorOrder>("Address", orderid, DbWhereType.Like);
                }
                else if (type == 6)
                {
                    where &= new DbWhere<M.DistributorOrder>("TotalMoney", Money.Parse(orderid));
                }
            }
            query = Db<M.DistributorOrder>.Query(DataSource)
                .Select(new DbSelect<M.DistributorOrder>(), new DbSelect<Pd.Supplier>())
                .InnerJoin(new DbColumn<M.DistributorOrder>("SupplierId"), new DbColumn<Pd.Supplier>("UserId"))
                .Where(where)
                .OrderBy(
                new DbOrderBy<M.DistributorOrder>("DeliveryDate", DbOrderByType.Desc),
                new DbOrderBy<M.DistributorOrder>("ReceiptDate", DbOrderByType.Desc),
                new DbOrderBy<M.DistributorOrder>("PaymentDate", DbOrderByType.Desc),
                new DbOrderBy<M.DistributorOrder>("CreationDate", DbOrderByType.Desc));
            return query;
        }

        public void Info(string orderId)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (CheckPost("distributororder", () =>
                     {
                         M.DistributorOrder order = M.DistributorOrder.GetById(DataSource, orderId);
                         this["Order"] = order;
                         this["PayMent"] = Cnaws.Pay.Modules.PayRecord.GetById(DataSource, orderId, PaymentType.Pay);

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
                                     Pd.ProductLogistics.UpdateProviderDetailed(DataSource, log);
                                 }
                             }
                             else
                             {
                                 this["ExpressInfo"] = Cnaws.Product.Logistics.ExpressQuery.Query(log.ProviderDetailed);
                             }
                         }
                         this["Logistics"] = log;
                     }))
                    {
                        NotFound();
                    }
                }
            }
        }

        //public void doDelivery()
        //{
        //    if (IsPost)
        //    {
        //        DataSource.Begin();
        //        try
        //        {
        //            Pd.ProductLogistics value = DbTable.Load<Pd.ProductLogistics>(Request.Form);
        //            if (Pd.ProductLogistics.GetByOrder(DataSource, value.OrderId) == null)
        //            {
        //                if (value.Insert(DataSource) != DataStatus.Success)
        //                    throw new Exception();
        //                M.DistributorOrder order = M.DistributorOrder.GetById(DataSource, value.OrderId);
        //                if ((new M.DistributorOrder() { Id = value.OrderId, UserId = order.UserId }).UpdateStateByUser(DataSource, Pd.OrderState.Delivery) != DataStatus.Success)
        //                    throw new Exception();
        //                DataSource.Commit();
        //                SetResult(true);
        //            }
        //            else
        //            {
        //                if (value.Update(DataSource) != DataStatus.Success)
        //                    throw new Exception();
        //                DataSource.Commit();
        //                SetResult(true);
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            DataSource.Rollback();
        //            SetResult(false);
        //        }
        //    }
        //    else
        //    {
        //        NotFound();
        //    }
        //    return;
        //}


        //public void doFreight()
        //{
        //    if (IsPost)
        //    {
        //        DataSource.Begin();
        //        try
        //        {
        //            M.DistributorOrder value = DbTable.Load<M.DistributorOrder>(Request.Form);
        //            value.SupplierId = User.Identity.Id;
        //            SetResult(value.UpdateFreightBySupplier(DataSource));
        //        }
        //        catch (Exception)
        //        {
        //            DataSource.Rollback();
        //            SetResult(false);
        //        }
        //    }
        //    else
        //    {
        //        NotFound();
        //    }
        //    return;
        //}

        //public void Notify()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                Cnaws.Product.Modules.Supplier value = DbTable.Load<Cnaws.Product.Modules.Supplier>(Request.Form);
        //                SetResult(value.Approved(DataSource), () =>
        //                {
        //                    WritePostLog("MOD");
        //                });
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        ///// <summary>
        ///// 导出excel
        ///// </summary>
        ///// <param name="state"></param>
        ///// <param name="type"></param>
        ///// <param name="orderid"></param>
        //public void Export(int state, int type, string orderid)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine(string.Concat("订单号", "\t", "总金额", "\t", "运费", "\t", "创建时间", "\t", "支付时间", "\t", "状态", "留言", "\t"));
        //    try
        //    {
        //        IList<M.DistributorOrder> list = GetQueryList(state, type, orderid).ToList<M.DistributorOrder>();
        //        if (list.Count == 0)
        //            throw new Exception("导出数据有误");
        //        foreach (M.DistributorOrder order in list)
        //            sb.AppendLine(string.Concat(
        //                "'", order.Id, "\t",
        //                order.TotalMoney.ToString("c2"), "\t",
        //                order.FreightMoney.ToString("c2"), "\t",
        //                order.CreationDate.ToString("yyyy-MM-dd HH:mm:ss"), "\t",
        //                order.PaymentDate.ToString("yyyy-MM-dd HH:mm:ss"), "\t",
        //                Pd.ProductOrder.GetStateText(order.State),
        //                 order.Message, "\t"));
        //        sb.Append(Environment.NewLine);

        //        Response.Charset = "UTF-8";
        //        Response.ContentEncoding = Encoding.UTF8;
        //        Response.ContentType = "application/ms-excel";
        //        Response.AppendHeader("Content-Disposition", "attachment;filename=" + System.Web.HttpUtility.UrlEncode("进货宝订单") + ".xls");
        //        Response.Write(sb);
        //    }
        //    catch (Exception ex)
        //    {
        //        Response.Write(ex.Message);
        //    }
        //}
    }
}
