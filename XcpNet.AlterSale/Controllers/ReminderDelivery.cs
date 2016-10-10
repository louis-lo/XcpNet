using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using P = Cnaws.Product.Modules;
using SM = XcpNet.Supplier.Modules.Modules;

namespace XcpNet.AfterSales.Controllers
{
    public class ReminderDelivery: DataController
    {
        public void Reminder(string orderId)
        {
            P.ProductOrder order = P.ProductOrder.GetById(DataSource, orderId);
            if (order != null && order.UserId == User.Identity.Id)
            {
                DataStatus status = Modules.ReminderDelivery.SetRemind(DataSource, order.UserId, order.SupplierId, order.Id);
                SetResult(status);
            }
        }
        /// <summary>
        /// 加盟商提醒发货
        /// </summary>
        /// <param name="orderId"></param>
        public void DistributorReminder(string orderId)
        {
            SM.DistributorOrder order = SM.DistributorOrder.GetById(DataSource, orderId);
            if (order != null && order.UserId == User.Identity.Id)
            {
                DataStatus status = Modules.ReminderDelivery.SetRemind(DataSource, order.UserId, order.SupplierId, order.Id);
                SetResult(status);
            }
        }
    }
}
