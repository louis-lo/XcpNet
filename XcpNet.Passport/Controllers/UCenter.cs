using System;
using Cnaws.Web;
using M = Cnaws.Passport.Modules;
using Cnaws.Web.Templates;
using System.Collections.Generic;
using PM = Cnaws.Product.Modules;
using Cnaws.Data;
using XcpNet.Common;
using V = Cnaws.Verification.Modules;

namespace XcpNet.Passport.Controllers.Extension
{
    public class UCenter : Common.CommPassportController
    {
        [Authorize(true)]
        public void Index()
        {
            if (IsWap)
            {
                PM.Distributor dis = PM.Distributor.GetById(DataSource, User.Identity.Id);
                if (dis != null)
                {
                    if (dis.State == PM.DistributorState.NotApproved)
                    {
                        Render(@"<!doctype html>
<html>
<head>
<meta charset=""utf-8"">
<title>跳转</title>
<meta http-equiv=""refresh"" content=""0; url=$Site.SupplierUrl$url('/joinus/wait')"" />
</head>
<body style=""font-size:14px;padding:10px;text-align:center;"">
如果您的浏览器没有跳转，请<a href=""$Site.SupplierUrl$url('/joinus/wait')"">点击这里</a>。
</body>
</html>", "ucenter.html");
                    }
                    else
                    {
                        Render(@"<!doctype html>
<html>
<head>
<meta charset=""utf-8"">
<title>跳转</title>
<meta http-equiv=""refresh"" content=""0; url=$Site.SupplierUrl"" />
</head>
<body style=""font-size:14px;padding:10px;text-align:center;"">
如果您的浏览器没有跳转，请<a href=""$Site.SupplierUrl"">点击这里</a>。
</body>
</html>", "ucenter.html");
                    }
                    return;
                }
                else
                {
                    Render(@"<!doctype html>
<html>
<head>
<meta charset=""utf-8"">
<title>跳转</title>
<meta http-equiv=""refresh"" content=""0; url=$Site.SupplierUrl"" />
</head>
<body style=""font-size:14px;padding:10px;text-align:center;"">
如果您的浏览器没有跳转，请<a href=""$Site.SupplierUrl"">点击这里</a>。
</body>
</html>", "ucenter.html");
                }
                return;

            }

            IList<DataJoin<PM.ProductCart, PM.Product>> list = PM.ProductCart.GetPageByUser(DataSource, User.Identity.Id);
            this["GetImg"] = new FuncHandler((args) =>
            {
                if (args[0] != null)
                    return Convert.ToString(args[0]).Split('|')[0];
                return "";
            });
            this["Member"] = M.MemberInfo.GetById(DataSource, User.Identity.Id);
            this["OrderList"] = Cnaws.Product.Modules.ProductOrder.GetPageByUserAndState(DataSource, User.Identity.Id, "_", 1, 3, 8);
            this["cartList"] = list;
            Render("ucenter.html");
        }
        /// <summary>
        /// 发货短信
        /// </summary>
        /// <param name="name">yuntongxun</param>
        [Authorize(true)]
        [HttpPost]
        public void SendSms(string name)
        {
            object code, data;
            try
            {
                int SmsType = 0; int.TryParse(Request["SmsType"], out SmsType);//0为注册类,1为密码类
                code = new CommPassport(this).SendSms(DataSource, long.Parse(Request.Form["Mobile"]), name, SmsType, ClientIp, out data);
                new CommUtility(this).CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }

        /// <summary>
        /// 验证短信
        /// </summary>
        /// <param name="name">yuntongxun</param>
        [HttpPost]
        public void CheckSMS()
        {
            try
            {
                int SmsType = 0; int.TryParse(Request["SmsType"], out SmsType);//0为注册类,1为密码类
                if (!V.MobileHash.CheckAndNotOperation(DataSource, long.Parse(Request.Form["Mobile"]), SmsType, Request.Form["SmsCaptcha"]))
                {
                    SetResult(CommUtility.SMSCAPTCHA_EQUALS);
                    throw new AggregateException();
                }
                SetResult(CommUtility.SUCCESS);
            }
            catch (AggregateException) { return; }
            catch (Exception ex)
            {
                SetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }
    }
}
