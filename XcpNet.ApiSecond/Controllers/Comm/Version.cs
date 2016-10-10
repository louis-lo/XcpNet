using System;
using A = XcpNet.Ad.Modules;
using XcpNet.Common;

namespace XcpNet.ApiSecond.Controllers
{
    public class Version2 : CommControllers2
    {
        public static string ClassName = "[type]Version2";
        protected override void OnInitController()
        {
            NotFound();
        }
        public void Get()
        {
            string mark;
            if (CheckMark(out mark))
            {
                try
                {
                    try
                    {
                        A.MachineCode.UpdateOnline(DataSource, mark);
                    }
                    catch (Exception) { }

                    SetResult(A.MachineVersion.GetVersionByName(DataSource, Request["Name"]));
                }
                catch (Exception)
                {
                    SetResult(CommUtility.PARAMETER_NOFOND);
                }
            }
        }
#if (DEBUG)
        public static void GetHelper()
        {
            CheckMarkApi(ClassName, "Get", "获取版本信息")
                .AddArgument("Name", typeof(int), "APP名称")
                .AddResult(true, typeof(A.MachineVersion), "返回结果");
        }
#endif
    }
}
