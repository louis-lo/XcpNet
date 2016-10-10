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
    public sealed class ProductManager : SupplierController
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
                where = new DbWhere<P.Product>("State", P.ProductState.BeforeSale);
                where &= new DbWhere<P.Product>("SupplierId").InSelect<P.Supplier>(new DbSelect("UserId")).Where(new DbWhere("Subjection", User.Identity.Id)).Result();
            }
            else if (state == "offshelves")///下架商品
            {
                where = (new DbWhere<P.Product>("State", P.ProductState.Sale) | new DbWhere<P.Product>("State", P.ProductState.BeforeSaved));
                where &= new DbWhere<P.Product>("SupplierId").InSelect<P.Supplier>(new DbSelect("UserId")).Where(new DbWhere("Subjection", User.Identity.Id)).Result();
            }
            if (state == "whole")///全国优选
            {
                where = (new DbWhere<P.Product>("State", P.ProductState.Sale) | new DbWhere<P.Product>("State", P.ProductState.BeforeSaved));
                where &= new DbWhere<P.Product>("SupplierType", 0);
                where &= (new DbWhere<P.ProductSalesArea>("Province", Distributor.Province) | new DbWhere<P.ProductSalesArea>("Province", 0));
                where &= (new DbWhere<P.ProductSalesArea>("City", Distributor.City) | new DbWhere<P.ProductSalesArea>("City", 0));
                where &= (new DbWhere<P.ProductSalesArea>("County", Distributor.County) | new DbWhere<P.ProductSalesArea>("County", 0));
            }
            if (categoryId != 0)
            {
                IList<P.ProductCategory> cates = P.ProductCategory.GetAllParentsById(DataSource, categoryId);
                if (cates.Count > 0)
                    this["CategoryName"] = cates[0].Name;
                if (cates.Count == 3)
                    where &= new DbWhere<P.Product>("CategoryId", categoryId);
                else if (cates.Count == 2)
                    where &= (new DbWhere<P.Product>("CategoryId", categoryId) | new DbWhere<P.Product>("CategoryId").InSelect<P.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result());
                else if (cates.Count == 1)
                    where &= (new DbWhere<P.Product>("CategoryId", categoryId) | new DbWhere<P.Product>("CategoryId").InSelect<P.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result() | new DbWhere<P.Product>("CategoryId").InSelect<P.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId").InSelect<P.ProductCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result()).Result());
            }

            where &= new DbWhere<P.Product>("ParentId", 0) & new DbWhere<P.Product>("ProductType", 1);
            if (!string.IsNullOrEmpty(keyword) && keyword != "_")
                where = new DbWhere<P.Product>("Title", keyword, DbWhereType.Like);
            long count;
            IList<dynamic> list;
            if (state == "whole")///全国优选
            {
                DbWhereQueue areawhere = new DbWhereQueue();
                areawhere = new DbWhere<P.ProductAreaMapping>("Province", Distributor.Province);
                areawhere &= new DbWhere<P.ProductAreaMapping>("City", Distributor.City);
                areawhere &= new DbWhere<P.ProductAreaMapping>("County", Distributor.County);

                list = Db<P.Product>.Query(DataSource)
                 .Select(new DbSelect<P.Product>(), new DbSelect<Cnaws.Product.Modules.Supplier>("Company"), new DbSelect<P.ProductCategory>("Name"), new DbSelect<P.ProductAreaMapping>("Saled"))
                 .InnerJoin(new DbColumn<P.Product>("SupplierId"), new DbColumn<Cnaws.Product.Modules.Supplier>("UserId"))
                 .InnerJoin(new DbColumn<P.Product>("CategoryId"), new DbColumn<P.ProductCategory>("Id"))
                 .LeftJoin(new DbColumn<P.Product>("Id"), new DbColumn<P.ProductAreaMapping>("ProductId")).Select().Where(areawhere).Result()
                 .InnerJoin(new DbColumn<P.Product>("Id"), new DbColumn<P.ProductSalesArea>("ProductId"))
                 .Where(where)
                 .OrderBy(new DbOrderBy<P.Product>("CreationDate", DbOrderByType.Desc))
                 .ToList(20, Math.Max(1, page), out count);
            }
            else
            {
                list = Db<P.Product>.Query(DataSource)
                 .Select(new DbSelect<P.Product>(), new DbSelect<Cnaws.Product.Modules.Supplier>("Company"), new DbSelect<P.ProductCategory>("Name"))
                 .InnerJoin(new DbColumn<P.Product>("SupplierId"), new DbColumn<Cnaws.Product.Modules.Supplier>("UserId"))
                 .InnerJoin(new DbColumn<P.Product>("CategoryId"), new DbColumn<P.ProductCategory>("Id"))
                 .Where(where)
                 .OrderBy(new DbOrderBy<P.Product>("CreationDate", DbOrderByType.Desc))
                 .ToList(20, Math.Max(1, page), out count);
            }
            this["ProductList"] = new SplitPageData<dynamic>(Math.Max(1, page), 20, list, count, 8);
            this["KeyWord"] = keyword;
            this["CategoryList"] = P.ProductCategory.GetAll(DataSource, 0);
            this["GetImage"] = new FuncHandler((args) =>
            {
                return Convert.ToString(args[0]).Split('|')[0];
            });
            this["State"] = state;
            Render("managerproduct_" + state + ".html");
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
                    if (Db<P.Product>.Query(DataSource).Update().Set("State", P.ProductState.Sale)
                                                        .Where((new DbWhere("Id", id) | new DbWhere("ParentId", long.Parse(Request.Form["Id"])))
                                                        & new DbWhere("State", P.ProductState.BeforeSale)
                                                        & new DbWhere("SupplierId").InSelect<P.Supplier>(new DbSelect("UserId")).Where(new DbWhere("Subjection", User.Identity.Id)).Result())
                                                        .Execute() > 0)
                    {
                        IList<dynamic> productlist = P.Product.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
                        foreach (dynamic product in productlist)
                        {
                            if (!(product.ProductAreaMapping_Price is DBNull) && product.ProductAreaMapping_Price > 0)
                            {
                                if (P.ProductAreaMapping.ModSaled(DataSource, product.Product_Id, Distributor.Province, Distributor.City, Distributor.County, true) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                P.ProductAreaMapping areamapping = new P.ProductAreaMapping()
                                {
                                    ProductId = product.Product_Id,
                                    Province = Distributor.Province,
                                    City = Distributor.City,
                                    County = Distributor.County,
                                    CostPrice = (Money)product.Product_CostPrice,
                                    CountyPrice = (Money)product.Product_CountyPrice,
                                    DotPrice = (Money)product.Product_DotPrice,
                                    Price = (Money)product.Product_Price,
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
                    if (Db<P.Product>.Query(DataSource).Update().Set("State", P.ProductState.Saved)
                                                 .Where((new DbWhere("Id", long.Parse(Request.Form["Id"])) | new DbWhere("ParentId", long.Parse(Request.Form["Id"])))
                                                 & (new DbWhere("State", P.ProductState.Sale) | new DbWhere("State", P.ProductState.BeforeSaved))
                                                 & new DbWhere("SupplierId").InSelect<P.Supplier>(new DbSelect("UserId")).Where(new DbWhere("Subjection", User.Identity.Id))
                                               .Result()).Execute() > 0)
                    {
                        IList<dynamic> productlist = P.Product.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
                        foreach (dynamic product in productlist)
                        {
                            if (!(product.ProductAreaMapping_Price is DBNull) && product.ProductAreaMapping_Price > 0)
                            {
                                if (P.ProductAreaMapping.ModSaled(DataSource, product.Product_Id, Distributor.Province, Distributor.City, Distributor.County, false) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                P.ProductAreaMapping areamapping = new P.ProductAreaMapping()
                                {
                                    ProductId = product.Product_Id,
                                    Province = Distributor.Province,
                                    City = Distributor.City,
                                    County = Distributor.County,
                                    CostPrice = (Money)product.Product_CostPrice,
                                    CountyPrice = (Money)product.Product_CountyPrice,
                                    DotPrice = (Money)product.Product_DotPrice,
                                    Price = (Money)product.Product_Price,
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
                    IList<dynamic> productlist = P.Product.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
                    if (productlist.Count > 0)
                    {
                        foreach (dynamic product in productlist)
                        {
                            if (!(product.ProductAreaMapping_Price is DBNull) && product.ProductAreaMapping_Price > 0)
                            {
                                if (P.ProductAreaMapping.ModSaled(DataSource, product.Product_Id, Distributor.Province, Distributor.City, Distributor.County, true) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                P.ProductAreaMapping areamapping = new P.ProductAreaMapping()
                                {
                                    ProductId = product.Product_Id,
                                    Province = Distributor.Province,
                                    City = Distributor.City,
                                    County = Distributor.County,
                                    CostPrice = (Money)product.Product_CostPrice,
                                    CountyPrice = (Money)product.Product_CountyPrice,
                                    DotPrice = (Money)product.Product_DotPrice,
                                    Price = (Money)product.Product_Price,
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
                    IList<dynamic> productlist = P.Product.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
                    if (productlist.Count > 0)
                    {
                        foreach (dynamic product in productlist)
                        {
                            if (!(product.ProductAreaMapping_Price is DBNull) && product.ProductAreaMapping_Price > 0)
                            {
                                if (P.ProductAreaMapping.ModSaled(DataSource, product.Product_Id, Distributor.Province, Distributor.City, Distributor.County, false) != DataStatus.Success)
                                {
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                P.ProductAreaMapping areamapping = new P.ProductAreaMapping()
                                {
                                    ProductId = product.Product_Id,
                                    Province = Distributor.Province,
                                    City = Distributor.City,
                                    County = Distributor.County,
                                    CostPrice = (Money)product.Product_CostPrice,
                                    CountyPrice = (Money)product.Product_CountyPrice,
                                    DotPrice = (Money)product.Product_DotPrice,
                                    Price = (Money)product.Product_Price,
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
            IList<dynamic> productlist = P.Product.GetPageByParentId2NoState(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
            this["ProductList"] = productlist;
            this["GetAttributes"] = new FuncHandler((args) =>
            {
                return new P.Product() { Id = Convert.ToInt64(args[0]) }.GetAttributes(DataSource);
            });
            this["GetImage"] = new FuncHandler((args) =>
            {
                return Convert.ToString(args[0]).Split('|')[0];
            });
            Render("manager_price.html");
        }
        [HttpPost]
        [Distributor(true)]
        public void SubmitPrice(long id)
        {
            string ModField = Request["modfield"];
            Money ModPrice = 0;
            Money.TryParse(Request["modprice"], out ModPrice);
            P.ProductAreaMapping areamapping = P.ProductAreaMapping.GetById(DataSource, id, Distributor.Province, Distributor.City, Distributor.County);
            if (areamapping == null)
            {
                Money DotPrice = 0, Price = 0;
                P.Product product = P.Product.GetById(DataSource, id);
                DotPrice = product.DotPrice;
                Price = product.Price;
                if (ModField == "DotPrice") DotPrice = ModPrice;
                if (ModField == "Price") Price = ModPrice;
                areamapping = new P.ProductAreaMapping()
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
                SetResult(P.ProductAreaMapping.ModPrice(DataSource, areamapping.ProductId, areamapping.Province, areamapping.City, areamapping.County, ModField, ModPrice));
            }
        }
    }
}
