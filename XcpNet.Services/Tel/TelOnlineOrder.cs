using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XcpNet.Services;

namespace XcpNet.Services.Tel
{
    /// <summary>
    /// 话费充值请求
    /// </summary>
    public class TelOnlineOrderRequest : TelRequest<TelOnlineOrderResponse>
    {
        /// <summary>
        /// 手机号码
        /// </summary>
        public long phoneno { get; set; }
        /// <summary>
        /// 充值面额
        /// </summary>
        public int cardnum { get; set; }
        /// <summary>
        /// 订单号
        /// </summary>
        public string orderid { get; set; }
        /// <summary>
        /// 签名
        /// </summary>
        public string sign
        {
            get
            {
                string data = $"{TelConfig.OPENID}{key}{phoneno}{cardnum}{orderid}";
                return Md5(data);
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
        /// 请求地址
        /// </summary>
        public override string RequestUrl
        {
            get
            {
                return $"http://op.juhe.cn/ofpay/mobile/onlineorder?{ToUrl()}";
            }
        }
    }
    public sealed class TelOnlineOrderResponse : TelResponse
    {
        /// <summary>
        /// 话费充值请求返回结果集
        /// </summary>
        public TelOnlineOrderResult result;
    }
    public sealed class TelOnlineOrderResult
    {
        /// <summary>
        /// 充值的卡类ID
        /// </summary>
        public string cardid;
        /// <summary>
        /// 数量
        /// </summary>
        public int cardnum;
        /// <summary>
        /// 进货价格
        /// </summary>
        public decimal ordercash;
        /// <summary>
        /// 充值名称
        /// </summary>
        public string cardname;
        /// <summary>
        /// 聚合订单号
        /// </summary>
        public string sporder_id;
        /// <summary>
        /// 商户自定义的订单号
        /// </summary>
        public string orderid;
        /// <summary>
        /// 充值的手机号码
        /// </summary>
        public string game_userid;
        /// <summary>
        /// 充值状态:0充值中 1成功 9撤销，刚提交都返回0
        /// </summary>
        public int game_state;
    }
    /// <summary>
    /// 话费充值状态通知类
    /// </summary>
    public sealed class TelOnlineOrderNotify
    {
        /// <summary>
        /// 聚合订单号
        /// </summary>
        public string sporder_id;
        /// <summary>
        /// 用户自定义的单号
        /// </summary>
        public string orderid;
        /// <summary>
        /// 充值状态1：成功 9：失败
        /// </summary>
        public int sta;
        /// <summary>
        /// 校验值，md5(appkey+sporder_id+orderid) 32位小写，用于校验请求合法性
        /// </summary>
        public string sign;
        /// <summary>
        /// 校验签名
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public bool ChkSign()
        {
            string str = $"{TelConfig.APPKEY}{sporder_id}{orderid}";
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            bytes = md5.ComputeHash(bytes);
            md5.Clear();

            StringBuilder ret = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
                ret.Append(Convert.ToString(bytes[i], 16).PadLeft(2, '0'));
            string chkSign = ret.ToString().PadLeft(32, '0');
            return chkSign.Equals(sign);
        }
    }
}
