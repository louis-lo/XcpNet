using Cnaws.Web;
using System;
using System.Reflection;

namespace XcpNet.Supplier
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SupplierOrDistributorAttribute : AuthorizeAttribute
    {
        public SupplierOrDistributorAttribute(bool redirect = false)
            : base(redirect)
        {
        }

        public override bool IsValidForRequest(Cnaws.Web.Controller controller, MethodInfo methodInfo)
        {
            Common.CommoSupplierController ctl = controller as Common.CommoSupplierController;
            if (ctl != null)
            {
                if (base.IsValidForRequest(controller, methodInfo))
                    return ctl.IsSupplier() || ctl.IsDistributor();
            }
            return false;
        }
    }
}
