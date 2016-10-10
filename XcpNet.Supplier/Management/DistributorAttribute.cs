using System;
using Cnaws.Web;
using Cnaws.Data;
using M = XcpNet.Supplier.Modules.Modules;
using Cnaws.Management;
using System.Collections.Generic;

namespace XcpNet.Supplier.Management
{
    public sealed class DistributorAttribute : ManagementController
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
                if (CheckPost("distributorattribute", () =>
                {
                    this["Id"] = id;
                    this["Parents"] = M.DistributorCategory.GetAllParentsById(DataSource, id);
                    this["AllCategory"] = M.DistributorCategory.GetAll(DataSource, -1);
                }))
                    NotFound();
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
        public void List(int categoryId = -1, int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                    SetResult(M.DistributorAttribute.GetPage(DataSource, categoryId, page, 10));
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
                        M.DistributorAttribute attr = new M.DistributorAttribute()
                        {
                            Name = Request["Name"],
                            CategoryId = int.Parse(Request["CategoryId"]),
                            SortNum = int.Parse(Request["SortNum"])
                        };
                        SetResult(attr.Insert(DataSource), () =>
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
                        M.DistributorAttribute attr = new M.DistributorAttribute()
                        {
                            Id = int.Parse(Request["Id"]),
                            Name = Request["Name"],
                            SortNum = int.Parse(Request["SortNum"])
                        };
                        SetResult(attr.Update(DataSource), () =>
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
                        M.DistributorAttribute attr = new M.DistributorAttribute()
                        {
                            Id = int.Parse(Request["Id"])
                        };
                        SetResult(attr.Delete(DataSource), () =>
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
