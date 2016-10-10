using System;
using Cnaws.Web;
using Cnaws.Data;
using System.Collections.Generic;
using Cnaws.Data.Query;
using XcpNet.Ad.Modules;
using System.Text;
using P = Cnaws.Product.Modules;

namespace XcpNet.ApiSecond.Controllers
{
    public sealed class MachineView : DataController
    {
        public void Index()
        {
            IList<dynamic> list = Db<MachineCode>.Query(DataSource)
                .Select(
                    //new DbSelectAs<MachineCode>("Code"),
                    //new DbSelectAs<MachineCode>("MemberId"),
                    new DbSelectAs<P.Distributor>("Company"),
                    new DbSelectAs<P.Distributor>("Signatories"),
                    new DbSelectAs<P.Distributor>("SignatoriesPhone"),
                    new DbSelectAs<MachineCode>("LastTime"),

                    new DbSelect<MachineCode>("LastTime")
                )
                .InnerJoin(new DbColumn<MachineCode>("MemberId"), new DbColumn<P.Distributor>("UserId"))
                .OrderBy(new DbOrderBy<MachineCode>("LastTime", DbOrderByType.Desc))
                .ToList();

            DateTime now = DateTime.Now;

            StringBuilder sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE html>
<html lang=""zh"">
<head>
    <meta charset=""UTF-8"">
    <title>购物机统计</title>
</head>
<body>
<table style=""width:100%"">
    <tr>
        <td>公司名</td>
        <td>签约人</td>
        <td>签约人电话</td>
        <td>最后活动时间</td>
        <td>状态</td>
    </tr>
");
            foreach (dynamic item in list)
            {
                sb.AppendFormat(@"
    <tr>
        <td>{0}</td>
        <td>{1}</td>
        <td>{2}</td>
        <td>{3}</td>
        <td>{4}</td>
    </tr>
", item.Company, item.Signatories, item.SignatoriesPhone, item.LastTime, item.LastTime.AddMinutes(5) < now ? @"<span style=""color:red"">离线</span>" : @"<span style=""color:green"">在线</span>");
            }
            sb.Append(@"
</table>
</body>
</html>");

            Response.Write(sb.ToString());
            End();
        }
    }
}
