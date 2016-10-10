using System.Collections.Generic;
using System.Text;
using System;
using System.Web;
using System.Security.Cryptography;
using System.Collections.Specialized;
using Cnaws.Web;
namespace XcpNet.Common
{/// <summary>
 /// 一些公众的方法
 /// </summary>
    public class CommUtility: DataController
    {        
        /// <summary>
        /// 成功
        /// </summary>
        public const int SUCCESS = -200;
        /// <summary>
        /// 程序错误
        /// </summary>
        public const int PROGRAM_ERROR = -500;
        /// <summary>
        /// 已存在相同
        /// </summary>
        public const int EXISTS = -601;
        /// <summary>
        /// 手机号码格式错误
        /// </summary>
        public const int MOBILE_FORMAT = -1000;
        /// <summary>
        /// 获取验证码出错，请重新获取验证码
        /// </summary>
        public const int SMSCAPTCHA_QUERY = -1001;
        /// <summary>
        /// 验证码错误
        /// </summary>
        public const int SMSCAPTCHA_EQUALS = -1002;
        /// <summary>
        /// 标识mark为空
        /// </summary>
        public const int MARK_EMPTY = -1003;
        /// <summary>
        /// Token为空
        /// </summary>
        public const int TOKEN_EMPTY = -1004;
        /// <summary>
        /// 未找到用户
        /// </summary>
        public const int MEMBER_NOTFOUND = -1005;
        /// <summary>
        /// 标识mark错误
        /// </summary>
        public const int MARK_EQUALS = -1006;
        /// <summary>
        /// token错误
        /// </summary>
        public const int TOKEN_EQUALS = -1007;
        /// <summary>
        /// 密码格式错误
        /// </summary>
        public const int PASSWORD_FORMAT = -1008;
        /// <summary>
        /// 用户未审核
        /// </summary>
        public const int MEMBER_APPROVED = -1009;
        /// <summary>
        /// 用户已锁定
        /// </summary>
        public const int MEMBER_LOCKED = -1010;
        /// <summary>
        /// 密码错误
        /// </summary>
        public const int PASSWORD_EQUALS = -1011;
        /// <summary>
        /// 操作数据失败
        /// </summary>
        public const int DATA_RESULT = -1012;
        /// <summary>
        /// 短信模块初始化失败
        /// </summary>
        public const int SMSAPI_INIT = -1013;
        /// <summary>
        /// 生成验证码失败
        /// </summary>
        public const int SMSCAPTCHA_NEW = -1014;
        /// <summary>
        /// 短信接口配置错误
        /// </summary>
        public const int SMS_SEND = -1015;
        /// <summary>
        /// 短信发送失败
        /// </summary>
        public const int SMSAPI_RESULT = -1016;
        /// <summary>
        /// 参数未找到
        /// </summary>
        public const int PARAMETER_NOFOND = -1017;
        /// <summary>
        /// 插入数据失败
        /// </summary>
        public const int INSERT_FAIL = -1018;
        /// <summary>
        /// 修改数据失败
        /// </summary>
        public const int UPDATE_FAIL = -1019;
        /// <summary>
        /// 修改数据失败
        /// </summary>
        public const int DEL_FAIL = -1020;
        /// <summary>
        /// 运行错误
        /// </summary>
        public const int RUN_ERROR = -1021;
        /// <summary>
        /// 用户已被注册
        /// </summary>
        public const int USER_EXISTENCE = -1022;
        /// <summary>
        /// 购买的商品为空
        /// </summary>
        public const int PRODUCT_EMPTY = -1023;
        /// <summary>
        /// 购买商品的数量为空
        /// </summary>
        public const int PRODUCT_SUM_EMPTY = -1024;
        /// <summary>
        /// 购买的商品数量错误
        /// </summary>
        public const int PRODUCT_SUM_ERROR = -1025;
        /// <summary>
        /// 商品错误
        /// </summary>
        public const int PRODUCT_ERROR = -1026;
        /// <summary>
        /// 商品库存不足
        /// </summary>
        public const int PRODUCT_INVENTORY_ENOUGH = -1027;
        /// <summary>
        /// 创建订单失败
        /// </summary>
        public const int ORDER_ADDERROT = -1028;
        /// <summary>
        /// 创建订单详情失败
        /// </summary>
        public const int ORDER_INFO_ADDERROT = -1029;
        /// <summary>
        /// 地址为空
        /// </summary>
        public const int ADDRESS_EMPTY = -1030;
        /// <summary>
        /// 订单不存在
        /// </summary>
        public const int ORDER_EMPTY = -1031;
        /// <summary>
        /// 更新订单失败
        /// </summary>
        public const int ORDER_UPDATE_ERROR = -1032;
        /// <summary>
        /// 添加快递费失败
        /// </summary>
        public const int FREIGHT_ADDERROR = -1033;
        /// <summary>
        /// 根据对应的Mark找不到供应商
        /// </summary>
        public const int DISTRIBUTOR_EMPTY = -1034;
        /// <summary>
        /// 该账号已经存在
        /// </summary>
        public const int MEMBER_EXISTENCE = -1035;
        /// <summary>
        /// 不是加盟商
        /// </summary>
        public const int DISTRIBUTOR_NOTIS = -1036;
        /// <summary>
        /// 不是供应商
        /// </summary>
        public const int SUPPLIER_NOTIS = -1037;
        /// <summary>
        /// 在指定的时间内已发送过短信
        /// </summary>
        public const int SMS_EXISTENCE = -1038;
        /// <summary>
        /// sign返回失败
        /// </summary>
        public const int SIGN_ERROR = -1039;
        /// <summary>
        /// 未绑定手机号
        /// </summary>
        public const int VERMOB_NOT = -1040;
        /// <summary>
        /// 已绑定手机号
        /// </summary>
        public const int VERMOB_BIND = -1041;
        /// <summary>
        /// 银行卡为空
        /// </summary>
        public const int BANK_EMPTY = -1042;
        /// <summary>
        /// 产品已申请售后
        /// </summary>
        public const int PRODUCT_SERVICED = -1043;
        /// <summary>
        /// 余额不足
        /// </summary>
        public const int MONEY_NOT_ENOUGH = -1044;
        /// <summary>
        /// 找不到物流信息
        /// </summary>
        public const int LOGISTICS_EMPTY = -1045;
        /// <summary>
        /// 退款金额超过最可退款数额
        /// </summary>
        public const int REFUND_EXCEED_MONEY = -1046;
        /// <summary>
        /// 退款金额超过最可退换货数量
        /// </summary>
        public const int REFUND_EXCEED_COUND = -1047;
        /// <summary>
        /// 参数错误
        /// </summary>
        public const int PARAMETER_ERROR = -1048;
        /// <summary>
        /// 今日已成功提醒过商家，请等待商家发货
        /// </summary>
        public const int REMINDER_REPEAT = -1049;
        /// <summary>
        /// 收益快照创建失败
        /// </summary>
        public const int ORDER_SETTLEMENT_ADDERROR = -1050;
        /// <summary>
        /// 充值失败
        /// </summary>
        public const int RECHARGE_FAIL = -1051;
        /// <summary>
        /// 找不到支付方式
        /// </summary>
        public const int PAYMENT_ERROR = -1052;
        /// <summary>
        /// 购物车ID错误
        /// </summary>
        public const int CART_EMPTY = -1053;
        /// <summary>
        /// 已经逾期
        /// </summary>
        public const int ALREADY_OVERDUE = -1054;
        /// <summary>
        /// 该售后订单不能退邮费
        /// </summary>
        public const int FREIGHT_RETURN_NOT = -1055;
        /// <summary>
        /// 存在金额不足最少下单金额的订单
        /// </summary>
        public const int PRICE_NOTENOUGH = -1056;
        /// <summary>
        /// 版本过低
        /// </summary>
        public const int VERSION_LOW=-1057;

        public static bool CheckMobile(long mobile)
        {
            return mobile >= 13000000000 && mobile < 19000000000;
        }

        public CommUtility(DataController controller)
        {
            InitController(controller);
        }
        /// <summary>
        /// 公众返回
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        public void CommSetResult(object code, object data)
        {
            if (data == null)
            {
                if (code.GetType() == typeof(int))
                    SetResult((int)code);
                else if (code.GetType() == typeof(DataStatus))
                    SetResult((DataStatus)code);
                else
                    SetResult(code);
            }
            else
                SetResult((int)code, data);
        }

    }
}
