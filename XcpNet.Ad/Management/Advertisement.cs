using System;
using System.Web;
using System.Collections.Generic;
using Cnaws.Data;
using Cnaws.Management;
using Cnaws.Web;
using Cnaws;
using M = XcpNet.Ad.Modules;
using P = Cnaws.Product.Modules;
using D = XcpNet.Supplier.Modules.Modules;

namespace XcpNet.Ad.Management
{
    public sealed class Advertisement : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }
        protected override string Namespace
        {
            get { return "XcpNet.Ad"; }
        }

        public void Index(int id = 0)
        {
            if (CheckRight())
            {
                if (CheckPost("advertisement", () =>
                {
                    this["Id"] = id;
                    this["Types"] = M.AdType.GetAll(DataSource, id);
                    if (id == (int)M.AdType.EAdType.Category)
                        this["AllCategory"] = P.ProductCategory.GetAll(DataSource, -1);
                    else if (id == (int)M.AdType.EAdType.DsitributorCategory)
                        this["AllCategory"] = D.DistributorCategory.GetAll(DataSource, -1);
                }))
                    NotFound();
            }
        }

        public void List(long label, int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                    SetResult(M.Advertisement.GetPageByType(DataSource, label, page, 10));
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
                        M.Advertisement ad = DbTable.Load<M.Advertisement>(Request.Form);
                        //if (!string.IsNullOrEmpty(Request.Form["area_provinces"]))
                        //    ad.Province = int.Parse(Request.Form["area_provinces"]);
                        //if (!string.IsNullOrEmpty(Request.Form["area_cities"]))
                        //    ad.City = int.Parse(Request.Form["area_cities"]);
                        //if (!string.IsNullOrEmpty(Request.Form["area_counties"]))
                        //    ad.County = int.Parse(Request.Form["area_counties"]);

                        SetResult(ad.Insert(DataSource), () =>
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

        public void Mod()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.Advertisement ad;
                        if (Array.IndexOf(Request.Form.AllKeys, "Id") >= 0)
                        {
                            ad = new M.Advertisement()
                            {
                                Id = int.Parse(Request.Form["Id"]),
                                LabelId = int.Parse(Request.Form["LabelId"]),
                                Title = Request.Form["Title"],
                                ImgUrl = Request.Form["ImgUrl"],
                                Width = int.Parse(Request.Form["Width"]),
                                Height = int.Parse(Request.Form["Height"]),
                                IsEnable = Types.GetBooleanFromString(Request.Form["IsEnable"]),
                                Sort = int.Parse(Request.Form["Sort"])
                            };
                        }
                        else
                        {
                            ad = new M.Advertisement()
                            {
                                Id = int.Parse(Request.Form["a_Id"]),
                                LabelId = int.Parse(Request.Form["a_LabelId"]),
                                Title = Request.Form["a_Title"],
                                ImgUrl = Request.Form["a_ImgUrl"],
                                Width = int.Parse(Request.Form["a_Width"]),
                                Height = int.Parse(Request.Form["a_Height"]),
                                IsEnable = Types.GetBooleanFromString(Request.Form["a_IsEnable"]),
                                Sort = int.Parse(Request.Form["a_Sort"])
                            };
                        }
                        SetResult(ad.Update(DataSource, ColumnMode.Include, "Title", "LabelId", "ImgUrl", "Width", "Height", "IsEnable", "Sort"), () =>
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

        //public void Update()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                M.Advertisement ad = DbTable.Load<M.Advertisement>(Request.Form);
        //                if (!string.IsNullOrEmpty(Request.Form["area_provinces"]))
        //                    ad.Province = int.Parse(Request.Form["area_provinces"]);
        //                if (!string.IsNullOrEmpty(Request.Form["area_cities"]))
        //                    ad.City = int.Parse(Request.Form["area_cities"]);
        //                if (!string.IsNullOrEmpty(Request.Form["area_counties"]))
        //                    ad.County = int.Parse(Request.Form["area_counties"]);
        //                SetResult(ad.Update(DataSource), () =>
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

        public void Del()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        SetResult(M.Advertisement.Delete(DataSource, int.Parse(Request.Form["a_Id"])), () =>
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
        //            SetResult(M.Advertisement.GetById(DataSource, id));
        //    }
        //}
        //public void GetAllType(int type)
        //{
        //    if (CheckAjax())
        //    {
        //        SetResult(M.AdType.GetAll(DataSource, (M.AdType.EAdType)type));
        //    }
        //}
    }
}
