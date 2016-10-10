using Cnaws.Product.Modules;
using Cnaws.Web;
using System;
using System.Reflection;

namespace XcpNet.B2bShop
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DistributorAttribute : AuthorizeAttribute
    {
        private int[] _level;

        public DistributorAttribute(params int[] level)
            : this(false, level)
        {
        }
        public DistributorAttribute(bool redirect = false, params int[] level)
            : base(redirect)
        {
            _level = level;
        }

        public override bool IsValidForRequest(Controller controller, MethodInfo methodInfo)
        {
            Common.CommoSupplierController ctl = controller as Common.CommoSupplierController;
            if (ctl != null)
            {
                if (base.IsValidForRequest(controller, methodInfo))
                {
                    if (ctl.IsDistributor())
                    {
                        if (_level == null || _level.Length == 0)
                            return true;
                        Distributor ins = ctl.Distributor;
                        if (ins != null)
                            return Array.IndexOf(_level, ins.Level) != -1;
                    }
                }
            }
            return false;
        }
    }
}
