using System;
using Cnaws.Web;
using Cnaws.Data;

namespace XcpNet.Web.Upload.Modules
{
    [Serializable]
    public sealed class Member : NoIdentityModule, IPassportUserInfo
    {
        [DataColumn(true)]
        public long Id = 0L;
        public int RoleId = 0;
        [DataColumn(36)]
        public string Mark = null;
        public Guid Token = Guid.Empty;
        public bool Approved = false;
        public bool Locked = false;

        #region IPassportUserInfo
        long IPassportUserInfo.Id
        {
            get { return Id; }
        }
        string IPassportUserInfo.Name
        {
            get { return string.Empty; }
        }
        long IPassportUserInfo.RoleId
        {
            get { return RoleId; }
        }
        DateTime IPassportUserInfo.CreationDate
        {
            get { return DateTime.MinValue; }
        }
        string IPassportUserInfo.LastIp
        {
            get { return string.Empty; }
        }
        DateTime IPassportUserInfo.LastTime
        {
            get { return DateTime.MinValue; }
        }
        long IPassportUserInfo.LoginCount
        {
            get { return 0; }
        }
        string IPassportUserInfo.UserData
        {
            get { return string.Empty; }
        }
        #endregion

        public override bool CanInstall
        {
            get { return false; }
        }

        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            return DataStatus.Failed;
        }
        protected override DataStatus OnUpdateBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            return DataStatus.Failed;
        }
        protected override DataStatus OnDeleteBefor(DataSource ds, ref DataColumn[] columns)
        {
            return DataStatus.Failed;
        }

        public static Member GetByToken(DataSource ds, Guid token)
        {
            return ExecuteSingleRow<Member>(ds, P("Token", token));
        }
    }
}
