using Cnaws;
using Cnaws.Management;
using Cnaws.Web;
using System;
using M = XcpNet.Supplier.Modules.Modules;

namespace XcpNet.Supplier.Management
{
    public sealed class DistributorBrand : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Supplier"; }
        }

        public void Index(int id = 0)
        {
            if (CheckRight())
            {
                if (CheckPost("distributorbrand", () =>
                {
                    this["Id"] = id;
                    this["Parents"] = M.DistributorCategory.GetAllParentsById(DataSource, id);
                    this["AllCategory"] = M.DistributorCategory.GetAll(DataSource, -1);
                }))
                {
                    NotFound();
                }
            }
        }
        //public void AllCategory()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            IList<M.ProductCategory> list = M.ProductCategory.GetAll(DataSource, -1);
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
        //            SetResult(M.ProductCategory.GetAll(DataSource, parentId));
        //    }
        //}
        //public void Parents(int id)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            List<int> list = new List<int>();
        //            M.ProductCategory ac = M.ProductCategory.GetById(DataSource, id);
        //            list.Add(ac.Id);
        //            while (ac.ParentId != 0)
        //            {
        //                ac = M.ProductCategory.GetById(DataSource, ac.ParentId);
        //                list.Insert(0, ac.Id);
        //            }
        //            SetResult(list.ToArray());
        //        }
        //    }
        //}
        public void List(int categoryId = -1, string sort = "", string order = "", int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                    SetResult(M.DistributorBrand.GetPageEx(DataSource, categoryId, sort, order, page, 10));
            }
        }
        public void Add()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.DistributorBrand brand = new M.DistributorBrand()
                        {
                            Name = Request["Name"],
                            FirstChar = Request["FirstChar"],
                            //CategoryId = int.Parse(Request["CategoryId"]),
                            Logo = Request["Logo"],
                            Recommend = Types.GetBooleanFromString(Request["Recommend"]),
                            SortNum = int.Parse(Request["SortNum"]),
                            Approved = Types.GetBooleanFromString(Request["Approved"])
                        };

                        DataStatus status;
                        DataSource.Begin();
                        try
                        {
                            string[] Categorys = Request["Categorys"].Split(',');
                            if (Categorys.Length > 0)
                            {
                                for (int i = 0; i < Categorys.Length; i++)
                                {
                                    M.DistributorBrandMapping m = new M.DistributorBrandMapping();
                                    m.BrandId = brand.Id;
                                    m.CategoryId = int.Parse(Categorys[i]);
                                    if (M.DistributorBrandMapping.Add(DataSource, m) != DataStatus.Success)
                                        throw new Exception();
                                }
                            }

                            if (brand.Insert(DataSource) != DataStatus.Success)
                                throw new Exception();

                            DataSource.Commit();
                            status = DataStatus.Success;
                        }
                        catch (Exception)
                        {
                            DataSource.Rollback();
                            status = DataStatus.Failed;
                        }
                        SetResult(status, () =>
                        {
                            WritePostLog("ADD");
                        });
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        //public void Edit()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                try {
        //                    M.ProductBrand brand = new M.ProductBrand()
        //                    {
        //                        Id = int.Parse(Request["Id"]),
        //                        Name = Request["Name"],
        //                        FirstChar = Request["FirstChar"],
        //                        //CategoryId = int.Parse(Request["CategoryId"]),
        //                        Logo = Request["Logo"],
        //                        Recommend = Types.GetBooleanFromString(Request["Recommend"]),
        //                        SortNum = int.Parse(Request["SortNum"]),
        //                        Approved = Types.GetBooleanFromString(Request["Approved"])
        //                    };

        //                    string[] CategorysStr = Request["Categorys"].Split('|');
        //                    IList<M.ProductCategory> productbrandmapping = M.ProductBrandMapping.GetCategoryListByBrandId(DataSource, brand.Id);
        //                    List<int> list = new List<int>();
        //                    foreach(M.ProductCategory cate in productbrandmapping)
        //                    {
        //                        list.Add(cate.Id);
        //                    }

        //                    if (CategorysStr.Length > 0)
        //                    {
        //                        for (int i = 0; i < CategorysStr.Length; i++)
        //                        {
        //                            try
        //                            {
        //                                list.Remove(int.Parse(CategorysStr[i]));
        //                            }
        //                            catch (Exception) { }
        //                            M.ProductBrandMapping m = new M.ProductBrandMapping();
        //                            m.BrandId = brand.Id;
        //                            m.CategoryId = int.Parse(CategorysStr[i]);
        //                            if (M.ProductBrandMapping.Add(DataSource, m) != DataStatus.Success)
        //                                throw new Exception();
        //                        }
        //                        M.ProductBrandMapping.Del(DataSource, list, brand.Id);
        //                    }

        //                    SetResult(brand.Update(DataSource), () =>
        //                    {
        //                        WritePostLog("MOD");
        //                    });
        //                }
        //                catch (Exception ex)
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
        public void Mod()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.DistributorBrand brand = new M.DistributorBrand()
                        {
                            Id = int.Parse(Request["Id"]),
                            Name = Request["Name"],
                            FirstChar = Request["FirstChar"],
                            //CategoryId = int.Parse(Request["CategoryId"]),
                            Logo = Request["Logo"],
                            Recommend = Types.GetBooleanFromString(Request["Recommend"]),
                            SortNum = int.Parse(Request["SortNum"]),
                            Approved = Types.GetBooleanFromString(Request["Approved"])
                        };

                        DataStatus status;
                        DataSource.Begin();
                        try
                        {
                            if (M.DistributorBrandMapping.DelByBrandId(DataSource, brand.Id) != DataStatus.Success)
                                throw new Exception();

                            string[] Categorys = Request["Categorys"].Split(',');
                            if (Categorys.Length > 0)
                            {
                                for (int i = 0; i < Categorys.Length; i++)
                                {
                                    M.DistributorBrandMapping m = new M.DistributorBrandMapping();
                                    m.BrandId = brand.Id;
                                    m.CategoryId = int.Parse(Categorys[i]);
                                    if (M.DistributorBrandMapping.Add(DataSource, m) != DataStatus.Success)
                                        throw new Exception();
                                }
                            }

                            if (brand.Update(DataSource) != DataStatus.Success)
                                throw new Exception();

                            DataSource.Commit();
                            status = DataStatus.Success;
                        }
                        catch (Exception)
                        {
                            DataSource.Rollback();
                            status = DataStatus.Failed;
                        }
                        SetResult(status, () =>
                        {
                            WritePostLog("MOD");
                        });
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
                        M.DistributorBrand brand = new M.DistributorBrand()
                        {
                            Id = int.Parse(Request["Id"])
                        };

                        DataStatus status;
                        DataSource.Begin();
                        try
                        {
                            if (M.DistributorBrandMapping.DelByBrandId(DataSource, brand.Id) != DataStatus.Success)
                                throw new Exception();
                            if (brand.Delete(DataSource) != DataStatus.Success)
                                throw new Exception();
                            DataSource.Commit();
                            status = DataStatus.Success;
                        }
                        catch (Exception)
                        {
                            DataSource.Rollback();
                            status = DataStatus.Failed;
                        }
                        SetResult(status, () =>
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
        //        if (CheckRight()) {
        //            M.ProductBrand productbrand = M.ProductBrand.GetById(DataSource, id);
        //            IList<M.ProductCategory> productcategory = M.ProductBrandMapping.GetCategoryListByBrandId(DataSource, id);
        //            SetResult(new { ProductBrand = productbrand, ProductCategory = productcategory });

        //        }
        //    }
        //}
    }
}
