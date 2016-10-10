using System;
using Cnaws.Data;
using C = Cnaws.Comment.Modules;
using P = Cnaws.Product.Modules;
using M = Cnaws.Passport.Modules;
using Cnaws.Web;
using System.Web;
using System.Collections.Generic;
using Cnaws.Data.Query;
using XcpNet.Common;
namespace XcpNet.ApiSecond.Controllers
{
    public class ShippingAddress2 : CommControllers2
    {
        public static string ClassName = "[type]ShippingAddress2";
        protected override void OnInitController()
        {
            NotFound();
        }
        /// <summary>
        /// 获取所有收货地址
        /// </summary>
        public void GetAllShippingAddress()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                SetResult(M.ShippingAddress.GetAll(DataSource, member.Id));
            }
        }
#if (DEBUG)
        public static void GetAllShippingAddressHelper()
        {
            CheckMemberApi(ClassName, "GetAllShippingAddress", "获取所有收货地址")
                .AddResult(true, typeof(IList<M.ShippingAddress>), "产品列表");
        }
#endif
        [HttpPost]
        public void SetShippingAddress()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    M.ShippingAddress address = DbTable.Load<M.ShippingAddress>(Request.Form);
                    address.UserId = member.Id;
                    SetResult(address.Update(DataSource));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void SetShippingAddressHelper()
        {
            CheckMemberApi(ClassName, "SetShippingAddress", "编辑收货地址")
                .AddArgument("Id", typeof(long), "编号")
                .AddArgument("Consignee", typeof(string), "收货人姓名")
                .AddArgument("Mobile", typeof(long), "联系手机号码")
                .AddArgument("PostId", typeof(int), "邮政编码")
                .AddArgument("Province", typeof(int), "省ID")
                .AddArgument("City", typeof(int), "市ID")
                .AddArgument("County", typeof(int), "区ID")
                .AddArgument("Address", typeof(string), "具体地址")
                .AddArgument("IsDefault", typeof(bool), "是否默认收货地址")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif
        [HttpPost]
        public void AddShippingAddress()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    M.ShippingAddress address = DbTable.Load<M.ShippingAddress>(Request.Form);
                    address.UserId = member.Id;
                    SetResult(address.Insert(DataSource));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void AddShippingAddressHelper()
        {
            CheckMemberApi(ClassName, "AddShippingAddress", "添加收货地址")
                .AddArgument("Consignee", typeof(string), "收货人姓名")
                .AddArgument("Mobile", typeof(long), "联系手机号码")
                .AddArgument("PostId", typeof(int), "邮政编码")
                .AddArgument("Province", typeof(int), "省ID")
                .AddArgument("City", typeof(int), "市ID")
                .AddArgument("County", typeof(int), "区ID")
                .AddArgument("Address", typeof(string), "具体地址")
                .AddArgument("IsDefault", typeof(bool), "是否默认收货地址")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif
        [HttpPost]
        public void DelShippingAddress()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    SetResult(new M.ShippingAddress() { Id = long.Parse(Request["Id"]), UserId = member.Id }.Delete(DataSource));
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void DelShippingAddressHelper()
        {
            CheckMemberApi(ClassName, "DelShippingAddress", "删除收货地址")
                .AddArgument("Id", typeof(string), "收货地址Id")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(DataStatus), "处理结果");
        }
#endif
    }
}
