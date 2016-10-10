using System;
using Cnaws;
using Cnaws.Data;
using Cnaws.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cnaws.Data.Query;

namespace XcpNet.Ad.Modules
{
    public class MachineVersion : IdentityModule
    {
        /// <summary>
        /// 名称
        /// </summary>
        [DataColumn(36)]
        public string Name = null;
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version = null;
        /// <summary>
        /// 版本修改时间
        /// </summary>
        public DateTime UpdateTime = DateTime.Now;

        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "Name", "Name");
        }

        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "Name");
        }


        public static MachineVersion GetVersionByName(DataSource ds,string name)
        {
            return Db<MachineVersion>.Query(ds)
                .Select().Where(W("Name", name))
                .First<MachineVersion>();
        }


    }
}
