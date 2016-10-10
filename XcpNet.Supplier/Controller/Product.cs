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
using Cnaws;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
namespace XcpNet.Supplier.Controllers.Extension
{
    public sealed class Product : SupplierController
    {
        /// <summary>
        /// 新增时的当前产品索引
        /// </summary>
        public int productIndex = 0;
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

        protected override void Dispose(bool disposing)
        {
            if (_country != null)
            {
                _country.Dispose();
                _country = null;
            }
            base.Dispose(disposing);
        }

        [SupplierOrDistributor(true)]
        public void Index()
        {
            Modify();
        }

        private void ModifyImpl(long id, bool isAdmin)
        {
            int location;
            P.Product product;
            if (id > 0)
                product = isAdmin ? P.Product.GetById(DataSource, id) : P.Product.GetBySupplier(DataSource, id, User.Identity.Id);
            else
                product = isAdmin ? null : new P.Product();
            if (product == null/* || product.State == P.ProductState.Sale*/)
            {
                this["Error"] = true;
            }
            else
            {
                if (product.County > 0)
                {
                    location = product.County;
                }
                else
                {
                    City city;
                    using (IPArea area = new IPArea())
                    {
                        IPLocation local = area.Search(ClientIp);
                        using (Country country = Country.GetCountry())
                            city = local.GetCity(country);
                    }
                    location = city != null ? city.Id : 441900;
                    if (IsDistributor() && Distributor.County > 0)
                    {
                        location = Distributor.County;
                    }
                    else if (IsSupplier() && Supplier.County > 0)
                    {
                        location = Supplier.County;
                    }
                }
                IList<P.ProductCategory> category;
                IList<P.ProductAttribute> attribute;
                Dictionary<long, string> values;
                if (product.Id > 0)
                {
                    category = P.ProductCategory.GetAllParentsById(DataSource, product.CategoryId);
                    attribute = P.ProductAttribute.GetAllByCategory(DataSource, product.CategoryId);
                    values = P.ProductAttributeMapping.GetAllByProduct(DataSource, product.Id);
                }
                else
                {
                    category = new List<P.ProductCategory>();
                    attribute = new List<P.ProductAttribute>();
                    values = new Dictionary<long, string>();
                }
                if (IsSupplier())
                    this["StoreInfo"] = P.StoreInfo.GetStoreInfoByUserId(DataSource, User.Identity.Id);
                else
                    this["StoreInfo"] = XcpNet.Supplier.Modules.Modules.XDGInfo.GetXDGInfoByUserId(DataSource, User.Identity.Id);
                this["Error"] = false;
                this["Location"] = location;
                this["Product"] = product;
                if (product.ParentId > 0)
                    this["Parent"] = P.Product.GetById(DataSource, product.ParentId);
                else
                    this["Parent"] = product;
                this["CategoryList"] = category;
                if (product.CategoryId > 0)
                    this["BrandList"] = P.ProductBrand.GetAllByCategory(DataSource, product.CategoryId);
                else
                    this["BrandList"] = new List<P.ProductBrand>();
                this["AttributeList"] = attribute;
                this["ValueList"] = values;
                this["Submit"] = isAdmin ? "submitex" : "submit";
                this["Modify"] = isAdmin ? "modifyex" : "modify";
                this["FreightTemplate"] = isAdmin ? string.Concat("freighttemplateex/", product.SupplierId) : "freighttemplate";
                this["Supplier"] = P.Supplier.GetById(DataSource, User.Identity.Id);
                this["StoreCategoryList"] = P.StoreCategory.GetXDGCategoryOne(DataSource, User.Identity.Id);
                this["XDGParentCategoryId"] = 0;
                this["GetArea"] = new FuncHandler((args) =>
                {
                    int p = Convert.ToInt32(args[0]);
                    int c = Convert.ToInt32(args[1]);
                    int d = Convert.ToInt32(args[2]);
                    if (p == 0) return "全国";
                    if (c == 0) return Country.GetCity(p).ShortName;
                    if (d == 0) return Country.GetCity(c).ShortName;
                    return Country.GetCity(c).ShortName + "/" + Country.GetCity(d).ShortName;
                });
                this["GetAreaId"] = new FuncHandler((args) =>
                {
                    int p = Convert.ToInt32(args[0]);
                    int c = Convert.ToInt32(args[1]);
                    int d = Convert.ToInt32(args[2]);
                    if (p == 0) return 0;
                    if (c == 0) return p;
                    if (d == 0) return c;
                    return d;
                });
                this["SaleAreaList"] = P.ProductSalesArea.GetById(DataSource, id);
                if (product.StoreCategoryId > 0)
                {
                    P.StoreCategory xdgCategory = P.StoreCategory.GetXDGCategoryByCategoryId(DataSource, product.StoreCategoryId);
                    if (xdgCategory != null)
                    {
                        this["XDGParentCategoryId"] = xdgCategory.ParentId;
                    }
                }
            }
            if (IsSupplier())
            {
                Render("supplier/product.html");
            }
            else
            {
                Render("product.html");
            }
        }
        [SupplierOrDistributor(true)]
        public void Modify(long id = 0L)
        {
            ModifyImpl(id, false);
        }
        [AdminAuthorize]
        public void ModifyEx(long id = 0L)
        {
            ModifyImpl(id, true);
        }

        private void SubmitImpl(int step, bool isAdmin)
        {
            try
            {
                switch (step)
                {
                    case 1:
                        {
                            DataSource.Begin();
                            try
                            {

                                P.Product product = DbTable.Load<P.Product>(Request.Form);
                                bool Synchro = Types.GetBooleanFromString(Request["Synchro"]);
                                product.SupplierId = User.Identity.Id;
                                product.Province = int.Parse(Request.Form["area_provinces"]);
                                product.City = int.Parse(Request.Form["area_cities"]);
                                product.County = int.Parse(Request.Form["area_counties"]);
                                product.CreationDate = DateTime.Now;
                                product.Content = System.Web.HttpUtility.UrlDecode(product.Content);
                                ///如果是本地产品，供应商价格等于出厂价//新增加时设置
                                product.Image = System.Web.HttpUtility.UrlDecode(product.Image);
                                if (product.Id == 0)
                                {
                                    if (IsSupplier())
                                    {
                                        Money baseMoney = product.Price - product.CostPrice;
                                        product.CountyPrice = product.CostPrice + (Money)(Math.Ceiling((double)baseMoney * 15.0) / 100);
                                        product.DotPrice = product.CountyPrice + (Money)(Math.Ceiling((double)baseMoney * 25.0) / 100);
                                        product.SupplierType = Supplier.SupplierType;
                                    }
                                }
                                product.State = P.ProductState.Saved;
                                if (product.Id > 0)
                                {
                                    P.Product oldproduct = P.Product.GetById(DataSource, product.Id);
                                    product.ParentId = oldproduct.ParentId;
                                    if (IsSupplier())
                                    {
                                        if (oldproduct.CostPrice != product.CostPrice || oldproduct.Price != product.Price)
                                        {
                                            Money baseMoney = product.Price - product.CostPrice;
                                            product.CountyPrice = product.CostPrice + (Money)(Math.Ceiling((double)baseMoney * 15.0) / 100);
                                            product.DotPrice = product.CountyPrice + (Money)(Math.Ceiling((double)baseMoney * 25.0) / 100);
                                        }
                                        else
                                        {
                                            product.CountyPrice = oldproduct.CountyPrice;
                                            product.DotPrice = oldproduct.DotPrice;
                                        }
                                    }
                                    string AreaStr = Request.Form["SaleArea"];
                                    if (AreaStr != "")
                                    {
                                        string[] AreaIds = AreaStr.Split(',');

                                        long parentId = product.ParentId > 0 ? product.ParentId : product.Id;

                                        long[] Ids;
                                        if (Synchro)
                                            Ids = P.Product.GetAllIdsByParentId(DataSource, parentId);
                                        else
                                            Ids = new long[] { product.Id };
                                        foreach (long productid in Ids)
                                        {
                                            if (P.ProductSalesArea.GetById(DataSource, productid).Count > 0)
                                            {
                                                if (P.ProductSalesArea.DelById(DataSource, productid) != DataStatus.Success)
                                                    throw new Exception();
                                            }
                                            int saleNum = 0;
                                            for (int i = 0; i < AreaIds.Length; i++)
                                            {
                                                if (AreaIds[i] != "")
                                                {
                                                    int p = 0;
                                                    int c = 0;
                                                    int d = 0;
                                                    if (int.Parse(AreaIds[i]) != 0)
                                                    {
                                                        City city = Country.GetCity(int.Parse(AreaIds[i]));
                                                        if (city.Level == CityLevel.City)
                                                        {
                                                            p = city.ParentId;
                                                            c = city.Id;
                                                        }
                                                        else if (city.Level == CityLevel.Province)
                                                        {
                                                            p = city.Id;
                                                        }
                                                        if (city.Level == CityLevel.County)
                                                        {
                                                            City newcity = Country.GetCity(city.ParentId);
                                                            p = newcity.ParentId;
                                                            c = newcity.Id;
                                                            d = city.Id;
                                                        }
                                                    }
                                                    P.ProductSalesArea area = new Cnaws.Product.Modules.ProductSalesArea()
                                                    {
                                                        ProductId = productid,
                                                        Province = p,
                                                        City = c,
                                                        County = d
                                                    };
                                                    if (area.Insert(DataSource) != DataStatus.Success)
                                                        throw new Exception();
                                                    saleNum++;
                                                }
                                            }
                                            if (saleNum <= 0)
                                                throw new Exception();
                                        }
                                    }
                                    else
                                        throw new Exception();
                                    if (product.Update(DataSource, ColumnMode.Include, "Image", "State", "DotPrice", "CountyPrice", "Title", "Content", "Keywords", "Description", "BarCode", "Unit", "Inventory", "InventoryAlert", "CostPrice", "Price", "MarketPrice", "Province", "City", "County", "FreightType", "FreightMoney", "FreightTemplate", "HasReceipt", "StoreCategoryId", "Settlement", "RoyaltyRate", "ProductSupplier", "Weight", "Volume") == DataStatus.Success)
                                    {
                                        if (Synchro)
                                        {
                                            if (P.Product.ModfiyByParentId(DataSource, product) != DataStatus.Success)
                                            {
                                                throw new Exception();
                                            }
                                        }
                                        DataSource.Commit();
                                        SetResult(DataStatus.Success);
                                    }
                                    else
                                        throw new Exception();
                                }
                                else
                                {
                                    if (IsDistributor())
                                    {
                                        product.ProductType = 2;
                                    }
                                    if (isAdmin)
                                    {
                                        DataSource.Rollback();
                                        SetResult(false);
                                    }
                                    else
                                    {
                                        DataStatus result = product.Insert(DataSource);
                                        if (result != DataStatus.Success)
                                            throw new Exception();
                                        string AreaStr = Request.Form["SaleArea"];
                                        if (AreaStr != "")
                                        {
                                            string[] AreaIds = AreaStr.Split(',');
                                            int saleNum = 0;
                                            for (int i = 0; i < AreaIds.Length; i++)
                                            {
                                                if (AreaIds[i] != "")
                                                {
                                                    int p = 0;
                                                    int c = 0;
                                                    int d = 0;
                                                    if (int.Parse(AreaIds[i]) != 0)
                                                    {
                                                        City city = Country.GetCity(int.Parse(AreaIds[i]));
                                                        if (city.Level == CityLevel.City)
                                                        {
                                                            p = city.ParentId;
                                                            c = city.Id;
                                                        }
                                                        else if (city.Level == CityLevel.Province)
                                                        {
                                                            p = city.Id;
                                                        }
                                                        if (city.Level == CityLevel.County)
                                                        {
                                                            City newcity = Country.GetCity(city.ParentId);
                                                            p = newcity.ParentId;
                                                            c = newcity.Id;
                                                            d = city.Id;
                                                        }
                                                    }
                                                    P.ProductSalesArea area = new Cnaws.Product.Modules.ProductSalesArea()
                                                    {
                                                        ProductId = product.Id,
                                                        Province = p,
                                                        City = c,
                                                        County = d
                                                    };
                                                    if (area.Insert(DataSource) != DataStatus.Success)
                                                        throw new Exception();
                                                    saleNum++;
                                                }
                                            }
                                            if (saleNum <= 0)
                                                throw new Exception();
                                        }
                                        else
                                            throw new Exception();


                                        ///查看是否有组合，如果有组合添加组合
                                        Regex reg = new Regex(@"SerieName_\d*");
                                        List<string> Keys = Request.Form.AllKeys.Where(name => reg.IsMatch(name)).ToList();
                                        foreach (string key in Keys)
                                        {

                                            P.ProductSerie productSerie = new P.ProductSerie() { Name = Request[key], ProductId = product.Id };
                                            if (!P.ProductSerie.Exists(DataSource, productSerie.ProductId, productSerie.Name))
                                            {
                                                if (productSerie.Insert(DataSource) != DataStatus.Success)
                                                    throw new Exception();
                                            }
                                            else
                                                throw new Exception();
                                        }
                                        foreach (string key in Keys)
                                        {
                                            int attrid = 0;
                                            string inputId = key.Replace("SerieName_", "");
                                            int.TryParse(inputId, out attrid);

                                        }
                                        if (Keys.Count > 0)
                                        {
                                            productIndex = 0;
                                            if (CreateProductSerie(Keys, 0, Request.Form, new List<KeyValuePair<long, KeyValuePair<string, string>>>(), product) != DataStatus.Success)
                                                throw new Exception();
                                        }
                                        DataSource.Commit();
                                        SetResult((int)result, product.Id);
                                        //产品上传成功之后发送短信通知
                                        if (result == DataStatus.Success)
                                        {
                                            try
                                            {
                                                SendMsg();
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                DataSource.Rollback();
                                SetResult(false, 0);
                            }
                        }
                        break;
                    case 2:
                        {
                            try
                            {
                                string ProductIds = Request.Form["ProductIds"];
                                if (!string.IsNullOrEmpty(ProductIds))
                                {
                                    ProductIds = ProductIds.Trim(',');
                                }
                                else
                                {
                                    throw new Exception();
                                }
                                string[] IdsStr = ProductIds.Split(',');
                                if (IdsStr.Length <= 0)
                                {
                                    throw new Exception();
                                }
                                long[] Ids = new long[IdsStr.Length];
                                for (int i = 0; i < IdsStr.Length; i++)
                                {
                                    Ids[i] = long.Parse(IdsStr[i]);
                                }
                                string Image = System.Web.HttpUtility.UrlDecode(Request.Form["Image"]);
                                if (string.IsNullOrEmpty(Image))
                                {
                                    throw new Exception();
                                }
                                P.Product product = P.Product.GetById(DataSource, Ids[0]);
                                string[] Images=product.GetImages();
                                if (Images.Length > 0)
                                {
                                    Images[0] = Image;
                                    product.Image = string.Join(P.Product.ImageSplitChar.ToString(), Images);
                                }
                                else
                                    product.Image = Image;
                                SetResult(P.Product.ModfiyImageByIds(DataSource, Ids, product.Image));
                            }
                            catch (Exception)
                            {
                                SetResult(false);
                            }
                            //product.Image = System.Web.HttpUtility.UrlDecode(product.Image);
                            //if (product.Id > 0)
                            //    SetResult(product.Update(DataSource, ColumnMode.Include, "Image"));
                            //else
                            //    SetResult(DataStatus.Exist);
                        }
                        break;
                    case 3:
                        {
                            P.Product product = DbTable.Load<P.Product>(Request.Form);
                            if (product.Id > 0)
                            {
                                List<P.ProductAttributeMapping> list = new List<P.ProductAttributeMapping>(Request.Form.Count);
                                foreach (string key in Request.Form.AllKeys)
                                {
                                    if (key.StartsWith("Attr"))
                                    {
                                        list.Add(new P.ProductAttributeMapping()
                                        {
                                            ProductId = product.Id,
                                            AttributeId = long.Parse(key.Substring(4)),
                                            Value = Request.Form[key]
                                        });
                                    }
                                }
                                DataSource.Begin();
                                try
                                {
                                    if (product.Update(DataSource, ColumnMode.Include, "BrandId") != DataStatus.Success)
                                        throw new Exception();
                                    foreach (P.ProductAttributeMapping item in list)
                                    {
                                        if (item.InsertOrUpdate(DataSource) != DataStatus.Success)
                                            throw new Exception();
                                    }
                                    DataSource.Commit();
                                    SetResult(true);
                                }
                                catch (Exception)
                                {
                                    DataSource.Rollback();
                                    SetResult(false);
                                }
                            }
                            else
                            {
                                SetResult(DataStatus.Exist);
                            }
                        }
                        break;
                    case 4:
                        {
                            P.Product product = DbTable.Load<P.Product>(Request.Form);
                            product.State = P.ProductState.Saved;
                            if (product.Id > 0)
                                SetResult(product.Update(DataSource, ColumnMode.Include, "State", "DiscountState", "Wholesale", "WholesalePrice", "WholesaleCount", "DiscountPrice", "DiscountBeginTime", "DiscountEndTime"));
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                    case 5:
                        {
                            P.ProductMapping product = DbTable.Load<P.ProductMapping>(Request.Form);
                            if (!string.IsNullOrEmpty(product.Value))
                            {
                                if (!P.ProductMapping.Exists(DataSource, product.ProductId, product.SerieId, product.Value))
                                {
                                    if (product.ProductId > 0)
                                    {
                                        try
                                        {
                                            if (product.Update(DataSource) != DataStatus.Success)
                                                throw new Exception();
                                            else
                                            {
                                                //IList<P.ProductMapping> list = P.ProductMapping.GetAllByAllProduct(DataSource, product.ProductId);
                                                //if (list.Count > 0)
                                                //{
                                                //    new P.Product { Id = product.ProductId, Norms = string.Join("/", list.Select(x => x.Value).ToArray()) }.Update(DataSource, ColumnMode.Include, "Norms");
                                                //}
                                                SetResult(true);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            if (product.Insert(DataSource) != DataStatus.Success)
                                            { SetResult(DataStatus.Failed); }
                                            else
                                            {
                                                //IList<P.ProductMapping> list = P.ProductMapping.GetAllByAllProduct(DataSource, product.ProductId);
                                                //if (list.Count > 0)
                                                //{
                                                //    new P.Product { Id = product.ProductId, Norms = string.Join("/", list.Select(x => x.Value).ToArray()) }.Update(DataSource, ColumnMode.Include, "Norms");
                                                //}
                                                SetResult(true);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        SetResult(DataStatus.Exist);
                                    }
                                }
                                else
                                {
                                    SetResult(Common.CommUtility.EXISTS);
                                }
                            }
                            else
                            {
                                SetResult(DataStatus.Exist);
                            }
                        }
                        break;
                    case 6:
                        {
                            P.ProductSerie product = DbTable.Load<P.ProductSerie>(Request.Form);
                            if (!P.ProductSerie.Exists(DataSource, product.ProductId, product.Name))
                            {
                                if (product.ProductId > 0)
                                    SetResult(product.Insert(DataSource));
                                else
                                    SetResult(DataStatus.Exist);
                            }
                            else
                            {
                                SetResult(Common.CommUtility.EXISTS);
                            }
                        }
                        break;
                    case 7:
                        {
                            long id = long.Parse(Request.Form["Id"]);
                            long newproductId = 0;
                            if (id > 0)
                                SetResult(P.Product.CreateCopy(DataSource, id, out newproductId));
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                    case 8:
                        {
                            long id = long.Parse(Request.Form["Id"]);
                            if (id > 0)
                                SetResult(P.ProductSerie.Delete(DataSource, id));
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                    case 9:
                        {
                            long id = long.Parse(Request.Form["Id"]);
                            if (id > 0)
                                SetResult(P.Product.RemoveToRecycleBin(DataSource, id));
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                    case 10:
                        {
                            long id = long.Parse(Request.Form["ProductId"]);
                            if (id > 0)
                                SetResult(new P.Product() { Id = id, Inventory = int.Parse(Request.Form["Inventory"]) }.Update(DataSource, ColumnMode.Include, "Inventory"));
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                    case 11:
                        {
                            long id = long.Parse(Request.Form["ProductId"]);
                            if (id > 0)
                                if (id > 0)
                                {
                                    P.Product product = new P.Product() { Id = id, State = P.ProductState.Saved, CostPrice = Money.Parse(Request.Form["CostPrice"]) };
                                    ///如果是本地产品，供应商价格等于出厂价
                                    if (IsSupplier())
                                    {
                                        P.Product oldproduct = P.Product.GetById(DataSource, product.Id);
                                        if (oldproduct.CostPrice != product.CostPrice)
                                        {
                                            Money baseMoney = oldproduct.Price - product.CostPrice;
                                            product.CountyPrice = product.CostPrice + (Money)(Math.Ceiling((double)baseMoney * 15.0) / 100);
                                            product.DotPrice = product.CountyPrice + (Money)(Math.Ceiling((double)baseMoney * 25.0) / 100);
                                        }
                                        else
                                        {
                                            product.CountyPrice = oldproduct.CountyPrice;
                                            product.DotPrice = oldproduct.DotPrice;
                                        }
                                        SetResult(product.Update(DataSource, ColumnMode.Include, "DotPrice", "CountyPrice", "State", "CostPrice"));
                                    }
                                    else if (IsDistributor())
                                    {
                                        SetResult(product.Update(DataSource, ColumnMode.Include, "State", "CostPrice"));
                                    }
                                }
                                else
                                    SetResult(DataStatus.Exist);
                        }
                        break;
                    case 12:
                        {
                            long id = long.Parse(Request.Form["ProductId"]);
                            if (id > 0)
                            {
                                P.Product product = new P.Product() { Id = id, State = P.ProductState.Saved, Price = Money.Parse(Request.Form["Price"]) };
                                if (IsSupplier())
                                {
                                    P.Product oldproduct = P.Product.GetById(DataSource, product.Id);
                                    if (oldproduct.Price != product.Price)
                                    {
                                        Money baseMoney = product.Price - oldproduct.CostPrice;
                                        product.CountyPrice = oldproduct.CostPrice + (Money)(Math.Ceiling((double)baseMoney * 15.0) / 100);
                                        product.DotPrice = product.CountyPrice + (Money)(Math.Ceiling((double)baseMoney * 25.0) / 100);
                                    }
                                    else
                                    {
                                        product.CountyPrice = oldproduct.CountyPrice;
                                        product.DotPrice = oldproduct.DotPrice;
                                    }
                                    SetResult(product.Update(DataSource, ColumnMode.Include, "DotPrice", "CountyPrice", "State", "Price"));
                                }
                                else if (IsDistributor())
                                {
                                    SetResult(product.Update(DataSource, ColumnMode.Include, "State", "Price"));
                                }
                            }
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                    case 13:
                        {
                            long id = long.Parse(Request.Form["ProductId"]);
                            if (id > 0)
                            {
                                P.Product product = new P.Product() { Id = id, State = P.ProductState.Saved, CountyPrice = Money.Parse(Request.Form["CountyPrice"]) };

                                if (IsDistributor())
                                {
                                    SetResult(product.Update(DataSource, ColumnMode.Include, "State", "CountyPrice"));
                                }
                                else
                                {
                                    SetResult(false);
                                }
                            }
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                    case 14:
                        {
                            long id = long.Parse(Request.Form["ProductId"]);
                            if (id > 0)
                            {
                                P.Product product = new P.Product() { Id = id, State = P.ProductState.Saved, DotPrice = Money.Parse(Request.Form["DotPrice"]) };

                                if (IsDistributor())
                                {
                                    SetResult(product.Update(DataSource, ColumnMode.Include, "State", "DotPrice"));
                                }
                                else
                                {
                                    SetResult(false);
                                }
                            }
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
        public DataStatus CreateProductSerie(List<string> Keys, int currIndex, NameValueCollection request, List<KeyValuePair<long, KeyValuePair<string, string>>> Attr, P.Product p)
        {
            int attrid = 0;
            string inputId = Keys[currIndex].Replace("SerieName_", "");
            string SerieName = request["SerieName_" + inputId];
            P.ProductSerie Serie = P.ProductSerie.GetByProductAndName(DataSource, p.Id, SerieName);
            int.TryParse(inputId, out attrid);
            string[] AttrValues = request["AttrValue_" + inputId].Split(',');
            foreach (string AttrValue in AttrValues)
            {
                if (!string.IsNullOrEmpty(AttrValue))
                {
                    if (currIndex < Keys.Count - 1)
                    {
                        KeyValuePair<string, string> SerieNameAndValue = new KeyValuePair<string, string>(AttrValue, SerieName);
                        List<KeyValuePair<long, KeyValuePair<string, string>>> NewAttr = new List<KeyValuePair<long, KeyValuePair<string, string>>>();
                        NewAttr.AddRange(Attr);
                        NewAttr.Add(new KeyValuePair<long, KeyValuePair<string, string>>(Serie.Id, SerieNameAndValue));
                        int NewCurrIndex = currIndex + 1;
                        if (CreateProductSerie(Keys, NewCurrIndex, request, NewAttr, p) != DataStatus.Success)
                            return DataStatus.Failed;
                    }
                    else
                    {
                        KeyValuePair<string, string> SerieNameAndValue = new KeyValuePair<string, string>(AttrValue, SerieName);
                        Dictionary<long, string> a = new Dictionary<long, string>();
                        List<KeyValuePair<long, KeyValuePair<string, string>>> NewAttr = new List<KeyValuePair<long, KeyValuePair<string, string>>>();
                        NewAttr.AddRange(Attr);
                        NewAttr.Add(new KeyValuePair<long, KeyValuePair<string, string>>(Serie.Id, SerieNameAndValue));
                        long newproductId = 0;
                        if (productIndex == 0)
                        {
                            newproductId = p.Id;
                        }
                        else
                        {
                            if (P.Product.CreateCopy(DataSource, p.Id, out newproductId) != DataStatus.Success)
                                return DataStatus.Failed;
                        }
                        productIndex++;
                        foreach (KeyValuePair<long, KeyValuePair<string, string>> seriePair in NewAttr)
                        {
                            P.ProductMapping product = new P.ProductMapping() { Value = seriePair.Value.Key, SerieId = seriePair.Key, ProductId = newproductId, Name = seriePair.Value.Value };
                            if (product.Insert(DataSource) != DataStatus.Success)
                                return DataStatus.Failed;
                            else
                            {
                                //try
                                //{
                                //    IList<P.ProductMapping> list = P.ProductMapping.GetAllByAllProduct(DataSource, product.ProductId);
                                //    if (list.Count > 0)
                                //    {
                                //        new P.Product { Id = product.ProductId, Norms = string.Join("/", list.Select(x => x.Value).ToArray()) }.Update(DataSource, ColumnMode.Include, "Norms");
                                //    }
                                //}
                                //catch (Exception) { }
                            }
                        }
                    }
                }
            }
            return DataStatus.Success;
        }


        /// <summary>
        /// 产品上传成功之后发送短信通知
        /// </summary>
        private void SendMsg()
        {
            string strMobile = string.IsNullOrEmpty(Supplier.ContactPhone) ? Supplier.SignatoriesPhone : Supplier.ContactPhone;
            long mobile = 0;
            long.TryParse(strMobile, out mobile);
            if (mobile > 0)
            {
                string surName = string.IsNullOrEmpty(Supplier.Contact) ? Supplier.Signatories : Supplier.Contact;
                if (!string.IsNullOrEmpty(surName)) surName = surName.Substring(0, 1);

                SmsMobset.Send(
                           DataSource,
                           mobile,
                           SmsTemplate.SupplierUploadedProduct,
                           surName,
                           P.Product.GetProductCountBySupplierId(DataSource, User.Identity.Id).ToString(),
                           User.Identity.Name);
            }
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void Submit(int step = 1)
        {
            SubmitImpl(step, false);
        }
        [HttpAjax]
        [HttpPost]
        [AdminAuthorize]
        public void SubmitEx(int step = 1)
        {
            SubmitImpl(step, true);
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        private void UpdateState(P.ProductState ns, P.ProductState os)
        {
            try
            {
                SetResult((new P.Product() { Id = long.Parse(Request.Form["Id"]), State = ns }).UpdateState(DataSource, User.Identity.Id, os));
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void Sale()
        {
            if (IsSupplier())
                UpdateState(P.ProductState.BeforeSale, P.ProductState.Saved);
            else if (IsDistributor())
                UpdateState(P.ProductState.Sale, P.ProductState.Saved);
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void Save()
        {
            UpdateState(P.ProductState.Saved, P.ProductState.Sale);
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void Cancel()
        {
            UpdateState(P.ProductState.Saved, P.ProductState.BeforeSale);
        }
        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void Delete()
        {

            if (IsSupplier())
            {
                UpdateState(P.ProductState.Deleted, P.ProductState.Saved);
            }
            else
            {
                UpdateState(P.ProductState.Deleted, P.ProductState.Saved);
            }
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void Inventory()
        {
            try
            {
                int value = 0;
                P.Product product = DbTable.Load<P.Product>(Request.Form);
                if (product != null && product.Inventory > 0)
                {
                    DataStatus status = product.UpdateInventory(DataSource, User.Identity.Id);
                    if (status == DataStatus.Success)
                        value = P.Product.GetById(DataSource, product.Id).Inventory;
                    SetResult((int)status, value);
                }
                else
                {
                    SetResult(false);
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }

        [SupplierOrDistributor(true)]
        public void Logistics(string id)
        {
            P.ProductLogistics log;
            D.DistributorOrder DisOrder = D.DistributorOrder.GetById(DataSource, id);
            if (DisOrder != null)
            {
                if (DisOrder.State > P.OrderState.Delivery)
                {
                    log = P.ProductLogistics.GetByOrder(DataSource, DisOrder.Id);
                    if (log != null)
                    {
                        try
                        {
                        }
                        catch (Exception) { }
                    }
                    else
                    {
                        log = new P.ProductLogistics();
                    }
                }
                else { log = new P.ProductLogistics(); }
                this["Order"] = DisOrder;
                this["OrderType"] = 1;
            }
            else
            {
                P.ProductOrder order = P.ProductOrder.GetById(DataSource, id);
                if (order != null && order.State > P.OrderState.Delivery)
                {
                    log = P.ProductLogistics.GetByOrder(DataSource, order.Id);
                    if (log != null)
                    {
                        try
                        {
                        }
                        catch (Exception) { }
                    }
                    else
                    {
                        log = new P.ProductLogistics();
                    }
                }
                else
                {
                    log = new P.ProductLogistics();
                }
                this["Order"] = order;
                this["OrderType"] = 0;
            }
            this["Logistics"] = log;
            if (!string.IsNullOrEmpty(log.BillNo))
                this["ExpressInfo"] = Cnaws.Product.Logistics.ExpressQuery.Query(log.ProviderKey, log.BillNo);
            else
                this["ExpressInfo"] = null;
            this["GetDay"] = new FuncHandler((args) =>
            {
                if (!string.IsNullOrEmpty(Convert.ToString(args[0])))
                    return Convert.ToDateTime(args[0]).ToString("yyyy-MM-dd");
                else
                    return "";
            });
            this["GetWeek"] = new FuncHandler((args) =>
            {
                if (!string.IsNullOrEmpty(Convert.ToString(args[0])))
                {
                    string week;
                    string dt = Convert.ToDateTime(args[0]).DayOfWeek.ToString();
                    switch (dt)
                    {
                        case "Monday":
                            week = "周一";
                            break;
                        case "Tuesday":
                            week = "周二";
                            break;
                        case "Wednesday":
                            week = "周三";
                            break;
                        case "Thursday":
                            week = "周四";
                            break;
                        case "Friday":
                            week = "周五";
                            break;
                        case "Saturday":
                            week = "周六";
                            break;
                        case "Sunday":
                            week = "周日";
                            break;
                        default:
                            week = "";
                            break;
                    }
                    return week;
                }
                else
                    return "";
            });
            this["GetTime"] = new FuncHandler((args) =>
             {
                 if (!string.IsNullOrEmpty(Convert.ToString(args[0])))
                     return Convert.ToDateTime(args[0]).ToString("HH:mm:ss");
                 else
                     return "";
             });
            if (IsSupplier())
            {
                Render("supplier/product_logistics.html");
            }
            else
            {
                Render("product_logistics.html");
            }
        }

        private void ListImpl(long user, string type, long page, bool isAdmin)
        {
            //saved sale order
            int productType = IsSupplier() ? 1 : 2;
            type = type.ToLower();
            string keyword = "";
            if (!string.IsNullOrEmpty(Request["keyword"]))
            {
                keyword = Request["keyword"];
            }
            switch (type)
            {
                case "saved":
                    this["ProductList"] = P.Product.GetBySupplierStateOrState(DataSource, user, keyword, P.ProductState.Saved, P.ProductState.BeforeSale, page, 10, 8, productType);
                    break;
                case "sale":
                    this["ProductList"] = P.Product.GetBySupplierStateOrState(DataSource, user, keyword, P.ProductState.Sale, P.ProductState.BeforeSaved, page, 10, 8, productType);
                    break;
                default:
                    NotFound();
                    return;
            }
            this["KeyWord"] = keyword;
            this["State"] = type;
            if (IsSupplier())
            {
                Render(string.Concat("supplier/product_", type, ".html"));
            }
            else
            {
                Render(string.Concat("product_", type, ".html"));
            }
        }
        [SupplierOrDistributor(true)]
        public void List(string type, long page = 1L)
        {
            ListImpl(User.Identity.Id, type, page, false);
        }


        [AdminAuthorize]
        public void ListEx(long user, string type, long page = 1L)
        {
            ListImpl(user, type, page, true);
        }




        public void UpdateStoreRecommend(long id, int storeRecommend)
        {
            SetResult(P.Product.UpdateStoreRecommend(DataSource, id, storeRecommend == 1 ? true : false, User.Identity.Id));
        }

        public void UpdateStoreSortNum(long id, int storeSortNum)
        {
            SetResult(P.Product.UpdateStoreSortNum(DataSource, id, storeSortNum, User.Identity.Id));
        }

        public void GetXDGTwoCategory(int id)
        {
            IList<P.StoreCategory> list = new P.StoreCategory() { Id = id }.GetXDGCategoryTwo(DataSource);
            if (list != null)
            {
                SetResult(-200, list);
            }
            else
            {
                SetResult(-500);
            }
        }
    }
}
