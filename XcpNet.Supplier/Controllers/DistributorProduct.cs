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

namespace XcpNet.Supplier.Controllers
{
    public sealed class DistributorProduct : SupplierController
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

        protected override void Dispose(bool disposing)
        {
            if (_country != null)
            {
                _country.Dispose();
                _country = null;
            }
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
                if (product.StoreCategoryId > 0)
                {
                    P.StoreCategory xdgCategory = P.StoreCategory.GetXDGCategoryByCategoryId(DataSource, product.StoreCategoryId);
                    if (xdgCategory != null)
                    {
                        this["XDGParentCategoryId"] = xdgCategory.ParentId;
                    }
                }
            }
            Render("product.html");
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
                            P.Product product = DbTable.Load<P.Product>(Request.Form);
                            product.SupplierId = User.Identity.Id;
                            product.Province = int.Parse(Request.Form["area_provinces"]);
                            product.City = int.Parse(Request.Form["area_cities"]);
                            product.County = int.Parse(Request.Form["area_counties"]);
                            product.CreationDate = DateTime.Now;
                            product.Content = System.Web.HttpUtility.UrlDecode(product.Content);
                            if (product.Settlement == P.SettlementType.Fixed)
                            {
                                product.RoyaltyRate = 0;
                            }
                            if (product.Id > 0)
                            {
                                SetResult(product.Update(DataSource, ColumnMode.Include, "Title", "Content", "Keywords", "Description", "BarCode", "Unit", "Inventory", "InventoryAlert", "CostPrice", "Price", "MarketPrice", "Wholesale", "WholesalePrice", "WholesaleCount", "Province", "City", "County", "FreightType", "FreightMoney", "FreightTemplate", "HasReceipt", "StoreCategoryId", "Settlement", "RoyaltyRate", "ProductSupplier"));
                            }
                            else
                            {
                                if (IsDistributor())
                                {
                                    product.ProductType = 2;
                                }
                                if (isAdmin)
                                {
                                    SetResult(false);
                                }
                                else
                                {
                                    DataStatus result = product.Insert(DataSource);
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
                        break;
                    case 2:
                        {
                            P.Product product = DbTable.Load<P.Product>(Request.Form);
                            product.Image = System.Web.HttpUtility.UrlDecode(product.Image);
                            if (product.Id > 0)
                                SetResult(product.Update(DataSource, ColumnMode.Include, "Image"));
                            else
                                SetResult(DataStatus.Exist);
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
                            if (product.Id > 0)
                                SetResult(product.Update(DataSource, ColumnMode.Include, "DiscountState", "DiscountPrice", "DiscountBeginTime", "DiscountEndTime"));
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                    case 5:
                        {
                            P.ProductMapping product = DbTable.Load<P.ProductMapping>(Request.Form);
                            if (product.ProductId > 0)
                            {
                                try
                                {
                                    if (product.Update(DataSource) != DataStatus.Success)
                                        throw new Exception();
                                    else
                                    {
                                        IList<P.ProductMapping> list = P.ProductMapping.GetAllByAllProduct(DataSource, product.ProductId);
                                        if (list.Count > 0)
                                        {
                                            new P.Product { Id = product.ProductId, Norms = string.Join("/", list.Select(x => x.Value).ToArray()) }.Update(DataSource, ColumnMode.Include, "Norms");
                                        }
                                        SetResult(true);
                                    }
                                }
                                catch (Exception)
                                {
                                    if (product.Insert(DataSource) != DataStatus.Success)
                                    { SetResult(DataStatus.Failed); }
                                    else
                                    {
                                        IList<P.ProductMapping> list = P.ProductMapping.GetAllByAllProduct(DataSource, product.ProductId);
                                        if (list.Count > 0)
                                        {
                                            new P.Product { Id = product.ProductId, Norms = string.Join("/", list.Select(x => x.Value).ToArray()) }.Update(DataSource, ColumnMode.Include, "Norms");
                                        }
                                        SetResult(true);
                                    }
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
                            if (product.ProductId > 0)
                                SetResult(product.Insert(DataSource));
                            else
                                SetResult(DataStatus.Exist);
                        }
                        break;
                    case 7:
                        {
                            long id = long.Parse(Request.Form["Id"]);
                            if (id > 0)
                                SetResult(P.Product.CreateCopy(DataSource, id));
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
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
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
            UpdateState(P.ProductState.BeforeSale, P.ProductState.Saved);
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void Save()
        {
            UpdateState(P.ProductState.BeforeSaved, P.ProductState.Sale);
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void Delete()
        {
            UpdateState(P.ProductState.Deleted, P.ProductState.Saved);
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
            }
            this["Logistics"] = log;
            this["ExpressInfo"] = Cnaws.Product.Logistics.ExpressQuery.Query(log.ProviderKey, log.BillNo);
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
            Render("product_logistics.html");
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
                    this["ProductList"] = P.Product.GetBySupplierStateOrState(DataSource, user, keyword, P.ProductState.Saved, P.ProductState.BeforeSale, page, 10, 11, productType);
                    break;
                case "sale":
                    this["ProductList"] = P.Product.GetBySupplierStateOrState(DataSource, user, keyword, P.ProductState.Sale, P.ProductState.BeforeSaved, page, 10, 11, productType);
                    break;
                default:
                    NotFound();
                    return;
            }
            this["State"] = type;
            Render(string.Concat("product_", type, ".html"));
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

        [SupplierOrDistributor(true)]
        public void Order(string type, long page = 0L)
        {
            //delivery all finished
            type = type.ToLower();
            switch (type)
            {
                case "payment":
                    this["OrderList"] = D.DistributorOrder.GetPageBySupplier(DataSource, User.Identity.Id, (int)P.OrderState.Payment, Math.Max(1, page), 10, 11);
                    break;
                case "delivery":
                    this["OrderList"] = D.DistributorOrder.GetPageBySupplier(DataSource, User.Identity.Id, (int)P.OrderState.Delivery, Math.Max(1, page), 10, 11);
                    break;
                case "dooutwarehouse"://出库
                    if (IsPost)
                    {
                        DataSource.Begin();
                        D.DistributorOrder order = new D.DistributorOrder();
                        string OrderId = Request.Form["OrderId"];
                        try
                        {
                            order = D.DistributorOrder.GetById(DataSource, OrderId);
                            if ((new D.DistributorOrder { Id = OrderId, UserId = order.UserId }).UpdateStateByUser(DataSource, P.OrderState.Delivery) != DataStatus.Success)
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
                    else
                    {
                        NotFound();
                    }
                    return;
                case "dodelivery":///发货
                    if (IsPost)
                    {
                        DataSource.Begin();
                        D.DistributorOrder order = new D.DistributorOrder();
                        P.ProductLogistics value = new P.ProductLogistics();
                        try
                        {
                            value = DbTable.Load<P.ProductLogistics>(Request.Form);
                            if (P.ProductLogistics.GetByOrder(DataSource, value.OrderId) == null)
                            {
                                if (value.Insert(DataSource) != DataStatus.Success)
                                    throw new Exception();
                                order = D.DistributorOrder.GetById(DataSource, value.OrderId);
                                if ((new D.DistributorOrder() { Id = value.OrderId, UserId = order.UserId }).UpdateStateByUser(DataSource, P.OrderState.OutWarehouse) != DataStatus.Success)
                                    throw new Exception();
                            }
                            else
                            {
                                if (value.Update(DataSource) != DataStatus.Success)
                                    throw new Exception();
                            }
                            DataSource.Commit();
                            SetResult(true);
                            try
                            {
                                //发货成功之后发送短信通知
                                M.MemberInfo member = M.MemberInfo.GetById(DataSource, order.UserId);
                                if (Member.Mobile > 0)
                                {
                                    SmsMobset.Send(
                                       DataSource,
                                       Member.Mobile,
                                       SmsTemplate.HasShipped,
                                       !string.IsNullOrEmpty(member.RealName) ? member.RealName : !string.IsNullOrEmpty(member.Name) ? member.Name : string.Empty,
                                       order.Id,
                                       value.ProviderName,
                                       value.BillNo);
                                }
                            }
                            catch (Exception) { }
                        }
                        catch (Exception)
                        {
                            DataSource.Rollback();
                            SetResult(false);
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                    return;
                case "dofreight":
                    if (IsPost)
                    {
                        D.DistributorOrder value = DbTable.Load<D.DistributorOrder>(Request.Form);
                        value.SupplierId = User.Identity.Id;
                        SetResult(value.UpdateFreightBySupplier(DataSource));
                    }
                    else
                    {
                        NotFound();
                    }
                    return;
                case "all":
                    if (page < 1L) page = 1L;
                    this["OrderList"] = D.DistributorOrder.GetPageBySupplier(DataSource, User.Identity.Id, -1, page, 10, 11);
                    break;
                case "finished":
                    if (page < 1L) page = 1L;
                    this["OrderList"] = D.DistributorOrder.GetPageBySupplier(DataSource, User.Identity.Id, (int)P.OrderState.Finished, page, 10, 11);
                    break;
                case "getlogistics":
                    string orderid = Request["orderid"];
                    this["Logistics"] = P.ProductLogistics.GetByOrder(DataSource, orderid);
                    type = "all";
                    break;
                default:
                    NotFound();
                    return;
            }
            this["Page"] = page;
            this["State"] = type;
            if (IsSupplier())
                Render(string.Concat("supplier/distributororder_", type, ".html"));
            else if (IsDistributor())
                Render(string.Concat("order_", type, ".html"));
        }
        private void FreightTemplate(long user)
        {
            this["GetArea"] = new FuncHandler((args) =>
            {
                int p = Convert.ToInt32(args[0]);
                int c = Convert.ToInt32(args[1]);
                if (p == 0) return "全国";
                if (c == 0) return Country.GetCity(p).Name;
                return Country.GetCity(c).Name;
            });
            this["Templates"] = P.FreightTemplate.GetAllBySeller(DataSource, user);
            Render("freighttemplate.html");
        }
        [SupplierOrDistributor(true)]
        public void FreightTemplate()
        {
            FreightTemplate(User.Identity.Id);
        }
        [AdminAuthorize]
        public void FreightTemplateEx(long user)
        {
            FreightTemplate(user);
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void AddFreight()
        {
            DataSource.Begin();
            try
            {
                P.FreightTemplate tmp = DbTable.Load<P.FreightTemplate>(Request.Form);
                tmp.SellerId = User.Identity.Id;
                if (tmp.Insert(DataSource) != DataStatus.Success)
                    throw new Exception(string.IsNullOrEmpty(tmp.Name) ? "请输入模板名称" : string.Empty);

                P.FreightMapping map = new P.FreightMapping()
                {
                    TemplateId = tmp.Id,
                    Number = 0,
                    Money = 0,
                    StepNumber = 0,
                    StepMoney = 0
                };
                if (map.Insert(DataSource) != DataStatus.Success)
                    throw new Exception();

                P.FreightAreaMapping area = new P.FreightAreaMapping()
                {
                    TemplateId = tmp.Id,
                    MappingId = map.Id,
                    ProvinceId = 0,
                    CityId = 0
                };
                if (area.Insert(DataSource) != DataStatus.Success)
                    throw new Exception();

                DataSource.Commit();
                SetResult(true);
            }
            catch (Exception e)
            {
                DataSource.Rollback();
                SetResult(false, e.Message);
            }
        }

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void AddMapping()
        {
            DataSource.Begin();
            try
            {
                string[] ps = Request.Form["ProvinceId"].Split(',');
                string[] cs = Request.Form["CityId"].Split(',');
                if (ps.Length == 0 || ps.Length != cs.Length)
                    throw new Exception();

                P.FreightMapping map = DbTable.Load<P.FreightMapping>(Request.Form);
                P.FreightTemplate tmp = P.FreightTemplate.GetById(DataSource, map.TemplateId);
                if (tmp == null || tmp.SellerId != User.Identity.Id)
                    throw new Exception();
                if (map.Insert(DataSource) != DataStatus.Success)
                    throw new Exception();

                P.FreightAreaMapping area;
                for (int j = 0; j < ps.Length; ++j)
                {
                    area = new P.FreightAreaMapping()
                    {
                        TemplateId = map.TemplateId,
                        MappingId = map.Id,
                        ProvinceId = int.Parse(ps[j]),
                        CityId = int.Parse(cs[j])
                    };
                    if (area.Insert(DataSource) != DataStatus.Success)
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

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void ModMapping()
        {
            DataSource.Begin();
            try
            {
                string[] ps = Request.Form["ProvinceId"].Split(',');
                string[] cs = Request.Form["CityId"].Split(',');
                if (ps.Length == 0 || ps.Length != cs.Length)
                    throw new Exception();

                P.FreightMapping map = DbTable.Load<P.FreightMapping>(Request.Form);
                P.FreightMapping tmap = P.FreightMapping.GetById(DataSource, map.Id);
                if (tmap == null)
                    throw new Exception();
                P.FreightTemplate tmp = P.FreightTemplate.GetById(DataSource, tmap.TemplateId);
                if (tmp == null || tmp.SellerId != User.Identity.Id)
                    throw new Exception();
                map.TemplateId = tmap.TemplateId;
                if (map.Update(DataSource) != DataStatus.Success)
                    throw new Exception();

                if (P.FreightAreaMapping.DeleteByMapping(DataSource, map.Id) != DataStatus.Success)
                    throw new Exception();

                P.FreightAreaMapping area;
                for (int j = 0; j < ps.Length; ++j)
                {
                    area = new P.FreightAreaMapping()
                    {
                        TemplateId = map.TemplateId,
                        MappingId = map.Id,
                        ProvinceId = int.Parse(ps[j]),
                        CityId = int.Parse(cs[j])
                    };
                    if (area.Insert(DataSource) != DataStatus.Success)
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

        [HttpAjax]
        [HttpPost]
        [SupplierOrDistributor]
        public void DelMapping()
        {
            DataSource.Begin();
            try
            {
                P.FreightMapping map = DbTable.Load<P.FreightMapping>(Request.Form);
                P.FreightMapping tmap = P.FreightMapping.GetById(DataSource, map.Id);
                if (tmap == null)
                    throw new Exception();
                P.FreightTemplate tmp = P.FreightTemplate.GetById(DataSource, tmap.TemplateId);
                if (tmp == null || tmp.SellerId != User.Identity.Id)
                    throw new Exception();
                if (map.Delete(DataSource) != DataStatus.Success)
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
        public void DelFreight()
        {
            DataSource.Begin();
            try
            {
                P.FreightTemplate tmp = DbTable.Load<P.FreightTemplate>(Request.Form);
                P.FreightTemplate tmap = P.FreightTemplate.GetById(DataSource, tmp.Id);
                if (tmap == null || tmap.SellerId != User.Identity.Id)
                    throw new Exception();
                if (tmp.Delete(DataSource) != DataStatus.Success)
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
