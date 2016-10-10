using Cnaws.Data;
using Cnaws.Web;
using System;
using M = Cnaws.Passport.Modules;
using XcpNet.Common;

namespace XcpNet.ApiSecond.Controllers
{
    public class Passport2 : Cnaws.Sms.Controllers.Sms
    {
        public static string ClassName = "[type]Passport2";
        protected override void OnInitController()
        {
            NotFound();
        }

        [HttpPost]
        public void SendSms(string name)
        {
            object code, data;
            string mark;
            if (new CommControllers2(this).CheckMark(out mark))
            {
                try
                {
                    int SmsType = 0; int.TryParse(Request["SmsType"], out SmsType);
                    code = new CommPassport(this).SendSms(DataSource, long.Parse(Request.Form["Mobile"]), name, SmsType, mark, out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void SendSmsHelper()
        {
            CommControllers2.CheckMarkApi(ClassName, "SendSms/yuntongxun", "发送短信")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddArgument("SmsType", typeof(int), "发送短信类型 0:注册类,1:密码类")
                .AddResult(CommUtility.SMS_SEND, "没有开启短信验证")
                .AddResult(CommUtility.SMS_EXISTENCE, "在指定的时间内已发送过短信")
                .AddResult(CommUtility.MOBILE_FORMAT, "手机号码格式错误")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(bool), "是否成功");
        }
#endif

        [HttpPost]
        public void Register()
        {
            string mark;
            object code, data;
            if (new CommControllers2(this).CheckMark(out mark))
            {
                try
                {
                    M.MemberInfo member = DbTable.Load<M.MemberInfo>(Request.Form);
                    code = CommPassport.Register(DataSource, int.Parse(Request.Form["RegisterType"]), member, Request.Form["SmsCaptcha"], mark, out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
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
                .AddResult(CommUtility.SMSCAPTCHA_EQUALS, "手机验证码错误")
                .AddResult(CommUtility.MEMBER_EXISTENCE, "该账号已经被注册")
                .AddResult(CommUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "成功返回用户Token");
        }
#endif

        [HttpPost]
        public void CheckRegisterMobile()
        {
            string mark;
            object code, data;
            if (new CommControllers2(this).CheckMark(out mark))
            {
                try
                {
                    long mobile = long.Parse(Request.Form["Mobile"]);
                    if (!M.Member.CheckMobile(DataSource, mobile))
                    {
                        SetResult(CommUtility.MEMBER_EXISTENCE);
                    }
                    else
                    {
                        SetResult(CommUtility.SUCCESS);
                    }
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }
            }
        }

#if (DEBUG)
        public static void CheckRegisterMobileHelper()
        {
            AddApiMethod(ClassName, "CheckRegisterMobile", "注册账号")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddResult(CommUtility.MEMBER_EXISTENCE, "该账号已经被注册")
                .AddResult(true, typeof(string), "成功为该账号可以注册");
        }
#endif

        [HttpPost]
        public void Login()
        {
            string mark;
            object code, data;
            if (new CommControllers2(this).CheckMark(out mark))
            {
                try
                {
                    code = CommPassport.Login(DataSource, Request.Form["UserName"], Request.Form["Password"], ClientIp, mark, out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }
            }
        }

#if (DEBUG)
        public static void LoginHelper()
        {
            CommControllers2.CheckMarkApi(ClassName, "Login", "登陆账号")
                .AddArgument("UserName", typeof(long), "手机号码")
                .AddArgument("Password", typeof(string), "密码")
                .AddResult(true, typeof(string), "成功返回token值,失败返回错误代码");
        }
#endif
        [HttpPost]
        public void FindPassword()
        {
            string mark;
            object code, data;
            if (new CommControllers2(this).CheckMark(out mark))
            {
                try
                {
                    long mobile = long.Parse(Request.Form["Mobile"]);
                    if (CommUtility.CheckMobile(mobile))
                    {
                        code = CommPassport.FindPassword(DataSource, mobile, Request.Form["Password"], Request.Form["SmsCaptcha"], mark, out data);
                        new CommUtility(this).CommSetResult(code, data);
                    }
                    else
                    {
                        SetResult(CommUtility.MOBILE_FORMAT);
                    }
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }

            }
        }
#if (DEBUG)
        public static void FindPasswordHelper()
        {
            CommControllers2.CheckMarkApi(ClassName, "FindPassword", "找回密码")
                .AddArgument("SmsCaptcha", typeof(string), "手机验证码")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddArgument("Password", typeof(string), "密码")
                .AddResult(CommUtility.MOBILE_FORMAT, "手机号码格式错误")
                .AddResult(CommUtility.SMSCAPTCHA_EQUALS, "验证码错误")
                .AddResult(CommUtility.MEMBER_NOTFOUND, "找不到该用户")
                .AddResult(CommUtility.UPDATE_FAIL, "修改失败")
                .AddResult(true, typeof(string), "成功返回token值,失败返回错误代码");
        }
#endif
        [HttpPost]
        public void ModifyPassword()
        {
            M.Member member;
            object code, data;
            if (new CommControllers2(this).CheckMember(out member))
            {
                try
                {
                    code = CommPassport.ModifyPassword(DataSource, member, Request.Form["Password"], Request.Form["OldPassword"], out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void ModifyPasswordHelper()
        {
            CommControllers2.CheckMemberApi(ClassName, "ModifyPassword", "修改密码")
                .AddArgument("oldpassword", typeof(string), "手机验证码")
                .AddArgument("password", typeof(string), "新密码")
                .AddResult(CommUtility.PASSWORD_EQUALS, "密码错误")
                .AddResult(CommUtility.UPDATE_FAIL, "修改失败")
                .AddResult(true, typeof(string), "成功");
        }
#endif

        public void GetUserInfo()
        {
            try
            {
                M.Member member;
                if (new CommControllers2(this).CheckMember(out member))
                {
                    SetResult(M.MemberInfo.GetById(DataSource, member.Id));
                }
            }
            catch (Exception)
            {
                SetResult(CommUtility.PROGRAM_ERROR);
            }
        }
#if (DEBUG)
        public static void GetUserInfoHelper()
        {
            CommControllers2.CheckMemberApi(ClassName, "GetUserInfo", "获取用户详细信息")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "成功状态");
        }
#endif

        [HttpPost]
        public void SetPayPassword()
        {
            M.Member member;
            object code, data;
            if (new CommControllers2(this).CheckMember(out member))
            {
                try
                {
                    code = CommPassport.SetPayPassword(DataSource, member, long.Parse(Request.Form["Mobile"]), Request["PayPassword"], Request.Form["SmsCaptcha"], out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void SetPayPasswordHelper()
        {
            CommControllers2.CheckMemberApi(ClassName, "SetPayPassword", "设置交易密码")
                .AddArgument("SmsCaptcha", typeof(string), "手机验证码")
                .AddArgument("Mobile", typeof(long), "手机号码")
                .AddArgument("PayPassword", typeof(string), "交易密码")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(CommUtility.MOBILE_FORMAT, "手机号码格式错误")
                .AddResult(CommUtility.SMSCAPTCHA_EQUALS, "验证码错误")
                .AddResult(CommUtility.MEMBER_NOTFOUND, "找不到该用户")
                .AddResult(CommUtility.UPDATE_FAIL, "修改失败")
                .AddResult(true, typeof(string), "成功返回token值,失败返回错误代码");
        }
#endif

        [HttpPost]
        public void ModifyInfo()
        {
            M.Member member;
            object code, data;
            if (new CommControllers2(this).CheckMember(out member))
            {
                try
                {
                    code = CommPassport.ModifyInfo(DataSource, member, Request.Form["Image"], Request.Form["NickName"], Request.Form["Sex"], out data);
                    new CommUtility(this).CommSetResult(code, data);
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void ModifyInfoHelper()
        {
            CommControllers2.CheckMemberApi(ClassName, "ModifyInfo", "修改用户信息")
                .AddArgument("Image", typeof(string), "头像地址,不修改则不传")
                .AddArgument("NickName", typeof(string), "昵称,不修改则不传")
                .AddArgument("Sex", typeof(int), "性别 0:未知,1:男,2:女,不修改则不传")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "成功状态");
        }
#endif

        [HttpPost]
        public void ModifyMobile()
        {

            M.Member member;
            object code, data;
            if (new CommControllers2(this).CheckMember(out member))
            {
                try
                {
                    code = CommPassport.ModifyMobile(DataSource, member, int.Parse(Request.Form["Type"]),long.Parse(Request.Form["NewMobile"]),Request.Form["PayPassword"],Request.Form["NewSmsCaptcha"], out data);
                    new CommUtility(this).CommSetResult(code, data);                    
                }
                catch (Exception ex)
                {
                    new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
                }
            }
        }
#if (DEBUG)
        public static void ModifyMobileHelper()
        {
            CommControllers2.CheckMemberApi(ClassName, "ModifyMobile", "修改绑定手机")
                .AddArgument("Type", typeof(int), "类别 0:绑定手机号,1:更改手机号")
                .AddArgument("PayPassword", typeof(string), "支付密码")
                .AddArgument("NewMobile", typeof(long), "新手机号码")
                .AddArgument("NewSmsCaptcha", typeof(string), "新手机验证码")
                .AddResult(CommUtility.PASSWORD_EQUALS, "交易密码验证错误")
                .AddResult(CommUtility.USER_EXISTENCE, "改用户已存在")
                .AddResult(CommUtility.SMSCAPTCHA_EQUALS, "验证码不正确")
                .AddResult(CommUtility.VERMOB_BIND, "type错误,当前账号已绑定手机号")
                .AddResult(CommUtility.VERMOB_NOT, "type错误,当前账号还没有绑定手机号")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "成功状态");
        }
#endif

        public void GetBilllist()
        {
            M.Member member;
            if (new CommControllers2(this).CheckMember(out member))
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
            CommControllers2.CheckMemberApi(ClassName, "GetBilllist", "获取用户账单")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                 .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(true, typeof(SplitPageData<M.MoneyRecord>), "成功返回");
        }
#endif

        public void CheckPayPassword()
        {
            M.Member member;
            if (new CommControllers2(this).CheckMember(out member))
            {
                
                string password = Request["PayPassWord"];
                if (!string.IsNullOrEmpty(password))
                {
                    if (M.MemberInfo.ApiCheckPayPassword(DataSource, member.Id, password))
                    {
                        SetResult(true);
                    }
                    else
                    {
                        SetResult(CommUtility.PASSWORD_EQUALS);
                    }
                }
                else
                {
                    SetResult(CommUtility.PASSWORD_EQUALS);
                }
            }
        }
#if (DEBUG)
        public static void CheckPayPasswordHelper()
        {
            CommControllers2.CheckMemberApi(ClassName, "CheckPayPassword", "验证支付密码")
                .AddArgument("PayPassWord", typeof(int), "md5(支付密码)")
                .AddResult(true, typeof(bool), "成功");
        }
#endif
    }
}
