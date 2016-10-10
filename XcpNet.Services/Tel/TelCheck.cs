using System.Net.Http;

namespace XcpNet.Services.Tel
{
    /// <summary>
    /// 检测手机号码是否能充值
    /// </summary>
    public class TelcheckRequest : TelRequest<TelResponse>
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
                return $"http://op.juhe.cn/ofpay/mobile/telcheck?{ToUrl()}";
            }
        }
    }
}
