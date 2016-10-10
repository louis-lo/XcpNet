using System;
using Cnaws.Data;
using Cnaws.Sms;
using M = Cnaws.Sms.Modules;

namespace XcpNet.Services.Sms
{
    /// <summary>
    /// 首易接口封装
    /// </summary>
    public sealed class SmsMobset
    {
        /// <summary>
        /// 使用首易接口发送短信
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="to">手机号码</param>
        /// <param name="tmplName">模板名称</param>
        /// <param name="arguments">模板对应该参数</param>
        public static void Send(DataSource ds, long to, string tmplName, params string[] arguments)
        {
            try
            {
                if (to <= 0)
                    throw new ArgumentNullException("发送手机号码不能为空");

                if (string.IsNullOrEmpty(tmplName))
                    throw new ArgumentNullException("模板名称不能为空");

                SmsProvider mobest = SmsProvider.Create("Mobset");
                if (mobest == null)
                    throw new Exception("Mobset不能为null");

                M.Sms sms = M.Sms.GetById(ds, "mobest");
                if (sms == null)
                    throw new Exception("Mobset 接口配置没有找到");

                mobest.Account = sms.Account;
                mobest.AppId = sms.AppId;
                mobest.Token = sms.Token;

                string body = M.SmsTemplate.GetByName(ds, tmplName).Content;
                if (string.IsNullOrEmpty(body))
                    throw new Exception(string.Concat(tmplName, "模板未找到"));

                mobest.Send(to, body, arguments);
            }
            catch
            {

            }
        }
    }
}
