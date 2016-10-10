using System;
using Cnaws.Area;
using Cnaws.Product.Modules;

namespace XcpNet.Api.Controllers
{
    public class ApiFreight : Cnaws.Product.Controllers.Freight
    {
        public void Show()
        {
            try
            {
                long productId; int p = 0; int c = 0;
                string mark = Request["mark"];
                if (string.IsNullOrEmpty(mark))
                    SetResult(ApiUtility.PARAMETER_NOFOND);
                long.TryParse(Request["id"], out productId);
                int.TryParse(Request["p"], out p);
                int.TryParse(Request["c"], out c);
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
                    string Money = Product.GetById(DataSource, productId).GetFreightString(DataSource, province.Id, city.Id);
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
                SetResult(ApiUtility.RUN_ERROR);
            }
        }
#if (DEBUG)
        public static void ShowHelper()
        {
            AddApiMethod("ApiFreight", "Show", "获取邮费模板")
                .AddArgument("mark", typeof(string), "mark")
                .AddArgument("id", typeof(long), "产品编号")
                .AddArgument("p", typeof(int), "省Id")
                .AddArgument("c", typeof(int), "城市Id")
                .AddResult(true, typeof(string), "Province:省信息,City:市信息,Freight:运费信息");
        }
#endif

    }
}
