using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D = XcpNet.Supplier.Modules.Modules;
namespace XcpNet.Supplier.Controllers
{
    public class DistributorCategory : SupplierController
    {
        [HttpAjax]
        public virtual void Child(int id)
        {
            if (IsAjax)
            {
                SetResult(D.DistributorCategory.GetAll(DataSource, id));
            }
            else
            {
                NotFound();
            }
        }
    }
}
