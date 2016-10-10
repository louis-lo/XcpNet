using Cnaws.Data;
using Cnaws.ExtensionMethods;
using Cnaws.Web;
using Cnaws.Web.Configuration;
using System;
using M = Cnaws.Passport.Modules;
using S = Cnaws.Sms.Modules;
using V = Cnaws.Verification.Modules;
using A = XcpNet.Ad.Modules;
using P = Cnaws.Product.Modules;
using System.Collections.Specialized;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web;
using System.Text.RegularExpressions;

namespace XcpNet.Common
{
    public class CommPassport : Cnaws.Sms.Controllers.Sms
    {
        public CommPassport(DataController controller)
        {
            InitController(controller);
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="registertype"></param>
        /// <param name="member"></param>
        /// <param name="smscaptcha"></param>
        /// <param name="mark"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object Register(DataSource ds, int registertype, M.MemberInfo member, string smscaptcha, string mark, out object data)
        {
            object code = null, newdata = null;
            data = null;
            try
            {
                M.RegisterType type = (M.RegisterType)registertype;
                PassportSection section = PassportSection.GetSection();
                if (type == M.RegisterType.Mobile)
                {
                    if (!M.Member.CheckMobile(ds, member.Mobile))
                    {
                        code = CommUtility.MEMBER_EXISTENCE;
                        throw new AggregateException();
                    }
                    if (section.VerifyMobile)
                    {
                        if (!V.MobileHash.Equals(ds, member.Mobile, V.MobileHash.Register, smscaptcha))
                        {
                            code = CommUtility.SMSCAPTCHA_EQUALS;
                            throw new AggregateException();
                        }
                        member.VerMob = true;
                    }
                }
                else if (type == M.RegisterType.Email)
                {
                    if (!M.Member.CheckEmail(ds, member.Email))
                    {
                        code = CommUtility.MEMBER_EXISTENCE;
                        throw new AggregateException();
                    }
                }
                else if (type == M.RegisterType.Name)
                {
                    if (M.Member.CheckName(ds, member.Name))
                    {
                        code = CommUtility.MEMBER_EXISTENCE;
                        throw new AggregateException();
                    }
                }

                P.Distributor distributor = A.MachineCode.GetDistributorByCode(ds, mark);
                if (distributor != null && distributor.UserId != 0)
                    member.ParentId = distributor.UserId;
                else
                    member.ParentId = 0;
                member.Token = Guid.NewGuid();
                member.Mark = mark;
                member.Approved = section.DefaultApproved;
                member.CreationDate = DateTime.Now;
                DataStatus status = member.Insert(ds);
                if (status != DataStatus.Success)
                {
                    code = CommUtility.INSERT_FAIL;
                    throw new AggregateException();
                }
                newdata = member.Token;
                data = newdata;
                return CommUtility.SUCCESS;
            }
            catch (AggregateException)
            {
                data = newdata;
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }
        
        /// <summary>
        /// 登陆
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="clientip"></param>
        /// <param name="mark"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object Login(DataSource ds, string username, string password, string clientip, string mark, out object data)
        {
            object code = null;
            data = null;
            try
            {
                if (string.IsNullOrEmpty(mark))
                {
                    code = CommUtility.PARAMETER_NOFOND;
                    throw new AggregateException();
                }
                int errCount;
                Guid token;
                M.LoginStatus status = M.Member.ApiLogin(ds, username, password, clientip, mark, out errCount, out token);
                if (status == M.LoginStatus.Success)
                {
                    code = CommUtility.SUCCESS;
                    data = token;
                }
                else
                {
                    if (status == M.LoginStatus.CaptchaError)
                    {
                        code = CommUtility.SMSCAPTCHA_EQUALS;
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.PasswordError)
                    {
                        code = CommUtility.PASSWORD_EQUALS;
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.Locked)
                    {
                        code = CommUtility.MEMBER_LOCKED;
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.NotApproved)
                    {
                        code = CommUtility.MEMBER_APPROVED;
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.NotFound)
                    {
                        code = CommUtility.MEMBER_NOTFOUND;
                        throw new AggregateException();
                    }
                    else if (status == M.LoginStatus.SmsCaptchaError)
                    {
                        code = CommUtility.SMSCAPTCHA_EQUALS;
                        throw new AggregateException();
                    }
                }
                return code;
            }
            catch (AggregateException)
            {
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }
        
        /// <summary>
        /// 发送短信
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="mobile"></param>
        /// <param name="name"></param>
        /// <param name="SmsType"></param>
        /// <param name="mark"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public object SendSms(DataSource ds, long mobile, string name, int SmsType, string mark, out object data)
        {
            object code = null;
            data = null;
            if (CommUtility.CheckMobile(mobile))
            {
                try
                {
                    PassportSection section = PassportSection.GetSection();
                    if (!section.VerifyMobile)
                    {
                        code = CommUtility.SMS_SEND;
                        throw new AggregateException();
                    }
                    int timespan = SMSCaptchaSection.GetSection().TimeSpan;
                    V.MobileHash hash = V.MobileHash.Create(ds, mobile, SmsType, timespan);
                    if (hash == null)
                    {
                        code = CommUtility.SMS_EXISTENCE;
                        data = new { TimeSpan = timespan };
                        throw new AggregateException();
                    }

                    if (string.IsNullOrEmpty(mark))
                    {
                        code = CommUtility.PARAMETER_NOFOND;
                        throw new AggregateException();
                    }
                    string md5 = mark.MD5();
                    V.StringHash sh = V.StringHash.Create(ds, md5, V.StringHash.SmsHash, timespan);
                    if (sh == null)
                    {
                        code = CommUtility.SMS_EXISTENCE;
                        data = new { TimeSpan = timespan };
                        throw new AggregateException();
                    }
                    string SmsTemplate;
                    if (SmsType == 0) SmsTemplate = "register"; else SmsTemplate = "password";
                    S.SmsTemplate temp = S.SmsTemplate.GetByName(ds, SmsTemplate);
                    if (temp.Type == S.SmsTemplateType.Template)
                        SendTemplateImpl(name, mobile, temp.Content, hash.Hash);
                    else
                        SendImpl(name, mobile, temp.Content, hash.Hash);

                    data = new { TimeSpan = timespan };
                    return CommUtility.SUCCESS;
                }
                catch (AggregateException)
                {
                    return code;
                }
                catch (Exception ex)
                {
                    data = new { Message = ex.Message };
                    return CommUtility.PROGRAM_ERROR;
                }
            }
            else
            {
                return CommUtility.MOBILE_FORMAT;
            }
        }

        /// <summary>
        /// 找回密码
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="mobile"></param>
        /// <param name="Password"></param>
        /// <param name="smscaptcha"></param>
        /// <param name="mark"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object FindPassword(DataSource ds, long mobile, string Password, string smscaptcha, string mark, out object data)
        {
            object code = null;
            data = null;
            try
            {
                if (CommUtility.CheckMobile(mobile))
                {
                    if (!V.MobileHash.Equals(ds, mobile, V.MobileHash.Password, smscaptcha))
                    {
                        code = CommUtility.SMSCAPTCHA_EQUALS;
                        throw new AggregateException();
                    }

                    M.Member temp = M.Member.Get(ds, mobile.ToString());
                    if (temp == null)
                    {
                        code = CommUtility.MEMBER_NOTFOUND;
                        throw new AggregateException();
                    }
                    temp.Password = Password;
                    if (temp.Update(ds, ColumnMode.Include, "Password") == DataStatus.Success)
                        return CommUtility.SUCCESS;
                    else
                    {
                        code = CommUtility.UPDATE_FAIL;
                        throw new AggregateException();
                    }
                }
                else
                {
                    code = CommUtility.MOBILE_FORMAT;
                    throw new AggregateException();
                }

            }
            catch (AggregateException)
            {
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }
        
        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="member"></param>
        /// <param name="password"></param>
        /// <param name="oldpassword"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object ModifyPassword(DataSource ds, M.Member member, string password, string oldpassword, out object data)
        {
            object code = null;
            data = null;
            try
            {
                if (oldpassword != member.Password)
                {
                    code = CommUtility.PASSWORD_EQUALS;
                    throw new AggregateException();
                }
                else
                {
                    member.Password = password;
                    if (member.Update(ds, ColumnMode.Include, "Password") == DataStatus.Success)
                        return CommUtility.SUCCESS;
                    else
                    {
                        code = CommUtility.UPDATE_FAIL;
                        throw new AggregateException();
                    }
                }
            }
            catch (AggregateException)
            {
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }

        }
        
        /// <summary>
        /// 设置支付密码
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="member"></param>
        /// <param name="mobile"></param>
        /// <param name="paypassword"></param>
        /// <param name="smscaptcha"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object SetPayPassword(DataSource ds, M.Member member, long mobile, string paypassword, string smscaptcha, out object data)
        {
            object code = null;
            data = null;
            try
            {
                if (CommUtility.CheckMobile(mobile))
                {
                    PassportSection section = PassportSection.GetSection();
                    if (section.VerifyMobile)
                    {
                        if (!V.MobileHash.Equals(ds, mobile, V.MobileHash.Password, smscaptcha))
                        {
                            code = CommUtility.SMSCAPTCHA_EQUALS;
                            throw new AggregateException();
                        }
                        if (member == null)
                        {
                            code = CommUtility.MEMBER_NOTFOUND;
                            throw new AggregateException();
                        }
                        if (new M.MemberInfo() { Id = member.Id, PayPassword = paypassword }.Update(ds, ColumnMode.Include, "PayPassword") == DataStatus.Success)
                            return CommUtility.SUCCESS;
                        else
                        {
                            code = CommUtility.UPDATE_FAIL;
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        if (member == null)
                        {
                            code = CommUtility.MEMBER_NOTFOUND;
                            throw new AggregateException();
                        }
                        if (new M.MemberInfo() { Id = member.Id, PayPassword = paypassword }.Update(ds, ColumnMode.Include, "PayPassword") == DataStatus.Success)
                            return CommUtility.SUCCESS;
                        else
                        {
                            code = CommUtility.UPDATE_FAIL;
                            throw new AggregateException();
                        }
                    }
                }
                else
                {
                    code = CommUtility.MOBILE_FORMAT;
                    throw new AggregateException();
                }
            }
            catch (AggregateException)
            {
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }
        
        /// <summary>
        /// 修改用户信息
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="member"></param>
        /// <param name="image">头像地址,不修改传空</param>
        /// <param name="nickname">昵称,不修改传空</param>
        /// <param name="sex">性别 0:未知,1:男,2:女,不修改传空</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object ModifyInfo(DataSource ds, M.Member member, string image, string nickname, string sex, out object data)
        {
            object code = null;
            data = null;
            try
            {

                DataColumn[] dc=new DataColumn[3];
                M.MemberInfo info = M.MemberInfo.GetById(ds, member.Id);
                if (!string.IsNullOrEmpty(image))
                    info.Image = image;
                if (!string.IsNullOrEmpty(nickname))
                    info.NickName = nickname;
                if (!string.IsNullOrEmpty(sex))
                    info.Sex = (M.MemberSex)int.Parse(sex);
                if (info.Update(ds, ColumnMode.Include, "NickName", "Image","Sex") == DataStatus.Success)
                    code = CommUtility.SUCCESS;
                else
                    code = (int)DataStatus.Success;
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }

        /// <summary>
        /// 修改绑定手机
        /// </summary>
        /// <param name="ds">数据源</param>
        /// <param name="member">用户</param>
        /// <param name="type">类别 0:绑定手机号,1:更改手机号</param>
        /// <param name="newmobile">新手机号码</param>
        /// <param name="paypassword">支付密码</param>
        /// <param name="newsmscaptcha">新手机验证码</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object ModifyMobile(DataSource ds, M.Member member, int type,long newmobile, string paypassword, string newsmscaptcha, out object data)
        {
            object code = null;
            data = null;
            try
            {
                PassportSection section = PassportSection.GetSection();
                if (section.VerifyMobile)
                {

                    if (member.Mobile != 0 && member.VerMob)
                    {
                        if (type == 1)
                        {
                            if (M.Member.Get(ds, newmobile.ToString()) != null)
                            {
                                code=CommUtility.USER_EXISTENCE;
                                throw new AggregateException();
                            }
                            if (string.IsNullOrEmpty(paypassword))
                            {
                                code=CommUtility.PASSWORD_EQUALS;
                                throw new AggregateException();
                            }
                            if (!M.MemberInfo.ApiCheckPayPassword(ds, member.Id, paypassword))
                            {
                                code=CommUtility.PASSWORD_EQUALS;
                                throw new AggregateException();
                            }
                            if (!V.MobileHash.Equals(ds, newmobile, V.MobileHash.Password, newsmscaptcha))
                            {
                                code=CommUtility.SMSCAPTCHA_EQUALS;
                                throw new AggregateException();
                            }
                            member.Mobile = newmobile;
                            member.VerMob = true;
                            if (member.Update(ds, ColumnMode.Include, "VerMob", "Mobile") == DataStatus.Success)
                                code = CommUtility.SUCCESS;
                            else
                                code = CommUtility.UPDATE_FAIL;
                            return code;
                        }
                        else
                        {
                            code=CommUtility.VERMOB_BIND;
                            throw new AggregateException();
                        }
                    }
                    else
                    {
                        if (type == 0)
                        {
                            if (M.Member.Get(ds, newmobile.ToString()) != null)
                            {
                                code=CommUtility.USER_EXISTENCE;
                                throw new AggregateException();
                            }
                            if (!V.MobileHash.Equals(ds, newmobile, V.MobileHash.Password, newsmscaptcha))
                            {
                                code=CommUtility.SMSCAPTCHA_EQUALS;
                                throw new AggregateException();
                            }
                            member.Mobile = newmobile;
                            member.VerMob = true;
                            if (member.Update(ds, ColumnMode.Include, "VerMob", "Mobile") == DataStatus.Success)
                                code = CommUtility.SUCCESS;
                            else
                                code = CommUtility.UPDATE_FAIL;
                            return code;
                        }
                        else
                        {
                            code=CommUtility.VERMOB_NOT;
                            throw new AggregateException();
                        }
                    }
                }
                else
                {
                    code = CommUtility.SMS_SEND;
                    throw new AggregateException();
                }
            }
            catch (AggregateException)
            {
                return code;
            }
            catch (Exception ex)
            {
                data = new { Message = ex.Message };
                return CommUtility.PROGRAM_ERROR;
            }
        }
    }
}
