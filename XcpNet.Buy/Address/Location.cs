using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XcpNet.Common.Address
{
    [Serializable]
    public sealed class Location
    {
        public int ProvinceId=0;
        public int CityId = 0;
        public int CountyId = 0;

        public string ProvinceTxt="";
        public string CityTxt="";
        public string CountyTxt = "";
    }
}
