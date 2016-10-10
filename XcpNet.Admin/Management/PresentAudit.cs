using System;
using System.Collections.Generic;
using Cnaws.Data;
using Cnaws.Web;
using Cnaws.Management;
using Cnaws.Data.Query;
using U = Cnaws.Passport.Modules;

namespace XcpNet.Admin.Management
{
    /// <summary>
    /// 加盟商提现审核
    /// </summary>
    public sealed class PresentAudit : ManagementController
    {
        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        private const string CONST_CHECKEDSTR = "权限不足或者请求不合法";

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
            if (CheckAjax() && CheckRight() && CheckPost("presentaudit"))
                NotFound();
            this["States"] = U.MemberDrawOrder.GetOrderStateList();
        }
        /// <summary>
        /// 分页读取提现申请数据
        /// </summary>
        /// <param name="query">查询字符</param>
        public void GetDrawOrders(string query)
        {
            try
            {
                if (!CheckAjax() && !CheckRight())
                    throw new AggregateException(CONST_CHECKEDSTR);

                int pageIndex;
                int.TryParse(Request["pagenum"], out pageIndex);
                ++pageIndex;

                var list = U.MemberDrawOrder.GetPresentAuditList(DataSource, query, pageIndex);
                List<object> result = new List<object>();
                foreach (var item in list)
                {
                    result.Add(new
                    {
                        OrderId = item.A.OrderId,
                        UserName = item.B.Name,
                        Mobile = item.B.Mobile,
                        AccountName = item.A.AccountName,
                        BankCard = item.A.BankCard,
                        BankName = item.A.BankName,
                        BankZone = item.A.BankZone,
                        DrawMoney = item.A.DrawMoney.ToString("c2"),
                        CreateTime = item.A.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        OrderStateText = item.A.GetOrderStateText(),
                        Action = new
                        {
                            OrderId = item.A.OrderId,
                            OrderState = item.A.OrderState.ToString(),
                            TransactionNumber = item.A.TransactionNumber,
                            RefusalReasons = item.A.RefusalReasons,
                            CredentialImage = item.A.CredentialImage
                        }
                    });
                }
                SetResult(new SplitPageData<object>(list.PageIndex, list.PageSize, result, (int)list.TotalCount));
            }
            catch (Exception e)
            {
                SetResult(false, e.Message);
            }
        }
        /// <summary>
        /// 处理动作
        /// </summary>
        [HttpPost]
        public void Action()
        {
            try
            {
                if (!CheckAjax() || !CheckRight())
                    throw new AggregateException(CONST_CHECKEDSTR);

                U.MemberDrawOrder order = DbTable.Load<U.MemberDrawOrder>(Request.Form);

                if (string.IsNullOrEmpty(order.OrderId) ||
                order.OrderState == U.DrawOrderStatus.None ||
                order.OrderState == U.DrawOrderStatus.PendingAudit)
                    throw new ArgumentException("参数无效");

                int updateResult = 0;

                switch (order.OrderState)
                {
                    case U.DrawOrderStatus.InTreatment:
                        updateResult = Db<U.MemberDrawOrder>
                               .Query(DataSource)
                               .Update()
                               .Set("OrderState", order.OrderState)
                               .Set("ProcessingDateTime", DateTime.Now)
                               .Where(new DbWhereQueue("OrderId", order.OrderId))
                               .Execute();
                        break;
                    case U.DrawOrderStatus.AuditFailure:
                        updateResult = Db<U.MemberDrawOrder>
                                 .Query(DataSource)
                                 .Update()
                                 .Set("OrderState", order.OrderState)
                                 .Set("ProcessingDateTime", DateTime.Now)
                                 .Set("TradeSuccessDateTime", DateTime.Now)
                                 .Set("AuditFailureDateTime", DateTime.Now)
                                .Set("RefusalReasons", order.RefusalReasons)
                                 .Where(new DbWhereQueue("OrderId", order.OrderId))
                                 .Execute();
                        break;

                    case U.DrawOrderStatus.TradeSuccess:
                        updateResult = Db<U.MemberDrawOrder>
                              .Query(DataSource)
                              .Update()
                              .Set("OrderState", order.OrderState)
                              .Set("TradeSuccessDateTime", DateTime.Now)
                              .Set("CredentialImage", order.CredentialImage)
                              .Set("TransactionNumber", order.TransactionNumber)
                              .Where(new DbWhereQueue("OrderId", order.OrderId))
                              .Execute();
                        break;
                    default:
                        break;
                }
                SetResult(updateResult > 0);
            }
            catch (ArgumentException e)
            {
                SetResult(false, e.Message);
            }
            catch (Exception e)
            {
                SetResult(false);
            }

        }
    }
}
