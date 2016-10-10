using System;
using Cnaws.Data;
using Cnaws.Web;
using Cnaws.Json;
using XcpNet.Services.Modules;
using XcpNet.Services.Tel;
using System.IO;
using System.Text;
using Cnaws.Pay.Controllers;
using Cnaws.Pay;
using Cnaws;
using M = Cnaws.Passport.Modules;
using Cnaws.Pay.Modules;
using XcpNet.AfterSales.Modules;
using System.Threading;

namespace XcpNet.Passport.Controllers
{
    /// <summary>
    /// 手机话费充值控制器
    /// </summary>
    public class Tel : PaymentBase
    {
        /// <summary>
        /// 充值首页
        /// </summary>
        [Authorize(true)]
        protected override void OnIndex()
        {
            Render("tel_index.html");
        }
        /// <summary>
        /// 确认充值，
        /// 这里使用"|"分隔参数是传递是因为paypassword2页面
        /// 使用的$target变量不能接收多个参数
        /// </summary>
        [Authorize(true)]
        public void TelComfim()
        {
            string p = Request["p"];
            if (string.IsNullOrEmpty(p))
                throw new ArgumentNullException("参数有误");
            string[] param = p.Split('|');
            int cardnum = 0;
            int.TryParse(param[1], out cardnum);

            this["Mob"] = param[0];
            this["CardNum"] = cardnum;
            this["HasPayPassword"] = !string.IsNullOrEmpty(M.MemberInfo.GetPayPasswordById(DataSource, User.Identity.Id));
            this["Balance"] = M.MemberInfo.GetMoney(DataSource, User.Identity.Id).ToString("c2");
            Render("tel_confirm.html");
        }
        /// <summary>
        /// 充值手机号和面额查询
        /// </summary>
        [HttpPost]
        [HttpAjax]
        [Authorize(true)]
        public void TelQuery()
        {
            string mob = Request["Mobile"],
             price = Request["CardNo"];
            int cardnum = 0;
            int.TryParse(price, out cardnum);

            try
            {
                if (string.IsNullOrEmpty(mob) || cardnum <= 0)
                    throw new ArgumentException("参数无效");
                TelcheckRequest chkReq = new TelcheckRequest
                {
                    cardnum = cardnum,
                    phoneno = mob
                };

                TelResponse resp = chkReq.Execute();
                if (resp.error_code != 0)
                    throw new Exception("手机号或充值金额不受支持");

                TelQueryRequest telQReq = new TelQueryRequest
                {
                    phoneno = mob,
                    cardnum = cardnum
                };
                TelQueryResponse telQresp = telQReq.Execute();
                SetResult(telQresp.result);
            }
            catch (Exception e)
            {
                SetResult(false, e.Message);
            }
        }
        /// <summary>
        /// 余额查询
        /// </summary>
        [HttpPost]
        [HttpAjax]
        [Authorize(true)]
        public void TelYue()
        {
            TelYueRequest oReq = new TelYueRequest();
            TelYueResponse resp = oReq.Execute();
            SetResult(resp);
        }
        /// <summary>
        /// 订单状态查询
        /// </summary>
        [HttpPost]
        [HttpAjax]
        [Authorize(true)]
        public void OrderQuery()
        {
            string orderId = Request["orderId"];
            if (string.IsNullOrEmpty(orderId))
            {
                SetResult(false, "订单号无效");
                return;
            }

            TelOrderQueryRequest oReq = new TelOrderQueryRequest
            {
                orderid = orderId
            };
            TelOrderQueryResponse resp = oReq.Execute();

            SetResult(resp);
        }
        /// <summary>
        /// 话费充值状态通知，与api接口共用
        /// </summary>
        [HttpPost]
        public void JuheNotify()
        {
            string json;
            using (StreamReader reader = new StreamReader(Request.InputStream, Encoding.UTF8))
                json = reader.ReadToEnd();
            TelOnlineOrderNotify notify = JsonValue.Deserialize<TelOnlineOrderNotify>(json);
            if (notify.ChkSign())
            {
                TelRecharge item = TelRecharge.Get(DataSource, notify.orderid, notify.sporder_id);
                if (item != null)
                {
                    item.Reason = notify.sta == 1 ? "充值成功" : "充值失败";
                    item.State = notify.sta == 1 ? 1 : 2;
                    item.Update(DataSource);
                    Response.Write("success");
                }
                else
                {
                    Response.Write("订单不存在");
                }
            }
            else
            {
                Response.Write("签名不合法");
            }
        }
        /// <summary>
        /// 验证支付密码
        /// </summary>
        public void ChkPayPwd()
        {
            string PayPassword = Request["PayPassword"];
            if (!string.IsNullOrEmpty(PayPassword))
            {
                if (M.MemberInfo.CheckPayPassword(DataSource, User.Identity.Id, PayPassword))
                {
                    SetResult(true);
                }
                else
                {
                    SetResult(false);
                }
            }
        }
        /// <summary>
        /// 最后的充值提示
        /// </summary>
        public void Success()
        {
            string money = Request["money"];
            this["Success"] = Request["success"];
            this["OrderId"] = Request["orderId"];
            this["Money"] = Money.Parse(money).ToString("c2");
            this["Reason"] = Request["reason"];
            Render("tel_result.html");
        }
        #region 支付相关
        /// <summary>
        /// 手机充值订单类型，等于20是为了跟其它的区分开
        /// </summary>
        private const int CONST_TELRECORDERTYPE = 20;
        private string returnUrl = string.Empty;
        protected override string ReturnUrl
        {
            get
            {
                return string.Concat("/tel/", returnUrl);
            }
        }
        protected override IPayOrder GetPayOrder(string provider)
        {
            string pwd = Request.Form["PayPassword"],
                     openId = null;
            long userId = User.Identity.Id;
            DataSource.Begin();
            try
            {
                if (userId == 0)
                    throw new Exception("找不到用户");

                if (!string.IsNullOrEmpty(pwd) && provider.ToLower() == "balance")
                {
                    if (!M.MemberInfo.CheckPayPassword(DataSource, userId, pwd))
                        throw new Exception("支付密码错误");
                }

                //判断是否微信支付
                M.OAuth2Member member = M.OAuth2Member.GetByUserPay(DataSource, provider, userId);
                if (member != null)
                    openId = member.UserId;

                //创建充值记录
                TelRecharge telRec = DbTable.Load<TelRecharge>(Request.Form);
                telRec.OrderId = telRec.GenerateOrderId(userId);
                telRec.CreationDate = DateTime.Now;
                telRec.UserId = userId;
                if (DataStatus.Success != telRec.Insert(DataSource))
                    throw new Exception("插入充值记录失败");

                //创建支付记录
                PayRecord pr = PayRecord.GetByUser(DataSource, userId, PaymentType.Pay, PayStatus.Paying, CONST_TELRECORDERTYPE);
                if (pr == null)
                {
                    pr = PayRecord.Create(DataSource, userId, openId, telRec.CardName, provider, telRec.OrderCash, CONST_TELRECORDERTYPE, telRec.OrderId);
                    if (pr == null)
                        throw new Exception("插入支付记录失败");
                }
                else
                {
                    pr.Provider = provider;
                    pr.OpenId = openId;
                    pr.Money = telRec.OrderCash;
                    pr.TargetId = telRec.OrderId;
                    pr.Type = CONST_TELRECORDERTYPE;
                    pr.Title = telRec.CardName;
                    pr.CreationDate = DateTime.Now;
                    if (pr.Update(DataSource, ColumnMode.Include, "Provider", "OpenId", "Money") != DataStatus.Success)
                        throw new Exception("更新支付记录失败");
                }

                DataSource.Commit();
                return pr;
            }
            catch (Exception)
            {
                DataSource.Rollback();
                throw;
            }
        }
        protected override void OnCallback(PayProvider provider, bool success, PaymentResult result)
        {
            base.OnCallback(provider, success, result);
            //如果不需要支付通知，直接去充值
            if (!provider.IsNeedNotify)
            {
                UpdateTelOrderAndRec(result);
            }
        }
        protected override bool OnNotify(PayProvider provider, bool success, PaymentResult result)
        {
            bool payResult = base.OnNotify(provider, success, result);
            if (payResult)
                UpdateTelOrderAndRec(result);
            return payResult;
        }
        protected override bool CheckMoney(IPayOrder order, out PaymentResult result)
        {
            result = new PayResult()
            {
                TradeNo = order.TradeNo,
                PayTradeNo = order.TradeNo,
                TotalFee = order.TotalFee
            };
            return true;
        }
        protected override bool OnModifyMoney(PayProvider provider, PaymentType payment,
            long userId, Money money, string trade, string title, int type, string targetId)
        {
            if (payment != PaymentType.Pay && type != CONST_TELRECORDERTYPE)
                throw new ArgumentException("参数无效");

            DataSource.Begin();
            try
            {
                M.MemberInfo member = M.MemberInfo.GetByRecharge(DataSource, userId);
                if (member == null)
                    throw new Exception("用户无效");
                TelRecharge order = TelRecharge.Get(DataSource, targetId);

                if (member.Money < order.OrderCash)
                    throw new Exception("余额不足");

                if (M.MemberInfo.ModifyMoney(DataSource, userId, -order.OrderCash, "手机话费充值", type, targetId) != DataStatus.Success)
                    throw new Exception("扣款失败");

                DataSource.Commit();
            }
            catch (ThreadAbortException)
            {
                DataSource.Rollback();
            }
            catch (Exception)
            {
                DataSource.Rollback();
            }
            return true;
        }
        /// <summary>
        /// 验证支付密码
        /// </summary>

        #endregion

        /// <summary>
        /// 更新充值订单和充值话费
        /// </summary>
        /// <param name="result">支付结果</param>
        private void UpdateTelOrderAndRec(PaymentResult result)
        {
            PayResult presult = (PayResult)result;
            PayRecord precord = PayRecord.GetById(DataSource, presult.TradeNo, result.Type);
            if (precord.Status != PayStatus.PaySuccess)
                throw new Exception("订单支付失败");

            TelRecharge telrec = TelRecharge.Get(DataSource, precord.TargetId);
            telrec.PayId = presult.PayTradeNo;

            TelOnlineOrderRequest recReq = new TelOnlineOrderRequest
            {
                cardnum = telrec.CardNum,
                phoneno = telrec.Mobile,
                orderid = telrec.OrderId
            };
            returnUrl = $"Success.html?orderId={telrec.OrderId}&money={telrec.OrderCash}&";
            TelOnlineOrderResponse response = recReq.Execute();
            if (response.error_code == 0 && response.result != null)
            {
                telrec.SporderId = response.result.sporder_id;
                returnUrl += "success =true";
            }
            else
            {
                //returnUrl += $"success =false&reason={response.reason}";
                returnUrl += $"success =false&reason=系统账户可用余额不足";
            }

            telrec.Reason = response.reason;
            telrec.ErrorCode = response.error_code;
            telrec.ApiUrl = recReq.RequestUrl;
            telrec.Update(DataSource);
        }
    }
}


