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
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using S = XcpNet.Supplier.Modules.Modules;

namespace XcpNet.Admin.Management
{
    public class MoneyRecord : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        private Dictionary<int, string> _menus;

        public MoneyRecord()
        {
            _menus = new Dictionary<int, string>();
            _menus.Add(-1, "所有类型");
            _menus.Add(0, "充值");
            _menus.Add(1, "购物");
            _menus.Add(2, "1元抢");
            _menus.Add(3, "后台充值");
            _menus.Add(4, "退款");
            _menus.Add(5, "进货");
        }

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Admin"; }
        }

        public void Index(int type = -1)
        {
            if (CheckRight())
            {
                if (CheckPost("moneyrecord", () =>
                {
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
        public void List(int type = -1, int page = 1, string search = "_")
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    long count;
                    long mobile;
                    DbWhereQueue where = null;

                    if ("_".Equals(search))
                        search = null;

                    if (type >= 0)
                        where &= new DbWhere<M.MoneyRecord>("Type", type);

                    if (!string.IsNullOrEmpty(search))
                    {
                        if (long.TryParse(search, out mobile))
                            where &= (new DbWhere<M.MoneyRecord>("MemberId").InSelect<M.Member>(new DbSelect("Id")).Where(new DbWhere("Mobile", long.Parse(search))).Result());
                        else if (Utility.EmailRegularExpression.IsMatch(search))
                            where &= (new DbWhere<M.MoneyRecord>("MemberId").InSelect<M.Member>(new DbSelect("Id")).Where(new DbWhere("Email", search)).Result());
                        else
                            where &= (new DbWhere<M.MoneyRecord>("MemberId").InSelect<M.Member>(new DbSelect("Id")).Where(new DbWhere("Name", search)).Result());
                    }

                    IList<dynamic> list = Db<M.MoneyRecord>.Query(DataSource)
                        .Select(
                            new DbSelectAs<M.MoneyRecord>("Id"),
                            new DbSelectAs<M.MoneyRecord>("MemberId"),
                            new DbSelectAs<M.Member>("Name"),
                            new DbSelectAs<M.Member>("Mobile"),
                            new DbSelectAs<M.MoneyRecord>("Title"),
                            new DbSelectAs<M.MoneyRecord>("Type"),
                            new DbSelectAs<M.MoneyRecord>("TargetId"),
                            new DbSelectAs<M.MoneyRecord>("Value"),
                            new DbSelectAs<M.MoneyRecord>("CreationDate"),

                            new DbSelect<M.MoneyRecord>("CreationDate")
                        )
                        .LeftJoin(new DbColumn<M.MoneyRecord>("MemberId"), new DbColumn<M.Member>("Id"))
                        .Where(where)
                        .OrderBy(new DbOrderBy<M.MoneyRecord>("CreationDate", DbOrderByType.Desc))
                        .ToList(10, page, out count);

                    SetResult(new SplitPageData<dynamic>(page, 10, list, count, 11));
                }
            }
        }

        public void Info(int type, string orderId)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (CheckPost("moneyrecord", () =>
                     {
                         if (type == 1 || type == 5)
                         {
                             P.ProductOrder order = null;
                             if (type == 1)
                                 order = P.ProductOrder.GetById(DataSource, orderId);
                             else if (type == 5)
                                 order = S.DistributorOrder.GetById(DataSource, orderId);
                             if (order != null)
                             {
                                 this["Type"] = type;
                                 this["Order"] = order;
                                 this["PayMent"] = Cnaws.Pay.Modules.PayRecord.GetById(DataSource, orderId, PaymentType.Pay);

                                 P.ProductLogistics log = null;
                                 if (order != null && order.State > P.OrderState.Delivery)
                                     log = P.ProductLogistics.GetByOrder(DataSource, order.Id);
                                 if (log == null)
                                     log = new P.ProductLogistics();
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
                                             P.ProductLogistics.UpdateProviderDetailed(DataSource, log);
                                         }
                                     }
                                     else
                                     {
                                         this["ExpressInfo"] = Cnaws.Product.Logistics.ExpressQuery.Query(log.ProviderDetailed);
                                     }
                                 }
                             }
                         }
                     }))
                    {
                        NotFound();
                    }
                }
            }
        }
    }
}
