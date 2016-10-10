using Cnaws;
using Cnaws.Data;
using Cnaws.Data.Query;
using Cnaws.Templates;
using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcpNet.Services.Tel;
using Cnaws.ExtensionMethods;

namespace XcpNet.Services.Modules
{
    /// <summary>
    /// 手机充值记录
    /// </summary>
    public class TelRecharge : LongIdentityModule
    {
        public long UserId = 0L;
        /// <summary>
        ///订单号
        /// </summary>
        [DataColumn(36)]
        public string OrderId = string.Empty;
        /// <summary>
        /// 聚合订单号
        /// </summary>
        [DataColumn(20)]
        public string SporderId = string.Empty;
        /// <summary>
        /// 聚合接口提供商的产品Id
        /// </summary>
        public int CardId = 0;
        /// <summary>
        /// 支付号
        /// </summary>
        [DataColumn(36)]
        public string PayId = string.Empty;
        /// <summary>
        /// 手机号码
        /// </summary>
        public long Mobile = 0L;
        /// <summary>
        /// 手机号码归属地
        /// </summary>
        [DataColumn(20)]
        public string Area = string.Empty;
        /// <summary>
        /// 充值面额
        /// </summary>
        public int CardNum = 0;
        /// <summary>
        /// 充值名称
        /// </summary>
        [DataColumn(36)]
        public string CardName = string.Empty;
        /// <summary>
        /// 优惠价格
        /// </summary>
        public Money InPrice = 0;
        /// <summary>
        /// 实际支付扣款金额
        /// </summary>
        public Money OrderCash = 0;
        /// <summary>
        /// 聚合平台返回状态说明
        /// </summary>
        [DataColumn(36)]
        public string Reason = string.Empty;
        /// <summary>
        /// 最终的充值状态,0为充值中，1为成功，2为失败
        /// </summary>
        public int State = 0;
        /// <summary>
        /// 聚合平台返回状态码，请参数
        /// https://www.juhe.cn/docs/api/id/85/aid/214
        /// </summary>
        public int ErrorCode = 0;
        /// <summary>
        /// 使用平台类型
        /// </summary>
        [DataColumn(10)]
        public string UsePlatform = PlatformType.Unknown.ToString();
        /// <summary>
        /// 充值时间
        /// </summary>
        public DateTime CreationDate = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);
        /// <summary>
        /// 充值api完整地址
        /// </summary>
        [DataColumn(500)]
        public string ApiUrl = string.Empty;

        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "IdIndex");
        }
        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "IdIndex", "Id", "UserId");
        }

        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            return (UserId > 0L &&
                Mobile > 0L &&
                InPrice > 0 &&
                OrderCash > 0 &&
                !string.IsNullOrEmpty(OrderId) ?
                DataStatus.Success : DataStatus.Failed);
        }
        /// <summary>
        ///以当前时间和随机数 生成订单号
        /// </summary>
        /// <returns></returns>
        public string GenerateOrderId(long userId)
        {
            return string.Concat(DateTime.Now.ToTimestamp().ToString(), userId.ToString()); ;
        }
        /// <summary>
        ///  获取充值记录
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="orderId"></param>
        /// <param name="sporderId"></param>
        /// <returns></returns>
        public static TelRecharge Get(DataSource ds, string orderId, string sporderId = "")
        {
            DbWhereQueue where = new DbWhereQueue();
            where = W("OrderId", orderId);
            if (!string.IsNullOrEmpty(sporderId))
                where = where & W("SporderId", sporderId);

            return Db<TelRecharge>
                .Query(ds)
                .Select()
                .Where(where)
                .First<TelRecharge>();
        }
    }
    /// <summary>
    /// 平台类型
    /// </summary>
    public enum PlatformType
    {
        Unknown,
        WEB,
        WAP,
        Wechat,
        Android,
        Apple,
        LED
    }
}
