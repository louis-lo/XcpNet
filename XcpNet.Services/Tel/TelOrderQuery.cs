using System.Net.Http;

namespace XcpNet.Services.Tel
{
    /// <summary>
    /// 订单状态查询请求类
    /// </summary>
    public sealed class TelOrderQueryRequest : TelRequest<TelOrderQueryResponse>
    {
        /// <summary>
        /// 商家订单号，8-32位字母数字组合，请填写已经成功提交的订单号
        /// </summary>
        public string orderid { get; set; }
        /// <summary>
        /// 请求方式
        /// </summary>
        public override HttpMethod Method
        {
            get
            {
                return HttpMethod.Get;
            }
        }
        /// <summary>
        /// 请求地址
        /// </summary>
        public override string RequestUrl
        {
            get
            {
                return $"http://op.juhe.cn/ofpay/mobile/ordersta?{ToUrl()}";
            }
        }
    }
    /// <summary>
    /// 订单状态查询返回类
    /// </summary>
    public sealed class TelOrderQueryResponse : TelResponse
    {
        public TelOrderQueryResult result;
    }
    /// <summary>
    /// 订单状态查询结果集
    /// </summary>
    public sealed class TelOrderQueryResult
    {
        /// <summary>
        /// 订单扣除金额
        /// </summary>
        public string uordercash;
        /// <summary>
        /// 聚合订单号
        /// </summary>
        public string sporder_id;
        /// <summary>
        /// 状态 1:成功 9:失败 0：充值中
        /// </summary>
        public int game_state;
    }
}
