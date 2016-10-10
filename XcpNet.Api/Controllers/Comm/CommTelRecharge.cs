using Cnaws.Data;
using Cnaws.Passport.Modules;
using Cnaws.Web;
using System;
using XcpNet.Services.Modules;
using XcpNet.Services.Tel;

namespace XcpNet.Api.Controllers
{
    /// <summary>
    /// 手机充值通用api
    /// </summary>
    public class CommTelRecharge : CommonControllers
    {
        public static string ClassName = "[type]TelRecharge";
        private string mark;
        /// <summary>
        /// 手机号码和充值面额有效性查询
        /// </summary>
        public void TelQuery()
        {
            string mob = Request["mobile"];
            string price = Request["cardNo"];
            int cardnum = 0;
            int.TryParse(price, out cardnum);
            TelQueryResponse resp = new TelQueryResponse();
            try
            {
                if (!CheckMark(out mark) || string.IsNullOrEmpty(mob) || cardnum <= 0)
                {
                    resp.error_code = -1;
                    throw new ArgumentException("参数无效");
                }

                TelcheckRequest chkReq = new TelcheckRequest
                {
                    cardnum = cardnum,
                    phoneno = mob
                };

                TelResponse chkResp = chkReq.Execute();
                if (chkResp.error_code != 0)
                    throw new Exception(chkResp.reason);

                TelQueryRequest telQReq = new TelQueryRequest
                {
                    phoneno = mob,
                    cardnum = cardnum
                };
                resp = telQReq.Execute();
                if (resp.error_code != 0)
                    throw new ArgumentException(chkResp.reason);
                SetResult(resp.error_code, resp.result);
            }
            catch (Exception e)
            {
                SetResult(resp.error_code, e.Message);
            }
        }
#if (DEBUG)
        public static void TelQueryHelper()
        {
            CheckMarkHelper("CommTelRecharge", "TelQuery", "话费充值面额查询")
                .AddArgument("mobile", typeof(string), "手机号码")
                .AddArgument("cardNo", typeof(int), "面额目前可选：10、20、30、50、100、300")
                .AddResult(-1, "参数无效", typeof(string))
                .AddResult(0, "查询成功", typeof(TelQueryResult))
                .AddResult(208501, "大于0的返回码都为失败，具体对照（https://www.juhe.cn/docs/api/id/85/aid/213）");

        }
#endif
        /// <summary>
        /// 充值
        /// </summary>
        public void TelRec()
        {
            TelOnlineOrderResponse resp = new TelOnlineOrderResponse();
            Member member;
            try
            {
                //验证用户
                if (CheckMember(out member))
                {
                    resp.error_code = -1;
                    throw new ArgumentException("用户信息验证失败");
                }
                //创建充值订单
                TelRecharge item = DbTable.Load<TelRecharge>(Request.Form);
                if (item.Mobile <= 0 || item.CardNum <= 0 || item.OrderCash <= 0 || item.InPrice <= 0)
                {
                    resp.error_code = -2;
                    throw new ArgumentException("参数无效");
                }
                item.UserId = member.Id;
                item.OrderId = item.GenerateOrderId(member.Id);
                item.CreationDate = DateTime.Now;
                if (item.Insert(DataSource) != DataStatus.Success)
                {
                    resp.error_code = -3;
                    throw new ArgumentException("创建充值订单失败");
                }
                //调用聚合接口充值
                TelOnlineOrderRequest recReq = new TelOnlineOrderRequest
                {
                    cardnum = item.CardNum,
                    phoneno = item.Mobile,
                    orderid = item.OrderId
                };
                resp = recReq.Execute();

                if (resp.error_code == 0 && resp.result != null)
                    item.SporderId = resp.result.sporder_id;
                item.Reason = resp.reason;
                item.ErrorCode = resp.error_code;
                item.ApiUrl = recReq.RequestUrl;

                SetResult(resp.error_code, resp.result);
            }
            catch (Exception e)
            {
                SetResult(resp.error_code, e.Message);
            }
        }
#if (DEBUG)
        public static void TelRecHelper()
        {
            CheckMarkHelper(ClassName, "TelRec", "话费充值")
                .AddArgument("Mobile", typeof(string), "手机号码")
                .AddArgument("CardNum", typeof(int), "充值面额")
                .AddArgument("CardName", typeof(int), "充值名称")
                .AddArgument("Area", typeof(int), "号码归属地")
                .AddArgument("CardId", typeof(int), "接口提供商的充值产品id")
                .AddArgument("OrderCash", typeof(int), "支付扣款金额")
                .AddArgument("InPrice", typeof(int), "优惠价格")
                .AddArgument("UsePlatform", typeof(int), "使用平台，Unknown,Web,Wap,Wechat,Android,Apple,LED")
                .AddArgument("ApiUrl", typeof(int), "接口调用的完整地址包括参数")
                .AddArgument("PayId", typeof(int), "支付Id")
                .AddResult(-2, "用户未授权")
                .AddResult(-1, "参数无数")
                .AddResult(0, "成功提交充值订单", typeof(TelOnlineOrderResult))
                .AddResult(208501, "大于0的返回码都为失败，具体对照（https://www.juhe.cn/docs/api/id/85/aid/213）");
        }
#endif
        /// <summary>
        /// 订单状态查询
        /// </summary>
        [HttpPost]
        public void TelOrderQuery(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
                orderId = Request["orderId"];

            if (!CheckMark(out mark) || string.IsNullOrEmpty(orderId))
            {
                SetResult(-1, "mark参数或者订单号无效");
                return;
            }

            TelOrderQueryRequest oReq = new TelOrderQueryRequest
            {
                orderid = orderId
            };
            TelOrderQueryResponse resp = oReq.Execute();

            SetResult(resp.error_code, resp.result);
        }
#if (DEBUG)
        public static void TelOrderQueryHelper()
        {
            CheckMarkHelper(ClassName, "TelOrderQuery", "话费充值状态查询")
                .AddArgument("orderId", typeof(string), "订单号")
                .AddResult(0, "充值中")
                .AddResult(9, "充值失败")
                .AddResult(1, "充值成功")
                .AddResult(208501, "大于0的返回码都为失败，具体对照（https://www.juhe.cn/docs/api/id/85/aid/213）");
        }
#endif
    }
}
