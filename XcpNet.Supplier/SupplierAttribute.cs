using Cnaws.Web;
using System;
using System.Reflection;

namespace XcpNet.Supplier
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SupplierAttribute : AuthorizeAttribute
    {
        public SupplierAttribute(bool redirect = false)
            : base(redirect)
        {
        }

        public override bool IsValidForRequest(Cnaws.Web.Controller controller, MethodInfo methodInfo)
        {
            Common.CommoSupplierController ctl = controller as Common.CommoSupplierController;
            if (ctl != null)
            {
                if (base.IsValidForRequest(controller, methodInfo))
                    return ctl.IsSupplier();
            }
            return false;
        }
    }
}
