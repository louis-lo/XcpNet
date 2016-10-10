using Cnaws.Data;
using Cnaws.ExtensionMethods;
using Cnaws.Web;
using Cnaws.Web.Configuration;
using System;
using M = Cnaws.Passport.Modules;
using S = Cnaws.Sms.Modules;
using V = Cnaws.Verification.Modules;
using A = XcpNet.Ad.Modules;
using P = Cnaws.Product.Modules;
using System.Collections.Specialized;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web;
using System.Text.RegularExpressions;

namespace XcpNet.Api.Controllers
{
    public class CommPassport : Cnaws.Sms.Controllers.Sms
    {
        public static string ClassName = "[type]Passport";
        protected override void OnInitController()
        {
            NotFound();
        }

        #region 验证信息
        public bool CheckMobile(out long mobile)
        {
            bool ret = false;
            if (long.TryParse(Request["mobile"], out mobile))
                ret = ApiUtility.CheckMobile(mobile);
            if (!ret)
                SetResult(ApiUtility.MOBILE_FORMAT);
            return ret;
        }

        public bool CheckCaptcha(long mobile, string hash)
        {
            return V.MobileHash.Equals(DataSource, mobile, V.MobileHash.Register, hash);
        }


        public bool CheckMark(out string mark)
        {
            mark = Request["mark"];
            if (string.IsNullOrEmpty(mark))
            {
                SetResult(ApiUtility.MARK_EMPTY);
                return false;
            }
            return true;
        }
#if (DEBUG)
        protected static ApiMethod CheckMarkHelper(ApiMethod m)
        {
            return m
                .AddArgument("mark", typeof(string), "标识")
                .AddResult(ApiUtility.MARK_EMPTY, "标识为空");
        }
        protected static ApiMethod CheckMarkHelper(string ns, string name, string summary)
        {
            return CheckMarkHelper(CheckSignHelper(AddApiMethod(ns, name, summary)));
        }
#endif
        public bool CheckToken(out Guid token, out M.Member member)
        {
            if (!Guid.TryParse(Request["token"], out token) || Guid.Empty.Equals(token))
            {
                member = null;
                SetResult(ApiUtility.TOKEN_EMPTY);
                return false;
            }
            member = M.Member.GetByToken(DataSource, token);
            if (member == null)
            {
                SetResult(ApiUtility.MEMBER_NOTFOUND);
                return false;
            }
            string mark = Request["mark"];
            if (string.IsNullOrEmpty(mark))
            {
                SetResult(ApiUtility.MARK_EMPTY);
                return false;
            }
            if (!string.Equals(mark, member.Mark))
            {
                SetResult(ApiUtility.MARK_EQUALS);
                return false;
            }
            if (!token.Equals(member.Token))
            {
                SetResult(ApiUtility.TOKEN_EQUALS);
                return false;
            }
            return true;
        }
#if (DEBUG)
        protected static ApiMethod CheckTokenHelper(ApiMethod m)
        {
            return m
                .AddArgument("token", typeof(string), "Token")
                .AddArgument("mark", typeof(string), "标识")
                .AddResult(ApiUtility.TOKEN_EMPTY, "Token为空")
                .AddResult(ApiUtility.MEMBER_NOTFOUND, "未找到用户")
                .AddResult(ApiUtility.MARK_EMPTY, "标识为空")
                .AddResult(ApiUtility.MARK_EQUALS, "标识错误")
                .AddResult(ApiUtility.TOKEN_EQUALS, "Token错误");
        }
        protected static ApiMethod CheckTokenHelper(string ns, string name, string summary)
        {
            return CheckTokenHelper(CheckSignHelper(AddApiMethod(ns, name, summary)));
        }
#endif
        public bool CheckMember(out M.Member member)
        {
            Guid token;
            bool ret = CheckToken(out token, out member);
            if (ret)
            {
                if (member.Approved)
                {
                    if (!member.Locked)
                    {
                        return true;
                    }
                    else
                    {
                        SetResult(ApiUtility.MEMBER_LOCKED);
                    }
                }
                else
                {
                    SetResult(ApiUtility.MEMBER_APPROVED);
                }
            }
            else
            {
                member = null;
            }
            return ret;
        }

#if (DEBUG)
        protected static ApiMethod CheckMemberHelper(ApiMethod m)
        {
            return m
                .AddResult(ApiUtility.MEMBER_LOCKED, "用户已锁定")
                .AddResult(ApiUtility.MEMBER_APPROVED, "用户未审核");
        }
        protected static ApiMethod CheckMemberHelper(string ns, string name, string summary)
        {
            return CheckMemberHelper(CheckTokenHelper(ns, name, summary));
        }
#endif



        public string ToUrl(NameValueCollection data)
        {
            int index = 0;
            StringBuilder buff = new StringBuilder();
            Dictionary<string, string> req = new Dictionary<string, string>();
            Array Keys = data.AllKeys;
            Array.Sort(Keys);
            foreach (string key in Keys)
            {
                req.Add(key, Regex.Replace(HttpUtility.UrlEncode(HttpUtility.UrlDecode(data[key], Encoding.UTF8), Encoding.UTF8).ToUpper(), @"\+", "%20"));
            }
            foreach (KeyValuePair<string, string> pair in req)
            {
                if (!"sign".Equals(pair.Key) && !string.IsNullOrEmpty(pair.Value))
                {
                    if (index++ > 0)
                        buff.Append('&');
                    buff.Append(pair.Key).Append('=').Append(pair.Value);
                }
            }
            return buff.ToString();
        }

        public string MakeSign(NameValueCollection data)
        {
            StringBuilder str = new StringBuilder(ToUrl(data));
            str.Append("&key=").Append("d7b5035a21dde32128a55aa4b0eaa27e");
            using (MD5 md5 = MD5.Create())
            {
                byte[] bs = md5.ComputeHash(Encoding.UTF8.GetBytes(str.ToString()));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bs)
                    sb.Append(b.ToString("x2"));
                return sb.ToString().ToUpper();
            }
        }

        public bool CheckSign(NameValueCollection data)
        {
            if (!string.IsNullOrEmpty(data["sign"]))
            {
                if (data["sign"] == MakeSign(data))
                    return true;
            }
            throw new JsonResultException(ApiUtility.SIGN_ERROR);
        }
#if (DEBUG)
        protected static ApiMethod CheckSignHelper(ApiMethod m)
        {
            return m
                .AddResult(ApiUtility.SIGN_ERROR, "验证签名失败");
        }
#endif

        #endregion 验证信息

        [HttpPost]
        public void SendSms(string name)
        {
            string mark;
            if (CheckMark(out mark))
            {
                long mobile = long.Parse(Request.Form["Mobile"]);
                if (ApiUtility.CheckMobile(mobile))
                {
                    try
                    {
                        int SmsType = 0; int.TryParse(Request["SmsType"], out SmsType);
                        PassportSection section = PassportSection.GetSection();
                        if (!section.VerifyMobile)
                        {
                            SetResult(ApiUtility.SMS_SEND);
                            throw new AggregateException();
                        }
                        int timespan = SMSCaptchaSection.GetSection().TimeSpan;
                        V.MobileHash hash = V.MobileHash.Create(DataSource, mobile, SmsType, timespan);
                        if (hash == null)
                        {
                            SetResult(ApiUtility.SMS_EXISTENCE,new { TimeSpan = timespan });
                            throw new AggregateException();
                        }

                        if (string.IsNullOrEmpty(mark))
                            SetResult(ApiUtility.PARAMETER_NOFOND);
                        string md5 = mark.MD5();
                        V.StringHash sh = V.StringHash.Create(DataSource, md5, V.StringHash.SmsHash, timespan);
                        if (sh == null)
                        {
                            SetResult(ApiUtility.SMS_EXISTENCE, new { TimeSpan = timespan });
                            throw new AggregateException();
                        }
                        string SmsTemplate;
                        if (SmsType == 0) SmsTemplate = "register"; else SmsTemplate = "password";
                        S.SmsTemplate temp = S.SmsTemplate.GetByName(DataSource, SmsTemplate);
                        if (temp.Type == S.SmsTemplateType.Template)
                            SendTemplateImpl(name, mobile, temp.Content, hash.Hash);
                        else
                            SendImpl(name, mobile, temp.Content, hash.Hash);
                        SetResult(true, new { TimeSpan = timespan });
                    }
                    catch (AggregateException)
                    {
                        return;
                    }
                    catch (Exception)
                    {
                        SetResult(false);
                    }
                }
                else
                {
                    SetResult(ApiUtility.MOBILE_FORMAT);
                }
            }
        }
#if (DEBUG)
        public static void GetOrderMappingHelper()
        {
            CheckMarkHelper(ClassName, "SendSms/yuntongxun", "发送短信")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddArgument("SmsType", typeof(int), "发送短信类型 0:注册类,1:密码类")
                .AddResult(ApiUtility.SMS_SEND, "没有开启短信验证")
                .AddResult(ApiUtility.SMS_EXISTENCE, "在指定的时间内已发送过短信")
                .AddResult(ApiUtility.MOBILE_FORMAT, "手机号码格式错误")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(bool), "是否成功");
        }
#endif

        [HttpPost]
        public void Register()
        {
            string mark = Request["mark"];
            if (CheckMark(out mark))
            {
                try
                {
                    M.RegisterType type = (M.RegisterType)int.Parse(Request.Form["RegisterType"]);
                    PassportSection section = PassportSection.GetSection();
                    M.Member member = DbTable.Load<M.Member>(Request.Form);
                    if (type == M.RegisterType.Mobile)
                    {
                        if (!M.Member.CheckMobile(DataSource, member.Mobile))
                        {
                            SetResult(ApiUtility.MEMBER_EXISTENCE);
                            throw new AggregateException();
                        }
                        if (section.VerifyMobile)
                        {
                            if (!V.MobileHash.Equals(DataSource, member.Mobile, V.MobileHash.Register, Request.Form["SmsCaptcha"]))
                            {
                                SetResult(ApiUtility.SMSCAPTCHA_EQUALS);
                                throw new AggregateException();
                            }
                            member.VerMob = true;
                        }
                    }
                    else if (type == M.RegisterType.Email)
                    {
                        if (!M.Member.CheckEmail(DataSource, member.Email))
                        {
                            SetResult(ApiUtility.MEMBER_EXISTENCE);
                            throw new AggregateException();
                        }
                    }
                    else if (type == M.RegisterType.Name)
                    {
                        if (M.Member.CheckName(DataSource, member.Name))
                        {
                            SetResult(ApiUtility.MEMBER_EXISTENCE);
                            throw new AggregateException();
                        }
                    }

                    string password = member.Password;
                    member.Token = Guid.NewGuid();
                    member.Mark = mark;
                    member.ParentId = 0;
                    member.Approved = section.DefaultApproved;
                    member.CreationDate = DateTime.Now;
                    DataStatus status = member.Insert(DataSource);
                    if (status != DataStatus.Success)
                    {
                        SetResult(ApiUtility.INSERT_FAIL);
                        throw new AggregateException();
                    }
                    SetResult(true, member.Token);
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception)
                {
                    SetResult(false);
                    return;
                }
            }
        }

#if (DEBUG)
        public static void RegisterHelper()
        {
            AddApiMethod(ClassName, "Register", "注册账号")
                .AddArgument("RegisterType", typeof(int), "注册类型 0:Name,1:Email,2:Mobile")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddArgument("Possword", typeof(string), "密码")
                .AddArgument("SmsCaptcha", typeof(string), "手机验证码")
                .AddResult(ApiUtility.SMSCAPTCHA_EQUALS, "手机验证码错误")
                .AddResult(ApiUtility.MEMBER_EXISTENCE, "该账号已经被注册")
                .AddResult(ApiUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "成功返回用户Token");
        }
#endif
        [HttpPost]
        public void Register2()
        {
            string mark = Request["mark"];
            if (CheckMark(out mark))
            {
                try
                {
                    M.RegisterType type = (M.RegisterType)int.Parse(Request.Form["RegisterType"]);
                    PassportSection section = PassportSection.GetSection();
                    M.MemberInfo member = DbTable.Load<M.MemberInfo>(Request.Form);
                    if (type == M.RegisterType.Mobile)
                    {
                        if (!M.Member.CheckMobile(DataSource, member.Mobile))
                        {
                            SetResult(ApiUtility.MEMBER_EXISTENCE);
                            throw new AggregateException();
                        }
                        if (section.VerifyMobile)
                        {
                            if (!V.MobileHash.Equals(DataSource, member.Mobile, V.MobileHash.Register, Request.Form["SmsCaptcha"]))
                            {
                                SetResult(ApiUtility.SMSCAPTCHA_EQUALS);
                                throw new AggregateException();
                            }
                            member.VerMob = true;
                        }
                    }
                    else if (type == M.RegisterType.Email)
                    {
                        if (!M.Member.CheckEmail(DataSource, member.Email))
                        {
                            SetResult(ApiUtility.MEMBER_EXISTENCE);
                            throw new AggregateException();
                        }
                    }
                    else if (type == M.RegisterType.Name)
                    {
                        if (M.Member.CheckName(DataSource, member.Name))
                        {
                            SetResult(ApiUtility.MEMBER_EXISTENCE);
                            throw new AggregateException();
                        }
                    }
                    P.Distributor distributor = A.MachineCode.GetDistributorByCode(DataSource, mark);
                    if (distributor == null || distributor.UserId == 0)
                    {
                        SetResult(ApiUtility.DISTRIBUTOR_EMPTY);
                        throw new AggregateException();
                    }
                    string password = member.Password;
                    member.Token = Guid.NewGuid();
                    member.Mark = mark;
                    member.ParentId = distributor.UserId;
                    member.Approved = section.DefaultApproved;
                    member.CreationDate = DateTime.Now;
                    DataStatus status = member.Insert(DataSource);
                    if (status != DataStatus.Success)
                    {
                        SetResult(ApiUtility.INSERT_FAIL);
                        throw new AggregateException();
                    }
                    SetResult(true, member.Token);
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception)
                {
                    SetResult(false);
                    return;
                }
            }
        }

#if (DEBUG)
        public static void Register2Helper()
        {
            AddApiMethod(ClassName, "Register2", "注册账号")
                .AddArgument("RegisterType", typeof(int), "注册类型 0:Name,1:Email,2:Mobile")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddArgument("Possword", typeof(string), "密码")
                .AddArgument("RealName", typeof(string), "真实姓名")
                .AddArgument("Province", typeof(int), "省")
                .AddArgument("City", typeof(int), "市")
                .AddArgument("County", typeof(int), "区")
                .AddArgument("Address", typeof(string), "联系地址")
                .AddArgument("SmsCaptcha", typeof(string), "手机验证码")
                .AddResult(ApiUtility.SMSCAPTCHA_EQUALS, "手机验证码错误")
                .AddResult(ApiUtility.MEMBER_EXISTENCE, "该账号已经被注册")
                .AddResult(ApiUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "成功返回用户Token");
        }
#endif
        [HttpPost]
        public void Login()
        {
            try
            {
                string mark = Request["mark"];
                if (string.IsNullOrEmpty(mark))
                {
                    SetResult(ApiUtility.PARAMETER_NOFOND);
                    throw new AggregateException();
                }
                int errCount;
                Guid token;
                M.LoginStatus status = M.Member.ApiLogin(DataSource, Request.Form["UserName"], Request.Form["Password"], ClientIp, mark, out errCount, out token);
                if (status == M.LoginStatus.Success)
                    SetResult(true, token);
                else
                {
                    if (status == M.LoginStatus.CaptchaError)
                    {
                        SetResult(ApiUtility.SMSCAPTCHA_EQUALS);
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.PasswordError)
                    {
                        SetResult(ApiUtility.PASSWORD_EQUALS);
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.Locked)
                    {
                        SetResult(ApiUtility.MEMBER_LOCKED);
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.NotApproved)
                    {
                        SetResult(ApiUtility.MEMBER_APPROVED);
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.NotFound)
                    {
                        SetResult(ApiUtility.MEMBER_NOTFOUND);
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.SmsCaptchaError)
                    {
                        SetResult(ApiUtility.SMSCAPTCHA_EQUALS);
                        throw new AggregateException();
                    }
                }
            }
            catch (AggregateException)
            {
                return;
            }
            catch (Exception)
            {
                SetResult(false);
                return;
            }
        }
#if (DEBUG)
        public static void LoginHelper()
        {
            CheckMarkHelper(ClassName, "Login", "注册账号")
                .AddArgument("UserName", typeof(long), "手机号码")
                .AddArgument("Password", typeof(string), "密码")
                .AddResult(true, typeof(string), "成功返回token值,失败返回错误代码");
        }
#endif
        [HttpPost]
        public void FindPassword()
        {
            string mark;
            if (CheckMark(out mark))
            {
                try
                {
                    long mobile = long.Parse(Request.Form["Mobile"]);
                    if (ApiUtility.CheckMobile(mobile))
                    {
                        string Password = Request["Password"];
                        PassportSection section = PassportSection.GetSection();
                        if (section.VerifyMobile)
                        {
                            if (!V.MobileHash.Equals(DataSource, mobile, V.MobileHash.Password, Request.Form["SmsCaptcha"]))
                            {
                                SetResult(ApiUtility.SMSCAPTCHA_EQUALS);
                                throw new AggregateException();
                            }
                            M.Member temp = M.Member.Get(DataSource, mobile.ToString());
                            if (temp == null)
                            {
                                SetResult(ApiUtility.MEMBER_NOTFOUND);
                                throw new AggregateException();
                            }
                            temp.Password = Password;
                            if (temp.Update(DataSource, ColumnMode.Include, "Password") == DataStatus.Success)
                                SetResult(true);
                            else
                            {
                                SetResult(ApiUtility.UPDATE_FAIL);
                                throw new AggregateException();
                            }
                        }
                    }
                    else
                    {
                        SetResult(ApiUtility.MOBILE_FORMAT);
                    }
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void FindPasswordHelper()
        {
            CheckMarkHelper(ClassName, "FindPassword", "找回密码")
                .AddArgument("SmsCaptcha", typeof(string), "手机验证码")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddArgument("Password", typeof(string), "密码")
                .AddResult(ApiUtility.MOBILE_FORMAT, "手机号码格式错误")
                .AddResult(ApiUtility.SMSCAPTCHA_EQUALS, "验证码错误")
                .AddResult(ApiUtility.MEMBER_NOTFOUND, "找不到该用户")
                .AddResult(ApiUtility.UPDATE_FAIL, "修改失败")
                .AddResult(true, typeof(string), "成功返回token值,失败返回错误代码");
        }
#endif
        [HttpPost]
        public void ModifyPassword()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    string oldpassword = Request["oldpassword"];
                    string Password = Request["password"];
                    if (oldpassword != member.Password)
                    {
                        SetResult(ApiUtility.PASSWORD_EQUALS);
                        throw new AggregateException();
                    }
                    else
                    {
                        member.Password = Password;
                        if (member.Update(DataSource, ColumnMode.Include, "Password") == DataStatus.Success)
                            SetResult(true);
                        else
                        {
                            SetResult(ApiUtility.UPDATE_FAIL);
                            throw new AggregateException();
                        }
                    }
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void ModifyPasswordHelper()
        {
            CheckMemberHelper(ClassName, "ModifyPassword", "修改密码")
                .AddArgument("oldpassword", typeof(string), "手机验证码")
                .AddArgument("password", typeof(string), "新密码")
                .AddResult(ApiUtility.PASSWORD_EQUALS, "密码错误")
                .AddResult(ApiUtility.UPDATE_FAIL, "修改失败")
                .AddResult(true, typeof(string), "成功");
        }
#endif

        public void GetUserInfo()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    SetResult(M.MemberInfo.GetById(DataSource, member.Id));
                }
            }
            catch (Exception)
            {
                SetResult(ApiUtility.PROGRAM_ERROR);
            }
        }
#if (DEBUG)
        public static void GetUserInfoHelper()
        {
            CheckMemberHelper(ClassName, "GetUserInfo", "获取用户详细信息")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "成功状态");
        }
#endif

        [HttpPost]
        public void SetPayPassword()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    long mobile = long.Parse(Request.Form["Mobile"]);
                    if (ApiUtility.CheckMobile(mobile))
                    {
                        string paypassword = Request["PayPassword"];
                        PassportSection section = PassportSection.GetSection();
                        if (section.VerifyMobile)
                        {
                            if (!V.MobileHash.Equals(DataSource, mobile, V.MobileHash.Password, Request.Form["SmsCaptcha"]))
                            {
                                SetResult(ApiUtility.SMSCAPTCHA_EQUALS);
                                throw new AggregateException();
                            }
                            if (member == null)
                            {
                                SetResult(ApiUtility.MEMBER_NOTFOUND);
                                throw new AggregateException();
                            }
                            if (new M.MemberInfo() { Id = member.Id, PayPassword = paypassword }.Update(DataSource, ColumnMode.Include, "PayPassword") == DataStatus.Success)
                                SetResult(true);
                            else
                            {
                                SetResult(ApiUtility.UPDATE_FAIL);
                                throw new AggregateException();
                            }
                        }
                    }
                    else
                    {
                        SetResult(ApiUtility.MOBILE_FORMAT);
                    }
                }
                catch (AggregateException)
                {
                    return;
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void SetPayPasswordHelper()
        {
            CheckMemberHelper(ClassName, "SetPayPassword", "设置交易密码")
                .AddArgument("SmsCaptcha", typeof(string), "手机验证码")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddArgument("PayPassword", typeof(string), "交易密码")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(ApiUtility.MOBILE_FORMAT, "手机号码格式错误")
                .AddResult(ApiUtility.SMSCAPTCHA_EQUALS, "验证码错误")
                .AddResult(ApiUtility.MEMBER_NOTFOUND, "找不到该用户")
                .AddResult(ApiUtility.UPDATE_FAIL, "修改失败")
                .AddResult(true, typeof(string), "成功返回token值,失败返回错误代码");
        }
#endif

        [HttpPost]
        public void ModifyHeadImage()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    SetResult(new M.MemberInfo() { Id = member.Id, Image = Request["Image"] }.Update(DataSource, ColumnMode.Include, "Image"));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void ModifyHeadImageHelper()
        {
            CheckMemberHelper(ClassName, "ModifyHeadImage", "修改头像")
                .AddArgument("Image", typeof(string), "头像地址")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "成功状态");
        }
#endif

        [HttpPost]
        public void ModifyNickName()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    SetResult(new M.MemberInfo() { Id = member.Id, NickName = Request["NickName"] }.Update(DataSource, ColumnMode.Include, "NickName"));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void ModifyNickNameHelper()
        {
            CheckMemberHelper(ClassName, "ModifyNickName", "修改昵称")
                .AddArgument("NickName", typeof(string), "昵称")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "成功状态");
        }
#endif

        [HttpPost]
        public void ModifySex()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    SetResult(new M.MemberInfo() { Id = member.Id, Sex = (M.MemberSex)int.Parse(Request["Sex"]) }.Update(DataSource, ColumnMode.Include, "Sex"));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void ModifySexHelper()
        {
            CheckMemberHelper(ClassName, "ModifySex", "修改性别")
                .AddArgument("Sex", typeof(int), "性别 0:未知,1:男,2:女")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "成功状态");
        }
#endif

        [HttpPost]
        public void ModifyMobile()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    int type = 0;
                    int.TryParse(Request["Type"], out type);
                    PassportSection section = PassportSection.GetSection();
                    if (section.VerifyMobile)
                    {

                        if (member.Mobile != 0 && member.VerMob)
                        {
                            if (type == 1)
                            {
                                long newmobile = long.Parse(Request.Form["NewMobile"]);
                                if (M.Member.Get(DataSource, newmobile.ToString()) != null)
                                {
                                    SetResult(ApiUtility.USER_EXISTENCE);
                                    throw new AggregateException();
                                }
                                string pwd = Request.Form["PayPassword"];
                                if (string.IsNullOrEmpty(pwd))
                                {
                                    SetResult(ApiUtility.PASSWORD_EQUALS);
                                    throw new AggregateException();
                                }
                                if (!M.MemberInfo.ApiCheckPayPassword(DataSource, member.Id, pwd))
                                {
                                    SetResult(ApiUtility.PASSWORD_EQUALS);
                                    throw new AggregateException();
                                }

                                if (!V.MobileHash.Equals(DataSource, newmobile, V.MobileHash.Password, Request.Form["NewSmsCaptcha"]))
                                {
                                    SetResult(ApiUtility.SMSCAPTCHA_EQUALS);
                                    throw new AggregateException();
                                }
                                member.Mobile = newmobile;
                                member.VerMob = true;
                                SetResult(member.Update(DataSource, ColumnMode.Include, "VerMob", "Mobile"));
                            }
                            else
                            {
                                SetResult(ApiUtility.VERMOB_BIND);
                                throw new AggregateException();
                            }
                        }
                        else
                        {
                            if (type == 0)
                            {
                                long newmobile = long.Parse(Request.Form["NewMobile"]);
                                if (M.Member.Get(DataSource, newmobile.ToString()) != null)
                                {
                                    SetResult(ApiUtility.USER_EXISTENCE);
                                    throw new AggregateException();
                                }
                                if (!V.MobileHash.Equals(DataSource, newmobile, V.MobileHash.Password, Request.Form["NewSmsCaptcha"]))
                                {
                                    SetResult(ApiUtility.SMSCAPTCHA_EQUALS);
                                    throw new AggregateException();
                                }
                                member.Mobile = newmobile;
                                member.VerMob = true;
                                SetResult(member.Update(DataSource, ColumnMode.Include, "Mobile", "VerMob"));
                            }
                            else
                            {
                                SetResult(ApiUtility.VERMOB_NOT);
                                throw new AggregateException();
                            }
                        }
                    }
                }
            }
            catch (AggregateException)
            {
                return;
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void ModifyMobileHelper()
        {
            CheckMemberHelper(ClassName, "ModifyMobile", "修改性别")
                .AddArgument("Type", typeof(int), "性别 0:绑定手机号,1:更改手机号")
                .AddArgument("PayPassword", typeof(string), "支付密码")
                .AddArgument("NewMobile", typeof(long), "新手机号码")
                .AddArgument("NewSmsCaptcha", typeof(string), "新手机验证码")
                .AddResult(ApiUtility.PASSWORD_EQUALS, "交易密码验证错误")
                .AddResult(ApiUtility.USER_EXISTENCE, "改用户已存在")
                .AddResult(ApiUtility.SMSCAPTCHA_EQUALS, "验证码不正确")
                .AddResult(ApiUtility.VERMOB_BIND, "type错误,当前账号已绑定手机号")
                .AddResult(ApiUtility.VERMOB_NOT, "type错误,当前账号还没有绑定手机号")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "成功状态");
        }
#endif

        public void GetBilllist()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                int size, page;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                if (!int.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                SetResult(M.MoneyRecord.GetPageByMember(DataSource, member.Id, page, size, 11));
            }
        }
#if (DEBUG)
        public static void GetBilllistHelper()
        {
            CheckMemberHelper(ClassName, "GetBilllist", "获取用户账单")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(true, typeof(SplitPageData<M.MoneyRecord>), "成功返回");
        }
#endif
    }
}
