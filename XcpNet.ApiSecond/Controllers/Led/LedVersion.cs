using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Passport;
using XDG = XcpNet.Supplier.Modules.Modules;
using Cnaws.Data;
using Pd = Cnaws.Product.Modules;
using Cnaws.Product;
using Cnaws.Web;

namespace XcpNet.ApiSecond.Controllers
{
    public class LedVersion2 : Version2
    {
        protected override void OnInitController()
        {

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
