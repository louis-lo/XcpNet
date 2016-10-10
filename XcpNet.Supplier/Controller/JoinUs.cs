using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Product.Modules;
using PM = Cnaws.Passport.Modules;
using System.Collections.Generic;

namespace XcpNet.Supplier.Controllers
{
    public sealed class JoinUs : SupplierController
    {
        [Authorize]
        public void Index()
        {
            PM.MemberInfo memberinfo = PM.MemberInfo.GetById(DataSource, User.Identity.Id);
            M.Distributor dis = M.Distributor.GetById(DataSource, User.Identity.Id);
            this["Member"] = memberinfo;
            Render("join.html");
        }
        [Authorize]
        public void Submit()
        {
            DataSource.Begin();
            try
            {
                M.Distributor distributor = DbTable.Load<M.Distributor>(Request.Form);
                distributor.UserId = User.Identity.Id;
                distributor.Signatories = distributor.Consignee;
                distributor.SignatoriesPhone = distributor.Mobile.ToString();
                distributor.State = Cnaws.Product.Modules.DistributorState.NotApproved;
                distributor.PostId = 0;
                distributor.CreationDate = DateTime.Now;
                distributor.Province = int.Parse(Request.Form["area_provinces"]);
                distributor.City = int.Parse(Request.Form["area_cities"]);
                distributor.County = int.Parse(Request.Form["area_counties"]);
                M.Distributor Countydistributor = M.Distributor.GetByAreaAndLevel(DataSource, distributor.Province, distributor.City, distributor.County, 1);
                if (Countydistributor != null)
                    distributor.ParentId = Countydistributor.UserId;
                else
                    distributor.ParentId = 0;
                distributor.Level = 100;
                if (distributor.Insert(DataSource) != DataStatus.Success)
                    throw new Exception();
                bool isDistributor = M.Distributor.IsDistributor(DataSource, distributor.UserId);
                bool isSupplier = false;
                PassportAuthentication.SetCustomCookie(XcpUtility.SupplierCookieName, XcpUtility.GetBytes(new KeyValuePair<bool, bool>(isSupplier, isDistributor)), Context);
                DataSource.Commit();
                SetResult(true);
            }
            catch (Exception)
            {
                DataSource.Rollback();
                SetResult(false);
            }
        }


        [Distributor(true)]
        public void Wait()
        {
            M.Distributor distributor = M.Distributor.GetById(DataSource, User.Identity.Id);
            if (distributor.State == Cnaws.Product.Modules.DistributorState.NotApproved)
            {
                PM.MemberInfo member = PM.MemberInfo.GetById(DataSource, User.Identity.Id);
                this["Member"] = member;
                Render("wait.html");
            }
            else
            {
                Redirect(GetUrl("/"));
            }
        }
        public void NotAuthorized()
        {
            PM.MemberInfo member = PM.MemberInfo.GetById(DataSource, User.Identity.Id);
            this["Member"] = member;
            Render("notauthorized.html");
        }

        protected override string GetLoginUrl()
        {
            return GetUrl("/joinus");
        }
    }
}
