using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using Cnaws.Passport.Controllers;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using S = XcpNet.Supplier.Modules.Modules;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws.ExtensionMethods;
using Cnaws.Templates;
using Cnaws;
using XcpNet.AfterSales.Modules;

namespace XcpNet.Admin.Management
{
    public sealed class DistributorEx : ManagementController
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
                    if (CheckPost("distributorex"))
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
                            string users = Request.Form["Users"];
                            string[] names = !string.IsNullOrEmpty(users) ? Request.Form["Users"].Split(',') : null;
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
                            }

                            DbWhereQueue dw = null;
                            if (mobiles.Count > 0)
                            {
                                if (dw == null)
                                    dw = new DbWhere<M.MemberInfo>("Mobile", mobiles, DbWhereType.In);
                                else
                                    dw |= new DbWhere<M.MemberInfo>("Mobile", mobiles, DbWhereType.In);
                            }
                            if (emails.Count > 0)
                            {
                                if (dw == null)
                                    dw = new DbWhere<M.MemberInfo>("Email", emails, DbWhereType.In);
                                else
                                    dw |= new DbWhere<M.MemberInfo>("Email", emails, DbWhereType.In);
                            }
                            if (newnames.Count > 0)
                            {
                                if (dw == null)
                                    dw = new DbWhere<M.MemberInfo>("Name", newnames, DbWhereType.In);
                                else
                                    dw |= new DbWhere<M.MemberInfo>("Name", newnames, DbWhereType.In);
                            }

                            //string state = Request.Form["State"];
                            //if (!string.IsNullOrEmpty(state))
                            //{
                            //    if (dw == null)
                            //        dw = new DbWhere<P.Distributor>("State", Enum.Parse(TType<P.DistributorState>.Type, state));
                            //    else
                            //        dw |= new DbWhere<P.Distributor>("State", Enum.Parse(TType<P.DistributorState>.Type, state));
                            //}

                            this["UserList"] = dw != null ? Db<M.MemberInfo>.Query(DataSource)
                                .Select(new DbSelect<M.MemberInfo>(), new DbSelect<P.Distributor>())
                                .LeftJoin(new DbColumn<M.MemberInfo>("Id"), new DbColumn<P.Distributor>("UserId"))
                                .Where(dw)
                                .ToList<DataJoin<M.MemberInfo, P.Distributor>>() : Db<M.MemberInfo>.Query(DataSource)
                                .Select(new DbSelect<M.MemberInfo>(), new DbSelect<P.Distributor>())
                                .InnerJoin(new DbColumn<M.MemberInfo>("Id"), new DbColumn<P.Distributor>("UserId"))
                                .ToList<DataJoin<M.MemberInfo, P.Distributor>>();
                        }
                        catch (Exception)
                        {
                            this["UserList"] = new List<DataJoin<M.MemberInfo, P.Distributor>>();
                        }
                        RenderTemplate("distributorex.html");
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }

        public void ChangeState()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        try
                        {
                            P.Distributor value = DbTable.Load<P.Distributor>(Request.Form);
                            value.State = P.DistributorState.Approved;
                            SetResult(value.Update(DataSource, ColumnMode.Include, "State"));
                        }
                        catch (Exception)
                        {
                            SetResult(false);
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }
        public void Submit()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        try
                        {
                            P.Distributor value = DbTable.Load<P.Distributor>(Request.Form);
                            value.ParentId = 0;
                            value.Level = 0;
                            value.State = P.DistributorState.Approved;
                            value.Money = 0;
                            SetResult(value.Insert(DataSource));
                        }
                        catch (Exception)
                        {
                            SetResult(false);
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }

        public void Money()
        {
            if (CheckAjax())
            {
                if (CheckRight())
                {
                    if (IsPost)
                    {
                        try
                        {
                            P.Distributor value = DbTable.Load<P.Distributor>(Request.Form);
                            DataSource.Begin();
                            try
                            {
                                string tagetid = P.ProductOrder.NewId(DateTime.Now, value.UserId);
                                if ((new S.DistributorMoneyRecord()
                                {
                                    MemberId = value.UserId,
                                    Title = "充值",
                                    Type = S.DistributorMoneyRecord.RechargeType,
                                    TargetId =tagetid ,
                                    Value = value.Money,
                                    CreationDate = DateTime.Now
                                }).Insert(DataSource) != DataStatus.Success)
                                    throw new Exception();
                                if (M.MemberInfo.ModifyMoney(DataSource, value.UserId, value.Money, "充值",0,tagetid) != DataStatus.Success)
                                    throw new DataException("余额不足");
                                //if (P.Distributor.ModifyMoney(DataSource, value.UserId, value.Money) != DataStatus.Success)
                                //    throw new Exception();
                                DataSource.Commit();
                                SetResult(true);
                            }
                            catch (Exception)
                            {
                                DataSource.Rollback();
                                throw;
                            }
                        }
                        catch (Exception)
                        {
                            SetResult(false);
                        }
                    }
                    else
                    {
                        NotFound();
                    }
                }
            }
        }

        /// <summary>
        /// 获取最大可平账金额
        /// </summary>
        [HttpPost]
        public void GetArrivalMoney(long userId)
        {
            try
            {
                if (userId <= 0)
                    throw new ArgumentException("参数有误");

                Money maxDrawMoney = ProfitRecord.GetArrivalMoney(DataSource, userId);
                SetResult(maxDrawMoney);
            }
            catch (Exception e)
            {
                SetResult(false, e.Message);
            }
        }
        /// <summary>
        /// 平账
        /// </summary>
        [HttpPost]
        public void PingZhang()
        {
            DataSource.Begin();
            try
            {
                M.MemberDrawOrder order = DbTable.Load<M.MemberDrawOrder>(Request.Form);
                if (order.DrawMoney <= 0 || order.UserId <= 0)
                    throw new ArgumentException("参数有误无效");
                order.CreateTime = DateTime.Now;
                order.OrderState = M.DrawOrderStatus.TradeSuccess;
                order.OrderId = P.ProductOrder.NewId(DateTime.Now, order.UserId);
                order.TradeSuccessDateTime = DateTime.Now;
                order.ProcessingDateTime = DateTime.Now;

                if (order.Insert(DataSource) != DataStatus.Success)
                    throw new Exception("创建平账记录失败");

                ProfitRecord precord = new ProfitRecord
                {
                    CreateDate = DateTime.Now,
                    OrderId = order.OrderId,
                    ProfitMoney = -order.DrawMoney,
                    ProfitState = ProfitRecord.EProfitState.Arrival,
                    ProfitType = ProfitRecord.EProfitType.OutOfAccount,
                    Title = $"平账{order.DrawMoney.ToString("c2")}",
                    TotalMoney = order.DrawMoney,
                    UserId = order.UserId,
                    SourceUserId = order.UserId
                };

                if (precord.Insert(DataSource) != DataStatus.Success)
                    throw new Exception("创建收溢记录失败");

                SetResult(true);
                DataSource.Commit();
            }
            catch (Exception e)
            {
                SetResult(false, e.Message);
                DataSource.Rollback();
            }
        }
    }
}
