using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using M = XcpNet.Supplier.Modules.Modules;
using Cnaws.Web.Templates;

namespace XcpNet.Supplier.Controllers
{
    public sealed class Courses : SupplierController
    {
        [Supplier(true)]
        public void Index()
        {
            IList<M.Courses> courses = M.Courses.GetAll(DataSource);
            this["Courses"] = courses;
            this["Mapping"] = new FuncHandler((args) =>
            {
                return M.CoursesMapping.Exists(DataSource, User.Identity.Id, (int)args[0]);
            });
            Render("courses.html");
        }
    }
}
