using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Area;
using Cnaws.Web.Templates;
using P=Cnaws.Product.Modules;

namespace XcpNet.Website.Controllers.Extension
{
    public sealed class Freight : Cnaws.Product.Controllers.Freight
    {
        public void Show(long id, long productId, int p = 0, int c = 0)
        {
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
                this["Province"] = province;
                this["City"] = city;
                this["Provinces"] = country.GetProvinces();
                this["Cities"] = country.GetCities(province.Id);
                this["Product"] = P.Product.GetById(DataSource, productId);
                Info(id);
            }
        }
    }
}
