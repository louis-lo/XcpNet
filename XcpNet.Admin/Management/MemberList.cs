using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using Cnaws.Passport.Controllers;
using Pd = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws.ExtensionMethods;
using Cnaws;
using Cnaws.Web.Templates;

namespace XcpNet.Admin.Management
{
    public sealed class MemberList : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        private Dictionary<int, string> _menus;

        public MemberList()
        {
            _menus = new Dictionary<int, string>();
            _menus.Add(0, "所有用户");
            _menus.Add(1, "未审核用户");
            _menus.Add(2, "已锁定用户");
        }

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Admin"; }
        }

        public void Index(int type = 0)
        {
            if (CheckRight())
            {
                if (CheckPost("memberlist", () =>
                {
                    this["Type"] = type;
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

        public void List(int type, int page = 1, string search = "")
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    DbWhereQueue dw = null;
                    List<long> mobiles = new List<long>();
                    List<string> emails = new List<string>();
                    List<string> newnames = new List<string>();
                    string[] names = string.IsNullOrEmpty(search) ? null : search.Split(' ');
                    if (names != null && names.Length > 0)
                    {
                        long mobile;
                        foreach (string name in names)
                        {
                            if (long.TryParse(name, out mobile))
                                mobiles.Add(mobile);
                            else if (Utility.EmailRegularExpression.IsMatch(name))
                                emails.Add(name);
                            else
                                newnames.Add(name);
                        }

                        if (mobiles.Count > 0)
                            dw |= new DbWhere<M.MemberInfo>("Mobile", mobiles.ToArray(), DbWhereType.In);
                        if (emails.Count > 0)
                            dw |= new DbWhere<M.MemberInfo>("Email", emails.ToArray(), DbWhereType.In);
                        if (newnames.Count > 0)
                            dw |= new DbWhere<M.MemberInfo>("Name", newnames.ToArray(), DbWhereType.In);
                    }

                    switch (type)
                    {
                        case 1:
                            dw &= new DbWhere("Approved", false);
                            break;
                        case 2:
                            dw &= new DbWhere("Locked", true);
                            break;
                    }

                    long count;
                    IList<M.MemberInfo> list = Db<M.MemberInfo>.Query(DataSource)
                        .Select()
                        .Where(dw)
                        .OrderBy(new DbOrderBy("Id"))
                        .ToList<M.MemberInfo>(10, page, out count);

                    SetResult(new SplitPageData<M.MemberInfo>(page, 10, list, count, 11));
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
                        M.MemberInfo value = new M.MemberInfo()
                        {
                            Id = long.Parse(Request.Form["Id"]),
                            Password = Request.Form["Password"],
                            Approved = Types.GetBooleanFromString(Request.Form["Approved"]),
                            Locked = Types.GetBooleanFromString(Request.Form["Locked"])
                        };
                        List<DataColumn> columns = new List<DataColumn>();
                        M.MemberInfo old = M.MemberInfo.GetById(DataSource, value.Id);
                        if (value.Password != old.Password)
                            columns.Add("Password");
                        if (value.Approved != old.Approved)
                            columns.Add("Approved");
                        if (value.Locked != old.Locked)
                        {
                            value.LockNum = 0;
                            columns.Add("Locked");
                            columns.Add("LockNum");
                        }
                        if (columns.Count > 0)
                        {
                            SetResult(value.Update(DataSource, ColumnMode.Include, columns.ToArray()), () =>
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
        public void Recharge()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        DataSource.Begin();
                        try
                        {
                            int type = 3;
                            long userId = long.Parse(Request.Form["Id"]);
                            Money money = Money.Parse(Request.Form["Money"]);
                            string title = Request.Form["Title"];
                            if (string.IsNullOrEmpty(title))
                                title = "系统充值";

                            if (M.MemberInfo.ModifyMoney(DataSource, userId, money, title, type) != DataStatus.Success)
                                throw new Exception();

                            DataSource.Commit();
                            SetResult(true);
                        }
                        catch
                        {
                            DataSource.Rollback();
                            SetResult(false);
                        }
                    }
                }
            }
        }
        //public void Approved()
        //{
        //    if (CheckAjax())
        //    {
        //        if (IsPost)
        //        {
        //            long UserId = long.Parse(Request.Form["Id"]);
        //            SetResult(new M.Member() { Id = UserId, Approved = true }.Update(DataSource, ColumnMode.Include, "Approved"));
        //        }
        //        else
        //        {
        //            NotFound();
        //        }
        //    }
        //}
        //public void Unlock()
        //{
        //    if (CheckAjax())
        //    {
        //        if (IsPost)
        //        {
        //            long UserId = long.Parse(Request.Form["Id"]);
        //            SetResult(new M.Member() { Id = UserId, Locked = false, LockNum = 0 }.Update(DataSource, ColumnMode.Include, "Locked", "LockNum"));
        //        }
        //        else
        //        {
        //            NotFound();
        //        }
        //    }
        //}
        //public void ResetPwd()
        //{
        //    if (CheckAjax())
        //    {
        //        if (IsPost)
        //        {
        //            long UserId = long.Parse(Request.Form["Id"]);
        //            SetResult(new M.Member() { Id = UserId, Password = Request.Form["Password"] }.Update(DataSource, ColumnMode.Include, "Password"));
        //        }
        //        else
        //        {
        //            NotFound();
        //        }
        //    }
        //}
    }
}
