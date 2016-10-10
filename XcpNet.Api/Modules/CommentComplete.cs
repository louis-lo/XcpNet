using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Comment.Modules;

namespace XcpNet.Api.Modules
{
    public sealed class CommentComplete : Comment
    {
        public string KeyWords = null;

        public string Images = null;

        public override bool CanInstall
        {
            get
            {
                return false;
            }
        }
    }
}
