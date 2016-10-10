using System;
using Cnaws;
using Cnaws.Web;
using Cnaws.Data;
using Cnaws.Web.Controllers;
using System.Web;
using System.Collections.Generic;
using M = Cnaws.Passport.Modules;
using System.Collections.Specialized;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace XcpNet.Resources.Controllers
{
    public sealed class wapFileManager : FileSystem
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
        public wapFileManager()
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
        public void InitController(wapFileManager controller)
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
            string token = Request.QueryString["token"];
            if (!string.IsNullOrEmpty(token))
                PassportAuthentication.SetAuthToken(token, Context);

            if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                return true;

            if (args != null && args.Count > 0)
            {
                token = string.Join("/", args.ToArray());
                PassportAuthentication.SetAuthToken(token, Context);
                if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                    return true;
            }

            Unauthorized();
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
