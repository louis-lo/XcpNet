using System;
using Cnaws.Web;
using Cnaws.Data;
using M = Cnaws.Passport.Modules;
using P = Cnaws.Product.Modules;
using Cnaws.Pay;
using System.Collections.Generic;
using D = XcpNet.Supplier.Modules.Modules;
using XcpNet.Common;
using A = XcpNet.AfterSales.Modules;
using System.Web;
using Cnaws.Data.Query;
using C = Cnaws.Comment.Modules;
using Cnaws.Json;
using Cnaws;

namespace XcpNet.ApiSecond.Controllers
{
    public class Distributor2 : CommControllers2
    {
        protected override bool CheckProvider(PayProvider provider)
        {
            return true;
        }
        public static string ClassName = "[type]Distributor2";
        protected override void OnInitController()
        {
            NotFound();
        }
        public void GetUntreatedSum()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    long paymentsum = D.DistributorOrder.GetMyCountByState(DataSource, P.OrderState.Payment, member.Id);
                    long outwarehousesum = D.DistributorOrder.GetMyCountByState(DataSource, P.OrderState.OutWarehouse, member.Id);
                    long deliverysum = D.DistributorOrder.GetMyCountByState(DataSource, P.OrderState.Delivery, member.Id);
                    long receiptsum = D.DistributorOrder.GetMyCountByState(DataSource, P.OrderState.Receipt, member.Id);
                    long evaluationsum = Db<C.Comment>.Query(DataSource)
                                 .Select(new DbSelect<C.Comment>("*"), new DbSelect<D.DistributorOrderMapping>("*"), new DbSelect<D.DistributorOrder>("ReceiptDate"))
                                 .RightJoin(new DbColumn<C.Comment>("TargetId"), new DbColumn<D.DistributorOrderMapping>("ProductId"))
                                 .InnerJoin(new DbColumn<D.DistributorOrderMapping>("OrderId"), new DbColumn<D.DistributorOrder>("Id"))
                                 .Where(new DbWhere<D.DistributorOrder>("UserId", member.Id) & new DbWhere<D.DistributorOrderMapping>("Evaluation", false) & new DbWhere<D.DistributorOrder>("State", P.OrderState.Finished))
                                 .Count();//D.DistributorOrder.GetMyCountByState(DataSource, P.OrderState.Evaluation, member.Id);
                    long cartsum = D.DistributorCart.GetCountByUser(DataSource, member.Id);

                    SetResult(new
                    {
                        PaymentSum = paymentsum,
                        OutWarehouseSum = outwarehousesum,
                        DeliverySum = deliverysum,
                        ReceiptSum = receiptsum,
                        EvaluationSum = evaluationsum,
                        CartSum = cartsum
                    });
                }
            }
            catch (Exception)
            {
                SetResult(false);
            }
        }
#if (DEBUG)
        public static void GetUntreatedSumHelper()
        {
            CheckMemberApi(ClassName, "GetUntreatedSum", "获取加盟商未处理的数量")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(true, typeof(string), "处理结果");
        }
#endif
        public void GetOrdersByChildDistributor()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    long parentid = 0;
                    if (!long.TryParse(Request["parentid"], out parentid) || parentid <= 0)
                        parentid = member.Id;
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    SetResult(A.ProfitRecord.GetOrderByRecord2(DataSource, parentid, page, size));
                }
                catch (Exception ex)
                {
                    SetResult(CommUtility.PROGRAM_ERROR, new { ErrorMessage = ex.ToString() });
                }
            }
        }
#if (DEBUG)
        public static void GetOrdersByChildDistributorHelper()
        {
            CheckMemberApi(ClassName, "GetOrdersByChildDistributor", "获取我的加盟商的订单")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddArgument("parentid", typeof(string), "加盟商编号,传0为获取当前用户")
                .AddResult(true, typeof(string), "返回结果，跟获取会员全部订单数据一致");
        }
#endif

        public void GetMoney()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    long parentid = 0;
                    if (!long.TryParse(Request["parentid"], out parentid) || parentid <= 0)
                        parentid = member.Id;
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    string Date = Request["Date"];
                    string begindt = "", endtime = "";
                    int type = -1;
                    if ((!string.IsNullOrEmpty(Date)) && Date.IndexOf('|') != -1)
                    {
                        string[] time = Date.Split('|');
                        begindt = time[0];
                        endtime = time[1];
                    }
                    string orderid = "";
                    orderid = Request["OrderId"];
                    Money TotalMoney;
                    A.ProfitRecord profitrecord = A.ProfitRecord.GetListCountByUser(DataSource, parentid, orderid, begindt, endtime, type);
                    var MyMoney = new
                    {
                        AllDrawMoney = Cnaws.Passport.Modules.MemberDrawOrder.GetDrawMoney(DataSource, parentid),
                        FreezeMoney = Cnaws.Passport.Modules.MemberDrawOrder.GetInTreatmentMoney(DataSource, parentid),
                        ArrivalMoney = A.ProfitRecord.GetArrivalMoney(DataSource, parentid),
                        HoustonFreeze = A.ProfitRecord.GetHoustonFreezeMoney(DataSource, parentid),
                        PendingAudit = Cnaws.Passport.Modules.MemberDrawOrder.GetPendingAuditMoney(DataSource, parentid),
                        Data = A.ProfitRecord.GetListByUser(DataSource, parentid, orderid, begindt, endtime, type, Math.Max(1, page), size, 8),
                        TotalMoney = profitrecord.TotalMoney,
                        TotalProfitMoney = profitrecord.ProfitMoney
                    };
                    SetResult(MyMoney);

                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetMoneyHelper()
        {
            CheckMemberApi(ClassName, "GetMoney", "获取加盟商的收益表")
                .AddArgument("OrderId", typeof(int), "订单号,如果没有请传空")
                .AddArgument("Date", typeof(string), "查询时间,用|隔开")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddArgument("parentid", typeof(string), "加盟商编号,传0为获取当前用户")
                .AddResult(true, typeof(SplitPageData<AfterSales.Modules.ProfitRecord>), "AllDrawMoney:已提现的,PendingAudit:已申请:FreezeMoney:转账中,ArrivalMoney:可提现,HoustonFreeze:未到账,TotalMoney:总金额,TotalProfitMoney:总收益");
        }
#endif

        public dynamic GetDistributorNum(long userId)
        {
            return new
            {
                MemberNum = M.Member.GetNumByParentId(DataSource, userId),
                OrderNum = A.ProfitRecord.GetOrderNumByRecord(DataSource, userId),
                DistributorNum = P.Distributor.GetNumByParentId(DataSource, userId),
                ProfitNum = A.ProfitRecord.GetAllMoney(DataSource, userId)
            };
        }


        public void GetNumersByUser()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                SetResult(new
                {
                    MemberNum = M.Member.GetNumByParentId(DataSource, member.Id),
                    OrderNum = A.ProfitRecord.GetOrderNumByRecord(DataSource, member.Id),
                    DistributorNum = P.Distributor.GetNumByParentId(DataSource, member.Id),
                    ProfitNum = A.ProfitRecord.GetAllMoney(DataSource, member.Id)
                });
            }
        }
#if (DEBUG)
        public static void GetNumersByUserHelper()
        {
            CheckMemberApi(ClassName, "GetNumersByUser", "获取我的显示数量")
                .AddResult(true, typeof(string), "返回结果");
        }
#endif


        public void GetDistributorByParentId()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {
                    long parentid = 0;
                    if (!long.TryParse(Request["parentid"], out parentid) || parentid <= 0)
                        parentid = member.Id;
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    SplitPageData<P.Distributor> DistributorList = P.Distributor.GetPageByParent(DataSource, parentid, page, size);

                    SetResult(DistributorList);
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetDistributorByParentIdHelper()
        {
            CheckMemberApi(ClassName, "GetDistributorByParentId", "获取我的加盟店")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddArgument("parentid", typeof(string), "加盟商编号,传0为获取当前用户")
                .AddResult(true, typeof(string), "返回结果");
        }
#endif
        public void GetNewDistributorByParentId()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                try
                {

                    long parentid = 0;
                    if (!long.TryParse(Request["parentid"], out parentid) || parentid <= 0)
                        parentid = member.Id;
                    int size, page;
                    if (!int.TryParse(Request["size"], out size) || size < 1)
                        size = 10;
                    if (!int.TryParse(Request["page"], out page) || page < 1)
                        page = 1;
                    SplitPageData<dynamic> Data;
                    List<dynamic> newList = new List<dynamic>();
                    SplitPageData<P.Distributor> DistributorList = P.Distributor.GetPageByParent(DataSource, parentid, page, size);
                    if (DistributorList.Data.Count > 0)
                    {
                        foreach (P.Distributor distributor in DistributorList.Data)
                        {
                            newList.Add(new
                            {
                                Numbers = GetDistributorNum(distributor.UserId),
                                Distributor = distributor
                            });
                        }
                    }
                    Data = new SplitPageData<dynamic>(DistributorList.PageIndex, DistributorList.PageSize, newList, DistributorList.TotalCount, 8);
                    SetResult(Data);
                }
                catch (Exception)
                {
                    SetResult(false);
                }
            }
        }
#if (DEBUG)
        public static void GetNewDistributorByParentIdHelper()
        {
            CheckMemberApi(ClassName, "GetNewDistributorByParentId", "获取我的加盟店")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddArgument("parentid", typeof(string), "加盟商编号,传0为获取当前用户")
                .AddResult(true, typeof(string), "返回结果");
        }
#endif



        public void GetMyChildMember()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                int size, page;
                if (!int.TryParse(Request["size"], out size) || size < 1)
                    size = 10;
                if (!int.TryParse(Request["page"], out page) || page < 1)
                    page = 1;
                SetResult(M.MemberInfo.GetByParentId(DataSource, member.Id, page, size));
            }
        }
#if (DEBUG)
        public static void GetMyChildMemberHelper()
        {
            CheckMemberApi(ClassName, "GetMyChildMember", "获取我的下级用户")
                .AddArgument("size", typeof(int), "每页展示个数,默认为10")
                .AddArgument("page", typeof(int), "当前页,默认为1")
                .AddResult(true, typeof(string), "返回结果");
        }
#endif

        public void CheckPassword()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                string password = Request["PassWord"];
                if (!string.IsNullOrEmpty(password))
                {
                    if (password.Equals(member.Password, StringComparison.OrdinalIgnoreCase))
                    {
                        SetResult(true);
                    }
                    else
                    {
                        SetResult(CommUtility.PASSWORD_EQUALS);
                    }
                }
                else
                {
                    SetResult(CommUtility.PASSWORD_EQUALS);
                }
            }
        }
#if (DEBUG)
        public static void CheckPasswordHelper()
        {
            CheckMemberApi(ClassName, "CheckPassword", "供应商再次验证密码")
                .AddArgument("PassWord", typeof(int), "md5(密码)")
                .AddResult(true, typeof(bool), "成功");
        }
#endif

        [HttpPost]
        public void SetDistributor()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    P.Distributor value = P.Distributor.GetById(DataSource, member.Id);
                    if (value != null)
                    {
                        value = DbTable.Load<P.Distributor>(Request.Form);
                        value.UserId = member.Id;
                        value.Images = HttpUtility.UrlDecode(value.Images);
                        SetResult(value.UpdateWithState(DataSource));
                    }
                    else
                    {
                        SetResult(CommUtility.DISTRIBUTOR_NOTIS);
                    }
                }
            }
            catch (Exception ex)
            {
                SetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.ToString() });
            }
        }
#if (DEBUG)
        public static void SetDistributorHelper()
        {
            CheckMemberApi(ClassName, "SetDistributor", "设置供应商信息")
                .AddArgument("Company", typeof(string), "公司名称")
                .AddArgument("Signatories", typeof(int), "签约人")
                .AddArgument("SignatoriesPhone", typeof(int), "签约人手机")
                .AddArgument("Contact", typeof(int), "负责人")
                .AddArgument("ContactPhone", typeof(int), "负责人联系电话")
                .AddArgument("Province", typeof(int), "省Id")
                .AddArgument("City", typeof(int), "市Id")
                .AddArgument("County", typeof(int), "区Id")
                .AddArgument("Address", typeof(int), "详细地址")
                .AddArgument("PostId", typeof(int), "邮政编码")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(CommUtility.DISTRIBUTOR_NOTIS, "不是供应商")
                .AddResult(true, typeof(DataStatus), "返回结果");
        }
#endif

        [HttpPost]
        public void SetAddress()
        {
            try
            {
                M.Member member;
                if (CheckMember(out member))
                {
                    P.Distributor value = P.Distributor.GetById(DataSource, member.Id);
                    if (value != null)
                    {
                        value = DbTable.Load<P.Distributor>(Request.Form);
                        value.UserId = member.Id;
                        SetResult(value.UpdateAddressWithState(DataSource));
                    }
                    else
                    {
                        SetResult(CommUtility.DISTRIBUTOR_NOTIS);
                    }
                }
            }
            catch (Exception ex)
            {
                SetResult(CommUtility.PROGRAM_ERROR, new { Message = ex.ToString() });
            }
        }
#if (DEBUG)
        public static void SetAddressHelper()
        {
            CheckMemberApi(ClassName, "SetAddress", "设置供应商收货信息")
                .AddArgument("Consignee", typeof(string), "收货人")
                .AddArgument("Mobile", typeof(int), "收货人手机")
                .AddArgument("Address", typeof(int), "收货地址")
                .AddResult(CommUtility.PROGRAM_ERROR, "程序错误")
                .AddResult(CommUtility.DISTRIBUTOR_NOTIS, "不是供应商")
                .AddResult(true, typeof(DataStatus), "返回结果");
        }
#endif

        [HttpPost]
        public void CreateAccount()
        {
            M.Member member;
            if (CheckMember(out member))
            {
                DataSource.Begin();
                try
                {
                    M.Member newmember = DbTable.Load<M.Member>(Request.Form);
                    newmember.ParentId = 0;
                    newmember.Approved = true;
                    newmember.CreationDate = DateTime.Now;
                    if (newmember.Insert(DataSource) != DataStatus.Success)
                    {
                        throw new AggregateException();
                    }

                    P.Distributor value = DbTable.Load<P.Distributor>(Request.Form);
                    value.UserId = newmember.Id;
                    value.ParentId = member.Id;
                    value.State = P.DistributorState.NotApproved;
                    value.Images = HttpUtility.UrlDecode(value.Images);
                    value.Province = int.Parse(Request.Form["Province"]);
                    value.City = int.Parse(Request.Form["City"]);
                    value.County = int.Parse(Request.Form["County"]);
                    value.CreationDate = newmember.CreationDate;
                    value.Consignee = value.Signatories;
                    long.TryParse(value.SignatoriesPhone, out value.Mobile);
                    if (value.Insert(DataSource) != DataStatus.Success)
                    {
                        SetResult(CommUtility.INSERT_FAIL);
                        throw new AggregateException();
                    }

                    DataSource.Commit();
                    SetResult(true);
                }
                catch (AggregateException)
                {
                    DataSource.Rollback();
                    SetResult(false);
                }
                catch (Exception ex)
                {
                    DataSource.Rollback();
                    SetResult(false, new { Message = ex.ToString() });
                }
            }
        }
#if (DEBUG)
        public static void CreateAccountHelper()
        {
            CheckMemberApi(ClassName, "CreateAccount", "开通加盟商账号")
                .AddArgument("Name", typeof(string), "加盟商账号")
                .AddArgument("Password", typeof(string), "加盟商密码")
                .AddArgument("Company", typeof(string), "加盟商公司名称")
                .AddArgument("Images", typeof(string), "加盟商证件号码")
                .AddArgument("Signatories", typeof(string), "签约人")
                .AddArgument("SignatoriesPhone", typeof(long), "签约人联系电话")
                .AddArgument("Contact", typeof(string), "负责人")
                .AddArgument("ContactPhone", typeof(string), "负责人联系电话")
                .AddArgument("Province", typeof(long), "省Id")
                .AddArgument("City", typeof(string), "市Id")
                .AddArgument("County", typeof(string), "区Id")
                .AddArgument("Address", typeof(string), "加盟商地址")
                .AddArgument("PostId", typeof(string), "邮政编码")
                .AddResult(true, typeof(string), "返回结果");
        }
#endif

    }
}
