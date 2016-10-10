using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Pay;
using System.Collections.Generic;
using System.Threading;
using A = XcpNet.Ad.Modules;
using Py = Cnaws.Pay.Modules;
using Cnaws.Data.Query;
using C = Cnaws.Comment.Modules;
using XcpNet.Common;
namespace XcpNet.ApiSecond.Controllers
{
    public sealed class LedBuy2 : Buy2
    {
        protected override void OnInitController()
        {
        }

        protected override void Dispose(bool disposing)
        {

            base.Dispose(disposing);
        }

        public void ToPayByOrder(string orderId, int type = 0)
        {
            M.Member member;
            if (CheckMember(out member))
            {
                Cnaws.Web.PassportAuthentication.SetAuthCookie(true, false, member);
                //string PostUrl=GetPassportUrl("/buy/submit/alipayqr");
                string PostUrl;
                if (type == 0)
                    PostUrl = "http://wappassnew.xcpnet.com/buy/submit/alipayqr.html";
                else
                    PostUrl = "http://wappassnew.xcpnet.com/recharge/submit/alipayqr.html";
                //string PostUrl = "http://localhost:1879/buy/submit/alipayqr.html";
                Response.Clear();
                Response.Write("<html><head>");
                Response.Write(string.Format("</head><body onload=\"document.{0}.submit()\">", "payform"));
                Response.Write(string.Format("<form name=\"{0}\" method=\"{1}\" action=\"{2}\" >", "payform", "POST", PostUrl));
                Response.Write(string.Format("<input name=\"{0}\" type=\"hidden\" value=\"{1}\">", "Id", orderId));
                Response.Write("</form>");
                Response.Write("</body></html>");
                Response.End();
            }
        }
#if (DEBUG)
        public static void ToPayByOrderHelper()
        {
            CheckMemberApi(ClassName, "ToPayByOrder/{订单号}/{支付类型}", "支付地址,直接访问打开，支付类型0为商品订单1为充值订单")
                .AddResult(true, typeof(string), "直接跳转支付页面");
        }
#endif
    }
}
