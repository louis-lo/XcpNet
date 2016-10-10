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
using XcpNet.Common;
namespace XcpNet.ApiSecond.Controllers
{
    public sealed class LedPassport2 : Passport2
    {
        protected override void OnInitController()
        {
        }

        protected override void Dispose(bool disposing)
        {

            base.Dispose(disposing);
        }
        public void GetMachineInfo()
        {
            string mark;
            if (new CommControllers2(this).CheckMark(out mark))
            {
                P.Distributor d = A.MachineCode.GetDistributorByCode(DataSource, mark);
                if (d != null)
                    SetResult(d);
                else
                    SetResult(CommUtility.DISTRIBUTOR_EMPTY);
            }
        }
#if (DEBUG)
        public static void GetMachineInfoHelper()
        {
            CommControllers2.CheckMarkApi("LedPassport2", "GetMachineInfo", "根据机器mark找到绑定的供应商信息")
                .AddResult(CommUtility.DISTRIBUTOR_EMPTY, "找不到供应商信息")
                .AddResult(true, typeof(string), "成功返回Distributor信息");
        }
#endif

        public void BindMachine()
        {
            string mark;
            if (new CommControllers2(this).CheckMark(out mark))
            {
                try
                {
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
        }

#if (DEBUG)
        public static void BindMachineHelper()
        {
            CommControllers2.CheckMarkApi("LedPassport2", "BingMachine", "给机器定定加盟商")
                .AddArgument("mark", typeof(string), "mark")
                .AddArgument("UserName", typeof(long), "登录账号")
                .AddArgument("Password", typeof(string), "密码")
                .AddResult(CommUtility.MARK_EMPTY, "标识mark为空")
                .AddResult(CommUtility.MEMBER_NOTFOUND, "找不到该用户")
                .AddResult(CommUtility.PASSWORD_EQUALS, "密码错误")
                .AddResult(CommUtility.INSERT_FAIL, "插入数据失败")
                .AddResult(CommUtility.DISTRIBUTOR_NOTIS, "不是加盟商")
                .AddResult(CommUtility.PROGRAM_ERROR, "返回错误代码")
                .AddResult(true, typeof(string), "成功返回code:-200值");
        }
#endif

        public void CheckMachineCode()
        {
            string mark;
            if (new CommControllers2(this).CheckMark(out mark))
            {
                try
                {
                    long Memberid = 0;
                    long.TryParse(Request["memberid"], out Memberid);
                    A.MachineCode machinecode = A.MachineCode.GetByCode(DataSource, mark);
                    if (machinecode == null || machinecode.MemberId == 0)
                    {
                        SetResult(CommUtility.DISTRIBUTOR_EMPTY);
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
                        SetResult(CommUtility.DISTRIBUTOR_EMPTY);
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
        public static void CheckMachineCodeHelper()
        {
            CommControllers2.CheckMarkApi("LedPassport2", "CheckMachineCode", "检查加盟商的Mark跟Id是否对应")
                .AddArgument("mark", typeof(string), "mark")
                .AddArgument("memberid", typeof(long), "供应商Id")
                .AddArgument("longitude", typeof(double), "经度")
                .AddArgument("latitude", typeof(double), "纬度")
                .AddResult(CommUtility.MARK_EMPTY, "标识mark为空")
                .AddResult(CommUtility.DISTRIBUTOR_EMPTY, "找不到对应的加盟商")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "成功返回code:-200值");
        }
#endif
        public new void GetUserInfo()
        {
            try
            {
                M.Member member;
                if (new CommControllers2(this).CheckMember(out member))
                {
                    M.MemberInfo memberinfo = M.MemberInfo.GetById(DataSource, member.Id);
                    if (string.IsNullOrEmpty(M.MemberInfo.GetPayPasswordById(DataSource, member.Id)))
                        memberinfo.PayPassword = "0";
                    else memberinfo.PayPassword = "1";
                    memberinfo.Password = "";
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
                SetResult(CommUtility.PROGRAM_ERROR);
            }
        }
#if (DEBUG)
        public new static void GetUserInfoHelper()
        {
            CommControllers2.CheckMemberApi("LedPassport2", "GetUserInfo", "获取用户信息")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "成功返回code:-200值,UserInfo:用户基础信息,IsDistributor:是否是加盟商,Level:加盟商等级,Distributor:加盟商信息");
        }
#endif

        public bool IsDistributor(out P.Distributor distributor, long userid)
        {
            distributor = P.Distributor.GetById(DataSource, userid);
            if (distributor == null)
                return false;
            return true;
        }
    }
}
