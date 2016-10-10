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
using XcpNet.Common;
namespace XcpNet.ApiSecond.Controllers
{
    public class Freight2 : CommControllers2
    {
        public static string ClassName = "[type]Freight2";
        protected override void OnInitController()
        {
            NotFound();
        }
        
        public void Show()
        {
            string mark;
            if (CheckMark(out mark))
            {
                try
                {
                    long productId; int p = 0; int c = 0; int count=1;
                    long.TryParse(Request["id"], out productId);
                    int.TryParse(Request["province"], out p);
                    int.TryParse(Request["city"], out c);
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
                        string Money = Product.GetById(DataSource, productId).GetNewFreightString(DataSource, province.Id, city.Id, count);
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
                    SetResult(CommUtility.RUN_ERROR, new { Message = ex.ToString() });
                }
            }
        }
#if (DEBUG)
        public static void ShowHelper()
        {
            CheckMarkApi(ClassName, "Show", "获取邮费模板")
                .AddArgument("id", typeof(long), "产品编号")
                .AddArgument("province", typeof(int), "省Id")
                .AddArgument("city", typeof(int), "城市Id")
                .AddArgument("count", typeof(int), "购买的数量,默认为1")
                .AddResult(true, typeof(string), "Province:省信息,City:市信息,Freight:运费信息");
        }
#endif



    }
}
