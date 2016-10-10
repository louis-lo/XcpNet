using System;
using System.Collections.Generic;
using M = Cnaws.Passport.Modules;
using Cnaws.Data;
using Cnaws.Web;
using Cnaws.Data.Query;
using Pd = Cnaws.Product.Modules;
using Cnaws;
using A = XcpNet.AfterSales.Modules;


namespace XcpNet.Api.Controllers
{
    public class CommExtract : CommonControllers
    {
        public static string ClassName = "[type]Extract";
        protected override void OnInitController()
        {
            NotFound();
        }

        public void GetBank()
        {
            try
            {
                string mark;
                if (CheckMark(out mark))
                {
                    SetResult(M.MemberBankInfo.GetAll(DataSource));
                }
            }
            catch (Exception)
            { }
        }
#if (DEBUG)
        public static void GetBankHelper()
        {
            CheckMarkHelper(ClassName, "GetBank", "获取银行列表")
                .AddResult(true, typeof(IList<M.MemberBankInfo>), "返回结果");
        }
#endif

        public void GetUserBank()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    SetResult(M.MemberBank.GetBanksByUserId(DataSource, member.Id));
                }
            }
            catch (Exception)
            { }
        }
#if (DEBUG)
        public static void GetUserBankHelper()
        {
            CheckMemberHelper(ClassName, "GetUserBank", "获取用户绑定的银行列表")
                .AddResult(true, typeof(IList<M.MemberBank>), "返回结果");
        }
#endif
        public void DelUserBank()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    int id = int.Parse(Request["Id"]);
                    if (Db<M.MemberBank>.Query(DataSource).Delete().Where(new DbWhere("Id", id) & new DbWhere("UserId", member.Id)).Execute() > 0)
                        SetResult(true);
                    else
                        SetResult(ApiUtility.DEL_FAIL);
                }
            }
            catch (Exception)
            { }
        }
#if (DEBUG)
        public static void DelUserBankHelper()
        {
            CheckMemberHelper(ClassName, "DelUserBank", "删除绑定的银行卡")
                .AddResult(ApiUtility.DEL_FAIL, "删除失败")
                .AddResult(true, typeof(DataStatus), "返回结果");
        }
#endif
        /// <summary>
        /// 绑定银行卡
        /// </summary>
        public void BindCard()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    M.MemberBank bank = DbTable.Load<M.MemberBank>(Request.Form);
                    bank.BankCard = bank.BankCard.Replace(" ", "").Trim();
                    bank.UserId = member.Id;
                    bank.IsVerify = false;

                    M.MemberBank defaultBank = M.MemberBank.GetUserDefaultBank(DataSource, member.Id);
                    if (defaultBank != null)
                    {
                        defaultBank.IsDefault = false;
                        defaultBank.Update(DataSource);
                    }
                    else
                    {
                        bank.IsDefault = true;
                    }
                    bank.IsVerify = true;
                    if (bank.Id != 0)
                        SetResult((int)bank.Update(DataSource));
                    else
                        SetResult((int)bank.Insert(DataSource));
                }
                catch (Exception)
                {
                    SetResult(ApiUtility.PROGRAM_ERROR);
                }
            }
        }
#if (DEBUG)
        public static void BindCardHelper()
        {
            CheckMemberHelper(ClassName, "BindCard", "绑定银行卡")
                .AddArgument("BankId", typeof(int), "银行Id")
                .AddArgument("AccountName", typeof(string), "开户名")
                .AddArgument("BankCard", typeof(string), "银行卡号")
                .AddArgument("Province", typeof(int), "开户省Id")
                .AddArgument("City", typeof(int), "开户市Id")
                .AddArgument("Region", typeof(int), "开户区Id")
                .AddArgument("BankZone", typeof(string), "开户网点")
                .AddArgument("IsDefault", typeof(bool), "是否设置为默认银行")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "返回结果");
        }
#endif
        /// <summary>
        /// 申请提现
        /// </summary>
        public void ApplyExtract()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                int DrawType = int.Parse(Request["WithdrawType"]);//0提现到余额,1提现到银行卡
                if (DrawType == 0)
                {
                    DataSource.Begin();
                    try
                    {
                        Money money;
                        Money.TryParse(Request["Money"], out money);
                        if (money <= 0)
                        {
                            SetResult(false, "参数有误");
                            SetResult(ApiUtility.PRODUCT_ERROR);
                        }

                        M.MemberDrawOrder order = new M.MemberDrawOrder
                        {
                            UserId = member.Id,
                            DrawMoney = money,
                            CreateTime = DateTime.Now,
                            OrderId = Pd.ProductOrder.NewId(DateTime.Now, member.Id),
                            OrderState = M.DrawOrderStatus.TradeSuccess,
                            ProcessingDateTime = DateTime.Now,
                            TradeSuccessDateTime = DateTime.Now
                        };
                        if (order.Insert(DataSource) == DataStatus.Success)
                        {
                            string title = $"{User.Identity.Name} 提现{money.ToString("c2")} 到余额";
                            if (A.ProfitRecord.WithDrawByMoney(DataSource, member.Id, money, title, order.OrderId,A.ProfitRecord.EProfitState.Arrival) == DataStatus.Success)
                            {
                                if (M.MemberInfo.ModifyMoney(DataSource, member.Id, money, title) == DataStatus.Success)
                                {
                                    SetResult(true);
                                    DataSource.Commit();
                                }
                                else
                                {
                                    SetResult(ApiUtility.UPDATE_FAIL);
                                    throw new AggregateException();
                                }
                            }
                            else
                            {
                                SetResult(ApiUtility.INSERT_FAIL);
                                throw new AggregateException();
                            }
                        }
                        else
                        {
                            SetResult(ApiUtility.INSERT_FAIL);
                            throw new AggregateException();
                        }
                    }
                    catch (AggregateException)
                    {
                        DataSource.Rollback();
                        return;
                    }
                    catch (Exception)
                    {
                        DataSource.Rollback();
                        SetResult(ApiUtility.PROGRAM_ERROR);
                        return;
                    }
                }
                else if (DrawType == 1)
                {
                    DataSource.Begin();
                    try
                    {
                        Money money;
                        Money.TryParse(Request["Money"], out money);
                        if (money <= 0)
                        {
                            SetResult(ApiUtility.PARAMETER_ERROR);
                            throw new AggregateException();
                        }
                        int bankId = 0;
                        int.TryParse(Request["BankId"], out bankId);
                        if (bankId <= 0)
                        {
                            SetResult(ApiUtility.PARAMETER_ERROR);
                            throw new AggregateException();
                        }
                        M.MemberDrawOrder order = new M.MemberDrawOrder();
                        M.MemberBankInfo bankInfo = M.MemberBankInfo.GetById(DataSource, bankId);
                        M.MemberBank bank = M.MemberBank.GetById(DataSource, bankId, member.Id);
                        if (bank == null || bank.Id <= 0)
                        {
                            SetResult(ApiUtility.BANK_EMPTY);
                            throw new AggregateException();
                        }
                        order.DrawMoney = money;
                        order.UserId = member.Id;
                        order.AccountName = bank.AccountName;
                        order.BankCard = bank.BankCard;
                        order.BankName = bankInfo.BankName;
                        order.BankZone = bank.BankZone;
                        order.CreateTime = DateTime.Now;
                        order.OrderId = Pd.ProductOrder.NewId(DateTime.Now, member.Id);

                        if (DataStatus.Success == order.Insert(DataSource))
                        {
                            string title = $"提现{order.DrawMoney.ToString("c2")}";
                            if (A.ProfitRecord.WithDrawByMoney(DataSource, member.Id, money, title, order.OrderId, A.ProfitRecord.EProfitState.NoArrival) == DataStatus.Success)
                            {
                                SetResult(true);
                                DataSource.Commit();
                            }
                            else
                            {
                                SetResult(ApiUtility.INSERT_FAIL);
                                throw new AggregateException();
                            }
                        }
                        else
                        {
                            SetResult(ApiUtility.INSERT_FAIL);
                            throw new AggregateException();
                        }
                    }
                    catch (AggregateException)
                    {
                        DataSource.Rollback();
                        return;
                    }
                    catch (Exception)
                    {
                        SetResult(ApiUtility.PROGRAM_ERROR);
                        DataSource.Rollback();
                        return;
                    }
                }
            }
        }
#if (DEBUG)
        public static void ApplyExtractHelper()
        {
            CheckMemberHelper(ClassName, "ApplyExtract", "申请提现")
                .AddArgument("WithdrawType", typeof(int), "提现类型,0:提现到余额,1:提现到银行卡")
                .AddArgument("Money", typeof(double), "提现的金额")
                .AddArgument("BankId", typeof(int), "选择的银行卡Id,提现到银行卡时必传")
                .AddResult(ApiUtility.PRODUCT_ERROR, "参数错误")
                .AddResult(ApiUtility.INSERT_FAIL, "插入提现记录失败")
                .AddResult(ApiUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "返回结果");
        }
#endif
        /// <summary>
        /// 获取提现列表
        /// </summary>
        public void GetWithDrawList()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                int size, page;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                if (!int.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                SetResult(M.MemberDrawOrder.GetListByUserId(DataSource, member.Id, page, size, DateTime.MinValue, DateTime.MinValue, M.DrawOrderStatus.None));
            }
        }
#if (DEBUG)
        public static void GetWithDrawListHelper()
        {
            CheckMemberHelper(ClassName, "GetWithDrawList", "获取提现列表")
                .AddArgument("page", typeof(int), "页码")
                .AddArgument("size", typeof(int), "每页显示条数")
                .AddResult(true, typeof(SplitPageData<M.MemberDrawOrder>), "返回结果");
        }
#endif
    }
}
