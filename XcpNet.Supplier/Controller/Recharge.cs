using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Passport.Modules;
using Cnaws.Pay;
using Cnaws.Pay.Controllers;
using Cnaws.Pay.Modules;
using System.Text.RegularExpressions;
using Cnaws;
using P = Cnaws.Product.Modules;
using System.Collections.Generic;
using System.Web;

namespace XcpNet.Supplier.Controllers.Extension
{
    public sealed class Recharge : Cnaws.Passport.Controllers.Recharge
    {
        public bool IsSupplier()
        {
            return XcpUtility.GetValue(PassportAuthentication.GetCustomCookie(XcpUtility.SupplierCookieName, Context)).Key;
        }
        public bool IsDistributor()
        {
            return XcpUtility.GetValue(PassportAuthentication.GetCustomCookie(XcpUtility.SupplierCookieName, Context)).Value;
        }
        public bool IsSupplierOrDistributor()
        {
            KeyValuePair<bool, bool> pair = XcpUtility.GetValue(PassportAuthentication.GetCustomCookie(XcpUtility.SupplierCookieName, Context));
            return pair.Key || pair.Value;
        }

        public P.Supplier Supplier
        {
            get
            {
                return P.Supplier.GetById(DataSource, User.Identity.Id);
            }
        }
        public P.Distributor Distributor
        {
            get
            {
                return P.Distributor.GetById(DataSource, User.Identity.Id);
            }
        }

        protected override void OnInitController()
        {
            if (IsSupplier())
                this["Supplier"] = Supplier;
            if (IsDistributor())
                this["Distributor"] = Distributor;
        }

        protected override void Unauthorized(bool redirect = false)
        {
            if (IsHtml())
                Response.Write(string.Concat("<script type=\"text/javascript\">alert('权限不足');window.location.href='", GetPassportUrl("/logout"), "?target=", HttpUtility.UrlEncode(string.Concat(GetPassportUrl("/login"), "?target=", Request.Url.ToString())), "';</script>"));
            else
                SetResult(-401);
        }

        protected override void OnIndex()
        {
            this["Member"] = P.Distributor.GetById(DataSource, User.Identity.Id);
            Render("recharge.html");
        }
        protected override IPayOrder GetPayOrder(string provider)
        {
            string openId = null;
            M.OAuth2Member member = M.OAuth2Member.GetByUserPay(DataSource, provider, User.Identity.Id);
            if (member != null)
                openId = member.UserId;
            return PayRecord.Create(DataSource, User.Identity.Id, openId, "充值", provider, Money.Parse(Request.Form["Money"]), 5, string.Empty);
        }

        protected override bool OnModifyMoney(PayProvider provider, PaymentType payment, long user, Money money, string trade, string title, int type, string targetId)
        {
            try
            {
                if (payment == PaymentType.Pay)
                    return ModifyMoney(DataSource, user, money, title, type, trade) == DataStatus.Success;
            }
            catch (Exception) { }
            return false;
        }

        private static DataStatus ModifyMoney(DataSource ds, long id, Money value, string title, int type = 0, string targetId = null)
        {
            if (value == 0)
                return DataStatus.Success;
            ds.Begin();
            try
            {
                DataStatus status = (new M.MoneyRecord()
                {
                    MemberId = id,
                    Title = title,
                    Type = type,
                    TargetId = targetId,
                    Value = value,
                    CreationDate = DateTime.Now
                }).Insert(ds);
                if (status != DataStatus.Success)
                    throw new Exception();
                DataWhereQueue where = new DataParameter("UserId", id);
                if (value < 0)
                    where = new DataNameWhere("Money", -value, "OldMoney", ">=") & where;
                status = (new P.Distributor() { UserId = id }).Update(ds, ColumnMode.Include, new DataColumn[] { new DataModifiedColumn<decimal>("Money", value) }, where);
                if (status != DataStatus.Success)
                    throw new Exception();
                ds.Commit();
                return DataStatus.Success;
            }
            catch (Exception)
            {
                ds.Rollback();
                return DataStatus.Rollback;
            }
        }
    }
}
