using System;
using System.Collections.Generic;
using Cnaws;
using Cnaws.Web;
using Cnaws.Data;
using M = XcpNet.Supplier.Modules.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Web.Templates;
using System.Linq;

namespace XcpNet.B2bShop.Controllers.Extension
{
    public class Cart : Common.CommoSupplierController
    {
        [Authorize(true)]
        [Distributor(true)]
        public virtual void Index()
        {
            if (IsDistributor())
            {
                if (Distributor.State == P.DistributorState.NotApproved && IsWap)
                {
                    Refresh("http://wapsupplier.xcpnet.com/joinus/wait.html");
                }
                else
                {
                    long[] SupplierIds = M.DistributorCart.GetBySupplierId(DataSource, User.Identity.Id);
                    if (SupplierIds.Length > 0)
                    {
                        this["SupplierList"] = P.Supplier.GetAndStorInfoByIds(DataSource, SupplierIds);
                    }
                    else
                    {
                        this["SupplierList"] = null;
                    }
                    IList<DataJoin<M.DistributorCart, M.DistributorProduct>> list = M.DistributorCart.GetPageByUser(DataSource, User.Identity.Id);
                    Money total = 0;
                    foreach (DataJoin<M.DistributorCart, M.DistributorProduct> item in list)
                        total += item.B.GetTotalMoney(item.A.Count);
                    this["CartList"] = list;
                    this["GetCartListBySupplierId"] = new FuncHandler((args) =>
                    {
                        long supplierId = Convert.ToInt32(args[0]);
                        KeyValuePair<Money, List<DataJoin<M.DistributorCart, M.DistributorProduct>>> dt;
                        List<DataJoin<M.DistributorCart, M.DistributorProduct>> newlist = list.Where((x) => x.A.SupplierId == supplierId).ToList();

                        Money newtotal = 0;
                        foreach (DataJoin<M.DistributorCart, M.DistributorProduct> item in newlist)
                            newtotal += item.B.GetTotalMoney(item.A.Count);
                        dt = new KeyValuePair<Money, List<DataJoin<M.DistributorCart, M.DistributorProduct>>>(newtotal, newlist);
                        return dt;
                    });
                    this["TotalMoney"] = total;
                    Render("cart.html");
                }
            }

        }

        [HttpAjax]
        [Authorize(true)]
        public virtual void GetList()
        {
            try
            {
                SetResult(M.DistributorCart.GetPageByUser(DataSource, User.Identity.Id));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [HttpAjax]
        [Authorize(true)]
        public virtual void Count()
        {
            try
            {
                SetResult(true, M.DistributorCart.GetCountByUser(DataSource, User.Identity.Id));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
        /// <summary>
        /// 支持多个产品添加到进货车中
        /// </summary>
        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
        public virtual void Add()
        {
            //string tmpIds = Request["Id"], tmpCounts = Request["Count"];
            P.Distributor distributor = P.Distributor.GetById(DataSource, User.Identity.Id);
            object code, data;
            code = XcpNet.Common.CommonBuy.CommAddToCart<M.DistributorCart>(DataSource, new Cnaws.Passport.Modules.Member() { Id=User.Identity.Id}, Request.Form["Id"], Request.Form["Count"], distributor.Province, distributor.City, distributor.County, out data);
            new XcpNet.Common.CommUtility(this).CommSetResult(code, data);
            //try
            //{
            //    if (string.IsNullOrEmpty(tmpIds) || string.IsNullOrEmpty(tmpCounts))
            //        throw new ArgumentNullException("参数无效");

            //    string[] ids = tmpIds.Split(','), counts = tmpCounts.Split(',');

            //    for (int i = 0; i < ids.Length; i++)
            //    {
            //        long productId = long.Parse(ids[i]);
            //        int count = int.Parse(counts[i]);
            //        M.DistributorProduct product = M.DistributorProduct.GetSaleProduct(DataSource, productId);
            //        if (product == null)
            //        {
            //            SetResult(-1023);
            //            break;
            //        }
            //        M.DistributorCart cart = new M.DistributorCart(DataSource, User.Identity.Id, product, count);
            //        if (cart.Count <= product.Inventory)
            //        {
            //            M.DistributorCart productcart = M.DistributorCart.GetProductByUser(DataSource, User.Identity.Id, productId);
            //            if (productcart == null || productcart.Id <= 0)
            //            {
            //                cart.Add(DataSource);
            //            }
            //            else
            //            {
            //                productcart.Count += count;
            //                productcart.Update(DataSource);
            //            }
            //        }
            //        else
            //        {
            //            SetResult(-1027);
            //            break;
            //        }
            //    }

            //    SetResult(true);
            //}
            //catch (Exception ex)
            //{
            //    SetResult(false, ex.Message);
            //}
        }

        [HttpAjax]
        [HttpPost]
        [Authorize(true)]
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
                            if ((new M.DistributorCart() { Id = long.Parse(ids[i]), UserId = User.Identity.Id }).Remove(DataSource) != DataStatus.Success)
                                throw new Exception();
                        }
                        DataSource.Commit();
                        SetResult(true);
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        SetResult(-1020);
                    }
                }
                else
                {
                    SetResult(-1023);
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
    }
}
