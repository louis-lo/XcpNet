using System;
using Cnaws.Data;
using P = Cnaws.Product.Modules;
using Cnaws.Web.Templates;
using M = Cnaws.Passport.Modules;
using Cnaws.Data.Query;
using System.Collections.Generic;
using D = XcpNet.Supplier.Modules.Modules;
using Cnaws.Web;
using Cnaws;

namespace XcpNet.Supplier.Controllers
{
    public sealed class DistributorManager : SupplierController
    {
        [Distributor(true)]
        public void Manager(string state, int page = 1)
        {

            int categoryId = 0;
            int.TryParse(Request["CategoryId"], out categoryId);
            string keyword = "";
            keyword = Request["KeyWord"];
            this["CategoryName"] = "所有产品";
            DbWhereQueue where = new DbWhereQueue();
            if (state == "onshelves")///上架商品
            {
                where = new DbWhere<D.DistributorProduct>("State", P.ProductState.BeforeSale);
                where &= new DbWhere<D.DistributorProduct>("SupplierId").InSelect<P.Supplier>(new DbSelect("UserId")).Where(new DbWhere("Subjection", User.Identity.Id)).Result();
            }
            else if (state == "offshelves")///下架商品
            {
                where = (new DbWhere<D.DistributorProduct>("State", P.ProductState.Sale) | new DbWhere<D.DistributorProduct>("State", P.ProductState.BeforeSaved));
                where &= new DbWhere<D.DistributorProduct>("SupplierId").InSelect<P.Supplier>(new DbSelect("UserId")).Where(new DbWhere("Subjection", User.Identity.Id)).Result();
            }
            if (state == "whole")///全国优选
            {
                where = (new DbWhere<D.DistributorProduct>("State", P.ProductState.Sale) | new DbWhere<D.DistributorProduct>("State", P.ProductState.BeforeSaved));
                where &= new DbWhere<D.DistributorProduct>("SupplierId").InSelect<P.Supplier>(new DbSelect("UserId")).Where(new DbWhere("Subjection", 0) & new DbWhere("SupplierType", 0)).Result();
                where &= (new DbWhere<D.DistributorSalesArea>("Province", Distributor.Province) | new DbWhere<D.DistributorSalesArea>("Province", 0));
                where &= (new DbWhere<D.DistributorSalesArea>("City", Distributor.City) | new DbWhere<D.DistributorSalesArea>("City", 0));
                where &= (new DbWhere<D.DistributorSalesArea>("County", Distributor.County) | new DbWhere<D.DistributorSalesArea>("County", 0));
            }
            if (categoryId != 0)
            {
                IList<D.DistributorCategory> cates = D.DistributorCategory.GetAllParentsById(DataSource, categoryId);
                if (cates.Count > 0)
                    this["CategoryName"] = cates[0].Name;
                if (cates.Count == 3)
                    where &= new DbWhere<D.DistributorProduct>("CategoryId", categoryId);
                else if (cates.Count == 2)
                    where &= (new DbWhere<D.DistributorProduct>("CategoryId", categoryId) | new DbWhere<D.DistributorProduct>("CategoryId").InSelect<D.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result());
                else if (cates.Count == 1)
                    where &= (new DbWhere<D.DistributorProduct>("CategoryId", categoryId) | new DbWhere<D.DistributorProduct>("CategoryId").InSelect<D.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result() | new DbWhere<D.DistributorProduct>("CategoryId").InSelect<D.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId").InSelect<D.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result()).Result());
            }

            where &= new DbWhere<D.DistributorProduct>("ParentId", 0);
            if (!string.IsNullOrEmpty(keyword) && keyword != "_")
                where = new DbWhere<D.DistributorProduct>("Title", keyword, DbWhereType.Like);
            long count;
            IList<dynamic> list;
            if (state == "whole")///全国优选
            {
                DbWhereQueue areawhere = new DbWhereQueue();
                areawhere = new DbWhere<D.DistributorAreaMapping>("Province", Distributor.Province);
                areawhere &= new DbWhere<D.DistributorAreaMapping>("City", Distributor.City);
                areawhere &= new DbWhere<D.DistributorAreaMapping>("County", Distributor.County);

                list = Db<D.DistributorProduct>.Query(DataSource)
                 .Select(new DbSelect<D.DistributorProduct>(), new DbSelect<M.Member>("Name"), new DbSelect<D.DistributorCategory>("Name"), new DbSelect<D.DistributorAreaMapping>("Saled"))
                 .InnerJoin(new DbColumn<D.DistributorProduct>("SupplierId"), new DbColumn<M.Member>("Id"))
                 .InnerJoin(new DbColumn<D.DistributorProduct>("CategoryId"), new DbColumn<D.DistributorCategory>("Id"))
                 .LeftJoin(new DbColumn<D.DistributorProduct>("Id"), new DbColumn<D.DistributorAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                 .InnerJoin(new DbColumn<D.DistributorProduct>("Id"), new DbColumn<D.DistributorSalesArea>("ProductId"))
                 .Where(where)
                 .OrderBy(new DbOrderBy<D.DistributorProduct>("CreationDate", DbOrderByType.Desc))
                 .ToList(20, Math.Max(1, page), out count);
            }
            else
            {
                list = Db<D.DistributorProduct>.Query(DataSource)
                 .Select(new DbSelect<D.DistributorProduct>(), new DbSelect<M.Member>("Name"), new DbSelect<D.DistributorCategory>("Name"))
                 .InnerJoin(new DbColumn<D.DistributorProduct>("SupplierId"), new DbColumn<M.Member>("Id"))
                 .InnerJoin(new DbColumn<D.DistributorProduct>("CategoryId"), new DbColumn<D.DistributorCategory>("Id"))
                 .Where(where)
                 .OrderBy(new DbOrderBy<D.DistributorProduct>("CreationDate", DbOrderByType.Desc))
                 .ToList(20, Math.Max(1, page), out count);
            }
            this["ProductList"] = new SplitPageData<dynamic>(Math.Max(1, page), 20, list, count, 11);
            this["KeyWord"] = keyword;
            this["CategoryList"] = D.DistributorCategory.GetAll(DataSource, 0);
            this["GetImage"] = new FuncHandler((args) =>
            {
                return Convert.ToString(args[0]).Split('|')[0];
            });
            this["State"] = state;
            Render("distributorproduct_" + state + ".html");
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor(true)]
        public void State()
        {
            DataSource.Begin();
            try
            {
                if (long.Parse(Request.Form["action"]) == 0)//根据状态判断是上架0还是下架1
                {
                    long id = long.Parse(Request.Form["Id"]);
                    if (Db<D.DistributorProduct>.Query(DataSource).Update().Set("State", P.ProductState.Sale)
                                                        .Where((new DbWhere("Id", id) | new DbWhere("ParentId", long.Parse(Request.Form["Id"])))
                                                        & new DbWhere("State", P.ProductState.BeforeSale)
                                                        & new DbWhere("SupplierId").InSelect<P.Supplier>(new DbSelect("UserId")).Where(new DbWhere("Subjection", User.Identity.Id)).Result())
                                                        .Execute() > 0)
                    {
                        IList<dynamic> productlist = D.DistributorProduct.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
                        foreach (dynamic product in productlist)
                        {
                            if (!(product.DistributorAreaMapping_Price is DBNull) && product.DistributorAreaMapping_Price > 0)
                            {
                                if (D.DistributorAreaMapping.ModSaled(DataSource, product.DistributorProduct_Id, Distributor.Province, Distributor.City, Distributor.County, true) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                D.DistributorAreaMapping areamapping = new D.DistributorAreaMapping()
                                {
                                    ProductId = product.DistributorProduct_Id,
                                    Province = Distributor.Province,
                                    City = Distributor.City,
                                    County = Distributor.County,
                                    CostPrice = (Money)product.DistributorProduct_CostPrice,
                                    CountyPrice = (Money)product.DistributorProduct_CountyPrice,
                                    DotPrice = (Money)product.DistributorProduct_WholesalePrice,
                                    Price = (Money)product.DistributorProduct_Price,
                                    Saled = true
                                };
                                if (areamapping.Insert(DataSource) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                        }
                        DataSource.Commit();
                        SetResult(true);
                    }
                    else
                        throw new AggregateException();
                }
                else if (long.Parse(Request.Form["action"]) == 1)
                {
                    long id = long.Parse(Request.Form["Id"]);
                    if (Db<D.DistributorProduct>.Query(DataSource).Update().Set("State", P.ProductState.Saved)
                                                 .Where((new DbWhere("Id", long.Parse(Request.Form["Id"])) | new DbWhere("ParentId", long.Parse(Request.Form["Id"])))
                                                 & (new DbWhere("State", P.ProductState.Sale) | new DbWhere("State", P.ProductState.BeforeSaved))
                                                 & new DbWhere("SupplierId").InSelect<P.Supplier>(new DbSelect("UserId")).Where(new DbWhere("Subjection", User.Identity.Id))
                                               .Result()).Execute() > 0)
                    {
                        IList<dynamic> productlist = D.DistributorProduct.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
                        foreach (dynamic product in productlist)
                        {
                            if (!(product.DistributorAreaMapping_Price is DBNull) && product.DistributorAreaMapping_Price > 0)
                            {
                                if (D.DistributorAreaMapping.ModSaled(DataSource, product.DistributorProduct_Id, Distributor.Province, Distributor.City, Distributor.County, false) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                D.DistributorAreaMapping areamapping = new D.DistributorAreaMapping()
                                {
                                    ProductId = product.DistributorProduct_Id,
                                    Province = Distributor.Province,
                                    City = Distributor.City,
                                    County = Distributor.County,
                                    CostPrice = (Money)product.DistributorProduct_CostPrice,
                                    CountyPrice = (Money)product.DistributorProduct_CountyPrice,
                                    DotPrice = (Money)product.DistributorProduct_WholesalePrice,
                                    Price = (Money)product.DistributorProduct_Price,
                                    Saled = false
                                };
                                if (areamapping.Insert(DataSource) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                        }
                        DataSource.Commit();
                        SetResult(true);
                    }
                    else
                        throw new AggregateException();
                }
                else if (long.Parse(Request.Form["action"]) == 3)
                {
                    long id = long.Parse(Request.Form["Id"]);
                    IList<dynamic> productlist = D.DistributorProduct.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
                    if (productlist.Count > 0)
                    {
                        foreach (dynamic product in productlist)
                        {
                            if (!(product.DistributorAreaMapping_Price is DBNull) && product.DistributorAreaMapping_Price > 0)
                            {
                                if (D.DistributorAreaMapping.ModSaled(DataSource, product.DistributorProduct_Id, Distributor.Province, Distributor.City, Distributor.County, true) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                D.DistributorAreaMapping areamapping = new D.DistributorAreaMapping()
                                {
                                    ProductId = product.DistributorProduct_Id,
                                    Province = Distributor.Province,
                                    City = Distributor.City,
                                    County = Distributor.County,
                                    CostPrice = (Money)product.DistributorProduct_CostPrice,
                                    CountyPrice = (Money)product.DistributorProduct_CountyPrice,
                                    DotPrice = (Money)product.DistributorProduct_WholesalePrice,
                                    Price = (Money)product.DistributorProduct_Price,
                                    Saled = true
                                };
                                if (areamapping.Insert(DataSource) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }                            
                        }
                        DataSource.Commit();
                        SetResult(true);
                    }
                    else
                    {
                        throw new AggregateException();
                    }
                }
                else if (long.Parse(Request.Form["action"]) == 4)
                {
                    long id = long.Parse(Request.Form["Id"]);
                    IList<dynamic> productlist = D.DistributorProduct.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
                    if (productlist.Count > 0)
                    {
                        foreach (dynamic product in productlist)
                        {
                            if (!(product.DistributorAreaMapping_Price is DBNull) && product.DistributorAreaMapping_Price > 0)
                            {
                                if (D.DistributorAreaMapping.ModSaled(DataSource, product.DistributorProduct_Id, Distributor.Province, Distributor.City, Distributor.County, false) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                D.DistributorAreaMapping areamapping = new D.DistributorAreaMapping()
                                {
                                    ProductId = product.DistributorProduct_Id,
                                    Province = Distributor.Province,
                                    City = Distributor.City,
                                    County = Distributor.County,
                                    CostPrice = (Money)product.DistributorProduct_CostPrice,
                                    CountyPrice = (Money)product.DistributorProduct_CountyPrice,
                                    DotPrice = (Money)product.DistributorProduct_WholesalePrice,
                                    Price = (Money)product.DistributorProduct_Price,
                                    Saled = false
                                };
                                if (areamapping.Insert(DataSource) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }                            
                        }
                        DataSource.Commit();
                        SetResult(true);
                    }
                    else
                    {
                        throw new AggregateException();
                    }
                }
                else
                {
                    throw new AggregateException();
                }

            }
            catch (AggregateException)
            {
                DataSource.Rollback();
                SetResult(Common.CommUtility.UPDATE_FAIL);
            }
            catch (Exception ex)
            {
                DataSource.Rollback();
                SetResult(Common.CommUtility.PROGRAM_ERROR);
            }
        }


        [Distributor(true)]
        public void ShowPrice(long id)
        {
            IList<dynamic> productlist = D.DistributorProduct.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
            this["ProductList"] = productlist;
            this["GetAttributes"] = new FuncHandler((args) =>
            {
                return new D.DistributorProduct() { Id = Convert.ToInt64(args[0]) }.GetAttributes(DataSource);
            });
            this["GetImage"] = new FuncHandler((args) =>
            {
                return Convert.ToString(args[0]).Split('|')[0];
            });
            Render("distributormanager_price.html");
        }
        [HttpPost]
        [Distributor(true)]
        public void SubmitPrice(long id)
        {
            string ModField = Request["modfield"];
            Money ModPrice = 0;
            Money.TryParse(Request["modprice"], out ModPrice);
            D.DistributorAreaMapping areamapping = D.DistributorAreaMapping.GetById(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
            if (areamapping == null)
            {
                Money DotPrice = 0, Price = 0;
                D.DistributorProduct product = D.DistributorProduct.GetById(DataSource, id);
                DotPrice = product.WholesalePrice;
                Price = product.Price;
                if (ModField == "DotPrice") DotPrice = ModPrice;
                if (ModField == "Price") Price = ModPrice;
                areamapping = new D.DistributorAreaMapping()
                {
                    ProductId = id,
                    Province = Distributor.Province,
                    City = Distributor.City,
                    County = Distributor.County,
                    CostPrice = product.CostPrice,
                    CountyPrice = product.CountyPrice,
                    DotPrice = DotPrice,
                    Price = Price
                };
                SetResult(areamapping.Insert(DataSource));
            }
            else
            {
                SetResult(D.DistributorAreaMapping.ModPrice(DataSource, areamapping.ProductId, areamapping.Province, areamapping.City, areamapping.County, ModField, ModPrice));
            }
        }
    }
}
