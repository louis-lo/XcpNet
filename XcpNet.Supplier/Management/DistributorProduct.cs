﻿using System;
using System.Web;
using System.Collections.Generic;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using M = XcpNet.Supplier.Modules.Modules;
using Pd = Cnaws.Product.Modules;
using Cnaws.Web.Templates;
using Cnaws.Product.Modules;
using Cnaws.Data.Query;
using U = Cnaws.Passport.Modules;
using System.Linq;
using Cnaws;
using System.Text;

namespace XcpNet.Supplier.Management
{
    public sealed class DistributorProduct : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        private Dictionary<int, string> _menus;

        public DistributorProduct()
        {
            _menus = new Dictionary<int, string>();
            _menus.Add(0, "所有商品");
            _menus.Add(1, "待上架商品");
            _menus.Add(2, "已上架商品");
            _menus.Add(3, "首页推荐商品");
            _menus.Add(4, "分类推荐商品");
            _menus.Add(5, "申请团购商品");
            _menus.Add(6, "团购商品");
            _menus.Add(7, "仓库中的商品");
            _menus.Add(8, "回收站");
        }

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Supplier"; }
        }

        public void Index(int id = 0, long sid = 0, int type = 0)
        {
            if (CheckRight())
            {
                if (CheckPost("distributorproduct", () =>
                {
                    StringBuilder sb = new StringBuilder();
                    IList<M.DistributorCategory> parents = M.DistributorCategory.GetAllParentsById(DataSource, id);
                    if (parents.Count > 0)
                    {
                        for (int i = 0; i < parents.Count; ++i)
                        {
                            if (i > 0) sb.Append("&nbsp;&nbsp;");
                            sb.Append(parents[i].Name);
                        }
                    }
                    else
                    {
                        sb.Append("所有分类");
                    }
                    this["Id"] = id;
                    this["SId"] = sid;
                    this["Type"] = type;
                    this["Parents"] = sb.ToString();
                    this["AllCategory"] = M.DistributorCategory.GetAll(DataSource, -1);
                    this["Menus"] = _menus;
                    this["GetMenuName"] = new FuncHandler((args) =>
                    {
                        try { return _menus[(int)args[0]]; }
                        catch (Exception) { }
                        return _menus[0];
                    });
                }))
                    NotFound();
            }
        }
        public void List(int categoryId, long sid = 0, int type = 0, int page = 1, string q = "_")
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    long count;
                    DbWhereQueue dw = null;
                    if (categoryId > 0)
                    {
                        dw &= new DbWhere<M.DistributorProduct>("State", Pd.ProductState.Deleted, DbWhereType.NotEqual);

                        int categorylevel = M.DistributorCategory.GetAllParentsById(DataSource, categoryId).Count;
                        if (categorylevel == 3)
                            dw &= new DbWhere<M.DistributorProduct>("CategoryId", categoryId);
                        else if (categorylevel == 2)
                            dw &= (new DbWhere<M.DistributorProduct>("CategoryId", categoryId) | new DbWhere<M.DistributorProduct>("CategoryId").InSelect<M.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result());
                        else if (categorylevel == 1)
                            dw &= (new DbWhere<M.DistributorProduct>("CategoryId", categoryId) | new DbWhere<M.DistributorProduct>("CategoryId").InSelect<M.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result() |
                                new DbWhere<M.DistributorProduct>("CategoryId").InSelect<M.DistributorCategory>(new DbSelect("Id")).
                                Where(new DbWhere("ParentId").InSelect<M.DistributorCategory>(new DbSelect("Id")).Where(new DbWhere("ParentId", categoryId)).Result()).Result());
                    }

                    if (sid > 0)
                        dw &= new DbWhere<M.DistributorProduct>("SupplierId", sid);

                    switch (type)
                    {
                        case 0:
                            dw &= new DbWhere<M.DistributorProduct>("State", ProductState.Deleted, DbWhereType.NotEqual);
                            break;
                        case 1:
                            dw &= new DbWhere<M.DistributorProduct>("State", ProductState.BeforeSale);
                            break;
                        case 2:
                            dw &= new DbWhere<M.DistributorProduct>("State", ProductState.Sale);
                            break;
                        case 3:
                            dw &= new DbWhere<M.DistributorProduct>("State", ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.DistributorProduct>("Recommend", true);
                            break;
                        case 4:
                            dw &= new DbWhere<M.DistributorProduct>("State", ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.DistributorProduct>("CategoryBest", 0, DbWhereType.GreaterThan);
                            break;
                        case 5:
                            dw &= new DbWhere<M.DistributorProduct>("State", ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.DistributorProduct>("DiscountState", DiscountState.Approval);
                            break;
                        case 6:
                            dw &= new DbWhere<M.DistributorProduct>("State", ProductState.Deleted, DbWhereType.NotEqual) & new DbWhere<M.DistributorProduct>("DiscountState", DiscountState.Activated);
                            break;
                        case 7:
                            dw &= new DbWhere<M.DistributorProduct>("State", ProductState.Saved);
                            break;
                        case 8:
                            dw &= new DbWhere<M.DistributorProduct>("State", ProductState.Deleted);
                            break;
                    }

                    if ("_".Equals(q))
                        q = null;
                    if (!string.IsNullOrEmpty(q))
                        dw &= new DbWhere<M.DistributorProduct>("Title", HttpUtility.UrlDecode(q), DbWhereType.Like);
                    
                    dw &= new DbWhere<M.DistributorProduct>("ParentId", 0);

                    IList<DataJoin<M.DistributorProduct, U.Member>> list = Db<M.DistributorProduct>.Query(DataSource)
                        .Select(new DbSelect<M.DistributorProduct>(), new DbSelect<U.Member>(), new DbSelectAs<Pd.Supplier>("Company"))
                        .LeftJoin(new DbColumn<M.DistributorProduct>("SupplierId"), new DbColumn<U.Member>("Id"))
                        .LeftJoin(new DbColumn<M.DistributorProduct>("SupplierId"), new DbColumn<Pd.Supplier>("UserId"))
                        .Where(dw)
                        .OrderBy(new DbOrderBy<M.DistributorProduct>("SortNum", DbOrderByType.Desc), new DbOrderBy<M.DistributorProduct>("Id", DbOrderByType.Desc))
                        .ToList<DataJoin<M.DistributorProduct, U.Member>>(10, page, out count);

                    SetResult(new SplitPageData<DataJoin<M.DistributorProduct, U.Member>>(page, 10, list, count, 11));
                }
            }
        }



        //public void AllCategory()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            IList<M.DistributorCategory> list = M.DistributorCategory.GetAll(DataSource, -1);
        //            if (IsPost)
        //                SetResult(list);
        //            else
        //                SetJavascript("allCategory", list);
        //        }
        //    }
        //}

        //public void Categories(int parentId)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //            SetResult(M.DistributorCategory.GetAll(DataSource, parentId));
        //    }
        //}

        //public void Add()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                SetResult(M.DistributorProduct.Insert(DataSource,
        //                    Request.Form["Title"],
        //                    Request.Form["Image"],
        //                    Request.Form["Content"],
        //                    int.Parse(Request.Form["Category"]),
        //                    DateTime.Parse(Request.Form["CreationDate"]),
        //                    Request.Form["Keywords"],
        //                    Request.Form["Description"],
        //                    "on".Equals(Request.Form["Visibility"]),
        //                    "on".Equals(Request.Form["Top"]),
        //                    int.Parse(Request.Form["Style"]),
        //                    Request.Form["Color"],
        //                    Request.Form["Author"],
        //                    Request.Form["Referer"]), () =>
        //                    {
        //                        WritePostLog("ADD");
        //                    });
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        //public void Update()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                M.DistributorProduct value = DbTable.Load<M.DistributorProduct>(Request.Form);
        //                SetResult(value.Update(DataSource, ColumnMode.Include, "CategoryId", "Recommend", "CategoryBest", "WholesaleCount"), () =>
        //                    {
        //                        WritePostLog("MOD");
        //                    });
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        //public void State()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                M.DistributorProduct value = DbTable.Load<M.DistributorProduct>(Request.Form);
        //                if (value.State == Cnaws.Product.Modules.ProductState.Sale)
        //                    value.SaleTime = DateTime.Now;
        //                SetResult(value.Update(DataSource, ColumnMode.Include, "State", "SaleTime"), () =>
        //                {
        //                    WritePostLog("MOD");
        //                });
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        //public void DState()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                M.DistributorProduct value = DbTable.Load<M.DistributorProduct>(Request.Form);
        //                SetResult(value.Update(DataSource, ColumnMode.Include, "DiscountState"), () =>
        //                {
        //                    WritePostLog("MOD");
        //                });
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}

        //private string GetMR(string path)
        //{
        //    string url = Application.Settings.ResourcesUrl;
        //    if (string.IsNullOrEmpty(url))
        //        return string.Concat(Application.Settings.RootUrl, path.Substring(1));
        //    return string.Concat(url, path);
        //}
        //private string GetVR(string path)
        //{
        //    string url = Application.Settings.ResourcesUrl;
        //    if (string.IsNullOrEmpty(url))
        //        return string.Concat(Application.Settings.ThemeUrl, path);
        //    return string.Concat(url, Application.Settings.ThemeUrl, path);
        //}
        //private string GetR(params string[] path)
        //{
        //    string url = string.Concat(path);
        //    if (url.StartsWith("/"))
        //        return GetMR(url);

        //    Uri uri = new Uri(url, UriKind.RelativeOrAbsolute);
        //    if (uri.IsAbsoluteUri)
        //        return url;

        //    return GetVR(url);
        //}
        //public void Child(int id)
        //{
        //    if (IsAjax)
        //    {
        //        SetResult(M.DistributorCategory.GetAll(DataSource, id));
        //    }
        //    else
        //    {
        //        NotFound();
        //    }
        //}
        //public void Info(long id)
        //{
        //    if (CheckRight())
        //    {
        //        int location;
        //        M.DistributorProduct product;
        //        if (id > 0)
        //            product = M.DistributorProduct.GetById(DataSource, id);
        //        else
        //            product = new M.DistributorProduct();
        //        if (product == null)
        //        {
        //            this["Error"] = true;
        //        }
        //        else
        //        {
        //            if (product.County > 0)
        //            {
        //                location = product.County;
        //            }
        //            else if (product.City > 0)
        //            {
        //                location = product.City;
        //            }
        //            else if (product.Province > 0)
        //            {
        //                location = product.Province;
        //            }
        //            else
        //            {
        //                location = 0;
        //            }
        //            IList<M.DistributorCategory> category;
        //            IList<M.DistributorAttribute> attribute;
        //            Dictionary<long, string> values;
        //            if (product.Id > 0)
        //            {
        //                category = M.DistributorCategory.GetAllParentsById(DataSource, product.CategoryId);
        //                attribute = M.DistributorAttribute.GetAllByCategory(DataSource, product.CategoryId);
        //                values = M.DistributorAttributeMapping.GetAllByProduct(DataSource, product.Id);
        //            }
        //            else
        //            {
        //                category = new List<M.DistributorCategory>();
        //                attribute = new List<M.DistributorAttribute>();
        //                values = new Dictionary<long, string>();
        //            }

        //            this["Error"] = false;
        //            this["Location"] = location;
        //            this["Product"] = product;
        //            if (product.ParentId > 0)
        //                this["Parent"] = M.DistributorProduct.GetById(DataSource, product.ParentId);
        //            else
        //                this["Parent"] = product;
        //            this["CategoryList"] = category;
        //            if (product.CategoryId > 0)
        //                this["BrandList"] = M.DistributorBrand.GetAllByCategory(DataSource, product.CategoryId);
        //            else
        //                this["BrandList"] = new List<M.DistributorBrand>();
        //            this["AttributeList"] = attribute;
        //            this["ValueList"] = values;
        //            //this["Submit"] = isAdmin ? "submitex" : "submit";
        //            //this["Modify"] = isAdmin ? "modifyex" : "modify";
        //            //this["FreightTemplate"] = isAdmin ? string.Concat("freighttemplateex/", product.SupplierId) : "freighttemplate";
        //        }
        //        this["res"] = new FuncHandler((args) =>
        //        {
        //            return GetR(Array.ConvertAll(args, new Converter<object, string>((x) =>
        //            {
        //                return x.ToString();
        //            })));
        //        });
        //        RenderTemplate("distributorproductex.html");
        //    }
        //}
        //public void Submit(int step)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                try
        //                {
        //                    switch (step)
        //                    {
        //                        case 1:
        //                            {
        //                                M.DistributorProduct product = DbTable.Load<M.DistributorProduct>(Request.Form);
        //                                product.CreationDate = DateTime.Now;
        //                                product.Province = int.Parse(Request.Form["area_provinces"]);
        //                                product.City = int.Parse(Request.Form["area_cities"]);
        //                                product.County = int.Parse(Request.Form["area_counties"]);
        //                                product.Content = HttpUtility.UrlDecode(product.Content);
        //                                if (product.Settlement == SettlementType.Fixed)
        //                                {
        //                                    product.RoyaltyRate = 0;
        //                                }
        //                                if (product.Id > 0)
        //                                {
        //                                    SetResult(product.Update(DataSource, ColumnMode.Include, "Province", "City", "County", "CategoryId", "Title", "Content", "BarCode", "Unit", "Inventory", "CostPrice", "Price", "Wholesale", "WholesalePrice", "WholesaleCount", "Settlement", "RoyaltyRate"), () =>
        //                                    {
        //                                        WritePostLog("MOD");
        //                                    });
        //                                }
        //                                else
        //                                {
        //                                    SetResult(product.Insert(DataSource), () =>
        //                                    {
        //                                        WritePostLog("ADD");
        //                                    }, product.Id);
        //                                }
        //                            }
        //                            break;
        //                        case 2:
        //                            {
        //                                M.DistributorProduct product = DbTable.Load<M.DistributorProduct>(Request.Form);
        //                                product.Image = System.Web.HttpUtility.UrlDecode(product.Image);
        //                                if (product.Id > 0)
        //                                    SetResult(product.Update(DataSource, ColumnMode.Include, "Image"), () =>
        //                                    {
        //                                        WritePostLog("MOD");
        //                                    });
        //                                else
        //                                    SetResult(DataStatus.Exist);
        //                            }
        //                            break;
        //                        case 3:
        //                            {
        //                                M.DistributorProduct product = DbTable.Load<M.DistributorProduct>(Request.Form);
        //                                if (product.Id > 0)
        //                                {
        //                                    List<M.DistributorAttributeMapping> list = new List<M.DistributorAttributeMapping>(Request.Form.Count);
        //                                    foreach (string key in Request.Form.AllKeys)
        //                                    {
        //                                        if (key.StartsWith("Attr"))
        //                                        {
        //                                            list.Add(new M.DistributorAttributeMapping()
        //                                            {
        //                                                ProductId = product.Id,
        //                                                AttributeId = long.Parse(key.Substring(4)),
        //                                                Value = Request.Form[key]
        //                                            });
        //                                        }
        //                                    }
        //                                    DataSource.Begin();
        //                                    try
        //                                    {
        //                                        if (product.Update(DataSource, ColumnMode.Include, "BrandId") != DataStatus.Success)
        //                                            throw new Exception();
        //                                        foreach (M.DistributorAttributeMapping item in list)
        //                                        {
        //                                            if (item.InsertOrUpdate(DataSource) != DataStatus.Success)
        //                                                throw new Exception();
        //                                        }
        //                                        DataSource.Commit();
        //                                        SetResult(true, () =>
        //                                        {
        //                                            WritePostLog("MOD");
        //                                        });
        //                                    }
        //                                    catch (Exception)
        //                                    {
        //                                        DataSource.Rollback();
        //                                        SetResult(false);
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    SetResult(DataStatus.Exist);
        //                                }
        //                            }
        //                            break;
        //                        //case 4:
        //                        //    {
        //                        //        M.DistributorProduct product = DbTable.Load<M.DistributorProduct>(Request.Form);
        //                        //        if (product.Id > 0)
        //                        //            SetResult(product.Update(DataSource, ColumnMode.Include, "DiscountState", "DiscountPrice", "DiscountBeginTime", "DiscountEndTime"));
        //                        //        else
        //                        //            SetResult(DataStatus.Exist);
        //                        //    }
        //                        //    break;
        //                        case 5:
        //                            {
        //                                M.DistributorMapping product = DbTable.Load<M.DistributorMapping>(Request.Form);
        //                                if (product.ProductId > 0)
        //                                {
        //                                    try
        //                                    {
        //                                        if (product.Update(DataSource) != DataStatus.Success)
        //                                        {
        //                                            throw new Exception();
        //                                        }
        //                                        else
        //                                        {
        //                                            IList<M.DistributorMapping> list = M.DistributorMapping.GetAllByAllProduct(DataSource, product.ProductId);
        //                                            if (list.Count > 0)
        //                                            {
        //                                                new M.DistributorProduct { Id = product.ProductId, Norms = string.Join("/", list.Select(x => x.Value).ToArray()) }.Update(DataSource, ColumnMode.Include, "Norms");
        //                                            }
        //                                            SetResult(true, () =>
        //                                            {
        //                                                WritePostLog("MOD");
        //                                            });

        //                                        }
        //                                    }
        //                                    catch (Exception)
        //                                    {
        //                                        if (product.Insert(DataSource) != DataStatus.Success) { SetResult(DataStatus.Failed); }
        //                                        else
        //                                        {
        //                                            IList<M.DistributorMapping> list = M.DistributorMapping.GetAllByAllProduct(DataSource, product.ProductId);
        //                                            if (list.Count > 0)
        //                                            {
        //                                                new M.DistributorProduct { Id = product.ProductId, Norms = string.Join("/", list.Select(x => x.Value).ToArray()) }.Update(DataSource, ColumnMode.Include, "Norms");
        //                                            }
        //                                            SetResult(true, () =>
        //                                            {
        //                                                WritePostLog("ADD");
        //                                            });
        //                                        }
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    SetResult(DataStatus.Exist);
        //                                }
        //                            }
        //                            break;
        //                        case 6:
        //                            {
        //                                M.DistributorSerie product = DbTable.Load<M.DistributorSerie>(Request.Form);
        //                                if (product.ProductId > 0)
        //                                    SetResult(product.Insert(DataSource), () =>
        //                                    {
        //                                        WritePostLog("ADD");
        //                                    });
        //                                else
        //                                    SetResult(DataStatus.Exist);
        //                            }
        //                            break;
        //                        case 7:
        //                            {
        //                                long id = long.Parse(Request.Form["Id"]);
        //                                if (id > 0)
        //                                    SetResult(M.DistributorProduct.CreateCopy(DataSource, id), () =>
        //                                    {
        //                                        WritePostLog("ADD");
        //                                    });
        //                                else
        //                                    SetResult(DataStatus.Exist);
        //                            }
        //                            break;
        //                        case 8:
        //                            {
        //                                long id = long.Parse(Request.Form["Id"]);
        //                                if (id > 0)
        //                                    SetResult(M.DistributorSerie.Delete(DataSource, id), () =>
        //                                    {
        //                                        WritePostLog("DEL");
        //                                    });
        //                                else
        //                                    SetResult(DataStatus.Exist);
        //                            }
        //                            break;
        //                        case 9:
        //                            {
        //                                long id = long.Parse(Request.Form["Id"]);
        //                                if (id > 0)
        //                                    SetResult(M.DistributorProduct.RemoveToRecycleBin(DataSource, id), () =>
        //                                    {
        //                                        WritePostLog("DEL");
        //                                    });
        //                                else
        //                                    SetResult(DataStatus.Exist);
        //                            }
        //                            break;
        //                    }
        //                }
        //                catch (Exception)
        //                {
        //                    SetResult(false);
        //                }
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        //public void Add()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                M.DistributorProduct value = DbTable.Load<M.DistributorProduct>(Request.Form);
        //                SetResult(value.Insert(DataSource), () =>
        //                {
        //                    WritePostLog("ADD");
        //                });
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        public void Mod()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.DistributorProduct value = new M.DistributorProduct()
                        {
                            Id = long.Parse(Request.Form["a_Id"]),
                            State = (Pd.ProductState)Enum.Parse(typeof(Pd.ProductState), Request.Form["a_State"]),
                            DiscountState = (Pd.DiscountState)Enum.Parse(typeof(Pd.DiscountState), Request.Form["a_DiscountState"]),
                            Recommend = Types.GetBooleanFromString(Request.Form["a_Recommend"]),
                            CategoryBest = int.Parse(Request.Form["a_CategoryBest"]),
                            CostPrice = Money.Parse(Request.Form["a_CostPrice"]),
                            CountyPrice = Money.Parse(Request.Form["a_CountyPrice"]),
                            DotPrice = Money.Parse(Request.Form["a_DotPrice"]),
                            Price = Money.Parse(Request.Form["a_Price"]),
                            SortNum = int.Parse(Request.Form["a_SortNum"])
                        };
                        List<DataColumn> columns = new List<DataColumn>();
                        M.DistributorProduct old = M.DistributorProduct.GetById(DataSource, value.Id);
                        if (value.State != old.State)
                        {
                            if (value.State == Pd.ProductState.Sale)
                            {
                                value.SaleTime = DateTime.Now;
                                columns.Add("SaleTime");
                            }
                            columns.Add("State");
                        }
                        if (value.DiscountState != old.DiscountState)
                            columns.Add("DiscountState");
                        if (value.Recommend != old.Recommend)
                            columns.Add("Recommend");
                        if (value.CategoryBest != old.CategoryBest)
                            columns.Add("CategoryBest");
                        if (value.CostPrice != old.CostPrice)
                            columns.Add("CostPrice");
                        if (value.CountyPrice != old.CountyPrice)
                            columns.Add("CountyPrice");
                        if (value.DotPrice != old.DotPrice)
                            columns.Add("DotPrice");
                        if (value.Price != old.Price)
                            columns.Add("Price");
                        if (value.SortNum != old.SortNum)
                            columns.Add("SortNum");
                        if (columns.Count > 0)
                        {
                            SetResult(value.Update(DataSource, ColumnMode.Include, columns.ToArray(), new DataWhere("Id", value.Id) | new DataWhere("ParentId", value.Id)), () =>
                            {
                                WritePostLog("MOD");
                            });
                        }
                        else
                        {
                            SetResult(true);
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        public void Del()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.DistributorProduct value = new M.DistributorProduct()
                        {
                            Id = long.Parse(Request.Form["a_Id"]),
                            State = Pd.ProductState.Deleted
                        };
                        SetResult(value.Update(DataSource, ColumnMode.Include, new DataColumn[] { "State" }, new DataWhere("Id", value.Id) | new DataWhere("ParentId", value.Id)), () =>
                        {
                            WritePostLog("DEL");
                        });
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        public void Remove()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        long[] ids = Array.ConvertAll(Request.Form["a_Id"].Split(','), new Converter<string, long>((x) => long.Parse(x)));
                        SetResult(Db<M.DistributorProduct>.Query(DataSource).Update().Set("State", Pd.ProductState.Deleted).Where(new DbWhere("ParentId", ids, DbWhereType.In) | new DbWhere("Id", ids, DbWhereType.In)).Execute() > 0, () =>
                        {
                            WritePostLog("DEL");
                        });
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        //public void Get(int id)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //            SetResult(M.DistributorProduct.GetById(DataSource, id));
        //    }
        //}
        //public void Parents(int id)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            List<int> list = new List<int>();
        //            M.DistributorCategory ac = M.DistributorCategory.GetById(DataSource, id);
        //            list.Add(ac.Id);
        //            while (ac.ParentId != 0)
        //            {
        //                ac = M.DistributorCategory.GetById(DataSource, ac.ParentId);
        //                list.Insert(0, ac.Id);
        //            }
        //            SetResult(list.ToArray());
        //        }
        //    }
        //}
    }
}
