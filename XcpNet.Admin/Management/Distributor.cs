using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using System.Collections.Generic;
using Cnaws.Web.Templates;
using M = Cnaws.Product.Modules;
using P = Cnaws.Passport.Modules;
using Cnaws.Data.Query;

namespace XcpNet.Admin.Management
{
    public sealed class Distributor : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        private Dictionary<int, string> _menus;
        private Dictionary<int, string> _states;

        public Distributor()
        {
            _menus = new Dictionary<int, string>();
            _menus.Add(-1, "所有加盟商");
            _menus.Add(0, "省级加盟商");
            _menus.Add(1, "县级加盟商");
            _menus.Add(2, "镇级旗舰店");
            _menus.Add(3, "镇级中心店");
            _menus.Add(4, "村级加盟店");
            _menus.Add(100, "会员加盟店");

            _states = new Dictionary<int, string>();
            _states.Add(-2, "所有状态");
            _states.Add(-1, "已删除");
            _states.Add(0, "未审核");
            _states.Add(1, "已审核");
            _states.Add(2, "审核失败");
        }

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Admin"; }
        }

        public void Index(int level = -1, int state = -2)
        {
            if (CheckRight())
            {
                if (CheckPost("distributor", () =>
                {
                    this["Level"] = level;
                    this["State"] = state;
                    this["Menus"] = _menus;
                    this["GetMenuName"] = new FuncHandler((args) =>
                    {
                        try { return _menus[(int)args[0]]; }
                        catch (Exception) { }
                        return _menus[-1];
                    });
                    this["States"] = _states;
                    this["GetStateName"] = new FuncHandler((args) =>
                    {
                        try { return _states[(int)args[0]]; }
                        catch (Exception) { }
                        return _states[-2];
                    });
                }))
                    NotFound();
            }
        }

        public void List(int level = -1, int state = -2, int page = 1)
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    long count;
                    DbWhereQueue where = null;
                    if (level > -1)
                        where &= new DbWhere<M.Distributor>("Level", level);
                    if (state > -2)
                        where &= new DbWhere<M.Distributor>("State", state);
                    IList<dynamic> list = Db<M.Distributor>.Query(DataSource)
                         .Select(
                            new DbSelectAs<M.Distributor>("UserId"),
                            new DbSelectAs<P.Member>("Name", "UserName"),
                            new DbSelectAs<P.Member>("Mobile", "UserMobile"),
                            new DbSelectAs<M.Distributor>("Signatories"),
                            new DbSelectAs<M.Distributor>("SignatoriesPhone"),
                            new DbSelectAs<M.Distributor>("Company"),
                            new DbSelectAs<M.Distributor>("Address"),
                            new DbSelectAs<M.Distributor>("Level"),
                            new DbSelectAs<M.Distributor>("State"),
                            new DbSelectAs<M.Distributor>("CreationDate"),

                            new DbSelect<M.Distributor>("CreationDate")
                         )
                         .InnerJoin(new DbColumn<M.Distributor>("UserId"), new DbColumn<P.Member>("Id"))
                         .Where(where)
                         .OrderBy(new DbOrderBy<M.Distributor>("CreationDate", DbOrderByType.Desc))
                         .ToList(10, page, out count);
                    SetResult(new SplitPageData<dynamic>(page, 10, list, count, 11));
                }
            }
        }

        public void Info(long id)
        {
            if (CheckAjax())
            {
                if (CheckPost("distributor", () =>
                {
                    this["Member"] = P.MemberInfo.GetById(DataSource, id);
                    this["Distributor"] = M.Distributor.GetById(DataSource, id);
                }))
                    NotFound();
            }
        }

        public void Approved()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    M.Distributor value = new M.Distributor()
                    {
                        UserId = long.Parse(Request.Form["Id"]),
                        State = M.DistributorState.Approved
                    };
                    SetResult(value.Update(DataSource, ColumnMode.Include, "State"), () =>
                    {
                        WritePostLog("APP");
                    });
                }
            }
        }

        public void Delete()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    M.Distributor value = new M.Distributor()
                    {
                        UserId = long.Parse(Request.Form["Id"]),
                        State = M.DistributorState.Deleted
                    };
                    SetResult(value.Update(DataSource, ColumnMode.Include, "State"), () =>
                    {
                        WritePostLog("DEL");
                    });
                }
            }
        }
    }
}
