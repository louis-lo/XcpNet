using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Passport.Modules;
using Cnaws.Area;
using Cnaws.Templates;
using Cnaws;

namespace XcpNet.Passport.Controllers.Extension
{
    public class MemberInfo : Common.CommPassportController
    {
        [Authorize(true)]
        public void Index()
        {
            M.MemberInfo member = M.MemberInfo.GetByModify(DataSource, User.Identity.Id);
            DateTime begin = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);
            if (member.Birthday < begin)
                member.Birthday = begin;
            int location = member.County;
            if (location == 0)
            {
                City city;
                using (IPArea area = new IPArea())
                {
                    IPLocation local = area.Search(ClientIp);
                    using (Country country = Country.GetCountry())
                        city = local.GetCity(country);
                }
                location = city != null ? city.Id : 441900;
            }
            this["Location"] = location;
            this["Member"] = member;
            Render("memberinfo.html");
        }

        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void Submit()
        {
            try
            {
                M.MemberInfo member = DbTable.Load<M.MemberInfo>(Request.Form);
                member.Id = User.Identity.Id;
                member.Province = int.Parse(Request.Form["area_provinces"]);
                member.City = int.Parse(Request.Form["area_cities"]);
                member.County = int.Parse(Request.Form["area_counties"]);
                SetResult(member.Modify(DataSource));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
    }
}
