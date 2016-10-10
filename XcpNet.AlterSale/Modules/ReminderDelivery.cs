using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Web;
using Cnaws;
using Cnaws.Templates;
using Cnaws.Data;
using Cnaws.Data.Query;

namespace XcpNet.AfterSales.Modules
{
    public class ReminderDelivery : LongIdentityModule
    {
        /// <summary>
        /// 订单号
        /// </summary>
        [DataColumn(64)]
        public string OrderId = null;
        /// <summary>
        /// 会员编号
        /// </summary>
        public long UserId = 0L;
        /// <summary>
        /// 会员名称
        /// </summary>
        public string UserName = null;
        /// <summary>
        /// 供应商编号
        /// </summary>
        public long SupplierId = 0L;
        /// <summary>
        /// 供应商名称
        /// </summary>
        public string SupplierName = null;
        /// <summary>
        /// 是否已读
        /// </summary>
        public bool IsRead = false;
        /// <summary>
        /// 提醒时间
        /// </summary>
        public DateTime RemindTime = (DateTime)Types.GetDefaultValue(TType<DateTime>.Type);
        /// <summary>
        /// 提醒次数
        /// </summary>
        public int ReminderCount = 0;

        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "OrderId", "OrderId");
            CreateIndex(ds, "UserId", "UserId");
            CreateIndex(ds, "SupplierId", "SupplierId");
            CreateIndex(ds, "IsRead", "IsRead");
            CreateIndex(ds, "RemindTime", "UserId", "RemindTime", "SupplierId");
        }

        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "OrderId");
            DropIndex(ds, "UserId");
            DropIndex(ds, "SupplierId");
            DropIndex(ds, "IsRead");
            DropIndex(ds, "RemindTime");
        }

        public static DataStatus SetRemind(DataSource ds,long userid,long supplierid,string orderid)
        {
            ReminderDelivery reminderDelivery = Db<ReminderDelivery>.Query(ds).Select().Where(W("OrderId", orderid)).First<ReminderDelivery>();
            if (reminderDelivery == null)
            {
                Cnaws.Passport.Modules.Member UserInfo = Cnaws.Passport.Modules.Member.GetById(ds, userid);
                Cnaws.Passport.Modules.Member SupplierInfo = Cnaws.Passport.Modules.Member.GetById(ds, supplierid);
                DataStatus result = new ReminderDelivery()
                {
                    OrderId = orderid,
                    UserId = userid,
                    UserName = UserInfo.Name,
                    SupplierId = supplierid,
                    SupplierName = SupplierInfo.Name,
                    IsRead = false,
                    RemindTime = DateTime.Now,
                    ReminderCount = 1
                }.Insert(ds);
                return result;
            }
            else
            {
                int result = Db<ReminderDelivery>.Query(ds).Update().Set(new DbColumn("ReminderCount"), reminderDelivery.ReminderCount + 1).Where(W("OrderId", orderid)&W("RemindTime",DateTime.Parse(DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")+ " 00:00:00"),DbWhereType.GreaterThan)).Execute();
                if (result > 0)
                {
                    return DataStatus.Success;
                }
                else
                {
                    return DataStatus.Failed;
                }
            }
        }

        public static ReminderDelivery GetReminderDeliveryByOrderIdAndSupplierId(DataSource ds, string orderId, long supplierId)
        {
            return Db<ReminderDelivery>.Query(ds).Select().Where(W("OrderId", orderId) & W("SupplierId", supplierId)).First<ReminderDelivery>();
        }
    }
}
