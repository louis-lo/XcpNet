using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XcpClient.Modules
{
    internal enum MediaType
    {
        Image = 1,
        Video = 2,
    }

    internal sealed class Media
    {
        public MediaType Type;
        public string Url;
        public int Time;

        public Media(MediaType type, string url, int time)
        {
            Type = type;
            Url = url;
            Time = time;
        }
    }
}
