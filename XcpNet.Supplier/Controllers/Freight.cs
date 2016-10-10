using System;
using System.Collections.Generic;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Area;
using Cnaws.Web.Templates;
using Cnaws.Sms.Modules;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using D = XcpNet.Supplier.Modules.Modules;
using XcpNet.Services.Sms;
using System.Linq;
using Cnaws.Data.Query;
using System.Text.RegularExpressions;
using System.Data;
using Cnaws;

namespace XcpNet.Supplier.Controllers.Extension
{
    public class Freight : SupplierController
    {
        private Country _country = null;

        public Country Country
        {
            get
            {
                if (_country == null)
                    _country = Country.GetCountry();
                return _country;
            }
        }
        private void Template(long user)
        {
            this["GetArea"] = new FuncHandler((args) =>
            {
                int p = Convert.ToInt32(args[0]);
                int c = Convert.ToInt32(args[1]);
                if (p == 0) return "全国";
                if (c == 0) return Country.GetCity(p).ShortName;
                return Country.GetCity(c).ShortName;
            });
            this["Templates"] = P.FreightTemplate.GetAllBySeller(DataSource, user);
            Render("supplier/freight_template.html");
        }
        [SupplierOrDistributor(true)]
        public void Template()
        {
            Template(User.Identity.Id);
        }
        [AdminAuthorize]
        [SupplierOrDistributor]
        public void TemplateEx(long user)
        {
            Template(user);
        }
        [SupplierOrDistributor]
        public void Edit(long id = 0)
        {
            this["GetCities"] = new FuncHandler((args) =>
            {
                int area = Convert.ToInt32(args[0]);
                return Country.GetCities(area);
            });
            this["GetProvinces"] = new FuncHandler((args) =>
            {
                return Country.GetProvinces();
            });
            this["GetAreaId"] = new FuncHandler((args) =>
            {
                int p = Convert.ToInt32(args[0]);
                int c = Convert.ToInt32(args[1]);
                return Math.Max(p, c);
            });
            this["GetArea"] = new FuncHandler((args) =>
            {
                int p = Convert.ToInt32(args[0]);
                int c = Convert.ToInt32(args[1]);
                if (p == 0) return "全国";
                if (c == 0) return Country.GetCity(p).ShortName;
                return Country.GetCity(c).ShortName;
            });
            P.FreightTemplate freighttemplate;
            if (id == 0)
                freighttemplate = new P.FreightTemplate();
            else
                freighttemplate = P.FreightTemplate.GetById(DataSource, id);
            this["Template"] = freighttemplate;
            Render("supplier/freight_edit.html");
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void Submit(int id = 0)
        {
            DataSource.Begin();
            try
            {
                P.FreightTemplate FreightTemplate = DbTable.Load<P.FreightTemplate>(Request.Form);
                int.TryParse(Request.Form["area_provinces"], out FreightTemplate.Province);
                int.TryParse(Request.Form["area_cities"], out FreightTemplate.City);
                int.TryParse(Request.Form["area_counties"], out FreightTemplate.County);
                FreightTemplate.EditTime = DateTime.Now;
                FreightTemplate.SellerId = User.Identity.Id;
                FreightTemplate.Id = id;
                if (FreightTemplate.Id <= 0)
                {
                    if (FreightTemplate.Insert(DataSource) != DataStatus.Success)
                        throw new AggregateException("6");
                }
                else
                {
                    if (FreightTemplate.ModByIdAndUserId(DataSource) != DataStatus.Success)
                        throw new AggregateException("5");
                }
                Regex reg = new Regex(@"Area_\d*");
                List<string> Keys = Request.Form.AllKeys.Where(name => reg.IsMatch(name)).ToList();
                foreach (string key in Keys)
                {

                    int MappingId = 0;
                    string inputId = key.Replace("Area_", "");
                    int.TryParse(inputId, out MappingId);
                    if (MappingId != 0)
                    {
                        P.FreightMapping FreightMapaing;
                        if (MappingId < 0)
                        {
                            FreightMapaing = new P.FreightMapping();
                        }
                        else
                        {
                            FreightMapaing = P.FreightMapping.GetById(DataSource, MappingId);
                        }
                        FreightMapaing.TemplateId = FreightTemplate.Id;
                        Money.TryParse(Request.Form["Money_" + MappingId],out FreightMapaing.Money);
                        int.TryParse(Request.Form["Number_" + MappingId],out FreightMapaing.Number);
                        Money.TryParse(Request.Form["StepMoney_" + MappingId],out FreightMapaing.StepMoney);
                        int.TryParse(Request.Form["StepNumber_" + MappingId], out FreightMapaing.StepNumber);
                        if (FreightTemplate.Type != P.FreightTemplate.ValuationType.ThePrece)
                        {
                            FreightMapaing.Number *= 100;
                            FreightMapaing.StepNumber *= 100;
                        }
                        if (FreightMapaing.InsertOrUpdate(DataSource) != DataStatus.Success)
                            throw new AggregateException("4");
                        string Area = Request.Form["Area_" + MappingId];
                        string[] AreaList = Area.TrimEnd(',').Split(',');

                        if (MappingId > 0)
                        {
                            if (P.FreightAreaMapping.DeleteByMapping(DataSource, MappingId) != DataStatus.Success)
                                throw new AggregateException("3");
                        }

                        P.FreightAreaMapping area;
                        for (int j = 0; j < AreaList.Length; ++j)
                        {
                            if (!string.IsNullOrEmpty(AreaList[j]))
                            {
                                int p = 0;
                                int c = 0;
                                City city = Country.GetCity(int.Parse(AreaList[j]));
                                if (city.Level == CityLevel.City)
                                {
                                    p = city.ParentId;
                                    c = city.Id;
                                }
                                else if (city.Level == CityLevel.Province)
                                {
                                    p = city.Id;
                                }
                                area = new P.FreightAreaMapping()
                                {
                                    TemplateId = FreightMapaing.TemplateId,
                                    MappingId = FreightMapaing.Id,
                                    ProvinceId = p,
                                    CityId = c
                                };
                                if (area.Insert(DataSource) != DataStatus.Success)
                                    throw new AggregateException("2");
                            }
                        }
                    }
                    else
                    {
                        throw new AggregateException("1");
                    }
                }

                DataSource.Commit();
                SetResult(true);
            }
            catch (AggregateException ag)
            {
                DataSource.Rollback();
                SetResult(false);
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                SetResult(false,ex.ToString());
            };

        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void DelAreaMapping(int mappingid)
        {
            DataSource.Begin();

            try
            {
                if (P.FreightMapping.DelById(DataSource, mappingid) != DataStatus.Success)
                    throw new Exception();
                if (P.FreightAreaMapping.DeleteByMapping(DataSource, mappingid) != DataStatus.Success)
                    throw new Exception();
                DataSource.Commit();
                SetResult(true);
            }
            catch (Exception)
            {
                DataSource.Rollback();
                SetResult(false);
            }
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void DelTemplate(long id)
        {
            DataSource.Begin();
            try
            {
                P.FreightTemplate tmap = P.FreightTemplate.GetById(DataSource, id);
                if (tmap == null || tmap.SellerId != User.Identity.Id)
                    throw new Exception();
                if (tmap.Delete(DataSource) != DataStatus.Success)
                    throw new Exception();

                DataSource.Commit();
                SetResult(true);
            }
            catch (Exception)
            {
                DataSource.Rollback();
                SetResult(false);
            }
        }

    }
}
