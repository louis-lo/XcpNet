using System;
using System.Web;
using System.Collections.Generic;
using M = Cnaws.Passport.Modules;
using Cnaws.Web.Controllers;
using Cnaws.Data;
using Cnaws.Web;

namespace XcpNet.Resources.Controllers
{
    public sealed class ApiFileManager : FileSystem
    {
        public const int SIGN_ERROR = -1039;//验证验名失败
        public const int ERROR_MARK_EMPTY = -1003; //标识为空
        public const int ERROR_TOKEN_EMPTY = -1004; //Token为空
        public const int ERROR_MEMBER_NOTFOUND = -1005; //未找到用户
        public const int ERROR_MARK_EQUALS = -1006; //标识错误
        public const int ERROR_TOKEN_EQUALS = -1007; //token错误
        public const int ERROR_MEMBER_APPROVED = -1009; //用户未审核
        public const int ERROR_MEMBER_LOCKED = -1010; //用户已锁定

        private DataSource _ds;
        public ApiFileManager()
        {
            _ds = null;
        }
        private string DbConnStr
        {
            get { return "LocalSqlServer"; }
        }
        public DataSource DataSource
        {
            get
            {
                if (_ds == null)
                    _ds = new DataSource(DbConnStr);
                return _ds;
            }
        }
        public void InitController(ApiFileManager controller)
        {
            base.InitController(controller);
            if (controller._ds != null)
                _ds = new DataSource(controller._ds);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_ds != null)
                {
                    _ds.Dispose();
                    _ds = null;
                }
            }
        }

        private bool CheckToken(out Guid token, out M.Member member)
        {
            if (!Guid.TryParse(Request["token"], out token) || Guid.Empty.Equals(token))
            {
                member = null;
                SetResult(ERROR_TOKEN_EMPTY);
                return false;
            }
            member = M.Member.GetByToken(DataSource, token);
            if (member == null)
            {
                SetResult(ERROR_MEMBER_NOTFOUND);
                return false;
            }
            string mark = Request["mark"];
            if (string.IsNullOrEmpty(mark))
            {
                SetResult(ERROR_MARK_EMPTY);
                return false;
            }
            if (!string.Equals(mark, member.Mark))
            {
                SetResult(ERROR_MARK_EQUALS);
                return false;
            }
            if (!token.Equals(member.Token))
            {
                SetResult(ERROR_TOKEN_EQUALS);
                return false;
            }
            return true;
        }

        private bool CheckMember(out M.Member member)
        {
            Guid token;
            bool ret = CheckToken(out token, out member);
            if (ret)
            {
                if (member.Approved)
                {
                    if (!member.Locked)
                    {
                        return true;
                    }
                    else
                    {
                        SetResult(ERROR_MEMBER_LOCKED);
                    }
                }
                else
                {
                    SetResult(ERROR_MEMBER_APPROVED);
                }
            }
            else
            {
                member = null;
            }
            return ret;
        }

        protected override string GetDirectory(long value)
        {
            byte[] array = BitConverter.GetBytes(value);
            List<byte> list = new List<byte>(16);
            list.AddRange(array);
            list.AddRange(array);
            return new Guid(list.ToArray()).ToString("N");
        }
        protected override bool CheckRight(Arguments args = null)
        {
            M.Member member;
            if (CheckMember(out member))
            {
                PassportAuthentication.SetAuthCookie(true, false, member, Context);
                if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                    return true;
            }
            return false;
        }
        protected override void RenderUploadResult(HttpContext context, upload_result result)
        {
            SetResult(result);
        }
        protected override void RenderFileManagerResult(HttpContext context, file_manager_result result)
        {
            SetResult(result);
        }
    }
}
