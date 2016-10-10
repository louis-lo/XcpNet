using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Json;
using Cnaws.Area;

namespace XcpNet.Supplier.Modules.Modules
{
    [Serializable]
    public sealed class ReturnAddress : Cnaws.Passport.Modules.ShippingAddress
    {

        protected override DataStatus OnInsertBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (UserId <= 0)
                return DataStatus.Failed;
            if (IsDefault)
                (new ReturnAddress() { UserId = long.MinValue, IsDefault = false }).Update(ds, ColumnMode.Include, Cs("IsDefault"), WN("IsDefault", true, "Value1") & WN("UserId", UserId, "Value2"));
            return DataStatus.Success;
        }
        protected override DataStatus OnUpdateBefor(DataSource ds, ColumnMode mode, ref DataColumn[] columns)
        {
            if (UserId == long.MinValue)
                return DataStatus.Success;
            if (UserId <= 0)
                return DataStatus.Failed;
            if (IsDefault)
                (new ReturnAddress() { UserId = long.MinValue, IsDefault = false }).Update(ds, ColumnMode.Include, Cs("IsDefault"), WN("IsDefault", true, "Value1") & WN("UserId", UserId, "Value2"));
            return DataStatus.Success;
        }

        public new DataStatus Modify(DataSource ds)
        {
            return Update(ds, ColumnMode.Exclude, Cs("UserId"), P("Id", Id));
        }

        public static new ReturnAddress GetById(DataSource ds, long id, long userId)
        {
            return ExecuteSingleRow<ReturnAddress>(ds, P("UserId", userId) & P("Id", id));
        }
        public static new IList<ReturnAddress> GetAll(DataSource ds, long userId)
        {
            return ExecuteReader<ReturnAddress>(ds, Os(Od("IsDefault"), Od("Id")), P("UserId", userId));
        }
        public static new DataStatus Remove(DataSource ds, long id, long userId)
        {
            return (new ReturnAddress() { Id = id, UserId = userId }).Delete(ds, "UserId", "Id");
        }
        public static new DataStatus  SetDefault(DataSource ds, long id, long userId)
        {
            return (new ReturnAddress() { Id = id, IsDefault = true, UserId = userId }).Update(ds, ColumnMode.Include, "IsDefault");
        }
    }
}
