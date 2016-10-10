using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Product.Modules;
using P = Cnaws.Passport.Modules;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws.Web.Templates;
using Cnaws.Area;

namespace XcpNet.Website.Controllers.Extension
{
    public sealed class One : Cnaws.Product.Controllers.One
    {
        private Country _country = null;

        private Country Country
        {
            get
            {
                if (_country == null)
                    _country = Country.GetCountry();
                return _country;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_country != null)
            {
                _country.Dispose();
                _country = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnInfo(M.OneProduct product, M.OneProductNumber number)
        {
            if (number.State != M.OneProductNumberState.Normal)
            {
                IList<DataJoin<M.OneProductOrder, P.MemberInfo>> top = Db<M.OneProductOrder>.Query(DataSource)
                    .Select(new DbSelect<M.OneProductOrder>(), new DbSelect<P.MemberInfo>(), new DbSelectAs<M.OneProduct>("Title"), new DbSelectAs<M.OneProduct>("Count"))
                    .InnerJoin(new DbColumn<M.OneProductOrder>("UserId"), new DbColumn<P.MemberInfo>("Id"))
                    .InnerJoin(new DbColumn<M.OneProductOrder>("ProductId"), new DbColumn<M.OneProduct>("Id"))
                    .Where(new DbWhere<M.OneProductOrder>("CreationDate", number.EndDate, DbWhereType.LessThanOrEqual))
                    .OrderBy(new DbOrderBy<M.OneProductOrder>("CreationDate", DbOrderByType.Desc), new DbOrderBy<M.OneProductOrder>("Id", DbOrderByType.Desc))
                    .ToList<DataJoin<M.OneProductOrder, P.MemberInfo>>(100);
                long max = 0L;
                foreach (DataJoin<M.OneProductOrder, P.MemberInfo> item in top)
                    max += long.Parse(item.A.CreationDate.ToString("HHmmssfff"));
                this["Member"] = P.MemberInfo.GetById(DataSource, number.UserId);
                this["TopList"] = top;
                this["Max"] = max;
            }
            if (User != null && User.Identity != null && User.Identity.IsAuthenticated && !User.Identity.IsAdmin)
            {
                this["Address"] = P.ShippingAddress.GetAll(DataSource, User.Identity.Id);
                this["Orders"] = M.OneProductOrder.GetAllByUserProductNum(DataSource, User.Identity.Id, product.Id, number.Id);
                this["GetCity"] = new FuncHandler((args) =>
                {
                    return Country.GetCity(Convert.ToInt32(args[0]));
                });
            }
        }

        public override void Orders(int productId, long productNum, long page = 1)
        {
            long count;
            page = Math.Max(1, page);
            IList<DataJoin<M.OneProductOrder, P.MemberInfo>> list = Db<M.OneProductOrder>.Query(DataSource)
                .Select(new DbSelect<M.OneProductOrder>(), new DbSelect<P.MemberInfo>())
                .InnerJoin(new DbColumn<M.OneProductOrder>("UserId"), new DbColumn<P.MemberInfo>("Id"))
                .Where(new DbWhere<M.OneProductOrder>("ProductId", productId) & new DbWhere<M.OneProductOrder>("ProductNum", productNum))
                .OrderBy(new DbOrderBy<M.OneProductOrder>("CreationDate", DbOrderByType.Desc), new DbOrderBy<M.OneProductOrder>("Id", DbOrderByType.Desc))
                .ToList<DataJoin<M.OneProductOrder, P.MemberInfo>>(110, page, out count);
            this["OrderList"] = new SplitPageData<DataJoin<M.OneProductOrder, P.MemberInfo>>(page, 100, list, count, 8);
            this["GetPageUrl"] = new FuncHandler((args) =>
              {
                  return GetUrl("/one/orders/", productId.ToString(), "/", productNum.ToString(), "/", Convert.ToInt64(args[0]).ToString());
              });
            Render("one.html");
        }

        public override void History(int productId, long page = 1)
        {
            long count;
            page = Math.Max(1, page);
            IList<DataJoin<M.OneProductNumber, P.MemberInfo>> list = Db<M.OneProductNumber>.Query(DataSource)
                .Select(new DbSelect<M.OneProductNumber>(), new DbSelect<P.MemberInfo>())
                .InnerJoin(new DbColumn<M.OneProductNumber>("UserId"), new DbColumn<P.MemberInfo>("Id"))
                .Where(new DbWhere<M.OneProductNumber>("UserId", 0, DbWhereType.NotEqual) & new DbWhere<M.OneProductNumber>("ProductId", productId))
                .OrderBy(new DbOrderBy<M.OneProductNumber>("Id", DbOrderByType.Desc))
                .ToList<DataJoin<M.OneProductNumber, P.MemberInfo>>(20, page, out count);
            this["HistoryList"] = new SplitPageData<DataJoin<M.OneProductNumber, P.MemberInfo>>(page, 20, list, count, 8);
            this["GetPageUrl"] = new FuncHandler((args) =>
            {
                return GetUrl("/one/history/", productId.ToString(), "/", Convert.ToInt64(args[0]).ToString());
            });
            this["GetCount"] = new FuncHandler((args) =>
              {
                  return M.OneProductOrder.GetCountByUser(DataSource, productId, Convert.ToInt64(args[0]), Convert.ToInt64(args[1]));
              });
            Render("one.html");
        }
    }
}
