using System;
using System.Collections.Generic;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using M = XcpNet.Supplier.Modules.Modules;
using Pd = Cnaws.Product.Modules;
using Cnaws;

namespace XcpNet.Supplier.Management
{
    public sealed class DistributorProgramme : ManagementController
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
                if (CheckPost("distributorprogramme", () =>
                {
                    this["Id"] = id;
                    this["Parents"] = M.IndutryCategory.GetAllParentsById(DataSource, id);
                    this["AllCategory"] = M.IndutryCategory.GetAll(DataSource, -1);
                }))
                {
                    NotFound();
                }
            }
        }
        public void List(int categoryId = -1, int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                    SetResult(M.DistributorProgramme.GetPageEx(DataSource, categoryId, page, 10));
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
                        M.DistributorProgramme brand = new M.DistributorProgramme()
                        {
                            Title = Request["Title"],
                            DistributorId = long.Parse(Request["Count"]),
                            UserId = long.Parse(Request["Count"]),
                            CategoryId = int.Parse(Request["CategoryId"]),
                            Type = (M.DistributorProgramme.EProgrammeType)int.Parse(Request["Type"]),
                            State = (Pd.ProductState)int.Parse(Request["State"]),
                            Count = int.Parse(Request["Count"]),
                            Province = int.Parse(Request["Province"]),
                            City = int.Parse(Request["City"]),
                            County = int.Parse(Request["County"]),
                            CreateDate = DateTime.Now
                        };
                        SetResult(brand.Insert(DataSource), () =>
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
                        M.DistributorProgramme brand = new M.DistributorProgramme()
                        {
                            Id = long.Parse(Request["Id"]),
                            Title = Request["Title"],
                            DistributorId = long.Parse(Request["Count"]),
                            UserId = long.Parse(Request["Count"]),
                            CategoryId = int.Parse(Request["CategoryId"]),
                            Type = (M.DistributorProgramme.EProgrammeType)int.Parse(Request["Type"]),
                            State = (Pd.ProductState)int.Parse(Request["State"]),
                            Count = int.Parse(Request["Count"]),
                            Province = int.Parse(Request["Province"]),
                            City = int.Parse(Request["City"]),
                            County = int.Parse(Request["County"]),
                        };
                        SetResult(brand.Update(DataSource), () =>
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
                        M.DistributorProgramme brand = new M.DistributorProgramme()
                        {
                            Id = long.Parse(Request["Id"])
                        };
                        SetResult(brand.Delete(DataSource), () =>
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
    }
}
