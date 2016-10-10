using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Web;
using Cnaws.Web.Configuration;
using Cnaws.Area;
using Cnaws.Json;
using XcpNet.Common.Address;
using System.Web;

namespace XcpNet.Common
{
    public abstract class CommonPayController : Cnaws.Pay.Controllers.PaymentBase
    {
        string locationCookieName = "LOCATION_COOKIE";

        /// <summary>
        ///获取地区编号，省份，城市，地区
        /// </summary>
        /// <returns></returns>
        public Location Location
        {
            get
            {
                return GetLocal();
            }
        }

        protected Location GetLocal()
        {
            Location loca = new Location();
            string val = Request.Cookies[locationCookieName]?.Value;

            if (!string.IsNullOrEmpty(val))
            {
                loca = JsonValue.Deserialize<Location>(
                    Encoding.UTF8.GetString(PassportAuthentication.DecodeCookie(val)));
            }
            return loca;
        }
        /// <summary>
        /// 把地址保存到cookie中
        /// </summary>
        [HttpAjax]
        [HttpPost]
        public void SetLocation()
        {
            int province = 0, city = 0, county = 0;
            int.TryParse(Request["Province"], out province);
            int.TryParse(Request["City"], out city);
            int.TryParse(Request["County"], out county);
            try
            {
                if (province > 0 && city > 0 && county > 0)
                {
                    Location local = new Location();
                    local.ProvinceId = province;
                    local.CityId = city;
                    local.CountyId = county;

                    Country country = Country.GetCountry();
                    local.ProvinceTxt = country.GetCity(province).Name;
                    local.CityTxt = country.GetCity(city).Name;
                    local.CountyTxt = country.GetCity(county).Name;

                    HttpCookie locationCookie = new HttpCookie(locationCookieName);
                    string val = JsonValue.Serialize(local);
                    locationCookie.Value = PassportAuthentication.EncodeCookie(Encoding.UTF8.GetBytes(val));
                    locationCookie.Expires = DateTime.Now.AddYears(1);
                    Response.SetCookie(locationCookie);

                    PassportSection captchaSection = PassportSection.GetSection();
                    if (!string.IsNullOrEmpty(captchaSection.CookieDomain))
                        locationCookie.Domain = captchaSection.CookieDomain;

                    SetResult(true);
                }
                else
                {
                    throw new ArgumentNullException("参数无关");
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }

        }

    }
}
