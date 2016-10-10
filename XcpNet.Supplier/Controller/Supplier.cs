using System;
using Cnaws.Web;
using Cnaws.Data;
using P = Cnaws.Product.Modules;
using System.Web;
using Cnaws.Area;
using Cnaws.Web.Templates;
using A = XcpNet.AfterSales.Modules;
using Cnaws.Data.Query;
using System.Collections.Generic;
using M = Cnaws.Passport.Modules;
namespace XcpNet.Supplier.Controllers
{
    public sealed class Supplier : SupplierController
    {
        [Supplier(true)]
        public void Index()
        {
            City city;
            using (IPArea area = new IPArea())
            {
                IPLocation local = area.Search(ClientIp);
                using (Country country = Country.GetCountry())
                {
                    city = local.GetCity(country);
                    this["GetCityName"] = new FuncHandler((args) =>
                    {
                        return country.GetCity(Convert.ToInt32(args[0]));
                    });
                }
            }
            this["Location"] = city != null ? city.Id : 441900;
            Country country1 = Country.GetCountry();
            this["GetCityName"] = new FuncHandler((args) =>
            {
                return country1.GetCity(Convert.ToInt32(args[0])).Name;
            });
            if (IsSupplier())
            {
                Render("supplier/supplier.html");
            }
            else
            {
                Render("supplier.html");   
            }
         
        }
        [Distributor]
        public void List(string state = "_", int page = 1)
        {
            this["State"] = state == null ? "_" : state;
            state = state == "_" ? "" : state;

            long count;
            DbWhereQueue where = new DbWhereQueue();
            where = new DbWhere<P.Supplier>("Subjection", Distributor.UserId);
            if (!string.IsNullOrEmpty(state))
                where &= new DbWhere<P.Supplier>("State", state);
            IList<DataJoin<P.Supplier,M.Member>> List = Db<P.Supplier>.Query(DataSource).Select(new DbSelect<P.Supplier>(), new DbSelect<M.Member>("Name"), new DbSelect<M.Member>("Mobile"), new DbSelect<M.Member>("NickName"))
                .InnerJoin(new DbColumn<P.Supplier>("UserId"), new DbColumn<M.Member>("Id"))                
                .Where(where)
                .OrderBy(new DbOrderBy<P.Supplier>("UserId",DbOrderByType.Desc)).ToList<DataJoin<P.Supplier,M.Member>>(15, Math.Max(1, page), out count);
            new SplitPageData<DataJoin<P.Supplier, M.Member>>(Math.Max(1, page), 15, List, count, 8);
            SplitPageData<DataJoin<P.Supplier, M.Member>> list = new SplitPageData<DataJoin<P.Supplier, M.Member>>(Math.Max(1, page), 15, List, count, 8);
            this["SupplierList"] = list;
            Render("supplier_list.html");
        }
        [HttpAjax]
        [Distributor]
        public void Info(long userid)
        {
            this["SupplierInfo"] = P.Supplier.GetById(DataSource, userid);
            Render("supplier_info.html");
        }
        [Distributor]
        public void Edit()
        {
            Render("supplier_open.html");
        }
        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void OnEdit()
        {
            try
            {
                DataSource.Begin();
                try
                {
                    Cnaws.Passport.Modules.Member member = DbTable.Load<Cnaws.Passport.Modules.Member>(Request.Form);
                    member.ParentId = 0;
                    member.Approved = true;
                    member.CreationDate = DateTime.Now;
                    if (member.Insert(DataSource) != DataStatus.Success)
                        throw new Exception();
                    string images = Request["Images"];

                    P.Supplier value = DbTable.Load<P.Supplier>(Request.Form);
                    value.Images = Request["Images"].Replace(',', '|');
                    value.SupplierType = 1;
                    value.Categories = Request["Categories"];
                    value.Subjection = User.Identity.Id;
                    value.UserId = member.Id;
                    value.Images = HttpUtility.UrlDecode(value.Images);
                    value.CreationDate = member.CreationDate;
                    if (value.Insert(DataSource) != DataStatus.Success)
                        throw new Exception();

                    DataSource.Commit();
                    SetResult(true);
                }
                catch (Exception)
                {
                    DataSource.Rollback();
                    throw;
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
        [HttpAjax]
        [HttpPost]
        [Supplier]
        public void Submit()
        {
            try
            {
                P.Supplier value = DbTable.Load<P.Supplier>(Request.Form);
                value.UserId = User.Identity.Id;
                value.Images = HttpUtility.UrlDecode(value.Images);
                value.Province = int.Parse(Request.Form["area_provinces"]);
                value.City = int.Parse(Request.Form["area_cities"]);
                value.County = int.Parse(Request.Form["area_counties"]);
                value.Categories = value.Categories.Trim(',');
                SetResult(value.UpdateWithState(DataSource));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
        public void Join()
        {
            Render("supplier_join.html");
        }

        [SupplierOrDistributor(true)]
        public void Money(long parent = 0, long index = 1)
        {
            if (parent <= 0)
                parent = User.Identity.Id;
            this["ParentId"] = parent;
            string orderid = Request["OrderId"];
            string begindt = Request["BeginTime"];
            string endtime = Request["EndTime"];
            int type = -1;
            int.TryParse(Request["profitType"], out type);
            this["Profit"] = new
            {
                ArrivalMoney = A.ProfitRecord.GetArrivalMoney(DataSource, parent),
                FreezeMoney = Cnaws.Passport.Modules.MemberDrawOrder.GetInTreatmentMoney(DataSource, parent),
                HoustonFreeze = A.ProfitRecord.GetHoustonFreezeMoney(DataSource, parent),
                AllHoustonMoney = A.ProfitRecord.GetAllHoustonMoney(DataSource, parent),
                PendingAudit = Cnaws.Passport.Modules.MemberDrawOrder.GetPendingAuditMoney(DataSource, parent)
            };
            this["Attr"] = Request.Url.Query;
            // M.Distributor.GetMoneyByParent(DataSource, parent, Math.Max(1, index), 20, 11);
            this["MoneyList"] = A.ProfitRecord.GetListByUser(DataSource, parent, orderid, begindt, endtime, type, Math.Max(1, index), 10, 8);
            this["ProfigRecord"] = A.ProfitRecord.GetListCountByUser(DataSource, parent, orderid, begindt, endtime, type);

            this["OrderId"] = orderid;
            this["BeginTime"] = begindt;
            this["EndTime"] = endtime;
            this["profitType"] = type;
            Render("Supplier/money.html");
        }
    }
}
