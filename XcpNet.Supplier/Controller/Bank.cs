using System;
using System.Linq;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Web.Templates;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Data.Query;
using XcpNet.AfterSales.Modules;

namespace XcpNet.Supplier.Controllers
{
    public sealed class Bank : Common.CommoSupplierController
    {
        long UserId
        {
            get
            {
                return User.Identity.Id;
            }
        }
        /// <summary>
        /// 绑定银行卡
        /// </summary>
        [SupplierOrDistributor]
        public void BindCard()
        {
            this["BankList"] = M.MemberBankInfo.GetAll(DataSource);
            if (IsSupplier())
                Render("supplier/bank_bindcard.html");
            else
                Render("bank_bindcard.html");
        }
        /// <summary>
        /// 保存银行卡
        /// </summary>
        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void SubmitBindCard()
        {
            try
            {
                M.MemberBank bank = DbTable.Load<M.MemberBank>(Request.Form);
                bank.BankCard = bank.BankCard.Replace(" ", "").Trim();
                bank.UserId = UserId;
                bank.Province = int.Parse(Request.Form["area_provinces"]);
                bank.City = int.Parse(Request.Form["area_cities"]);
                bank.Region = int.Parse(Request.Form["area_counties"]);

                M.MemberBank defaultBank = M.MemberBank.GetUserDefaultBank(DataSource, UserId);
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
                SetResult(false);
            }
        }
        /// <summary>
        /// 删除用户绑定银行卡
        /// </summary>
        /// <param name="bankid"></param>
        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void DelBankCard()
        {
            long id = 0L;
            long.TryParse(Request["id"], out id);
            if (id > 0)
            {
                var result = (new M.MemberBank() { Id = id }).Delete(DataSource);
                SetResult(result);
            }
            else
            {
                SetResult(false, "参数无效！");
            }
        }
        /// <summary>
        /// 申请提现
        /// </summary>
        [Authorize]
        public void Drawmoney()
        {
            
            //所有可提现金额
            this["ArrivalMoney"] = ProfitRecord.GetArrivalMoney(DataSource, UserId);
            //已到账收益
            this["TotalMoney"] = ProfitRecord.GetAllHoustonMoney(DataSource,UserId);
            //提现冻结资金(已提现但未到账)提现审核中，转账中
            this["FreezeMoney"] = -ProfitRecord.GetFreezeMoney(DataSource, UserId);
            //累计提现
            this["WithdrawalMoney"] = -ProfitRecord.GetAllOutMoney(DataSource, UserId);
            //未到账收益
            this["NotaccountMoney"] = ProfitRecord.GetHoustonFreezeMoney(DataSource, UserId);
            this["CurrentUser"] = M.MemberInfo.GetById(DataSource, UserId);
            var banks = M.MemberBank.GetBanksByUserId(DataSource, UserId);
            this["Banks"] = banks;
            this["DefaultBank"] = banks.FirstOrDefault(b => b.A.IsDefault);
            this["GetBankText"] = new FuncHandler((args) =>
            {
                string bankCard = args[2].ToString();
                return $"{args[0]}  {args[1]} {subBankCard(bankCard)}";
            });
            if (IsSupplier())
                Render("supplier/bank_drawmoney.html");
            else
                Render("bank_drawmoney.html");
        }

        public void DrawmoneytoCard()
        {
            //所有可提现金额
            this["ArrivalMoney"] = -ProfitRecord.GetArrivalMoney(DataSource, UserId);
            this["TotalMoney"] = ProfitRecord.GetAllHoustonMoney(DataSource, UserId);
            var banks = M.MemberBank.GetBanksByUserId(DataSource, UserId);
            this["Banks"] = banks;
            this["DefaultBank"] = banks.FirstOrDefault(b => b.A.IsDefault);
            this["GetBankText"] = new FuncHandler((args) =>
            {
                string bankCard = args[2].ToString();
                return $"{args[0]}  {args[1]} {subBankCard(bankCard)}";
            });
            Render("bank_tocard.html");
        }
        public void DrawmoneytoBalance()
        {
            //所有可提现金额
            this["ArrivalMoney"] = -ProfitRecord.GetArrivalMoney(DataSource, UserId);
            this["TotalMoney"] = ProfitRecord.GetAllHoustonMoney(DataSource, UserId);
            var banks = M.MemberBank.GetBanksByUserId(DataSource, UserId);
            this["Banks"] = banks;
            this["DefaultBank"] = banks.FirstOrDefault(b => b.A.IsDefault);
            this["GetBankText"] = new FuncHandler((args) =>
            {
                string bankCard = args[2].ToString();
                return $"{args[0]}  {args[1]} {subBankCard(bankCard)}";
            });
            this["CurrentUser"] = M.MemberInfo.GetById(DataSource, UserId);
            if (IsSupplier())
                Render("supplier/bank_tobalance.html");
            else
                Render("bank_tobalance.html");
        }
        /// <summary>
        /// 保存提现订单,提现到银行卡
        /// </summary>
        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void SubmitDrawmoney()
        {
            DataSource.Begin();
            try
            {
                M.MemberDrawOrder order = DbTable.Load<M.MemberDrawOrder>(Request.Form);
                int bankId = int.Parse(Request["BankId"]);               
                M.MemberBank bank = M.MemberBank.GetById(DataSource, bankId, UserId);
                M.MemberBankInfo bankInfo = M.MemberBankInfo.GetById(DataSource, bank.BankId);                
                order.UserId = UserId;
                order.AccountName = bank.AccountName;
                order.BankCard = bank.BankCard;
                order.BankName = bankInfo.BankName;
                order.BankZone = bank.BankZone;
                order.CreateTime = DateTime.Now;
                order.OrderId = P.ProductOrder.NewId(DateTime.Now, UserId);

                if (DataStatus.Success == order.Insert(DataSource))
                {
                    ProfitRecord precord = new ProfitRecord
                    {
                        CreateDate = DateTime.Now,
                        OrderId = order.OrderId,
                        ProfitMoney = -order.DrawMoney,
                        ProfitState = ProfitRecord.EProfitState.NoArrival,
                        ProfitType = ProfitRecord.EProfitType.OutOfAccount,
                        Title = $"提现{order.DrawMoney.ToString("c2")}",
                        TotalMoney = order.DrawMoney,
                        UserId = UserId,
                        SourceUserId = UserId
                    };

                    if (precord.Insert(DataSource) == DataStatus.Success)
                    {
                        SetResult(true);
                        DataSource.Commit();
                    }
                    else
                    {
                        SetResult(false);
                        DataSource.Rollback();
                    }
                }
                else
                {
                    SetResult(false);
                    DataSource.Rollback();
                }
            }
            catch (AggregateException)
            {
                SetResult(false);
                DataSource.Rollback();
            }
            catch (Exception)
            {
                SetResult(false);
                DataSource.Rollback();
            }
        }
        /// <summary>
        /// 提现到余额
        /// </summary>
        [HttpAjax]
        [HttpPost]
        [Authorize]
        public void SubmitDrawmoneyToBalance()
        {
            DataSource.Begin();
            try
            {
                Cnaws.Money money;
                Cnaws.Money.TryParse(Request["money"], out money);
                if (money <= 0)
                {
                    throw new AggregateException("参数有误");
                }

                M.MemberDrawOrder order = new M.MemberDrawOrder
                {
                    UserId = UserId,
                    DrawMoney = money,
                    CreateTime = DateTime.Now,
                    OrderId = P.ProductOrder.NewId(DateTime.Now, UserId),
                    OrderState = M.DrawOrderStatus.TradeSuccess,
                    ProcessingDateTime = DateTime.Now,
                    TradeSuccessDateTime = DateTime.Now
                };
                if (order.Insert(DataSource) == DataStatus.Success)
                {
                    string title = $"{User.Identity.Name} 提现{money.ToString("c2")} 到余额";
                    ProfitRecord precord = new ProfitRecord
                    {
                        CreateDate = DateTime.Now,
                        OrderId = order.OrderId,
                        ProfitMoney = -money,
                        ProfitState = ProfitRecord.EProfitState.Arrival,
                        ProfitType = ProfitRecord.EProfitType.OutOfAccount,
                        Title = title,
                        TotalMoney = money,
                        UserId = UserId,
                        SourceUserId = UserId
                    };
                    if (precord.Insert(DataSource) == DataStatus.Success)
                    {
                        if (M.MemberInfo.ModifyMoney(DataSource, UserId, money, title) == DataStatus.Success)
                        {
                            SetResult(true);
                            DataSource.Commit();
                        }
                        else
                        {
                            SetResult(false);
                            DataSource.Rollback();
                        }
                    }
                    else
                    {
                        SetResult(false);
                        DataSource.Rollback();
                    }
                }
                else
                {
                    SetResult(false);
                    DataSource.Rollback();
                }
            }
            catch (AggregateException)
            {
                SetResult(false);
                DataSource.Rollback();
            }
        }
        /// <summary>
        /// 提现记录
        /// </summary>
        [Authorize]
        public void DrawmoneyHistory(int pageIndex)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            DateTime stDate, endDate;
            M.DrawOrderStatus state;
            if (string.IsNullOrEmpty(Request["State"]))
                state = M.DrawOrderStatus.None;
            else
                Enum.TryParse(Request["State"], out state);

            DateTime.TryParse(System.Web.HttpUtility.UrlDecode(Request["startDate"]), out stDate);
            DateTime.TryParse(System.Web.HttpUtility.UrlDecode(Request["endDate"]), out endDate);
            #region 
            var banks = M.MemberBank.GetBanksByUserId(DataSource, UserId);
            this["Banks"] = banks;
            this["DefaultBank"] = banks.FirstOrDefault(b => b.A.IsDefault);
            #endregion
            this["OrderList"] = M.MemberDrawOrder.GetPagesByUserId(DataSource, UserId, pageIndex, 10, stDate, endDate, state);
            this["StateList"] = M.MemberDrawOrder.GetOrderStateList();
            this["GetUrl"] = new FuncHandler((args) =>
            {
                return GetUrl(string.Concat("/bank/DrawmoneyHistory/", args[0]));
            });

            this["SubBankCard"] = new FuncHandler((args) =>
            {
                return subBankCard(args[0]?.ToString());
            });
            this["HttpMethod"] = Request.HttpMethod;
            if (IsSupplier())
                Render("supplier/bank_drawHistory.html");
            else
                Render("bank_drawHistory.html");
        }

        public void DrawDetail(long orderId)
        {
            #region 
            var banks = M.MemberBank.GetBanksByUserId(DataSource, UserId);
            this["Banks"] = banks;
            this["DefaultBank"] = banks.FirstOrDefault(b => b.A.IsDefault);
            #endregion
            M.MemberDrawOrder order = M.MemberDrawOrder.GetById(DataSource, orderId);
            this["DrawOrder"] = order;
            this["SubBankCard"] = $"{subBankCard(order.BankCard)} {order.AccountName}";
            this["CredentialImgs"] = string.IsNullOrEmpty(order.CredentialImage) ? new string[0] : order.CredentialImage.Split('|');
            if (IsSupplier())
                Render("supplier/bank_details.html");
            else
                Render("bank_details.html");
        }

        string subBankCard(string bankCard)
        {
            if (string.IsNullOrEmpty(bankCard) || bankCard.Length < 10) return "银行卡无效";

            string start = bankCard.Substring(0, 4),
                 end = bankCard.Substring(bankCard.Length - 4);
            return $"{start}  **** ****  {end}";
        }
    }
}
