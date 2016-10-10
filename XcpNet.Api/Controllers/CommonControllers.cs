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

namespace XcpNet.Api.Controllers
{
    public abstract class CommonControllers : Recharge
    {
        protected override void OnIndex()
        {
            NotFound();
        }
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
        protected static ApiMethod CheckDistributorHelper(ApiMethod m)
        {
            return m
                .AddArgument("mark", typeof(string), "标识")
                .AddResult(ApiUtility.MARK_EMPTY, "标识为空");
        }
        protected static ApiMethod CheckDistributorHelper(string ns, string name, string summary)
        {
            return CheckMarkHelper(CheckSignHelper(AddApiMethod(ns, name, summary)));
        }
#endif

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
                if (data["sign"].ToUpper() == MakeSign(data))
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

    }
}
