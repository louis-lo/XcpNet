using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Passport.Modules;
using Cnaws.ExtensionMethods;
using Cnaws.Web.Configuration;
using V = Cnaws.Verification.Modules;
using Cnaws.Data.Query;
using System.Text.RegularExpressions;
using XcpNet.Common;

namespace XcpNet.Passport.Controllers.Extension
{
    public class Security : Common.CommPassportController
    {
        [Authorize(true)]
        public void Index(string type = null, int step = 1)
        {
            M.MemberInfo member = M.MemberInfo.GetBySecurity(DataSource, User.Identity.Id);
            this["Member"] = member;
            this["IsPayPsw"] = !string.IsNullOrEmpty(member.PayPassword);
            this["IsVerMob"] = member.VerMob;
            Render("security.html");
        }

        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void Submit(string type, int step)
        {
            try
            {
                switch (type.ToLower())
                {
                    case "password":
                        {
                            switch (step)
                            {
                                case 1:
                                    {
                                        string p1 = Request.Form["Password"];
                                        string p2 = M.Member.GetPasswordById(DataSource, User.Identity.Id);
                                        if (!string.IsNullOrEmpty(p1) && !string.IsNullOrEmpty(p2))
                                        {
                                            p1 = p1.MD5();
                                            if (p2.Equals(p1))
                                            {
                                                Response.Cookies["CNAWS.PASSPORT.OLDPASSWORD"].Value = p1;
                                                SetResult(true);
                                            }
                                            else
                                            {
                                                SetResult(false);
                                            }
                                        }
                                        else
                                        {
                                            SetResult(false);
                                        }
                                    }
                                    break;
                                case 2:
                                    {
                                        System.Web.HttpCookie cookie = Request.Cookies["CNAWS.PASSPORT.OLDPASSWORD"];
                                        if (cookie != null)
                                        {
                                            string password = M.Member.GetPasswordById(DataSource, User.Identity.Id);
                                            if (string.Equals(password, cookie.Value))
                                                SetResult((new M.Member() { Id = User.Identity.Id, Password = Request.Form["Password"] }).Update(DataSource, ColumnMode.Include, "Password") == DataStatus.Success);
                                            else
                                                SetResult((int)M.LoginStatus.PasswordError);
                                        }
                                        else
                                        {
                                            SetResult((int)M.LoginStatus.PasswordError);
                                        }
                                    }
                                    break;
                                case 3:
                                    {
                                        string old = Request.Form["OldPassword"];
                                        string pwd = Request.Form["NewPassword"];
                                        string opwd = M.Member.GetPasswordById(DataSource, User.Identity.Id);
                                        if (!string.IsNullOrEmpty(old) && !string.IsNullOrEmpty(pwd) && !string.IsNullOrEmpty(opwd))
                                        {
                                            old = old.MD5();
                                            if (opwd.Equals(old))
                                            {
                                                SetResult((new M.Member() { Id = User.Identity.Id, Password = pwd }).Update(DataSource, ColumnMode.Include, "Password") == DataStatus.Success);
                                            }
                                            else
                                            {
                                                SetResult(-1002);
                                            }
                                        }
                                        else
                                        {
                                            SetResult(false);
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    case "email":
                        {
                            switch (step)
                            {
                                case 1:
                                    {

                                    }
                                    break;
                                case 2:
                                    {

                                    }
                                    break;
                                case 3:
                                    {

                                    }
                                    break;
                            }
                        }
                        break;
                    case "phone":
                        {
                            switch (step)
                            {
                                case 1:
                                    {
                                        long mobile;
                                        if (long.TryParse(Request.Form["Mobile"], out mobile))
                                        {
                                            //判断验证码是否正确
                                            if (!V.MobileHash.Equals(DataSource, mobile, V.MobileHash.Password, Request.Form["Code"]))
                                            {
                                                SetResult(-1002);
                                            }
                                            else
                                            {
                                                M.MemberInfo memberInfo = M.MemberInfo.GetBySecurity(DataSource, User.Identity.Id);
                                                //判断是否已绑定过手机号码，绑定过验证支付密码是否正确
                                                if (memberInfo.VerMob)
                                                {
                                                    if (string.Equals(Request.Form["PayPass"].MD5(), memberInfo.PayPassword))
                                                    {
                                                        if (Db<M.MemberInfo>.Query(DataSource).Update().Set("Mobile", mobile).Where(new DbWhereQueue("Id", User.Identity.Id)).Execute() > 0)
                                                        {
                                                            SetResult(DataStatus.Success);
                                                        }
                                                        else
                                                        {
                                                            SetResult(DataStatus.Failed);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        SetResult(-1003);
                                                    }
                                                }
                                                else
                                                {
                                                    if (Db<M.MemberInfo>.Query(DataSource).Update().Set("VerMob", true).Set("Mobile", mobile).Where(new DbWhereQueue("Id", User.Identity.Id)).Execute() > 0)
                                                    {
                                                        SetResult(DataStatus.Success);
                                                    }
                                                    else
                                                    {
                                                        SetResult(DataStatus.Failed);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            SetResult(DataStatus.Failed);
                                        }
                                    }
                                    break;
                                case 2:
                                    {

                                    }
                                    break;
                                case 3:
                                    {

                                    }
                                    break;
                            }
                        }
                        break;
                    case "paypassword":
                        {
                            switch (step)
                            {
                                case 1:
                                    {
                                        try
                                        {
                                            string pwd = Request.Form["PayPassword"];
                                            M.Member member = M.Member.GetById(DataSource, User.Identity.Id);
                                            if (!V.MobileHash.Equals(DataSource, member.Mobile, 1, Request.Form["setSmsCaptcha"]))
                                            {
                                                SetResult(CommUtility.SMSCAPTCHA_EQUALS);
                                                throw new AggregateException();
                                            }
                                            else
                                            {
                                                SetResult((new M.MemberInfo() { Id = User.Identity.Id, PayPassword = pwd }).Update(DataSource, ColumnMode.Include, "PayPassword") == DataStatus.Success);
                                            }
                                        }
                                        catch (AggregateException)
                                        {
                                            return;
                                        }
                                        catch (Exception)
                                        {
                                            SetResult(CommUtility.PROGRAM_ERROR);
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    case "validpaypassword":
                        {
                            switch (step)
                            {
                                case 1:
                                    {
                                        try
                                        {
                                            M.MemberInfo memberInfo = M.MemberInfo.GetBySecurity(DataSource, User.Identity.Id);
                                            if (string.Equals(Request.Form["PayPass"].MD5(), memberInfo.PayPassword))
                                            {
                                                SetResult(DataStatus.Success);
                                            }
                                            else
                                            {
                                                SetResult(DataStatus.Failed);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            SetResult(CommUtility.PROGRAM_ERROR);
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [Authorize(true)]
        public void Default(string type, int step = 1)
        {
            M.MemberInfo member = M.MemberInfo.GetBySecurity(DataSource, User.Identity.Id);
            string mobile = member.Mobile.ToString();
            this["Member"] = member;
            if (mobile.Length > 7)
                mobile = mobile.Remove(3, 4).Insert(3, "****");
            this["Mobile"] = mobile;

            Render(string.Concat(type, step, ".html"));
        }

        [Authorize(true)]
        public void UpdatePhone()
        {
            this["Sms"] = SMSCaptchaSection.GetSection();
            this["Member"] = M.MemberInfo.GetBySecurity(DataSource, User.Identity.Id);
            Render("update_phone.html");
        }

        [Authorize(true)]
        public void setuname()
        {
            this["Member"] = M.MemberInfo.GetByModify(DataSource, User.Identity.Id);
            Render("setuname.html");
        }
        [Authorize]
        public void SubmitSetName()
        {
            Regex regex = new Regex(@"^[\u4e00-\u9fa50-9a-zA-Z_\-]{4,20}$");
            if (regex.IsMatch(Request.Form["NickName"]))
            {
                if (Db<M.Member>.Query(DataSource).Update().Set("NickName", Request.Form["NickName"]).Where(new DbWhereQueue("Id", User.Identity.Id)).Execute() > 0)
                {
                    SetResult(DataStatus.Success);
                }
                else
                {
                    SetResult(DataStatus.Failed);
                }
            }
            else
            {
                SetResult(-1002);
            }
        }

        [Authorize(true)]
        public void setgender()
        {
            this["Member"] = M.MemberInfo.GetByModify(DataSource, User.Identity.Id);
            Render("setgender.html");
        }

        [Authorize]
        public void SubmitSetGender()
        {
            string gender = Request.Form["gender"];
            int gd = 0;
            if (string.IsNullOrEmpty(gender))
            {
                SetResult(-1002);
            }
            else 
            {
                if (gender == "Girl")
                {
                    gd = 2;
                }
                else if (gender == "Boy")
                {
                    gd = 1;
                }

                if (Db<M.Member>.Query(DataSource).Update().Set("Sex", gd).Where(new DbWhereQueue("Id", User.Identity.Id)).Execute() > 0)
                {
                    SetResult(DataStatus.Success);
                }
                else
                {
                    SetResult(DataStatus.Failed);
                }
            }

        }
        /// <summary>
        /// 验证支付密码
        /// </summary>
        [Authorize]
        [HttpAjax]
        [HttpPost]
        public void ChkPayPwd()
        {
            string PayPassword = Request["PayPassword"];
            if (!string.IsNullOrEmpty(PayPassword))
            {
                if (M.MemberInfo.CheckPayPassword(DataSource, User.Identity.Id, PayPassword))
                {
                    SetResult(true);
                }
                else
                {
                    SetResult(false);
                }
            }
        }
    }
}
