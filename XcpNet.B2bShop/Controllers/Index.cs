using System;
using System.Text;
using Cnaws.Json;
using Cnaws.Web;
using XcpNet.Common;
using XcpNet.Common.Address;

namespace XcpNet.B2bShop.Controllers.Extension
{
    public class Index : CommoSupplierController
    {
        protected override Location GetLocal()
        {
            Location local = new Location();
            string val = Request.Cookies[locationCookieName]?.Value;

            if (!string.IsNullOrEmpty(val))
            {
                local = JsonValue.Deserialize<Location>(
                    Encoding.UTF8.GetString(PassportAuthentication.DecodeCookie(val)));
            }
            else
            {
                local = SetLocation(Distributor.Province, Distributor.City, Distributor.County);
            }
            return local;
        }
        [Authorize(true)]
        [Distributor(true)]
        public void index()
        {
            if (Distributor.State == Cnaws.Product.Modules.DistributorState.NotApproved && IsWap)
            {
                Refresh("http://wapsupplier.xcpnet.com/joinus/wait.html");
            }
            else
            {
                Render("index.html");
            }
        }
    }
}
