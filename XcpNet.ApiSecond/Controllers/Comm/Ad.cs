﻿using System;
using System.Collections.Generic;
using M = XcpNet.Ad.Modules;
using Pd = Cnaws.Product.Modules;
using XcpNet.Common;
namespace XcpNet.ApiSecond.Controllers
{
    public class Ad2 : CommControllers2
    {
        public static string ClassName= "[type]Ad2";
        protected override void OnInitController()
        {
            NotFound();
        }
        /// <summary>
        /// 根据tag获取广告
        /// </summary>
        public void GetByTag()
        {
            string mark;
            if (CheckMark(out mark))
            {
                try
                {
                    Pd.Distributor distributor = new Cnaws.Product.Modules.Distributor();
                    if (CheckDistributor(out distributor) && distributor != null)
                    {
                        int Tag = 0;
                        int Type = 1;
                        if (!string.IsNullOrEmpty(Request["tag"]) && !string.IsNullOrEmpty(Request["type"]))
                        {
                            Tag = int.Parse(Request["tag"]);
                            Type = int.Parse(Request["type"]);
                            SetResult(M.Advertisement.GetByLabel(DataSource, Tag, Type, distributor.Province, distributor.City, distributor.County));
                        }
                        else
                        {
                            SetResult(CommUtility.PARAMETER_NOFOND);
                        }
                    }
                    else
                    {
                        int Tag = 0;
                        int Type = 1;
                        if (!string.IsNullOrEmpty(Request["tag"]) && !string.IsNullOrEmpty(Request["type"]))
                        {
                            Tag = int.Parse(Request["tag"]);
                            Type = int.Parse(Request["type"]);
                            SetResult(M.Advertisement.GetByLabel(DataSource, Tag, Type, 0, 0, 0));
                        }
                        else
                        {
                            SetResult(CommUtility.PARAMETER_NOFOND);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetResult(false,new { Message=ex.ToString()});
                }
            }
        }
#if (DEBUG)
        public static void GetByTagHelper()
        {
            CheckMarkApi(ClassName, "GetByTag", "根据tag获取广告")
                .AddArgument("tag", typeof(int), "标签编号")
                .AddArgument("type", typeof(int), "类型：1.Banner  2.轮播广告  3.促销广告")
                .AddResult(true, typeof(IList<M.Advertisement>), "广告列表");
        }
#endif
    }
}