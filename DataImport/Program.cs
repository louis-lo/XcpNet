using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Cnaws.Data;
using XcpNet.Supplier.Modules.Modules;
using Cnaws.Data.Query;
using Cnaws.Html;

namespace DataImport
{
    class Program
    {
        static void Main(string[] args)
        {
            //char a = 'A';
            //char t = 'Z';
            //char r = 'a';
            //char n = 'z';
            //char x = '0';
            //char y = '9';

            string s;
            HtmlElement e;
            HtmlElementCollection c;
            //HtmlAttributeCollection a;
            using (HtmlDocument doc = new HtmlDocument())
            {
                doc.LoadHtml(@"<html><head></head><body class='c' style='width:100%'><div><div><div><div></div</div</div</div></body></html>");
                c = doc.All;
                e = doc.Body;
                s = doc.DefaultEncoding;
                s = doc.Encoding;
                c = doc.Images;
                c = doc.Links;
                s = doc.Title;
                e = doc.DocumentElement;

                c = doc.Body.All;
                //a = doc.Body.Attributes;
                c = doc.Body.Children;
                e = doc.Body.FirstChild;
                e = doc.Body.LastChild;
                s = doc.Body.Id;
                s = doc.Body.InnerHtml;
                s = doc.Body.InnerText;
                e = doc.Body.PreviousSibling;
                e = doc.Body.NextSibling;
                s = doc.Body.OuterHtml;
                s = doc.Body.OuterText;
                s = doc.Body.Style;
                s = doc.Body.TagName;

                //doc.Body.Attributes.GetEnumerator();

                doc.Close();
            }

            //using (DataSource ds = new DataSource("LocalSqlServer"))
            //{
            //    IList<TempTable> temp = Db<TempTable>.Query(ds)
            //        .Select()
            //        .ToList<TempTable>();
            //    foreach (TempTable item in temp)
            //    {
            //        if (item.商品名称 != null)
            //        {
            //            DistributorProduct p = new DistributorProduct();

            //        }
            //    }
            //}
        }
    }
}
