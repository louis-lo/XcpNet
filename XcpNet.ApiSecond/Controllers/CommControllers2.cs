using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws;
using Cnaws.Data;
using Cnaws.Web;
using Cnaws.Passport.Controllers;
using V = Cnaws.Verification.Modules;
using A = XcpNet.Ad.Modules;
using M = Cnaws.Passport.Modules;
using Pd = Cnaws.Product.Modules;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Web;
using System.Text.RegularExpressions;
using XcpNet.Common;

namespace XcpNet.ApiSecond.Controllers
{
    public class CommControllers2 : Recharge
    {
        public CommControllers2()
        {

        }
        public CommControllers2(DataController controller)
        {
            InitController(controller);
        }
        protected override void OnIndex()
        {
            NotFound();
        }
        public bool CheckMobile(out long mobile)
        {
            bool ret = false;
            if (long.TryParse(Request["mobile"], out mobile))
                ret = CommUtility.CheckMobile(mobile);
            if (!ret)
                SetResult(CommUtility.MOBILE_FORMAT);
            return ret;
        }

        public bool CheckCaptcha(long mobile, string hash)
        {
            return V.MobileHash.Equals(DataSource, mobile, V.MobileHash.Register, hash);
        }

        public bool IsDistributor(out Pd.Distributor distributor,long userid)
        {
            distributor = Pd.Distributor.GetById(DataSource, userid);
            if (distributor == null)
                return false;
            return true;
        }

        public bool CheckDistributor(out Pd.Distributor distributor)
        {
            if (string.IsNullOrEmpty(Request["mark"]))
            {
                distributor = null;
                return false;
            }
            distributor = A.MachineCode.GetDistributorByCode(DataSource, Request["mark"]);
            return true;
        }
#if (DEBUG)
        public static ApiMethod CheckDistributorApi(ApiMethod m)
        {
            return m
                .AddArgument("mark", typeof(string), "标识")
                .AddResult(CommUtility.MARK_EMPTY, "标识为空");
        }
        public static ApiMethod CheckDistributorApi(string ns, string name, string summary)
        {
            return CheckMarkApi(CheckSignApi(AddApiMethod(ns, name, summary)));
        }
#endif
        
        public bool CheckVersion(out int code)
        {
            string version;
            bool hasversion = false;
            if (Array.IndexOf(Request.QueryString.AllKeys, "Version") > 0 || Array.IndexOf(Request.Form.AllKeys, "Version") > 0)
                hasversion = true;
            if (hasversion)
            {
                version = Request["Version"];
                if (string.IsNullOrEmpty(version))
                {
                    code = CommUtility.VERSION_LOW;
                    return false;
                }
                if(A.MachineVersion.GetVersionByName(DataSource, "Machine").Version != version)
                {
                    code = CommUtility.VERSION_LOW;
                    return false;
                }
            }
            code = CommUtility.SUCCESS;
            return true;
        }

        public bool CheckMark(out string mark)
        {
            mark = Request["mark"];
            if (string.IsNullOrEmpty(mark))
            {
                SetResult(CommUtility.MARK_EMPTY);
                return false;
            }
            return true;
        }
#if (DEBUG)
        public static ApiMethod CheckMarkApi(ApiMethod m)
        {
            return m
                .AddArgument("mark", typeof(string), "标识")
                .AddResult(CommUtility.MARK_EMPTY, "标识为空");
        }
        public static ApiMethod CheckMarkApi(string ns, string name, string summary)
        {
            return CheckMarkApi(CheckSignApi(AddApiMethod(ns, name, summary)));
        }
#endif
        public bool CheckToken(out Guid token, out M.Member member)
        {
            if (!Guid.TryParse(Request["token"], out token) || Guid.Empty.Equals(token))
            {
                member = null;
                SetResult(CommUtility.TOKEN_EMPTY);
                return false;
            }
            member = M.Member.GetByToken(DataSource, token);
            if (member == null)
            {
                SetResult(CommUtility.MEMBER_NOTFOUND);
                return false;
            }
            string mark = Request["mark"];
            if (string.IsNullOrEmpty(mark))
            {
                SetResult(CommUtility.MARK_EMPTY);
                return false;
            }
            if (!string.Equals(mark, member.Mark))
            {
                SetResult(CommUtility.MARK_EQUALS);
                return false;
            }
            if (!token.Equals(member.Token))
            {
                SetResult(CommUtility.TOKEN_EQUALS);
                return false;
            }
            return true;
        }
#if (DEBUG)
        public static ApiMethod CheckTokenApi(ApiMethod m)
        {
            return m
                .AddArgument("token", typeof(string), "Token")
                .AddArgument("mark", typeof(string), "标识")
                .AddResult(CommUtility.TOKEN_EMPTY, "Token为空")
                .AddResult(CommUtility.MEMBER_NOTFOUND, "未找到用户")
                .AddResult(CommUtility.MARK_EMPTY, "标识为空")
                .AddResult(CommUtility.MARK_EQUALS, "标识错误")
                .AddResult(CommUtility.TOKEN_EQUALS, "Token错误");
        }
        public static ApiMethod CheckTokenApi(string ns, string name, string summary)
        {
            return CheckTokenApi(CheckSignApi(AddApiMethod(ns, name, summary)));
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
                        SetResult(CommUtility.MEMBER_LOCKED);
                    }
                }
                else
                {
                    SetResult(CommUtility.MEMBER_APPROVED);
                }
            }
            else
            {
                member = null;
            }
            return ret;
        }

#if (DEBUG)
        public static ApiMethod CheckMemberApi(ApiMethod m)
        {
            return m
                .AddResult(CommUtility.MEMBER_LOCKED, "用户已锁定")
                .AddResult(CommUtility.MEMBER_APPROVED, "用户未审核");
        }
        public static ApiMethod CheckMemberApi(string ns, string name, string summary)
        {
            return CheckMemberApi(CheckTokenApi(ns, name, summary));
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
                if (data["sign"].ToUpper() == MakeSign(data))
                    return true;
            }
            throw new JsonResultException(CommUtility.SIGN_ERROR);
        }
#if (DEBUG)
        public static ApiMethod CheckSignApi(ApiMethod m)
        {
            return m
                .AddResult(CommUtility.SIGN_ERROR, "验证签名失败");
        }
#endif

    }
}
