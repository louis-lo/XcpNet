using System;
using System.Web;
using System.Collections.Generic;
using Cnaws.Data;
using Cnaws.Management;
using Cnaws.Web;
using Cnaws;
using M = XcpNet.Ad.Modules;

namespace XcpNet.Ad.Management
{
    public sealed class AdType : ManagementController
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

        public void Index(int id = -1)
        {
            if (CheckRight())
            {
                if (CheckPost("adtype", () =>
                {
                    this["Id"] = id;
                }))
                    NotFound();
            }
        }

        public void List(int type, int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                    SetResult(M.AdType.GetPage(DataSource, (M.AdType.EAdType)type, page, 10));
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
                        M.AdType ad = DbTable.Load<M.AdType>(Request.Form);
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
                        M.AdType ad = DbTable.Load<M.AdType>(Request.Form);
                        SetResult(ad.Update(DataSource, ColumnMode.Include, "Name", "MaxSum", "IsEnable"), () =>
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

        public void Update()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        M.AdType ad = DbTable.Load<M.AdType>(Request.Form);
                        SetResult(ad.Update(DataSource), () =>
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

        //public void Del()
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckRight())
        //        {
        //            if (IsPost)
        //            {
        //                SetResult((new M.AdType() { Id = int.Parse(Request.Form["Id"]) }).Delete(DataSource), () =>
        //                 {
        //                     WritePostLog("DEL");
        //                 });
        //            }
        //            else
        //            {
        //                NotFound();
        //            }
        //        }
        //    }
        //}
        //public void Get(long id)
        //{
        //    if (CheckAjax())
        //    {
        //        if (CheckAjax())
        //        {
        //            if (CheckRight())
        //                SetResult(M.AdType.GetById(DataSource, id));
        //        }
        //    }
        //}
    }
}
