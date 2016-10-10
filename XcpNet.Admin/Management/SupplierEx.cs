using System;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Management;
using Cnaws.Passport.Controllers;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using System.Collections.Generic;
using Cnaws.Data.Query;
using Cnaws.ExtensionMethods;
using XcpNet.AfterSales.Modules;
using Cnaws;

namespace XcpNet.Admin.Management
{
    public sealed class SupplierEx : ManagementController
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
                    if (CheckPost("supplierex"))
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
                                .Select(new DbSelect<M.MemberInfo>(), new DbSelect<P.Supplier>())
                                .LeftJoin(new DbColumn<M.MemberInfo>("Id"), new DbColumn<P.Supplier>("UserId"))
                                .Where(dw)
                                .ToList<DataJoin<M.MemberInfo, P.Supplier>>();
                        }
                        catch (Exception)
                        {
                            this["UserList"] = new List<DataJoin<M.MemberInfo, P.Supplier>>();
                        }
                        RenderTemplate("supplierex.html");
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
                            P.Supplier value = DbTable.Load<P.Supplier>(Request.Form);
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
                DataSource.Commit();
                SetResult(true);                
            }
            catch (Exception e)
            {
                DataSource.Rollback();
                SetResult(false, e.Message);                
            }
        }
    }
}
