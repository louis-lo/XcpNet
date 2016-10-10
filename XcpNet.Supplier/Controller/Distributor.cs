using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Product.Modules;
using P = Cnaws.Passport.Modules;
using System.Web;
using Cnaws.Area;
using A = XcpNet.AfterSales.Modules;
using Cnaws.Sms;
using Cnaws.Sms.Modules;
using XcpNet.Services.Sms;
using Cnaws.Web.Templates;
using System.Linq;
using System.Collections.Generic;
using Cnaws.Data.Query;

namespace XcpNet.Supplier.Controllers
{
    public sealed class Distributor : SupplierController
    {
        [Distributor(true)]
        public void Index()
        {

            //City city;
            ////using (IPArea area = new IPArea())
            //{
            //    IPLocation local = area.Search(ClientIp);
            //    using (Country country = Country.GetCountry())
            //    {
            //        city = local.GetCity(country);
            //        this["GetCityName"] = new FuncHandler((args) =>
            //        {
            //            return country.GetCity(Convert.ToInt32(args[0]));
            //        });
            //    }
            //}
            //this["Location"] = city != null ? city.Id : 441900;
            Country country1 = Country.GetCountry();
            this["GetCityName"] = new FuncHandler((args) =>
            {
                return country1.GetCity(Convert.ToInt32(args[0])).Name;
            });
            Render("distributor.html");
        }

        [HttpAjax]
        [HttpPost]
        [Distributor]
        public void Submit()
        {
            try
            {
                M.Distributor value = DbTable.Load<M.Distributor>(Request.Form);
                value.UserId = User.Identity.Id;
                value.Images = HttpUtility.UrlDecode(value.Images);
                value.Province = int.Parse(Request.Form["area_provinces"]);
                value.City = int.Parse(Request.Form["area_cities"]);
                value.County = int.Parse(Request.Form["area_counties"]);
                SetResult(value.UpdateWithState(DataSource));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [Distributor(true, 0, 1, 2, 3)]
        public void Account()
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

            Render("account.html");
        }
        [HttpAjax]
        [HttpPost]
        [Distributor(0, 1, 2, 3)]
        public void CreateAccount()
        {
            try
            {
                DataSource.Begin();
                try
                {
                    P.Member member = DbTable.Load<P.Member>(Request.Form);
                    member.ParentId = 0;
                    member.Approved = true;
                    member.CreationDate = DateTime.Now;
                    if (member.Insert(DataSource) != DataStatus.Success)
                        throw new Exception();

                    M.Distributor value = DbTable.Load<M.Distributor>(Request.Form);
                    value.UserId = member.Id;
                    value.ParentId = User.Identity.Id;
                    value.State = M.DistributorState.NotApproved;
                    value.Images = HttpUtility.UrlDecode(value.Images);
                    value.Province = int.Parse(Request.Form["area_provinces"]);
                    value.City = int.Parse(Request.Form["area_cities"]);
                    value.County = int.Parse(Request.Form["area_counties"]);
                    value.CreationDate = member.CreationDate;
                    value.Consignee = value.Signatories;
                    long.TryParse(value.SignatoriesPhone, out value.Mobile);
                    if (value.Insert(DataSource) != DataStatus.Success)
                        throw new Exception();

                    DataSource.Commit();
                    SetResult(true);
                    try
                    {
                        //发送短信
                        SendMsg(
                            string.IsNullOrEmpty(value.ContactPhone) ? value.SignatoriesPhone : value.ContactPhone,
                            value.GetLevelString(),
                            string.IsNullOrEmpty(value.Contact) ? value.Signatories : value.Contact,
                            member.Name, Request["Password"]);
                    }
                    catch (Exception) { }
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
        #region
        [Distributor(true, 0, 1, 2, 3)]
        public void Contacts(long parent = 0)
        {
            if (parent <= 0)
                parent = User.Identity.Id;
            this["ParentId"] = parent;
            this["DistributorList"] = M.Distributor.GetContactByParent(DataSource, parent);
            Render("contacts.html");

        }
        [Distributor(true, 0, 1, 2, 3)]
        public void accountStatus(long parent = 0, long index = 1)
        {
            if (parent <= 0)
                parent = User.Identity.Id;
            this["ParentId"] = parent;
            //SplitPageData<M.Distributor> distributor = M.Distributor.GetPageByParent(DataSource, parent, Math.Max(1, index), 20, 11);
            ////this["DistributorList"] = distributor;
            //////M.Distributor dis = M.Distributor.GetUserIdByParent(DataSource, parent);
            //long[] ids = distributor.Data.Select(x => x.UserId).ToArray();
            //IList<P.Member> members = P.Member.GetByIds(DataSource, ids);
            //this["GetNames"] = new FuncHandler((args) =>
            //{
            //    return members.Where(x => x.Id == Convert.ToInt64(args[0])).First().Name;
            //    //return P.Member.GetById(DataSource, Convert.ToInt64(args[0])).Name;
            //});
            //连表查询Distributor表所有数据和Member表Name值，在控制器中要new
            long count;
            IList<Cnaws.Data.DataJoin<M.Distributor, P.Member>> list =
                Db<M.Distributor>.Query(DataSource)
                .Select(new DbSelect<M.Distributor>(), new DbSelect<P.Member>("*"))
                .InnerJoin(new DbColumn<M.Distributor>("UserId"), new DbColumn<P.Member>("Id"))
                .Where(new DbWhere<M.Distributor>("ParentId", parent))
                .OrderBy(new DbOrderBy<M.Distributor>("CreationDate", DbOrderByType.Desc))
                .ToList<Cnaws.Data.DataJoin<M.Distributor, P.Member>>(14, Math.Max(1, index), out count);
            this["DistributorList"] = new SplitPageData<Cnaws.Data.DataJoin<M.Distributor, P.Member>>(Math.Max(1, index), 14, list, count, 8);

            Render("accountStatus.html");
        }
        [Distributor(true, 0, 1, 2, 3)]
        public void statusDetails(long id)
        {
            this["DistributorList"] = M.Distributor.GetById(DataSource, id);
            Render("statusDetails.html");
        }
        [Distributor(true, 0, 1, 2, 3)]
        public void MemberLevel(long parent, long index = 1)
        {
            if (parent < 0)
                parent = User.Identity.Id;
            this["ParentId"] = parent;
            this["Level"] = M.Distributor.GetLevelByUserId(DataSource, User.Identity.Id);
            this["DistributorList"] = M.Distributor.GetPageByParent(DataSource, parent, Math.Max(1, index), 4, 8);
            Render("memberlevel.html");
        }
        [Distributor(true, 0, 1, 2, 3)]
        public void MyMember(long parent, long index = 1)
        {
            if (parent <= 0)
                parent = User.Identity.Id;
            this["ParentId"] = parent;
            long count;
            IList<Cnaws.Data.DataJoin<M.Distributor, P.Member>> list =
               Db<M.Distributor>.Query(DataSource)
               .Select(new DbSelect<M.Distributor>(), new DbSelect<P.Member>("Name"))
               .InnerJoin(new DbColumn<M.Distributor>("UserId"), new DbColumn<P.Member>("Id"))
               .Where(new DbWhere<M.Distributor>("ParentId", parent))
               .OrderBy(new DbOrderBy<M.Distributor>("CreationDate", DbOrderByType.Desc))
               .ToList<Cnaws.Data.DataJoin<M.Distributor, P.Member>>(14, Math.Max(1, index), out count);
            this["DistributorList"] = new SplitPageData<Cnaws.Data.DataJoin<M.Distributor, P.Member>>(Math.Max(1, index), 14, list, count, 8);
            Render("mymember.html");

        }

        #endregion
        [Distributor(true, 0, 1, 2, 3,4)]
        public new void Member(long parent = 0, long index = 1)
        {
            if (parent <= 0)
                parent = User.Identity.Id;
            this["ParentId"] = parent;
            this["Level"] = M.Distributor.GetLevelByUserId(DataSource, User.Identity.Id);
            this["DistributorList"] = M.Distributor.GetPageByParent(DataSource, parent, Math.Max(1, index), 20, 8);
            Render("member.html");
        }

        [SupplierOrDistributor(true)]
        public void Money(long parent = 0, long index = 1)
        {
            if (parent <= 0)
                parent = User.Identity.Id;
            this["ParentId"] = parent;
            string orderid = Request["OrderId"];
            string begindt= Request["BeginTime"];
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
            this["ProfigRecord"] = A.ProfitRecord.GetListCountByUser(DataSource, parent, orderid, begindt, endtime,type);

            this["OrderId"] = orderid;
            this["BeginTime"] = begindt;
            this["EndTime"] = endtime;
            this["profitType"] = type;
            Render("money.html");
        }

        public void SearchMoney()
        {
            Render("searchmoney.html");
        }

        [Distributor(true)]
        public void Cash()
        {
            this["Cash"] = M.Distributor.GetCash(DataSource, User.Identity.Id);
            Render("cash.html");
        }

        [Distributor(true)]
        public void Check()
        {
            Render("check.html");
        }

        public void Join()
        {
            Render("distributor_join.html");
        }

        /// <summary>
        /// 发送短信通知
        /// </summary>
        private void SendMsg(string mobile, string level, string surName, string loginName, string pwd)
        {
            long mob = 0;
            long.TryParse(mobile, out mob);
            if (mob > 0)
            {
                if (!string.IsNullOrEmpty(surName)) surName = surName.Substring(0, 1);

                SmsMobset.Send(
                         DataSource,
                         mob,
                         SmsTemplate.DistributorRegistered,
                         surName,
                         level,
                         loginName,
                         pwd);
            }
        }

        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        [Distributor(1)]        
        public void Approved()
        {
            try {
                long UserId = long.Parse(Request.Form["Id"]);
                int State = int.Parse(Request.Form["State"]);
                SetResult(M.Distributor.Approved(DataSource, UserId, (Cnaws.Product.Modules.DistributorState)Enum.Parse(typeof(Cnaws.Product.Modules.DistributorState), State.ToString())));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
    }
}
