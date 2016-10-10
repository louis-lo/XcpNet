using System.Net.Http;

namespace XcpNet.Services.Tel
{
    /// <summary>
    /// 根据手机号和面值查询商品
    /// </summary>
    public sealed class TelQueryRequest : TelRequest<TelQueryResponse>
    {
        /// <summary>
        /// 手机号码
        /// </summary>
        public string phoneno { get; set; }
        /// <summary>
        /// 充值面额
        /// </summary>
        public int cardnum { get; set; }
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
                return $"http://op.juhe.cn/ofpay/mobile/telquery?{ToUrl()}";
            }
        }
    }
    /// <summary>
    /// 根据手机号和面值查询返回
    /// </summary>
    public sealed class TelQueryResponse : TelResponse
    {
        public TelQueryResult result;
    }
    /// <summary>
    /// 根据手机号和面值查询返回结果集
    /// </summary>
    public sealed class TelQueryResult
    {
        /// <summary>
        /// 卡类ID
        /// </summary>
        public string cardid;
        /// <summary>
        /// 卡类名称
        /// </summary>
        public string cardname;
        /// <summary>
        /// 购买价格
        /// </summary>
        public decimal inprice;
        /// <summary>
        /// 手机号码归属地
        /// </summary>
        public string game_area;
    }
}
