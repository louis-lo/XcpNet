using Cnaws.Web;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XcpNet.Supplier.Controllers
{
    public class QRCode : SupplierController
    {
        [Distributor(true)]
        public void RecommendQRCode()
        {
            Render("recommendqrcode.html");
        }
        [Distributor(true)]
        public void GetRecommendQRCode()
        {
            long userId = User.Identity.Id;
            if (userId == 0)
            {
                string Token = Request["Token"];
                string Mark = Request["Mark"];
                if (!string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(Mark))
                {
                    Cnaws.Passport.Modules.Member member = Cnaws.Passport.Modules.Member.GetByToken(DataSource, new Guid(Token));
                    if (member.Mark == Mark)
                    {
                        userId = member.Id;
                    }
                }
                else
                {
                    NotFound();
                    return;
                }
            }
            string path = GetPassportUrl("/register") + "?ParentId =" + userId.ToString();
            Bitmap image = XcpNet.Supplier.QRCode.MarkQRCode.Create_ImgCode(path, 10);
            using (MemoryStream ms = new MemoryStream())
            {
                XcpNet.Supplier.QRCode.MarkQRCode.CombinImage(image, Server.MapPath("~/logo.png")).Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                Response.ClearContent();
                Response.ContentType = "image/png";
                Response.BinaryWrite(ms.ToArray());
            }
        }
    }
}
