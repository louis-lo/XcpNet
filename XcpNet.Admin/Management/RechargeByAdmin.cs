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

namespace XcpNet.Admin.Management
{
    public sealed class RechargeByAdmin : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);

        protected override Version Version
        {
            get { return VERSION; }
        }

        protected override string Namespace
        {
            get { return "XcpNet.Admin"; }
        }

        public void Index()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (CheckPost("rechargebyadmin"))
                        NotFound();
                }
            }
        }

        public void Search()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        try
                        {
                            List<long> mobiles = new List<long>();
                            List<string> emails = new List<string>();
                            List<string> newnames = new List<string>();
                            string[] names = Request.Form["Users"].Split(',');
                            if (names.Length < 1)
                                throw new Exception();
                            else
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
                            }
                            DbWhereQueue dw = null;
                            if (mobiles.Count > 0)
                            {
                                if (dw == null)
                                    dw = new DbWhere<M.MemberInfo>("Mobile", mobiles.ToArray(), DbWhereType.In);
                                else
                                    dw |= new DbWhere<M.MemberInfo>("Mobile", mobiles.ToArray(), DbWhereType.In);
                            }
                            if (emails.Count > 0)
                            {
                                if (dw == null)
                                    dw = new DbWhere<M.MemberInfo>("Email", emails.ToArray(), DbWhereType.In);
                                else
                                    dw |= new DbWhere<M.MemberInfo>("Email", emails.ToArray(), DbWhereType.In);
                            }
                            if (newnames.Count > 0)
                            {
                                if (dw == null)
                                    dw = new DbWhere<M.MemberInfo>("Name", newnames.ToArray(), DbWhereType.In);
                                else
                                    dw |= new DbWhere<M.MemberInfo>("Name", newnames.ToArray(), DbWhereType.In);
                            }


                            this["UserList"] = Db<M.MemberInfo>.Query(DataSource)
                                .Select(new DbSelect<M.MemberInfo>())
                                .Where(dw)
                                .ToList<M.MemberInfo>();
                        }
                        catch (Exception)
                        {
                            this["UserList"] = new List<M.MemberInfo>();
                        }
                        RenderTemplate("rechargebyadmin.html");
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
                            int RechargeType = int.Parse(Request["RechargeType"]);
                            long UserId = long.Parse(Request["rid"]);
                            Money money = Money.Parse(Request["Money"]);
                            string Title = "";
                            if (RechargeType == 3)
                            {
                                Title = "系统充值";
                                if (M.MemberInfo.ModifyMoney(DataSource, UserId, money, Title, 3) != DataStatus.Success)
                                {
                                    throw new Exception();
                                }
                            }
                            if (RechargeType == 4)
                            {
                                Title = "1元抢开奖";
                                long targetid = long.Parse(Request["targetId"]);
                                if (Pd.OneProductNumber.ModState(DataSource, targetid, Pd.OneProductNumberState.Finished, UserId) != DataStatus.Success)
                                {
                                    throw new Exception();
                                }
                                if (M.MemberInfo.ModifyMoney(DataSource, UserId, money, Title, 4, targetid.ToString()) != DataStatus.Success)
                                {
                                    throw new Exception();
                                }
                            }
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
    }
}
