using System;
using System.Collections.Generic;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using M = XcpNet.Supplier.Modules.Modules;
using Cnaws;

namespace XcpNet.Supplier.Management
{
    public sealed class IndutryCategory : ManagementController
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
                if (CheckPost("indutrycategory", () =>
                {
                    this["Id"] = id;
                    this["Parents"] = M.IndutryCategory.GetAllParentsById(DataSource, id);
                    this["AllCategory"] = M.IndutryCategory.GetAll(DataSource, -1);
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
        //            IList<M.IndutryCategory> list = M.IndutryCategory.GetAll(DataSource, -1);
        //            if (IsPost)
        //                SetResult(list);
        //            else
        //                SetJavascript("allCategory", list);
        //        }
        //    }
        //}
        //public void GetAllCategory()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            IList<M.IndutryCategory> list = M.IndutryCategory.GetAll(DataSource, -1);
        //            if (IsPost)
        //                SetResult(list);
        //            else
        //                SetJavascript("allIndutryCategory", list);
        //        }
        //    }
        //}
        public void List(int parentId, int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                    SetResult(M.IndutryCategory.GetPage(DataSource, parentId, page, 10));
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
                        M.IndutryCategory category = new M.IndutryCategory()
                        {
                            Name = Request["Name"],
                            Image = Request["Image"],
                            ParentId = int.Parse(Request["ParentId"]),
                            ShowLogo = Types.GetBooleanFromString(Request["ShowLogo"]),
                            SortNum = int.Parse(Request["SortNum"])
                        };
                        SetResult(category.Insert(DataSource), () =>
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
                        M.IndutryCategory category = new M.IndutryCategory()
                        {
                            Id = int.Parse(Request["Id"]),
                            Name = Request["Name"],
                            Image = Request["Image"],
                            ShowLogo = Types.GetBooleanFromString(Request["ShowLogo"]),
                            SortNum = int.Parse(Request["SortNum"])
                        };
                        SetResult(category.Update(DataSource), () =>
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
                        M.IndutryCategory category = new M.IndutryCategory()
                        {
                            Id = int.Parse(Request["Id"])
                        };
                        SetResult(category.Delete(DataSource), () =>
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
        //public void All(int parentId)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //            SetResult(M.IndutryCategory.GetAll(DataSource, parentId));
        //    }
        //}
    }
}
