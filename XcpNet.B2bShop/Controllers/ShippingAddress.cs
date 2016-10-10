using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Passport.Modules;
using Cnaws.Area;
using System.Collections.Generic;
using Cnaws.Web.Templates;

namespace XcpNet.B2bShop.Controllers.Extension
{
    public class ShippingAddress : Common.CommoSupplierController
    {
        [Authorize(true)]
        public void Index()
        {
            Render("shippingaddress.html");
        }

        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        [Distributor(true)]
        public void Submit()
        {
            try
            {
                M.ShippingAddress sa = DbTable.Load<M.ShippingAddress>(Request.Form);
                sa.UserId = User.Identity.Id;
                sa.Province = int.Parse(Request.Form["area_provinces"]);
                sa.City = int.Parse(Request.Form["area_cities"]);
                sa.County = int.Parse(Request.Form["area_counties"]);
                if (sa.Id != 0)
                    SetResult((int)sa.Modify(DataSource));
                else
                    SetResult((int)sa.Insert(DataSource));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [Authorize(true)]
        public void SetAddress(int id = 0)
        {
            Country County = Country.GetCountry();
            string target = Request.QueryString["target"];
            if (string.IsNullOrEmpty(target))
            {
                if (Request.UrlReferrer != null)
                    target = Request.UrlReferrer.ToString();
            }
            M.ShippingAddress address = M.ShippingAddress.GetById(DataSource, id, User.Identity.Id);
            int location;
            if (address == null)
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
            else
            {
                location = address.County;
            }
            this["GetAreaName"] = new FuncHandler((args) => {
                return County.GetCity(Convert.ToInt32(args[0])).Name;
            });
            this["Target"] = target;
            this["Location"] = location;
            this["Address"] = address;
            Render("set_address.html");
        }
        [Authorize(true)]
        [Distributor(true)]
        public void SelectAddress()
        {
            if (IsWap)
            {
                string id = Request.QueryString["Id"];
                this["Id"] = id;
                Render("select_address.html");
            }
            else
            {
                NotFound();
            }
        }

        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        [Distributor(true)]
        public void Delete()
        {
            try
            {
                SetResult(M.ShippingAddress.Remove(DataSource, long.Parse(Request.Form["Id"]), User.Identity.Id));
                IList<M.ShippingAddress> add = M.ShippingAddress.GetAll(DataSource, User.Identity.Id);
                if (add.Count>0)
                {
                    M.ShippingAddress s = add[0];
                    s.IsDefault = true;
                    s.Modify(DataSource);
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        [Distributor(true)]
        public void SetDefault()
        {
            try
            {
                SetResult(M.ShippingAddress.SetDefault(DataSource, long.Parse(Request.Form["Id"]), User.Identity.Id));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
        

        [HttpAjax]
        [Authorize(true)]
        [Distributor(true)]
        public void Get(long id)
        {
            try
            {
                SetResult(M.ShippingAddress.GetById(DataSource, id, User.Identity.Id));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [HttpAjax]
        [Authorize(true)]
        [Distributor(true)]
        public void List(int isHtml=0)
        {
            try
            {
                if (isHtml == 1)
                {
                    Country County = Country.GetCountry();
                    IList<M.ShippingAddress> add = M.ShippingAddress.GetAll(DataSource, User.Identity.Id);
                    this["List"] = add;
                    this["GetAreaName"] = new FuncHandler((args)=> {
                        return County.GetCity(Convert.ToInt32(args[0])).Name;
                    });
                    Render("ShippingAddress.html");
                }
                else
                {
                    SetResult(M.ShippingAddress.GetAll(DataSource, User.Identity.Id));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
    }
}
