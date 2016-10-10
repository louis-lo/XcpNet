using Cnaws;
using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws.ExtensionMethods;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using M = XcpNet.Supplier.Modules.Modules;
using P = Cnaws.Product.Modules;
using U = Cnaws.Passport.Modules;

namespace XcpNet.Admin.Management
{
    public sealed class DistributorOrder : ProductOrder
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        public override string Path
        {
            get { return "distributororder"; }
        }
        public override int Channel
        {
            get { return 0; }
        }

        protected override object GetList(int state, int type, int page, string orderid)
        {
            long count;

            if ("_".Equals(orderid))
                orderid = string.Empty;

            DbWhereQueue where = null;
            List<DbOrderBy> order = new List<DbOrderBy>();
            switch (state)
            {
                case -1:
                    order.Add(new DbOrderBy<M.DistributorOrder>("Id", DbOrderByType.Desc));
                    break;
                case (int)P.OrderState.Delivery:
                case (int)P.OrderState.OutWarehouse:
                    where &= new DbWhere<M.DistributorOrder>("State", state);
                    order.Add(new DbOrderBy<M.DistributorOrder>("PaymentDate", DbOrderByType.Desc));
                    break;
                case (int)P.OrderState.Receipt:
                    where &= new DbWhere<M.DistributorOrder>("State", state);
                    order.Add(new DbOrderBy<M.DistributorOrder>("DeliveryDate", DbOrderByType.Desc));
                    break;
                case (int)P.OrderState.Finished:
                    where &= new DbWhere<M.DistributorOrder>("State", state);
                    order.Add(new DbOrderBy<M.DistributorOrder>("ReceiptDate", DbOrderByType.Desc));
                    break;
                case (int)P.OrderState.Invalid:
                case (int)P.OrderState.Perfect:
                case (int)P.OrderState.Payment:
                    where &= new DbWhere<M.DistributorOrder>("State", state);
                    order.Add(new DbOrderBy<M.DistributorOrder>("CreationDate", DbOrderByType.Desc));
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
                        where &= new DbWhere<M.DistributorOrder>("Id", orderid);
                        break;
                    case 1:
                        if (long.TryParse(orderid, out mobile))
                            where &= (new DbWhere<M.DistributorOrder>("SupplierId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Mobile", long.Parse(orderid))).Result());
                        else if (Utility.EmailRegularExpression.IsMatch(orderid))
                            where &= (new DbWhere<M.DistributorOrder>("SupplierId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Email", orderid)).Result());
                        else
                            where &= (new DbWhere<M.DistributorOrder>("SupplierId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Name", orderid)).Result());
                        break;
                    case 2:
                        if (long.TryParse(orderid, out mobile))
                            where &= (new DbWhere<M.DistributorOrder>("UserId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Mobile", long.Parse(orderid))).Result());
                        else if (Utility.EmailRegularExpression.IsMatch(orderid))
                            where &= (new DbWhere<M.DistributorOrder>("UserId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Email", orderid)).Result());
                        else
                            where &= (new DbWhere<M.DistributorOrder>("UserId").InSelect<U.Member>(new DbSelect("Id")).Where(new DbWhere("Name", orderid)).Result());
                        break;
                    case 3:
                        if (orderid.IndexOf('|') != -1)
                        {
                            string[] time = orderid.Split('|');
                            DateTime begindt = new DateTime(), endtime = new DateTime();
                            if (DateTime.TryParse(time[0], out begindt) && DateTime.TryParse(time[1], out endtime))
                            {
                                where &= (new DbWhere<M.DistributorOrder>("CreationDate", begindt, DbWhereType.GreaterThan) & new DbWhere<M.DistributorOrder>("CreationDate", endtime, DbWhereType.LessThan));
                            }
                        }
                        break;
                    case 4:
                        where &= new DbWhere<M.DistributorOrder>("Address", orderid, DbWhereType.Like);
                        break;
                    case 5:
                        where &= new DbWhere<M.DistributorOrder>("TotalMoney", Money.Parse(orderid));
                        break;
                    case 6:
                        where &= new DbWhere<P.Supplier>("Company", orderid, DbWhereType.Like);
                        break;
                    case 7:
                        where &= new DbWhere<P.Distributor>("Company", orderid, DbWhereType.Like);
                        break;
                }
            }

            IList<dynamic> list = Db<M.DistributorOrder>.Query(DataSource)
                .Select(
                    new DbSelectAs<M.DistributorOrder>("Id"),
                    new DbSelectAs<U.Member>("Name"),
                    new DbSelectAs<P.Supplier>("Company"),
                    new DbSelectAs<P.Supplier>("Contact"),
                    new DbSelectAs<P.Supplier>("ContactPhone"),
                    new DbSelectAs<P.Distributor>("Company", "Distributor"),
                    new DbSelectAs<M.DistributorOrder>("TotalMoney"),
                    new DbSelectAs<M.DistributorOrder>("CreationDate"),
                    new DbSelectAs<M.DistributorOrder>("PaymentDate"),
                    new DbSelectAs<M.DistributorOrder>("State"),

                    new DbSelect<M.DistributorOrder>("Id"),
                    new DbSelect<M.DistributorOrder>("DeliveryDate"),
                    new DbSelect<M.DistributorOrder>("ReceiptDate"),
                    new DbSelect<M.DistributorOrder>("PaymentDate"),
                    new DbSelect<M.DistributorOrder>("CreationDate")
                )
                .LeftJoin(new DbColumn<M.DistributorOrder>("SupplierId"), new DbColumn<U.Member>("Id"))
                .LeftJoin(new DbColumn<M.DistributorOrder>("SupplierId"), new DbColumn<P.Supplier>("UserId"))
                .LeftJoin(new DbColumn<M.DistributorOrder>("ShopId"), new DbColumn<P.Distributor>("UserId"))
                .Where(where)
                .OrderBy(order.ToArray())
                .ToList(10, page, out count);

            return new SplitPageData<dynamic>(page, 10, list, count, 11);
        }
        protected override P.ProductOrder GetOrder(string orderId)
        {
            return M.DistributorOrder.GetById(DataSource, orderId);
        }
    }
}
