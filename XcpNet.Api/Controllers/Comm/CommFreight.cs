using System;
using Cnaws.Area;
using Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using V = Cnaws.Verification.Modules;
using System.Collections.Specialized;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web;
using System.Text.RegularExpressions;
using Cnaws.Web;

namespace XcpNet.Api.Controllers
{
    public class CommFreight : Cnaws.Product.Controllers.Freight
    {
        public static string ClassName = "[type]Freight";
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
        public void Show()
        {
            try
            {
                long productId; int p = 0; int c = 0 ,count=1;
                string mark = Request["mark"];
                if (string.IsNullOrEmpty(mark))
                    SetResult(ApiUtility.PARAMETER_NOFOND);
                long.TryParse(Request["id"], out productId);
                int.TryParse(Request["p"], out p);
                int.TryParse(Request["c"], out c);
                int.TryParse(Request["count"], out count);
                using (Country country = Country.GetCountry())
                {
                    City province, city;
                    try
                    {
                        if (p > 0 || c > 0)
                        {
                            if (c > 0)
                            {
                                city = country.GetCity(c);
                                province = country.GetCity(city.ParentId);
                            }
                            else
                            {
                                province = country.GetCity(p);
                                city = country.GetCities(province.Id)[0];
                            }
                        }
                        else
                        {
                            IPLocation local;
                            using (IPArea area = new IPArea())
                                local = area.Search(ClientIp);
                            city = local.GetCity(country);
                            if (city.ParentId > 0)
                            {
                                province = country.GetCity(city.ParentId);
                            }
                            else
                            {
                                province = city;
                                city = country.GetCities(province.Id)[0];
                            }
                        }
                        if (province == null || city == null)
                            throw new Exception();
                    }
                    catch (Exception)
                    {
                        province = country.GetCity(440000);
                        city = country.GetCity(441900);
                    }
                    string Money = Product.GetById(DataSource, productId).GetNewFreightString(DataSource, province.Id, city.Id,count);
                    SetResult(new
                    {
                        Province = province,
                        City = city,
                        Freight = Money
                    });
                }
            }
            catch (Exception ex)
            {
                SetResult(ApiUtility.RUN_ERROR, new { Message = ex.ToString() });
            }
        }
#if (DEBUG)
        public static void ShowHelper()
        {
            AddApiMethod(ClassName, "Show", "获取邮费模板")
                .AddArgument("mark", typeof(string), "mark")
                .AddArgument("id", typeof(long), "产品编号")
                .AddArgument("p", typeof(int), "省Id")
                .AddArgument("c", typeof(int), "城市Id")
                .AddArgument("count", typeof(int), "产品数量")
                .AddResult(true, typeof(string), "Province:省信息,City:市信息,Freight:运费信息");
        }
#endif



    }
}
