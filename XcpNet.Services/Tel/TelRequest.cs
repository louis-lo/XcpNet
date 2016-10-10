using Cnaws.Json;
using System;

namespace XcpNet.Services.Tel
{
    [Serializable]
    public abstract class TelRequest<T> : RequestObject<T> where T : TelResponse, new()
    {
        /// <summary>
        /// 密钥
        /// </summary>
        public virtual string key
        {
            get
            {
                return TelConfig.APPKEY;
            }
        }
    }

    [Serializable]
    public class TelResponse : ResponseObject
    {
        /// <summary>
        /// 返回说明
        /// </summary>
        public string reason;
        /// <summary>
        ///返回码
        /// </summary>
        public int error_code;
        /// <summary>
        /// 获取请求返回数据
        /// </summary>
        /// <typeparam name="T">TelResponse子类</typeparam>
        /// <returns></returns>
        public override T GetResponse<T>()
        {
            if (string.IsNullOrEmpty(Body))
                throw new ArgumentNullException($"{Body}不能为空");

            T res1 = JsonValue.Deserialize<T>(Body);
            return res1;
        }
    }

    public static class TelConfig
    {
        public static string OPENID = "JH70b12eee0266e7a5ff35e1c70e8f8cc8";
        public static string APPKEY = "53b8a28aa75ca5d69d9f31079bed26ba";
    }
}
