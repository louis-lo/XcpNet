using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Web;
using Cnaws.Templates;
using XcpNet.Common;

namespace XcpNet.Website.Controllers.Extension
{
    public class Index : CommonController
    {
        public void index()
        {
            Render("index.html");
        }
    }
}
