using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Passport.Modules;
using Cnaws.Pay;
using Cnaws.Pay.Controllers;
using Cnaws.Pay.Modules;
using System.Text.RegularExpressions;
using Cnaws;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace XcpNet.Passport.Controllers.Extension
{
    public class Recharge : Common.CommonPayController
    {
        private static readonly Regex MicroMessengerRegex = new Regex(@"MicroMessenger", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string PayProvider
        {
            get
            {
                if (IsWap)
                {
                    Match m = MicroMessengerRegex.Match(Request.UserAgent);
                    if (m.Success)
                        return "wxpay";
                    return "alipaymobile";
                }
                return "alipaydirect";
            }
        }
        public string PayName
        {
            get { return LoadProvider(PayProvider).Name; }
        }

        protected override void OnIndex()
        {
            this["Member"] = M.MemberInfo.GetByRecharge(DataSource, User.Identity.Id);
            Render("recharge.html");
        }

        protected override bool CheckProvider(PayProvider provider)
        {
            return provider.IsOnlinePay;
        }
        protected override IPayOrder GetPayOrder(string provider)
        {
            string openId = null;
            long UserId = User.Identity.Id;
            if (UserId == 0)
            {
                if (!string.IsNullOrEmpty(Request["mark"]) && !string.IsNullOrEmpty(Request["token"]))
                {
                    Guid token;
                    M.Member newmember;
                    if (!Guid.TryParse(Request["token"], out token) || Guid.Empty.Equals(token))
                    {
                        newmember = null;
                    }
                    newmember = M.Member.GetByToken(DataSource, token);
                    if (newmember != null)
                    {
                        string mark = Request["mark"];
                        if (string.Equals(mark, newmember.Mark) || token.Equals(newmember.Token))
                        {
                            UserId = newmember.Id;
                        }
                    }
                }
            }
            PayRecord record=null;
            if (!string.IsNullOrEmpty(Request["Id"]))
            {
                record = PayRecord.GetById(DataSource, Request["Id"], PaymentType.Pay);                
            }
            if (record == null)
            {
                M.OAuth2Member member = M.OAuth2Member.GetByUserPay(DataSource, provider, UserId);
                if (member != null)
                    openId = member.UserId;
                record = PayRecord.Create(DataSource, User.Identity.Id, openId, "充值", provider, Money.Parse(Request.Form["Money"]), 0, string.Empty);
            }
            return record;
        }

        protected override void OnMakeQR(string url,string orderid = "")
        {
            if (!IsAjax && string.IsNullOrEmpty(Request["token"]))
            {
                this["OrderId"] = orderid;
                Money TotalMoney = 0;
                TotalMoney = Money.Parse(Request.Form["Money"]);
                string path = url;
                Bitmap image = XcpNet.Passport.QRCode.MarkQRCode.Create_ImgCode(path, 10);
                using (MemoryStream ms = new MemoryStream())
                {
                    XcpNet.Passport.QRCode.MarkQRCode.CombinImage(image, Server.MapPath("~/logo.png")).Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    this["QrUrl"] = string.Concat("data:image/png;base64,", Convert.ToBase64String(ms.ToArray()));
                }
                this["Money"] = TotalMoney;
                Render("makeqr.html");
            }
            else
            {
                base.OnMakeQR(url,orderid);
            }
        }

        protected override bool OnModifyMoney(PayProvider provider, PaymentType payment, long user, Money money, string trade, string title, int type, string targetId)
        {
            try
            {
                if (payment == PaymentType.Pay)
                    return M.MemberInfo.ModifyMoney(DataSource, user, money, title, type, trade) == DataStatus.Success;
            }
            catch (Exception) { }
            return false;
        }
        protected override void OnError(string message)
        {
            SetResult(false, message);
        }
    }
}
