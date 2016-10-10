using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Product.Modules;
using P = Cnaws.Passport.Modules;
using System.Collections.Generic;
using XcpNet.Common;
using Cnaws;

namespace XcpNet.Common.Controllers.Extension
{
    public class Cart : CommPassportController
    {
        [Authorize(true)]
        public virtual void Index()
        {
            IList<DataJoin<M.ProductCart, M.Product>> list = M.ProductCart.GetPageByUser(DataSource, User.Identity.Id);
            Money total = 0;
            foreach (DataJoin<M.ProductCart, M.Product> item in list)
                total += item.B.GetTotalMoney(item.A.Count);
            this["CartList"] = list;
            this["TotalMoney"] = total;
            Render("cart.html");
        }

        [HttpAjax]
        [Authorize]
        public virtual void GetList()
        {
            try
            {
                SetResult(M.ProductCart.GetPageByUser(DataSource, User.Identity.Id));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [HttpAjax]
        [Authorize]
        public virtual void Count()
        {
            try
            {
                SetResult(true, M.ProductCart.GetCountByUser(DataSource, User.Identity.Id));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [HttpAjax]
        [HttpPost]
        [Authorize]
        public virtual void Add()
        {
            object code, data;
            try
            {
                P.Member member = new P.Member { Id = User.Identity.Id };

                code = CommonBuy.CommAddToCart<M.ProductCart>(DataSource, member, Request.Form["Id"], Request.Form["Count"],
                     Location.ProvinceId, Location.CityId, Location.CountyId, out data);
                new CommUtility(this).CommSetResult(code, data);
            }
            catch (Exception ex)
            {
                new CommUtility(this).CommSetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.Message });
            }
        }

        [HttpAjax]
        [HttpPost]
        [Authorize]
        public virtual void Del()
        {
            try
            {

                string temp = Request.Form["id"];
                string[] ids = temp.Split(',');
                if (ids.Length > 0)
                {
                    DataSource.Begin();
                    try
                    {
                        for (int i = 0; i < ids.Length; ++i)
                        {
                            if ((new M.ProductCart() { Id = long.Parse(ids[i]), UserId = User.Identity.Id }).Remove(DataSource) != DataStatus.Success)
                                throw new Exception();
                        }
                        DataSource.Commit();
                        SetResult(true);
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        SetResult((int)-1020);
                    }
                }
                else
                {
                    SetResult((int)-1023);
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
    }
}
