using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace XcpNet.Services.Tel
{
    /// <summary>
    /// 根据手机号查询号码归属地
    /// </summary>
    public sealed class TelAreaQuery : TelRequest<TelAreaResponse>
    {
        /// <summary>
        /// 手机号码
        /// </summary>
        public string phoneno { get; set; }
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
                return $"http://apis.juhe.cn/mobile/get?{ToUrl()}";
            }
        }
    }
    /// <summary>
    /// 根据手机号查询返回
    /// </summary>
    public sealed class TelAreaResponse : TelResponse
    {
        public TelAreaResult result;
    }
    /// <summary>
    /// 根据手机号查询返回结果集
    /// </summary>
    public sealed class TelAreaResult
    {
        /// <summary>
        /// 省份
        /// </summary>
        public string province;
        /// <summary>
        /// 邮编
        /// </summary>
        public string zip;
        /// <summary>
        /// 城市
        /// </summary>
        public string city;
        /// <summary>
        /// 运营商
        /// </summary>
        public string company;
        /// <summary>
        /// 区号
        /// </summary>
        public string areacode;
        /// <summary>
        /// 卡类型
        /// </summary>
        public string card;
    }
}

