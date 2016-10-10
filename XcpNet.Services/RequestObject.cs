using System;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Security.Cryptography;
using Cnaws.Json;

namespace XcpNet.Services
{
    /// <summary>
    /// web请求公用类
    /// </summary>
    public abstract class RequestObject<TResponse> where TResponse : ResponseObject, new()
    {
        /// <summary>
        /// 请求地址
        /// </summary>
        [UrlIgnore]
        public abstract string RequestUrl { get; }
        /// <summary>
        /// 请求方式
        /// </summary>
        [UrlIgnore]
        public virtual HttpMethod Method
        {
            get { return HttpMethod.Post; }
        }
        [UrlIgnore]
        public object this[string propertyName]
        {
            get { return GetType().GetProperty(propertyName).GetValue(this, null); }
        }

        /// <summary>
        /// 序列化请求数据
        /// </summary>
        /// <returns></returns>
        public virtual string GenerateRequestData()
        {
            return JsonValue.Serialize(this);
        }

        /// <summary>
        /// 生成地址参数
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string ToUrl()
        {
            StringBuilder buff = new StringBuilder();
            Type type = GetType();
            var properties = type.GetProperties().OrderBy(a => a.Name).ToList();
            foreach (PropertyInfo property in properties)
            {
                if (property.GetCustomAttribute<UrlIgnore>() != null) continue;

                object value = property.GetValue(this);
                if (value == null)
                    throw new ArgumentNullException($"{property.Name}不能为null!");

                if (!string.IsNullOrEmpty(value.ToString()) && value.ToString() != "0")
                {
                    buff.AppendFormat("{0}={1}&", property.Name, value);
                }
            }
            return buff.ToString().Trim('&');
        }
        /// <summary>
        /// 32位MD5加密
        /// </summary>
        /// <param name="s">
        /// <returns></returns>
        public string Md5(string s)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            bytes = md5.ComputeHash(bytes);
            md5.Clear();

            StringBuilder ret = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                ret.Append(Convert.ToString(bytes[i], 16).PadLeft(2, '0'));
            }

            return ret.ToString().PadLeft(32, '0');
        }
        /// <summary>
        /// 生成时间戳
        /// </summary>
        /// <returns></returns>
        public string GenerateTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        /// <summary>
        /// 执行网络主动
        /// </summary>
        /// <returns></returns>
        public TResponse Execute()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(RequestUrl);

            request.Timeout = 8 * 1000;
            if (Method == HttpMethod.Post)
            {
                request.Method = "POST";
                //设置POST的数据类型和长度
                byte[] data = Encoding.UTF8.GetBytes(GenerateRequestData());
                request.ContentLength = data.Length;
                //往服务器写入数据
                using (Stream reqStream = request.GetRequestStream())
                    reqStream.Write(data, 0, data.Length);
            }

            TResponse resp = new TResponse();
            //获取服务端返回
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream s = response.GetResponseStream())
                {
                    //获取服务端返回数据
                    using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
                        resp.Body = sr.ReadToEnd().Trim();
                }
            }

            return resp.GetResponse<TResponse>();
        }
        /// <summary>
        /// 异步执行网络请求
        /// </summary>
        public void ExecuteAsync()
        {
            Thread thread = new Thread(() =>
            {
                Execute();
            });
            thread.Start();
        }
    }
    /// <summary>
    /// web请求返回
    /// </summary>
    public abstract class ResponseObject
    {
        /// <summary>
        /// 返回数据体
        /// </summary>
        public virtual string Body { get; set; }
        public abstract T GetResponse<T>() where T : ResponseObject, new();
    }
    /// <summary>
    /// 生成地址参数是标记为忽略
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class UrlIgnore : Attribute
    {
        public UrlIgnore() { }
    }
}
