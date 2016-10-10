﻿using System;
using A = XcpNet.Ad.Modules;

namespace XcpNet.Api.Controllers
{
    public class ApiVersion : CommonControllers
    {
        public void Get()
        {
            string mark;
            if (CheckMark(out mark))
            {
                try
                {
                    SetResult(A.MachineVersion.GetVersionByName(DataSource, Request["Name"]));
                }
                catch (Exception)
                {
                    SetResult(ApiUtility.PARAMETER_NOFOND);
                }
            }
        }
#if (DEBUG)
        public static void GetHelper()
        {
            CheckMarkHelper("ApiVersion", "Get", "获取版本信息")
                .AddArgument("Name", typeof(int), "APP名称")
                .AddResult(true, typeof(A.MachineVersion), "返回结果");
        }
#endif
    }
}
