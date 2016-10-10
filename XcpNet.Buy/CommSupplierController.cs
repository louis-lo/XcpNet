using Cnaws.Web;
using System.Web;
using M = Cnaws.Product.Modules;
using P = Cnaws.Passport.Modules;

namespace XcpNet.Common
{
    public abstract class CommoSupplierController : CommPassportController
    {
        private long _memberId;
        private bool _isAdmin;
        private P.MemberInfo _member;
        private M.Supplier _supplier;
        private bool _isSupplier;
        private M.Distributor _distributor;
        private bool _isDistributor;

        protected CommoSupplierController()
        {
            _memberId = 0L;
            _isAdmin = false;
            _supplier = null;
            _isSupplier = false;
            _distributor = null;
            _isDistributor = false;
        }

        protected override void OnInitController()
        {
            _isAdmin = User != null && User.Identity != null && User.Identity.IsAuthenticated && User.Identity.IsAdmin;
            if (!_isAdmin)
            {
                _member = P.MemberInfo.GetById(DataSource, User.Identity.Id);
                _supplier = M.Supplier.GetById(DataSource, User.Identity.Id);
                _isSupplier = XcpUtility.GetValue(PassportAuthentication.GetCustomCookie(XcpUtility.SupplierCookieName, Context)).Key;
                _distributor = M.Distributor.GetById(DataSource, User.Identity.Id);
                _isDistributor = XcpUtility.GetValue(PassportAuthentication.GetCustomCookie(XcpUtility.SupplierCookieName, Context)).Value;
                if (_isDistributor)
                    CommSetLocation(_distributor.Province, _distributor.City, _distributor.County);
                this["Member"] = _member;
                this["HomeUrl"] = GetUrl("/");
                if (IsSupplier())
                    this["Supplier"] = Supplier;
                if (IsDistributor())
                    this["Distributor"] = Distributor;
            }
        }

        public long MemberId
        {
            get { return _memberId; }
            set
            {
                _memberId = value;
                if (_isAdmin)
                {
                    _member = P.MemberInfo.GetById(DataSource, MemberId);
                    _supplier = M.Supplier.GetById(DataSource, MemberId);
                    _isSupplier = _supplier != null;
                    _distributor = M.Distributor.GetById(DataSource, MemberId);
                    _isDistributor = _distributor != null;
                    if (_isDistributor)
                        CommSetLocation(_distributor.Province, _distributor.City, _distributor.County);
                    this["Member"] = _member;
                    this["HomeUrl"] = GetUrl("/index/admin/", MemberId.ToString());
                    if (IsSupplier())
                        this["Supplier"] = Supplier;
                    if (IsDistributor())
                        this["Distributor"] = Distributor;
                }
            }
        }
        public P.MemberInfo Member
        {
            get { return _member; }
        }
        public bool IsAdmin
        {
            get { return _isAdmin; }
        }
        public bool IsSupplier()
        {
            return _isSupplier && _supplier != null;
        }
        public bool IsDistributor()
        {
            return _isDistributor && _distributor != null;
        }
        public bool IsSupplierOrDistributor()
        {
            return IsSupplier() || IsDistributor();
        }

        public M.Supplier Supplier
        {
            get { return _supplier; }
        }
        public M.Distributor Distributor
        {
            get { return _distributor; }
        }

        protected override void Unauthorized(bool redirect = false)
        {
            if (IsHtml())
            {
                if (User != null && User.Identity != null && User.Identity.Id > 0)
                {
                    if (IsWap&&!IsSupplier())
                        Redirect("http://wapsupplier.xcpnet.com/joinus.html");
                    else if (IsWap && IsSupplier())
                    {
                        Render("errors/code/notauthorized.html");
                    }
                    else
                    {
                        this["LogoutUrl"] = string.Concat(GetPassportUrl("/logout"), "?target=", HttpUtility.UrlEncode(string.Concat(GetPassportUrl("/login"), "?target=", Request.Url.ToString())));
                        Render("errors/code/notauthorized.html");
                    }
                }
                else
                {
                    Redirect(string.Concat(GetPassportUrl("/logout"), "?target=", HttpUtility.UrlEncode(string.Concat(GetPassportUrl("/login"), "?target=", Request.Url.ToString()))), false);
                }
                //Response.Write(string.Concat("<script type=\"text/javascript\">alert('权限不足');window.location.href='", GetPassportUrl("/logout"), "?target=", HttpUtility.UrlEncode(string.Concat(GetPassportUrl("/login"), "?target=", Request.Url.ToString())), "';</script>"));
            }

            else
                SetResult(-401);
        }
    }
}
