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
using System.Text;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Web;
using System.Text.RegularExpressions;

namespace XcpNet.Api.Controllers
{
    public class ApiPassport : Cnaws.Sms.Controllers.Sms
    {
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
                            SetResult(ApiUtility.SMS_EXISTENCE);
                            throw new AggregateException();
                        }
                        string md5 = mark.MD5();
                        V.StringHash sh = V.StringHash.Create(DataSource, md5, V.StringHash.SmsHash, timespan);
                        if (sh == null)
                        {
                            SetResult(ApiUtility.SMS_EXISTENCE);
                            throw new AggregateException();
                        }
                        string SmsTemplate;
                        if (SmsType == 0) SmsTemplate = "register"; else SmsTemplate = "password";
                        S.SmsTemplate temp = S.SmsTemplate.GetByName(DataSource, SmsTemplate);
                        if (temp.Type == S.SmsTemplateType.Template)
                            SendTemplateImpl(name, mobile, temp.Content, hash.Hash);
                        else
                            SendImpl(name, mobile, temp.Content, hash.Hash);
                        SetResult(true);
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
        }
#if (DEBUG)
        public static void GetOrderMappingHelper()
        {
            AddApiMethod("ApiPassport", "SendSms/yuntongxun", "发送短信")
                .AddArgument("mark", typeof(string), "mark")
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
            try
            {
                string mark;
                if (CheckMark(out mark))
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

                    P.Distributor distributor = A.MachineCode.GetDistributorByCode(DataSource, mark);
                    if (distributor == null || distributor.UserId == 0)
                    {
                        SetResult(ApiUtility.DISTRIBUTOR_EMPTY);
                        throw new AggregateException();
                    }
                    string password = member.Password;
                    member.ParentId = distributor.UserId;
                    member.Approved = section.DefaultApproved;
                    member.CreationDate = DateTime.Now;
                    DataStatus status = member.Insert(DataSource);
                    if (status != DataStatus.Success)
                    {
                        SetResult(ApiUtility.INSERT_FAIL);
                        throw new AggregateException();
                    }

                    M.ShippingAddress shippingaddress = new M.ShippingAddress()
                    {
                        Consignee = distributor.Company,
                        Province = distributor.Province,
                        City = distributor.City,
                        County = distributor.County,
                        Address = distributor.Address,
                        Mobile = member.Mobile,
                        PostId = distributor.PostId,
                        IsDefault = true,
                        UserId = member.Id
                    };
                    shippingaddress.Insert(DataSource);
                    SetResult(true, member.Id);
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
        public static void RegisterHelper()
        {
            AddApiMethod("ApiPassport", "Register", "注册账号")
                .AddArgument("mark", typeof(string), "mark")
                .AddArgument("RegisterType", typeof(int), "注册类型 0:Name,1:Email,2:Mobile")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddArgument("Possword", typeof(string), "手机号码")
                .AddArgument("SmsCaptcha", typeof(string), "手机验证码")
                .AddResult(ApiUtility.SMSCAPTCHA_EQUALS, "手机验证码错误")
                .AddResult(ApiUtility.MEMBER_EXISTENCE, "该账号已经被注册")
                .AddResult(ApiUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "成功返回用户ID,失败返回错误信息");
        }
#endif

        [HttpPost]
        public void Login()
        {
            string mark;
            if (CheckMark(out mark))
            {
                int errCount;
                Guid token;
                M.LoginStatus status = M.Member.Login(DataSource, Request.Form["UserName"], Request.Form["Password"], ClientIp, mark, out errCount, out token);
                if (status == M.LoginStatus.Success)
                    SetResult(true, token);
                else
                    SetResult(false, status);
            }
        }
#if (DEBUG)
        public static void LoginHelper()
        {
            AddApiMethod("ApiPassport", "Login", "注册账号")
                .AddArgument("mark", typeof(string), "mark")
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
            CheckMarkHelper("ApiPassport", "FindPassword", "找回密码")
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
                    if (oldpassword.MD5() != member.Password)
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
            CheckMemberHelper("ApiPassport", "ModifyPassword", "修改密码")
                .AddArgument("oldpassword", typeof(string), "手机验证码")
                .AddArgument("password", typeof(string), "新密码")
                .AddResult(ApiUtility.PASSWORD_EQUALS, "密码错误")
                .AddResult(ApiUtility.UPDATE_FAIL, "修改失败")
                .AddResult(true, typeof(string), "成功");
        }
#endif


        public void BindMachine()
        {
            string mark = Request["mark"];
            try
            {
                if (string.IsNullOrEmpty(mark))
                {
                    SetResult(ApiUtility.MARK_EMPTY);
                    return;
                }
                long Id = A.MachineCode.Add(DataSource, mark, Request["UserName"], Request["Password"].MD5());
                if (Id > 0)
                    SetResult(true, Id);
                else
                    SetResult((int)Id);
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

#if (DEBUG)
        public static void BindMachineHelper()
        {
            AddApiMethod("ApiPassport", "BingMachine", "给机器定定加盟商")
                .AddArgument("mark", typeof(string), "mark")
                .AddArgument("UserName", typeof(long), "登录账号")
                .AddArgument("Password", typeof(string), "密码")
                .AddResult(ApiUtility.MARK_EMPTY, "标识mark为空")
                .AddResult(ApiUtility.MEMBER_NOTFOUND, "找不到该用户")
                .AddResult(ApiUtility.PASSWORD_EQUALS, "密码错误")
                .AddResult(ApiUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(ApiUtility.DISTRIBUTOR_NOTIS, "不是加盟商")
                .AddResult(ApiUtility.PROGRAM_ERROR, "返回错误代码")
                .AddResult(true, typeof(string), "成功返回code:-200值");
        }
#endif

        public void CheckMachineCode()
        {
            string mark = Request["mark"];
            try
            {
                long Memberid = 0;
                long.TryParse(Request["memberid"], out Memberid);
                if (string.IsNullOrEmpty(mark))
                {
                    SetResult(ApiUtility.MARK_EMPTY);
                    throw new AggregateException();
                }
                A.MachineCode machinecode = A.MachineCode.GetByCode(DataSource, mark);
                if (machinecode == null || machinecode.MemberId == 0)
                {
                    SetResult(ApiUtility.DISTRIBUTOR_EMPTY);
                    throw new AggregateException();
                }
                if (Memberid == machinecode.MemberId)
                {
                    double longitude = 0, latitude = 0;
                    double.TryParse(Request["longitude"], out longitude);
                    double.TryParse(Request["latitude"], out latitude);
                    if (longitude != 0 && latitude != 0)
                    {
                        new P.Distributor() { UserId = machinecode.MemberId, Longitude = longitude, Latitude = latitude }.Update(DataSource, ColumnMode.Include, "Longitude", "Latitude", "UserId");
                    }
                    SetResult(true);
                }
                else
                    SetResult(ApiUtility.DISTRIBUTOR_EMPTY);
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
        public static void CheckMachineCodeHelper()
        {
            AddApiMethod("ApiPassport", "CheckMachineCode", "检查加盟商的Mark跟Id是否对应")
                .AddArgument("mark", typeof(string), "mark")
                .AddArgument("memberid", typeof(long), "供应商Id")
                .AddArgument("longitude", typeof(double), "经度")
                .AddArgument("latitude", typeof(double), "纬度")
                .AddResult(ApiUtility.MARK_EMPTY, "标识mark为空")
                .AddResult(ApiUtility.DISTRIBUTOR_EMPTY, "找不到对应的加盟商")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "成功返回code:-200值");
        }
#endif


        public void GetUserInfo()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    M.MemberInfo memberinfo = M.MemberInfo.GetById(DataSource, member.Id);
                    P.Distributor distributor;
                    if (IsDistributor(out distributor, member.Id))
                    {
                        SetResult(new { UserInfo = memberinfo, IsDistributor = true });
                    }
                    else
                    {
                        SetResult(new { UserInfo = memberinfo, IsDistributor = false });
                    }
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
            CheckMemberHelper("ApiPassport", "GetUserInfo", "获取用户详细信息")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(M.MemberInfo), "成功状态");
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
            CheckMemberHelper("ApiPassport", "ModifyNickName", "修改昵称")
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
            CheckMemberHelper("ApiPassport", "ModifySex", "修改性别")
                .AddArgument("Sex", typeof(int), "性别 0:未知,1:男,2:女")
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
            CheckMemberHelper("ApiPassport", "GetBilllist", "获取用户账单")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(true, typeof(SplitPageData<M.MoneyRecord>), "成功返回");
        }
#endif


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
            return CheckMarkHelper(AddApiMethod(ns, name, summary));
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
            return CheckTokenHelper(AddApiMethod(ns, name, summary));
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
                req.Add(key, Regex.Replace(HttpUtility.UrlEncode(HttpUtility.UrlDecode(data[key]), Encoding.UTF8).ToUpper(), @"\+", "%20"));
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

        //        public string MakeSign(NameValueCollection data)
        //        {
        //            StringBuilder str = new StringBuilder(ToUrl(data));
        //            str.Append("&key=").Append("d7b5035a21dde32128a55aa4b0eaa27e");
        //            using (MD5 md5 = MD5.Create())
        //            {
        //                byte[] bs = md5.ComputeHash(Encoding.UTF8.GetBytes(str.ToString()));
        //                StringBuilder sb = new StringBuilder();
        //                foreach (byte b in bs)
        //                    sb.Append(b.ToString("x2"));
        //                return sb.ToString().ToUpper();
        //            }
        //        }

        //        public bool CheckSign(NameValueCollection data)
        //        {
        //            if (!string.IsNullOrEmpty(data["sign"]))
        //            {
        //                if (data["sign"] == MakeSign(data))
        //                    return true;
        //                else
        //                {
        //                    SetResult(ApiUtility.SIGN_ERROR);
        //                    return false;
        //                }
        //            }
        //            SetResult(ApiUtility.SIGN_ERROR);
        //            return false;
        //        }
        //#if (DEBUG)
        //        protected static ApiMethod CheckSignHelper(ApiMethod m)
        //        {
        //            return m
        //                .AddResult(ApiUtility.SIGN_ERROR, "验证签名失败");
        //        }
        //#endif
        public bool IsDistributor(out P.Distributor distributor, long userid)
        {
            distributor = P.Distributor.GetById(DataSource, userid);
            if (distributor == null)
                return false;
            return true;
        }
        #endregion 验证信息
    }
}
