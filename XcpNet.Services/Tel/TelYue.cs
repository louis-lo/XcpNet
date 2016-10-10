using System.Net.Http;

namespace XcpNet.Services.Tel
{
    /// <summary>
    /// 账户余额查询
    /// </summary>
    public sealed class TelYueRequest : TelRequest<TelYueResponse>
    {
        /// <summary>
        /// 当前时间戳
        /// </summary>
        public string timestamp
        {
            get
            {
                return GenerateTimeStamp();
            }
        }
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
        /// 签名
        /// </summary>
        public string sign
        {
            get
            {
                string data = $"{TelConfig.OPENID}{key}{timestamp}";
                return Md5(data);
            }
        }
        /// <summary>
        /// 请求地址
        /// </summary>
        public override string RequestUrl
        {
            get
            {
                return $"http://op.juhe.cn/ofpay/mobile/yue?{ToUrl()}";
            }
        }
    }
    /// <summary>
    /// 账户余额查询返回结果集
    /// </summary>
    public sealed class TelYueResult
    {
        /// <summary>
        /// 用户账号
        /// </summary>
        public string uid;
        /// <summary>
        /// 	账户余额
        /// </summary>
        public decimal money;
    }
    /// <summary>
    /// 账户余额查询返回类
    /// </summary>
    public sealed class TelYueResponse : TelResponse
    {
        public TelYueResult result;
    }
}
