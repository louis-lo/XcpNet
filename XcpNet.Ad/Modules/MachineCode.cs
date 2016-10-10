using System;
using Cnaws;
using Cnaws.Data;
using Cnaws.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using M = Cnaws.Passport.Modules;
using Cnaws.Data.Query;
using Pd = Cnaws.Product.Modules;

namespace XcpNet.Ad.Modules
{
    public class MachineCode : NoIdentityModule
    {
        /// <summary>
        /// 机器码
        /// </summary>
        [DataColumn(true, 36)]
        public string Code = null;
        /// <summary>
        /// 会员Id
        /// </summary>
        public long MemberId = 0;
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime = DateTime.Now;
        /// <summary>
        /// 最后上线时间
        /// </summary>
        public DateTime LastTime = DateTime.Now;

        protected override void OnInstallAfter(DataSource ds)
        {
            CreateIndex(ds, "Code", "Code");
            CreateIndex(ds, "MemberId", "MemberId");
            CreateIndex(ds, "CodeAndMemberId", "Code", "MemberId");
        }

        protected override void OnInstallBefor(DataSource ds)
        {
            DropIndex(ds, "Code");
            DropIndex(ds, "MemberId");
            DropIndex(ds, "CodeAndMemberId");
        }

        public static void UpdateOnline(DataSource ds, string code)
        {
            (new MachineCode() { Code = code, LastTime = DateTime.Now }).Update(ds, ColumnMode.Include, "LastTime");
        }
        
        public static long Add(DataSource ds, string code, string username, string passport)
        {
            M.Member member = M.Member.Get(ds, username);
            if (member == null)
                return -1005;
            if (member.Password != passport)
                return -1011;
            if (Pd.Distributor.IsDistributor(ds, member.Id))
            {
                MachineCode machinecode = new MachineCode() { MemberId = member.Id, Code = code };
                if (machinecode.Insert(ds) == DataStatus.Success) {
                    return machinecode.MemberId;
                }
                else
                {
                    machinecode = GetByCode(ds, code);
                    if (machinecode.MemberId == member.Id)
                        return member.Id;
                    else
                    {
                        machinecode.MemberId = member.Id;
                        machinecode.Update(ds, ColumnMode.Include, "MemberId");
                        return machinecode.MemberId;
                    }
                }
            }
            else
            return -1036;
        }

        public static MachineCode GetByCode(DataSource ds,string code)
        {
            return Db<MachineCode>.Query(ds)
                .Select()
                .Where(W<MachineCode>("Code", code))
                .First<MachineCode>();
        }

        public static Pd.Distributor GetDistributorByCode(DataSource ds, string code)
        {
            return Db<Pd.Distributor>.Query(ds)
                .Select(S<Pd.Distributor>("*"))
                .InnerJoin(O<Pd.Distributor>("UserId"),O<MachineCode>("MemberId"))
                .Where(W<MachineCode>("Code", code))
                .First<Pd.Distributor>();
        }
    }
}
