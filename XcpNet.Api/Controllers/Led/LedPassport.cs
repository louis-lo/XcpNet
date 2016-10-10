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
    public sealed class LedPassport : CommPassport
    {
        protected override void OnInitController()
        {
        }
        [HttpPost]
        public new void Register()
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
                    if (distributor.Province > 0 && distributor.City > 0 && distributor.County > 0 && !string.IsNullOrEmpty(distributor.Address))
                    {
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
                    }

                    SetResult(true, member.Token);
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
        public new void Login()
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
                M.LoginStatus status = M.Member.ApiLogin(DataSource, Request.Form["UserName"], Request.Form["Password"].MD5(), ClientIp, mark, out errCount, out token);
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
            CheckMarkHelper("ApiPassport", "Login", "注册账号")
                .AddArgument("UserName", typeof(long), "手机号码")
                .AddArgument("Password", typeof(string), "密码")
                .AddResult(true, typeof(string), "成功返回token值,失败返回错误代码");
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
        public new void GetUserInfo()
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
                        SetResult(new { UserInfo = memberinfo, IsDistributor = true, Level = distributor.Level, Distributor = distributor });
                    }
                    else
                    {
                        SetResult(new { UserInfo = memberinfo, IsDistributor = false, Level = -1 });
                    }
                }
            }
            catch (Exception)
            {
                SetResult(ApiUtility.PROGRAM_ERROR);
            }
        }
        public bool IsDistributor(out P.Distributor distributor, long userid)
        {
            distributor = P.Distributor.GetById(DataSource, userid);
            if (distributor == null)
                return false;
            return true;
        }

    }
}
